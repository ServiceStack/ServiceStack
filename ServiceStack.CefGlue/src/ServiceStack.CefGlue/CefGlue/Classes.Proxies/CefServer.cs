namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing a server that supports HTTP and WebSocket requests. Server
    /// capacity is limited and is intended to handle only a small number of
    /// simultaneous connections (e.g. for communicating between applications on
    /// localhost). The methods of this class are safe to call from any thread in the
    /// brower process unless otherwise indicated.
    /// </summary>
    public sealed unsafe partial class CefServer
    {
        /// <summary>
        /// Create a new server that binds to |address| and |port|. |address| must be a
        /// valid IPv4 or IPv6 address (e.g. 127.0.0.1 or ::1) and |port| must be a
        /// port number outside of the reserved range (e.g. between 1025 and 65535 on
        /// most platforms). |backlog| is the maximum number of pending connections.
        /// A new thread will be created for each CreateServer call (the "dedicated
        /// server thread"). It is therefore recommended to use a different
        /// CefServerHandler instance for each CreateServer call to avoid thread safety
        /// issues in the CefServerHandler implementation. The
        /// CefServerHandler::OnServerCreated method will be called on the dedicated
        /// server thread to report success or failure. See
        /// CefServerHandler::OnServerCreated documentation for a description of server
        /// lifespan.
        /// </summary>
        public static void Create(string address, ushort port, int backlog, CefServerHandler handler)
        {
            fixed (char* address_str = address)
            {
                var n_address = new cef_string_t(address_str, address.Length);
                cef_server_t.create(&n_address, port, backlog, handler.ToNative());
            }
        }

        /// <summary>
        /// Returns the task runner for the dedicated server thread.
        /// </summary>
        public CefTaskRunner GetTaskRunner()
        {
            return CefTaskRunner.FromNative(
                cef_server_t.get_task_runner(_self)
                );
        }

        /// <summary>
        /// Stop the server and shut down the dedicated server thread. See
        /// CefServerHandler::OnServerCreated documentation for a description of
        /// server lifespan.
        /// </summary>
        public void Shutdown()
        {
            cef_server_t.shutdown(_self);
        }

        /// <summary>
        /// Returns true if the server is currently running and accepting incoming
        /// connections. See CefServerHandler::OnServerCreated documentation for a
        /// description of server lifespan. This method must be called on the dedicated
        /// server thread.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return cef_server_t.is_running(_self) != 0;
            }
        }

        /// <summary>
        /// Returns the server address including the port number.
        /// </summary>
        public string Address
        {
            get
            {
                return cef_string_userfree.ToString(
                    cef_server_t.get_address(_self)
                    );
            }
        }

        /// <summary>
        /// Returns true if the server currently has a connection. This method must be
        /// called on the dedicated server thread.
        /// </summary>
        public bool HasConnection
        {
            get
            {
                return cef_server_t.has_connection(_self) != 0;
            }
        }

        /// <summary>
        /// Returns true if |connection_id| represents a valid connection. This method
        /// must be called on the dedicated server thread.
        /// </summary>
        public bool IsValidConnection(int connectionId)
        {
            return cef_server_t.is_valid_connection(_self, connectionId) != 0;
        }

        /// <summary>
        /// Send an HTTP 200 "OK" response to the connection identified by
        /// |connection_id|. |content_type| is the response content type (e.g.
        /// "text/html"), |data| is the response content, and |data_size| is the size
        /// of |data| in bytes. The contents of |data| will be copied. The connection
        /// will be closed automatically after the response is sent.
        /// </summary>
        public void SendHttp200Response(int connectionId, string contentType, IntPtr data, long dataSize)
        {
            fixed (char* contentType_str = contentType)
            {
                var n_contentType = new cef_string_t(contentType_str, contentType != null ? contentType.Length : 0);
                var n_dataSize = checked((UIntPtr)dataSize);
                cef_server_t.send_http200response(_self, connectionId, &n_contentType, (void*)data, n_dataSize);
            }
        }

        /// <summary>
        /// Send an HTTP 404 "Not Found" response to the connection identified by
        /// |connection_id|. The connection will be closed automatically after the
        /// response is sent.
        /// </summary>
        public void SendHttp404Response(int connectionId)
        {
            cef_server_t.send_http404response(_self, connectionId);
        }

        /// <summary>
        /// Send an HTTP 500 "Internal Server Error" response to the connection
        /// identified by |connection_id|. |error_message| is the associated error
        /// message. The connection will be closed automatically after the response is
        /// sent.
        /// </summary>
        public void SendHttp500Response(int connectionId, string errorMessage)
        {
            fixed (char* errorMessage_str = errorMessage)
            {
                var n_errorMessage = new cef_string_t(errorMessage_str, errorMessage != null ? errorMessage.Length : 0);
                cef_server_t.send_http500response(_self, connectionId, &n_errorMessage);
            }
        }

        /// <summary>
        /// Send a custom HTTP response to the connection identified by
        /// |connection_id|. |response_code| is the HTTP response code sent in the
        /// status line (e.g. 200), |content_type| is the response content type sent
        /// as the "Content-Type" header (e.g. "text/html"), |content_length| is the
        /// expected content length, and |extra_headers| is the map of extra response
        /// headers. If |content_length| is >= 0 then the "Content-Length" header will
        /// be sent. If |content_length| is 0 then no content is expected and the
        /// connection will be closed automatically after the response is sent. If
        /// |content_length| is &lt; 0 then no "Content-Length" header will be sent and
        /// the client will continue reading until the connection is closed. Use the
        /// SendRawData method to send the content, if applicable, and call
        /// CloseConnection after all content has been sent.
        /// </summary>
        public void SendHttpResponse(int connectionId, int responseCode, string contentType, long contentLength, NameValueCollection extraHeaders)
        {
            fixed(char* contentType_str = contentType)
            {
                var n_contentType = new cef_string_t(contentType_str, contentType != null ? contentType.Length : 0);
                var n_extraHeaders = cef_string_multimap.From(extraHeaders);
                cef_server_t.send_http_response(_self, connectionId, responseCode, &n_contentType, contentLength, n_extraHeaders);
                libcef.string_multimap_free(n_extraHeaders);
            }
        }

        /// <summary>
        /// Send raw data directly to the connection identified by |connection_id|.
        /// |data| is the raw data and |data_size| is the size of |data| in bytes.
        /// The contents of |data| will be copied. No validation of |data| is
        /// performed internally so the client should be careful to send the amount
        /// indicated by the "Content-Length" header, if specified. See
        /// SendHttpResponse documentation for intended usage.
        /// </summary>
        public void SendRawData(int connectionId, IntPtr data, long dataSize)
        {
            var n_dataSize = checked((UIntPtr)dataSize);
            cef_server_t.send_raw_data(_self, connectionId, (void*)data, n_dataSize);
        }

        /// <summary>
        /// Close the connection identified by |connection_id|. See SendHttpResponse
        /// documentation for intended usage.
        /// </summary>
        public void CloseConnection(int connectionId)
        {
            cef_server_t.close_connection(_self, connectionId);
        }

        /// <summary>
        /// Send a WebSocket message to the connection identified by |connection_id|.
        /// |data| is the response content and |data_size| is the size of |data| in
        /// bytes. The contents of |data| will be copied. See
        /// CefServerHandler::OnWebSocketRequest documentation for intended usage.
        /// </summary>
        public void SendWebSocketMessage(int connectionId, IntPtr data, long dataSize)
        {
            var n_dataSize = checked((UIntPtr)dataSize);
            cef_server_t.send_web_socket_message(_self, connectionId, (void*)data, n_dataSize);
        }
    }
}
