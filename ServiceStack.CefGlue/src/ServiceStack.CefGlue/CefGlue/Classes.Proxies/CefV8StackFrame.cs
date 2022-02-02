namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing a V8 stack frame handle. V8 handles can only be accessed
    /// from the thread on which they are created. Valid threads for creating a V8
    /// handle include the render process main thread (TID_RENDERER) and WebWorker
    /// threads. A task runner for posting tasks on the associated thread can be
    /// retrieved via the CefV8Context::GetTaskRunner() method.
    /// </summary>
    public sealed unsafe partial class CefV8StackFrame
    {
        /// <summary>
        /// Returns true if the underlying handle is valid and it can be accessed on
        /// the current thread. Do not call any other methods if this method returns
        /// false.
        /// </summary>
        public bool IsValid
        {
            get { return cef_v8stack_frame_t.is_valid(_self) != 0; }
        }

        /// <summary>
        /// Returns the name of the resource script that contains the function.
        /// </summary>
        public string ScriptName
        {
            get
            {
                var n_result = cef_v8stack_frame_t.get_script_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the name of the resource script that contains the function or the
        /// sourceURL value if the script name is undefined and its source ends with
        /// a "//@ sourceURL=..." string.
        /// </summary>
        public string ScriptNameOrSourceUrl
        {
            get
            {
                var n_result = cef_v8stack_frame_t.get_script_name_or_source_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the name of the function.
        /// </summary>
        public string FunctionName
        {
            get
            {
                var n_result = cef_v8stack_frame_t.get_function_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the 1-based line number for the function call or 0 if unknown.
        /// </summary>
        public int LineNumber
        {
            get { return cef_v8stack_frame_t.get_line_number(_self); }
        }

        /// <summary>
        /// Returns the 1-based column offset on the line for the function call or 0 if
        /// unknown.
        /// </summary>
        public int Column
        {
            get { return cef_v8stack_frame_t.get_column(_self); }
        }

        /// <summary>
        /// Returns true if the function was compiled using eval().
        /// </summary>
        public bool IsEval
        {
            get { return cef_v8stack_frame_t.is_eval(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the function was called as a constructor via "new".
        /// </summary>
        public bool IsConstructor
        {
            get { return cef_v8stack_frame_t.is_constructor(_self) != 0; }
        }
    }
}
