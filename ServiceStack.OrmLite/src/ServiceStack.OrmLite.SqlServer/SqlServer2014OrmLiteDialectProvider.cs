using System;
using ServiceStack.Text;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.SqlServer
{
    public class SqlServer2014OrmLiteDialectProvider : SqlServer2012OrmLiteDialectProvider
    {
        public new static SqlServer2014OrmLiteDialectProvider Instance = new SqlServer2014OrmLiteDialectProvider();

        public override string GetColumnDefinition(FieldDefinition fieldDef)
        {
            // https://msdn.microsoft.com/en-us/library/ms182776.aspx
            if (fieldDef.IsRowVersion)
                return $"{fieldDef.FieldName} rowversion NOT NULL";

            var fieldDefinition = ResolveFragment(fieldDef.CustomFieldDefinition) ??
                GetColumnTypeDefinition(fieldDef.ColumnType, fieldDef.FieldLength, fieldDef.Scale);

            var memTableAttrib = fieldDef.PropertyInfo?.ReflectedType.FirstAttribute<SqlServerMemoryOptimizedAttribute>();
            var isMemoryTable = memTableAttrib != null;

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

            var bucketCount = fieldDef.PropertyInfo?.FirstAttribute<SqlServerBucketCountAttribute>()?.Count;

            if (fieldDef.IsPrimaryKey)
            {
                if (isMemoryTable)
                {
                    sql.Append($" NOT NULL PRIMARY KEY NONCLUSTERED");
                }
                else
                {
                    sql.Append(" PRIMARY KEY");

                    if (fieldDef.IsNonClustered)
                        sql.Append(" NONCLUSTERED");
                }

                if (fieldDef.AutoIncrement)
                {
                    sql.Append(" ").Append(GetAutoIncrementDefinition(fieldDef));
                }

                if (isMemoryTable && bucketCount.HasValue)
                {
                    sql.Append($" HASH WITH (BUCKET_COUNT = {bucketCount.Value})");
                }
            }
            else
            {
                if (isMemoryTable && bucketCount.HasValue)
                {
                    sql.Append($" NOT NULL INDEX {GetQuotedColumnName("IDX_" + fieldDef.FieldName)}");

                    if (fieldDef.IsNonClustered)
                    {
                        sql.Append(" NONCLUSTERED");
                    }

                    sql.Append($" HASH WITH (BUCKET_COUNT = {bucketCount.Value})");
                }
                else
                {
                    sql.Append(fieldDef.IsNullable ? " NULL" : " NOT NULL");
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

            return StringBuilderCache.ReturnAndFree(sql);
        }

        public override string ToCreateTableStatement(Type tableType)
        {
            var sbColumns = StringBuilderCache.Allocate();
            var sbConstraints = StringBuilderCacheAlt.Allocate();
            var sbTableOptions = StringBuilderCacheAlt.Allocate();

            var fileTableAttrib = tableType.FirstAttribute<SqlServerFileTableAttribute>();
            var memoryTableAttrib = tableType.FirstAttribute<SqlServerMemoryOptimizedAttribute>();

            var modelDef = GetModel(tableType);

            if (fileTableAttrib == null)
            {
                foreach (var fieldDef in modelDef.FieldDefinitions)
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

                    var refModelDef = OrmLiteUtils.GetModelDefinition(fieldDef.ForeignKey.ReferenceType);
                    sbConstraints.Append(
                        $", \n\n  CONSTRAINT {GetQuotedName(fieldDef.ForeignKey.GetForeignKeyName(modelDef, refModelDef, NamingStrategy, fieldDef))} " +
                        $"FOREIGN KEY ({GetQuotedColumnName(fieldDef.FieldName)}) " +
                        $"REFERENCES {GetQuotedTableName(refModelDef)} ({GetQuotedColumnName(refModelDef.PrimaryKey.FieldName)})");

                    sbConstraints.Append(GetForeignKeyOnDeleteClause(fieldDef.ForeignKey));
                    sbConstraints.Append(GetForeignKeyOnUpdateClause(fieldDef.ForeignKey));
                }

                if (memoryTableAttrib != null)
                {
                    var attrib = tableType.FirstAttribute<SqlServerMemoryOptimizedAttribute>();
                    sbTableOptions.Append(" WITH (MEMORY_OPTIMIZED = ON");
                    if (attrib.Durability == SqlServerDurability.SchemaOnly)
                        sbTableOptions.Append(", DURABILITY = SCHEMA_ONLY");
                    else if (attrib.Durability == SqlServerDurability.SchemaAndData)
                        sbTableOptions.Append(", DURABILITY = SCHEMA_AND_DATA");
                    sbTableOptions.Append(")");
                }
            }
            else
            {
                var hasFileTableDir = !string.IsNullOrEmpty(fileTableAttrib.FileTableDirectory);
                var hasFileTableCollateFileName = !string.IsNullOrEmpty(fileTableAttrib.FileTableCollateFileName);

                if (hasFileTableDir || hasFileTableCollateFileName)
                {
                    sbTableOptions.Append(" WITH (");

                    if (hasFileTableDir)
                    {
                        sbTableOptions.Append($" FILETABLE_DIRECTORY = N'{fileTableAttrib.FileTableDirectory}'\n");
                    }

                    if (hasFileTableCollateFileName)
                    {
                        if (hasFileTableDir) sbTableOptions.Append(" ,");
                        sbTableOptions.Append($" FILETABLE_COLLATE_FILENAME = {fileTableAttrib.FileTableCollateFileName ?? "database_default" }\n");
                    }
                    sbTableOptions.Append(")");
                }
            }
            
            var uniqueConstraints = GetUniqueConstraints(modelDef);
            if (uniqueConstraints != null)
            {
                sbConstraints.Append(",\n" + uniqueConstraints);
            }

            var sql = $"CREATE TABLE {GetQuotedTableName(modelDef)} ";
            sql += (fileTableAttrib != null)
                ? $"\n AS FILETABLE{StringBuilderCache.ReturnAndFree(sbTableOptions)};"
                : $"\n(\n  {StringBuilderCache.ReturnAndFree(sbColumns)}{StringBuilderCacheAlt.ReturnAndFree(sbConstraints)} \n){StringBuilderCache.ReturnAndFree(sbTableOptions)}; \n";

            return sql;
        }
    }
}
