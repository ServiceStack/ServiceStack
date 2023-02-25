using System;
using System.Net;

namespace ServiceStack;

public static class ClientConfig
{
    public static bool SkipEmptyArrays { get; set; } = false;

    public static bool ImplicitRefInfo { get; set; } = true;

    public static Func<string, object> EvalExpression { get; set; }

    public static void ConfigureTls12()
    {
        //https://githubengineering.com/crypto-removal-notice/
        ServicePointManager.Expect100Continue = true;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    }

    public static Func<string, string> EncodeDispositionFileName { get; set; } = DefaultEncodeDispositionFileName;

    public static string DefaultEncodeDispositionFileName(string fileName) =>
        fileName.UrlEncode().Replace("+", "%20");
}
