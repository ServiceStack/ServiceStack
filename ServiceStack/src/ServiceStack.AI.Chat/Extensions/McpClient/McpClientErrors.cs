using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;

namespace ServiceStack.AI;

internal static class McpClientErrors
{
    // SDK exception messages can include response bodies (and echoed credentials).
    // Expose only our own messages and structured transport details.
    internal static McpClientException Connection(Exception error)
    {
        for (Exception? e = error; e != null; e = e.InnerException) {
            if (e is McpClientException known) return known;
        }
        for (Exception? e = error; e != null; e = e.InnerException) {
            if (e is HttpRequestException { StatusCode: { } status }) return Http(status);
            if (e is AuthenticationException)
                return new("tls_error", "Could not establish a trusted TLS connection to the MCP server.");
            if (e is SocketException socket)
                return socket.SocketErrorCode is SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain
                    ? new("dns_error", "Could not resolve the MCP server hostname. Check the server URL.")
                    : new("network_error", "Could not reach the MCP server. Check the server URL and network connection.");
        }
        if (error is OperationCanceledException)
            return new("timeout", "The MCP server did not respond before the connection timed out. Try again.");
        return new("connection_error", "Could not establish an MCP connection. Check the server URL and authentication settings.");
    }

    internal static McpClientException Http(HttpStatusCode status)
    {
        var (code, message) = (int)status switch {
            400 => ("http_400", "The MCP server rejected the request (HTTP 400). Check the server URL and credentials; for Bearer authentication, enter the token only."),
            401 => ("auth_required", "The MCP server rejected authentication (HTTP 401). Enter a valid token or reconnect your account."),
            403 => ("access_denied", "The MCP server denied access (HTTP 403). Check the token permissions and your account's access to this server."),
            404 => ("http_404", "The MCP endpoint was not found (HTTP 404). Check the server URL."),
            429 => ("rate_limited", "The MCP server rate limit was reached (HTTP 429). Try again later."),
            _ => ("http_" + (int)status, $"The MCP server returned HTTP {(int)status}. Check the server configuration or try again later."),
        };
        return new(code, message);
    }
}
