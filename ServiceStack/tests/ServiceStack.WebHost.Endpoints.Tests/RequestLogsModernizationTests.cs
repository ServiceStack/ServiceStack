using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Admin;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Redis;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class SensitiveAuthDto
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class StandardTestDto
{
    public int Id { get; set; }
    public string Value { get; set; }
}

[TestFixture]
public class RequestLogsModernizationTests
{
    [Test]
    public void InMemoryRollingRequestLogger_NullGuards()
    {
        var logger = new InMemoryRollingRequestLogger(10);

        Assert.DoesNotThrow(() => logger.Log(null, null, null, TimeSpan.Zero));
        Assert.DoesNotThrow(() => logger.Log(null, new StandardTestDto(), null, TimeSpan.Zero));

        Assert.That(logger.ShouldSkip(null, null), Is.False);
        Assert.That(logger.ShouldSkip(null, new StandardTestDto()), Is.False);

        var mockReq = new MockHttpRequest();
        Assert.DoesNotThrow(() => logger.Log(mockReq, null, null, TimeSpan.Zero));
        Assert.That(logger.GetLatestLogs(null).Count, Is.EqualTo(1));
    }

    [Test]
    public void InMemoryRollingRequestLogger_CircularBufferCapacity_EnforcesBounds()
    {
        var capacity = 5;
        var logger = new InMemoryRollingRequestLogger(capacity);

        for (var i = 0; i < 20; i++)
        {
            var mockReq = new MockHttpRequest { PathInfo = $"/api/test/{i}" };
            logger.Log(mockReq, new StandardTestDto { Id = i }, null, TimeSpan.FromMilliseconds(i));
        }

        var logs = logger.GetLatestLogs(null);
        Assert.That(logs.Count, Is.EqualTo(capacity));
    }

    [Test]
    public void InMemoryRollingRequestLogger_GetLatestLogs_ClampsNegativeValues()
    {
        var logger = new InMemoryRollingRequestLogger(10);
        var mockReq = new MockHttpRequest();
        logger.Log(mockReq, new StandardTestDto { Id = 1 }, null, TimeSpan.Zero);

        Assert.That(logger.GetLatestLogs(-5).Count, Is.EqualTo(0));
        Assert.That(logger.GetLatestLogs(0).Count, Is.EqualTo(0));
        Assert.That(logger.GetLatestLogs(1).Count, Is.EqualTo(1));
        Assert.That(logger.GetLatestLogs(10).Count, Is.EqualTo(1));
    }

    [Test]
    public void InMemoryRollingRequestLogger_HideSensitiveRequestBody()
    {
        var logger = new InMemoryRollingRequestLogger(10)
        {
            EnableRequestBodyTracking = true,
            HideRequestBodyForRequestDtoTypes = [typeof(SensitiveAuthDto)],
        };

        var mockReq = new MockHttpRequest
        {
            FormData = new System.Collections.Specialized.NameValueCollection { { "Password", "Secret123" } }
        };

        logger.Log(mockReq, new SensitiveAuthDto { Username = "alice", Password = "Secret123" }, null, TimeSpan.Zero);

        var logs = logger.GetLatestLogs(1);
        Assert.That(logs[0].RequestDto, Is.Null);
        Assert.That(logs[0].FormData, Is.Null);
    }

    [Test]
    public void InMemoryRollingRequestLogger_IgnoreFilter_StripsFilteredProperties()
    {
        var logger = new InMemoryRollingRequestLogger(10)
        {
            EnableErrorTracking = true,
            IgnoreFilter = o => o is string s && s.Contains("REDACT"),
        };

        var mockReq = new MockHttpRequest();
        var ex = new Exception("Error message with REDACT content");
        ex.Data["SecretKey"] = "REDACT_THIS_VALUE";
        ex.Data["NormalKey"] = "KeepThisValue";

        logger.Log(mockReq, "REDACT_REQUEST", ex, TimeSpan.Zero);

        var logs = logger.GetLatestLogs(1);
        Assert.That(logs[0].RequestDto, Is.Null);
        Assert.That(logs[0].ExceptionData, Is.Not.Null);
        Assert.That(logs[0].ExceptionData.Contains("SecretKey"), Is.False);
        Assert.That(logs[0].ExceptionData.Contains("NormalKey"), Is.True);
    }

    [Test]
    public void InMemoryRollingRequestLogger_ToSerializableErrorResponse_HandlesNullAndExceptions()
    {
        Assert.That(InMemoryRollingRequestLogger.ToSerializableErrorResponse(null), Is.Null);

        var ex = new InvalidOperationException("Something went wrong");
        var status = InMemoryRollingRequestLogger.ToSerializableErrorResponse(ex) as ResponseStatus;
        Assert.That(status, Is.Not.Null);
        Assert.That(status.ErrorCode, Is.EqualTo("InvalidOperationException"));
        Assert.That(status.Message, Is.EqualTo("Something went wrong"));
    }

    [Test]
    public void CsvRequestLogger_LifecycleAndNullGuards()
    {
        var vfs = new MemoryVirtualFiles();
        using var csvLogger = new CsvRequestLogger(vfs, appendEvery: TimeSpan.FromMilliseconds(500));

        Assert.DoesNotThrow(() => csvLogger.Log(null, null, null, TimeSpan.Zero));

        var mockReq = new MockHttpRequest { PathInfo = "/test-csv" };
        csvLogger.Log(mockReq, new StandardTestDto { Id = 1, Value = "One" }, null, TimeSpan.FromMilliseconds(10));
        csvLogger.Flush();

        Assert.That(csvLogger.GetLatestLogs(-1).Count, Is.EqualTo(0));
        Assert.That(vfs.Files.Count, Is.GreaterThan(0));
        Assert.That(csvLogger.GetLatestLogs(1).Count, Is.EqualTo(1));
    }

    [Test]
    public void RequestLogsFeature_NullGuards()
    {
        var feature = new RequestLogsFeature();
        Assert.DoesNotThrow(() => feature.Register(null));
        Assert.DoesNotThrow(() => feature.BeforePluginsLoaded(null));
        Assert.That(feature.DefaultIgnoreFilter(null), Is.False);
    }

    [Test]
    public void RedisErrorLoggerFeature_NullGuards()
    {
        var redisFeature = new RedisErrorLoggerFeature(new BasicRedisClientManager());
        Assert.DoesNotThrow(() => redisFeature.Register(null));
        Assert.DoesNotThrow(() => redisFeature.HandleServiceException(null, null, null));
        Assert.DoesNotThrow(() => redisFeature.HandleUncaughtException(null, null, null, null));
    }

    [Test]
    public async Task RequestLogsService_Any_HandlesNullAndPaginationClamping()
    {
        var inMemoryLogger = new InMemoryRollingRequestLogger(10);
        var mockReq = new MockHttpRequest();
        inMemoryLogger.Log(mockReq, new StandardTestDto { Id = 100, Value = "Val" }, null, TimeSpan.FromMilliseconds(5));

        var service = new RequestLogsService(inMemoryLogger)
        {
            Request = mockReq
        };

        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new RequestLogsFeature { AccessRole = RoleNames.AllowAnon })
        }.Init();

        var response = await service.Any(new RequestLogs
        {
            Take = -5,
            Skip = -2
        }) as RequestLogsResponse;

        Assert.That(response, Is.Not.Null);
        Assert.That(response.Total, Is.EqualTo(1));
        Assert.That(response.Results.Count, Is.EqualTo(0)); // Take = 0 clamped from -5

        var normalResponse = await service.Any(new RequestLogs
        {
            Take = 5,
            Skip = 0
        }) as RequestLogsResponse;

        Assert.That(normalResponse, Is.Not.Null);
        Assert.That(normalResponse.Results.Count, Is.EqualTo(1));
        Assert.That(normalResponse.Results[0].OperationName, Is.EqualTo(mockReq.OperationName));
    }
}
