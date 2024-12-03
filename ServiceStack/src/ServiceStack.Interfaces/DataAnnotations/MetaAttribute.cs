using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Decorate any type or property with custom metadata
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class MetaAttribute : AttributeBase
{
    public string Name { get; set; }
    public string Value { get; set; }

    public MetaAttribute(string name, string value)
    {
        Name = name;
        Value = value;
    }
}