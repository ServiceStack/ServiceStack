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
                CSharpTypeAlias = new Dictionary<string, string> 
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
                FSharpTypeAlias = new Dictionary<string, string>
                {
                },
                VbNetTypeAlias = new Dictionary<string, string>
                {
                    { "Int16", "Short" },    
                    { "Int32", "Integer" },    
                    { "Int64", "Long" },    
                    { "DateTime", "Date" },    
                },
                TypeScriptTypeAlias = new Dictionary<string, string>
                {
                    { "String", "string" },    
                    { "Boolean", "boolean" },    
                    { "DateTime", "string" },    
                    { "TimeSpan", "string" },    
                    { "Byte", "number" },    
                    { "Int16", "number" },    
                    { "Int32", "number" },    
                    { "Int64", "number" },    
                    { "UInt16", "number" },    
                    { "UInt32", "number" },    
                    { "UInt64", "number" },    
                    { "Single", "number" },    
                    { "Double", "number" },    
                    { "Decimal", "number" },    
                },
                VbNetKeyWords = new HashSet<string>
                {
                    "Default",
                    "Dim",
                    "Catch",
                    "Byte",
                    "Short",
                    "Integer",
                    "Long",
                    "UShort",
                    "ULong",
                    "Double",
                    "Decimal",
                    "String",
                    "Object",
                    "Each",
                    "Error",
                    "Finally",
                    "Function",
                    "Global",
                    "If",
                    "Imports",
                    "Inherits",
                    "Not",
                    "IsNot",
                    "Module",
                    "MyBase",
                    "Option",
                    "Out",
                    "Protected",
                    "Return",
                    "Shadows",
                    "Static",
                    "Then",
                    "With",
                    "When",
                },
                ExportAttributes = new HashSet<Type>
                {
                    typeof(FlagsAttribute),
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