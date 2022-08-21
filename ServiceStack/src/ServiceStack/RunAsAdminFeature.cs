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
        appHost.LoadPlugin(new AuthFeature(() => new AuthUserSession(), new [] {
            new CredentialsAuthProvider(appHost.AppSettings),
        }));
        
        appHost.PreRequestFilters.Add((req, res) => {
            req.Items[Keywords.Session] = appHost.GetPlugin<AuthFeature>().AuthSecretSession;
        });

        if (RedirectTo != null)
        {
            appHost.CatchAllHandlers.Add((string httpMethod, string pathInfo, string filePath) =>
            {
                if (httpMethod == HttpMethods.Get && string.IsNullOrEmpty(pathInfo.TrimStart('/')))
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