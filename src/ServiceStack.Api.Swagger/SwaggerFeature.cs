using System;
using System.Text.RegularExpressions;

namespace ServiceStack.Api.Swagger
{
    public class SwaggerFeature : IPlugin
    {
        /// <summary>
        /// Gets or sets <see cref="Regex"/> pattern to filter available resources. 
        /// </summary>
        public string ResourceFilterPattern { get; set; }

        public bool UseCamelCaseModelPropertyNames { get; set; }

        public bool UseLowercaseUnderscoreModelPropertyNames { get; set; }

        public bool DisableAutoDtoInBodyParam { get; set; }

        public Action<SwaggerModel> ModelFilter { get; set; }

        public Action<ModelProperty> ModelPropertyFilter { get; set; }

        public void Register(IAppHost appHost)
        {
            if (ResourceFilterPattern != null)
                SwaggerResourcesService.resourceFilterRegex = new Regex(ResourceFilterPattern, RegexOptions.Compiled);

            SwaggerApiService.UseCamelCaseModelPropertyNames = UseCamelCaseModelPropertyNames;
            SwaggerApiService.UseLowercaseUnderscoreModelPropertyNames = UseLowercaseUnderscoreModelPropertyNames;
            SwaggerApiService.DisableAutoDtoInBodyParam = DisableAutoDtoInBodyParam;
            SwaggerApiService.ModelFilter = ModelFilter;
            SwaggerApiService.ModelPropertyFilter = ModelPropertyFilter;

            appHost.RegisterService(typeof(SwaggerResourcesService), new[] { "/resources" });
            appHost.RegisterService(typeof(SwaggerApiService), new[] { SwaggerResourcesService.RESOURCE_PATH + "/{Name*}" });
        }

        public static bool IsEnabled
        {
            get { return HostContext.HasPlugin<SwaggerFeature>(); }
        }
    }
}