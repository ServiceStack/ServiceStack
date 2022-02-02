namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle events related to keyboard input. The
    /// methods of this class will be called on the UI thread.
    /// </summary>
    public abstract unsafe partial class CefKeyboardHandler
    {
        private int on_pre_key_event(cef_keyboard_handler_t* self, cef_browser_t* browser, cef_key_event_t* @event, IntPtr os_event, int* is_keyboard_shortcut)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_event = CefKeyEvent.FromNative(@event);
            // TODO: wrap cef_event_handle_t (os_event)
            IntPtr m_os_event = IntPtr.Zero;
            if (os_event != IntPtr.Zero)
            {
            }

            var m_is_keyboard_shortcut = *is_keyboard_shortcut != 0;

            var result = OnPreKeyEvent(m_browser, m_event, m_os_event, out m_is_keyboard_shortcut);
            *is_keyboard_shortcut = m_is_keyboard_shortcut ? 1 : 0;

            return result ? 1 : 0;
        }

        /// <summary>
        /// Called before a keyboard event is sent to the renderer. |event| contains
        /// information about the keyboard event. |os_event| is the operating system
        /// event message, if any. Return true if the event was handled or false
        /// otherwise. If the event will be handled in OnKeyEvent() as a keyboard
        /// shortcut set |is_keyboard_shortcut| to true and return false.
        /// </summary>
        protected virtual bool OnPreKeyEvent(CefBrowser browser, CefKeyEvent keyEvent, IntPtr os_event, out bool isKeyboardShortcut)
        {
            isKeyboardShortcut = false;
            return false;
        }


        private int on_key_event(cef_keyboard_handler_t* self, cef_browser_t* browser, cef_key_event_t* @event, IntPtr os_event)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_event = CefKeyEvent.FromNative(@event);
            // TODO: wrap cef_event_handle_t (os_event)
            IntPtr m_os_event = IntPtr.Zero;
            if (os_event != IntPtr.Zero)
            {
            }

            var result = OnKeyEvent(m_browser, m_event, m_os_event);

            return result ? 1 : 0;
        }

        /// <summary>
        /// Called after the renderer and JavaScript in the page has had a chance to
        /// handle the event. |event| contains information about the keyboard event.
        /// |os_event| is the operating system event message, if any. Return true if
        /// the keyboard event was handled or false otherwise.
        /// </summary>
        protected virtual bool OnKeyEvent(CefBrowser browser, CefKeyEvent keyEvent, IntPtr osEvent)
        {
            return false;
        }
    }
}
