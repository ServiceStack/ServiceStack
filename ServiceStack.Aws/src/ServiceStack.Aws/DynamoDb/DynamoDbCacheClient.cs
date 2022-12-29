// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Caching;
using ServiceStack.Logging;

namespace ServiceStack.Aws.DynamoDb
{
    public partial class DynamoDbCacheClient : ICacheClientExtended, IRequiresSchema, IRemoveByPattern
    {
        public const string IdField = "Id";
        public const string DataField = "Data";

        private static readonly ILog Log = LogManager.GetLogger(typeof(DynamoDbCacheClient));

        public IPocoDynamo Dynamo { get; private set; }

        private Table schema;
        private readonly DynamoMetadataType metadata;

        public DynamoDbCacheClient(IPocoDynamo dynamo, bool initSchema = false)
        {
            this.Dynamo = dynamo;
            dynamo.RegisterTable<CacheEntry>();
            metadata = dynamo.GetTableMetadata<CacheEntry>();

            if (initSchema)
                InitSchema();
        }

        public void InitSchema()
        {
            schema = Dynamo.GetTableSchema<CacheEntry>();

            if (schema == null)
            {
                Dynamo.CreateTableIfMissing(metadata);
                schema = Dynamo.GetTableSchema<CacheEntry>();
            }
        }

        private T GetValue<T>(string key)
        {
            var entry = Dynamo.GetItem<CacheEntry>(key);
            if (entry == null)
                return default;

            if (entry.ExpiryDate != null && DateTime.UtcNow > entry.ExpiryDate.Value.ToUniversalTime())
            {
                Remove(key);
                return default;
            }

            return entry.Data.FromJson<T>();
        }

        [Obsolete("Use RemoveExpiredEntries")]
        public void ClearExpiredEntries() => RemoveExpiredEntries();

        public void RemoveExpiredEntries()
        {
            var expiredIds = Dynamo.FromScan<CacheEntry>()
                .Filter(x => x.ExpiryDate < DateTime.UtcNow)
                .ExecColumn(x => x.Id);

            Dynamo.DeleteItems<CacheEntry>(expiredIds);
        }

        private bool CacheAdd<T>(string key, T value, DateTime? expiresAt)
        {
            var entry = GetValue<T>(key);
            if (!Equals(entry, default))
                return false;

            CacheSet(key, value, expiresAt);
            return true;
        }

        private bool CacheReplace<T>(string key, T value, DateTime? expiresAt)
        {
            var entry = GetValue<T>(key);
            if (Equals(entry, default))
                return false;

            CacheSet(key, value, expiresAt);
            return true;
        }

        private bool CacheSet<T>(string key, T value, DateTime? expiresAt)
        {
            var request = ToCacheEntryPutItemRequest(key, value, expiresAt);

            Exception lastEx = null;
            var i = 0;
            var firstAttempt = DateTime.UtcNow;
            while (DateTime.UtcNow - firstAttempt < Dynamo.MaxRetryOnExceptionTimeout)
            {
                i++;
                try
                {
                    ((PocoDynamo)Dynamo).Exec(() => Dynamo.DynamoDb.PutItem(request));
                    return true;
                }
                catch (ResourceNotFoundException ex)
                {
                    lastEx = ex;
                    i.SleepBackOffMultiplier(); //Table could temporarily not exist after a FlushAll()
                }
            }

            throw new TimeoutException($"Exceeded timeout of {Dynamo.MaxRetryOnExceptionTimeout}", lastEx);
        }

        private PutItemRequest ToCacheEntryPutItemRequest<T>(string key, T value, DateTime? expiresAt)
        {
            var now = DateTime.UtcNow;
            string json = AwsClientUtils.ToScopedJson(value);
            var entry = new CacheEntry
            {
                Id = key,
                Data = json,
                CreatedDate = now,
                ModifiedDate = now,
                ExpiryDate = expiresAt,
            };
            var table = DynamoMetadata.GetTable<CacheEntry>();
            var request = new PutItemRequest
            {
                TableName = table.Name,
                Item = Dynamo.Converters.ToAttributeValues(Dynamo, entry, table),
                ReturnValues = ReturnValue.NONE,
            };
            if (typeof(T).IsNumericType())
            {
                request.Item[DataField] = new AttributeValue { N = json }; //Needs to be Number Type to be able to increment value
            }

            return request;
        }

        private int UpdateCounterBy(string key, int amount)
        {
            return (int) Dynamo.Increment<CacheEntry>(key, DataField, amount);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheAdd(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            return CacheAdd(key, value, expiresAt.ToUniversalTime());
        }

        public bool Add<T>(string key, T value)
        {
            return CacheAdd(key, value, null);
        }

        public long Decrement(string key, uint amount)
        {
            return UpdateCounterBy(key, (int)-amount);
        }

        /// <summary>
        /// IMPORTANT: This method will delete and re-create the DynamoDB table in order to reduce read/write capacity costs, make sure the proper table name and throughput properties are set!
        /// TODO: This method may take upwards of a minute to complete, need to look into a faster implementation
        /// </summary>
        public void FlushAll()
        {
            Dynamo.DeleteTable<CacheEntry>();
            Dynamo.CreateTableIfMissing<CacheEntry>();
        }

        public T Get<T>(string key)
        {
            return GetValue<T>(key);
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            var valueMap = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                var value = Get<T>(key);
                valueMap[key] = value;
            }
            return valueMap;
        }

        public long Increment(string key, uint amount)
        {
            return UpdateCounterBy(key, (int)amount);
        }

        public bool Remove(string key)
        {
            var existingItem = Dynamo.DeleteItem<CacheEntry>(key, ReturnItem.Old);
            return existingItem != null;
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                Remove(key);
            }
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheReplace(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            return CacheReplace(key, value, expiresAt.ToUniversalTime());
        }

        public bool Replace<T>(string key, T value)
        {
            return CacheReplace(key, value, null);
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheSet(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            return CacheSet(key, value, expiresAt.ToUniversalTime());
        }

        public bool Set<T>(string key, T value)
        {
            return CacheSet(key, value, null);
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            foreach (var entry in values)
            {
                Set(entry.Key, entry.Value);
            }
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            var entry = Dynamo.GetItem<CacheEntry>(key);
            if (entry == null)
                return null;

            if (entry.ExpiryDate == null)
                return TimeSpan.MaxValue;

            return entry.ExpiryDate - DateTime.UtcNow;
        }

        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            if (pattern == "*")
                return Dynamo.FromScan<CacheEntry>().ExecColumn(x => x.Id);

            if (!pattern.EndsWith("*"))
                throw new NotImplementedException("DynamoDb only supports begins_with* patterns");

            var beginWith = pattern.Substring(0, pattern.Length - 1);
            if (beginWith.Contains("*"))
                throw new NotImplementedException("DynamoDb only supports begins_with* patterns");

            return Dynamo.FromScan<CacheEntry>(x => x.Id.StartsWith(beginWith)).ExecColumn(x => x.Id);
        }

        public void RemoveByPattern(string pattern)
        {
            var idsToRemove = GetKeysByPattern(pattern);
            RemoveAll(idsToRemove);
        }

        public void RemoveByRegex(string regex)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            Dynamo?.Close();
        }

        ~DynamoDbCacheClient()
        {
            Close();
        }

        public void Dispose() { }
    }

    public class CacheEntry
    {
        public string Id { get; set; }
        public string Data { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
