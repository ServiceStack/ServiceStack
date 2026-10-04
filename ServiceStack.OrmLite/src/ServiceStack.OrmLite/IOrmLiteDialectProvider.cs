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

using ServiceStack.DataAnnotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

public enum DbKind
{
    Sqlite,
    PostgreSql,
    SqlServer,
    MySql,
    Oracle,
    Firebird,
    Unknown,
}

public interface IOrmLiteDialectProvider
{
    DbKind Kind { get; }

    /// <summary>
    /// Max number of values in a single IN list (i.e. db params) before APIs like SelectByIds / DeleteByIds are
    /// executed in batches and Contains() expressions use an alternative strategy
    /// </summary>
    int MaxInListParams { get; set; }

    /// <summary>
    /// Whether InsertAll, UpdateAll, UpsertAll and SaveAll send their statements together with an ADO.NET DbBatch
    /// when the driver supports it, instead of a round trip for each row. Enabled by default.
    /// </summary>
    bool UseDbBatch { get; set; }

    /// <summary>
    /// The most statements that are sent together when UseDbBatch is enabled, 1000 by default
    /// </summary>
    int BatchSize { get; set; }

    /// <summary>
    /// Whether the driver returns the rows affected by each statement of a DbBatch, which UpdateAll needs to know
    /// if a row with a RowVersion was updated
    /// </summary>
    bool SupportsBatchRowsAffected { get; }

    /// <summary>
    /// When statements and connections that fail with a temporary error are run again, e.g. after a deadlock,
    /// throttling or a lost connection. Uses OrmLiteConfig.RetryPolicy when it's not set, use OrmLiteRetry.None to
    /// not retry this dialect. Null when nothing is retried: without a policy, with one that doesn't retry, e.g.
    /// OrmLiteRetry.None, or when the dialect doesn't support retries.
    /// </summary>
    OrmLiteRetryPolicy RetryPolicy { get; set; }

    /// <summary>
    /// Whether the dialect retries temporary errors, when it has a RetryPolicy or there's a global one
    /// </summary>
    bool SupportsRetries { get; }

    /// <summary>
    /// The statement that makes the rest of a connection's session read-only, or read-write again, or null when the
    /// database doesn't have one, e.g. SQL Server, whose read-only connections OrmLite checks instead
    /// </summary>
    string ToReadOnlySessionStatement(bool readOnly);

    /// <summary>
    /// What's selected for a column, which is the column unless the driver can't read it as it is, e.g. the vectors
    /// of PostgreSQL, which are selected as text
    /// </summary>
    string ToSelectColumn(FieldDefinition fieldDef, string quotedColumn);

    /// <summary>
    /// Whether an error of the driver is temporary, and if it is, whether the database confirmed the statement
    /// wasn't applied
    /// </summary>
    TransientError GetTransientError(Exception ex);

    /// <summary>
    /// Converts an UPDATE or DELETE statement into one that also returns the affected rows, e.g. with RETURNING or
    /// OUTPUT, with all their columns or only the returnFields. Throws NotSupportedException if the RDBMS doesn't
    /// support it.
    /// </summary>
    string ToReturningStatement(string sql, ModelDefinition modelDef, bool isDelete, ICollection<FieldDefinition> returnFields = null);

    /// <summary>
    /// How to get the query plan of a statement, where analyze also runs the statement to include actual row counts
    /// and timings. Throws NotSupportedException if the RDBMS doesn't support it.
    /// </summary>
    ExplainQuery ToExplainQuery(IDbConnection db, string sql, bool analyze);
    
    /// <summary>
    /// Configure Provider with connection string options 
    /// </summary>
    void Init(string connectionString);
        
    /// <summary>
    /// Register custom value type converter  
    /// </summary>
    void RegisterConverter<T>(IOrmLiteConverter converter);

    /// <summary>
    /// Used to create an OrmLiteConnection
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="namedConnection"></param>
    /// <returns></returns>
    OrmLiteConnection CreateOrmLiteConnection(OrmLiteConnectionFactory factory, string namedConnection = null);

    /// <summary>
    /// Invoked when a DB Connection is opened
    /// </summary>
    void InitConnection(IDbConnection dbConn);

    /// <summary>
    /// Custom delegate invoked when a DB Connection is opened
    /// </summary>
    Action<IDbConnection> OnOpenConnection { get; set; }
    Action<IDbConnection> OnDisposeConnection { get; set; }
    Action<IDbCommand> OnBeforeExecuteNonQuery { get; set; }
    Action<IDbCommand> OnAfterExecuteNonQuery { get; set; }

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

    bool SupportsSchema { get; }
    bool SupportsConcurrentWrites { get; }

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

    /// <summary>
    /// Whether the SQL is a complete SELECT statement, e.g. starting with SELECT or a common table expression, rather
    /// than a WHERE filter
    /// </summary>
    bool IsFullSelectStatement(string sql);

    /// <summary>
    /// Read each field of a row individually instead of with a single IDataReader.GetValues() call, for ADO.NET
    /// providers whose GetValues() changes how fields are read, e.g. System.Data.SQLite's GetGuid()
    /// </summary>
    bool DeoptimizeReader { get; set; }

    /// <summary>
    /// Table hint that locks the rows selected from a table for SqlExpression.ForUpdate(), e.g. WITH (UPDLOCK, ROWLOCK)
    /// in SQL Server, or null if the RDBMS uses a lock clause instead
    /// </summary>
    string GetForUpdateTableHint(bool skipLocked);

    /// <summary>
    /// Clause appended to a SELECT statement to lock the selected rows for SqlExpression.ForUpdate(), e.g. FOR UPDATE,
    /// or null if the RDBMS uses a table hint or doesn't support row locks
    /// </summary>
    /// <param name="lockTable">The quoted table or alias to lock when the query has joins, otherwise null</param>
    /// <param name="skipLocked">Skip rows locked by other transactions instead of waiting for them</param>
    string GetForUpdateClause(string lockTable, bool skipLocked);

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

    string GetTableNameOnly(TableRef tableRef);
    string UnquotedTable(TableRef tableRef);
    string GetSchemaName(TableRef tableRef);
    string QuoteSchema(string schema, string table);
    string QuoteTable(TableRef tableRef);
    string GetQuotedTableName(Type modelType);
    string GetQuotedTableName(ModelDefinition modelDef);

    string GetQuotedColumnName(string columnName);
    string GetQuotedColumnName(FieldDefinition fieldDef);

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

    string ToInsertRowSql<T>(T obj, ICollection<string> insertFields = null);
    string ToInsertRowsSql<T>(IEnumerable<T> objs, ICollection<string> insertFields = null);

    void BulkInsert<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null);
    
    Task BulkInsertAsync<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null, CancellationToken token=default);

    /// <summary>
    /// Bulk loads the rows into a temporary table, then inserts those with a new primary key and updates those
    /// with an existing one in a single statement
    /// </summary>
    void BulkUpsert<T>(IDbConnection db, IEnumerable<T> objs, ICollection<string> updateOnly = null, BulkInsertConfig config = null);

    Task BulkUpsertAsync<T>(IDbConnection db, IEnumerable<T> objs, ICollection<string> updateOnly = null, BulkInsertConfig config = null, CancellationToken token=default);
        
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
    string ToDropTableStatement(ModelDefinition modelDef);

    /// <summary>
    /// The clause that reads previous versions of a system-versioned table, e.g. FOR SYSTEM_TIME AS OF @0
    /// </summary>
    string ToSystemTimeClause(string condition);
    /// <summary>
    /// The time a system-versioned table is compared with, e.g. in UTC for SQL Server
    /// </summary>
    DateTime ToSystemTime(DateTime time);

    List<string> ToCreateIndexStatements(Type tableType);
    /// <summary>
    /// The statements that add the [Description] of a table and its columns as comments
    /// </summary>
    List<string> ToCreateCommentStatements(Type tableType);

    /// <summary>
    /// Converts the float[] of a [Vector] column to and from the RDBMS's vector type
    /// </summary>
    IOrmLiteConverter VectorConverter { get; set; }
    /// <summary>
    /// The SQL for a db param with a vector's value, e.g. cast to the RDBMS's vector type
    /// </summary>
    string ToVectorParam(string param, int dimensions);
    /// <summary>
    /// The SQL for a db param with a vector's value, cast to the vector type of a column with the precision
    /// </summary>
    string ToVectorParam(string param, int dimensions, VectorPrecision precision);
    /// <summary>
    /// The statements that set how this connection searches vector indexes, e.g. PostgreSQL's hnsw.ef_search,
    /// which are empty where the RDBMS doesn't have them
    /// </summary>
    List<string> ToVectorSearchStatements(VectorSearchOptions options);
    /// <summary>
    /// The SQL for the distance of 2 vectors, which are columns or the SQL from ToVectorParam()
    /// </summary>
    string ToVectorDistance(VectorDistance distance, string vector, string other);
    List<string> ToCreateSequenceStatements(Type tableType);
    string ToCreateSequenceStatement(Type tableType, string sequenceName);
    string ToResetSequenceStatement(Type tableType, string columnName, int value);

    string ToCreateSavePoint(string name);
    string ToReleaseSavePoint(string name);
    string ToRollbackSavePoint(string name);

    List<string> SequenceList(Type tableType);
    Task<List<string>> SequenceListAsync(Type tableType, CancellationToken token=default);

    List<string> GetSchemas(IDbCommand dbCmd);
    Dictionary<string, List<string>> GetSchemaTables(IDbCommand dbCmd);

    bool DoesSchemaExist(IDbCommand dbCmd, string schema);
    Task<bool> DoesSchemaExistAsync(IDbCommand dbCmd, string schema, CancellationToken token=default);
    bool DoesTableExist(IDbConnection db, TableRef tableRef);
    Task<bool> DoesTableExistAsync(IDbConnection db, TableRef tableRef, CancellationToken token=default);
    bool DoesTableExist(IDbCommand dbCmd, TableRef tableRef);
    Task<bool> DoesTableExistAsync(IDbCommand dbCmd, TableRef tableRef, CancellationToken token=default);
    bool DoesColumnExist(IDbConnection db, string columnName, TableRef tableRef);
    Task<bool> DoesColumnExistAsync(IDbConnection db, string columnName, TableRef tableRef, CancellationToken token=default);
    bool DoesSequenceExist(IDbCommand dbCmd, string sequence);
    Task<bool> DoesSequenceExistAsync(IDbCommand dbCmd, string sequenceName, CancellationToken token=default);

    object FromDbRowVersion(Type fieldType,  object value);

    SelectItem GetRowVersionSelectColumn(FieldDefinition field, string tablePrefix = null);
    string GetRowVersionColumn(FieldDefinition field, string tablePrefix = null);

    string GetColumnNames(ModelDefinition modelDef);
    SelectItem[] GetColumnNames(ModelDefinition modelDef, string tablePrefix);

    SqlExpression<T> SqlExpression<T>();

    IDbDataParameter CreateParam();

    //DDL
    string GetDropForeignKeyConstraints(ModelDefinition modelDef);

    /// <summary>
    /// The columns of a table as they're reported by the database, e.g. their type, size and if they allow nulls
    /// </summary>
    ColumnSchema[] GetSchemaColumns(IDbConnection db, string quotedTable);

    /// <summary>
    /// The columns the fields of a model are created with, as they're reported by the database, which are read
    /// from a temporary table that's created with them
    /// </summary>
    ColumnSchema[] GetModelSchemaColumns(IDbConnection db, List<FieldDefinition> fieldDefs);

    /// <summary>
    /// The names of the indexes of a table, or null if they can't be read
    /// </summary>
    List<string> GetTableIndexNames(IDbConnection db, TableRef tableRef);

    /// <summary>
    /// The indexes of a table with their key columns, or null if they can't be read
    /// </summary>
    List<IndexSchema> GetTableIndexes(IDbConnection db, TableRef tableRef);

    /// <summary>
    /// The default values of the columns of a table by column name, as they're written by the database, e.g.
    /// ((0)) on SQL Server, with null for columns without a default, or null if they can't be read. It reads the
    /// temporary tables of GetModelSchemaColumns() too.
    /// </summary>
    Dictionary<string, string> GetColumnDefaults(IDbConnection db, string quotedTable);

    /// <summary>
    /// The foreign keys of a table, or null if they can't be read
    /// </summary>
    List<ForeignKeySchema> GetTableForeignKeys(IDbConnection db, TableRef tableRef);

    string ToAddColumnStatement(TableRef tableRef, FieldDefinition fieldDef);
    string ToAlterColumnStatement(TableRef tableRef, FieldDefinition fieldDef);
    string ToChangeColumnNameStatement(TableRef tableRef, FieldDefinition fieldDef, string oldColumn);
    string ToRenameColumnStatement(TableRef tableRef, string oldColumn, string newColumn);
    string ToDropColumnStatement(TableRef tableRef, string column);
    string ToDropConstraintStatement(TableRef tableRef, string constraint);
        
    string ToAddForeignKeyStatement<T, TForeign>(Expression<Func<T, object>> field,
        Expression<Func<TForeign, object>> foreignField,
        OnFkOption onUpdate,
        OnFkOption onDelete,
        string foreignKeyName = null);

    string ToDropForeignKeyStatement(TableRef tableRef, string foreignKeyName);

    /// <summary>
    /// Add the foreign key of a field to its table, or null if the database can't add it to an existing table
    /// </summary>
    string ToAddForeignKeyStatement(TableRef tableRef, FieldDefinition fieldDef);

    /// <summary>
    /// Change the default value of a column to its field's, or remove it when the field doesn't have one, or null if
    /// the database can't change it
    /// </summary>
    string ToAlterColumnDefaultStatement(TableRef tableRef, FieldDefinition fieldDef);
        
    string ToCreateIndexStatement<T>(Expression<Func<T,object>> field, string indexName=null, bool unique=false);

    string ToDropIndexStatement<T>(string indexName);

    //Async
    bool SupportsAsync { get; }
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
        
    string GetRefSelfSql<From>(SqlExpression<From> refQ, ModelDefinition modelDef, FieldDefinition refSelf, ModelDefinition refModelDef, FieldDefinition refId);
    string GetRefFieldSql(string subSql, ModelDefinition refModelDef, FieldDefinition refField);
    string GetFieldReferenceSql(string subSql, FieldDefinition fieldDef, FieldReference fieldRef);

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
    string SqlDateFormat(string quotedColumn, string format);
    string SqlChar(int charCode);

    string SqlRandom { get; }

    /// <summary>
    ///  Generates a SQL comment.
    /// </summary>
    /// <param name="text">The comment text.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateComment(in string text);
}