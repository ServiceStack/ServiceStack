using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Auth
{
    public interface IRedisClientManagerFacade : IClearable
    {
        IRedisClientFacade GetClient();
    }

    public interface IClearable
    {
        void Clear();		
    }

    public interface IRedisClientFacade : IDisposable
    {
        HashSet<string> GetAllItemsFromSet(string setId);
        void Store<T>(T item);
        void DeleteById<T>(string id);
        string GetValueFromHash(string hashId, string key);
        void SetEntryInHash(string hashId, string key, string value);
        void RemoveEntryFromHash(string hashId, string key);
        void AddItemToSet(string setId, string item);
        ITypedRedisClientFacade<T> As<T>();
    }

    public interface ITypedRedisClientFacade<T>
    {
        int GetNextSequence();
        T GetById(object id);
        List<T> GetByIds(IEnumerable ids);
        void DeleteById(string id);
        void DeleteByIds(IEnumerable ids);
    }

    public class RedisClientManagerFacade : IRedisClientManagerFacade
    {
        private readonly IRedisClientsManager redisManager;

        public RedisClientManagerFacade(IRedisClientsManager redisManager)
        {
            this.redisManager = redisManager;
        }

        public IRedisClientFacade GetClient()
        {
            return new RedisClientFacade(redisManager.GetClient());
        }

        public void Clear()
        {
            using (var redis = redisManager.GetClient())
                redis.FlushAll();
        }

        private class RedisClientFacade : IRedisClientFacade
        {
            private readonly IRedisClient redisClient;

            class RedisITypedRedisClientFacade<T> : ITypedRedisClientFacade<T>
            {
                private readonly IRedisTypedClient<T> redisTypedClient;

                public RedisITypedRedisClientFacade(IRedisTypedClient<T> redisTypedClient)
                {
                    this.redisTypedClient = redisTypedClient;
                }

                public int GetNextSequence()
                {
                    return (int) redisTypedClient.GetNextSequence();
                }

                public T GetById(object id)
                {
                    return redisTypedClient.GetById(id);
                }

                public List<T> GetByIds(IEnumerable ids)
                {
                    return redisTypedClient.GetByIds(ids).ToList();
                }

                public void DeleteById(string id)
                {
                    redisTypedClient.DeleteById(id);
                }

                public void DeleteByIds(IEnumerable ids)
                {
                    redisTypedClient.DeleteByIds(ids);
                }
            }

            public RedisClientFacade(IRedisClient redisClient)
            {
                this.redisClient = redisClient;
            }

            public HashSet<string> GetAllItemsFromSet(string setId)
            {
                return redisClient.GetAllItemsFromSet(setId);
            }

            public void Store<T>(T item)
            {
                redisClient.Store(item);
            }

            public void DeleteById<T>(string id)
            {
                redisClient.DeleteById<T>(id);
            }

            public string GetValueFromHash(string hashId, string key)
            {
                return redisClient.GetValueFromHash(hashId, key);
            }

            public void SetEntryInHash(string hashId, string key, string value)
            {
                redisClient.SetEntryInHash(hashId, key, value);
            }

            public void RemoveEntryFromHash(string hashId, string key)
            {
                redisClient.RemoveEntryFromHash(hashId, key);
            }

            public void AddItemToSet(string setId, string item)
            {
                redisClient.AddItemToSet(setId, item);
            }

            public ITypedRedisClientFacade<T> As<T>()
            {
                return new RedisITypedRedisClientFacade<T>(redisClient.As<T>());
            }

            public void Dispose()
            {
                this.redisClient.Dispose();
            }
        }
    }

}