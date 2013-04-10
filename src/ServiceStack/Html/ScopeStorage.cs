// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Net30.Collections.Concurrent;

namespace ServiceStack.Html
{
    public static class ScopeStorage
    {
        private static readonly IScopeStorageProvider _defaultStorageProvider = new StaticScopeStorageProvider();
        private static IScopeStorageProvider _stateStorageProvider;

        public static IScopeStorageProvider CurrentProvider
        {
            get { return _stateStorageProvider ?? _defaultStorageProvider; }
            set { _stateStorageProvider = value; }
        }

        public static IDictionary<object, object> CurrentScope
        {
            get { return CurrentProvider.CurrentScope; }
        }

        public static IDictionary<object, object> GlobalScope
        {
            get { return CurrentProvider.GlobalScope; }
        }

        public static IDisposable CreateTransientScope(IDictionary<object, object> context)
        {
            var currentContext = CurrentScope;
            CurrentProvider.CurrentScope = context;
            return new DisposableAction(() => CurrentProvider.CurrentScope = currentContext); // Return an IDisposable that pops the item back off
        }

        public static IDisposable CreateTransientScope()
        {
            return CreateTransientScope(new ScopeStorageDictionary(baseScope: CurrentScope));
        }
    }

    public class StaticScopeStorageProvider : IScopeStorageProvider
    {
        private static readonly IDictionary<object, object> _defaultContext =
            new ScopeStorageDictionary(null, new ConcurrentDictionary<object, object>(ScopeStorageComparer.Instance));

        private IDictionary<object, object> _currentContext;

        public IDictionary<object, object> CurrentScope
        {
            get { return _currentContext ?? _defaultContext; }
            set { _currentContext = value; }
        }

        public IDictionary<object, object> GlobalScope
        {
            get { return _defaultContext; }
        }
    }

    public interface IScopeStorageProvider
    {
        IDictionary<object, object> CurrentScope { get; set; }

        IDictionary<object, object> GlobalScope { get; }
    }

    public class ScopeStorageDictionary : IDictionary<object, object>
    {
        private static readonly StateStorageKeyValueComparer _keyValueComparer = new StateStorageKeyValueComparer();
        private readonly IDictionary<object, object> _baseScope;
        private readonly IDictionary<object, object> _backingStore;

        public ScopeStorageDictionary()
            : this(baseScope: null)
        {
        }

        public ScopeStorageDictionary(IDictionary<object, object> baseScope)
            : this(baseScope: baseScope, backingStore: new Dictionary<object, object>(ScopeStorageComparer.Instance))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeStorageDictionary"/> class.
        /// </summary>
        /// <param name="baseScope">The base scope.</param>
        /// <param name="backingStore">
        /// The dictionary to use as a storage. Since the dictionary would be used as-is, we expect the implementer to 
        /// use the same key-value comparison logic as we do here.
        /// </param>
        internal ScopeStorageDictionary(IDictionary<object, object> baseScope, IDictionary<object, object> backingStore)
        {
            _baseScope = baseScope;
            _backingStore = backingStore;
        }

        protected IDictionary<object, object> BackingStore
        {
            get { return _backingStore; }
        }

        protected IDictionary<object, object> BaseScope
        {
            get { return _baseScope; }
        }

        public virtual ICollection<object> Keys
        {
            get { return GetItems().Select(item => item.Key).ToList(); }
        }

        public virtual ICollection<object> Values
        {
            get { return GetItems().Select(item => item.Value).ToList(); }
        }

        public virtual int Count
        {
            get { return GetItems().Count(); }
        }

        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public object this[object key]
        {
            get
            {
                object value;
                TryGetValue(key, out value);
                return value;
            }
            set { SetValue(key, value); }
        }

        public virtual void SetValue(object key, object value)
        {
            _backingStore[key] = value;
        }

        public virtual bool TryGetValue(object key, out object value)
        {
            return _backingStore.TryGetValue(key, out value) || (_baseScope != null && _baseScope.TryGetValue(key, out value));
        }

        public virtual bool Remove(object key)
        {
            return _backingStore.Remove(key);
        }

        public virtual IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return GetItems().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual void Add(object key, object value)
        {
            SetValue(key, value);
        }

        public virtual bool ContainsKey(object key)
        {
            return _backingStore.ContainsKey(key) || (_baseScope != null && _baseScope.ContainsKey(key));
        }

        public virtual void Add(KeyValuePair<object, object> item)
        {
            SetValue(item.Key, item.Value);
        }

        public virtual void Clear()
        {
            _backingStore.Clear();
        }

        public virtual bool Contains(KeyValuePair<object, object> item)
        {
            return _backingStore.Contains(item) || (_baseScope != null && _baseScope.Contains(item));
        }

        public virtual void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
        {
            GetItems().ToList().CopyTo(array, arrayIndex);
        }

        public virtual bool Remove(KeyValuePair<object, object> item)
        {
            return _backingStore.Remove(item);
        }

        protected virtual IEnumerable<KeyValuePair<object, object>> GetItems()
        {
            if (_baseScope == null) {
                return _backingStore;
            }
            return Enumerable.Concat(_backingStore, _baseScope).Distinct(_keyValueComparer);
        }

        private class StateStorageKeyValueComparer : IEqualityComparer<KeyValuePair<object, object>>
        {
            private IEqualityComparer<object> _stateStorageComparer = ScopeStorageComparer.Instance;

            public bool Equals(KeyValuePair<object, object> x, KeyValuePair<object, object> y)
            {
                return _stateStorageComparer.Equals(x.Key, y.Key);
            }

            public int GetHashCode(KeyValuePair<object, object> obj)
            {
                return _stateStorageComparer.GetHashCode(obj.Key);
            }
        }
    }

    /// <summary>
    /// Custom comparer for the context dictionaries
    /// The comparer treats strings as a special case, performing case insesitive comparison. 
    /// This guaratees that we remain consistent throughout the chain of contexts since PageData dictionary 
    /// behaves in this manner.
    /// </summary>
    internal class ScopeStorageComparer : IEqualityComparer<object>
    {
        private static IEqualityComparer<object> _instance;
        private readonly IEqualityComparer<object> _defaultComparer = EqualityComparer<object>.Default;
        private readonly IEqualityComparer<string> _stringComparer = StringComparer.OrdinalIgnoreCase;

        private ScopeStorageComparer()
        {
        }

        public static IEqualityComparer<object> Instance
        {
            get
            {
                if (_instance == null) {
                    _instance = new ScopeStorageComparer();
                }
                return _instance;
            }
        }

        public new bool Equals(object x, object y)
        {
            string xString = x as string;
            string yString = y as string;

            if ((xString != null) && (yString != null)) {
                return _stringComparer.Equals(xString, yString);
            }

            return _defaultComparer.Equals(x, y);
        }

        public int GetHashCode(object obj)
        {
            string objString = obj as string;
            if (objString != null) {
                return _stringComparer.GetHashCode(objString);
            }

            return _defaultComparer.GetHashCode(obj);
        }
    }

    internal class DisposableAction : IDisposable
    {
        private Action _action;
        private bool _hasDisposed;

        public DisposableAction(Action action)
        {
            if (action == null) {
                throw new ArgumentNullException("action");
            }
            _action = action;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // If we were disposed by the finalizer it's because the user didn't use a "using" block, so don't do anything!
            if (disposing) {
                lock (this) {
                    if (!_hasDisposed) {
                        _hasDisposed = true;
                        _action();
                    }
                }
            }
        }
    }
}
