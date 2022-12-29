// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDb
{
    public partial class PocoDynamo
    {
        public Task InitSchemaAsync(CancellationToken token = default) => 
            CreateMissingTablesAsync(DynamoMetadata.GetTables(), token);
        
        public async Task<List<string>> GetTableNamesAsync(CancellationToken token = default)
        {
            ListTablesResponse response = null;
            var to = new List<string>();
            do
            {
                response = response == null
                    ? await ExecAsync(async () => await DynamoDb.ListTablesAsync(new ListTablesRequest(), token)).ConfigAwait()
                    : await ExecAsync(async () => await DynamoDb.ListTablesAsync(new ListTablesRequest(response.LastEvaluatedTableName), token)).ConfigAwait();

                foreach (var tableName in response.TableNames)
                {
                    to.Add(tableName);
                }
            } while (response.LastEvaluatedTableName != null);
            return to;
        }

        public async Task<bool> CreateMissingTablesAsync(IEnumerable<DynamoMetadataType> tables, CancellationToken token = default)
        {
            var tablesList = tables.Safe().ToList();
            if (tablesList.Count == 0)
                return true;

            var existingTableNames = GetTableNames().ToList();

            foreach (var table in tablesList)
            {
                if (existingTableNames.Contains(table.Name))
                    continue;

                if (Log.IsDebugEnabled)
                    Log.Debug("Creating Table: " + table.Name);

                await CreateTableAsync(table, token).ConfigAwait();
            }

            return await WaitForTablesToBeReadyAsync(tablesList.Map(x => x.Name), token).ConfigAwait();
        }

        public async Task<bool> CreateTablesAsync(IEnumerable<DynamoMetadataType> tables, TimeSpan? timeout = null, CancellationToken token = default)
        {
            var tablesList = tables.Safe().ToList();
            if (tablesList.Count == 0)
                return true;

            foreach (var table in tablesList)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("Creating Table: " + table.Name);

                await CreateTableAsync(table, token).ConfigAwait();
            }

            return WaitForTablesToBeReady(tablesList.Map(x => x.Name), timeout);
        }

        private async Task CreateTableAsync(DynamoMetadataType table, CancellationToken token = default)
        {
            var request = ToCreateTableRequest(table);
            CreateTableFilter?.Invoke(request);
            try
            {
                await ExecAsync(async () => 
                    await DynamoDb.CreateTableAsync(request, token).ConfigAwait()).ConfigAwait();
            }
            catch (AmazonDynamoDBException ex)
            {
                if (ex.ErrorCode == DynamoErrors.AlreadyExists)
                    return;

                throw;
            }
        }

        public async Task<bool> DeleteAllTablesAsync(TimeSpan? timeout = null, CancellationToken token = default)
        {
            return await DeleteTablesAsync(
                (await GetTableNamesAsync(token).ConfigAwait()).ToList(), timeout, token).ConfigAwait();
        }

        public async Task<bool> DeleteTablesAsync(IEnumerable<string> tableNames, TimeSpan? timeout = null, CancellationToken token = default)
        {
            foreach (var tableName in tableNames)
            {
                try
                {
                    await ExecAsync(async () => 
                        await DynamoDb.DeleteTableAsync(new DeleteTableRequest(tableName), token).ConfigAwait()).ConfigAwait();
                }
                catch (AmazonDynamoDBException ex)
                {
                    if (ex.ErrorCode != DynamoErrors.NotFound)
                        throw;
                }
            }

            return await WaitForTablesToBeDeletedAsync(tableNames, null, token).ConfigAwait();
        }

        public async Task<T> GetItemAsync<T>(object hash, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new GetItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(this, table.HashKey, hash),
                ConsistentRead = ConsistentRead,
            };

            return await ConvertGetItemResponseAsync<T>(request, table, token).ConfigAwait();
        }

        public async Task<T> GetItemAsync<T>(object hash, object range, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new GetItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(this, table, hash, range),
                ConsistentRead = ConsistentRead,
            };

            return await ConvertGetItemResponseAsync<T>(request, table, token).ConfigAwait();
        }

        public async Task<T> GetItemAsync<T>(DynamoId id, CancellationToken token = default)
        {
            return id.Range != null
                ? await GetItemAsync<T>(id.Hash, id.Range, token).ConfigAwait()
                : await GetItemAsync<T>(id.Hash, token).ConfigAwait();
        }

        public async Task<List<T>> GetItemsAsync<T>(IEnumerable<object> hashes, CancellationToken token = default)
        {
            var to = new List<T>();

            var table = DynamoMetadata.GetTable<T>();
            var remainingIds = hashes.ToList();

            while (remainingIds.Count > 0)
            {
                var batchSize = Math.Min(remainingIds.Count, MaxReadBatchSize);
                var nextBatch = remainingIds.GetRange(0, batchSize);
                remainingIds.RemoveRange(0, batchSize);

                var getItems = new KeysAndAttributes
                {
                    ConsistentRead = ConsistentRead,
                };
                nextBatch.Each(id =>
                    getItems.Keys.Add(Converters.ToAttributeKeyValue(this, table.HashKey, id)));

                to.AddRange(await ConvertBatchGetItemResponseAsync<T>(table, getItems, token).ConfigAwait());
            }

            return to;
        }

        public async Task<List<T>> GetItemsAsync<T>(IEnumerable<DynamoId> ids, CancellationToken token = default)
        {
            var to = new List<T>();

            var table = DynamoMetadata.GetTable<T>();
            var remainingIds = ids.ToList();

            while (remainingIds.Count > 0)
            {
                var batchSize = Math.Min(remainingIds.Count, MaxReadBatchSize);
                var nextBatch = remainingIds.GetRange(0, batchSize);
                remainingIds.RemoveRange(0, batchSize);

                var getItems = new KeysAndAttributes
                {
                    ConsistentRead = ConsistentRead,
                };
                nextBatch.Each(id =>
                    getItems.Keys.Add(Converters.ToAttributeKeyValue(this, table, id)));

                to.AddRange(await ConvertBatchGetItemResponseAsync<T>(table, getItems, token).ConfigAwait());
            }

            return to;
        }

        public async Task<List<T>> GetRelatedItemsAsync<T>(object hash, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();

            var argType = hash.GetType();
            var dbType = Converters.GetFieldType(argType);
            var request = new QueryRequest(table.Name)
            {
                Limit = PagingLimit,
                KeyConditionExpression = $"{table.HashKey.Name} = :k1",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":k1", Converters.ToAttributeValue(this, argType, dbType, hash) }
                }
            };

#if NET472 || NETCORE
            return await QueryAsync(request, r => r.ConvertAll<T>(), token).ToListAsync(token);
#else
            return await QueryAsync(request, r => r.ConvertAll<T>(), token).ConfigAwait();
#endif
        }

        public async Task DeleteRelatedItemsAsync<T>(object hash, IEnumerable<object> ranges, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();

            if (table.HashKey == null || table.RangeKey == null)
                throw new ArgumentException($"Related table '{typeof(T).Name}' needs both a HashKey and RangeKey");

            await DeleteItemsAsync<T>(ranges.Map(range => new DynamoId(hash, range)), token).ConfigAwait();
        }

        public async Task<T> PutItemAsync<T>(T value, bool returnOld = false, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new PutItemRequest
            {
                TableName = table.Name,
                Item = Converters.ToAttributeValues(this, value, table),
                ReturnValues = returnOld ? ReturnValue.ALL_OLD : ReturnValue.NONE,
            };

            var response = await ExecAsync(async () => 
                await DynamoDb.PutItemAsync(request, token).ConfigAwait()).ConfigAwait();

            if (response.Attributes.IsEmpty())
                return default(T);

            return Converters.FromAttributeValues<T>(table, response.Attributes);
        }
        
        public async Task<bool> UpdateItemAsync<T>(UpdateExpression<T> update, CancellationToken token = default)
        {
            try
            {
                await ExecAsync(async () => 
                    await DynamoDb.UpdateItemAsync(update, token).ConfigAwait()).ConfigAwait();
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        public async Task UpdateItemAsync<T>(DynamoUpdateItem update, CancellationToken token = default)
        {
            var request = ToUpdateItemRequest<T>(update);
            await ExecAsync(async () => 
                await DynamoDb.UpdateItemAsync(request, token).ConfigAwait()).ConfigAwait();
        }

        public async Task<T> UpdateItemNonDefaultsAsync<T>(T value, bool returnOld = false, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new UpdateItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKey(this, table, value),
                AttributeUpdates = Converters.ToNonDefaultAttributeValueUpdates(this, value, table),
                ReturnValues = returnOld ? ReturnValue.ALL_OLD : ReturnValue.NONE,
            };

            var response = await ExecAsync(async () => 
                await DynamoDb.UpdateItemAsync(request, token).ConfigAwait()).ConfigAwait();

            if (response.Attributes.IsEmpty())
                return default;

            return Converters.FromAttributeValues<T>(table, response.Attributes);
        }

        public async Task PutRelatedItemAsync<T>(object hash, T item, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();

            if (table.HashKey == null || table.RangeKey == null)
                throw new ArgumentException($"Related table '{typeof(T).Name}' needs both a HashKey and RangeKey");

            table.HashKey.SetValue(item, hash);
            await PutItemAsync(item, token: token).ConfigAwait();
        }

        public async Task PutRelatedItemsAsync<T>(object hash, IEnumerable<T> items, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();

            if (table.HashKey == null || table.RangeKey == null)
                throw new ArgumentException($"Related table '{typeof(T).Name}' needs both a HashKey and RangeKey");

            var related = items.ToList();
            related.Each(x => table.HashKey.SetValue(x, hash));
            await PutItemsAsync(related, token).ConfigAwait();
        }

        public async Task PutItemsAsync<T>(IEnumerable<T> items, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var remaining = items.ToList();

            await PopulateMissingHashesAsync(table, remaining, token).ConfigAwait();

            while (remaining.Count > 0)
            {
                var batchSize = Math.Min(remaining.Count, MaxWriteBatchSize);
                var nextBatch = remaining.GetRange(0, batchSize);
                remaining.RemoveRange(0, batchSize);

                var putItems = nextBatch.Map(x => new WriteRequest(
                    new PutRequest(Converters.ToAttributeValues(this, x, table))));

                var request = new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>> {
                    { table.Name, putItems }
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
        }
        public async Task<T> DeleteItemAsync<T>(object hash, ReturnItem returnItem = ReturnItem.None, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new DeleteItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(this, table.HashKey, hash),
                ReturnValues = returnItem.ToReturnValue(),
            };

            var response = await ExecAsync(async () => 
                await DynamoDb.DeleteItemAsync(request, token).ConfigAwait()).ConfigAwait();

            if (response.Attributes.IsEmpty())
                return default(T);

            return Converters.FromAttributeValues<T>(table, response.Attributes);
        }

        public async Task<T> DeleteItemAsync<T>(DynamoId id, ReturnItem returnItem = ReturnItem.None, CancellationToken token = default)
        {
            return id.Range != null
                ? await DeleteItemAsync<T>(id.Hash, id.Range, returnItem, token).ConfigAwait()
                : await DeleteItemAsync<T>(id.Hash, returnItem, token).ConfigAwait();
        }

        public async Task<T> DeleteItemAsync<T>(object hash, object range, ReturnItem returnItem = ReturnItem.None, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new DeleteItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(this, table, hash, range),
                ReturnValues = returnItem.ToReturnValue(),
            };

            var response = await ExecAsync(async () => 
                await DynamoDb.DeleteItemAsync(request, token).ConfigAwait()).ConfigAwait();

            if (response.Attributes.IsEmpty())
                return default(T);

            return Converters.FromAttributeValues<T>(table, response.Attributes);
        }

        public async Task DeleteItemsAsync<T>(IEnumerable<object> hashes, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var remainingIds = hashes.ToList();

            while (remainingIds.Count > 0)
            {
                var batchSize = Math.Min(remainingIds.Count, MaxWriteBatchSize);
                var nextBatch = remainingIds.GetRange(0, batchSize);
                remainingIds.RemoveRange(0, batchSize);

                var deleteItems = nextBatch.Map(id => new WriteRequest(
                    new DeleteRequest(Converters.ToAttributeKeyValue(this, table.HashKey, id))));

                await ExecBatchWriteItemResponseAsync<T>(table, deleteItems, token).ConfigAwait();
            }
        }

        public async Task DeleteItemsAsync<T>(IEnumerable<DynamoId> ids, CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var remainingIds = ids.ToList();

            while (remainingIds.Count > 0)
            {
                var batchSize = Math.Min(remainingIds.Count, MaxWriteBatchSize);
                var nextBatch = remainingIds.GetRange(0, batchSize);
                remainingIds.RemoveRange(0, batchSize);

                var deleteItems = nextBatch.Map(id => new WriteRequest(
                    new DeleteRequest(Converters.ToAttributeKeyValue(this, table, id))));

                await ExecBatchWriteItemResponseAsync<T>(table, deleteItems, token).ConfigAwait();
            }
        }

        public async Task<long> IncrementAsync<T>(object hash, string fieldName, long amount = 1, CancellationToken token = default)
        {
            var type = DynamoMetadata.GetType<T>();
            var request = new UpdateItemRequest
            {
                TableName = type.Name,
                Key = Converters.ToAttributeKeyValue(this, type.HashKey, hash),
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate> {
                    {
                        fieldName,
                        new AttributeValueUpdate {
                            Action = AttributeAction.ADD,
                            Value = new AttributeValue { N = amount.ToString() }
                        }
                    }
                },
                ReturnValues = ReturnValue.ALL_NEW,
            };

            var response = await DynamoDb.UpdateItemAsync(request, token).ConfigAwait();

            return response.Attributes.Count > 0
                ? Convert.ToInt64(response.Attributes[fieldName].N)
                : 0;
        }

        public async Task<long> DescribeItemCountAsync<T>(CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var response = await ExecAsync(async () => 
                await DynamoDb.DescribeTableAsync(new DescribeTableRequest(table.Name), token).ConfigAwait()).ConfigAwait();
            return response.Table.ItemCount;
        }
        
#if NET472 || NETCORE
        public async Task<long> ScanItemCountAsync<T>(CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new ScanRequest(table.Name);
            var response = await ScanAsync(request, r => new[] { r.Count }, token).ToListAsync(token);
            return response.Sum();
        }

        public IAsyncEnumerable<T> ScanAllAsync<T>(CancellationToken token = default)
        {
            var type = DynamoMetadata.GetType<T>();
            var request = new ScanRequest
            {
                Limit = PagingLimit,
                TableName = type.Name,
            };

            return ScanAsync(request, r => r.ConvertAll<T>(), token);
        }

#pragma warning disable CS8425
        public async IAsyncEnumerable<T> ScanAsync<T>(ScanRequest request, Func<ScanResponse, IEnumerable<T>> converter, CancellationToken token = default)
#pragma warning restore CS8425
        {
            ScanResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = await ExecAsync(async () => 
                    await DynamoDb.ScanAsync(request, token).ConfigAwait()).ConfigAwait();

                var results = converter(response);
                foreach (var result in results)
                {
                    yield return result;
                }

            } while (!response.LastEvaluatedKey.IsEmpty());
        }

        public async IAsyncEnumerable<T> ScanAsync<T>(ScanExpression<T> request, int limit, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (request.Limit == default)
                request.Limit = limit;

            ScanResponse response = null;
            var count = 0;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = await ExecAsync(async () => 
                    await DynamoDb.ScanAsync(request, token).ConfigAwait()).ConfigAwait();
                var results = response.ConvertAll<T>();

                foreach (var result in results)
                {
                    token.ThrowIfCancellationRequested();

                    yield return result;

                    if (++count >= limit)
                        break;
                }

            } while (!response.LastEvaluatedKey.IsEmpty() && count < limit);
        }

        public IAsyncEnumerable<T> ScanAsync<T>(ScanExpression<T> request, CancellationToken token = default)
        {
            return ScanAsync(request, r => r.ConvertAll<T>(), token);
        }

        public async IAsyncEnumerable<T> ScanAsync<T>(ScanRequest request, int limit, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (request.Limit == default(int))
                request.Limit = limit;

            ScanResponse response = null;
            var count = 0;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = await ExecAsync(async () => 
                    await DynamoDb.ScanAsync(request, token).ConfigAwait()).ConfigAwait();
                var results = response.ConvertAll<T>();

                foreach (var result in results)
                {
                    token.ThrowIfCancellationRequested();

                    yield return result;

                    if (++count >= limit)
                        break;
                }

            } while (!response.LastEvaluatedKey.IsEmpty() && count < limit);
        }

        public IAsyncEnumerable<T> ScanAsync<T>(ScanRequest request, CancellationToken token = default)
        {
            return ScanAsync(request, r => r.ConvertAll<T>(), token);
        }

        public IAsyncEnumerable<T> QueryAsync<T>(QueryExpression<T> request, CancellationToken token=default)
        {
            return QueryAsync(request, r => r.ConvertAll<T>(), token);
        }

        public IAsyncEnumerable<T> QueryAsync<T>(QueryExpression<T> request, int limit, CancellationToken token=default)
        {
            return QueryAsync<T>((QueryRequest)request, limit, token);
        }

        public IAsyncEnumerable<T> QueryAsync<T>(QueryRequest request, CancellationToken token=default)
        {
            return QueryAsync(request, r => r.ConvertAll<T>(), token);
        }

        public async IAsyncEnumerable<T> QueryAsync<T>(QueryRequest request, int limit, [EnumeratorCancellation] CancellationToken token=default)
        {
            if (request.Limit == default(int))
                request.Limit = limit;

            QueryResponse response = null;
            var count = 0;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = await ExecAsync(async () => 
                    await DynamoDb.QueryAsync(request, token).ConfigAwait()).ConfigAwait();
                var results = response.ConvertAll<T>();

                foreach (var result in results)
                {
                    token.ThrowIfCancellationRequested();

                    yield return result;

                    if (++count >= limit)
                        break;
                }

            } while (!response.LastEvaluatedKey.IsEmpty() && count < limit);
        }

        public async IAsyncEnumerable<T> QueryAsync<T>(QueryRequest request, Func<QueryResponse, IEnumerable<T>> converter, [EnumeratorCancellation] CancellationToken token=default)
        {
            QueryResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = await ExecAsync(async () => 
                    await DynamoDb.QueryAsync(request, token).ConfigAwait()).ConfigAwait();
                var results = converter(response);
                
                foreach (var result in results)
                {
                    token.ThrowIfCancellationRequested();

                    yield return result;
                }

            } while (!response.LastEvaluatedKey.IsEmpty());
        }
#else

        public async Task<long> ScanItemCountAsync<T>(CancellationToken token = default)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new ScanRequest(table.Name);
            var response = await ScanAsync(request, r => new[] { r.Count }, token).ConfigAwait();
            return response.Sum();
        }

        public async Task<List<T>> ScanAllAsync<T>(CancellationToken token = default)
        {
            var type = DynamoMetadata.GetType<T>();
            var request = new ScanRequest
            {
                Limit = PagingLimit,
                TableName = type.Name,
            };

            return await ScanAsync(request, r => r.ConvertAll<T>(), token).ConfigAwait();
        }

        public async Task<List<T>> ScanAsync<T>(ScanRequest request, Func<ScanResponse, IEnumerable<T>> converter, CancellationToken token = default)
        {
            ScanResponse response = null;
            var to = new List<T>();
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = await ExecAsync(async () => 
                    await DynamoDb.ScanAsync(request, token).ConfigAwait()).ConfigAwait();

                var results = converter(response);
                to.AddRange(results);

            } while (!response.LastEvaluatedKey.IsEmpty());
            return to;
        }

        public async Task<List<T>> ScanAsync<T>(ScanExpression<T> request, int limit, CancellationToken token = default)
        {
            var to = new List<T>();

            if (request.Limit == default(int))
                request.Limit = limit;

            ScanResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = await ExecAsync(async () => 
                    await DynamoDb.ScanAsync(request, token).ConfigAwait()).ConfigAwait();
                var results = response.ConvertAll<T>();

                foreach (var result in results)
                {
                    to.Add(result);

                    if (to.Count >= limit)
                        break;
                }

            } while (!response.LastEvaluatedKey.IsEmpty() && to.Count < limit);

            return to;
        }

        public async Task<List<T>> ScanAsync<T>(ScanExpression<T> request, CancellationToken token = default)
        {
            return await ScanAsync(request, r => r.ConvertAll<T>(), token).ConfigAwait();
        }

        public async Task<List<T>> ScanAsync<T>(ScanRequest request, int limit, CancellationToken token = default)
        {
            var to = new List<T>();

            if (request.Limit == default(int))
                request.Limit = limit;

            ScanResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = await ExecAsync(async () => 
                    await DynamoDb.ScanAsync(request, token).ConfigAwait()).ConfigAwait();
                var results = response.ConvertAll<T>();

                foreach (var result in results)
                {
                    to.Add(result);

                    if (to.Count >= limit)
                        break;
                }

            } while (!response.LastEvaluatedKey.IsEmpty() && to.Count < limit);

            return to;
        }

        public async Task<List<T>> ScanAsync<T>(ScanRequest request, CancellationToken token = default)
        {
            return await ScanAsync(request, r => r.ConvertAll<T>(), token).ConfigAwait();
        }

        public async Task<List<T>> QueryAsync<T>(QueryExpression<T> request, CancellationToken token=default)
        {
            return await QueryAsync(request, r => r.ConvertAll<T>(), token).ConfigAwait();
        }

        public async Task<List<T>> QueryAsync<T>(QueryExpression<T> request, int limit, CancellationToken token=default)
        {
            return await QueryAsync<T>((QueryRequest)request, limit, token).ConfigAwait();
        }

        public async Task<List<T>> QueryAsync<T>(QueryRequest request, CancellationToken token=default)
        {
            return await QueryAsync(request, r => r.ConvertAll<T>(), token).ConfigAwait();
        }

        public async Task<List<T>> QueryAsync<T>(QueryRequest request, int limit, CancellationToken token=default)
        {
            var to = new List<T>();

            if (request.Limit == default(int))
                request.Limit = limit;

            QueryResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = await ExecAsync(async () => 
                    await DynamoDb.QueryAsync(request, token).ConfigAwait()).ConfigAwait();
                var results = response.ConvertAll<T>();

                foreach (var result in results)
                {
                    to.Add(result);

                    if (to.Count >= limit)
                        break;
                }

            } while (!response.LastEvaluatedKey.IsEmpty() && to.Count < limit);

            return to;
        }

        public async Task<List<T>> QueryAsync<T>(QueryRequest request, Func<QueryResponse, IEnumerable<T>> converter, CancellationToken token=default)
        {
            QueryResponse response = null;
            var to = new List<T>();
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = await ExecAsync(async () => 
                    await DynamoDb.QueryAsync(request, token).ConfigAwait()).ConfigAwait();
                var results = converter(response);
                
                to.AddRange(results);

            } while (!response.LastEvaluatedKey.IsEmpty());
            return to;
        }
#endif
        
    }
}