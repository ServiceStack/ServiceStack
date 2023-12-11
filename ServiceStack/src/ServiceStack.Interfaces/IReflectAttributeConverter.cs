using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack;

public interface IReflectAttributeConverter
{
    ReflectAttribute ToReflectAttribute();
}

public interface IReflectAttributeFilter
{
    bool ShouldInclude(PropertyInfo pi, string value);
}

public class ReflectAttribute
{
    public string Name { get; set; }
    public List<KeyValuePair<PropertyInfo, object>> ConstructorArgs { get; set; }
    public List<KeyValuePair<PropertyInfo, object>> PropertyArgs { get; set; }
}