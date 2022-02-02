namespace Xilium.CefGlue.Wrapper
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    // Maps an arbitrary TId to an arbitrary TObject on a per-browser basis.
    internal sealed class CefBrowserInfoMap<TKey, TValue>
    {
        public delegate bool Visitor(int browserId, TKey key, TValue value, ref bool remove);

        private Dictionary<int, Dictionary<TKey, TValue>> _map = new Dictionary<int, Dictionary<TKey, TValue>>();

        public CefBrowserInfoMap()
        {
        }

        public bool IsEmpty
        {
            get
            {
                return Count() == 0;
            }
        }

        public int Count()
        {
            var result = 0;
            foreach (var v in _map.Values)
            {
                result += v.Count;
            }
            return result;
        }

        public int Count(int browserId)
        {
            Dictionary<TKey, TValue> v;
            if (_map.TryGetValue(browserId, out v)) return v.Count;
            else return 0;
        }


        public void Add(int browserId, TKey key, TValue value)
        {
            Dictionary<TKey, TValue> v;
            if (!_map.TryGetValue(browserId, out v))
            {
                v = new Dictionary<TKey, TValue>();
                _map.Add(browserId, v);
            }

            v.Add(key, value);
        }

        public TValue Find(int browserId, TKey key, Visitor visitor)
        {
            Dictionary<TKey, TValue> v;
            if (!_map.TryGetValue(browserId, out v))
            {
                return default(TValue);
            }

            TValue x;
            if (v.TryGetValue(key, out x))
            {
                bool remove = false;
                visitor(browserId, key, x, ref remove);
                if (remove)
                {
                    v.Remove(key);
                }
                return x;
            }
            else return default(TValue);
        }

        public void FindAll(int browserId, Visitor visitor)
        {
            Dictionary<TKey, TValue> v;
            if (!_map.TryGetValue(browserId, out v)) return;

            bool hasRemoveKey = false;
            TKey removeKey = default(TKey);
            List<TKey> removeList = null;
            foreach (var kv in v)
            {
                bool remove = false;
                visitor(browserId, kv.Key, kv.Value, ref remove);
                if (remove)
                {
                    if (!hasRemoveKey)
                    {
                        removeKey = kv.Key;
                        hasRemoveKey = true;
                    }
                    else
                    {
                        if (removeList == null) removeList = new List<TKey>();
                        removeList.Add(kv.Key);
                    }
                }
            }

            if (hasRemoveKey) v.Remove(removeKey);
            if (removeList != null)
            {
                foreach (var k in removeList) v.Remove(k);
            }
        }

        public void FindAll(Visitor visitor)
        {
            foreach (var k in _map.Keys)
            {
                FindAll(k, visitor);
            }
        }
    }
}
