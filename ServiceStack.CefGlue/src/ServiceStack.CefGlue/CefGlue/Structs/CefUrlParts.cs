namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// URL component parts.
    /// </summary>
    public sealed class CefUrlParts
    {
        /// <summary>
        /// The complete URL specification.
        /// </summary>
        public string Spec { get; set; }

        /// <summary>
        /// Scheme component not including the colon (e.g., "http").
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// User name component.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Password component.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Host component. This may be a hostname, an IPv4 address or an IPv6 literal
        /// surrounded by square brackets (e.g., "[2001:db8::1]").
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Port number component.
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// Origin contains just the scheme, host, and port from a URL. Equivalent to
        /// clearing any username and password, replacing the path with a slash, and
        /// clearing everything after that. This value will be empty for non-standard
        /// URLs.
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// Path component including the first slash following the host.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Query string component (i.e., everything following the '?').
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Fragment (hash) identifier component (i.e., the string following the '#').
        /// </summary>
        public string Fragment { get; set; }

        internal static unsafe CefUrlParts FromNative(cef_urlparts_t* n_parts)
        {
            var result = new CefUrlParts();
            result.Spec = cef_string_t.ToString(&n_parts->spec);
            result.Scheme = cef_string_t.ToString(&n_parts->scheme);
            result.UserName = cef_string_t.ToString(&n_parts->username);
            result.Password = cef_string_t.ToString(&n_parts->password);
            result.Host = cef_string_t.ToString(&n_parts->host);
            result.Port = cef_string_t.ToString(&n_parts->port);
            result.Origin = cef_string_t.ToString(&n_parts->origin);
            result.Path = cef_string_t.ToString(&n_parts->path);
            result.Query = cef_string_t.ToString(&n_parts->query);
            result.Fragment = cef_string_t.ToString(&n_parts->fragment);
            return result;
        }

        internal unsafe cef_urlparts_t ToNative()
        {
            var result = new cef_urlparts_t();
            cef_string_t.Copy(Spec, &result.spec);
            cef_string_t.Copy(Scheme, &result.scheme);
            cef_string_t.Copy(UserName, &result.username);
            cef_string_t.Copy(Password, &result.password);
            cef_string_t.Copy(Host, &result.host);
            cef_string_t.Copy(Port, &result.port);
            cef_string_t.Copy(Origin, &result.origin);
            cef_string_t.Copy(Path, &result.path);
            cef_string_t.Copy(Query, &result.query);
            cef_string_t.Copy(Fragment, &result.fragment);
            return result;
        }
    }
}
