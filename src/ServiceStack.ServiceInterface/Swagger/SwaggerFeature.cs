﻿using System.Text.RegularExpressions;
using ServiceStack.WebHost.Endpoints;

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
            
            appHost.RegisterService(typeof (SwaggerResourcesService), new[] {"/resources"});
            appHost.RegisterService(typeof(SwaggerApiService), new[] { "/resource/{Name*}" });
        }
    }
}