// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDb;

public partial class PocoDynamo
{
    public Action<Exception> ExceptionFilter { get; set; }

    //Error Handling: http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/ErrorHandling.html
    public void Exec(Action fn, Type[] rethrowExceptions = null, HashSet<string> retryOnErrorCodes = null)
    {
        Exec(() => {
            fn();
            return true;
        }, rethrowExceptions, retryOnErrorCodes);
    }

    public async Task ExecAsync(Func<Task> fn, Type[] rethrowExceptions = null, HashSet<string> retryOnErrorCodes = null)
    {
        await ExecAsync(async () => {
            await fn();
            return true;
        }, rethrowExceptions, retryOnErrorCodes);
    }

    public T Exec<T>(Func<T> fn, Type[] rethrowExceptions = null, HashSet<string> retryOnErrorCodes = null)
    {
        var i = 0;
        Exception originalEx = null;
        var firstAttempt = DateTime.UtcNow;

        if (retryOnErrorCodes == null)
            retryOnErrorCodes = RetryOnErrorCodes;

        while (DateTime.UtcNow - firstAttempt < MaxRetryOnExceptionTimeout)
        {
            i++;
            try
            {
                return fn();
            }
            catch (Exception outerEx)
            {
                var ex = outerEx.UnwrapIfSingleException();

                ExceptionFilter?.Invoke(ex);

                if (rethrowExceptions != null)
                {
                    foreach (var rethrowEx in rethrowExceptions)
                    {
                        if (ex.GetType().IsAssignableFrom(rethrowEx))
                        {
                            if (ex != outerEx)
                                throw ex;

                            throw;
                        }
                    }
                }

                if (originalEx == null)
                    originalEx = ex;

                var amazonEx = ex as AmazonDynamoDBException;
                if (amazonEx?.StatusCode == HttpStatusCode.BadRequest &&
                    !retryOnErrorCodes.Contains(amazonEx.ErrorCode))
                    throw;

                i.SleepBackOffMultiplier();
            }
        }

        throw new TimeoutException($"Exceeded timeout of {MaxRetryOnExceptionTimeout}", originalEx);
    }

    public async Task<T> ExecAsync<T>(Func<Task<T>> fn, Type[] rethrowExceptions = null, HashSet<string> retryOnErrorCodes = null)
    {
        var i = 0;
        Exception originalEx = null;
        var firstAttempt = DateTime.UtcNow;

        if (retryOnErrorCodes == null)
            retryOnErrorCodes = RetryOnErrorCodes;

        while (DateTime.UtcNow - firstAttempt < MaxRetryOnExceptionTimeout)
        {
            i++;
            try
            {
                return await fn();
            }
            catch (Exception ex)
            {
                ExceptionFilter?.Invoke(ex);

                if (rethrowExceptions != null)
                {
                    foreach (var rethrowEx in rethrowExceptions)
                    {
                        if (ex.GetType().IsAssignableFrom(rethrowEx))
                            throw;
                    }
                }

                if (originalEx == null)
                    originalEx = ex;

                var amazonEx = ex as AmazonDynamoDBException;
                if (amazonEx?.StatusCode == HttpStatusCode.BadRequest &&
                    !retryOnErrorCodes.Contains(amazonEx.ErrorCode))
                    throw;

                await i.SleepBackOffMultiplierAsync();
            }
        }

        throw new TimeoutException($"Exceeded timeout of {MaxRetryOnExceptionTimeout}", originalEx);
    }

    public bool WaitForTablesToBeReady(IEnumerable<string> tableNames, TimeSpan? timeout = null)
    {
        var pendingTables = new List<string>(tableNames);

        if (pendingTables.Count == 0)
            return true;

        var startAt = DateTime.UtcNow;
        do
        {
            try
            {
                var responses = pendingTables.Map(x =>
                    Exec(() => DynamoDb.DescribeTable(new DescribeTableRequest(x))));

                foreach (var response in responses)
                {
                    if (response.Table.TableStatus == DynamoStatus.Active)
                        pendingTables.Remove(response.Table.TableName);
                }

                if (Log.IsDebugEnabled)
                    Log.Debug($"Tables Pending: {pendingTables.ToJsv()}");

                if (pendingTables.Count == 0)
                    return true;

                if (timeout != null && DateTime.UtcNow - startAt > timeout.Value)
                    return false;

                Thread.Sleep(PollTableStatus);
            }
            catch (ResourceNotFoundException)
            {
                // DescribeTable is eventually consistent. So you might
                // get resource not found. So we handle the potential exception.
            }
        } while (true);
    }

    public async Task<bool> WaitForTablesToBeReadyAsync(IEnumerable<string> tableNames, CancellationToken token = default)
    {
        var pendingTables = new List<string>(tableNames);

        if (pendingTables.Count == 0)
            return true;

        do
        {
            try
            {
                var responses = await Task.WhenAll(pendingTables.Map(x =>
                    ExecAsync(() => DynamoDb.DescribeTableAsync(x, token))
                ).ToArray()).ConfigAwait();

                foreach (var response in responses)
                {
                    if (response.Table.TableStatus == DynamoStatus.Active)
                        pendingTables.Remove(response.Table.TableName);
                }

                if (Log.IsDebugEnabled)
                    Log.Debug($"Tables Pending: {pendingTables.ToJsv()}");

                if (pendingTables.Count == 0)
                    return true;

                if (token.IsCancellationRequested)
                    return false;

                await Task.Delay(PollTableStatus, token).ConfigAwait();
            }
            catch (ResourceNotFoundException)
            {
                // DescribeTable is eventually consistent. So you might
                // get resource not found. So we handle the potential exception.
            }
        } while (true);
    }

    public bool WaitForTablesToBeDeleted(IEnumerable<string> tableNames, TimeSpan? timeout = null)
    {
        var pendingTables = new List<string>(tableNames);

        if (pendingTables.Count == 0)
            return true;

        var startAt = DateTime.UtcNow;
        do
        {
            var existingTables = GetTableNames().ToList();
            pendingTables.RemoveAll(x => !existingTables.Contains(x));

            if (Log.IsDebugEnabled)
                Log.DebugFormat("Waiting for Tables to be removed: {0}", pendingTables.Dump());

            if (pendingTables.Count == 0)
                return true;

            if (timeout != null && DateTime.UtcNow - startAt > timeout.Value)
                return false;

            Thread.Sleep(PollTableStatus);

        } while (true);
    }

    public async Task<bool> WaitForTablesToBeDeletedAsync(IEnumerable<string> tableNames, TimeSpan? timeout = null, CancellationToken token = default)
    {
        var pendingTables = new List<string>(tableNames);

        if (pendingTables.Count == 0)
            return true;

        var startAt = DateTime.UtcNow;
        do
        {
            var existingTables = (await GetTableNamesAsync(token).ConfigAwait()).ToList();
            pendingTables.RemoveAll(x => !existingTables.Contains(x));

            if (Log.IsDebugEnabled)
                Log.DebugFormat("Waiting for Tables to be removed: {0}", pendingTables.Dump());

            if (pendingTables.Count == 0)
                return true;

            if (timeout != null && DateTime.UtcNow - startAt > timeout.Value)
                return false;

            await Task.Delay(PollTableStatus, token).ConfigAwait();

        } while (true);
    }

    private T ConvertGetItemResponse<T>(GetItemRequest request, DynamoMetadataType table)
    {
        var response = Exec(() => DynamoDb.GetItem(request), rethrowExceptions: throwNotFoundExceptions);

        if (!response.IsItemSet)
            return default;
        var attributeValues = response.Item;

        return Converters.FromAttributeValues<T>(table, attributeValues);
    }

    private async Task<T> ConvertGetItemResponseAsync<T>(GetItemRequest request, DynamoMetadataType table, CancellationToken token = default)
    {
        var response = await ExecAsync(async () => 
            await DynamoDb.GetItemAsync(request, token).ConfigAwait(), throwNotFoundExceptions).ConfigAwait();

        if (!response.IsItemSet)
            return default;
        var attributeValues = response.Item;

        return Converters.FromAttributeValues<T>(table, attributeValues);
    }

    private List<T> ConvertBatchGetItemResponse<T>(DynamoMetadataType table, KeysAndAttributes getItems)
    {
        var to = new List<T>();

        var request = new BatchGetItemRequest(new Dictionary<string, KeysAndAttributes> {
            {table.Name, getItems}
        });

        var response = Exec(() => DynamoDb.BatchGetItem(request));

        if (response.Responses.TryGetValue(table.Name, out var results))
            results.Each(x => to.Add(Converters.FromAttributeValues<T>(table, x)));

        var i = 0;
        while (response.UnprocessedKeys.Count > 0)
        {
            response = Exec(() => DynamoDb.BatchGetItem(new BatchGetItemRequest(response.UnprocessedKeys)));
            if (response.Responses.TryGetValue(table.Name, out results))
                results.Each(x => to.Add(Converters.FromAttributeValues<T>(table, x)));

            if (response.UnprocessedKeys.Count > 0)
                i.SleepBackOffMultiplier();
        }

        return to;
    }

    private async Task<List<T>> ConvertBatchGetItemResponseAsync<T>(DynamoMetadataType table, KeysAndAttributes getItems, CancellationToken token = default)
    {
        var to = new List<T>();

        var request = new BatchGetItemRequest(new Dictionary<string, KeysAndAttributes> {
            {table.Name, getItems}
        });

        var response = await ExecAsync(async () => 
            await DynamoDb.BatchGetItemAsync(request, token).ConfigAwait()).ConfigAwait();

        if (response.Responses.TryGetValue(table.Name, out var results))
            results.Each(x => to.Add(Converters.FromAttributeValues<T>(table, x)));

        var i = 0;
        while (response.UnprocessedKeys.Count > 0)
        {
            response = await ExecAsync(async () => 
                await DynamoDb.BatchGetItemAsync(new BatchGetItemRequest(response.UnprocessedKeys), token).ConfigAwait()).ConfigAwait();
            if (response.Responses.TryGetValue(table.Name, out results))
                results.Each(x => to.Add(Converters.FromAttributeValues<T>(table, x)));

            if (response.UnprocessedKeys.Count > 0)
                await i.SleepBackOffMultiplierAsync(token).ConfigAwait();
        }

        return to;
    }

    private void ExecBatchWriteItemResponse<T>(DynamoMetadataType table, List<WriteRequest> deleteItems)
    {
        var request = new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>>
        {
            {table.Name, deleteItems}
        });

        var response = Exec(() => DynamoDb.BatchWriteItem(request));

        var i = 0;
        while (response.UnprocessedItems.Count > 0)
        {
            response = Exec(() => DynamoDb.BatchWriteItem(new BatchWriteItemRequest(response.UnprocessedItems)));

            if (response.UnprocessedItems.Count > 0)
                i.SleepBackOffMultiplier();
        }
    }

    private async Task ExecBatchWriteItemResponseAsync<T>(DynamoMetadataType table, List<WriteRequest> deleteItems, CancellationToken token=default)
    {
        var request = new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>>
        {
            {table.Name, deleteItems}
        });

        var response = await ExecAsync(async () => 
            await DynamoDb.BatchWriteItemAsync(request, token).ConfigAwait()).ConfigAwait();

        var i = 0;
        while (response.UnprocessedItems.Count > 0)
        {
            response = await ExecAsync(async () => 
                await DynamoDb.BatchWriteItemAsync(new BatchWriteItemRequest(response.UnprocessedItems), token).ConfigAwait()).ConfigAwait();

            if (response.UnprocessedItems.Count > 0)
                await i.SleepBackOffMultiplierAsync(token).ConfigAwait();
        }
    }
        
    private void PopulateMissingHashes<T>(DynamoMetadataType table, List<T> items)
    {
        var autoIncr = table.Fields.FirstOrDefault(x => x.IsAutoIncrement);
        if (autoIncr != null)
        {
            var seqRequiredPos = new List<int>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var value = autoIncr.GetValue(item);
                if (DynamoConverters.IsNumberDefault(value))
                    seqRequiredPos.Add(i);
            }
            if (seqRequiredPos.Count == 0)
                return;

            var nextSequences = Sequences.GetNextSequences(table, seqRequiredPos.Count);
            for (int i = 0; i < nextSequences.Length; i++)
            {
                var pos = seqRequiredPos[i];
                autoIncr.SetValue(items[pos], nextSequences[i]);
            }
        }
    }
        
    private async Task PopulateMissingHashesAsync<T>(DynamoMetadataType table, List<T> items, CancellationToken token=default)
    {
        var autoIncr = table.Fields.FirstOrDefault(x => x.IsAutoIncrement);
        if (autoIncr != null)
        {
            var seqRequiredPos = new List<int>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var value = autoIncr.GetValue(item);
                if (DynamoConverters.IsNumberDefault(value))
                    seqRequiredPos.Add(i);
            }
            if (seqRequiredPos.Count == 0)
                return;

            var nextSequences = await SequencesAsync.GetNextSequencesAsync(table, seqRequiredPos.Count);
            for (int i = 0; i < nextSequences.Length; i++)
            {
                var pos = seqRequiredPos[i];
                autoIncr.SetValue(items[pos], nextSequences[i]);
            }
        }
    }
}