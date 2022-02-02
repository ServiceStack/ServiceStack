using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack.CefGlue.Win64.AspNetCore
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseServiceStack(new AppHost());

            // app.Run(context =>
            // {
            //     context.Response.Redirect("/");
            //     return Task.FromResult(0);
            // });
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost() : base("MyApp", typeof(MyServices).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new SharpPagesFeature());

            // Plugins.Add(new ProxyFeature(
            //     matchingRequests: req => req.PathInfo.StartsWith("/theverge"),
            //     resolveUrl: req => $"https://www.theverge.com" + req.RawUrl.Replace("/theverge", "/")) {
            //     IgnoreResponseHeaders = {
            //         "X-Frame-Options"
            //     }
            // });
        }
    }

    [Route("/hello")]
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
        public object Any(Hello request) => new HelloResponse { Result = $"Hello, {request.Name}!" };
    }

}