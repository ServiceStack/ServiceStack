using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite
{
    public static class OrmLiteSchemaApi
    {
        /// <summary>
        /// Checks whether a Table Exists. E.g:
        /// <para>db.TableExists("Person")</para>
        /// </summary>
        public static bool TableExists(this IDbConnection dbConn, string tableName, string schema = null)
        {
            return dbConn.GetDialectProvider().DoesTableExist(dbConn, tableName, schema);
        }

        /// <summary>
        /// Checks whether a Table Exists. E.g:
        /// <para>db.TableExistsAsync("Person")</para>
        /// </summary>
        public static Task<bool> TableExistsAsync(this IDbConnection dbConn, string tableName, string schema = null, CancellationToken token=default)
        {
            return dbConn.GetDialectProvider().DoesTableExistAsync(dbConn, tableName, schema, token);
        }

        /// <summary>
        /// Checks whether a Table Exists. E.g:
        /// <para>db.TableExists&lt;Person&gt;()</para>
        /// </summary>
        public static bool TableExists<T>(this IDbConnection dbConn)
        {
            var dialectProvider = dbConn.GetDialectProvider();
            var modelDef = typeof(T).GetModelDefinition();
            var schema = modelDef.Schema == null ? null : dialectProvider.NamingStrategy.GetSchemaName(modelDef.Schema);
            var tableName = dialectProvider.NamingStrategy.GetTableName(modelDef);
            return dialectProvider.DoesTableExist(dbConn, tableName, schema);
        }

        /// <summary>
        /// Checks whether a Table Exists. E.g:
        /// <para>db.TableExistsAsync&lt;Person&gt;()</para>
        /// </summary>
        public static Task<bool> TableExistsAsync<T>(this IDbConnection dbConn, CancellationToken token=default)
        {
            var dialectProvider = dbConn.GetDialectProvider();
            var modelDef = typeof(T).GetModelDefinition();
            var schema = modelDef.Schema == null ? null : dialectProvider.NamingStrategy.GetSchemaName(modelDef.Schema);
            var tableName = dialectProvider.NamingStrategy.GetTableName(modelDef);
            return dialectProvider.DoesTableExistAsync(dbConn, tableName, schema, token);
        }

        /// <summary>
        /// Checks whether a Table Column Exists. E.g:
        /// <para>db.ColumnExists("Age", "Person")</para>
        /// </summary>
        public static bool ColumnExists(this IDbConnection dbConn, string columnName, string tableName, string schema = null)
        {
            return dbConn.GetDialectProvider().DoesColumnExist(dbConn, columnName, tableName, schema);
        }

        /// <summary>
        /// Checks whether a Table Column Exists. E.g:
        /// <para>db.ColumnExistsAsync("Age", "Person")</para>
        /// </summary>
        public static Task<bool> ColumnExistsAsync(this IDbConnection dbConn, string columnName, string tableName, string schema = null, CancellationToken token=default)
        {
            return dbConn.GetDialectProvider().DoesColumnExistAsync(dbConn, columnName, tableName, schema, token);
        }

        /// <summary>
        /// Checks whether a Table Column Exists. E.g:
        /// <para>db.ColumnExists&lt;Person&gt;(x =&gt; x.Age)</para>
        /// </summary>
        public static bool ColumnExists<T>(this IDbConnection dbConn, Expression<Func<T, object>> field)
        {
            var dialectProvider = dbConn.GetDialectProvider();
            var modelDef = typeof(T).GetModelDefinition();
            var schema = modelDef.Schema == null ? null : dialectProvider.NamingStrategy.GetSchemaName(modelDef.Schema);
            var tableName = dialectProvider.NamingStrategy.GetTableName(modelDef);
            var fieldDef = modelDef.GetFieldDefinition(field);
            var fieldName = dialectProvider.NamingStrategy.GetColumnName(fieldDef.FieldName);
            return dialectProvider.DoesColumnExist(dbConn, fieldName, tableName, schema);
        }

        /// <summary>
        /// Checks whether a Table Column Exists. E.g:
        /// <para>db.ColumnExistsAsync&lt;Person&gt;(x =&gt; x.Age)</para>
        /// </summary>
        public static Task<bool> ColumnExistsAsync<T>(this IDbConnection dbConn, Expression<Func<T, object>> field, CancellationToken token=default)
        {
            var dialectProvider = dbConn.GetDialectProvider();
            var modelDef = typeof(T).GetModelDefinition();
            var schema = modelDef.Schema == null ? null : dialectProvider.NamingStrategy.GetSchemaName(modelDef.Schema);
            var tableName = dialectProvider.NamingStrategy.GetTableName(modelDef);
            var fieldDef = modelDef.GetFieldDefinition(field);
            var fieldName = dialectProvider.NamingStrategy.GetColumnName(fieldDef.FieldName);
            return dialectProvider.DoesColumnExistAsync(dbConn, fieldName, tableName, schema, token);
        }
        
        /// <summary>
        /// Create a DB Schema from the Schema attribute on the generic type. E.g:
        /// <para>db.CreateSchema&lt;Person&gt;() //default</para> 
        /// </summary>
        public static void CreateSchema<T>(this IDbConnection dbConn)
        {
            dbConn.Exec(dbCmd => dbCmd.CreateSchema<T>());
        }

        /// <summary>
        /// Create a DB Schema. E.g:
        /// <para>db.CreateSchema("schemaName")</para> 
        /// </summary>
        public static bool CreateSchema(this IDbConnection dbConn, string schemaName)
        {
            return dbConn.Exec(dbCmd => dbCmd.CreateSchema(schemaName));
        }

        /// <summary>
        /// Create DB Tables from the schemas of runtime types. E.g:
        /// <para>db.CreateTables(typeof(Table1), typeof(Table2))</para> 
        /// </summary>
        public static void CreateTables(this IDbConnection dbConn, bool overwrite, params Type[] tableTypes)
        {
            dbConn.Exec(dbCmd => dbCmd.CreateTables(overwrite, tableTypes));
        }

        /// <summary>
        /// Create DB Table from the schema of the runtime type. Use overwrite to drop existing Table. E.g:
        /// <para>db.CreateTable(true, typeof(Table))</para> 
        /// </summary>
        public static void CreateTable(this IDbConnection dbConn, bool overwrite, Type modelType)
        {
            dbConn.Exec(dbCmd => dbCmd.CreateTable(overwrite, modelType));
        }

        /// <summary>
        /// Only Create new DB Tables from the schemas of runtime types if they don't already exist. E.g:
        /// <para>db.CreateTableIfNotExists(typeof(Table1), typeof(Table2))</para> 
        /// </summary>
        public static void CreateTableIfNotExists(this IDbConnection dbConn, params Type[] tableTypes)
        {
            dbConn.Exec(dbCmd => dbCmd.CreateTables(overwrite: false, tableTypes: tableTypes));
        }

        /// <summary>
        /// Drop existing DB Tables and re-create them from the schemas of runtime types. E.g:
        /// <para>db.DropAndCreateTables(typeof(Table1), typeof(Table2))</para> 
        /// </summary>
        public static void DropAndCreateTables(this IDbConnection dbConn, params Type[] tableTypes)
        {
            dbConn.Exec(dbCmd => dbCmd.CreateTables(overwrite: true, tableTypes: tableTypes));
        }

        /// <summary>
        /// Create a DB Table from the generic type. Use overwrite to drop the existing table or not. E.g:
        /// <para>db.CreateTable&lt;Person&gt;(overwrite=false) //default</para> 
        /// <para>db.CreateTable&lt;Person&gt;(overwrite=true)</para> 
        /// </summary>
        public static void CreateTable<T>(this IDbConnection dbConn, bool overwrite = false)
        {
            dbConn.Exec(dbCmd => dbCmd.CreateTable<T>(overwrite));
        }

        /// <summary>
        /// Only create a DB Table from the generic type if it doesn't already exist. E.g:
        /// <para>db.CreateTableIfNotExists&lt;Person&gt;()</para> 
        /// </summary>
        public static bool CreateTableIfNotExists<T>(this IDbConnection dbConn)
        {
            return dbConn.Exec(dbCmd => dbCmd.CreateTable<T>(overwrite:false));
        }

        /// <summary>
        /// Only create a DB Table from the runtime type if it doesn't already exist. E.g:
        /// <para>db.CreateTableIfNotExists(typeof(Person))</para> 
        /// </summary>
        public static bool CreateTableIfNotExists(this IDbConnection dbConn, Type modelType)
        {
            return dbConn.Exec(dbCmd => dbCmd.CreateTable(false, modelType));
        }

        /// <summary>
        /// Drop existing table if exists and re-create a DB Table from the generic type. E.g:
        /// <para>db.DropAndCreateTable&lt;Person&gt;()</para> 
        /// </summary>
        public static void DropAndCreateTable<T>(this IDbConnection dbConn)
        {
            dbConn.Exec(dbCmd => dbCmd.CreateTable<T>(true));
        }

        /// <summary>
        /// Drop existing table if exists and re-create a DB Table from the runtime type. E.g:
        /// <para>db.DropAndCreateTable(typeof(Person))</para> 
        /// </summary>
        public static void DropAndCreateTable(this IDbConnection dbConn, Type modelType)
        {
            dbConn.Exec(dbCmd => dbCmd.CreateTable(true, modelType));
        }

        /// <summary>
        /// Drop any existing tables from their runtime types. E.g:
        /// <para>db.DropTables(typeof(Table1),typeof(Table2))</para> 
        /// </summary>
        public static void DropTables(this IDbConnection dbConn, params Type[] tableTypes)
        {
            dbConn.Exec(dbCmd => dbCmd.DropTables(tableTypes));
        }

        /// <summary>
        /// Drop any existing tables from the runtime type. E.g:
        /// <para>db.DropTable(typeof(Person))</para> 
        /// </summary>
        public static void DropTable(this IDbConnection dbConn, Type modelType)
        {
            dbConn.Exec(dbCmd => dbCmd.DropTable(modelType));
        }

        /// <summary>
        /// Drop any existing tables from the generic type. E.g:
        /// <para>db.DropTable&lt;Person&gt;()</para> 
        /// </summary>
        public static void DropTable<T>(this IDbConnection dbConn)
        {
            dbConn.Exec(dbCmd => dbCmd.DropTable<T>());
        }

        /// <summary>
        /// Get a list of available user schemas for this connection 
        /// </summary>
        public static List<string> GetSchemas(this IDbConnection dbConn)
        {
            return dbConn.Exec(dbCmd => dbConn.GetDialectProvider().GetSchemas(dbCmd));
        }

        /// <summary>
        /// Get available user Schemas and their tables for this connection 
        /// </summary>
        public static Dictionary<string, List<string>> GetSchemaTables(this IDbConnection dbConn)
        {
            return dbConn.Exec(dbCmd => dbConn.GetDialectProvider().GetSchemaTables(dbCmd));
        }
    }
}