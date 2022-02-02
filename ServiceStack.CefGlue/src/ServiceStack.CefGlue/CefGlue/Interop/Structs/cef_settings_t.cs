//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_settings_t.
//
// Note: cef_settings_t structure in CEF has 2 layouts (choosed in compile time),
//       so in C# we make different structures and will choose layouts in runtime.
//    Windows platform: cef_settings_t_windows.
//    Non-windows platforms: cef_settings_t_posix.
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_settings_t
    {
        public UIntPtr size;
        public int no_sandbox;
        public cef_string_t browser_subprocess_path;
        public cef_string_t framework_dir_path;
        public cef_string_t main_bundle_path;
        public int chrome_runtime;
        public int multi_threaded_message_loop;
        public int external_message_pump;
        public int windowless_rendering_enabled;
        public int command_line_args_disabled;
        public cef_string_t cache_path;
        public cef_string_t root_cache_path;
        public cef_string_t user_data_path;
        public int persist_session_cookies;
        public int persist_user_preferences;
        public cef_string_t user_agent;
        public cef_string_t product_version;
        public cef_string_t locale;
        public cef_string_t log_file;
        public CefLogSeverity log_severity;
        public cef_string_t javascript_flags;
        public cef_string_t resources_dir_path;
        public cef_string_t locales_dir_path;
        public int pack_loading_disabled;
        public int remote_debugging_port;
        public int uncaught_exception_stack_size;
        public int ignore_certificate_errors;
        public uint background_color;
        public cef_string_t accept_language_list;
        public cef_string_t application_client_id_for_file_scanning;

        #region Alloc & Free
        private static int _sizeof;

        static cef_settings_t()
        {
            _sizeof = Marshal.SizeOf(typeof(cef_settings_t));
        }

        public static cef_settings_t* Alloc()
        {
            var ptr = (cef_settings_t*)Marshal.AllocHGlobal(_sizeof);
            *ptr = new cef_settings_t();
            ptr->size = (UIntPtr)_sizeof;
            return ptr;
        }

        public static void Free(cef_settings_t* ptr)
        {
            Marshal.FreeHGlobal((IntPtr)ptr);
        }
        #endregion
    }
}
