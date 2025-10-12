// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

namespace ServiceStack.Aws.DynamoDb;

/// <summary>
/// Interface for the code-first PocoDynamo client for DynamoDB
/// </summary>
public interface IPocoDynamo : IPocoDynamoAsync, IRequiresSchema
{
    /// <summary>
    /// Get the underlying AWS DynamoDB low-level client
    /// </summary>
    IAmazonDynamoDB DynamoDb { get; }

    /// <summary>
    /// Get the numeric Sequence provider configured with this client
    /// </summary>
    ISequenceSource Sequences { get; }

    /// <summary>
    /// Get the Async numeric Sequence provider configured with this client
    /// </summary>
    ISequenceSourceAsync SequencesAsync { get; }

    /// <summary>
    /// Access the converters that converts POCO's into DynamoDB data types
    /// </summary>
    DynamoConverters Converters { get; }

    /// <summary>
    /// How long should PocoDynamo keep retrying failed operations in an exponential backoff (default 60s)
    /// </summary>
    TimeSpan MaxRetryOnExceptionTimeout { get; }

    /// <summary>
    /// Get the AWSSDK DocumentModel schema for this Table
    /// </summary>
    ITable GetTableSchema(Type table);

    /// <summary>
    /// Get PocoDynamo Table metadata for this table
    /// </summary>
    DynamoMetadataType GetTableMetadata(Type table);

    /// <summary>
    /// Calls 'ListTables' to return all Table Names in DynamoDB
    /// </summary>
    IEnumerable<string> GetTableNames();

    /// <summary>
    /// Creates any tables missing in DynamoDB from the Tables registered with PocoDynamo
    /// </summary>
    bool CreateMissingTables(IEnumerable<DynamoMetadataType> tables, TimeSpan? timeout = null);

    /// <summary>
    /// Creates any tables missing from the specified list of tables
    /// </summary>
    bool CreateTables(IEnumerable<DynamoMetadataType> tables, TimeSpan? timeout = null);

    /// <summary>
    /// Polls 'DescribeTable' until all Tables have an ACTIVE TableStatus
    /// </summary>
    bool WaitForTablesToBeReady(IEnumerable<string> tableNames, TimeSpan? timeout = null);

    /// <summary>
    /// Deletes all DynamoDB Tables
    /// </summary>
    bool DeleteAllTables(TimeSpan? timeout = null);

    /// <summary>
    /// Deletes the tables in DynamoDB with the specified table names
    /// </summary>
    bool DeleteTables(IEnumerable<string> tableNames, TimeSpan? timeout = null);

    /// <summary>
    /// Polls 'ListTables' until all specified tables have been deleted
    /// </summary>
    bool WaitForTablesToBeDeleted(IEnumerable<string> tableNames, TimeSpan? timeout = null);

    /// <summary>
    /// Gets the POCO instance with the specified hash
    /// </summary>
    T GetItem<T>(object hash);

    /// <summary>
    /// Gets the POCO instance with the specified hash and range value
    /// </summary>
    T GetItem<T>(DynamoId id);

    /// <summary>
    /// Gets the POCO instance with the specified hash and range value
    /// </summary>
    T GetItem<T>(object hash, object range);

    /// <summary>
    /// Calls 'BatchGetItem' in the min number of batch requests to return POCOs with the specified hashes 
    /// </summary>
    List<T> GetItems<T>(IEnumerable<object> hashes);

    /// <summary>
    /// Calls 'BatchGetItem' in the min number of batch requests to return POCOs with the specified hash and ranges 
    /// </summary>
    List<T> GetItems<T>(IEnumerable<DynamoId> ids);

    /// <summary>
    /// Calls 'PutItem' to store instance in DynamoDB
    /// </summary>
    T PutItem<T>(T value, bool returnOld = false);

    /// <summary>
    /// Creates an Typed `UpdateExpression` for the specified table
    /// </summary>
    UpdateExpression<T> UpdateExpression<T>(object hash, object range = null);

    /// <summary>
    /// Calls 'UpdateItem' to SET, REMOVE, ADD or DELETE item fields in DynamoDB.
    /// </summary>
    /// <returns>false if conditional check failed, otherwise true</returns>
    bool UpdateItem<T>(UpdateExpression<T> update);

    /// <summary>
    /// Calls 'UpdateItem' to ADD, PUT or DELETE item fields in DynamoDB
    /// </summary>
    void UpdateItem<T>(DynamoUpdateItem update);

    /// <summary>
    /// Calls 'UpdateItem' to PUT non null or default values from instance into DynamoDB
    /// </summary>
    T UpdateItemNonDefaults<T>(T value, bool returnOld = false);

    /// <summary>
    /// Calls 'BatchWriteItem' to efficiently store items in min number of batched requests
    /// </summary>
    void PutItems<T>(IEnumerable<T> items);

    /// <summary>
    /// Deletes the instance at the specified hash
    /// </summary>
    T DeleteItem<T>(object hash, ReturnItem returnItem = ReturnItem.None);

    /// <summary>
    /// Deletes the instance at the specified hash and range values
    /// </summary>
    T DeleteItem<T>(DynamoId id, ReturnItem returnItem = ReturnItem.None);

    /// <summary>
    /// Deletes the instance at the specified hash and range values
    /// </summary>
    T DeleteItem<T>(object hash, object range, ReturnItem returnItem = ReturnItem.None);

    /// <summary>
    /// Calls 'BatchWriteItem' to efficiently delete all items with the specified hashes
    /// </summary>
    void DeleteItems<T>(IEnumerable<object> hashes);

    /// <summary>
    /// Calls 'BatchWriteItem' to efficiently delete all items with the specified hash and range pairs
    /// </summary>
    void DeleteItems<T>(IEnumerable<DynamoId> ids);

    /// <summary>
    /// Calls 'UpdateItem' with ADD AttributeUpdate to atomically increment specific field numeric value
    /// </summary>
    long Increment<T>(object hash, string fieldName, long amount = 1);

    /// <summary>
    /// Updates item Hash field with hash value then calls 'PutItem' to store the related instance
    /// </summary>
    void PutRelatedItem<T>(object hash, T item);

    /// <summary>
    /// Updates all item Hash fields with hash value then calls 'PutItems' to store all related instances
    /// </summary>
    void PutRelatedItems<T>(object hash, IEnumerable<T> items);

    /// <summary>
    /// Calls 'Query' to return all related Items containing the specified hash value
    /// </summary>
    IEnumerable<T> GetRelatedItems<T>(object hash);

    /// <summary>
    /// Deletes all items with the specified hash and ranges
    /// </summary>
    void DeleteRelatedItems<T>(object hash, IEnumerable<object> ranges);


    /// <summary>
    /// Calls 'Scan' to return lazy enumerated results that's transparently paged across multiple queries
    /// </summary>
    IEnumerable<T> ScanAll<T>();

    /// <summary>
    /// Creates a Typed `ScanExpression` for the specified table
    /// </summary>
    ScanExpression<T> FromScan<T>(Expression<Func<T, bool>> filterExpression = null);

    /// <summary>
    /// Creates a Typed `ScanExpression` for the specified Global Index
    /// </summary>
    ScanExpression<T> FromScanIndex<T>(Expression<Func<T, bool>> filterExpression = null);

    /// <summary>
    /// Executes the `ScanExpression` returning the specified maximum limit of results
    /// </summary>
    List<T> Scan<T>(ScanExpression<T> request, int limit);

    /// <summary>
    /// Executes the `ScanExpression` returning lazy results transparently paged across multiple queries
    /// </summary>
    IEnumerable<T> Scan<T>(ScanExpression<T> request);

    /// <summary>
    /// Executes AWSSDK `ScanRequest` returning the specified maximum limit of results
    /// </summary>
    List<T> Scan<T>(ScanRequest request, int limit);

    /// <summary>
    /// Executes AWSSDK `ScanRequest` returning lazy results transparently paged across multiple queries
    /// </summary>
    IEnumerable<T> Scan<T>(ScanRequest request);

    /// <summary>
    /// Executes AWSSDK `ScanRequest` with a custom conversion function to map ScanResponse to results
    /// </summary>
    IEnumerable<T> Scan<T>(ScanRequest request, Func<ScanResponse, IEnumerable<T>> converter);


    /// <summary>
    /// Creates a Typed `QueryExpression` for the specified table
    /// </summary>
    QueryExpression<T> FromQuery<T>(Expression<Func<T, bool>> keyExpression = null);

    /// <summary>
    /// Executes the `QueryExpression` returning lazy results transparently paged across multiple queries
    /// </summary>
    IEnumerable<T> Query<T>(QueryExpression<T> request);

    /// <summary>
    /// Executes the `QueryExpression` returning the specified maximum limit of results
    /// </summary>
    List<T> Query<T>(QueryExpression<T> request, int limit);

    /// <summary>
    /// Creates a Typed `QueryExpression` for the specified Local or Global Index
    /// </summary>
    QueryExpression<T> FromQueryIndex<T>(Expression<Func<T, bool>> keyExpression = null);

    /// <summary>
    /// Executes AWSSDK `QueryRequest` returning the specified maximum limit of results
    /// </summary>
    List<T> Query<T>(QueryRequest request, int limit);

    /// <summary>
    /// Executes AWSSDK `QueryRequest` returning lazy results transparently paged across multiple queries
    /// </summary>
    IEnumerable<T> Query<T>(QueryRequest request);

    /// <summary>
    /// Executes AWSSDK `QueryRequest` with a custom conversion function to map QueryResponse to results
    /// </summary>
    IEnumerable<T> Query<T>(QueryRequest request, Func<QueryResponse, IEnumerable<T>> converter);

    /// <summary>
    /// Return Live ItemCount using Table ScanRequest
    /// </summary>
    int? ScanItemCount<T>();

    /// <summary>
    /// Return cached ItemCount in summary DescribeTable
    /// </summary>
    long? DescribeItemCount<T>();

    /// <summary>
    /// Create a clone of the PocoDynamo client with different default settings
    /// </summary>
    IPocoDynamo ClientWith(
        bool? consistentRead = null,
        long? readCapacityUnits = null,
        long? writeCapacityUnits = null,
        TimeSpan? pollTableStatus = null,
        TimeSpan? maxRetryOnExceptionTimeout = null,
        int? limit = null,
        bool? scanIndexForward = null);

    /// <summary>
    /// Disposes the underlying IAmazonDynamoDB client
    /// </summary>
    void Close();
}