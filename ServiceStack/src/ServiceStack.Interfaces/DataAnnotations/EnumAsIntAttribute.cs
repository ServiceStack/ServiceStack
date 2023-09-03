using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Save Enum integer value in RDBMS column
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public class EnumAsIntAttribute : AttributeBase
{
}