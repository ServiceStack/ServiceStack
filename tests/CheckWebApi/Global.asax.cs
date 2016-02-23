using System;
using Funq;
using ServiceStack;

namespace CheckWebApi
{
    public class AppHost : AppHostBase
    {
        public AppHost() 
            : base("CheckWeb at /api", typeof(MyServices).Assembly) {}

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                HandlerFactoryPath = "api",
                DefaultRedirectPath = "docs"
            });
        }
    }


    public class Hello { }

    public class MyServices : Service
    {
        public object Any(Hello request)
        {
            return request;
        }
    }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            new AppHost().Init();
        }
    }
}