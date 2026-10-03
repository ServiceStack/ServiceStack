using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.OrmLite.MySql.Converters;
using ServiceStack.OrmLite.MySql.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.MySql;

public abstract class MySqlDialectProviderBase<TDialect> : OrmLiteDialectProviderBase<TDialect> where TDialect : IOrmLiteDialectProvider
{
    public override DbKind Kind => DbKind.MySql;

    private const string TextColumnDefinition = "TEXT";

    /// <summary>
    /// Errors where MySQL confirmed the statement wasn't applied: deadlock victims and connections that couldn't
    /// be made
    /// </summary>
    public static HashSet<int> NotAppliedErrors { get; } = [
        1040, 1042, 1203, 1213, 2002, 2003,
    ];

    /// <summary>
    /// Errors where the connection was lost, so the statement may have been applied
    /// </summary>
    public static HashSet<int> ConnectionLostErrors { get; } = [
        1053, 1158, 1159, 1160, 1161, 1927, 2006, 2013, 4031,
    ];

    /// <summary>
    /// Whether a MySQL error number is a temporary error
    /// </summary>
    protected static TransientError GetTransientError(int errorNumber) =>
        NotAppliedErrors.Contains(errorNumber) ? TransientError.NotApplied
        : ConnectionLostErrors.Contains(errorNumber) ? TransientError.MaybeApplied
        : TransientError.None;

	public MySqlDialectProviderBase()
	{
		base.AutoIncrementDefinition = "AUTO_INCREMENT";
		base.DefaultValueFormat = " DEFAULT {0}";
		base.SelectIdentitySql = "SELECT LAST_INSERT_ID()";
		base.QuoteChar = '`';

		base.InitColumnTypeMap();

		base.RegisterConverter<string>(new MySqlStringConverter());
		base.RegisterConverter<char[]>(new MySqlCharArrayConverter());
		base.RegisterConverter<bool>(new MySqlBoolConverter());

		base.RegisterConverter<byte>(new MySqlByteConverter());
		base.RegisterConverter<sbyte>(new MySqlSByteConverter());
		base.RegisterConverter<short>(new MySqlInt16Converter());
		base.RegisterConverter<ushort>(new MySqlUInt16Converter());
		base.RegisterConverter<int>(new MySqlInt32Converter());
		base.RegisterConverter<uint>(new MySqlUInt32Converter());

		base.RegisterConverter<decimal>(new MySqlDecimalConverter());

		base.RegisterConverter<Guid>(new MySqlGuidConverter());
		base.RegisterConverter<DateTimeOffset>(new MySqlDateTimeOffsetConverter());

		this.Variables = new Dictionary<string, string>
		{
			{ OrmLiteVariables.SystemUtc, "CURRENT_TIMESTAMP" },
			{ OrmLiteVariables.MaxText, "LONGTEXT" },
			{ OrmLiteVariables.MaxTextUnicode, "LONGTEXT" },
			{ OrmLiteVariables.True, SqlBool(true) },                
			{ OrmLiteVariables.False, SqlBool(false) },                
		};
	}

	public override bool SupportsSchema => false;

	// MariaDB doesn't support FOR UPDATE OF, so rows of all joined tables are locked
	public override string GetForUpdateClause(string lockTable, bool skipLocked) =>
		"FOR UPDATE" + (skipLocked ? " SKIP LOCKED" : "");

	/// <summary>
	/// MySQL's ON DUPLICATE KEY UPDATE also matches secondary UNIQUE constraints.
	/// Set to false to use OrmLite's primary-key-only Save fallback instead.
	/// </summary>
	public bool UseNativeUpsert { get; set; } = true;

	public override bool SupportsUpsert => UseNativeUpsert;

	// EXPLAIN returns a row for each table of the query. Analyzing uses EXPLAIN ANALYZE in MySQL 8.0.18+ which
	// returns the lines of the plan, and ANALYZE in MariaDB which returns rows with actual row counts.
	public override ExplainQuery ToExplainQuery(IDbConnection db, string sql, bool analyze)
	{
		if (!analyze)
			return new() { Sql = "EXPLAIN " + sql, ReadPlan = reader => ExplainQuery.ReadTable(reader) };

		var serverVersion = (db as DbConnection ?? db?.ToDbConnection() as DbConnection)?.ServerVersion;
		var isMariaDb = serverVersion?.IndexOf("MariaDB", StringComparison.OrdinalIgnoreCase) >= 0;
		return isMariaDb
			? new() { Sql = "ANALYZE " + sql, ReadPlan = reader => ExplainQuery.ReadTable(reader) }
			: new() { Sql = "EXPLAIN ANALYZE " + sql };
	}

	public override void PrepareParameterizedUpsertStatement<T>(IDbCommand cmd,
		ICollection<string> insertFields = null, ICollection<string> updateOnly = null)
	{
		PrepareUpsertFields<T>(cmd, insertFields, updateOnly,
			out var modelDef, out var insertFieldDefs, out var updateFieldDefs);

		var updateSql = updateFieldDefs.Count > 0
			? GetUpsertUpdateSql(updateFieldDefs)
			: $"{GetQuotedColumnName(modelDef.PrimaryKey)}={GetQuotedColumnName(modelDef.PrimaryKey)}";

		cmd.CommandText = $"{GetUpsertInsertSql(modelDef, insertFieldDefs)} " +
		                  $"ON DUPLICATE KEY UPDATE {updateSql}";
	}

	// DROP TABLE without TEMPORARY commits the current transaction
	protected override string ToDropBulkStagingTableStatement(string stagingTable) => "DROP TEMPORARY TABLE " + stagingTable;

	protected override string ToBulkUpsertStatement(ModelDefinition modelDef, string stagingTable,
		List<FieldDefinition> insertFieldDefs, List<FieldDefinition> updateFieldDefs)
	{
		var table = GetQuotedTableName(modelDef);
		var updateSql = updateFieldDefs.Count > 0
			? updateFieldDefs.Map(x => $"{table}.{GetQuotedColumnName(x)}=" +
				GetBulkUpsertValue(x.CustomUpdate, $"{stagingTable}.{GetQuotedColumnName(x)}")).Join(", ")
			: $"{table}.{GetQuotedColumnName(modelDef.PrimaryKey)}={table}.{GetQuotedColumnName(modelDef.PrimaryKey)}";

		return $"{GetBulkUpsertInsertSql(modelDef, stagingTable, insertFieldDefs)} ON DUPLICATE KEY UPDATE {updateSql}";
	}

	public static string RowVersionTriggerFormat = "{0}RowVersionUpdateTrigger";

	public static HashSet<string> ReservedWords = new([
		"ACCESSIBLE",
		"ADD",
		"ALL",
		"ALTER",
		"ANALYZE",
		"AND",
		"AS",
		"ASC",
		"ASENSITIVE",
		"BEFORE",
		"BETWEEN",
		"BIGINT",
		"BINARY",
		"BLOB",
		"BOTH",
		"BY",
		"CALL",
		"CASCADE",
		"CASE",
		"CHANGE",
		"CHAR",
		"CHARACTER",
		"CHECK",
		"COLLATE",
		"COLUMN",
		"CONDITION",
		"CONSTRAINT",
		"CONTINUE",
		"CONVERT",
		"CREATE",
		"CROSS",
		"CUBE",
		"CUME_DIST",
		"CURRENT_DATE",
		"CURRENT_TIME",
		"CURRENT_TIMESTAMP",
		"CURRENT_USER",
		"CURSOR",
		"DATABASE",
		"DATABASES",
		"DAY_HOUR",
		"DAY_MICROSECOND",
		"DAY_MINUTE",
		"DAY_SECOND",
		"DEC",
		"DECIMAL",
		"DECLARE",
		"DEFAULT",
		"DELAYED",
		"DELETE",
		"DENSE_RANK",
		"DESC",
		"DESCRIBE",
		"DETERMINISTIC",
		"DISTINCT",
		"DISTINCTROW",
		"DIV",
		"DOUBLE",
		"DROP",
		"DUAL",
		"EACH",
		"ELSE",
		"ELSEIF",
		"EMPTY",
		"ENCLOSED",
		"ESCAPED",
		"EXCEPT",
		"EXISTS",
		"EXIT",
		"EXPLAIN",
		"FALSE",
		"FETCH",
		"FIRST_VALUE",
		"FLOAT",
		"FLOAT4",
		"FLOAT8",
		"FOR",
		"FORCE",
		"FOREIGN",
		"FROM",
		"FULLTEXT",
		"FUNCTION",
		"GENERATED",
		"GET",
		"GRANT",
		"GROUP",
		"GROUPING",
		"GROUPS",
		"HAVING",
		"HIGH_PRIORITY",
		"HOUR_MICROSECOND",
		"HOUR_MINUTE",
		"HOUR_SECOND",
		"IF",
		"IGNORE",
		"IN",
		"INDEX",
		"INFILE",
		"INNER",
		"INOUT",
		"INSENSITIVE",
		"INSERT",
		"INT",
		"INT1",
		"INT2",
		"INT3",
		"INT4",
		"INT8",
		"INTEGER",
		"INTERVAL",
		"INTO",
		"IO_AFTER_GTIDS",
		"IO_BEFORE_GTIDS",
		"IS",
		"ITERATE",
		"JOIN",
		"JSON_TABLE",
		"KEY",
		"KEYS",
		"KILL",
		"LAG",
		"LAST_VALUE",
		"LEAD",
		"LEADING",
		"LEAVE",
		"LEFT",
		"LIKE",
		"LIMIT",
		"LINEAR",
		"LINES",
		"LOAD",
		"LOCALTIME",
		"LOCALTIMESTAMP",
		"LOCK",
		"LONG",
		"LONGBLOB",
		"LONGTEXT",
		"LOOP",
		"LOW_PRIORITY",
		"MASTER_BIND",
		"MASTER_SSL_VERIFY_SERVER_CERT",
		"MATCH",
		"MAXVALUE",
		"MEDIUMBLOB",
		"MEDIUMINT",
		"MEDIUMTEXT",
		"MIDDLEINT",
		"MINUTE_MICROSECOND",
		"MINUTE_SECOND",
		"MOD",
		"MODIFIES",
		"NATURAL",
		"NOT",
		"NO_WRITE_TO_BINLOG",
		"NTH_VALUE",
		"NTILE",
		"NULL",
		"NUMERIC",
		"OF",
		"ON",
		"OPTIMIZE",
		"OPTIMIZER_COSTS",
		"OPTION",
		"OPTIONALLY",
		"OR",
		"ORDER",
		"OUT",
		"OUTER",
		"OUTFILE",
		"OVER",
		"PARTITION",
		"PERCENT_RANK",
		"PERSIST",
		"PERSIST_ONLY",
		"PRECISION",
		"PRIMARY",
		"PROCEDURE",
		"PURGE",
		"RANGE",
		"RANK",
		"READ",
		"READS",
		"READ_WRITE",
		"REAL",
		"RECURSIVE",
		"REFERENCES",
		"REGEXP",
		"RELEASE",
		"RENAME",
		"REPEAT",
		"REPLACE",
		"REQUIRE",
		"RESIGNAL",
		"RESTRICT",
		"RETURN",
		"REVOKE",
		"RIGHT",
		"RLIKE",
		"ROW",
		"ROWS",
		"ROW_NUMBER",
		"SCHEMA",
		"SCHEMAS",
		"SECOND_MICROSECOND",
		"SELECT",
		"SENSITIVE",
		"SEPARATOR",
		"SET",
		"SHOW",
		"SIGNAL",
		"SMALLINT",
		"SPATIAL",
		"SPECIFIC",
		"SQL",
		"SQLEXCEPTION",
		"SQLSTATE",
		"SQLWARNING",
		"SQL_BIG_RESULT",
		"SQL_CALC_FOUND_ROWS",
		"SQL_SMALL_RESULT",
		"SSL",
		"STARTING",
		"STORED",
		"STRAIGHT_JOIN",
		"SYSTEM",
		"TABLE",
		"TERMINATED",
		"THEN",
		"TINYBLOB",
		"TINYINT",
		"TINYTEXT",
		"TO",
		"TRAILING",
		"TRIGGER",
		"TRUE",
		"UNDO",
		"UNION",
		"UNIQUE",
		"UNLOCK",
		"UNSIGNED",
		"UPDATE",
		"USAGE",
		"USE",
		"USING",
		"UTC_DATE",
		"UTC_TIME",
		"UTC_TIMESTAMP",
		"VALUES",
		"VARBINARY",
		"VARCHAR",
		"VARCHARACTER",
		"VARYING",
		"VIRTUAL",
		"WHEN",
		"WHERE",
		"WHILE",
		"WINDOW",
		"WITH",
		"WRITE",
		"XOR",
		"YEAR_MONTH",
		"ZEROFILL"
	], StringComparer.OrdinalIgnoreCase);

	public override void Init(string connectionString)
	{
		if (connectionString.ToLower().Contains("allowloadlocalinfile=true"))
		{
			AllowLoadLocalInfile = true;
		}
	}

	public override string GetLoadChildrenSubSelect<From>(SqlExpression<From> expr)
	{
		// Workaround for: MySQL - This version of MySQL doesn't yet support 'LIMIT & IN/ALL/ANY/SOME subquery
		return expr.Rows != null
			? $"SELECT * FROM ({base.GetLoadChildrenSubSelect(expr)}) AS SubQuery" 
			: base.GetLoadChildrenSubSelect(expr);
	}
        
	public override string ToPostDropTableStatement(ModelDefinition modelDef)
	{
		if (modelDef.RowVersion != null)
		{
			var triggerName = RowVersionTriggerFormat.Fmt(GetTableNameOnly(new(modelDef)));
			return "DROP TRIGGER IF EXISTS {0}".Fmt(GetQuotedName(triggerName));
		}
		return null;
	}

	public override string ToPostCreateTableStatement(ModelDefinition modelDef)
	{
		if (modelDef.RowVersion != null)
		{
			var triggerName = RowVersionTriggerFormat.Fmt(modelDef.ModelName);
			var triggerBody = "SET NEW.{0} = OLD.{0} + 1;".Fmt(
				modelDef.RowVersion.FieldName.SqlColumn(this));

			var sql = string.Format("CREATE TRIGGER {0} BEFORE UPDATE ON {1} FOR EACH ROW BEGIN {2} END;",
				triggerName, GetQuotedTableName(modelDef), triggerBody);

			return sql;
		}

		return null;
	}

	/// <summary>
	/// Returns the MySqlBulkLoader columns and SET expressions for loading CSV serialized rows of T.
	/// Guids are serialized in CSVs without dashes, so they're loaded into a variable and converted into the
	/// 'D' format stored in CHAR(36) columns, which MySqlConnector requires when reading them back.
	/// Not used by MySql.Data which treats @variables as params unless 'Allow User Variables=true'.
	/// </summary>
	internal static (List<string> Columns, List<string> Expressions) GetBulkLoadColumns<T>(IOrmLiteDialectProvider dialect)
	{
		var modelDef = ModelDefinition<T>.Definition;
		var columns = new List<string>();
		var expressions = new List<string>();
		foreach (var prop in CsvSerializer.PropertiesFor<T>())
		{
			var fieldDef = modelDef.GetFieldDefinition(prop.PropertyName);
			var column = dialect.GetQuotedColumnName(fieldDef);
			var fieldType = Nullable.GetUnderlyingType(fieldDef.ColumnType) ?? fieldDef.ColumnType;
			if (fieldType == typeof(Guid) && dialect.GetConverterBestMatch(fieldDef) is MySqlGuidConverter)
			{
				var variable = "@guid" + expressions.Count;
				columns.Add(variable);
				expressions.Add($"{column} = INSERT(INSERT(INSERT(INSERT(NULLIF({variable},''),9,0,'-'),14,0,'-'),19,0,'-'),24,0,'-')");
			}
			else
			{
				columns.Add(column);
			}
		}
		return (columns, expressions);
	}

	public override string GetQuotedValue(string paramValue)
	{
		return "'" + paramValue.Replace("\\", "\\\\").Replace("'", @"\'") + "'";
	}

	public override string GetQuotedValue(object value, Type fieldType)
	{
		if (value == null) 
			return "NULL";

		if (fieldType == typeof(byte[]))
			return "0x" + BitConverter.ToString((byte[])value).Replace("-", "");

		return base.GetQuotedValue(value, fieldType);
	}

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

	public override string QuoteSchema(string schema, string table) =>
		GetQuotedName(JoinSchema(schema, table));
	public override string JoinSchema(string schema, string table) => string.IsNullOrEmpty(schema) || table.StartsWith(schema + "_") 
		? table
		: schema + "_" + table;

	public override bool ShouldQuote(string name) => name != null && 
	                                                 (ReservedWords.Contains(name) || name.IndexOf(' ') >= 0 || name.IndexOf('.') >= 0);

	public override SqlExpression<T> SqlExpression<T>()
	{
		return new MySqlExpression<T>(this);
	}

	public override string ToTableNamesStatement(string schema)
	{
		return schema == null 
			? "SELECT table_name FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema = DATABASE()"
			: "SELECT table_name FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema = DATABASE() AND table_name LIKE {0}".SqlFmt(this, NamingStrategy.GetSchemaName(schema)  + "\\_%");
	}

	public override string ToTableNamesWithRowCountsStatement(bool live, string schema)
	{
		if (live)
			return null;
            
		return schema == null 
			? "SELECT table_name, table_rows FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema = DATABASE()"
			: "SELECT table_name, table_rows FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema = DATABASE() AND table_name LIKE {0}".SqlFmt(this, NamingStrategy.GetSchemaName(schema)  + "\\_%");
	}
        
	public override List<string> GetTableIndexNames(IDbConnection db, TableRef tableRef) => db.Column<string>(
		"SELECT DISTINCT INDEX_NAME FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_NAME = {0} AND TABLE_SCHEMA = {1}"
			.SqlFmt(UnquotedTable(tableRef), db.Database));

	public override bool DoesTableExist(IDbCommand dbCmd, TableRef tableRef)
	{
		var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0} AND TABLE_SCHEMA = {1}"
			.SqlFmt(UnquotedTable(tableRef), dbCmd.Connection.Database);

		var result = dbCmd.ExecLongScalar(sql);

		return result > 0;
	}

	public override async Task<bool> DoesTableExistAsync(IDbCommand dbCmd, TableRef tableRef, CancellationToken token=default)
	{
		var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0} AND TABLE_SCHEMA = {1}"
			.SqlFmt(UnquotedTable(tableRef), dbCmd.Connection.Database);

		var result = await dbCmd.ExecLongScalarAsync(sql, token);

		return result > 0;
	}

	public override bool DoesColumnExist(IDbConnection db, string columnName, TableRef tableRef)
	{
		var tableName = UnquotedTable(tableRef);
		var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS"
		          + " WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName AND TABLE_SCHEMA = @schema"
			          .SqlFmt(UnquotedTable(tableRef), columnName);
            
		var result = db.SqlScalar<long>(sql, new { tableName, columnName, schema = db.Database });

		return result > 0;
	}

	public override async Task<bool> DoesColumnExistAsync(IDbConnection db, string columnName, TableRef tableRef, CancellationToken token=default)
	{
		var tableName = UnquotedTable(tableRef);
		var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS"
		          + " WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName AND TABLE_SCHEMA = @schema"
			          .SqlFmt(tableName, columnName);
            
		var result = await db.SqlScalarAsync<long>(sql, new { tableName, columnName, schema = db.Database }, token);

		return result > 0;
	}

	public override string ToCreateTableStatement(Type tableType)
	{
		var sbColumns = StringBuilderCache.Allocate();
		var sbConstraints = StringBuilderCache.Allocate();

		var modelDef = GetModel(tableType);
		foreach (var fieldDef in CreateTableFieldsStrategy(modelDef))
		{
			if (fieldDef.ShouldSkipCreate())
				continue;

			if (sbColumns.Length != 0) sbColumns.Append(", \n  ");

			sbColumns.Append(GetColumnDefinition(fieldDef));
                
			var sqlConstraint = GetCheckConstraint(modelDef, fieldDef);
			if (sqlConstraint != null)
			{
				sbConstraints.Append(",\n" + sqlConstraint);
			}

			if (fieldDef.ForeignKey == null || OrmLiteConfig.SkipForeignKeys)
				continue;

			var refModelDef = GetModel(fieldDef.ForeignKey.ReferenceType);
			sbConstraints.AppendFormat(
				", \n\n  CONSTRAINT {0} FOREIGN KEY ({1}) REFERENCES {2} ({3})",
				GetQuotedName(fieldDef.ForeignKey.GetForeignKeyName(modelDef, refModelDef, NamingStrategy, fieldDef)),
				GetQuotedColumnName(fieldDef),
				GetQuotedTableName(refModelDef),
				GetQuotedColumnName(refModelDef.PrimaryKey));

			if (!string.IsNullOrEmpty(fieldDef.ForeignKey.OnDelete))
				sbConstraints.AppendFormat(" ON DELETE {0}", fieldDef.ForeignKey.OnDelete);

			if (!string.IsNullOrEmpty(fieldDef.ForeignKey.OnUpdate))
				sbConstraints.AppendFormat(" ON UPDATE {0}", fieldDef.ForeignKey.OnUpdate);
		}

		var uniqueConstraints = GetUniqueConstraints(modelDef);
		if (uniqueConstraints != null)
		{
			sbConstraints.Append(",\n" + uniqueConstraints);
		}

		var sql = $"CREATE TABLE {GetQuotedTableName(modelDef)} \n(\n  {StringBuilderCache.ReturnAndFree(sbColumns)}{StringBuilderCacheAlt.ReturnAndFree(sbConstraints)} \n); \n";

		return WithSystemVersioning(modelDef, sql);
	}

	public override List<string> GetSchemas(IDbCommand dbCmd)
	{
		var sql = "SELECT DISTINCT TABLE_SCHEMA FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA NOT IN ('information_schema', 'performance_schema', 'sys', 'mysql')";
		return dbCmd.SqlColumn<string>(sql);
	}

	public override Dictionary<string, List<string>> GetSchemaTables(IDbCommand dbCmd)
	{
		var sql = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA NOT IN ('information_schema', 'performance_schema', 'sys', 'mysql')";
		return dbCmd.Lookup<string, string>(sql);
	}
        
	public override bool DoesSchemaExist(IDbCommand dbCmd, string schemaName) => false;

	public override string ToCreateSchemaStatement(string schemaName)
	{
		// https://mariadb.com/kb/en/library/create-database/
		return $"SELECT 1";
	}
        
	public override string ToDropForeignKeyStatement(TableRef tableRef, string foreignKeyName) =>
		$"ALTER TABLE {QuoteTable(tableRef)} DROP FOREIGN KEY {GetQuotedName(foreignKeyName)};";

	public override string ToDropIndexStatement<T>(string indexName)
	{
		return $"DROP INDEX {GetQuotedName(indexName)} ON {GetQuotedTableName(typeof(T))}";
	}

	public override string GetColumnDefinition(FieldDefinition fieldDef)
	{
		if (fieldDef.PropertyInfo?.HasAttributeCached<TextAttribute>() == true)
		{
			var sql = StringBuilderCache.Allocate();
			sql.AppendFormat("{0} {1}", GetQuotedName(NamingStrategy.GetColumnName(fieldDef.FieldName)), TextColumnDefinition);
			sql.Append(fieldDef.IsNullable ? " NULL" : " NOT NULL");
			return StringBuilderCache.ReturnAndFree(sql);
		}

		var ret = base.GetColumnDefinition(fieldDef);
		if (fieldDef.IsRowVersion)
			return $"{ret} DEFAULT 1";
		if (!string.IsNullOrEmpty(fieldDef.Description))
			return $"{ret} COMMENT {GetQuotedValue(fieldDef.Description)}";

		return ret;
	}

	// MariaDB 10.3+ has system-versioned tables, MySQL doesn't
	// MySqlConnector doesn't return the rows affected by each statement of a batch
	public override bool SupportsBatchRowsAffected => false;

	public override bool SupportsSystemVersioning => IsMariaDb == true;

	protected override string GetSystemTimeColumnDefinition(FieldDefinition fieldDef) => SupportsSystemVersioning
		? $"{GetQuotedColumnName(fieldDef)} TIMESTAMP(6) GENERATED ALWAYS AS ROW {(fieldDef.IsRowStart ? "START" : "END")}"
		: throw SystemVersioningNotSupported();

	protected override string ToSystemVersionedTableStatement(ModelDefinition modelDef, string createTableSql)
	{
		if (!SupportsSystemVersioning)
			throw SystemVersioningNotSupported();

		GetSystemTimeFields(modelDef, out var rowStart, out var rowEnd);
		var definitions = rowStart != null
			? $"PERIOD FOR SYSTEM_TIME ({GetQuotedColumnName(rowStart)}, {GetQuotedColumnName(rowEnd)})"
			: null;
		return AddToCreateTable(createTableSql, definitions, " WITH SYSTEM VERSIONING");
	}

	// MySQL and MariaDB have no filtered indexes or INCLUDE
	protected override bool SupportsFilteredIndexes => false;

	/// <summary>
	/// Whether the server is MariaDB, which has different vector functions and a vector index. It's detected from
	/// the first connection that's opened, set it to use them before then.
	/// </summary>
	public bool? IsMariaDb { get; set; }

	public override void InitConnection(IDbConnection dbConn)
	{
		base.InitConnection(dbConn);
		IsMariaDb ??= (dbConn.ToDbConnection() as DbConnection)?.ServerVersion?
			.IndexOf("MariaDB", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	// Vectors use the VECTOR type of MariaDB 11.7+ and MySQL 9+
	public override string GetVectorColumnDefinition(int dimensions) => $"VECTOR({dimensions})";

	public override string ToVectorDistance(VectorDistance distance, string vector, string other)
	{
		if (IsMariaDb == true)
		{
			return distance switch {
				VectorDistance.Cosine => $"VEC_DISTANCE_COSINE({vector}, {other})",
				VectorDistance.L2 => $"VEC_DISTANCE_EUCLIDEAN({vector}, {other})",
				_ => throw new NotSupportedException("MariaDB doesn't have an inner product vector distance"),
			};
		}

		// Only in MySQL HeatWave and Enterprise
		var metric = distance switch {
			VectorDistance.Cosine => "COSINE",
			VectorDistance.L2 => "EUCLIDEAN",
			_ => "DOT",
		};
		return $"DISTANCE({vector}, {other}, '{metric}')";
	}

	// MariaDB has a vector index for NOT NULL columns, MySQL has none
	protected override string ToCreateVectorIndexStatement(ModelDefinition modelDef, FieldDefinition fieldDef, string indexName)
	{
		if (IsMariaDb != true)
			return null;
		if (fieldDef.VectorDistance == VectorDistance.NegativeInnerProduct)
			throw new NotSupportedException("MariaDB doesn't have an inner product vector distance");

		var distance = fieldDef.VectorDistance == VectorDistance.Cosine ? "cosine" : "euclidean";
		return $"CREATE VECTOR INDEX {indexName} ON {GetQuotedTableName(modelDef)} ({GetQuotedColumnName(fieldDef)}) DISTANCE={distance}; \n";
	}

	// Column comments are part of their definition
	public override List<string> ToCreateCommentStatements(Type tableType)
	{
		var modelDef = GetModel(tableType);
		return string.IsNullOrEmpty(modelDef.Description)
			? []
			: [$"ALTER TABLE {GetQuotedTableName(modelDef)} COMMENT = {GetQuotedValue(modelDef.Description)}"];
	}

	public override string SqlConflict(string sql, string conflictResolution)
	{
		var parts = sql.SplitOnFirst(' ');
		return $"{parts[0]} {conflictResolution} {parts[1]}";
	}

	public override string SqlCurrency(string fieldOrValue, string currencySymbol) =>
		SqlConcat(new[] {GetQuotedValue(currencySymbol), $"cast({fieldOrValue} as decimal(15,2))"});

	public override string SqlCast(object fieldOrValue, string castAs) => 
		castAs == Sql.VARCHAR
			? $"CAST({fieldOrValue} AS CHAR(1000))"
			: $"CAST({fieldOrValue} AS {castAs})";

	public override string SqlBool(bool value) => value ? "1" : "0";
	public override string SqlDateFormat(string quotedColumn, string format) => $"DATE_FORMAT({quotedColumn}, {GetQuotedValue(format)})";

	public override void EnableForeignKeysCheck(IDbCommand cmd) => cmd.ExecNonQuery("SET FOREIGN_KEY_CHECKS=1;");
	public override Task EnableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token = default) => 
		cmd.ExecNonQueryAsync("SET FOREIGN_KEY_CHECKS=1;", null, token);
	public override void DisableForeignKeysCheck(IDbCommand cmd) => cmd.ExecNonQuery("SET FOREIGN_KEY_CHECKS=0;");
	public override Task DisableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token = default) => 
		cmd.ExecNonQueryAsync("SET FOREIGN_KEY_CHECKS=0;", null, token);

	protected DbConnection Unwrap(IDbConnection db)
	{
		return (DbConnection)db.ToDbConnection();
	}

	protected DbCommand Unwrap(IDbCommand cmd)
	{
		return (DbCommand)cmd.ToDbCommand();
	}

	protected DbDataReader Unwrap(IDataReader reader)
	{
		return (DbDataReader)reader;
	}

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
