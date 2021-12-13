using System;

namespace ServiceStack;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public class TextInputAttribute : AttributeBase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public string Placeholder { get; set; }
    public string Help { get; set; }
    public string Label { get; set; }
    public string Size { get; set; }
    public string Pattern { get; set; }
    public bool? ReadOnly { get; set; }
    public bool? IsRequired { get; set; }
    public string Min { get; set; }
    public string Max { get; set; }
    public int? Step { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string[] AllowableValues { get; set; }

    public TextInputAttribute() { }
    public TextInputAttribute(string id) => Id = id;
    public TextInputAttribute(string id, string type)
    {
        Id = id;
        Type = type;
    }
}
