using System;
using System.Collections.Generic;

namespace ServiceStack;

public static class HttpHeaders
{
    public const string XParamOverridePrefix = "X-Param-Override-";

    public const string XHttpMethodOverride = "X-Http-Method-Override";

    public const string XAutoBatchCompleted = "X-AutoBatch-Completed"; // How many requests were completed before first failure

    public const string XTag = "X-Tag";

    public const string XUserAuthId = "X-UAId";

    public const string XTrigger = "X-Trigger"; // Trigger Events on UserAgent

    public const string XForwardedFor = "X-Forwarded-For"; // IP Address

    public const string XForwardedPort = "X-Forwarded-Port";  // 80

    public const string XForwardedProtocol = "X-Forwarded-Proto"; // http or https

    public const string XRealIp = "X-Real-IP";

    public const string XLocation = "X-Location";

    public const string XStatus = "X-Status";

    public const string XPoweredBy = "X-Powered-By";
        
    public const string Referer = "Referer";

    public const string CacheControl = "Cache-Control";

    public const string IfModifiedSince = "If-Modified-Since";

    public const string IfUnmodifiedSince = "If-Unmodified-Since";

    public const string IfNoneMatch = "If-None-Match";

    public const string IfMatch = "If-Match";

    public const string LastModified = "Last-Modified";

    public const string Accept = "Accept";

    public const string AcceptEncoding = "Accept-Encoding";

    public const string ContentType = "Content-Type";

    public const string ContentEncoding = "Content-Encoding";

    public const string ContentLength = "Content-Length";

    public const string ContentDisposition = "Content-Disposition";

    public const string Location = "Location";

    public const string SetCookie = "Set-Cookie";

    public const string ETag = "ETag";

    public const string Age = "Age";

    public const string Expires = "Expires";

    public const string Vary = "Vary";

    public const string Authorization = "Authorization";

    public const string WwwAuthenticate = "WWW-Authenticate";

    public const string AllowOrigin = "Access-Control-Allow-Origin";

    public const string AllowMethods = "Access-Control-Allow-Methods";

    public const string AllowHeaders = "Access-Control-Allow-Headers";

    public const string AllowCredentials = "Access-Control-Allow-Credentials";

    public const string ExposeHeaders = "Access-Control-Expose-Headers";

    public const string AccessControlMaxAge = "Access-Control-Max-Age";

    public const string Origin = "Origin";

    public const string RequestMethod = "Access-Control-Request-Method";

    public const string RequestHeaders = "Access-Control-Request-Headers";

    public const string AcceptRanges = "Accept-Ranges";

    public const string ContentRange = "Content-Range";

    public const string Range = "Range";

    public const string SOAPAction = "SOAPAction";

    public const string Allow = "Allow";

    public const string AcceptCharset = "Accept-Charset";

    public const string AcceptLanguage = "Accept-Language";

    public const string Connection = "Connection";

    public const string Cookie = "Cookie";

    public const string ContentLanguage = "Content-Language";

    public const string Expect = "Expect";

    public const string Pragma = "Pragma";
        
    public const string ProxyAuthenticate = "Proxy-Authenticate";

    public const string ProxyAuthorization = "Proxy-Authorization";

    public const string ProxyConnection = "Proxy-Connection";

    public const string SetCookie2 = "Set-Cookie2";

    public const string TE = "TE";

    public const string Trailer = "Trailer";

    public const string TransferEncoding = "Transfer-Encoding";

    public const string Upgrade = "Upgrade";

    public const string Via = "Via";

    public const string Warning = "Warning";

    public const string Date = "Date";
    public const string Host = "Host";
    public const string UserAgent = "User-Agent";

    public static HashSet<string> RestrictedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        Accept,
        Connection,
        ContentLength,
        ContentType,
        Date,
        Expect,
        Host,
        IfModifiedSince,
        Range,
        Referer,
        TransferEncoding,
        UserAgent,
        ProxyConnection,
    };
}


public static class CompressionTypes
{
    public static readonly string[] AllCompressionTypes =
    {
#if NET6_0_OR_GREATER        
        Brotli,
#endif
        Deflate, 
        GZip,
    };

#if NET6_0_OR_GREATER        
    public const string Default = Brotli;
#else
    public const string Default = Deflate;
#endif

    public const string Brotli = "br";
    public const string Deflate = "deflate";
    public const string GZip = "gzip";

    public static bool IsValid(string compressionType)
    {
#if NET6_0_OR_GREATER        
        return compressionType is Deflate or GZip or Brotli;
#else
        return compressionType is Deflate or GZip;
#endif
    }

    public static void AssertIsValid(string compressionType)
    {
        if (!IsValid(compressionType))
        {
            throw new NotSupportedException(compressionType
                                            + " is not a supported compression type. Valid types: " + string.Join(", ", AllCompressionTypes));
        }
    }

    public static string GetExtension(string compressionType)
    {
        switch (compressionType)
        {
            case Brotli:
            case Deflate:
            case GZip:
                return "." + compressionType;
            default:
                throw new NotSupportedException(
                    "Unknown compressionType: " + compressionType);
        }
    }
}