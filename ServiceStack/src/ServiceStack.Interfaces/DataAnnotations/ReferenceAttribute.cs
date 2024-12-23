#nullable enable
using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Define this property as containing a POCO Complex Type Reference
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ReferenceAttribute : AttributeBase
{
    public string? SelfId { get; set; }
    public string? RefId { get; set; }
}