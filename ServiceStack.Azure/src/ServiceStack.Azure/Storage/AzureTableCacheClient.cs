using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.Logging;
using Microsoft.WindowsAzure.Storage;
using ServiceStack.Support;
using ServiceStack.Text;
using Microsoft.WindowsAzure.Storage.Table;
using ServiceStack.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace ServiceStack.Azure.Storage
{
    public partial class AzureTableCacheClient : AdapterBase, ICacheClientExtended, IRemoveByPattern
    {
        TableCacheEntry CreateTableEntry(string rowKey, string data = null,
            DateTime? created = null, DateTime? expires = null)
        {
            var createdDate = created ?? DateTime.UtcNow;
            return new TableCacheEntry(rowKey)
            {
                Data = data,
                ExpiryDate = expires,
                CreatedDate = createdDate,
                ModifiedDate = createdDate,
            };
        }

        protected override ILog Log => LogManager.GetLogger(GetType());
        public bool FlushOnDispose { get; set; }

        string connectionString;
        string partitionKey = "";
        CloudTable table = null;
        IStringSerializer serializer;

        public AzureTableCacheClient(string ConnectionString, string tableName = "Cache")
        {
            connectionString = ConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();

            serializer = new JsonStringSerializer();
        }

        private bool TryGetValue(string key, out TableCacheEntry entry)
        {
            entry = null;

            var op = TableOperation.Retrieve<TableCacheEntry>(partitionKey, key);
            var retrievedResult = table.Execute(op);

            if (retrievedResult.Result != null)
            {
                entry = retrievedResult.Result as TableCacheEntry;
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (!FlushOnDispose) return;

            FlushAll();
        }

        public bool Add<T>(string key, T value)
        {
            var sVal = serializer.SerializeToString<T>(value);

            var entry = CreateTableEntry(key, sVal, null);
            return AddInternal(key, entry);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            return Add<T>(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            var sVal = serializer.SerializeToString<T>(value);

            var entry = CreateTableEntry(key, sVal, null, expiresAt);
            return AddInternal(key, entry);
        }

        public bool AddInternal(string key, TableCacheEntry entry)
        {
            var op = TableOperation.Insert(entry);
            var result = table.Execute(op);
            return result.HttpStatusCode == 200;
        }

        public long Decrement(string key, uint amount)
        {
            return AtomicIncDec(key, amount * -1);
        }

        internal long AtomicIncDec(string key, long amount)
        {
            long count = 0;
            bool updated = false;

            ExecUtils.RetryUntilTrue(() =>
            {
                var entry = GetEntry(key);

                if (entry == null)
                {
                    count = amount;
                    entry = CreateTableEntry(key, Serialize(count));
                    try
                    {
                        updated = table.Execute(TableOperation.Insert(entry)).HttpStatusCode == (int)HttpStatusCode.NoContent;
                    }
                    catch (StorageException ex)
                    {
                        if (!ex.HasStatus(HttpStatusCode.Conflict))
                            throw;
                    }
                }
                else
                {
                    count = Deserialize<long>(entry.Data) + amount;
                    entry.Data = Serialize<long>(count);
                    var op = TableOperation.Replace(entry);
                    try
                    {
                        var result = table.Execute(op).HttpStatusCode;
                        updated = result == (int)HttpStatusCode.OK || result == (int)HttpStatusCode.NoContent;
                    }
                    catch (StorageException ex)
                    {
                        if (!ex.HasStatus(HttpStatusCode.PreconditionFailed))
                            throw;
                    }
                }

                return updated;
            }, TimeSpan.FromSeconds(30));

            return count;
        }

        public void FlushAll()
        {
            GetKeysByPattern("*").Each(q => Remove(q));
        }

        public T Get<T>(string key)
        {
            var entry = GetEntry(key);
            if (entry != null)
                return Deserialize<T>(entry.Data);
            return default(T);
        }

        internal TableCacheEntry GetEntry(string key)
        {
            if (TryGetValue(key, out var entry))
            {
                if (entry.HasExpired)
                {
                    this.Remove(key);
                    return null;
                }
                return entry;
            }
            return null;
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
            return AtomicIncDec(key, amount);
        }

        public bool Remove(string key)
        {
            var entry = CreateTableEntry(key);
            entry.ETag = "*";   // Avoids concurrency
            var op = TableOperation.Delete(entry);
            try
            {
                TableResult result = table.Execute(op);
                return result.HttpStatusCode == 200 || result.HttpStatusCode == 204;
            }
            catch (Microsoft.WindowsAzure.Storage.StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)System.Net.HttpStatusCode.NotFound)
                    return false;
                throw ex;
            }
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            keys.Each(q => Remove(q));
        }

        public bool Replace<T>(string key, T value)
        {
            return ReplaceInternal(key, Serialize(value));
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return ReplaceInternal(key, Serialize(value), DateTime.UtcNow.Add(expiresIn));
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            return ReplaceInternal(key, Serialize(value), expiresAt);
        }

        private bool ReplaceInternal(string key, string value, DateTime? expiresAt = null)
        {
            if (TryGetValue(key, out var entry))
            {
                entry = CreateTableEntry(key, value, null, expiresAt);
                var op = TableOperation.Replace(entry);
                var result = table.Execute(op);
                return result.HttpStatusCode == 200;
            }
            return false;
        }

        public bool Set<T>(string key, T value)
        {
            var sVal = Serialize(value);
            var entry = CreateTableEntry(key, sVal);
            return SetInternal(entry);
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return Set(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            var sVal = Serialize<T>(value);
            var entry = CreateTableEntry(key, sVal, null, expiresAt);
            return SetInternal(entry);
        }

        internal bool SetInternal(TableCacheEntry entry)
        {
            var op = TableOperation.InsertOrReplace(entry);
            var result = table.Execute(op);
            return result.HttpStatusCode == 200 || result.HttpStatusCode == 204;    // Success or "No content"
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            foreach (var key in values.Keys)
            {
                Set<T>(key, values[key]);
            }
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            var entry = GetEntry(key);
            if (entry != null)
            {
                if (entry.ExpiryDate == null)
                    return TimeSpan.MaxValue;

                return entry.ExpiryDate - DateTime.UtcNow;
            }
            return null;
        }

        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            // Very inefficient - query all keys and do client-side filter
            var query = new TableQuery<TableCacheEntry>();

            return table.ExecuteQuery<TableCacheEntry>(query)
                .Where(q => q.RowKey.Glob(pattern))
                .Select(q => q.RowKey);
        }

        public void RemoveExpiredEntries()
        {
            GetKeysByPattern("*").Each(x =>
                GetEntry(x)); // removes if expired
        }

        public IEnumerable<string> GetKeysByRegex(string regex)
        {
            // Very inefficient - query all keys and do client-side filter
            var query = new TableQuery<TableCacheEntry>();

            var re = new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline);

            return table.ExecuteQuery(query)
                .Where(q => re.IsMatch(q.RowKey))
                .Select(q => q.RowKey);
        }

        private string Serialize<T>(T value)
        {
            using (JsConfig.With(new Text.Config {ExcludeTypeInfo = false}))
            {
                return serializer.SerializeToString<T>(value);
            }
        }

        private T Deserialize<T>(string text)
        {
            using (JsConfig.With(new Text.Config {ExcludeTypeInfo = false}))
            {
                return (text.IsNullOrEmpty()) ? default(T) :
                    serializer.DeserializeFromString<T>(text);
            }
        }

        public void RemoveByPattern(string pattern)
        {
            RemoveAll(GetKeysByPattern(pattern));
        }

        public void RemoveByRegex(string regex)
        {
            RemoveAll(GetKeysByRegex(regex));
        }

        public class TableCacheEntry : TableEntity
        {
            public TableCacheEntry(string key)
            {
                this.PartitionKey = "";
                this.RowKey = key;
            }

            public TableCacheEntry() { }

            [StringLength(1024 * 2014 /* 1 MB max */
                - 1024 /* partition key max size*/
                - 1024 /* row key max size */
                - 64   /* timestamp size */
                - 64 * 3 /* 3 datetime fields */
                // - 8 * 1024 /* ID */
                )]
            public string Data { get; set; }

            public DateTime? ExpiryDate { get; set; }

            public DateTime CreatedDate { get; set; }

            public DateTime ModifiedDate { get; set; }

            internal bool HasExpired => ExpiryDate != null && ExpiryDate < DateTime.UtcNow;
        }

    }
}
