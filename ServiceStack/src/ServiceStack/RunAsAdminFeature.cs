using ServiceStack.Auth;
using ServiceStack.Host.Handlers;

namespace ServiceStack;

/// <summary>
/// Allow every request to run as Admin User
/// </summary>
public class RunAsAdminFeature : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.RunAsAdmin;

    public string RedirectTo { get; set; } = "/admin-ui";
    
    public void Register(IAppHost appHost)
    {
        appHost.LoadPlugin(new AuthFeature(() => new AuthUserSession(), [
            new CredentialsAuthProvider(appHost.AppSettings)
        ]));
        
        appHost.PreRequestFilters.Add((req, res) => {
            req.Items[Keywords.Session] = appHost.GetPlugin<AuthFeature>().AuthSecretSession;
        });

        if (RedirectTo != null)
        {
            appHost.CatchAllHandlers.Add(httpReq =>
            {
                if (httpReq.Verb == HttpMethods.Get && string.IsNullOrEmpty(httpReq.PathInfo.TrimStart('/')))
                {
                    return new RedirectHttpHandler {
                        RelativeUrl = RedirectTo
                    };
                }
                return null;
            });
        }
    }
}