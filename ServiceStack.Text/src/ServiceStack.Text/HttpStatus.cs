namespace ServiceStack.Text;

public static class HttpStatus
{
    public static string GetStatusDescription(int statusCode)
    {
        if (statusCode is >= 100 and < 600)
        {
            int i = statusCode / 100;
            int j = statusCode % 100;

            if (j < Descriptions[i].Length)
                return Descriptions[i][j];
        }

        return string.Empty;
    }

    private static readonly string[][] Descriptions =
    [
        null,
        [
            /* 100 */ "Continue",
            /* 101 */ "Switching Protocols",
            /* 102 */ "Processing"
        ],
        [
            /* 200 */ "OK",
            /* 201 */ "Created",
            /* 202 */ "Accepted",
            /* 203 */ "Non-Authoritative Information",
            /* 204 */ "No Content",
            /* 205 */ "Reset Content",
            /* 206 */ "Partial Content",
            /* 207 */ "Multi-Status"
        ],
        [
            /* 300 */ "Multiple Choices",
            /* 301 */ "Moved Permanently",
            /* 302 */ "Found",
            /* 303 */ "See Other",
            /* 304 */ "Not Modified",
            /* 305 */ "Use Proxy",
            /* 306 */ string.Empty,
            /* 307 */ "Temporary Redirect"
        ],
        [
            /* 400 */ "Bad Request",
            /* 401 */ "Unauthorized",
            /* 402 */ "Payment Required",
            /* 403 */ "Forbidden",
            /* 404 */ "Not Found",
            /* 405 */ "Method Not Allowed",
            /* 406 */ "Not Acceptable",
            /* 407 */ "Proxy Authentication Required",
            /* 408 */ "Request Timeout",
            /* 409 */ "Conflict",
            /* 410 */ "Gone",
            /* 411 */ "Length Required",
            /* 412 */ "Precondition Failed",
            /* 413 */ "Request Entity Too Large",
            /* 414 */ "Request-Uri Too Long",
            /* 415 */ "Unsupported Media Type",
            /* 416 */ "Requested Range Not Satisfiable",
            /* 417 */ "Expectation Failed",
            /* 418 */ string.Empty,
            /* 419 */ string.Empty,
            /* 420 */ string.Empty,
            /* 421 */ string.Empty,
            /* 422 */ "Unprocessable Entity",
            /* 423 */ "Locked",
            /* 424 */ "Failed Dependency"
        ],
        [
            /* 500 */ "Internal Server Error",
            /* 501 */ "Not Implemented",
            /* 502 */ "Bad Gateway",
            /* 503 */ "Service Unavailable",
            /* 504 */ "Gateway Timeout",
            /* 505 */ "Http Version Not Supported",
            /* 506 */ string.Empty,
            /* 507 */ "Insufficient Storage"
        ]
    ];
}