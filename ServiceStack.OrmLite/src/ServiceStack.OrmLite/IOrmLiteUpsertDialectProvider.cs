using System.Collections.Generic;
using System.Data;

namespace ServiceStack.OrmLite;

/// <summary>
/// Optional dialect capability for preparing a native, primary-key based UPSERT statement.
/// Dialects which don't implement this interface use OrmLite's Save() behavior instead.
/// </summary>
public interface IOrmLiteUpsertDialectProvider
{
    bool SupportsUpsert { get; }

    void PrepareParameterizedUpsertStatement<T>(
        IDbCommand cmd,
        ICollection<string> insertFields = null,
        ICollection<string> updateOnly = null);

    /// <summary>
    /// Converts a native UPSERT statement into one that also returns the upserted row, e.g. with RETURNING or
    /// OUTPUT, with all its columns or only the returnFields, or returns null if the RDBMS doesn't support it
    /// </summary>
    string ToUpsertReturningStatement(string sql, ModelDefinition modelDef, ICollection<FieldDefinition> returnFields = null);
}
