namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Cookie information.
    /// </summary>
    public sealed unsafe class CefCookie
    {
        public CefCookie()
        { }

        /// <summary>
        /// The cookie name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The cookie value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// If |domain| is empty a host cookie will be created instead of a domain
        /// cookie. Domain cookies are stored with a leading "." and are visible to
        /// sub-domains whereas host cookies are not.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// If |path| is non-empty only URLs at or below the path will get the cookie
        /// value.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// If |secure| is true the cookie will only be sent for HTTPS requests.
        /// </summary>
        public bool Secure { get; set; }

        /// <summary>
        /// If |httponly| is true the cookie will only be sent for HTTP requests.
        /// </summary>
        public bool HttpOnly { get; set; }

        /// <summary>
        /// The cookie creation date. This is automatically populated by the system on
        /// cookie creation.
        /// </summary>
        public DateTime Creation { get; set; }

        /// <summary>
        /// The cookie last access date. This is automatically populated by the system
        /// on access.
        /// </summary>
        public DateTime LastAccess { get; set; }

        /// <summary>
        /// The cookie expiration date.
        /// </summary>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// Same site.
        /// </summary>
        public CefCookieSameSite SameSite { get; set; }

        /// <summary>
        /// Priority.
        /// </summary>
        public CefCookiePriority Priority { get; set; }

        internal static CefCookie FromNative(cef_cookie_t* ptr)
        {
            return new CefCookie
                {
                    Name = cef_string_t.ToString(&ptr->name),
                    Value = cef_string_t.ToString(&ptr->value),
                    Domain = cef_string_t.ToString(&ptr->domain),
                    Path = cef_string_t.ToString(&ptr->path),
                    Secure = ptr->secure != 0,
                    HttpOnly = ptr->httponly != 0,
                    Creation = cef_time_t.ToDateTime(&ptr->creation),
                    LastAccess = cef_time_t.ToDateTime(&ptr->last_access),
                    Expires = ptr->has_expires != 0 ? (DateTime?)cef_time_t.ToDateTime(&ptr->expires) : null,
                    SameSite = ptr->same_site,
                    Priority = ptr->priority,
                };
        }

        internal cef_cookie_t* ToNative()
        {
            var ptr = cef_cookie_t.Alloc();

            cef_string_t.Copy(Name, &ptr->name);
            cef_string_t.Copy(Value, &ptr->value);
            cef_string_t.Copy(Domain, &ptr->domain);
            cef_string_t.Copy(Path, &ptr->path);
            ptr->secure = Secure ? 1 : 0;
            ptr->httponly = HttpOnly ? 1 : 0;
            ptr->creation = new cef_time_t(Creation);
            ptr->last_access = new cef_time_t(LastAccess);
            ptr->has_expires = Expires != null ? 1 : 0;
            ptr->expires = Expires != null ? new cef_time_t(Expires.Value) : new cef_time_t();
            ptr->same_site = SameSite;
            ptr->priority = Priority;

            return ptr;
        }

        internal static void Free(cef_cookie_t* ptr)
        {
            cef_cookie_t.Clear((cef_cookie_t*)ptr);
            cef_cookie_t.Free((cef_cookie_t*)ptr);
        }
    }
}
