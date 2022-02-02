namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing a X.509 certificate.
    /// </summary>
    public sealed unsafe partial class CefX509Certificate
    {
        /// <summary>
        /// Returns the subject of the X.509 certificate. For HTTPS server
        /// certificates this represents the web server.  The common name of the
        /// subject should match the host name of the web server.
        /// </summary>
        public CefX509CertPrincipal GetSubject()
        {
            return CefX509CertPrincipal.FromNative(
                cef_x509certificate_t.get_subject(_self)
                );
        }

        /// <summary>
        /// Returns the issuer of the X.509 certificate.
        /// </summary>
        public CefX509CertPrincipal GetIssuer()
        {
            return CefX509CertPrincipal.FromNative(
                cef_x509certificate_t.get_issuer(_self)
                );
        }

        /// <summary>
        /// Returns the DER encoded serial number for the X.509 certificate. The value
        /// possibly includes a leading 00 byte.
        /// </summary>
        public CefBinaryValue GetSerialNumber()
        {
            var n_result = cef_x509certificate_t.get_serial_number(_self);
            return CefBinaryValue.FromNative(n_result);
        }

        /// <summary>
        /// Returns the date before which the X.509 certificate is invalid.
        /// CefTime.GetTimeT() will return 0 if no date was specified.
        /// </summary>
        public DateTime GetValidStart()
        {
            var n_result = cef_x509certificate_t.get_valid_start(_self);
            return cef_time_t.ToDateTime(&n_result);
        }

        /// <summary>
        /// Returns the date after which the X.509 certificate is invalid.
        /// CefTime.GetTimeT() will return 0 if no date was specified.
        /// </summary>
        public DateTime GetValidExpiry()
        {
            var n_result = cef_x509certificate_t.get_valid_expiry(_self);
            return cef_time_t.ToDateTime(&n_result);
        }

        /// <summary>
        /// Returns the DER encoded data for the X.509 certificate.
        /// </summary>
        public CefBinaryValue GetDerEncoded()
        {
            var n_result = cef_x509certificate_t.get_derencoded(_self);
            return CefBinaryValue.FromNative(n_result);
        }

        /// <summary>
        /// Returns the PEM encoded data for the X.509 certificate.
        /// </summary>
        public CefBinaryValue GetPemEncoded()
        {
            var n_result = cef_x509certificate_t.get_pemencoded(_self);
            return CefBinaryValue.FromNative(n_result);
        }

        /// <summary>
        /// Returns the number of certificates in the issuer chain.
        /// If 0, the certificate is self-signed.
        /// </summary>
        public long GetIssuerChainSize()
        {
            return (long)cef_x509certificate_t.get_issuer_chain_size(_self);
        }

        /// <summary>
        /// Returns the DER encoded data for the certificate issuer chain.
        /// If we failed to encode a certificate in the chain it is still
        /// present in the array but is an empty string.
        /// </summary>
        public void GetDerEncodedIssuerChain(out long chainCount, out CefBinaryValue chain)
        {
            UIntPtr n_chainCount;
            cef_binary_value_t* n_chain;

            cef_x509certificate_t.get_derencoded_issuer_chain(_self, &n_chainCount, &n_chain);

            chainCount = (long)n_chainCount;
            chain = CefBinaryValue.FromNative(n_chain);
        }

        /// <summary>
        /// Returns the PEM encoded data for the certificate issuer chain.
        /// If we failed to encode a certificate in the chain it is still
        /// present in the array but is an empty string.
        /// </summary>
        public void GetPEMEncodedIssuerChain(out long chainCount, out CefBinaryValue chain)
        {
            UIntPtr n_chainCount;
            cef_binary_value_t* n_chain;

            cef_x509certificate_t.get_pemencoded_issuer_chain(_self, &n_chainCount, &n_chain);

            chainCount = (long)n_chainCount;
            chain = CefBinaryValue.FromNative(n_chain);
        }
    }
}
