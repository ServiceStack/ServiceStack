namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Information about a specific web plugin.
    /// </summary>
    public sealed unsafe partial class CefWebPluginInfo
    {
        /// <summary>
        /// Returns the plugin name (i.e. Flash).
        /// </summary>
        public string Name
        {
            get
            {
                var n_result = cef_web_plugin_info_t.get_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the plugin file path (DLL/bundle/library).
        /// </summary>
        public string Path
        {
            get
            {
                var n_result = cef_web_plugin_info_t.get_path(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns the version of the plugin (may be OS-specific).
        /// </summary>
        public string Version
        {
            get
            {
                var n_result = cef_web_plugin_info_t.get_version(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Returns a description of the plugin from the version information.
        /// </summary>
        public string Description
        {
            get
            {
                var n_result = cef_web_plugin_info_t.get_description(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }
    }
}
