using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

public static class OrmLiteRebuildTableApi
{
    /// <summary>
    /// SQLite: create a table again from its model and copy its rows, to change what SQLite can't alter, e.g. the type,
    /// nullability or default of a column, and its foreign keys and constraints. The rows of the columns in both are
    /// kept, columns that aren't in the model are dropped, and the model's indexes and the table's triggers are created
    /// again. It's run in a transaction of its own, or in the connection's transaction, e.g. a migration's.
    /// <para>A table that's referenced by the foreign keys of other tables can't be rebuilt in a transaction while
    /// foreign keys are enforced, as SQLite would delete or change the rows that reference it.</para>
    /// </summary>
    public static void RebuildTable<T>(this IDbConnection db) => db.RebuildTable(typeof(T));

    /// <summary>
    /// SQLite: create a table again from its model and copy its rows, see RebuildTable&lt;T&gt;()
    /// </summary>
    public static void RebuildTable(this IDbConnection db, Type modelType)
    {
        var dialect = db.GetDialectProvider();
        if (dialect.Kind != DbKind.Sqlite)
            throw new NotSupportedException($"RebuildTable() is for SQLite, which can't alter columns or constraints. " +
                $"{dialect.GetType().Name} alters them with AlterColumn() and ALTER TABLE statements.");

        var table = dialect.UnquotedTable(new TableRef(modelType.GetModelDefinition()));
        // With foreign keys enforced, dropping the table deletes or changes the rows of the tables that reference it,
        // and renaming a table changes their references, which can only be stopped outside a transaction
        var referencedBy = db.Scalar<long>("PRAGMA foreign_keys") == 1
            ? db.Column<string>(("SELECT DISTINCT m.name FROM sqlite_master m JOIN pragma_foreign_key_list(m.name) f " +
                "WHERE m.type = 'table' AND f.\"table\" = {0} COLLATE NOCASE AND m.name <> {0} COLLATE NOCASE").SqlFmt(dialect, table))
            : [];
        var inTransaction = db.InTransaction();
        if (referencedBy.Count > 0 && inTransaction)
            throw new InvalidOperationException($"{table} can't be rebuilt in a transaction while foreign keys are " +
                $"enforced, as it's referenced by the foreign keys of {referencedBy.Join(", ")}, whose rows SQLite " +
                "would delete or change. Rebuild it outside a transaction, which stops enforcing foreign keys while " +
                "it's rebuilt, or run PRAGMA foreign_keys=OFF before the transaction starts.");

        if (referencedBy.Count > 0)
            db.ExecuteSql("PRAGMA foreign_keys=OFF");
        try
        {
            var statements = ToRebuildTableStatements(db, modelType);
            if (inTransaction)
            {
                foreach (var sql in statements)
                    db.ExecuteSql(sql);
                return;
            }

            using var trans = db.OpenTransaction();
            foreach (var sql in statements)
                db.ExecuteSql(sql);
            if (referencedBy.Count > 0)
            {
                // The rows that reference the table, or that it references, are checked before they're committed
                var violations = db.SqlList<Dictionary<string, object>>("PRAGMA foreign_key_check")
                    .Where(x => table.EqualsIgnoreCase(x["table"]?.ToString()) || table.EqualsIgnoreCase(x["parent"]?.ToString()))
                    .ToList();
                if (violations.Count > 0)
                    throw new InvalidOperationException($"{table} wasn't rebuilt, as {violations.Count} rows would break " +
                        $"foreign keys, e.g. row {violations[0]["rowid"]} of {violations[0]["table"]} referencing {violations[0]["parent"]}");
            }
            trans.Commit();
        }
        finally
        {
            if (referencedBy.Count > 0)
                db.ExecuteSql("PRAGMA foreign_keys=ON");
        }
    }

    /// <summary>
    /// The statements that rebuild a SQLite table from its model: create it with another name, copy the rows of the
    /// columns in both, keep its AUTOINCREMENT sequence, drop it, rename the new table, and create the model's indexes
    /// and the table's triggers again. See https://www.sqlite.org/lang_altertable.html#otheralter
    /// </summary>
    internal static List<string> ToRebuildTableStatements(IDbConnection db, Type modelType)
    {
        var dialect = db.GetDialectProvider();
        var modelDef = modelType.GetModelDefinition();
        var table = dialect.UnquotedTable(new TableRef(modelDef));
        var quotedTable = dialect.GetQuotedTableName(modelDef);
        var newTable = table + "_ormlite_rebuild";
        var quotedNewTable = dialect.GetQuotedName(newTable);

        var createSql = dialect.ToCreateTableStatement(modelType);
        var namePos = createSql.IndexOf(quotedTable, StringComparison.Ordinal);
        createSql = createSql.Substring(0, namePos) + quotedNewTable + createSql.Substring(namePos + quotedTable.Length);

        // The rows of the columns that are in the table and the model, other than the columns the database computes
        var dbColumns = dialect.GetSchemaColumns(db, quotedTable);
        var columns = new List<string>();
        foreach (var fieldDef in modelDef.FieldDefinitions)
        {
            if (fieldDef.ShouldSkipCreate() || fieldDef.IsComputed || string.IsNullOrEmpty(dialect.GetColumnDefinition(fieldDef)))
                continue;
            var name = dialect.NamingStrategy.GetColumnName(fieldDef.FieldName);
            var dbColumn = dbColumns.FirstOrDefault(x => x.ColumnName.EqualsIgnoreCase(name));
            if (dbColumn != null)
                columns.Add(dialect.GetQuotedName(dbColumn.ColumnName));
        }

        var triggers = db.Column<string>("SELECT sql FROM sqlite_master WHERE type = 'trigger' AND tbl_name = {0} COLLATE NOCASE"
            .SqlFmt(dialect, table));
        var legacyAlterTable = db.Scalar<long>("PRAGMA legacy_alter_table");

        var statements = new List<string> {
            createSql.Trim(),
            $"INSERT INTO {quotedNewTable} ({columns.Join(", ")}) SELECT {columns.Join(", ")} FROM {quotedTable};",
        };
        if (createSql.IndexOf("AUTOINCREMENT", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // The next id isn't reused after the rows with the highest ids were deleted. It's main's sequence, as a
            // temporary table with AUTOINCREMENT, e.g. of GetSchemaDiff(), adds a temp.sqlite_sequence
            statements.Add(("UPDATE main.sqlite_sequence SET seq = (SELECT seq FROM main.sqlite_sequence WHERE name = {0}) " +
                "WHERE name = {1} AND seq < (SELECT seq FROM main.sqlite_sequence WHERE name = {0});").SqlFmt(dialect, table, newTable));
            statements.Add(("INSERT INTO main.sqlite_sequence (name, seq) SELECT {1}, seq FROM main.sqlite_sequence WHERE name = {0} " +
                "AND NOT EXISTS (SELECT 1 FROM main.sqlite_sequence WHERE name = {1});").SqlFmt(dialect, table, newTable));
        }
        statements.Add($"DROP TABLE {quotedTable};");
        // Views that reference the table aren't checked while it doesn't exist
        statements.Add("PRAGMA legacy_alter_table=ON;");
        statements.Add($"ALTER TABLE {quotedNewTable} RENAME TO {quotedTable};");
        statements.Add($"PRAGMA legacy_alter_table={(legacyAlterTable == 1 ? "ON" : "OFF")};");
        statements.AddRange(dialect.ToCreateIndexStatements(modelType).Map(x => x.Trim()));
        statements.AddRange(triggers.Where(x => !string.IsNullOrEmpty(x)).Map(x => x.Trim().TrimEnd(';') + ";"));
        return statements;
    }
}
