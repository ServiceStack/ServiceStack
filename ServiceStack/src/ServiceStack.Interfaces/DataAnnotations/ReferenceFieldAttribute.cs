using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Populate with a field from a foreign table in Load* APIs
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ReferenceFieldAttribute : AttributeBase
{
    /// <summary>
    /// Foreign Key Table name
    /// </summary>
    public Type Model { get; set; }
    
    /// <summary>
    /// The Field name on current Model to use for the Foreign Key Table Lookup 
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Specify Field to reference (if different from property name)
    /// </summary>
    public string Field { get; set; }

    public ReferenceFieldAttribute() {}
    public ReferenceFieldAttribute(Type model, string id)
    {
        Model = model;
        Id = id;
    }
    
    public ReferenceFieldAttribute(Type model, string id, string field)
    {
        Model = model;
        Id = id;
        Field = field;
    }
}