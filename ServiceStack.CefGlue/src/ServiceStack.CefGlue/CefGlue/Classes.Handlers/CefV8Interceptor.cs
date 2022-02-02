namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Interface that should be implemented to handle V8 interceptor calls. The
    /// methods of this class will be called on the thread associated with the V8
    /// interceptor. Interceptor's named property handlers (with first argument of
    /// type CefString) are called when object is indexed by string. Indexed property
    /// handlers (with first argument of type int) are called when object is indexed
    /// by integer.
    /// </summary>
    public abstract unsafe partial class CefV8Interceptor
    {
        private int get_byname(cef_v8interceptor_t* self, cef_string_t* name, cef_v8value_t* @object, cef_v8value_t** retval, cef_string_t* exception)
        {
            CheckSelf(self);

            var m_name = cef_string_t.ToString(name);
            var m_obj = CefV8Value.FromNative(@object);

            CefV8Value m_retval;
            string m_exception;
            if (GetByName(m_name, m_obj, out m_retval, out m_exception))
            {
                *retval = m_retval != null ? m_retval.ToNative() : null;
                cef_string_t.Copy(m_exception, exception);
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Handle retrieval of the interceptor value identified by |name|. |object| is
        /// the receiver ('this' object) of the interceptor. If retrieval succeeds, set
        /// |retval| to the return value. If the requested value does not exist, don't
        /// set either |retval| or |exception|. If retrieval fails, set |exception| to
        /// the exception that will be thrown. If the property has an associated
        /// accessor, it will be called only if you don't set |retval|.
        /// Return true if interceptor retrieval was handled, false otherwise.
        /// </summary>
        protected virtual bool GetByName(string name, CefV8Value @object, out CefV8Value retval, out string exception)
        {
            retval = null;
            exception = null;
            return false;
        }


        private int get_byindex(cef_v8interceptor_t* self, int index, cef_v8value_t* @object, cef_v8value_t** retval, cef_string_t* exception)
        {
            CheckSelf(self);

            var m_obj = CefV8Value.FromNative(@object);

            CefV8Value m_retval;
            string m_exception;
            if (GetByIndex(index, m_obj, out m_retval, out m_exception))
            {
                *retval = m_retval != null ? m_retval.ToNative() : null;
                cef_string_t.Copy(m_exception, exception);
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Handle retrieval of the interceptor value identified by |index|. |object|
        /// is the receiver ('this' object) of the interceptor. If retrieval succeeds,
        /// set |retval| to the return value. If the requested value does not exist,
        /// don't set either |retval| or |exception|. If retrieval fails, set
        /// |exception| to the exception that will be thrown.
        /// Return true if interceptor retrieval was handled, false otherwise.
        /// </summary>
        protected virtual bool GetByIndex(int index, CefV8Value @object, out CefV8Value retval, out string exception)
        {
            retval = null;
            exception = null;
            return false;
        }


        private int set_byname(cef_v8interceptor_t* self, cef_string_t* name, cef_v8value_t* @object, cef_v8value_t* value, cef_string_t* exception)
        {
            CheckSelf(self);

            var m_name = cef_string_t.ToString(name);
            var m_obj = CefV8Value.FromNative(@object);
            var m_value = CefV8Value.FromNative(value);

            string m_exception;
            if (SetByName(m_name, m_obj, m_value, out m_exception))
            {
                cef_string_t.Copy(m_exception, exception);
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Handle assignment of the interceptor value identified by |name|. |object|
        /// is the receiver ('this' object) of the interceptor. |value| is the new
        /// value being assigned to the interceptor. If assignment fails, set
        /// |exception| to the exception that will be thrown. This setter will always
        /// be called, even when the property has an associated accessor.
        /// Return true if interceptor assignment was handled, false otherwise.
        /// </summary>
        protected virtual bool SetByName(string name, CefV8Value @object, CefV8Value value, out string exception)
        {
            exception = null;
            return false;
        }


        private int set_byindex(cef_v8interceptor_t* self, int index, cef_v8value_t* @object, cef_v8value_t* value, cef_string_t* exception)
        {
            CheckSelf(self);

            var m_obj = CefV8Value.FromNative(@object);
            var m_value = CefV8Value.FromNative(value);

            string m_exception;
            if (SetByIndex(index, m_obj, m_value, out m_exception))
            {
                cef_string_t.Copy(m_exception, exception);
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Handle assignment of the interceptor value identified by |index|. |object|
        /// is the receiver ('this' object) of the interceptor. |value| is the new
        /// value being assigned to the interceptor. If assignment fails, set
        /// |exception| to the exception that will be thrown.
        /// Return true if interceptor assignment was handled, false otherwise.
        /// </summary>
        protected virtual bool SetByIndex(int index, CefV8Value @object, CefV8Value value, out string exception)
        {
            exception = null;
            return false;
        }
    }
}
