using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Treat this property is the Primary Key of the table
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PrimaryKeyAttribute : AttributeBase
{
}