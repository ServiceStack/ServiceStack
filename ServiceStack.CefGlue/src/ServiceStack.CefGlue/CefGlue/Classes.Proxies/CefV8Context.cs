namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing a V8 context handle. V8 handles can only be accessed from
    /// the thread on which they are created. Valid threads for creating a V8 handle
    /// include the render process main thread (TID_RENDERER) and WebWorker threads.
    /// A task runner for posting tasks on the associated thread can be retrieved via
    /// the CefV8Context::GetTaskRunner() method.
    /// </summary>
    public sealed unsafe partial class CefV8Context
    {
        /// <summary>
        /// Returns the current (top) context object in the V8 context stack.
        /// </summary>
        public static CefV8Context GetCurrentContext()
        {
            return CefV8Context.FromNative(
                cef_v8context_t.get_current_context()
                );
        }

        /// <summary>
        /// Returns the entered (bottom) context object in the V8 context stack.
        /// </summary>
        public static CefV8Context GetEnteredContext()
        {
            return CefV8Context.FromNative(
                cef_v8context_t.get_entered_context()
                );
        }

        /// <summary>
        /// Returns true if V8 is currently inside a context.
        /// </summary>
        public static bool InContext
        {
            get { return cef_v8context_t.in_context() != 0; }
        }

        /// <summary>
        /// Returns the task runner associated with this context. V8 handles can only
        /// be accessed from the thread on which they are created. This method can be
        /// called on any render process thread.
        /// </summary>
        public CefTaskRunner GetTaskRunner()
        {
            return CefTaskRunner.FromNative(
                cef_v8context_t.get_task_runner(_self)
                );
        }

        /// <summary>
        /// Returns true if the underlying handle is valid and it can be accessed on
        /// the current thread. Do not call any other methods if this method returns
        /// false.
        /// </summary>
        public bool IsValid
        {
            get { return cef_v8context_t.is_valid(_self) != 0; }
        }

        /// <summary>
        /// Returns the browser for this context. This method will return an empty
        /// reference for WebWorker contexts.
        /// </summary>
        public CefBrowser GetBrowser()
        {
            return CefBrowser.FromNativeOrNull(
                cef_v8context_t.get_browser(_self)
                );
        }

        /// <summary>
        /// Returns the frame for this context. This method will return an empty
        /// reference for WebWorker contexts.
        /// </summary>
        public CefFrame GetFrame()
        {
            return CefFrame.FromNativeOrNull(
                cef_v8context_t.get_frame(_self)
                );
        }

        /// <summary>
        /// Returns the global object for this context. The context must be entered
        /// before calling this method.
        /// </summary>
        public CefV8Value GetGlobal()
        {
            return CefV8Value.FromNative(
                cef_v8context_t.get_global(_self)
                );
        }

        /// <summary>
        /// Enter this context. A context must be explicitly entered before creating a
        /// V8 Object, Array, Function or Date asynchronously. Exit() must be called
        /// the same number of times as Enter() before releasing this context. V8
        /// objects belong to the context in which they are created. Returns true if
        /// the scope was entered successfully.
        /// </summary>
        public bool Enter()
        {
            return cef_v8context_t.enter(_self) != 0;
        }

        /// <summary>
        /// Exit this context. Call this method only after calling Enter(). Returns
        /// true if the scope was exited successfully.
        /// </summary>
        public bool Exit()
        {
            return cef_v8context_t.exit(_self) != 0;
        }

        /// <summary>
        /// Returns true if this object is pointing to the same handle as |that|
        /// object.
        /// </summary>
        public bool IsSame(CefV8Context that)
        {
            if (that == null) return false;
            return cef_v8context_t.is_same(_self, that.ToNative()) != 0;
        }

        /// <summary>
        /// Execute a string of JavaScript code in this V8 context. The |script_url|
        /// parameter is the URL where the script in question can be found, if any.
        /// The |start_line| parameter is the base line number to use for error
        /// reporting. On success |retval| will be set to the return value, if any, and
        /// the function will return true. On failure |exception| will be set to the
        /// exception, if any, and the function will return false.
        /// </summary>
        public bool TryEval(string code, string scriptUrl, int startLine, out CefV8Value returnValue, out CefV8Exception exception)
        {
            bool result;
            cef_v8value_t* n_retval = null;
            cef_v8exception_t* n_exception = null;

            fixed (char* code_str = code)
            fixed (char* scriptUrl_str = scriptUrl)
            {
                var n_code = new cef_string_t(code_str, code != null ? code.Length : 0);
                var n_scriptUrl = new cef_string_t(scriptUrl_str, scriptUrl != null ? scriptUrl.Length : 0);
                result = cef_v8context_t.eval(_self, &n_code, &n_scriptUrl, startLine, &n_retval, &n_exception) != 0;
            }

            returnValue = n_retval != null ? CefV8Value.FromNative(n_retval) : null;
            exception = n_exception != null ? CefV8Exception.FromNative(n_exception) : null;

            return result;
        }
    }
}
