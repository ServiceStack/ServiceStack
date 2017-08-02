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
using ServiceStack.Auth;
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

            //app.Use(new RazorHandler("/login").Middleware);
            //app.Use(new StaticFileHandler("wwwroot/img/react-logo.png").Middleware);
            //app.Use(new StaticFileHandler("wwwroot/_ViewImports.cshtml").Middleware);
            //app.Use(new RequestInfoHandler());

            app.Use(new RazorHandler("/notfound"));

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

    public class Test
    {
        public string ExternalId { get; set; }
    }

    [Route("/test")]
    public class TestGet : IGet, IReturn<Test>
    {
    }

    public class TestService : Service
    {
        public Test Get(TestGet request)
        {
            var test = new Test { ExternalId = "abc" };
            return test;
        }
    }

    [Route("/req-info")]
    public class GetRequestInfo { }

    public class RequestInfoServices : Service
    {
        public object Any(GetRequestInfo request)
        {
            return new RequestInfoResponse
            {
                HttpMethod = Request.Verb,
                RawUrl = Request.RawUrl,
                AbsoluteUri = Request.AbsoluteUri,
                PathInfo = Request.PathInfo,
            };
        }
    }

    class AppHost : AppHostBase
    {
        public AppHost() : base("ServiceStack + MVC Integration", typeof(MyServices).GetAssembly()) {}

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                DebugMode = true,
                HandlerFactoryPath = "api"
            });

            Plugins.Add(new RazorFormat());

            //Works but recommend handling 404 at end of .NET Core pipeline
            //this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = new RazorHandler("/notfound");
            this.CustomErrorHttpHandlers[HttpStatusCode.Unauthorized] = new RazorHandler("/login");

            Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[]
                {
                    new CredentialsAuthProvider(),        //HTML Form post of UserName/Password credentials
                    new BasicAuthProvider(),                    //Sign-in with HTTP Basic Auth
                    new DigestAuthProvider(AppSettings),        //Sign-in with HTTP Digest Auth
                    new TwitterAuthProvider(AppSettings),       //Sign-in with Twitter
                    new FacebookAuthProvider(AppSettings),      //Sign-in with Facebook
                    new GithubAuthProvider(AppSettings),        //Sign-in with GitHub OAuth Provider
                    new YandexAuthProvider(AppSettings),        //Sign-in with Yandex OAuth Provider        
                    new VkAuthProvider(AppSettings),            //Sign-in with VK.com OAuth Provider 
                })
            {
                HtmlRedirect = "/",
                IncludeRegistrationService = true,
            });

            container.Register<IAuthRepository>(c =>
                new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()) {
                    UseDistinctRoleTables = AppSettings.Get("UseDistinctRoleTables", true),
                });

            var authRepo = (OrmLiteAuthRepository)container.Resolve<IAuthRepository>();
            SessionService.ResetUsers(authRepo);
        }
    }
}
