#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace ServiceStack;

public class AdminDatabaseFeature : IPlugin, Model.IHasStringId, IPreInitPlugin
{
    public string Id { get; set; } = Plugins.AdminDatabase;
    public string AdminRole { get; set; } = RoleNames.Admin;

    public Action<List<SchemaInfo>>? SchemasFilter { get; set; }

    public int QueryLimit { get; set; } = 100;

    public void Register(IAppHost appHost)
    {
        appHost.RegisterService(typeof(AdminDatabaseService));

        var dbFactory = appHost.Resolve<IDbConnectionFactory>();
        using var db = dbFactory.Open();

        var schemasMap = db.GetSchemaTables();
        var schemas = new List<SchemaInfo>();
        schemasMap.Keys.OrderBy(x => x).Each(schema => {
            schemas.Add(new SchemaInfo {
                Name = schema,
                Tables = schemasMap[schema],
            });
        });
        SchemasFilter?.Invoke(schemas);

        appHost.AddToAppMetadata(meta => {
            meta.Plugins.AdminDatabase = new AdminDatabaseInfo {
                QueryLimit = QueryLimit,
                Schemas = schemas,
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