using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class InProcessServiceGatewayAuthAttributesTests
{
    [Authenticate]
    public class GatewayAuthOnly : IReturn<GatewayAuthResponse> { }

    [RequiredRole("Admin")]
    public class GatewayAdminOnly : IReturn<GatewayAuthResponse> { }

    [RequiredPermission("ManageUsers")]
    public class GatewayPermissionOnly : IReturn<GatewayAuthResponse> { }

    public class GatewayServiceClassAuth : IReturn<GatewayAuthResponse> { }

    public class GatewayAnonymous : IReturn<GatewayAuthResponse> { }

    [Authenticate]
    public class GatewayAuthOneWay : IReturnVoid { }

    public class GatewayAuthResponse
    {
        public string UserName { get; set; }
    }

    public class GatewayAuthService : Service
    {
        public List<string> Executed { get; set; }

        GatewayAuthResponse Run(object request)
        {
            Executed.Add(request.GetType().Name);
            return new GatewayAuthResponse { UserName = GetSession().UserAuthName };
        }

        public object Any(GatewayAuthOnly request) => Run(request);
        public object Any(GatewayAdminOnly request) => Run(request);
        public object Any(GatewayPermissionOnly request) => Run(request);
        public object Any(GatewayAnonymous request) => Run(request);
        public void Any(GatewayAuthOneWay request) => Run(request);
    }

    [Authenticate]
    public class GatewayServiceClassAuthService : Service
    {
        public List<string> Executed { get; set; }

        public object Any(GatewayServiceClassAuth request)
        {
            Executed.Add(request.GetType().Name);
            return new GatewayAuthResponse { UserName = GetSession().UserAuthName };
        }
    }

    /// <summary>
    /// Simulates an IAuthWithRequest provider that would replace the session from copied cookies or headers
    /// </summary>
    public class ReplacingAuthProvider : CredentialsAuthProvider, IAuthWithRequest
    {
        public Task PreAuthenticateAsync(IRequest req, IResponse res)
        {
            req.SetItem(Keywords.Session, CreateSession("intruder"));
            return Task.CompletedTask;
        }
    }

    static AuthUserSession CreateSession(string userName, string[] roles = null, string[] permissions = null) => new()
    {
        Id = Guid.NewGuid().ToString("n"),
        IsAuthenticated = true,
        AuthProvider = CredentialsAuthProvider.Name,
        UserAuthId = "1",
        UserAuthName = userName,
        Roles = roles != null ? [..roles] : [],
        Permissions = permissions != null ? [..permissions] : [],
    };

    List<string> executed;

    ServiceStackHost CreateAppHost(bool applyGatewayAuthAttributes, bool replacingProvider = false)
    {
        executed = [];
        var appHost = new BasicAppHost
        {
            ConfigureContainer = container => container.Register(executed),
            ConfigureAppHost = host =>
            {
                host.Config.ApplyGatewayAuthAttributes = applyGatewayAuthAttributes;
                host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [
                    replacingProvider ? new ReplacingAuthProvider() : new CredentialsAuthProvider(),
                ]));
                host.RegisterService<GatewayAuthService>();
                host.RegisterService<GatewayServiceClassAuthService>();
            },
        };
        appHost.Init();
        return appHost;
    }

    static InProcessServiceGateway CreateGateway(IAuthSession session = null)
    {
        var req = new BasicRequest();
        if (session != null)
            req.SetItem(Keywords.Session, session);
        return new InProcessServiceGateway(req);
    }

    [Test]
    public async Task Does_not_apply_auth_attributes_by_default()
    {
        using var appHost = CreateAppHost(applyGatewayAuthAttributes: false);
        var gateway = CreateGateway();

        await gateway.SendAsync(new GatewayAuthOnly());
        await gateway.SendAsync(new GatewayAdminOnly());
        await gateway.SendAsync(new GatewayServiceClassAuth());

        Assert.That(executed, Is.EqualTo(new[] { nameof(GatewayAuthOnly), nameof(GatewayAdminOnly), nameof(GatewayServiceClassAuth) }));
    }

    [Test]
    public void Throws_401_for_unauthenticated_requests([Values] bool useAsync)
    {
        using var appHost = CreateAppHost(applyGatewayAuthAttributes: true);
        var gateway = CreateGateway();

        foreach (var request in new IReturn<GatewayAuthResponse>[] { new GatewayAuthOnly(), new GatewayAdminOnly(), new GatewayServiceClassAuth() })
        {
            var error = useAsync
                ? Assert.ThrowsAsync<HttpError>(async () => await gateway.SendAsync(request))
                : Assert.Throws<HttpError>(() => gateway.Send(request));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), request.GetType().Name);
            Assert.That(error.Message, Is.EqualTo(ErrorMessages.NotAuthenticated));
            Assert.That(gateway.Request.Response.IsClosed, Is.False);
        }
        Assert.That(executed, Is.Empty);
    }

    [Test]
    public void Throws_403_for_missing_roles_and_permissions([Values] bool useAsync)
    {
        using var appHost = CreateAppHost(applyGatewayAuthAttributes: true);
        var gateway = CreateGateway(CreateSession("user"));

        var roleError = useAsync
            ? Assert.ThrowsAsync<HttpError>(async () => await gateway.SendAsync(new GatewayAdminOnly()))
            : Assert.Throws<HttpError>(() => gateway.Send(new GatewayAdminOnly()));
        Assert.That(roleError.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(roleError.Message, Is.EqualTo(ErrorMessages.InvalidRole));

        var permissionError = useAsync
            ? Assert.ThrowsAsync<HttpError>(async () => await gateway.SendAsync(new GatewayPermissionOnly()))
            : Assert.Throws<HttpError>(() => gateway.Send(new GatewayPermissionOnly()));
        Assert.That(permissionError.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(permissionError.Message, Is.EqualTo(ErrorMessages.InvalidPermission));

        Assert.That(executed, Is.Empty);
    }

    [Test]
    public async Task Executes_authorized_requests([Values] bool useAsync)
    {
        using var appHost = CreateAppHost(applyGatewayAuthAttributes: true);
        var gateway = CreateGateway(CreateSession("admin", roles: ["Admin"], permissions: ["ManageUsers"]));

        foreach (var request in new IReturn<GatewayAuthResponse>[] {
            new GatewayAuthOnly(), new GatewayAdminOnly(), new GatewayPermissionOnly(), new GatewayServiceClassAuth(), new GatewayAnonymous() })
        {
            var response = useAsync
                ? await gateway.SendAsync(request)
                : gateway.Send(request);
            Assert.That(response.UserName, Is.EqualTo("admin"));
        }
        Assert.That(executed, Has.Count.EqualTo(5));
    }

    [Test]
    public void Anonymous_requests_are_unaffected()
    {
        using var appHost = CreateAppHost(applyGatewayAuthAttributes: true);
        var gateway = CreateGateway();

        Assert.That(gateway.Send(new GatewayAnonymous()), Is.Not.Null);
        Assert.That(executed, Is.EqualTo(new[] { nameof(GatewayAnonymous) }));
    }

    [Test]
    public void Applies_to_batched_and_published_requests([Values] bool useAsync)
    {
        using var appHost = CreateAppHost(applyGatewayAuthAttributes: true);
        var gateway = CreateGateway();

        async Task SendAll()
        {
            if (useAsync) await gateway.SendAllAsync(new[] { new GatewayAuthOnly(), new GatewayAuthOnly() });
            else gateway.SendAll(new[] { new GatewayAuthOnly(), new GatewayAuthOnly() });
        }
        async Task Publish()
        {
            if (useAsync) await gateway.PublishAsync(new GatewayAuthOneWay());
            else gateway.Publish(new GatewayAuthOneWay());
        }
        async Task PublishAll()
        {
            if (useAsync) await gateway.PublishAllAsync(new[] { new GatewayAuthOneWay(), new GatewayAuthOneWay() });
            else gateway.PublishAll(new[] { new GatewayAuthOneWay(), new GatewayAuthOneWay() });
        }

        foreach (var fn in new Func<Task>[] { SendAll, Publish, PublishAll })
        {
            var error = Assert.ThrowsAsync<HttpError>(() => fn());
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
        Assert.That(executed, Is.Empty);
    }

    [Test]
    public async Task Attached_authenticated_session_is_not_replaced_by_auth_providers()
    {
        using var appHost = CreateAppHost(applyGatewayAuthAttributes: true, replacingProvider: true);
        var gateway = CreateGateway(CreateSession("circuit-user"));

        var response = await gateway.SendAsync(new GatewayAuthOnly());

        Assert.That(response.UserName, Is.EqualTo("circuit-user"));
    }
}
