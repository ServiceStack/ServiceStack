#nullable enable

using System;
using System.Reflection;

namespace ServiceStack;

public class AttributeBase : Attribute
{
    protected readonly Guid typeId = Guid.NewGuid(); //Hack required to give Attributes unique identity
}

public class MetadataAttributeBase : AttributeBase, IReflectAttributeFilter
{
    /// <summary>
    /// Don't include default bool or nullable int default values
    /// </summary>
    public virtual bool ShouldInclude(PropertyInfo pi, string value)
    {
        if (pi.PropertyType == typeof(int) && value == "-2147483648") //int.MinValue
            return false;
        if (pi.PropertyType == typeof(bool) && value == "false")
            return false;
            
        return true;
    }
}