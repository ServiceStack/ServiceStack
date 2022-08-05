#nullable enable

using System;
using ServiceStack.Admin;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;

namespace ServiceStack;

public class AdminDatabaseFeature : IPlugin, Model.IHasStringId, IPreInitPlugin
{
    public string Id { get; set; } = Plugins.AdminDatabase;
    public string AdminRole { get; set; } = RoleNames.Admin;

    public int QueryLimit { get; set; } = 100;

    public void Register(IAppHost appHost)
    {
        appHost.RegisterService(typeof(AdminDatabaseService));

        var dbFactory = appHost.Resolve<IDbConnectionFactory>();

        appHost.AddToAppMetadata(meta => {
            meta.Plugins.AdminDatabase = new AdminDatabaseInfo {
                QueryLimit = QueryLimit,
            };
        });
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<UiFeature>(feature => {
            feature.AddAdminLink(AdminUiFeature.Database, new LinkInfo {
                Id = "database",
                Label = "Database",
                Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Database)),
                Show = $"role:{AdminRole}",
            });
        });
    }
}


[ExcludeMetadata, Tag("admin")]
public class AdminDatabase : IPost, IReturn<AdminDatabaseResponse>
{
    public string? NamedConnection { get; set; }
}

public class AdminDatabaseResponse : IHasResponseStatus
{
    public ResponseStatus? ResponseStatus { get; set; }
}

public class AdminDatabaseService : Service
{
    public object Post(AdminDatabase request)
    {
        return new AdminDatabaseResponse();
    }
}