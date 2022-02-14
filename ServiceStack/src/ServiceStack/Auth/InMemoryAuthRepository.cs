using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Auth
{
    public class InMemoryAuthRepository : InMemoryAuthRepository<UserAuth, UserAuthDetails>
    {
    }

    public interface IMemoryAuthRepository
        : IUserAuthRepository, IClearable, IManageApiKeys, ICustomUserAuth
    {
        Dictionary<string, HashSet<string>> Sets { get; }
        Dictionary<string, Dictionary<string, string>> Hashes { get; }
    }

    /// <summary>
    /// Thread-safe In memory UserAuth data store so it can be used without a dependency on Redis.
    /// </summary>
    public class InMemoryAuthRepository<TUserAuth, TUserAuthDetails> 
        : RedisAuthRepository<TUserAuth, TUserAuthDetails>, IMemoryAuthRepository
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        public static readonly InMemoryAuthRepository<TUserAuth, TUserAuthDetails> Instance = 
            new InMemoryAuthRepository<TUserAuth, TUserAuthDetails>();

        public Dictionary<string, HashSet<string>> Sets { get; set; }
        public Dictionary<string, Dictionary<string, string>> Hashes { get; set; }
        internal List<IClearable> TrackedTypes = new List<IClearable>();

        internal class TypedData<T> : IClearable
        {
            internal static TypedData<T> Instance = new();

            private TypedData()
            {
                lock (InMemoryAuthRepository.Instance.TrackedTypes)
                    InMemoryAuthRepository.Instance.TrackedTypes.Add(this);
            }

            internal readonly List<T> Items = new();
            internal int Sequence = 0;

            public void Clear()
            {
                lock (Items) Items.Clear();
                Interlocked.CompareExchange(ref Sequence, 0, Sequence);
            }
        }


        public InMemoryAuthRepository()
            : base(new InMemoryManagerFacade(Instance))
        {
            this.Sets = new Dictionary<string, HashSet<string>>();
            this.Hashes = new Dictionary<string, Dictionary<string, string>>();
        }

        internal class InMemoryManagerFacade : IRedisClientManagerFacade
        {
            private readonly IMemoryAuthRepository root;

            public InMemoryManagerFacade(IMemoryAuthRepository root)
            {
                this.root = root;
            }

            public IRedisClientFacade GetClient()
            {
                return new InMemoryClientFacade(root);
            }

            public void Clear()
            {
                lock (Instance.Sets) Instance.Sets.Clear();
                lock (Instance.Hashes) Instance.Hashes.Clear();
                lock (Instance.TrackedTypes) Instance.TrackedTypes.ForEach(x => x.Clear());
            }

            public Task ClearAsync(CancellationToken token = default)
            {
                Clear();
                return TypeConstants.EmptyTask;
            }

            public Task<IRedisClientFacadeAsync> GetClientAsync(CancellationToken token = default)
            {
                return Task.FromResult((IRedisClientFacadeAsync)new InMemoryClientFacadeAsync(root));
            }
        }

        internal class InMemoryClientFacade : IRedisClientFacade
        {
            private readonly IMemoryAuthRepository root;

            public InMemoryClientFacade(IMemoryAuthRepository root)
            {
                this.root = root;
            }

            class InMemoryTypedClientFacade<T> : ITypedRedisClientFacade<T>
            {
                private readonly IMemoryAuthRepository root;

                public InMemoryTypedClientFacade(IMemoryAuthRepository root)
                {
                    this.root = root;
                }

                public int GetNextSequence()
                {
                    return Interlocked.Increment(ref TypedData<T>.Instance.Sequence);
                }

                public T GetById(object id)
                {
                    if (id == null) 
                        return default;

                    lock (TypedData<T>.Instance.Items)
                    {
                        return TypedData<T>.Instance.Items.FirstOrDefault(x => id.ToString() == x.ToId().ToString());
                    }
                }

                public List<T> GetByIds(IEnumerable ids)
                {
                    var idsSet = new HashSet<object>();
                    foreach (var id in ids) idsSet.Add(id.ToString());

                    lock (TypedData<T>.Instance.Items)
                    {
                        return TypedData<T>.Instance.Items.Where(x => idsSet.Contains(x.ToId().ToString())).ToList();
                    }
                }

                public void DeleteById(string id)
                {
                    lock (TypedData<T>.Instance.Items)
                    {
                        TypedData<T>.Instance.Items.RemoveAll(x => x.GetId().ToString() == id);
                    }
                }

                public void DeleteByIds(IEnumerable ids)
                {
                    var idsSet = new HashSet<object>();
                    foreach (var id in ids) idsSet.Add(id.ToString());

                    lock (TypedData<T>.Instance.Items)
                    {
                        TypedData<T>.Instance.Items.RemoveAll(x => idsSet.Contains(x.ToId().ToString()));
                    }
                }

                public List<T> GetAll(int? skip=null, int? take=null)
                {
                    lock (TypedData<T>.Instance.Items)
                    {
                        if (skip != null || take != null)
                        {
                            var to = TypedData<T>.Instance.Items.AsEnumerable();
                            if (skip != null)
                                to = to.Skip(skip.Value);
                            if (take != null)
                                to = to.Take(take.Value);
                            return to.ToList();
                        }

                        return TypedData<T>.Instance.Items.ToList();
                    }
                }
            }

            public HashSet<string> GetAllItemsFromSet(string setId)
            {
                lock (root.Sets)
                {
                    return root.Sets.TryGetValue(setId, out var set) ? set : new HashSet<string>();
                }
            }

            public void Store<T>(T item)
            {
                if (Equals(item, default(T))) 
                    return;

                lock (TypedData<T>.Instance.Items)
                {
                    for (var i = 0; i < TypedData<T>.Instance.Items.Count; i++)
                    {
                        var o = TypedData<T>.Instance.Items[i];
                        if (o.ToId().ToString() != item.ToId().ToString()) continue;
                        TypedData<T>.Instance.Items[i] = item;
                        return;
                    }
                    TypedData<T>.Instance.Items.Add(item);
                }
            }

            public void DeleteById<T>(string id)
            {
                lock (TypedData<T>.Instance.Items)
                {
                    TypedData<T>.Instance.Items.RemoveAll(x => x.GetId().ToString() == id);
                }
            }

            public string GetValueFromHash(string hashId, string key)
            {
                if (hashId == null)
                    throw new ArgumentNullException(nameof(hashId));
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                lock (root.Hashes)
                {
                    if (!root.Hashes.TryGetValue(hashId, out var hash)) 
                        return null;

                    hash.TryGetValue(key, out var value);
                    return value;
                }
            }

            public void SetEntryInHash(string hashId, string key, string value)
            {
                if (hashId == null)
                    throw new ArgumentNullException(nameof(hashId));
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                lock (root.Hashes)
                {
                    if (!root.Hashes.TryGetValue(hashId, out var hash))
                        root.Hashes[hashId] = hash = new Dictionary<string, string>();

                    hash[key] = value;
                }
            }

            public void RemoveEntryFromHash(string hashId, string key)
            {
                if (hashId == null)
                    throw new ArgumentNullException(nameof(hashId));
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                lock (root.Hashes)
                {
                    if (!root.Hashes.TryGetValue(hashId, out var hash))
                        root.Hashes[hashId] = hash = new Dictionary<string, string>();

                    hash.Remove(key);
                }
            }

            public void AddItemToSet(string setId, string item)
            {
                lock (root.Sets)
                {
                    if (!root.Sets.TryGetValue(setId, out var set))
                        root.Sets[setId] = set = new HashSet<string>();

                    set.Add(item);
                }
            }

            public ITypedRedisClientFacade<T> As<T>()
            {
                return new InMemoryTypedClientFacade<T>(root);
            }

            public void Dispose()
            {
            }
        }

        internal class InMemoryClientFacadeAsync : IRedisClientFacadeAsync
        {
            private readonly IMemoryAuthRepository root;

            public InMemoryClientFacadeAsync(IMemoryAuthRepository root)
            {
                this.root = root;
            }

            class InMemoryTypedClientFacadeAsync<T> : ITypedRedisClientFacadeAsync<T>
            {
                private readonly IMemoryAuthRepository root;

                public InMemoryTypedClientFacadeAsync(IMemoryAuthRepository root)
                {
                    this.root = root;
                }

                public Task<int> GetNextSequenceAsync(CancellationToken token = default)
                {
                    return Interlocked.Increment(ref TypedData<T>.Instance.Sequence).InTask();
                }

                public Task<T> GetByIdAsync(object id, CancellationToken token = default)
                {
                    if (id == null) 
                        return default;

                    lock (TypedData<T>.Instance.Items)
                    {
                        return TypedData<T>.Instance.Items.FirstOrDefault(x => id.ToString() == x.ToId().ToString()).InTask();
                    }
                }

                public Task<List<T>> GetByIdsAsync(IEnumerable ids, CancellationToken token = default)
                {
                    var idsSet = new HashSet<object>();
                    foreach (var id in ids) 
                        idsSet.Add(id.ToString());

                    lock (TypedData<T>.Instance.Items)
                    {
                        return TypedData<T>.Instance.Items.Where(x => idsSet.Contains(x.ToId().ToString())).ToList().InTask();
                    }
                }

                public Task DeleteByIdAsync(string id, CancellationToken token = default)
                {
                    lock (TypedData<T>.Instance.Items)
                    {
                        TypedData<T>.Instance.Items.RemoveAll(x => x.GetId().ToString() == id);
                    }
                    return TypeConstants.EmptyTask;
                }

                public Task DeleteByIdsAsync(IEnumerable ids, CancellationToken token = default)
                {
                    var idsSet = new HashSet<object>();
                    foreach (var id in ids) 
                        idsSet.Add(id.ToString());

                    lock (TypedData<T>.Instance.Items)
                    {
                        TypedData<T>.Instance.Items.RemoveAll(x => idsSet.Contains(x.ToId().ToString()));
                    }
                    return TypeConstants.EmptyTask;
                }

                public Task<List<T>> GetAllAsync(int? skip = null, int? take = null, CancellationToken token = default)
                {
                    lock (TypedData<T>.Instance.Items)
                    {
                        if (skip != null || take != null)
                        {
                            var to = TypedData<T>.Instance.Items.AsEnumerable();
                            if (skip != null)
                                to = to.Skip(skip.Value);
                            if (take != null)
                                to = to.Take(take.Value);
                            return to.ToList().InTask();
                        }

                        return TypedData<T>.Instance.Items.ToList().InTask();
                    }
                }
            }

            public Task<HashSet<string>> GetAllItemsFromSetAsync(string setId, CancellationToken token = default)
            {
                lock (root.Sets)
                {
                    return (root.Sets.TryGetValue(setId, out var set) ? set : new HashSet<string>()).InTask();
                }
            }

            public Task StoreAsync<T>(T item, CancellationToken token = default)
            {
                if (Equals(item, default(T))) 
                    return TypeConstants.EmptyTask;

                lock (TypedData<T>.Instance.Items)
                {
                    for (var i = 0; i < TypedData<T>.Instance.Items.Count; i++)
                    {
                        var o = TypedData<T>.Instance.Items[i];
                        if (o.ToId().ToString() != item.ToId().ToString()) continue;
                        TypedData<T>.Instance.Items[i] = item;
                        return TypeConstants.EmptyTask;
                    }
                    TypedData<T>.Instance.Items.Add(item);
                }
                return TypeConstants.EmptyTask;
            }

            public Task DeleteByIdAsync<T>(string id, CancellationToken token = default)
            {
                lock (TypedData<T>.Instance.Items)
                {
                    TypedData<T>.Instance.Items.RemoveAll(x => x.GetId().ToString() == id);
                }
                return TypeConstants.EmptyTask;
            }

            public Task<string> GetValueFromHashAsync(string hashId, string key, CancellationToken token = default)
            {
                if (hashId == null)
                    throw new ArgumentNullException(nameof(hashId));
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                lock (root.Hashes)
                {
                    if (!root.Hashes.TryGetValue(hashId, out var hash)) 
                        return null;

                    hash.TryGetValue(key, out var value);
                    return Task.FromResult(value);
                }
            }

            public Task SetEntryInHashAsync(string hashId, string key, string value, CancellationToken token = default)
            {
                if (hashId == null)
                    throw new ArgumentNullException(nameof(hashId));
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                lock (root.Hashes)
                {
                    if (!root.Hashes.TryGetValue(hashId, out var hash))
                        root.Hashes[hashId] = hash = new Dictionary<string, string>();

                    hash[key] = value;
                }
                return TypeConstants.EmptyTask;
            }

            public Task RemoveEntryFromHashAsync(string hashId, string key, CancellationToken token = default)
            {
                if (hashId == null)
                    throw new ArgumentNullException(nameof(hashId));
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                lock (root.Hashes)
                {
                    if (!root.Hashes.TryGetValue(hashId, out var hash))
                        root.Hashes[hashId] = hash = new Dictionary<string, string>();

                    hash.Remove(key);
                }
                return TypeConstants.EmptyTask;
            }

            public Task AddItemToSetAsync(string setId, string item, CancellationToken token = default)
            {
                lock (root.Sets)
                {
                    if (!root.Sets.TryGetValue(setId, out var set))
                        root.Sets[setId] = set = new HashSet<string>();

                    set.Add(item);
                }
                return TypeConstants.EmptyTask;
            }

            public ITypedRedisClientFacadeAsync<T> AsAsync<T>()
            {
                return new InMemoryTypedClientFacadeAsync<T>(root);
            }

            public ValueTask DisposeAsync() => default;
        }
        
    }
    
    public static class AuthRepositoryUtils
    {
        public static string ParseOrderBy(string orderBy, out bool desc)
        {
            desc = false;
            if (string.IsNullOrEmpty(orderBy))
                return null;
            
            if (orderBy.IndexOf(' ') >= 0)
            {
                desc = orderBy.LastRightPart(' ').EqualsIgnoreCase("DESC");
                orderBy = orderBy.LeftPart(' ');
            }
            else if (orderBy[0] == '-')
            {
                orderBy = orderBy.Substring(1);
                desc = true;
            }

            return orderBy;
        }
        
        public static IEnumerable<TUserAuth> SortAndPage<TUserAuth>(this IEnumerable<TUserAuth> q, string orderBy, int? skip, int? take)
            where TUserAuth : IUserAuth
        {
            if (!string.IsNullOrEmpty(orderBy))
            {
                orderBy = ParseOrderBy(orderBy, out var desc);

                if (orderBy.EqualsIgnoreCase(nameof(IUserAuth.Id)))
                {
                    q = desc 
                        ? q.OrderByDescending(x => x.Id)
                        : q.OrderBy(x => x.Id);
                }
                else if (orderBy.EqualsIgnoreCase(nameof(IUserAuth.PrimaryEmail)))
                {
                    q = desc 
                        ? q.OrderByDescending(x => x.PrimaryEmail)
                        : q.OrderBy(x => x.PrimaryEmail);
                }
                else if (orderBy.EqualsIgnoreCase(nameof(IUserAuth.CreatedDate)))
                {
                    q = desc 
                        ? q.OrderByDescending(x => x.CreatedDate)
                        : q.OrderBy(x => x.CreatedDate);
                }
                else if (orderBy.EqualsIgnoreCase(nameof(IUserAuth.ModifiedDate)))
                {
                    q = desc 
                        ? q.OrderByDescending(x => x.ModifiedDate)
                        : q.OrderBy(x => x.ModifiedDate);
                }
                else if (orderBy.EqualsIgnoreCase(nameof(IUserAuth.LockedDate)))
                {
                    q = desc 
                        ? q.OrderByDescending(x => x.LockedDate)
                        : q.OrderBy(x => x.LockedDate);
                }
                else if (orderBy.EqualsIgnoreCase(nameof(IUserAuthDetailsExtended.UserName)))
                {
                    q = desc
                        ? q.OrderByDescending(x => x is IUserAuthDetailsExtended u ? u.UserName : null)
                        : q.OrderBy(x => x is IUserAuthDetailsExtended u ? u.UserName : null);
                }
                else if (orderBy.EqualsIgnoreCase(nameof(IUserAuthDetailsExtended.DisplayName)))
                {
                    q = desc
                        ? q.OrderByDescending(x => x is IUserAuthDetailsExtended u ? u.DisplayName : null)
                        : q.OrderBy(x => x is IUserAuthDetailsExtended u ? u.DisplayName : null);
                }
                else if (orderBy.EqualsIgnoreCase(nameof(IUserAuthDetailsExtended.FirstName)))
                {
                    q = desc
                        ? q.OrderByDescending(x => x is IUserAuthDetailsExtended u ? u.FirstName : null)
                        : q.OrderBy(x => x is IUserAuthDetailsExtended u ? u.FirstName : null);
                }
                else if (orderBy.EqualsIgnoreCase(nameof(IUserAuthDetailsExtended.LastName)))
                {
                    q = desc
                        ? q.OrderByDescending(x => x is IUserAuthDetailsExtended u ? u.LastName : null)
                        : q.OrderBy(x => x is IUserAuthDetailsExtended u ? u.LastName : null);
                }
                else if (orderBy.EqualsIgnoreCase(nameof(IUserAuthDetailsExtended.Email)))
                {
                    q = desc
                        ? q.OrderByDescending(x => x is IUserAuthDetailsExtended u ? u.Email : null)
                        : q.OrderBy(x => x is IUserAuthDetailsExtended u ? u.Email : null);
                }
                else if (orderBy.EqualsIgnoreCase(nameof(IUserAuthDetailsExtended.Company)))
                {
                    q = desc
                        ? q.OrderByDescending(x => x is IUserAuthDetailsExtended u ? u.Company : null)
                        : q.OrderBy(x => x is IUserAuthDetailsExtended u ? u.Company : null);
                }
            }
            
            if (skip != null)
                q = q.Skip(skip.Value);
            if (take != null)
                q = q.Take(take.Value);

            return q;
        }
    }    
}