namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing a V8 exception. The methods of this class may be called on
    /// any render process thread.
    /// </summary>
    public sealed unsafe partial class CefV8Exception
    {
        /// <summary>
        /// Returns the exception message.
        /// </summary>
        public string Message
        {
            get
            {
                var n_result = cef_v8exception_t.get_message(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the line of source code that the exception occurred within.
        /// </summary>
        public string SourceLine
        {
            get
            {
                var n_result = cef_v8exception_t.get_source_line(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the resource name for the script from where the function causing
        /// the error originates.
        /// </summary>
        public string ScriptResourceName
        {
            get
            {
                var n_result = cef_v8exception_t.get_script_resource_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the 1-based number of the line where the error occurred or 0 if the
        /// line number is unknown.
        /// </summary>
        public int LineNumber
        {
            get { return cef_v8exception_t.get_line_number(_self); }
        }

        /// <summary>
        /// Returns the index within the script of the first character where the error
        /// occurred.
        /// </summary>
        public int StartPosition
        {
            get { return cef_v8exception_t.get_start_position(_self); }
        }

        /// <summary>
        /// Returns the index within the script of the last character where the error
        /// occurred.
        /// </summary>
        public int EndPosition
        {
            get { return cef_v8exception_t.get_end_position(_self); }
        }

        /// <summary>
        /// Returns the index within the line of the first character where the error
        /// occurred.
        /// </summary>
        public int StartColumn
        {
            get { return cef_v8exception_t.get_start_column(_self); }
        }

        /// <summary>
        /// Returns the index within the line of the last character where the error
        /// occurred.
        /// </summary>
        public int EndColumn
        {
            get { return cef_v8exception_t.get_end_column(_self); }
        }
    }
}
