#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

        OrmLiteConfig.DeoptimizeReader = true;
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
        get => ConnectionCommands.Contains(SqlitePragmas.JournalModeWal);
        set
        {
            if (value)
                ConnectionCommands.AddIfNotExists(SqlitePragmas.JournalModeWal);
            else
                ConnectionCommands.Remove(SqlitePragmas.JournalModeWal);
        }
    }

    /// <summary>
    /// Enable Foreign Keys (PRAGMA foreign_keys=ON)
    /// </summary>
    public bool EnableForeignKeys
    {
        get => ConnectionCommands.Contains(SqlitePragmas.EnableForeignKeys);
        set
        {
            if (value)
                ConnectionCommands.AddIfNotExists(SqlitePragmas.EnableForeignKeys);
            else
                ConnectionCommands.Remove(SqlitePragmas.DisableForeignKeys);
        }
    }

    /// <summary>
    /// Enable Foreign Keys (PRAGMA foreign_keys=ON)
    /// </summary>
    public TimeSpan BusyTimeout
    {
        set
        {
            ConnectionCommands.RemoveAll(x => x.StartsWith("PRAGMA busy_timeout"));
            ConnectionCommands.Add(SqlitePragmas.BusyTimeout(value));
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

    public bool EnableWriterLock { get; set; } = true;
    public static string Password { get; set; }
    public static bool UTF8Encoded { get; set; }
    public static bool ParseViaFramework { get; set; }

    public static string RowVersionTriggerFormat = "{0}RowVersionUpdateTrigger";

    public override bool SupportsSchema => false;
    public override bool SupportsConcurrentWrites => false;

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

            sb.Append(GetQuotedColumnName(fieldDef.FieldName));
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

    public override string ToPostDropTableStatement(ModelDefinition modelDef)
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
        return RowVersionTriggerFormat.Fmt(GetTableName(modelDef));
    }

    public override string ToPostCreateTableStatement(ModelDefinition modelDef)
    {
        if (modelDef.RowVersion != null)
        {
            var triggerName = GetTriggerName(modelDef);
            var tableName = GetTableName(modelDef);
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
            : "SELECT name FROM sqlite_master WHERE type ='table' AND name LIKE {0}".SqlFmt(this, GetTableName("",schema) + "%");
    }

    public override string GetSchemaName(string? schema)
    {
        return schema != null
            ? NamingStrategy.GetSchemaName(schema).Replace(".", "_")
            : NamingStrategy.GetSchemaName(schema);
    }

    public override string GetTableName(string table, string? schema = null) => 
        GetTableName(table, schema, useStrategy: true);

    public override string GetTableName(string table, string? schema, bool useStrategy)
    {
        if (useStrategy)
        {
            return schema != null && !table.StartsWithIgnoreCase(schema + "_")
                ? $"{NamingStrategy.GetSchemaName(schema)}_{NamingStrategy.GetTableName(table)}"
                : NamingStrategy.GetTableName(table);
        }
            
        return schema != null && !table.StartsWithIgnoreCase(schema + "_")
            ? $"{schema}_{table}"
            : table;
    }

    public override string GetQuotedTableName(string tableName, string? schema = null) =>
        GetQuotedName(GetTableName(tableName, schema));

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

    public override bool DoesTableExist(IDbCommand dbCmd, string tableName, string? schema = null)
    {
        var sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = {0}"
            .SqlFmt(this, GetTableName(tableName, schema));

        dbCmd.CommandText = sql;
        var result = dbCmd.LongScalar();

        return result > 0;
    }

    public override bool DoesColumnExist(IDbConnection db, string columnName, string tableName, string? schema = null)
    {
        var sql = "PRAGMA table_info({0})"
            .SqlFmt(this, GetTableName(tableName, schema));

        var columns = db.SqlList<Dictionary<string, object>>(sql);
        foreach (var column in columns)
        {
            if (column.TryGetValue("name", out var name) && name.ToString().EqualsIgnoreCase(columnName))
                return true;
        }
        return false;
    }

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

    public override string SqlCurrency(string fieldOrValue, string currencySymbol) => SqlConcat(["'" + currencySymbol + "'", "printf(\"%.2f\", " + fieldOrValue + ")"]);

    public override string SqlBool(bool value) => value ? "1" : "0";

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
