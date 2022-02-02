namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Initialization settings. Specify <c>null</c> or 0 to get the recommended default
    /// values. Many of these and other settings can also configured using command-line
    /// switches.
    /// </summary>
    public sealed unsafe class CefSettings
    {
        public CefSettings()
        {
            BackgroundColor = new CefColor(255, 255, 255, 255);
        }

        /// <summary>
        /// Set to <c>true</c> to disable the sandbox for sub-processes. See
        /// cef_sandbox_win.h for requirements to enable the sandbox on Windows. Also
        /// configurable using the "no-sandbox" command-line switch.
        /// </summary>
        public bool NoSandbox { get; set; }

        /// <summary>
        /// The path to a separate executable that will be launched for sub-processes.
        /// If this value is empty on Windows or Linux then the main process executable
        /// will be used. If this value is empty on macOS then a helper executable must
        /// exist at "Contents/Frameworks/&lt;app&gt; Helper.app/Contents/MacOS/&lt;app&gt; Helper"
        /// in the top-level app bundle. See the comments on CefExecuteProcess() for
        /// details. If this value is non-empty then it must be an absolute path. Also
        /// configurable using the "browser-subprocess-path" command-line switch.
        /// </summary>
        public string BrowserSubprocessPath { get; set; }

        /// <summary>
        /// The path to the CEF framework directory on macOS. If this value is empty
        /// then the framework must exist at "Contents/Frameworks/Chromium Embedded
        /// Framework.framework" in the top-level app bundle. If this value is
        /// non-empty then it must be an absolute path. Also configurable using the
        /// "framework-dir-path" command-line switch.
        /// </summary>
        public string FrameworkDirPath { get; set; }

        /// <summary>
        /// The path to the main bundle on macOS. If this value is empty then it
        /// defaults to the top-level app bundle. If this value is non-empty then it
        /// must be an absolute path. Also configurable using the "main-bundle-path"
        /// command-line switch.
        /// </summary>
        public string MainBundlePath { get; set; }

        /// <summary>
        /// Set to true to enable use of the Chrome runtime in CEF. This feature is
        /// considered experimental and is not recommended for most users at this time.
        /// See issue #2969 for details.
        /// </summary>
        public bool ChromeRuntime { get; set; }

        /// <summary>
        /// Set to <c>true</c> to have the browser process message loop run in a separate
        /// thread. If <c>false</c> than the CefDoMessageLoopWork() function must be
        /// called from your application message loop. This option is only supported on
        /// Windows and Linux.
        /// </summary>
        public bool MultiThreadedMessageLoop { get; set; }

        /// <summary>
        /// Set to <c>true</c> to control browser process main (UI) thread message pump
        /// scheduling via the CefBrowserProcessHandler::OnScheduleMessagePumpWork()
        /// callback. This option is recommended for use in combination with the
        /// CefDoMessageLoopWork() function in cases where the CEF message loop must be
        /// integrated into an existing application message loop (see additional
        /// comments and warnings on CefDoMessageLoopWork). Enabling this option is not
        /// recommended for most users; leave this option disabled and use either the
        /// CefRunMessageLoop() function or multi_threaded_message_loop if possible.
        /// </summary>
        public bool ExternalMessagePump { get; set; }

        /// <summary>
        /// Set to true (1) to enable windowless (off-screen) rendering support. Do not
        /// enable this value if the application does not use windowless rendering as
        /// it may reduce rendering performance on some systems.
        /// </summary>
        public bool WindowlessRenderingEnabled { get; set; }

        /// <summary>
        /// Set to <c>true</c> to disable configuration of browser process features using
        /// standard CEF and Chromium command-line arguments. Configuration can still
        /// be specified using CEF data structures or via the
        /// CefApp::OnBeforeCommandLineProcessing() method.
        /// </summary>
        public bool CommandLineArgsDisabled { get; set; }

        /// <summary>
        /// The location where data for the global browser cache will be stored on
        /// disk. If this value is non-empty then it must be an absolute path that is
        /// either equal to or a child directory of CefSettings.root_cache_path. If
        /// this value is empty then browsers will be created in "incognito mode" where
        /// in-memory caches are used for storage and no data is persisted to disk.
        /// HTML5 databases such as localStorage will only persist across sessions if a
        /// cache path is specified. Can be overridden for individual CefRequestContext
        /// instances via the CefRequestContextSettings.cache_path value.
        /// </summary>
        public string CachePath { get; set; }

        /// <summary>
        /// The root directory that all CefSettings.cache_path and
        /// CefRequestContextSettings.cache_path values must have in common. If this
        /// value is empty and CefSettings.cache_path is non-empty then it will
        /// default to the CefSettings.cache_path value. If this value is non-empty
        /// then it must be an absolute path. Failure to set this value correctly may
        /// result in the sandbox blocking read/write access to the cache_path
        /// directory.
        /// </summary>
        public string RootCachePath { get; set; }

        /// <summary>
        /// The location where user data such as spell checking dictionary files will
        /// be stored on disk. If this value is empty then the default
        /// platform-specific user data directory will be used ("~/.cef_user_data"
        /// directory on Linux, "~/Library/Application Support/CEF/User Data" directory
        /// on Mac OS X, "Local Settings\Application Data\CEF\User Data" directory
        /// under the user profile directory on Windows). If this value is non-empty
        /// then it must be an absolute path.
        /// </summary>
        public string UserDataPath { get; set; }

        /// <summary>
        /// To persist session cookies (cookies without an expiry date or validity
        /// interval) by default when using the global cookie manager set this value to
        /// true. Session cookies are generally intended to be transient and most
        /// Web browsers do not persist them. A |cache_path| value must also be
        /// specified to enable this feature. Also configurable using the
        /// "persist-session-cookies" command-line switch. Can be overridden for
        /// individual CefRequestContext instances via the
        /// CefRequestContextSettings.persist_session_cookies value.
        /// </summary>
        public bool PersistSessionCookies { get; set; }

        /// <summary>
        /// To persist user preferences as a JSON file in the cache path directory set
        /// this value to true. A |cache_path| value must also be specified
        /// to enable this feature. Also configurable using the
        /// "persist-user-preferences" command-line switch. Can be overridden for
        /// individual CefRequestContext instances via the
        /// CefRequestContextSettings.persist_user_preferences value.
        /// </summary>
        public bool PersistUserPreferences { get; set; }

        /// <summary>
        /// Value that will be returned as the User-Agent HTTP header. If empty the
        /// default User-Agent string will be used. Also configurable using the
        /// "user-agent" command-line switch.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Value that will be inserted as the product portion of the default
        /// User-Agent string. If empty the Chromium product version will be used. If
        /// |userAgent| is specified this value will be ignored. Also configurable
        /// using the "product-version" command-line switch.
        /// </summary>
        public string ProductVersion { get; set; }

        /// <summary>
        /// The locale string that will be passed to WebKit. If empty the default
        /// locale of "en-US" will be used. This value is ignored on Linux where locale
        /// is determined using environment variable parsing with the precedence order:
        /// LANGUAGE, LC_ALL, LC_MESSAGES and LANG. Also configurable using the "lang"
        /// command-line switch.
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// The directory and file name to use for the debug log. If empty a default
        /// log file name and location will be used. On Windows and Linux a "debug.log"
        /// file will be written in the main executable directory. On Mac OS X a
        /// "~/Library/Logs/[app name]_debug.log" file will be written where [app name]
        /// is the name of the main app executable. Also configurable using the
        /// "log-file" command-line switch.
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// The log severity. Only messages of this severity level or higher will be
        /// logged. When set to DISABLE no messages will be written to the log file,
        /// but FATAL messages will still be output to stderr. Also configurable using
        /// the "log-severity" command-line switch with a value of "verbose", "info",
        /// "warning", "error", "fatal" or "disable".
        /// </summary>
        public CefLogSeverity LogSeverity { get; set; }

        /// <summary>
        /// Custom flags that will be used when initializing the V8 JavaScript engine.
        /// The consequences of using custom flags may not be well tested. Also
        /// configurable using the "js-flags" command-line switch.
        /// </summary>
        public string JavaScriptFlags { get; set; }

        /// <summary>
        /// The fully qualified path for the resources directory. If this value is
        /// empty the cef.pak and/or devtools_resources.pak files must be located in
        /// the module directory on Windows/Linux or the app bundle Resources directory
        /// on Mac OS X. If this value is non-empty then it must be an absolute path.
        /// Also configurable using the "resources-dir-path" command-line switch.
        /// </summary>
        public string ResourcesDirPath { get; set; }

        /// <summary>
        /// The fully qualified path for the locales directory. If this value is empty
        /// the locales directory must be located in the module directory. If this
        /// value is non-empty then it must be an absolute path. This value is ignored
        /// on Mac OS X where pack files are always loaded from the app bundle
        /// Resources directory. Also configurable using the "locales-dir-path"
        /// </summary>
        public string LocalesDirPath { get; set; }

        /// <summary>
        /// Set to <c>true</c> to disable loading of pack files for resources and locales.
        /// A resource bundle handler must be provided for the browser and render
        /// processes via CefApp::GetResourceBundleHandler() if loading of pack files
        /// is disabled. Also configurable using the "disable-pack-loading" command-
        /// line switch.
        /// </summary>
        public bool PackLoadingDisabled { get; set; }

        /// <summary>
        /// Set to a value between 1024 and 65535 to enable remote debugging on the
        /// specified port. For example, if 8080 is specified the remote debugging URL
        /// will be http://localhost:8080. CEF can be remotely debugged from any CEF or
        /// Chrome browser window. Also configurable using the "remote-debugging-port"
        /// command-line switch.
        /// </summary>
        public int RemoteDebuggingPort { get; set; }

        /// <summary>
        /// The number of stack trace frames to capture for uncaught exceptions.
        /// Specify a positive value to enable the CefV8ContextHandler::
        /// OnUncaughtException() callback. Specify 0 (default value) and
        /// OnUncaughtException() will not be called. Also configurable using the
        /// "uncaught-exception-stack-size" command-line switch.
        /// </summary>
        public int UncaughtExceptionStackSize { get; set; }

        /// <summary>
        /// Set to true (1) to ignore errors related to invalid SSL certificates.
        /// Enabling this setting can lead to potential security vulnerabilities like
        /// "man in the middle" attacks. Applications that load content from the
        /// internet should not enable this setting. Also configurable using the
        /// "ignore-certificate-errors" command-line switch. Can be overridden for
        /// individual CefRequestContext instances via the
        /// CefRequestContextSettings.ignore_certificate_errors value.
        /// </summary>
        public bool IgnoreCertificateErrors { get; set; }

        /// <summary>
        /// Background color used for the browser before a document is loaded and when
        /// no document color is specified. The alpha component must be either fully
        /// opaque (0xFF) or fully transparent (0x00). If the alpha component is fully
        /// opaque then the RGB components will be used as the background color. If the
        /// alpha component is fully transparent for a windowed browser then the
        /// default value of opaque white be used. If the alpha component is fully
        /// transparent for a windowless (off-screen) browser then transparent painting
        /// will be enabled.
        /// </summary>
        public CefColor BackgroundColor { get; set; }

        /// <summary>
        /// Comma delimited ordered list of language codes without any whitespace that
        /// will be used in the "Accept-Language" HTTP header. May be overridden on a
        /// per-browser basis using the CefBrowserSettings.accept_language_list value.
        /// If both values are empty then "en-US,en" will be used. Can be overridden
        /// for individual CefRequestContext instances via the
        /// CefRequestContextSettings.accept_language_list value.
        /// </summary>
        public string AcceptLanguageList { get; set; }

        /// <summary>
        /// GUID string used for identifying the application. This is passed to the
        /// system AV function for scanning downloaded files. By default, the GUID
        /// will be an empty string and the file will be treated as an untrusted
        /// file when the GUID is empty.
        /// </summary>
        public string ApplicationClientIdForFileScanning { get; set; }

        internal cef_settings_t* ToNative()
        {
            var ptr = cef_settings_t.Alloc();
            ptr->no_sandbox = NoSandbox ? 1 : 0;
            cef_string_t.Copy(BrowserSubprocessPath, &ptr->browser_subprocess_path);
            cef_string_t.Copy(FrameworkDirPath, &ptr->framework_dir_path);
            cef_string_t.Copy(MainBundlePath, &ptr->main_bundle_path);
            ptr->chrome_runtime = ChromeRuntime ? 1 : 0;
            ptr->multi_threaded_message_loop = MultiThreadedMessageLoop ? 1 : 0;
            ptr->windowless_rendering_enabled = WindowlessRenderingEnabled ? 1 : 0;
            ptr->external_message_pump = ExternalMessagePump ? 1 : 0;
            ptr->command_line_args_disabled = CommandLineArgsDisabled ? 1 : 0;
            cef_string_t.Copy(CachePath, &ptr->cache_path);
            cef_string_t.Copy(RootCachePath, &ptr->root_cache_path);
            cef_string_t.Copy(UserDataPath, &ptr->user_data_path);
            ptr->persist_session_cookies = PersistSessionCookies ? 1 : 0;
            ptr->persist_user_preferences = PersistUserPreferences ? 1 : 0;
            cef_string_t.Copy(UserAgent, &ptr->user_agent);
            cef_string_t.Copy(ProductVersion, &ptr->product_version);
            cef_string_t.Copy(Locale, &ptr->locale);
            cef_string_t.Copy(LogFile, &ptr->log_file);
            ptr->log_severity = LogSeverity;
            cef_string_t.Copy(JavaScriptFlags, &ptr->javascript_flags);
            cef_string_t.Copy(ResourcesDirPath, &ptr->resources_dir_path);
            cef_string_t.Copy(LocalesDirPath, &ptr->locales_dir_path);
            ptr->pack_loading_disabled = PackLoadingDisabled ? 1 : 0;
            ptr->remote_debugging_port = RemoteDebuggingPort;
            ptr->uncaught_exception_stack_size = UncaughtExceptionStackSize;
            ptr->ignore_certificate_errors = IgnoreCertificateErrors ? 1 : 0;
            ptr->background_color = BackgroundColor.ToArgb();
            cef_string_t.Copy(AcceptLanguageList, &ptr->accept_language_list);
            cef_string_t.Copy(ApplicationClientIdForFileScanning, &ptr->application_client_id_for_file_scanning);
            return ptr;
        }

        private static void Clear(cef_settings_t* ptr)
        {
            libcef.string_clear(&ptr->browser_subprocess_path);
            libcef.string_clear(&ptr->framework_dir_path);
            libcef.string_clear(&ptr->main_bundle_path);
            libcef.string_clear(&ptr->cache_path);
            libcef.string_clear(&ptr->root_cache_path);
            libcef.string_clear(&ptr->user_data_path);
            libcef.string_clear(&ptr->user_agent);
            libcef.string_clear(&ptr->product_version);
            libcef.string_clear(&ptr->locale);
            libcef.string_clear(&ptr->log_file);
            libcef.string_clear(&ptr->javascript_flags);
            libcef.string_clear(&ptr->resources_dir_path);
            libcef.string_clear(&ptr->locales_dir_path);
            libcef.string_clear(&ptr->accept_language_list);
            libcef.string_clear(&ptr->application_client_id_for_file_scanning);
        }

        internal static void Free(cef_settings_t* ptr)
        {
            Clear(ptr);
            cef_settings_t.Free(ptr);
        }
    }
}
