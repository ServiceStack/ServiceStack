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
#if !NETFRAMEWORK
using ApplicationException = System.InvalidOperationException;
#endif

namespace ServiceStack.OrmLite.SqlServer
{
    public class SqlServerOrmLiteDialectProvider : OrmLiteDialectProviderBase<SqlServerOrmLiteDialectProvider>
    {
        public static SqlServerOrmLiteDialectProvider Instance = new();

        public SqlServerOrmLiteDialectProvider()
        {
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
            var sql = $"CREATE SCHEMA [{GetSchemaName(schemaName)}]";
            return sql;
        }
        
        public override string ToCreateSavePoint(string name) => $"SAVE TRANSACTION {name}";
        public override string ToReleaseSavePoint(string name) => null;
        public override string ToRollbackSavePoint(string name) => $"ROLLBACK TRANSACTION {name}";

        public override bool DoesTableExist(IDbCommand dbCmd, string tableName, string schema = null)
        {
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}"
                .SqlFmt(this, tableName);

            if (schema != null)
                sql += " AND TABLE_SCHEMA = {0}".SqlFmt(this, schema);
            else
                sql += " AND TABLE_SCHEMA <> 'Security'";

            var result = dbCmd.ExecLongScalar(sql);

            return result > 0;
        }

        public override async Task<bool> DoesTableExistAsync(IDbCommand dbCmd, string tableName, string schema = null, CancellationToken token = default)
        {
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}"
                .SqlFmt(this, tableName);

            if (schema != null)
                sql += " AND TABLE_SCHEMA = {0}".SqlFmt(this, schema);
            else
                sql += " AND TABLE_SCHEMA <> 'Security'";

            var result = await dbCmd.ExecLongScalarAsync(sql, token);

            return result > 0;
        }

        public override bool DoesColumnExist(IDbConnection db, string columnName, string tableName, string schema = null)
        {
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName"
                .SqlFmt(this, tableName, columnName);

            if (schema != null)
                sql += " AND TABLE_SCHEMA = @schema";

            var result = db.SqlScalar<long>(sql, new { tableName, columnName, schema });

            return result > 0;
        }

        public override async Task<bool> DoesColumnExistAsync(IDbConnection db, string columnName, string tableName, string schema = null,
            CancellationToken token = default)
        {
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName"
                .SqlFmt(this, tableName, columnName);

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

        public override string ToAddColumnStatement(string schema, string table, FieldDefinition fieldDef) => 
            $"ALTER TABLE {GetQuotedTableName(table, schema)} ADD {GetColumnDefinition(fieldDef)};";

        public override string ToAlterColumnStatement(string schema, string table, FieldDefinition fieldDef) => 
            $"ALTER TABLE {GetQuotedTableName(table, schema)} ALTER COLUMN {GetColumnDefinition(fieldDef)};";

        public override string ToChangeColumnNameStatement(string schema, string table, FieldDefinition fieldDef, string oldColumn)
        {
            var modelName = NamingStrategy.GetTableName(table);
            var objectName = $"{modelName}.{oldColumn}";
            return $"EXEC sp_rename {GetQuotedValue(objectName)}, {GetQuotedValue(fieldDef.FieldName)}, {GetQuotedValue("COLUMN")};";
        }

        public override string ToRenameColumnStatement(string schema, string table, string oldColumn, string newColumn)
        {
            var modelName = NamingStrategy.GetTableName(table);
            var objectName = $"{modelName}.{GetQuotedColumnName(oldColumn)}";
            return $"EXEC sp_rename {GetQuotedValue(objectName)}, {GetQuotedColumnName(newColumn)}, 'COLUMN';";
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

        public override string GetColumnDefinition(FieldDefinition fieldDef)
        {
            // https://msdn.microsoft.com/en-us/library/ms182776.aspx
            if (fieldDef.IsRowVersion)
                return $"{fieldDef.FieldName} rowversion NOT NULL";

            var fieldDefinition = ResolveFragment(fieldDef.CustomFieldDefinition) ??
                GetColumnTypeDefinition(fieldDef.ColumnType, fieldDef.FieldLength, fieldDef.Scale);

            var sql = StringBuilderCache.Allocate();
            sql.Append($"{GetQuotedColumnName(fieldDef.FieldName)} {fieldDefinition}");

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

        public override string ToDropConstraintStatement(string schema, string table, string constraintName) =>
            $"ALTER TABLE {GetQuotedTableName(table, schema)} DROP CONSTRAINT {GetQuotedName(constraintName)};";

        public override void BulkInsert<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null)
        {
            config ??= new();
            if (config.Mode == BulkInsertMode.Sql)
            {
                base.BulkInsert(db, objs, config);
                return;
            }
            
            var sqlConn = (SqlConnection)db.ToDbConnection();
            using var bulkCopy = new SqlBulkCopy(sqlConn);
            var modelDef = ModelDefinition<T>.Definition;

            bulkCopy.BatchSize = config.BatchSize;
            bulkCopy.DestinationTableName = modelDef.ModelName;
            
            var table = new DataTable();
            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields:config.InsertFields);
            foreach (var fieldDef in fieldDefs)
            {
                if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                    continue;

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
                    if (ShouldSkipInsert(fieldDef) && !fieldDef.AutoId)
                        continue;
                    
                    var value = fieldDef.AutoId
                        ? GetInsertDefaultValue(fieldDef)
                        : fieldDef.GetValue(obj);

                    var converter = GetConverterBestMatch(fieldDef);
                    var dbValue = converter.ToDbValue(fieldDef.FieldType, value);
                    var columnName = NamingStrategy.GetColumnName(fieldDef.FieldName);
                    dbValue ??= DBNull.Value;
                    row[columnName] = dbValue;
                }
                table.Rows.Add(row);
            }
            
            bulkCopy.WriteToServer(table);
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
                    sbReturningColumns.Append("INSERTED." + GetQuotedColumnName(fieldDef.FieldName));
                }

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

            foreach (var fieldDef in modelDef.AutoIdFields) // need to include any AutoId fields that weren't included 
            {
                if (fieldDefs.Contains(fieldDef))
                    continue;

                if (sbReturningColumns.Length > 0)
                    sbReturningColumns.Append(",");
                sbReturningColumns.Append("INSERTED." + GetQuotedColumnName(fieldDef.FieldName));
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

            var escapedSchema = NamingStrategy.GetSchemaName(schema)
                .Replace(".", "\".\"");

            return GetQuotedName(escapedSchema)
                   + "."
                   + GetQuotedName(sequence);
        }

        protected override bool ShouldSkipInsert(FieldDefinition fieldDef) => 
            fieldDef.ShouldSkipInsert() || fieldDef.AutoId;

        protected virtual bool ShouldReturnOnInsert(ModelDefinition modelDef, FieldDefinition fieldDef) =>
            fieldDef.ReturnOnInsert || (fieldDef.IsPrimaryKey && fieldDef.AutoIncrement && HasInsertReturnValues(modelDef)) || fieldDef.AutoId;

        public override bool HasInsertReturnValues(ModelDefinition modelDef) =>
            modelDef.FieldDefinitions.Any(x => x.ReturnOnInsert || (x.AutoId && x.FieldType == typeof(Guid)));

        protected virtual bool SupportsSequences(FieldDefinition fieldDef) => false;

        public override void EnableIdentityInsert<T>(IDbCommand cmd)
        {
            var tableName = cmd.GetDialectProvider().GetQuotedTableName(ModelDefinition<T>.Definition);
            cmd.ExecNonQuery($"SET IDENTITY_INSERT {tableName} ON");
        }

        public override Task EnableIdentityInsertAsync<T>(IDbCommand cmd, CancellationToken token=default)
        {
            var tableName = cmd.GetDialectProvider().GetQuotedTableName(ModelDefinition<T>.Definition);
            return cmd.ExecNonQueryAsync($"SET IDENTITY_INSERT {tableName} ON", null, token);
        }

        public override void DisableIdentityInsert<T>(IDbCommand cmd)
        {
            var tableName = cmd.GetDialectProvider().GetQuotedTableName(ModelDefinition<T>.Definition);
            cmd.ExecNonQuery($"SET IDENTITY_INSERT {tableName} OFF");
        }

        public override Task DisableIdentityInsertAsync<T>(IDbCommand cmd, CancellationToken token=default)
        {
            var tableName = cmd.GetDialectProvider().GetQuotedTableName(ModelDefinition<T>.Definition);
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
                    sbReturningColumns.Append("INSERTED." + GetQuotedColumnName(fieldDef.FieldName));
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
                sbReturningColumns.Append("INSERTED." + GetQuotedColumnName(fieldDef.FieldName));
            }

            var strReturning = StringBuilderCacheAlt.ReturnAndFree(sbReturningColumns);
            strReturning = strReturning.Length > 0 ? "OUTPUT " + strReturning + " " : "";
            cmd.CommandText = sbColumnNames.Length > 0
                ? $"INSERT INTO {GetQuotedTableName(modelDef)} ({StringBuilderCache.ReturnAndFree(sbColumnNames)}) {strReturning}" +                              
                  $"VALUES ({StringBuilderCacheAlt.ReturnAndFree(sbColumnValues)})"
                : $"INSERT INTO {GetQuotedTableName(modelDef)}{strReturning} DEFAULT VALUES";
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
                    sbReturningColumns.Append("INSERTED." + GetQuotedColumnName(fieldDef.FieldName));
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
                    sbColumnNames.Append(GetQuotedColumnName(fieldDef.FieldName));
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
            SqlConcat(new[] { "'" + currencySymbol + "'", $"CONVERT(VARCHAR, CONVERT(MONEY, {fieldOrValue}), 1)" });

        public override string SqlBool(bool value) => value ? "1" : "0";

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

        public override void EnableForeignKeysCheck(IDbCommand cmd) => cmd.ExecNonQuery("EXEC sp_msforeachtable \"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\"");
        public override Task EnableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token = default) => 
            cmd.ExecNonQueryAsync("EXEC sp_msforeachtable \"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\"", null, token);
        public override void DisableForeignKeysCheck(IDbCommand cmd) => cmd.ExecNonQuery("EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"");
        public override Task DisableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token = default) => 
            cmd.ExecNonQueryAsync("EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"", null, token);
        
        protected DbConnection Unwrap(IDbConnection db) => (DbConnection)db.ToDbConnection();

        protected DbCommand Unwrap(IDbCommand cmd) => (DbCommand)cmd.ToDbCommand();

        protected DbDataReader Unwrap(IDataReader reader) => (DbDataReader)reader;

#if ASYNC
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
#endif

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
