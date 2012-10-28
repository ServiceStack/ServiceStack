using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Metadata;

namespace ServiceStack.ServiceInterface.Swagger
{
    public class SwaggerFeature : IPlugin
    {
        /// <summary>
        /// Gets or sets <see cref="Regex"/> pattern to filter available resources. 
        /// </summary>
        public string ResourceFilterPattern { get; set; }

        public void Register(IAppHost appHost)
        {
            if (ResourceFilterPattern != null)
                SwaggerResourcesService.resourceFilterRegex = new Regex(ResourceFilterPattern, RegexOptions.Compiled);
            
            appHost.RegisterService(typeof (SwaggerResourcesService), new string[] {"/resources"});
            appHost.RegisterService(typeof(SwaggerApiService), new string[] { "/resource/{Name*}" });
        }
    }
}