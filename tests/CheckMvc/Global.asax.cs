using System;
using System.Web.Mvc;
using System.Web.Routing;
using Funq;
using ServiceStack;

namespace CheckMvc
{
    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("Check MVC", typeof(MyServices).Assembly) {}

        public override void Configure(Container container)
        {
        }
    }

    [Route("/hello")]
    public class Hello
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
            return new HelloResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }
    }


    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            //WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            new AppHost().Init();
        }
    }
}