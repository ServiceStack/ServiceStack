namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xilium.CefGlue.Interop;

    public abstract unsafe partial class CefApp
    {
        private void on_before_command_line_processing(cef_app_t* self, cef_string_t* process_type, cef_command_line_t* command_line)
        {
            CheckSelf(self);

            var processType = cef_string_t.ToString(process_type);
            var m_commandLine = CefCommandLine.FromNative(command_line);

            OnBeforeCommandLineProcessing(processType, m_commandLine);

            m_commandLine.Dispose();
        }

        /// <summary>
        /// Provides an opportunity to view and/or modify command-line arguments before
        /// processing by CEF and Chromium. The |process_type| value will be empty for
        /// the browser process. Do not keep a reference to the CefCommandLine object
        /// passed to this method. The CefSettings.command_line_args_disabled value
        /// can be used to start with an empty command-line object. Any values
        /// specified in CefSettings that equate to command-line arguments will be set
        /// before this method is called. Be cautious when using this method to modify
        /// command-line arguments for non-browser processes as this may result in
        /// undefined behavior including crashes.
        /// </summary>
        protected virtual void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
        {
        }


        private void on_register_custom_schemes(cef_app_t* self, cef_scheme_registrar_t* registrar)
        {
            CheckSelf(self);

            var m_registrar = CefSchemeRegistrar.FromNative(registrar);

            OnRegisterCustomSchemes(m_registrar);

            m_registrar.ReleaseObject();
        }

        /// <summary>
        /// Provides an opportunity to register custom schemes. Do not keep a reference
        /// to the |registrar| object. This method is called on the main thread for
        /// each process and the registered schemes should be the same across all
        /// processes.
        /// </summary>
        protected virtual void OnRegisterCustomSchemes(CefSchemeRegistrar registrar)
        {
        }


        private cef_resource_bundle_handler_t* get_resource_bundle_handler(cef_app_t* self)
        {
            CheckSelf(self);

            var result = GetResourceBundleHandler();

            return result != null ? result.ToNative() : null;
        }

        /// <summary>
        /// Return the handler for resource bundle events. If
        /// CefSettings.pack_loading_disabled is true a handler must be returned. If no
        /// handler is returned resources will be loaded from pack files. This method
        /// is called by the browser and renderer processes on multiple threads.
        /// </summary>
        protected virtual CefResourceBundleHandler GetResourceBundleHandler()
        {
            return null;
        }


        private cef_browser_process_handler_t* get_browser_process_handler(cef_app_t* self)
        {
            CheckSelf(self);

            var result = GetBrowserProcessHandler();

            return result != null ? result.ToNative() : null;
        }

        /// <summary>
        /// Return the handler for functionality specific to the browser process. This
        /// method is called on multiple threads in the browser process.
        /// </summary>
        protected virtual CefBrowserProcessHandler GetBrowserProcessHandler()
        {
            return null;
        }


        private cef_render_process_handler_t* get_render_process_handler(cef_app_t* self)
        {
            CheckSelf(self);

            var result = GetRenderProcessHandler();

            return result != null ? result.ToNative() : null;
        }

        /// <summary>
        /// Return the handler for render process events. This method is called by the
        /// render process main thread.
        /// </summary>
        /// <returns></returns>
        protected virtual CefRenderProcessHandler GetRenderProcessHandler()
        {
            return null;
        }
    }
}
