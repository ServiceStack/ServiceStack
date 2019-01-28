using System;
using Check.ServiceModel.Types;
using ServiceStack;
using ServiceStack.NativeTypes.CSharp;

namespace Check.ServiceInterface
{
    /// <summary>
    /// Run before AppHost.Configure() is run.
    /// </summary>
    public class ConfigureNativeTypes : IConfigureAppHost
    {
        public void Configure(IAppHost appHost)
        {
            CSharpGenerator.PreTypeFilter = (sb, type) => 
            {
                if (!type.IsEnum.GetValueOrDefault() && !type.IsInterface.GetValueOrDefault())
                {
                    sb.AppendLine("[Serializable]");
                }
            };
            
            var nativeTypes = appHost.GetPlugin<NativeTypesFeature>();
            nativeTypes.MetadataTypesConfig.ExportTypes.Add(typeof(DayOfWeek));
            nativeTypes.MetadataTypesConfig.IgnoreTypes.Add(typeof(IgnoreInMetadataConfig));

            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute));
            //nativeTypes.MetadataTypesConfig.GlobalNamespace = "Check.ServiceInterface";
        }
    }
}