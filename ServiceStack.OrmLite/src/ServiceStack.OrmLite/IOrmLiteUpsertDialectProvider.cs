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
}
