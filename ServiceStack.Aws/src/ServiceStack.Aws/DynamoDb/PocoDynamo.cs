// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDb
{
    public partial class PocoDynamo : IPocoDynamo
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PocoDynamo));

        public IAmazonDynamoDB DynamoDb { get; private set; }

        public ISequenceSource Sequences { get; set; }
        public ISequenceSourceAsync SequencesAsync { get; set; }

        public DynamoConverters Converters { get; set; }

        public bool ConsistentRead { get; set; }

        public bool ScanIndexForward { get; set; }

        /// <summary>
        /// If the client needs to delete/re-create the DynamoDB table, this is the Read Capacity to use
        /// </summary>
        public long ReadCapacityUnits { get; set; }

        /// <summary>
        /// If the client needs to delete/re-create the DynamoDB table, this is the Write Capacity to use
        /// </summary> 
        public long WriteCapacityUnits { get; set; }

        public int PagingLimit { get; set; }

        public HashSet<string> RetryOnErrorCodes { get; set; }

        public TimeSpan PollTableStatus { get; set; }

        public TimeSpan MaxRetryOnExceptionTimeout { get; set; }
        
        public Action<CreateTableRequest> CreateTableFilter { get; set; }

        public PocoDynamo(IAmazonDynamoDB dynamoDb)
        {
            this.DynamoDb = dynamoDb ?? throw new ArgumentNullException(nameof(dynamoDb));

            this.Sequences = new DynamoDbSequenceSource(this);
            this.SequencesAsync = (ISequenceSourceAsync)this.Sequences;
            this.Converters = DynamoMetadata.Converters;
            PollTableStatus = TimeSpan.FromSeconds(2);
            MaxRetryOnExceptionTimeout = TimeSpan.FromSeconds(60);
            ReadCapacityUnits = 10;
            WriteCapacityUnits = 5;
            ConsistentRead = true;
            ScanIndexForward = true;
            PagingLimit = 1000;
            RetryOnErrorCodes = new HashSet<string> {
                "ThrottlingException",
                "ProvisionedThroughputExceededException",
                "LimitExceededException",
                "ResourceInUseException",
            };

            JsConfig.InitStatics();
        }

        public void InitSchema()
        {
            CreateMissingTables(DynamoMetadata.GetTables());
        }

        public IPocoDynamo ClientWith(
            bool? consistentRead = null,
            long? readCapacityUnits = null,
            long? writeCapacityUnits = null,
            TimeSpan? pollTableStatus = null,
            TimeSpan? maxRetryOnExceptionTimeout = null,
            int? limit = null,
            bool? scanIndexForward = null)
        {
            return new PocoDynamo(DynamoDb)
            {
                ConsistentRead = consistentRead ?? ConsistentRead,
                ReadCapacityUnits = readCapacityUnits ?? ReadCapacityUnits,
                WriteCapacityUnits = writeCapacityUnits ?? WriteCapacityUnits,
                PollTableStatus = pollTableStatus ?? PollTableStatus,
                MaxRetryOnExceptionTimeout = maxRetryOnExceptionTimeout ?? MaxRetryOnExceptionTimeout,
                PagingLimit = limit ?? PagingLimit,
                ScanIndexForward = scanIndexForward ?? ScanIndexForward,
                Converters = Converters,
                Sequences = Sequences,
                RetryOnErrorCodes = new HashSet<string>(RetryOnErrorCodes),
            };
        }

        public DynamoMetadataType GetTableMetadata(Type table)
        {
            return DynamoMetadata.TryGetTable(table) ?? DynamoMetadata.RegisterTable(table);
        }

        public IEnumerable<string> GetTableNames()
        {
            ListTablesResponse response = null;
            do
            {
                response = response == null
                    ? Exec(() => DynamoDb.ListTables(new ListTablesRequest()))
                    : Exec(() => DynamoDb.ListTables(new ListTablesRequest(response.LastEvaluatedTableName)));

                foreach (var tableName in response.TableNames)
                {
                    yield return tableName;
                }
            } while (response.LastEvaluatedTableName != null);
        }

        readonly Type[] throwNotFoundExceptions = {
            typeof(ResourceNotFoundException)
        };

        public Table GetTableSchema(Type type)
        {
            var table = DynamoMetadata.GetTable(type);
            return Exec(() =>
            {
                try
                {
                    Table.TryLoadTable(DynamoDb, table.Name, out var awsTable);
                    return awsTable;
                }
                catch (ResourceNotFoundException)
                {
                    return null;
                }
            }, throwNotFoundExceptions);
        }

        public bool CreateMissingTables(IEnumerable<DynamoMetadataType> tables, TimeSpan? timeout = null)
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

                CreateTable(table);
            }

            return WaitForTablesToBeReady(tablesList.Map(x => x.Name), timeout);
        }

        public bool CreateTables(IEnumerable<DynamoMetadataType> tables, TimeSpan? timeout = null)
        {
            var tablesList = tables.Safe().ToList();
            if (tablesList.Count == 0)
                return true;

            foreach (var table in tablesList)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("Creating Table: " + table.Name);

                CreateTable(table);
            }

            return WaitForTablesToBeReady(tablesList.Map(x => x.Name), timeout);
        }

        private void CreateTable(DynamoMetadataType table)
        {
            var request = ToCreateTableRequest(table);
            CreateTableFilter?.Invoke(request);
            
            Exec(() =>
            {
                try
                {
                    DynamoDb.CreateTable(request);
                }
                catch (AmazonDynamoDBException ex)
                {
                    if (ex.ErrorCode == DynamoErrors.AlreadyExists 
                        || ex.Message == "Cannot create preexisting table")
                        return;

                    throw;
                }
            });
        }

        protected virtual CreateTableRequest ToCreateTableRequest(DynamoMetadataType table)
        {
            var props = table.Type.GetSerializableProperties();
            if (props.Length == 0)
                throw new NotSupportedException($"{table.Name} does not have any serializable properties");

            var keySchema = new List<KeySchemaElement> {
                new(table.HashKey.Name, KeyType.HASH),
            };
            var attrDefinitions = new List<AttributeDefinition> {
                new(table.HashKey.Name, table.HashKey.DbType),
            };
            if (table.RangeKey != null)
            {
                keySchema.Add(new KeySchemaElement(table.RangeKey.Name, KeyType.RANGE));
                attrDefinitions.Add(new AttributeDefinition(table.RangeKey.Name, table.RangeKey.DbType));
            }

            var to = new CreateTableRequest
            {
                TableName = table.Name,
                KeySchema = keySchema,
                AttributeDefinitions = attrDefinitions,
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = table.ReadCapacityUnits ?? ReadCapacityUnits,
                    WriteCapacityUnits = table.WriteCapacityUnits ?? WriteCapacityUnits,
                }
            };

            if (!table.LocalIndexes.IsEmpty())
            {
                to.LocalSecondaryIndexes = table.LocalIndexes.Map(x => new LocalSecondaryIndex
                {
                    IndexName = x.Name,
                    KeySchema = x.ToKeySchemas(),
                    Projection = new Projection
                    {
                        ProjectionType = x.ProjectionType,
                        NonKeyAttributes = x.ProjectedFields.Safe().ToList(),
                    },
                });

                table.LocalIndexes.Each(x =>
                {
                    if (x.RangeKey != null && attrDefinitions.All(a => a.AttributeName != x.RangeKey.Name))
                        attrDefinitions.Add(new AttributeDefinition(x.RangeKey.Name, x.RangeKey.DbType));
                });
            }
            if (!table.GlobalIndexes.IsEmpty())
            {
                to.GlobalSecondaryIndexes = table.GlobalIndexes.Map(x => new GlobalSecondaryIndex
                {
                    IndexName = x.Name,
                    KeySchema = x.ToKeySchemas(),
                    Projection = new Projection
                    {
                        ProjectionType = x.ProjectionType,
                        NonKeyAttributes = x.ProjectedFields.Safe().ToList(),
                    },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = x.ReadCapacityUnits ?? ReadCapacityUnits,
                        WriteCapacityUnits = x.WriteCapacityUnits ?? WriteCapacityUnits,
                    }
                });

                table.GlobalIndexes.Each(x =>
                {
                    if (x.HashKey != null && attrDefinitions.All(a => a.AttributeName != x.HashKey.Name))
                        attrDefinitions.Add(new AttributeDefinition(x.HashKey.Name, x.HashKey.DbType));
                    if (x.RangeKey != null && attrDefinitions.All(a => a.AttributeName != x.RangeKey.Name))
                        attrDefinitions.Add(new AttributeDefinition(x.RangeKey.Name, x.RangeKey.DbType));
                });
            }
            return to;
        }

        public bool DeleteAllTables(TimeSpan? timeout = null)
        {
            return DeleteTables(GetTableNames().ToList(), timeout);
        }

        public bool DeleteTables(IEnumerable<string> tableNames, TimeSpan? timeout = null)
        {
            foreach (var tableName in tableNames)
            {
                try
                {
                    Exec(() => DynamoDb.DeleteTable(new DeleteTableRequest(tableName)));
                }
                catch (AmazonDynamoDBException ex)
                {
                    if (ex.ErrorCode != DynamoErrors.NotFound)
                        throw;
                }
            }

            return WaitForTablesToBeDeleted(tableNames);
        }

        public T GetItem<T>(object hash)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new GetItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(this, table.HashKey, hash),
                ConsistentRead = ConsistentRead,
            };

            return ConvertGetItemResponse<T>(request, table);
        }

        public T GetItem<T>(DynamoId id)
        {
            return id.Range != null
                ? GetItem<T>(id.Hash, id.Range)
                : GetItem<T>(id.Hash);
        }

        public T GetItem<T>(object hash, object range)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new GetItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(this, table, hash, range),
                ConsistentRead = ConsistentRead,
            };

            return ConvertGetItemResponse<T>(request, table);
        }

        const int MaxReadBatchSize = 100;

        public List<T> GetItems<T>(IEnumerable<object> hashes)
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

                to.AddRange(ConvertBatchGetItemResponse<T>(table, getItems));
            }

            return to;
        }

        public List<T> GetItems<T>(IEnumerable<DynamoId> ids)
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

                to.AddRange(ConvertBatchGetItemResponse<T>(table, getItems));
            }

            return to;
        }

        public IEnumerable<T> GetRelatedItems<T>(object hash)
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

            return Query(request, r => r.ConvertAll<T>());
        }

        public void DeleteRelatedItems<T>(object hash, IEnumerable<object> ranges)
        {
            var table = DynamoMetadata.GetTable<T>();

            if (table.HashKey == null || table.RangeKey == null)
                throw new ArgumentException($"Related table '{typeof(T).Name}' needs both a HashKey and RangeKey");

            DeleteItems<T>(ranges.Map(range => new DynamoId(hash, range)));
        }

        public T PutItem<T>(T value, bool returnOld = false)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new PutItemRequest
            {
                TableName = table.Name,
                Item = Converters.ToAttributeValues(this, value, table),
                ReturnValues = returnOld ? ReturnValue.ALL_OLD : ReturnValue.NONE,
            };

            var response = Exec(() => DynamoDb.PutItem(request));

            if (response.Attributes.IsEmpty())
                return default;

            return Converters.FromAttributeValues<T>(table, response.Attributes);
        }

        public UpdateExpression<T> UpdateExpression<T>(object hash, object range=null)
        {
            return new UpdateExpression<T>(this, DynamoMetadata.GetTable<T>(), hash, range);
        }

        public bool UpdateItem<T>(UpdateExpression<T> update)
        {
            try
            {
                Exec(() => DynamoDb.UpdateItem(update));
                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        private UpdateItemRequest ToUpdateItemRequest<T>(DynamoUpdateItem update)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new UpdateItemRequest {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(this, table, update.Hash, update.Range),
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate>(),
                ReturnValues = ReturnValue.NONE,
            };

            if (update.Put != null)
            {
                foreach (var entry in update.Put)
                {
                    var field = table.GetField(entry.Key);
                    if (field == null)
                        continue;

                    request.AttributeUpdates[field.Name] = new AttributeValueUpdate(
                        Converters.ToAttributeValue(this, field.Type, field.DbType, entry.Value), DynamoAttributeAction.Put);
                }
            }

            if (update.Add != null)
            {
                foreach (var entry in update.Add)
                {
                    var field = table.GetField(entry.Key);
                    if (field == null)
                        continue;

                    request.AttributeUpdates[field.Name] = new AttributeValueUpdate(
                        Converters.ToAttributeValue(this, field.Type, field.DbType, entry.Value), DynamoAttributeAction.Add);
                }
            }

            if (update.Delete != null)
            {
                foreach (var key in update.Delete)
                {
                    var field = table.GetField(key);
                    if (field == null)
                        continue;

                    request.AttributeUpdates[field.Name] = new AttributeValueUpdate(null, DynamoAttributeAction.Delete);
                }
            }

            return request;
        }

        public void UpdateItem<T>(DynamoUpdateItem update)
        {
            var request = ToUpdateItemRequest<T>(update);
            Exec(() => DynamoDb.UpdateItem(request));
        }

        public T UpdateItemNonDefaults<T>(T value, bool returnOld = false)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new UpdateItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKey(this, table, value),
                AttributeUpdates = Converters.ToNonDefaultAttributeValueUpdates(this, value, table),
                ReturnValues = returnOld ? ReturnValue.ALL_OLD : ReturnValue.NONE,
            };

            var response = Exec(() => DynamoDb.UpdateItem(request));

            if (response.Attributes.IsEmpty())
                return default(T);

            return Converters.FromAttributeValues<T>(table, response.Attributes);
        }

        public void PutRelatedItem<T>(object hash, T item)
        {
            var table = DynamoMetadata.GetTable<T>();

            if (table.HashKey == null || table.RangeKey == null)
                throw new ArgumentException($"Related table '{typeof(T).Name}' needs both a HashKey and RangeKey");

            table.HashKey.SetValue(item, hash);
            PutItem(item);
        }

        public void PutRelatedItems<T>(object hash, IEnumerable<T> items)
        {
            var table = DynamoMetadata.GetTable<T>();

            if (table.HashKey == null || table.RangeKey == null)
                throw new ArgumentException($"Related table '{typeof(T).Name}' needs both a HashKey and RangeKey");

            var related = items.ToList();
            related.Each(x => table.HashKey.SetValue(x, hash));
            PutItems(related);
        }

        const int MaxWriteBatchSize = 25;

        public void PutItems<T>(IEnumerable<T> items)
        {
            var table = DynamoMetadata.GetTable<T>();
            var remaining = items.ToList();

            PopulateMissingHashes(table, remaining);

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

                var response = Exec(() => DynamoDb.BatchWriteItem(request));

                var i = 0;
                while (response.UnprocessedItems.Count > 0)
                {
                    response = Exec(() => DynamoDb.BatchWriteItem(new BatchWriteItemRequest(response.UnprocessedItems)));

                    if (response.UnprocessedItems.Count > 0)
                        i.SleepBackOffMultiplier();
                }
            }
        }

        public T DeleteItem<T>(object hash, ReturnItem returnItem = ReturnItem.None)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new DeleteItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(this, table.HashKey, hash),
                ReturnValues = returnItem.ToReturnValue(),
            };

            var response = Exec(() => DynamoDb.DeleteItem(request));

            if (response.Attributes.IsEmpty())
                return default(T);

            return Converters.FromAttributeValues<T>(table, response.Attributes);
        }

        public T DeleteItem<T>(DynamoId id, ReturnItem returnItem = ReturnItem.None)
        {
            return id.Range != null
                ? DeleteItem<T>(id.Hash, id.Range, returnItem)
                : DeleteItem<T>(id.Hash, returnItem);
        }

        public T DeleteItem<T>(object hash, object range, ReturnItem returnItem = ReturnItem.None)
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new DeleteItemRequest
            {
                TableName = table.Name,
                Key = Converters.ToAttributeKeyValue(this, table, hash, range),
                ReturnValues = returnItem.ToReturnValue(),
            };

            var response = Exec(() => DynamoDb.DeleteItem(request));

            if (response.Attributes.IsEmpty())
                return default(T);

            return Converters.FromAttributeValues<T>(table, response.Attributes);
        }

        public void DeleteItems<T>(IEnumerable<object> hashes)
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

                ExecBatchWriteItemResponse<T>(table, deleteItems);
            }
        }

        public void DeleteItems<T>(IEnumerable<DynamoId> ids)
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

                ExecBatchWriteItemResponse<T>(table, deleteItems);
            }
        }

        public long Increment<T>(object hash, string fieldName, long amount = 1)
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

            var response = DynamoDb.UpdateItem(request);

            return response.Attributes.Count > 0
                ? Convert.ToInt64(response.Attributes[fieldName].N)
                : 0;
        }

        public IEnumerable<T> ScanAll<T>()
        {
            var type = DynamoMetadata.GetType<T>();
            var request = new ScanRequest
            {
                Limit = PagingLimit,
                TableName = type.Name,
            };

            return Scan(request, r => r.ConvertAll<T>());
        }

        public IEnumerable<T> Scan<T>(ScanRequest request, Func<ScanResponse, IEnumerable<T>> converter)
        {
            ScanResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = Exec(() => DynamoDb.Scan(request));

                var results = converter(response);

                foreach (var result in results)
                {
                    yield return result;
                }

            } while (!response.LastEvaluatedKey.IsEmpty());
        }

        public ScanExpression<T> FromScan<T>(Expression<Func<T, bool>> filterExpression = null)
        {
            var q = new ScanExpression<T>(this)
            {
                Limit = PagingLimit,
                ConsistentRead = !typeof(T).IsGlobalIndex() && this.ConsistentRead,
            };

            if (filterExpression != null)
                q.Filter(filterExpression);

            return q;
        }

        public ScanExpression<T> FromScanIndex<T>(Expression<Func<T, bool>> filterExpression = null)
        {
            var table = typeof(T).GetIndexTable();
            var index = table.GetIndex(typeof(T));
            var q = new ScanExpression<T>(this, table)
            {
                IndexName = index.Name,
                Limit = PagingLimit,
                ConsistentRead = !typeof(T).IsGlobalIndex() && this.ConsistentRead,
            };

            if (filterExpression != null)
                q.Filter(filterExpression);

            return q;
        }

        public List<T> Scan<T>(ScanExpression<T> request, int limit)
        {
            var to = new List<T>();

            if (request.Limit == default(int))
                request.Limit = limit;

            ScanResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = Exec(() => DynamoDb.Scan(request));
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

        public IEnumerable<T> Scan<T>(ScanExpression<T> request)
        {
            return Scan(request, r => r.ConvertAll<T>());
        }

        public List<T> Scan<T>(ScanRequest request, int limit)
        {
            var to = new List<T>();

            if (request.Limit == default(int))
                request.Limit = limit;

            ScanResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = Exec(() => DynamoDb.Scan(request));
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

        public IEnumerable<T> Scan<T>(ScanRequest request)
        {
            return Scan(request, r => r.ConvertAll<T>());
        }

        public long ScanItemCount<T>()
        {
            var table = DynamoMetadata.GetTable<T>();
            var request = new ScanRequest(table.Name);
            var response = Scan(request, r => new[] { r.Count });
            return response.Sum();
        }

        public long DescribeItemCount<T>()
        {
            var table = DynamoMetadata.GetTable<T>();
            var response = Exec(() => DynamoDb.DescribeTable(new DescribeTableRequest(table.Name)));
            return response.Table.ItemCount;
        }

        public QueryExpression<T> FromQuery<T>(Expression<Func<T, bool>> keyExpression = null)
        {
            var q = new QueryExpression<T>(this)
            {
                Limit = PagingLimit,
                ConsistentRead = !typeof(T).IsGlobalIndex() && this.ConsistentRead,
                ScanIndexForward = this.ScanIndexForward,
            };

            if (keyExpression != null)
                q.KeyCondition(keyExpression);

            return q;
        }

        public QueryExpression<T> FromQueryIndex<T>(Expression<Func<T, bool>> keyExpression = null)
        {
            var table = typeof(T).GetIndexTable();
            var index = table.GetIndex(typeof(T));
            if (index == null)
                throw new ArgumentException("Index is not referenced on table Type " + typeof(T).Name);

            var q = new QueryExpression<T>(this, table)
            {
                IndexName = index.Name,
                Limit = PagingLimit,
                ConsistentRead = !typeof(T).IsGlobalIndex() && this.ConsistentRead,
                ScanIndexForward = this.ScanIndexForward,
            };

            if (keyExpression != null)
                q.KeyCondition(keyExpression);

            return q;
        }

        public IEnumerable<T> Query<T>(QueryExpression<T> request)
        {
            return Query(request, r => r.ConvertAll<T>());
        }

        public List<T> Query<T>(QueryExpression<T> request, int limit)
        {
            return Query<T>((QueryRequest)request, limit);
        }

        public IEnumerable<T> Query<T>(QueryRequest request)
        {
            return Query(request, r => r.ConvertAll<T>());
        }

        public List<T> Query<T>(QueryRequest request, int limit)
        {
            var to = new List<T>();

            if (request.Limit == default(int))
                request.Limit = limit;

            QueryResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = Exec(() => DynamoDb.Query(request));
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

        public IEnumerable<T> Query<T>(QueryRequest request, Func<QueryResponse, IEnumerable<T>> converter)
        {
            QueryResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = Exec(() => DynamoDb.Query(request));
                var results = converter(response);

                foreach (var result in results)
                {
                    yield return result;
                }

            } while (!response.LastEvaluatedKey.IsEmpty());
        }

        public void Close()
        {
            if (DynamoDb == null)
                return;

            DynamoDb.Dispose();
            DynamoDb = null;
        }
    }
}