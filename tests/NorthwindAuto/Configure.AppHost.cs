using Funq;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using MyApp.ServiceInterface;

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
                DebugMode = true
            });

            // Register Database Connection, see: https://github.com/ServiceStack/ServiceStack.OrmLite#usage
            container.AddSingleton<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(MapProjectPath("~/northwind.sqlite"), SqliteDialect.Provider));

            Plugins.Add(new AutoQueryFeature {
                MaxLimit = 100,
                GenerateCrudServices = new GenerateCrudServices {}
            });

            Plugins.RemoveAll(x => x is HtmlModulesFeature { Id: "module:/ui" });
            Plugins.Add(new HtmlModulesFeature(new HtmlModule("/ui")));
            
            Plugins.AddIfDebug(new HotReloadFeature {
                DefaultPattern = "*.html;*.js;*.css",
                VirtualFiles = VirtualFiles,
            });
            
            Plugins.Add(new PostmanFeature());
        }
    }
}