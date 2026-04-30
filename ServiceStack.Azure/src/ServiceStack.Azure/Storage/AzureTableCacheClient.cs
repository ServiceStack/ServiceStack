using System.Collections.Generic;
using System.Linq;
using System;
using ServiceStack.Caching;
using ServiceStack.Logging;
using ServiceStack.Support;
using ServiceStack.Text;
using ServiceStack.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;
using Azure.Data.Tables;
using Azure;

namespace ServiceStack.Azure.Storage;

public partial class AzureTableCacheClient : AdapterBase, ICacheClientExtended, IRemoveByPattern
{
    TableCacheEntry CreateTableEntry(string rowKey, string? data = null,
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

    private readonly string? connectionString;
    private readonly string? partitionKey = "";
    private readonly TableClient? tableClient;
    IStringSerializer? serializer;

    public AzureTableCacheClient(string? connectionString, string tableName = "Cache")
    {
        this.connectionString = connectionString;
        tableClient = new TableClient(this.connectionString, tableName);
        tableClient.CreateIfNotExists();
        serializer = new JsonStringSerializer();
    }

    private bool TryGetValue(string key, out TableCacheEntry? entry)
    {
        entry = null;
        try
        {
            entry = tableClient!.GetEntity<TableCacheEntry>(partitionKey, key).Value;
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (!FlushOnDispose) return;
        FlushAll();
    }

    public bool Add<T>(string key, T value)
    {
        var sVal = serializer!.SerializeToString(value);
        var entry = CreateTableEntry(key, sVal, null);
        return AddInternal(key, entry);
    }

    public bool Add<T>(string key, T value, TimeSpan expiresIn)
    {
        return Add(key, value, DateTime.UtcNow.Add(expiresIn));
    }

    public bool Add<T>(string key, T value, DateTime expiresAt)
    {
        var sVal = serializer!.SerializeToString(value);
        var entry = CreateTableEntry(key, sVal, null, expiresAt);
        return AddInternal(key, entry);
    }

    public bool AddInternal(string key, TableCacheEntry entry)
    {
        try
        {
            tableClient!.AddEntity(entry);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
        {
            return false;
        }
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
                    tableClient!.AddEntity(entry);
                    updated = true;
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
                {
                    // another writer beat us, retry
                }
            }
            else
            {
                count = Deserialize<long>(entry.Data) + amount;
                entry.Data = Serialize(count);
                try
                {
                    tableClient!.UpdateEntity(entry, entry.ETag, TableUpdateMode.Replace);
                    updated = true;
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.PreconditionFailed)
                {
                    // concurrent modification, retry
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

    public T? Get<T>(string key)
    {
        var entry = GetEntry(key);
        if (entry != null)
            return Deserialize<T>(entry.Data);
        return default;
    }

    internal TableCacheEntry? GetEntry(string key)
    {
        if (TryGetValue(key, out var entry))
        {
            if (entry!.HasExpired)
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
        if (!TryGetValue(key, out _))
            return false;

        try
        {
            tableClient!.DeleteEntity(partitionKey, key, ETag.All);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return false;
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
        if (!TryGetValue(key, out _))
            return false;

        var entry = CreateTableEntry(key, value, null, expiresAt);
        tableClient!.UpdateEntity(entry, ETag.All, TableUpdateMode.Replace);
        return true;
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
        tableClient!.UpsertEntity(entry, TableUpdateMode.Replace);
        return true;
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
        return tableClient!.Query<TableCacheEntry>()
            .Where(q => q.RowKey.Glob(pattern))
            .Select(static q => q.RowKey);
    }

    public void RemoveExpiredEntries()
    {
        GetKeysByPattern("*").Each(x =>
            GetEntry(x)); // removes if expired
    }

    public IEnumerable<string> GetKeysByRegex(string regex)
    {
        var re = new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline);
        return tableClient!.Query<TableCacheEntry>()
            .Where(q => re.IsMatch(q.RowKey))
            .Select(static q => q.RowKey);
    }

    private string Serialize<T>(T value)
    {
        using (JsConfig.With(new Text.Config { ExcludeTypeInfo = false }))
        {
            return serializer!.SerializeToString<T>(value);
        }
    }

    private T Deserialize<T>(string? text)
    {
        using (JsConfig.With(new Text.Config { ExcludeTypeInfo = false }))
        {
            return text.IsNullOrEmpty() ? default(T) :
                serializer!.DeserializeFromString<T>(text);
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

    public class TableCacheEntry : ITableEntity
    {
        public TableCacheEntry(string key)
        {
            PartitionKey = "";
            RowKey = key;
        }

        public TableCacheEntry() { }

        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [StringLength(1024 * 2014 /* 1 MB max */
                      - 1024 /* partition key max size*/
                      - 1024 /* row key max size */
                      - 64   /* timestamp size */
                      - 64 * 3 /* 3 datetime fields */
            // - 8 * 1024 /* ID */
        )]
        public string? Data { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        // ToUniversalTime() normalises Kind (Azure.Data.Tables may return Utc, Local, or Unspecified).
        // The 200ms grace absorbs the network roundtrip time between when ExpiryDate is computed
        // (before the Set call) and when HasExpired is evaluated (after a subsequent Get call).
        internal bool HasExpired => ExpiryDate != null &&
            ExpiryDate.Value.ToUniversalTime() < DateTime.UtcNow.AddMilliseconds(-200);
    }
}
