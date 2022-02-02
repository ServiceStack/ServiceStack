namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing a V8 value handle. V8 handles can only be accessed from
    /// the thread on which they are created. Valid threads for creating a V8 handle
    /// include the render process main thread (TID_RENDERER) and WebWorker threads.
    /// A task runner for posting tasks on the associated thread can be retrieved via
    /// the CefV8Context::GetTaskRunner() method.
    /// </summary>
    public sealed unsafe partial class CefV8Value
    {
        /// <summary>
        /// Create a new CefV8Value object of type undefined.
        /// </summary>
        public static CefV8Value CreateUndefined()
        {
            return CefV8Value.FromNative(
                cef_v8value_t.create_undefined()
                );
        }

        /// <summary>
        /// Create a new CefV8Value object of type null.
        /// </summary>
        public static CefV8Value CreateNull()
        {
            return CefV8Value.FromNative(
                cef_v8value_t.create_null()
                );
        }

        /// <summary>
        /// Create a new CefV8Value object of type bool.
        /// </summary>
        public static CefV8Value CreateBool(bool value)
        {
            return CefV8Value.FromNative(
                cef_v8value_t.create_bool(value ? 1 : 0)
                );
        }

        /// <summary>
        /// Create a new CefV8Value object of type int.
        /// </summary>
        public static CefV8Value CreateInt(int value)
        {
            return CefV8Value.FromNative(
                cef_v8value_t.create_int(value)
                );
        }

        /// <summary>
        /// Create a new CefV8Value object of type unsigned int.
        /// </summary>
        public static CefV8Value CreateUInt(uint value)
        {
            return CefV8Value.FromNative(
                cef_v8value_t.create_uint(value)
                );
        }

        /// <summary>
        /// Create a new CefV8Value object of type double.
        /// </summary>
        public static CefV8Value CreateDouble(double value)
        {
            return CefV8Value.FromNative(
                cef_v8value_t.create_double(value)
                );
        }

        /// <summary>
        /// Create a new CefV8Value object of type Date. This method should only be
        /// called from within the scope of a CefRenderProcessHandler, CefV8Handler or
        /// CefV8Accessor callback, or in combination with calling Enter() and Exit()
        /// on a stored CefV8Context reference.
        /// </summary>
        public static CefV8Value CreateDate(DateTime value)
        {
            var n_value = new cef_time_t(value);
            return CefV8Value.FromNative(
                cef_v8value_t.create_date(&n_value)
                );
        }

        /// <summary>
        /// Create a new CefV8Value object of type string.
        /// </summary>
        public static CefV8Value CreateString(string value)
        {
            fixed (char* value_str = value)
            {
                var n_value = new cef_string_t(value_str, value != null ? value.Length : 0);
                return CefV8Value.FromNative(
                    cef_v8value_t.create_string(&n_value)
                    );
            }
        }

        /// <summary>
        /// Create a new CefV8Value object of type object with optional accessor and/or
        /// interceptor. This method should only be called from within the scope of a
        /// CefRenderProcessHandler, CefV8Handler or CefV8Accessor callback, or in
        /// combination with calling Enter() and Exit() on a stored CefV8Context
        /// reference.
        /// </summary>
        public static CefV8Value CreateObject(CefV8Accessor accessor = null, CefV8Interceptor interceptor = null)
        {
            return CefV8Value.FromNative(
                cef_v8value_t.create_object(
                    accessor != null ? accessor.ToNative() : null,
                    interceptor != null ? interceptor.ToNative() : null
                    )
                );
        }

        /// <summary>
        /// Create a new CefV8Value object of type array with the specified |length|.
        /// If |length| is negative the returned array will have length 0. This method
        /// should only be called from within the scope of a CefRenderProcessHandler,
        /// CefV8Handler or CefV8Accessor callback, or in combination with calling
        /// Enter() and Exit() on a stored CefV8Context reference.
        /// </summary>
        public static CefV8Value CreateArray(int length)
        {
            return CefV8Value.FromNative(
                cef_v8value_t.create_array(length)
                );
        }

        /// <summary>
        /// Create a new CefV8Value object of type ArrayBuffer which wraps the provided
        /// |buffer| of size |length| bytes. The ArrayBuffer is externalized, meaning
        /// that it does not own |buffer|. The caller is responsible for freeing
        /// |buffer| when requested via a call to CefV8ArrayBufferReleaseCallback::
        /// ReleaseBuffer. This method should only be called from within the scope of a
        /// CefRenderProcessHandler, CefV8Handler or CefV8Accessor callback, or in
        /// combination with calling Enter() and Exit() on a stored CefV8Context
        /// reference.
        /// </summary>
        public static CefV8Value CreateArrayBuffer(IntPtr buffer, ulong length, CefV8ArrayBufferReleaseCallback releaseCallback)
        {
            if (releaseCallback == null) throw new ArgumentNullException(nameof(releaseCallback));

            var n_value = cef_v8value_t.create_array_buffer(
                (void*)buffer,
                checked((UIntPtr)length),
                releaseCallback.ToNative()
                );

            return CefV8Value.FromNative(n_value);
        }

        /// <summary>
        /// Create a new CefV8Value object of type function. This method should only be
        /// called from within the scope of a CefRenderProcessHandler, CefV8Handler or
        /// CefV8Accessor callback, or in combination with calling Enter() and Exit()
        /// on a stored CefV8Context reference.
        /// </summary>
        public static CefV8Value CreateFunction(string name, CefV8Handler handler)
        {
            fixed (char* name_str = name)
            {
                var n_name = new cef_string_t(name_str, name != null ? name.Length : 0);

                return CefV8Value.FromNative(
                    cef_v8value_t.create_function(&n_name, handler.ToNative())
                    );
            }
        }

        /// <summary>
        /// Returns true if the underlying handle is valid and it can be accessed on
        /// the current thread. Do not call any other methods if this method returns
        /// false.
        /// </summary>
        public bool IsValid
        {
            get { return cef_v8value_t.is_valid(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is undefined.
        /// </summary>
        public bool IsUndefined
        {
            get { return cef_v8value_t.is_undefined(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is null.
        /// </summary>
        public bool IsNull
        {
            get { return cef_v8value_t.is_null(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is bool.
        /// </summary>
        public bool IsBool
        {
            get { return cef_v8value_t.is_bool(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is int.
        /// </summary>
        public bool IsInt
        {
            get { return cef_v8value_t.is_int(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is unsigned int.
        /// </summary>
        public bool IsUInt
        {
            get { return cef_v8value_t.is_uint(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is double.
        /// </summary>
        public bool IsDouble
        {
            get { return cef_v8value_t.is_double(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is Date.
        /// </summary>
        public bool IsDate
        {
            get { return cef_v8value_t.is_date(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is string.
        /// </summary>
        public bool IsString
        {
            get { return cef_v8value_t.is_string(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is object.
        /// </summary>
        public bool IsObject
        {
            get { return cef_v8value_t.is_object(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is array.
        /// </summary>
        public bool IsArray
        {
            get { return cef_v8value_t.is_array(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is an ArrayBuffer.
        /// </summary>
        public bool IsArrayBuffer
        {
            get { return cef_v8value_t.is_array_buffer(_self) != 0; }
        }

        /// <summary>
        /// True if the value type is function.
        /// </summary>
        public bool IsFunction
        {
            get { return cef_v8value_t.is_function(_self) != 0; }
        }

        /// <summary>
        /// Returns true if this object is pointing to the same handle as |that|
        /// object.
        /// </summary>
        public bool IsSame(CefV8Value that)
        {
            if (that == null) return false;

            return cef_v8value_t.is_same(_self, that.ToNative()) != 0;
        }

        /// <summary>
        /// Return a bool value.
        /// </summary>
        public bool GetBoolValue()
        {
            return cef_v8value_t.get_bool_value(_self) != 0;
        }

        /// <summary>
        /// Return an int value.
        /// </summary>
        public int GetIntValue()
        {
            return cef_v8value_t.get_int_value(_self);
        }

        /// <summary>
        /// Return an unsigned int value.
        /// </summary>
        public uint GetUIntValue()
        {
            return cef_v8value_t.get_uint_value(_self);
        }

        /// <summary>
        /// Return a double value.
        /// </summary>
        public double GetDoubleValue()
        {
            return cef_v8value_t.get_double_value(_self);
        }

        /// <summary>
        /// Return a Date value.
        /// </summary>
        public DateTime GetDateValue()
        {
            var value = cef_v8value_t.get_date_value(_self);
            return cef_time_t.ToDateTime(&value);
        }

        /// <summary>
        /// Return a string value.
        /// </summary>
        public string GetStringValue()
        {
            var n_result = cef_v8value_t.get_string_value(_self);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// OBJECT METHODS - These methods are only available on objects. Arrays and
        /// functions are also objects. String- and integer-based keys can be used
        /// interchangably with the framework converting between them as necessary.
        /// Returns true if this is a user created object.
        /// </summary>
        public bool IsUserCreated
        {
            get { return cef_v8value_t.is_user_created(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the last method call resulted in an exception. This
        /// attribute exists only in the scope of the current CEF value object.
        /// </summary>
        public bool HasException
        {
            get { return cef_v8value_t.has_exception(_self) != 0; }
        }

        /// <summary>
        /// Returns the exception resulting from the last method call. This attribute
        /// exists only in the scope of the current CEF value object.
        /// </summary>
        public CefV8Exception GetException()
        {
            return CefV8Exception.FromNativeOrNull(
                cef_v8value_t.get_exception(_self)
                );
        }

        /// <summary>
        /// Clears the last exception and returns true on success.
        /// </summary>
        public bool ClearException()
        {
            return cef_v8value_t.clear_exception(_self) != 0;
        }

        /// <summary>
        /// Returns true if this object will re-throw future exceptions. This attribute
        /// exists only in the scope of the current CEF value object.
        /// </summary>
        public bool WillRethrowExceptions()
        {
            return cef_v8value_t.will_rethrow_exceptions(_self) != 0;
        }

        /// <summary>
        /// Set whether this object will re-throw future exceptions. By default
        /// exceptions are not re-thrown. If a exception is re-thrown the current
        /// context should not be accessed again until after the exception has been
        /// caught and not re-thrown. Returns true on success. This attribute exists
        /// only in the scope of the current CEF value object.
        /// </summary>
        public bool SetRethrowExceptions(bool rethrow)
        {
            return cef_v8value_t.set_rethrow_exceptions(_self, rethrow ? 1 : 0) != 0;
        }

        /// <summary>
        /// Returns true if the object has a value with the specified identifier.
        /// </summary>
        public bool HasValue(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_v8value_t.has_value_bykey(_self, &n_key) != 0;
            }
        }

        /// <summary>
        /// Returns true if the object has a value with the specified identifier.
        /// </summary>
        public bool HasValue(int index)
        {
            return cef_v8value_t.has_value_byindex(_self, index) != 0;
        }

        /// <summary>
        /// Deletes the value with the specified identifier and returns true on
        /// success. Returns false if this method is called incorrectly or an exception
        /// is thrown. For read-only and don't-delete values this method will return
        /// true even though deletion failed.
        /// </summary>
        public bool DeleteValue(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_v8value_t.delete_value_bykey(_self, &n_key) != 0;
            }
        }

        /// <summary>
        /// Deletes the value with the specified identifier and returns true on
        /// success. Returns false if this method is called incorrectly, deletion fails
        /// or an exception is thrown. For read-only and don't-delete values this
        /// method will return true even though deletion failed.
        /// </summary>
        public bool DeleteValue(int index)
        {
            return cef_v8value_t.delete_value_byindex(_self, index) != 0;
        }

        /// <summary>
        /// Returns the value with the specified identifier on success. Returns NULL
        /// if this method is called incorrectly or an exception is thrown.
        /// </summary>
        public CefV8Value GetValue(string key)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return CefV8Value.FromNativeOrNull(
                    cef_v8value_t.get_value_bykey(_self, &n_key)
                    );
            }
        }

        /// <summary>
        /// Returns the value with the specified identifier on success. Returns NULL
        /// if this method is called incorrectly or an exception is thrown.
        /// </summary>
        public CefV8Value GetValue(int index)
        {
            return CefV8Value.FromNativeOrNull(
                    cef_v8value_t.get_value_byindex(_self, index)
                    );
        }

        /// <summary>
        /// Associates a value with the specified identifier and returns true on
        /// success. Returns false if this method is called incorrectly or an exception
        /// is thrown. For read-only values this method will return true even though
        /// assignment failed.
        /// </summary>
        public bool SetValue(string key, CefV8Value value, CefV8PropertyAttribute attribute = CefV8PropertyAttribute.None)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_v8value_t.set_value_bykey(_self, &n_key, value.ToNative(), attribute) != 0;
            }
        }

        /// <summary>
        /// Associates a value with the specified identifier and returns true on
        /// success. Returns false if this method is called incorrectly or an exception
        /// is thrown. For read-only values this method will return true even though
        /// assignment failed.
        /// </summary>
        public bool SetValue(int index, CefV8Value value)
        {
            return cef_v8value_t.set_value_byindex(_self, index, value.ToNative()) != 0;
        }

        /// <summary>
        /// Registers an identifier and returns true on success. Access to the
        /// identifier will be forwarded to the CefV8Accessor instance passed to
        /// CefV8Value::CreateObject(). Returns false if this method is called
        /// incorrectly or an exception is thrown. For read-only values this method
        /// will return true even though assignment failed.
        /// </summary>
        public bool SetValue(string key, CefV8AccessControl settings, CefV8PropertyAttribute attribute = CefV8PropertyAttribute.None)
        {
            fixed (char* key_str = key)
            {
                var n_key = new cef_string_t(key_str, key != null ? key.Length : 0);
                return cef_v8value_t.set_value_byaccessor(_self, &n_key, settings, attribute) != 0;
            }
        }

        /// <summary>
        /// Read the keys for the object's values into the specified vector. Integer-
        /// based keys will also be returned as strings.
        /// </summary>
        public bool TryGetKeys(out string[] keys)
        {
            var list = libcef.string_list_alloc();
            var result = cef_v8value_t.get_keys(_self, list) != 0;
            if (result) keys = cef_string_list.ToArray(list);
            else keys = null;
            libcef.string_list_free(list);
            return result;
        }

        /// <summary>
        /// Read the keys for the object's values into the specified vector. Integer-
        /// based keys will also be returned as strings.
        /// </summary>
        public string[] GetKeys()
        {
            string[] keys;
            if (TryGetKeys(out keys)) return keys;
            else throw new InvalidOperationException();
        }

        /// <summary>
        /// Sets the user data for this object and returns true on success. Returns
        /// false if this method is called incorrectly. This method can only be called
        /// on user created objects.
        /// </summary>
        public bool SetUserData(CefUserData userData)
        {
            return cef_v8value_t.set_user_data(_self, userData != null ? (cef_base_ref_counted_t*)userData.ToNative() : null) != 0;
        }

        /// <summary>
        /// Returns the user data, if any, assigned to this object.
        /// </summary>
        public CefUserData GetUserData()
        {
            return CefUserData.FromNativeOrNull(
                (cef_user_data_t*)cef_v8value_t.get_user_data(_self)
                );
        }

        /// <summary>
        /// Returns the amount of externally allocated memory registered for the
        /// object.
        /// </summary>
        public int GetExternallyAllocatedMemory()
        {
            return cef_v8value_t.get_externally_allocated_memory(_self);
        }

        /// <summary>
        /// Adjusts the amount of registered external memory for the object. Used to
        /// give V8 an indication of the amount of externally allocated memory that is
        /// kept alive by JavaScript objects. V8 uses this information to decide when
        /// to perform global garbage collection. Each CefV8Value tracks the amount of
        /// external memory associated with it and automatically decreases the global
        /// total by the appropriate amount on its destruction. |change_in_bytes|
        /// specifies the number of bytes to adjust by. This method returns the number
        /// of bytes associated with the object after the adjustment. This method can
        /// only be called on user created objects.
        /// </summary>
        public int AdjustExternallyAllocatedMemory(int changeInBytes)
        {
            return cef_v8value_t.adjust_externally_allocated_memory(_self, changeInBytes);
        }

        /// <summary>
        /// ARRAY METHODS - These methods are only available on arrays.
        /// Returns the number of elements in the array.
        /// </summary>
        public int GetArrayLength()
        {
            return cef_v8value_t.get_array_length(_self);
        }

        /// <summary>
        /// ARRAY BUFFER METHODS - These methods are only available on ArrayBuffers.
        /// Returns the ReleaseCallback object associated with the ArrayBuffer or NULL
        /// if the ArrayBuffer was not created with CreateArrayBuffer.
        /// </summary>
        public CefV8ArrayBufferReleaseCallback GetArrayBufferReleaseCallback()
        {
            var n_releaseCallback = cef_v8value_t.get_array_buffer_release_callback(_self);
            return CefV8ArrayBufferReleaseCallback.FromNativeOrNull(n_releaseCallback);
        }

        /// <summary>
        /// Prevent the ArrayBuffer from using it's memory block by setting the length
        /// to zero. This operation cannot be undone. If the ArrayBuffer was created
        /// with CreateArrayBuffer then CefV8ArrayBufferReleaseCallback::ReleaseBuffer
        /// will be called to release the underlying buffer.
        /// </summary>
        public bool NeuterArrayBuffer()
        {
            return cef_v8value_t.neuter_array_buffer(_self) != 0;
        }

        /// <summary>
        /// FUNCTION METHODS - These methods are only available on functions.
        /// Returns the function name.
        /// </summary>
        public string GetFunctionName()
        {
            var n_result = cef_v8value_t.get_function_name(_self);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Returns the function handler or NULL if not a CEF-created function.
        /// </summary>
        public CefV8Handler GetFunctionHandler()
        {
            return CefV8Handler.FromNativeOrNull(
                cef_v8value_t.get_function_handler(_self)
                );
        }

        /// <summary>
        /// Execute the function using the current V8 context. This method should only
        /// be called from within the scope of a CefV8Handler or CefV8Accessor
        /// callback, or in combination with calling Enter() and Exit() on a stored
        /// CefV8Context reference. |object| is the receiver ('this' object) of the
        /// function. If |object| is empty the current context's global object will be
        /// used. |arguments| is the list of arguments that will be passed to the
        /// function. Returns the function return value on success. Returns NULL if
        /// this method is called incorrectly or an exception is thrown.
        /// </summary>
        public CefV8Value ExecuteFunction(CefV8Value obj, CefV8Value[] arguments)
        {
            var n_arguments = CreateArguments(arguments);
            cef_v8value_t* n_retval;

            fixed (cef_v8value_t** n_arguments_ptr = n_arguments)
            {
                n_retval = cef_v8value_t.execute_function(
                    _self,
                    obj != null ? obj.ToNative() : null,
                    n_arguments != null ? (UIntPtr)n_arguments.Length : UIntPtr.Zero,
                    n_arguments_ptr
                    );
            }

            return CefV8Value.FromNativeOrNull(n_retval);
        }

        /// <summary>
        /// Execute the function using the specified V8 context. |object| is the
        /// receiver ('this' object) of the function. If |object| is empty the
        /// specified context's global object will be used. |arguments| is the list of
        /// arguments that will be passed to the function. Returns the function return
        /// value on success. Returns NULL if this method is called incorrectly or an
        /// exception is thrown.
        /// </summary>
        public CefV8Value ExecuteFunctionWithContext(CefV8Context context, CefV8Value obj, CefV8Value[] arguments)
        {
            var n_arguments = CreateArguments(arguments);
            cef_v8value_t* n_retval;

            fixed (cef_v8value_t** n_arguments_ptr = n_arguments)
            {
                n_retval = cef_v8value_t.execute_function_with_context(
                    _self,
                    context.ToNative(),
                    obj != null ? obj.ToNative() : null,
                    n_arguments != null ? (UIntPtr)n_arguments.Length : UIntPtr.Zero,
                    n_arguments_ptr
                    );
            }

            return CefV8Value.FromNativeOrNull(n_retval);
        }

        private static cef_v8value_t*[] CreateArguments(CefV8Value[] arguments)
        {
            if (arguments == null) return null;

            var length = arguments.Length;
            if (length == 0) return null;

            var result = new cef_v8value_t*[arguments.Length];

            for (var i = 0; i < length; i++)
            {
                result[i] = arguments[i].ToNative();
            }

            return result;
        }
    }
}
