using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.NativeTypes;

namespace ServiceStack
{
    public class NativeTypesFeature : IPlugin
    {
        public MetadataTypesConfig MetadataTypesConfig { get; set; }

        public static bool DisableTokenVerification { get; set; }

        public Func<MetadataType, bool?> InitializeCollectionsForType { get; set; }

        public static bool? DontInitializeAutoQueryCollections(MetadataType type)
        {
            return type.Inherits != null
                && (type.Inherits.Name == "QueryBase`1" || type.Inherits.Name == "QueryDb`1" || type.Inherits.Name == "QueryData`1")
                    ? false
                    : (bool?)null;
        }

        public NativeTypesFeature()
        {
            MetadataTypesConfig = new MetadataTypesConfig
            {
                AddDefaultXmlNamespace = HostConfig.DefaultWsdlNamespace,
                ExportAttributes = new HashSet<Type>
                {
                    typeof(FlagsAttribute),
                    typeof(ApiAttribute),
                    typeof(ApiResponseAttribute),
                    typeof(ApiMemberAttribute),
                    typeof(StringLengthAttribute),
                    typeof(IgnoreAttribute),
                    typeof(IgnoreDataMemberAttribute),
                    typeof(MetaAttribute),
                    typeof(RequiredAttribute),
                    typeof(ReferencesAttribute),
                    typeof(StringLengthAttribute),
                    typeof(AutoQueryViewerAttribute),
                    typeof(AutoQueryViewerFieldAttribute),
                },
                ExportTypes = new HashSet<Type>
                {
                    typeof(IGet),                    
                    typeof(IPost),                    
                    typeof(IPut),                    
                    typeof(IDelete),                    
                    typeof(IPatch),
                },
                IgnoreTypes = new HashSet<Type>
                {
                },
                IgnoreTypesInNamespaces = new List<string>
                {
                    "ServiceStack",
                    "ServiceStack.Auth",
                    "ServiceStack.Caching",
                    "ServiceStack.Configuration",
                    "ServiceStack.Data",
                    "ServiceStack.IO",
                    "ServiceStack.Logging",
                    "ServiceStack.Messaging",
                    "ServiceStack.Model",
                    "ServiceStack.Redis",
                    "ServiceStack.Web",
                    "ServiceStack.Admin",
                    "ServiceStack.NativeTypes",
                    "ServiceStack.Api.Swagger",
                },
                DefaultNamespaces = new List<string>
                {
                    "System",
                    "System.Collections",
                    "System.Collections.Generic",
                    "System.Runtime.Serialization",
                    "ServiceStack",
                    "ServiceStack.DataAnnotations",
                },
            };
        }

        public void Register(IAppHost appHost)
        {
            appHost.Register<INativeTypesMetadata>(
                new NativeTypesMetadata(appHost.Metadata, MetadataTypesConfig));

            appHost.RegisterService<NativeTypesService>();
        }
    }

    internal static class NativeTypesFeatureExtensions
    {
        internal static bool ShouldInitializeCollections(this NativeTypesFeature feature, MetadataType type, bool defaultValue)
        {
            return feature.InitializeCollectionsForType != null
                ? feature.InitializeCollectionsForType(type).GetValueOrDefault(defaultValue)
                : defaultValue;
        }
    }
}