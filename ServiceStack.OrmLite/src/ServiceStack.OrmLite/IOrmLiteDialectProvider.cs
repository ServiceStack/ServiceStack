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
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.OrmLite.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    public interface IOrmLiteDialectProvider
    {
        void RegisterConverter<T>(IOrmLiteConverter converter);

        /// <summary>
        /// Invoked when a DB Connection is opened
        /// </summary>
        void InitConnection(IDbConnection dbConn);

        /// <summary>
        /// Custom delegate invoked when a DB Connection is opened
        /// </summary>
        Action<IDbConnection> OnOpenConnection { get; set; }

        IOrmLiteExecFilter ExecFilter { get; set; }

        /// <summary>
        /// Gets the explicit Converter registered for a specific type
        /// </summary>
        IOrmLiteConverter GetConverter(Type type);

        /// <summary>
        /// Return best matching converter, falling back to Enum, Value or Ref Type Converters
        /// </summary>
        IOrmLiteConverter GetConverterBestMatch(Type type);
        
        IOrmLiteConverter GetConverterBestMatch(FieldDefinition fieldDef);

        string ParamString { get; set; }

        string EscapeWildcards(string value);

        INamingStrategy NamingStrategy { get; set; }

        IStringSerializer StringSerializer { get; set; }

        Func<string, string> ParamNameFilter { get; set; }
        
        Dictionary<string, string> Variables { get; }

        /// <summary>
        /// Quote the string so that it can be used inside an SQL-expression
        /// Escape quotes inside the string
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        string GetQuotedValue(string paramValue);

        string GetQuotedValue(object value, Type fieldType);

        string GetDefaultValue(Type tableType, string fieldName);

        string GetDefaultValue(FieldDefinition fieldDef);

        bool HasInsertReturnValues(ModelDefinition modelDef);

        object GetParamValue(object value, Type fieldType);

        // Customize DB Parameters in SELECT or WHERE queries 
        void InitQueryParam(IDbDataParameter param);

        // Customize UPDATE or INSERT DB Parameters
        void InitUpdateParam(IDbDataParameter param);

        object ToDbValue(object value, Type type);

        object FromDbValue(object value, Type type);

        object GetValue(IDataReader reader, int columnIndex, Type type);

        int GetValues(IDataReader reader, object[] values);

        IDbConnection CreateConnection(string filePath, Dictionary<string, string> options);

        string GetTableName(ModelDefinition modelDef);

        string GetTableName(ModelDefinition modelDef, bool useStrategy);

        string GetTableName(string table, string schema = null);
        string GetTableName(string table, string schema, bool useStrategy);

        string GetQuotedTableName(ModelDefinition modelDef);

        string GetQuotedTableName(string tableName, string schema=null);

        string GetQuotedTableName(string tableName, string schema, bool useStrategy);

        string GetQuotedColumnName(string columnName);

        string GetQuotedName(string name);
        string GetQuotedName(string name, string schema);

        string SanitizeFieldNameForParamName(string fieldName);

        string GetColumnDefinition(FieldDefinition fieldDef);

        long GetLastInsertId(IDbCommand command);

        string GetLastInsertIdSqlSuffix<T>();

        string ToSelectStatement(Type tableType, string sqlFilter, params object[] filterParams);

        string ToSelectStatement(
            QueryType queryType, 
            ModelDefinition modelDef,
            string selectExpression,
            string bodyExpression, 
            string orderByExpression = null, 
            int? offset = null,
            int? rows = null,
            ISet<string> tags=null);

        string ToInsertRowStatement(IDbCommand cmd, object objWithProperties, ICollection<string> insertFields = null);

        void PrepareParameterizedInsertStatement<T>(IDbCommand cmd, ICollection<string> insertFields = null, Func<FieldDefinition,bool> shouldInclude=null);

        /// <returns>If had RowVersion</returns>
        bool PrepareParameterizedUpdateStatement<T>(IDbCommand cmd, ICollection<string> updateFields = null);

        /// <returns>If had RowVersion</returns>
        bool PrepareParameterizedDeleteStatement<T>(IDbCommand cmd, IDictionary<string, object> deleteFieldValues);

        void PrepareStoredProcedureStatement<T>(IDbCommand cmd, T obj);

        void SetParameterValues<T>(IDbCommand dbCmd, object obj);

        void SetParameter(FieldDefinition fieldDef, IDbDataParameter p);

        void EnableIdentityInsert<T>(IDbCommand cmd);
        Task EnableIdentityInsertAsync<T>(IDbCommand cmd, CancellationToken token=default);
        void DisableIdentityInsert<T>(IDbCommand cmd);
        Task DisableIdentityInsertAsync<T>(IDbCommand cmd, CancellationToken token=default);

        void EnableForeignKeysCheck(IDbCommand cmd);
        Task EnableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token=default);
        void DisableForeignKeysCheck(IDbCommand cmd);
        Task DisableForeignKeysCheckAsync(IDbCommand cmd, CancellationToken token=default);

        Dictionary<string, FieldDefinition> GetFieldDefinitionMap(ModelDefinition modelDef);

        object GetFieldValue(FieldDefinition fieldDef, object value);
        object GetFieldValue(Type fieldType, object value);

        void PrepareUpdateRowStatement(IDbCommand dbCmd, object objWithProperties, ICollection<string> updateFields = null);

        void PrepareUpdateRowStatement<T>(IDbCommand dbCmd, Dictionary<string, object> args, string sqlFilter);

        void PrepareUpdateRowAddStatement<T>(IDbCommand dbCmd, Dictionary<string, object> args, string sqlFilter);

        void PrepareInsertRowStatement<T>(IDbCommand dbCmd, Dictionary<string, object> args);

        string ToDeleteStatement(Type tableType, string sqlFilter, params object[] filterParams);

        IDbCommand CreateParameterizedDeleteStatement(IDbConnection connection, object objWithProperties);

        string ToExistStatement(Type fromTableType,
            object objWithProperties,
            string sqlFilter,
            params object[] filterParams);

        string ToSelectFromProcedureStatement(object fromObjWithProperties,
            Type outputModelType,
            string sqlFilter,
            params object[] filterParams);

        string ToExecuteProcedureStatement(object objWithProperties);

        string ToCreateSchemaStatement(string schema);
        string ToCreateTableStatement(Type tableType);
        string ToPostCreateTableStatement(ModelDefinition modelDef);
        string ToPostDropTableStatement(ModelDefinition modelDef);

        List<string> ToCreateIndexStatements(Type tableType);
        List<string> ToCreateSequenceStatements(Type tableType);
        string ToCreateSequenceStatement(Type tableType, string sequenceName);

        List<string> SequenceList(Type tableType);
        Task<List<string>> SequenceListAsync(Type tableType, CancellationToken token=default);

        List<string> GetSchemas(IDbCommand dbCmd);
        Dictionary<string, List<string>> GetSchemaTables(IDbCommand dbCmd);

        bool DoesSchemaExist(IDbCommand dbCmd, string schema);
        Task<bool> DoesSchemaExistAsync(IDbCommand dbCmd, string schema, CancellationToken token=default);
        bool DoesTableExist(IDbConnection db, string tableName, string schema = null);
        Task<bool> DoesTableExistAsync(IDbConnection db, string tableName, string schema = null, CancellationToken token=default);
        bool DoesTableExist(IDbCommand dbCmd, string tableName, string schema = null);
        Task<bool> DoesTableExistAsync(IDbCommand dbCmd, string tableName, string schema = null, CancellationToken token=default);
        bool DoesColumnExist(IDbConnection db, string columnName, string tableName, string schema = null);
        Task<bool> DoesColumnExistAsync(IDbConnection db, string columnName, string tableName, string schema = null, CancellationToken token=default);
        bool DoesSequenceExist(IDbCommand dbCmd, string sequenceName);
        Task<bool> DoesSequenceExistAsync(IDbCommand dbCmd, string sequenceName, CancellationToken token=default);

        void DropColumn(IDbConnection db, Type modelType, string columnName);

        object FromDbRowVersion(Type fieldType,  object value);

        SelectItem GetRowVersionSelectColumn(FieldDefinition field, string tablePrefix = null);
        string GetRowVersionColumn(FieldDefinition field, string tablePrefix = null);

        string GetColumnNames(ModelDefinition modelDef);
        SelectItem[] GetColumnNames(ModelDefinition modelDef, string tablePrefix);

        SqlExpression<T> SqlExpression<T>();

        IDbDataParameter CreateParam();

        //DDL
        string GetDropForeignKeyConstraints(ModelDefinition modelDef);

        string ToAddColumnStatement(Type modelType, FieldDefinition fieldDef);
        string ToAlterColumnStatement(Type modelType, FieldDefinition fieldDef);
        string ToChangeColumnNameStatement(Type modelType, FieldDefinition fieldDef, string oldColumnName);
        string ToAddForeignKeyStatement<T, TForeign>(Expression<Func<T, object>> field,
                                                     Expression<Func<TForeign, object>> foreignField,
                                                     OnFkOption onUpdate,
                                                     OnFkOption onDelete,
                                                     string foreignKeyName = null);
        string ToCreateIndexStatement<T>(Expression<Func<T, object>> field,
                                         string indexName = null, bool unique = false);

        //Async
        Task OpenAsync(IDbConnection db, CancellationToken token = default);
        Task<IDataReader> ExecuteReaderAsync(IDbCommand cmd, CancellationToken token = default);
        Task<int> ExecuteNonQueryAsync(IDbCommand cmd, CancellationToken token = default);
        Task<object> ExecuteScalarAsync(IDbCommand cmd, CancellationToken token = default);
        Task<bool> ReadAsync(IDataReader reader, CancellationToken token = default);
        Task<List<T>> ReaderEach<T>(IDataReader reader, Func<T> fn, CancellationToken token = default);
        Task<Return> ReaderEach<Return>(IDataReader reader, Action fn, Return source, CancellationToken token = default);
        Task<T> ReaderRead<T>(IDataReader reader, Func<T> fn, CancellationToken token = default);

        Task<long> InsertAndGetLastInsertIdAsync<T>(IDbCommand dbCmd, CancellationToken token);
    
        string GetLoadChildrenSubSelect<From>(SqlExpression<From> expr);
        string ToRowCountStatement(string innerSql);

        string ToUpdateStatement<T>(IDbCommand dbCmd, T item, ICollection<string> updateFields = null);
        string ToInsertStatement<T>(IDbCommand dbCmd, T item, ICollection<string> insertFields = null);
        string MergeParamsIntoSql(string sql, IEnumerable<IDbDataParameter> dbParams);

        string ToTableNamesStatement(string schema);

        /// <summary>
        /// Return table, row count SQL for listing all tables with their row counts
        /// </summary>
        /// <param name="live">If true returns live current row counts of each table (slower), otherwise returns cached row counts from RDBMS table stats</param>
        /// <param name="schema">The table schema if any</param>
        /// <returns></returns>
        string ToTableNamesWithRowCountsStatement(bool live, string schema);

        string SqlConflict(string sql, string conflictResolution);

        string SqlConcat(IEnumerable<object> args);
        string SqlCurrency(string fieldOrValue);
        string SqlCurrency(string fieldOrValue, string currencySymbol);
        string SqlBool(bool value);
        string SqlLimit(int? offset = null, int? rows = null);
        string SqlCast(object fieldOrValue, string castAs);
        string SqlRandom { get; }

        /// <summary>
        ///  Generates a SQL comment.
        /// </summary>
        /// <param name="text">The comment text.</param>
        /// <returns>The generated SQL.</returns>
        string GenerateComment(in string text);
    }
}