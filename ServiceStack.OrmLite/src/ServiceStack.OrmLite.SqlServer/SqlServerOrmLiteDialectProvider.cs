using ServiceStack.OrmLite.Converters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
#if MSDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.SqlServer.Converters;
using ServiceStack.Text;
#if NETCORE
using ApplicationException = System.InvalidOperationException;
#endif

namespace ServiceStack.OrmLite.SqlServer
{
    public class SqlServerOrmLiteDialectProvider : OrmLiteDialectProviderBase<SqlServerOrmLiteDialectProvider>
    {
        public override DbKind Kind => DbKind.SqlServer;
        public static SqlServerOrmLiteDialectProvider Instance = new();

        public SqlServerOrmLiteDialectProvider()
        {
            VectorConverter = new VectorTextConverter();
            base.AutoIncrementDefinition = "IDENTITY(1,1)";
            base.SelectIdentitySql = "SELECT SCOPE_IDENTITY()";

            base.InitColumnTypeMap();

            RowVersionConverter = new SqlServerRowVersionConverter();

            base.RegisterConverter<string>(new SqlServerStringConverter());
            base.RegisterConverter<bool>(new SqlServerBoolConverter());

            base.RegisterConverter<sbyte>(new SqlServerSByteConverter());
            base.RegisterConverter<ushort>(new SqlServerUInt16Converter());
            base.RegisterConverter<uint>(new SqlServerUInt32Converter());
            base.RegisterConverter<ulong>(new SqlServerUInt64Converter());

            base.RegisterConverter<float>(new SqlServerFloatConverter());
            base.RegisterConverter<double>(new SqlServerDoubleConverter());
            base.RegisterConverter<decimal>(new SqlServerDecimalConverter());

            base.RegisterConverter<DateTime>(new SqlServerDateTimeConverter());

            base.RegisterConverter<Guid>(new SqlServerGuidConverter());

            base.RegisterConverter<byte[]>(new SqlServerByteArrayConverter());

            this.Variables = new Dictionary<string, string>
            {
                { OrmLiteVariables.SystemUtc, "SYSUTCDATETIME()" },
                { OrmLiteVariables.MaxText, "VARCHAR(MAX)" },
                { OrmLiteVariables.MaxTextUnicode, "NVARCHAR(MAX)" },
                { OrmLiteVariables.True, SqlBool(true) },                
                { OrmLiteVariables.False, SqlBool(false) },                
            };
        }

        public override string GetQuotedValue(string paramValue)
        {
            return (StringConverter.UseUnicode ? "N'" : "'") + paramValue.Replace("'", "''") + "'";
        }

        public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
        {
            var isFullConnectionString = connectionString.Contains(";");

            if (!isFullConnectionString)
            {
                var filePath = connectionString;

                var filePathWithExt = filePath.EndsWithIgnoreCase(".mdf")
                    ? filePath
                    : filePath + ".mdf";

                var fileName = Path.GetFileName(filePathWithExt);
                var dbName = fileName.Substring(0, fileName.Length - ".mdf".Length);

                connectionString = $@"Data Source=.\SQLEXPRESS;AttachDbFilename={filePathWithExt};Initial Catalog={dbName};Integrated Security=True;User Instance=True;";
            }

            if (options != null)
            {
                foreach (var option in options)
                {
                    if (option.Key.ToLower() == "read only")
                    {
                        if (option.Value.ToLower() == "true")
                        {
                            connectionString += "Mode = Read Only;";
                        }
                        continue;
                    }
                    connectionString += option.Key + "=" + option.Value + ";";
                }
            }

            return new SqlConnection(connectionString);
        }

        public override SqlExpression<T> SqlExpression<T>() => new SqlServerExpression<T>(this);

        public override IDbDataParameter CreateParam() => new SqlParameter();

        private const string DefaultSchema = "dbo";
        public override string ToTableNamesStatement(string schema)
        {
            var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'";
            return sql + " AND TABLE_SCHEMA = {0}".SqlFmt(this, schema ?? DefaultSchema);
        }

        public override string ToTableNamesWithRowCountsStatement(bool live, string schema)
        {
            var schemaSql = " AND s.Name = {0}".SqlFmt(this, schema ?? DefaultSchema);
            
            var sql = @"SELECT t.NAME, p.rows FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
                               INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id 
                               INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
                         WHERE t.is_ms_shipped = 0 " + schemaSql + " GROUP BY t.NAME, p.Rows";
            return sql;
        }

        public override List<string> GetSchemas(IDbCommand dbCmd)
        {
            var sql = "SELECT DISTINCT TABLE_SCHEMA FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'";
            return dbCmd.SqlColumn<string>(sql);
        }

        public override Dictionary<string, List<string>> GetSchemaTables(IDbCommand dbCmd)
        {
            var sql = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'";
            return dbCmd.Lookup<string, string>(sql);
        }

        public override bool DoesSchemaExist(IDbCommand dbCmd, string schemaName)
        {
            var sql = $"SELECT count(*) FROM sys.schemas WHERE name = '{schemaName.SqlParam()}'";
            var result = dbCmd.ExecLongScalar(sql);
            return result > 0; 
        }

        public override async Task<bool> DoesSchemaExistAsync(IDbCommand dbCmd, string schemaName, CancellationToken token = default)
        {
            var sql = $"SELECT count(*) FROM sys.schemas WHERE name = '{schemaName.SqlParam()}'";
            var result = await dbCmd.ExecLongScalarAsync(sql, token);
            return result > 0; 
        }

        public override string ToCreateSchemaStatement(string schemaName)
        {
            var sql = $"CREATE SCHEMA [{NamingStrategy.GetSchemaName(schemaName).Replace("]", "]]")}]";
            return sql;
        }
        
        public override string ToCreateSavePoint(string name) => $"SAVE TRANSACTION {name}";
        public override string ToReleaseSavePoint(string name) => null;
        public override string ToRollbackSavePoint(string name) => $"ROLLBACK TRANSACTION {name}";

        public override bool DoesTableExist(IDbCommand dbCmd, TableRef tableRef)
        {
            var tableName = GetTableNameOnly(tableRef);
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}"
                .SqlFmt(this, tableName);

            var schema = GetSchemaName(tableRef);
            if (schema != null)
                sql += " AND TABLE_SCHEMA = {0}".SqlFmt(this, schema);
            else
                sql += " AND TABLE_SCHEMA <> 'Security'";

            var result = dbCmd.ExecLongScalar(sql);

            return result > 0;
        }

        public override async Task<bool> DoesTableExistAsync(IDbCommand dbCmd, TableRef tableRef, CancellationToken token = default)
        {
            var tableName = GetTableNameOnly(tableRef);
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}"
                .SqlFmt(this, tableName);

            var schema = GetSchemaName(tableRef);
            if (schema != null)
                sql += " AND TABLE_SCHEMA = {0}".SqlFmt(this, schema);
            else
                sql += " AND TABLE_SCHEMA <> 'Security'";

            var result = await dbCmd.ExecLongScalarAsync(sql, token);

            return result > 0;
        }

        public override bool DoesColumnExist(IDbConnection db, string columnName, TableRef tableRef)
        {
            var tableName = GetTableNameOnly(tableRef);
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName"
                .SqlFmt(this, tableName, columnName);

            var schema = GetSchemaName(tableRef);
            if (schema != null)
                sql += " AND TABLE_SCHEMA = @schema";

            var result = db.SqlScalar<long>(sql, new { tableName, columnName, schema });

            return result > 0;
        }

        public override async Task<bool> DoesColumnExistAsync(IDbConnection db, string columnName, TableRef tableRef,
            CancellationToken token = default)
        {
            var tableName = GetTableNameOnly(tableRef);
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName"
                .SqlFmt(this, tableName, columnName);

            var schema = GetSchemaName(tableRef);
            if (schema != null)
                sql += " AND TABLE_SCHEMA = @schema";

            var result = await db.SqlScalarAsync<long>(sql, new { tableName, columnName, schema }, token: token);

            return result > 0;
        }

        public override string GetForeignKeyOnDeleteClause(ForeignKeyConstraint foreignKey)
        {
            return "RESTRICT" == (foreignKey.OnDelete ?? "").ToUpper()
                ? ""
                : base.GetForeignKeyOnDeleteClause(foreignKey);
        }

        public override string GetForeignKeyOnUpdateClause(ForeignKeyConstraint foreignKey)
        {
            return "RESTRICT" == (foreignKey.OnUpdate ?? "").ToUpper()
                ? ""
                : base.GetForeignKeyOnUpdateClause(foreignKey);
        }

        public override string GetDropForeignKeyConstraints(ModelDefinition modelDef)
        {
            //TODO: find out if this should go in base class?
            var sb = StringBuilderCache.Allocate();
            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (fieldDef.ForeignKey != null)
                {
                    var foreignKeyName = fieldDef.ForeignKey.GetForeignKeyName(
                        modelDef,
                        OrmLiteUtils.GetModelDefinition(fieldDef.ForeignKey.ReferenceType),
                        NamingStrategy,
                        fieldDef);

                    var tableName = GetQuotedTableName(modelDef);
                    sb.AppendLine($"IF EXISTS (SELECT name FROM sys.foreign_keys WHERE name = '{foreignKeyName}')");
                    sb.AppendLine("BEGIN");
                    sb.AppendLine($"  ALTER TABLE {tableName} DROP {foreignKeyName};");
                    sb.AppendLine("END");
                }
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override string ToAddColumnStatement(TableRef tableRef, FieldDefinition fieldDef) => 
            $"ALTER TABLE {QuoteTable(tableRef)} ADD {GetColumnDefinition(fieldDef)};";

        public override string ToAlterColumnStatement(TableRef tableRef, FieldDefinition fieldDef) => 
            $"ALTER TABLE {QuoteTable(tableRef)} ALTER COLUMN {GetColumnDefinition(fieldDef)};";

        public override string ToChangeColumnNameStatement(TableRef tableRef, FieldDefinition fieldDef, string oldColumn)
        {
            var objectName = $"{QuoteTable(tableRef)}.{GetQuotedColumnName(oldColumn)}";
            return $"EXEC sp_rename {GetQuotedValue(objectName)}, {GetQuotedValue(fieldDef.FieldName)}, {GetQuotedValue("COLUMN")};";
        }

        public override string ToRenameColumnStatement(TableRef tableRef, string oldColumn, string newColumn)
        {
            var objectName = $"{QuoteTable(tableRef)}.{GetQuotedColumnName(oldColumn)}";
            return $"EXEC sp_rename {GetQuotedValue(objectName)}, {GetQuotedColumnName(newColumn)}, 'COLUMN';";
        }

        public override string ToDropIndexStatement<T>(string indexName)
        {
            return $"DROP INDEX IF EXISTS {GetQuotedName(indexName)} ON {GetQuotedTableName(typeof(T))};";
        }

        protected virtual string GetAutoIncrementDefinition(FieldDefinition fieldDef)
        {
            return AutoIncrementDefinition;
        }

        public override string GetAutoIdDefaultValue(FieldDefinition fieldDef)
        {
            return fieldDef.FieldType == typeof(Guid) 
                ? "newid()" 
                : null;
        }

        protected override bool SupportsIndexInclude => true;

        // System-versioned (temporal) tables need SQL Server 2016+
        public override bool SupportsSystemVersioning => true;

        // The period of a system-versioned table is in UTC
        public override DateTime ToSystemTime(DateTime time) =>
            time.Kind == DateTimeKind.Local ? time.ToUniversalTime() : time;

        protected override string GetSystemTimeColumnDefinition(FieldDefinition fieldDef) =>
            $"{GetQuotedColumnName(fieldDef)} DATETIME2 GENERATED ALWAYS AS ROW {(fieldDef.IsRowStart ? "START" : "END")} NOT NULL";

        // Previous versions are kept in a history table, in the table's schema
        private string GetHistoryTableName(ModelDefinition modelDef)
        {
            var tableRef = new TableRef(modelDef);
            return GetQuotedName(GetSchemaName(tableRef) ?? DefaultSchema) + "." +
                   GetQuotedName(modelDef.HistoryTable ?? GetTableNameOnly(tableRef) + "History");
        }

        protected override string ToSystemVersionedTableStatement(ModelDefinition modelDef, string createTableSql)
        {
            GetSystemTimeFields(modelDef, out var rowStart, out var rowEnd);
            var definitions = "";
            var startColumn = rowStart != null ? GetQuotedColumnName(rowStart) : GetQuotedName("SysStartTime");
            var endColumn = rowEnd != null ? GetQuotedColumnName(rowEnd) : GetQuotedName("SysEndTime");
            if (rowStart == null)
            {
                // The period needs columns, which aren't selected unless they're named
                definitions = $"{startColumn} DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,\n  " +
                              $"{endColumn} DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,\n  ";
            }
            definitions += $"PERIOD FOR SYSTEM_TIME ({startColumn}, {endColumn})";

            return AddToCreateTable(createTableSql, definitions,
                $" WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {GetHistoryTableName(modelDef)}))");
        }

        // A system-versioned table can't be dropped until it stops being versioned, which leaves its history table
        public override string ToDropTableStatement(ModelDefinition modelDef)
        {
            var table = GetQuotedTableName(modelDef);
            if (!modelDef.IsSystemVersioned)
                return $"DROP TABLE {table}";

            return $"IF OBJECTPROPERTY(OBJECT_ID({GetQuotedValue(table)}), 'TableTemporalType') = 2 " +
                   $"ALTER TABLE {table} SET (SYSTEM_VERSIONING = OFF);\n" +
                   $"DROP TABLE {table};\n" +
                   $"DROP TABLE IF EXISTS {GetHistoryTableName(modelDef)};";
        }

        // Vectors use the VECTOR type of SQL Server 2025 and Azure SQL
        public override string GetVectorColumnDefinition(int dimensions) => $"VECTOR({dimensions})";

        public override string ToVectorParam(string param, int dimensions) => $"CAST({param} AS VECTOR({dimensions}))";

        // Half-precision vectors are a preview feature of SQL Server 2025, which needs PREVIEW_FEATURES to be enabled
        public override string GetVectorColumnDefinition(int dimensions, VectorPrecision precision) =>
            precision == VectorPrecision.Half ? $"VECTOR({dimensions}, float16)" : GetVectorColumnDefinition(dimensions);

        public override string ToVectorParam(string param, int dimensions, VectorPrecision precision) =>
            precision == VectorPrecision.Half ? $"CAST({param} AS VECTOR({dimensions}, float16))" : ToVectorParam(param, dimensions);

        public override string ToVectorDistance(VectorDistance distance, string vector, string other)
        {
            var metric = distance switch {
                VectorDistance.Cosine => "cosine",
                VectorDistance.L2 => "euclidean",
                _ => "dot",
            };
            return $"VECTOR_DISTANCE('{metric}', {vector}, {other})";
        }

        // Computed columns have the type of their expression
        protected override string GetGeneratedColumnDefinition(FieldDefinition fieldDef) =>
            $"{GetQuotedColumnName(fieldDef)} AS ({ResolveColumnRefs(fieldDef.ModelDef, fieldDef.ComputeExpression)})" +
            (fieldDef.IsPersisted ? " PERSISTED" : "");

        public override List<string> ToCreateCommentStatements(Type tableType)
        {
            var to = new List<string>();
            var modelDef = GetModel(tableType);
            var tableRef = new TableRef(modelDef);
            string N(string text) => "N" + GetQuotedValue(text);
            // Tables without a [Schema] are created in the connection's default schema, which isn't always dbo
            var schema = GetSchemaName(tableRef);
            var table = "@level0type=N'SCHEMA', @level0name=" + (schema != null ? N(schema) : "@schema") +
                        ", @level1type=N'TABLE', @level1name=" + N(GetTableNameOnly(tableRef));
            var addDescription = (schema == null ? "DECLARE @schema sysname = SCHEMA_NAME(); " : "") +
                                 "EXEC sp_addextendedproperty @name=N'MS_Description', @value=";

            if (!string.IsNullOrEmpty(modelDef.Description))
                to.Add($"{addDescription}{N(modelDef.Description)}, {table}");

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (string.IsNullOrEmpty(fieldDef.Description) || fieldDef.ShouldSkipCreate())
                    continue;
                var column = GetQuotedColumnName(fieldDef).StripDbQuotes();
                to.Add($"{addDescription}{N(fieldDef.Description)}, {table}, @level2type=N'COLUMN', @level2name={N(column)}");
            }
            return to;
        }

        public override string GetColumnDefinition(FieldDefinition fieldDef)
        {
            // https://msdn.microsoft.com/en-us/library/ms182776.aspx
            if (fieldDef.IsRowVersion)
                return $"{fieldDef.FieldName} rowversion NOT NULL";
            if (fieldDef.IsRowStart || fieldDef.IsRowEnd)
                return GetSystemTimeColumnDefinition(fieldDef);
            if (fieldDef.IsGenerated)
                return GetGeneratedColumnDefinition(fieldDef);

            var fieldDefinition = GetFieldTypeDefinition(fieldDef);

            var sql = StringBuilderCache.Allocate();
            sql.Append($"{GetQuotedColumnName(fieldDef)} {fieldDefinition}");

            if (fieldDef.FieldType == typeof(string))
            {
                // https://msdn.microsoft.com/en-us/library/ms184391.aspx
                var collation = fieldDef.PropertyInfo?.FirstAttribute<SqlServerCollateAttribute>()?.Collation;
                if (!string.IsNullOrEmpty(collation))
                {
                    sql.Append($" COLLATE {collation}");
                }
            }

            if (fieldDef.IsPrimaryKey)
            {
                sql.Append(" PRIMARY KEY");

                if (fieldDef.IsNonClustered)
                    sql.Append(" NONCLUSTERED");
 
                if (fieldDef.AutoIncrement)
                {
                    sql.Append(" ").Append(GetAutoIncrementDefinition(fieldDef));
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
                if (fieldDef.DefaultValueConstraint != null)
                {
                    sql.Append(" CONSTRAINT ").Append(GetQuotedName(fieldDef.DefaultValueConstraint));
                }
                sql.AppendFormat(DefaultValueFormat, defaultValue);
            }

            return StringBuilderCache.ReturnAndFree(sql);
        }

        public override string ToDropConstraintStatement(TableRef tableRef, string constraintName) =>
            $"ALTER TABLE {QuoteTable(tableRef)} DROP CONSTRAINT {GetQuotedName(constraintName)};";

        public override void BulkInsert<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null)
        {
            config ??= new();
            if (config.Mode == BulkInsertMode.Sql)
            {
                base.BulkInsert(db, objs, config);
                return;
            }

            using var bulkCopy = CreateBulkCopy(db, objs, config, out var table);
            bulkCopy.WriteToServer(table);
        }

        public override async Task BulkInsertAsync<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null, CancellationToken token=default)
        {
            config ??= new();
            if (config.Mode == BulkInsertMode.Sql)
            {
                await base.BulkInsertAsync(db, objs, config, token).ConfigAwait();
                return;
            }

            using var bulkCopy = CreateBulkCopy(db, objs, config, out var table);
            await bulkCopy.WriteToServerAsync(table, token).ConfigAwait();
        }

        // A temporary table of the connection
        protected override string GetBulkStagingTableName(string name) => GetQuotedName("#" + name);

        protected override string ToCreateTempTableStatement(string tempTable, string columnDefinitions) =>
            $"CREATE TABLE {tempTable} (\n  {columnDefinitions}\n)";

        public override List<string> GetTableIndexNames(IDbConnection db, TableRef tableRef) => db.Column<string>(
            "SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID({0}) AND name IS NOT NULL"
                .SqlFmt(this, QuoteTable(tableRef)));

        public override List<IndexSchema> GetTableIndexes(IDbConnection db, TableRef tableRef)
        {
            string Columns(int included, string orderBy) =>
                "STUFF((SELECT ',' + c.name FROM sys.index_columns ic " +
                "JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id " +
                $"WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = {included} " +
                $"ORDER BY {orderBy} FOR XML PATH('')), 1, 1, '')";
            return ToIndexSchemas(db.SqlList<Dictionary<string, object>>(
                ("SELECT i.name AS name, i.is_unique AS is_unique, " +
                 "CAST(CASE WHEN i.is_primary_key = 1 OR i.is_unique_constraint = 1 THEN 1 ELSE 0 END AS bit) AS is_constraint, " +
                 $"i.is_primary_key AS is_primary_key, {Columns(0, "ic.key_ordinal")} AS columns, " +
                 $"{Columns(1, "ic.index_column_id")} AS include, i.filter_definition AS where_condition " +
                 "FROM sys.indexes i WHERE i.object_id = OBJECT_ID({0}) AND i.name IS NOT NULL")
                    .SqlFmt(this, QuoteTable(tableRef))));
        }

        public override List<string> GetModelIndexConditions(IDbConnection db, List<FieldDefinition> fieldDefs,
            List<IndexSchema> indexes) => ReadModelIndexConditions(db, fieldDefs, indexes, tempTable => ToColumnDefaults(
                db.SqlList<Dictionary<string, object>>(
                    "SELECT name AS name, filter_definition AS value FROM tempdb.sys.indexes WHERE object_id = OBJECT_ID({0})"
                        .SqlFmt(this, "tempdb.." + tempTable.StripDbQuotes()))));

        // The catalog full-text indexes are created in
        public const string FullTextCatalog = "ormlite_fts";

        // A full-text index keyed by the table's primary key. Its catalog and index can't be created in a transaction, so
        // they're created with EXEC, which runs them in batches of their own.
        public override List<string> ToCreateFullTextIndexStatements(ModelDefinition modelDef)
        {
            if (modelDef.PrimaryKey == null)
                throw new NotSupportedException($"SQL Server's full-text index of {modelDef.Name} needs a primary key");
            var table = GetQuotedTableName(modelDef);
            var language = modelDef.FullTextIndex?.Language;
            var languageTerm = string.IsNullOrEmpty(language) ? "0"
                : int.TryParse(language, out var lcid) ? lcid.ToString() : GetQuotedValue(language);
            var columns = GetFullTextColumnNames(modelDef).Map(x => $"{x} LANGUAGE {languageTerm}").Join(", ");
            var catalog = GetQuotedName(FullTextCatalog);
            return [
                $"IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = {GetQuotedValue(FullTextCatalog)}) " +
                $"EXEC({GetQuotedValue($"CREATE FULLTEXT CATALOG {catalog}")});",
                $"DECLARE @key sysname = (SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID({GetQuotedValue(table)}) AND is_primary_key = 1);\n" +
                $"DECLARE @sql nvarchar(max) = {GetQuotedValue($"CREATE FULLTEXT INDEX ON {table} ({columns}) KEY INDEX ")} + QUOTENAME(@key) + " +
                $"{GetQuotedValue($" ON {catalog} WITH (CHANGE_TRACKING = AUTO, STOPLIST = OFF)")};\n" +
                "EXEC(@sql);",
            ];
        }

        public override List<string> ToDropFullTextIndexStatements(ModelDefinition modelDef)
        {
            var table = GetQuotedTableName(modelDef);
            return [
                $"IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID({GetQuotedValue(table)})) " +
                $"EXEC({GetQuotedValue($"DROP FULLTEXT INDEX ON {table}")});",
            ];
        }

        // Full-Text Search is an optional component of SQL Server
        public override bool SupportsFullTextSearch(IDbConnection db) =>
            db.Scalar<int?>("SELECT CONVERT(int, FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))") == 1;

        public override bool HasFullTextIndex(IDbConnection db, ModelDefinition modelDef) =>
            db.Scalar<int>("SELECT COUNT(*) FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID({0})"
                .SqlFmt(this, GetQuotedTableName(modelDef))) > 0;

        // Rows are indexed in the background after they're written
        public override bool IsFullTextIndexUpToDate(IDbConnection db, ModelDefinition modelDef)
        {
            var objectId = "OBJECT_ID({0})".SqlFmt(this, GetQuotedTableName(modelDef));
            return db.Scalar<int>($"SELECT CASE WHEN OBJECTPROPERTYEX({objectId}, 'TableFulltextPendingChanges') = 0 " +
                $"AND OBJECTPROPERTYEX({objectId}, 'TableFulltextPopulateStatus') = 0 THEN 1 ELSE 0 END") == 1;
        }

        // CONTAINS() of several columns only matches rows with every word of its search in the same column, so each word
        // and phrase is matched by a CONTAINS() of its own, which matches it in any column
        public override bool FullTextMatchesEachTerm => true;

        // Words are prefixes of the words they match, e.g. "data*"
        public override List<string> ToFullTextSearch(List<FullTextTerm> terms) =>
            terms.Map(x => x.IsPhrase ? "\"" + x.Words.Join(" ") + "\"" : "\"" + x.Words[0] + "*\"");

        public override string ToFullTextMatch(FullTextColumns columns, List<string> parameters) =>
            "(" + parameters.Map(p => $"CONTAINS(({columns.Columns.Join(", ")}), {p})").Join(" AND ") + ")";

        // The sum of the rank of each word and phrase
        public override string ToFullTextRank(FullTextColumns columns, List<string> parameters)
        {
            var ftColumns = GetFullTextColumnNames(columns.ModelDef).Join(", ");
            return "(" + parameters.Map(p => $"COALESCE((SELECT ft.[RANK] FROM CONTAINSTABLE({columns.Table}, ({ftColumns}), {p}) ft " +
                $"WHERE ft.[KEY] = {columns.PrimaryKey}), 0)").Join(" + ") + ")";
        }

        // Temporary tables, e.g. of GetModelCheckConstraints(), are in tempdb
        public override List<CheckConstraintSchema> GetCheckConstraints(IDbConnection db, string quotedTable)
        {
            var isTemp = quotedTable.IndexOf('#') >= 0;
            var catalog = isTemp ? "tempdb." : "";
            var objectName = isTemp ? "tempdb.." + quotedTable.StripDbQuotes() : quotedTable;
            return ToCheckConstraintSchemas(db.SqlList<Dictionary<string, object>>(
                ($"SELECT cc.name AS name, cc.definition AS condition FROM {catalog}sys.check_constraints cc " +
                 "WHERE cc.parent_object_id = OBJECT_ID({0})").SqlFmt(this, objectName)));
        }

        // Temporary tables, e.g. of GetModelSchemaColumns(), are in tempdb
        public override Dictionary<string, string> GetColumnDefaults(IDbConnection db, string quotedTable)
        {
            var isTemp = quotedTable.IndexOf('#') >= 0;
            var catalog = isTemp ? "tempdb." : "";
            var objectName = isTemp ? "tempdb.." + quotedTable.StripDbQuotes() : quotedTable;
            return ToColumnDefaults(db.SqlList<Dictionary<string, object>>(
                ($"SELECT c.name AS name, dc.definition AS value FROM {catalog}sys.columns c " +
                 $"LEFT JOIN {catalog}sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id " +
                 "WHERE c.object_id = OBJECT_ID({0})").SqlFmt(this, objectName)));
        }

        public override List<ForeignKeySchema> GetTableForeignKeys(IDbConnection db, TableRef tableRef)
        {
            string Columns(string table, string column) =>
                "STUFF((SELECT ',' + c.name FROM sys.foreign_key_columns fkc " +
                $"JOIN sys.columns c ON c.object_id = fkc.{table} AND c.column_id = fkc.{column} " +
                "WHERE fkc.constraint_object_id = fk.object_id ORDER BY fkc.constraint_column_id FOR XML PATH('')), 1, 1, '')";
            return ToForeignKeySchemas(db.SqlList<Dictionary<string, object>>(
                ($"SELECT fk.name AS name, {Columns("parent_object_id", "parent_column_id")} AS columns, " +
                 $"OBJECT_NAME(fk.referenced_object_id) AS ref_table, {Columns("referenced_object_id", "referenced_column_id")} AS ref_columns, " +
                 "fk.delete_referential_action_desc AS on_delete, fk.update_referential_action_desc AS on_update " +
                 "FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID({0})").SqlFmt(this, QuoteTable(tableRef))));
        }

        // Defaults are constraints, so the column's default is dropped by its name before it's added
        public override string ToAlterColumnDefaultStatement(TableRef tableRef, FieldDefinition fieldDef)
        {
            var table = QuoteTable(tableRef);
            var sql = "DECLARE @default sysname = (SELECT dc.name FROM sys.default_constraints dc " +
                "JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id " +
                "WHERE dc.parent_object_id = OBJECT_ID({0}) AND c.name = {1});\n".SqlFmt(this, table, NamingStrategy.GetColumnName(fieldDef.FieldName)) +
                $"DECLARE @drop nvarchar(max) = {GetQuotedValue($"ALTER TABLE {table} DROP CONSTRAINT ")} + QUOTENAME(@default);\n" +
                "IF @default IS NOT NULL EXEC(@drop);";
            var defaultValue = GetDefaultValue(fieldDef);
            if (string.IsNullOrEmpty(defaultValue))
                return sql;
            var constraint = fieldDef.DefaultValueConstraint != null ? $" CONSTRAINT {GetQuotedName(fieldDef.DefaultValueConstraint)}" : "";
            return sql + $"\nALTER TABLE {table} ADD{constraint} DEFAULT {defaultValue} FOR {GetQuotedColumnName(fieldDef)};";
        }

        // UNION ALL stops the staging table inheriting the IDENTITY of the table's column, so it can be given values
        protected override string ToCreateBulkStagingTableStatement(ModelDefinition modelDef, string stagingTable, List<FieldDefinition> fieldDefs)
        {
            var columns = fieldDefs.Map(GetQuotedColumnName).Join(",");
            var table = GetQuotedTableName(modelDef);
            return $"SELECT {columns} INTO {stagingTable} FROM {table} WHERE 1=0 " +
                   $"UNION ALL SELECT {columns} FROM {table} WHERE 1=0";
        }

        protected override string ToBulkUpsertStatement(ModelDefinition modelDef, string stagingTable,
            List<FieldDefinition> insertFieldDefs, List<FieldDefinition> updateFieldDefs)
        {
            var quotedPrimaryKey = GetQuotedColumnName(modelDef.PrimaryKey);
            var whenMatched = updateFieldDefs.Count > 0
                ? "WHEN MATCHED THEN UPDATE SET " + updateFieldDefs.Map(x =>
                    $"target.{GetQuotedColumnName(x)}=" + GetBulkUpsertValue(x.CustomUpdate, "source." + GetQuotedColumnName(x))).Join(", ") + " "
                : "";

            return $"MERGE INTO {GetQuotedTableName(modelDef)} WITH (HOLDLOCK) AS target " +
                   $"USING {stagingTable} AS source " +
                   $"ON target.{quotedPrimaryKey}=source.{quotedPrimaryKey} " +
                   whenMatched +
                   $"WHEN NOT MATCHED THEN INSERT ({insertFieldDefs.Map(GetQuotedColumnName).Join(",")}) " +
                   $"VALUES ({insertFieldDefs.Map(x => GetBulkUpsertValue(x.CustomInsert, "source." + GetQuotedColumnName(x))).Join(",")});";
        }

        // Rows are loaded with SqlBulkCopy
        protected override void BulkLoad<T>(IDbConnection db, string quotedTable, List<FieldDefinition> fieldDefs, IEnumerable<T> rows, BulkInsertConfig config)
        {
            if (config.Mode == BulkInsertMode.Sql)
            {
                base.BulkLoad(db, quotedTable, fieldDefs, rows, config);
                return;
            }

            using var bulkCopy = CreateBulkCopy(db, quotedTable, fieldDefs, rows, config, GetBulkLoadValue, out var table);
            bulkCopy.WriteToServer(table);
        }

        protected override async Task BulkLoadAsync<T>(IDbConnection db, string quotedTable, List<FieldDefinition> fieldDefs, IEnumerable<T> rows, BulkInsertConfig config, CancellationToken token)
        {
            if (config.Mode == BulkInsertMode.Sql)
            {
                await base.BulkLoadAsync(db, quotedTable, fieldDefs, rows, config, token).ConfigAwait();
                return;
            }

            using var bulkCopy = CreateBulkCopy(db, quotedTable, fieldDefs, rows, config, GetBulkLoadValue, out var table);
            await bulkCopy.WriteToServerAsync(table, token).ConfigAwait();
        }

        private SqlBulkCopy CreateBulkCopy<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config, out DataTable table)
        {
            var modelDef = ModelDefinition<T>.Definition;
            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields:config.InsertFields)
                .Where(x => !ShouldSkipInsert(x) || x.AutoId)
                .ToList();

            return CreateBulkCopy(db, GetQuotedTableName(modelDef), fieldDefs, objs, config,
                (fieldDef, obj) => fieldDef.AutoId ? GetInsertDefaultValue(fieldDef) : fieldDef.GetValue(obj), out table);
        }

        private SqlBulkCopy CreateBulkCopy<T>(IDbConnection db, string quotedTable, List<FieldDefinition> fieldDefs,
            IEnumerable<T> objs, BulkInsertConfig config, Func<FieldDefinition, object, object> getValue, out DataTable table)
        {
            var sqlConn = (SqlConnection)db.ToDbConnection();
            // Rows are copied in the connection's transaction, which SqlBulkCopy doesn't use unless it's given it
            var sqlTrans = db.GetTransaction()?.ToDbTransaction() as SqlTransaction;
            var bulkCopy = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.Default, sqlTrans);

            bulkCopy.BatchSize = config.BatchSize;
            bulkCopy.DestinationTableName = quotedTable;
            
            table = new DataTable();
            foreach (var fieldDef in fieldDefs)
            {
                var columnName = NamingStrategy.GetColumnName(fieldDef.FieldName);
                bulkCopy.ColumnMappings.Add(columnName, columnName);
                
                var converter = GetConverterBestMatch(fieldDef);
                var colType = converter.DbType switch
                {
                    DbType.String => typeof(string),
                    DbType.Int32 => typeof(int),
                    DbType.Int64 => typeof(long),
                    _ => Nullable.GetUnderlyingType(fieldDef.FieldType) ?? fieldDef.FieldType
                };

                table.Columns.Add(columnName, colType);
            }

            foreach (var obj in objs)
            {
                var row = table.NewRow();
                foreach (var fieldDef in fieldDefs)
                {
                    var value = getValue(fieldDef, obj);

                    var converter = GetConverterBestMatch(fieldDef);
                    var dbValue = converter.ToDbValue(fieldDef.FieldType, value);
                    var columnName = NamingStrategy.GetColumnName(fieldDef.FieldName);
                    dbValue ??= DBNull.Value;
                    row[columnName] = dbValue;
                }
                table.Rows.Add(row);
            }
            return bulkCopy;
        }
        
        public override string ToInsertRowStatement(IDbCommand cmd, object objWithProperties, ICollection<string> insertFields = null)
        {
            var sbColumnNames = StringBuilderCache.Allocate();
            var sbColumnValues = StringBuilderCacheAlt.Allocate();
            var sbReturningColumns = StringBuilderCacheAlt.Allocate();
            var tableType = objWithProperties.GetType();
            var modelDef = GetModel(tableType);

            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields);
            foreach (var fieldDef in fieldDefs)
            {
                if (ShouldReturnOnInsert(modelDef, fieldDef))
                {
                    if (sbReturningColumns.Length > 0)
                        sbReturningColumns.Append(",");
                    sbReturningColumns.Append("INSERTED." + GetQuotedColumnName(fieldDef));
                }

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

            foreach (var fieldDef in modelDef.AutoIdFields) // need to include any AutoId fields that weren't included 
            {
                if (fieldDefs.Contains(fieldDef))
                    continue;

                if (sbReturningColumns.Length > 0)
                    sbReturningColumns.Append(",");
                sbReturningColumns.Append("INSERTED." + GetQuotedColumnName(fieldDef));
            }

            var strReturning = StringBuilderCacheAlt.ReturnAndFree(sbReturningColumns);
            strReturning = strReturning.Length > 0 ? "OUTPUT " + strReturning + " " : "";
            var sql = sbColumnNames.Length > 0
                ? $"INSERT INTO {GetQuotedTableName(modelDef)} ({StringBuilderCache.ReturnAndFree(sbColumnNames)}) " +
                  strReturning +
                  $"VALUES ({StringBuilderCacheAlt.ReturnAndFree(sbColumnValues)})"
                : $"INSERT INTO {GetQuotedTableName(modelDef)} {strReturning} DEFAULT VALUES";

            return sql;
        }

        protected string Sequence(string schema, string sequence)
        {
            if (schema == null)
                return GetQuotedName(sequence);

            return QuoteSchemaName(NamingStrategy.GetSchemaName(schema))
                   + "."
                   + GetQuotedName(sequence);
        }

        protected override bool ShouldSkipInsert(FieldDefinition fieldDef) => 
            fieldDef.ShouldSkipInsert() || fieldDef.AutoId;

        protected virtual bool ShouldReturnOnInsert(ModelDefinition modelDef, FieldDefinition fieldDef) =>
            fieldDef.ReturnOnInsert || (fieldDef.IsPrimaryKey && fieldDef.AutoIncrement && HasInsertReturnValues(modelDef)) || fieldDef.AutoId;

        // UPDLOCK holds the lock until the end of the transaction, READPAST skips locked rows
        public override string GetForUpdateTableHint(bool skipLocked) =>
            skipLocked ? "WITH (UPDLOCK, ROWLOCK, READPAST)" : "WITH (UPDLOCK, ROWLOCK)";

        public override string GetForUpdateClause(string lockTable, bool skipLocked) => null;

        public override bool HasInsertReturnValues(ModelDefinition modelDef) =>
            modelDef.FieldDefinitions.Any(x => x.ReturnOnInsert || (x.AutoId && x.FieldType == typeof(Guid)));

        protected virtual bool SupportsSequences(FieldDefinition fieldDef) => false;

        public override void EnableIdentityInsert<T>(IDbCommand cmd)
        {
            var tableName = cmd.GetDialectProvider().GetQuotedTableName(ModelDefinition<T>.Definition);
            cmd.Parameters.Clear(); // a SET that's sent with params only lasts for its statement
            cmd.ExecNonQuery($"SET IDENTITY_INSERT {tableName} ON");
        }

        public override Task EnableIdentityInsertAsync<T>(IDbCommand cmd, CancellationToken token=default)
        {
            var tableName = cmd.GetDialectProvider().GetQuotedTableName(ModelDefinition<T>.Definition);
            cmd.Parameters.Clear(); // a SET that's sent with params only lasts for its statement
            return cmd.ExecNonQueryAsync($"SET IDENTITY_INSERT {tableName} ON", null, token);
        }

        public override void DisableIdentityInsert<T>(IDbCommand cmd)
        {
            var tableName = cmd.GetDialectProvider().GetQuotedTableName(ModelDefinition<T>.Definition);
            cmd.Parameters.Clear(); // a SET that's sent with params only lasts for its statement
            cmd.ExecNonQuery($"SET IDENTITY_INSERT {tableName} OFF");
        }

        public override Task DisableIdentityInsertAsync<T>(IDbCommand cmd, CancellationToken token=default)
        {
            var tableName = cmd.GetDialectProvider().GetQuotedTableName(ModelDefinition<T>.Definition);
            cmd.Parameters.Clear(); // a SET that's sent with params only lasts for its statement
            return cmd.ExecNonQueryAsync($"SET IDENTITY_INSERT {tableName} OFF", null, token);
        }

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
                    if (sbReturningColumns.Length > 0)
                        sbReturningColumns.Append(",");
                    sbReturningColumns.Append("INSERTED." + GetQuotedColumnName(fieldDef));
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

                    if (SupportsSequences(fieldDef))
                    {
                        sbColumnValues.Append("NEXT VALUE FOR " + Sequence(NamingStrategy.GetSchemaName(modelDef), fieldDef.Sequence));
                    }
                    else
                    {
                        sbColumnValues.Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName),fieldDef.CustomInsert));
                        AddParameter(cmd, fieldDef);
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

                if (sbReturningColumns.Length > 0)
                    sbReturningColumns.Append(",");
                sbReturningColumns.Append("INSERTED." + GetQuotedColumnName(fieldDef));
            }

            var strReturning = StringBuilderCacheAlt.ReturnAndFree(sbReturningColumns);
            strReturning = strReturning.Length > 0 ? "OUTPUT " + strReturning + " " : "";
            cmd.CommandText = sbColumnNames.Length > 0
                ? $"INSERT INTO {GetQuotedTableName(modelDef)} ({StringBuilderCache.ReturnAndFree(sbColumnNames)}) {strReturning}" +                              
                  $"VALUES ({StringBuilderCacheAlt.ReturnAndFree(sbColumnValues)})"
                : $"INSERT INTO {GetQuotedTableName(modelDef)}{strReturning} DEFAULT VALUES";
        }

        public override bool SupportsUpsert => true;

        // MERGE ... OUTPUT INSERTED.* returns the row as it is after the insert or update
        public override string ToUpsertReturningStatement(string sql, ModelDefinition modelDef, ICollection<FieldDefinition> returnFields = null) =>
            sql.TrimEnd().TrimEnd(';') + " " + GetOutputClause(modelDef, "INSERTED", returnFields) + ";";

        /// <summary>
        /// OUTPUT clause of all the table's columns, or only the returnFields
        /// </summary>
        private string GetOutputClause(ModelDefinition modelDef, string prefix, ICollection<FieldDefinition> returnFields)
        {
            var returnAll = returnFields == null || returnFields.Count == 0;
            var sb = StringBuilderCache.Allocate();
            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (fieldDef.CustomSelect != null || (!returnAll && !returnFields.Contains(fieldDef)))
                    continue;
                sb.Append(sb.Length == 0 ? "OUTPUT " : ", ").Append(prefix).Append('.').Append(GetQuotedColumnName(fieldDef));
            }
            if (sb.Length == 0)
                throw new ArgumentException($"No columns of {modelDef.Name} to return", nameof(returnFields));
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override void PrepareParameterizedUpsertStatement<T>(IDbCommand cmd,
            ICollection<string> insertFields = null, ICollection<string> updateOnly = null)
        {
            PrepareUpsertFields<T>(cmd, insertFields, updateOnly,
                out var modelDef, out var insertFieldDefs, out var updateFieldDefs);

            var primaryKey = modelDef.PrimaryKey;
            var quotedPrimaryKey = GetQuotedColumnName(primaryKey);
            var primaryKeyParam = this.GetParam(SanitizeFieldNameForParamName(primaryKey.FieldName));
            var insertColumns = insertFieldDefs.Map(GetQuotedColumnName).Join(",");
            var insertValues = insertFieldDefs.Map(x =>
                this.GetParam(SanitizeFieldNameForParamName(x.FieldName), x.CustomInsert)).Join(",");
            var whenMatched = updateFieldDefs.Count > 0
                ? "WHEN MATCHED THEN UPDATE SET " + GetUpsertUpdateSql(updateFieldDefs, "target.") + " "
                : "";

            cmd.CommandText = $"MERGE INTO {GetQuotedTableName(modelDef)} WITH (HOLDLOCK) AS target " +
                              $"USING (VALUES ({primaryKeyParam})) AS source ({quotedPrimaryKey}) " +
                              $"ON target.{quotedPrimaryKey}=source.{quotedPrimaryKey} " +
                              whenMatched +
                              $"WHEN NOT MATCHED THEN INSERT ({insertColumns}) VALUES ({insertValues});";
        }

        public override void PrepareInsertRowStatement<T>(IDbCommand dbCmd, Dictionary<string, object> args)
        {
            var sbColumnNames = StringBuilderCache.Allocate();
            var sbColumnValues = StringBuilderCacheAlt.Allocate();
            var sbReturningColumns = StringBuilderCacheAlt.Allocate();
            var modelDef = OrmLiteUtils.GetModelDefinition(typeof(T));

            dbCmd.Parameters.Clear();

            foreach (var entry in args)
            {
                var fieldDef = modelDef.AssertFieldDefinition(entry.Key);

                if (ShouldReturnOnInsert(modelDef, fieldDef))
                {
                    if (sbReturningColumns.Length > 0)
                        sbReturningColumns.Append(",");
                    sbReturningColumns.Append("INSERTED." + GetQuotedColumnName(fieldDef));
                }

                if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
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

            var strReturning = StringBuilderCacheAlt.ReturnAndFree(sbReturningColumns);
            strReturning = strReturning.Length > 0 ? "OUTPUT " + strReturning + " " : "";
            dbCmd.CommandText = sbColumnNames.Length > 0
                ? $"INSERT INTO {GetQuotedTableName(modelDef)} ({StringBuilderCache.ReturnAndFree(sbColumnNames)}) {strReturning}" +                                
                  $"VALUES ({StringBuilderCacheAlt.ReturnAndFree(sbColumnValues)})"
                : $"INSERT INTO {GetQuotedTableName(modelDef)} {strReturning}DEFAULT VALUES";
        }
 
        public override string ToSelectStatement(QueryType queryType, ModelDefinition modelDef,
            string selectExpression,
            string bodyExpression,
            string orderByExpression = null,
            int? offset = null,
            int? rows = null,
            ISet<string> tags=null)
        {
            var sb = StringBuilderCache.Allocate();
            ApplyTags(sb, tags);

            sb.Append(selectExpression)
            .Append(bodyExpression);

            if (!offset.HasValue && !rows.HasValue || (queryType != QueryType.Select && rows != 1))
                return StringBuilderCache.ReturnAndFree(sb) + orderByExpression;

            if (offset is < 0)
                throw new ArgumentException($"Skip value:'{offset.Value}' must be>=0");

            if (rows is < 0)
                throw new ArgumentException($"Rows value:'{rows.Value}' must be>=0");

            var skip = offset ?? 0;
            var take = rows ?? int.MaxValue;

            var selectType = selectExpression.StartsWithIgnoreCase("SELECT DISTINCT") ? "SELECT DISTINCT" : "SELECT";

            //avoid Windowing function if unnecessary
            if (skip == 0)
            {
                var sql = StringBuilderCache.ReturnAndFree(sb) + orderByExpression;
                return SqlTop(sql, take, selectType);
            }

            // Required because ordering is done by Windowing function
            if (string.IsNullOrEmpty(orderByExpression))
            {
                if (modelDef.PrimaryKey == null)
                    throw new ApplicationException("Malformed model, no PrimaryKey defined");

                orderByExpression = $"ORDER BY {this.GetQuotedColumnName(modelDef, modelDef.PrimaryKey)}";
            }

            var row = take == int.MaxValue ? take : skip + take;

            var ret = $"SELECT * FROM (SELECT {selectExpression.Substring(selectType.Length)}, ROW_NUMBER() OVER ({orderByExpression}) As RowNum {bodyExpression}) AS RowConstrainedResult WHERE RowNum > {skip} AND RowNum <= {row}";

            return ret;
        }

        protected static string SqlTop(string sql, int take, string selectType = null)
        {
            selectType ??= sql.StartsWithIgnoreCase("SELECT DISTINCT") ? "SELECT DISTINCT" : "SELECT";

            if (take == int.MaxValue)
                return sql;

            if (sql.Length < "SELECT".Length)
                return sql;

            return sql.Substring(0, sql.IndexOf(selectType)) + selectType + " TOP " + take + sql.Substring(sql.IndexOf(selectType) + selectType.Length);
        }

        //SELECT without RowNum and prefer aliases to be able to use in SELECT IN () Reference Queries
        public static string UseAliasesOrStripTablePrefixes(string selectExpression)
        {
            if (selectExpression.IndexOf('.') < 0)
                return selectExpression;

            var sb = StringBuilderCache.Allocate();
            var selectToken = selectExpression.SplitOnFirst(' ');
            var tokens = selectToken[1].Split(',');
            foreach (var token in tokens)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                var field = token.Trim();

                var aliasParts = field.SplitOnLast(' ');
                if (aliasParts.Length > 1)
                {
                    sb.Append(" " + aliasParts[aliasParts.Length - 1]);
                    continue;
                }

                var parts = field.SplitOnLast('.');
                if (parts.Length > 1)
                {
                    sb.Append(" " + parts[parts.Length - 1]);
                }
                else
                {
                    sb.Append(" " + field);
                }
            }

            var sqlSelect = selectToken[0] + " " + StringBuilderCache.ReturnAndFree(sb).Trim();
            return sqlSelect;
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

        public override string SqlCurrency(string fieldOrValue, string currencySymbol) => 
            SqlConcat(new[] { GetQuotedValue(currencySymbol), $"CONVERT(VARCHAR, CONVERT(MONEY, {fieldOrValue}), 1)" });

        public override string SqlBool(bool value) => value ? "1" : "0";

        /// <summary>
        /// Adds an OUTPUT clause before the statement's WHERE clause, e.g:
        /// UPDATE "Table" SET ... OUTPUT INSERTED."Id", ... WHERE ...
        /// Note: SQL Server doesn't allow OUTPUT without INTO on tables with enabled triggers
        /// </summary>
        // SHOWPLAN_TEXT returns the estimated plan without running the query, but not for statements with params,
        // so they're merged into the SQL. STATISTICS PROFILE runs the query and returns the plan with actual row
        // counts after its results.
        public override ExplainQuery ToExplainQuery(IDbConnection db, string sql, bool analyze) => analyze
            ? new() {
                Sql = sql,
                BeforeSql = "SET STATISTICS PROFILE ON",
                AfterSql = "SET STATISTICS PROFILE OFF",
                ReadPlan = reader => ExplainQuery.ReadTable(reader, "Rows", "Executes", "StmtText"),
            }
            : new() {
                Sql = sql,
                BeforeSql = "SET SHOWPLAN_TEXT ON",
                AfterSql = "SET SHOWPLAN_TEXT OFF",
                MergeParams = true,
            };

        public override string ToReturningStatement(string sql, ModelDefinition modelDef, bool isDelete, ICollection<FieldDefinition> returnFields = null)
        {
            var output = GetOutputClause(modelDef, isDelete ? "DELETED" : "INSERTED", returnFields);

            sql = sql.TrimEnd().TrimEnd(';');

            // DELETE with joins, e.g. DELETE "Table" FROM "Table" INNER JOIN ... needs OUTPUT before FROM
            var isDeleteWithJoin = isDelete && !sql.TrimStart().StartsWith("DELETE FROM", StringComparison.OrdinalIgnoreCase);
            var index = IndexOfTopLevelKeyword(sql, isDeleteWithJoin ? "FROM" : "WHERE");
            return index < 0
                ? sql + " " + output
                : sql.Substring(0, index) + output + " " + sql.Substring(index);
        }

        /// <summary>
        /// Index of the first keyword outside of quotes and parentheses, or -1
        /// </summary>
        private static int IndexOfTopLevelKeyword(string sql, string keyword)
        {
            var depth = 0;
            char quote = default;
            for (var i = 0; i < sql.Length; i++)
            {
                var c = sql[i];
                if (quote != default)
                {
                    if (c == quote) quote = default;
                    continue;
                }
                switch (c)
                {
                    case '\'': case '"': quote = c; continue;
                    case '[': quote = ']'; continue;
                    case '(': depth++; continue;
                    case ')': depth--; continue;
                }
                if (depth == 0 
                    && string.Compare(sql, i, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) == 0
                    && (i == 0 || !char.IsLetterOrDigit(sql[i - 1]))
                    && (i + keyword.Length >= sql.Length || !char.IsLetterOrDigit(sql[i + keyword.Length])))
                    return i;
            }
            return -1;
        }

        public override string SqlLimit(int? offset = null, int? rows = null) => rows == null && offset == null
            ? ""
            : rows != null
                ? "OFFSET " + offset.GetValueOrDefault() + " ROWS FETCH NEXT " + rows + " ROWS ONLY"
                : "OFFSET " + offset.GetValueOrDefault(int.MaxValue) + " ROWS";

        public override string SqlCast(object fieldOrValue, string castAs) => 
            castAs == Sql.VARCHAR
                ? $"CAST({fieldOrValue} AS VARCHAR(MAX))"
                : $"CAST({fieldOrValue} AS {castAs})";

        public override string SqlRandom => "NEWID()";

        // strftime('%Y-%m-%d %H:%M:%S', 'now')
        public Dictionary<string, string> DateFormatMap = new() {
            {"%Y", "YYYY"},
            {"%m", "MM"},
            {"%d", "DD"},
            {"%H", "HH"},
            {"%M", "mm"},
            {"%S", "ss"},
        };
        public override string SqlDateFormat(string quotedColumn, string format)
        {
            var fmt = format.Contains('\'')
                ? format.Replace("'", "")
                : format;
            foreach (var entry in DateFormatMap)
            {
                fmt = fmt.Replace(entry.Key, entry.Value);
            }
            return $"FORMAT({quotedColumn}, '{fmt}')";
        }

        public override void EnableForeignKeysCheck(IDbCommand cmd) => cmd.ExecNonQuery("EXEC sp_msforeachtable \"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\"");
        public override Task EnableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token = default) => 
            cmd.ExecNonQueryAsync("EXEC sp_msforeachtable \"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\"", null, token);
        public override void DisableForeignKeysCheck(IDbCommand cmd) => cmd.ExecNonQuery("EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"");
        public override Task DisableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token = default) => 
            cmd.ExecNonQueryAsync("EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"", null, token);
        
        protected DbConnection Unwrap(IDbConnection db) => (DbConnection)db.ToDbConnection();

        /// <summary>
        /// Errors where SQL Server confirmed the statement wasn't applied: deadlock and snapshot update conflict
        /// victims, In-Memory OLTP conflicts and Azure SQL throttling, failovers and resource limits
        /// </summary>
        public static HashSet<int> NotAppliedErrors { get; } = [
            1205, 3960, 4060, 4221, 10928, 10929, 40501, 40613, 41301, 41302, 41305, 41325, 41839, 49918, 49919, 49920,
        ];

        /// <summary>
        /// Errors where the connection was lost, so the statement may have been applied
        /// </summary>
        public static HashSet<int> ConnectionLostErrors { get; } = [
            64, 121, 233, 10053, 10054, 10060, 40197,
        ];

        public override TransientError GetTransientError(Exception ex)
        {
            if (ex is SqlException sqlEx)
            {
                foreach (SqlError error in sqlEx.Errors)
                {
                    if (NotAppliedErrors.Contains(error.Number))
                        return TransientError.NotApplied;
                    if (ConnectionLostErrors.Contains(error.Number))
                        return TransientError.MaybeApplied;
                }
            }
            return base.GetTransientError(ex);
        }

        protected DbCommand Unwrap(IDbCommand cmd) => (DbCommand)cmd.ToDbCommand();

        protected DbDataReader Unwrap(IDataReader reader) => (DbDataReader)reader;

        public override bool SupportsAsync => true;
        public override Task OpenAsync(IDbConnection db, CancellationToken token = default)
            => Unwrap(db).OpenAsync(token);

        public override Task<IDataReader> ExecuteReaderAsync(IDbCommand cmd, CancellationToken token = default)
            => Unwrap(cmd).ExecuteReaderAsync(token).Then(x => (IDataReader)x);

        public override Task<int> ExecuteNonQueryAsync(IDbCommand cmd, CancellationToken token = default)
            => Unwrap(cmd).ExecuteNonQueryAsync(token);

        public override Task<object> ExecuteScalarAsync(IDbCommand cmd, CancellationToken token = default)
            => Unwrap(cmd).ExecuteScalarAsync(token);

        public override Task<bool> ReadAsync(IDataReader reader, CancellationToken token = default)
            => Unwrap(reader).ReadAsync(token);

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
        
        public override void InitConnection(IDbConnection dbConn)
        {
            if (dbConn is OrmLiteConnection ormLiteConn && dbConn.ToDbConnection() is SqlConnection sqlConn)
                ormLiteConn.ConnectionId = sqlConn.ClientConnectionId;
            
            foreach (var command in ConnectionCommands)
            {
                using var cmd = dbConn.CreateCommand();
                cmd.ExecNonQuery(command);
            }
            
            OnOpenConnection?.Invoke(dbConn);
        }
    }
}
