using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Extensions.Tests;

public class HelloApiKey : IReturn<HelloResponse>
{
    public string Name { get; set; }
}

[ValidateApiKey]
public class SecuredApiKey : IReturn<HelloResponse>
{
    public string Name { get; set; }
}

[ValidateApiKey(ApiKeyTests.TheScope)]
public class ScopedApiKey : IReturn<HelloResponse>
{
    public string Name { get; set; }
}

[ValidateAuthSecret]
public class AuthSecretApiKey : IReturn<HelloResponse>
{
    public string Name { get; set; }
}

[ValidateIsAdmin]
public class AdminApiKey : IReturn<HelloResponse>
{
    public string Name { get; set; }
}

public class ApiKeyServices : Service
{
    public object Any(HelloApiKey request) => 
        new HelloResponse { Result = $"Hello, {request.Name}!" };

    public object Any(SecuredApiKey request) => 
        new HelloResponse { Result = $"Hello, {request.Name}!" };

    public object Any(ScopedApiKey request) => 
        new HelloResponse { Result = $"Hello, {request.Name}!" };

    public object Any(AuthSecretApiKey request) => 
        new HelloResponse { Result = $"Hello, {request.Name}!" };

    public object Any(AdminApiKey request) => 
        new HelloResponse { Result = $"Hello, {request.Name}!" };
}

public class ApiKeyTests
{
    public const string TheScope = nameof(TheScope);
    public const string AltScope = nameof(AltScope);
    
    public const string AnonKey = "ak-200F62B274954568A70E1BAE38BB983D";
    public const string UserKey = "ak-C56C9065463A4AEEAD4D33C0DCB1FCD9";
    public const string ScopeKey = "ak-0CC6FFF739304FC180FE8F778B7A360B";
    public const string AltKey = "ak-7B1CB47BDD6B4742914AEBE5E7D4D591";
    public const string AdminKey = "ak-CCEE28F476C2413191D62F57803297E4";

    public const string AuthSecret = "zsecret";
    
    public static string[] AllKeys = [AnonKey, UserKey, ScopeKey, AltKey, AdminKey];
    
    class AppHost() : AppHostBase(nameof(ApiKeyTests))
    {
        public override void Configure()
        {
            var services = this.GetApplicationServices();
            var apiKeysFeature = GetPlugin<ApiKeysFeature>();
            apiKeysFeature.InitSchema();
            
            var dbFactory = services.GetRequiredService<IDbConnectionFactory>();
            using var db = dbFactory.OpenDbConnection();
            apiKeysFeature.InsertAll(db, [
                new() { Key = AnonKey },
                new() { Key = UserKey, UserId = "89C1698D-9FD1-43B1-8C8B-C76EFA65E99B", UserName = "apiuser" },
                new() { Key = ScopeKey, UserId = "37AF1EF4-6E67-4171-B526-B830275CE9AE", UserName = "apiscope", Scopes = [TheScope] },
                new() { Key = AltKey, UserId = "F4889349-367F-4918-A9F5-D96005F74C33", UserName = "altscope", Scopes = [AltScope] },
                new() { Key = AdminKey, UserId = "40E566F2-DD08-4432-9D9C-528B3B0CCBEE", UserName = "admin", Scopes = [Roles.Admin] },
            ]);
            
            SetConfig(new() {
                AdminAuthSecret = AuthSecret,
                DebugMode = true,
            });
        }
    }

    public ApiKeyTests()
    {
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        var services = builder.Services;
        var config = builder.Configuration;

        services.AddPlugin(new ApiKeysFeature());
        
        var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddServiceStack(typeof(ApiKeyServices).Assembly);

        var app = builder.Build();
        app.UseServiceStack(new AppHost(), options => {
            options.MapEndpoints();
        });

        app.StartAsync(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    [Test]
    public async Task Does_not_allow_access_to_Api_Secured_with_ApiKey()
    {
        var client = new JsonApiClient(TestsConfig.ListeningOn);
        try
        {
            var apiSecure = await client.ApiAsync(new SecuredApiKey { Name = "Secured" });
            apiSecure.ThrowIfError();
            Assert.Fail("Should throw");
        }
        catch (WebServiceException e)
        {
            Assert.That(e.Message, Is.EqualTo("Invalid API Key"));
        }
    }

    [Test]
    [TestCaseSource(nameof(AllKeys))]
    public async Task Does_allow_access_to_Api_Secured_with_ApiKey(string apiKey)
    {
        var client = new JsonApiClient(TestsConfig.ListeningOn)
        {
            BearerToken = apiKey
        };
        
        var apiPublic = await client.ApiAsync(new HelloApiKey { Name = "World" });
        apiPublic.ThrowIfError();
        Assert.That(apiPublic.Response.Result, Is.EqualTo("Hello, World!"));
        
        var apiSecure = await client.ApiAsync(new SecuredApiKey { Name = "Secured" });
        apiSecure.ThrowIfError();
        Assert.That(apiSecure.Response.Result, Is.EqualTo("Hello, Secured!"));
    }

    [Test]
    [TestCaseSource(nameof(AllKeys))]
    public async Task Only_allows_admin_or_matching_scope_ApiKey(string apiKey)
    {
        var client = new JsonApiClient(TestsConfig.ListeningOn)
        {
            BearerToken = apiKey
        };

        if (apiKey is ScopeKey or AdminKey)
        {
            var apiScope = await client.ApiAsync(new ScopedApiKey { Name = "Scoped" });
            apiScope.ThrowIfError();
            Assert.That(apiScope.Response.Result, Is.EqualTo("Hello, Scoped!"));
        }
        else
        {
            try
            {
                var apiSecure = await client.ApiAsync(new ScopedApiKey { Name = "Scoped" });
                apiSecure.ThrowIfError();
                Assert.Fail("Should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.Message, Is.EqualTo("Invalid API Key"));
            }
        }
    }

    [Test]
    public async Task Only_allows_AuthSecret_to_call_AuthSecretApiKey()
    {
        var client = new JsonApiClient(TestsConfig.ListeningOn);
        var apiAnon = await client.ApiAsync(new AuthSecretApiKey { Name = "AuthSecret" });
        Assert.Throws<WebServiceException>(() => apiAnon.ThrowIfError());
        
        client = new JsonApiClient(TestsConfig.ListeningOn) {
            BearerToken = AdminKey
        };
        var apiAdmin = await client.ApiAsync(new AuthSecretApiKey { Name = "AuthSecret" });
        Assert.Throws<WebServiceException>(() => apiAdmin.ThrowIfError());
        
        client = new JsonApiClient(TestsConfig.ListeningOn) {
            BearerToken = AuthSecret
        };
        var apiAuthSecret = await client.ApiAsync(new AuthSecretApiKey { Name = "AuthSecret" });
        apiAuthSecret.ThrowIfError();
        Assert.That(apiAuthSecret.Response.Result, Is.EqualTo("Hello, AuthSecret!"));
    }

    [Test]
    public async Task Allows_Admin_ApiKey_and_AuthSecret_to_call_IsAdmin_Apis()
    {
        var client = new JsonApiClient(TestsConfig.ListeningOn);
        
        var apiAnon = await client.ApiAsync(new AdminApiKey { Name = "Admin" });
        Assert.Throws<WebServiceException>(() => apiAnon.ThrowIfError());

        client = new JsonApiClient(TestsConfig.ListeningOn) {
            BearerToken = ScopeKey
        };
        var apiUser = await client.ApiAsync(new AdminApiKey { Name = "Admin" });
        Assert.Throws<WebServiceException>(() => apiUser.ThrowIfError());
        
        client = new JsonApiClient(TestsConfig.ListeningOn) {
            BearerToken = AdminKey
        };
        var apiAdmin = await client.ApiAsync(new AdminApiKey { Name = "Admin" });
        apiAdmin.ThrowIfError();
        Assert.That(apiAdmin.Response.Result, Is.EqualTo("Hello, Admin!"));
        
        client = new JsonApiClient(TestsConfig.ListeningOn) {
            BearerToken = AuthSecret
        };
        var apiAuthSecret = await client.ApiAsync(new AdminApiKey { Name = "Admin" });
        apiAuthSecret.ThrowIfError();
        Assert.That(apiAuthSecret.Response.Result, Is.EqualTo("Hello, Admin!"));
    }
}
