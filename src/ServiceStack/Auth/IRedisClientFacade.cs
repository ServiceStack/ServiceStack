using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Auth
{
    public partial interface IRedisClientManagerFacade : IClearable
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
        List<T> GetAll(int? skip=null, int? take=null);
    }

    public interface IClearableAsync
    {
        Task ClearAsync(CancellationToken token=default);
    }
    
    public partial interface IRedisClientManagerFacade : IClearableAsync
    {
        Task<IRedisClientFacadeAsync> GetClientAsync(CancellationToken token=default);
    }

    public interface IRedisClientFacadeAsync : IAsyncDisposable
    {
        Task<HashSet<string>> GetAllItemsFromSetAsync(string setId, CancellationToken token=default);
        Task StoreAsync<T>(T item, CancellationToken token=default);
        Task DeleteByIdAsync<T>(string id, CancellationToken token=default);
        Task<string> GetValueFromHashAsync(string hashId, string key, CancellationToken token=default);
        Task SetEntryInHashAsync(string hashId, string key, string value, CancellationToken token=default);
        Task RemoveEntryFromHashAsync(string hashId, string key, CancellationToken token=default);
        Task AddItemToSetAsync(string setId, string item, CancellationToken token=default);
        ITypedRedisClientFacadeAsync<T> AsAsync<T>();
    }

    public interface ITypedRedisClientFacadeAsync<T>
    {
        Task<int> GetNextSequenceAsync(CancellationToken token=default);
        Task<T> GetByIdAsync(object id,CancellationToken token=default);
        Task<List<T>> GetByIdsAsync(IEnumerable ids, CancellationToken token=default);
        Task DeleteByIdAsync(string id, CancellationToken token=default);
        Task DeleteByIdsAsync(IEnumerable ids, CancellationToken token=default);
        Task<List<T>> GetAllAsync(int? skip=null, int? take=null, CancellationToken token=default);
    }

    public class RedisClientManagerFacade : IRedisClientManagerFacade
    {
        private readonly IRedisClientsManager redisManager;

        public RedisClientManagerFacade(IRedisClientsManager redisManager)
        {
            this.redisManager = redisManager;
            this.redisManagerAsync = (IRedisClientsManagerAsync) redisManager;
        }

        public IRedisClientFacade GetClient()
        {
            return new RedisClientFacade(redisManager.GetClient());
        }

        public void Clear()
        {
            using var redis = redisManager.GetClient();
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

                public List<T> GetAll(int? skip=null, int? take=null)
                {
                    if (skip != null || take != null)
                    {
                        var keys = redisTypedClient.TypeIdsSet.GetAll().OrderBy(x => x).AsEnumerable();
                        if (skip != null)
                            keys = keys.Skip(skip.Value);
                        if (take != null)
                            keys = keys.Take(take.Value);
                        return redisTypedClient.GetByIds(keys).ToList();
                    }
                    
                    return redisTypedClient.GetAll().ToList();
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

        private readonly IRedisClientsManagerAsync redisManagerAsync;
        public async Task ClearAsync(CancellationToken token=default)
        {
            var redis = await redisManagerAsync.GetClientAsync(token);
            await redis.FlushAllAsync(token);
        }

        public async Task<IRedisClientFacadeAsync> GetClientAsync(CancellationToken token = default) =>
            new RedisClientFacadeAsync(await redisManagerAsync.GetClientAsync(token));

        private class RedisClientFacadeAsync : IRedisClientFacadeAsync
        {
            private readonly IRedisClientAsync redisClient;

            public RedisClientFacadeAsync(IRedisClientAsync redisClient)
            {
                this.redisClient = redisClient;
            }

            class RedisITypedRedisClientFacadeAsync<T> : ITypedRedisClientFacadeAsync<T>
            {
                private readonly IRedisTypedClientAsync<T> redisTypedClient;

                public RedisITypedRedisClientFacadeAsync(IRedisTypedClientAsync<T> redisTypedClient)
                {
                    this.redisTypedClient = redisTypedClient;
                }

                public async Task<int> GetNextSequenceAsync(CancellationToken token=default)
                {
                    return (int) await redisTypedClient.GetNextSequenceAsync(token);
                }

                public async Task<T> GetByIdAsync(object id, CancellationToken token=default)
                {
                    return await redisTypedClient.GetByIdAsync(id, token);
                }

                public async Task<List<T>> GetByIdsAsync(IEnumerable ids, CancellationToken token=default)
                {
                    return (await redisTypedClient.GetByIdsAsync(ids, token)).ToList();
                }

                public async Task DeleteByIdAsync(string id, CancellationToken token=default)
                {
                    await redisTypedClient.DeleteByIdAsync(id, token);
                }

                public async Task DeleteByIdsAsync(IEnumerable ids, CancellationToken token=default)
                {
                    await redisTypedClient.DeleteByIdsAsync(ids, token);
                }

                public async Task<List<T>> GetAllAsync(int? skip=null, int? take=null, CancellationToken token=default)
                {
                    if (skip != null || take != null)
                    {
                        var keys = (await redisTypedClient.TypeIdsSet.GetAllAsync(token)).OrderBy(x => x).AsEnumerable();
                        if (skip != null)
                            keys = keys.Skip(skip.Value);
                        if (take != null)
                            keys = keys.Take(take.Value);
                        return (await redisTypedClient.GetByIdsAsync(keys, token)).ToList();
                    }
                    
                    return (await redisTypedClient.GetAllAsync(token)).ToList();
                }
            }

            public async Task<HashSet<string>> GetAllItemsFromSetAsync(string setId, CancellationToken token=default)
            {
                return await redisClient.GetAllItemsFromSetAsync(setId, token);
            }

            public async Task StoreAsync<T>(T item, CancellationToken token=default)
            {
                await redisClient.StoreAsync(item, token);
            }

            public async Task DeleteByIdAsync<T>(string id, CancellationToken token=default)
            {
                await redisClient.DeleteByIdAsync<T>(id, token);
            }

            public async Task<string> GetValueFromHashAsync(string hashId, string key, CancellationToken token=default)
            {
                return await redisClient.GetValueFromHashAsync(hashId, key, token);
            }

            public async Task SetEntryInHashAsync(string hashId, string key, string value, CancellationToken token=default)
            {
                await redisClient.SetEntryInHashAsync(hashId, key, value, token);
            }

            public async Task RemoveEntryFromHashAsync(string hashId, string key, CancellationToken token=default)
            {
                await redisClient.RemoveEntryFromHashAsync(hashId, key, token);
            }

            public async Task AddItemToSetAsync(string setId, string item, CancellationToken token=default)
            {
                await redisClient.AddItemToSetAsync(setId, item, token);
            }

            public ITypedRedisClientFacadeAsync<T> AsAsync<T>()
            {
                return new RedisITypedRedisClientFacadeAsync<T>(redisClient.As<T>());
            }

            public async ValueTask DisposeAsync()
            {
                await this.redisClient.DisposeAsync();
            }
        }
        
    }

}