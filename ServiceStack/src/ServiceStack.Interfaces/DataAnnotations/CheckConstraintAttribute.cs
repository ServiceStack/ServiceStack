using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Create an RDBMS Check Constraint on a Table column
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CheckConstraintAttribute : AttributeBase
{
    public string Constraint { get; }

    public CheckConstraintAttribute(string constraint)
    {
        this.Constraint = constraint;
    }
}