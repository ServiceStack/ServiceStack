using Kdbndp;
using KdbndpTypes;
using Oracle.ManagedDataAccess.Client;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Converters;
using ServiceStack.OrmLite.Kingbase.Converters.MySql;
using ServiceStack.OrmLite.MySql;
using ServiceStack.OrmLite.MySql.Converters;
using ServiceStack.OrmLite.Oracle;
using ServiceStack.OrmLite.Oracle.Converters;
using ServiceStack.OrmLite.PostgreSQL;
using ServiceStack.OrmLite.PostgreSQL.Converters;
using ServiceStack.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite.Kingbase;

public sealed class KingbaseDialectProvider : OrmLiteDialectProviderBase<KingbaseDialectProvider>
{
    private readonly IOrmLiteDialectProvider _flavorProvider;
    public bool UseReturningForLastInsertId { get; set; } = true;
    public string AutoIdGuidFunction { get; set; } = "uuid_generate_v4()";

    public readonly DbMode DbMode;

    public KingbaseDialectProvider(
        IOrmLiteDialectProvider flavorProvider,
        Dictionary<string, object> options = null)
    {
        _flavorProvider = flavorProvider;
        StringSerializer = new JsonStringSerializer();
        InitColumnTypeMap();

        if (flavorProvider is MySqlConnectorDialectProvider mySqlConnectorDialectProvider)
        {
            DbMode = DbMode.Mysql;
            //align with MySqlConnectorDialectProvider
            AutoIncrementDefinition = mySqlConnectorDialectProvider.AutoIncrementDefinition;
            DefaultValueFormat = mySqlConnectorDialectProvider.DefaultValueFormat;
            SelectIdentitySql = mySqlConnectorDialectProvider.SelectIdentitySql;
            UseReturningForLastInsertId = false; // MySQL does not support RETURNING
            Variables = mySqlConnectorDialectProvider.Variables;
            RegisterMySqlConverters();
        }
        else if (flavorProvider is PostgreSqlDialectProvider postgreSqlDialectProvider)
        {
            DbMode = DbMode.Pg;
            //align with PostgreSqlDialectProvider
            AutoIncrementDefinition = postgreSqlDialectProvider.AutoIncrementDefinition;
            ParamString = postgreSqlDialectProvider.ParamString;
            SelectIdentitySql = postgreSqlDialectProvider.SelectIdentitySql;
            UseReturningForLastInsertId = postgreSqlDialectProvider.UseReturningForLastInsertId;
            NamingStrategy = postgreSqlDialectProvider.NamingStrategy;
            RowVersionConverter = postgreSqlDialectProvider.RowVersionConverter;
            Variables = postgreSqlDialectProvider.Variables;
            RegisterPostgresConverters();
        }
        else if (flavorProvider is Oracle11OrmLiteDialectProvider oracle11OrmLiteDialectProvider)
        {
            DbMode = DbMode.Oracle;
            //align with Oracle11OrmLiteDialectProvider
            AutoIncrementDefinition = oracle11OrmLiteDialectProvider.AutoIncrementDefinition;
            ParamString = oracle11OrmLiteDialectProvider.ParamString;
            SelectIdentitySql = oracle11OrmLiteDialectProvider.SelectIdentitySql;
            UseReturningForLastInsertId = oracle11OrmLiteDialectProvider.UseReturningForLastInsertId;
            NamingStrategy = oracle11OrmLiteDialectProvider.NamingStrategy;
            RowVersionConverter = oracle11OrmLiteDialectProvider.RowVersionConverter;
            Variables = oracle11OrmLiteDialectProvider.Variables;
            ExecFilter = oracle11OrmLiteDialectProvider.ExecFilter;
            EnumConverter = oracle11OrmLiteDialectProvider.EnumConverter;

            OrmLiteContext.UseThreadStatic = true;
            OrmLiteConfig.DeoptimizeReader = true;

            OracleQuoteNames = TryGetOptions(options, "Oracle.QuoteNames", false);
            OracleCompactGuid = TryGetOptions(options, "Oracle.CompactGuid", false);
            OracleClientProvider = TryGetOptions(options, "Oracle.ClientProvider",
                OracleOrmLiteDialectProvider.ManagedProvider);

            RegisterOracleConverters();
        }
        else
        {
            throw new NotSupportedException();
        }

#if NET6_0_OR_GREATER
        // AppContext.SetSwitch("Kdbndp.EnableDiagnostics", true);
        // AppContext.SetSwitch("Kdbndp.DisableDateTimeInfinityConversions", true);
        AppContext.SetSwitch("Kdbndp.EnableLegacyTimestampBehavior", true);
#endif

#if NET472
        AppContext.SetSwitch("Kdbndp.EnableLegacyTimestampBehavior", true);
#endif

        ExecFilter = new KingbaseSqlExecFilter(DbMode);
    }

    public bool OracleQuoteNames { get; }
    public bool OracleCompactGuid { get; }
    public string OracleClientProvider { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T TryGetOptions<T>(Dictionary<string, object> options, string key, T defaultValue)
    {
        if (options != null && options.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return defaultValue;
    }

    private void RegisterOracleConverters()
    {
        var factory = OracleClientFactory.Instance;
        var timestampConverter = new OracleTimestampConverter(factory.GetType(), OracleClientProvider);
        if (OracleCompactGuid)
            RegisterConverter<Guid>(new OracleCompactGuidConverter());
        else
            RegisterConverter<Guid>(new OracleGuidConverter());

        RegisterConverter<TimeSpan>(new OracleTimeSpanAsIntConverter());
        RegisterConverter<string>(new OracleStringConverter());
        RegisterConverter<char[]>(new OracleCharArrayConverter());
        RegisterConverter<byte[]>(new OracleByteArrayConverter());

        RegisterConverter<long>(new OracleInt64Converter());
        RegisterConverter<sbyte>(new OracleSByteConverter());
        RegisterConverter<ushort>(new OracleUInt16Converter());
        RegisterConverter<uint>(new OracleUInt32Converter());
        RegisterConverter<ulong>(new OracleUInt64Converter());

        RegisterConverter<float>(new OracleFloatConverter());
        RegisterConverter<double>(new OracleDoubleConverter());
        RegisterConverter<decimal>(new OracleDecimalConverter());

        RegisterConverter<DateTime>(new OracleDateTimeConverter());
        RegisterConverter<DateTimeOffset>(new OracleDateTimeOffsetConverter(timestampConverter));
        RegisterConverter<bool>(new OracleBoolConverter());
    }

    private void RegisterMySqlConverters()
    {
        RegisterConverter<string>(new MySqlStringConverter());
        RegisterConverter<char[]>(new MySqlCharArrayConverter());
        RegisterConverter<bool>(new KingbaseMySqlBoolConverter());

        RegisterConverter<byte>(new KingbaseMySqlByteConverter());
        RegisterConverter<sbyte>(new KingbaseMySqlSByteConverter());
        RegisterConverter<short>(new KingbaseMySqlInt16Converter());
        RegisterConverter<ushort>(new KingbaseMySqlUInt16Converter());
        RegisterConverter<int>(new KingbaseMySqlInt32Converter());
        RegisterConverter<uint>(new KingbaseMySqlUInt32Converter());

        RegisterConverter<decimal>(new MySqlDecimalConverter());

        RegisterConverter<Guid>(new MySqlGuidConverter());
        RegisterConverter<DateTimeOffset>(new KingbaseMySqlDateTimeOffsetConverter());
    }

    private void RegisterPostgresConverters()
    {
        RowVersionConverter = new PostgreSqlRowVersionConverter();

        RegisterConverter<string>(new PostgreSqlStringConverter());
        RegisterConverter<char[]>(new PostgreSqlCharArrayConverter());

        RegisterConverter<bool>(new PostgreSqlBoolConverter());
        RegisterConverter<Guid>(new PostgreSqlGuidConverter());

        RegisterConverter<DateTime>(new PostgreSqlDateTimeConverter());
        RegisterConverter<DateTimeOffset>(new PostgreSqlDateTimeOffsetConverter());


        RegisterConverter<sbyte>(new PostrgreSqlSByteConverter());
        RegisterConverter<ushort>(new PostrgreSqlUInt16Converter());
        RegisterConverter<uint>(new PostrgreSqlUInt32Converter());
        RegisterConverter<ulong>(new PostrgreSqlUInt64Converter());

        RegisterConverter<float>(new PostrgreSqlFloatConverter());
        RegisterConverter<double>(new PostrgreSqlDoubleConverter());
        RegisterConverter<decimal>(new PostgreSqlDecimalConverter());

        RegisterConverter<byte[]>(new PostrgreSqlByteArrayConverter());

        //TODO provide support for pgsql native data structures:
        RegisterConverter<string[]>(new PostgreSqlStringArrayConverter());
        RegisterConverter<short[]>(new PostgreSqlShortArrayConverter());
        RegisterConverter<int[]>(new PostgreSqlIntArrayConverter());
        RegisterConverter<long[]>(new PostgreSqlLongArrayConverter());
        RegisterConverter<float[]>(new PostgreSqlFloatArrayConverter());
        RegisterConverter<double[]>(new PostgreSqlDoubleArrayConverter());
        RegisterConverter<decimal[]>(new PostgreSqlDecimalArrayConverter());
        RegisterConverter<DateTime[]>(new PostgreSqlDateTimeTimeStampArrayConverter());
        RegisterConverter<DateTimeOffset[]>(new PostgreSqlDateTimeOffsetTimeStampTzArrayConverter());

        RegisterConverter<XmlValue>(new PostgreSqlXmlConverter());
#if NET6_0_OR_GREATER
        RegisterConverter<DateOnly>(new PostgreSqlDateOnlyConverter());
#endif
    }

    public bool UseHstore
    {
        set
        {
            if (value)
            {
                RegisterConverter<IDictionary<string, string>>(new PostgreSqlHstoreConverter());
                RegisterConverter<Dictionary<string, string>>(new PostgreSqlHstoreConverter());
            }
            else
            {
                RemoveConverter<IDictionary<string, string>>();
                RemoveConverter<Dictionary<string, string>>();
            }
        }
    }

    private bool normalize;

    public bool Normalize
    {
        get => normalize;
        set
        {
            normalize = value;
            if (DbMode == DbMode.Pg)
            {
            }

            if (normalize)
            {
                if (DbMode == DbMode.Pg)
                {
                    NamingStrategy = PostgreSqlNamingStrategy.Instance;
                }
                else if (DbMode == DbMode.Oracle)
                {
                    //NamingStrategy =  // OracleNamingStrategy.Instance;
                }
            }
            else
            {
                NamingStrategy = OrmLiteNamingStrategyBase.Instance;
            }
        }
    }

    //https://www.postgresql.org/docs/7.3/static/sql-keywords-appendix.html
    public static HashSet<string> ReservedWords = new(new[]
    {
        "ALL",
        "ANALYSE",
        "ANALYZE",
        "AND",
        "ANY",
        "AS",
        "ASC",
        "AUTHORIZATION",
        "BETWEEN",
        "BINARY",
        "BOTH",
        "CASE",
        "CAST",
        "CHECK",
        "COLLATE",
        "COLUMN",
        "CONSTRAINT",
        "CURRENT_DATE",
        "CURRENT_TIME",
        "CURRENT_TIMESTAMP",
        "CURRENT_USER",
        "DEFAULT",
        "DEFERRABLE",
        "DISTINCT",
        "DO",
        "ELSE",
        "END",
        "EXCEPT",
        "FOR",
        "FOREIGN",
        "FREEZE",
        "FROM",
        "FULL",
        "HAVING",
        "ILIKE",
        "IN",
        "INITIALLY",
        "INNER",
        "INTERSECT",
        "INTO",
        "IS",
        "ISNULL",
        "JOIN",
        "LEADING",
        "LEFT",
        "LIKE",
        "LIMIT",
        "LOCALTIME",
        "LOCALTIMESTAMP",
        "NEW",
        "NOT",
        "NOTNULL",
        "NULL",
        "OFF",
        "OFFSET",
        "OLD",
        "ON",
        "ONLY",
        "OR",
        "ORDER",
        "OUTER",
        "OVERLAPS",
        "PLACING",
        "PRIMARY",
        "REFERENCES",
        "RIGHT",
        "SELECT",
        "SESSION_USER",
        "SIMILAR",
        "SOME",
        "TABLE",
        "THEN",
        "TO",
        "TRAILING",
        "TRUE",
        "UNION",
        "UNIQUE",
        "USER",
        "USING",
        "VERBOSE",
        "WHEN",
        "WHERE",
    }, StringComparer.OrdinalIgnoreCase);

    public override string GetColumnDefinition(FieldDefinition fieldDef)
    {
        if (fieldDef.IsRowVersion)
            return null;

        string fieldDefinition = null;
        if (fieldDef.CustomFieldDefinition != null)
        {
            fieldDefinition = ResolveFragment(fieldDef.CustomFieldDefinition);
        }
        else
        {
            if (fieldDef.AutoIncrement)
            {
                if (fieldDef.ColumnType == typeof(long))
                    fieldDefinition = "bigserial";
                else if (fieldDef.ColumnType == typeof(int))
                    fieldDefinition = "serial";
            }
            else
            {
                fieldDefinition = GetColumnTypeDefinition(fieldDef.ColumnType, fieldDef.FieldLength, fieldDef.Scale);
            }
        }

        var sql = StringBuilderCache.Allocate();
        sql.AppendFormat("{0} {1}", GetQuotedColumnName(fieldDef), fieldDefinition);

        if (fieldDef.IsPrimaryKey)
        {
            sql.Append(" PRIMARY KEY");
        }
        else
        {
            if (fieldDef.IsNullable)
            {
                sql.Append(" NULL");
            }
            else
            {
                sql.Append(" NOT NULL");
            }
        }

        if (fieldDef.IsUniqueConstraint)
        {
            sql.Append(" UNIQUE");
        }

        var defaultValue = GetDefaultValue(fieldDef);
        if (!string.IsNullOrEmpty(defaultValue))
        {
            sql.AppendFormat(DefaultValueFormat, defaultValue);
        }

        var definition = StringBuilderCache.ReturnAndFree(sql);
        return definition;
    }

    public override string GetAutoIdDefaultValue(FieldDefinition fieldDef)
    {
        return fieldDef.FieldType == typeof(Guid)
            ? AutoIdGuidFunction
            : null;
    }

    public override bool IsFullSelectStatement(string sql)
    {
        sql = sql?.TrimStart();
        if (string.IsNullOrEmpty(sql))
            return false;

        return sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
               sql.StartsWith("WITH ", StringComparison.OrdinalIgnoreCase);
    }

    public override void BulkInsert<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null)
    {
        config ??= new();
        if (config.Mode == BulkInsertMode.Sql)
        {
            base.BulkInsert(db, objs, config);
            return;
        }

        var pgConn = (KdbndpConnection)db.ToDbConnection();

        var modelDef = ModelDefinition<T>.Definition;

        var sb = StringBuilderCache.Allocate()
            .Append($"COPY {GetQuotedTableName(modelDef)} (");

        var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields: config.InsertFields);
        var i = 0;
        foreach (var fieldDef in fieldDefs)
        {
            if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                continue;

            if (i++ > 0)
                sb.Append(",");

            sb.Append(GetQuotedColumnName(fieldDef));
        }

        sb.Append(") FROM STDIN (FORMAT BINARY)");

        var copyCmd = StringBuilderCache.ReturnAndFree(sb);
        using var writer = pgConn.BeginBinaryImport(copyCmd);

        foreach (var obj in objs)
        {
            writer.StartRow();
            foreach (var fieldDef in fieldDefs)
            {
                if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                    continue;

                var value = fieldDef.AutoId
                    ? GetInsertDefaultValue(fieldDef)
                    : fieldDef.GetValue(obj);

                var converter = GetConverterBestMatch(fieldDef);
                if (converter == null)
                {
                    throw new NotSupportedException($"No converter found for {fieldDef.FieldType.Name}");
                }

                var dbValue = converter.ToDbValue(fieldDef.FieldType, value);
                if (dbValue is float f)
                    dbValue = (double)f;

                if (dbValue is null or DBNull)
                {
                    writer.WriteNull();
                }
                else
                {
                    try
                    {
                        var dbType = GetKdbndpDbType(fieldDef);
                        if (dbType == KdbndpDbType.Text && dbValue is not string && dbValue is not char)
                        {
                            dbValue = StringSerializer.SerializeToString(dbValue);
                        }

                        writer.Write(dbValue, dbType);
                    }
                    catch (Exception e)
                    {
                        LogManager.GetLogger(GetType()).Error(e.Message, e);
                        throw;
                    }
                }
            }
        }

        writer.Complete();
    }

    public KdbndpDbType GetKdbndpDbType(FieldDefinition fieldDef)
    {
        var converter = GetConverterBestMatch(fieldDef);

        var columnDef = fieldDef.CustomFieldDefinition ?? converter.ColumnDefinition;
        var dbType = converter.DbType;
        if (converter is EnumConverter)
        {
            dbType = fieldDef.TreatAsType == typeof(int)
                ? DbType.Int32
                : fieldDef.TreatAsType == typeof(long)
                    ? DbType.Int64
                    : DbType.String;
        }

        return columnDef switch
        {
            "json" => KdbndpDbType.Json,
            "jsonb" => KdbndpDbType.Jsonb,
            "hstore" => KdbndpDbType.Hstore,
            "text[]" => KdbndpDbType.Array | KdbndpDbType.Text,
            "short[]" => KdbndpDbType.Array | KdbndpDbType.Smallint,
            "integer[]" => KdbndpDbType.Array | KdbndpDbType.Integer,
            "bigint[]" => KdbndpDbType.Array | KdbndpDbType.Bigint,
            "real[]" => KdbndpDbType.Array | KdbndpDbType.Real,
            "double precision[]" => KdbndpDbType.Array | KdbndpDbType.Double,
            "double numeric[]" => KdbndpDbType.Array | KdbndpDbType.Numeric,
            "timestamp[]" => KdbndpDbType.Array | KdbndpDbType.Timestamp,
            "timestamp with time zone[]" => KdbndpDbType.Array | KdbndpDbType.TimestampTz,
            _ => dbType switch
            {
                DbType.Boolean => KdbndpDbType.Boolean,
                DbType.SByte => KdbndpDbType.Smallint,
                DbType.UInt16 => KdbndpDbType.Smallint,
                DbType.Byte => KdbndpDbType.Integer,
                DbType.Int16 => KdbndpDbType.Integer,
                DbType.Int32 => KdbndpDbType.Integer,
                DbType.UInt32 => KdbndpDbType.Integer,
                DbType.Int64 => KdbndpDbType.Bigint,
                DbType.UInt64 => KdbndpDbType.Bigint,
                DbType.Single => KdbndpDbType.Double,
                DbType.Double => KdbndpDbType.Double,
                DbType.Decimal => KdbndpDbType.Numeric,
                DbType.VarNumeric => KdbndpDbType.Numeric,
                DbType.Currency => KdbndpDbType.Money,
                DbType.Guid => KdbndpDbType.Uuid,
                DbType.String => KdbndpDbType.Text,
                DbType.AnsiString => KdbndpDbType.Text,
                DbType.StringFixedLength => KdbndpDbType.Text,
                DbType.AnsiStringFixedLength => KdbndpDbType.Text,
                DbType.Xml => KdbndpDbType.Text,
                DbType.Object => KdbndpDbType.Text,
                DbType.Binary => KdbndpDbType.Bytea,
                DbType.DateTime => KdbndpDbType.TimestampTz,
                DbType.DateTimeOffset => KdbndpDbType.TimestampTz,
                DbType.DateTime2 => KdbndpDbType.Timestamp,
                DbType.Date => KdbndpDbType.Date,
                DbType.Time => KdbndpDbType.Time,
                _ => throw new AggregateException($"Unknown KdbndpDbType for {fieldDef.FieldType} {fieldDef.Name}")
            }
        };
    }

    protected override bool ShouldSkipInsert(FieldDefinition fieldDef) =>
        fieldDef.ShouldSkipInsert() || fieldDef.AutoId;

    private bool ShouldReturnOnInsert(ModelDefinition modelDef, FieldDefinition fieldDef) =>
        fieldDef.ReturnOnInsert ||
        (fieldDef.IsPrimaryKey && fieldDef.AutoIncrement && HasInsertReturnValues(modelDef)) || fieldDef.AutoId;

    public override bool HasInsertReturnValues(ModelDefinition modelDef) =>
        modelDef.FieldDefinitions.Any(x => x.ReturnOnInsert || (x.AutoId && x.FieldType == typeof(Guid)));

    public override void PrepareParameterizedInsertStatement<T>(IDbCommand cmd, ICollection<string> insertFields = null,
        Func<FieldDefinition, bool> shouldInclude = null)
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

                sbColumnValues.Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName),
                    fieldDef.CustomInsert));
                AddParameter(cmd, fieldDef);
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

    //Convert xmin into an integer so it can be used in comparisons
    public const string RowVersionFieldComparer = "int8in(xidout(xmin))";

    public override SelectItem GetRowVersionSelectColumn(FieldDefinition field, string tablePrefix = null)
    {
        return new SelectItemColumn(this, "xmin", field.FieldName, tablePrefix);
    }

    public override string GetRowVersionColumn(FieldDefinition field, string tablePrefix = null)
    {
        return RowVersionFieldComparer;
    }

    public override void AppendFieldCondition(StringBuilder sqlFilter, FieldDefinition fieldDef, IDbCommand cmd)
    {
        var columnName = fieldDef.IsRowVersion
            ? RowVersionFieldComparer
            : GetQuotedColumnName(fieldDef);

        sqlFilter
            .Append(columnName)
            .Append("=")
            .Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName)));

        AddParameter(cmd, fieldDef);
    }

    public override string GetQuotedValue(string paramValue)
    {
        return "'" + paramValue.Replace("'", @"''") + "'";
    }

    public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
    {
        var result = new KdbndpConnection(connectionString);
        return result;
    }

    public override SqlExpression<T> SqlExpression<T>()
    {
        return new KingbaseSqlExpression<T>(this);
    }

    public override IDbDataParameter CreateParam()
    {
        return new KdbndpParameter();
    }

    public override string ToTableNamesStatement(string schema)
    {
        var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'";

        var schemaName = schema != null
            ? NamingStrategy.GetSchemaName(schema)
            : "public";
        return sql + " AND table_schema = {0}".SqlFmt(this, schemaName);
    }

    public override string ToTableNamesWithRowCountsStatement(bool live, string schema)
    {
        var schemaName = schema != null
            ? NamingStrategy.GetSchemaName(schema)
            : "public";
        return live
            ? null
            : "SELECT relname, reltuples FROM pg_class JOIN pg_catalog.pg_namespace n ON n.oid = pg_class.relnamespace WHERE relkind = 'r' AND nspname = {0}"
                .SqlFmt(this, schemaName);
    }

    public override bool DoesTableExist(IDbCommand dbCmd, string tableName, string schema = null)
    {
        var sql = DoesTableExistSql(dbCmd, tableName, schema);
        var result = dbCmd.ExecLongScalar(sql);
        return result > 0;
    }

    public override async Task<bool> DoesTableExistAsync(IDbCommand dbCmd, string tableName, string schema = null,
        CancellationToken token = default)
    {
        var sql = DoesTableExistSql(dbCmd, tableName, schema);
        var result = await dbCmd.ExecLongScalarAsync(sql, token);
        return result > 0;
    }

    private string DoesTableExistSql(IDbCommand dbCmd, string tableName, string schema)
    {
        var sql = !Normalize || ReservedWords.Contains(tableName)
            ? "SELECT COUNT(*) FROM pg_class WHERE relname = {0} AND relkind = 'r'".SqlFmt(this, tableName)
            : "SELECT COUNT(*) FROM pg_class WHERE lower(relname) = {0} AND relkind = 'r'".SqlFmt(this,
                tableName.ToLower());

        var conn = dbCmd.Connection;
        if (conn != null)
        {
            var builder = new KdbndpConnectionStringBuilder(conn.ConnectionString);
            if (schema == null)
                schema = builder.SearchPath;
            if (schema == null)
                schema = "public";

            // If a search path (schema) is specified, and there is only one, then assume the CREATE TABLE directive should apply to that schema.
            if (!string.IsNullOrEmpty(schema) && !schema.Contains(","))
            {
                sql = !Normalize || ReservedWords.Contains(schema)
                    ? "SELECT COUNT(*) FROM pg_class JOIN pg_catalog.pg_namespace n ON n.oid = pg_class.relnamespace WHERE relname = {0} AND relkind = 'r' AND nspname = {1}"
                        .SqlFmt(this, tableName, schema)
                    : "SELECT COUNT(*) FROM pg_class JOIN pg_catalog.pg_namespace n ON n.oid = pg_class.relnamespace WHERE lower(relname) = {0} AND relkind = 'r' AND lower(nspname) = {1}"
                        .SqlFmt(this, tableName.ToLower(), schema.ToLower());
            }
        }

        return sql;
    }

    public override List<string> GetSchemas(IDbCommand dbCmd)
    {
        var sql =
            "SELECT DISTINCT TABLE_SCHEMA FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA NOT IN ('information_schema', 'pg_catalog')";
        return dbCmd.SqlColumn<string>(sql);
    }

    public override Dictionary<string, List<string>> GetSchemaTables(IDbCommand dbCmd)
    {
        var sql =
            "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA NOT IN ('information_schema', 'pg_catalog')";
        return dbCmd.Lookup<string, string>(sql);
    }

    public override bool DoesSchemaExist(IDbCommand dbCmd, string schemaName)
    {
        dbCmd.CommandText =
            $"SELECT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = '{GetSchemaName(schemaName).SqlParam()}');";
        var query = dbCmd.ExecuteScalar();
        return query as bool? ?? false;
    }

    public override async Task<bool> DoesSchemaExistAsync(IDbCommand dbCmd, string schemaName,
        CancellationToken token = default)
    {
        dbCmd.CommandText =
            $"SELECT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = '{GetSchemaName(schemaName).SqlParam()}');";
        var query = await dbCmd.ScalarAsync();
        return query as bool? ?? false;
    }

    public override string ToCreateSchemaStatement(string schemaName)
    {
        var sql = $"CREATE SCHEMA {GetSchemaName(schemaName)}";
        return sql;
    }

    public override bool DoesColumnExist(IDbConnection db, string columnName, string tableName, string schema = null)
    {
        var sql = DoesColumnExistSql(columnName, tableName, ref schema);
        var result = db.SqlScalar<long>(sql, new { tableName, columnName, schema });
        return result > 0;
    }

    public override async Task<bool> DoesColumnExistAsync(IDbConnection db, string columnName, string tableName,
        string schema = null,
        CancellationToken token = default)
    {
        var sql = DoesColumnExistSql(columnName, tableName, ref schema);
        var result = await db.SqlScalarAsync<long>(sql, new { tableName, columnName, schema }, token);
        return result > 0;
    }

    private string DoesColumnExistSql(string columnName, string tableName, ref string schema)
    {
        var sql = !Normalize || ReservedWords.Contains(tableName)
            ? "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName".SqlFmt(this, tableName)
            : "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE lower(TABLE_NAME) = @tableName".SqlFmt(
                this,
                tableName.ToLower());

        sql += !Normalize || ReservedWords.Contains(columnName)
            ? " AND COLUMN_NAME = @columnName".SqlFmt(this, columnName)
            : " AND lower(COLUMN_NAME) = @columnName".SqlFmt(this, columnName.ToLower());

        if (schema != null)
        {
            sql += !Normalize || ReservedWords.Contains(schema)
                ? " AND TABLE_SCHEMA = @schema"
                : " AND lower(TABLE_SCHEMA) = @schema";

            if (Normalize)
                schema = schema.ToLower();
        }

        return sql;
    }

    public override string ToExecuteProcedureStatement(object objWithProperties)
    {
        var sbColumnValues = StringBuilderCache.Allocate();

        var tableType = objWithProperties.GetType();
        var modelDef = GetModel(tableType);

        foreach (var fieldDef in modelDef.FieldDefinitions)
        {
            if (sbColumnValues.Length > 0) sbColumnValues.Append(",");
            sbColumnValues.Append(fieldDef.GetQuotedValue(objWithProperties));
        }

        var colValues = StringBuilderCache.ReturnAndFree(sbColumnValues);
        var sql = string.Format("{0} {1}{2}{3};",
            GetQuotedTableName(modelDef),
            colValues.Length > 0 ? "(" : "",
            colValues,
            colValues.Length > 0 ? ")" : "");

        return sql;
    }

    public override string ToAlterColumnStatement(string schema, string table, FieldDefinition fieldDef)
    {
        var columnDefinition = GetColumnDefinition(fieldDef);
        var modelName = GetQuotedTableName(table, schema);

        var parts = columnDefinition.SplitOnFirst(' ');
        var columnName = parts[0];
        var columnType = parts[1];

        var notNull = columnDefinition.Contains("NOT NULL");

        var nullLiteral = notNull ? " NOT NULL" : " NULL";
        columnType = columnType.Replace(nullLiteral, "");

        var nullSql = notNull
            ? "SET NOT NULL"
            : "DROP NOT NULL";

        var sql = $"ALTER TABLE {modelName}\n"
                  + $"  ALTER COLUMN {columnName} TYPE {columnType},\n"
                  + $"  ALTER COLUMN {columnName} {nullSql}";

        return sql;
    }

    public override bool ShouldQuote(string name) => !string.IsNullOrEmpty(name) &&
                                                     (Normalize || ReservedWords.Contains(name) ||
                                                      name.IndexOf(' ') >= 0 || name.IndexOf('.') >= 0);

    public override string GetQuotedName(string name)
    {
        return name.IndexOf('.') >= 0
            ? base.GetQuotedName(name.Replace(".", "\".\""))
            : base.GetQuotedName(name);
    }

    public override string GetQuotedTableName(ModelDefinition modelDef)
    {
        if (!modelDef.IsInSchema)
            return base.GetQuotedTableName(modelDef);
        if (Normalize && !ShouldQuote(modelDef.ModelName) && !ShouldQuote(modelDef.Schema))
            return GetQuotedName(NamingStrategy.GetSchemaName(modelDef.Schema)) + "." +
                   GetQuotedName(NamingStrategy.GetTableName(modelDef.ModelName));

        return
            $"{GetQuotedName(NamingStrategy.GetSchemaName(modelDef.Schema))}.{GetQuotedName(NamingStrategy.GetTableName(modelDef.ModelName))}";
    }

    public override string GetTableName(string table, string schema, bool useStrategy)
    {
        if (useStrategy)
        {
            return schema != null
                ? $"{QuoteIfRequired(NamingStrategy.GetSchemaName(schema))}.{QuoteIfRequired(NamingStrategy.GetTableName(table))}"
                : QuoteIfRequired(NamingStrategy.GetTableName(table));
        }

        return schema != null
            ? $"{QuoteIfRequired(schema)}.{QuoteIfRequired(table)}"
            : QuoteIfRequired(table);
    }

    public override string GetLastInsertIdSqlSuffix<T>()
    {
        if (SelectIdentitySql == null)
            throw new NotImplementedException(
                "Returning last inserted identity is not implemented on this DB Provider.");

        if (UseReturningForLastInsertId)
        {
            var modelDef = GetModel(typeof(T));
            var pkName = NamingStrategy.GetColumnName(modelDef.PrimaryKey.FieldName);
            return !Normalize
                ? $" RETURNING \"{pkName}\""
                : " RETURNING " + pkName;
        }

        return "; " + SelectIdentitySql;
    }

    public Dictionary<Type, KdbndpDbType> TypesMap { get; } = new()
    {
        [typeof(bool)] = KdbndpDbType.Boolean,
        [typeof(short)] = KdbndpDbType.Smallint,
        [typeof(int)] = KdbndpDbType.Integer,
        [typeof(long)] = KdbndpDbType.Bigint,
        [typeof(float)] = KdbndpDbType.Real,
        [typeof(double)] = KdbndpDbType.Double,
        [typeof(decimal)] = KdbndpDbType.Numeric,
        [typeof(string)] = KdbndpDbType.Text,
        [typeof(char[])] = KdbndpDbType.Varchar,
        [typeof(char)] = KdbndpDbType.Char,
        [typeof(KdbndpPoint)] = KdbndpDbType.Point,
        [typeof(KdbndpLSeg)] = KdbndpDbType.LSeg,
        [typeof(KdbndpPath)] = KdbndpDbType.Path,
        [typeof(KdbndpPolygon)] = KdbndpDbType.Polygon,
        [typeof(KdbndpLine)] = KdbndpDbType.Line,
        [typeof(KdbndpCircle)] = KdbndpDbType.Circle,
        [typeof(KdbndpBox)] = KdbndpDbType.Box,
        [typeof(BitArray)] = KdbndpDbType.Varbit,
        [typeof(IDictionary<string, string>)] = KdbndpDbType.Hstore,
        [typeof(Guid)] = KdbndpDbType.Uuid,
        [typeof(ValueTuple<IPAddress, int>)] = KdbndpDbType.Cidr,
        [typeof(ValueTuple<IPAddress, int>)] = KdbndpDbType.Inet,
        [typeof(IPAddress)] = KdbndpDbType.Inet,
        [typeof(PhysicalAddress)] = KdbndpDbType.MacAddr,
        [typeof(KdbndpTsQuery)] = KdbndpDbType.TsQuery,
        [typeof(KdbndpTsVector)] = KdbndpDbType.TsVector,
#if NET6_0_OR_GREATER
        [typeof(DateOnly)] = KdbndpDbType.Date,
        [typeof(TimeOnly)] = KdbndpDbType.Time,
#endif
        [typeof(DateTime)] = KdbndpDbType.Timestamp,
        [typeof(DateTimeOffset)] = KdbndpDbType.TimestampTz,
        [typeof(TimeSpan)] = KdbndpDbType.Time,
        [typeof(byte[])] = KdbndpDbType.Bytea,
        [typeof(uint)] = KdbndpDbType.Oid,
        [typeof(uint[])] = KdbndpDbType.Oidvector,
    };

    public KdbndpDbType GetDbType<T>() => GetDbType(typeof(T));

    public KdbndpDbType GetDbType(Type type)
    {
        if (TypesMap.TryGetValue(type, out var paramType))
            return paramType;
        var genericEnum = type.GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));
        if (genericEnum != null)
            return GetDbType(genericEnum.GenericTypeArguments[0]) | KdbndpDbType.Array;

        throw new NotSupportedException($"Type '{type.Name}' not found in 'TypesMap'");
    }

    public Dictionary<string, KdbndpDbType> NativeTypes = new()
    {
        { "json", KdbndpDbType.Json },
        { "jsonb", KdbndpDbType.Jsonb },
        { "hstore", KdbndpDbType.Hstore },
        { "text[]", KdbndpDbType.Array | KdbndpDbType.Text },
        { "short[]", KdbndpDbType.Array | KdbndpDbType.Smallint },
        { "int[]", KdbndpDbType.Array | KdbndpDbType.Integer },
        { "integer[]", KdbndpDbType.Array | KdbndpDbType.Integer },
        { "bigint[]", KdbndpDbType.Array | KdbndpDbType.Bigint },
        { "real[]", KdbndpDbType.Array | KdbndpDbType.Real },
        { "double precision[]", KdbndpDbType.Array | KdbndpDbType.Double },
        { "numeric[]", KdbndpDbType.Array | KdbndpDbType.Numeric },
        { "timestamp[]", KdbndpDbType.Array | KdbndpDbType.Timestamp },
        { "timestamp with time zone[]", KdbndpDbType.Array | KdbndpDbType.TimestampTz },
        { "bool[]", KdbndpDbType.Array | KdbndpDbType.Boolean },
        { "boolean[]", KdbndpDbType.Array | KdbndpDbType.Boolean },
    };

    public override void SetParameter(FieldDefinition fieldDef, IDbDataParameter p)
    {
        if (fieldDef.CustomFieldDefinition != null &&
            NativeTypes.TryGetValue(fieldDef.CustomFieldDefinition, out var kdbndpDbType))
        {
            p.ParameterName = this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName));
            ((KdbndpParameter)p).KdbndpDbType = kdbndpDbType;
        }
        else
        {
            base.SetParameter(fieldDef, p);
        }
    }

    public bool UseRawValue(string columnDef) => columnDef?.EndsWith("[]") == true;

    protected override object GetValue(FieldDefinition fieldDef, object obj)
    {
        if (fieldDef.CustomFieldDefinition != null && NativeTypes.ContainsKey(fieldDef.CustomFieldDefinition)
                                                   && UseRawValue(fieldDef.CustomFieldDefinition))
        {
            return fieldDef.GetValue(obj);
        }

        return base.GetValue(fieldDef, obj);
    }

    public override void PrepareStoredProcedureStatement<T>(IDbCommand cmd, T obj)
    {
        var tableType = obj.GetType();
        var modelDef = GetModel(tableType);

        cmd.CommandText = GetQuotedTableName(modelDef);
        cmd.CommandType = CommandType.StoredProcedure;

        foreach (var fieldDef in modelDef.FieldDefinitions)
        {
            var p = cmd.CreateParameter();
            SetParameter(fieldDef, p);
            cmd.Parameters.Add(p);
        }

        SetParameterValues<T>(cmd, obj);
    }

    public override string ToChangeColumnNameStatement(string schema, string table, FieldDefinition fieldDef,
        string oldColumn)
    {
        //var column = GetColumnDefinition(fieldDef);
        var columnType = GetColumnTypeDefinition(fieldDef.ColumnType, fieldDef.FieldLength, fieldDef.Scale);
        var newColumnName = GetColumnName(fieldDef);

        var sql = $"ALTER TABLE {GetQuotedTableName(table, schema)} " +
                  $"ALTER COLUMN {GetQuotedColumnName(oldColumn)} TYPE {columnType}";
        sql += newColumnName != oldColumn
            ? $", RENAME COLUMN {GetQuotedColumnName(oldColumn)} TO {GetQuotedColumnName(newColumnName)};"
            : ";";
        return sql;
    }

    public override string ToResetSequenceStatement(Type tableType, string columnName, int value)
    {
        base.ToResetSequenceStatement(tableType, columnName, value);
        var modelDef = GetModel(tableType);
        var fieldDef = modelDef.GetFieldDefinition(columnName);
        // Table needs to be quoted but not column
        var useTable = GetQuotedTableName(modelDef);
        var useColumn = fieldDef != null ? GetColumnName(fieldDef) : columnName;

        return $"SELECT setval(pg_get_serial_sequence('{useTable}', '{useColumn}'), {value}, false);";
    }

    public override string SqlConflict(string sql, string conflictResolution)
    {
        //https://www.postgresql.org/docs/current/static/sql-insert.html
        return sql + " ON CONFLICT " + (conflictResolution == ConflictResolution.Ignore
            ? " DO NOTHING"
            : conflictResolution);
    }

    public override string SqlConcat(IEnumerable<object> args) => string.Join(" || ", args);

    public override string SqlCurrency(string fieldOrValue, string currencySymbol) => currencySymbol == "$"
        ? fieldOrValue + "::text::money::text"
        : "replace(" + fieldOrValue + "::text::money::text,'$','" + currencySymbol + "')";

    public override string SqlCast(object fieldOrValue, string castAs) =>
        $"({fieldOrValue})::{castAs}";

    public override string SqlRandom => "RANDOM()";

    private DbConnection Unwrap(IDbConnection db) => (DbConnection)db.ToDbConnection();

    private DbCommand Unwrap(IDbCommand cmd) => (DbCommand)cmd.ToDbCommand();

    private DbDataReader Unwrap(IDataReader reader) => (DbDataReader)reader;

    public override bool SupportsAsync => true;

    public override Task OpenAsync(IDbConnection db, CancellationToken token = default)
    {
        return Unwrap(db).OpenAsync(token);
    }

    public override Task<IDataReader> ExecuteReaderAsync(IDbCommand cmd, CancellationToken token = default)
    {
        return Unwrap(cmd).ExecuteReaderAsync(token).Then(x => (IDataReader)x);
    }

    public override Task<int> ExecuteNonQueryAsync(IDbCommand cmd, CancellationToken token = default)
    {
        return Unwrap(cmd).ExecuteNonQueryAsync(token);
    }

    public override Task<object> ExecuteScalarAsync(IDbCommand cmd, CancellationToken token = default)
    {
        return Unwrap(cmd).ExecuteScalarAsync(token);
    }

    public override Task<bool> ReadAsync(IDataReader reader, CancellationToken token = default)
    {
        return Unwrap(reader).ReadAsync(token);
    }

    public override async Task<List<T>> ReaderEach<T>(IDataReader reader, Func<T> fn, CancellationToken token = default)
    {
        try
        {
            var to = new List<T>();
            while (await ReadAsync(reader, token).ConfigureAwait(false))
            {
                var row = fn();
                to.Add(row);
            }

            return to;
        }
        finally
        {
            reader.Dispose();
        }
    }

    public override async Task<Return> ReaderEach<Return>(IDataReader reader, Action fn, Return source,
        CancellationToken token = default)
    {
        try
        {
            while (await ReadAsync(reader, token).ConfigureAwait(false))
            {
                fn();
            }

            return source;
        }
        finally
        {
            reader.Dispose();
        }
    }

    public override async Task<T> ReaderRead<T>(IDataReader reader, Func<T> fn, CancellationToken token = default)
    {
        try
        {
            if (await ReadAsync(reader, token).ConfigureAwait(false))
                return fn();

            return default(T);
        }
        finally
        {
            reader.Dispose();
        }
    }

    public override string ToRowCountStatement(string innerSql) =>
        $"SELECT COUNT(*) AS COUNT FROM {innerSql}";
}