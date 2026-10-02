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

namespace ServiceStack.OrmLite;

public abstract class OrmLiteDialectProviderBase<TDialect>
    : IOrmLiteDialectProvider, IOrmLiteUpsertDialectProvider
    where TDialect : IOrmLiteDialectProvider
{
    protected static readonly ILog Log = LogManager.GetLogger(typeof(IOrmLiteDialectProvider));
    public virtual DbKind Kind => DbKind.Unknown;

    public int MaxInListParams { get; set; } = 1000;

    public virtual string ToReturningStatement(string sql, ModelDefinition modelDef, bool isDelete, ICollection<FieldDefinition> returnFields = null) =>
        throw new NotSupportedException($"{GetType().Name} doesn't support returning rows from UPDATE and DELETE statements");

    public virtual ExplainQuery ToExplainQuery(IDbConnection db, string sql, bool analyze) =>
        throw new NotSupportedException($"{GetType().Name} doesn't support returning query plans");

    /// <summary>
    /// The columns of a RETURNING clause: all the table's columns, or only the returnFields
    /// </summary>
    protected virtual string GetReturningColumns(ModelDefinition modelDef, ICollection<FieldDefinition> returnFields)
    {
        if (returnFields == null || returnFields.Count == 0)
            return GetColumnNames(modelDef);

        // The table's columns are in the same order as its fields
        var allColumns = GetColumnNames(modelDef, null);
        var sqlColumns = new List<SelectItem>();
        for (var i = 0; i < allColumns.Length; i++)
        {
            if (returnFields.Contains(modelDef.FieldDefinitions[i]))
                sqlColumns.Add(allColumns[i]);
        }
        return sqlColumns.ToArray().ToSelectString();
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

#if NET6_0_OR_GREATER
        RegisterConverter<DateOnly>(new DateOnlyConverter());
        RegisterConverter<TimeOnly>(new TimeOnlyConverter());
#endif
    }
    
    /// <summary>
    /// Use JSON for serializing Complex Types
    /// </summary>
    public virtual bool UseJson
    {
#if NET8_0_OR_GREATER
        set => StringSerializer = value ? new JsonComplexTypeSerializer() : new JsvStringSerializer();
#else
        set => StringSerializer = value ? new JsonStringSerializer() : new JsvStringSerializer();
#endif
    }

#if NET8_0_OR_GREATER
    public OrmLiteDialectProviderBase<TDialect> ConfigureJson(Action<JsonComplexTypeSerializer> configure)
    {
        if (StringSerializer is JsonComplexTypeSerializer jsonSerializer)
        {
            configure(jsonSerializer);
        }
        else throw new NotSupportedException($"StringSerializer {StringSerializer.GetType().Name} is not a {nameof(JsonComplexTypeSerializer)}");
        return this;
    }
#endif
    
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

    public Dictionary<string, string> Variables { get; set; } = new();

    public IOrmLiteExecFilter ExecFilter { get; set; }

    public Dictionary<Type, IOrmLiteConverter> Converters = new();

    public string AutoIncrementDefinition = "AUTOINCREMENT"; //SqlServer express limit

    public DecimalConverter DecimalConverter => (DecimalConverter)Converters[typeof(decimal)];

    public StringConverter StringConverter => (StringConverter)Converters[typeof(string)];

    public Action<IDbConnection> OnOpenConnection { get; set; }
    public Action<IDbConnection> OnDisposeConnection { get; set; }
    public Action<IDbCommand> OnBeforeExecuteNonQuery { get; set; }
    public Action<IDbCommand> OnAfterExecuteNonQuery { get; set; }

    internal int OneTimeConnectionCommandsRun;

    /// <summary>
    /// Enable Bulk Inserts from CSV files
    /// </summary>
    public bool AllowLoadLocalInfile
    {
        set => OneTimeConnectionCommands.Add($"SET GLOBAL LOCAL_INFILE={value.ToString().ToUpper()};");
    }
        
    public List<string> OneTimeConnectionCommands { get; } = [];
    public List<string> ConnectionCommands { get; } = [];

    public string ParamString { get; set; } = "@";

    public INamingStrategy NamingStrategy { get; set; } = new OrmLiteDefaultNamingStrategy();

    public IStringSerializer StringSerializer { get; set; } = new JsvStringSerializer();

    private Func<string, string> paramNameFilter;
    public Func<string, string> ParamNameFilter
    {
        get => paramNameFilter ?? OrmLiteConfig.ParamNameFilter;
        set => paramNameFilter = value;
    }
        
    public virtual bool SupportsSchema => true;
    public virtual bool SupportsConcurrentWrites => true;

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
        return converter is null or NativeValueOrmLiteConverter;
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
            ? ReferenceTypeConverter
            : ValueTypeConverter;
    }

    public virtual IOrmLiteConverter GetConverterBestMatch(FieldDefinition fieldDef)
    {
        var fieldType = Nullable.GetUnderlyingType(fieldDef.FieldType) ?? fieldDef.FieldType;

        if (fieldDef.IsRowVersion)
            return RowVersionConverter;

        if (fieldDef.VectorDimensions != null)
            return VectorConverter;

        if (Converters.TryGetValue(fieldType, out var converter))
            return converter;

        if (fieldType.IsEnum)
            return EnumConverter;

        return fieldType.IsRefType()
            ? ReferenceTypeConverter
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
        if (value is null or DBNull)
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

    /// <summary>
    /// Returns an unquoted table name (inc schema if exists), using naming strategy 
    /// </summary>
    public virtual string UnquotedTable(TableRef tableRef)
    {
        if (tableRef.QuotedName != null)
            return tableRef.QuotedName.Replace("\"","");

        var schema = tableRef.GetSchemaName();
        var alias = tableRef.ModelDef?.Alias; 
        if (alias != null)
            return schema != null
                ? JoinSchema(NamingStrategy.GetSchemaName(schema), NamingStrategy.GetAlias(alias))
                : NamingStrategy.GetAlias(alias);

        var tableName = tableRef.GetTableName();
        return schema != null
            ? JoinSchema(NamingStrategy.GetSchemaName(schema), NamingStrategy.GetTableName(tableName))
            : NamingStrategy.GetTableName(tableName);
    }

    /// <summary>
    /// Returns an quoted table name (inc schema if exists), using naming strategy 
    /// </summary>
    public virtual string QuoteTable(TableRef tableRef)
    {
        var useVerbatim = tableRef.QuotedName;
        if (useVerbatim != null)
            return useVerbatim;

        if (tableRef.ModelDef != null)
            return GetQuotedTableName(tableRef.ModelDef);
        
        var schema = tableRef.GetSchemaName();
        return schema != null
            ? QuoteSchema(NamingStrategy.GetSchemaName(schema), NamingStrategy.GetTableName(tableRef.Name))
            : tableRef.Name != null
                ? GetQuotedName(NamingStrategy.GetTableName(tableRef.Name))
                : null;
    }

    /// <summary>
    /// Return unquoted table name only (i.e. without schema), using naming strategy
    /// </summary>
    /// <param name="tableRef"></param>
    /// <returns></returns>
    public virtual string GetTableNameOnly(TableRef tableRef)
    {
        return tableRef.QuotedName?.LastRightPart('.').StripDbQuotes() ?? 
               (tableRef.ModelDef != null
                   ? NamingStrategy.GetTableName(tableRef.ModelDef)
                   : NamingStrategy.GetTableName(tableRef.Name));
    }
    
    public virtual string GetSchemaName(TableRef tableRef) => NamingStrategy.GetSchemaName(tableRef.GetSchemaName()); 
    
    public virtual string GetQuotedTableName(Type modelType) => 
        GetQuotedTableName(modelType.GetModelDefinition());
        
    public virtual string GetQuotedTableName(ModelDefinition modelDef)
    {
        if (modelDef == null) 
            return null;
        var schema = modelDef.Schema;
        if (modelDef.Alias != null)
        {
            return schema == null 
                ? GetQuotedName(NamingStrategy.GetAlias(modelDef.Alias)) 
                : QuoteSchema(NamingStrategy.GetSchemaName(schema),NamingStrategy.GetAlias(modelDef.Alias));
        }

        return schema == null 
            ? GetQuotedName(NamingStrategy.GetTableName(modelDef.Name)) 
            : QuoteSchema(NamingStrategy.GetSchemaName(schema), NamingStrategy.GetTableName(modelDef.Name));
    }

    /// <summary>
    /// Return a quoted schema + table name (does not use naming strategy)
    /// </summary>
    public virtual string QuoteSchema(string schema, string table)
    {
        if (string.IsNullOrEmpty(schema))
            return string.IsNullOrEmpty(table)
                ? null
                : GetQuotedName(table);
        return JoinSchema(QuoteSchemaName(schema), GetQuotedName(table));
    }
    
    /// <summary>
    /// Quotes each part of a multi-part schema name individually, e.g. db.dbo => "db"."dbo"
    /// </summary>
    public virtual string QuoteSchemaName(string schema) => schema == null ? null : schema.IndexOf('.') >= 0
        ? string.Join(".", schema.Split('.').Select(GetQuotedName))
        : GetQuotedName(schema);

    public virtual string JoinSchema(string schema, string table) => schema != null 
        ? schema + "." + table 
        : table;

    public virtual string GetQuotedColumnName(FieldDefinition fieldDef)
    {
        if (fieldDef == null)
            return null;
        return GetQuotedName(fieldDef.Alias != null 
            ? NamingStrategy.GetAlias(fieldDef.Alias)
            : GetQuotedColumnName(fieldDef.Name));
    }
    
    public virtual string GetQuotedColumnName(string columnName) => columnName == null ? null : 
        GetQuotedName(NamingStrategy.GetColumnName(columnName));

    public virtual string GetColumnName(FieldDefinition fieldDef)
    {
        if (fieldDef == null)
            return null;
        return fieldDef.Alias != null
            ? NamingStrategy.GetAlias(fieldDef.Alias)
            : NamingStrategy.GetColumnName(fieldDef.Name);
    }

    public virtual bool ShouldQuote(string name) => 
        !string.IsNullOrEmpty(name) && (name.IndexOf(' ') >= 0 || name.IndexOf('.') >= 0);

    public virtual string QuoteIfRequired(string name)
    {
        return ShouldQuote(name)
            ? GetQuotedName(name)
            : name;
    }

    protected char QuoteChar = '"';
    public virtual string GetQuotedName(string name)
    {
        if (name == null) return null;
        if (IsQuotedName(name, QuoteChar))
            return name;
        var quoteStr = QuoteChar.ToString();
        return QuoteChar + name.Replace(quoteStr, quoteStr + quoteStr) + QuoteChar;
    }

    /// <summary>
    /// Whether name is a regular identifier that's safe to emit unquoted, i.e. starts with a letter or '_'
    /// and only contains letters, digits, '_' or any of the dialect-specific allowedChars (e.g. '$', '#')
    /// </summary>
    public static bool IsRegularIdentifier(string name, string allowedChars = null)
    {
        if (string.IsNullOrEmpty(name) || !(char.IsLetter(name[0]) || name[0] == '_'))
            return false;
        foreach (var c in name)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_' || (allowedChars != null && allowedChars.IndexOf(c) >= 0)))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Whether name is already a single well-formed quoted identifier, i.e. wrapped in quoteChar with any
    /// embedded quoteChar escaped by doubling. Prevents identifier breakout via names like: "a"; DROP TABLE b; --"
    /// </summary>
    public static bool IsQuotedName(string name, char quoteChar)
    {
        if (name == null || name.Length < 2 || name[0] != quoteChar || name[name.Length - 1] != quoteChar)
            return false;
        for (var i = 1; i < name.Length - 1; i++)
        {
            if (name[i] != quoteChar) 
                continue;
            if (i + 1 < name.Length - 1 && name[i + 1] == quoteChar)
                i++;
            else
                return false;
        }
        return true;
    }

    public virtual string GetQuotedName(string name, string schema)
    {
        return schema != null
            ? $"{GetQuotedName(schema)}.{GetQuotedName(name)}"
            : name != null 
                ? GetQuotedName(name)
                : null;
    }

    public virtual string GetQuotedValue(string paramValue)
    {
        return "'" + paramValue.Replace("'", "''") + "'";
    }

    public virtual string SanitizeFieldNameForParamName(string fieldName)
    {
        return OrmLiteConfig.SanitizeFieldNameForParamNameFn(fieldName);
    }

    /// <summary>
    /// Replaces {Property} references in SQL from attributes with their quoted column name, e.g. in
    /// [Index(Where)] and [Compute(expression)]
    /// </summary>
    public virtual string ResolveColumnRefs(ModelDefinition modelDef, string sql)
    {
        if (modelDef == null || string.IsNullOrEmpty(sql) || sql.IndexOf('{') < 0)
            return sql;

        return ColumnRefRegex.Replace(sql, m => modelDef.GetFieldDefinition(m.Groups[1].Value) is { } fieldDef
            ? GetQuotedColumnName(fieldDef)
            : m.Value);
    }
    private static readonly Regex ColumnRefRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);

    /// <summary>
    /// A column the RDBMS generates from its [Compute("expression")], which is stored when it's [Persisted]
    /// </summary>
    /// <summary>
    /// The column type of a field: its [CustomField], the vector type of a [Vector] or the type of its converter
    /// </summary>
    protected virtual string GetFieldTypeDefinition(FieldDefinition fieldDef) =>
        ResolveFragment(fieldDef.CustomFieldDefinition) ?? (fieldDef.VectorDimensions != null
            ? GetVectorColumnDefinition(fieldDef.VectorDimensions.Value)
            : GetColumnTypeDefinition(fieldDef.ColumnType, fieldDef.FieldLength, fieldDef.Scale));

    public IOrmLiteConverter VectorConverter { get; set; } = new VectorConverter();

    /// <summary>
    /// The column type of a [Vector] with these dimensions
    /// </summary>
    public virtual string GetVectorColumnDefinition(int dimensions) =>
        throw new NotSupportedException($"{GetType().Name} doesn't support [Vector] columns");

    public virtual string ToVectorParam(string param, int dimensions) => param;

    public virtual string ToVectorDistance(VectorDistance distance, string vector, string other) =>
        throw new NotSupportedException($"{GetType().Name} doesn't support vector distances");

    /// <summary>
    /// The statement that creates the vector index of an indexed [Vector] column, or null if the RDBMS has none
    /// </summary>
    protected virtual string ToCreateVectorIndexStatement(ModelDefinition modelDef, FieldDefinition fieldDef, string indexName) => null;

    /// <summary>
    /// What's selected for a [Vector] column if it can't be read as it is, or null to select the column
    /// </summary>
    protected virtual string GetVectorSelectExpression(string quotedColumn) => null;

    protected virtual string GetGeneratedColumnDefinition(FieldDefinition fieldDef)
    {
        var columnType = GetFieldTypeDefinition(fieldDef);
        return $"{GetQuotedColumnName(fieldDef)} {columnType} GENERATED ALWAYS AS " +
               $"({ResolveColumnRefs(fieldDef.ModelDef, fieldDef.ComputeExpression)}) " +
               (fieldDef.IsPersisted ? "STORED" : "VIRTUAL");
    }

    public virtual string GetColumnDefinition(FieldDefinition fieldDef)
    {
        if (fieldDef.IsGenerated)
            return GetGeneratedColumnDefinition(fieldDef);

        var fieldDefinition = GetFieldTypeDefinition(fieldDef);

        var sql = StringBuilderCache.Allocate();
        sql.Append($"{GetQuotedColumnName(fieldDef)} {fieldDefinition}");

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
        && (sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || sql.TrimStart().StartsWith("WITH ", StringComparison.OrdinalIgnoreCase)); // common table expressions

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
    
    public virtual OrmLiteConnection CreateOrmLiteConnection(OrmLiteConnectionFactory factory, string namedConnection = null)
    {
        return new OrmLiteConnection(factory) {
            NamedConnection = namedConnection,
        };
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
        return new SelectItemColumn(this, field, tablePrefix);
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
        // If tablePrefix is the same as the table alias, use the quoted table name instead 
        var quotedPrefix = tablePrefix != null 
            ? tablePrefix == modelDef.Alias  
                ? GetQuotedTableName(modelDef)
                : QuoteTable(new(modelDef.Schema, tablePrefix)) 
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
            else if (field.VectorDimensions != null && GetVectorSelectExpression(
                         (quotedPrefix.Length > 0 ? quotedPrefix + "." : "") + GetQuotedColumnName(field)) is { } vectorSelect)
            {
                sqlColumns[i] = new SelectItemExpression(this, vectorSelect, field.FieldName);
            }
            else
            {
                sqlColumns[i] = new SelectItemColumn(this, field, quotedPrefix);
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

            sbColumnNames.Append(GetQuotedColumnName(fieldDef));

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

    public virtual void BulkInsert<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null)
    {
        config ??= new();
        foreach (var batch in objs.BatchesOf(config.BatchSize))
        {
            var sql = ToInsertRowsSql(batch, insertFields:config.InsertFields);
            db.ExecuteSql(sql);
        }
    }
    
    public virtual async Task BulkInsertAsync<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null, CancellationToken token=default)
    {
        config ??= new();
        foreach (var batch in objs.BatchesOf(config.BatchSize))
        {
            var sql = ToInsertRowsSql(batch, insertFields:config.InsertFields);
            await db.ExecuteSqlAsync(sql, token: token).ConfigAwait();
        }
    }

    // What a bulk upsert does: load the rows into a temporary staging table, then upsert them from it
    protected sealed class BulkUpsertPlan
    {
        public FieldDefinition PrimaryKey;
        /// <summary>The quoted name of the temporary table the rows are loaded into</summary>
        public string StagingTable;
        /// <summary>The fields loaded into the staging table: those that are inserted or updated</summary>
        public List<FieldDefinition> FieldDefs;
        public string CreateStagingTableSql;
        public string UpsertSql;
        public string DropStagingTableSql;
    }

    /// <summary>
    /// Returns null when rows have to be upserted one at a time: the RDBMS has no native upsert, or the connection
    /// has filters or write rules for the table, which a single statement can't apply to the rows it updates
    /// </summary>
    protected virtual BulkUpsertPlan CreateBulkUpsertPlan<T>(IDbConnection db, ICollection<string> updateOnly, BulkInsertConfig config)
    {
        var modelDef = ModelDefinition<T>.Definition;
        var primaryKey = modelDef.FieldDefinitions.FirstOrDefault(x => x.IsPrimaryKey)
            ?? throw new NotSupportedException($"'{typeof(T).Name}' does not have a primary key");
        var updateFieldDefs = OrmLiteWriteCommandExtensions.GetUpsertUpdateFieldDefinitions(modelDef, updateOnly);

        if (!SupportsUpsert || db.Exec(dbCmd => dbCmd.HasFilters<T>() || dbCmd.HasWriteRules<T>()))
            return null;

        var requestedInsertFields = GetInsertFieldDefinitions(modelDef, config.InsertFields).ToSet();
        requestedInsertFields.Add(primaryKey);
        var insertFieldDefs = modelDef.FieldDefinitions
            .Where(x => requestedInsertFields.Contains(x) && (!ShouldSkipInsert(x) || x.AutoId || x.IsPrimaryKey))
            .ToList();

        var fieldDefs = modelDef.FieldDefinitions
            .Where(x => insertFieldDefs.Contains(x) || updateFieldDefs.Contains(x))
            .ToList();

        var stagingTable = GetBulkStagingTableName("ormlite_stage_" + Guid.NewGuid().ToString("N"));
        return new BulkUpsertPlan {
            PrimaryKey = primaryKey,
            StagingTable = stagingTable,
            FieldDefs = fieldDefs,
            CreateStagingTableSql = ToCreateBulkStagingTableStatement(modelDef, stagingTable, fieldDefs),
            UpsertSql = ToBulkUpsertStatement(modelDef, stagingTable, insertFieldDefs, updateFieldDefs),
            DropStagingTableSql = ToDropBulkStagingTableStatement(stagingTable),
        };
    }

    // Rows with an [AutoIncrement] primary key that hasn't been assigned are new, and are inserted for the RDBMS
    // to assign it. The others are loaded into the staging table.
    private static IEnumerable<T> GetRowsToStage<T>(IEnumerable<T> objs, FieldDefinition primaryKey, List<T> newRows)
    {
        if (!primaryKey.AutoIncrement)
            return objs;

        var defaultId = primaryKey.FieldType.GetDefaultValue();
        IEnumerable<T> RowsWithIds()
        {
            foreach (var obj in objs)
            {
                var id = primaryKey.GetValue(obj);
                if (id == null || Equals(id, defaultId))
                    newRows.Add(obj);
                else
                    yield return obj;
            }
        }
        return RowsWithIds();
    }

    public virtual void BulkUpsert<T>(IDbConnection db, IEnumerable<T> objs, ICollection<string> updateOnly = null, BulkInsertConfig config = null)
    {
        config ??= new();
        var plan = CreateBulkUpsertPlan<T>(db, updateOnly, config);
        if (plan == null)
        {
            if (updateOnly == null)
                db.UpsertAll(objs);
            else
                db.UpsertAll(objs, updateOnly.ToArray());
            return;
        }

        var newRows = new List<T>();
        db.ExecuteSql(plan.CreateStagingTableSql);
        try
        {
            BulkLoad(db, plan.StagingTable, plan.FieldDefs, GetRowsToStage(objs, plan.PrimaryKey, newRows), config);

            db.Exec(dbCmd => {
                // Rows keep the primary keys they were given
                if (plan.PrimaryKey.AutoIncrement)
                    EnableIdentityInsert<T>(dbCmd);
                try
                {
                    return dbCmd.ExecuteSql(plan.UpsertSql);
                }
                finally
                {
                    if (plan.PrimaryKey.AutoIncrement)
                        DisableIdentityInsert<T>(dbCmd);
                }
            });
        }
        finally
        {
            db.ExecuteSql(plan.DropStagingTableSql);
        }

        if (newRows.Count > 0)
            db.BulkInsert(newRows, config);
    }

    public virtual async Task BulkUpsertAsync<T>(IDbConnection db, IEnumerable<T> objs, ICollection<string> updateOnly = null, BulkInsertConfig config = null, CancellationToken token=default)
    {
        config ??= new();
        var plan = CreateBulkUpsertPlan<T>(db, updateOnly, config);
        if (plan == null)
        {
            if (updateOnly == null)
                await db.UpsertAllAsync(objs, token).ConfigAwait();
            else
                await db.UpsertAllAsync(objs, updateOnly.ToArray(), token).ConfigAwait();
            return;
        }

        var newRows = new List<T>();
        await db.ExecuteSqlAsync(plan.CreateStagingTableSql, token: token).ConfigAwait();
        try
        {
            await BulkLoadAsync(db, plan.StagingTable, plan.FieldDefs, GetRowsToStage(objs, plan.PrimaryKey, newRows), config, token).ConfigAwait();

            await db.Exec(async dbCmd => {
                if (plan.PrimaryKey.AutoIncrement)
                    await EnableIdentityInsertAsync<T>(dbCmd, token).ConfigAwait();
                try
                {
                    return await dbCmd.ExecuteSqlAsync(plan.UpsertSql, token).ConfigAwait();
                }
                finally
                {
                    if (plan.PrimaryKey.AutoIncrement)
                        await DisableIdentityInsertAsync<T>(dbCmd, token).ConfigAwait();
                }
            }).ConfigAwait();
        }
        finally
        {
            await db.ExecuteSqlAsync(plan.DropStagingTableSql, token: token).ConfigAwait();
        }

        if (newRows.Count > 0)
            await db.BulkInsertAsync(newRows, config, token).ConfigAwait();
    }

    /// <summary>
    /// The quoted name of a temporary table of this connection
    /// </summary>
    protected virtual string GetBulkStagingTableName(string name) => GetQuotedName(name);

    /// <summary>
    /// Creates an empty temporary table with the same columns as the fields of a table
    /// </summary>
    protected virtual string ToCreateBulkStagingTableStatement(ModelDefinition modelDef, string stagingTable, List<FieldDefinition> fieldDefs) =>
        $"CREATE TEMPORARY TABLE {stagingTable} AS " +
        $"SELECT {fieldDefs.Map(GetQuotedColumnName).Join(",")} FROM {GetQuotedTableName(modelDef)} WHERE 1=0";

    protected virtual string ToDropBulkStagingTableStatement(string stagingTable) => "DROP TABLE " + stagingTable;

    /// <summary>
    /// The statement that inserts the rows of the staging table with a new primary key, and updates the
    /// updateFieldDefs of those with an existing one
    /// </summary>
    protected virtual string ToBulkUpsertStatement(ModelDefinition modelDef, string stagingTable,
        List<FieldDefinition> insertFieldDefs, List<FieldDefinition> updateFieldDefs) =>
        throw new NotSupportedException($"{GetType().Name} does not support bulk upserts");

    // INSERT INTO Table (columns) SELECT columns FROM staging
    protected string GetBulkUpsertInsertSql(ModelDefinition modelDef, string stagingTable, List<FieldDefinition> insertFieldDefs) =>
        $"INSERT INTO {GetQuotedTableName(modelDef)} ({insertFieldDefs.Map(GetQuotedColumnName).Join(",")}) " +
        $"SELECT {insertFieldDefs.Map(x => GetBulkUpsertValue(x.CustomInsert, GetQuotedColumnName(x))).Join(",")} FROM {stagingTable}";

    // The value a column is inserted or updated with: the staged column, in the field's [CustomInsert] or [CustomUpdate]
    protected static string GetBulkUpsertValue(string customFormat, string stagedColumn) =>
        customFormat != null ? string.Format(customFormat, stagedColumn) : stagedColumn;

    /// <summary>
    /// The value of a row that's loaded into a table, where an [AutoId] that hasn't been assigned gets a new one
    /// </summary>
    protected object GetBulkLoadValue(FieldDefinition fieldDef, object obj)
    {
        var value = fieldDef.GetValue(obj);
        return fieldDef.AutoId && (value == null || Equals(value, fieldDef.FieldType.GetDefaultValue()))
            ? GetInsertDefaultValue(fieldDef)
            : value;
    }

    // INSERT INTO table (columns) VALUES (row),(row),...
    private string ToBulkLoadSql<T>(string quotedTable, List<FieldDefinition> fieldDefs, IEnumerable<T> rows)
    {
        var sb = StringBuilderCache.Allocate()
            .Append("INSERT INTO ").Append(quotedTable).Append(" (")
            .Append(fieldDefs.Map(GetQuotedColumnName).Join(",")).Append(") VALUES ");

        var count = 0;
        foreach (var row in rows)
        {
            sb.Append(count++ > 0 ? ",\n(" : "\n(");
            for (var i = 0; i < fieldDefs.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append(GetQuotedValue(GetBulkLoadValue(fieldDefs[i], row), fieldDefs[i].FieldType));
            }
            sb.Append(')');
        }
        var sql = StringBuilderCache.ReturnAndFree(sb);
        return count > 0 ? sql : null;
    }

    /// <summary>
    /// Loads rows into a table with the fields of the model, using the fastest way the RDBMS has
    /// </summary>
    protected virtual void BulkLoad<T>(IDbConnection db, string quotedTable, List<FieldDefinition> fieldDefs, IEnumerable<T> rows, BulkInsertConfig config)
    {
        foreach (var batch in rows.BatchesOf(config.BatchSize))
        {
            var sql = ToBulkLoadSql(quotedTable, fieldDefs, batch);
            if (sql != null)
                db.ExecuteSql(sql);
        }
    }

    protected virtual async Task BulkLoadAsync<T>(IDbConnection db, string quotedTable, List<FieldDefinition> fieldDefs, IEnumerable<T> rows, BulkInsertConfig config, CancellationToken token)
    {
        foreach (var batch in rows.BatchesOf(config.BatchSize))
        {
            var sql = ToBulkLoadSql(quotedTable, fieldDefs, batch);
            if (sql != null)
                await db.ExecuteSqlAsync(sql, token: token).ConfigAwait();
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
                sbColumnNames.Append(GetQuotedColumnName(fieldDef));
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

        cmd.CommandText = $"INSERT INTO {GetQuotedTableName(modelDef)} ({StringBuilderCache.ReturnAndFree(sbColumnNames)}) " +
                          $"VALUES ({StringBuilderCacheAlt.ReturnAndFree(sbColumnValues)})";
    }

    public virtual bool SupportsUpsert => false;

    public virtual string ToUpsertReturningStatement(string sql, ModelDefinition modelDef, ICollection<FieldDefinition> returnFields = null) => null;

    public virtual void PrepareParameterizedUpsertStatement<T>(IDbCommand cmd,
        ICollection<string> insertFields = null, ICollection<string> updateOnly = null) =>
        throw new NotSupportedException($"{GetType().Name} does not support native UPSERT statements");

    protected void PrepareUpsertFields<T>(IDbCommand cmd,
        ICollection<string> insertFields,
        ICollection<string> updateOnly,
        out ModelDefinition modelDef,
        out List<FieldDefinition> insertFieldDefs,
        out List<FieldDefinition> updateFieldDefs)
    {
        modelDef = typeof(T).GetModelDefinition();
        var primaryKey = modelDef.FieldDefinitions.FirstOrDefault(x => x.IsPrimaryKey)
            ?? throw new NotSupportedException($"'{typeof(T).Name}' does not have a primary key");

        var requestedInsertFields = GetInsertFieldDefinitions(modelDef, insertFields).ToSet();
        requestedInsertFields.Add(primaryKey);

        insertFieldDefs = modelDef.FieldDefinitions
            .Where(x => requestedInsertFields.Contains(x)
                && (!ShouldSkipInsert(x) || x.AutoId || x.IsPrimaryKey))
            .ToList();

        var updateAllFields = updateOnly == null;
        var requestedUpdateFields = updateAllFields
            ? null
            : GetInsertFieldDefinitions(modelDef, updateOnly).ToSet();

        updateFieldDefs = modelDef.FieldDefinitions
            .Where(x => !x.IsPrimaryKey
                && !x.IsRowVersion
                && !x.ShouldSkipUpdate()
                && (updateAllFields || requestedUpdateFields.Contains(x)))
            .ToList();

        cmd.Parameters.Clear();
        foreach (var fieldDef in modelDef.FieldDefinitions)
        {
            if (!insertFieldDefs.Contains(fieldDef) && !updateFieldDefs.Contains(fieldDef))
                continue;

            var p = AddParameter(cmd, fieldDef);
            if (fieldDef.AutoId)
                p.Value = GetInsertDefaultValue(fieldDef);
        }
    }

    protected string GetUpsertInsertSql(ModelDefinition modelDef, IEnumerable<FieldDefinition> insertFieldDefs)
    {
        var fields = insertFieldDefs.ToList();
        var columnNames = fields.Map(GetQuotedColumnName).Join(",");
        var columnValues = fields.Map(x =>
            this.GetParam(SanitizeFieldNameForParamName(x.FieldName), x.CustomInsert)).Join(",");
        return $"INSERT INTO {GetQuotedTableName(modelDef)} ({columnNames}) VALUES ({columnValues})";
    }

    protected string GetUpsertUpdateSql(IEnumerable<FieldDefinition> updateFieldDefs, string targetPrefix = null)
    {
        return updateFieldDefs.Map(x =>
            (targetPrefix ?? "") + GetQuotedColumnName(x) + "=" +
            this.GetParam(SanitizeFieldNameForParamName(x.FieldName), x.CustomUpdate)).Join(", ");
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
                sbColumnNames.Append(GetQuotedColumnName(fieldDef));
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
        dialectProvider.PrepareParameterizedUpdateStatement<T>(dbCmd, updateFields);

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
    public virtual string GetRefSelfSql<From>(SqlExpression<From> refQ, ModelDefinition modelDef, FieldDefinition refSelf, ModelDefinition refModelDef, FieldDefinition refId)
    {
        refQ.Select(this.GetQuotedColumnName(modelDef, refSelf));
        refQ.OrderBy().ClearLimits(); //clear any ORDER BY or LIMIT's in Sub Select's

        var subSqlRef = refQ.ToMergedParamsSelectStatement();

        var sqlRef = $"SELECT {GetColumnNames(refModelDef)} " +
                     $"FROM {GetQuotedTableName(refModelDef)} " +
                     $"WHERE {this.GetQuotedColumnName(refId)} " +
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
                    .Append(GetQuotedColumnName(fieldDef))
                    .Append("=")
                    .Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName), fieldDef.CustomUpdate));

                AddParameter(cmd, fieldDef);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ERROR in PrepareParameterizedUpdateStatement(): " + ex.Message);
                if (OrmLiteConfig.ThrowOnError)
                    throw;
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
            .Append(GetQuotedColumnName(fieldDef))
            .Append(" IS NULL");
    }

    public virtual void AppendFieldCondition(StringBuilder sqlFilter, FieldDefinition fieldDef, IDbCommand cmd)
    {
        sqlFilter
            .Append(GetQuotedColumnName(fieldDef))
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
                Log.Error(ex, "ERROR in PrepareParameterizedDeleteStatement(): " + ex.Message);
                if (OrmLiteConfig.ThrowOnError)
                    throw;
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
        if (fieldDef.VectorDimensions != null)
            VectorConverter.InitDbParam(p, fieldDef.ColumnType);
        else
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
            if (OrmLiteConnectionFiltersApi.IsFilterParam(p.ParameterName))
                continue; // params of the connection's filter conditions

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
                    throw new ArgumentException("Field Definition was not found", fieldName);
            }

            if (fieldDef.AutoId && p.Value != null)
            {
                var existingId = fieldDef.GetValue(obj);
                if (existingId is Guid existingGuid && existingGuid != default)
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
                        .Append(GetQuotedColumnName(fieldDef))
                        .Append("=")
                        .Append(this.AddQueryParam(dbCmd, fieldDef.GetValue(objWithProperties), fieldDef).ParameterName);

                    continue;
                }

                if (!updateAllFields && !updateFields.Contains(fieldDef.Name, StringComparer.OrdinalIgnoreCase) || fieldDef.AutoIncrement)
                    continue;

                if (sql.Length > 0)
                    sql.Append(", ");

                sql
                    .Append(GetQuotedColumnName(fieldDef))
                    .Append("=")
                    .Append(this.GetUpdateParam(dbCmd, fieldDef.GetValue(objWithProperties), fieldDef));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ERROR in ToUpdateRowStatement(): " + ex.Message);
                if (OrmLiteConfig.ThrowOnError)
                    throw;
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
                    .Append(GetQuotedColumnName(fieldDef))
                    .Append("=")
                    .Append(this.GetUpdateParam(dbCmd, value, fieldDef));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ERROR in PrepareUpdateRowStatement(cmd,args): " + ex.Message);
                if (OrmLiteConfig.ThrowOnError)
                    throw;
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

                var quotedFieldName = GetQuotedColumnName(fieldDef);

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
                Log.Error(ex, "ERROR in PrepareUpdateRowAddStatement(): " + ex.Message);
                if (OrmLiteConfig.ThrowOnError)
                    throw;
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

    public bool DeoptimizeReader { get; set; }

    public virtual bool HasInsertReturnValues(ModelDefinition modelDef) =>
        modelDef.FieldDefinitions.Any(x => x.ReturnOnInsert);

    public virtual string GetForUpdateTableHint(bool skipLocked) => null;

    public virtual string GetForUpdateClause(string lockTable, bool skipLocked) =>
        "FOR UPDATE" + (lockTable != null ? " OF " + lockTable : "") + (skipLocked ? " SKIP LOCKED" : "");

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

    public virtual List<string> GetSchemas(IDbCommand dbCmd) => ["default"];

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
            if (fieldDef.ShouldSkipCreate())
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
                $"FOREIGN KEY ({GetQuotedColumnName(fieldDef)}) " +
                $"REFERENCES {GetQuotedTableName(refModelDef)} ({GetQuotedColumnName(refModelDef.PrimaryKey)})");

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
            $"CONSTRAINT {GetUniqueConstraintName(x, GetTableNameOnly(new(modelDef)))} UNIQUE ({x.FieldNames.Map(f => modelDef.GetQuotedName(f,this)).Join(",")})" );

        return constraints.Count > 0
            ? constraints.Join(",\n")
            : null;
    }

    protected virtual string GetUniqueConstraintName(UniqueConstraintAttribute constraint, string tableName) =>
        constraint.Name ?? $"UC_{tableName}_{constraint.FieldNames.Join("_")}";

    public virtual string GetCheckConstraint(ModelDefinition modelDef, FieldDefinition fieldDef)
    {
        var constraint = fieldDef.CheckConstraint;
        if (fieldDef.CheckEnum)
        {
            var enumConstraint = GetEnumCheckConstraint(fieldDef);
            constraint = constraint == null
                ? enumConstraint
                : $"({constraint}) AND {enumConstraint}";
        }
        if (constraint == null)
            return null;

        return $"CONSTRAINT CHK_{modelDef.Schema}_{modelDef.ModelName}_{fieldDef.FieldName} CHECK ({constraint})";
    }

    /// <summary>
    /// The condition a [CheckEnum] column is constrained by, which only allows the values of its Enum as they're
    /// stored, e.g: "Status" IN ('New','Shipped')
    /// </summary>
    protected virtual string GetEnumCheckConstraint(FieldDefinition fieldDef)
    {
        var enumType = fieldDef.FieldType;
        if (!enumType.IsEnum)
            throw new NotSupportedException($"[CheckEnum] is only valid on Enum properties, {fieldDef.Name} is a {enumType.Name}");
        if (enumType.HasAttributeCached<FlagsAttribute>())
            throw new NotSupportedException($"[CheckEnum] isn't supported on [Flags] Enums like {enumType.Name}, whose values are combined");

        var values = new List<string>();
        foreach (var value in Enum.GetValues(enumType))
        {
            var quotedValue = GetQuotedValue(value, enumType);
            if (!values.Contains(quotedValue))
                values.Add(quotedValue);
        }
        return $"{GetQuotedColumnName(fieldDef)} IN ({string.Join(",", values)})";
    }

    public virtual List<string> ToCreateCommentStatements(Type tableType) => [];

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

            if (fieldDef.VectorDimensions != null)
            {
                // A vector index where the RDBMS has one, as other indexes can't be used to find similar vectors
                var sqlVectorIndex = ToCreateVectorIndexStatement(modelDef, fieldDef, indexName);
                if (sqlVectorIndex != null)
                    sqlIndexes.Add(sqlVectorIndex);
                continue;
            }

            var keyColumns = GetIndexKeyColumns(modelDef, fieldDef.IsUniqueIndex, GetQuotedColumnName(fieldDef), fieldDef.IndexInclude);
            sqlIndexes.Add(WithIndexOptions(modelDef, fieldDef.IndexInclude, fieldDef.IndexWhere, keyColumns != null
                ? ToCreateIndexStatement(fieldDef.IsUniqueIndex, indexName, modelDef, keyColumns, isCombined: true, fieldDef: fieldDef)
                : ToCreateIndexStatement(fieldDef.IsUniqueIndex, indexName, modelDef, fieldDef.FieldName, isCombined: false, fieldDef: fieldDef)));
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
                    var name = parts[0];
                    var fieldDef = modelDef.GetFieldDefinition(name);
                    sb.Append(fieldDef != null ? GetQuotedColumnName(fieldDef) : GetQuotedColumnName(name))
                        .Append(' ')
                        .Append(parts[1]);
                }
                else
                {
                    var fieldDef = modelDef.GetFieldDefinition(fieldName);
                    sb.Append(fieldDef != null ? GetQuotedColumnName(fieldDef) : GetQuotedColumnName(fieldName));
                }
            }

            var columns = StringBuilderCache.ReturnAndFree(sb);
            sqlIndexes.Add(WithIndexOptions(modelDef, compositeIndex.Include, compositeIndex.Where,
                ToCreateIndexStatement(compositeIndex.Unique, indexName, modelDef,
                    GetIndexKeyColumns(modelDef, compositeIndex.Unique, columns, compositeIndex.Include) ?? columns,
                    isCombined: true)));
        }

        return sqlIndexes;
    }

    /// <summary>
    /// Whether indexes can keep other columns with INCLUDE. Where they can't, the columns of a covering index
    /// are added to the key columns of non-unique indexes instead.
    /// </summary>
    protected virtual bool SupportsIndexInclude => false;

    /// <summary>
    /// Whether an index can be limited to the rows matching a condition
    /// </summary>
    protected virtual bool SupportsFilteredIndexes => true;

    private string GetQuotedIndexColumn(ModelDefinition modelDef, string name) =>
        modelDef.GetFieldDefinition(name) is { } fieldDef
            ? GetQuotedColumnName(fieldDef)
            : GetQuotedColumnName(name);

    // The key columns with the columns to include, for an RDBMS without INCLUDE. Returns null if they're unchanged.
    private string GetIndexKeyColumns(ModelDefinition modelDef, bool isUnique, string keyColumns, string[] include)
    {
        // Adding columns to a unique index would change which rows it allows
        if (include == null || include.Length == 0 || SupportsIndexInclude || isUnique)
            return null;
        return keyColumns + ", " + string.Join(", ", include.Map(x => GetQuotedIndexColumn(modelDef, x)));
    }

    // Adds the INCLUDE and WHERE of a covering or filtered index to its CREATE INDEX statement
    private string WithIndexOptions(ModelDefinition modelDef, string[] include, string where, string sql)
    {
        var options = "";
        if (include is { Length: > 0 } && SupportsIndexInclude)
            options += $" INCLUDE ({string.Join(", ", include.Map(x => GetQuotedIndexColumn(modelDef, x)))})";

        if (!string.IsNullOrEmpty(where))
        {
            if (!SupportsFilteredIndexes)
                throw new NotSupportedException($"{GetType().Name} doesn't support filtered indexes, used by {modelDef.Name}");
            options += " WHERE " + ResolveColumnRefs(modelDef, where);
        }
        if (options.Length == 0)
            return sql;

        var endPos = sql.LastIndexOf(';');
        return endPos >= 0
            ? sql.Substring(0, endPos) + options + sql.Substring(endPos)
            : sql + options;
    }

    public virtual bool DoesTableExist(IDbConnection db, TableRef tableRef)
    {
        return db.Exec(dbCmd => DoesTableExist(dbCmd, tableRef));
    }

    public virtual async Task<bool> DoesTableExistAsync(IDbConnection db, TableRef tableRef, CancellationToken token = default)
    {
        return await db.Exec(async dbCmd => await DoesTableExistAsync(dbCmd, tableRef, token));
    }

    public virtual bool DoesTableExist(IDbCommand dbCmd, TableRef tableRef)
    {
        throw new NotImplementedException();
    }

    public virtual Task<bool> DoesTableExistAsync(IDbCommand dbCmd, TableRef tableRef, CancellationToken token = default)
    {
        return DoesTableExist(dbCmd, tableRef).InTask();
    }

    public virtual bool DoesColumnExist(IDbConnection db, string columnName, TableRef tableRef)
    {
        throw new NotImplementedException();
    }

    public virtual Task<bool> DoesColumnExistAsync(IDbConnection db, string columnName, TableRef tableRef, CancellationToken token = default)
    {
        return DoesColumnExist(db, columnName, tableRef).InTask();
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
        fieldDef ??= modelDef.GetFieldDefinition(fieldName);
        return $"CREATE {(isUnique ? "UNIQUE" : "")}" +
               (fieldDef?.IsClustered == true ? " CLUSTERED" : "") +
               (fieldDef?.IsNonClustered == true ? " NONCLUSTERED" : "") +
               $" INDEX {indexName} ON {GetQuotedTableName(modelDef)} " +
               $"({(isCombined 
                   ? fieldName 
                   : fieldDef != null 
                       ? GetQuotedColumnName(fieldDef) 
                       : GetQuotedColumnName(fieldName))}); \n";
    }

    public virtual List<string> ToCreateSequenceStatements(Type tableType)
    {
        return new List<string>();
    }

    public virtual string ToCreateSequenceStatement(Type tableType, string sequenceName)
    {
        return "";
    }

    public virtual string ToResetSequenceStatement(Type tableType, string columnName, int value)
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

    public virtual string ToAddColumnStatement(TableRef tableRef, FieldDefinition fieldDef) => 
        $"ALTER TABLE {QuoteTable(tableRef)} ADD COLUMN {GetColumnDefinition(fieldDef)};";

    public virtual string ToAlterColumnStatement(TableRef tableRef, FieldDefinition fieldDef) => 
        $"ALTER TABLE {QuoteTable(tableRef)} MODIFY COLUMN {GetColumnDefinition(fieldDef)};";
        
    public virtual string ToChangeColumnNameStatement(TableRef tableRef, FieldDefinition fieldDef, string oldColumn) => 
        $"ALTER TABLE {QuoteTable(tableRef)} CHANGE COLUMN {GetQuotedColumnName(oldColumn)} {GetColumnDefinition(fieldDef)};";

    public virtual string ToRenameColumnStatement(TableRef tableRef, string oldColumn, string newColumn) => 
        $"ALTER TABLE {QuoteTable(tableRef)} RENAME COLUMN {GetQuotedColumnName(oldColumn)} TO {GetQuotedColumnName(newColumn)};";

    public virtual string ToAddForeignKeyStatement<T, TForeign>(Expression<Func<T, object>> field,
        Expression<Func<TForeign, object>> foreignField,
        OnFkOption onUpdate,
        OnFkOption onDelete,
        string foreignKeyName = null)
    {
        var sourceMD = ModelDefinition<T>.Definition;
        var fieldDef = sourceMD.GetFieldDefinition(field);

        var referenceMD = ModelDefinition<TForeign>.Definition;
        var referenceFieldDef = referenceMD.GetFieldDefinition(foreignField);

        string name = GetQuotedName(foreignKeyName.IsNullOrEmpty() ?
            "fk_" + sourceMD.ModelName + "_" + fieldDef.FieldName + "_" + referenceFieldDef.FieldName :
            foreignKeyName);

        return $"ALTER TABLE {GetQuotedTableName(sourceMD)} " +
               $"ADD CONSTRAINT {name} FOREIGN KEY ({GetQuotedColumnName(fieldDef)}) " +
               $"REFERENCES {GetQuotedTableName(referenceMD)} " +
               $"({GetQuotedColumnName(referenceFieldDef)})" +
               $"{GetForeignKeyOnDeleteClause(new ForeignKeyConstraint(typeof(T), onDelete: FkOptionToString(onDelete)))}" +
               $"{GetForeignKeyOnUpdateClause(new ForeignKeyConstraint(typeof(T), onUpdate: FkOptionToString(onUpdate)))};";
    }

    public virtual string ToDropForeignKeyStatement(TableRef tableRef, string foreignKeyName) =>
        $"ALTER TABLE {QuoteTable(tableRef)} DROP CONSTRAINT {GetQuotedName(foreignKeyName)};";

    public virtual string ToDropConstraintStatement(TableRef tableRef, string constraintName) => null;

    public virtual string ToCreateIndexStatement<T>(Expression<Func<T, object>> field, string indexName = null, bool unique = false)
    {
        var sourceDef = ModelDefinition<T>.Definition;
        var fieldDef = sourceDef.GetFieldDefinition(field);

        string name = GetQuotedName(indexName.IsNullOrEmpty() ?
            (unique ? "uidx" : "idx") + "_" + sourceDef.ModelName + "_" + fieldDef.FieldName :
            indexName);

        string command = $"CREATE {(unique ? "UNIQUE" : "")} " +
                         $"INDEX {name} ON {GetQuotedTableName(sourceDef)}" +
                         $"({GetQuotedColumnName(fieldDef)});";
        return command;
    }

    public virtual string ToDropIndexStatement<T>(string indexName)
    {
        return $"DROP INDEX IF EXISTS {GetQuotedName(indexName)};";
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

    public virtual string ToDropColumnStatement(TableRef tableRef, string column) => 
        $"ALTER TABLE {QuoteTable(tableRef)} DROP COLUMN {GetQuotedColumnName(column)};";

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
    public virtual string SqlDateFormat(string quotedColumn, string format) => $"strftime({GetQuotedValue(format)},{quotedColumn})";
    public virtual string SqlChar(int charCode) => $"CHAR({charCode})";
    
    //Async API's, should be overriden by Dialect Providers to use .ConfigureAwait(false)
    //Default impl below uses TaskAwaiter shim in async.cs
    public virtual bool SupportsAsync => false;

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
}
