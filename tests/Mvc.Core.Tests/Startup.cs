using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RazorRockstars;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.Host.Handlers;
using ServiceStack.OrmLite;
using ServiceStack.Mvc;

namespace Mvc.Core.Tests
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddSingleton<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseServiceStack(new AppHost());

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            //app.Use(new RazorHandler("/notfound").Middleware);
            //app.Use(new RazorHandler("/login").Middleware);
            //app.Use(new StaticFileHandler("wwwroot/img/react-logo.png").Middleware);
            //app.Use(new StaticFileHandler("wwwroot/_ViewImports.cshtml").Middleware);
            app.Use(new RequestInfoHandler());

            //Populate Rockstars
            using (var db = app.ApplicationServices.GetService<IDbConnectionFactory>().Open())
            {
                db.CreateTableIfNotExists<Rockstar>();
                db.InsertAll(RockstarsService.SeedData);
            }
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
        public object Any(Hello request) =>
            new HelloResponse { Result = $"Hello, {request.Name ?? "World"}!" };
    }

    class AppHost : AppHostBase
    {
        public AppHost() : base("ServiceStack + MVC Integration", typeof(MyServices).GetAssembly()) {}

        public override void Configure(Container container)
        {
            Plugins.Add(new RazorFormat());

            //Works but recommend handling 404 at end of .NET Core pipeline
            //this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = new RazorHandler("/notfound");
            this.CustomErrorHttpHandlers[HttpStatusCode.Unauthorized] = new RazorHandler("/login");
        }
    }
}
