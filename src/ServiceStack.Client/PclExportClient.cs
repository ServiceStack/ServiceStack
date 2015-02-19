//
// System.Collections.Specialized.NameObjectCollectionBase.cs
//
// Author:
//   Gleb Novodran
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//
// (C) Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

/*** REMINDER: Keep this file in sync with ServiceStack.Text/Pcl.NameValueCollection.cs ***/

using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

#if NETFX_CORE || PCL || SL5
//namespace System.Collections.Specialized
namespace ServiceStack.Pcl
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Net;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;

    public class Hashtable : Dictionary<string, object>
    {
        public Hashtable(IDictionary<string, object> dictionary, IEqualityComparer<string> comparer) : base(dictionary, comparer) {}
        public Hashtable(int capacity, IEqualityComparer<string> comparer) : base(capacity, comparer) {}
        public Hashtable(int capacity) : base(capacity) {}
        public Hashtable(IDictionary<string, object> dictionary) : base(dictionary) {}
        public Hashtable(IEqualityComparer<string> comparer) : base(comparer) {}
        public Hashtable() {}
    }

    public class SerializableAttribute : Attribute {}
    public class ArrayList : List<object>{}

    public class StringEqualityComparer : IEqualityComparer<string>
    {
        private readonly IEqualityComparer comparer;

        public StringEqualityComparer(IEqualityComparer comparer)
        {
            this.comparer = comparer;
        }

        public bool Equals(string x, string y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return comparer.Equals(x, y);
        }

        public int GetHashCode(string obj)
        {
            return comparer.GetHashCode(obj);
        }
    }

    [Serializable]
    public abstract class NameObjectCollectionBase : ICollection, IEnumerable
        //, ISerializable, IDeserializationCallback
    {
        private Hashtable m_ItemsContainer;
        /// <summary>
        /// Extends Hashtable based Items container to support storing null-key pairs
        /// </summary>
        private _Item m_NullKeyItem;
        private ArrayList m_ItemsArray;
        private IComparer m_comparer;
        private int m_defCapacity;
        private bool m_readonly;
        SerializationInfo infoCopy;
        private KeysCollection keyscoll;
        private IEqualityComparer equality_comparer;

        internal IEqualityComparer EqualityComparer
        {
            get { return equality_comparer; }
        }

        internal IComparer Comparer
        {
            get { return m_comparer; }
        }

        internal class _Item
        {
            public string key;
            public object value;
            public _Item(string key, object value)
            {
                this.key = key;
                this.value = value;
            }
        }
        /// <summary>
        /// Implements IEnumerable interface for KeysCollection
        /// </summary>
        [Serializable]
        internal class _KeysEnumerator : IEnumerator
        {
            private NameObjectCollectionBase m_collection;
            private int m_position;

            internal _KeysEnumerator(NameObjectCollectionBase collection)
            {
                m_collection = collection;
                Reset();
            }
            public object Current
            {

                get
                {
                    if ((m_position < m_collection.Count) || (m_position < 0))
                        return m_collection.BaseGetKey(m_position);
                    else
                        throw new InvalidOperationException();
                }

            }
            public bool MoveNext()
            {
                return ((++m_position) < m_collection.Count);
            }
            public void Reset()
            {
                m_position = -1;
            }
        }

        /// <summary>
        /// SDK: Represents a collection of the String keys of a collection.
        /// </summary>
        [Serializable]
        public class KeysCollection : ICollection, IEnumerable
        {
            private NameObjectCollectionBase m_collection;

            internal KeysCollection(NameObjectCollectionBase collection)
            {
                this.m_collection = collection;
            }

            public virtual string Get(int index)
            {
                return m_collection.BaseGetKey(index);
            }

            // ICollection methods -----------------------------------
            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                ArrayList items = m_collection.m_ItemsArray;
                if (null == array)
                    throw new ArgumentNullException("array");

                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException("arrayIndex");

                if ((array.Length > 0) && (arrayIndex >= array.Length))
                    throw new ArgumentException("arrayIndex is equal to or greater than array.Length");

                if (arrayIndex + items.Count > array.Length)
                    throw new ArgumentException("Not enough room from arrayIndex to end of array for this KeysCollection");

                if (array != null && array.Rank > 1)
                    throw new ArgumentException("array is multidimensional");

                object[] objArray = (object[])array;
                for (int i = 0; i < items.Count; i++, arrayIndex++)
                    objArray[arrayIndex] = ((_Item)items[i]).key;
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }
            object ICollection.SyncRoot
            {
                get
                {
                    return m_collection;
                }
            }
            /// <summary>
            /// Gets the number of keys in the NameObjectCollectionBase.KeysCollection
            /// </summary>
            public int Count
            {
                get
                {
                    return m_collection.Count;
                }
            }

            public string this[int index]
            {
                get { return Get(index); }
            }

            // IEnumerable methods --------------------------------
            /// <summary>
            /// SDK: Returns an enumerator that can iterate through the NameObjectCollectionBase.KeysCollection.
            /// </summary>
            /// <returns></returns>
            public IEnumerator GetEnumerator()
            {
                return new _KeysEnumerator(m_collection);
            }
        }

        //--------------- Protected Instance Constructors --------------

        /// <summary>
        /// SDK: Initializes a new instance of the NameObjectCollectionBase class that is empty.
        /// </summary>
        protected NameObjectCollectionBase()
        {
            m_readonly = false;
            m_comparer = StringComparer.Ordinal;
            m_defCapacity = 0;
            Init();
        }

        protected NameObjectCollectionBase(int capacity)
        {
            m_readonly = false;
            m_comparer = StringComparer.Ordinal;
            m_defCapacity = capacity;
            Init();
        }

        protected NameObjectCollectionBase(IEqualityComparer equalityComparer)
            : this(0, (equalityComparer ?? StringComparer.OrdinalIgnoreCase))
        {
        }

        protected NameObjectCollectionBase(SerializationInfo info, StreamingContext context)
        {
            infoCopy = info;
        }

        protected NameObjectCollectionBase(int capacity, IEqualityComparer equalityComparer)
        {
            m_readonly = false;
            equality_comparer = (equalityComparer ?? StringComparer.OrdinalIgnoreCase);
            m_defCapacity = capacity;
            Init();
        }

        private void Init()
        {
            if (m_ItemsContainer != null)
            {
                m_ItemsContainer.Clear();
                m_ItemsContainer = null;
            }

            if (m_ItemsArray != null)
            {
                m_ItemsArray.Clear();
                m_ItemsArray = null;
            }

            if (equality_comparer == null)
                equality_comparer = StringComparer.OrdinalIgnoreCase;

            m_ItemsContainer = new Hashtable(m_defCapacity, new StringEqualityComparer(equality_comparer));
            m_ItemsArray = new ArrayList();
            m_NullKeyItem = null;
        }

        //--------------- Public Instance Properties -------------------

        public virtual NameObjectCollectionBase.KeysCollection Keys
        {
            get
            {
                if (keyscoll == null)
                    keyscoll = new KeysCollection(this);
                return keyscoll;
            }
        }

        //--------------- Public Instance Methods ----------------------
        // 
        /// <summary>
        /// SDK: Returns an enumerator that can iterate through the NameObjectCollectionBase.
        /// 
        /// <remark>This enumerator returns the keys of the collection as strings.</remark>
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator GetEnumerator()
        {
            return new _KeysEnumerator(this);
        }

        // ISerializable
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            int count = Count;
            string[] keys = new string[count];
            object[] values = new object[count];
            int i = 0;
            foreach (_Item item in m_ItemsArray)
            {
                keys[i] = item.key;
                values[i] = item.value;
                i++;
            }

            info.AddValue("KeyComparer", equality_comparer, typeof(IEqualityComparer));
            info.AddValue("Version", 4, typeof(int));

            info.AddValue("ReadOnly", m_readonly);
            info.AddValue("Count", count);
            info.AddValue("Keys", keys, typeof(string[]));
            info.AddValue("Values", values, typeof(object[]));
        }

        // ICollection
        public virtual int Count
        {
            get
            {
                return m_ItemsArray.Count;
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return this; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)Keys).CopyTo(array, index);
        }

        // IDeserializationCallback
        public virtual void OnDeserialization(object sender)
        {
            SerializationInfo info = infoCopy;

            // If a subclass overrides the serialization constructor
            // and inplements its own serialization process, infoCopy will
            // be null and we can ignore this callback.
            if (info == null)
                return;

            infoCopy = null;

            m_comparer = (IComparer)info.GetValue("Comparer", typeof(IComparer));
            if (m_comparer == null)
                throw new SerializationException("The comparer is null");

            m_readonly = info.GetBoolean("ReadOnly");
            string[] keys = (string[])info.GetValue("Keys", typeof(string[]));
            if (keys == null)
                throw new SerializationException("keys is null");

            object[] values = (object[])info.GetValue("Values", typeof(object[]));
            if (values == null)
                throw new SerializationException("values is null");

            Init();
            int count = keys.Length;
            for (int i = 0; i < count; i++)
                BaseAdd(keys[i], values[i]);
        }

        //--------------- Protected Instance Properties ----------------
        /// <summary>
        /// SDK: Gets or sets a value indicating whether the NameObjectCollectionBase instance is read-only.
        /// </summary>
        protected bool IsReadOnly
        {
            get
            {
                return m_readonly;
            }
            set
            {
                m_readonly = value;
            }
        }

        //--------------- Protected Instance Methods -------------------
        /// <summary>
        /// Adds an Item with the specified key and value into the <see cref="NameObjectCollectionBase"/>NameObjectCollectionBase instance.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected void BaseAdd(string name, object value)
        {
            if (this.IsReadOnly)
                throw new NotSupportedException("Collection is read-only");

            _Item newitem = new _Item(name, value);

            if (name == null)
            {
                //todo: consider nullkey entry
                if (m_NullKeyItem == null)
                    m_NullKeyItem = newitem;
            }
            else
                if (!HasItem(name))
                {
                    m_ItemsContainer.Add(name, newitem);
                }
            m_ItemsArray.Add(newitem);
        }

        protected void BaseClear()
        {
            if (this.IsReadOnly)
                throw new NotSupportedException("Collection is read-only");
            Init();
        }

        /// <summary>
        /// SDK: Gets the value of the entry at the specified index of the NameObjectCollectionBase instance.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected object BaseGet(int index)
        {
            return ((_Item)m_ItemsArray[index]).value;
        }

        /// <summary>
        /// SDK: Gets the value of the first entry with the specified key from the NameObjectCollectionBase instance.
        /// </summary>
        /// <remark>CAUTION: The BaseGet method does not distinguish between a null reference which is returned because the specified key is not found and a null reference which is returned because the value associated with the key is a null reference.</remark>
        /// <param name="name"></param>
        /// <returns></returns>
        protected object BaseGet(string name)
        {
            _Item item = FindFirstMatchedItem(name);
            /// CAUTION: The BaseGet method does not distinguish between a null reference which is returned because the specified key is not found and a null reference which is returned because the value associated with the key is a null reference.
            if (item == null)
                return null;
            else
                return item.value;
        }

        /// <summary>
        /// SDK:Returns a String array that contains all the keys in the NameObjectCollectionBase instance.
        /// </summary>
        /// <returns>A String array that contains all the keys in the NameObjectCollectionBase instance.</returns>
        protected string[] BaseGetAllKeys()
        {
            int cnt = m_ItemsArray.Count;
            string[] allKeys = new string[cnt];
            for (int i = 0; i < cnt; i++)
                allKeys[i] = BaseGetKey(i);//((_Item)m_ItemsArray[i]).key;

            return allKeys;
        }

        /// <summary>
        /// SDK: Returns an Object array that contains all the values in the NameObjectCollectionBase instance.
        /// </summary>
        /// <returns>An Object array that contains all the values in the NameObjectCollectionBase instance.</returns>
        protected object[] BaseGetAllValues()
        {
            int cnt = m_ItemsArray.Count;
            object[] allValues = new object[cnt];
            for (int i = 0; i < cnt; i++)
                allValues[i] = BaseGet(i);

            return allValues;
        }

        protected object[] BaseGetAllValues(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("'type' argument can't be null");
            int cnt = m_ItemsArray.Count;
            object[] allValues = (object[])Array.CreateInstance(type, cnt);
            for (int i = 0; i < cnt; i++)
                allValues[i] = BaseGet(i);

            return allValues;
        }

        protected string BaseGetKey(int index)
        {
            return ((_Item)m_ItemsArray[index]).key;
        }

        /// <summary>
        /// Gets a value indicating whether the NameObjectCollectionBase instance contains entries whose keys are not a null reference 
        /// </summary>
        /// <returns>true if the NameObjectCollectionBase instance contains entries whose keys are not a null reference otherwise, false.</returns>
        protected bool BaseHasKeys()
        {
            return (m_ItemsContainer.Count > 0);
        }

        protected void BaseRemove(string name)
        {
            int cnt = 0;
            String key;
            if (this.IsReadOnly)
                throw new NotSupportedException("Collection is read-only");
            if (name != null)
            {
                m_ItemsContainer.Remove(name);
            }
            else
            {
                m_NullKeyItem = null;
            }

            cnt = m_ItemsArray.Count;
            for (int i = 0; i < cnt; )
            {
                key = BaseGetKey(i);
                if (Equals(key, name))
                {
                    m_ItemsArray.RemoveAt(i);
                    cnt--;
                }
                else
                    i++;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <LAME>This function implemented the way Microsoft implemented it - 
        /// item is removed from hashtable and array without considering the case when there are two items with the same key but different values in array.
        /// E.g. if
        /// hashtable is [("Key1","value1")] and array contains [("Key1","value1")("Key1","value2")] then
        /// after RemoveAt(1) the collection will be in following state:
        /// hashtable:[] 
        /// array: [("Key1","value1")] 
        /// It's ok only then the key is uniquely assosiated with the value
        /// To fix it a comparsion of objects stored under the same key in the hashtable and in the arraylist should be added 
        /// </LAME>>
        protected void BaseRemoveAt(int index)
        {
            if (this.IsReadOnly)
                throw new NotSupportedException("Collection is read-only");
            string key = BaseGetKey(index);
            if (key != null)
            {
                // TODO: see LAME description above
                m_ItemsContainer.Remove(key);
            }
            else
                m_NullKeyItem = null;
            m_ItemsArray.RemoveAt(index);
        }

        /// <summary>
        /// SDK: Sets the value of the entry at the specified index of the NameObjectCollectionBase instance.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        protected void BaseSet(int index, object value)
        {
            if (this.IsReadOnly)
                throw new NotSupportedException("Collection is read-only");
            _Item item = (_Item)m_ItemsArray[index];
            item.value = value;
        }

        /// <summary>
        /// Sets the value of the first entry with the specified key in the NameObjectCollectionBase instance, if found; otherwise, adds an entry with the specified key and value into the NameObjectCollectionBase instance.
        /// </summary>
        /// <param name="name">The String key of the entry to set. The key can be a null reference </param>
        /// <param name="value">The Object that represents the new value of the entry to set. The value can be a null reference</param>
        protected void BaseSet(string name, object value)
        {
            if (this.IsReadOnly)
                throw new NotSupportedException("Collection is read-only");
            _Item item = FindFirstMatchedItem(name);
            if (item != null)
                item.value = value;
            else
                BaseAdd(name, value);
        }

        private bool HasItem(string name)
        {
            return FindFirstMatchedItem(name) != m_NullKeyItem;
        }

        //[MonoTODO]
        private _Item FindFirstMatchedItem(string name)
        {
            if (name != null)
            {
                object value;
                if (m_ItemsContainer.TryGetValue(name, out value))
                    return (value as _Item) ?? m_NullKeyItem;

                return m_NullKeyItem;
            }
            
            //TODO: consider null key case
            return m_NullKeyItem;
        }

        internal bool Equals(string s1, string s2)
        {
            if (m_comparer != null)
                return (m_comparer.Compare(s1, s2) == 0);
            else
                return equality_comparer.Equals(s1, s2);
        }
    }

	[Serializable]
	public class NameValueCollection : NameObjectCollectionBase
	{
		string[] cachedAllKeys = null;
		string[] cachedAll = null;

		//--------------------- Constructors -----------------------------

		public NameValueCollection () : base ()
		{
		}
		
		public NameValueCollection (int capacity) : base (capacity)
		{
		}
		
		public NameValueCollection (NameValueCollection col) 
            : base(col.EqualityComparer)
		{
			if (col==null)
				throw new ArgumentNullException ("col");		
			Add(col);
		}

		protected NameValueCollection (SerializationInfo info, StreamingContext context)
			:base (info, context)
		{
			
		}

		public NameValueCollection (IEqualityComparer equalityComparer)
			: base (equalityComparer)
		{
		}

		public NameValueCollection (int capacity, IEqualityComparer equalityComparer)
			: base (capacity, equalityComparer)
		{
		}

		public virtual string[] AllKeys 
		{
			get {
				if (cachedAllKeys == null)
					cachedAllKeys = BaseGetAllKeys ();
				return this.cachedAllKeys;
			}
		}
		
		public string this [int index] 
		{
			get{
				return this.Get (index);
			}
		}
		
		public string this [string name] {
			get{
				return this.Get (name);
			}
			set{
				this.Set (name,value);
			}
		}
		
		public void Add (NameValueCollection c)
		{
			if (this.IsReadOnly)
				throw new NotSupportedException ("Collection is read-only");
			if (c == null)
				throw new ArgumentNullException ("c");

// make sense - but it's not the exception thrown
//				throw new ArgumentNullException ();
			
			InvalidateCachedArrays ();
			int max = c.Count;
			for (int i=0; i < max; i++){
				string key = c.GetKey (i);
				string[] values = c.GetValues (i);

				if (values != null && values.Length > 0) {
					foreach (string value in values)
						Add (key, value);
				} else
					Add (key, null);
			}
		}

		/// in SDK doc: If the same value already exists under the same key in the collection, 
		/// it just adds one more value in other words after
		/// <code>
		/// NameValueCollection nvc;
		/// nvc.Add ("LAZY","BASTARD")
		/// nvc.Add ("LAZY","BASTARD")
		/// </code>
		/// nvc.Get ("LAZY") will be "BASTARD,BASTARD" instead of "BASTARD"

		public virtual void Add (string name, string value)
		{
			if (this.IsReadOnly)
				throw new NotSupportedException ("Collection is read-only");
			
			InvalidateCachedArrays ();
			ArrayList values = (ArrayList)BaseGet (name);
			if (values == null){
				values = new ArrayList ();
				if (value != null)
					values.Add (value);
				BaseAdd (name, values);
			}
			else {
				if (value != null)
					values.Add (value);
			}

		}

		public virtual void Clear ()
		{
			if (this.IsReadOnly)
				throw new NotSupportedException ("Collection is read-only");
			InvalidateCachedArrays ();
			BaseClear ();
		}

		public void CopyTo (Array dest, int index)
		{
			if (dest == null)
				throw new ArgumentNullException ("dest", "Null argument - dest");
			if (index < 0)
				throw new ArgumentOutOfRangeException ("index", "index is less than 0");
			if (dest.Rank > 1)
				throw new ArgumentException ("dest", "multidim");

			if (cachedAll == null)
				RefreshCachedAll ();
			try {
				cachedAll.CopyTo (dest, index);
		        } catch (ArrayTypeMismatchException) {
		        	throw new InvalidCastException();
		        }
		}

		private void RefreshCachedAll ()
		{
			this.cachedAll = null;
			int max = this.Count;
			cachedAll = new string [max];
			for (int i = 0; i < max; i++)
				cachedAll [i] = this.Get (i);
		}
		
		public virtual string Get (int index)
		{
			ArrayList values = (ArrayList)BaseGet (index);
			// if index is out of range BaseGet throws an ArgumentOutOfRangeException

			return AsSingleString (values);
		}
		
		public virtual string Get (string name)
		{
			ArrayList values = (ArrayList)BaseGet (name);
			return AsSingleString (values);
		}

		private static string AsSingleString (ArrayList values)
		{
			const char separator = ',';
			
			if (values == null)
				return null;
			int max = values.Count;
			
			switch (max) {
			case 0:
				return null;
			case 1:
				return (string)values [0];
			case 2:
				return String.Concat ((string)values [0], separator, (string)values [1]);
			default:
				int len = max;
				for (int i = 0; i < max; i++)
					len += ((string)values [i]).Length;
				StringBuilder sb = new StringBuilder ((string)values [0], len);
				for (int i = 1; i < max; i++){
					sb.Append (separator);
					sb.Append (values [i]);
				}

				return sb.ToString ();
			}
		}
		
		
		public virtual string GetKey (int index)
		{
			return BaseGetKey (index);
		}
		
		
		public virtual string[] GetValues (int index)
		{
			ArrayList values = (ArrayList)BaseGet (index);
			
			return AsStringArray (values);
		}
		
		
		public virtual string[] GetValues (string name)
		{
			ArrayList values = (ArrayList)BaseGet (name);
			
			return AsStringArray (values);
		}
		
		private static string[] AsStringArray (ArrayList values)
		{
			if (values == null)
				return null;
			int max = values.Count;//get_Count ();
			if (max == 0)
				return null;
			
			string[] valArray = new string[max];
			values.CopyTo (valArray);
			return valArray;
		}
		
		public bool HasKeys ()
		{
			return BaseHasKeys ();
		}
		
		public virtual void Remove (string name)
		{
			if (this.IsReadOnly)
				throw new NotSupportedException ("Collection is read-only");
			InvalidateCachedArrays ();
			BaseRemove (name);
			
		}
		
		public virtual void Set (string name, string value)
		{
			if (this.IsReadOnly)
				throw new NotSupportedException ("Collection is read-only");

			InvalidateCachedArrays ();
			
			ArrayList values = new ArrayList ();
			if (value != null) {
				values.Add (value);
				BaseSet (name,values);
			}
			else {
				// remove all entries
				BaseSet (name, null);
			}
		}
		
		protected void InvalidateCachedArrays ()
		{
			cachedAllKeys = null;
			cachedAll = null;
		}

	}
}

//namespace System.Runtime.Serialization
namespace ServiceStack.Pcl
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text;
    using ServiceStack;

    public sealed class SerializationInfo
    {
        Dictionary<string, SerializationEntry> serialized = new Dictionary<string, SerializationEntry>();
        List<SerializationEntry> values = new List<SerializationEntry>();

        string assemblyName; // the assembly being serialized
        string fullTypeName; // the type being serialized.
#if NET_4_0
        Type objectType;
        bool isAssemblyNameSetExplicit;
        bool isFullTypeNameSetExplicit;
#endif

        IFormatterConverter converter;

        /* used by the runtime */
        private SerializationInfo(Type type)
        {
            assemblyName = type.AssemblyQualifiedName;
            fullTypeName = type.FullName;
            converter = new FormatterConverter();
#if NET_4_0
            objectType = type;
#endif
        }

        /* used by the runtime */
        private SerializationInfo(Type type, SerializationEntry[] data)
        {
            int len = data.Length;

            assemblyName = type.AssemblyQualifiedName;
            fullTypeName = type.FullName;
            converter = new FormatterConverter();
#if NET_4_0
            objectType = type;
#endif

            for (int i = 0; i < len; i++)
            {
                serialized.Add(data[i].Name, data[i]);
                values.Add(data[i]);
            }
        }

        // Constructor
        [CLSCompliant(false)]
        public SerializationInfo(Type type, IFormatterConverter converter)
        {
            if (type == null)
                throw new ArgumentNullException("type", "Null argument");

            if (converter == null)
                throw new ArgumentNullException("converter", "Null argument");

            this.converter = converter;
            assemblyName = type.AssemblyQualifiedName;
            fullTypeName = type.FullName;
#if NET_4_0
                        objectType = type;
#endif
        }

        // Properties
        public string AssemblyName
        {
            get { return assemblyName; }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("Argument is null.");
                assemblyName = value;
#if NET_4_0
                isAssemblyNameSetExplicit = true;
#endif
            }
        }

        public string FullTypeName
        {
            get { return fullTypeName; }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("Argument is null.");
                fullTypeName = value;
#if NET_4_0
                isFullTypeNameSetExplicit = true;
#endif
            }
        }

        public int MemberCount
        {
            get { return serialized.Count; }
        }

#if NET_4_0
        public bool IsAssemblyNameSetExplicit {
                get {
                        return isAssemblyNameSetExplicit;
                }
        }

        public bool IsFullTypeNameSetExplicit {
                get {
                        return isFullTypeNameSetExplicit;
                }
        }

        public Type ObjectType {
                get {
                        return objectType;
                }
        }
#endif

        // Methods
        public void AddValue(string name, object value, Type type)
        {
            if (name == null)
                throw new ArgumentNullException("name is null");
            if (type == null)
                throw new ArgumentNullException("type is null");

            if (serialized.ContainsKey(name))
                throw new SerializationException("Value has been serialized already.");

            SerializationEntry entry = new SerializationEntry(name, type, value);

            serialized.Add(name, entry);
            values.Add(entry);
        }

        public object GetValue(string name, Type type)
        {
            if (name == null)
                throw new ArgumentNullException("name is null.");
            if (type == null)
                throw new ArgumentNullException("type");
            if (!serialized.ContainsKey(name))
                throw new SerializationException("No element named " + name + " could be found.");

            SerializationEntry entry = serialized[name];

#if SL5
            if (entry.Value != null && !type.IsAssignableFrom(entry.Value.GetType()))
#else
            if (entry.Value != null && !type.GetTypeInfo().IsAssignableFrom(entry.Value.GetType().GetTypeInfo()))
#endif
                return converter.Convert(entry.Value, type);
            else
                return entry.Value;
        }

        internal bool HasKey(string name)
        {
            return serialized.ContainsKey(name);
        }

        public void SetType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type is null.");

            fullTypeName = type.FullName;
            assemblyName = type.AssemblyQualifiedName;
#if NET_4_0
            objectType = type;
            isAssemblyNameSetExplicit = false;
            isFullTypeNameSetExplicit = false;
#endif
        }

        public SerializationInfoEnumerator GetEnumerator()
        {
            return new SerializationInfoEnumerator(values);
        }

        public void AddValue(string name, short value)
        {
            AddValue(name, value, typeof(System.Int16));
        }

        [CLSCompliant(false)]
        public void AddValue(string name, UInt16 value)
        {
            AddValue(name, value, typeof(System.UInt16));
        }

        public void AddValue(string name, int value)
        {
            AddValue(name, value, typeof(System.Int32));
        }

        public void AddValue(string name, byte value)
        {
            AddValue(name, value, typeof(System.Byte));
        }

        public void AddValue(string name, bool value)
        {
            AddValue(name, value, typeof(System.Boolean));
        }

        public void AddValue(string name, char value)
        {
            AddValue(name, value, typeof(System.Char));
        }

        [CLSCompliant(false)]
        public void AddValue(string name, SByte value)
        {
            AddValue(name, value, typeof(System.SByte));
        }

        public void AddValue(string name, double value)
        {
            AddValue(name, value, typeof(System.Double));
        }

        public void AddValue(string name, Decimal value)
        {
            AddValue(name, value, typeof(System.Decimal));
        }

        public void AddValue(string name, DateTime value)
        {
            AddValue(name, value, typeof(System.DateTime));
        }

        public void AddValue(string name, float value)
        {
            AddValue(name, value, typeof(System.Single));
        }

        [CLSCompliant(false)]
        public void AddValue(string name, UInt32 value)
        {
            AddValue(name, value, typeof(System.UInt32));
        }

        public void AddValue(string name, long value)
        {
            AddValue(name, value, typeof(System.Int64));
        }

        [CLSCompliant(false)]
        public void AddValue(string name, UInt64 value)
        {
            AddValue(name, value, typeof(System.UInt64));
        }

        public void AddValue(string name, object value)
        {
            if (value == null)
                AddValue(name, value, typeof(System.Object));
            else
                AddValue(name, value, value.GetType());
        }

        public bool GetBoolean(string name)
        {
            object value = GetValue(name, typeof(System.Boolean));
            return converter.ToBoolean(value);
        }

        public byte GetByte(string name)
        {
            object value = GetValue(name, typeof(System.Byte));
            return converter.ToByte(value);
        }

        public char GetChar(string name)
        {
            object value = GetValue(name, typeof(System.Char));
            return converter.ToChar(value);
        }

        public DateTime GetDateTime(string name)
        {
            object value = GetValue(name, typeof(System.DateTime));
            return converter.ToDateTime(value);
        }

        public Decimal GetDecimal(string name)
        {
            object value = GetValue(name, typeof(System.Decimal));
            return converter.ToDecimal(value);
        }

        public double GetDouble(string name)
        {
            object value = GetValue(name, typeof(System.Double));
            return converter.ToDouble(value);
        }

        public short GetInt16(string name)
        {
            object value = GetValue(name, typeof(System.Int16));
            return converter.ToInt16(value);
        }

        public int GetInt32(string name)
        {
            object value = GetValue(name, typeof(System.Int32));
            return converter.ToInt32(value);
        }

        public long GetInt64(string name)
        {
            object value = GetValue(name, typeof(System.Int64));
            return converter.ToInt64(value);
        }

        [CLSCompliant(false)]
        public SByte GetSByte(string name)
        {
            object value = GetValue(name, typeof(System.SByte));
            return converter.ToSByte(value);
        }

        public float GetSingle(string name)
        {
            object value = GetValue(name, typeof(System.Single));
            return converter.ToSingle(value);
        }

        public string GetString(string name)
        {
            object value = GetValue(name, typeof(System.String));
            if (value == null) return null;
            return converter.ToString(value);
        }

        [CLSCompliant(false)]
        public UInt16 GetUInt16(string name)
        {
            object value = GetValue(name, typeof(System.UInt16));
            return converter.ToUInt16(value);
        }

        [CLSCompliant(false)]
        public UInt32 GetUInt32(string name)
        {
            object value = GetValue(name, typeof(System.UInt32));
            return converter.ToUInt32(value);
        }
        [CLSCompliant(false)]
        public UInt64 GetUInt64(string name)
        {
            object value = GetValue(name, typeof(System.UInt64));
            return converter.ToUInt64(value);
        }

        /* used by the runtime */
#pragma warning disable 169
        private SerializationEntry[] get_entries()
        {
            SerializationEntry[] res = new SerializationEntry[this.MemberCount];
            int i = 0;

            foreach (SerializationEntry e in this)
                res[i++] = e;

            return res;
        }
#pragma warning restore 169
    }

    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public struct SerializationEntry
    {
        string name;
        Type objectType;
        object value;

        // Properties
        public string Name
        {
            get { return name; }
        }

        public Type ObjectType
        {
            get { return objectType; }
        }

        public object Value
        {
            get { return value; }
        }

        internal SerializationEntry(string name, Type type, object value)
        {
            this.name = name;
            this.objectType = type;
            this.value = value;
        }
    }

    public interface IFormatterConverter
    {
        object Convert(object value, Type type);
        //object Convert(object value, TypeCode typeCode);

        bool ToBoolean(object value);
        byte ToByte(object value);
        char ToChar(object value);
        DateTime ToDateTime(object value);
        Decimal ToDecimal(object value);
        double ToDouble(object value);
        Int16 ToInt16(object value);
        Int32 ToInt32(object value);
        Int64 ToInt64(object value);
        sbyte ToSByte(object value);
        float ToSingle(object value);
        string ToString(object value);
        UInt16 ToUInt16(object value);
        UInt32 ToUInt32(object value);
        UInt64 ToUInt64(object value);
    }

    public sealed class SerializationInfoEnumerator : IEnumerator
    {
        IEnumerator enumerator;

        // Constructor
        internal SerializationInfoEnumerator(IEnumerable list)
        {
            this.enumerator = list.GetEnumerator();
        }

        // Properties
        public SerializationEntry Current
        {
            get { return (SerializationEntry)enumerator.Current; }
        }

        object IEnumerator.Current
        {
            get { return enumerator.Current; }
        }

        public string Name
        {
            get { return this.Current.Name; }
        }

        public Type ObjectType
        {
            get { return this.Current.ObjectType; }
        }

        public object Value
        {
            get { return this.Current.Value; }
        }

        // Methods
        public bool MoveNext()
        {
            return enumerator.MoveNext();
        }

        public void Reset()
        {
            enumerator.Reset();
        }
    }

    public class FormatterConverter : IFormatterConverter
    {

        public FormatterConverter()
        {
        }

        public object Convert(object value, Type type)
        {
            return System.Convert.ChangeType(value, type, null);
        }

        //public object Convert(object value, TypeCode typeCode)
        //{
        //    return System.Convert.ChangeType(value, typeCode);
        //}

        public bool ToBoolean(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToBoolean(value);
        }

        public byte ToByte(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToByte(value);
        }

        public char ToChar(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToChar(value);
        }

        public DateTime ToDateTime(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToDateTime(value);
        }

        public decimal ToDecimal(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToDecimal(value);
        }

        public double ToDouble(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToDouble(value);
        }

        public short ToInt16(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToInt16(value);
        }

        public int ToInt32(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToInt32(value);
        }

        public long ToInt64(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToInt64(value);
        }

        public float ToSingle(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToSingle(value);
        }

        public string ToString(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToString(value);
        }

        [CLSCompliant(false)]
        public sbyte ToSByte(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToSByte(value);
        }

        [CLSCompliant(false)]
        public ushort ToUInt16(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToUInt16(value);
        }

        [CLSCompliant(false)]
        public uint ToUInt32(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToUInt32(value);
        }

        [CLSCompliant(false)]
        public ulong ToUInt64(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value is null.");

            return System.Convert.ToUInt64(value);
        }
    }
}

#endif

//Dummy namespaces
namespace System.Collections.Specialized {}
namespace System.Web {}
namespace ServiceStack.Pcl {}

namespace ServiceStack
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using ServiceStack.Web;
    using ServiceStack.Pcl;
    using System.Collections.Specialized;

    public class PclExportClient 
    {
        public static PclExportClient Instance
#if PCL
          /*attempts to be inferred otherwise needs to be set explicitly by host project*/
#elif SL5
          = Sl5PclExportClient.Configure()
#elif NETFX_CORE
          = WinStorePclExportClient.Configure()
#elif WP
          = WpPclExportClient.Configure()
#elif XBOX
          = XboxPclExportClient.Configure()
#elif __IOS__
          = IosPclExportClient.Configure()
#elif ANDROID
          = AndroidPclExportClient.Configure()
#else
          = Net40PclExportClient.Configure()
#endif
        ;

        public static readonly Task<object> EmptyTask;

        static PclExportClient()
        {
            if (Instance != null) 
                return;

            try
            {
                if (ConfigureProvider("ServiceStack.IosPclExportClient, ServiceStack.Pcl.iOS"))
                    return;
                if (ConfigureProvider("ServiceStack.AndroidPclExportClient, ServiceStack.Pcl.Android"))
                    return;
                if (ConfigureProvider("ServiceStack.WinStorePclExportClient, ServiceStack.Pcl.WinStore"))
                    return;
                if (ConfigureProvider("ServiceStack.Net40PclExportClient, ServiceStack.Pcl.Net45"))
                    return;
            }
            catch (Exception /*ignore*/) {}

            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            EmptyTask = tcs.Task;
        }

        public static bool ConfigureProvider(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null)
                return false;

            var mi = type.GetMethod("Configure");
            if (mi != null)
            {
                mi.Invoke(null, new object[0]);
            }

            return true;
        }

        public virtual INameValueCollection NewNameValueCollection()
        {
            return new NameValueCollectionWrapper(new NameValueCollection());
        }

        public virtual INameValueCollection ParseQueryString(string query)
        {
#if SL5 || PCL
            return ServiceStack.Pcl.HttpUtility.ParseQueryString(query).InWrapper();
#else
			return System.Web.HttpUtility.ParseQueryString(query).InWrapper();
#endif
        }

        public virtual string UrlEncode(string url)
        {
#if SL5
            return System.Windows.Browser.HttpUtility.UrlEncode(url);
#elif PCL
            return WebUtility.UrlEncode(url);
#else
            return System.Web.HttpUtility.UrlEncode(url);
#endif
        }

        public virtual string UrlDecode(string url)
        {
#if SL5
            return System.Windows.Browser.HttpUtility.UrlDecode(url);
#elif PCL
            return WebUtility.UrlDecode(url);
#else
            return System.Web.HttpUtility.UrlDecode(url);
#endif
        }

        public virtual string HtmlEncode(string html)
        {
#if SL5
            return System.Windows.Browser.HttpUtility.HtmlEncode(html);
#elif PCL
            return WebUtility.HtmlEncode(html);
#else
            return System.Web.HttpUtility.HtmlEncode(html);
#endif
        }

        public virtual string HtmlDecode(string html)
        {
#if SL5
            return System.Windows.Browser.HttpUtility.HtmlDecode(html);
#elif PCL
            return WebUtility.HtmlDecode(html);
#else
            return System.Web.HttpUtility.HtmlDecode(html);
#endif
        }
 
        public virtual void AddHeader(WebRequest webReq, INameValueCollection headers)
        {
            foreach (var name in headers.AllKeys)
            {
                webReq.Headers[name] = headers[name];
            }
        }

        public virtual string GetHeader(WebHeaderCollection headers, string name, Func<string, bool> valuePredicate)
        {
            return null;
        }

        public virtual void SetCookieContainer(HttpWebRequest webRequest, ServiceClientBase client)
        {
            webRequest.CookieContainer = client.CookieContainer;
        }

        public virtual void SetCookieContainer(HttpWebRequest webRequest, AsyncServiceClient client)
        {
            webRequest.CookieContainer = client.CookieContainer;
        }

        public virtual void SynchronizeCookies(AsyncServiceClient client)
        {
        }

        public virtual ITimer CreateTimer(TimerCallback cb, TimeSpan timeOut, object state)
        {
#if PCL
            return new Timer(cb, state, (int)timeOut.TotalMilliseconds);
#else
            return new AsyncTimer(new
                System.Threading.Timer(cb, state, (int)timeOut.TotalMilliseconds, Timeout.Infinite));
#endif
        }

        public virtual Task WaitAsync(int waitForMs)
        {
#if PCL
            return EmptyTask;
#else
            var tcs = new TaskCompletionSource<object>();
            Thread.Sleep(waitForMs);
            tcs.SetResult(null);
            return tcs.Task;
#endif
        }

        public virtual void RunOnUiThread(Action fn)
        {
            if (UiContext == null)
            {
                fn();
            }
            else
            {
                UiContext.Post(_ => fn(), null);
            }
        }

        public SynchronizationContext UiContext;
        public static void Configure(PclExportClient instance)
        {
            Instance = instance;
            Instance.UiContext = SynchronizationContext.Current;
        }
    }

#if PCL
    public delegate void TimerCallback(object state);

    public sealed class Timer : CancellationTokenSource, ITimer, IDisposable
    {
        public Timer(TimerCallback callback, object state, int dueTime)
        {
            Task.Delay(dueTime, Token).ContinueWith((t, s) =>
            {
                var tuple = (Tuple<TimerCallback, object>)s;
                tuple.Item1(tuple.Item2);
            }, Tuple.Create(callback, state), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

        public void Cancel()
        {
            base.Cancel();
        }
    }
#endif

}
