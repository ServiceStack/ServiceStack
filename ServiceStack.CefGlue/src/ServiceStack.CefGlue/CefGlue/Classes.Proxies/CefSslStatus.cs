namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing the SSL information for a navigation entry.
    /// </summary>
    public sealed unsafe partial class CefSslStatus
    {
        /// <summary>
        /// Returns true if the status is related to a secure SSL/TLS connection.
        /// </summary>
        public bool IsSecureConnection
        {
            get { return cef_sslstatus_t.is_secure_connection(_self) != 0; }
        }

        /// <summary>
        /// Returns a bitmask containing any and all problems verifying the server
        /// certificate.
        /// </summary>
        public CefCertStatus CertStatus
        {
            get { return cef_sslstatus_t.get_cert_status(_self); }
        }

        /// <summary>
        /// Returns the SSL version used for the SSL connection.
        /// </summary>
        public CefSslVersion SslVersion
        {
            get { return cef_sslstatus_t.get_sslversion(_self); }
        }

        /// <summary>
        /// Returns a bitmask containing the page security content status.
        /// </summary>
        public CefSslContentStatus ContentStatus
        {
            get { return cef_sslstatus_t.get_content_status(_self); }
        }

        /// <summary>
        /// Returns the X.509 certificate.
        /// </summary>
        public CefX509Certificate GetX509Certificate()
        {
            return CefX509Certificate.FromNative(
                cef_sslstatus_t.get_x509certificate(_self)
                );
        }
    }
}
