//
// This file manually written from cef/include/base/internal/cef_net_error_list.h.
// C API name: cef_errorcode_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Supported error code values.
    /// </summary>
    public enum CefErrorCode
    {
        None = 0,

        //
        // Ranges:
        //     0- 99 System related errors
        //   100-199 Connection related errors
        //   200-299 Certificate errors
        //   300-399 HTTP errors
        //   400-499 Cache errors
        //   500-599 ?
        //   600-699 FTP errors
        //   700-799 Certificate manager errors
        //   800-899 DNS resolver errors

        /// <summary>
        /// An asynchronous IO operation is not yet complete.  This usually does not
        /// indicate a fatal error.  Typically this error will be generated as a
        /// notification to wait for some external notification that the IO operation
        /// finally completed.
        /// </summary>
        IO_PENDING = -1,

        /// <summary>
        /// A generic failure occurred.
        /// </summary>
        FAILED = -2,

        /// <summary>
        /// An operation was aborted (due to user action).
        /// </summary>
        ABORTED = -3,

        /// <summary>
        /// An argument to the function is incorrect.
        /// </summary>
        INVALID_ARGUMENT = -4,

        /// <summary>
        /// The handle or file descriptor is invalid.
        /// </summary>
        INVALID_HANDLE = -5,

        /// <summary>
        /// The file or directory cannot be found.
        /// </summary>
        FILE_NOT_FOUND = -6,

        /// <summary>
        /// An operation timed out.
        /// </summary>
        TIMED_OUT = -7,

        /// <summary>
        /// The file is too large.
        /// </summary>
        FILE_TOO_BIG = -8,

        /// <summary>
        /// An unexpected error.  This may be caused by a programming mistake or an
        /// invalid assumption.
        /// </summary>
        UNEXPECTED = -9,

        /// <summary>
        /// Permission to access a resource, other than the network, was denied.
        /// </summary>
        ACCESS_DENIED = -10,

        /// <summary>
        /// The operation failed because of unimplemented functionality.
        /// </summary>
        NOT_IMPLEMENTED = -11,

        /// <summary>
        /// There were not enough resources to complete the operation.
        /// </summary>
        INSUFFICIENT_RESOURCES = -12,

        /// <summary>
        /// Memory allocation failed.
        /// </summary>
        OUT_OF_MEMORY = -13,

        /// <summary>
        /// The file upload failed because the file's modification time was different
        /// from the expectation.
        /// </summary>
        UPLOAD_FILE_CHANGED = -14,

        /// <summary>
        /// The socket is not connected.
        /// </summary>
        SOCKET_NOT_CONNECTED = -15,

        /// <summary>
        /// The file already exists.
        /// </summary>
        FILE_EXISTS = -16,

        /// <summary>
        /// The path or file name is too long.
        /// </summary>
        FILE_PATH_TOO_LONG = -17,

        /// <summary>
        /// Not enough room left on the disk.
        /// </summary>
        FILE_NO_SPACE = -18,

        /// <summary>
        /// The file has a virus.
        /// </summary>
        FILE_VIRUS_INFECTED = -19,

        /// <summary>
        /// The client chose to block the request.
        /// </summary>
        BLOCKED_BY_CLIENT = -20,

        /// <summary>
        /// The network changed.
        /// </summary>
        NETWORK_CHANGED = -21,

        /// <summary>
        /// The request was blocked by the URL block list configured by the domain
        /// administrator.
        /// </summary>
        BLOCKED_BY_ADMINISTRATOR = -22,

        /// <summary>
        /// The socket is already connected.
        /// </summary>
        SOCKET_IS_CONNECTED = -23,

        /// <summary>
        /// The request was blocked because the forced reenrollment check is still
        /// pending. This error can only occur on ChromeOS.
        /// The error can be emitted by code in chrome/browser/policy/policy_helpers.cc.
        /// </summary>
        BLOCKED_ENROLLMENT_CHECK_PENDING = -24,

        /// <summary>
        /// The upload failed because the upload stream needed to be re-read, due to a
        /// retry or a redirect, but the upload stream doesn't support that operation.
        /// </summary>
        UPLOAD_STREAM_REWIND_NOT_SUPPORTED = -25,

        /// <summary>
        /// The request failed because the URLRequestContext is shutting down, or has
        /// been shut down.
        /// </summary>
        CONTEXT_SHUT_DOWN = -26,

        /// <summary>
        /// The request failed because the response was delivered along with requirements
        /// which are not met ('X-Frame-Options' and 'Content-Security-Policy' ancestor
        /// checks and 'Cross-Origin-Resource-Policy', for instance).
        /// </summary>
        BLOCKED_BY_RESPONSE = -27,

        // Error -28 was removed (BLOCKED_BY_XSS_AUDITOR).

        /// <summary>
        /// The request was blocked by system policy disallowing some or all cleartext
        /// requests. Used for NetworkSecurityPolicy on Android.
        /// </summary>
        CLEARTEXT_NOT_PERMITTED = -29,

        /// <summary>
        /// The request was blocked by a Content Security Policy.
        /// </summary>
        BLOCKED_BY_CSP = -30,

        /// <summary>
        /// The request was blocked because of no H/2 or QUIC session.
        /// </summary>
        H2_OR_QUIC_REQUIRED = -31,

        /// <summary>
        /// The request was blocked because it is a private network request coming from
        /// an insecure context in a less private IP address space. This is used to
        /// enforce CORS-RFC1918: https://wicg.github.io/cors-rfc1918.
        /// </summary>
        INSECURE_PRIVATE_NETWORK_REQUEST = -32,

        /// <summary>
        /// A connection was closed (corresponding to a TCP FIN).
        /// </summary>
        CONNECTION_CLOSED = -100,

        /// <summary>
        /// A connection was reset (corresponding to a TCP RST).
        /// </summary>
        CONNECTION_RESET = -101,

        /// <summary>
        /// A connection attempt was refused.
        /// </summary>
        CONNECTION_REFUSED = -102,

        /// <summary>
        /// A connection timed out as a result of not receiving an ACK for data sent.
        /// This can include a FIN packet that did not get ACK'd.
        /// </summary>
        CONNECTION_ABORTED = -103,

        /// <summary>
        /// A connection attempt failed.
        /// </summary>
        CONNECTION_FAILED = -104,

        /// <summary>
        /// The host name could not be resolved.
        /// </summary>
        NAME_NOT_RESOLVED = -105,

        /// <summary>
        /// The Internet connection has been lost.
        /// </summary>
        INTERNET_DISCONNECTED = -106,

        /// <summary>
        /// An SSL protocol error occurred.
        /// </summary>
        SSL_PROTOCOL_ERROR = -107,

        /// <summary>
        /// The IP address or port number is invalid (e.g., cannot connect to the IP
        /// address 0 or the port 0).
        /// </summary>
        ADDRESS_INVALID = -108,

        /// <summary>
        /// The IP address is unreachable.  This usually means that there is no route to
        /// the specified host or network.
        /// </summary>
        ADDRESS_UNREACHABLE = -109,

        /// <summary>
        /// The server requested a client certificate for SSL client authentication.
        /// </summary>
        SSL_CLIENT_AUTH_CERT_NEEDED = -110,

        /// <summary>
        /// A tunnel connection through the proxy could not be established.
        /// </summary>
        TUNNEL_CONNECTION_FAILED = -111,

        /// <summary>
        /// No SSL protocol versions are enabled.
        /// </summary>
        NO_SSL_VERSIONS_ENABLED = -112,

        /// <summary>
        /// The client and server don't support a common SSL protocol version or
        /// cipher suite.
        /// </summary>
        SSL_VERSION_OR_CIPHER_MISMATCH = -113,

        /// <summary>
        /// The server requested a renegotiation (rehandshake).
        /// </summary>
        SSL_RENEGOTIATION_REQUESTED = -114,

        /// <summary>
        /// The proxy requested authentication (for tunnel establishment) with an
        /// unsupported method.
        /// </summary>
        PROXY_AUTH_UNSUPPORTED = -115,

        /// <summary>
        /// During SSL renegotiation (rehandshake), the server sent a certificate with
        /// an error.
        ///
        /// Note: this error is not in the -2xx range so that it won't be handled as a
        /// certificate error.
        /// </summary>
        CERT_ERROR_IN_SSL_RENEGOTIATION = -116,

        /// <summary>
        /// The SSL handshake failed because of a bad or missing client certificate.
        /// </summary>
        BAD_SSL_CLIENT_AUTH_CERT = -117,

        /// <summary>
        /// A connection attempt timed out.
        /// </summary>
        CONNECTION_TIMED_OUT = -118,

        /// <summary>
        /// There are too many pending DNS resolves, so a request in the queue was
        /// aborted.
        /// </summary>
        HOST_RESOLVER_QUEUE_TOO_LARGE = -119,

        /// <summary>
        /// Failed establishing a connection to the SOCKS proxy server for a target host.
        /// </summary>
        SOCKS_CONNECTION_FAILED = -120,

        /// <summary>
        /// The SOCKS proxy server failed establishing connection to the target host
        /// because that host is unreachable.
        /// </summary>
        SOCKS_CONNECTION_HOST_UNREACHABLE = -121,

        /// <summary>
        /// The request to negotiate an alternate protocol failed.
        /// </summary>
        ALPN_NEGOTIATION_FAILED = -122,

        /// <summary>
        /// The peer sent an SSL no_renegotiation alert message.
        /// </summary>
        SSL_NO_RENEGOTIATION = -123,

        /// <summary>
        /// Winsock sometimes reports more data written than passed.  This is probably
        /// due to a broken LSP.
        /// </summary>
        WINSOCK_UNEXPECTED_WRITTEN_BYTES = -124,

        /// <summary>
        /// An SSL peer sent us a fatal decompression_failure alert. This typically
        /// occurs when a peer selects DEFLATE compression in the mistaken belief that
        /// it supports it.
        /// </summary>
        SSL_DECOMPRESSION_FAILURE_ALERT = -125,

        /// <summary>
        /// An SSL peer sent us a fatal bad_record_mac alert. This has been observed
        /// from servers with buggy DEFLATE support.
        /// </summary>
        SSL_BAD_RECORD_MAC_ALERT = -126,

        /// <summary>
        /// The proxy requested authentication (for tunnel establishment).
        /// </summary>
        PROXY_AUTH_REQUESTED = -127,

        // Error -129 was removed (SSL_WEAK_SERVER_EPHEMERAL_DH_KEY).

        /// <summary>
        /// Could not create a connection to the proxy server. An error occurred
        /// either in resolving its name, or in connecting a socket to it.
        /// Note that this does NOT include failures during the actual "CONNECT" method
        /// of an HTTP proxy.
        /// </summary>
        PROXY_CONNECTION_FAILED = -130,

        /// <summary>
        /// A mandatory proxy configuration could not be used. Currently this means
        /// that a mandatory PAC script could not be fetched, parsed or executed.
        /// </summary>
        MANDATORY_PROXY_CONFIGURATION_FAILED = -131,

        // -132 was formerly ERR_ESET_ANTI_VIRUS_SSL_INTERCEPTION

        /// <summary>
        /// We've hit the max socket limit for the socket pool while preconnecting.  We
        /// don't bother trying to preconnect more sockets.
        /// </summary>
        PRECONNECT_MAX_SOCKET_LIMIT = -133,

        /// <summary>
        /// The permission to use the SSL client certificate's private key was denied.
        /// </summary>
        SSL_CLIENT_AUTH_PRIVATE_KEY_ACCESS_DENIED = -134,

        /// <summary>
        /// The SSL client certificate has no private key.
        /// </summary>
        SSL_CLIENT_AUTH_CERT_NO_PRIVATE_KEY = -135,

        /// <summary>
        /// The certificate presented by the HTTPS Proxy was invalid.
        /// </summary>
        PROXY_CERTIFICATE_INVALID = -136,

        /// <summary>
        /// An error occurred when trying to do a name resolution (DNS).
        /// </summary>
        NAME_RESOLUTION_FAILED = -137,

        /// <summary>
        /// Permission to access the network was denied. This is used to distinguish
        /// errors that were most likely caused by a firewall from other access denied
        /// errors. See also ERR_ACCESS_DENIED.
        /// </summary>
        NETWORK_ACCESS_DENIED = -138,

        /// <summary>
        /// The request throttler module cancelled this request to avoid DDOS.
        /// </summary>
        TEMPORARILY_THROTTLED = -139,

        /// <summary>
        /// A request to create an SSL tunnel connection through the HTTPS proxy
        /// received a 302 (temporary redirect) response.  The response body might
        /// include a description of why the request failed.
        ///
        /// TODO(https://crbug.com/928551): This is deprecated and should not be used by
        /// new code.
        /// </summary>
        HTTPS_PROXY_TUNNEL_RESPONSE_REDIRECT = -140,

        /// <summary>
        /// We were unable to sign the CertificateVerify data of an SSL client auth
        /// handshake with the client certificate's private key.
        ///
        /// Possible causes for this include the user implicitly or explicitly
        /// denying access to the private key, the private key may not be valid for
        /// signing, the key may be relying on a cached handle which is no longer
        /// valid, or the CSP won't allow arbitrary data to be signed.
        /// </summary>
        SSL_CLIENT_AUTH_SIGNATURE_FAILED = -141,

        /// <summary>
        /// The message was too large for the transport.  (for example a UDP message
        /// which exceeds size threshold).
        /// </summary>
        MSG_TOO_BIG = -142,

        // Error -143 was removed (SPDY_SESSION_ALREADY_EXISTS)
        // Error -144 was removed (LIMIT_VIOLATION).

        /// <summary>
        /// Websocket protocol error. Indicates that we are terminating the connection
        /// due to a malformed frame or other protocol violation.
        /// </summary>
        WS_PROTOCOL_ERROR = -145,

        // Error -146 was removed (PROTOCOL_SWITCHED)

        /// <summary>
        /// Returned when attempting to bind an address that is already in use.
        /// </summary>
        ADDRESS_IN_USE = -147,

        /// <summary>
        /// An operation failed because the SSL handshake has not completed.
        /// </summary>
        SSL_HANDSHAKE_NOT_COMPLETED = -148,

        /// <summary>
        /// SSL peer's public key is invalid.
        /// </summary>
        SSL_BAD_PEER_PUBLIC_KEY = -149,

        /// <summary>
        /// The certificate didn't match the built-in public key pins for the host name.
        /// The pins are set in net/http/transport_security_state.cc and require that
        /// one of a set of public keys exist on the path from the leaf to the root.
        /// </summary>
        SSL_PINNED_KEY_NOT_IN_CERT_CHAIN = -150,

        /// <summary>
        /// Server request for client certificate did not contain any types we support.
        /// </summary>
        CLIENT_AUTH_CERT_TYPE_UNSUPPORTED = -151,

        // Error -152 was removed (ORIGIN_BOUND_CERT_GENERATION_TYPE_MISMATCH)

        /// <summary>
        /// An SSL peer sent us a fatal decrypt_error alert. This typically occurs when
        /// a peer could not correctly verify a signature (in CertificateVerify or
        /// ServerKeyExchange) or validate a Finished message.
        /// </summary>
        SSL_DECRYPT_ERROR_ALERT = -153,

        /// <summary>
        /// There are too many pending WebSocketJob instances, so the new job was not
        /// pushed to the queue.
        /// </summary>
        WS_THROTTLE_QUEUE_TOO_LARGE = -154,

        // Error -155 was removed (TOO_MANY_SOCKET_STREAMS)

        /// <summary>
        /// The SSL server certificate changed in a renegotiation.
        /// </summary>
        SSL_SERVER_CERT_CHANGED = -156,

        // Error -157 was removed (SSL_INAPPROPRIATE_FALLBACK).

        // Error -158 was removed (CT_NO_SCTS_VERIFIED_OK).

        /// <summary>
        /// The SSL server sent us a fatal unrecognized_name alert.
        /// </summary>
        SSL_UNRECOGNIZED_NAME_ALERT = -159,

        /// <summary>
        /// Failed to set the socket's receive buffer size as requested.
        /// </summary>
        SOCKET_SET_RECEIVE_BUFFER_SIZE_ERROR = -160,

        /// <summary>
        /// Failed to set the socket's send buffer size as requested.
        /// </summary>
        SOCKET_SET_SEND_BUFFER_SIZE_ERROR = -161,

        /// <summary>
        /// Failed to set the socket's receive buffer size as requested, despite success
        /// return code from setsockopt.
        /// </summary>
        SOCKET_RECEIVE_BUFFER_SIZE_UNCHANGEABLE = -162,

        /// <summary>
        /// Failed to set the socket's send buffer size as requested, despite success
        /// return code from setsockopt.
        /// </summary>
        SOCKET_SEND_BUFFER_SIZE_UNCHANGEABLE = -163,

        /// <summary>
        /// Failed to import a client certificate from the platform store into the SSL
        /// library.
        /// </summary>
        SSL_CLIENT_AUTH_CERT_BAD_FORMAT = -164,

        // Error -165 was removed (SSL_FALLBACK_BEYOND_MINIMUM_VERSION).

        /// <summary>
        /// Resolving a hostname to an IP address list included the IPv4 address
        /// "127.0.53.53". This is a special IP address which ICANN has recommended to
        /// indicate there was a name collision, and alert admins to a potential
        /// problem.
        /// </summary>
        ICANN_NAME_COLLISION = -166,

        /// <summary>
        /// The SSL server presented a certificate which could not be decoded. This is
        /// not a certificate error code as no X509Certificate object is available. This
        /// error is fatal.
        /// </summary>
        SSL_SERVER_CERT_BAD_FORMAT = -167,

        /// <summary>
        /// Certificate Transparency: Received a signed tree head that failed to parse.
        /// </summary>
        CT_STH_PARSING_FAILED = -168,

        /// <summary>
        /// Certificate Transparency: Received a signed tree head whose JSON parsing was
        /// OK but was missing some of the fields.
        /// </summary>
        CT_STH_INCOMPLETE = -169,

        /// <summary>
        /// The attempt to reuse a connection to send proxy auth credentials failed
        /// before the AuthController was used to generate credentials. The caller should
        /// reuse the controller with a new connection. This error is only used
        /// internally by the network stack.
        /// </summary>
        UNABLE_TO_REUSE_CONNECTION_FOR_PROXY_AUTH = -170,

        /// <summary>
        /// Certificate Transparency: Failed to parse the received consistency proof.
        /// </summary>
        CT_CONSISTENCY_PROOF_PARSING_FAILED = -171,

        /// <summary>
        /// The SSL server required an unsupported cipher suite that has since been
        /// removed. This error will temporarily be signaled on a fallback for one or two
        /// releases immediately following a cipher suite's removal, after which the
        /// fallback will be removed.
        /// </summary>
        SSL_OBSOLETE_CIPHER = -172,

        /// <summary>
        /// When a WebSocket handshake is done successfully and the connection has been
        /// upgraded, the URLRequest is cancelled with this error code.
        /// </summary>
        WS_UPGRADE = -173,

        /// <summary>
        /// Socket ReadIfReady support is not implemented. This error should not be user
        /// visible, because the normal Read() method is used as a fallback.
        /// </summary>
        READ_IF_READY_NOT_IMPLEMENTED = -174,

        // Error -175 was removed (SSL_VERSION_INTERFERENCE).

        /// <summary>
        /// No socket buffer space is available.
        /// </summary>
        NO_BUFFER_SPACE = -176,

        /// <summary>
        /// There were no common signature algorithms between our client certificate
        /// private key and the server's preferences.
        /// </summary>
        SSL_CLIENT_AUTH_NO_COMMON_ALGORITHMS = -177,

        /// <summary>
        /// TLS 1.3 early data was rejected by the server. This will be received before
        /// any data is returned from the socket. The request should be retried with
        /// early data disabled.
        /// </summary>
        EARLY_DATA_REJECTED = -178,

        /// <summary>
        /// TLS 1.3 early data was offered, but the server responded with TLS 1.2 or
        /// earlier. This is an internal error code to account for a
        /// backwards-compatibility issue with early data and TLS 1.2. It will be
        /// received before any data is returned from the socket. The request should be
        /// retried with early data disabled.
        ///
        /// See https://tools.ietf.org/html/rfc8446#appendix-D.3 for details.
        /// </summary>
        WRONG_VERSION_ON_EARLY_DATA = -179,

        /// <summary>
        /// TLS 1.3 was enabled, but a lower version was negotiated and the server
        /// returned a value indicating it supported TLS 1.3. This is part of a security
        /// check in TLS 1.3, but it may also indicate the user is behind a buggy
        /// TLS-terminating proxy which implemented TLS 1.2 incorrectly. (See
        /// https://crbug.com/boringssl/226.)
        /// </summary>
        TLS13_DOWNGRADE_DETECTED = -180,

        /// <summary>
        /// The server's certificate has a keyUsage extension incompatible with the
        /// negotiated TLS key exchange method.
        /// </summary>
        SSL_KEY_USAGE_INCOMPATIBLE = -181,

        // Certificate error codes
        //
        // The values of certificate error codes must be consecutive.

        /// <summary>
        /// The server responded with a certificate whose common name did not match
        /// the host name.  This could mean:
        ///
        /// 1. An attacker has redirected our traffic to their server and is
        ///    presenting a certificate for which they know the private key.
        ///
        /// 2. The server is misconfigured and responding with the wrong cert.
        ///
        /// 3. The user is on a wireless network and is being redirected to the
        ///    network's login page.
        ///
        /// 4. The OS has used a DNS search suffix and the server doesn't have
        ///    a certificate for the abbreviated name in the address bar.
        /// </summary>
        CERT_COMMON_NAME_INVALID = -200,

        /// <summary>
        /// The server responded with a certificate that, by our clock, appears to
        /// either not yet be valid or to have expired.  This could mean:
        ///
        /// 1. An attacker is presenting an old certificate for which they have
        ///    managed to obtain the private key.
        ///
        /// 2. The server is misconfigured and is not presenting a valid cert.
        ///
        /// 3. Our clock is wrong.
        /// </summary>
        CERT_DATE_INVALID = -201,

        /// <summary>
        /// The server responded with a certificate that is signed by an authority
        /// we don't trust.  The could mean:
        ///
        /// 1. An attacker has substituted the real certificate for a cert that
        ///    contains their public key and is signed by their cousin.
        ///
        /// 2. The server operator has a legitimate certificate from a CA we don't
        ///    know about, but should trust.
        ///
        /// 3. The server is presenting a self-signed certificate, providing no
        ///    defense against active attackers (but foiling passive attackers).
        /// </summary>
        CERT_AUTHORITY_INVALID = -202,

        /// <summary>
        /// The server responded with a certificate that contains errors.
        /// This error is not recoverable.
        ///
        /// MSDN describes this error as follows:
        ///   "The SSL certificate contains errors."
        /// NOTE: It's unclear how this differs from ERR_CERT_INVALID. For consistency,
        /// use that code instead of this one from now on.
        /// </summary>
        CERT_CONTAINS_ERRORS = -203,

        /// <summary>
        /// The certificate has no mechanism for determining if it is revoked.  In
        /// effect, this certificate cannot be revoked.
        /// </summary>
        CERT_NO_REVOCATION_MECHANISM = -204,

        /// <summary>
        /// Revocation information for the security certificate for this site is not
        /// available.  This could mean:
        ///
        /// 1. An attacker has compromised the private key in the certificate and is
        ///    blocking our attempt to find out that the cert was revoked.
        ///
        /// 2. The certificate is unrevoked, but the revocation server is busy or
        ///    unavailable.
        /// </summary>
        CERT_UNABLE_TO_CHECK_REVOCATION = -205,

        /// <summary>
        /// The server responded with a certificate has been revoked.
        /// We have the capability to ignore this error, but it is probably not the
        /// thing to do.
        /// </summary>
        CERT_REVOKED = -206,

        /// <summary>
        /// The server responded with a certificate that is invalid.
        /// This error is not recoverable.
        ///
        /// MSDN describes this error as follows:
        ///   "The SSL certificate is invalid."
        /// </summary>
        CERT_INVALID = -207,

        /// <summary>
        /// The server responded with a certificate that is signed using a weak
        /// signature algorithm.
        /// </summary>
        CERT_WEAK_SIGNATURE_ALGORITHM = -208,

        // -209 is availible: was CERT_NOT_IN_DNS.

        /// <summary>
        /// The host name specified in the certificate is not unique.
        /// </summary>
        CERT_NON_UNIQUE_NAME = -210,

        /// <summary>
        /// The server responded with a certificate that contains a weak key (e.g.
        /// a too-small RSA key).
        /// </summary>
        CERT_WEAK_KEY = -211,

        /// <summary>
        /// The certificate claimed DNS names that are in violation of name constraints.
        /// </summary>
        CERT_NAME_CONSTRAINT_VIOLATION = -212,

        /// <summary>
        /// The certificate's validity period is too long.
        /// </summary>
        CERT_VALIDITY_TOO_LONG = -213,

        /// <summary>
        /// Certificate Transparency was required for this connection, but the server
        /// did not provide CT information that complied with the policy.
        /// </summary>
        CERTIFICATE_TRANSPARENCY_REQUIRED = -214,

        /// <summary>
        /// The certificate chained to a legacy Symantec root that is no longer trusted.
        /// https://g.co/chrome/symantecpkicerts
        /// </summary>
        CERT_SYMANTEC_LEGACY = -215,

        // -216 was QUIC_CERT_ROOT_NOT_KNOWN which has been renumbered to not be in the
        // certificate error range.

        /// <summary>
        /// The certificate is known to be used for interception by an entity other
        /// the device owner.
        /// </summary>
        CERT_KNOWN_INTERCEPTION_BLOCKED = -217,

        /// <summary>
        /// The connection uses an obsolete version of SSL/TLS.
        /// </summary>
        SSL_OBSOLETE_VERSION = -218,

        // Add new certificate error codes here.
        //
        // Update the value of CERT_END whenever you add a new certificate error
        // code.

        // The value immediately past the last certificate error code.
        //CERT_END = -219,

        /// <summary>
        /// The URL is invalid.
        /// </summary>
        INVALID_URL = -300,

        /// <summary>
        /// The scheme of the URL is disallowed.
        /// </summary>
        DISALLOWED_URL_SCHEME = -301,

        /// <summary>
        /// The scheme of the URL is unknown.
        /// </summary>
        UNKNOWN_URL_SCHEME = -302,

        /// <summary>
        /// Attempting to load an URL resulted in a redirect to an invalid URL.
        /// </summary>
        INVALID_REDIRECT = -303,

        /// <summary>
        /// Attempting to load an URL resulted in too many redirects.
        /// </summary>
        TOO_MANY_REDIRECTS = -310,

        /// <summary>
        /// Attempting to load an URL resulted in an unsafe redirect (e.g., a redirect
        /// to file:// is considered unsafe).
        /// </summary>
        UNSAFE_REDIRECT = -311,

        /// <summary>
        /// Attempting to load an URL with an unsafe port number.  These are port
        /// numbers that correspond to services, which are not robust to spurious input
        /// that may be constructed as a result of an allowed web construct (e.g., HTTP
        /// looks a lot like SMTP, so form submission to port 25 is denied).
        /// </summary>
        UNSAFE_PORT = -312,

        /// <summary>
        /// The server's response was invalid.
        /// </summary>
        INVALID_RESPONSE = -320,

        /// <summary>
        /// Error in chunked transfer encoding.
        /// </summary>
        INVALID_CHUNKED_ENCODING = -321,

        /// <summary>
        /// The server did not support the request method.
        /// </summary>
        METHOD_NOT_SUPPORTED = -322,

        /// <summary>
        /// The response was 407 (Proxy Authentication Required), yet we did not send
        /// the request to a proxy.
        /// </summary>
        UNEXPECTED_PROXY_AUTH = -323,

        /// <summary>
        /// The server closed the connection without sending any data.
        /// </summary>
        EMPTY_RESPONSE = -324,

        /// <summary>
        /// The headers section of the response is too large.
        /// </summary>
        RESPONSE_HEADERS_TOO_BIG = -325,

        // Error -326 was removed (PAC_STATUS_NOT_OK)

        /// <summary>
        /// The evaluation of the PAC script failed.
        /// </summary>
        PAC_SCRIPT_FAILED = -327,

        /// <summary>
        /// The response was 416 (Requested range not satisfiable) and the server cannot
        /// satisfy the range requested.
        /// </summary>
        REQUEST_RANGE_NOT_SATISFIABLE = -328,

        /// <summary>
        /// The identity used for authentication is invalid.
        /// </summary>
        MALFORMED_IDENTITY = -329,

        /// <summary>
        /// Content decoding of the response body failed.
        /// </summary>
        CONTENT_DECODING_FAILED = -330,

        /// <summary>
        /// An operation could not be completed because all network IO
        /// is suspended.
        /// </summary>
        NETWORK_IO_SUSPENDED = -331,

        /// <summary>
        /// FLIP data received without receiving a SYN_REPLY on the stream.
        /// </summary>
        SYN_REPLY_NOT_RECEIVED = -332,

        /// <summary>
        /// Converting the response to target encoding failed.
        /// </summary>
        ENCODING_CONVERSION_FAILED = -333,

        /// <summary>
        /// The server sent an FTP directory listing in a format we do not understand.
        /// </summary>
        UNRECOGNIZED_FTP_DIRECTORY_LISTING_FORMAT = -334,

        // Obsolete.  Was only logged in NetLog when an HTTP/2 pushed stream expired.
        // INVALID_SPDY_STREAM, -335,

        /// <summary>
        /// There are no supported proxies in the provided list.
        /// </summary>
        NO_SUPPORTED_PROXIES = -336,

        /// <summary>
        /// There is an HTTP/2 protocol error.
        /// </summary>
        HTTP2_PROTOCOL_ERROR = -337,

        /// <summary>
        /// Credentials could not be established during HTTP Authentication.
        /// </summary>
        INVALID_AUTH_CREDENTIALS = -338,

        /// <summary>
        /// An HTTP Authentication scheme was tried which is not supported on this
        /// machine.
        /// </summary>
        UNSUPPORTED_AUTH_SCHEME = -339,

        /// <summary>
        /// Detecting the encoding of the response failed.
        /// </summary>
        ENCODING_DETECTION_FAILED = -340,

        /// <summary>
        /// (GSSAPI) No Kerberos credentials were available during HTTP Authentication.
        /// </summary>
        MISSING_AUTH_CREDENTIALS = -341,

        /// <summary>
        /// An unexpected, but documented, SSPI or GSSAPI status code was returned.
        /// </summary>
        UNEXPECTED_SECURITY_LIBRARY_STATUS = -342,

        /// <summary>
        /// The environment was not set up correctly for authentication (for
        /// example, no KDC could be found or the principal is unknown.
        /// </summary>
        MISCONFIGURED_AUTH_ENVIRONMENT = -343,

        /// <summary>
        /// An undocumented SSPI or GSSAPI status code was returned.
        /// </summary>
        UNDOCUMENTED_SECURITY_LIBRARY_STATUS = -344,

        /// <summary>
        /// The HTTP response was too big to drain.
        /// </summary>
        RESPONSE_BODY_TOO_BIG_TO_DRAIN = -345,

        /// <summary>
        /// The HTTP response contained multiple distinct Content-Length headers.
        /// </summary>
        RESPONSE_HEADERS_MULTIPLE_CONTENT_LENGTH = -346,

        /// <summary>
        /// HTTP/2 headers have been received, but not all of them - status or version
        /// headers are missing, so we're expecting additional frames to complete them.
        /// </summary>
        INCOMPLETE_HTTP2_HEADERS = -347,

        /// <summary>
        /// No PAC URL configuration could be retrieved from DHCP. This can indicate
        /// either a failure to retrieve the DHCP configuration, or that there was no
        /// PAC URL configured in DHCP.
        /// </summary>
        PAC_NOT_IN_DHCP = -348,

        /// <summary>
        /// The HTTP response contained multiple Content-Disposition headers.
        /// </summary>
        RESPONSE_HEADERS_MULTIPLE_CONTENT_DISPOSITION = -349,

        /// <summary>
        /// The HTTP response contained multiple Location headers.
        /// </summary>
        RESPONSE_HEADERS_MULTIPLE_LOCATION = -350,

        /// <summary>
        /// HTTP/2 server refused the request without processing, and sent either a
        /// GOAWAY frame with error code NO_ERROR and Last-Stream-ID lower than the
        /// stream id corresponding to the request indicating that this request has not
        /// been processed yet, or a RST_STREAM frame with error code REFUSED_STREAM.
        /// Client MAY retry (on a different connection).  See RFC7540 Section 8.1.4.
        /// </summary>
        HTTP2_SERVER_REFUSED_STREAM = -351,

        /// <summary>
        /// HTTP/2 server didn't respond to the PING message.
        /// </summary>
        HTTP2_PING_FAILED = -352,

        // Obsolete.  Kept here to avoid reuse, as the old error can still appear on
        // histograms.
        // PIPELINE_EVICTION = -353,

        /// <summary>
        /// The HTTP response body transferred fewer bytes than were advertised by the
        /// Content-Length header when the connection is closed.
        /// </summary>
        CONTENT_LENGTH_MISMATCH = -354,

        /// <summary>
        /// The HTTP response body is transferred with Chunked-Encoding, but the
        /// terminating zero-length chunk was never sent when the connection is closed.
        /// </summary>
        INCOMPLETE_CHUNKED_ENCODING = -355,

        /// <summary>
        /// There is a QUIC protocol error.
        /// </summary>
        QUIC_PROTOCOL_ERROR = -356,

        /// <summary>
        /// The HTTP headers were truncated by an EOF.
        /// </summary>
        RESPONSE_HEADERS_TRUNCATED = -357,

        /// <summary>
        /// The QUIC crytpo handshake failed.  This means that the server was unable
        /// to read any requests sent, so they may be resent.
        /// </summary>
        QUIC_HANDSHAKE_FAILED = -358,

        // Obsolete.  Kept here to avoid reuse, as the old error can still appear on
        // histograms.
        // REQUEST_FOR_SECURE_RESOURCE_OVER_INSECURE_QUIC = -359,

        /// <summary>
        /// Transport security is inadequate for the HTTP/2 version.
        /// </summary>
        HTTP2_INADEQUATE_TRANSPORT_SECURITY = -360,

        /// <summary>
        /// The peer violated HTTP/2 flow control.
        /// </summary>
        HTTP2_FLOW_CONTROL_ERROR = -361,

        /// <summary>
        /// The peer sent an improperly sized HTTP/2 frame.
        /// </summary>
        HTTP2_FRAME_SIZE_ERROR = -362,

        /// <summary>
        /// Decoding or encoding of compressed HTTP/2 headers failed.
        /// </summary>
        HTTP2_COMPRESSION_ERROR = -363,

        /// <summary>
        /// Proxy Auth Requested without a valid Client Socket Handle.
        /// </summary>
        PROXY_AUTH_REQUESTED_WITH_NO_CONNECTION = -364,

        /// <summary>
        /// HTTP_1_1_REQUIRED error code received on HTTP/2 session.
        /// </summary>
        HTTP_1_1_REQUIRED = -365,

        /// <summary>
        /// HTTP_1_1_REQUIRED error code received on HTTP/2 session to proxy.
        /// </summary>
        PROXY_HTTP_1_1_REQUIRED = -366,

        /// <summary>
        /// The PAC script terminated fatally and must be reloaded.
        /// </summary>
        PAC_SCRIPT_TERMINATED = -367,

        // Obsolete. Kept here to avoid reuse.
        // Request is throttled because of a Backoff header.
        // See: crbug.com/486891.
        // TEMPORARY_BACKOFF = -369,

        /// <summary>
        /// The server was expected to return an HTTP/1.x response, but did not. Rather
        /// than treat it as HTTP/0.9, this error is returned.
        /// </summary>
        INVALID_HTTP_RESPONSE = -370,

        /// <summary>
        /// Initializing content decoding failed.
        /// </summary>
        CONTENT_DECODING_INIT_FAILED = -371,

        /// <summary>
        /// Received HTTP/2 RST_STREAM frame with NO_ERROR error code.  This error should
        /// be handled internally by HTTP/2 code, and should not make it above the
        /// SpdyStream layer.
        /// </summary>
        HTTP2_RST_STREAM_NO_ERROR_RECEIVED = -372,

        /// <summary>
        /// The pushed stream claimed by the request is no longer available.
        /// </summary>
        HTTP2_PUSHED_STREAM_NOT_AVAILABLE = -373,

        /// <summary>
        /// A pushed stream was claimed and later reset by the server. When this happens,
        /// the request should be retried.
        /// </summary>
        HTTP2_CLAIMED_PUSHED_STREAM_RESET_BY_SERVER = -374,

        /// <summary>
        /// An HTTP transaction was retried too many times due for authentication or
        /// invalid certificates. This may be due to a bug in the net stack that would
        /// otherwise infinite loop, or if the server or proxy continually requests fresh
        /// credentials or presents a fresh invalid certificate.
        /// </summary>
        TOO_MANY_RETRIES = -375,

        /// <summary>
        /// Received an HTTP/2 frame on a closed stream.
        /// </summary>
        HTTP2_STREAM_CLOSED = -376,

        /// <summary>
        /// Client is refusing an HTTP/2 stream.
        /// </summary>
        HTTP2_CLIENT_REFUSED_STREAM = -377,

        /// <summary>
        /// A pushed HTTP/2 stream was claimed by a request based on matching URL and
        /// request headers, but the pushed response headers do not match the request.
        /// </summary>
        HTTP2_PUSHED_RESPONSE_DOES_NOT_MATCH = -378,

        /// <summary>
        /// The server returned a non-2xx HTTP response code.
        ///
        /// Not that this error is only used by certain APIs that interpret the HTTP
        /// response itself. URLRequest for instance just passes most non-2xx
        /// response back as success.
        /// </summary>
        HTTP_RESPONSE_CODE_FAILURE = -379,

        /// <summary>
        /// The certificate presented on a QUIC connection does not chain to a known root
        /// and the origin connected to is not on a list of domains where unknown roots
        /// are allowed.
        /// </summary>
        QUIC_CERT_ROOT_NOT_KNOWN = -380,

        /// <summary>
        /// A GOAWAY frame has been received indicating that the request has not been
        /// processed and is therefore safe to retry on a different connection.
        /// </summary>
        QUIC_GOAWAY_REQUEST_CAN_BE_RETRIED = -381,

        /// <summary>
        /// The cache does not have the requested entry.
        /// </summary>
        CACHE_MISS = -400,

        /// <summary>
        /// Unable to read from the disk cache.
        /// </summary>
        CACHE_READ_FAILURE = -401,

        /// <summary>
        /// Unable to write to the disk cache.
        /// </summary>
        CACHE_WRITE_FAILURE = -402,

        /// <summary>
        /// The operation is not supported for this entry.
        /// </summary>
        CACHE_OPERATION_NOT_SUPPORTED = -403,

        /// <summary>
        /// The disk cache is unable to open this entry.
        /// </summary>
        CACHE_OPEN_FAILURE = -404,

        /// <summary>
        /// The disk cache is unable to create this entry.
        /// </summary>
        CACHE_CREATE_FAILURE = -405,

        /// <summary>
        /// Multiple transactions are racing to create disk cache entries. This is an
        /// internal error returned from the HttpCache to the HttpCacheTransaction that
        /// tells the transaction to restart the entry-creation logic because the state
        /// of the cache has changed.
        /// </summary>
        CACHE_RACE = -406,

        /// <summary>
        /// The cache was unable to read a checksum record on an entry. This can be
        /// returned from attempts to read from the cache. It is an internal error,
        /// returned by the SimpleCache backend, but not by any URLRequest methods
        /// or members.
        /// </summary>
        CACHE_CHECKSUM_READ_FAILURE = -407,

        /// <summary>
        /// The cache found an entry with an invalid checksum. This can be returned from
        /// attempts to read from the cache. It is an internal error, returned by the
        /// SimpleCache backend, but not by any URLRequest methods or members.
        /// </summary>
        CACHE_CHECKSUM_MISMATCH = -408,

        /// <summary>
        /// Internal error code for the HTTP cache. The cache lock timeout has fired.
        /// </summary>
        CACHE_LOCK_TIMEOUT = -409,

        /// <summary>
        /// Received a challenge after the transaction has read some data, and the
        /// credentials aren't available.  There isn't a way to get them at that point.
        /// </summary>
        CACHE_AUTH_FAILURE_AFTER_READ = -410,

        /// <summary>
        /// Internal not-quite error code for the HTTP cache. In-memory hints suggest
        /// that the cache entry would not have been useable with the transaction's
        /// current configuration (e.g. load flags, mode, etc.)
        /// </summary>
        CACHE_ENTRY_NOT_SUITABLE = -411,

        /// <summary>
        /// The disk cache is unable to doom this entry.
        /// </summary>
        CACHE_DOOM_FAILURE = -412,

        /// <summary>
        /// The disk cache is unable to open or create this entry.
        /// </summary>
        CACHE_OPEN_OR_CREATE_FAILURE = -413,

        /// <summary>
        /// The server's response was insecure (e.g. there was a cert error).
        /// </summary>
        INSECURE_RESPONSE = -501,

        /// <summary>
        /// An attempt to import a client certificate failed, as the user's key
        /// database lacked a corresponding private key.
        /// </summary>
        NO_PRIVATE_KEY_FOR_CERT = -502,

        /// <summary>
        /// An error adding a certificate to the OS certificate database.
        /// </summary>
        ADD_USER_CERT_FAILED = -503,

        /// <summary>
        /// An error occurred while handling a signed exchange.
        /// </summary>
        INVALID_SIGNED_EXCHANGE = -504,

        /// <summary>
        /// An error occurred while handling a Web Bundle source.
        /// </summary>
        INVALID_WEB_BUNDLE = -505,

        /// <summary>
        /// A Trust Tokens protocol operation-executing request failed for one of a
        /// number of reasons (precondition failure, internal error, bad response).
        /// </summary>
        TRUST_TOKEN_OPERATION_FAILED = -506,

        /// <summary>
        /// When handling a Trust Tokens protocol operation-executing request, the system
        /// found that the request's desired Trust Tokens results were already present in
        /// a local cache; as a result, the main request was cancelled.
        /// </summary>
        TRUST_TOKEN_OPERATION_CACHE_HIT = -507,

        // *** Code -600 is reserved (was FTP_PASV_COMMAND_FAILED). ***

        /// <summary>
        /// A generic error for failed FTP control connection command.
        /// If possible, please use or add a more specific error code.
        /// </summary>
        FTP_FAILED = -601,

        /// <summary>
        /// The server cannot fulfill the request at this point. This is a temporary
        /// error.
        /// FTP response code 421.
        /// </summary>
        FTP_SERVICE_UNAVAILABLE = -602,

        /// <summary>
        /// The server has aborted the transfer.
        /// FTP response code 426.
        /// </summary>
        FTP_TRANSFER_ABORTED = -603,

        /// <summary>
        /// The file is busy, or some other temporary error condition on opening
        /// the file.
        /// FTP response code 450.
        /// </summary>
        FTP_FILE_BUSY = -604,

        /// <summary>
        /// Server rejected our command because of syntax errors.
        /// FTP response codes 500, 501.
        /// </summary>
        FTP_SYNTAX_ERROR = -605,

        /// <summary>
        /// Server does not support the command we issued.
        /// FTP response codes 502, 504.
        /// </summary>
        FTP_COMMAND_NOT_SUPPORTED = -606,

        /// <summary>
        /// Server rejected our command because we didn't issue the commands in right
        /// order.
        /// FTP response code 503.
        /// </summary>
        FTP_BAD_COMMAND_SEQUENCE = -607,

        /// <summary>
        /// PKCS #12 import failed due to incorrect password.
        /// </summary>
        PKCS12_IMPORT_BAD_PASSWORD = -701,

        /// <summary>
        /// PKCS #12 import failed due to other error.
        /// </summary>
        PKCS12_IMPORT_FAILED = -702,

        /// <summary>
        /// CA import failed - not a CA cert.
        /// </summary>
        IMPORT_CA_CERT_NOT_CA = -703,

        /// <summary>
        /// Import failed - certificate already exists in database.
        /// Note it's a little weird this is an error but reimporting a PKCS12 is ok
        /// (no-op).  That's how Mozilla does it, though.
        /// </summary>
        IMPORT_CERT_ALREADY_EXISTS = -704,

        /// <summary>
        /// CA import failed due to some other error.
        /// </summary>
        IMPORT_CA_CERT_FAILED = -705,

        /// <summary>
        /// Server certificate import failed due to some internal error.
        /// </summary>
        IMPORT_SERVER_CERT_FAILED = -706,

        /// <summary>
        /// PKCS #12 import failed due to invalid MAC.
        /// </summary>
        PKCS12_IMPORT_INVALID_MAC = -707,

        /// <summary>
        /// PKCS #12 import failed due to invalid/corrupt file.
        /// </summary>
        PKCS12_IMPORT_INVALID_FILE = -708,

        /// <summary>
        /// PKCS #12 import failed due to unsupported features.
        /// </summary>
        PKCS12_IMPORT_UNSUPPORTED = -709,

        /// <summary>
        /// Key generation failed.
        /// </summary>
        KEY_GENERATION_FAILED = -710,

        // Error -711 was removed (ORIGIN_BOUND_CERT_GENERATION_FAILED)

        /// <summary>
        /// Failure to export private key.
        /// </summary>
        PRIVATE_KEY_EXPORT_FAILED = -712,

        /// <summary>
        /// Self-signed certificate generation failed.
        /// </summary>
        SELF_SIGNED_CERT_GENERATION_FAILED = -713,

        /// <summary>
        /// The certificate database changed in some way.
        /// </summary>
        CERT_DATABASE_CHANGED = -714,

        // Error -715 was removed (CHANNEL_ID_IMPORT_FAILED)

        // DNS error codes.

        /// <summary>
        /// DNS resolver received a malformed response.
        /// </summary>
        DNS_MALFORMED_RESPONSE = -800,

        /// <summary>
        /// DNS server requires TCP
        /// </summary>
        DNS_SERVER_REQUIRES_TCP = -801,

        /// <summary>
        /// DNS server failed.  This error is returned for all of the following
        /// error conditions:
        /// 1 - Format error - The name server was unable to interpret the query.
        /// 2 - Server failure - The name server was unable to process this query
        ///     due to a problem with the name server.
        /// 4 - Not Implemented - The name server does not support the requested
        ///     kind of query.
        /// 5 - Refused - The name server refuses to perform the specified
        ///     operation for policy reasons.
        /// </summary>
        DNS_SERVER_FAILED = -802,

        /// <summary>
        /// DNS transaction timed out.
        /// </summary>
        DNS_TIMED_OUT = -803,

        /// <summary>
        /// The entry was not found in cache or other local sources, for lookups where
        /// only local sources were queried.
        /// TODO(ericorth): Consider renaming to DNS_LOCAL_MISS or something like that as
        /// the cache is not necessarily queried either.
        /// </summary>
        DNS_CACHE_MISS = -804,

        /// <summary>
        /// Suffix search list rules prevent resolution of the given host name.
        /// </summary>
        DNS_SEARCH_EMPTY = -805,

        /// <summary>
        /// Failed to sort addresses according to RFC3484.
        /// </summary>
        DNS_SORT_ERROR = -806,

        // Error -807 was removed (DNS_HTTP_FAILED)

        /// <summary>
        /// Failed to resolve the hostname of a DNS-over-HTTPS server.
        /// </summary>
        DNS_SECURE_RESOLVER_HOSTNAME_RESOLUTION_FAILED = -808,

        // CefGlue backward compatiblity.
        // Generally we prefer .NET naming rules, but will care about later.
        // Clients very rarely use this codes directly in their code.

        Failed = FAILED,
        Aborted = ABORTED,
        InvalidArgument = INVALID_ARGUMENT,
        InvalidHandle = INVALID_HANDLE,
        FileNotFound = FILE_NOT_FOUND,
        TimedOut = TIMED_OUT,
        FileTooBig = FILE_TOO_BIG,
        Unexpected = UNEXPECTED,
        AccessDenied = ACCESS_DENIED,
        NotImplemented = NOT_IMPLEMENTED,

        ConnectionClosed = CONNECTION_CLOSED,
        ConnectionReset = CONNECTION_RESET,
        ConnectionRefused = CONNECTION_REFUSED,
        ConnectionAborted = CONNECTION_ABORTED,
        ConnectionFailed = CONNECTION_FAILED,
        NameNotResolved = NAME_NOT_RESOLVED,
        InternetDisconnected = INTERNET_DISCONNECTED,
        SslProtocolError = SSL_PROTOCOL_ERROR,
        AddressInvalid = ADDRESS_INVALID,
        AddressUnreachable = ADDRESS_UNREACHABLE,
        SslClientAuthCertNeeded = SSL_CLIENT_AUTH_CERT_NEEDED,
        TunnelConnectionFailed = TUNNEL_CONNECTION_FAILED,
        NoSslVersionsEnabled = NO_SSL_VERSIONS_ENABLED,
        SslVersionOrCipherMismatch = SSL_VERSION_OR_CIPHER_MISMATCH,
        SslRenegotiationRequested = SSL_RENEGOTIATION_REQUESTED,

        CertCommonNameInvalid = CERT_COMMON_NAME_INVALID,
        CertDateInvalid = CERT_DATE_INVALID,
        CertAuthorityInvalid = CERT_AUTHORITY_INVALID,
        CertContainsErrors = CERT_CONTAINS_ERRORS,
        CertNoRevocationMechanism = CERT_NO_REVOCATION_MECHANISM,
        CertUnableToCheckRevocation = CERT_UNABLE_TO_CHECK_REVOCATION,
        CertRevoked = CERT_REVOKED,
        CertInvalid = CERT_INVALID,
        CertWeakSignatureAlgorithm = CERT_WEAK_SIGNATURE_ALGORITHM,
        CertNonUniqueName = CERT_NON_UNIQUE_NAME,
        CertWeakKey = CERT_WEAK_KEY,
        CertNameConstraintViolation = CERT_NAME_CONSTRAINT_VIOLATION,
        CertValidityTooLong = CERT_VALIDITY_TOO_LONG,

        InvalidUrl = INVALID_URL,
        DisallowedUrlScheme = DISALLOWED_URL_SCHEME,
        UnknownUrlScheme = UNKNOWN_URL_SCHEME,
        TooManyRedirects = TOO_MANY_REDIRECTS,
        UnsafeRedirect = UNSAFE_REDIRECT,
        UnsafePort = UNSAFE_PORT,
        InvalidResponse = INVALID_RESPONSE,
        InvalidChunkedEncoding = INVALID_CHUNKED_ENCODING,
        MethodNotSupported = METHOD_NOT_SUPPORTED,
        UnexpectedProxyAuth = UNEXPECTED_PROXY_AUTH,
        EmptyResponse = EMPTY_RESPONSE,
        ResponseHeadersTooBig = RESPONSE_HEADERS_TOO_BIG,

        CacheMiss = CACHE_MISS,

        InsecureResponse = INSECURE_RESPONSE,
    }
}
