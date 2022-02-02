namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing a dictionary value. Can be used on any process and thread.
    /// </summary>
    public sealed unsafe partial class CefDictionaryValue
    {
        /// <summary>
        /// Creates a new object that is not owned by any other object.
        /// </summary>
        public static CefDictionaryValue Create()
        {
            return CefDictionaryValue.FromNative(
                cef_dictionary_value_t.create()
                );
        }

        /// <summary>
        /// Returns true if this object is valid. This object may become invalid if
        /// the underlying data is owned by another object (e.g. list or dictionary)
        /// and that other object is then modified or destroyed. Do not call any other
        /// methods if this method returns false.
        /// </summary>
        public bool IsValid
        {
            get { return cef_dictionary_value_t.is_valid(_self) != 0; }
        }

        /// <summary>
        /// Returns true if this object is currently owned by another object.
        /// </summary>
        public bool IsOwned
        {
            get { return cef_dictionary_value_t.is_owned(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the values of this object are read-only. Some APIs may
        /// expose read-only objects.
        /// </summary>
        public bool IsReadOnly
        {
            get { return cef_dictionary_value_t.is_read_only(_self) != 0; }
        }

        /// <summary>
        /// Returns true if this object and |that| object have the same underlying
        /// data. If true modifications to this object will also affect |that| object
        /// and vice-versa.
        /// </summary>
        public bool IsSame(CefDictionaryValue that)
        {
            return cef_dictionary_value_t.is_same(_self, that.ToNative()) != 0;
        }

        /// <summary>
        /// Returns true if this object and |that| object have an equivalent underlying
        /// value but are not necessarily the same object.
        /// </summary>
        public bool IsEqual(CefDictionaryValue that)
        {
            return cef_dictionary_value_t.is_equal(_self, that.ToNative()) != 0;
        }

        /// <summary>
        /// Returns a writable copy of this object. If |exclude_empty_children| is true
        /// any empty dictionaries or lists will be excluded from the copy.
        /// </summary>
        public CefDictionaryValue Copy(bool excludeEmptyChildren)
        {
            return CefDictionaryValue.FromNative(
                cef_dictionary_value_t.copy(_self, excludeEmptyChildren ? 1 : 0)
                );
        }

        /// <summary>
        /// Returns the number of values.
        /// </summary>
        public int Count
        {
            get { return (int)cef_dictionary_value_t.get_size(_self); }
        }

        /// <summary>
        /// Removes all values. Returns true on success.
        /// </summary>
        public bool Clear()
        {
            return cef_dictionary_value_t.clear(_self) != 0;
        }

        /// <summary>
        /// Returns true if the current dictionary has a value for the given key.
        /// </summary>
        public bool HasKey(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);

                return cef_dictionary_value_t.has_key(_self, &n_key) != 0;
            }
        }

        /// <summary>
        /// Reads all keys for this dictionary into the specified vector.
        /// </summary>
        public string[] GetKeys()
        {
            var list = libcef.string_list_alloc();
            var success = cef_dictionary_value_t.get_keys(_self, list) != 0;
            if (!success) throw new InvalidOperationException(); // TODO: use ExceptionBuilder
            var result = cef_string_list.ToArray(list);
            libcef.string_list_free(list);
            return result;
        }

        /// <summary>
        /// Removes the value at the specified key. Returns true is the value was
        /// removed successfully.
        /// </summary>
        public bool Remove(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.remove(_self, &n_key) != 0;
            }
        }

        /// <summary>
        /// Returns the value type for the specified key.
        /// </summary>
        public CefValueType GetValueType(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.get_type(_self, &n_key);
            }
        }

        /// <summary>
        /// Returns the value at the specified key. For simple types the returned
        /// value will copy existing data and modifications to the value will not
        /// modify this object. For complex types (binary, dictionary and list) the
        /// returned value will reference existing data and modifications to the value
        /// will modify this object.
        /// </summary>
        public CefValue GetValue(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);

                var n_result = cef_dictionary_value_t.get_value(_self, &n_key);

                return CefValue.FromNativeOrNull(n_result);
            }
        }

        /// <summary>
        /// Returns the value at the specified key as type bool.
        /// </summary>
        public bool GetBool(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.get_bool(_self, &n_key) != 0;
            }
        }

        /// <summary>
        /// Returns the value at the specified key as type int.
        /// </summary>
        public int GetInt(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.get_int(_self, &n_key);
            }
        }

        /// <summary>
        /// Returns the value at the specified key as type double.
        /// </summary>
        public double GetDouble(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.get_double(_self, &n_key);
            }
        }

        /// <summary>
        /// Returns the value at the specified key as type string.
        /// </summary>
        public string GetString(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                var n_result = cef_dictionary_value_t.get_string(_self, &n_key);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the value at the specified key as type binary. The returned
        /// value will reference existing data.
        /// </summary>
        public CefBinaryValue GetBinary(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                var n_result = cef_dictionary_value_t.get_binary(_self, &n_key);
                return CefBinaryValue.FromNative(n_result);
            }
        }

        /// <summary>
        /// Returns the value at the specified key as type dictionary. The returned
        /// value will reference existing data and modifications to the value will
        /// modify this object.
        /// </summary>
        public CefDictionaryValue GetDictionary(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                var n_result = cef_dictionary_value_t.get_dictionary(_self, &n_key);
                return CefDictionaryValue.FromNative(n_result);
            }
        }

        /// <summary>
        /// Returns the value at the specified key as type list. The returned value
        /// will reference existing data and modifications to the value will modify
        /// this object.
        /// </summary>
        public CefListValue GetList(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                var n_result = cef_dictionary_value_t.get_list(_self, &n_key);
                return CefListValue.FromNative(n_result);
            }
        }

        /// <summary>
        /// Sets the value at the specified key. Returns true if the value was set
        /// successfully. If |value| represents simple data then the underlying data
        /// will be copied and modifications to |value| will not modify this object. If
        /// |value| represents complex data (binary, dictionary or list) then the
        /// underlying data will be referenced and modifications to |value| will modify
        /// this object.
        /// </summary>
        public bool SetValue(string key, CefValue value)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                var n_value = value.ToNative();

                return cef_dictionary_value_t.set_value(_self, &n_key, n_value) != 0;
            }
        }

        /// <summary>
        /// Sets the value at the specified key as type null. Returns true if the
        /// value was set successfully.
        /// </summary>
        public bool SetNull(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.set_null(_self, &n_key) != 0;
            }
        }

        /// <summary>
        /// Sets the value at the specified key as type bool. Returns true if the
        /// value was set successfully.
        /// </summary>
        public bool SetBool(string key, bool value)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.set_bool(_self, &n_key, value ? 1 : 0) != 0;
            }
        }

        /// <summary>
        /// Sets the value at the specified key as type int. Returns true if the
        /// value was set successfully.
        /// </summary>
        public bool SetInt(string key, int value)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.set_int(_self, &n_key, value) != 0;
            }
        }

        /// <summary>
        /// Sets the value at the specified key as type double. Returns true if the
        /// value was set successfully.
        /// </summary>
        public bool SetDouble(string key, double value)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.set_double(_self, &n_key, value) != 0;
            }
        }

        /// <summary>
        /// Sets the value at the specified key as type string. Returns true if the
        /// value was set successfully.
        /// </summary>
        public bool SetString(string key, string value)
        {
            fixed (char* key_str = key)
            fixed (char* value_str = value)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                var n_value = new cef_string_t(value_str, value != null ? value.Length : 0);
                return cef_dictionary_value_t.set_string(_self, &n_key, &n_value) != 0;
            }
        }

        /// <summary>
        /// Sets the value at the specified key as type binary. Returns true if the
        /// value was set successfully. If |value| is currently owned by another object
        /// then the value will be copied and the |value| reference will not change.
        /// Otherwise, ownership will be transferred to this object and the |value|
        /// reference will be invalidated.
        /// </summary>
        public bool SetBinary(string key, CefBinaryValue value)
        {
            //FIXME: what means reference will be invalidated ?
            if (value == null) throw new ArgumentNullException("value");

            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.set_binary(_self, &n_key, value.ToNative()) != 0;
            }
        }

        /// <summary>
        /// Sets the value at the specified key as type dict. Returns true if the
        /// value was set successfully. If |value| is currently owned by another object
        /// then the value will be copied and the |value| reference will not change.
        /// Otherwise, ownership will be transferred to this object and the |value|
        /// reference will be invalidated.
        /// </summary>
        public bool SetDictionary(string key, CefDictionaryValue value)
        {
            if (value == null) throw new ArgumentNullException("value");

            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.set_dictionary(_self, &n_key, value.ToNative()) != 0;
            }
        }

        /// <summary>
        /// Sets the value at the specified key as type list. Returns true if the
        /// value was set successfully. If |value| is currently owned by another object
        /// then the value will be copied and the |value| reference will not change.
        /// Otherwise, ownership will be transferred to this object and the |value|
        /// reference will be invalidated.
        /// </summary>
        public bool SetList(string key, CefListValue value)
        {
            if (value == null) throw new ArgumentNullException("value");

            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_dictionary_value_t.set_list(_self, &n_key, value.ToNative()) != 0;
            }
        }
    }
}
