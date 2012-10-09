using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Metadata;

namespace ServiceStack.ServiceInterface.Swagger
{
    public class SwaggerFeature : IPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.RegisterService(typeof (SwaggerResourcesService), new string[] {"/resources"});
            appHost.RegisterService(typeof(SwaggerApiService), new string[] { "/resource/{Name*}" });
        }
    }
}