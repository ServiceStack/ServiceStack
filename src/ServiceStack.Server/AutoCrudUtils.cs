using System;
using System.Collections.Generic;

namespace ServiceStack
{
    public static class AutoCrudUtils
    {
        public static MetadataAttribute ToAttribute(string name, Dictionary<string, object> args = null, Attribute attr = null) =>
            new MetadataAttribute {
                Name = name,
                Attribute = attr,
                Args = args?.Map(x => new MetadataPropertyType {
                    Name = x.Key,
                    Value = x.Value?.ToString(),
                    Type = x.Value?.GetType().Name,
                })
            };

        public static MetadataType AddAttribute(this MetadataType type, string name,
            Dictionary<string, object> args = null, Attribute attr = null)
        {
            var metaAttr = ToAttribute(name, args, attr);
            type.Attributes ??= new List<MetadataAttribute>();
            type.Attributes.Add(metaAttr);
            return type;
        }

        public static MetadataType AddAttribute(this MetadataType type, Attribute attr)
        {
            var nativeTypesGen = HostContext.AssertPlugin<NativeTypesFeature>().DefaultGenerator;
            var metaAttr = nativeTypesGen.ToMetadataAttribute(attr);
            type.Attributes ??= new List<MetadataAttribute>();
            type.Attributes.Add(metaAttr);
            return type;
        }
 
        public static MetadataPropertyType AddAttribute(this MetadataPropertyType propType, Attribute attr)
        {
            var nativeTypesGen = HostContext.AssertPlugin<NativeTypesFeature>().DefaultGenerator;
            var metaAttr = nativeTypesGen.ToMetadataAttribute(attr);
            propType.Attributes ??= new List<MetadataAttribute>();
            propType.Attributes.Add(metaAttr);
            return propType;
        }
    }
}