namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class that wraps other data value types. Complex types (binary, dictionary
    /// and list) will be referenced but not owned by this object. Can be used on any
    /// process and thread.
    /// </summary>
    public sealed unsafe partial class CefValue
    {
        /// <summary>
        /// Creates a new object.
        /// </summary>
        public static CefValue Create()
        {
            return CefValue.FromNative(cef_value_t.create());
        }

        /// <summary>
        /// Returns true if the underlying data is valid. This will always be true for
        /// simple types. For complex types (binary, dictionary and list) the
        /// underlying data may become invalid if owned by another object (e.g. list or
        /// dictionary) and that other object is then modified or destroyed. This value
        /// object can be re-used by calling Set*() even if the underlying data is
        /// invalid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return cef_value_t.is_valid(_self) != 0;
            }
        }

        /// <summary>
        /// Returns true if the underlying data is owned by another object.
        /// </summary>
        public bool IsOwned
        {
            get
            {
                return cef_value_t.is_owned(_self) != 0;
            }
        }

        /// <summary>
        /// Returns true if the underlying data is read-only. Some APIs may expose
        /// read-only objects.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return cef_value_t.is_read_only(_self) != 0;
            }
        }

        /// <summary>
        /// Returns true if this object and |that| object have the same underlying
        /// data. If true modifications to this object will also affect |that| object
        /// and vice-versa.
        /// </summary>
        public bool IsSame(CefValue that)
        {
            return cef_value_t.is_same(_self, that.ToNative()) != 0;
        }

        /// <summary>
        /// Returns true if this object and |that| object have an equivalent underlying
        /// value but are not necessarily the same object.
        /// </summary>
        public bool IsEqual(CefValue that)
        {
            return cef_value_t.is_equal(_self, that.ToNative()) != 0;
        }

        /// <summary>
        /// Returns a copy of this object. The underlying data will also be copied.
        /// </summary>
        public CefValue Copy()
        {
            return CefValue.FromNative(
                cef_value_t.copy(_self)
                );
        }

        /// <summary>
        /// Returns the underlying value type.
        /// </summary>
        public CefValueType GetValueType()
        {
            return cef_value_t.get_type(_self);
        }

        /// <summary>
        /// Returns the underlying value as type bool.
        /// </summary>
        public bool GetBool()
        {
            return cef_value_t.get_bool(_self) != 0;
        }

        /// <summary>
        /// Returns the underlying value as type int.
        /// </summary>
        public int GetInt()
        {
            return cef_value_t.get_int(_self);
        }

        /// <summary>
        /// Returns the underlying value as type double.
        /// </summary>
        public double GetDouble()
        {
            return cef_value_t.get_double(_self);
        }

        /// <summary>
        /// Returns the underlying value as type string.
        /// </summary>
        public string GetString()
        {
            var n_result = cef_value_t.get_string(_self);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Returns the underlying value as type binary. The returned reference may
        /// become invalid if the value is owned by another object or if ownership is
        /// transferred to another object in the future. To maintain a reference to
        /// the value after assigning ownership to a dictionary or list pass this
        /// object to the SetValue() method instead of passing the returned reference
        /// to SetBinary().
        /// </summary>
        public CefBinaryValue GetBinary()
        {
            return CefBinaryValue.FromNative(
                cef_value_t.get_binary(_self)
                );
        }

        /// <summary>
        /// Returns the underlying value as type dictionary. The returned reference may
        /// become invalid if the value is owned by another object or if ownership is
        /// transferred to another object in the future. To maintain a reference to
        /// the value after assigning ownership to a dictionary or list pass this
        /// object to the SetValue() method instead of passing the returned reference
        /// to SetDictionary().
        /// </summary>
        public CefDictionaryValue GetDictionary()
        {
            return CefDictionaryValue.FromNative(
                cef_value_t.get_dictionary(_self)
                );
        }

        /// <summary>
        /// Returns the underlying value as type list. The returned reference may
        /// become invalid if the value is owned by another object or if ownership is
        /// transferred to another object in the future. To maintain a reference to
        /// the value after assigning ownership to a dictionary or list pass this
        /// object to the SetValue() method instead of passing the returned reference
        /// to SetList().
        /// </summary>
        public CefListValue GetList()
        {
            return CefListValue.FromNative(
                cef_value_t.get_list(_self)
                );
        }

        /// <summary>
        /// Sets the underlying value as type null. Returns true if the value was set
        /// successfully.
        /// </summary>
        public bool SetNull()
        {
            return cef_value_t.set_null(_self) != 0;
        }

        /// <summary>
        /// Sets the underlying value as type bool. Returns true if the value was set
        /// successfully.
        /// </summary>
        public bool SetBool(bool value)
        {
            return cef_value_t.set_bool(_self, value ? 1 : 0) != 0;
        }

        /// <summary>
        /// Sets the underlying value as type int. Returns true if the value was set
        /// successfully.
        /// </summary>
        public bool SetInt(int value)
        {
            return cef_value_t.set_int(_self, value) != 0;
        }

        /// <summary>
        /// Sets the underlying value as type double. Returns true if the value was set
        /// successfully.
        /// </summary>
        public bool SetDouble(double value)
        {
            return cef_value_t.set_double(_self, value) != 0;
        }

        /// <summary>
        /// Sets the underlying value as type string. Returns true if the value was set
        /// successfully.
        /// </summary>
        public bool SetString(string value)
        {
            fixed (char* value_str = value)
            {
                var n_value = new cef_string_t(value_str, value != null ? value.Length : 0);

                return cef_value_t.set_string(_self, &n_value) != 0;
            }
        }

        /// <summary>
        /// Sets the underlying value as type binary. Returns true if the value was set
        /// successfully. This object keeps a reference to |value| and ownership of the
        /// underlying data remains unchanged.
        /// </summary>
        public bool SetBinary(CefBinaryValue value)
        {
            return cef_value_t.set_binary(_self, value.ToNative()) != 0;
        }

        /// <summary>
        /// Sets the underlying value as type dict. Returns true if the value was set
        /// successfully. This object keeps a reference to |value| and ownership of the
        /// underlying data remains unchanged.
        /// </summary>
        public bool SetDictionary(CefDictionaryValue value)
        {
            return cef_value_t.set_dictionary(_self, value.ToNative()) != 0;
        }

        /// <summary>
        /// Sets the underlying value as type list. Returns true if the value was set
        /// successfully. This object keeps a reference to |value| and ownership of the
        /// underlying data remains unchanged.
        /// </summary>
        public bool SetList(CefListValue value)
        {
            return cef_value_t.set_list(_self, value.ToNative()) != 0;
        }
    }
}
