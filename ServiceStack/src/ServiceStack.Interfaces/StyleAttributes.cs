#nullable enable
using System;

namespace ServiceStack;

/// <summary>
/// Customize the Form and Field CSS in API Explorer Forms
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ExplorerCssAttribute : AttributeBase
{
    public string? Form { get; set; }
    public string? Fieldset { get; set; }
    public string? Field { get; set; }
}

/// <summary>
/// Customize the Form and Field CSS in Locode Forms
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class LocodeCssAttribute : AttributeBase
{
    public string? Form { get; set; }
    public string? Fieldset { get; set; }
    public string? Field { get; set; }
}

/// <summary>
/// Customize a Property Form Field CSS
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class FieldCssAttribute : AttributeBase
{
    public string? Field { get; set; }
    public string? Input { get; set; }
    public string? Label { get; set; }
}
