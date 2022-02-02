namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Callback interface for CefBrowserHost::AddDevToolsMessageObserver. The
    /// methods of this class will be called on the browser process UI thread.
    /// </summary>
    public abstract unsafe partial class CefDevToolsMessageObserver
    {
        private int on_dev_tools_message(cef_dev_tools_message_observer_t* self, cef_browser_t* browser, void* message, UIntPtr message_size)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            var m_result = OnDevToolsMessage(m_browser, (IntPtr)message, checked((int)message_size));

            return m_result ? 1 : 0;
        }

        /// <summary>
        /// Method that will be called on receipt of a DevTools protocol message.
        /// |browser| is the originating browser instance. |message| is a UTF8-encoded
        /// JSON dictionary representing either a method result or an event. |message|
        /// is only valid for the scope of this callback and should be copied if
        /// necessary. Return true if the message was handled or false if the message
        /// should be further processed and passed to the OnDevToolsMethodResult or
        /// OnDevToolsEvent methods as appropriate.
        /// Method result dictionaries include an "id" (int) value that identifies the
        /// orginating method call sent from CefBrowserHost::SendDevToolsMessage, and
        /// optionally either a "result" (dictionary) or "error" (dictionary) value.
        /// The "error" dictionary will contain "code" (int) and "message" (string)
        /// values. Event dictionaries include a "method" (string) value and optionally
        /// a "params" (dictionary) value. See the DevTools protocol documentation at
        /// https://chromedevtools.github.io/devtools-protocol/ for details of
        /// supported method calls and the expected "result" or "params" dictionary
        /// contents. JSON dictionaries can be parsed using the CefParseJSON function
        /// if desired, however be aware of performance considerations when parsing
        /// large messages (some of which may exceed 1MB in size).
        /// </summary>
        protected abstract bool OnDevToolsMessage(CefBrowser browser, IntPtr message, int messageSize);


        private void on_dev_tools_method_result(cef_dev_tools_message_observer_t* self, cef_browser_t* browser, int message_id, int success, void* result, UIntPtr result_size)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            OnDevToolsMethodResult(m_browser, message_id, success != 0, (IntPtr)result, checked((int)result_size));
        }

        /// <summary>
        /// Method that will be called after attempted execution of a DevTools protocol
        /// method. |browser| is the originating browser instance. |message_id| is the
        /// "id" value that identifies the originating method call message. If the
        /// method succeeded |success| will be true and |result| will be the
        /// UTF8-encoded JSON "result" dictionary value (which may be empty). If the
        /// method failed |success| will be false and |result| will be the UTF8-encoded
        /// JSON "error" dictionary value. |result| is only valid for the scope of this
        /// callback and should be copied if necessary. See the OnDevToolsMessage
        /// documentation for additional details on |result| contents.
        /// </summary>
        protected abstract void OnDevToolsMethodResult(CefBrowser browser, int messageId, bool success, IntPtr result, int resultSize);


        private void on_dev_tools_event(cef_dev_tools_message_observer_t* self, cef_browser_t* browser, cef_string_t* method, void* @params, UIntPtr params_size)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_method = cef_string_t.ToString(method);

            OnDevToolsEvent(m_browser, m_method, (IntPtr)@params, checked((int)params_size));
        }

        /// <summary>
        /// Method that will be called on receipt of a DevTools protocol event.
        /// |browser| is the originating browser instance. |method| is the "method"
        /// value. |params| is the UTF8-encoded JSON "params" dictionary value (which
        /// may be empty). |params| is only valid for the scope of this callback and
        /// should be copied if necessary. See the OnDevToolsMessage documentation for
        /// additional details on |params| contents.
        /// </summary>
        protected abstract void OnDevToolsEvent(CefBrowser browser, string method, IntPtr parameters, int parametersSize);


        private void on_dev_tools_agent_attached(cef_dev_tools_message_observer_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            OnDevToolsAgentAttached(m_browser);
        }

        /// <summary>
        /// Method that will be called when the DevTools agent has attached. |browser|
        /// is the originating browser instance. This will generally occur in response
        /// to the first message sent while the agent is detached.
        /// </summary>
        protected abstract void OnDevToolsAgentAttached(CefBrowser browser);


        private void on_dev_tools_agent_detached(cef_dev_tools_message_observer_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            OnDevToolsAgentDetached(m_browser);
        }

        /// <summary>
        /// Method that will be called when the DevTools agent has detached. |browser|
        /// is the originating browser instance. Any method results that were pending
        /// before the agent became detached will not be delivered, and any active
        /// event subscriptions will be canceled.
        /// </summary>
        protected abstract void OnDevToolsAgentDetached(CefBrowser browser);
    }
}
