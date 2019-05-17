using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Mvc;

namespace CheckRazorCore
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseServiceStack(new AppHost {
                AppSettings = new NetCoreAppSettings(Configuration)
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
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/testauth")]
    public class TestAuth : IReturn<TestAuth> {}

    public class MyServices : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse {Result = $"Hello, {request.Name}!"};
        }

        [Authenticate]
        public object Any(TestAuth request) => request;
    }

    public class AppHost : AppHostBase
    {
        public AppHost() 
            : base(nameof(CheckRazorCore), typeof(MyServices).Assembly) { }

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
        }
    }
}