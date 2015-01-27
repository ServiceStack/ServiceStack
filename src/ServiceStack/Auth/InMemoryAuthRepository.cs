using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Thread-safe In memory UserAuth data store so it can be used without a dependency on Redis.
    /// </summary>
    public class InMemoryAuthRepository : RedisAuthRepository, IDisposable 
    {
        public static readonly InMemoryAuthRepository Instance = new InMemoryAuthRepository();

        protected Dictionary<string, HashSet<string>> Sets { get; set; }
        protected Dictionary<string, Dictionary<string, string>> Hashes { get; set; }
        internal List<IClearable> TrackedTypes = new List<IClearable>();

        class TypedData<T> : IClearable
        {
            internal static TypedData<T> Instance = new TypedData<T>();

            private TypedData()
            {
                lock (InMemoryAuthRepository.Instance.TrackedTypes)
                    InMemoryAuthRepository.Instance.TrackedTypes.Add(this);
            }

            internal readonly List<T> Items = new List<T>();
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

        class InMemoryManagerFacade : IRedisClientManagerFacade
        {
            private readonly InMemoryAuthRepository root;

            public InMemoryManagerFacade(InMemoryAuthRepository root)
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
        }

        class InMemoryClientFacade : IRedisClientFacade
        {
            private readonly InMemoryAuthRepository root;

            public InMemoryClientFacade(InMemoryAuthRepository root)
            {
                this.root = root;
            }

            class InMemoryTypedClientFacade<T> : ITypedRedisClientFacade<T>
            {
                private readonly InMemoryAuthRepository root;

                public InMemoryTypedClientFacade(InMemoryAuthRepository root)
                {
                    this.root = root;
                }

                public int GetNextSequence()
                {
                    return Interlocked.Increment(ref TypedData<T>.Instance.Sequence);
                }

                public T GetById(object id)
                {
                    if (id == null) return default(T);

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
            }

            public HashSet<string> GetAllItemsFromSet(string setId)
            {
                lock (root.Sets)
                {
                    HashSet<string> set;
                    return root.Sets.TryGetValue(setId, out set) ? set : new HashSet<string>();
                }
            }

            public void Store<T>(T item)
            {
                if (Equals(item, default(T))) return;

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

            public string GetValueFromHash(string hashId, string key)
            {
                hashId.ThrowIfNull("hashId");
                key.ThrowIfNull("key");

                lock (root.Hashes)
                {
                    Dictionary<string, string> hash;
                    if (!root.Hashes.TryGetValue(hashId, out hash)) return null;

                    string value;
                    hash.TryGetValue(key, out value);
                    return value;
                }
            }

            public void SetEntryInHash(string hashId, string key, string value)
            {
                hashId.ThrowIfNull("hashId");
                key.ThrowIfNull("key");

                lock (root.Hashes)
                {
                    Dictionary<string, string> hash;
                    if (!root.Hashes.TryGetValue(hashId, out hash))
                        root.Hashes[hashId] = hash = new Dictionary<string, string>();

                    hash[key] = value;
                }
            }

            public void RemoveEntryFromHash(string hashId, string key)
            {
                hashId.ThrowIfNull("hashId");
                key.ThrowIfNull("key");

                lock (root.Hashes)
                {
                    Dictionary<string, string> hash;
                    if (!root.Hashes.TryGetValue(hashId, out hash))
                        root.Hashes[hashId] = hash = new Dictionary<string, string>();

                    hash.Remove(key);
                }
            }

            public void AddItemToSet(string setId, string item)
            {
                lock (root.Sets)
                {
                    HashSet<string> set;
                    if (!root.Sets.TryGetValue(setId, out set))
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

        public void Dispose()
        {
            Clear();
        }
    }
}