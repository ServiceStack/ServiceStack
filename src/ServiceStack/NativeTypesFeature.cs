using System;
using System.Collections.Generic;
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
                SkipExistingTypes = new HashSet<Type>
                {
                    typeof(ResponseStatus),
                    typeof(ErrorResponse),
                    typeof(Authenticate),
                    typeof(AuthenticateResponse),
                    typeof(Register),
                    typeof(RegisterResponse),
                    typeof(QueryResponse<>),
                },
                IgnoreTypes = new HashSet<Type>
                {
                    typeof(TypesCSharp),
                    typeof(TypesMetadata),
                    typeof(NativeTypesBase),
                    typeof(MetadataTypesConfig),
                },
                DefaultNamespaces = new List<string> 
                {
                    "System",
                    "System.Collections",
                    "System.ComponentModel",
                    "System.Collections.Generic",
                    "System.Runtime.Serialization",
                    "ServiceStack.ServiceHost",
                    "ServiceStack.ServiceInterface.ServiceModel",
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