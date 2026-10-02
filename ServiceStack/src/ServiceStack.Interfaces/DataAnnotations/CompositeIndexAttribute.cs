using System;
using System.Collections.Generic;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Create an Composite RDBMS Index and optional Unique constraint
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class CompositeIndexAttribute : AttributeBase
{
    public CompositeIndexAttribute()
    {
        this.FieldNames = new List<string>();
    }

    public CompositeIndexAttribute(params string[] fieldNames)
    {
        this.FieldNames = new List<string>(fieldNames);
    }

    public CompositeIndexAttribute(bool unique, params string[] fieldNames)
    {
        this.Unique = unique;
        this.FieldNames = [..fieldNames];
    }

    public List<string> FieldNames { get; set; }

    public bool Unique { get; set; }

    public string Name { get; set; }

    /// <summary>
    /// Only index the rows matching this SQL condition (a filtered or partial index), e.g:
    /// <para>[CompositeIndex(nameof(TenantId), nameof(Email), Unique = true, Where = "{DeletedDate} IS NULL")]</para>
    /// {Property} names are replaced with their quoted column name.
    /// </summary>
    public string Where { get; set; }

    /// <summary>
    /// Other properties whose columns are kept in the index (a covering index), so queries that only use them
    /// and the indexed columns are answered from the index
    /// </summary>
    public string[] Include { get; set; }
}