using System;
using System.Collections.Generic;
using Funq;
using ServiceStack.Razor;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.RazorHostTests
{
    public class DataSource
    {
        public string[] Items = new[] { "Eeny", "meeny", "miny", "moe" };
    }

    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("Razor Test", typeof(AppHost).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new RazorFormat());

            container.Register(new DataSource());
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
                Name = request.Id ?? "Foo",
                Results = new List<string> { "Tom", "Dick", "Harry" }
            };
        }
    }

    
    [RestService("/viewmodel2/{Id}")]
    public class ViewThatUsesLayoutAndModel2
    {
        public string Id { get; set; }
    }

    public class ViewThatUsesLayoutAndModel2Response
    {
        public string Name { get; set; }
        public List<string> Results { get; set; }
    }

    public class View2Service : ServiceBase<ViewThatUsesLayoutAndModel2>
    {
        protected override object Run(ViewThatUsesLayoutAndModel2 request)
        {
            return new ViewThatUsesLayoutAndModel2Response {
                Name = request.Id ?? "Foo",
                Results = new List<string> { "Tom", "Dick", "Harry" }
            };
        }
    }

}
