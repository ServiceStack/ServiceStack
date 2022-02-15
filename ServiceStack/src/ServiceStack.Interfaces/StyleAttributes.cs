#nullable enable
using System;

namespace ServiceStack;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ExplorerStylesAttribute : AttributeBase
{
    public string? Form { get; set; }
    public string? Rows { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class QueryStylesAttribute : AttributeBase
{
    public string? Form { get; set; }
    public string? Rows { get; set; }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class StyleAttribute : AttributeBase
{
    public string Cls { get; set; }

    public StyleAttribute(string cls) => Cls = cls;
}
