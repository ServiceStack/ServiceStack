using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;
using ServiceStack.OrmLite.Oracle.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Oracle
{
    public class OracleOrmLiteDialectProvider : OrmLiteDialectProviderBase<OracleOrmLiteDialectProvider>
    {
        public const string ManagedProvider = "Oracle.ManagedDataAccess.Client";
        public string AutoIdGuidFunction { get; set; } = "SYS_GUID()";
        public bool UseReturningForLastInsertId { get; set; } = true;
        
        public static readonly OracleOrmLiteDialectProvider Instance = new OracleOrmLiteDialectProvider();
        public static string RowVersionTriggerFormat = "{0}RowVersionUpdateTrigger";

        // TODO refactor to hashset (case insensitive
        protected readonly List<string> ReservedNames = new List<string>
        {
            "ACCESS", "DEFAULT", "INTEGER", "ONLINE", "START", "ADD", "DELETE", "INTERSECT", "OPTION", "SUCCESSFUL", "ALL", "DESC",
            "INTO", "OR", "SYNONYM", "ALTER", "DISTINCT", "IS", "ORDER", "SYSDATE", "AND", "DROP", "LEVEL", "PCTFREE", "TABLE", "ANY",
            "ELSE", "LIKE", "PRIOR", "THEN", "AS", "EXCLUSIVE", "LOCK", "PRIVILEGES", "TO", "ASC", "EXISTS", "LONG", "PUBLIC", "TRIGGER",
            "AUDIT", "FILE", "MAXEXTENTS", "RAW", "UID", "BETWEEN", "FLOAT", "MINUS", "RENAME", "UNION", "BY", "FOR", "MLSLABEL", "RESOURCE",
            "UNIQUE", "CHAR", "FROM", "MODE", "REVOKE", "UPDATE", "CHECK", "GRANT", "MODIFY", "ROW", "USER", "CLUSTER", "GROUP", "NOAUDIT",
            "ROWID", "VALIDATE", "COLUMN", "HAVING", "NOCOMPRESS", "ROWNUM", "VALUES", "COMMENT", "IDENTIFIED", "NOT", "ROWS", "VARCHAR",
            "COMPRESS", "IMMEDIATE", "NOWAIT", "SELECT", "VARCHAR2", "CONNECT", "IN", "NULL", "SESSION", "VIEW", "CREATE", "INCREMENT",
            "NUMBER", "SET", "WHENEVER", "CURRENT", "INDEX", "OF", "SHARE", "WHERE", "DATE", "INITIAL", "OFFLINE", "SIZE", "WITH", "DECIMAL",
            "INSERT", "ON", "SMALLINT", "PASSWORD", "ACTIVE", "LEFT", "DOUBLE", "STRING", "DATETIME", "TYPE", "TIMESTAMP",
            "BYTE", "SHORT", "INT", "SUBTYPE"
        };

        // TODO refactor to hashset (case insensitive
        protected readonly List<string> ReservedParameterNames = new List<string>
        {
            "ACCESS", "DEFAULT", "INTEGER", "ONLINE", "START", "ADD", "DELETE", "INTERSECT", "OPTION", "SUCCESSFUL", "ALL", "DESC",
            "INTO", "OR", "SYNONYM", "ALTER", "DISTINCT", "IS", "ORDER", "SYSDATE", "AND", "DROP", "LEVEL", "PCTFREE", "TABLE", "ANY",
            "ELSE", "LIKE", "PRIOR", "THEN", "AS", "EXCLUSIVE", "LOCK", "PRIVILEGES", "TO", "ASC", "EXISTS", "LONG", "PUBLIC", "TRIGGER",
            "AUDIT", "FILE", "MAXEXTENTS", "RAW", "UID", "BETWEEN", "FLOAT", "MINUS", "RENAME", "UNION", "BY", "FOR", "MLSLABEL", "RESOURCE",
            "UNIQUE", "CHAR", "FROM", "MODE", "REVOKE", "UPDATE", "CHECK", "GRANT", "MODIFY", "ROW", "USER", "CLUSTER", "GROUP", "NOAUDIT",
            "ROWID", "VALIDATE", "COLUMN", "HAVING", "NOCOMPRESS", "ROWNUM", "VALUES", "COMMENT", "IDENTIFIED", "NOT", "ROWS", "VARCHAR",
            "COMPRESS", "IMMEDIATE", "NOWAIT", "SELECT", "VARCHAR2", "CONNECT", "IN", "NULL", "SESSION", "VIEW", "CREATE", "INCREMENT",
            "NUMBER", "SET", "WHENEVER", "CURRENT", "INDEX", "OF", "SHARE", "WHERE", "DATE", "INITIAL", "OFFLINE", "SIZE", "WITH", "DECIMAL",
            "INSERT", "ON", "SMALLINT",
            "BYTE", "SHORT", "INT", "SUBTYPE"
        };

        internal long LastInsertId { get; set; }
        protected const int MaxNameLength = 30;
        protected const int MaxStringColumnLength = 4000;
        private readonly DbProviderFactory _factory;
        private readonly OracleTimestampConverter _timestampConverter;

        public OracleOrmLiteDialectProvider()
            : this(false, false)
        {
        }

        public OracleOrmLiteDialectProvider(bool compactGuid, bool quoteNames, string clientProvider = ManagedProvider)
        {
            // Make managed provider work with CaptureSqlFilter, safe since Oracle providers don't support async
            OrmLiteContext.UseThreadStatic = true;
            // Not nice to slow down, but need to read some types via Oracle-specific read methods so can't read all fields in single call
            OrmLiteConfig.DeoptimizeReader = true;

            QuoteNames = quoteNames;
            AutoIncrementDefinition = string.Empty;

            ParamString = ":";
            
            NamingStrategy = new OracleNamingStrategy(MaxNameLength);
            ExecFilter = new OracleExecFilter();

            _factory = OracleClientFactory.Instance;
#if !NETFRAMEWORK
            // TODO tune settings 
            //OracleConfiguration.FetchSize = 1024 * 1024;
            //OracleConfiguration.SelfTuning = false;
            OracleConfiguration.BindByName = true;
            OracleConfiguration.CommandTimeout = OrmLiteConfig.CommandTimeout;
            //OracleConfiguration.StatementCacheSize = -1;
            //OracleConfiguration.SendBufferSize = 8192;
            //OracleConfiguration.ReceiveBufferSize = 8192;
            //OracleConfiguration.DisableOOB = true;
            //OracleConfiguration.OnsMode = OnsConfigMode.Unspecified;
            OracleConfiguration.TraceOption = 1;
            //OracleConfiguration.TraceLevel = 7;
            //OracleConfiguration.TcpNoDelay = true;
            OracleConfiguration.TraceFileLocation = "c:\\temp\\ora";
#endif
            
            _timestampConverter = new OracleTimestampConverter(_factory.GetType(), clientProvider);

            InitColumnTypeMap();

            //Special Converters if you need to override default behavior
            base.EnumConverter = new OracleEnumConverter();

            if (compactGuid)
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
            RegisterConverter<DateTimeOffset>(new OracleDateTimeOffsetConverter(_timestampConverter));
            RegisterConverter<bool>(new OracleBoolConverter());

            this.Variables = new Dictionary<string, string>
            {
                { OrmLiteVariables.SystemUtc, "sys_extract_utc(systimestamp)" },
                { OrmLiteVariables.MaxText, $"VARCHAR2({MaxStringColumnLength})" },
                { OrmLiteVariables.MaxTextUnicode, $"NVARCHAR2({MaxStringColumnLength / 2})" },
                { OrmLiteVariables.True, SqlBool(true) },                
                { OrmLiteVariables.False, SqlBool(false) },                
            };
        }

        public override string ToPostCreateTableStatement(ModelDefinition modelDef)
        {
            if (modelDef.RowVersion != null)
            {
                var triggerName = NamingStrategy.ApplyNameRestrictions(RowVersionTriggerFormat.Fmt(modelDef.ModelName));
                var sqlColumn = modelDef.RowVersion.FieldName.SqlColumn(this);
                var triggerBody = $":NEW.{sqlColumn} := :OLD.{sqlColumn}+1;";

                var sql = $"CREATE TRIGGER {Quote(triggerName)} BEFORE UPDATE ON {NamingStrategy.GetTableName(modelDef)} FOR EACH ROW BEGIN {triggerBody} END;";

                return sql;
            }

            return null;
        }


        public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
        {
            if (options != null)
            {
                connectionString = options.Aggregate(connectionString, (current, option) => $"{current}{option.Key}={option.Value};");
            }

            var connection = _factory.CreateConnection();
            if (connection != null) connection.ConnectionString = connectionString;
            return new OracleConnection(connection);
        }

        public override long GetLastInsertId(IDbCommand dbCmd)
        {
            return LastInsertId;
        }

        public override object ToDbValue(object value, Type type)
        {
            if (value == null || value is DBNull)
                return null;

            if (type.IsEnum && !type.HasAttributeCached<EnumAsIntAttribute>())
                return EnumConverter.ToDbValue(type, value);

            if (type.IsRefType())
                return ReferenceTypeConverter.ToDbValue(type, value);

            IOrmLiteConverter converter = null;
            try
            {
                if (Converters.TryGetValue(type, out converter))
                {
                    if (type == typeof(DateTimeOffset))
                    {
                        return converter.ToQuotedString(type, value);
                    }

                    return converter.ToDbValue(type, value);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in {converter?.GetType().Name}.ToDbValue() value '{value.GetType().Name}' and Type '{type.Name}'", ex);
                throw;
            }

            return base.ToDbValue(value, type);
        }

        const string IsoDateFormat = "yyyy-MM-dd";
        const string IsoTimeFormat = "HH:mm:ss";
        const string IsoMillisecondFormat = "fffffff";
        const string IsoTimeZoneFormat = "zzz";
        const string OracleDateFormat = "YYYY-MM-DD";
        const string OracleTimeFormat = "HH24:MI:SS";
        const string OracleMillisecondFormat = "FF9";
        const string OracleTimeZoneFormat = "TZH:TZM";

        internal string GetQuotedDateTimeOffsetValue(DateTimeOffset dateValue)
        {
            var iso8601Format = $"{GetIsoDateTimeFormat(dateValue.TimeOfDay)} {IsoTimeZoneFormat}";
            var oracleFormat = $"{GetOracleDateTimeFormat(dateValue.TimeOfDay)} {OracleTimeZoneFormat}";
            return string.Format("TO_TIMESTAMP_TZ({0}, {1})", base.GetQuotedValue(dateValue.ToString(iso8601Format), typeof(string)), base.GetQuotedValue(oracleFormat, typeof(string)));
        }

        internal string GetQuotedDateTimeValue(DateTime dateValue)
        {
            var iso8601Format = GetIsoDateTimeFormat(dateValue.TimeOfDay);
            var oracleFormat = GetOracleDateTimeFormat(dateValue.TimeOfDay);
            return string.Format("TO_TIMESTAMP({0}, {1})", base.GetQuotedValue(dateValue.ToString(iso8601Format), typeof(string)), base.GetQuotedValue(oracleFormat, typeof(string)));
        }

        private string GetIsoDateTimeFormat(TimeSpan timeOfDay)
        {
            return GetTimeFormat(timeOfDay, IsoDateFormat, IsoTimeFormat, IsoMillisecondFormat);
        }

        private string GetOracleDateTimeFormat(TimeSpan timeOfDay)
        {
            return GetTimeFormat(timeOfDay, OracleDateFormat, OracleTimeFormat, OracleMillisecondFormat);
        }

        private string GetTimeFormat(TimeSpan timeOfDay, string dateFormat, string timeFormat, string millisecondFormat)
        {
            var isStartOfDay = timeOfDay.Ticks == 0;
            if (isStartOfDay) return dateFormat;
            var hasFractionalSeconds = (timeOfDay.Milliseconds != 0) || ((timeOfDay.Ticks % TimeSpan.TicksPerMillisecond) != 0);
            return hasFractionalSeconds 
                ? $"{dateFormat} {timeFormat}.{millisecondFormat}"
                : $"{dateFormat} {timeFormat}";
        }

        public override bool IsFullSelectStatement(string sqlFilter)
        {
            const string selectStatement = "SELECT ";
            if (!string.IsNullOrEmpty(sqlFilter))
            {
                var cleanFilter = sqlFilter.Trim().Replace('\r', ' ').Replace('\n', ' ').ToUpperInvariant();
                return cleanFilter.Length > selectStatement.Length && cleanFilter.Substring(0, selectStatement.Length).Equals(selectStatement);
            }
            return false;
        }

        public override string ToSelectStatement(Type tableType, string sqlFilter, params object[] filterParams)
        {
            var sql = StringBuilderCache.Allocate();
            var modelDef = GetModel(tableType);

            if (IsFullSelectStatement(sqlFilter))
            {
                if (Regex.Matches(sqlFilter.Trim().ToUpperInvariant(), @"(\b|\n)FROM(\b|\n)").Count < 1)
                    sqlFilter += " FROM DUAL";
                return sqlFilter.SqlFmt(filterParams);
            }

            sql.AppendFormat("SELECT {0} FROM {1}",
                             GetColumnNames(modelDef),
                             GetQuotedTableName(modelDef));
            if (!string.IsNullOrEmpty(sqlFilter))
            {
                sqlFilter = sqlFilter.SqlFmt(filterParams);
                if (!sqlFilter.StartsWith("\nORDER ", StringComparison.OrdinalIgnoreCase)
                    && !sqlFilter.StartsWith("\nROWS ", StringComparison.OrdinalIgnoreCase)) // ROWS <m> [TO <n>])
                {
                    sql.Append("\nWHERE ");
                }
                sql.Append(sqlFilter);
            }
            return StringBuilderCache.ReturnAndFree(sql);
        }

        public override void PrepareParameterizedInsertStatement<T>(IDbCommand dbCommand, ICollection<string> insertFields = null, 
            Func<FieldDefinition,bool> shouldInclude=null)
        {
            var sbColumnNames = StringBuilderCache.Allocate();
            var sbColumnValues = StringBuilderCacheAlt.Allocate();
            var modelDef = GetModel(typeof(T));

            dbCommand.Parameters.Clear();
            dbCommand.CommandTimeout = OrmLiteConfig.CommandTimeout;

            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields);
            foreach (var fieldDef in fieldDefs)
            {
                if (((fieldDef.IsComputed && !fieldDef.IsPersisted) || fieldDef.IsRowVersion)
                    && shouldInclude?.Invoke(fieldDef) != true) 
                    continue;

                if (sbColumnNames.Length > 0) sbColumnNames.Append(",");
                if (sbColumnValues.Length > 0) sbColumnValues.Append(",");

                try
                {
                    sbColumnNames.Append(GetQuotedColumnName(fieldDef.FieldName));
                    sbColumnValues.Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName),fieldDef.CustomInsert));

                    AddParameter(dbCommand, fieldDef);
                }
                catch (Exception ex)
                {
                    Log.Error("ERROR in CreateParameterizedInsertStatement(): " + ex.Message, ex);
                    throw;
                }
            }

            dbCommand.CommandText = string.Format("INSERT INTO {0} ({1}) VALUES ({2})",
                GetQuotedTableName(modelDef), 
                StringBuilderCache.ReturnAndFree(sbColumnNames), 
                StringBuilderCacheAlt.ReturnAndFree(sbColumnValues));
        }

        public override void SetParameterValues<T>(IDbCommand dbCmd, object obj)
        {
            var modelDef = GetModel(typeof(T));
            var fieldMap = GetFieldDefinitionMap(modelDef);

            foreach (IDataParameter p in dbCmd.Parameters)
            {
                var fieldName = this.ToFieldName(p.ParameterName);
                fieldMap.TryGetValue(fieldName, out var fieldDef);

                if (fieldDef == null)
                    throw new ArgumentException("Field Definition '{0}' was not found".Fmt(fieldName));

                if (fieldDef.AutoIncrement || !string.IsNullOrEmpty(fieldDef.Sequence))
                {
                    if (fieldDef.AutoIncrement && string.IsNullOrEmpty(fieldDef.Sequence))
                    {
                        fieldDef.Sequence = Sequence(NamingStrategy.GetTableName(modelDef), fieldDef.FieldName, fieldDef.Sequence);
                    }

                    var pi = typeof(T).GetProperty(fieldDef.Name,
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                    //TODO fix this hack! Because of the way we handle sequences we have to know whether
                    // this is an insert or update/delete. If we did sequences with triggers this would
                    // not be a problem.
                    var sql = dbCmd.CommandText.TrimStart().ToUpperInvariant();
                    bool isInsert = sql.StartsWith("INSERT", StringComparison.InvariantCulture);

                    var result = GetNextValue(dbCmd, fieldDef.Sequence, pi.GetValue(obj, new object[] { }));
                    if (pi.PropertyType == typeof(String))
                        pi.SetProperty(obj, result.ToString());
                    else if (pi.PropertyType == typeof(Int16) || pi.PropertyType == typeof(Int16?))
                        pi.SetProperty(obj, Convert.ToInt16(result));
                    else if (pi.PropertyType == typeof(Int32) || pi.PropertyType == typeof(Int32?))
                        pi.SetProperty(obj, Convert.ToInt32(result));
                    else if (pi.PropertyType == typeof(Guid) || pi.PropertyType == typeof(Guid?))
                        pi.SetProperty(obj, result);
                    else
                        pi.SetProperty(obj, Convert.ToInt64(result));
                }

                SetParameterValue(fieldDef, p, obj);
            }
        }

        //TODO: Change to parameterized query to match all other ToInsertRowStatement() impls
        public override string ToInsertRowStatement(IDbCommand dbCommand, object objWithProperties, ICollection<string> insertFields = null)
        {
            var sbColumnNames = StringBuilderCache.Allocate();
            var sbColumnValues = StringBuilderCacheAlt.Allocate();

            var tableType = objWithProperties.GetType();
            var modelDef = GetModel(tableType);

            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields);
            foreach (var fieldDef in fieldDefs)
            {
                if ((fieldDef.IsComputed && !fieldDef.IsPersisted))
                    continue;

                if ((fieldDef.AutoIncrement || !string.IsNullOrEmpty(fieldDef.Sequence))
                    && dbCommand != null)
                {
                    if (fieldDef.AutoIncrement && string.IsNullOrEmpty(fieldDef.Sequence))
                    {
                        fieldDef.Sequence = Sequence(NamingStrategy.GetTableName(modelDef), fieldDef.FieldName, fieldDef.Sequence);
                    }

                    var pi = tableType.GetProperty(fieldDef.Name,
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                    var result = GetNextValue(dbCommand, fieldDef.Sequence, pi.GetValue(objWithProperties, new object[] { }));
                    if (pi.PropertyType == typeof(String))
                        pi.SetProperty(objWithProperties, result.ToString());
                    else if (pi.PropertyType == typeof(Int16))
                        pi.SetProperty(objWithProperties, Convert.ToInt16(result));
                    else if (pi.PropertyType == typeof(Int32))
                        pi.SetProperty(objWithProperties, Convert.ToInt32(result));
                    else if (pi.PropertyType == typeof(Guid))
                        pi.SetProperty(objWithProperties, result);
                    else
                        pi.SetProperty(objWithProperties, Convert.ToInt64(result));
                }

                if (sbColumnNames.Length > 0) sbColumnNames.Append(",");
                if (sbColumnValues.Length > 0) sbColumnValues.Append(",");

                try
                {
                    sbColumnNames.Append($"{GetQuotedColumnName(fieldDef.FieldName)}");
                    if (!string.IsNullOrEmpty(fieldDef.Sequence) && dbCommand == null)
                        sbColumnValues.Append($":{fieldDef.Name}");
                    else
                        sbColumnValues.Append(fieldDef.GetQuotedValue(objWithProperties));
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error in ToInsertRowStatement on column {0}: {1}", fieldDef.FieldName, ex);
                    throw;
                }
            }

            var sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2}) ",
                GetQuotedTableName(modelDef), 
                StringBuilderCache.ReturnAndFree(sbColumnNames), 
                StringBuilderCacheAlt.ReturnAndFree(sbColumnValues));

            return sql;
        }

        public override void PrepareUpdateRowStatement(IDbCommand dbCmd, object objWithProperties, ICollection<string> updateFields = null)
        {
            var sql = StringBuilderCache.Allocate();
            var sqlFilter = StringBuilderCacheAlt.Allocate();
            var tableType = objWithProperties.GetType();
            var modelDef = GetModel(tableType);

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if ((fieldDef.IsComputed && !fieldDef.IsPersisted)) 
                    continue;

                var updateFieldsEmptyOrNull = updateFields == null || updateFields.Count == 0;
                if ((fieldDef.IsPrimaryKey || fieldDef.Name == OrmLiteConfig.IdField)
                    && updateFieldsEmptyOrNull)
                {
                    if (sqlFilter.Length > 0)
                        sqlFilter.Append(" AND ");

                    sqlFilter
                        .Append(GetQuotedColumnName(fieldDef.FieldName))
                        .Append("=")
                        .Append(this.AddQueryParam(dbCmd, fieldDef.GetValue(objWithProperties), fieldDef).ParameterName);

                    continue;
                }

                if (!updateFieldsEmptyOrNull && !updateFields.Contains(fieldDef.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (sql.Length > 0)
                    sql.Append(",");

                sql
                    .Append(GetQuotedColumnName(fieldDef.FieldName))
                    .Append("=")
                    .Append(this.GetUpdateParam(dbCmd, fieldDef.GetValue(objWithProperties), fieldDef));
            }

            var strFilter = StringBuilderCacheAlt.ReturnAndFree(sqlFilter);
            dbCmd.CommandText = string.Format("UPDATE {0} \nSET {1} {2}",
                GetQuotedTableName(modelDef), 
                StringBuilderCache.ReturnAndFree(sql),
                strFilter.Length > 0 ? "\nWHERE " + strFilter : "");
        }

        public override string ToCreateTableStatement(Type tableType)
        {
            var sbColumns = StringBuilderCache.Allocate();
            var sbConstraints = StringBuilderCacheAlt.Allocate();

            var sbPk = new StringBuilder();

            var modelDef = GetModel(tableType);
            foreach (var fieldDef in CreateTableFieldsStrategy(modelDef))
            {
                if (fieldDef.CustomSelect != null || (fieldDef.IsComputed && !fieldDef.IsPersisted))
                    continue;

                if (fieldDef.IsPrimaryKey)
                {
                    sbPk.AppendFormat(sbPk.Length != 0 ? ",{0}" : "{0}", GetQuotedColumnName(fieldDef.FieldName));
                }

                if (sbColumns.Length != 0) sbColumns.Append(", \n  ");

                var columnDefinition = GetColumnDefinition(fieldDef);
                sbColumns.Append(columnDefinition);

                if (fieldDef.ForeignKey == null || OrmLiteConfig.SkipForeignKeys)
                    continue;

                var refModelDef = GetModel(fieldDef.ForeignKey.ReferenceType);
                sbConstraints.AppendFormat(
                    ", \n\n  CONSTRAINT {0} FOREIGN KEY ({1}) REFERENCES {2} ({3})",
                    GetQuotedName(fieldDef.ForeignKey.GetForeignKeyName(modelDef, refModelDef, NamingStrategy, fieldDef)),
                    GetQuotedColumnName(fieldDef.FieldName),
                    GetQuotedTableName(refModelDef),
                    GetQuotedColumnName(refModelDef.PrimaryKey.FieldName));

                sbConstraints.Append(GetForeignKeyOnDeleteClause(fieldDef.ForeignKey));
            }

            if (sbPk.Length != 0)
                sbColumns.AppendFormat(", \n  PRIMARY KEY({0})", sbPk);

            var uniqueConstraints = GetUniqueConstraints(modelDef);
            if (uniqueConstraints != null)
            {
                sbConstraints.Append(",\n" + uniqueConstraints);
            }

            var sql = string.Format(
                "CREATE TABLE {0} \n(\n  {1}{2} \n) \n", GetQuotedTableName(modelDef), 
                StringBuilderCache.ReturnAndFree(sbColumns), 
                StringBuilderCacheAlt.ReturnAndFree(sbConstraints));

            return sql;
        }

        public override string GetForeignKeyOnDeleteClause(ForeignKeyConstraint foreignKey)
        {
            if (string.IsNullOrEmpty(foreignKey.OnDelete)) return string.Empty;
            var onDelete = foreignKey.OnDelete.ToUpperInvariant();
            return (onDelete == "SET NULL" || onDelete == "CASCADE") ? " ON DELETE " + onDelete : string.Empty;
        }

        public override string GetLoadChildrenSubSelect<From>(SqlExpression<From> expr)
        {
            if (!expr.OrderByExpression.IsNullOrEmpty() && expr.Rows == null)
            {
                var modelDef = expr.ModelDef;
                expr.Select(this.GetQuotedColumnName(modelDef, modelDef.PrimaryKey))
                    .ClearLimits()
                    .OrderBy(""); //Invalid in Sub Selects

                var subSql = expr.ToSelectStatement();

                return subSql;
            }

            return base.GetLoadChildrenSubSelect(expr);
        }

        public override string ToCreateSequenceStatement(Type tableType, string sequenceName)
        {
            var result = "";
            var modelDef = GetModel(tableType);

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (fieldDef.AutoIncrement || !fieldDef.Sequence.IsNullOrEmpty())
                {
                    var seqName = Sequence(NamingStrategy.GetTableName(modelDef), fieldDef.FieldName, fieldDef.Sequence);
                    if (seqName.EqualsIgnoreCase(sequenceName))
                    {
                        result = "CREATE SEQUENCE " + GetQuotedName(seqName);
                        break;
                    }
                }
            }
            return result;
        }

        public override List<string> ToCreateSequenceStatements(Type tableType)
        {
            return SequenceList(tableType).Select(seq => "CREATE SEQUENCE " + GetQuotedName(seq)).ToList();
        }

        public override List<string> SequenceList(Type tableType)
        {
            var gens = new List<string>();
            var modelDef = GetModel(tableType);

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (fieldDef.AutoIncrement || !fieldDef.Sequence.IsNullOrEmpty())
                {
                    var seqName = Sequence(NamingStrategy.GetTableName(modelDef), fieldDef.FieldName, fieldDef.Sequence);

                    if (gens.IndexOf(seqName) == -1)
                        gens.Add(seqName);
                }
            }
            return gens;
        }

        public override string GetColumnDefinition(FieldDefinition fieldDef)
        {
            var fieldDefinition = ResolveFragment(fieldDef.CustomFieldDefinition) 
                ?? GetColumnTypeDefinition(fieldDef.FieldType, fieldDef.FieldLength, fieldDef.Scale);

            var sql = StringBuilderCache.Allocate();
            sql.AppendFormat("{0} {1}", GetQuotedColumnName(fieldDef.FieldName), fieldDefinition);

            var defaultValue = GetDefaultValue(fieldDef);
            if (fieldDef.IsRowVersion)
            {
                sql.AppendFormat(DefaultValueFormat, 1L);
            }
            else if (!string.IsNullOrEmpty(defaultValue))
            {
                sql.AppendFormat(DefaultValueFormat, defaultValue);
            }

            sql.Append(fieldDef.IsNullable ? " NULL" : " NOT NULL");

            if (fieldDef.IsUniqueConstraint)
            {
                sql.Append(" UNIQUE");
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

        public override List<string> ToCreateIndexStatements(Type tableType)
        {
            var sqlIndexes = new List<string>();

            var modelDef = GetModel(tableType);
            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (!fieldDef.IsIndexed) continue;

                var indexName = fieldDef.IndexName ?? GetIndexName(
                    fieldDef.IsUniqueIndex,
                    (modelDef.IsInSchema
                        ? modelDef.Schema + "_" + modelDef.ModelName
                        : modelDef.ModelName).SafeVarName(),
                    fieldDef.FieldName);
                indexName = NamingStrategy.ApplyNameRestrictions(indexName);

                sqlIndexes.Add(
                    ToCreateIndexStatement(fieldDef.IsUniqueIndex, indexName, modelDef, fieldDef.FieldName));
            }

            foreach (var compositeIndex in modelDef.CompositeIndexes)
            {
                var indexName = GetCompositeIndexNameWithSchema(compositeIndex, modelDef);
                indexName = NamingStrategy.ApplyNameRestrictions(indexName);
                var indexNames = string.Join(",", compositeIndex.FieldNames.ToArray());

                sqlIndexes.Add(
                    ToCreateIndexStatement(compositeIndex.Unique, indexName, modelDef, indexNames, isCombined:true));
            }

            return sqlIndexes;
        }

        protected override string ToCreateIndexStatement(bool isUnique, string indexName, ModelDefinition modelDef, string fieldName,
            bool isCombined = false, FieldDefinition fieldDef = null)
        {
            var unique = isUnique ? "UNIQUE" : "";
            var field = isCombined ? fieldName : GetQuotedColumnName(fieldName);
            return $"CREATE {unique} INDEX {indexName} ON {GetQuotedTableName(modelDef)} ({field}) \n";
        }

        public override string ToExistStatement(Type fromTableType,
            object objWithProperties,
            string sqlFilter,
            params object[] filterParams)
        {
            var fromModelDef = GetModel(fromTableType);
            var sql = StringBuilderCache.Allocate();
            sql.AppendFormat("SELECT 1 FROM {0}", GetQuotedTableName(fromModelDef));

            var filter = StringBuilderCacheAlt.Allocate();
            var hasFilter = false;

            if (objWithProperties != null)
            {
                var tableType = objWithProperties.GetType();

                if (fromTableType != tableType)
                {
                    var i = 0;
                    var modelDef = GetModel(tableType);

                    var fpk = modelDef.FieldDefinitions.Where(def => def.IsPrimaryKey).ToList();

                    foreach (var fieldDef in fromModelDef.FieldDefinitions)
                    {
                        if (fieldDef.IsComputed) 
                            continue;
                        
                        if (fieldDef.ForeignKey != null
                            && GetModel(fieldDef.ForeignKey.ReferenceType).ModelName == modelDef.ModelName)
                        {
                            if (filter.Length > 0) filter.Append(" AND ");
                            filter.AppendFormat("{0} = {1}", GetQuotedColumnName(fieldDef.FieldName),
                                fpk[i].GetQuotedValue(objWithProperties));
                            i++;
                        }
                    }
                }
                else
                {
                    var modelDef = GetModel(tableType);
                    foreach (var fieldDef in modelDef.FieldDefinitions)
                    {
                        if (fieldDef.IsComputed) 
                            continue;
                        
                        if (fieldDef.IsPrimaryKey)
                        {
                            if (filter.Length > 0) filter.Append(" AND ");
                            filter.AppendFormat("{0} = {1}",
                                GetQuotedColumnName(fieldDef.FieldName),
                                fieldDef.GetQuotedValue(objWithProperties));
                        }
                    }
                }

                hasFilter = filter.Length > 0;
                if (hasFilter)
                    sql.AppendFormat("\nWHERE {0} ", StringBuilderCacheAlt.ReturnAndFree(filter));
            }

            if (!string.IsNullOrEmpty(sqlFilter))
            {
                sqlFilter = sqlFilter.SqlFmt(filterParams);
                if (!sqlFilter.StartsWith("\nORDER ", StringComparison.OrdinalIgnoreCase)
                    && !sqlFilter.StartsWith("\nROWS ", StringComparison.OrdinalIgnoreCase)) // ROWS <m> [TO <n>])
                {
                    sql.Append(hasFilter ? " AND  " : "\nWHERE ");
                }
                sql.Append(sqlFilter);
            }

            var sb = StringBuilderCacheAlt.Allocate()
                .Append("select 1  from dual where")
                .AppendFormat(" exists ({0})", StringBuilderCache.ReturnAndFree(sql));
            return StringBuilderCacheAlt.ReturnAndFree(sb);
        }

        public override string ToSelectFromProcedureStatement(
            object fromObjWithProperties,
            Type outputModelType,
            string sqlFilter,
            params object[] filterParams)
        {

            var sbColumnValues = StringBuilderCache.Allocate();

            Type fromTableType = fromObjWithProperties.GetType();

            var modelDef = GetModel(fromTableType);

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (sbColumnValues.Length > 0) sbColumnValues.Append(",");

                sbColumnValues.Append(fieldDef.GetQuotedValue(fromObjWithProperties));
            }

            var columnValues = StringBuilderCache.ReturnAndFree(sbColumnValues);
            var sql = StringBuilderCache.Allocate();
            sql.AppendFormat("SELECT {0} FROM  {1} {2}{3}{4}  \n",
                GetColumnNames(GetModel(outputModelType)),
                GetQuotedTableName(modelDef),
                columnValues.Length > 0 ? "(" : "",
                columnValues,
                columnValues.Length > 0 ? ")" : "");

            if (!string.IsNullOrEmpty(sqlFilter))
            {
                sqlFilter = sqlFilter.SqlFmt(filterParams);
                if (!sqlFilter.StartsWith("\nORDER ", StringComparison.OrdinalIgnoreCase)
                    && !sqlFilter.StartsWith("\nROWS ", StringComparison.OrdinalIgnoreCase)) // ROWS <m> [TO <n>]
                {
                    sql.Append("\nWHERE ");
                }
                sql.Append(sqlFilter);
            }

            return StringBuilderCache.ReturnAndFree(sql);
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

            var columnValues = StringBuilderCache.ReturnAndFree(sbColumnValues);
            var sql = string.Format("EXECUTE PROCEDURE {0} {1}{2}{3};",
                GetQuotedTableName(modelDef),
                columnValues.Length > 0 ? "(" : "",
                columnValues,
                columnValues.Length > 0 ? ")" : "");

            return sql;
        }

        public override string GetLastInsertIdSqlSuffix<T>()
        {
            if (SelectIdentitySql == null)
                throw new NotImplementedException("Returning last inserted identity is not implemented on this DB Provider.");

            if (UseReturningForLastInsertId)
            {
                var modelDef = GetModel(typeof(T));
                var pkName = NamingStrategy.GetColumnName(modelDef.PrimaryKey.FieldName);
                return $" RETURNING {pkName} into " + pkName;
            }

            return "; " + SelectIdentitySql;
        }
        
        private object GetNextValue(IDbCommand dbCmd, string sequence, object value)
        {
            if (value.ToString() != "0")
            {
                object retObj;
                if (long.TryParse(value.ToString(), out var nv))
                {
                    LastInsertId = nv;
                    retObj = LastInsertId;
                }
                else
                {
                    LastInsertId = 0;
                    retObj = value;
                }
                return retObj;
            }
            //Get current CommandText
            var lastSql = dbCmd.CommandText;
            dbCmd.CommandText = $"SELECT {Quote(sequence)}.NEXTVAL FROM dual";
            long result = (long)dbCmd.LongScalar();
            LastInsertId = result;
            //Set CommandText back
            dbCmd.CommandText = lastSql;
            return result;
        }

        public bool QuoteNames { get; set; }

        private bool WillQuote(string name)
        {
            return QuoteNames || ReservedNames.Contains(name.ToUpper())
                              || name.Contains(" ");
        }

        private string Quote(string name)
        {
            return WillQuote(name) ? string.Format("\"{0}\"", name) : name;
        }

        public override string GetQuotedName(string name)
        {
            return Quote(name);
        }

        public override string GetQuotedTableName(ModelDefinition modelDef)
        {
            return Quote(NamingStrategy.GetTableName(modelDef));
        }

        public override string GetQuotedTableName(string tableName, string schema=null)
        {
            return schema == null 
                ? Quote(NamingStrategy.GetTableName(tableName))
                : Quote(NamingStrategy.GetSchemaName(schema))
                  + "."
                  + Quote(NamingStrategy.GetTableName(tableName));
        }

        public override string GetQuotedColumnName(string fieldName)
        {
            return Quote(NamingStrategy.GetColumnName(fieldName));
        }

        public override string SanitizeFieldNameForParamName(string fieldName)
        {
            var name = (fieldName ?? "").Replace(" ", "");
            if (ReservedParameterNames.Contains(name.ToUpper()))
            {
                name = "P_" + name;
            }
            if (name.Length > MaxNameLength)
            {
                name = name.Substring(0, MaxNameLength);
            }
            return name.TrimStart('_');
        }

        public virtual string Sequence(string modelName, string fieldName, string sequence)
        {
            //TODO used to return Quote(sequence)
            if (!sequence.IsNullOrEmpty()) return sequence;
            var seqName = NamingStrategy.GetSequenceName(modelName, fieldName);
            return seqName;
        }

        public override SqlExpression<T> SqlExpression<T>()
        {
            return new OracleSqlExpression<T>(this);
        }

        public override IDbDataParameter CreateParam()
        {
            return _factory.CreateParameter();
        }
        
        public override bool DoesSchemaExist(IDbCommand dbCmd, string schemaName)
        {
            dbCmd.CommandText = $"SELECT 1 FROM sys.schemas WHERE name = {schemaName.Quoted()}";
            var query = dbCmd.ExecuteNonQuery();
            return query == 1;
        }

        public override string ToCreateSchemaStatement(string schemaName)
        {
            var sql = $"CREATE SCHEMA {GetSchemaName(schemaName)}";
            return sql;
        }

        public override string ToAddColumnStatement(Type modelType, FieldDefinition fieldDef)
        {
            var command = base.ToAddColumnStatement(modelType, fieldDef);

            command = RemoveTerminatingSemicolon(command);

            return command.Replace("ADD COLUMN", "ADD");
        }

        private static string RemoveTerminatingSemicolon(string command)
        {
            command = command.Trim();

            if (command[command.Length - 1] == ';') command = command.Substring(0, command.Length - 1);

            return command;
        }

        protected override string ToDropColumnStatement(Type modelType, string columnName, IOrmLiteDialectProvider provider)
        {
            var command = base.ToDropColumnStatement(modelType, columnName, provider);

            return RemoveTerminatingSemicolon(command);
        }

        public override bool DoesTableExist(IDbCommand dbCmd, string tableName, string schema=null)
        {
            if (!WillQuote(tableName)) tableName = tableName.ToUpper();

            tableName = RemoveSchemaName(tableName);
            var sql = "SELECT count(*) FROM USER_TABLES WHERE TABLE_NAME = {0}".SqlFmt(tableName);

            if (schema != null)
                sql += " AND OWNER = {0}".SqlFmt(schema);

            dbCmd.CommandText = sql;
            var result = dbCmd.LongScalar();

            return result > 0;
        }

        private static string RemoveSchemaName(string tableName)
        {
            var indexOfPeriod = tableName.IndexOf(".", StringComparison.Ordinal);
            return indexOfPeriod < 0 ? tableName : tableName.Substring(indexOfPeriod + 1);
        }

        public override bool DoesColumnExist(IDbConnection db, string columnName, string tableName, string schema = null)
        {
            if (!WillQuote(tableName))
                tableName = tableName.ToUpper();

            columnName = columnName.ToUpper();
            tableName = RemoveSchemaName(tableName);
            var sql = "SELECT count(*) from all_tab_cols"
                    + " WHERE table_name = :tableName"
                    + "   AND upper(column_name) = :columnName";

            if (schema != null)
                sql += " AND OWNER = :schema";

            var result = db.SqlScalar<long>(sql, new { tableName, columnName, schema });

            return result > 0;
        }

        public override bool DoesSequenceExist(IDbCommand dbCmd, string sequenceName)
        {
            if (!WillQuote(sequenceName)) sequenceName = sequenceName.ToUpper();

            var sql = "SELECT count(*) FROM USER_SEQUENCES WHERE SEQUENCE_NAME = {0}".SqlFmt(sequenceName);
            dbCmd.CommandText = sql;
            var result = dbCmd.LongScalar();
            return result == 1;
        }

        public override string ToAddForeignKeyStatement<T, TForeign>(Expression<Func<T, object>> field,
                                                                    Expression<Func<TForeign, object>> foreignField,
                                                                    OnFkOption onUpdate,
                                                                    OnFkOption onDelete,
                                                                    string foreignKeyName = null)
        {
            var sourceMd = ModelDefinition<T>.Definition;
            var fieldName = sourceMd.GetFieldDefinition(field).FieldName;

            var referenceMd = ModelDefinition<TForeign>.Definition;
            var referenceFieldName = referenceMd.GetFieldDefinition(foreignField).FieldName;

            var name = GetQuotedName(foreignKeyName.IsNullOrEmpty()
                                     ? "fk_" + sourceMd.ModelName + "_" + fieldName + "_" + referenceFieldName
                                     : foreignKeyName);

            return string.Format("ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3} ({4}){5}",
                                 GetQuotedTableName(sourceMd.ModelName),
                                 name,
                                 GetQuotedColumnName(fieldName),
                                 GetQuotedTableName(referenceMd.ModelName),
                                 GetQuotedColumnName(referenceFieldName),
                                 GetForeignKeyOnDeleteClause(new ForeignKeyConstraint(typeof(T), FkOptionToString(onDelete))));
        }

        public override string EscapeWildcards(string value)
        {
            return value?.Replace("^", @"^^")
                .Replace("_", @"^_")
                .Replace("%", @"^%");
        }

        public override string ToSelectStatement(QueryType queryType, ModelDefinition modelDef,
            string selectExpression,
            string bodyExpression,
            string orderByExpression = null,
            int? offset = null,
            int? rows = null,
            ISet<string> tags=null)
        {
            var sbInner = StringBuilderCache.Allocate();
            ApplyTags(sbInner, tags);

            sbInner.Append(selectExpression);
            if (!bodyExpression.StartsWith(" ") && !bodyExpression.StartsWith("\n")
                && !selectExpression.EndsWith(" ") && !selectExpression.EndsWith("\n"))
            {
                sbInner.Append(" ");
            }
            sbInner.Append(bodyExpression);

            if (!rows.HasValue && !offset.HasValue)
                return StringBuilderCache.ReturnAndFree(sbInner) + " " + orderByExpression;

            if (!offset.HasValue)
                offset = 0;

            if (queryType == QueryType.Select && (offset.GetValueOrDefault() > 0 || rows.GetValueOrDefault() > 1) && orderByExpression.IsEmpty())
            {
                var primaryKey = modelDef.FieldDefinitions.FirstOrDefault(x => x.IsPrimaryKey);
                if (primaryKey == null)
                {
                    if (rows.Value == 1 && offset.Value == 0)
                    {
                        // Probably used Single<> extension method on a table with a composite key so let it through.
                        // Lack of an order by expression will mean it returns a random matching row, but that is OK.
                        orderByExpression = "";
                    }
                    else
                        throw new ApplicationException("Malformed model, no PrimaryKey defined");
                }
                else
                {
                    orderByExpression = $"ORDER BY {this.GetQuotedColumnName(modelDef, primaryKey.FieldName)}";
                }
            }
            sbInner.Append(" " + orderByExpression);

            var sql = StringBuilderCache.ReturnAndFree(sbInner);

            //TODO paging doesn't work with ORACLE because we are returning RNUM so we need to figure out a way to return just the desired columns
            var sb = StringBuilderCache.Allocate();
            sb.AppendLine("SELECT * FROM (");
            sb.AppendLine("SELECT \"_ss_ormlite_1_\".*, ROWNUM RNUM FROM (");
            sb.Append(sql);
            sb.AppendLine(") \"_ss_ormlite_1_\"");
            if (rows.HasValue)
                sb.AppendFormat("WHERE ROWNUM <= {0} + {1}) \"_ss_ormlite_2_\" ", offset.Value, rows.Value);
            else
                sb.Append(") \"_ss_ormlite_2_\" ");
            sb.AppendFormat("WHERE \"_ss_ormlite_2_\".RNUM > {0}", offset.Value);

            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override string ToRowCountStatement(string innerSql)
        {
            return "SELECT COUNT(*) FROM ({0})".Fmt(innerSql);
        }

        public override string SqlConcat(IEnumerable<object> args) => string.Join(" || ", args);

        public override string SqlRandom => "dbms_random.value";
        
        protected OracleConnection Unwrap(IDbConnection db)
        {
            return (OracleConnection)db.ToDbConnection();
        }

        protected OracleCommand Unwrap(IDbCommand cmd)
        {
            return (OracleCommand)cmd.ToDbCommand();
        }

        protected OracleDataReader Unwrap(IDataReader reader)
        {
            return (OracleDataReader)reader;
        }
    }
}
