using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Define a unique RDBMS column constraint
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class UniqueAttribute : AttributeBase {}