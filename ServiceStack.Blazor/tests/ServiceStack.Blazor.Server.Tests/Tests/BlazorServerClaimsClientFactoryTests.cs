using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Blazor;
using ServiceStack.Testing;

namespace MyApp.Tests;

public class BlazorServerClaimsClientFactoryTests
{
    public class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        private AuthenticationState state = new(new ClaimsPrincipal(new ClaimsIdentity()));
        public int Calls { get; private set; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            Calls++;
            return Task.FromResult(state);
        }

        public void SetUser(ClaimsPrincipal user)
        {
            state = new AuthenticationState(user);
            NotifyAuthenticationStateChanged(Task.FromResult(state));
        }
    }

    public class ReadClaimsSession : IReturn<ClaimsSessionResponse>;

    [Authenticate]
    public class ReadProtectedClaimsSession : IReturn<ClaimsSessionResponse>;

    public class ClaimsSessionResponse
    {
        public string? SessionId { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? UserAuthName { get; set; }
        public string? Email { get; set; }
        public List<string>? Roles { get; set; }
        public string? Filtered { get; set; }
        public string? ScopedValue { get; set; }
        public string? ClaimsPrincipalName { get; set; }
    }

    public class CircuitContext
    {
        public string? Value { get; set; }
    }

    public class ClaimsSessionService : Service
    {
        public object Any(ReadClaimsSession request) => CreateResponse();
        public object Any(ReadProtectedClaimsSession request) => CreateResponse();

        ClaimsSessionResponse CreateResponse()
        {
            var session = GetSession();
            return new ClaimsSessionResponse
            {
                SessionId = session.Id,
                IsAuthenticated = session.IsAuthenticated,
                UserAuthName = session.UserAuthName,
                Email = session.Email,
                Roles = session.Roles,
                Filtered = (session as IMeta)?.Meta?.GetValueOrDefault("filtered"),
                ScopedValue = Request!.TryResolve<CircuitContext>()?.Value,
                ClaimsPrincipalName = Request.GetClaimsPrincipal()?.Identity?.Name,
            };
        }
    }

    static ClaimsPrincipal CreateUser(string id, string name, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, $"{name}@example.org"),
        };
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "OpenIdConnect"));
    }

    static ServiceStackHost CreateAppHost(bool applyGatewayAuthAttributes = false) => new BasicAppHost
    {
        ConfigureAppHost = host =>
        {
            host.Config.ApplyGatewayAuthAttributes = applyGatewayAuthAttributes;
            host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [
                new NetCoreIdentityAuthProvider(host.AppSettings)
                {
                    AutoSignInSessions = false, // requires ASP.NET Core middleware
                    PopulateSessionFilter = (session, principal, req) =>
                        ((IMeta)session).Meta!["filtered"] = principal.Identity!.Name!,
                },
            ]));
            host.RegisterService<ClaimsSessionService>();
        },
    }.Init();

    static BlazorServerClaimsClientFactory CreateFactory(IServiceProvider services,
        AuthenticationStateProvider? authStateProvider, BlazorServerApiClientConfig? config = null) =>
        new(new JsonApiClient(new HttpClient()), "https://localhost:5001", services, config)
        {
            AuthenticationStateProvider = authStateProvider,
        };

    [Test]
    public async Task Converts_circuit_user_with_registered_auth_provider()
    {
        using var appHost = CreateAppHost();
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var authState = new TestAuthenticationStateProvider();
        authState.SetUser(CreateUser("1", "alice", "Admin", "Employee"));
        var factory = CreateFactory(provider, authState);

        // ManagedApiAsync clones the gateway for each call
        var gateway = factory.GetGateway();
        for (var i = 0; i < 2; i++)
        {
            var api = await gateway.ManagedApiAsync(new ReadClaimsSession());
            Assert.That(api.Succeeded, Is.True, api.Error?.Message);
            var response = api.Response!;
            Assert.Multiple(() =>
            {
                Assert.That(response.IsAuthenticated, Is.True);
                Assert.That(response.SessionId, Is.EqualTo("1"));
                Assert.That(response.UserAuthName, Is.EqualTo("alice"));
                Assert.That(response.Email, Is.EqualTo("alice@example.org"));
                Assert.That(response.Roles, Is.EquivalentTo(new[] { "Admin", "Employee" }));
                Assert.That(response.Filtered, Is.EqualTo("alice"));
                Assert.That(response.ClaimsPrincipalName, Is.EqualTo("alice"));
            });
        }
    }

    [Test]
    public async Task Uses_the_current_user_after_authentication_state_changes()
    {
        using var appHost = CreateAppHost();
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var authState = new TestAuthenticationStateProvider();
        var factory = CreateFactory(provider, authState);

        var anon = await factory.GetGateway().SendAsync(new ReadClaimsSession());
        Assert.That(anon.IsAuthenticated, Is.False);

        authState.SetUser(CreateUser("1", "alice"));
        var alice = await factory.GetGateway().SendAsync(new ReadClaimsSession());
        var aliceAgain = await factory.GetGateway().SendAsync(new ReadClaimsSession());
        Assert.That(alice.UserAuthName, Is.EqualTo("alice"));
        Assert.That(aliceAgain.UserAuthName, Is.EqualTo("alice"));

        authState.SetUser(CreateUser("2", "bob"));
        var bob = await factory.GetGateway().SendAsync(new ReadClaimsSession());
        Assert.That(bob.UserAuthName, Is.EqualTo("bob"));
        Assert.That(bob.SessionId, Is.EqualTo("2"));
    }

    [Test]
    public async Task Is_unauthenticated_without_an_AuthenticationStateProvider()
    {
        using var appHost = CreateAppHost();
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var factory = CreateFactory(provider, authStateProvider: null);

        var response = await factory.GetGateway().SendAsync(new ReadClaimsSession());
        Assert.That(response.IsAuthenticated, Is.False);
    }

    [Test]
    public async Task Uses_custom_session_conversion()
    {
        using var appHost = CreateAppHost();
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var authState = new TestAuthenticationStateProvider();
        authState.SetUser(CreateUser("1", "alice"));
        var factory = CreateFactory(provider, authState, new BlazorServerApiClientConfig
        {
            ConvertPrincipalToSessionAsync = async (req, user) =>
            {
                await Task.Yield();
                return new AuthUserSession { Id = "custom", IsAuthenticated = true, UserAuthName = "custom:" + user.Identity!.Name };
            },
        });

        var response = await factory.GetGateway().SendAsync(new ReadClaimsSession());
        Assert.That(response.UserAuthName, Is.EqualTo("custom:alice"));
    }

    /// <summary>
    /// Simulates the Blazor renderer's SynchronizationContext while it's blocked running the current work item
    /// </summary>
    class BlockedSynchronizationContext : System.Threading.SynchronizationContext
    {
        public override void Post(System.Threading.SendOrPostCallback d, object? state) {}
    }

    [Test]
    public async Task GetGateway_does_not_deadlock_on_the_renderers_SynchronizationContext()
    {
        using var appHost = CreateAppHost();
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var authState = new TestAuthenticationStateProvider();
        authState.SetUser(CreateUser("1", "alice"));
        var factory = CreateFactory(provider, authState, new BlazorServerApiClientConfig
        {
            ConvertPrincipalToSessionAsync = async (req, user) =>
            {
                await Task.Delay(10); // continuation would be posted to the captured SynchronizationContext
                return new AuthUserSession { Id = "1", IsAuthenticated = true, UserAuthName = user.Identity!.Name };
            },
        });

        var getGateway = Task.Factory.StartNew(() =>
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(new BlockedSynchronizationContext());
            return factory.GetGateway();
        }, TaskCreationOptions.LongRunning);

        Assert.That(await Task.WhenAny(getGateway, Task.Delay(5_000)), Is.SameAs(getGateway), "GetGateway() deadlocked");
        var gateway = (InProcessServiceGateway)await getGateway;
        Assert.That(((IAuthSession)gateway.Request.GetItem(Keywords.Session)).UserAuthName, Is.EqualTo("alice"));
    }

    [Test]
    public async Task Resolves_dependencies_from_circuit_scope_when_enabled([Values] bool useCircuitServiceScope)
    {
        using var appHost = CreateAppHost();
        await using var provider = new ServiceCollection().AddScoped<CircuitContext>().BuildServiceProvider();
        await using var circuitScope = provider.CreateAsyncScope();
        circuitScope.ServiceProvider.GetRequiredService<CircuitContext>().Value = "circuit";
        var factory = CreateFactory(circuitScope.ServiceProvider, new TestAuthenticationStateProvider(),
            new BlazorServerApiClientConfig { UseCircuitServiceScope = useCircuitServiceScope });

        var api = await factory.GetGateway().ManagedApiAsync(new ReadClaimsSession());
        Assert.That(api.Succeeded, Is.True, api.Error?.Message);
        Assert.That(api.Response!.ScopedValue, Is.EqualTo(useCircuitServiceScope ? "circuit" : null));

        // The circuit's scope is owned by Blazor and stays usable after gateway calls
        Assert.That(circuitScope.ServiceProvider.GetRequiredService<CircuitContext>().Value, Is.EqualTo("circuit"));
    }

    [Test]
    public async Task ManagedApiAsync_returns_auth_errors_when_gateway_auth_attributes_are_applied()
    {
        using var appHost = CreateAppHost(applyGatewayAuthAttributes: true);
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var authState = new TestAuthenticationStateProvider();
        var factory = CreateFactory(provider, authState);

        var anon = await factory.GetGateway().ManagedApiAsync(new ReadProtectedClaimsSession());
        Assert.That(anon.Succeeded, Is.False);
        Assert.That(anon.Error?.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));

        authState.SetUser(CreateUser("1", "alice"));
        var alice = await factory.GetGateway().ManagedApiAsync(new ReadProtectedClaimsSession());
        Assert.That(alice.Succeeded, Is.True, alice.Error?.Message);
        Assert.That(alice.Response!.UserAuthName, Is.EqualTo("alice"));
    }
}
