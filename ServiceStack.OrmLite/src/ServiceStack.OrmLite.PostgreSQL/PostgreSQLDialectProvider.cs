using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Npgsql.TypeMapping;
using NpgsqlTypes;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Converters;
using ServiceStack.OrmLite.PostgreSQL.Converters;
using ServiceStack.OrmLite.Support;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.PostgreSQL
{
    public class PostgreSqlDialectProvider : OrmLiteDialectProviderBase<PostgreSqlDialectProvider>
    {
        public static PostgreSqlDialectProvider Instance = new();

        public bool UseReturningForLastInsertId { get; set; } = true;

        public string AutoIdGuidFunction { get; set; } = "uuid_generate_v4()";

        public PostgreSqlDialectProvider()
        {
            base.AutoIncrementDefinition = "";
            base.ParamString = ":";
            base.SelectIdentitySql = "SELECT LASTVAL()";
            this.NamingStrategy = new PostgreSqlNamingStrategy();
            this.StringSerializer = new JsonStringSerializer();
            
            base.InitColumnTypeMap();

            this.RowVersionConverter = new PostgreSqlRowVersionConverter();

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
            RegisterConverter<decimal>(new PostrgreSqlDecimalConverter());

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
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.EnableLegacyCaseInsensitiveDbParameters", true);
            RegisterConverter<DateOnly>(new PostgreSqlDateOnlyConverter());
#endif
            
#if NET472
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
#endif

            this.Variables = new Dictionary<string, string>
            {
                { OrmLiteVariables.SystemUtc, "now() at time zone 'utc'" },
                { OrmLiteVariables.MaxText, "TEXT" },
                { OrmLiteVariables.MaxTextUnicode, "TEXT" },
                { OrmLiteVariables.True, SqlBool(true) },                
                { OrmLiteVariables.False, SqlBool(false) },                
            };
            
            //this.ExecFilter = new PostgreSqlExecFilter {
            //    OnCommand = cmd => cmd.AllResultTypesAreUnknown = true
            //};
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
                NamingStrategy = normalize
                    ? new OrmLiteNamingStrategyBase()
                    : new PostgreSqlNamingStrategy();
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
            sql.AppendFormat("{0} {1}", GetQuotedColumnName(fieldDef.FieldName), fieldDefinition);

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
	        
            var pgConn = (NpgsqlConnection)db.ToDbConnection();
	        
            var modelDef = ModelDefinition<T>.Definition;

            var sb = StringBuilderCache.Allocate()
                .Append($"COPY {GetTableName(modelDef)} (");
            
            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields:config.InsertFields);
            var i = 0;
            foreach (var fieldDef in fieldDefs)
            {
                if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                    continue;

                if (i++ > 0)
                    sb.Append(",");

                sb.Append(NamingStrategy.GetColumnName(fieldDef.FieldName));
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
                            var dbType = GetNpgsqlDbType(fieldDef);
                            if (dbType == NpgsqlDbType.Text && dbValue is not string && dbValue is not char)
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

        public NpgsqlDbType GetNpgsqlDbType(FieldDefinition fieldDef)
        {
            var converter = GetConverterBestMatch(fieldDef);

            var columnDef = fieldDef.CustomFieldDefinition ?? converter.ColumnDefinition;
            return columnDef switch
            {
                "json" => NpgsqlDbType.Json,
                "jsonb" => NpgsqlDbType.Jsonb,
                "hstore" => NpgsqlDbType.Hstore,
                "text[]" => NpgsqlDbType.Array | NpgsqlDbType.Text,
                "short[]" => NpgsqlDbType.Array | NpgsqlDbType.Smallint,
                "integer[]" => NpgsqlDbType.Array | NpgsqlDbType.Integer,
                "bigint[]" => NpgsqlDbType.Array | NpgsqlDbType.Bigint,
                "real[]" => NpgsqlDbType.Array | NpgsqlDbType.Real,
                "double precision[]" => NpgsqlDbType.Array | NpgsqlDbType.Double,
                "double numeric[]" => NpgsqlDbType.Array | NpgsqlDbType.Numeric,
                "timestamp[]" => NpgsqlDbType.Array | NpgsqlDbType.Timestamp,
                "timestamp with time zone[]" => NpgsqlDbType.Array | NpgsqlDbType.TimestampTz,
                _ => converter.DbType switch
                {
                    DbType.Boolean => NpgsqlDbType.Boolean,
                    DbType.SByte => NpgsqlDbType.Smallint,
                    DbType.UInt16 => NpgsqlDbType.Smallint,
                    DbType.Byte => NpgsqlDbType.Integer,
                    DbType.Int16 => NpgsqlDbType.Integer,
                    DbType.Int32 => NpgsqlDbType.Integer,
                    DbType.UInt32 => NpgsqlDbType.Integer,
                    DbType.Int64 => NpgsqlDbType.Bigint,
                    DbType.UInt64 => NpgsqlDbType.Bigint,
                    DbType.Single => NpgsqlDbType.Double,
                    DbType.Double => NpgsqlDbType.Double,
                    DbType.Decimal => NpgsqlDbType.Numeric,
                    DbType.VarNumeric => NpgsqlDbType.Numeric,
                    DbType.Currency => NpgsqlDbType.Money,
                    DbType.Guid => NpgsqlDbType.Uuid,
                    DbType.String => NpgsqlDbType.Text,
                    DbType.AnsiString => NpgsqlDbType.Text,
                    DbType.StringFixedLength => NpgsqlDbType.Text,
                    DbType.AnsiStringFixedLength => NpgsqlDbType.Text,
                    DbType.Xml => NpgsqlDbType.Text,
                    DbType.Object => NpgsqlDbType.Text,
                    DbType.Binary => NpgsqlDbType.Bytea,
                    DbType.DateTime => NpgsqlDbType.TimestampTz,
                    DbType.DateTimeOffset => NpgsqlDbType.TimestampTz,
                    DbType.DateTime2 => NpgsqlDbType.Timestamp,
                    DbType.Date => NpgsqlDbType.Date,
                    DbType.Time => NpgsqlDbType.Time,
                    _ => throw new AggregateException($"Unknown NpgsqlDbType for {fieldDef.FieldType} {fieldDef.Name}")
                }
            };
        }
        
        protected override bool ShouldSkipInsert(FieldDefinition fieldDef) => 
            fieldDef.ShouldSkipInsert() || fieldDef.AutoId;

        protected virtual bool ShouldReturnOnInsert(ModelDefinition modelDef, FieldDefinition fieldDef) =>
            fieldDef.ReturnOnInsert || (fieldDef.IsPrimaryKey && fieldDef.AutoIncrement && HasInsertReturnValues(modelDef)) || fieldDef.AutoId;

        public override bool HasInsertReturnValues(ModelDefinition modelDef) =>
            modelDef.FieldDefinitions.Any(x => x.ReturnOnInsert || (x.AutoId && x.FieldType == typeof(Guid)));

        public override void PrepareParameterizedInsertStatement<T>(IDbCommand cmd, ICollection<string> insertFields = null, 
            Func<FieldDefinition,bool> shouldInclude=null)
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
                    sbReturningColumns.Append(GetQuotedColumnName(fieldDef.FieldName));
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
                    sbColumnNames.Append(GetQuotedColumnName(fieldDef.FieldName));

                    sbColumnValues.Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName),fieldDef.CustomInsert));
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
                sbReturningColumns.Append(GetQuotedColumnName(fieldDef.FieldName));
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
                : GetQuotedColumnName(fieldDef.FieldName);
            
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
            return new NpgsqlConnection(connectionString);
        }

        public override SqlExpression<T> SqlExpression<T>()
        {
            return new PostgreSqlExpression<T>(this);
        }

        public override IDbDataParameter CreateParam()
        {
            return new NpgsqlParameter();
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
                : "SELECT relname, reltuples FROM pg_class JOIN pg_catalog.pg_namespace n ON n.oid = pg_class.relnamespace WHERE relkind = 'r' AND nspname = {0}".SqlFmt(this, schemaName);
        }

        public override bool DoesTableExist(IDbCommand dbCmd, string tableName, string schema = null)
        {
            var sql = DoesTableExistSql(dbCmd, tableName, schema);
            var result = dbCmd.ExecLongScalar(sql);
            return result > 0;
        }

        public override async Task<bool> DoesTableExistAsync(IDbCommand dbCmd, string tableName, string schema = null, CancellationToken token=default)
        {
            var sql = DoesTableExistSql(dbCmd, tableName, schema);
            var result = await dbCmd.ExecLongScalarAsync(sql, token);
            return result > 0;
        }

        private string DoesTableExistSql(IDbCommand dbCmd, string tableName, string schema)
        {
            var sql = !Normalize || ReservedWords.Contains(tableName)
                ? "SELECT COUNT(*) FROM pg_class WHERE relname = {0} AND relkind = 'r'".SqlFmt(tableName)
                : "SELECT COUNT(*) FROM pg_class WHERE lower(relname) = {0} AND relkind = 'r'".SqlFmt(tableName.ToLower());

            var conn = dbCmd.Connection;
            if (conn != null)
            {
                var builder = new NpgsqlConnectionStringBuilder(conn.ConnectionString);
                if (schema == null)
                    schema = builder.SearchPath;
                if (schema == null)
                    schema = "public";

                // If a search path (schema) is specified, and there is only one, then assume the CREATE TABLE directive should apply to that schema.
                if (!string.IsNullOrEmpty(schema) && !schema.Contains(","))
                {
                    sql = !Normalize || ReservedWords.Contains(schema)
                        ? "SELECT COUNT(*) FROM pg_class JOIN pg_catalog.pg_namespace n ON n.oid = pg_class.relnamespace WHERE relname = {0} AND relkind = 'r' AND nspname = {1}"
                            .SqlFmt(tableName, schema)
                        : "SELECT COUNT(*) FROM pg_class JOIN pg_catalog.pg_namespace n ON n.oid = pg_class.relnamespace WHERE lower(relname) = {0} AND relkind = 'r' AND lower(nspname) = {1}"
                            .SqlFmt(tableName.ToLower(), schema.ToLower());
                }
            }

            return sql;
        }

        public override List<string> GetSchemas(IDbCommand dbCmd)
        {
            var sql = "SELECT DISTINCT TABLE_SCHEMA FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA NOT IN ('information_schema', 'pg_catalog')";
            return dbCmd.SqlColumn<string>(sql);
        }

        public override Dictionary<string, List<string>> GetSchemaTables(IDbCommand dbCmd)
        {
            var sql = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA NOT IN ('information_schema', 'pg_catalog')";
            return dbCmd.Lookup<string, string>(sql);
        }

        public override bool DoesSchemaExist(IDbCommand dbCmd, string schemaName)
        {
            dbCmd.CommandText = $"SELECT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = '{GetSchemaName(schemaName).SqlParam()}');";
            var query = dbCmd.ExecuteScalar();
            return query as bool? ?? false;
        }

        public override async Task<bool> DoesSchemaExistAsync(IDbCommand dbCmd, string schemaName, CancellationToken token = default)
        {
            dbCmd.CommandText = $"SELECT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = '{GetSchemaName(schemaName).SqlParam()}');";
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

        public override async Task<bool> DoesColumnExistAsync(IDbConnection db, string columnName, string tableName, string schema = null,
            CancellationToken token = default)
        {
            var sql = DoesColumnExistSql(columnName, tableName, ref schema);
            var result = await db.SqlScalarAsync<long>(sql, new { tableName, columnName, schema }, token);
            return result > 0;
        }

        private string DoesColumnExistSql(string columnName, string tableName, ref string schema)
        {
            var sql = !Normalize || ReservedWords.Contains(tableName)
                ? "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName".SqlFmt(tableName)
                : "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE lower(TABLE_NAME) = @tableName".SqlFmt(
                    tableName.ToLower());

            sql += !Normalize || ReservedWords.Contains(columnName)
                ? " AND COLUMN_NAME = @columnName".SqlFmt(columnName)
                : " AND lower(COLUMN_NAME) = @columnName".SqlFmt(columnName.ToLower());

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
            (Normalize || ReservedWords.Contains(name) || name.IndexOf(' ') >= 0 || name.IndexOf('.') >= 0);

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
                return GetQuotedName(NamingStrategy.GetSchemaName(modelDef.Schema)) + "." + GetQuotedName(NamingStrategy.GetTableName(modelDef.ModelName));

            return $"{GetQuotedName(NamingStrategy.GetSchemaName(modelDef.Schema))}.{GetQuotedName(NamingStrategy.GetTableName(modelDef.ModelName))}";
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
                throw new NotImplementedException("Returning last inserted identity is not implemented on this DB Provider.");

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
        
        public Dictionary<Type,NpgsqlDbType> TypesMap { get; } = new()
        {
            [typeof(bool)] = NpgsqlDbType.Boolean,
            [typeof(short)] = NpgsqlDbType.Smallint,
            [typeof(int)] = NpgsqlDbType.Integer,
            [typeof(long)] = NpgsqlDbType.Bigint,
            [typeof(float)] = NpgsqlDbType.Real,
            [typeof(double)] = NpgsqlDbType.Double,
            [typeof(decimal)] = NpgsqlDbType.Numeric,
            [typeof(string)] = NpgsqlDbType.Text,
            [typeof(char[])] = NpgsqlDbType.Varchar,
            [typeof(char)] = NpgsqlDbType.Char,
            [typeof(NpgsqlPoint)] = NpgsqlDbType.Point,
            [typeof(NpgsqlLSeg)] = NpgsqlDbType.LSeg,
            [typeof(NpgsqlPath)] = NpgsqlDbType.Path,
            [typeof(NpgsqlPolygon)] = NpgsqlDbType.Polygon,
            [typeof(NpgsqlLine)] = NpgsqlDbType.Line,
            [typeof(NpgsqlCircle)] = NpgsqlDbType.Circle,
            [typeof(NpgsqlBox)] = NpgsqlDbType.Box,
            [typeof(BitArray)] = NpgsqlDbType.Varbit,
            [typeof(IDictionary<string, string>)] = NpgsqlDbType.Hstore,
            [typeof(Guid)] = NpgsqlDbType.Uuid,
            [typeof(ValueTuple<IPAddress, int>)] = NpgsqlDbType.Cidr,
            [typeof(ValueTuple<IPAddress,int>)] = NpgsqlDbType.Inet,
            [typeof(IPAddress)] = NpgsqlDbType.Inet,
            [typeof(PhysicalAddress)] = NpgsqlDbType.MacAddr,
            [typeof(NpgsqlTsQuery)] = NpgsqlDbType.TsQuery,
            [typeof(NpgsqlTsVector)] = NpgsqlDbType.TsVector,
#if NET6_0_OR_GREATER
            [typeof(DateOnly)] = NpgsqlDbType.Date,
            [typeof(TimeOnly)] = NpgsqlDbType.Time,  
#endif
            [typeof(DateTime)] = NpgsqlDbType.Timestamp,
            [typeof(DateTimeOffset)] = NpgsqlDbType.TimestampTz,
            [typeof(TimeSpan)] = NpgsqlDbType.Time,
            [typeof(byte[])] = NpgsqlDbType.Bytea,
            [typeof(uint)] = NpgsqlDbType.Oid,
            [typeof(uint[])] = NpgsqlDbType.Oidvector,
        };

        public NpgsqlDbType GetDbType<T>() => GetDbType(typeof(T));
        public NpgsqlDbType GetDbType(Type type)
        {
            if (TypesMap.TryGetValue(type, out var paramType))
                return paramType;
            var genericEnum = type.GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));
            if (genericEnum != null)
                return GetDbType(genericEnum.GenericTypeArguments[0]) | NpgsqlDbType.Array;
            
            throw new NotSupportedException($"Type '{type.Name}' not found in 'TypesMap'");
        }
        
        public Dictionary<string, NpgsqlDbType> NativeTypes = new()
        {
            { "json", NpgsqlDbType.Json },
            { "jsonb", NpgsqlDbType.Jsonb },
            { "hstore", NpgsqlDbType.Hstore },
            { "text[]", NpgsqlDbType.Array | NpgsqlDbType.Text },
            { "short[]", NpgsqlDbType.Array | NpgsqlDbType.Smallint },
            { "int[]", NpgsqlDbType.Array | NpgsqlDbType.Integer },
            { "integer[]", NpgsqlDbType.Array | NpgsqlDbType.Integer },
            { "bigint[]", NpgsqlDbType.Array | NpgsqlDbType.Bigint },
            { "real[]", NpgsqlDbType.Array | NpgsqlDbType.Real },
            { "double precision[]", NpgsqlDbType.Array | NpgsqlDbType.Double },
            { "numeric[]", NpgsqlDbType.Array | NpgsqlDbType.Numeric },
            { "timestamp[]", NpgsqlDbType.Array | NpgsqlDbType.Timestamp },
            { "timestamp with time zone[]", NpgsqlDbType.Array | NpgsqlDbType.TimestampTz },
            { "bool[]", NpgsqlDbType.Array | NpgsqlDbType.Boolean },
            { "boolean[]", NpgsqlDbType.Array | NpgsqlDbType.Boolean },
        };
        
        public override void SetParameter(FieldDefinition fieldDef, IDbDataParameter p)
        {
            if (fieldDef.CustomFieldDefinition != null &&
                NativeTypes.TryGetValue(fieldDef.CustomFieldDefinition, out var npgsqlDbType))
            {
                p.ParameterName = this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName));
                ((NpgsqlParameter) p).NpgsqlDbType = npgsqlDbType;
            }
            else
            {
                base.SetParameter(fieldDef, p);
            }
        }

        public virtual bool UseRawValue(string columnDef) => columnDef?.EndsWith("[]") == true;

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

        public override string ToChangeColumnNameStatement(string schema, string table, FieldDefinition fieldDef, string oldColumn)
        {
            //var column = GetColumnDefinition(fieldDef);
            var columnType = GetColumnTypeDefinition(fieldDef.ColumnType, fieldDef.FieldLength, fieldDef.Scale);
            var newColumnName = NamingStrategy.GetColumnName(fieldDef.FieldName);

            var sql = $"ALTER TABLE {GetQuotedTableName(table, schema)} " +
                      $"ALTER COLUMN {GetQuotedColumnName(oldColumn)} TYPE {columnType}";
            sql += newColumnName != oldColumn
                ? $", RENAME COLUMN {GetQuotedColumnName(oldColumn)} TO {GetQuotedColumnName(newColumnName)};"
                : ";";
            return sql;
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

        protected DbConnection Unwrap(IDbConnection db) => (DbConnection)db.ToDbConnection();

        protected DbCommand Unwrap(IDbCommand cmd) => (DbCommand)cmd.ToDbCommand();

        protected DbDataReader Unwrap(IDataReader reader) => (DbDataReader)reader;

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

        public override async Task<Return> ReaderEach<Return>(IDataReader reader, Action fn, Return source, CancellationToken token = default)
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
    }
}
