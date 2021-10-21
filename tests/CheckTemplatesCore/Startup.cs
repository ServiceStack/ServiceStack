using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Validation;

namespace CheckTemplatesCore
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration) => Configuration = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
        public AppHost() : base("MyApp", typeof(MyServices).Assembly) { }

        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            base.SetConfig(new HostConfig
            {
                DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), true)
            });

            Plugins.Add(new ValidationFeature());
            Plugins.Add(new SharpPagesFeature());
            
            this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = 
                new SharpPageHandler("/notfound");
            
            this.GlobalHtmlErrorHttpHandler = new SharpPageHandler("/error");
        }
    }

    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/throw404")]
    [Route("/throw404/{Message}")]
    public class Throw404
    {
        public string Message { get; set; }
    }

    [Route("/throw")]
    [Route("/throw/{Message}")]
    public class Throw
    {
        public string Message { get; set; }
    }

    public class MyServices : Service
    {
        public object Any(Hello request) => new HelloResponse {
            Result = $"Hi, {request.Name}!"
        };
        
        public object Any(Throw404 request) => throw HttpError.NotFound(request.Message ?? "Not Found");
        
        public object Any(Throw request) => throw new Exception(request.Message ?? "Exception in 'Throw' Service");
        
        public object Any(ViewIndex request)
        {
            return Request.GetPageResult("/index");
            //equivalent to: return new PageResult(Request.GetPage("/index")).BindRequest(Request);
        }

        public object Any(ValidationTest request) => request;
    }
    
    [Route("/validation/test")]
    public class ValidationTest : IReturn<ValidationTest>
    {
        [ValidateNotNull]
        public string Name { get; set; }
    }

    [FallbackRoute("/{PathInfo*}", Matches="AcceptsHtml")]
    public class ViewIndex
    {
        public string PathInfo { get; set; }
    }
}