using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    public enum OnFkOption
    {
        Cascade,
        SetNull,
        NoAction,
        SetDefault,
        Restrict
    }

    public static class OrmLiteSchemaModifyApi
    {
        private static void InitUserFieldDefinition(Type modelType, FieldDefinition fieldDef)
        {
            if (fieldDef.PropertyInfo == null)
            {
                fieldDef.PropertyInfo = TypeProperties.Get(modelType).GetPublicProperty(fieldDef.Name);
            }
        }

        public static void AlterTable<T>(this IDbConnection dbConn, string command)
        {
            AlterTable(dbConn, typeof(T), command);
        }

        public static void AlterTable(this IDbConnection dbConn, Type modelType, string command)
        {
            var sql = $"ALTER TABLE {dbConn.GetDialectProvider().GetQuotedTableName(modelType.GetModelDefinition())} {command};";
            dbConn.ExecuteSql(sql);
        }

        public static void AddColumn<T>(this IDbConnection dbConn, Expression<Func<T, object>> field)
        {
            var modelDef = ModelDefinition<T>.Definition;
            var fieldDef = modelDef.GetFieldDefinition(field);
            if (fieldDef.Name != OrmLiteConfig.IdField)
                fieldDef.IsPrimaryKey = false;
            dbConn.AddColumn(typeof(T), fieldDef);
        }

        public static void AddColumn(this IDbConnection dbConn, Type modelType, FieldDefinition fieldDef)
        {
            InitUserFieldDefinition(modelType, fieldDef);
            var command = dbConn.GetDialectProvider().ToAddColumnStatement(modelType, fieldDef);
            dbConn.ExecuteSql(command);
        }
        
        public static void AddColumn(this IDbConnection dbConn, string table, FieldDefinition fieldDef) => 
            dbConn.ExecuteSql(dbConn.GetDialectProvider().ToAddColumnStatement(null, table, fieldDef));
        
        public static void AddColumn(this IDbConnection dbConn, string schema, string table, FieldDefinition fieldDef) => 
            dbConn.ExecuteSql(dbConn.GetDialectProvider().ToAddColumnStatement(schema, table, fieldDef));

        public static void AlterColumn<T>(this IDbConnection dbConn, Expression<Func<T, object>> field)
        {
            var modelDef = ModelDefinition<T>.Definition;
            var fieldDef = modelDef.GetFieldDefinition<T>(field);
            dbConn.AlterColumn(typeof(T), fieldDef);
        }

        public static void AlterColumn(this IDbConnection dbConn, Type modelType, FieldDefinition fieldDef)
        {
            InitUserFieldDefinition(modelType, fieldDef);
            dbConn.ExecuteSql(dbConn.GetDialectProvider().ToAlterColumnStatement(modelType, fieldDef));
        }

        public static void AlterColumn(this IDbConnection dbConn, string table, FieldDefinition fieldDef) => 
            dbConn.ExecuteSql(dbConn.GetDialectProvider().ToAlterColumnStatement(null, table, fieldDef));
        public static void AlterColumn(this IDbConnection dbConn, string schema, string table, FieldDefinition fieldDef) => 
            dbConn.ExecuteSql(dbConn.GetDialectProvider().ToAlterColumnStatement(schema, table, fieldDef));

        public static void ChangeColumnName<T>(this IDbConnection dbConn,
            Expression<Func<T, object>> field,
            string oldColumn)
        {
            var modelDef = ModelDefinition<T>.Definition;
            var fieldDef = modelDef.GetFieldDefinition<T>(field);
            dbConn.ChangeColumnName(typeof(T), fieldDef, oldColumn);
        }

        public static void ChangeColumnName(this IDbConnection dbConn,
            Type modelType,
            FieldDefinition fieldDef,
            string oldColumn)
        {
            var command = dbConn.GetDialectProvider().ToChangeColumnNameStatement(modelType, fieldDef, oldColumn);
            dbConn.ExecuteSql(command);
        }

        public static void RenameColumn<T>(this IDbConnection dbConn,
            Expression<Func<T, object>> field,
            string oldColumn)
        {
            var modelDef = ModelDefinition<T>.Definition;
            var fieldDef = modelDef.GetFieldDefinition(field);
            dbConn.RenameColumn(typeof(T), oldColumn, dbConn.GetNamingStrategy().GetColumnName(fieldDef.FieldName));
        }

        public static void RenameColumn<T>(this IDbConnection dbConn, string oldColumn, string newColumn) =>
            dbConn.RenameColumn(typeof(T), oldColumn, newColumn);
        public static void RenameColumn(this IDbConnection dbConn, Type modelType, string oldColumn, string newColumn) =>
            dbConn.ExecuteSql(X.Map(dbConn.Dialect(), d => d.ToRenameColumnStatement(modelType, 
                d.NamingStrategy.GetColumnName(oldColumn), d.NamingStrategy.GetColumnName(newColumn))));
        public static void RenameColumn(this IDbConnection dbConn, string table, string oldColumn, string newColumn) =>
            dbConn.ExecuteSql(X.Map(dbConn.Dialect(), d => d.ToRenameColumnStatement(null, table, 
                d.NamingStrategy.GetColumnName(oldColumn), d.NamingStrategy.GetColumnName(newColumn))));
        public static void RenameColumn(this IDbConnection dbConn, string schema, string table, string oldColumn, string newColumn) =>
            dbConn.ExecuteSql(X.Map(dbConn.Dialect(), d => d.ToRenameColumnStatement(schema, table, 
                d.NamingStrategy.GetColumnName(oldColumn), d.NamingStrategy.GetColumnName(newColumn))));

        public static void DropColumn<T>(this IDbConnection dbConn, Expression<Func<T, object>> field)
        {
            var modelDef = ModelDefinition<T>.Definition;
            var fieldDef = modelDef.GetFieldDefinition(field);
            dbConn.DropColumn(typeof(T), fieldDef.FieldName);
        }

        public static void DropColumn<T>(this IDbConnection dbConn, string column) => 
            dbConn.DropColumn(typeof(T), column);

        public static void DropColumn(this IDbConnection dbConn, Type modelType, string column) => 
            dbConn.ExecuteSql(X.Map(dbConn.Dialect(), d => d.ToDropColumnStatement(modelType, column)));
        public static void DropColumn(this IDbConnection dbConn, string table, string column) => 
            dbConn.ExecuteSql(X.Map(dbConn.Dialect(), d => d.ToDropColumnStatement(null, table, column)));
        public static void DropColumn(this IDbConnection dbConn, string schema, string table, string column) => 
            dbConn.ExecuteSql(X.Map(dbConn.Dialect(), d => d.ToDropColumnStatement(schema, table, column)));

        public static void AddForeignKey<T, TForeign>(this IDbConnection dbConn,
            Expression<Func<T, object>> field,
            Expression<Func<TForeign, object>> foreignField,
            OnFkOption onUpdate,
            OnFkOption onDelete,
            string foreignKeyName = null)
        {
            var command = dbConn.GetDialectProvider().ToAddForeignKeyStatement(
                field, foreignField, onUpdate, onDelete, foreignKeyName);

            dbConn.ExecuteSql(command);
        }

        public static void DropForeignKeys<T>(this IDbConnection dbConn)
        {
            var provider = dbConn.GetDialectProvider();
            var modelDef = ModelDefinition<T>.Definition;
            var dropSql = provider.GetDropForeignKeyConstraints(modelDef);
            if (string.IsNullOrEmpty(dropSql))
                throw new NotSupportedException($"Drop All Foreign Keys not supported by {provider.GetType().Name}");
            
            dbConn.ExecuteSql(dropSql);
        }

        public static void DropForeignKey<T>(this IDbConnection dbConn, string foreignKeyName)
        {
            var provider = dbConn.GetDialectProvider();
            var modelDef = ModelDefinition<T>.Definition;
            var dropSql = provider.ToDropForeignKeyStatement(modelDef.Schema, modelDef.ModelName, foreignKeyName);
            dbConn.ExecuteSql(dropSql);
        }

        public static void CreateIndex<T>(this IDbConnection dbConn, Expression<Func<T, object>> field,
            string indexName = null, bool unique = false)
        {
            var command = dbConn.GetDialectProvider().ToCreateIndexStatement(field, indexName, unique);
            dbConn.ExecuteSql(command);
        }

        public static void DropIndex<T>(this IDbConnection dbConn, string indexName)
        {
            var provider = dbConn.GetDialectProvider();
            var command = $"ALTER TABLE {provider.GetQuotedTableName(ModelDefinition<T>.Definition.ModelName)} " +
                          $"DROP INDEX  {provider.GetQuotedName(indexName)};";
            dbConn.ExecuteSql(command);
        }
                
        /// <summary>
        /// Alter tables by adding properties for missing columns and removing properties annotated with [RemoveColumn]
        /// </summary>
        public static void Migrate(this IDbConnection dbConn, Type modelType)
        {
            var modelDef = modelType.GetModelDefinition();
            var migrateFieldDefinitions = modelDef.FieldDefinitions.Map(x => x.Clone(f => {
                f.IsPrimaryKey = false;
            }));
            foreach (var fieldDef in migrateFieldDefinitions)
            {
                var attrs = fieldDef.PropertyInfo.AllAttributes().Where(x => x is AlterColumnAttribute).ToList();
                if (attrs.Count > 1)
                    throw new Exception($"Only 1 AlterColumnAttribute allowed on {modelType.Name}.{fieldDef.Name}");
                
                var attr = attrs.FirstOrDefault();
                if (attr is RemoveColumnAttribute)
                {
                    dbConn.DropColumn(modelType, fieldDef.FieldName);
                }
                else if (attr is RenameColumnAttribute renameAttr)
                {
                    dbConn.RenameColumn(modelType, renameAttr.From, fieldDef.FieldName);
                }
                else if (attr is AddColumnAttribute or null)
                {
                    dbConn.AddColumn(modelType, fieldDef);
                }
                else
                    throw new Exception($"Unsupported AlterColumnAttribute '{attr.GetType().Name}' on {modelType.Name}.{fieldDef.Name}");
            }
        }
                
        /// <summary>
        /// Apply schema changes by Migrate in reverse to revert changes
        /// </summary>
        public static void Revert(this IDbConnection dbConn, Type modelType)
        {
            var modelDef = modelType.GetModelDefinition();
            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                var attrs = fieldDef.PropertyInfo.AllAttributes().Where(x => x is AlterColumnAttribute).ToList();
                if (attrs.Count > 1)
                    throw new Exception($"Only 1 AlterColumnAttribute allowed on {modelType.Name}.{fieldDef.Name}");
                
                var attr = attrs.FirstOrDefault();
                if (attr is AddColumnAttribute or null)
                {
                    dbConn.DropColumn(modelType, fieldDef.FieldName);
                }
                else if (attr is RenameColumnAttribute renameAttr)
                {
                    dbConn.RenameColumn(modelType, fieldDef.FieldName, renameAttr.From);
                }
                else if (attr is RemoveColumnAttribute)
                {
                    dbConn.AddColumn(modelType, fieldDef);
                }
                else
                    throw new Exception($"Unsupported AlterColumnAttribute '{attr.GetType().Name}' on {modelType.Name}.{fieldDef.Name}");
            }
        }

    }
}
