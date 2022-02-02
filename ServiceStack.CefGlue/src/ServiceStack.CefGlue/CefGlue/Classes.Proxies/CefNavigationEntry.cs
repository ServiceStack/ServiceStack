namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to represent an entry in navigation history.
    /// </summary>
    public sealed unsafe partial class CefNavigationEntry
    {
        /// <summary>
        /// Returns true if this object is valid. Do not call any other methods if this
        /// function returns false.
        /// </summary>
        public bool IsValid
        {
            get { return cef_navigation_entry_t.is_valid(_self) != 0; }
        }

        /// <summary>
        /// Returns the actual URL of the page. For some pages this may be data: URL or
        /// similar. Use GetDisplayURL() to return a display-friendly version.
        /// </summary>
        public string Url
        {
            get
            {
                var n_value = cef_navigation_entry_t.get_url(_self);
                return cef_string_userfree.ToString(n_value);
            }
        }

        /// <summary>
        /// Returns a display-friendly version of the URL.
        /// </summary>
        public string DisplayUrl
        {
            get
            {
                var n_value = cef_navigation_entry_t.get_display_url(_self);
                return cef_string_userfree.ToString(n_value);
            }
        }

        /// <summary>
        /// Returns the original URL that was entered by the user before any redirects.
        /// </summary>
        public string OriginalUrl
        {
            get
            {
                var n_value = cef_navigation_entry_t.get_original_url(_self);
                return cef_string_userfree.ToString(n_value);
            }
        }

        /// <summary>
        /// Returns the title set by the page. This value may be empty.
        /// </summary>
        public string Title
        {
            get
            {
                var n_value = cef_navigation_entry_t.get_title(_self);
                return cef_string_userfree.ToString(n_value);
            }
        }

        /// <summary>
        /// Returns the transition type which indicates what the user did to move to
        /// this page from the previous page.
        /// </summary>
        public CefTransitionType TransitionType
        {
            get { return cef_navigation_entry_t.get_transition_type(_self); }
        }

        /// <summary>
        /// Returns true if this navigation includes post data.
        /// </summary>
        public bool HasPostData
        {
            get { return cef_navigation_entry_t.has_post_data(_self) != 0; }
        }

        /// <summary>
        /// Returns the time for the last known successful navigation completion. A
        /// navigation may be completed more than once if the page is reloaded. May be
        /// 0 if the navigation has not yet completed.
        /// </summary>
        public DateTime CompletionTime
        {
            get
            {
                var n_value = cef_navigation_entry_t.get_completion_time(_self);
                return n_value.ToDateTime();
            }
        }

        /// <summary>
        /// Returns the HTTP status code for the last known successful navigation
        /// response. May be 0 if the response has not yet been received or if the
        /// navigation has not yet completed.
        /// </summary>
        public int HttpStatusCode
        {
            get { return cef_navigation_entry_t.get_http_status_code(_self); }
        }

        /// <summary>
        /// Returns the SSL information for this navigation entry.
        /// </summary>
        public CefSslStatus GetSslStatus()
        {
            return CefSslStatus.FromNative(
                cef_navigation_entry_t.get_sslstatus(_self)
                );
        }
    }
}
