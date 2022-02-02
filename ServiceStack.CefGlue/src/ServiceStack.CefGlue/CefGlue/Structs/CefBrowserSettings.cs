namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Browser initialization settings. Specify <c>null</c> or 0 to get the recommended
    /// default values. The consequences of using custom values may not be well
    /// tested. Many of these and other settings can also configured using command-
    /// line switches.
    /// </summary>
    public sealed unsafe class CefBrowserSettings
    {
        private cef_browser_settings_t* _self;

        public CefBrowserSettings()
        {
            _self = cef_browser_settings_t.Alloc();
        }

        internal CefBrowserSettings(cef_browser_settings_t* ptr)
        {
            _self = ptr;
        }

        internal void Dispose()
        {
            _self = null;
        }

        internal cef_browser_settings_t* ToNative()
        {
            return _self;
        }

        /// <summary>
        /// The maximum rate in frames per second (fps) that CefRenderHandler::OnPaint
        /// will be called for a windowless browser. The actual fps may be lower if
        /// the browser cannot generate frames at the requested rate. The minimum
        /// value is 1 and the maximum value is 60 (default 30). This value can also be
        /// changed dynamically via CefBrowserHost::SetWindowlessFrameRate.
        /// </summary>
        public int WindowlessFrameRate
        {
            get { return _self->windowless_frame_rate; }
            set { _self->windowless_frame_rate = value; }
        }

        // The below values map to WebPreferences settings.

        #region Font Settings

        public string StandardFontFamily
        {
            get { return cef_string_t.ToString(&_self->standard_font_family); }
            set { cef_string_t.Copy(value, &_self->standard_font_family); }
        }

        public string FixedFontFamily
        {
            get { return cef_string_t.ToString(&_self->fixed_font_family); }
            set { cef_string_t.Copy(value, &_self->fixed_font_family); }
        }

        public string SerifFontFamily
        {
            get { return cef_string_t.ToString(&_self->serif_font_family); }
            set { cef_string_t.Copy(value, &_self->serif_font_family); }
        }

        public string SansSerifFontFamily
        {
            get { return cef_string_t.ToString(&_self->sans_serif_font_family); }
            set { cef_string_t.Copy(value, &_self->sans_serif_font_family); }
        }

        public string CursiveFontFamily
        {
            get { return cef_string_t.ToString(&_self->cursive_font_family); }
            set { cef_string_t.Copy(value, &_self->cursive_font_family); }
        }

        public string FantasyFontFamily
        {
            get { return cef_string_t.ToString(&_self->fantasy_font_family); }
            set { cef_string_t.Copy(value, &_self->fantasy_font_family); }
        }

        public int DefaultFontSize
        {
            get { return _self->default_font_size; }
            set { _self->default_font_size = value; }
        }

        public int DefaultFixedFontSize
        {
            get { return _self->default_fixed_font_size; }
            set { _self->default_fixed_font_size = value; }
        }

        public int MinimumFontSize
        {
            get { return _self->minimum_font_size; }
            set { _self->minimum_font_size = value; }
        }

        public int MinimumLogicalFontSize
        {
            get { return _self->minimum_logical_font_size; }
            set { _self->minimum_logical_font_size = value; }
        }

        #endregion


        /// <summary>
        /// Default encoding for Web content. If empty "ISO-8859-1" will be used. Also
        /// configurable using the "default-encoding" command-line switch.
        /// </summary>
        public string DefaultEncoding
        {
            get { return cef_string_t.ToString(&_self->default_encoding); }
            set { cef_string_t.Copy(value, &_self->default_encoding); }
        }


        /// <summary>
        /// Controls the loading of fonts from remote sources. Also configurable using
        /// the "disable-remote-fonts" command-line switch.
        /// </summary>
        public CefState RemoteFonts
        {
            get { return _self->remote_fonts; }
            set { _self->remote_fonts = value; }
        }

        /// <summary>
        /// Controls whether JavaScript can be executed. Also configurable using the
        /// "disable-javascript" command-line switch.
        /// </summary>
        public CefState JavaScript
        {
            get { return _self->javascript; }
            set { _self->javascript = value; }
        }

        /// <summary>
        /// Controls whether JavaScript can be used to close windows that were not
        /// opened via JavaScript. JavaScript can still be used to close windows that
        /// were opened via JavaScript or that have no back/forward history. Also
        /// configurable using the "disable-javascript-close-windows" command-line
        /// switch.
        /// </summary>
        public CefState JavaScriptCloseWindows
        {
            get { return _self->javascript_close_windows; }
            set { _self->javascript_close_windows = value; }
        }

        /// <summary>
        /// Controls whether JavaScript can access the clipboard. Also configurable
        /// using the "disable-javascript-access-clipboard" command-line switch.
        /// </summary>
        public CefState JavaScriptAccessClipboard
        {
            get { return _self->javascript_access_clipboard; }
            set { _self->javascript_access_clipboard = value; }
        }

        /// <summary>
        /// Controls whether DOM pasting is supported in the editor via
        /// execCommand("paste"). The |javascript_access_clipboard| setting must also
        /// be enabled. Also configurable using the "disable-javascript-dom-paste"
        /// command-line switch.
        /// </summary>
        public CefState JavaScriptDomPaste
        {
            get { return _self->javascript_dom_paste; }
            set { _self->javascript_dom_paste = value; }
        }

        /// <summary>
        /// Controls whether any plugins will be loaded. Also configurable using the
        /// "disable-plugins" command-line switch.
        /// </summary>
        public CefState Plugins
        {
            get { return _self->plugins; }
            set { _self->plugins = value; }
        }

        /// <summary>
        /// Controls whether file URLs will have access to all URLs. Also configurable
        /// using the "allow-universal-access-from-files" command-line switch.
        /// </summary>
        public CefState UniversalAccessFromFileUrls
        {
            get { return _self->universal_access_from_file_urls; }
            set { _self->universal_access_from_file_urls = value; }
        }

        /// <summary>
        /// Controls whether file URLs will have access to other file URLs. Also
        /// configurable using the "allow-access-from-files" command-line switch.
        /// </summary>
        public CefState FileAccessFromFileUrls
        {
            get { return _self->file_access_from_file_urls; }
            set { _self->file_access_from_file_urls = value; }
        }

        /// <summary>
        /// Controls whether web security restrictions (same-origin policy) will be
        /// enforced. Disabling this setting is not recommend as it will allow risky
        /// security behavior such as cross-site scripting (XSS). Also configurable
        /// using the "disable-web-security" command-line switch.
        /// </summary>
        public CefState WebSecurity
        {
            get { return _self->web_security; }
            set { _self->web_security = value; }
        }

        /// <summary>
        /// Controls whether image URLs will be loaded from the network. A cached image
        /// will still be rendered if requested. Also configurable using the
        /// "disable-image-loading" command-line switch.
        /// </summary>
        public CefState ImageLoading
        {
            get { return _self->image_loading; }
            set { _self->image_loading = value; }
        }

        /// <summary>
        /// Controls whether standalone images will be shrunk to fit the page. Also
        /// configurable using the "image-shrink-standalone-to-fit" command-line
        /// switch.
        /// </summary>
        public CefState ImageShrinkStandaloneToFit
        {
            get { return _self->image_shrink_standalone_to_fit; }
            set { _self->image_shrink_standalone_to_fit = value; }
        }

        /// <summary>
        /// Controls whether text areas can be resized. Also configurable using the
        /// "disable-text-area-resize" command-line switch.
        /// </summary>
        public CefState TextAreaResize
        {
            get { return _self->text_area_resize; }
            set { _self->text_area_resize = value; }
        }

        /// <summary>
        /// Controls whether the tab key can advance focus to links. Also configurable
        /// using the "disable-tab-to-links" command-line switch.
        /// </summary>
        public CefState TabToLinks
        {
            get { return _self->tab_to_links; }
            set { _self->tab_to_links = value; }
        }

        /// <summary>
        /// Controls whether local storage can be used. Also configurable using the
        /// "disable-local-storage" command-line switch.
        /// </summary>
        public CefState LocalStorage
        {
            get { return _self->local_storage; }
            set { _self->local_storage = value; }
        }

        /// <summary>
        /// Controls whether databases can be used. Also configurable using the
        /// "disable-databases" command-line switch.
        /// </summary>
        public CefState Databases
        {
            get { return _self->databases; }
            set { _self->databases = value; }
        }

        /// <summary>
        /// Controls whether the application cache can be used. Also configurable using
        /// the "disable-application-cache" command-line switch.
        /// </summary>
        public CefState ApplicationCache
        {
            get { return _self->application_cache; }
            set { _self->application_cache = value; }
        }

        /// <summary>
        /// Controls whether WebGL can be used. Note that WebGL requires hardware
        /// support and may not work on all systems even when enabled. Also
        /// configurable using the "disable-webgl" command-line switch.
        /// </summary>
        public CefState WebGL
        {
            get { return _self->webgl; }
            set { _self->webgl = value; }
        }

        /// <summary>
        /// Background color used for the browser before a document is loaded and when
        /// no document color is specified. The alpha component must be either fully
        /// opaque (0xFF) or fully transparent (0x00). If the alpha component is fully
        /// opaque then the RGB components will be used as the background color. If the
        /// alpha component is fully transparent for a windowed browser then the
        /// CefSettings.background_color value will be used. If the alpha component is
        /// fully transparent for a windowless (off-screen) browser then transparent
        /// painting will be enabled.
        /// </summary>
        public CefColor BackgroundColor
        {
            get { return new CefColor(_self->background_color); }
            set { _self->background_color = value.ToArgb(); }
        }

        /// <summary>
        /// Comma delimited ordered list of language codes without any whitespace that
        /// will be used in the "Accept-Language" HTTP header. May be set globally
        /// using the CefBrowserSettings.accept_language_list value. If both values are
        /// empty then "en-US,en" will be used.
        /// </summary>
        public string AcceptLanguageList
        {
            get { return cef_string_t.ToString(&_self->accept_language_list); }
            set { cef_string_t.Copy(value, &_self->accept_language_list); }
        }
    }
}
