using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Compute attribute.
/// Use to indicate that a property is a Calculated Field.
/// Use [Persisted] attribute to persist column
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ComputeAttribute : AttributeBase
{
    public string Expression { get; set; }

    public ComputeAttribute() : this(string.Empty) { }

    public ComputeAttribute(string expression)
    {
        Expression = expression;
    }
}
    
/// <summary>
/// Ignore calculated C# Property from being persisted in RDBMS Table
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ComputedAttribute : AttributeBase {}
    
/// <summary>
/// Whether to persist calculated column
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PersistedAttribute : AttributeBase {}