using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Funq;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace NorthwindCrud
{
    public class Startup : ModularStartup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public new void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseServiceStack(new AppHost
            {
                AppSettings = new NetCoreAppSettings(Configuration)
            });
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost() : base("Northwind Auto", typeof(MyServices).Assembly) { }

        public override void Configure(IServiceCollection services)
        {
            // Register Database Connection, see: https://github.com/ServiceStack/ServiceStack.OrmLite#usage
            services.AddSingleton<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(MapProjectPath("~/northwind.sqlite"), SqliteDialect.Provider));
        }

        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            SetConfig(new HostConfig { DebugMode = true });

            // Register Database Connection, see: https://github.com/ServiceStack/ServiceStack.OrmLite#usage
            container.AddSingleton<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(MapProjectPath("~/northwind.sqlite"), SqliteDialect.Provider));

            Plugins.Add(new AutoQueryFeature {
                MaxLimit = 100,
                GenerateCrudServices = new GenerateCrudServices {}
            });
        }
    }

    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    public class MyServices : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }
    }

}
