namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to represent a web request. The methods of this class may be
    /// called on any thread.
    /// </summary>
    public sealed unsafe partial class CefRequest
    {
        /// <summary>
        /// Create a new CefRequest object.
        /// </summary>
        public static CefRequest Create()
        {
            return CefRequest.FromNative(
                cef_request_t.create()
                );
        }

        /// <summary>
        /// Returns true if this object is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return cef_request_t.is_read_only(_self) != 0; }
        }

        /// <summary>
        /// Gets or sets the fully qualified URL.
        /// </summary>
        public string Url
        {
            get
            {
                var n_result = cef_request_t.get_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");

                fixed (char* value_str = value)
                {
                    var n_value = new cef_string_t(value_str, value.Length);
                    cef_request_t.set_url(_self, &n_value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the request method type.
        /// The value will default to POST if post data is provided and GET otherwise.
        /// </summary>
        public string Method
        {
            get
            {
                var n_result = cef_request_t.get_method(_self);
                return cef_string_userfree.ToString(n_result);
            }
            set
            {
                fixed (char* value_str = value)
                {
                    var n_value = new cef_string_t(value_str, value != null ? value.Length : 0);
                    cef_request_t.set_method(_self, &n_value);
                }
            }
        }

        /// <summary>
        /// Set the referrer URL and policy. If non-empty the referrer URL must be
        /// fully qualified with an HTTP or HTTPS scheme component. Any username,
        /// password or ref component will be removed.
        /// </summary>
        public void SetReferrer(string referrerUrl, CefReferrerPolicy policy)
        {
            fixed (char* referrerUrl_str = referrerUrl)
            {
                var n_referrerUrl = new cef_string_t(referrerUrl_str, referrerUrl != null ? referrerUrl.Length : 0);
                cef_request_t.set_referrer(_self, &n_referrerUrl, policy);
            }
        }

        /// <summary>
        /// Get the referrer URL.
        /// </summary>
        public string ReferrerURL
        {
            get
            {
                var n_result = cef_request_t.get_referrer_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Get the referrer policy.
        /// </summary>
        public CefReferrerPolicy ReferrerPolicy
        {
            get
            {
                return cef_request_t.get_referrer_policy(_self);
            }
        }

        /// <summary>
        /// Get the post data.
        /// </summary>
        public CefPostData PostData
        {
            get
            {
                return CefPostData.FromNativeOrNull(
                    cef_request_t.get_post_data(_self)
                    );
            }
            set
            {
                var n_value = value != null ? value.ToNative() : null;
                cef_request_t.set_post_data(_self, n_value);
            }
        }

        /// <summary>
        /// Get the header values. Will not include the Referer value if any.
        /// </summary>
        public NameValueCollection GetHeaderMap()
        {
            var headerMap = libcef.string_multimap_alloc();
            cef_request_t.get_header_map(_self, headerMap);
            var result = cef_string_multimap.ToNameValueCollection(headerMap);
            libcef.string_multimap_free(headerMap);
            return result;
        }

        /// <summary>
        /// Set the header values. If a Referer value exists in the header map it will
        /// be removed and ignored.
        /// </summary>
        public void SetHeaderMap(NameValueCollection headers)
        {
            var headerMap = cef_string_multimap.From(headers);
            cef_request_t.set_header_map(_self, headerMap);
            libcef.string_multimap_free(headerMap);
        }

        /// <summary>
        /// Returns the first header value for |name| or an empty string if not found.
        /// Will not return the Referer value if any. Use GetHeaderMap instead if
        /// |name| might have multiple values.
        /// </summary>
        public string GetHeaderByName(string name)
        {
            fixed (char* name_str = name)
            {
                var n_name = new cef_string_t(name_str, name != null ? name.Length : 0);
                var n_result = cef_request_t.get_header_by_name(_self, &n_name);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Set the header |name| to |value|. If |overwrite| is true any existing
        /// values will be replaced with the new value. If |overwrite| is false any
        /// existing values will not be overwritten. The Referer value cannot be set
        /// using this method.
        /// </summary>
        public void SetHeaderByName(string name, string value, bool overwrite)
        {
            fixed (char* name_str = name)
            fixed (char* value_str = value)
            {
                var n_name = new cef_string_t(name_str, name != null ? name.Length : 0);
                var n_value = new cef_string_t(value_str, value != null ? value.Length : 0);
                cef_request_t.set_header_by_name(_self, &n_name, &n_value, overwrite ? 1 : 0);
            }
        }

        /// <summary>
        /// Set all values at one time.
        /// </summary>
        public void Set(string url, string method, CefPostData postData, NameValueCollection headers)
        {
            fixed (char* url_str = url)
            fixed (char* method_str = method)
            {
                var n_url = new cef_string_t(url_str, url != null ? url.Length : 0);
                var n_method = new cef_string_t(method_str, method_str != null ? method.Length : 0);
                var n_postData = postData != null ? postData.ToNative() : null;
                var n_headerMap = cef_string_multimap.From(headers);
                cef_request_t.set(_self, &n_url, &n_method, n_postData, n_headerMap);
                libcef.string_multimap_free(n_headerMap);
            }
        }

        /// <summary>
        /// Get the options used in combination with CefUrlRequest.
        /// </summary>
        public CefUrlRequestOptions Options
        {
            get { return (CefUrlRequestOptions)cef_request_t.get_flags(_self); }
            set { cef_request_t.set_flags(_self, (int)value); }
        }

        /// <summary>
        /// Gets or sets the URL to the first party for cookies used in combination with
        /// CefURLRequest.
        /// </summary>
        public string FirstPartyForCookies
        {
            get
            {
                var n_result = cef_request_t.get_first_party_for_cookies(_self);
                return cef_string_userfree.ToString(n_result);
            }
            set
            {
                fixed (char* value_str = value)
                {
                    var n_value = new cef_string_t(value_str, value != null ? value.Length : 0);
                    cef_request_t.set_first_party_for_cookies(_self, &n_value);
                }
            }
        }

        /// <summary>
        /// Get the resource type for this request. Only available in the browser
        /// process.
        /// </summary>
        public CefResourceType ResourceType
        {
            get
            {
                return cef_request_t.get_resource_type(_self);
            }
        }

        /// <summary>
        /// Get the transition type for this request. Only available in the browser
        /// process and only applies to requests that represent a main frame or
        /// sub-frame navigation.
        /// </summary>
        public CefTransitionType TransitionType
        {
            get
            {
                return cef_request_t.get_transition_type(_self);
            }
        }

        /// <summary>
        /// Returns the globally unique identifier for this request or 0 if not
        /// specified. Can be used by CefResourceRequestHandler implementations in the
        /// browser process to track a single request across multiple callbacks.
        /// </summary>
        public ulong Identifier
        {
            get
            {
                return cef_request_t.get_identifier(_self);
            }
        }
    }
}
