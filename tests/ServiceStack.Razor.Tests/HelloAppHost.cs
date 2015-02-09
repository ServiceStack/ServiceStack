using System.IO;
using System.Web;
using ServiceStack.Admin;
using ServiceStack.Web;

namespace ServiceStack.Razor.Tests
{
    public class HelloAppHost : AppHostBase
    {
        public HelloAppHost()
            : base("Hello Web Services", typeof(HelloService).Assembly) { }

        public override void Configure(Funq.Container container)
        {
            //http://stackoverflow.com/questions/13206038/servicestack-razor-default-page/13206221

            var razor3 = new RazorFormat();

            this.Plugins.Add(razor3);
            this.Plugins.Add(new RequestLogsFeature()
                {
                    EnableErrorTracking = true,
                    EnableResponseTracking = true,
                    EnableSessionTracking = true,
                    EnableRequestBodyTracking = true,
                    RequiredRoles = new string[0]
                });

            this.PreRequestFilters.Add(SimplePreRequestFilter);

            this.GlobalRequestFilters.Add(SimpleRequestFilter);

            //this.SetConfig( new EndpointHostConfig()
            //    {
            //        DebugMode = false,

            //    } );


            this.Routes.Add<HelloRequest>("/hello");
            this.Routes.Add<HelloRequest>("/hello/{Name}");
            this.Routes.Add<FooRequest>("/Foo/{WhatToSay}");
            this.Routes.Add<DefaultViewFooRequest>("/DefaultViewFoo/{WhatToSay}");
        }

        private void SimpleRequestFilter(IRequest req, IResponse res, object obj)
        {
            if (Path.GetFileName(req.PathInfo).StartsWith("_"))
            {
                throw new HttpException("FIles with leading underscore ('_') cannot be served.");
            }
        }

        private void SimplePreRequestFilter(IRequest req, IResponse res)
        {
            if (Path.GetFileName(req.PathInfo).StartsWith("_"))
            {
                throw new HttpException("Files with leading underscores ('_') cannot be served.");
            }
        }
    }
}
