using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class InProcessServiceGatewayPublishFiltersTests
{
    public class PublishFilterRequest : IReturnVoid
    {
        public int Id { get; set; }
    }

    public class PublishFilterService : Service
    {
        public List<int> ExecutedIds { get; set; }

        public void Any(PublishFilterRequest request) => ExecutedIds.Add(request.Id);
    }

    public enum FilterResult { Allow, Forbidden, CloseResponse }

    [Test]
    public async Task Applies_gateway_request_filters_before_publish(
        [Values] bool useAsync, [Values] bool batch, [Values] bool useMq, [Values] FilterResult result)
    {
        var executedIds = new List<int>();
        var queuedIds = new List<int>();
        var filteredDtos = new List<object>();
        var dto = new PublishFilterRequest { Id = 1 };
        var batchDtos = new[] { dto, new PublishFilterRequest { Id = 2 } };

        using var mq = new InMemoryTransientMessageService();
        mq.RegisterHandler<PublishFilterRequest>(message =>
        {
            queuedIds.Add(message.GetBody().Id);
            return null;
        });

        using var appHost = new BasicAppHost
        {
            ConfigureContainer = container =>
            {
                container.Register(executedIds);
                if (useMq)
                    container.Register<IMessageFactory>(mq.MessageFactory);
            },
            ConfigureAppHost = host =>
            {
                host.RegisterService<PublishFilterService>();
                host.GatewayRequestFilters.Add((req, request) =>
                {
                    filteredDtos.Add(request);
                    if (result == FilterResult.CloseResponse)
                        req.Response.Close();
                });
                host.GatewayRequestFiltersAsync.Add(async (_, request) =>
                {
                    await Task.Delay(1).ConfigureAwait(false);
                    Assert.That(executedIds, Is.Empty);
                    Assert.That(queuedIds, Is.Empty);
                    filteredDtos.Add(request);
                    if (result == FilterResult.Forbidden)
                        throw HttpError.Forbidden("blocked by gateway filter");
                });
            },
        }.Init();

        var gateway = new InProcessServiceGateway(new BasicRequest());
        async Task Publish()
        {
            if (useAsync && batch)
                await gateway.PublishAllAsync(batchDtos);
            else if (useAsync)
                await gateway.PublishAsync(dto);
            else if (batch)
                gateway.PublishAll(batchDtos);
            else
                gateway.Publish(dto);
        }

        if (result == FilterResult.Forbidden)
        {
            var error = Assert.ThrowsAsync<HttpError>(Publish);
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }
        else
        {
            await Publish();
        }

        Assert.That(filteredDtos, Has.Count.EqualTo(result == FilterResult.CloseResponse ? 1 : 2));
        foreach (var filteredDto in filteredDtos)
        {
            if (batch)
            {
                Assert.That(filteredDto, Is.TypeOf<PublishFilterRequest[]>());
                Assert.That(filteredDto, Is.EqualTo(batchDtos));
            }
            else
            {
                Assert.That(filteredDto, Is.SameAs(dto));
            }
        }

        var expectedIds = result == FilterResult.Allow ? (batch ? new[] { 1, 2 } : new[] { 1 }) : [];
        Assert.That(executedIds, Is.EqualTo(useMq ? [] : expectedIds));
        Assert.That(queuedIds, Is.EqualTo(useMq ? expectedIds : []));
    }
}
