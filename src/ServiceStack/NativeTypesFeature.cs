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

        public NativeTypesFeature()
        {
            MetadataTypesConfig = new MetadataTypesConfig
            {
                AddDefaultXmlNamespace = HostConfig.DefaultWsdlNamespace,
                TypeAlias = new Dictionary<string, string> 
                {
                    { "String", "string" },    
                    { "Boolean", "bool" },    
                    { "Byte", "byte" },    
                    { "Int16", "short" },    
                    { "Int32", "int" },    
                    { "Int64", "long" },    
                    { "UInt16", "ushort" },    
                    { "UInt32", "uint" },    
                    { "UInt64", "ulong" },    
                    { "Single", "float" },    
                    { "Double", "double" },    
                    { "Decimal", "decimal" },    
                },
                ExportAttributes = new HashSet<Type>
                {
                    typeof(ApiAttribute),
                    typeof(ApiResponseAttribute),
                    typeof(ApiMemberAttribute),
                    typeof(StringLengthAttribute),
                    typeof(DefaultAttribute),
                    typeof(IgnoreAttribute),
                    typeof(IgnoreDataMemberAttribute),
                    typeof(MetaAttribute),
                    typeof(RequiredAttribute),
                    typeof(ReferencesAttribute),
                    typeof(StringLengthAttribute),
                },
                IgnoreTypes = new HashSet<Type>
                {
                },
                IgnoreTypesInNamespaces = new List<string>
                {
                    "ServiceStack",    
                    "ServiceStack.Auth",
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
                }
            };
        }

        public void Register(IAppHost appHost)
        {
            appHost.Register<INativeTypesMetadata>(
                new NativeTypesMetadata(appHost.Metadata, MetadataTypesConfig));

            appHost.RegisterService<NativeTypesService>();
        }
    }
}