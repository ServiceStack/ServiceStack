using Funq;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using MyApp.ServiceInterface;
using ServiceStack.HtmlModules;

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp
{
    public class AppHost : AppHostBase, IHostingStartup
    {
        public void Configure(IWebHostBuilder builder) => builder
            .Configure(app => {
                if (!HasInit) 
                    app.UseServiceStack(new AppHost());
            });
        
        public AppHost() : base("Northwind Auto", typeof(MyServices).Assembly) { }

        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                //DebugMode = false,
                DebugMode = true,
                AdminAuthSecret = "secret",
            });

            // Register Database Connection, see: https://github.com/ServiceStack/ServiceStack.OrmLite#usage
            container.AddSingleton<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(MapProjectPath("~/northwind.sqlite"), SqliteDialect.Provider));

            // For TodosService
            Plugins.Add(new AutoQueryDataFeature());

            // For NorthwindAuto + Bookings
            Plugins.Add(new AutoQueryFeature {
                MaxLimit = 100,
                GenerateCrudServices = new GenerateCrudServices {}
            });

            var uiFeature = this.AssertPlugin<UiFeature>();
            uiFeature.Configure = feature =>
            {
                //feature.Module.EnableHttpCaching = true;
                feature.Module.Configure = null;
                feature.HtmlModules.ForEach(x => x.DirPath = x.DirPath.Replace("/modules",""));
                feature.Handlers.Cast<SharedFolder>().Each(x => x.SharedDir = x.SharedDir.Replace("/modules", ""));
            };
            
            Plugins.AddIfDebug(new HotReloadFeature {
                DefaultPattern = "*.html;*.js;*.css",
                VirtualFiles = VirtualFiles,
            });
            
            Plugins.Add(new PostmanFeature());
        }
    }
}