namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Interface to implement for receiving unstable plugin information. The methods
    /// of this class will be called on the browser process IO thread.
    /// </summary>
    public abstract unsafe partial class CefWebPluginUnstableCallback
    {
        private void is_unstable(cef_web_plugin_unstable_callback_t* self, cef_string_t* path, int unstable)
        {
            CheckSelf(self);

            var m_path = cef_string_t.ToString(path);
            IsUnstable(m_path, unstable != 0);
        }

        /// <summary>
        /// Method that will be called for the requested plugin. |unstable| will be
        /// true if the plugin has reached the crash count threshold of 3 times in 120
        /// seconds.
        /// </summary>
        protected abstract void IsUnstable(string path, bool unstable);
    }
}
