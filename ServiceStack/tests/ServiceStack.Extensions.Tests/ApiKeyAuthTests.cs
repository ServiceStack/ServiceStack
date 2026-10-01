#nullable enable
#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

[ValidateIsAuthenticated]
public class WhoAmI : IGet, IReturn<WhoAmIResponse> {}

[ValidateIsAuthenticated]
public class WhoAmIRestricted : IGet, IReturn<WhoAmIResponse> {}

[ValidateHasRole(Roles.Manager)]
public class WhoAmIManager : IGet, IReturn<WhoAmIResponse> {}

[ValidateIsAdmin]
public class WhoAmIAdmin : IGet, IReturn<WhoAmIResponse> {}

[ValidateApiKey]
public class WhoAmIApiKey : IGet, IReturn<WhoAmIResponse> {}

public class WhoAmIResponse
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? AuthProvider { get; set; }
    public bool IsApiKeyUser { get; set; }
    public List<string> Scopes { get; set; } = [];
    public List<string> Roles { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

public class WhoAmIServices : Service
{
    public async Task<object> Any(WhoAmI request) => await WhoAsync();
    public async Task<object> Any(WhoAmIRestricted request) => await WhoAsync();
    public async Task<object> Any(WhoAmIManager request) => await WhoAsync();
    public async Task<object> Any(WhoAmIAdmin request) => await WhoAsync();
    public object Any(WhoAmIApiKey request) => new WhoAmIResponse { UserId = Request.GetApiKey()?.UserAuthId };

    private async Task<WhoAmIResponse> WhoAsync()
    {
        var session = await GetSessionAsync();
        return new() {
            UserId = session.UserAuthId,
            UserName = session.UserAuthName,
            AuthProvider = session.AuthProvider,
            IsApiKeyUser = Request.GetClaimsPrincipal().IsApiKeyUser(),
            Scopes = (session as IAuthSessionExtended)?.Scopes ?? [],
            Roles = session.Roles ?? [],
        };
    }
}

/// <summary>
/// With AddApiKeyAuth() a User API Key authenticates as its user, so [ValidateIsAuthenticated] APIs can be called
/// with either the user's session or one of their API Keys.
/// </summary>
public class ApiKeyAuthTests
{
    private const string ListeningOn = "http://localhost:20049/";
    private const string TenantHeader = "X-Test-Tenant";

    private const string UserKey = "ak-A1C9065463A4AEEAD4D33C0DCB1FCD01";
    private const string ScopedKey = "ak-A1C9065463A4AEEAD4D33C0DCB1FCD02";
    private const string AdminKey = "ak-A1C9065463A4AEEAD4D33C0DCB1FCD03";
    private const string RestrictedKey = "ak-A1C9065463A4AEEAD4D33C0DCB1FCD04";
    private const string CancelledKey = "ak-A1C9065463A4AEEAD4D33C0DCB1FCD05";
    private const string AnonKey = "ak-A1C9065463A4AEEAD4D33C0DCB1FCD06";
    private const string OtherTenantKey = "ak-A1C9065463A4AEEAD4D33C0DCB1FCD07";
    private const string UnknownKey = "ak-FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";

    private static string managerId = "";

    class AppHost() : AppHostBase(nameof(ApiKeyAuthTests), typeof(WhoAmIServices).Assembly)
    {
        public override void Configure()
        {
            IdentityJwtAuthProviderTests.CreateIdentityUsers(ApplicationServices);
            using (var scope = ApplicationServices.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                managerId = userManager.FindByEmailAsync("manager@email.com").Result!.Id;
            }

            var feature = GetPlugin<ApiKeysFeature>();
            using var db = feature.OpenDb();
            feature.InitSchema(db);
            feature.InsertAll(db, [
                new() { Key = UserKey, UserId = managerId, UserName = "manager@email.com", Name = "User", RefIdStr = "tenant-a" },
                new() { Key = ScopedKey, UserId = managerId, UserName = "manager@email.com", Name = "Scoped", RefIdStr = "tenant-a", Scopes = ["usage:write"] },
                new() { Key = AdminKey, UserId = managerId, UserName = "manager@email.com", Name = "Admin", RefIdStr = "tenant-a", Scopes = [Roles.Admin] },
                new() { Key = RestrictedKey, UserId = managerId, UserName = "manager@email.com", Name = "Restricted", RefIdStr = "tenant-a", RestrictTo = [nameof(WhoAmIRestricted)] },
                new() { Key = CancelledKey, UserId = managerId, UserName = "manager@email.com", Name = "Cancelled", RefIdStr = "tenant-a", CancelledDate = DateTime.UtcNow },
                new() { Key = OtherTenantKey, UserId = managerId, UserName = "manager@email.com", Name = "Other Tenant", RefIdStr = "tenant-b" },
                new() { Key = AnonKey, Name = "No User" },
            ]);
        }

        // An App confining the connections it opens for a request to the request's tenant
        public override IDbConnection GetDbConnection(IRequest? req, Action<IDbConnection> configure)
        {
            var db = base.GetDbConnection(req, configure);
            var tenant = req?.GetHeader(TenantHeader);
            if (!string.IsNullOrEmpty(tenant))
                db.EnsureFilter<ApiKeysFeature.ApiKey>(x => x.RefIdStr == tenant);
            return db;
        }
    }

    public ApiKeyAuthTests()
    {
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        var services = builder.Services;

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            // Not using DisableRedirectsForApis(), so unauthenticated requests are redirected to Sign In
            .AddIdentityCookies();
        services.AddAuthentication().AddApiKeyAuth();
        services.AddAuthorization();

        var dbPath = Path.Combine(Path.GetTempPath(), $"apikeyauth-{Guid.NewGuid():N}.sqlite");
        var dbFactory = new OrmLiteConnectionFactory($"DataSource={dbPath};Cache=Shared", SqliteDialect.Provider);
        services.AddSingleton<IDbConnectionFactory>(dbFactory);
        services.AddDbContext<ApplicationDbContext>();

        services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        ServiceStackHost.InitOptions.ScriptContext.ScriptMethods.AddRange([
            new DbScriptsAsync(),
            new MyValidators(),
        ]);

        services.AddPlugin(new AuthFeature(IdentityAuth.For<ApplicationUser>(options =>
        {
            options.SessionFactory = () => new CustomUserSession();
            options.CredentialsAuth();
        })));
        services.AddPlugin(new ApiKeysFeature());
        services.AddServiceStack(typeof(WhoAmIServices).Assembly);

        var app = builder.Build();
        app.UseAuthorization();
        app.UseServiceStack(new AppHost(), options => options.MapEndpoints());
        app.StartAsync(ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    private static JsonApiClient CreateClient(string? apiKey = null) => new(ListeningOn) { BearerToken = apiKey };

    private static async Task<HttpResponseMessage> GetAsync(string path, Action<HttpRequestMessage>? configure = null)
    {
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, ListeningOn.CombineWith(path));
        request.Headers.Accept.ParseAdd(MimeTypes.Json);
        configure?.Invoke(request);
        return await client.SendAsync(request);
    }

    [Test]
    public async Task User_ApiKey_authenticates_as_its_user()
    {
        var api = await CreateClient(ScopedKey).ApiAsync(new WhoAmI());
        api.ThrowIfError();

        Assert.That(api.Response!.UserId, Is.EqualTo(managerId));
        Assert.That(api.Response.UserName, Is.EqualTo("manager@email.com"));
        Assert.That(api.Response.AuthProvider, Is.EqualTo(Keywords.ApiKeyParam));
        Assert.That(api.Response.IsApiKeyUser, Is.True);
        Assert.That(api.Response.Scopes, Is.EqualTo(new[] { "usage:write" }));
    }

    [Test]
    public async Task User_ApiKey_can_be_sent_in_the_ApiKey_header()
    {
        var response = await GetAsync("/api/WhoAmI", x => x.Headers.Add(HttpHeaders.XApiKey, UserKey));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await response.Content.ReadAsStringAsync()).FromJson<WhoAmIResponse>().UserId, Is.EqualTo(managerId));
    }

    [Test]
    public async Task User_ApiKey_does_not_have_the_roles_of_its_user()
    {
        var api = await CreateClient(UserKey).ApiAsync(new WhoAmI());
        Assert.That(api.Response!.Roles, Is.Empty);

        var apiManager = await CreateClient(UserKey).ApiAsync(new WhoAmIManager());
        Assert.That(apiManager.Error?.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Forbidden)));

        var apiAdmin = await CreateClient(UserKey).ApiAsync(new WhoAmIAdmin());
        Assert.That(apiAdmin.Failed);
    }

    [Test]
    public async Task Admin_scope_has_the_Admin_role()
    {
        var api = await CreateClient(AdminKey).ApiAsync(new WhoAmIAdmin());
        api.ThrowIfError();
    }

    [Test]
    public async Task ApiKey_restricted_to_an_API_cannot_call_other_APIs()
    {
        var api = await CreateClient(RestrictedKey).ApiAsync(new WhoAmIRestricted());
        api.ThrowIfError();
        Assert.That(api.Response!.UserId, Is.EqualTo(managerId));

        var apiOther = await CreateClient(RestrictedKey).ApiAsync(new WhoAmI());
        Assert.That(apiOther.Error?.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Forbidden)));
    }

    [TestCase(UnknownKey)]
    [TestCase(CancelledKey)]
    [TestCase(AnonKey)]
    public async Task ApiKeys_that_cannot_authenticate_a_user_are_unauthorized_instead_of_redirected(string apiKey)
    {
        var response = await GetAsync("/api/WhoAmI", x => x.Headers.Add(HttpHeaders.XApiKey, apiKey));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(response.Headers.Location, Is.Null);
    }

    [Test]
    public async Task Requests_without_an_ApiKey_are_still_handled_by_the_other_schemes()
    {
        var response = await GetAsync("/api/WhoAmI");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
    }

    [Test]
    public async Task ApiKey_without_a_user_can_still_call_ApiKey_APIs()
    {
        var api = await CreateClient(AnonKey).ApiAsync(new WhoAmIApiKey());
        api.ThrowIfError();
        Assert.That(api.Response!.UserId, Is.Null);
    }

    [Test]
    public async Task Users_can_still_authenticate_with_their_session()
    {
        var client = CreateClient();
        var auth = await client.ApiAsync(new Authenticate {
            provider = "credentials", UserName = "manager@email.com", Password = "p@55wOrd",
        });
        auth.ThrowIfError();

        var api = await client.ApiAsync(new WhoAmI());
        api.ThrowIfError();
        Assert.That(api.Response!.UserId, Is.EqualTo(managerId));
        Assert.That(api.Response.IsApiKeyUser, Is.False);
        Assert.That(api.Response.Roles, Does.Contain(Roles.Manager));

        var apiManager = await client.ApiAsync(new WhoAmIManager());
        apiManager.ThrowIfError();
    }

    [Test]
    public async Task ApiKey_services_use_the_connection_opened_for_the_request()
    {
        var client = CreateClient(UserKey);
        var all = await client.ApiAsync(new QueryUserApiKeys());
        all.ThrowIfError();
        Assert.That(all.Response!.Results.Select(x => x.Name), Does.Contain("Other Tenant"));

        // The App's GetDbConnection(IRequest) confines the request's connections to its tenant
        client.Headers[TenantHeader] = "tenant-a";
        var api = await client.ApiAsync(new QueryUserApiKeys());
        api.ThrowIfError();
        Assert.That(api.Response!.Results.Select(x => x.Name), Is.EquivalentTo(new[] { "User", "Scoped", "Admin", "Restricted", "Cancelled" }));
    }
}
#endif
