#nullable enable

using System;
using System.Collections.Generic;
using ServiceStack.Admin;
using ServiceStack.Configuration;

namespace ServiceStack;

public class AdminRedisFeature : IPlugin, Model.IHasStringId, IPreInitPlugin
{
    public string Id { get; set; } = Plugins.AdminRedis;
    public string AdminRole { get; set; } = RoleNames.Admin;

    public int QueryLimit { get; set; } = 100;
    public List<int> Databases { get; set; } = new() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

    public HashSet<string> IllegalCommands { get; set; } = new(StringComparer.OrdinalIgnoreCase) {
        "BLMOVE",
        "BLMPOP",
        "BLPOP",
        "BRPOP",
        "BRPOPLPUSH",
        "FLUSHDB",
        "FLUSHALL",
        "MONITOR",
    };

    public void Register(IAppHost appHost)
    {
        appHost.RegisterService(typeof(AdminRedisService));

        appHost.AddToAppMetadata(meta => {
            meta.Plugins.AdminRedis = new AdminRedisInfo {
                QueryLimit = QueryLimit,
                Databases = Databases,
            };
        });
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<UiFeature>(feature => {
            feature.AddAdminLink(AdminUiFeature.Redis, new LinkInfo {
                Id = "redis",
                Label = "Redis",
                Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Redis, fill:"currentColor", stroke:"none")),
                Show = $"role:{AdminRole}",
            });
        });
    }
}