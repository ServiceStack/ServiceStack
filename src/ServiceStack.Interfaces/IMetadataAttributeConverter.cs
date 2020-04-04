using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack
{
    public interface IMetaAttributeConverter
    {
        MetaAttribute ToMetaAttribute();
    }

    public class MetaAttribute
    {
        public string Name { get; set; }
        public List<KeyValuePair<PropertyInfo, object>> ConstructorArgs { get; set; }
        public List<KeyValuePair<PropertyInfo, object>> Args { get; set; }
    }
}