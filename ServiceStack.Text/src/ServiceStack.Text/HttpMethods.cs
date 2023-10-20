using System;
using System.Collections.Generic;

namespace ServiceStack;

public static class HttpMethods
{
    static readonly string[] allVerbs = {
        "OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "CONNECT", // RFC 2616
        "PROPFIND", "PROPPATCH", "MKCOL", "COPY", "MOVE", "LOCK", "UNLOCK",    // RFC 2518
        "VERSION-CONTROL", "REPORT", "CHECKOUT", "CHECKIN", "UNCHECKOUT",
        "MKWORKSPACE", "UPDATE", "LABEL", "MERGE", "BASELINE-CONTROL", "MKACTIVITY",  // RFC 3253
        "ORDERPATCH", // RFC 3648
        "ACL",        // RFC 3744
        "PATCH",      // https://datatracker.ietf.org/doc/draft-dusseault-http-patch/
        "SEARCH",     // https://datatracker.ietf.org/doc/draft-reschke-webdav-search/
        "BCOPY", "BDELETE", "BMOVE", "BPROPFIND", "BPROPPATCH", "NOTIFY",
        "POLL",  "SUBSCRIBE", "UNSUBSCRIBE" //MS Exchange WebDav: http://msdn.microsoft.com/en-us/library/aa142917.aspx
    };

    public static HashSet<string> AllVerbs = new(allVerbs);

    public static bool Exists(string httpMethod) => AllVerbs.Contains(httpMethod.ToUpper());
    public static bool HasVerb(string httpVerb) => Exists(httpVerb);

    public const string Get = "GET";
    public const string Put = "PUT";
    public const string Post = "POST";
    public const string Delete = "DELETE";
    public const string Options = "OPTIONS";
    public const string Head = "HEAD";
    public const string Patch = "PATCH";
    public const string Connect = "CONNECT";
    public const string Trace = "TRACE";

    // Add Microsoft.AspNetCore.Http.HttpMethods so existing source still compiles if ServiceStack.HttpMethod takes preference
    public static bool IsConnect(string method) => Equals(Connect, method);
    public static bool IsDelete(string method) => Equals(Delete, method);
    public static bool IsGet(string method) => Equals(Get, method);
    public static bool IsHead(string method) => Equals(Head, method);
    public static bool IsOptions(string method) => Equals(Options, method);
    public static bool IsPatch(string method) => Equals(Patch, method);
    public static bool IsPost(string method) => Equals(Post, method);
    public static bool IsPut(string method) => Equals(Put, method);
    public static bool IsTrace(string method) => Equals(Trace, method);
    public static string GetCanonicalizedValue(string method) => method switch
    {
        string _ when IsGet(method) => Get,
        string _ when IsPost(method) => Post,
        string _ when IsPut(method) => Put,
        string _ when IsDelete(method) => Delete,
        string _ when IsOptions(method) => Options,
        string _ when IsHead(method) => Head,
        string _ when IsPatch(method) => Patch,
        string _ when IsTrace(method) => Trace,
        string _ when IsConnect(method) => Connect,
        string _ => method
    };

    /// <summary>
    /// Returns a value that indicates if the HTTP methods are the same.
    /// </summary>
    public static bool Equals(string methodA, string methodB) =>
        ReferenceEquals(methodA, methodB) || StringComparer.OrdinalIgnoreCase.Equals(methodA, methodB);
}
