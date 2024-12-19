using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Decorate any type or property with custom metadata
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class MetaAttribute(string name, string value) : AttributeBase
{
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;
}