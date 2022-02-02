//
// https://github.com/ServiceStack/ServiceStack.Redis/
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ServiceStack.Redis.Generic;
using ServiceStack.Redis.Pipeline;
using ServiceStack.Text;
using ServiceStack.Caching;

namespace ServiceStack.Redis
{
    /// <summary>
    /// The client wraps the native redis operations into a more readable c# API.
    /// 
    /// Where possible these operations are also exposed in common c# interfaces, 
    /// e.g. RedisClient.Lists => IList[string]
    ///		 RedisClient.Sets => ICollection[string]
    /// </summary>
    public partial class RedisClient
        : RedisNativeClient, IRedisClient, IRemoveByPattern // IRemoveByPattern is implemented in this file.
    {
        public RedisClient()
        {
            Init();
        }

        internal static HashSet<Type> __uniqueTypes = new HashSet<Type>();

        public static Func<RedisClient> NewFactoryFn = () => new RedisClient();

        public static Func<object, Dictionary<string, string>> ConvertToHashFn =
            x => x.ToJson().FromJson<Dictionary<string, string>>();

        /// <summary>
        /// Creates a new instance of the Redis Client from NewFactoryFn. 
        /// </summary>
        public static RedisClient New()
        {
            return NewFactoryFn();
        }

        public RedisClient(string host)
            : base(host)
        {
            Init();
        }

        public RedisClient(RedisEndpoint config)
            : base(config)
        {
            Init();
        }

        public RedisClient(string host, int port)
            : base(host, port)
        {
            Init();
        }

        public RedisClient(string host, int port, string password = null, long db = RedisConfig.DefaultDb)
            : base(host, port, password, db)
        {
            Init();
        }

        public RedisClient(Uri uri)
            : base(uri.Host, uri.Port)
        {
            var password = !string.IsNullOrEmpty(uri.UserInfo) ? uri.UserInfo.Split(':').Last() : null;
            Password = password;
            Init();
        }

        public void Init()
        {
            this.Lists = new RedisClientLists(this);
            this.Sets = new RedisClientSets(this);
            this.SortedSets = new RedisClientSortedSets(this);
            this.Hashes = new RedisClientHashes(this);
        }

        public string this[string key]
        {
            get { return GetValue(key); }
            set { SetValue(key, value); }
        }

        public override void OnConnected() { }

        public RedisText Custom(params object[] cmdWithArgs)
        {
            var data = base.RawCommand(cmdWithArgs);
            var ret = data.ToRedisText();
            return ret;
        }

        public DateTime ConvertToServerDate(DateTime expiresAt) => expiresAt;

        public string GetTypeSequenceKey<T>() => string.Concat(NamespacePrefix, "seq:", typeof(T).Name);

        public string GetTypeIdsSetKey<T>() => string.Concat(NamespacePrefix, "ids:", typeof(T).Name);

        public string GetTypeIdsSetKey(Type type) => string.Concat(NamespacePrefix, "ids:", type.Name);

        public void RewriteAppendOnlyFileAsync() => base.BgRewriteAof();

        public List<string> GetAllKeys() => SearchKeys("*");

        public void SetValue(string key, string value)
        {
            var bytesValue = value?.ToUtf8Bytes();
            base.Set(key, bytesValue);
        }

        public bool SetValue(byte[] key, byte[] value, TimeSpan expireIn)
        {
            if (AssertServerVersionNumber() >= 2600)
            {
                Exec(r => r.Set(key, value, 0, expiryMs: (long)expireIn.TotalMilliseconds));
            }
            else
            {
                Exec(r => r.SetEx(key, (int)expireIn.TotalSeconds, value));
            }

            return true;
        }

        public void SetValue(string key, string value, TimeSpan expireIn)
        {
            var bytesValue = value?.ToUtf8Bytes();

            if (AssertServerVersionNumber() >= 2610)
            {
                if (expireIn.Milliseconds > 0)
                    base.Set(key, bytesValue, 0, (long)expireIn.TotalMilliseconds);
                else
                    base.Set(key, bytesValue, (int)expireIn.TotalSeconds, 0);
            }
            else
            {
                SetEx(key, (int)expireIn.TotalSeconds, bytesValue);
            }
        }

        public bool SetValueIfExists(string key, string value)
        {
            var bytesValue = value?.ToUtf8Bytes();
            return base.Set(key, bytesValue, exists: true);
        }

        public bool SetValueIfNotExists(string key, string value)
        {
            var bytesValue = value?.ToUtf8Bytes();
            return base.Set(key, bytesValue, exists: false);
        }

        public bool SetValueIfExists(string key, string value, TimeSpan expireIn)
        {
            var bytesValue = value?.ToUtf8Bytes();

            if (expireIn.Milliseconds > 0)
                return base.Set(key, bytesValue, exists: true, expiryMs: (long)expireIn.TotalMilliseconds);
            else
                return base.Set(key, bytesValue, exists: true, expirySeconds: (int)expireIn.TotalSeconds);
        }

        public bool SetValueIfNotExists(string key, string value, TimeSpan expireIn)
        {
            var bytesValue = value?.ToUtf8Bytes();

            if (expireIn.Milliseconds > 0)
                return base.Set(key, bytesValue, exists: false, expiryMs: (long)expireIn.TotalMilliseconds);
            else
                return base.Set(key, bytesValue, exists: false, expirySeconds: (int)expireIn.TotalSeconds);
        }

        public void SetValues(Dictionary<string, string> map)
        {
            SetAll(map);
        }

        public void SetAll(IEnumerable<string> keys, IEnumerable<string> values)
        {
            if (GetSetAllBytes(keys, values, out var keyBytes, out var valBytes))
            {
                base.MSet(keyBytes, valBytes);
            }
        }

        bool GetSetAllBytes(IEnumerable<string> keys, IEnumerable<string> values, out byte[][] keyBytes, out byte[][] valBytes)
        {
            keyBytes = valBytes = default;
            if (keys == null || values == null) return false;
            var keyArray = keys.ToArray();
            var valueArray = values.ToArray();

            if (keyArray.Length != valueArray.Length)
                throw new Exception("Key length != Value Length. {0}/{1}".Fmt(keyArray.Length, valueArray.Length));

            if (keyArray.Length == 0) return false;

            keyBytes = new byte[keyArray.Length][];
            valBytes = new byte[keyArray.Length][];
            for (int i = 0; i < keyArray.Length; i++)
            {
                keyBytes[i] = keyArray[i].ToUtf8Bytes();
                valBytes[i] = valueArray[i].ToUtf8Bytes();
            }

            return true;
        }

        public void SetAll(Dictionary<string, string> map)
        {
            if (GetSetAllBytes(map, out var keyBytes, out var valBytes))
            {
                base.MSet(keyBytes, valBytes);
            }
        }

        private static bool GetSetAllBytes(IDictionary<string, string> map, out byte[][] keyBytes, out byte[][] valBytes)
        {
            if (map == null || map.Count == 0)
            {
                keyBytes = null;
                valBytes = null;
                return false;
            }

            keyBytes = new byte[map.Count][];
            valBytes = new byte[map.Count][];

            var i = 0;
            foreach (var key in map.Keys)
            {
                var val = map[key];
                keyBytes[i] = key.ToUtf8Bytes();
                valBytes[i] = val.ToUtf8Bytes();
                i++;
            }
            return true;
        }

        public string GetValue(string key)
        {
            var bytes = Get(key);
            return bytes?.FromUtf8Bytes();
        }

        public string GetAndSetValue(string key, string value)
        {
            return GetSet(key, value.ToUtf8Bytes()).FromUtf8Bytes();
        }

        public bool ContainsKey(string key)
        {
            return Exists(key) == Success;
        }

        public bool Remove(string key)
        {
            return Del(key) == Success;
        }

        public bool Remove(byte[] key)
        {
            return Del(key) == Success;
        }

        public bool RemoveEntry(params string[] keys)
        {
            if (keys.Length == 0) return false;

            return Del(keys) == Success;
        }

        public long IncrementValue(string key)
        {
            return Incr(key);
        }

        public long IncrementValueBy(string key, int count)
        {
            return IncrBy(key, count);
        }

        public long IncrementValueBy(string key, long count)
        {
            return IncrBy(key, count);
        }

        public double IncrementValueBy(string key, double count)
        {
            return IncrByFloat(key, count);
        }

        public long DecrementValue(string key)
        {
            return Decr(key);
        }

        public long DecrementValueBy(string key, int count)
        {
            return DecrBy(key, count);
        }

        public long AppendToValue(string key, string value)
        {
            return base.Append(key, value.ToUtf8Bytes());
        }

        public void RenameKey(string fromName, string toName)
        {
            base.Rename(fromName, toName);
        }

        public long GetStringCount(string key)
        {
            return base.StrLen(key);
        }

        public string GetRandomKey()
        {
            return RandomKey();
        }

        public bool ExpireEntryIn(string key, TimeSpan expireIn)
        {
            if (UseMillisecondExpiration(expireIn))
            {
                return PExpire(key, (long)expireIn.TotalMilliseconds);
            }

            return Expire(key, (int)expireIn.TotalSeconds);
        }

        private bool UseMillisecondExpiration(TimeSpan value)

            => AssertServerVersionNumber() >= 2600 && value.Milliseconds > 0;

        public bool ExpireEntryIn(byte[] key, TimeSpan expireIn)
        {
            if (UseMillisecondExpiration(expireIn))
            {
                return PExpire(key, (long)expireIn.TotalMilliseconds);
            }

            return Expire(key, (int)expireIn.TotalSeconds);
        }

        public bool ExpireEntryAt(string key, DateTime expireAt)
        {
            if (AssertServerVersionNumber() >= 2600)
            {
                return PExpireAt(key, ConvertToServerDate(expireAt).ToUnixTimeMs());
            }
            else
            {
                return ExpireAt(key, ConvertToServerDate(expireAt).ToUnixTime());
            }
        }

        public TimeSpan? GetTimeToLive(string key)
            => ParseTimeToLiveResult(Ttl(key));

        private static TimeSpan? ParseTimeToLiveResult(long ttlSecs)
        {
            if (ttlSecs == -1)
                return TimeSpan.MaxValue; //no expiry set

            if (ttlSecs == -2)
                return null; //key does not exist

            return TimeSpan.FromSeconds(ttlSecs);
        }

        public void RemoveExpiredEntries()
        {
            //Redis automatically removed expired Cache Entries
        }

        public IRedisTypedClient<T> As<T>()
        {
            try
            {
                var typedClient = new RedisTypedClient<T>(this);
                LicenseUtils.AssertValidUsage(LicenseFeature.Redis, QuotaType.Types, __uniqueTypes.Count);
                return typedClient;
            }
            catch (TypeInitializationException ex)
            {
                throw ex.GetInnerMostException();
            }
        }

        public IDisposable AcquireLock(string key)
        {
            return new RedisLock(this, key, null);
        }

        public IDisposable AcquireLock(string key, TimeSpan timeOut)
        {
            return new RedisLock(this, key, timeOut);
        }

        public IRedisTransaction CreateTransaction()
        {
            AssertServerVersionNumber(); // pre-fetch call to INFO before transaction if needed
            return new RedisTransaction(this, false);
        }

        public void AssertNotInTransaction()
        {
            if (Transaction != null || Pipeline != null)
                throw new NotSupportedException("Only atomic redis-server operations are supported in a transaction");
        }

        public IRedisPipeline CreatePipeline()
        {
            return new RedisAllPurposePipeline(this);
        }

        public List<string> SearchKeys(string pattern)
        {
            var multiDataList = ScanAllKeys(pattern);
            return multiDataList.ToList();
        }

        public List<string> GetValues(List<string> keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0) return new List<string>();

            return ParseGetValuesResult(MGet(keys.ToArray()));
        }
        private static List<string> ParseGetValuesResult(byte[][] resultBytesArray)
        {
            var results = new List<string>(resultBytesArray.Length);
            foreach (var resultBytes in resultBytesArray)
            {
                if (resultBytes == null) continue;

                var resultString = resultBytes.FromUtf8Bytes();
                results.Add(resultString);
            }

            return results;
        }

        public List<T> GetValues<T>(List<string> keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0) return new List<T>();

            return ParseGetValuesResult<T>(MGet(keys.ToArray()));
        }

        private static List<T> ParseGetValuesResult<T>(byte[][] resultBytesArray)
        {
            var results = new List<T>(resultBytesArray.Length);
            foreach (var resultBytes in resultBytesArray)
            {
                if (resultBytes == null) continue;

                var resultString = resultBytes.FromUtf8Bytes();
                var result = resultString.FromJson<T>();
                results.Add(result);
            }

            return results;
        }

        public Dictionary<string, string> GetValuesMap(List<string> keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0) return new Dictionary<string, string>();

            var keysArray = keys.ToArray();
            var resultBytesArray = MGet(keysArray);

            return ParseGetValuesMapResult(keysArray, resultBytesArray);
        }

        private static Dictionary<string, string> ParseGetValuesMapResult(string[] keysArray, byte[][] resultBytesArray)
        {
            var results = new Dictionary<string, string>();
            for (var i = 0; i < resultBytesArray.Length; i++)
            {
                var key = keysArray[i];

                var resultBytes = resultBytesArray[i];
                if (resultBytes == null)
                {
                    results.Add(key, null);
                }
                else
                {
                    var resultString = resultBytes.FromUtf8Bytes();
                    results.Add(key, resultString);
                }
            }

            return results;
        }

        public Dictionary<string, T> GetValuesMap<T>(List<string> keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0) return new Dictionary<string, T>();

            var keysArray = keys.ToArray();
            var resultBytesArray = MGet(keysArray);

            return ParseGetValuesMapResult<T>(keysArray, resultBytesArray);
        }

        private static Dictionary<string, T> ParseGetValuesMapResult<T>(string[] keysArray, byte[][] resultBytesArray)
        {
            var results = new Dictionary<string, T>();
            for (var i = 0; i < resultBytesArray.Length; i++)
            {
                var key = keysArray[i];

                var resultBytes = resultBytesArray[i];
                if (resultBytes == null)
                {
                    results.Add(key, default(T));
                }
                else
                {
                    var resultString = resultBytes.FromUtf8Bytes();
                    var result = JsonSerializer.DeserializeFromString<T>(resultString);
                    results.Add(key, result);
                }
            }

            return results;
        }

        public override IRedisSubscription CreateSubscription()
        {
            return new RedisSubscription(this);
        }

        public long PublishMessage(string toChannel, string message)
        {
            return base.Publish(toChannel, message.ToUtf8Bytes());
        }

        #region IBasicPersistenceProvider

        Dictionary<string, HashSet<string>> registeredTypeIdsWithinPipelineMap = new Dictionary<string, HashSet<string>>();

        internal HashSet<string> GetRegisteredTypeIdsWithinPipeline(string typeIdsSet)
        {
            if (!registeredTypeIdsWithinPipelineMap.TryGetValue(typeIdsSet, out var registeredTypeIdsWithinPipeline))
            {
                registeredTypeIdsWithinPipeline = new HashSet<string>();
                registeredTypeIdsWithinPipelineMap[typeIdsSet] = registeredTypeIdsWithinPipeline;
            }
            return registeredTypeIdsWithinPipeline;
        }

        internal void RegisterTypeId<T>(T value)
        {
            var typeIdsSetKey = GetTypeIdsSetKey<T>();
            var id = value.GetId().ToString();

            RegisterTypeId(typeIdsSetKey, id);
        }

        internal void RegisterTypeId(string typeIdsSetKey, string id)
        {
            if (this.Pipeline != null)
            {
                var registeredTypeIdsWithinPipeline = GetRegisteredTypeIdsWithinPipeline(typeIdsSetKey);
                registeredTypeIdsWithinPipeline.Add(id);
            }
            else
            {
                this.AddItemToSet(typeIdsSetKey, id);
            }
        }

        internal void RegisterTypeIds<T>(IEnumerable<T> values)
        {
            var typeIdsSetKey = GetTypeIdsSetKey<T>();
            var ids = values.Map(x => x.GetId().ToString());

            if (this.Pipeline != null)
            {
                var registeredTypeIdsWithinPipeline = GetRegisteredTypeIdsWithinPipeline(typeIdsSetKey);
                ids.ForEach(x => registeredTypeIdsWithinPipeline.Add(x));
            }
            else
            {
                AddRangeToSet(typeIdsSetKey, ids);
            }
        }

        internal void RemoveTypeIdsById<T>(string id)
        {
            var typeIdsSetKey = GetTypeIdsSetKey<T>();
            if (this.Pipeline != null)
                GetRegisteredTypeIdsWithinPipeline(typeIdsSetKey).Remove(id);
            else
                this.RemoveItemFromSet(typeIdsSetKey, id);
        }

        internal void RemoveTypeIdsByIds<T>(IEnumerable<string> ids)
        {
            var typeIdsSetKey = GetTypeIdsSetKey<T>();
            if (this.Pipeline != null)
            {
                var registeredTypeIdsWithinPipeline = GetRegisteredTypeIdsWithinPipeline(typeIdsSetKey);
                ids.Each(x => registeredTypeIdsWithinPipeline.Remove(x));
            }
            else
            {
                ids.Each(x => this.RemoveItemFromSet(typeIdsSetKey, x));
            }
        }

        internal void RemoveTypeIdsByValue<T>(T value) => RemoveTypeIdsById<T>(value.GetId().ToString());

        internal void RemoveTypeIdsByValues<T>(IEnumerable<T> values)
        {
            var typeIdsSetKey = GetTypeIdsSetKey<T>();
            if (this.Pipeline != null)
            {
                var registeredTypeIdsWithinPipeline = GetRegisteredTypeIdsWithinPipeline(typeIdsSetKey);
                values.Each(x => registeredTypeIdsWithinPipeline.Remove(x.GetId().ToString()));
            }
            else
            {
                values.Each(x => this.RemoveItemFromSet(typeIdsSetKey, x.GetId().ToString()));
            }
        }

        // Called just after original Pipeline is closed.
        internal void AddTypeIdsRegisteredDuringPipeline()
        {
            foreach (var entry in registeredTypeIdsWithinPipelineMap)
            {
                AddRangeToSet(entry.Key, entry.Value.ToList());
            }
            registeredTypeIdsWithinPipelineMap = new Dictionary<string, HashSet<string>>();
        }

        internal void ClearTypeIdsRegisteredDuringPipeline()
        {
            registeredTypeIdsWithinPipelineMap = new Dictionary<string, HashSet<string>>();
        }

        public T GetById<T>(object id)
        {
            var key = UrnKey<T>(id);
            var valueString = this.GetValue(key);
            var value = JsonSerializer.DeserializeFromString<T>(valueString);
            return value;
        }

        public IList<T> GetByIds<T>(ICollection ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<T>();

            var urnKeys = ids.Cast<object>().Map(UrnKey<T>);
            return GetValues<T>(urnKeys);
        }

        public IList<T> GetAll<T>()
        {
            var typeIdsSetKy = this.GetTypeIdsSetKey<T>();
            var allTypeIds = this.GetAllItemsFromSet(typeIdsSetKy);
            var urnKeys = allTypeIds.Cast<object>().Map(UrnKey<T>);
            return GetValues<T>(urnKeys);
        }

        public T Store<T>(T entity)
        {
            var urnKey = UrnKey(entity);
            var valueString = JsonSerializer.SerializeToString(entity);

            this.SetValue(urnKey, valueString);
            RegisterTypeId(entity);

            return entity;
        }

        public object StoreObject(object entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var id = entity.GetObjectId();
            var entityType = entity.GetType();
            var urnKey = UrnKey(entityType, id);
            var valueString = JsonSerializer.SerializeToString(entity);

            this.SetValue(urnKey, valueString);

            RegisterTypeId(GetTypeIdsSetKey(entityType), id.ToString());

            return entity;
        }

        public void StoreAll<TEntity>(IEnumerable<TEntity> entities)
        {
            _StoreAll(entities);
        }

        public T GetFromHash<T>(object id)
        {
            var key = UrnKey<T>(id);
            return GetAllEntriesFromHash(key).ToJson().FromJson<T>();
        }

        /// <summary>
        /// Store object fields as a dictionary of values in a Hash value.
        /// Conversion to Dictionary can be customized with RedisClient.ConvertToHashFn
        /// </summary>
        public void StoreAsHash<T>(T entity)
        {
            var key = UrnKey(entity);
            var hash = ConvertToHashFn(entity);
            SetRangeInHash(key, hash);
            RegisterTypeId(entity);
        }

        //Without the Generic Constraints
        internal void _StoreAll<TEntity>(IEnumerable<TEntity> entities)
        {
            if (PrepareStoreAll(entities, out var keys, out var values, out var entitiesList))
            {
                base.MSet(keys, values);
                RegisterTypeIds(entitiesList);
            }
        }

        private bool PrepareStoreAll<TEntity>(IEnumerable<TEntity> entities, out byte[][] keys, out byte[][] values, out List<TEntity> entitiesList)
        {
            if (entities == null)
            {
                entitiesList = default;
                keys = values = default;
                return false;
            }

            entitiesList = entities.ToList();
            var len = entitiesList.Count;
            if (len == 0)
            {
                keys = values = default;
                return false;
            }

            keys = new byte[len][];
            values = new byte[len][];

            for (var i = 0; i < len; i++)
            {
                keys[i] = UrnKey(entitiesList[i]).ToUtf8Bytes();
                values[i] = SerializeToUtf8Bytes(entitiesList[i]);
            }
            return true;
        }

        public void WriteAll<TEntity>(IEnumerable<TEntity> entities)
        {
            if (PrepareWriteAll(entities, out var keys, out var values))
            {
                base.MSet(keys, values);
            }
        }

        private bool PrepareWriteAll<TEntity>(IEnumerable<TEntity> entities, out byte[][] keys, out byte[][] values)
        {
            if (entities == null)
            {
                keys = values = default;
                return false;
            }

            var entitiesList = entities.ToList();
            var len = entitiesList.Count;

            keys = new byte[len][];
            values = new byte[len][];

            for (var i = 0; i < len; i++)
            {
                keys[i] = UrnKey(entitiesList[i]).ToUtf8Bytes();
                values[i] = SerializeToUtf8Bytes(entitiesList[i]);
            }
            return true;
        }

        public static byte[] SerializeToUtf8Bytes<T>(T value)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.SerializeToString(value));
        }

        public void Delete<T>(T entity)
        {
            var urnKey = UrnKey(entity);
            this.Remove(urnKey);
            this.RemoveTypeIdsByValue(entity);
        }

        public void DeleteById<T>(object id)
        {
            var urnKey = UrnKey<T>(id);
            this.Remove(urnKey);
            this.RemoveTypeIdsById<T>(id.ToString());
        }

        public void DeleteByIds<T>(ICollection ids)
        {
            if (ids == null || ids.Count == 0) return;

            var idsList = ids.Cast<object>();
            var urnKeys = idsList.Select(UrnKey<T>).ToArray();
            this.RemoveEntry(urnKeys);
            this.RemoveTypeIdsByIds<T>(ids.Map(x => x.ToString()).ToArray());
        }

        public void DeleteAll<T>()
        {
            DeleteAll<T>(0,RedisConfig.CommandKeysBatchSize);
        }

        private void DeleteAll<T>(ulong cursor, int batchSize)
        {
            var typeIdsSetKey = this.GetTypeIdsSetKey<T>();
            do
            {
                var scanResult = this.SScan(typeIdsSetKey, cursor, batchSize);
                cursor = scanResult.Cursor;
                var urnKeys = scanResult.Results.Select(id => UrnKey<T>(id.FromUtf8Bytes())).ToArray();
                if (urnKeys.Length > 0)
                {
                    this.RemoveEntry(urnKeys);
                }
            } while (cursor != 0);
            
            this.RemoveEntry(typeIdsSetKey);
        }

        public RedisClient CloneClient() => new(Host, Port, Password, Db) {
            SendTimeout = SendTimeout,
            ReceiveTimeout = ReceiveTimeout
        };

        /// <summary>
        /// Returns key with automatic object id detection in provided value with <typeparam name="T">generic type</typeparam>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string UrnKey<T>(T value)
        {
            return string.Concat(NamespacePrefix, value.CreateUrn());
        }

        /// <summary>
        /// Returns key with explicit object id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string UrnKey<T>(object id)
        {
            return string.Concat(NamespacePrefix, IdUtils.CreateUrn<T>(id));
        }

        /// <summary>
        /// Returns key with explicit object type and id.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public string UrnKey(Type type, object id)
        {
            return string.Concat(NamespacePrefix, IdUtils.CreateUrn(type, id));
        }


        #endregion

        #region LUA EVAL

        static readonly ConcurrentDictionary<string, string> CachedLuaSha1Map = new();

        public T ExecCachedLua<T>(string scriptBody, Func<string, T> scriptSha1)
        {
            if (!CachedLuaSha1Map.TryGetValue(scriptBody, out var sha1))
                CachedLuaSha1Map[scriptBody] = sha1 = LoadLuaScript(scriptBody);

            try
            {
                return scriptSha1(sha1);
            }
            catch (RedisResponseException ex)
            {
                if (!ex.Message.StartsWith("NOSCRIPT"))
                    throw;

                CachedLuaSha1Map[scriptBody] = sha1 = LoadLuaScript(scriptBody);
                return scriptSha1(sha1);
            }
        }

        public RedisText ExecLua(string body, params string[] args)
        {
            var data = base.EvalCommand(body, 0, args.ToMultiByteArray());
            return data.ToRedisText();
        }

        public RedisText ExecLua(string luaBody, string[] keys, string[] args)
        {
            var data = base.EvalCommand(luaBody, keys.Length, MergeAndConvertToBytes(keys, args));
            return data.ToRedisText();
        }

        public RedisText ExecLuaSha(string sha1, params string[] args)
        {
            var data = base.EvalShaCommand(sha1, 0, args.ToMultiByteArray());
            return data.ToRedisText();
        }

        public RedisText ExecLuaSha(string sha1, string[] keys, string[] args)
        {
            var data = base.EvalShaCommand(sha1, keys.Length, MergeAndConvertToBytes(keys, args));
            return data.ToRedisText();
        }

        public long ExecLuaAsInt(string body, params string[] args)
        {
            return base.EvalInt(body, 0, args.ToMultiByteArray());
        }

        public long ExecLuaAsInt(string luaBody, string[] keys, string[] args)
        {
            return base.EvalInt(luaBody, keys.Length, MergeAndConvertToBytes(keys, args));
        }

        public long ExecLuaShaAsInt(string sha1, params string[] args)
        {
            return base.EvalShaInt(sha1, 0, args.ToMultiByteArray());
        }

        public long ExecLuaShaAsInt(string sha1, string[] keys, string[] args)
        {
            return base.EvalShaInt(sha1, keys.Length, MergeAndConvertToBytes(keys, args));
        }

        public string ExecLuaAsString(string body, params string[] args)
        {
            return base.EvalStr(body, 0, args.ToMultiByteArray());
        }

        public string ExecLuaAsString(string sha1, string[] keys, string[] args)
        {
            return base.EvalStr(sha1, keys.Length, MergeAndConvertToBytes(keys, args));
        }

        public string ExecLuaShaAsString(string sha1, params string[] args)
        {
            return base.EvalShaStr(sha1, 0, args.ToMultiByteArray());
        }

        public string ExecLuaShaAsString(string sha1, string[] keys, string[] args)
        {
            return base.EvalShaStr(sha1, keys.Length, MergeAndConvertToBytes(keys, args));
        }

        public List<string> ExecLuaAsList(string body, params string[] args)
        {
            return base.Eval(body, 0, args.ToMultiByteArray()).ToStringList();
        }

        public List<string> ExecLuaAsList(string luaBody, string[] keys, string[] args)
        {
            return base.Eval(luaBody, keys.Length, MergeAndConvertToBytes(keys, args)).ToStringList();
        }

        public List<string> ExecLuaShaAsList(string sha1, params string[] args)
        {
            return base.EvalSha(sha1, 0, args.ToMultiByteArray()).ToStringList();
        }

        public List<string> ExecLuaShaAsList(string sha1, string[] keys, string[] args)
        {
            return base.EvalSha(sha1, keys.Length, MergeAndConvertToBytes(keys, args)).ToStringList();
        }


        public bool HasLuaScript(string sha1Ref)
        {
            return WhichLuaScriptsExists(sha1Ref)[sha1Ref];
        }

        public Dictionary<string, bool> WhichLuaScriptsExists(params string[] sha1Refs)
        {
            var intFlags = base.ScriptExists(sha1Refs.ToMultiByteArray());
            return WhichLuaScriptsExistsParseResult(sha1Refs, intFlags);
        }
        static Dictionary<string, bool> WhichLuaScriptsExistsParseResult(string[] sha1Refs, byte[][] intFlags)
        {
            var map = new Dictionary<string, bool>();
            for (int i = 0; i < sha1Refs.Length; i++)
            {
                var sha1Ref = sha1Refs[i];
                map[sha1Ref] = intFlags[i].FromUtf8Bytes() == "1";
            }
            return map;
        }

        public void RemoveAllLuaScripts()
        {
            base.ScriptFlush();
        }

        public void KillRunningLuaScript()
        {
            base.ScriptKill();
        }

        public string LoadLuaScript(string body)
        {
            return base.ScriptLoad(body).FromUtf8Bytes();
        }

        #endregion

        public void RemoveByPattern(string pattern)
        {
            var keys = ScanAllKeys(pattern).ToArray();
            if (keys.Length > 0)
                Del(keys);
        }

        public void RemoveByRegex(string pattern)
        {
            RemoveByPattern(RegexToGlob(pattern));
        }
        
        private static string RegexToGlob(string regex)
            => regex.Replace(".*", "*").Replace(".+", "?");

        public IEnumerable<string> ScanAllKeys(string pattern = null, int pageSize = 1000)
        {
            var ret = new ScanResult();
            while (true)
            {
                ret = pattern != null
                    ? base.Scan(ret.Cursor, pageSize, match: pattern)
                    : base.Scan(ret.Cursor, pageSize);

                foreach (var key in ret.Results)
                {
                    yield return key.FromUtf8Bytes();
                }

                if (ret.Cursor == 0) break;
            }
        }

        public IEnumerable<string> ScanAllSetItems(string setId, string pattern = null, int pageSize = 1000)
        {
            var ret = new ScanResult();
            while (true)
            {
                ret = pattern != null
                    ? base.SScan(setId, ret.Cursor, pageSize, match: pattern)
                    : base.SScan(setId, ret.Cursor, pageSize);

                foreach (var key in ret.Results)
                {
                    yield return key.FromUtf8Bytes();
                }

                if (ret.Cursor == 0) break;
            }
        }

        public IEnumerable<KeyValuePair<string, double>> ScanAllSortedSetItems(string setId, string pattern = null, int pageSize = 1000)
        {
            var ret = new ScanResult();
            while (true)
            {
                ret = pattern != null
                    ? base.ZScan(setId, ret.Cursor, pageSize, match: pattern)
                    : base.ZScan(setId, ret.Cursor, pageSize);

                foreach (var entry in ret.AsItemsWithScores())
                {
                    yield return entry;
                }

                if (ret.Cursor == 0) break;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> ScanAllHashEntries(string hashId, string pattern = null, int pageSize = 1000)
        {
            var ret = new ScanResult();
            while (true)
            {
                ret = pattern != null
                    ? base.HScan(hashId, ret.Cursor, pageSize, match: pattern)
                    : base.HScan(hashId, ret.Cursor, pageSize);

                foreach (var entry in ret.AsKeyValues())
                {
                    yield return entry;
                }

                if (ret.Cursor == 0) break;
            }
        }

        public bool AddToHyperLog(string key, params string[] elements)
        {
            return base.PfAdd(key, elements.Map(x => x.ToUtf8Bytes()).ToArray());
        }

        public long CountHyperLog(string key)
        {
            return base.PfCount(key);
        }

        public void MergeHyperLogs(string toKey, params string[] fromKeys)
        {
            base.PfMerge(toKey, fromKeys);
        }

        public RedisServerRole GetServerRole()
        {
            if (AssertServerVersionNumber() >= 2812)
            {
                var text = base.Role();
                var roleName = text.Children[0].Text;
                return ToServerRole(roleName);
            }

            this.Info.TryGetValue("role", out var role);
            return ToServerRole(role);
        }

        private static RedisServerRole ToServerRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return RedisServerRole.Unknown;

            switch (roleName)
            {
                case "master":
                    return RedisServerRole.Master;
                case "slave":
                    return RedisServerRole.Slave;
                case "sentinel":
                    return RedisServerRole.Sentinel;
                default:
                    return RedisServerRole.Unknown;
            }
        }

        internal RedisClient LimitAccessToThread(int originalThreadId, string originalStackTrace)
        {            
            TrackThread = new TrackThread(originalThreadId, originalStackTrace);
            return this;
        }        
    }

    internal struct TrackThread
    {
        public readonly int ThreadId;
        public readonly string StackTrace;
        
        public TrackThread(int threadId, string stackTrace)
        {
            ThreadId = threadId;
            StackTrace = stackTrace;
        }
    }

    public class InvalidAccessException : RedisException
    {
        public InvalidAccessException(int threadId, string stackTrace) 
            : base($"The Current Thread #{Thread.CurrentThread.ManagedThreadId} is different to the original Thread #{threadId} that resolved this pooled client at: \n{stackTrace}") { }
    }

}