using System.Net;
using Funq;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Mvc;
using ServiceStack.Validation;

namespace CheckRazorCore;

public class AppHost() : AppHostBase(nameof(CheckRazorCore), typeof(MyServices).Assembly)
{
    public override void Configure(IServiceCollection services)
    {
        //Register dependencies shared by ServiceStack and ASP.NET Core 
    }

    public override void Configure(Container container)
    {
        if (Config.DebugMode)
        {
            Plugins.Add(new HotReloadFeature {
                DefaultPattern = "*.js;*.css;*.html;*.cshtml"
            });
        }

        Plugins.Add(new RazorFormat());

        Plugins.Add(new ValidationFeature());

        this.CustomErrorHttpHandlers[HttpStatusCode.Forbidden] = new RazorHandler("/forbidden");
    }
}
