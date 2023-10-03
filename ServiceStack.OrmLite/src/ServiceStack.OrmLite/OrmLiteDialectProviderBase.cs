//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Converters;
using ServiceStack.Text;
using ServiceStack.Script;

namespace ServiceStack.OrmLite
{
    public abstract class OrmLiteDialectProviderBase<TDialect>
        : IOrmLiteDialectProvider
        where TDialect : IOrmLiteDialectProvider
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(IOrmLiteDialectProvider));

        protected OrmLiteDialectProviderBase()
        {
            Variables = new Dictionary<string, string>();
            StringSerializer = new JsvStringSerializer();
        }

        #region ADO.NET supported types
        /* ADO.NET UNDERSTOOD DATA TYPES:
			COUNTER	DbType.Int64
			AUTOINCREMENT	DbType.Int64
			IDENTITY	DbType.Int64
			LONG	DbType.Int64
			TINYINT	DbType.Byte
			INTEGER	DbType.Int64
			INT	DbType.Int32
			VARCHAR	DbType.String
			NVARCHAR	DbType.String
			CHAR	DbType.String
			NCHAR	DbType.String
			TEXT	DbType.String
			NTEXT	DbType.String
			STRING	DbType.String
			DOUBLE	DbType.Double
			FLOAT	DbType.Double
			REAL	DbType.Single
			BIT	DbType.Boolean
			YESNO	DbType.Boolean
			LOGICAL	DbType.Boolean
			BOOL	DbType.Boolean
			NUMERIC	DbType.Decimal
			DECIMAL	DbType.Decimal
			MONEY	DbType.Decimal
			CURRENCY	DbType.Decimal
			TIME	DbType.DateTime
			DATE	DbType.DateTime
			TIMESTAMP	DbType.DateTime
			DATETIME	DbType.DateTime
			BLOB	DbType.Binary
			BINARY	DbType.Binary
			VARBINARY	DbType.Binary
			IMAGE	DbType.Binary
			GENERAL	DbType.Binary
			OLEOBJECT	DbType.Binary
			GUID	DbType.Guid
			UNIQUEIDENTIFIER	DbType.Guid
			MEMO	DbType.String
			NOTE	DbType.String
			LONGTEXT	DbType.String
			LONGCHAR	DbType.String
			SMALLINT	DbType.Int16
			BIGINT	DbType.Int64
			LONGVARCHAR	DbType.String
			SMALLDATE	DbType.DateTime
			SMALLDATETIME	DbType.DateTime
		 */
        #endregion

        protected void InitColumnTypeMap()
        {
            EnumConverter = new EnumConverter();
            RowVersionConverter = new RowVersionConverter();
            ReferenceTypeConverter = new ReferenceTypeConverter();
            ValueTypeConverter = new ValueTypeConverter();

            RegisterConverter<string>(new StringConverter());
            RegisterConverter<char>(new CharConverter());
            RegisterConverter<char[]>(new CharArrayConverter());
            RegisterConverter<byte[]>(new ByteArrayConverter());

            RegisterConverter<byte>(new ByteConverter());
            RegisterConverter<sbyte>(new SByteConverter());
            RegisterConverter<short>(new Int16Converter());
            RegisterConverter<ushort>(new UInt16Converter());
            RegisterConverter<int>(new Int32Converter());
            RegisterConverter<uint>(new UInt32Converter());
            RegisterConverter<long>(new Int64Converter());
            RegisterConverter<ulong>(new UInt64Converter());

            RegisterConverter<ulong>(new UInt64Converter());

            RegisterConverter<float>(new FloatConverter());
            RegisterConverter<double>(new DoubleConverter());
            RegisterConverter<decimal>(new DecimalConverter());

            RegisterConverter<Guid>(new GuidConverter());
            RegisterConverter<TimeSpan>(new TimeSpanAsIntConverter());
            RegisterConverter<DateTime>(new DateTimeConverter());
            RegisterConverter<DateTimeOffset>(new DateTimeOffsetConverter());

#if NET6_0
            RegisterConverter<DateOnly>(new DateOnlyConverter());
            RegisterConverter<TimeOnly>(new TimeOnlyConverter());
#endif
        }

        public string GetColumnTypeDefinition(Type columnType, int? fieldLength, int? scale)
        {
            var converter = GetConverter(columnType);
            if (converter != null)
            {
                if (converter is IHasColumnDefinitionPrecision customPrecisionConverter)
                    return customPrecisionConverter.GetColumnDefinition(fieldLength, scale);

                if (converter is IHasColumnDefinitionLength customLengthConverter)
                    return customLengthConverter.GetColumnDefinition(fieldLength);

                if (string.IsNullOrEmpty(converter.ColumnDefinition))
                    throw new ArgumentException($"{converter.GetType().Name} requires a ColumnDefinition");

                return converter.ColumnDefinition;
            }

            var stringConverter = columnType.IsRefType()
                ? ReferenceTypeConverter
                : columnType.IsEnum
                    ? EnumConverter
                    : (IHasColumnDefinitionLength)ValueTypeConverter;

            return stringConverter.GetColumnDefinition(fieldLength);
        }

        public virtual void InitDbParam(IDbDataParameter dbParam, Type columnType)
        {
            var converter = GetConverterBestMatch(columnType);
            converter.InitDbParam(dbParam, columnType);
        }

        public abstract IDbDataParameter CreateParam();

        public Dictionary<string, string> Variables { get; set; }

        public IOrmLiteExecFilter ExecFilter { get; set; }

        public Dictionary<Type, IOrmLiteConverter> Converters = new();

        public string AutoIncrementDefinition = "AUTOINCREMENT"; //SqlServer express limit

        public DecimalConverter DecimalConverter => (DecimalConverter)Converters[typeof(decimal)];

        public StringConverter StringConverter => (StringConverter)Converters[typeof(string)];

        public Action<IDbConnection> OnOpenConnection { get; set; }

        internal int OneTimeConnectionCommandsRun;

        /// <summary>
        /// Enable Bulk Inserts from CSV files
        /// </summary>
        public bool AllowLoadLocalInfile
        {
            set => OneTimeConnectionCommands.Add($"SET GLOBAL LOCAL_INFILE={value.ToString().ToUpper()};");
        }
        
        public List<string> OneTimeConnectionCommands { get; } = new();
        public List<string> ConnectionCommands { get; } = new();

        public string ParamString { get; set; } = "@";

        public INamingStrategy NamingStrategy { get; set; } = new OrmLiteDefaultNamingStrategy();

        public IStringSerializer StringSerializer { get; set; }

        private Func<string, string> paramNameFilter;
        public Func<string, string> ParamNameFilter
        {
            get => paramNameFilter ?? OrmLiteConfig.ParamNameFilter;
            set => paramNameFilter = value;
        }
        
        public virtual bool SupportsSchema => true;

        public string DefaultValueFormat = " DEFAULT ({0})";

        private EnumConverter enumConverter;
        public EnumConverter EnumConverter
        {
            get => enumConverter;
            set
            {
                value.DialectProvider = this;
                enumConverter = value;
            }
        }

        private RowVersionConverter rowVersionConverter;
        public RowVersionConverter RowVersionConverter
        {
            get => rowVersionConverter;
            set
            {
                value.DialectProvider = this;
                rowVersionConverter = value;
            }
        }

        private ReferenceTypeConverter referenceTypeConverter;
        public ReferenceTypeConverter ReferenceTypeConverter
        {
            get => referenceTypeConverter;
            set
            {
                value.DialectProvider = this;
                referenceTypeConverter = value;
            }
        }

        private ValueTypeConverter valueTypeConverter;
        public ValueTypeConverter ValueTypeConverter
        {
            get => valueTypeConverter;
            set
            {
                value.DialectProvider = this;
                valueTypeConverter = value;
            }
        }

        public void RemoveConverter<T>()
        {
            if (Converters.TryRemove(typeof(T), out var converter))
                converter.DialectProvider = null;
        }

        public virtual void Init(string connectionString) {}

        public void RegisterConverter<T>(IOrmLiteConverter converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            converter.DialectProvider = this;
            Converters[typeof(T)] = converter;
        }

        public IOrmLiteConverter GetConverter(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return Converters.TryGetValue(type, out IOrmLiteConverter converter)
                ? converter
                : null;
        }

        public virtual bool ShouldQuoteValue(Type fieldType)
        {
            var converter = GetConverter(fieldType);
            return converter == null || converter is NativeValueOrmLiteConverter;
        }

		public virtual object FromDbRowVersion(Type fieldType, object value)
		{
			return RowVersionConverter.FromDbValue(fieldType, value);
		}

		public IOrmLiteConverter GetConverterBestMatch(Type type)
		{
		    if (type == typeof(RowVersionConverter))
		        return RowVersionConverter;
            
            var converter = GetConverter(type);
            if (converter != null)
                return converter;

            if (type.IsEnum)
                return EnumConverter;

            return type.IsRefType()
                ? (IOrmLiteConverter)ReferenceTypeConverter
                : ValueTypeConverter;
        }

        public virtual IOrmLiteConverter GetConverterBestMatch(FieldDefinition fieldDef)
        {
            var fieldType = Nullable.GetUnderlyingType(fieldDef.FieldType) ?? fieldDef.FieldType;

            if (fieldDef.IsRowVersion)
                return RowVersionConverter;

            if (Converters.TryGetValue(fieldType, out var converter))
                return converter;

            if (fieldType.IsEnum)
                return EnumConverter;

            return fieldType.IsRefType()
                ? (IOrmLiteConverter)ReferenceTypeConverter
                : ValueTypeConverter;
        }

        public virtual object ToDbValue(object value, Type type)
        {
            if (value == null || value is DBNull)
                return null;

            var converter = GetConverterBestMatch(type);
            try
            {
                return converter.ToDbValue(type, value);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in {converter.GetType().Name}.ToDbValue() value '{value.GetType().Name}' and Type '{type.Name}'", ex);
                throw;
            }
        }

        public virtual object FromDbValue(object value, Type type)
        {
            if (value == null || value is DBNull)
                return null;

            var converter = GetConverterBestMatch(type);
            try
            {
                return converter.FromDbValue(type, value);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in {converter.GetType().Name}.FromDbValue() value '{value.GetType().Name}' and Type '{type.Name}'", ex);
                throw;
            }
        }

        public object GetValue(IDataReader reader, int columnIndex, Type type)
        {
            if (Converters.TryGetValue(type, out var converter))
                return converter.GetValue(reader, columnIndex, null);

            return reader.GetValue(columnIndex);
        }

        public virtual int GetValues(IDataReader reader, object[] values)
        {
            return reader.GetValues(values);
        }

        public abstract IDbConnection CreateConnection(string filePath, Dictionary<string, string> options);

        public virtual string GetQuotedValue(string paramValue)
        {
            return "'" + paramValue.Replace("'", "''") + "'";
        }

        public virtual string GetSchemaName(string schema)
        {
            return NamingStrategy.GetSchemaName(schema);
        }

        public virtual string GetTableName(Type modelType) => GetTableName(modelType.GetModelDefinition());

        public virtual string GetTableName(ModelDefinition modelDef) => 
            GetTableName(modelDef.ModelName, modelDef.Schema, useStrategy:true);

        public virtual string GetTableName(ModelDefinition modelDef, bool useStrategy) => 
            GetTableName(modelDef.ModelName, modelDef.Schema, useStrategy);

        public virtual string GetTableName(string table, string schema = null) =>
            GetTableName(table, schema, useStrategy: true);

        public virtual string GetTableName(string table, string schema, bool useStrategy)
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

        public virtual string GetQuotedTableName(Type modelType) => GetQuotedTableName(modelType.GetModelDefinition());
        
        public virtual string GetQuotedTableName(ModelDefinition modelDef)
        {
            return GetQuotedTableName(modelDef.ModelName, modelDef.Schema);
        }

        public virtual string GetQuotedTableName(string tableName, string schema = null)
        {
            if (schema == null)
                return GetQuotedName(NamingStrategy.GetTableName(tableName));

            var escapedSchema = NamingStrategy.GetSchemaName(schema)
                .Replace(".", "\".\"");

            return $"{GetQuotedName(escapedSchema)}.{GetQuotedName(NamingStrategy.GetTableName(tableName))}";
        }

        public virtual string GetQuotedTableName(string tableName, string schema, bool useStrategy) => 
            GetQuotedName(GetTableName(tableName, schema, useStrategy));

        public virtual string GetQuotedColumnName(string columnName)
        {
            return GetQuotedName(NamingStrategy.GetColumnName(columnName));
        }

        public virtual bool ShouldQuote(string name) => !string.IsNullOrEmpty(name) && 
            (name.IndexOf(' ') >= 0 || name.IndexOf('.') >= 0);

        public virtual string QuoteIfRequired(string name)
        {
            return ShouldQuote(name)
                ? GetQuotedName(name)
                : name;
        }

        public virtual string GetQuotedName(string name) => name == null ? null : name.FirstCharEquals('"') 
            ? name : '"' + name + '"';

        public virtual string GetQuotedName(string name, string schema)
        {
            return schema != null
                ? $"{GetQuotedName(schema)}.{GetQuotedName(name)}"
                : GetQuotedName(name);
        }

        public virtual string SanitizeFieldNameForParamName(string fieldName)
        {
            return OrmLiteConfig.SanitizeFieldNameForParamNameFn(fieldName);
        }

        public virtual string GetColumnDefinition(FieldDefinition fieldDef)
        {
            var fieldDefinition = ResolveFragment(fieldDef.CustomFieldDefinition) ?? 
                GetColumnTypeDefinition(fieldDef.ColumnType, fieldDef.FieldLength, fieldDef.Scale);

            var sql = StringBuilderCache.Allocate();
            sql.Append($"{GetQuotedColumnName(fieldDef.FieldName)} {fieldDefinition}");

            if (fieldDef.IsPrimaryKey)
            {
                sql.Append(" PRIMARY KEY");
                if (fieldDef.AutoIncrement)
                {
                    sql.Append(" ").Append(AutoIncrementDefinition);
                }
            }
            else
            {
                sql.Append(fieldDef.IsNullable ? " NULL" : " NOT NULL");
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

            return StringBuilderCache.ReturnAndFree(sql);
        }

        public virtual string SelectIdentitySql { get; set; }

        public virtual long GetLastInsertId(IDbCommand dbCmd)
        {
            if (SelectIdentitySql == null)
                throw new NotImplementedException("Returning last inserted identity is not implemented on this DB Provider.");

            dbCmd.CommandText = SelectIdentitySql;
            return dbCmd.ExecLongScalar();
        }

        public virtual string GetLastInsertIdSqlSuffix<T>()
        {
            if (SelectIdentitySql == null)
                throw new NotImplementedException("Returning last inserted identity is not implemented on this DB Provider.");

            return "; " + SelectIdentitySql;
        }
        
        public virtual bool IsFullSelectStatement(string sql) => !string.IsNullOrEmpty(sql)
            && sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

        // Fmt
        public virtual string ToSelectStatement(Type tableType, string sqlFilter, params object[] filterParams)
        {
            if (IsFullSelectStatement(sqlFilter))
                return sqlFilter.SqlFmt(this, filterParams);

            var modelDef = tableType.GetModelDefinition();
            var sql = StringBuilderCache.Allocate();
            sql.Append($"SELECT {GetColumnNames(modelDef)} FROM {GetQuotedTableName(modelDef)}");

            if (string.IsNullOrEmpty(sqlFilter))
                return StringBuilderCache.ReturnAndFree(sql);

            sqlFilter = sqlFilter.SqlFmt(this, filterParams);
            if (!sqlFilter.StartsWith("ORDER ", StringComparison.OrdinalIgnoreCase)
                && !sqlFilter.StartsWith("LIMIT ", StringComparison.OrdinalIgnoreCase))
            {
                sql.Append(" WHERE ");
            }

            sql.Append(sqlFilter);

            return StringBuilderCache.ReturnAndFree(sql);
        }

        protected virtual void ApplyTags(StringBuilder sqlBuilder, ISet<string> tags)
        {
            if (tags is { Count: > 0 })
            {
                foreach (var tag in tags)
                {
                    sqlBuilder.AppendLine(GenerateComment(tag));
                }
                sqlBuilder.Append("\n");
            }
        }

        public virtual string ToSelectStatement(
            QueryType queryType, 
            ModelDefinition modelDef,
            string selectExpression,
            string bodyExpression,
            string orderByExpression = null,
            int? offset = null,
            int? rows = null,
            ISet<string> tags = null)
        {
            var sb = StringBuilderCache.Allocate();

            ApplyTags(sb, tags);

            sb.Append(selectExpression);
            sb.Append(bodyExpression);
            if (!string.IsNullOrEmpty(orderByExpression))
            {
                sb.Append(orderByExpression);
            }

            if ((queryType == QueryType.Select || (rows == 1 && offset is null or 0)) && (offset != null || rows != null))
            {
                sb.Append("\n");
                sb.Append(SqlLimit(offset, rows));
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }

        public virtual string GenerateComment(in string text)
        {
            return $"-- {text}";
        }

        public virtual void InitConnection(IDbConnection dbConn)
        {
            if (dbConn is OrmLiteConnection ormLiteConn)
                ormLiteConn.ConnectionId = Guid.NewGuid();

            if (Interlocked.CompareExchange(ref OneTimeConnectionCommandsRun, 1, 0) == 0)
            {
                foreach (var command in OneTimeConnectionCommands)
                {
                    using var cmd = dbConn.CreateCommand();
                    cmd.ExecNonQuery(command);
                }
            }
            
            foreach (var command in ConnectionCommands)
            {
                using var cmd = dbConn.CreateCommand();
                cmd.ExecNonQuery(command);
            }
            
            OnOpenConnection?.Invoke(dbConn);
        }

        public virtual SelectItem GetRowVersionSelectColumn(FieldDefinition field, string tablePrefix = null)
        {
            return new SelectItemColumn(this, field.FieldName, null, tablePrefix);
        }

        public virtual string GetRowVersionColumn(FieldDefinition field, string tablePrefix = null)
        {
            return GetRowVersionSelectColumn(field, tablePrefix).ToString();
        }
        
        public virtual string GetColumnNames(ModelDefinition modelDef)
        {
            return GetColumnNames(modelDef, null).ToSelectString();
        }

        public virtual SelectItem[] GetColumnNames(ModelDefinition modelDef, string tablePrefix)
        {
            var quotedPrefix = tablePrefix != null 
                ? GetQuotedTableName(tablePrefix, modelDef.Schema) 
                : "";

            var sqlColumns = new SelectItem[modelDef.FieldDefinitions.Count];
            for (var i = 0; i < sqlColumns.Length; ++i)
            {
                var field = modelDef.FieldDefinitions[i];

                if (field.CustomSelect != null)
                {
                    sqlColumns[i] = new SelectItemExpression(this, field.CustomSelect, field.FieldName);
                }
                else if (field.IsRowVersion)
                {
                    sqlColumns[i] = GetRowVersionSelectColumn(field, quotedPrefix);
                }
                else
                {
                    sqlColumns[i] = new SelectItemColumn(this, field.FieldName, null, quotedPrefix);
                }
            }

            return sqlColumns;
        }

        protected virtual bool ShouldSkipInsert(FieldDefinition fieldDef) => 
            fieldDef.ShouldSkipInsert();

        public virtual string ColumnNameOnly(string columnExpr)
        {
            var nameOnly = columnExpr.LastRightPart('.');
            var ret = nameOnly.StripDbQuotes();
            return ret;
        }

        public virtual FieldDefinition[] GetInsertFieldDefinitions(ModelDefinition modelDef, ICollection<string> insertFields=null)
        {
            var insertColumns = insertFields?.Map(ColumnNameOnly);
            return insertColumns != null 
                ? NamingStrategy.GetType() == typeof(OrmLiteDefaultNamingStrategy) 
                    ? modelDef.GetOrderedFieldDefinitions(insertColumns)
                    : modelDef.GetOrderedFieldDefinitions(insertColumns, name => NamingStrategy.GetColumnName(name)) 
                : modelDef.FieldDefinitionsArray;
        }

        public virtual void AppendInsertRowValueSql(StringBuilder sbColumnValues, FieldDefinition fieldDef, object obj)
        {
            if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                return;

            try
            {
                if (fieldDef.AutoId)
                {
                    var dbValue = GetInsertDefaultValue(fieldDef);
                    sbColumnValues.Append(dbValue != null ? GetQuotedValue(dbValue.ToString()) : "NULL");
                }
                else
                {
                    sbColumnValues.Append(GetQuotedValue(fieldDef.GetValue(obj), fieldDef.FieldType));
                }
            }
            catch (Exception ex)
            {
                Log.Error("ERROR in ToInsertRowStatement(): " + ex.Message, ex);
                throw;
            }
        }
        
        public virtual string ToInsertRowSql<T>(T obj, ICollection<string> insertFields = null)
        {
            var sbColumnNames = StringBuilderCache.Allocate();
            var sbColumnValues = StringBuilderCacheAlt.Allocate();
            var modelDef = obj.GetType().GetModelDefinition();

            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields);
            foreach (var fieldDef in fieldDefs)
            {
                if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                    continue;

                if (sbColumnNames.Length > 0)
                    sbColumnNames.Append(",");

                sbColumnNames.Append(GetQuotedColumnName(fieldDef.FieldName));

                if (sbColumnValues.Length > 0)
                    sbColumnValues.Append(",");

                AppendInsertRowValueSql(sbColumnValues, fieldDef, obj);
            }

            var sql = $"INSERT INTO {GetQuotedTableName(modelDef)} ({StringBuilderCache.ReturnAndFree(sbColumnNames)}) " +
                      $"VALUES ({StringBuilderCacheAlt.ReturnAndFree(sbColumnValues)})";

            return sql;
        }

        public virtual string ToInsertRowsSql<T>(IEnumerable<T> objs, ICollection<string> insertFields = null)
        {
            var modelDef = ModelDefinition<T>.Definition;
            var sb = StringBuilderCache.Allocate()
                .Append($"INSERT INTO {GetQuotedTableName(modelDef)} (");

            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields:insertFields);
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

        public virtual void BulkInsert<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null)
        {
            config ??= new();
            foreach (var batch in objs.BatchesOf(config.BatchSize))
            {
                var sql = ToInsertRowsSql(batch, insertFields:config.InsertFields);
                db.ExecuteSql(sql);
            }
        }

        public virtual string ToInsertRowStatement(IDbCommand cmd, object objWithProperties, ICollection<string> insertFields = null)
        {
            var sbColumnNames = StringBuilderCache.Allocate();
            var sbColumnValues = StringBuilderCacheAlt.Allocate();
            var modelDef = objWithProperties.GetType().GetModelDefinition();

            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields);
            foreach (var fieldDef in fieldDefs)
            {
                if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                    continue;

                if (sbColumnNames.Length > 0)
                    sbColumnNames.Append(",");
                if (sbColumnValues.Length > 0)
                    sbColumnValues.Append(",");

                try
                {
                    sbColumnNames.Append(GetQuotedColumnName(fieldDef.FieldName));
                    sbColumnValues.Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName)));

                    AddParameter(cmd, fieldDef);
                }
                catch (Exception ex)
                {
                    Log.Error("ERROR in ToInsertRowStatement(): " + ex.Message, ex);
                    throw;
                }
            }

            var sql = $"INSERT INTO {GetQuotedTableName(modelDef)} ({StringBuilderCache.ReturnAndFree(sbColumnNames)}) " +
                      $"VALUES ({StringBuilderCacheAlt.ReturnAndFree(sbColumnValues)})";

            return sql;
        }

        public virtual string ToInsertStatement<T>(IDbCommand dbCmd, T item, ICollection<string> insertFields = null)
        {
            dbCmd.Parameters.Clear();
            var dialectProvider = dbCmd.GetDialectProvider();
            dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd, insertFields);

            if (string.IsNullOrEmpty(dbCmd.CommandText))
                return null;

            dialectProvider.SetParameterValues<T>(dbCmd, item);

            return MergeParamsIntoSql(dbCmd.CommandText, ToArray(dbCmd.Parameters));
        }

        protected virtual object GetInsertDefaultValue(FieldDefinition fieldDef)
        {
            if (!fieldDef.AutoId)
                return null;
            if (fieldDef.FieldType == typeof(Guid))
                return Guid.NewGuid();
            return null;
        }

        public virtual void PrepareParameterizedInsertStatement<T>(IDbCommand cmd, ICollection<string> insertFields = null, 
            Func<FieldDefinition,bool> shouldInclude=null)
        {
            var sbColumnNames = StringBuilderCache.Allocate();
            var sbColumnValues = StringBuilderCacheAlt.Allocate();
            var modelDef = typeof(T).GetModelDefinition();

            cmd.Parameters.Clear();

            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields);
            foreach (var fieldDef in fieldDefs)
            {
                if (fieldDef.ShouldSkipInsert() && shouldInclude?.Invoke(fieldDef) != true)
                    continue;

                if (sbColumnNames.Length > 0)
                    sbColumnNames.Append(",");
                if (sbColumnValues.Length > 0)
                    sbColumnValues.Append(",");

                try
                {
                    sbColumnNames.Append(GetQuotedColumnName(fieldDef.FieldName));
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

            cmd.CommandText = $"INSERT INTO {GetQuotedTableName(modelDef)} ({StringBuilderCache.ReturnAndFree(sbColumnNames)}) " +
                              $"VALUES ({StringBuilderCacheAlt.ReturnAndFree(sbColumnValues)})";
        }

        public virtual void PrepareInsertRowStatement<T>(IDbCommand dbCmd, Dictionary<string, object> args)
        {
            var sbColumnNames = StringBuilderCache.Allocate();
            var sbColumnValues = StringBuilderCacheAlt.Allocate();
            var modelDef = typeof(T).GetModelDefinition();

            dbCmd.Parameters.Clear();

            foreach (var entry in args)
            {
                var fieldDef = modelDef.AssertFieldDefinition(entry.Key);
                if (fieldDef.ShouldSkipInsert())
                    continue;

                var value = entry.Value;

                if (sbColumnNames.Length > 0)
                    sbColumnNames.Append(",");
                if (sbColumnValues.Length > 0)
                    sbColumnValues.Append(",");

                try
                {
                    sbColumnNames.Append(GetQuotedColumnName(fieldDef.FieldName));
                    sbColumnValues.Append(this.GetInsertParam(dbCmd, value, fieldDef));
                }
                catch (Exception ex)
                {
                    Log.Error("ERROR in PrepareInsertRowStatement(): " + ex.Message, ex);
                    throw;
                }
            }

            dbCmd.CommandText = $"INSERT INTO {GetQuotedTableName(modelDef)} ({StringBuilderCache.ReturnAndFree(sbColumnNames)}) " +
                                $"VALUES ({StringBuilderCacheAlt.ReturnAndFree(sbColumnValues)})";
        }

        public virtual string ToUpdateStatement<T>(IDbCommand dbCmd, T item, ICollection<string> updateFields = null)
        {
            dbCmd.Parameters.Clear();
            var dialectProvider = dbCmd.GetDialectProvider();
            dialectProvider.PrepareParameterizedUpdateStatement<T>(dbCmd);

            if (string.IsNullOrEmpty(dbCmd.CommandText))
                return null;

            dialectProvider.SetParameterValues<T>(dbCmd, item);

            return MergeParamsIntoSql(dbCmd.CommandText, ToArray(dbCmd.Parameters));
        }

        IDbDataParameter[] ToArray(IDataParameterCollection dbParams)
        {
            var to = new IDbDataParameter[dbParams.Count];
            for (int i = 0; i < dbParams.Count; i++)
            {
                to[i] = (IDbDataParameter)dbParams[i];
            }
            return to;
        }

        public virtual string MergeParamsIntoSql(string sql, IEnumerable<IDbDataParameter> dbParams)
        {
            foreach (var dbParam in dbParams)
            {
                var quotedValue = dbParam.Value != null
                    ? GetQuotedValue(dbParam.Value, dbParam.Value.GetType())
                    : "null";

                var pattern = dbParam.ParameterName + @"(,|\s|\)|$)";
                var replacement = quotedValue.Replace("$", "$$") + "$1";
                sql = Regex.Replace(sql, pattern, replacement);
            }
            return sql;
        }

        //Load Self Table.RefTableId PK
        public virtual string GetRefSelfSql<From>(SqlExpression<From> refQ, ModelDefinition modelDef, FieldDefinition refSelf, ModelDefinition refModelDef)
        {
            refQ.Select(this.GetQuotedColumnName(modelDef, refSelf));
            refQ.OrderBy().ClearLimits(); //clear any ORDER BY or LIMIT's in Sub Select's

            var subSqlRef = refQ.ToMergedParamsSelectStatement();

            var sqlRef = $"SELECT {GetColumnNames(refModelDef)} " +
                         $"FROM {GetQuotedTableName(refModelDef)} " +
                         $"WHERE {this.GetQuotedColumnName(refModelDef.PrimaryKey)} " +
                         $"IN ({subSqlRef})";

            if (OrmLiteConfig.LoadReferenceSelectFilter != null)
                sqlRef = OrmLiteConfig.LoadReferenceSelectFilter(refModelDef.ModelType, sqlRef);

            return sqlRef;
        }

        public virtual string GetRefFieldSql(string subSql, ModelDefinition refModelDef, FieldDefinition refField)
        {
            var sqlRef = $"SELECT {GetColumnNames(refModelDef)} " +
                         $"FROM {GetQuotedTableName(refModelDef)} " +
                         $"WHERE {this.GetQuotedColumnName(refField)} " +
                         $"IN ({subSql})";

            if (OrmLiteConfig.LoadReferenceSelectFilter != null)
                sqlRef = OrmLiteConfig.LoadReferenceSelectFilter(refModelDef.ModelType, sqlRef);

            return sqlRef;
        }

        public virtual string GetFieldReferenceSql(string subSql, FieldDefinition fieldDef, FieldReference fieldRef)
        {
            var refModelDef = fieldRef.RefModelDef;
            
            var useSubSql = $"SELECT {this.GetQuotedColumnName(fieldRef.RefIdFieldDef)} FROM "
                + subSql.RightPart("FROM");

            var pk = this.GetQuotedColumnName(refModelDef.PrimaryKey);
            var sqlRef = $"SELECT {pk}, {this.GetQuotedColumnName(fieldRef.RefFieldDef)} " +
                         $"FROM {GetQuotedTableName(refModelDef)} " +
                         $"WHERE {pk} " +
                         $"IN ({useSubSql})";

            if (OrmLiteConfig.LoadReferenceSelectFilter != null)
                sqlRef = OrmLiteConfig.LoadReferenceSelectFilter(refModelDef.ModelType, sqlRef);

            return sqlRef;
        }

        public virtual bool PrepareParameterizedUpdateStatement<T>(IDbCommand cmd, ICollection<string> updateFields = null)
        {
            var sql = StringBuilderCache.Allocate();
            var sqlFilter = StringBuilderCacheAlt.Allocate();
            var modelDef = typeof(T).GetModelDefinition();
            var hadRowVersion = false;
            var updateAllFields = updateFields == null || updateFields.Count == 0;

            cmd.Parameters.Clear();

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (fieldDef.ShouldSkipUpdate())
                    continue;

                try
                {
                    if ((fieldDef.IsPrimaryKey || fieldDef.IsRowVersion) && updateAllFields)
                    {
                        if (sqlFilter.Length > 0)
                            sqlFilter.Append(" AND ");

                        AppendFieldCondition(sqlFilter, fieldDef, cmd);

                        if (fieldDef.IsRowVersion)
                            hadRowVersion = true;

                        continue;
                    }

                    if (!updateAllFields && !updateFields.Contains(fieldDef.Name, StringComparer.OrdinalIgnoreCase))
                        continue;

                    if (sql.Length > 0)
                        sql.Append(", ");

                    sql
                        .Append(GetQuotedColumnName(fieldDef.FieldName))
                        .Append("=")
                        .Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName), fieldDef.CustomUpdate));

                    AddParameter(cmd, fieldDef);
                }
                catch (Exception ex)
                {
                    OrmLiteUtils.HandleException(ex, "ERROR in PrepareParameterizedUpdateStatement(): " + ex.Message);
                }
            }

            if (sql.Length > 0)
            {
                var strFilter = StringBuilderCacheAlt.ReturnAndFree(sqlFilter);
                cmd.CommandText = $"UPDATE {GetQuotedTableName(modelDef)} " +
                                  $"SET {StringBuilderCache.ReturnAndFree(sql)} {(strFilter.Length > 0 ? "WHERE " + strFilter : "")}";
            }
            else
            {
                cmd.CommandText = "";
            }

            return hadRowVersion;
        }

        public virtual void AppendNullFieldCondition(StringBuilder sqlFilter, FieldDefinition fieldDef)
        {
            sqlFilter
                .Append(GetQuotedColumnName(fieldDef.FieldName))
                .Append(" IS NULL");
        }

        public virtual void AppendFieldCondition(StringBuilder sqlFilter, FieldDefinition fieldDef, IDbCommand cmd)
        {
            sqlFilter
                .Append(GetQuotedColumnName(fieldDef.FieldName))
                .Append("=")
                .Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName)));

            AddParameter(cmd, fieldDef);
        }

        public virtual bool PrepareParameterizedDeleteStatement<T>(IDbCommand cmd, IDictionary<string, object> deleteFieldValues)
        {
            if (deleteFieldValues == null || deleteFieldValues.Count == 0)
                throw new ArgumentException("DELETE's must have at least 1 criteria");

            var sqlFilter = StringBuilderCache.Allocate();
            var modelDef = typeof(T).GetModelDefinition();
            var hadRowVersion = false;

            cmd.Parameters.Clear();

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (fieldDef.ShouldSkipDelete())
                    continue;

                if (!deleteFieldValues.TryGetValue(fieldDef.Name, out var fieldValue))
                    continue;

                if (fieldDef.IsRowVersion)
                    hadRowVersion = true;

                try
                {
                    if (sqlFilter.Length > 0)
                        sqlFilter.Append(" AND ");

                    if (fieldValue != null)
                    {
                        AppendFieldCondition(sqlFilter, fieldDef, cmd);
                    }
                    else
                    {
                        AppendNullFieldCondition(sqlFilter, fieldDef);
                    }
                }
                catch (Exception ex)
                {
                    OrmLiteUtils.HandleException(ex, "ERROR in PrepareParameterizedDeleteStatement(): " + ex.Message);
                }
            }

            cmd.CommandText = $"DELETE FROM {GetQuotedTableName(modelDef)} WHERE {StringBuilderCache.ReturnAndFree(sqlFilter)}";

            return hadRowVersion;
        }

        public virtual void PrepareStoredProcedureStatement<T>(IDbCommand cmd, T obj)
        {
            cmd.CommandText = ToExecuteProcedureStatement(obj);
            cmd.CommandType = CommandType.StoredProcedure;
        }

        /// <summary>
        /// Used for adding updated DB params in INSERT and UPDATE statements  
        /// </summary>
        protected IDbDataParameter AddParameter(IDbCommand cmd, FieldDefinition fieldDef)
        {
            var p = cmd.CreateParameter();
            SetParameter(fieldDef, p);
            InitUpdateParam(p);
            cmd.Parameters.Add(p);
            return p;
        }

        public virtual void SetParameter(FieldDefinition fieldDef, IDbDataParameter p)
        {
            p.ParameterName = this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName));
            InitDbParam(p, fieldDef.ColumnType);
        }

        public virtual void EnableIdentityInsert<T>(IDbCommand cmd) {}
        public virtual Task EnableIdentityInsertAsync<T>(IDbCommand cmd, CancellationToken token=default) => TypeConstants.EmptyTask;

        public virtual void DisableIdentityInsert<T>(IDbCommand cmd) {}
        public virtual Task DisableIdentityInsertAsync<T>(IDbCommand cmd, CancellationToken token=default) => TypeConstants.EmptyTask;

        public virtual void EnableForeignKeysCheck(IDbCommand cmd) {}
        public virtual Task EnableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token=default) => TypeConstants.EmptyTask;

        public virtual void DisableForeignKeysCheck(IDbCommand cmd) {}
        public virtual Task DisableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token=default) => TypeConstants.EmptyTask;

        public virtual void SetParameterValues<T>(IDbCommand dbCmd, object obj)
        {
            var modelDef = GetModel(typeof(T));
            var fieldMap = GetFieldDefinitionMap(modelDef);

            foreach (IDataParameter p in dbCmd.Parameters)
            {
                var fieldName = this.ToFieldName(p.ParameterName);
                fieldMap.TryGetValue(fieldName, out var fieldDef);

                if (fieldDef == null)
                {
                    if (ParamNameFilter != null)
                    {
                        fieldDef = modelDef.GetFieldDefinition(name => 
                            string.Equals(ParamNameFilter(name), fieldName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (fieldDef == null)
                        throw new ArgumentException($"Field Definition '{fieldName}' was not found");
                }

                if (fieldDef.AutoId && p.Value != null)
                {
                    var existingId = fieldDef.GetValue(obj);
                    if (existingId is Guid existingGuid && existingGuid != default(Guid))
                    {
                        p.Value = existingGuid; // Use existing value if not default
                    }

                    fieldDef.SetValue(obj, p.Value); //Auto populate default values
                    continue;
                }
                
                SetParameterValue(fieldDef, p, obj);
            }
        }

        public Dictionary<string, FieldDefinition> GetFieldDefinitionMap(ModelDefinition modelDef)
        {
            return modelDef.GetFieldDefinitionMap(SanitizeFieldNameForParamName);
        }

        public virtual void SetParameterValue(FieldDefinition fieldDef, IDataParameter p, object obj)
        {
            var value = GetValueOrDbNull(fieldDef, obj);
            p.Value = value;

            SetParameterSize(fieldDef, p);
        }

        protected virtual void SetParameterSize(FieldDefinition fieldDef, IDataParameter p)
        {
            if (p.Value is string s && p is IDbDataParameter dataParam && dataParam.Size > 0 && s.Length > dataParam.Size)
            {
                // db param Size set in StringConverter
                dataParam.Size = s.Length;
            }
        }

        protected virtual object GetValue(FieldDefinition fieldDef, object obj)
        {
            return GetFieldValue(fieldDef, fieldDef.GetValue(obj));
        }

        public object GetFieldValue(FieldDefinition fieldDef, object value)
        {
            if (value == null)
                return null;

            var converter = GetConverterBestMatch(fieldDef);
            try
            {
                return converter.ToDbValue(fieldDef.FieldType, value);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in {converter.GetType().Name}.ToDbValue() for field '{fieldDef.Name}' of Type '{fieldDef.FieldType}' with value '{value.GetType().Name}'", ex);
                throw;
            }
        }

        public object GetFieldValue(Type fieldType, object value)
        {
            if (value == null)
                return null;

            var converter = GetConverterBestMatch(fieldType);
            try
            {
                return converter.ToDbValue(fieldType, value);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in {converter.GetType().Name}.ToDbValue() for field of Type '{fieldType}' with value '{value.GetType().Name}'", ex);
                throw;
            }
        }

        protected virtual object GetValueOrDbNull(FieldDefinition fieldDef, object obj)
        {
            var value = GetValue(fieldDef, obj);
            if (value == null)
                return DBNull.Value;

            return value;
        }

        protected virtual object GetQuotedValueOrDbNull<T>(FieldDefinition fieldDef, object obj)
        {
            var value = fieldDef.GetValue(obj);

            if (value == null)
                return DBNull.Value;

            var unquotedVal = GetQuotedValue(value, fieldDef.FieldType)
                .TrimStart('\'').TrimEnd('\''); ;

            if (string.IsNullOrEmpty(unquotedVal))
                return DBNull.Value;

            return unquotedVal;
        }

        public virtual void PrepareUpdateRowStatement(IDbCommand dbCmd, object objWithProperties, ICollection<string> updateFields = null)
        {
            var sql = StringBuilderCache.Allocate();
            var sqlFilter = StringBuilderCacheAlt.Allocate();
            var modelDef = objWithProperties.GetType().GetModelDefinition();
            var updateAllFields = updateFields == null || updateFields.Count == 0;

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (fieldDef.ShouldSkipUpdate())
                    continue;

                try
                {
                    if (fieldDef.IsPrimaryKey && updateAllFields)
                    {
                        if (sqlFilter.Length > 0)
                            sqlFilter.Append(" AND ");

                        sqlFilter
                            .Append(GetQuotedColumnName(fieldDef.FieldName))
                            .Append("=")
                            .Append(this.AddQueryParam(dbCmd, fieldDef.GetValue(objWithProperties), fieldDef).ParameterName);

                        continue;
                    }

                    if (!updateAllFields && !updateFields.Contains(fieldDef.Name, StringComparer.OrdinalIgnoreCase) || fieldDef.AutoIncrement)
                        continue;

                    if (sql.Length > 0)
                        sql.Append(", ");

                    sql
                        .Append(GetQuotedColumnName(fieldDef.FieldName))
                        .Append("=")
                        .Append(this.GetUpdateParam(dbCmd, fieldDef.GetValue(objWithProperties), fieldDef));
                }
                catch (Exception ex)
                {
                    OrmLiteUtils.HandleException(ex, "ERROR in ToUpdateRowStatement(): " + ex.Message);
                }
            }

            var strFilter = StringBuilderCacheAlt.ReturnAndFree(sqlFilter);
            dbCmd.CommandText = $"UPDATE {GetQuotedTableName(modelDef)} " +
                                $"SET {StringBuilderCache.ReturnAndFree(sql)}{(strFilter.Length > 0 ? " WHERE " + strFilter : "")}";

            if (sql.Length == 0)
                throw new Exception("No valid update properties provided (e.g. p => p.FirstName): " + dbCmd.CommandText);
        }

        public virtual void PrepareUpdateRowStatement<T>(IDbCommand dbCmd, Dictionary<string, object> args, string sqlFilter)
        {
            var sql = StringBuilderCache.Allocate();
            var modelDef = typeof(T).GetModelDefinition();

            foreach (var entry in args)
            {
                var fieldDef = modelDef.AssertFieldDefinition(entry.Key);
                if (fieldDef.ShouldSkipUpdate() || fieldDef.IsPrimaryKey || fieldDef.AutoIncrement)
                    continue;

                var value = entry.Value;

                try
                {
                    if (sql.Length > 0)
                        sql.Append(", ");

                    sql
                        .Append(GetQuotedColumnName(fieldDef.FieldName))
                        .Append("=")
                        .Append(this.GetUpdateParam(dbCmd, value, fieldDef));
                }
                catch (Exception ex)
                {
                    OrmLiteUtils.HandleException(ex, "ERROR in PrepareUpdateRowStatement(cmd,args): " + ex.Message);
                }
            }

            dbCmd.CommandText = $"UPDATE {GetQuotedTableName(modelDef)} " +
                                $"SET {StringBuilderCache.ReturnAndFree(sql)}{(string.IsNullOrEmpty(sqlFilter) ? "" : " ")}{sqlFilter}";

            if (sql.Length == 0)
                throw new Exception("No valid update properties provided (e.g. () => new Person { Age = 27 }): " + dbCmd.CommandText);
        }

        public virtual void PrepareUpdateRowAddStatement<T>(IDbCommand dbCmd, Dictionary<string, object> args, string sqlFilter)
        {
            var sql = StringBuilderCache.Allocate();
            var modelDef = typeof(T).GetModelDefinition();

            foreach (var entry in args)
            {
                var fieldDef = modelDef.AssertFieldDefinition(entry.Key);
                if (fieldDef.ShouldSkipUpdate() || fieldDef.AutoIncrement || fieldDef.IsPrimaryKey ||
                    fieldDef.IsRowVersion || fieldDef.Name == OrmLiteConfig.IdField)
                    continue;

                var value = entry.Value;

                try
                {
                    if (sql.Length > 0)
                        sql.Append(", ");

                    var quotedFieldName = GetQuotedColumnName(fieldDef.FieldName);

                    if (fieldDef.FieldType.IsNumericType())
                    {
                        sql
                            .Append(quotedFieldName)
                            .Append("=")
                            .Append(quotedFieldName)
                            .Append("+")
                            .Append(this.GetUpdateParam(dbCmd, value, fieldDef));
                    }
                    else
                    {
                        sql
                            .Append(quotedFieldName)
                            .Append("=")
                            .Append(this.GetUpdateParam(dbCmd, value, fieldDef));
                    }
                }
                catch (Exception ex)
                {
                    OrmLiteUtils.HandleException(ex, "ERROR in PrepareUpdateRowAddStatement(): " + ex.Message);
                }
            }

            dbCmd.CommandText = $"UPDATE {GetQuotedTableName(modelDef)} " +
                                $"SET {StringBuilderCache.ReturnAndFree(sql)}{(string.IsNullOrEmpty(sqlFilter) ? "" : " ")}{sqlFilter}";

            if (sql.Length == 0)
                throw new Exception("No valid update properties provided (e.g. () => new Person { Age = 27 }): " + dbCmd.CommandText);
        }

        public virtual string ToDeleteStatement(Type tableType, string sqlFilter, params object[] filterParams)
        {
            var sql = StringBuilderCache.Allocate();
            const string deleteStatement = "DELETE ";

            var isFullDeleteStatement =
                !string.IsNullOrEmpty(sqlFilter)
                && sqlFilter.Length > deleteStatement.Length
                && sqlFilter.Substring(0, deleteStatement.Length).ToUpper().Equals(deleteStatement);

            if (isFullDeleteStatement)
                return sqlFilter.SqlFmt(this, filterParams);

            var modelDef = tableType.GetModelDefinition();
            sql.Append($"DELETE FROM {GetQuotedTableName(modelDef)}");

            if (string.IsNullOrEmpty(sqlFilter))
                return StringBuilderCache.ReturnAndFree(sql);

            sqlFilter = sqlFilter.SqlFmt(this, filterParams);
            sql.Append(" WHERE ");
            sql.Append(sqlFilter);

            return StringBuilderCache.ReturnAndFree(sql);
        }

        public virtual bool HasInsertReturnValues(ModelDefinition modelDef) =>
            modelDef.FieldDefinitions.Any(x => x.ReturnOnInsert);

        public string GetDefaultValue(Type tableType, string fieldName)
        {
            var modelDef = tableType.GetModelDefinition();
            var fieldDef = modelDef.AssertFieldDefinition(fieldName);
            return GetDefaultValue(fieldDef);
        }

        public virtual string GetDefaultValue(FieldDefinition fieldDef)
        {
            var defaultValue = fieldDef.DefaultValue;
            if (string.IsNullOrEmpty(defaultValue))
            {
                return fieldDef.AutoId 
                    ? GetAutoIdDefaultValue(fieldDef) 
                    : null;
            }

            return ResolveFragment(defaultValue);
        }

        public virtual string ResolveFragment(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return null;
            
            if (!sql.StartsWith("{"))
                return sql;

            return Variables.TryGetValue(sql, out var variable)
                ? variable
                : null;
        }

        public virtual string GetAutoIdDefaultValue(FieldDefinition fieldDef) => null;

        public Func<ModelDefinition, IEnumerable<FieldDefinition>> CreateTableFieldsStrategy { get; set; } = GetFieldDefinitions;

        public static IEnumerable<FieldDefinition> GetFieldDefinitions(ModelDefinition modelDef) => modelDef.FieldDefinitions.OrderBy(fd=>fd.Order);

        public abstract string ToCreateSchemaStatement(string schemaName);

        public virtual List<string> GetSchemas(IDbCommand dbCmd) => new() { "default" };

        public virtual Dictionary<string, List<string>> GetSchemaTables(IDbCommand dbCmd) => new();

        public abstract bool DoesSchemaExist(IDbCommand dbCmd, string schemaName);

        public virtual Task<bool> DoesSchemaExistAsync(IDbCommand dbCmd, string schema, CancellationToken token = default)
        {
            return DoesSchemaExist(dbCmd, schema).InTask();
        }

        public virtual string ToCreateTableStatement(Type tableType)
        {
            var sbColumns = StringBuilderCache.Allocate();
            var sbConstraints = StringBuilderCacheAlt.Allocate();

            var modelDef = tableType.GetModelDefinition();
            foreach (var fieldDef in CreateTableFieldsStrategy(modelDef))
            {
                if (fieldDef.CustomSelect != null || (fieldDef.IsComputed && !fieldDef.IsPersisted))
                    continue;

                var columnDefinition = GetColumnDefinition(fieldDef);

                if (columnDefinition == null)
                    continue;

                if (sbColumns.Length != 0)
                    sbColumns.Append(", \n  ");

                sbColumns.Append(columnDefinition);

                var sqlConstraint = GetCheckConstraint(modelDef, fieldDef);
                if (sqlConstraint != null)
                {
                    sbConstraints.Append(",\n" + sqlConstraint);
                }

                if (fieldDef.ForeignKey == null || OrmLiteConfig.SkipForeignKeys)
                    continue;

                var refModelDef = fieldDef.ForeignKey.ReferenceType.GetModelDefinition();
                sbConstraints.Append(
                    $", \n\n  CONSTRAINT {GetQuotedName(fieldDef.ForeignKey.GetForeignKeyName(modelDef, refModelDef, NamingStrategy, fieldDef))} " +
                    $"FOREIGN KEY ({GetQuotedColumnName(fieldDef.FieldName)}) " +
                    $"REFERENCES {GetQuotedTableName(refModelDef)} ({GetQuotedColumnName(refModelDef.PrimaryKey.FieldName)})");

                sbConstraints.Append(GetForeignKeyOnDeleteClause(fieldDef.ForeignKey));
                sbConstraints.Append(GetForeignKeyOnUpdateClause(fieldDef.ForeignKey));
            }

            var uniqueConstraints = GetUniqueConstraints(modelDef);
            if (uniqueConstraints != null)
            {
                sbConstraints.Append(",\n" + uniqueConstraints);
            }

            var sql = $"CREATE TABLE {GetQuotedTableName(modelDef)} " +
                      $"\n(\n  {StringBuilderCache.ReturnAndFree(sbColumns)}{StringBuilderCacheAlt.ReturnAndFree(sbConstraints)} \n); \n";

            return sql;
        }

        public virtual string GetUniqueConstraints(ModelDefinition modelDef)
        {
            var constraints = modelDef.UniqueConstraints.Map(x => 
                $"CONSTRAINT {GetUniqueConstraintName(x, GetTableName(modelDef).StripDbQuotes())} UNIQUE ({x.FieldNames.Map(f => modelDef.GetQuotedName(f,this)).Join(",")})" );

            return constraints.Count > 0
                ? constraints.Join(",\n")
                : null;
        }

        protected virtual string GetUniqueConstraintName(UniqueConstraintAttribute constraint, string tableName) =>
            constraint.Name ?? $"UC_{tableName}_{constraint.FieldNames.Join("_")}";

        public virtual string GetCheckConstraint(ModelDefinition modelDef, FieldDefinition fieldDef)
        {
            if (fieldDef.CheckConstraint == null)
                return null;

            return $"CONSTRAINT CHK_{modelDef.Schema}_{modelDef.ModelName}_{fieldDef.FieldName} CHECK ({fieldDef.CheckConstraint})";
        }

        public virtual string ToPostCreateTableStatement(ModelDefinition modelDef)
        {
            return null;
        }

        public virtual string ToPostDropTableStatement(ModelDefinition modelDef)
        {
            return null;
        }

        public virtual string GetForeignKeyOnDeleteClause(ForeignKeyConstraint foreignKey)
        {
            return !string.IsNullOrEmpty(foreignKey.OnDelete) ? " ON DELETE " + foreignKey.OnDelete : "";
        }

        public virtual string GetForeignKeyOnUpdateClause(ForeignKeyConstraint foreignKey)
        {
            return !string.IsNullOrEmpty(foreignKey.OnUpdate) ? " ON UPDATE " + foreignKey.OnUpdate : "";
        }

        public virtual List<string> ToCreateIndexStatements(Type tableType)
        {
            var sqlIndexes = new List<string>();

            var modelDef = tableType.GetModelDefinition();
            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (!fieldDef.IsIndexed) continue;

                var indexName = fieldDef.IndexName 
                    ?? GetIndexName(fieldDef.IsUniqueIndex, modelDef.ModelName.SafeVarName(), fieldDef.FieldName);

                sqlIndexes.Add(
                    ToCreateIndexStatement(fieldDef.IsUniqueIndex, indexName, modelDef, fieldDef.FieldName, isCombined: false, fieldDef: fieldDef));
            }

            foreach (var compositeIndex in modelDef.CompositeIndexes)
            {
                var indexName = GetCompositeIndexName(compositeIndex, modelDef);

                var sb = StringBuilderCache.Allocate();
                foreach (var fieldName in compositeIndex.FieldNames)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");

                    var parts = fieldName.SplitOnLast(' ');
                    if (parts.Length == 2 && (parts[1].ToLower().StartsWith("desc") || parts[1].ToLower().StartsWith("asc")))
                    {
                        sb.Append(GetQuotedColumnName(parts[0]))
                          .Append(' ')
                          .Append(parts[1]);
                    }
                    else
                    {
                        sb.Append(GetQuotedColumnName(fieldName));
                    }
                }

                sqlIndexes.Add(
                    ToCreateIndexStatement(compositeIndex.Unique, indexName, modelDef,
                    StringBuilderCache.ReturnAndFree(sb),
                    isCombined: true));
            }

            return sqlIndexes;
        }

        public virtual bool DoesTableExist(IDbConnection db, string tableName, string schema = null)
        {
            return db.Exec(dbCmd => DoesTableExist(dbCmd, tableName, schema));
        }

        public virtual async Task<bool> DoesTableExistAsync(IDbConnection db, string tableName, string schema = null, CancellationToken token = default)
        {
            return await db.Exec(async dbCmd => await DoesTableExistAsync(dbCmd, tableName, schema, token));
        }

        public virtual bool DoesTableExist(IDbCommand dbCmd, string tableName, string schema = null)
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> DoesTableExistAsync(IDbCommand dbCmd, string tableName, string schema = null, CancellationToken token = default)
        {
            return DoesTableExist(dbCmd, tableName, schema).InTask();
        }

        public virtual bool DoesColumnExist(IDbConnection db, string columnName, string tableName, string schema = null)
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> DoesColumnExistAsync(IDbConnection db, string columnName, string tableName, string schema = null, CancellationToken token = default)
        {
            return DoesColumnExist(db, columnName, tableName, schema).InTask();
        }

        public virtual bool DoesSequenceExist(IDbCommand dbCmd, string sequence)
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> DoesSequenceExistAsync(IDbCommand dbCmd, string sequenceName, CancellationToken token = default)
        {
            return DoesSequenceExist(dbCmd, sequenceName).InTask();
        }

        protected virtual string GetIndexName(bool isUnique, string modelName, string fieldName)
        {
            return $"{(isUnique ? "u" : "")}idx_{modelName}_{fieldName}".ToLower();
        }

        protected virtual string GetCompositeIndexName(CompositeIndexAttribute compositeIndex, ModelDefinition modelDef)
        {
            return compositeIndex.Name ?? GetIndexName(compositeIndex.Unique, modelDef.ModelName.SafeVarName(),
                string.Join("_", compositeIndex.FieldNames.Map(x => x.LeftPart(' ')).ToArray()));
        }

        protected virtual string GetCompositeIndexNameWithSchema(CompositeIndexAttribute compositeIndex, ModelDefinition modelDef)
        {
            return compositeIndex.Name ?? GetIndexName(compositeIndex.Unique,
                    (modelDef.IsInSchema
                        ? modelDef.Schema + "_" + GetQuotedTableName(modelDef)
                        : GetQuotedTableName(modelDef)).SafeVarName(),
                    string.Join("_", compositeIndex.FieldNames.ToArray()));
        }

        protected virtual string ToCreateIndexStatement(bool isUnique, string indexName, ModelDefinition modelDef, string fieldName,
            bool isCombined = false, FieldDefinition fieldDef = null)
        {
            return $"CREATE {(isUnique ? "UNIQUE" : "")}" +
                   (fieldDef?.IsClustered == true ? " CLUSTERED" : "") +
                   (fieldDef?.IsNonClustered == true ? " NONCLUSTERED" : "") +
                   $" INDEX {indexName} ON {GetQuotedTableName(modelDef)} " +
                   $"({(isCombined ? fieldName : GetQuotedColumnName(fieldName))}); \n";
        }

        public virtual List<string> ToCreateSequenceStatements(Type tableType)
        {
            return new List<string>();
        }

        public virtual string ToCreateSequenceStatement(Type tableType, string sequenceName)
        {
            return "";
        }

        public virtual string ToCreateSavePoint(string name) => $"SAVEPOINT {name}";
        public virtual string ToReleaseSavePoint(string name) => $"RELEASE SAVEPOINT {name}";
        public virtual string ToRollbackSavePoint(string name) => $"ROLLBACK TO SAVEPOINT {name}";

        public virtual List<string> SequenceList(Type tableType) => new List<string>();

        public virtual Task<List<string>> SequenceListAsync(Type tableType, CancellationToken token = default) => new List<string>().InTask();

        // TODO : make abstract  ??
        public virtual string ToExistStatement(Type fromTableType,
            object objWithProperties,
            string sqlFilter,
            params object[] filterParams)
        {
            throw new NotImplementedException();
        }

        // TODO : make abstract  ??
        public virtual string ToSelectFromProcedureStatement(
            object fromObjWithProperties,
            Type outputModelType,
            string sqlFilter,
            params object[] filterParams)
        {
            throw new NotImplementedException();
        }

        // TODO : make abstract  ??
        public virtual string ToExecuteProcedureStatement(object objWithProperties) => null;

        protected static ModelDefinition GetModel(Type modelType) => modelType.GetModelDefinition();

        public virtual SqlExpression<T> SqlExpression<T>()
        {
            throw new NotImplementedException();
        }

        public IDbCommand CreateParameterizedDeleteStatement(IDbConnection connection, object objWithProperties)
        {
            throw new NotImplementedException();
        }

        public virtual string GetDropForeignKeyConstraints(ModelDefinition modelDef) => null;

        public virtual string ToAddColumnStatement(string schema, string table, FieldDefinition fieldDef) => 
            $"ALTER TABLE {GetQuotedTableName(table, schema)} ADD COLUMN {GetColumnDefinition(fieldDef)};";

        public virtual string ToAlterColumnStatement(string schema, string table, FieldDefinition fieldDef) => 
            $"ALTER TABLE {GetQuotedTableName(table, schema)} MODIFY COLUMN {GetColumnDefinition(fieldDef)};";
        
        public virtual string ToChangeColumnNameStatement(string schema, string table, FieldDefinition fieldDef, string oldColumn) => 
            $"ALTER TABLE {GetQuotedTableName(table, schema)} CHANGE COLUMN {GetQuotedColumnName(oldColumn)} {GetColumnDefinition(fieldDef)};";

        public virtual string ToRenameColumnStatement(string schema, string table, string oldColumn, string newColumn) => 
            $"ALTER TABLE {GetQuotedTableName(table, schema)} RENAME COLUMN {GetQuotedColumnName(oldColumn)} TO {GetQuotedColumnName(newColumn)};";

        public virtual string ToAddForeignKeyStatement<T, TForeign>(Expression<Func<T, object>> field,
            Expression<Func<TForeign, object>> foreignField,
            OnFkOption onUpdate,
            OnFkOption onDelete,
            string foreignKeyName = null)
        {
            var sourceMD = ModelDefinition<T>.Definition;
            var fieldName = sourceMD.GetFieldDefinition(field).FieldName;

            var referenceMD = ModelDefinition<TForeign>.Definition;
            var referenceFieldName = referenceMD.GetFieldDefinition(foreignField).FieldName;

            string name = GetQuotedName(foreignKeyName.IsNullOrEmpty() ?
                "fk_" + sourceMD.ModelName + "_" + fieldName + "_" + referenceFieldName :
                foreignKeyName);

            return $"ALTER TABLE {GetQuotedTableName(sourceMD)} " +
                   $"ADD CONSTRAINT {name} FOREIGN KEY ({GetQuotedColumnName(fieldName)}) " +
                   $"REFERENCES {GetQuotedTableName(referenceMD)} " +
                   $"({GetQuotedColumnName(referenceFieldName)})" +
                   $"{GetForeignKeyOnDeleteClause(new ForeignKeyConstraint(typeof(T), onDelete: FkOptionToString(onDelete)))}" +
                   $"{GetForeignKeyOnUpdateClause(new ForeignKeyConstraint(typeof(T), onUpdate: FkOptionToString(onUpdate)))};";
        }

        public virtual string ToDropForeignKeyStatement(string schema, string table, string foreignKeyName) =>
            $"ALTER TABLE {GetQuotedTableName(table, schema)} DROP CONSTRAINT {GetQuotedName(foreignKeyName)};";

        public virtual string ToCreateIndexStatement<T>(Expression<Func<T, object>> field, string indexName = null, bool unique = false)
        {
            var sourceDef = ModelDefinition<T>.Definition;
            var fieldName = sourceDef.GetFieldDefinition(field).FieldName;

            string name = GetQuotedName(indexName.IsNullOrEmpty() ?
                (unique ? "uidx" : "idx") + "_" + sourceDef.ModelName + "_" + fieldName :
                indexName);

            string command = $"CREATE {(unique ? "UNIQUE" : "")} " +
                             $"INDEX {name} ON {GetQuotedTableName(sourceDef)}" +
                             $"({GetQuotedColumnName(fieldName)});";
            return command;
        }


        protected virtual string FkOptionToString(OnFkOption option)
        {
            switch (option)
            {
                case OnFkOption.Cascade: return "CASCADE";
                case OnFkOption.NoAction: return "NO ACTION";
                case OnFkOption.SetNull: return "SET NULL";
                case OnFkOption.SetDefault: return "SET DEFAULT";
                case OnFkOption.Restrict:
                default: return "RESTRICT";
            }
        }

        public virtual string GetQuotedValue(object value, Type fieldType)
        {
            if (value == null || value == DBNull.Value) 
                return "NULL";

            var converter = value.GetType().IsEnum
                ? EnumConverter
                : GetConverterBestMatch(fieldType);
            try
            {
                return converter.ToQuotedString(fieldType, value);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in {converter.GetType().Name}.ToQuotedString() value '{converter.GetType().Name}' and Type '{value.GetType().Name}'", ex);
                throw;
            }
        }

        public virtual object GetParamValue(object value, Type fieldType)
        {
            return ToDbValue(value, fieldType);
        }

        public virtual void InitQueryParam(IDbDataParameter param) {}
        public virtual void InitUpdateParam(IDbDataParameter param) {}

        public virtual string EscapeWildcards(string value)
        {
            return value?.Replace("^", @"^^")
                .Replace(@"\", @"^\")
                .Replace("_", @"^_")
                .Replace("%", @"^%");
        }

        public virtual string GetLoadChildrenSubSelect<From>(SqlExpression<From> expr)
        {
            var modelDef = expr.ModelDef;
            expr.UnsafeSelect(this.GetQuotedColumnName(modelDef, modelDef.PrimaryKey));

            var subSql = expr.ToSelectStatement(QueryType.Select);

            return subSql;
        }

        public virtual string ToRowCountStatement(string innerSql) => 
            $"SELECT COUNT(*) FROM ({innerSql}) AS COUNT";

        public virtual string ToDropColumnStatement(string schema, string table, string column) => 
            $"ALTER TABLE {GetQuotedTableName(table, schema)} DROP COLUMN {GetQuotedColumnName(column)};";

        public virtual string ToTableNamesStatement(string schema) => throw new NotSupportedException();

        public virtual string ToTableNamesWithRowCountsStatement(bool live, string schema) => null; //returning null Fallsback to slow UNION N+1 COUNT(*) op

        public virtual string SqlConflict(string sql, string conflictResolution) => sql; //NOOP

        public virtual string SqlConcat(IEnumerable<object> args) => $"CONCAT({string.Join(", ", args)})";

        public virtual string SqlCurrency(string fieldOrValue) => SqlCurrency(fieldOrValue, "$");

        public virtual string SqlCurrency(string fieldOrValue, string currencySymbol) => SqlConcat(new List<string> { currencySymbol, fieldOrValue });

        public virtual string SqlBool(bool value) => value ? "true" : "false";

        public virtual string SqlLimit(int? offset = null, int? rows = null) => rows == null && offset == null
            ? "" 
            : offset == null
                ? "LIMIT " + rows
                : "LIMIT " + rows.GetValueOrDefault(int.MaxValue) + " OFFSET " + offset;
        
        public virtual string SqlCast(object fieldOrValue, string castAs) => $"CAST({fieldOrValue} AS {castAs})";

        public virtual string SqlRandom => "RAND()";

        //Async API's, should be overriden by Dialect Providers to use .ConfigureAwait(false)
        //Default impl below uses TaskAwaiter shim in async.cs

        public virtual Task OpenAsync(IDbConnection db, CancellationToken token = default)
        {
            db.Open();
            return TaskResult.Finished;
        }

        public virtual Task<IDataReader> ExecuteReaderAsync(IDbCommand cmd, CancellationToken token = default)
        {
            return cmd.ExecuteReader().InTask();
        }

        public virtual Task<int> ExecuteNonQueryAsync(IDbCommand cmd, CancellationToken token = default)
        {
            return cmd.ExecuteNonQuery().InTask();
        }

        public virtual Task<object> ExecuteScalarAsync(IDbCommand cmd, CancellationToken token = default)
        {
            return cmd.ExecuteScalar().InTask();
        }

        public virtual Task<bool> ReadAsync(IDataReader reader, CancellationToken token = default)
        {
            return reader.Read().InTask();
        }

#if ASYNC
        public virtual async Task<List<T>> ReaderEach<T>(IDataReader reader, Func<T> fn, CancellationToken token = default)
        {
            try
            {
                var to = new List<T>();
                while (await ReadAsync(reader, token))
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

        public virtual async Task<Return> ReaderEach<Return>(IDataReader reader, Action fn, Return source, CancellationToken token = default)
        {
            try
            {
                while (await ReadAsync(reader, token))
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

        public virtual async Task<T> ReaderRead<T>(IDataReader reader, Func<T> fn, CancellationToken token = default)
        {
            try
            {
                if (await ReadAsync(reader, token))
                    return fn();

                return default(T);
            }
            finally
            {
                reader.Dispose();
            }
        }

        public virtual Task<long> InsertAndGetLastInsertIdAsync<T>(IDbCommand dbCmd, CancellationToken token)
        {
            if (SelectIdentitySql == null)
                return new NotImplementedException("Returning last inserted identity is not implemented on this DB Provider.")
                    .InTask<long>();

            dbCmd.CommandText += "; " + SelectIdentitySql;

            return dbCmd.ExecLongScalarAsync(null, token);
        }
#else
        public Task<List<T>> ReaderEach<T>(IDataReader reader, Func<T> fn, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException(OrmLiteUtils.AsyncRequiresNet45Error);
        }

        public Task<Return> ReaderEach<Return>(IDataReader reader, Action fn, Return source, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException(OrmLiteUtils.AsyncRequiresNet45Error);
        }

        public Task<T> ReaderRead<T>(IDataReader reader, Func<T> fn, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException(OrmLiteUtils.AsyncRequiresNet45Error);
        }

        public Task<long> InsertAndGetLastInsertIdAsync<T>(IDbCommand dbCmd, CancellationToken token)
        {
            throw new NotImplementedException(OrmLiteUtils.AsyncRequiresNet45Error);
        }
#endif
    }
}
