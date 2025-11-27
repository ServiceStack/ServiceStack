using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public class AuthSecretAuthProvider(string? authSecret=null)
    : AuthProvider(null, "/auth/" + Keywords.AuthSecret, Keywords.AuthSecret), IAuthInit, IAuthWithRequest
{
    public override string Type => Keywords.AuthSecret;

    public string? AuthSecret { get; set; } = authSecret;

    public void Init(AuthFeature feature)
    {
        feature.RegisterPlugins.RemoveAll(x => x is SessionFeature);
        feature.IncludeAssignRoleServices = false;
    }

    public override void Register(IAppHost appHost, AuthFeature feature)
    {
        Label = feature.AdminAuthSecretInfo.Label;
        FormLayout = feature.AdminAuthSecretInfo.FormLayout;
        feature.AdminAuthSecretInfo.FormLayout = null;
        appHost.Config.AdminAuthSecret ??= AuthSecret;
    }

    public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate? request = null)
    {
        return session == HostContext.AssertPlugin<AuthFeature>().AuthSecretSession;
    }

    public override Task<object?> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request,
        CancellationToken token = new())
    {
        var req = authService.Request;
        var authSecret = req.GetAuthSecret() ?? req.GetBearerToken();
        if (HostContext.Config.AdminAuthSecret != null && HostContext.Config.AdminAuthSecret == authSecret)
        {
            session = HostContext.AssertPlugin<AuthFeature>().AuthSecretSession;
            req.SetItem(Keywords.Session, session);

            return Task.FromResult((object?)new AuthenticateResponse
            {
                UserId = session.UserAuthId,
                UserName = session.UserName,
                SessionId = session.Id,
                DisplayName = session.DisplayName ?? session.UserName,
                ReferrerUrl = authService.Request.GetReturnUrl(),
            });
        }
        return Task.FromResult<object?>(null);
    }

    public Task PreAuthenticateAsync(IRequest req, IResponse res)
    {
        var authSecret = req.GetAuthSecret() ?? req.GetBearerToken();
        if (HostContext.Config.AdminAuthSecret != null && HostContext.Config.AdminAuthSecret == authSecret)
        {
            req.SetItem(Keywords.Session, HostContext.AssertPlugin<AuthFeature>().AuthSecretSession);
        }
        return Task.CompletedTask;
    }
}
