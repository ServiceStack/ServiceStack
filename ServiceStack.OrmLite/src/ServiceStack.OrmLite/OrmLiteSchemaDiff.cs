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
    /// The name of the column or index in the database, null for a table
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The property of the column, null for columns that aren't in the model
    /// </summary>
    public FieldDefinition Field { get; set; }

    /// <summary>
    /// The column the model would be created with, e.g. VARCHAR(200) NULL
    /// </summary>
    public string ModelColumn { get; set; }

    /// <summary>
    /// The column that's in the database, e.g. VARCHAR(50) NOT NULL
    /// </summary>
    public string DatabaseColumn { get; set; }

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
        SchemaChangeType.AddColumn => $"Column {Table}.{Name} isn't in the database: {ModelColumn}",
        SchemaChangeType.AlterColumn => $"Column {Table}.{Name} is {DatabaseColumn} in the database, {ModelColumn} in {ModelType.Name}"
            + (Sql == null ? " (can't be altered in this database)" : ""),
        SchemaChangeType.DropColumn => $"Column {Table}.{Name} isn't in {ModelType.Name}: {DatabaseColumn}",
        SchemaChangeType.CreateIndex => $"Index {Name} of {Table} isn't in the database",
        _ => Type.ToString(),
    };

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
                    SchemaChangeType.AddColumn => $"  + {change.Name}  {change.ModelColumn}",
                    SchemaChangeType.AlterColumn => $"  ~ {change.Name}  {change.DatabaseColumn} -> {change.ModelColumn}"
                        + (change.Sql == null ? " (can't be altered in this database)" : ""),
                    SchemaChangeType.DropColumn => $"  - {change.Name}  {change.DatabaseColumn} (not in {change.ModelType.Name})",
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

    /// <summary>
    /// The source code of a migration that makes the changes, to review and add to your migrations, e.g:
    /// <para>File.WriteAllText("Migrations/Migration1005.cs", diff.ToMigration("Migration1005", "MyApp.Migrations"))</para>
    /// Columns that aren't in a model are only dropped by code that's commented out, as they may have been renamed.
    /// </summary>
    public string ToMigration(string className, string @namespace = null) =>
        SchemaMigrationWriter.Write(this, className, @namespace);
}

public static class OrmLiteSchemaDiffApi
{
    /// <summary>
    /// The differences between a model and its table: missing tables, columns and indexes, columns that aren't in
    /// the model and columns with a different type, size or nullability. E.g:
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

            var dbColumns = dialect.GetSchemaColumns(db, dialect.GetQuotedTableName(modelDef));

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

            var matched = new HashSet<ColumnSchema>();
            foreach (var fieldDef in columnFields)
            {
                var index = fieldDefs.IndexOf(fieldDef);
                var modelColumn = modelColumns != null && index >= 0 ? modelColumns[index] : null;
                var name = modelColumn?.ColumnName ?? dialect.NamingStrategy.GetColumnName(fieldDef.FieldName);
                var dbColumn = dbColumns.FirstOrDefault(x => x.ColumnName.EqualsIgnoreCase(name));
                if (dbColumn == null)
                {
                    diff.Changes.Add(new SchemaChange {
                        Type = SchemaChangeType.AddColumn,
                        ModelType = modelType,
                        Table = table,
                        Name = name,
                        Field = fieldDef,
                        ModelColumn = modelColumn != null ? Describe(modelColumn) : ColumnType(dialect, fieldDef),
                        Sql = dialect.ToAddColumnStatement(tableRef, fieldDef.Clone(f => f.IsPrimaryKey = false)),
                    });
                    continue;
                }

                matched.Add(dbColumn);
                if (modelColumn == null || IsSame(dbColumn, modelColumn, dialect))
                    continue;

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

            foreach (var dbColumn in dbColumns)
            {
                if (matched.Contains(dbColumn))
                    continue;

                diff.Changes.Add(new SchemaChange {
                    Type = SchemaChangeType.DropColumn,
                    ModelType = modelType,
                    Table = table,
                    Name = dbColumn.ColumnName,
                    DatabaseColumn = Describe(dbColumn),
                    Sql = dialect.ToDropColumnStatement(tableRef, dbColumn.ColumnName),
                    IsDestructive = true,
                });
            }

            var createIndexes = dialect.ToCreateIndexStatements(modelType);
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
    internal static string Write(SchemaDiff diff, string className, string ns)
    {
        if (string.IsNullOrEmpty(className))
            throw new ArgumentNullException(nameof(className));

        var sb = new StringBuilder();
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
                        up.Add($"// Db.RenameColumn<{name}>({ToLiteral(change.Name)}, \"NewName\");");
                        up.Add($"// Db.DropColumn<{name}>({ToLiteral(change.Name)});");
                        break;
                    case SchemaChangeType.CreateIndex:
                        up.Add($"Db.ExecuteSql({ToLiteral(change.Sql)});");
                        undo.Add($"Db.DropIndex<{name}>({ToLiteral(change.Name)});");
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
