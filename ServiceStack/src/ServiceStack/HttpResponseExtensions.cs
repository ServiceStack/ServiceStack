using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Auth;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public static class HttpResponseExtensions
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseExtensions));
    //public static bool IsXsp;
    //public static bool IsModMono;
    public static bool IsMonoFastCgi;
    //public static bool IsWebDevServer;
    //public static bool IsIis;
    public static bool IsHttpListener;
    public static bool IsNetCore;

    static HttpResponseExtensions()
    {
        //IsXsp = Env.IsMono;
        //IsModMono = Env.IsMono;
        IsMonoFastCgi = Env.IsMono;

        //IsWebDevServer = !Env.IsMono;
        //IsIis = !Env.IsMono;
#if !NETCORE
            IsHttpListener = HttpContext.Current == null;
#else
        IsNetCore = true;
#endif
    }

#if !NETCORE
        public static void CloseOutputStream(this HttpResponseBase response)
        {
            try
            {
                //Don't close for MonoFastCGI as it outputs random 4-letters at the start
                if (!IsMonoFastCgi)
                {
                    response.OutputStream.Flush();
                    response.OutputStream.Close();
                    //response.Close(); //This kills .NET Development Web Server
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception closing HttpResponse: " + ex.Message, ex);
            }
        }

        public static void CloseOutputStream(this HttpListenerResponse response)
        {
            try
            {
                response.OutputStream.Flush();
                response.OutputStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Error in HttpListenerResponseWrapper: " + ex.Message, ex);
            }
        }
#endif

    public static void RedirectToUrl(this IResponse httpRes, string url, HttpStatusCode redirectStatusCode = HttpStatusCode.Redirect)
    {
        httpRes.StatusCode = (int)redirectStatusCode;
        httpRes.AddHeader(HttpHeaders.Location, url);
        httpRes.EndRequest();
    }

    public static void TransmitFile(this IResponse httpRes, string filePath)
    {
#if !NETCORE
            if (httpRes is ServiceStack.Host.AspNet.AspNetResponse aspNetRes)
            {
                aspNetRes.Response.TransmitFile(filePath);
                return;
            }
#endif

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fs.WriteTo(httpRes.OutputStream);
        }

        httpRes.EndRequest();
    }

    public static void WriteFile(this IResponse httpRes, string filePath)
    {
#if !NETCORE
            if (httpRes is ServiceStack.Host.AspNet.AspNetResponse aspNetRes)
            {
                aspNetRes.Response.WriteFile(filePath);
                return;
            }
#endif

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fs.WriteTo(httpRes.OutputStream);
        }

        httpRes.EndRequest();
    }

#if NETCORE
    public static Microsoft.AspNetCore.Http.HttpRequest AllowSyncIO(this Microsoft.AspNetCore.Http.HttpRequest req)
    {
        req.HttpContext.AllowSyncIO();
        return req;
    }
    public static Microsoft.AspNetCore.Http.HttpContext AllowSyncIO(this Microsoft.AspNetCore.Http.HttpContext ctx)
    {
        // AllowSynchronousIO for sync SSE notifications https://github.com/aspnet/AspNetCore/issues/7644 
        var feature = ctx.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
        if (feature != null) // Not available in AWS
        {
            feature.AllowSynchronousIO = true;
        }
        return ctx;
    }
#endif

    public static IRequest AllowSyncIO(this IRequest req)
    {
#if NETCORE
        (req as ServiceStack.Host.NetCore.NetCoreRequest)?.HttpContext.AllowSyncIO();
#endif
        return req;
    }

    public static IResponse AllowSyncIO(this IResponse res)
    {
#if NETCORE
        (res as ServiceStack.Host.NetCore.NetCoreResponse)?.HttpContext.AllowSyncIO();
#endif
        return res;
    }

    public static void Redirect(this IResponse httpRes, string url)
    {
        httpRes.AddHeader(HttpHeaders.Location, url);
        httpRes.EndRequest();
    }

    public static Task ReturnFailedAuthentication(this IAuthSession session, IRequest request)
    {
        var authFeature = HostContext.GetPlugin<AuthFeature>();
        if (authFeature != null)
        {
            if (AuthenticateService.AuthProviders.FirstOrDefault() is AuthProvider defaultAuth)
                return defaultAuth.OnFailedAuthentication(session, request, request.Response);
        }
        return request.Response.ReturnAuthRequired();
    }

    public static Task ReturnAuthRequired(this IResponse httpRes)
    {
        return httpRes.ReturnAuthRequired("Auth Required");
    }

    public static Task ReturnAuthRequired(this IResponse httpRes, string authRealm)
    {
        return httpRes.ReturnAuthRequired(AuthenticationHeaderType.Basic, authRealm);
    }

    public static Task ReturnAuthRequired(this IResponse httpRes, AuthenticationHeaderType AuthType, string authRealm)
    {
        httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
        httpRes.AddHeader(HttpHeaders.WwwAuthenticate, $"{AuthType} realm=\"{authRealm}\"");
        return HostContext.AppHost.HandleShortCircuitedErrors(httpRes.Request, httpRes, httpRes.Request.Dto);
    }

    public static void ClearCookies(this IResponse response)
    {
        var httpRes = response as IHttpResponse;
        httpRes?.ClearCookies();
    }

    /// <summary>
    /// Sets a persistent cookie which never expires
    /// </summary>
    public static void SetPermanentCookie(this IResponse response, string cookieName, string cookieValue)
    {
        var httpRes = response as IHttpResponse;
        httpRes?.Cookies.AddPermanentCookie(cookieName, cookieValue);
    }

    /// <summary>
    /// Sets a session cookie which expires after the browser session closes
    /// </summary>
    public static void SetSessionCookie(this IResponse response, string cookieName, string cookieValue)
    {
        var httpRes = response as IHttpResponse;
        httpRes?.Cookies.AddSessionCookie(cookieName, cookieValue);
    }

    /// <summary>
    /// Sets a persistent cookie which expires after the given time
    /// </summary>
    public static void SetCookie(this IResponse response, string cookieName, string cookieValue, TimeSpan expiresIn, string path = "/")
    {
        response.SetCookie(new Cookie(cookieName, cookieValue, path)
        {
            Expires = DateTime.UtcNow + expiresIn
        });
    }

    public static void SetCookie(this IResponse response, Cookie cookie)
    {
        var httpRes = response as IHttpResponse;
        httpRes?.SetCookie(cookie);
    }

    /// <summary>
    /// Sets a persistent cookie with an expiresAt date
    /// </summary>
    public static void SetCookie(this IResponse response, string cookieName,
        string cookieValue, DateTime expiresAt, string path = "/")
    {
        response.SetCookie(new Cookie(cookieName, cookieValue, path)
        {
            Expires = expiresAt,
        });
    }

    /// <summary>
    /// Deletes a specified cookie by setting its value to empty and expiration to -1 days
    /// </summary>
    public static void DeleteCookie(this IResponse response, string cookieName)
    {
        var httpRes = response as IHttpResponse;
        httpRes?.Cookies.DeleteCookie(cookieName);
    }

    public static Dictionary<string, string> CookiesAsDictionary(this IResponse httpRes)
    {
        return Platform.Instance.GetCookiesAsDictionary(httpRes);
    }

    public static void AddHeaderLastModified(this IResponse httpRes, DateTime? lastModified)
    {
        if (!lastModified.HasValue) return;
        var lastWt = lastModified.Value.ToUniversalTime();
        httpRes.AddHeader(HttpHeaders.LastModified, lastWt.ToString("r"));
    }

    public static string SetParam(this string url, string key, object val)
    {
        return url.SetParam(key, val.ToString());
    }

    public static string SetParam(this string url, string key, string val)
    {
        var addToQueryString = HostContext.Config.AddRedirectParamsToQueryString;
        return addToQueryString
            ? url.SetQueryParam(key, val)
            : url.SetHashParam(key, val);
    }

    public static string AddParam(this string url, string key, object val)
    {
        return url.AddParam(key, val.ToString());
    }

    public static string AddParam(this string url, string key, string val)
    {
        var addToQueryString = HostContext.Config.AddRedirectParamsToQueryString;
        return addToQueryString
            ? url.AddQueryParam(key, val)
            : url.AddHashParam(key, val);
    }

    [Obsolete("Use WriteAsync")]
    public static void Write(this IResponse response, string contents)
    {
#if !NETCORE
            if (response is Host.AspNet.AspNetResponse aspRes)
            {
                aspRes.Response.Write(contents);
                return;
            }
#endif

        if (contents == null)
        {
            response.SetContentLength(0);
            response.EndRequest();
            return;
        }

        //retain behavior with ASP.NET's response.Write(string)
        if (response.ContentType?.IndexOf(';') == -1)
            response.ContentType += ContentFormat.Utf8Suffix;

        var bytes = contents.ToUtf8Bytes();
        response.SetContentLength(bytes.Length);
        response.AllowSyncIO().OutputStream.Write(bytes, 0, bytes.Length);
    }

    public static Task WriteAsync(this IResponse response, string contents)
    {
        if (contents == null)
        {
            response.SetContentLength(0);
            response.EndRequest();
            return TypeConstants.EmptyTask;
        }

        //retain behavior with ASP.NET's response.Write(string)
        if (response.ContentType?.IndexOf(';') == -1)
            response.ContentType += ContentFormat.Utf8Suffix;

        var bytes = contents.ToUtf8Bytes();
        response.SetContentLength(bytes.Length);
        return response.OutputStream.WriteAsync(bytes).AsTask();
    }

    public static async Task WriteAsync(this IResponse response, ReadOnlyMemory<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            response.SetContentLength(0);
            response.EndRequest();
            return;
        }

        response.SetContentLength(bytes.Length);
        await response.OutputStream.WriteAsync(bytes);
        await response.EndHttpHandlerRequestAsync();
    }
 
    public static void EndWith(this IResponse res, HttpStatusCode code, string description=null)
    {
        res.StatusCode = (int)code;
        if (description != null)
            res.StatusDescription = description;
        res.EndRequest();
    }
}

public enum AuthenticationHeaderType
{
    Basic,
    Digest
}