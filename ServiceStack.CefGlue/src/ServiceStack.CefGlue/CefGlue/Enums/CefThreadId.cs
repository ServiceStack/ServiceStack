//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_thread_id_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Existing thread IDs.
    /// </summary>
    public enum CefThreadId
    {
        // BROWSER PROCESS THREADS -- Only available in the browser process.

        /// <summary>
        /// The main thread in the browser. This will be the same as the main
        /// application thread if CefInitialize() is called with a
        /// CefSettings.multi_threaded_message_loop value of false. Do not perform
        /// blocking tasks on this thread. All tasks posted after
        /// CefBrowserProcessHandler::OnContextInitialized() and before CefShutdown()
        /// are guaranteed to run. This thread will outlive all other CEF threads.
        /// </summary>
        UI,

        /// <summary>
        /// Used for blocking tasks (e.g. file system access) where the user won't
        /// notice if the task takes an arbitrarily long time to complete. All tasks
        /// posted after CefBrowserProcessHandler::OnContextInitialized() and before
        /// CefShutdown() are guaranteed to run.
        /// </summary>
        FileBackground,
        File = FileBackground,

        /// <summary>
        /// Used for blocking tasks (e.g. file system access) that affect UI or
        /// responsiveness of future user interactions. Do not use if an immediate
        /// response to a user interaction is expected. All tasks posted after
        /// CefBrowserProcessHandler::OnContextInitialized() and before CefShutdown()
        /// are guaranteed to run.
        /// Examples:
        /// - Updating the UI to reflect progress on a long task.
        /// - Loading data that might be shown in the UI after a future user
        ///   interaction.
        /// </summary>
        FileUserVisible,

        /// <summary>
        /// Used for blocking tasks (e.g. file system access) that affect UI
        /// immediately after a user interaction. All tasks posted after
        /// CefBrowserProcessHandler::OnContextInitialized() and before CefShutdown()
        /// are guaranteed to run.
        /// Example: Generating data shown in the UI immediately after a click.
        /// </summary>
        FileUserBlocking,

        /// <summary>
        /// Used to launch and terminate browser processes.
        /// </summary>
        ProcessLauncher,

        /// <summary>
        /// Used to process IPC and network messages. Do not perform blocking tasks on
        /// this thread. All tasks posted after
        /// CefBrowserProcessHandler::OnContextInitialized() and before CefShutdown()
        /// are guaranteed to run.
        /// </summary>
        IO,

        // RENDER PROCESS THREADS -- Only available in the render process.

        /// <summary>
        /// The main thread in the renderer. Used for all WebKit and V8 interaction.
        /// Tasks may be posted to this thread after
        /// CefRenderProcessHandler::OnWebKitInitialized but are not guaranteed to
        /// run before sub-process termination (sub-processes may be killed at any time
        /// without warning).
        /// </summary>
        Renderer,
    }
}
