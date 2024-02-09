#nullable enable
#if NET8_0_OR_GREATER

using System;
using System.Threading.Tasks;
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
        app.UseServiceStack(new AppHost(), options => { options.MapEndpoints(); });

        app.StartAsync(TestsConfig.ListeningOn);
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

#endif