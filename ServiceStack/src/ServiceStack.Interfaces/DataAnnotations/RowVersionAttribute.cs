using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Treat property as an automatically incremented RDBMS Row Version 
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RowVersionAttribute : AttributeBase
{
}