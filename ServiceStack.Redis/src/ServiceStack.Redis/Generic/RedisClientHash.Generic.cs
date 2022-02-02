//
// https://github.com/ServiceStack/ServiceStack.Redis
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
using System.Collections.Generic;

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// Wrap the common redis set operations under a ICollection[string] interface.
    /// </summary>
    internal partial class RedisClientHash<TKey, T>
        : IRedisHash<TKey, T>
    {
        private readonly RedisTypedClient<T> client;
        private readonly string hashId;

        public RedisClientHash(RedisTypedClient<T> client, string hashId)
        {
            this.client = client;
            this.hashId = hashId;
        }

        public string Id
        {
            get { return this.hashId; }
        }

        public IEnumerator<KeyValuePair<TKey, T>> GetEnumerator()
        {
            return client.GetAllEntriesFromHash(this).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Dictionary<TKey, T> GetAll()
        {
            return client.GetAllEntriesFromHash(this);
        }

        public void Add(KeyValuePair<TKey, T> item)
        {
            client.SetEntryInHash(this, item.Key, item.Value);
        }

        public void Clear()
        {
            client.RemoveEntry(this);
        }

        public bool Contains(KeyValuePair<TKey, T> item)
        {
            var value = client.GetValueFromHash(this, item.Key);
            return !Equals(value, default(T)) && Equals(value, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, T>[] array, int arrayIndex)
        {
            var allItemsInHash = client.GetAllEntriesFromHash(this);

            var i = arrayIndex;
            foreach (var entry in allItemsInHash)
            {
                if (i >= array.Length) return;
                array[i] = entry;
            }
        }

        public bool Remove(KeyValuePair<TKey, T> item)
        {
            return Contains(item) && client.RemoveEntryFromHash(this, item.Key);
        }

        public int Count
        {
            get { return (int)client.GetHashCount(this); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(TKey key)
        {
            return client.HashContainsEntry(this, key);
        }

        public void Add(TKey key, T value)
        {
            client.SetEntryInHash(this, key, value);
        }

        public bool Remove(TKey key)
        {
            return client.RemoveEntryFromHash(this, key);
        }

        public bool TryGetValue(TKey key, out T value)
        {
            if (ContainsKey(key))
            {
                value = client.GetValueFromHash(this, key);
                return true;
            }
            value = default(T);
            return false;
        }

        public T this[TKey key]
        {
            get { return client.GetValueFromHash(this, key); }
            set { client.SetEntryInHash(this, key, value); }
        }

        public ICollection<TKey> Keys
        {
            get { return client.GetHashKeys(this); }
        }

        public ICollection<T> Values
        {
            get { return client.GetHashValues(this); }
        }

        public List<TKey> GetAllKeys()
        {
            return client.GetHashKeys(this);
        }

        public List<T> GetAllValues()
        {
            return client.GetHashValues(this);
        }
    }
}