using System.Collections.Generic;

namespace ServiceStack.OrmLite.Firebird
{
    // Firebird 5.0 dialect. FB5 (ODS 13.1) is largely a performance release and introduces NO new *reserved*
    // keywords over FB4 — only non-reserved keywords (LOCKED, OPTIMIZE, QUARTER, TARGET, TIMEZONE_NAME,
    // UNICODE_CHAR, UNICODE_VAL), which are valid identifiers and must NOT be quoted. So this dialect inherits
    // Firebird4 as-is (identity columns, BOOLEAN, TIMESTAMP + LOCALTIMESTAMP, the type converters, and the FB4
    // reserved-word set). It exists as the canonical FB5 dialect and a forward extension point for FB5-specific
    // features (e.g. any future SQL additions serviced into FB5).
    public class Firebird5OrmLiteDialectProvider : Firebird4OrmLiteDialectProvider
    {
        public new static Firebird5OrmLiteDialectProvider Instance = new();

        public Firebird5OrmLiteDialectProvider() : this(true) { }

        public Firebird5OrmLiteDialectProvider(bool compactGuid) : base(compactGuid)
        {
            // FB5 adds no new reserved words over FB4 -> reuse the inherited RESERVED list unchanged.
        }
    }
}
