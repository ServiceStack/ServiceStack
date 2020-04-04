using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack
{
    public interface IReflectAttributeConverter
    {
        ReflectAttribute ToReflectAttribute();
    }

    public class ReflectAttribute
    {
        public string Name { get; set; }
        public List<KeyValuePair<PropertyInfo, object>> ConstructorArgs { get; set; }
        public List<KeyValuePair<PropertyInfo, object>> PropertyArgs { get; set; }
    }
}