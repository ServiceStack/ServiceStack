#nullable enable

using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.OrmLite.Sqlite.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Sqlite;

public abstract class SqliteOrmLiteDialectProviderBase : OrmLiteDialectProviderBase<SqliteOrmLiteDialectProviderBase>
{
    protected SqliteOrmLiteDialectProviderBase()
    {
        base.SelectIdentitySql = "SELECT last_insert_rowid()";

        base.InitColumnTypeMap();

        // Only for SQLite, GetValues() changes the behavior of System.Data.SQLite's GetGuid()
        DeoptimizeReader = true;
        base.RegisterConverter<DateTime>(new SqliteCoreDateTimeConverter());
        //Old behavior using native sqlite3.dll
        //base.RegisterConverter<DateTime>(new SqliteNativeDateTimeConverter());

        base.RegisterConverter<string>(new SqliteStringConverter());
        base.RegisterConverter<DateTimeOffset>(new SqliteDateTimeOffsetConverter());
        base.RegisterConverter<Guid>(new SqliteGuidConverter());
        base.RegisterConverter<bool>(new SqliteBoolConverter());
        base.RegisterConverter<byte[]>(new SqliteByteArrayConverter());
#if NETCORE            
            base.RegisterConverter<char>(new SqliteCharConverter());
#endif
        this.Variables = new Dictionary<string, string>
        {
            { OrmLiteVariables.SystemUtc, "CURRENT_TIMESTAMP" },
            { OrmLiteVariables.MaxText, "VARCHAR(1000000)" },
            { OrmLiteVariables.MaxTextUnicode, "NVARCHAR(1000000)" },
            { OrmLiteVariables.True, SqlBool(true) },                
            { OrmLiteVariables.False, SqlBool(false) },                
        };
    }

    /// <summary>
    /// Enable Write Ahead Logging (PRAGMA journal_mode=WAL)
    /// </summary>
    public bool EnableWal
    {
        get => OneTimeConnectionCommands.Contains(SqlitePragmas.JournalModeWal);
        set
        {
            if (value)
                OneTimeConnectionCommands.AddIfNotExists(SqlitePragmas.JournalModeWal);
            else
                OneTimeConnectionCommands.Remove(SqlitePragmas.JournalModeWal);
        }
    }

    /// <summary>
    /// Enable Foreign Keys (PRAGMA foreign_keys=ON)
    /// </summary>
    public bool EnableForeignKeys
    {
        get => OneTimeConnectionCommands.Contains(SqlitePragmas.EnableForeignKeys);
        set
        {
            if (value)
                OneTimeConnectionCommands.AddIfNotExists(SqlitePragmas.EnableForeignKeys);
            else
                OneTimeConnectionCommands.Remove(SqlitePragmas.DisableForeignKeys);
        }
    }

    /// <summary>
    /// PRAGMA busy_timeout
    /// </summary>
    public TimeSpan BusyTimeout
    {
        set
        {
            ConnectionCommands.RemoveAll(x => x.StartsWith("PRAGMA busy_timeout"));
            if (value > TimeSpan.Zero)
            {
                ConnectionCommands.Add(SqlitePragmas.BusyTimeout(value));
            }
        }
    }

    /// <summary>
    /// Whether to use UTC for DateTime fields
    /// </summary>
    public bool UseUtc
    {
        set => ((OrmLite.Converters.DateTimeConverter)this.GetConverter<DateTime>()).DateStyle = value 
            ? DateTimeKind.Utc 
            : DateTimeKind.Unspecified;
    }

    public bool EnableWriterLock { get; set; }
    public static string Password { get; set; }
    public static bool UTF8Encoded { get; set; }
    public static bool ParseViaFramework { get; set; }

    public static string RowVersionTriggerFormat = "{0}RowVersionUpdateTrigger";

    public override bool SupportsSchema => false;
    public override bool SupportsConcurrentWrites => false;

    /// <summary>
    /// Its drivers already wait for a lock to be released until the command times out, and an embedded database
    /// doesn't lose its connection, so retries would only wait longer
    /// </summary>
    public override bool SupportsRetries => false;

    public override string ToReadOnlySessionStatement(bool readOnly) => readOnly
        ? "PRAGMA query_only = ON"
        : "PRAGMA query_only = OFF";

    protected virtual bool ShouldReturnOnInsert(ModelDefinition modelDef, FieldDefinition fieldDef) =>
        fieldDef.ReturnOnInsert || (fieldDef.IsPrimaryKey && fieldDef.AutoIncrement && HasInsertReturnValues(modelDef));

    // SQLite only has database-level locks, ForUpdate() is ignored
    public override string? GetForUpdateClause(string? lockTable, bool skipLocked) => null;

    public override bool HasInsertReturnValues(ModelDefinition modelDef) =>
        modelDef.FieldDefinitions.Any(x => x.ReturnOnInsert);

    public override void PrepareParameterizedInsertStatement<T>(IDbCommand cmd, ICollection<string>? insertFields = null, 
        Func<FieldDefinition,bool>? shouldInclude=null)
    {
        var sbColumnNames = StringBuilderCache.Allocate();
        var sbColumnValues = StringBuilderCacheAlt.Allocate();
        var sbReturningColumns = StringBuilderCacheAlt.Allocate();
        var modelDef = OrmLiteUtils.GetModelDefinition(typeof(T));

        cmd.Parameters.Clear();

        var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields);
        foreach (var fieldDef in fieldDefs)
        {
            if (ShouldReturnOnInsert(modelDef, fieldDef))
            {
                sbReturningColumns.Append(sbReturningColumns.Length == 0 ? " RETURNING " : ",");
                sbReturningColumns.Append(GetQuotedColumnName(fieldDef));
            }

            if ((ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                && shouldInclude?.Invoke(fieldDef) != true)
                continue;

            if (sbColumnNames.Length > 0)
                sbColumnNames.Append(",");
            if (sbColumnValues.Length > 0)
                sbColumnValues.Append(",");

            try
            {
                sbColumnNames.Append(GetQuotedColumnName(fieldDef));

                sbColumnValues.Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName),fieldDef.CustomInsert));
                var p = AddParameter(cmd, fieldDef);
                if (fieldDef.AutoId)
                {
                    p.Value = GetInsertDefaultValue(fieldDef);
                }
            }
            catch (Exception ex)
            {
                Log.Error("ERROR in PrepareParameterizedInsertStatement(): " + ex.Message, ex);
                throw;
            }
        }

        foreach (var fieldDef in modelDef.AutoIdFields) // need to include any AutoId fields that weren't included 
        {
            if (fieldDefs.Contains(fieldDef))
                continue;

            sbReturningColumns.Append(sbReturningColumns.Length == 0 ? " RETURNING " : ",");
            sbReturningColumns.Append(GetQuotedColumnName(fieldDef));
        }

        var strReturning = StringBuilderCacheAlt.ReturnAndFree(sbReturningColumns);
        cmd.CommandText = sbColumnNames.Length > 0
            ? $"INSERT INTO {GetQuotedTableName(modelDef)} ({StringBuilderCache.ReturnAndFree(sbColumnNames)}) " +
              $"VALUES ({StringBuilderCacheAlt.ReturnAndFree(sbColumnValues)}){strReturning}"
            : $"INSERT INTO {GetQuotedTableName(modelDef)} DEFAULT VALUES{strReturning}";
    }

    public override bool SupportsUpsert => true;

    public override string ToUpsertReturningStatement(string sql, ModelDefinition modelDef, ICollection<FieldDefinition>? returnFields = null) =>
        ToReturningStatement(sql, modelDef, isDelete: false, returnFields);

    public override void PrepareParameterizedUpsertStatement<T>(IDbCommand cmd,
        ICollection<string>? insertFields = null, ICollection<string>? updateOnly = null)
    {
        PrepareUpsertFields<T>(cmd, insertFields, updateOnly,
            out var modelDef, out var insertFieldDefs, out var updateFieldDefs);

        var conflictTarget = GetQuotedColumnName(modelDef.PrimaryKey);
        var conflictAction = updateFieldDefs.Count == 0
            ? "DO NOTHING"
            : "DO UPDATE SET " + GetUpsertUpdateSql(updateFieldDefs);

        cmd.CommandText = $"{GetUpsertInsertSql(modelDef, insertFieldDefs)} " +
                          $"ON CONFLICT ({conflictTarget}) {conflictAction}";
    }

    protected override string ToBulkUpsertStatement(ModelDefinition modelDef, string stagingTable,
        List<FieldDefinition> insertFieldDefs, List<FieldDefinition> updateFieldDefs)
    {
        var conflictAction = updateFieldDefs.Count == 0
            ? "DO NOTHING"
            : "DO UPDATE SET " + updateFieldDefs.Map(x =>
                GetQuotedColumnName(x) + "=" + GetBulkUpsertValue(x.CustomUpdate, "excluded." + GetQuotedColumnName(x))).Join(", ");

        // WHERE true tells SQLite's parser that ON starts the upsert clause, and not a join of the SELECT
        return $"{GetBulkUpsertInsertSql(modelDef, stagingTable, insertFieldDefs)} WHERE true " +
               $"ON CONFLICT ({GetQuotedColumnName(modelDef.PrimaryKey)}) {conflictAction}";
    }

    public override string ToInsertRowsSql<T>(IEnumerable<T> objs, ICollection<string>? insertFields = null)
    {
        var modelDef = ModelDefinition<T>.Definition;
        var sb = StringBuilderCache.Allocate()
            .Append($"INSERT INTO {GetQuotedTableName(modelDef)} (");

        var fieldDefs = GetInsertFieldDefinitions(modelDef);
        var i = 0;
        foreach (var fieldDef in fieldDefs)
        {
            if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                continue;

            if (i++ > 0)
                sb.Append(",");

            sb.Append(GetQuotedColumnName(fieldDef));
        }
        sb.Append(") VALUES");

        var count = 0;
        foreach (var obj in objs)
        {
            count++;
            sb.AppendLine();
            sb.Append('(');
            i = 0;
            foreach (var fieldDef in fieldDefs)
            {
                if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                    continue;

                if (i++ > 0)
                    sb.Append(',');
                
                AppendInsertRowValueSql(sb, fieldDef, obj);
            }
            sb.Append("),");
        }
        if (count == 0)
            return "";

        sb.Length--;
        sb.AppendLine(";");
        var sql = StringBuilderCache.ReturnAndFree(sb);
        return sql;
    }

    public override string? ToPostDropTableStatement(ModelDefinition modelDef)
    {
        if (modelDef.RowVersion != null)
        {
            var triggerName = GetTriggerName(modelDef);
            return $"DROP TRIGGER IF EXISTS {GetQuotedName(triggerName)}";
        }

        return null;
    }

    private string GetTriggerName(ModelDefinition modelDef)
    {
        return RowVersionTriggerFormat.Fmt(GetTableNameOnly(new(modelDef)));
    }

    public override string? ToPostCreateTableStatement(ModelDefinition modelDef)
    {
        if (modelDef.RowVersion != null)
        {
            var triggerName = GetTriggerName(modelDef);
            var tableName = GetQuotedTableName(modelDef);
            var triggerBody = string.Format("UPDATE {0} SET {1} = OLD.{1} + 1 WHERE {2} = NEW.{2};",
                tableName, 
                modelDef.RowVersion.FieldName.SqlColumn(this), 
                modelDef.PrimaryKey.FieldName.SqlColumn(this));

            var sql = $"CREATE TRIGGER {triggerName} BEFORE UPDATE ON {tableName} FOR EACH ROW BEGIN {triggerBody} END;";

            return sql;
        }

        return null;
    }

    public static string CreateFullTextCreateTableStatement(object objectWithProperties)
    {
        var sbColumns = StringBuilderCache.Allocate();
        foreach (var propertyInfo in objectWithProperties.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var columnDefinition = (sbColumns.Length == 0)
                ? $"{propertyInfo.Name} TEXT PRIMARY KEY"
                : $", {propertyInfo.Name} TEXT";

            sbColumns.AppendLine(columnDefinition);
        }

        var tableName = objectWithProperties.GetType().Name;
        var sql = $"CREATE VIRTUAL TABLE \"{tableName}\" USING FTS3 ({StringBuilderCache.ReturnAndFree(sbColumns)});";

        return sql;
    }

    public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
    {
        if (connectionString == "DataSource=:memory:")
            connectionString = ":memory:";
            
        var isFullConnectionString = connectionString.Contains(";");
        var connString = StringBuilderCache.Allocate();
        if (!isFullConnectionString)
        {
            if (connectionString != ":memory:")
            {
                var existingDir = Path.GetDirectoryName(connectionString);
                if (!string.IsNullOrEmpty(existingDir) && !Directory.Exists(existingDir))
                {
                    Directory.CreateDirectory(existingDir);
                }
            }
            connString.AppendFormat(@"Data Source={0};", connectionString.Trim());
        }
        else
        {
            connString.Append(connectionString);
        }
        if (!string.IsNullOrEmpty(Password))
        {
            connString.AppendFormat("Password={0};", Password);
        }
        if (UTF8Encoded)
        {
            connString.Append("UseUTF16Encoding=True;");
        }

        if (options != null)
        {
            foreach (var option in options)
            {
                connString.AppendFormat("{0}={1};", option.Key, option.Value);
            }
        }
        
        ConnectionStringFilter?.Invoke(connString);

        return CreateConnection(StringBuilderCache.ReturnAndFree(connString));
    }

    public override OrmLiteConnection CreateOrmLiteConnection(OrmLiteConnectionFactory factory, string namedConnection = null)
    {
        var conn = base.CreateOrmLiteConnection(factory, namedConnection);
        if (EnableWriterLock)
        {
            conn.WriteLock = namedConnection == null
                ? Locks.AppDb
                : Locks.GetDbLock(namedConnection);
        }
        return conn;
    }

    public Action<StringBuilder>? ConnectionStringFilter { get; set; }

    protected abstract IDbConnection CreateConnection(string connectionString);

    public override string GetQuotedName(string name, string schema) => GetQuotedName(name); //schema name is embedded in table name in MySql

    public override string ToTableNamesStatement(string? schema)
    {
        return schema == null 
            ? "SELECT name FROM sqlite_master WHERE type ='table' AND name NOT LIKE 'sqlite_%'"
            : "SELECT name FROM sqlite_master WHERE type ='table' AND name LIKE {0}".SqlFmt(this, NamingStrategy.GetSchemaName(schema) + "%");
    }

    public override string QuoteSchema(string schema, string table) =>
        GetQuotedName(JoinSchema(schema, table));
    public override string JoinSchema(string schema, string table) => string.IsNullOrEmpty(schema) || table.StartsWith(schema + "_") 
        ? table
        : schema + "_" + table;

    public override string UnquotedTable(TableRef tableRef)
    {
        if (tableRef.QuotedName != null)
            return tableRef.QuotedName.Replace("\"","");
        var alias = tableRef.ModelDef?.Alias; 
        if (alias != null)
            return tableRef.ModelDef?.Schema != null
                ? NamingStrategy.GetSchemaName(tableRef.ModelDef.Schema) + "_" + NamingStrategy.GetAlias(alias)
                : NamingStrategy.GetAlias(alias);
        
        var schema = tableRef.ModelDef?.Schema ?? tableRef.Schema;
        var tableName = tableRef.ModelDef?.Name ?? tableRef.Name;
        return schema != null
            ? NamingStrategy.GetSchemaName(schema) + "_" + NamingStrategy.GetTableName(tableName)
            : NamingStrategy.GetTableName(tableName);
    }

    public override SqlExpression<T> SqlExpression<T>() => new SqliteExpression<T>(this);

    public override Dictionary<string, List<string>> GetSchemaTables(IDbCommand dbCmd)
    {
        return new Dictionary<string, List<string>> {
            ["default"] = dbCmd.SqlColumn<string>("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'") 
        };
    }

    public override bool DoesSchemaExist(IDbCommand dbCmd, string schemaName) => false;

    public override string ToCreateSchemaStatement(string schemaName)
    {
        throw new NotImplementedException("Schemas are not supported by sqlite");
    }

    // Columns are read from the table's definition, which has their declared type and if they allow nulls
    public override ColumnSchema[] GetSchemaColumns(IDbConnection db, string quotedTable)
    {
        var columns = db.SqlList<Dictionary<string, object>>($"PRAGMA table_xinfo({quotedTable})");
        return columns.Map(column => {
            var to = new ColumnSchema {
                ColumnName = column["name"]?.ToString(),
                ColumnOrdinal = Convert.ToInt32(column["cid"]),
                DataTypeName = column["type"]?.ToString(),
                DataType = typeof(object),
                ColumnSize = -1,
                // A primary key is only reported as not null when it's declared as NOT NULL
                AllowDBNull = Convert.ToInt32(column["notnull"]) == 0 && Convert.ToInt32(column["pk"]) == 0,
                IsKey = Convert.ToInt32(column["pk"]) > 0,
                DefaultValue = column["dflt_value"],
            };
            // VARCHAR(50) or DECIMAL(18,2)
            var size = to.DataTypeName?.RightPart('(').LeftPart(')').Split(',');
            if (to.DataTypeName?.EndsWith(")") == true && int.TryParse(size[0].Trim(), out var first))
            {
                to.DataTypeName = to.DataTypeName.LeftPart('(').Trim();
                if (size.Length == 2 && int.TryParse(size[1].Trim(), out var scale))
                {
                    to.NumericPrecision = first;
                    to.NumericScale = scale;
                }
                else
                {
                    to.ColumnSize = first;
                }
            }
            return to;
        }).ToArray();
    }

    public override List<string> GetTableIndexNames(IDbConnection db, TableRef tableRef) => db.Column<string>(
        "SELECT name FROM sqlite_master WHERE type='index' AND tbl_name = {0} COLLATE NOCASE".SqlFmt(this, UnquotedTable(tableRef)));

    // Indexes that aren't created with CREATE INDEX are for primary keys and unique constraints
    public override List<IndexSchema> GetTableIndexes(IDbConnection db, TableRef tableRef) => ToIndexSchemas(
        db.SqlList<Dictionary<string, object>>(
            "SELECT il.name AS name, il.\"unique\" AS is_unique, CASE WHEN il.origin = 'c' THEN 0 ELSE 1 END AS is_constraint, " +
            "(SELECT group_concat(name, ',') FROM (SELECT ii.name FROM pragma_index_info(il.name) ii ORDER BY ii.seqno)) AS columns " +
            "FROM pragma_index_list({0}) il".SqlFmt(this, UnquotedTable(tableRef))));

    public override Dictionary<string, string> GetColumnDefaults(IDbConnection db, string quotedTable)
    {
        var to = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in db.SqlList<Dictionary<string, object>>($"PRAGMA table_xinfo({quotedTable})"))
        {
            var defaultValue = column["dflt_value"];
            to[column["name"].ToString()] = defaultValue is null or DBNull ? null : defaultValue.ToString();
        }
        return to;
    }

    // SQLite doesn't keep the names of foreign keys, and "to" is null for foreign keys of the referenced primary key
    public override List<ForeignKeySchema> GetTableForeignKeys(IDbConnection db, TableRef tableRef) => ToForeignKeySchemas(
        db.SqlList<Dictionary<string, object>>(
            ("SELECT NULL AS name, group_concat(\"from\", ',') AS columns, \"table\" AS ref_table, " +
             "group_concat(\"to\", ',') AS ref_columns, on_delete, on_update " +
             "FROM (SELECT * FROM pragma_foreign_key_list({0}) ORDER BY id, seq) GROUP BY id")
                .SqlFmt(this, UnquotedTable(tableRef))));

    // Constraints and defaults can only be changed by creating the table again
    public override string ToAddForeignKeyStatement(TableRef tableRef, FieldDefinition fieldDef) => null;

    public override string ToAlterColumnDefaultStatement(TableRef tableRef, FieldDefinition fieldDef) => null;

    public override bool DoesTableExist(IDbCommand dbCmd, TableRef tableRef)
    {
        // The names of tables aren't case sensitive
        var sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = {0} COLLATE NOCASE"
            .SqlFmt(this, UnquotedTable(tableRef));

        dbCmd.CommandText = sql;
        var result = dbCmd.LongScalar();

        return result > 0;
    }

    public override bool DoesColumnExist(IDbConnection db, string columnName, TableRef tableRef)
    {
        var sql = "PRAGMA table_info({0})"
            .SqlFmt(this, UnquotedTable(tableRef));

        var columns = db.SqlList<Dictionary<string, object>>(sql);
        foreach (var column in columns)
        {
            if (column.TryGetValue("name", out var name) && name.ToString().EqualsIgnoreCase(columnName))
                return true;
        }
        return false;
    }

    // Vectors are stored as the bytes of their floats, which the functions of the sqlite-vec extension compare
    public override string GetVectorColumnDefinition(int dimensions) => "BLOB";

    public override string ToVectorDistance(VectorDistance distance, string vector, string other) => distance switch {
        VectorDistance.Cosine => $"vec_distance_cosine({vector}, {other})",
        VectorDistance.L2 => $"vec_distance_L2({vector}, {other})",
        _ => throw new NotSupportedException("sqlite-vec doesn't have an inner product vector distance"),
    };

    public override string GetColumnDefinition(FieldDefinition fieldDef)
    {
        // http://www.sqlite.org/lang_createtable.html#rowid
        var ret = base.GetColumnDefinition(fieldDef);
        if (fieldDef.IsPrimaryKey)
            return ret.Replace(" BIGINT ", " INTEGER ");
        if (fieldDef.IsRowVersion)
            return ret + " DEFAULT 1";

        return ret;
    }

    public override string SqlConflict(string sql, string conflictResolution)
    {
        // http://www.sqlite.org/lang_conflict.html
        var parts = sql.SplitOnFirst(' ');
        return parts[0] + " OR " + conflictResolution + " " + parts[1];
    }

    public override string SqlConcat(IEnumerable<object> args) => string.Join(" || ", args);

    public override string SqlCurrency(string fieldOrValue, string currencySymbol) => SqlConcat([GetQuotedValue(currencySymbol), "printf(\"%.2f\", " + fieldOrValue + ")"]);

    public override string SqlBool(bool value) => value ? "1" : "0";

    public override ExplainQuery ToExplainQuery(IDbConnection db, string sql, bool analyze) => analyze
        ? throw new NotSupportedException("SQLite doesn't support analyzing query plans")
        : new() { Sql = "EXPLAIN QUERY PLAN " + sql, ReadPlan = ReadQueryPlan };

    // EXPLAIN QUERY PLAN returns the steps of the plan as a tree of (id, parent, notused, detail) rows
    private static string ReadQueryPlan(IDataReader reader)
    {
        var depths = new Dictionary<long, int>();
        var sb = new StringBuilder();
        while (reader.Read())
        {
            var id = Convert.ToInt64(reader.GetValue(0));
            var parent = Convert.ToInt64(reader.GetValue(1));
            var depth = depths.TryGetValue(parent, out var parentDepth) ? parentDepth + 1 : 0;
            depths[id] = depth;

            if (sb.Length > 0)
                sb.Append('\n');
            sb.Append(' ', depth * 2).Append(Convert.ToString(reader.GetValue(3)));
        }
        return sb.ToString();
    }

    // Requires SQLite 3.35+
    public override string ToReturningStatement(string sql, ModelDefinition modelDef, bool isDelete, ICollection<FieldDefinition>? returnFields = null) =>
        sql.TrimEnd().TrimEnd(';') + " RETURNING " + GetReturningColumns(modelDef, returnFields);

    public override string SqlRandom => "random()";

    public override void EnableForeignKeysCheck(IDbCommand cmd) => cmd.ExecNonQuery(SqlitePragmas.EnableForeignKeys);
    public override Task EnableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token = default) => 
        cmd.ExecNonQueryAsync(SqlitePragmas.EnableForeignKeys, null, token);

    public override void DisableForeignKeysCheck(IDbCommand cmd) => cmd.ExecNonQuery(SqlitePragmas.DisableForeignKeys);
    public override Task DisableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token = default) => 
        cmd.ExecNonQueryAsync(SqlitePragmas.DisableForeignKeys, null, token);
}

public static class SqlitePragmas
{
    public const string JournalModeWal = "PRAGMA journal_mode=WAL;";
    public const string EnableForeignKeys = "PRAGMA foreign_keys=ON;";
    public const string DisableForeignKeys = "PRAGMA foreign_keys=OFF;";
    public static string BusyTimeout(TimeSpan timeout) => $"PRAGMA busy_timeout={(int)timeout.TotalMilliseconds};";
}

public static class SqliteExtensions
{
    public static IOrmLiteDialectProvider Configure(this IOrmLiteDialectProvider provider,
        string? password = null, bool parseViaFramework = false, bool utf8Encoding = false)
    {
        if (password != null)
            SqliteOrmLiteDialectProviderBase.Password = password;
        if (parseViaFramework)
            SqliteOrmLiteDialectProviderBase.ParseViaFramework = true;
        if (utf8Encoding)
            SqliteOrmLiteDialectProviderBase.UTF8Encoded = true;

        return provider;
    }
}
