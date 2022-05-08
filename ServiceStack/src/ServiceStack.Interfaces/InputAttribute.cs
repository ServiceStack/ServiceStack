using System;

namespace ServiceStack;

/// <summary>
/// Customize the HTML Input control in Auto Form UIs
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class InputAttribute : InputAttributeBase
{
}

/// <summary>
/// Customize a Form Field and HTML Input for a Type's Property
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class FieldAttribute : InputAttributeBase
{
    public string Name { get; set; }
    public string FieldCss { get; set; }
    public string InputCss { get; set; }
    public string LabelCss { get; set; }

    public FieldAttribute(){}
    public FieldAttribute(string name) => Name = name;
}

public class InputAttributeBase : MetadataAttributeBase
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
    public string Accept  { get; set; }
    public string Capture  { get; set; }
    public bool Multiple { get; set; }
    public string[] AllowableValues { get; set; }
    public string Options  { get; set; }
    public bool Ignore { get; set; }
}