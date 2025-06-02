#nullable enable

using System;

namespace ServiceStack;

/// <summary>
/// Format Results to use custom formatting function. 
/// Can use any available JS function, see <see cref="FormatMethods"/> for built-in format functions
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FormatAttribute : AttributeBase
{
    /// <summary>
    /// Name of available JS function, see <see cref="FormatMethods"/> for built-in functions
    /// </summary>
    public string Method { get; set; }
    public string? Options { get; set; }
    public string? Locale { get; set; }
    public FormatAttribute(){}
    public FormatAttribute(string method) => Method = method;
}

/// <summary>
/// Format Enum Flags into expanded enum strings 
/// </summary>
public class FormatEnumFlags : FormatAttribute
{
    public FormatEnumFlags(string type) : base(FormatMethods.EnumFlags)
    {
        Options = "{type:'" + type + "'}";
    }
}
