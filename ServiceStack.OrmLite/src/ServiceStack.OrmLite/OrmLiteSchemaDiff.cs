using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

public enum SchemaChangeType
{
    /// <summary>
    /// The table of the model isn't in the database
    /// </summary>
    CreateTable,
    /// <summary>
    /// The column of a property isn't in the table
    /// </summary>
    AddColumn,
    /// <summary>
    /// The column isn't the type or size of its property, or allows nulls when its property doesn't
    /// </summary>
    AlterColumn,
    /// <summary>
    /// The column of the table isn't a property of the model
    /// </summary>
    DropColumn,
    /// <summary>
    /// The index of the model isn't in the database
    /// </summary>
    CreateIndex,
    /// <summary>
    /// The index of the model is in the database with other columns or uniqueness, so it's dropped and created again
    /// </summary>
    AlterIndex,
    /// <summary>
    /// The index of the table isn't in the model, e.g. it was removed from the model or added to the database by
    /// hand. It's destructive, so it's only dropped with allowDestructive.
    /// </summary>
    DropIndex,
    /// <summary>
    /// The default value of the column isn't its property's, or the column has a default that its property doesn't.
    /// It only changes the values of rows that are inserted without one.
    /// </summary>
    AlterDefault,
    /// <summary>
    /// The foreign key of a property isn't in the table. It's destructive unless its column is added in the same
    /// diff, as the rows in the table can reference rows that don't exist.
    /// </summary>
    AddForeignKey,
    /// <summary>
    /// The foreign key of a property references another table or has other ON DELETE or ON UPDATE actions, so it's
    /// dropped and added again. It's destructive when it references another table.
    /// </summary>
    AlterForeignKey,
    /// <summary>
    /// The foreign key of the table isn't in the model, e.g. it was removed from the model or added to the database by
    /// hand. It's destructive, so it's only dropped with allowDestructive.
    /// </summary>
    DropForeignKey,
}

/// <summary>
/// An index of a table in the database
/// </summary>
public class IndexSchema
{
    public string Name { get; set; }

    public bool IsUnique { get; set; }

    /// <summary>
    /// The key columns of the index in order, which don't include the columns of INCLUDE
    /// </summary>
    public List<string> Columns { get; set; } = [];

    /// <summary>
    /// Whether the index is created by the database for a constraint, e.g. a primary key or a unique constraint,
    /// which isn't compared with the indexes of a model
    /// </summary>
    public bool IsConstraint { get; set; }

    public override string ToString() => (IsUnique ? "UNIQUE " : "") + "(" + string.Join(", ", Columns) + ")";
}

/// <summary>
/// A foreign key of a table in the database
/// </summary>
public class ForeignKeySchema
{
    /// <summary>
    /// The name of its constraint, null in SQLite, which doesn't keep the names of foreign keys
    /// </summary>
    public string Name { get; set; }

    public List<string> Columns { get; set; } = [];

    /// <summary>
    /// The name of the table it references, without its schema
    /// </summary>
    public string RefTable { get; set; }

    /// <summary>
    /// The columns of the table it references, empty when the database doesn't say, e.g. SQLite's foreign keys
    /// that reference a primary key without naming its column
    /// </summary>
    public List<string> RefColumns { get; set; } = [];

    /// <summary>
    /// e.g. CASCADE, SET NULL or NO ACTION
    /// </summary>
    public string OnDelete { get; set; }

    public string OnUpdate { get; set; }

    public override string ToString() =>
        $"({string.Join(", ", Columns)}) REFERENCES {RefTable}" + (RefColumns.Count > 0 ? $" ({string.Join(", ", RefColumns)})" : "")
        + Action("DELETE", OnDelete) + Action("UPDATE", OnUpdate);

    private static string Action(string on, string action)
    {
        action = OrmLiteSchemaDiffApi.NormalizeFkAction(action);
        return action == "NO ACTION" ? "" : $" ON {on} {action}";
    }
}

/// <summary>
/// A change to the database that makes it the same as a model
/// </summary>
public class SchemaChange
{
    public SchemaChangeType Type { get; set; }

    public Type ModelType { get; set; }

    /// <summary>
    /// The name of the table in the database
    /// </summary>
    public string Table { get; set; }

    /// <summary>
    /// The name of the column, index or foreign key in the database, null for a table
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The property of the column, null for columns that aren't in the model
    /// </summary>
    public FieldDefinition Field { get; set; }

    /// <summary>
    /// The column the model would be created with, e.g. VARCHAR(200) NULL, the columns of its index, e.g.
    /// UNIQUE (Email, TenantId), its default value or its foreign key
    /// </summary>
    public string ModelColumn { get; set; }

    /// <summary>
    /// The column that's in the database, e.g. VARCHAR(50) NOT NULL, the columns of its index, its default value or
    /// its foreign key
    /// </summary>
    public string DatabaseColumn { get; set; }

    /// <summary>
    /// The column that's likely the same column renamed: of a column that isn't in the database, the column that isn't
    /// in its model with the same type, and of a column that isn't in the model, the column that isn't in the database.
    /// It's only likely when there's one column of the type that's in each, so the columns aren't renamed by
    /// ApplySchemaDiff(), and the migrations of ToMigration() say how to rename them instead.
    /// </summary>
    public string LikelyRename { get; set; }

    /// <summary>
    /// The SQL that makes the change, null if the database can't make it, e.g. SQLite can't alter a column
    /// </summary>
    public string Sql { get; set; }

    /// <summary>
    /// Whether the change can lose data or fail with the rows that are in the table, e.g. dropping a column,
    /// making it smaller or not allowing nulls
    /// </summary>
    public bool IsDestructive { get; set; }

    public string Description => Type switch {
        SchemaChangeType.CreateTable => $"Table {Table} isn't in the database",
        SchemaChangeType.AddColumn => $"Column {Table}.{Name} isn't in the database: {ModelColumn}"
            + (LikelyRename != null ? $" (renamed from {LikelyRename}?)" : ""),
        SchemaChangeType.AlterColumn => $"Column {Table}.{Name} is {DatabaseColumn} in the database, {ModelColumn} in {ModelType.Name}"
            + (Sql == null ? " (can't be altered in this database)" : ""),
        SchemaChangeType.DropColumn => $"Column {Table}.{Name} isn't in {ModelType.Name}: {DatabaseColumn}"
            + (LikelyRename != null ? $" (renamed to {LikelyRename}?)" : ""),
        SchemaChangeType.CreateIndex => $"Index {Name} of {Table} isn't in the database",
        SchemaChangeType.AlterIndex => $"Index {Name} of {Table} is {DatabaseColumn} in the database, {ModelColumn} in {ModelType.Name}",
        SchemaChangeType.DropIndex => $"Index {Name} of {Table} isn't in {ModelType.Name}: {DatabaseColumn}",
        SchemaChangeType.AlterDefault => $"Default of {Table}.{Name} is {DatabaseColumn} in the database, {ModelColumn} in {ModelType.Name}"
            + CantChange,
        SchemaChangeType.AddForeignKey => $"Foreign key {Name} of {Table} isn't in the database: {ModelColumn}" + CantChange,
        SchemaChangeType.AlterForeignKey => $"Foreign key {Name} of {Table} is {DatabaseColumn} in the database, {ModelColumn} in {ModelType.Name}"
            + CantChange,
        SchemaChangeType.DropForeignKey => $"Foreign key {Name} of {Table} isn't in {ModelType.Name}: {DatabaseColumn}" + CantChange,
        _ => Type.ToString(),
    };

    private string CantChange => Sql == null ? " (can't be changed in this database)" : "";

    public override string ToString() => Description;
}

/// <summary>
/// The tables that GetSchemaDiff() doesn't compare, e.g. tables that aren't managed by OrmLite
/// </summary>
public class SchemaDiffOptions
{
    /// <summary>
    /// Tables that aren't compared, by their name in the database after [Alias], ignoring case. A * matches any
    /// characters, e.g. Legacy*. Ignores the tables of ASP.NET Core Identity and EF Core's migrations by default.
    /// </summary>
    public List<string> IgnoreTables { get; set; } = ["AspNet*", "__EFMigrationsHistory"];

    /// <summary>
    /// Models whose tables aren't compared
    /// </summary>
    public List<Type> IgnoreTypes { get; set; } = [];

    /// <summary>
    /// Whether the table of a model isn't compared
    /// </summary>
    public bool IsIgnored(Type modelType, IOrmLiteDialectProvider dialect)
    {
        if (IgnoreTypes.Contains(modelType))
            return true;
        var modelDef = modelType.GetModelDefinition();
        // The name of the model's table, and the name in the database after the dialect's naming strategy
        return IsIgnoredTable(modelDef.ModelName)
            || IsIgnoredTable(dialect.UnquotedTable(new TableRef(modelDef)));
    }

    /// <summary>
    /// Whether a table isn't compared, by its name
    /// </summary>
    public bool IsIgnoredTable(string table)
    {
        foreach (var pattern in IgnoreTables)
        {
            if (Matches(table, pattern))
                return true;
        }
        return false;
    }

    private static bool Matches(string table, string pattern)
    {
        if (pattern.IndexOf('*') == -1)
            return string.Equals(table, pattern, StringComparison.OrdinalIgnoreCase);
        var parts = pattern.Split('*');
        if (!table.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase)
            || !table.EndsWith(parts[parts.Length - 1], StringComparison.OrdinalIgnoreCase))
            return false;
        // Each part between *s is after the part before it
        var pos = parts[0].Length;
        var end = table.Length - parts[parts.Length - 1].Length;
        for (var i = 1; i < parts.Length - 1; i++)
        {
            var index = table.IndexOf(parts[i], pos, StringComparison.OrdinalIgnoreCase);
            if (index == -1 || index + parts[i].Length > end)
                return false;
            pos = index + parts[i].Length;
        }
        return pos <= end;
    }
}

/// <summary>
/// The differences between models and their tables in a database
/// </summary>
public class SchemaDiff
{
    /// <summary>
    /// The changes that make the database the same as the models, in the order they're applied
    /// </summary>
    public List<SchemaChange> Changes { get; set; } = [];

    /// <summary>
    /// What couldn't be compared, e.g. when temporary tables can't be created to compare column types
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// The tables of models that weren't compared, as set by SchemaDiffOptions, e.g. AspNetUsers
    /// </summary>
    public List<string> Ignored { get; set; } = [];

    public bool HasChanges => Changes.Count > 0;

    /// <summary>
    /// The differences of each table, e.g. to log:
    /// <para>Order</para>
    /// <para>  + ShippedDate  DATETIME NULL</para>
    /// </summary>
    public override string ToString()
    {
        var ignored = Ignored.Count > 0 ? $"Ignored: {string.Join(", ", Ignored)}" : null;
        if (Changes.Count == 0 && Warnings.Count == 0)
            return ignored != null ? "No schema differences\n" + ignored : "No schema differences";

        var sb = StringBuilderCache.Allocate();
        foreach (var table in Changes.GroupBy(x => x.Table))
        {
            sb.AppendLine(table.Key);
            foreach (var change in table)
            {
                sb.AppendLine(change.Type switch {
                    SchemaChangeType.CreateTable => "  + table isn't in the database",
                    SchemaChangeType.AddColumn => $"  + {change.Name}  {change.ModelColumn}"
                        + (change.LikelyRename != null ? $" (renamed from {change.LikelyRename}?)" : ""),
                    SchemaChangeType.AlterColumn => $"  ~ {change.Name}  {change.DatabaseColumn} -> {change.ModelColumn}"
                        + (change.Sql == null ? " (can't be altered in this database)" : ""),
                    SchemaChangeType.DropColumn => $"  - {change.Name}  {change.DatabaseColumn} (not in {change.ModelType.Name})"
                        + (change.LikelyRename != null ? $" (renamed to {change.LikelyRename}?)" : ""),
                    SchemaChangeType.AlterIndex => $"  ~ index {change.Name}  {change.DatabaseColumn} -> {change.ModelColumn}",
                    SchemaChangeType.DropIndex => $"  - index {change.Name}  {change.DatabaseColumn} (not in {change.ModelType.Name})",
                    SchemaChangeType.AlterDefault => $"  ~ {change.Name}  default {change.DatabaseColumn} -> {change.ModelColumn}" + CantChange(change),
                    SchemaChangeType.AddForeignKey => $"  + foreign key {change.Name}  {change.ModelColumn}" + CantChange(change),
                    SchemaChangeType.AlterForeignKey => $"  ~ foreign key {change.Name}  {change.DatabaseColumn} -> {change.ModelColumn}" + CantChange(change),
                    SchemaChangeType.DropForeignKey => $"  - foreign key {change.Name}  {change.DatabaseColumn} (not in {change.ModelType.Name})" + CantChange(change),
                    _ => $"  + index {change.Name}",
                });
            }
        }
        foreach (var warning in Warnings)
            sb.AppendLine("Warning: " + warning);
        if (ignored != null)
            sb.AppendLine(ignored);
        return StringBuilderCache.ReturnAndFree(sb).TrimEnd();
    }

    private static string CantChange(SchemaChange change) =>
        change.Sql == null ? " (can't be changed in this database)" : "";

    /// <summary>
    /// The source code of a migration that makes the changes, to review and add to your migrations. It starts with a
    /// #warning that's reported when it's compiled, until it's reviewed and the warning is deleted, e.g:
    /// <para>File.WriteAllText("Migrations/Migration1005.cs", diff.ToMigration("Migration1005", "MyApp.Migrations"))</para>
    /// Columns that aren't in a model are only dropped by code that's commented out, as they may have been renamed.
    /// </summary>
    public string ToMigration(string className, string @namespace = null) =>
        SchemaMigrationWriter.Write(this, className, @namespace);
}

public static class OrmLiteSchemaDiffApi
{
    /// <summary>
    /// The differences between a model and its table: missing tables, columns, indexes and foreign keys, the ones
    /// that aren't in the model, and columns with a different type, size, nullability or default value. E.g:
    /// <para>var diff = db.GetSchemaDiff&lt;Order&gt;();</para>
    /// </summary>
    public static SchemaDiff GetSchemaDiff<T>(this IDbConnection db) => db.GetSchemaDiff(typeof(T));

    /// <summary>
    /// The differences between models and their tables, e.g. to log when an App starts:
    /// <para>var diff = db.GetSchemaDiff(typeof(Order), typeof(Customer));</para>
    /// <para>if (diff.HasChanges) log.Warn(diff.ToString());</para>
    /// </summary>
    public static SchemaDiff GetSchemaDiff(this IDbConnection db, params Type[] modelTypes) =>
        db.GetSchemaDiff(OrmLiteConfig.SchemaDiff, modelTypes);

    /// <summary>
    /// The differences between models and their tables, ignoring the tables of options instead of
    /// OrmLiteConfig.SchemaDiff
    /// </summary>
    public static SchemaDiff GetSchemaDiff(this IDbConnection db, SchemaDiffOptions options, params Type[] modelTypes)
    {
        var diff = new SchemaDiff();
        var dialect = db.GetDialectProvider();
        var warnedIndexes = false;
        var warnedForeignKeys = false;
        foreach (var modelType in modelTypes)
        {
            var modelDef = modelType.GetModelDefinition();
            var tableRef = new TableRef(modelDef);
            var table = dialect.UnquotedTable(tableRef);
            if (options != null && options.IsIgnored(modelType, dialect))
            {
                diff.Ignored.Add(table);
                continue;
            }

            if (!dialect.DoesTableExist(db, tableRef))
            {
                var createSql = new List<string> { dialect.ToCreateTableStatement(modelType).Trim() };
                createSql.AddRange(dialect.ToCreateIndexStatements(modelType).Map(x => x.Trim()));
                diff.Changes.Add(new SchemaChange {
                    Type = SchemaChangeType.CreateTable,
                    ModelType = modelType,
                    Table = table,
                    Sql = createSql.Join("\n"),
                });
                continue;
            }

            var quotedTable = dialect.GetQuotedTableName(modelDef);
            var dbColumns = dialect.GetSchemaColumns(db, quotedTable);
            // Changes to the foreign keys of columns that are dropped are made before them
            var firstChange = diff.Changes.Count;

            // Some fields don't have a column, e.g. PostgreSQL's row version is the xmin of its rows
            var columnFields = modelDef.FieldDefinitions
                .Where(x => !x.ShouldSkipCreate() && !string.IsNullOrEmpty(dialect.GetColumnDefinition(x))).ToList();
            // Fields of a system-versioned table's period can only be created in their table
            var fieldDefs = columnFields.Where(x => !x.IsRowStart && !x.IsRowEnd).ToList();
            ColumnSchema[] modelColumns = null;
            try
            {
                modelColumns = dialect.GetModelSchemaColumns(db, fieldDefs);
                if (modelColumns.Length != fieldDefs.Count)
                    modelColumns = null;
            }
            catch (Exception e)
            {
                diff.Warnings.Add($"The types of {table}'s columns weren't compared, as a temporary table " +
                                  $"couldn't be created with them: {e.Message}");
            }
            // The defaults of the model's columns are read from the temporary table they're created in
            var dbDefaults = modelColumns != null ? dialect.GetColumnDefaults(db, quotedTable) : null;

            var matched = new HashSet<ColumnSchema>();
            var added = new List<(SchemaChange change, ColumnSchema modelColumn)>();
            var dropped = new List<(SchemaChange change, ColumnSchema dbColumn)>();
            foreach (var fieldDef in columnFields)
            {
                var index = fieldDefs.IndexOf(fieldDef);
                var modelColumn = modelColumns != null && index >= 0 ? modelColumns[index] : null;
                var name = modelColumn?.ColumnName ?? dialect.NamingStrategy.GetColumnName(fieldDef.FieldName);
                var dbColumn = dbColumns.FirstOrDefault(x => x.ColumnName.EqualsIgnoreCase(name));
                if (dbColumn == null)
                {
                    var addColumn = new SchemaChange {
                        Type = SchemaChangeType.AddColumn,
                        ModelType = modelType,
                        Table = table,
                        Name = name,
                        Field = fieldDef,
                        ModelColumn = modelColumn != null ? Describe(modelColumn) : ColumnType(dialect, fieldDef),
                        Sql = dialect.ToAddColumnStatement(tableRef, fieldDef.Clone(f => f.IsPrimaryKey = false)),
                    };
                    diff.Changes.Add(addColumn);
                    added.Add((addColumn, modelColumn));
                    continue;
                }

                matched.Add(dbColumn);
                if (modelColumn == null)
                    continue;

                if (!IsSame(dbColumn, modelColumn, dialect))
                {
                    diff.Changes.Add(new SchemaChange {
                        Type = SchemaChangeType.AlterColumn,
                        ModelType = modelType,
                        Table = table,
                        Name = dbColumn.ColumnName,
                        Field = fieldDef,
                        ModelColumn = Describe(modelColumn, dbColumn),
                        DatabaseColumn = Describe(dbColumn, modelColumn),
                        Sql = dialect.Kind == DbKind.Sqlite // can't alter the columns of a table
                            ? null
                            : dialect.ToAlterColumnStatement(tableRef, fieldDef),
                        IsDestructive = !IsSafeToAlter(dbColumn, modelColumn),
                    });
                }

                // The defaults of auto incremented columns are their sequences, e.g. PostgreSQL's nextval('seq')
                if (dbDefaults == null || fieldDef.AutoIncrement)
                    continue;
                dbDefaults.TryGetValue(dbColumn.ColumnName, out var dbDefault);
                var modelDefault = modelColumn.DefaultValue as string;
                if (IsSameDefault(dbDefault, modelDefault))
                    continue;

                diff.Changes.Add(new SchemaChange {
                    Type = SchemaChangeType.AlterDefault,
                    ModelType = modelType,
                    Table = table,
                    Name = dbColumn.ColumnName,
                    Field = fieldDef,
                    ModelColumn = DescribeDefault(modelDefault),
                    DatabaseColumn = DescribeDefault(dbDefault),
                    Sql = dialect.ToAlterColumnDefaultStatement(tableRef, fieldDef),
                });
            }

            foreach (var dbColumn in dbColumns)
            {
                if (matched.Contains(dbColumn))
                    continue;

                var dropColumn = new SchemaChange {
                    Type = SchemaChangeType.DropColumn,
                    ModelType = modelType,
                    Table = table,
                    Name = dbColumn.ColumnName,
                    DatabaseColumn = Describe(dbColumn),
                    Sql = dialect.ToDropColumnStatement(tableRef, dbColumn.ColumnName),
                    IsDestructive = true,
                };
                diff.Changes.Add(dropColumn);
                dropped.Add((dropColumn, dbColumn));
            }
            FindLikelyRenames(added, dropped, dialect);

            if (!OrmLiteConfig.SkipForeignKeys)
            {
                var dbForeignKeys = dialect.GetTableForeignKeys(db, tableRef);
                if (dbForeignKeys != null)
                    CompareForeignKeys(diff, modelType, tableRef, table, columnFields, added, dbForeignKeys, firstChange, dialect);
                else if (columnFields.Any(x => x.ForeignKey != null) && !warnedForeignKeys)
                {
                    diff.Warnings.Add($"Foreign keys aren't compared for {dialect.GetType().Name}");
                    warnedForeignKeys = true;
                }
            }

            var createIndexes = dialect.ToCreateIndexStatements(modelType);
            var dbIndexes = dialect.GetTableIndexes(db, tableRef);
            if (dbIndexes != null)
            {
                CompareIndexes(diff, modelType, tableRef, table, createIndexes, dbIndexes, dialect);
                continue;
            }
            if (createIndexes.Count == 0)
                continue;

            var indexNames = dialect.GetTableIndexNames(db, tableRef);
            if (indexNames == null)
            {
                if (!warnedIndexes)
                    diff.Warnings.Add($"Indexes aren't compared for {dialect.GetType().Name}");
                warnedIndexes = true;
                continue;
            }

            foreach (var createIndex in createIndexes)
            {
                var indexName = GetIndexName(createIndex);
                if (indexName == null || indexNames.Any(x => x.EqualsIgnoreCase(indexName)))
                    continue;

                diff.Changes.Add(new SchemaChange {
                    Type = SchemaChangeType.CreateIndex,
                    ModelType = modelType,
                    Table = table,
                    Name = indexName,
                    Sql = createIndex.Trim(),
                });
            }
        }
        return diff;
    }

    // A column that's not in the database and a column that's not in the model are likely the same column renamed
    // when they're the only ones of their type
    private static void FindLikelyRenames(List<(SchemaChange change, ColumnSchema modelColumn)> added,
        List<(SchemaChange change, ColumnSchema dbColumn)> dropped, IOrmLiteDialectProvider dialect)
    {
        foreach (var (addColumn, modelColumn) in added)
        {
            if (modelColumn == null)
                continue;
            var sameType = dropped.Where(x => IsSameColumnType(x.dbColumn, modelColumn, dialect)).ToList();
            if (sameType.Count != 1
                || added.Count(x => x.modelColumn != null && IsSameColumnType(sameType[0].dbColumn, x.modelColumn, dialect)) != 1)
                continue;
            addColumn.LikelyRename = sameType[0].change.Name;
            sameType[0].change.LikelyRename = addColumn.Name;
        }
    }

    private static bool IsSameColumnType(ColumnSchema dbColumn, ColumnSchema modelColumn, IOrmLiteDialectProvider dialect) =>
        IsSameType(dbColumn, modelColumn) || IsTextFor(dbColumn, modelColumn, dialect)
        || (IsIntegerType(dbColumn) && IsIntegerType(modelColumn));

    // The indexes of the model that aren't in the database or are with other columns, and the indexes in the database
    // that aren't in the model. Indexes of constraints, e.g. primary keys, aren't compared.
    private static void CompareIndexes(SchemaDiff diff, Type modelType, TableRef tableRef, string table,
        List<string> createIndexes, List<IndexSchema> dbIndexes, IOrmLiteDialectProvider dialect)
    {
        var modelIndexNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var createIndex in createIndexes)
        {
            var indexName = GetIndexName(createIndex);
            if (indexName == null)
                continue;
            modelIndexNames.Add(indexName);

            var dbIndex = dbIndexes.FirstOrDefault(x => x.Name.EqualsIgnoreCase(indexName));
            if (dbIndex == null)
            {
                diff.Changes.Add(new SchemaChange {
                    Type = SchemaChangeType.CreateIndex,
                    ModelType = modelType,
                    Table = table,
                    Name = indexName,
                    Sql = createIndex.Trim(),
                });
                continue;
            }

            var modelIndex = ParseIndex(createIndex);
            if (dbIndex.IsConstraint || modelIndex == null
                || (modelIndex.IsUnique == dbIndex.IsUnique
                    && modelIndex.Columns.Count == dbIndex.Columns.Count
                    && modelIndex.Columns.Zip(dbIndex.Columns, (a, b) => a.EqualsIgnoreCase(b)).All(x => x)))
                continue;

            diff.Changes.Add(new SchemaChange {
                Type = SchemaChangeType.AlterIndex,
                ModelType = modelType,
                Table = table,
                Name = dbIndex.Name,
                ModelColumn = modelIndex.ToString(),
                DatabaseColumn = dbIndex.ToString(),
                Sql = ToDropIndexStatement(dialect, modelType, dbIndex.Name).Trim().TrimEnd(';') + ";\n" + createIndex.Trim(),
                // Rows can have the same values of the columns of an index that's made unique
                IsDestructive = modelIndex.IsUnique && !dbIndex.IsUnique,
            });
        }

        foreach (var dbIndex in dbIndexes)
        {
            if (dbIndex.IsConstraint || modelIndexNames.Contains(dbIndex.Name))
                continue;
            diff.Changes.Add(new SchemaChange {
                Type = SchemaChangeType.DropIndex,
                ModelType = modelType,
                Table = table,
                Name = dbIndex.Name,
                DatabaseColumn = dbIndex.ToString(),
                Sql = ToDropIndexStatement(dialect, modelType, dbIndex.Name),
                IsDestructive = true,
            });
        }
    }

    // The foreign keys of the model that aren't in the database or are different, and the foreign keys in the database
    // that aren't in the model. Foreign keys are compared by their column, as OrmLite creates one for each property.
    private static void CompareForeignKeys(SchemaDiff diff, Type modelType, TableRef tableRef, string table,
        List<FieldDefinition> columnFields, List<(SchemaChange change, ColumnSchema modelColumn)> added,
        List<ForeignKeySchema> dbForeignKeys, int firstChange, IOrmLiteDialectProvider dialect)
    {
        var modelDef = modelType.GetModelDefinition();
        var matched = new HashSet<ForeignKeySchema>();
        foreach (var fieldDef in columnFields)
        {
            if (fieldDef.ForeignKey == null)
                continue;

            var refModelDef = fieldDef.ForeignKey.ReferenceType.GetModelDefinition();
            var refTableRef = new TableRef(refModelDef);
            var column = dialect.NamingStrategy.GetColumnName(fieldDef.FieldName);
            var modelForeignKey = new ForeignKeySchema {
                Name = fieldDef.ForeignKey.GetForeignKeyName(modelDef, refModelDef, dialect.NamingStrategy, fieldDef),
                Columns = [column],
                RefTable = dialect.GetTableNameOnly(refTableRef),
                RefColumns = [dialect.NamingStrategy.GetColumnName(refModelDef.PrimaryKey.FieldName)],
                OnDelete = fieldDef.ForeignKey.OnDelete,
                OnUpdate = fieldDef.ForeignKey.OnUpdate,
            };

            var dbForeignKey = dbForeignKeys.FirstOrDefault(x => !matched.Contains(x)
                && x.Columns.Count == 1 && x.Columns[0].EqualsIgnoreCase(column));
            if (dbForeignKey == null)
            {
                // A column that's added doesn't have values that reference rows that don't exist
                var isAdded = added.Any(x => x.change.Field == fieldDef);
                diff.Changes.Add(new SchemaChange {
                    Type = SchemaChangeType.AddForeignKey,
                    ModelType = modelType,
                    Table = table,
                    Name = modelForeignKey.Name,
                    Field = fieldDef,
                    ModelColumn = modelForeignKey.ToString(),
                    Sql = dialect.ToAddForeignKeyStatement(tableRef, fieldDef),
                    IsDestructive = !isAdded,
                });
                continue;
            }
            matched.Add(dbForeignKey);

            var isSameTable = IsSameTable(dbForeignKey.RefTable, refTableRef, dialect)
                && (dbForeignKey.RefColumns.Count == 0
                    || (dbForeignKey.RefColumns.Count == 1 && dbForeignKey.RefColumns[0].EqualsIgnoreCase(modelForeignKey.RefColumns[0])));
            if (isSameTable
                && NormalizeFkAction(dbForeignKey.OnDelete) == NormalizeFkAction(modelForeignKey.OnDelete)
                && NormalizeFkAction(dbForeignKey.OnUpdate) == NormalizeFkAction(modelForeignKey.OnUpdate))
                continue;

            var addSql = dialect.ToAddForeignKeyStatement(tableRef, fieldDef);
            diff.Changes.Add(new SchemaChange {
                Type = SchemaChangeType.AlterForeignKey,
                ModelType = modelType,
                Table = table,
                Name = dbForeignKey.Name ?? modelForeignKey.Name,
                Field = fieldDef,
                ModelColumn = modelForeignKey.ToString(),
                DatabaseColumn = dbForeignKey.ToString(),
                Sql = addSql != null && dbForeignKey.Name != null
                    ? dialect.ToDropForeignKeyStatement(tableRef, dbForeignKey.Name).Trim().TrimEnd(';') + ";\n" + addSql.Trim()
                    : null,
                // The rows in the table can reference rows that don't exist in the other table
                IsDestructive = !isSameTable,
            });
        }

        // Dropped before the columns they're of are dropped, which some databases don't drop with them
        var drops = new List<SchemaChange>();
        foreach (var dbForeignKey in dbForeignKeys)
        {
            if (matched.Contains(dbForeignKey))
                continue;
            drops.Add(new SchemaChange {
                Type = SchemaChangeType.DropForeignKey,
                ModelType = modelType,
                Table = table,
                Name = dbForeignKey.Name,
                DatabaseColumn = dbForeignKey.ToString(),
                Sql = dbForeignKey.Name != null && dialect.Kind != DbKind.Sqlite
                    ? dialect.ToDropForeignKeyStatement(tableRef, dbForeignKey.Name)
                    : null,
                IsDestructive = true,
            });
        }
        diff.Changes.InsertRange(firstChange, drops);
    }

    private static bool IsSameTable(string dbTable, TableRef tableRef, IOrmLiteDialectProvider dialect) =>
        dbTable != null && (dbTable.EqualsIgnoreCase(dialect.GetTableNameOnly(tableRef))
            || dbTable.EqualsIgnoreCase(dialect.UnquotedTable(tableRef).LastRightPart('.')));

    /// <summary>
    /// The action of a foreign key as it's compared, where RESTRICT, NO ACTION and none are the same
    /// </summary>
    internal static string NormalizeFkAction(string action)
    {
        action = (action ?? "").Trim().Replace('_', ' ').ToUpperInvariant();
        return action is "" or "RESTRICT" or "NO ACTION" ? "NO ACTION" : action;
    }

    // Databases write the defaults of the model's columns the same way as the table's, which are only wrapped in
    // parentheses more or less by some, e.g. SQL Server's ((0))
    private static bool IsSameDefault(string dbDefault, string modelDefault) =>
        NormalizeDefault(dbDefault) == NormalizeDefault(modelDefault);

    private static string NormalizeDefault(string value)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value) || value.EqualsIgnoreCase("NULL"))
            return null;
        while (value.Length >= 2 && value[0] == '(' && value[value.Length - 1] == ')' && IsWrapped(value))
            value = value.Substring(1, value.Length - 2).Trim();
        return value;
    }

    // Whether the parenthesis at the start of a value is closed at its end, e.g. (0) but not (a) + (b)
    private static bool IsWrapped(string value)
    {
        var depth = 0;
        var inQuotes = false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '\'')
                inQuotes = !inQuotes;
            else if (inQuotes)
                continue;
            else if (c == '(')
                depth++;
            else if (c == ')' && --depth == 0)
                return i == value.Length - 1;
        }
        return false;
    }

    private static string DescribeDefault(string value) => NormalizeDefault(value) != null ? $"DEFAULT {value.Trim()}" : "no default";

    private static readonly System.Reflection.MethodInfo ToDropIndexStatementMethod =
        typeof(IOrmLiteDialectProvider).GetMethod(nameof(IOrmLiteDialectProvider.ToDropIndexStatement));

    private static string ToDropIndexStatement(IOrmLiteDialectProvider dialect, Type modelType, string indexName) =>
        (string)ToDropIndexStatementMethod.MakeGenericMethod(modelType).Invoke(dialect, [indexName]);

    private static readonly Regex UniqueIndexRegex = new(@"^\s*CREATE\s+UNIQUE\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex IndexColumnsRegex = new(@"\bON\s+(?:""[^""]*""|`[^`]*`|\[[^\]]*\]|[^\s(])+(?:\s+USING\s+\w+)?\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// The key columns and uniqueness of an index from the statement that creates it, or null if they can't be read
    /// </summary>
    internal static IndexSchema ParseIndex(string createIndexSql)
    {
        var match = IndexColumnsRegex.Match(createIndexSql);
        if (!match.Success)
            return null;

        var to = new IndexSchema {
            Name = GetIndexName(createIndexSql),
            IsUnique = UniqueIndexRegex.IsMatch(createIndexSql),
        };
        var depth = 0;
        var start = match.Index + match.Length;
        for (var i = start; i < createIndexSql.Length; i++)
        {
            var c = createIndexSql[i];
            if (c == '(')
            {
                depth++;
            }
            else if (c == ')' && depth > 0)
            {
                depth--;
            }
            else if ((c == ',' || c == ')') && depth == 0)
            {
                to.Columns.Add(IndexColumnName(createIndexSql.Substring(start, i - start)));
                if (c == ')')
                    return to;
                start = i + 1;
            }
        }
        return null;
    }

    // The column of a key column of an index, without its quotes, ASC or DESC, or operator class
    private static string IndexColumnName(string keyColumn)
    {
        keyColumn = keyColumn.Trim();
        if (keyColumn.Length > 0 && keyColumn[0] is '"' or '`' or '[')
        {
            var close = keyColumn[0] == '[' ? ']' : keyColumn[0];
            var end = keyColumn.IndexOf(close, 1);
            return end > 0 ? keyColumn.Substring(1, end - 1) : keyColumn;
        }
        var space = keyColumn.IndexOfAny([' ', '\t', '\n']);
        return space > 0 ? keyColumn.Substring(0, space) : keyColumn;
    }

    /// <summary>
    /// Make the changes of a schema diff to the database and return the changes that were made. Changes that can
    /// lose data or fail with the rows of a table are only made with allowDestructive: dropping columns that
    /// aren't in a model, and altering a column unless it's made larger or to allow nulls.
    /// </summary>
    public static List<SchemaChange> ApplySchemaDiff(this IDbConnection db, SchemaDiff diff, bool allowDestructive = false)
    {
        var applied = new List<SchemaChange>();
        foreach (var change in diff.Changes)
        {
            if (change.IsDestructive && !allowDestructive)
                continue;

            if (change.Type == SchemaChangeType.CreateTable)
            {
                db.CreateTable(overwrite: false, change.ModelType);
            }
            else
            {
                if (change.Sql == null)
                    continue;
                db.ExecuteSql(change.Sql);
            }
            applied.Add(change);
        }
        return applied;
    }

    private static readonly Regex IndexNameRegex = new(
        @"\bINDEX\s+(?:CONCURRENTLY\s+)?(?:IF\s+NOT\s+EXISTS\s+)?([^\s(]+)\s+ON\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    internal static string GetIndexName(string createIndexSql)
    {
        var match = IndexNameRegex.Match(createIndexSql);
        return match.Success
            ? match.Groups[1].Value.Trim('"', '`', '[', ']')
            : null;
    }

    private static bool IsSame(ColumnSchema dbColumn, ColumnSchema modelColumn, IOrmLiteDialectProvider dialect) =>
        (IsSameType(dbColumn, modelColumn) || IsTextFor(dbColumn, modelColumn, dialect)
            || (IsIntegerType(dbColumn) && IsIntegerType(modelColumn)))
        && dbColumn.AllowDBNull == modelColumn.AllowDBNull;

    private static readonly HashSet<string> IntegerTypes = new(StringComparer.OrdinalIgnoreCase) {
        "SMALLINT", "INT", "INTEGER", "MEDIUMINT", "BIGINT", "INT2", "INT4", "INT8",
        "SMALLSERIAL", "SERIAL", "BIGSERIAL",
    };

    // An int property is often used with a BIGINT column and a long with an INTEGER, which aren't a difference
    private static bool IsIntegerType(ColumnSchema column)
    {
        var type = column.DataTypeName?.Trim() ?? "";
        if (type.EndsWith(" UNSIGNED", StringComparison.OrdinalIgnoreCase))
            type = type.Substring(0, type.Length - " UNSIGNED".Length);
        return IntegerTypes.Contains(type);
    }

    private static bool IsCharType(ColumnSchema column)
    {
        var type = column.DataTypeName?.ToUpper() ?? "";
        return type.Contains("CHAR") || type.Contains("TEXT") || type.Contains("CLOB");
    }

    // TEXT, and character types without a length, e.g. VARCHAR(MAX)
    private static bool IsText(ColumnSchema column)
    {
        var type = column.DataTypeName?.ToUpper() ?? "";
        return type.Contains("TEXT") || type.Contains("CLOB")
            || (type.Contains("CHAR") && (column.ColumnSize <= 0 || column.ColumnSize >= MaxSize));
    }

    // A text column holds the values of a property with any length, so it isn't a difference when the model
    // would create it as a VARCHAR(n), e.g. in tables that weren't created by OrmLite.
    // SQLite doesn't use the length of a column, all its character types are text.
    private static bool IsTextFor(ColumnSchema dbColumn, ColumnSchema modelColumn, IOrmLiteDialectProvider dialect) =>
        IsCharType(modelColumn)
        && (IsText(dbColumn) || (dialect.Kind == DbKind.Sqlite && IsCharType(dbColumn)));

    private static bool IsSameType(ColumnSchema a, ColumnSchema b) =>
        string.Equals(a.DataTypeName, b.DataTypeName, StringComparison.OrdinalIgnoreCase)
        && a.ColumnSize == b.ColumnSize
        && a.NumericPrecision == b.NumericPrecision
        && a.NumericScale == b.NumericScale;

    // A column can be made larger and to allow nulls without changing its rows
    private static bool IsSafeToAlter(ColumnSchema dbColumn, ColumnSchema modelColumn)
    {
        if (dbColumn.AllowDBNull && !modelColumn.AllowDBNull)
            return false;
        if (!string.Equals(dbColumn.DataTypeName, modelColumn.DataTypeName, StringComparison.OrdinalIgnoreCase)
            || dbColumn.NumericPrecision != modelColumn.NumericPrecision
            || dbColumn.NumericScale != modelColumn.NumericScale)
            return false;
        return dbColumn.ColumnSize == modelColumn.ColumnSize
            || (dbColumn.ColumnSize > 0 && modelColumn.ColumnSize > dbColumn.ColumnSize);
    }

    private const int MaxSize = 1_000_000_000;

    // e.g. VARCHAR(50) NOT NULL, with the sizes of a column it's compared with when they'd otherwise be the same
    private static string Describe(ColumnSchema column, ColumnSchema other = null)
    {
        var type = (column.DataTypeName ?? "?").ToUpper();
        if (type.IndexOf('(') == -1)
        {
            // SQLite only has the types that its columns are declared with
            var isDeclared = column.DataType == typeof(object);
            if ((column.DataType == typeof(decimal) || isDeclared) && column.NumericPrecision > 0)
                type += $"({column.NumericPrecision},{column.NumericScale})";
            else if ((column.DataType == typeof(string) || column.DataType == typeof(byte[]) || isDeclared) && column.ColumnSize > 0)
                type += column.ColumnSize >= MaxSize ? "(MAX)" : $"({column.ColumnSize})";
            else if (other != null && !IsSameType(column, other)
                     && string.Equals(column.DataTypeName, other.DataTypeName, StringComparison.OrdinalIgnoreCase))
                type += $"(size {column.ColumnSize}, precision {column.NumericPrecision}, scale {column.NumericScale})";
        }
        return type + (column.AllowDBNull ? " NULL" : " NOT NULL");
    }

    // The type of the column's definition, without its name
    private static string ColumnType(IOrmLiteDialectProvider dialect, FieldDefinition fieldDef)
    {
        var definition = dialect.GetColumnDefinition(fieldDef.Clone(f => f.IsPrimaryKey = false)).Trim();
        var quotedName = dialect.GetQuotedColumnName(fieldDef);
        return definition.StartsWith(quotedName)
            ? definition.Substring(quotedName.Length).Trim()
            : definition;
    }
}

/// <summary>
/// Writes the source code of a migration for the changes of a schema diff
/// </summary>
internal static class SchemaMigrationWriter
{
    // Generated migrations are a guide, so they're compiled with a warning until they're reviewed
    internal static readonly string[] Header = [
        "// GENERATED BY SCHEMA DIFF: A GUIDE TO REVIEW, NOT A FINISHED MIGRATION",
        "//",
        "// Schema Diff is new, and it's too early to know how accurate the migrations it writes are. Don't trust this",
        "// class to do the right thing: check every change does what you intend, e.g. a column that's not in a model is",
        "// only dropped by commented code as it may have been renamed, and run it against a copy of your data first.",
        "// Delete the #warning below once you've reviewed it.",
        "#warning Generated by Schema Diff: review this migration before running it, then delete this line",
    ];

    internal static string Write(SchemaDiff diff, string className, string ns)
    {
        if (string.IsNullOrEmpty(className))
            throw new ArgumentNullException(nameof(className));

        var sb = new StringBuilder();
        foreach (var line in Header)
            sb.AppendLine(line);
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using ServiceStack;");
        sb.AppendLine("using ServiceStack.DataAnnotations;");
        sb.AppendLine("using ServiceStack.OrmLite;");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(ns))
        {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }
        sb.AppendLine($"public class {className} : MigrationBase");
        sb.AppendLine("{");

        var up = new List<string>();
        var down = new List<string>();
        foreach (var model in diff.Changes.GroupBy(x => x.ModelType))
        {
            var modelType = model.Key;
            var modelDef = modelType.GetModelDefinition();
            var name = modelType.Name;
            var createsTable = model.Any(x => x.Type == SchemaChangeType.CreateTable);

            // The table as it is after the migration, with the properties that it changes
            foreach (var attr in CustomAttributeData.GetCustomAttributes(modelType))
            {
                if (IsSchemaAttribute(attr) && (createsTable || attr.AttributeType == typeof(AliasAttribute)
                                                              || attr.AttributeType == typeof(SchemaAttribute)))
                    sb.AppendLine("    " + ToSource(attr));
            }
            sb.AppendLine($"    public class {name}");
            sb.AppendLine("    {");
            // Its primary key is included so the properties that are changed aren't used as one
            var fieldDefs = createsTable
                ? modelDef.FieldDefinitions
                : modelDef.FieldDefinitions.Where(f => f.IsPrimaryKey || model.Any(x => x.Field == f && x.Sql != null)).ToList();
            var first = true;
            foreach (var fieldDef in fieldDefs)
            {
                if (fieldDef.PropertyInfo == null)
                    continue;
                if (!first)
                    sb.AppendLine();
                first = false;
                foreach (var attr in CustomAttributeData.GetCustomAttributes(fieldDef.PropertyInfo))
                {
                    if (IsSchemaAttribute(attr))
                        sb.AppendLine("        " + ToSource(attr));
                }
                sb.AppendLine($"        public {ToSource(fieldDef.PropertyInfo.PropertyType)} {fieldDef.PropertyInfo.Name} {{ get; set; }}");
            }
            sb.AppendLine("    }");
            sb.AppendLine();

            var undo = new List<string>();
            foreach (var change in model)
            {
                var property = change.Field?.PropertyInfo?.Name;
                switch (change.Type)
                {
                    case SchemaChangeType.CreateTable:
                        up.Add($"Db.CreateTable<{name}>();");
                        undo.Add($"Db.DropTable<{name}>();");
                        break;
                    case SchemaChangeType.AddColumn:
                        if (change.LikelyRename != null)
                        {
                            up.Add($"// {change.Name} is likely {change.LikelyRename} renamed. If it is, rename it instead of adding it:");
                            up.Add($"// Db.RenameColumn<{name}>(x => x.{property}, {ToLiteral(change.LikelyRename)});");
                        }
                        up.Add($"Db.AddColumn<{name}>(x => x.{property});");
                        undo.Add($"Db.DropColumn<{name}>(x => x.{property});");
                        break;
                    case SchemaChangeType.AlterColumn:
                        if (change.Sql == null)
                        {
                            up.Add($"// {change.Name} is {change.DatabaseColumn}, which can't be altered to {change.ModelColumn} in this database");
                            break;
                        }
                        up.Add($"// {change.Name} is {change.DatabaseColumn}");
                        up.Add($"Db.AlterColumn<{name}>(x => x.{property});");
                        undo.Add($"// {change.Name} was {change.DatabaseColumn}");
                        break;
                    case SchemaChangeType.DropColumn:
                        up.Add($"// {change.Name} ({change.DatabaseColumn}) isn't in {name}. If it was renamed, rename it instead:");
                        up.Add($"// Db.RenameColumn<{name}>({ToLiteral(change.Name)}, {ToLiteral(change.LikelyRename ?? "NewName")});");
                        up.Add($"// Db.DropColumn<{name}>({ToLiteral(change.Name)});");
                        break;
                    case SchemaChangeType.CreateIndex:
                        up.Add($"Db.ExecuteSql({ToLiteral(change.Sql)});");
                        undo.Add($"Db.DropIndex<{name}>({ToLiteral(change.Name)});");
                        break;
                    case SchemaChangeType.AlterIndex:
                        up.Add($"// {change.Name} is {change.DatabaseColumn}");
                        up.Add($"Db.DropIndex<{name}>({ToLiteral(change.Name)});");
                        up.Add($"Db.ExecuteSql({ToLiteral(change.Sql.Substring(change.Sql.IndexOf('\n') + 1))});");
                        undo.Add($"// {change.Name} was {change.DatabaseColumn}");
                        break;
                    case SchemaChangeType.DropIndex:
                        up.Add($"// Index {change.Name} {change.DatabaseColumn} isn't in {name}. If it's no longer used, drop it:");
                        up.Add($"// Db.DropIndex<{name}>({ToLiteral(change.Name)});");
                        break;
                    case SchemaChangeType.AlterDefault:
                        if (change.Sql == null)
                        {
                            up.Add($"// The default of {change.Name} is {change.DatabaseColumn}, which can't be changed to {change.ModelColumn} in this database");
                            break;
                        }
                        up.Add($"// The default of {change.Name} is {change.DatabaseColumn}");
                        up.Add($"Db.ExecuteSql({ToLiteral(change.Sql)});");
                        undo.Add($"// The default of {change.Name} was {change.DatabaseColumn}");
                        break;
                    case SchemaChangeType.AddForeignKey:
                        if (change.Sql == null)
                        {
                            up.Add($"// Foreign key {change.Name} {change.ModelColumn} can't be added to an existing table in this database");
                            break;
                        }
                        up.Add($"Db.ExecuteSql({ToLiteral(change.Sql)});");
                        undo.Add($"Db.DropForeignKey<{name}>({ToLiteral(change.Name)});");
                        break;
                    case SchemaChangeType.AlterForeignKey:
                        if (change.Sql == null)
                        {
                            up.Add($"// Foreign key {change.Name} is {change.DatabaseColumn}, which can't be changed to {change.ModelColumn} in this database");
                            break;
                        }
                        up.Add($"// Foreign key {change.Name} is {change.DatabaseColumn}");
                        up.Add($"Db.DropForeignKey<{name}>({ToLiteral(change.Name)});");
                        up.Add($"Db.ExecuteSql({ToLiteral(change.Sql.Substring(change.Sql.IndexOf('\n') + 1))});");
                        undo.Add($"// Foreign key {change.Name} was {change.DatabaseColumn}");
                        break;
                    case SchemaChangeType.DropForeignKey:
                        if (change.Sql == null)
                        {
                            up.Add($"// Foreign key {change.DatabaseColumn} isn't in {name}, and can't be dropped in this database");
                            break;
                        }
                        up.Add($"// Foreign key {change.Name} {change.DatabaseColumn} isn't in {name}. If it's no longer used, drop it:");
                        up.Add($"// Db.DropForeignKey<{name}>({ToLiteral(change.Name)});");
                        break;
                }
            }
            // Changes are reverted in the reverse order they're made
            undo.Reverse();
            down.InsertRange(0, undo);
        }

        sb.AppendLine("    public override void Up()");
        sb.AppendLine("    {");
        foreach (var line in up)
            sb.AppendLine("        " + line);
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void Down()");
        sb.AppendLine("    {");
        foreach (var line in down)
            sb.AppendLine("        " + line);
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // The attributes that tables are created from
    private static bool IsSchemaAttribute(CustomAttributeData attr)
    {
        var ns = attr.AttributeType.Namespace;
        return ns == "ServiceStack.DataAnnotations" || ns == "System.ComponentModel.DataAnnotations";
    }

    private static readonly Dictionary<Type, string> Keywords = new() {
        [typeof(bool)] = "bool", [typeof(byte)] = "byte", [typeof(sbyte)] = "sbyte", [typeof(char)] = "char",
        [typeof(short)] = "short", [typeof(ushort)] = "ushort", [typeof(int)] = "int", [typeof(uint)] = "uint",
        [typeof(long)] = "long", [typeof(ulong)] = "ulong", [typeof(float)] = "float", [typeof(double)] = "double",
        [typeof(decimal)] = "decimal", [typeof(string)] = "string", [typeof(object)] = "object",
    };

    private static readonly HashSet<string> Usings = ["System", "System.Collections.Generic", "ServiceStack",
        "ServiceStack.DataAnnotations", "ServiceStack.OrmLite"];

    internal static string ToSource(Type type)
    {
        if (Keywords.TryGetValue(type, out var keyword))
            return keyword;
        if (type.IsArray)
            return ToSource(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null)
            return ToSource(nullable) + "?";

        var name = type.Name;
        if (type.IsGenericType)
        {
            name = name.LeftPart('`') + "<" + type.GetGenericArguments().Map(ToSource).Join(", ") + ">";
        }
        if (type.IsNested)
            return ToSource(type.DeclaringType) + "." + name;
        return type.Namespace == null || Usings.Contains(type.Namespace)
            ? name
            : type.Namespace + "." + name;
    }

    private static string ToSource(CustomAttributeData attr)
    {
        var name = ToSource(attr.AttributeType);
        if (name.EndsWith("Attribute"))
            name = name.Substring(0, name.Length - "Attribute".Length);

        var args = attr.ConstructorArguments.Map(ToSource);
        foreach (var arg in attr.NamedArguments)
            args.Add($"{arg.MemberName} = {ToSource(arg.TypedValue)}");

        return args.Count > 0
            ? $"[{name}({args.Join(", ")})]"
            : $"[{name}]";
    }

    private static string ToSource(CustomAttributeTypedArgument arg)
    {
        var value = arg.Value;
        if (value == null)
            return "null";
        if (value is IReadOnlyCollection<CustomAttributeTypedArgument> items)
            return $"new {ToSource(arg.ArgumentType.GetElementType())}[] {{ {items.Select(ToSource).Join(", ")} }}";
        if (value is Type type)
            return $"typeof({ToSource(type)})";
        if (arg.ArgumentType.IsEnum)
        {
            var enumName = Enum.GetName(arg.ArgumentType, value);
            return enumName != null
                ? $"{ToSource(arg.ArgumentType)}.{enumName}"
                : $"({ToSource(arg.ArgumentType)}){Convert.ToInt64(value)}";
        }
        return value switch {
            string s => ToLiteral(s),
            char c => "'" + (c == '\'' ? "\\'" : c == '\\' ? "\\\\" : c.ToString()) + "'",
            bool b => b ? "true" : "false",
            float f => f.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f",
            double d => d.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "d",
            long l => l + "L",
            ulong ul => ul + "UL",
            uint ui => ui + "U",
            IFormattable n => n.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
    }

    internal static string ToLiteral(string text)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in text)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.Append('"').ToString();
    }
}
