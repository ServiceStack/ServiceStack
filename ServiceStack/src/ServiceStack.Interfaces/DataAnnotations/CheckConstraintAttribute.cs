using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Create an RDBMS Check Constraint on a Table column
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CheckConstraintAttribute(string constraint) : AttributeBase
{
    public string Constraint { get; } = constraint;
}