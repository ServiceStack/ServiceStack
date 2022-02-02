namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to implement render process callbacks. The methods of this class
    /// will be called on the render process main thread (TID_RENDERER) unless
    /// otherwise indicated.
    /// </summary>
    public abstract unsafe partial class CefRenderProcessHandler
    {
        private void on_web_kit_initialized(cef_render_process_handler_t* self)
        {
            CheckSelf(self);

            OnWebKitInitialized();
        }

        /// <summary>
        /// Called after WebKit has been initialized.
        /// </summary>
        protected virtual void OnWebKitInitialized()
        {
        }


        private void on_browser_created(cef_render_process_handler_t* self, cef_browser_t* browser, cef_dictionary_value_t* extra_info)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_extraInfo = CefDictionaryValue.FromNative(extra_info);

            OnBrowserCreated(m_browser, m_extraInfo);
        }

        /// <summary>
        /// Called after a browser has been created. When browsing cross-origin a new
        /// browser will be created before the old browser with the same identifier is
        /// destroyed. |extra_info| is a read-only value originating from
        /// CefBrowserHost::CreateBrowser(), CefBrowserHost::CreateBrowserSync(),
        /// CefLifeSpanHandler::OnBeforePopup() or CefBrowserView::CreateBrowserView().
        /// </summary>
        protected virtual void OnBrowserCreated(CefBrowser browser, CefDictionaryValue extraInfo)
        {
        }


        private void on_browser_destroyed(cef_render_process_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            OnBrowserDestroyed(m_browser);
        }

        /// <summary>
        /// Called before a browser is destroyed.
        /// </summary>
        protected virtual void OnBrowserDestroyed(CefBrowser browser)
        {
        }


        private cef_load_handler_t* get_load_handler(cef_render_process_handler_t* self)
        {
            CheckSelf(self);

            var result = GetLoadHandler();

            return result != null ? result.ToNative() : null;
        }

        /// <summary>
        /// Return the handler for browser load status events.
        /// </summary>
        protected virtual CefLoadHandler GetLoadHandler()
        {
            return null;
        }


        private void on_context_created(cef_render_process_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_v8context_t* context)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_frame = CefFrame.FromNative(frame);
            var m_context = CefV8Context.FromNative(context);

            OnContextCreated(m_browser, m_frame, m_context);
        }

        /// <summary>
        /// Called immediately after the V8 context for a frame has been created. To
        /// retrieve the JavaScript 'window' object use the CefV8Context::GetGlobal()
        /// method. V8 handles can only be accessed from the thread on which they are
        /// created. A task runner for posting tasks on the associated thread can be
        /// retrieved via the CefV8Context::GetTaskRunner() method.
        /// </summary>
        protected virtual void OnContextCreated(CefBrowser browser, CefFrame frame, CefV8Context context)
        {
        }


        private void on_context_released(cef_render_process_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_v8context_t* context)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_frame = CefFrame.FromNative(frame);
            var m_context = CefV8Context.FromNative(context);

            OnContextReleased(m_browser, m_frame, m_context);
        }

        /// <summary>
        /// Called immediately before the V8 context for a frame is released. No
        /// references to the context should be kept after this method is called.
        /// </summary>
        protected virtual void OnContextReleased(CefBrowser browser, CefFrame frame, CefV8Context context)
        {
        }


        private void on_uncaught_exception(cef_render_process_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_v8context_t* context, cef_v8exception_t* exception, cef_v8stack_trace_t* stackTrace)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            var mFrame = CefFrame.FromNative(frame);
            var mContext = CefV8Context.FromNative(context);
            var mException = CefV8Exception.FromNative(exception);
            var mStackTrace = CefV8StackTrace.FromNative(stackTrace);

            OnUncaughtException(mBrowser, mFrame, mContext, mException, mStackTrace);
        }

        /// <summary>
        /// Called for global uncaught exceptions in a frame. Execution of this
        /// callback is disabled by default. To enable set
        /// CefSettings.uncaught_exception_stack_size &gt; 0.
        /// </summary>
        protected virtual void OnUncaughtException(CefBrowser browser, CefFrame frame, CefV8Context context, CefV8Exception exception, CefV8StackTrace stackTrace)
        {
        }


        private void on_focused_node_changed(cef_render_process_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_domnode_t* node)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_frame = CefFrame.FromNative(frame);
            var m_node = CefDomNode.FromNativeOrNull(node);

            OnFocusedNodeChanged(m_browser, m_frame, m_node);

            if (m_node != null) m_node.Dispose();
        }

        /// <summary>
        /// Called when a new node in the the browser gets focus. The |node| value may
        /// be empty if no specific node has gained focus. The node object passed to
        /// this method represents a snapshot of the DOM at the time this method is
        /// executed. DOM objects are only valid for the scope of this method. Do not
        /// keep references to or attempt to access any DOM objects outside the scope
        /// of this method.
        /// </summary>
        protected virtual void OnFocusedNodeChanged(CefBrowser browser, CefFrame frame, CefDomNode node)
        {
        }


        private int on_process_message_received(cef_render_process_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, CefProcessId source_process, cef_process_message_t* message)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_frame = CefFrame.FromNative(frame);
            var m_message = CefProcessMessage.FromNative(message);

            var result = OnProcessMessageReceived(m_browser, m_frame, source_process, m_message);

            m_message.Dispose();

            return result ? 1 : 0;
        }

        /// <summary>
        /// Called when a new message is received from a different process. Return true
        /// if the message was handled or false otherwise. Do not keep a reference to
        /// or attempt to access the message outside of this callback.
        /// </summary>
        protected virtual bool OnProcessMessageReceived(CefBrowser browser, CefFrame frame, CefProcessId sourceProcess, CefProcessMessage message)
        {
            return false;
        }
    }
}
