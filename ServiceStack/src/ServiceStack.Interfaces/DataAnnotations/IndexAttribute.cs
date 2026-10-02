using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Create an RDBMS Column Index
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
public class IndexAttribute : AttributeBase
{
    public IndexAttribute() { }

    public IndexAttribute(bool unique)
    {
        Unique = unique;
    }
        
    public string Name { get; set; }

    public bool Unique { get; set; }

    public bool Clustered { get; set; }

    public bool NonClustered { get; set; }

    /// <summary>
    /// Only index the rows matching this SQL condition (a filtered or partial index), e.g:
    /// <para>[Index(Where = "{DeletedDate} IS NULL")]</para>
    /// {Property} names are replaced with their quoted column name.
    /// </summary>
    public string Where { get; set; }

    /// <summary>
    /// Other properties whose columns are kept in the index (a covering index), so queries that only use them
    /// and the indexed column are answered from the index
    /// </summary>
    public string[] Include { get; set; }
}