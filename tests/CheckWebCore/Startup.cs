using System.Collections.Generic;
using System.Net;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Mvc;
using ServiceStack.Validation;

namespace CheckWebCore
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration) => Configuration = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var AppSettings = new NetCoreAppSettings(Configuration);
            AppSettings.GetNullableString("servicestack:license");
            
            app.UseServiceStack(new AppHost
            {
                AppSettings = AppSettings
            });
        }
    }
    
    public class AppHost : AppHostBase
    {
        public AppHost() : base("TestLogin", typeof(MyServices).Assembly) { }


        // http://localhost:5000/auth/credentials?username=testman@test.com&&password=!Abc1234
        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            // enable server-side rendering, see: https://sharpscript.net
            Plugins.Add(new SharpPagesFeature()); 

            if (Config.DebugMode)
            {
                Plugins.Add(new HotReloadFeature());
            }

            Plugins.Add(new RazorFormat()); // enable ServiceStack.Razor
            
            SetConfig(new HostConfig
            {
                AddRedirectParamsToQueryString = true,
                DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), false),
                UseSameSiteCookies = true, // prevents OAuth providers which use Sessions like Twitter from working
                UseSecureCookies = true,
            });

            container.Register<ICacheClient>(new MemoryCacheClient());
            
            Plugins.Add(new ValidationFeature());
        }
    }
    
    [Exclude(Feature.Metadata)]
    [FallbackRoute("/{PathInfo*}", Matches="AcceptsHtml")]
    public class FallbackForClientRoutes
    {
        public string PathInfo { get; set; }
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

    [Route("/testauth")]
    public class TestAuth : IReturn<TestAuth> {}

    [Route("/session")]
    public class Session : IReturn<AuthUserSession> {}
    
    [Route("/throw")]
    public class Throw {}

    //    [Authenticate]
    public class MyServices : Service
    {
        //Return index.html for unmatched requests so routing is handled on client
        public object Any(FallbackForClientRoutes request) => 
            Request.GetPageResult("/");
//            new PageResult(Request.GetPage("/")) { Args = { [nameof(Request)] = Request } };

        public object Any(Hello request)
        {
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }

        public object Any(TestAuth request) => request;

        [Authenticate]
        public object Any(Session request) => SessionAs<AuthUserSession>();

        public object Any(Throw request) => HttpError.Conflict("Conflict message");
//        public object Any(Throw request) => new HttpResult
//            {StatusCode = HttpStatusCode.Conflict, Response = "Error message"};
    }
}