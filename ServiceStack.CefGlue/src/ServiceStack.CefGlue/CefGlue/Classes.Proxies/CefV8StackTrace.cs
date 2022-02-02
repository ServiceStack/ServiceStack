namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing a V8 stack trace handle. V8 handles can only be accessed
    /// from the thread on which they are created. Valid threads for creating a V8
    /// handle include the render process main thread (TID_RENDERER) and WebWorker
    /// threads. A task runner for posting tasks on the associated thread can be
    /// retrieved via the CefV8Context::GetTaskRunner() method.
    /// </summary>
    public sealed unsafe partial class CefV8StackTrace
    {
        /// <summary>
        /// Returns the stack trace for the currently active context. |frame_limit| is
        /// the maximum number of frames that will be captured.
        /// </summary>
        public static CefV8StackTrace GetCurrent(int frameLimit)
        {
            return CefV8StackTrace.FromNative(
                cef_v8stack_trace_t.get_current(frameLimit)
                );
        }

        /// <summary>
        /// Returns true if the underlying handle is valid and it can be accessed on
        /// the current thread. Do not call any other methods if this method returns
        /// false.
        /// </summary>
        public bool IsValid
        {
            get { return cef_v8stack_trace_t.is_valid(_self) != 0; }
        }

        /// <summary>
        /// Returns the number of stack frames.
        /// </summary>
        public int FrameCount
        {
            get { return cef_v8stack_trace_t.get_frame_count(_self); }
        }

        /// <summary>
        /// Returns the stack frame at the specified 0-based index.
        /// </summary>
        public CefV8StackFrame GetFrame(int index)
        {
            return CefV8StackFrame.FromNative(
                cef_v8stack_trace_t.get_frame(_self, index)
                );
        }
    }
}
