namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class that asynchronously executes tasks on the associated thread. It is safe
    /// to call the methods of this class on any thread.
    /// CEF maintains multiple internal threads that are used for handling different
    /// types of tasks in different processes. The cef_thread_id_t definitions in
    /// cef_types.h list the common CEF threads. Task runners are also available for
    /// other CEF threads as appropriate (for example, V8 WebWorker threads).
    /// </summary>
    public sealed unsafe partial class CefTaskRunner
    {
        /// <summary>
        /// Returns the task runner for the current thread. Only CEF threads will have
        /// task runners. An empty reference will be returned if this method is called
        /// on an invalid thread.
        /// </summary>
        public static CefTaskRunner GetForCurrentThread()
        {
            return CefTaskRunner.FromNativeOrNull(cef_task_runner_t.get_for_current_thread());
        }

        /// <summary>
        /// Returns the task runner for the specified CEF thread.
        /// </summary>
        public static CefTaskRunner GetForThread(CefThreadId threadId)
        {
            return CefTaskRunner.FromNativeOrNull(cef_task_runner_t.get_for_thread(threadId));
        }

        /// <summary>
        /// Returns true if this object is pointing to the same task runner as |that|
        /// object.
        /// </summary>
        public bool IsSame(CefTaskRunner that)
        {
            return cef_task_runner_t.is_same(_self, that.ToNative()) != 0;
        }

        /// <summary>
        /// Returns true if this task runner belongs to the current thread.
        /// </summary>
        public bool BelongsToCurrentThread
        {
            get { return cef_task_runner_t.belongs_to_current_thread(_self) != 0; }
        }

        /// <summary>
        /// Returns true if this task runner is for the specified CEF thread.
        /// </summary>
        public bool BelongsToThread(CefThreadId threadId)
        {
            return cef_task_runner_t.belongs_to_thread(_self, threadId) != 0;
        }

        /// <summary>
        /// Post a task for execution on the thread associated with this task runner.
        /// Execution will occur asynchronously.
        /// </summary>
        public bool PostTask(CefTask task)
        {
            return cef_task_runner_t.post_task(_self, task.ToNative()) != 0;
        }

        /// <summary>
        /// Post a task for delayed execution on the thread associated with this task
        /// runner. Execution will occur asynchronously. Delayed tasks are not
        /// supported on V8 WebWorker threads and will be executed without the
        /// specified delay.
        /// </summary>
        public bool PostDelayedTask(CefTask task, long delay)
        {
            return cef_task_runner_t.post_delayed_task(_self, task.ToNative(), delay) != 0;
        }
    }
}
