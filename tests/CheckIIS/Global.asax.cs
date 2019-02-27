using System;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using Funq;
using ServiceStack;

namespace CheckIIS
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            new AppHost()
                .Init();

            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
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

    public class MyServices : Service
    {
        public object Any(Hello request) => new HelloResponse
        {
            Result = $"Hi, {request.Name}!"
        };
    }

    public class AppHost : AppHostBase
    {
        public AppHost() 
            : base(nameof(MyServices), typeof(MyServices).Assembly) {}

        public override void Configure(Container container)
        {
        }
    }
}