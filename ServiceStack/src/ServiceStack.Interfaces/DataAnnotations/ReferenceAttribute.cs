using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Define this property as containing a POCO Complex Type Reference
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ReferenceAttribute : AttributeBase
{
}