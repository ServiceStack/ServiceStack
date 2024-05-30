// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace ServiceStack.Aws.DynamoDb;

/// <summary>
/// Available API's with Async equivalents
/// </summary>
public interface IPocoDynamoAsync
{
    Task InitSchemaAsync(CancellationToken token = default);
    Task<List<string>> GetTableNamesAsync(CancellationToken token = default);
    Task<bool> CreateMissingTablesAsync(IEnumerable<DynamoMetadataType> tables, 
        CancellationToken token = default);
    Task<bool> CreateTablesAsync(IEnumerable<DynamoMetadataType> tables, TimeSpan? timeout = null,
        CancellationToken token = default);
    Task<bool> WaitForTablesToBeReadyAsync(IEnumerable<string> tableNames, 
        CancellationToken token = default);
    Task<bool> DeleteAllTablesAsync(TimeSpan? timeout = null, CancellationToken token = default);
    Task<bool> DeleteTablesAsync(IEnumerable<string> tableNames, TimeSpan? timeout = null,
        CancellationToken token = default);
    Task<bool> WaitForTablesToBeDeletedAsync(IEnumerable<string> tableNames, TimeSpan? timeout = null,
        CancellationToken token = default);
    Task<T> GetItemAsync<T>(object hash, CancellationToken token = default);
    Task<T> GetItemAsync<T>(object hash, object range, CancellationToken token = default);
    Task<T> GetItemAsync<T>(DynamoId id, CancellationToken token = default);
    Task<List<T>> GetItemsAsync<T>(IEnumerable<object> hashes, CancellationToken token = default);
    Task<List<T>> GetItemsAsync<T>(IEnumerable<DynamoId> ids, CancellationToken token = default);
    Task<List<T>> GetRelatedItemsAsync<T>(object hash, CancellationToken token = default);
    Task DeleteRelatedItemsAsync<T>(object hash, IEnumerable<object> ranges, CancellationToken token = default);
    Task<T> PutItemAsync<T>(T value, bool returnOld = false, CancellationToken token = default);
    Task<bool> UpdateItemAsync<T>(UpdateExpression<T> update, CancellationToken token = default);
    Task UpdateItemAsync<T>(DynamoUpdateItem update, CancellationToken token = default);
    Task<T> UpdateItemNonDefaultsAsync<T>(T value, bool returnOld = false, CancellationToken token = default);
    Task PutRelatedItemAsync<T>(object hash, T item, CancellationToken token = default);
    Task PutRelatedItemsAsync<T>(object hash, IEnumerable<T> items, CancellationToken token = default);
    Task PutItemsAsync<T>(IEnumerable<T> items, CancellationToken token = default);
    Task<T> DeleteItemAsync<T>(object hash, ReturnItem returnItem = ReturnItem.None,
        CancellationToken token = default);
    Task<T> DeleteItemAsync<T>(DynamoId id, ReturnItem returnItem = ReturnItem.None,
        CancellationToken token = default);
    Task<T> DeleteItemAsync<T>(object hash, object range, ReturnItem returnItem = ReturnItem.None,
        CancellationToken token = default);
    Task DeleteItemsAsync<T>(IEnumerable<object> hashes, CancellationToken token = default);
    Task DeleteItemsAsync<T>(IEnumerable<DynamoId> ids, CancellationToken token = default);
    Task<long> IncrementAsync<T>(object hash, string fieldName, long amount = 1, CancellationToken token = default);
    Task<long> ScanItemCountAsync<T>(CancellationToken token = default);
    Task<long> DescribeItemCountAsync<T>(CancellationToken token = default);

    IAsyncEnumerable<T> ScanAllAsync<T>(CancellationToken token = default);
    IAsyncEnumerable<T> ScanAsync<T>(ScanRequest request, Func<ScanResponse, IEnumerable<T>> converter,
        CancellationToken token = default);
    IAsyncEnumerable<T> ScanAsync<T>(ScanExpression<T> request, int limit, CancellationToken token = default);
    IAsyncEnumerable<T> ScanAsync<T>(ScanExpression<T> request, CancellationToken token = default);
    IAsyncEnumerable<T> ScanAsync<T>(ScanRequest request, int limit, CancellationToken token = default);
    IAsyncEnumerable<T> ScanAsync<T>(ScanRequest request, CancellationToken token = default);
    IAsyncEnumerable<T> QueryAsync<T>(QueryExpression<T> request, CancellationToken token = default);
    IAsyncEnumerable<T> QueryAsync<T>(QueryExpression<T> request, int limit, CancellationToken token = default);
    IAsyncEnumerable<T> QueryAsync<T>(QueryRequest request, CancellationToken token = default);
    IAsyncEnumerable<T> QueryAsync<T>(QueryRequest request, int limit, CancellationToken token = default);
    IAsyncEnumerable<T> QueryAsync<T>(QueryRequest request, Func<QueryResponse, IEnumerable<T>> converter,
        CancellationToken token = default);
}