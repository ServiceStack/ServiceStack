using System;
using System.Collections.Generic;
using ServiceStack.HtmlModules;
using ServiceStack.Model;

namespace ServiceStack;

public class UiFeature : IPlugin, IPostInitPlugin, IHasStringId
{
    public string Id => Plugins.Ui;

    public UiInfo Info { get; set; }

    public List<HtmlModule> HtmlModules { get; } = new();

    public List<IHtmlModulesHandler> Handlers { get; set; } = new()
    {
        new SharedFolder("shared", "/modules/shared", ".html"),
        new SharedFolder("shared/js", "/modules/shared/js", ".js"),
        new SharedFolder("plugins", "/modules/shared/plugins", ".js"),
    };

    public HtmlModulesFeature Module { get; } = new()
    {
        IgnoreIfError = true,
        Configure = (appHost,module) => module.VirtualFiles = appHost.VirtualFileSources,
    };
    
    public Action<UiFeature> Configure { get; set; }

    public UiFeature()
    {
        Info = new UiInfo
        {
            BrandIconUri = Svg.GetDataUri(Svg.Logos.ServiceStack, "#000000"),
            HideTags = new List<string> { TagNames.Auth },
            AdminLinks = new()
            {
                new LinkInfo
                {
                    Id = "",
                    Label = "Dashboard",
                    IconSvg = Svg.Create(Svg.Body.Home),
                },
            },
        };
    }

    public void Register(IAppHost appHost) {}

    public void AfterPluginsLoaded(IAppHost appHost)
    {
        if (HtmlModules.Count > 0)
        {
            Configure?.Invoke(this);
            Module.Modules.AddRange(HtmlModules);
            Module.Handlers.AddRange(Handlers);
            Module.Register(appHost);
        }
    }
}