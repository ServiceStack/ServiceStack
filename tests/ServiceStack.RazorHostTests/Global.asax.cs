using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Funq;
using ServiceStack.RazorEngine;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.RazorHostTests
{
    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("Razor Test", typeof(AppHost).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new RazorFormat());
        }
    }

    public class Global : System.Web.HttpApplication
    {

        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            new AppHost().Init();
        }

        void Application_End(object sender, EventArgs e)
        {
            //  Code that runs on application shutdown

        }

        void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs

        }
    }

    [RestService("/viewmodel/{Id}")]
    public class ViewThatUsesLayoutAndModel
    {
        public string Id { get; set; }
    }

    public class ViewThatUsesLayoutAndModelResponse
    {
        public string Name { get; set; }
        public List<string> Results { get; set; }
    }

    public class ViewService : ServiceBase<ViewThatUsesLayoutAndModel>
    {
        protected override object Run(ViewThatUsesLayoutAndModel request)
        {
            return new ViewThatUsesLayoutAndModelResponse {
                Name = "Foo",
                Results = new List<string> { "Tom", "Dick", "Harry" }
            };
        }
    }

}
