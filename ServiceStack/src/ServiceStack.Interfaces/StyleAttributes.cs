#nullable enable
using System;

namespace ServiceStack;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ExplorerCssAttribute : AttributeBase
{
    public string? Form { get; set; }
    public string? Fieldset { get; set; }
    public string? Field { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class LocodeCssAttribute : AttributeBase
{
    public string? Form { get; set; }
    public string? Fieldset { get; set; }
    public string? Field { get; set; }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class FieldCssAttribute : AttributeBase
{
    public string? Field { get; set; }
    public string? Input { get; set; }
    public string? Label { get; set; }
}
