
using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Populate property from ambiguous column name in the specified joined table type
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class BelongToAttribute(Type belongToTableType) : AttributeBase
{
    public Type BelongToTableType { get; set; } = belongToTableType;
}