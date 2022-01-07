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
                DebugMode = true,
                AdminAuthSecret = "secret",
            });

            // Register Database Connection, see: https://github.com/ServiceStack/ServiceStack.OrmLite#usage
            container.AddSingleton<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(MapProjectPath("~/northwind.sqlite"), SqliteDialect.Provider));

            Plugins.Add(new AutoQueryFeature {
                MaxLimit = 100,
                GenerateCrudServices = new GenerateCrudServices {}
            });

            this.AssertPlugin<MetadataFeature>().UiModule = null;
            Plugins.Add(new HtmlModulesFeature(new HtmlModule("/ui"))
            {
                Handlers = {
                    new SharedFolder("shared", "/shared", defaultExt:".html"),
                    new SharedFolder("shared/js", "/shared/js", defaultExt:".js"),
                }
            });
            
            Plugins.AddIfDebug(new HotReloadFeature {
                DefaultPattern = "*.html;*.js;*.css",
                VirtualFiles = VirtualFiles,
            });
            
            Plugins.Add(new PostmanFeature());
        }
    }
}