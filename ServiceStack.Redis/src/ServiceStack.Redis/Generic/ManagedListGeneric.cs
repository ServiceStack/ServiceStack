using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Redis.Generic
{
    public class ManagedList<T> : IList<T>
    {
        private string key = null;
        private IRedisClientsManager manager;
        private ManagedList() { }

        public ManagedList(IRedisClientsManager manager, string key)
        {
            this.key = key;
            this.manager = manager;
        }

        private IRedisClient GetClient()
        {
            return manager.GetClient();
        }

        private List<T> GetRedisList()
        {
            using (var redis = GetClient())
            {
                var client = redis.As<T>();
                return client.Lists[key].ToList();
            }
        }
        public int IndexOf(T item)
        {
            return GetRedisList().IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            using (var redis = GetClient())
            {
                redis.As<T>().Lists[key].Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            using (var redis = GetClient())
            {
                redis.As<T>().Lists[key].RemoveAt(index);
            }
        }

        public T this[int index]
        {
            get
            {
                return GetRedisList()[index];
            }
            set
            {
                using (var redis = GetClient())
                {
                    redis.As<T>().Lists[key][index] = value;
                }
            }
        }


        public void Add(T item)
        {
            using (var redis = GetClient())
            {
                redis.As<T>().Lists[key].Add(item);
            }
        }

        public void Clear()
        {
            using (var redis = GetClient())
            {
                redis.As<T>().Lists[key].Clear();
            }
        }

        public bool Contains(T item)
        {
            return GetRedisList().Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            GetRedisList().CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return GetRedisList().Count(); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            var index = this.IndexOf(item);
            if (index != -1)
            {
                this.RemoveAt(index);
                return true;
            }

            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetRedisList().GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)GetRedisList()).GetEnumerator();
        }
    }
}
