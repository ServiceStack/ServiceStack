using System;
using System.Collections.Generic;
using System.Threading;
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

    public HtmlModulesFeature Module { get; } = new HtmlModulesFeature {
            IgnoreIfError = true,
        }
        .Configure((appHost,module) => 
            module.VirtualFiles = appHost.VirtualFileSources);
    
    public Action<IAppHost> Configure { get; set; }

    public UiFeature()
    {
        Info = new UiInfo
        {
            HideTags = new List<string> { TagNames.Auth },
            BrandIcon = Svg.ImageUri(Svg.GetDataUri(Svg.Logos.ServiceStack, "#000000")),
            Theme = new ThemeInfo
            {
                Form = "shadow overflow-hidden sm:rounded-md bg-white",
                ModelIcon = Svg.ImageSvg(Svg.Create(Svg.Body.Table)),
            },
            DefaultFormats = new ApiFormat
            {
                // Defaults to browsers navigator.languages
                //Locale = Thread.CurrentThread.CurrentCulture.Name,
                AssumeUtc = true,
                Date = new Intl(IntlFormat.DateTime) {
                    Date = DateStyle.Medium,
                }.ToFormat(),
            },
            Locode = new()
            {
                Css = new ApiCss
                {
                    Form = "max-w-screen-2xl",
                    Fieldset = "grid grid-cols-12 gap-6",
                    Field = "col-span-12 lg:col-span-6 xl:col-span-4",
                },
                Tags = new AppTags
                {
                    Default = "Tables",
                    Other = "other",
                },
                MaxFieldLength = 150,
                MaxNestedFields = 2,
                MaxNestedFieldLength = 30,
            },
            Explorer = new()
            {
                Css = new ApiCss
                {
                    Form = "max-w-screen-md",
                    Fieldset = "grid grid-cols-12 gap-6", 
                    Field = "col-span-12 sm:col-span-6",
                },
                Tags = new AppTags
                {
                    Default = "APIs",
                    Other = "other",
                },
            },
            AdminLinks = new()
            {
                new LinkInfo
                {
                    Id = "",
                    Label = "Dashboard",
                    Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Home)),
                },
            },
        };
    }

    public void Register(IAppHost appHost) {}

    public void AfterPluginsLoaded(IAppHost appHost)
    {
        if (HtmlModules.Count > 0)
        {
            Info.Modules = HtmlModules.Map(x => x.BasePath);
            Configure?.Invoke(appHost);
            Module.Modules.AddRange(HtmlModules);
            Module.Handlers.AddRange(Handlers);
            Module.Register(appHost);
        }
    }
}