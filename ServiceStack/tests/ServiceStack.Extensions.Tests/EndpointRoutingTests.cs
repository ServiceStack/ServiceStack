#nullable enable
#if NET8_0_OR_GREATER

using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.FluentValidation;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Extensions.Tests;

public class EndpointRoutingTests
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
    }
    
    class AppHost() : AppHostBase(nameof(EndpointRoutingTests), typeof(AutoQueryService).Assembly)
    {
        public override void Configure()
        {
        }
    }

    public EndpointRoutingTests()
    {
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        var services = builder.Services;
        var config = builder.Configuration;

        var dbPath = contentRootPath.CombineWith("App_Data/northwind.sqlite");

        var connectionString = $"DataSource={dbPath};Cache=Shared";
        var dbFactory = new OrmLiteConnectionFactory(connectionString, SqliteDialect.Provider);
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString /*, b => b.MigrationsAssembly(nameof(MyApp))*/));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("endpoint-orders", _ => RateLimitPartition.GetFixedWindowLimiter(
                "orders", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 4, Window = TimeSpan.FromHours(1), QueueLimit = 0,
                }));
            options.AddPolicy("endpoint-batch", _ => RateLimitPartition.GetFixedWindowLimiter(
                "batch", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1, Window = TimeSpan.FromHours(1), QueueLimit = 0,
                }));
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.RegisterValidator(c => new TestValidationValidator(true));
        services.AddTransient<TestTransientDisposable>();
        services.AddScoped<TestScopedDisposable>();
        services.AddSingleton<TestSingletonDisposable>();

        ServiceStackHost.InitOptions.ScriptContext.ScriptMethods.AddRange([
            new MyValidators(),
        ]);
        
        services.AddServiceStack(typeof(MyServices).Assembly, c =>
        {
            c.AddSwagger(o =>
            {
                //o.AddJwtBearer();
                //o.AddBasicAuth();
            });
        });

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseServiceStack(new AppHost(), options =>
        {
            options.MapEndpoints();
            options.RateLimitTag("endpoint-orders", "endpoint-orders");
            options.RateLimitTag("endpoint-batch", "endpoint-batch");
        });

        app.StartAsync(TestsConfig.ListeningOn);
    }
 
    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    [Test]
    public async Task Tagged_endpoints_share_a_rate_limit_on_api_and_format_routes()
    {
        using var client = new HttpClient();
        var root = TestsConfig.ListeningOn.TrimEnd('/');
        Assert.That((int)(await client.GetAsync(root + "/api/RateLimitedOrderA")).StatusCode, Is.EqualTo(200));
        Assert.That((int)(await client.GetAsync(root + "/api/RateLimitedOrderB.json")).StatusCode, Is.EqualTo(200));
        Assert.That((int)(await client.GetAsync(root + "/rate-limited-c")).StatusCode, Is.EqualTo(200));
        Assert.That((int)(await client.GetAsync(root + "/rate-limited-c.json")).StatusCode, Is.EqualTo(200));
        Assert.That(RateLimitedOrderService.Calls, Is.EqualTo(4));
        Assert.That((int)(await client.GetAsync(root + "/api/RateLimitedOrderA")).StatusCode, Is.EqualTo(429));
        Assert.That((int)(await client.GetAsync(root + "/api/RateLimitedOrderC.json")).StatusCode, Is.EqualTo(429));
        Assert.That((int)(await client.PostAsync(root + "/rate-limited-c", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"))).StatusCode, Is.EqualTo(429));
        Assert.That((int)(await client.GetAsync(root + "/json/reply/RateLimitedOrderA")).StatusCode, Is.Not.EqualTo(200));
        Assert.That(RateLimitedOrderService.Calls, Is.EqualTo(4));
    }

    [Test]
    public async Task Autobatch_endpoint_uses_the_named_policy()
    {
        using var client = new HttpClient();
        var url = TestsConfig.ListeningOn.TrimEnd('/') + "/api/RateLimitedBatch[]";
        using var body = new StringContent("[{}]", System.Text.Encoding.UTF8, "application/json");
        Assert.That((int)(await client.PostAsync(url, body)).StatusCode, Is.EqualTo(200));
        Assert.That((int)(await client.PostAsync(url, new StringContent("[{}]", System.Text.Encoding.UTF8, "application/json"))).StatusCode, Is.EqualTo(429));
    }

    [Test]
    public async Task Endpoints_does_dispose_of_property_injected_services()
    {
        TestTransientDisposable.IsDisposed = TestScopedDisposable.IsDisposed = TestSingletonDisposable.IsDisposed = false;
        
        var client = new JsonApiClient(TestsConfig.ListeningOn);
        var response = await client.SendAsync(new TestIoc());

        response.PrintDump();
        Assert.That(response.Results, Is.EquivalentTo((string[])[
            nameof(IAppSettings),
            nameof(TestTransientDisposable),
            nameof(TestScopedDisposable),
            nameof(TestSingletonDisposable),
        ]));
        
        Assert.That(TestTransientDisposable.IsDisposed);
        Assert.That(TestScopedDisposable.IsDisposed);
        Assert.That(TestSingletonDisposable.IsDisposed, Is.False);
    }

    [Test]
    public void Can_resolve_manually_registered_validator()
    {
        var validator = ValidatorCache.GetValidator(new BasicHttpRequest(), typeof(TestValidation));
        Assert.That(validator, Is.Not.Null);
    }

}

public class TestTransientDisposable : IDisposable
{
    public static bool IsDisposed { get; set; }
    public void Dispose() => IsDisposed = true;
}

public class TestScopedDisposable : IDisposable
{
    public static bool IsDisposed { get; set; }
    public void Dispose() => IsDisposed = true;
}

public class TestSingletonDisposable : IDisposable
{
    public static bool IsDisposed { get; set; }
    public void Dispose() => IsDisposed = true;
}

public class TestIoc : IReturn<StringsResponse> { }

public class TestIocService : Service 
{
    [FromServices]
    public IAppSettings? AppSettings { get; set; }
    
    [FromServices]
    public TestTransientDisposable? TestTransientDisposable { get; set; }
    
    [FromServices]
    public TestScopedDisposable? TestScopedDisposable { get; set; }
    
    [FromServices]
    public TestSingletonDisposable? TestSingletonDisposable { get; set; }

    public object Any(TestIoc request)
    {
        var to = new StringsResponse();
        if (AppSettings != null)
            to.Results.Add(nameof(IAppSettings));
        if (TestTransientDisposable != null)
            to.Results.Add(nameof(TestTransientDisposable));
        if (TestScopedDisposable != null)
            to.Results.Add(nameof(TestScopedDisposable));
        if (TestSingletonDisposable != null)
            to.Results.Add(nameof(TestSingletonDisposable));
        return to;
    }
}


[IgnoreServices] // prevent auto registration
public sealed class TestValidationValidator : AbstractValidator<TestValidation>
{
    public TestValidationValidator(bool isVaf) // Illegal constructor arg preventing auto registration
    {
        RuleFor(p => p.Id).GreaterThan(0);
        RuleFor(p => p.Date).Null().When(p => isVaf);
        RuleFor(p => p.Date).NotNull().When(p => !isVaf && p.ArticleId.IsNullOrEmpty());
    }
}

public class TestValidation
{
    public int Id { get; set; }
    public string ArticleId { get; set; }
    public DateTime Date { get; set; }
}

[Tag("endpoint-orders")]
[RateLimiting("endpoint-orders")]
public class RateLimitedOrderA : IGet, IReturn<StringResponse> { }

[Tag("endpoint-orders")]
public class RateLimitedOrderB : IGet, IReturn<StringResponse> { }

[Tag("endpoint-orders")]
[EnableRateLimiting("endpoint-orders")]
[Route("/rate-limited-c", "GET,POST")]
public class RateLimitedOrderC : IGet, IReturn<StringResponse> { }

public class RateLimitedOrderService : Service
{
    public static int Calls;
    public object Get(RateLimitedOrderA request) { System.Threading.Interlocked.Increment(ref Calls); return new StringResponse { Result = "A" }; }
    public object Get(RateLimitedOrderB request) { System.Threading.Interlocked.Increment(ref Calls); return new StringResponse { Result = "B" }; }
    public object Get(RateLimitedOrderC request) { System.Threading.Interlocked.Increment(ref Calls); return new StringResponse { Result = "C" }; }
    public object Post(RateLimitedOrderC request) { System.Threading.Interlocked.Increment(ref Calls); return new StringResponse { Result = "C" }; }
}

[Tag("endpoint-batch")]
public class RateLimitedBatch : IPost, IReturn<StringResponse> { }
public class RateLimitedBatchService : Service
{
    public object Post(RateLimitedBatch request) => new StringResponse { Result = "batch" };
}

[TestFixture]
public class RateLimitBindingTests
{
    static Operation Op<T>(params string[] tags) => new()
    {
        RequestType = typeof(T), ServiceType = typeof(RateLimitedOrderService),
        Method = "GET", Tags = new System.Collections.Generic.List<string>(tags),
    };

    [Test]
    public void Conflicting_tags_fail_and_an_operation_binding_overrides_them()
    {
        var options = new ServiceStackOptions();
        options.MapEndpoints();
        options.RateLimitTag("one", "first");
        options.RateLimitTag("two", "second");
        var op = Op<RateLimitMultipleTags>("one", "two");
        Assert.That(Assert.Throws<InvalidOperationException>(() => options.ValidateRateLimiting([op]))!.Message,
            Does.Contain(nameof(RateLimitMultipleTags)).And.Contain("one=first").And.Contain("two=second"));

        var overridden = new ServiceStackOptions();
        overridden.MapEndpoints();
        overridden.RateLimitTag("one", "first");
        overridden.RateLimitTag("two", "second");
        overridden.RateLimitOperation<RateLimitMultipleTags>("chosen");
        Assert.DoesNotThrow(() => overridden.ValidateRateLimiting([op]));
    }

    [Test]
    public void Attribute_conflicts_and_unsafe_routing_fail_at_startup()
    {
        var options = new ServiceStackOptions();
        options.MapEndpoints();
        Assert.DoesNotThrow(() => options.ValidateRateLimiting([Op<RateLimitEqualAttributes>()]));
        Assert.That(Assert.Throws<InvalidOperationException>(() =>
            new ServiceStackOptions().ValidateRateLimiting([Op<RateLimitEqualAttributes>()]))!.Message,
            Does.Contain("MapEndpoints"));
        Assert.That(Assert.Throws<InvalidOperationException>(() =>
            new ServiceStackOptions().ValidateRateLimiting([Op<RateLimitConflictingAttributes>()]))!.Message,
            Does.Contain("conflicting rate-limiting attributes"));
        Assert.That(Assert.Throws<InvalidOperationException>(() =>
            new ServiceStackOptions().ValidateRateLimiting([Op<RateLimitDisabledConflict>()]))!.Message,
            Does.Contain("disables rate limiting"));
    }
}

public class RateLimitMultipleTags { }
[RateLimiting("same")]
[EnableRateLimiting("same")]
public class RateLimitEqualAttributes { }
[RateLimiting("one")]
[EnableRateLimiting("two")]
public class RateLimitConflictingAttributes { }
[RateLimiting("one")]
[DisableRateLimiting]
public class RateLimitDisabledConflict { }

#endif
