using System;

namespace ServiceStack;

[AttributeUsage(AttributeTargets.Property)]
public class InputAttribute : AttributeBase
{
    public string Type { get; set; }
    public string Value { get; set; }
    public string Placeholder { get; set; }
    public string Help { get; set; }
    public string Label { get; set; }
    public string Title { get; set; }
    public string Size { get; set; }
    public string Pattern { get; set; }
    public bool ReadOnly { get; set; }
    public bool Required { get; set; }
    public bool Disabled { get; set; }
    public string Autocomplete { get; set; }
    public string Autofocus  { get; set; }
    public string Min { get; set; }
    public string Max { get; set; }
    public int Step { get; set; } = int.MinValue;
    public int MinLength { get; set; } = int.MinValue;
    public int MaxLength { get; set; } = int.MinValue;
    public string[] AllowableValues { get; set; }
}
