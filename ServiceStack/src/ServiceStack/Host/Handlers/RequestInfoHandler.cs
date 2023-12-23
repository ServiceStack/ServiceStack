using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers;

[ExcludeMetadata]
[DataContract]
public class RequestInfo { }

[ExcludeMetadata]
[DataContract]
public class RequestInfoResponse
{
    [DataMember]
    public string Usage { get; set; }

    [DataMember]
    public string Host { get; set; }

    [DataMember]
    public string HostType { get; set; }

    [DataMember]
    public string StartedAt { get; set; }

    [DataMember]
    public string Date { get; set; }

    [DataMember]
    public string ServiceName { get; set; }

    [DataMember]
    public string HandlerFactoryPath { get; set; }

    [DataMember]
    public string UserHostAddress { get; set; }

    [DataMember]
    public string HttpMethod { get; set; }

    [DataMember]
    public string PathInfo { get; set; }

    [DataMember]
    public string OriginalPathInfo { get; set; }

    [DataMember]
    public bool StripApplicationVirtualPath { get; set; }

    [DataMember]
    public string GetLeftPath { get; set; }

    [DataMember]
    public string Path { get; set; }

    [DataMember]
    public string GetPathUrl { get; set; }

    [DataMember]
    public string AbsoluteUri { get; set; }

    [DataMember]
    public string WebHostUrl { get; set; }

    [DataMember]
    public string ApplicationBaseUrl { get; set; }

    [DataMember]
    public string ResolveAbsoluteUrl { get; set; }

    [DataMember]
    public string ApplicationPath { get; set; }

    [DataMember]
    public string ApplicationVirtualPath { get; set; }

    [DataMember]
    public string VirtualAbsolutePathRoot { get; set; }

    [DataMember]
    public string VirtualAppRelativePathRoot { get; set; }

    [DataMember]
    public string RootDirectoryPath { get; set; }

    [DataMember]
    public string ContentRootDirectoryPath { get; set; }

    [DataMember]
    public string CurrentDirectory { get; set; }

    [DataMember]
    public string HandlerFactoryArgs { get; set; }

    [DataMember]
    public string RawUrl { get; set; }

    [DataMember]
    public string Url { get; set; }

    [DataMember]
    public string ContentType { get; set; }

    [DataMember]
    public int Status { get; set; }

    [DataMember]
    public long ContentLength { get; set; }

    [DataMember]
    public Dictionary<string, string> Headers { get; set; }

    [DataMember]
    public Dictionary<string, string> QueryString { get; set; }

    [DataMember]
    public Dictionary<string, string> FormData { get; set; }

    [DataMember]
    public List<string> AcceptTypes { get; set; }

    [DataMember]
    public string OperationName { get; set; }

    [DataMember]
    public string ResponseContentType { get; set; }

    [DataMember]
    public string RequestAttributes { get; set; }

    [DataMember]
    public string Ipv4Addresses { get; set; }

    [DataMember]
    public string Ipv6Addresses { get; set; }

    [DataMember]
    public string ErrorCode { get; set; }

    [DataMember]
    public string ErrorMessage { get; set; }

    [DataMember]
    public Dictionary<string, string> LogonUserInfo { get; set; }

    [DataMember]
    public string DebugString { get; set; }

    [DataMember]
    public List<string> OperationNames { get; set; }

    [DataMember]
    public List<string> AllOperationNames { get; set; }

    [DataMember]
    public Dictionary<string, string> RequestResponseMap { get; set; }

    [DataMember]
    public List<string> PluginsLoaded { get; set; }

    [DataMember]
    public List<ResponseStatus> StartUpErrors { get; set; }

    [DataMember]
    public List<ResponseStatus> AsyncErrors { get; set; }

    [DataMember]
    public RequestHandlerInfo LastRequestInfo { get; set; }

    [DataMember]
    public Dictionary<string, string> Stats { get; set; }

    [DataMember]
    public List<string> VirtualPathProviderFiles { get; set; }
}

[ExcludeMetadata]
public class RequestHandlerInfo
{
    public string HandlerType { get; set; }
    public string OperationName { get; set; }
    public string PathInfo { get; set; }
}

public class RequestInfoHandler : HttpAsyncTaskHandler
{
    public RequestInfoHandler()
    {
        this.RequestName = GetType().Name;
    }

    public RequestInfoResponse RequestInfo { get; set; }

    public static RequestHandlerInfo LastRequestInfo;

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
            return;

        var response = this.RequestInfo ?? GetRequestInfo(httpReq);
        response.HandlerFactoryArgs = HttpHandlerFactory.DebugLastHandlerArgs;
        response.DebugString = "";
#if NETFX || NET472
            if (HttpContext.Current != null)
            {
                response.DebugString += HttpContext.Current.Request.GetType().FullName
                    + "|" + HttpContext.Current.Response.GetType().FullName;
            }

            if (HostContext.IsAspNetHost)
            {
                var aspReq = (HttpRequestBase)httpReq.OriginalRequest;
                response.GetLeftPath = aspReq.Url.GetLeftAuthority();
                response.Path = aspReq.Path;
                response.UserHostAddress = aspReq.UserHostAddress;
                response.ApplicationPath = aspReq.ApplicationPath;
                response.ApplicationVirtualPath = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
                response.VirtualAbsolutePathRoot = VirtualPathUtility.ToAbsolute("/");
                response.VirtualAppRelativePathRoot = VirtualPathUtility.ToAppRelative("/");

                if (!Env.IsMono)
                {
                    var userIdentity = aspReq.LogonUserIdentity;
                    if (userIdentity != null)
                    {
                        response.LogonUserInfo = new Dictionary<string, string> {
                            { "Name", userIdentity.Name },
                            { "AuthenticationType", userIdentity.AuthenticationType },
                            { "IsAuthenticated", userIdentity.IsAuthenticated.ToString() },
                            { "IsAnonymous", userIdentity.IsAnonymous.ToString() },
                            { "IsGuest", userIdentity.IsGuest.ToString() },
                            { "IsSystem", userIdentity.IsSystem.ToString() },
                            { "Groups", userIdentity.Groups.Map(x => x.Value).Join(", ") },
                        };
                        var winUser = userIdentity.User;
                        if (winUser != null)
                        {
                            response.LogonUserInfo["User"] = winUser.Value;
                            response.LogonUserInfo["User.AccountDomainSid"] = winUser.AccountDomainSid?.ToString() ?? "null";
                            response.LogonUserInfo["User.IsAccountSid"] = winUser.IsAccountSid().ToString();
                        }
                    }
                }
        }
#endif

        var json = JsonSerializer.SerializeToString(response);
        httpRes.ContentType = MimeTypes.Json;
        try
        {
            await httpRes.WriteAsync(json).ConfigAwait();
        }
        finally
        {
            httpRes.EndHttpHandlerRequest(skipHeaders: true);
        }
    }

    public static Dictionary<string, string> ToDictionary(NameValueCollection nvc)
    {
        var map = new Dictionary<string, string>();
        for (var i = 0; i < nvc.Count; i++)
        {
            map[nvc.GetKey(i)] = nvc.Get(i);
        }
        return map;
    }

    public static string ToString(NameValueCollection nvc)
    {
        var map = ToDictionary(nvc);
        return TypeSerializer.SerializeToString(map);
    }

    public static RequestInfoResponse GetRequestInfo(IRequest httpReq)
    {
        int.TryParse(httpReq.QueryString["virtualPathCount"], out var virtualPathCount);
        var appHost = HostContext.AppHost;
        var hostType = appHost.GetType();

        var ipv4Addr = "";
        foreach (var entry in ServiceStackHandlerBase.NetworkInterfaceIpv4Addresses)
        {
            if (ipv4Addr.Length > 0)
                ipv4Addr += ", ";
            ipv4Addr += new IPAddress(entry.Key) + "/" + new IPAddress(entry.Value);
        }

        var ipv6Address = "";
        foreach (var addr in ServiceStackHandlerBase.NetworkInterfaceIpv6Addresses)
        {
            if (ipv6Address.Length > 0)
                ipv6Address += ", ";
            ipv6Address += new IPAddress(addr);
        }

        var config = HostContext.Config;
        var response = new RequestInfoResponse
        {
            //Usage = "append '?debug=requestinfo' to any querystring. Optional params: virtualPathCount",
            Host = HostContext.ServiceName + "_" + config.DebugHttpListenerHostEnvironment + "_" + Env.ServerUserAgent,
            HostType = $"{(HostContext.IsAspNetHost ? "ASP.NET" : "SelfHost")} ({hostType.BaseType?.Name ?? hostType.Name})",
            StartedAt = appHost.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            ServiceName = HostContext.ServiceName,
            HandlerFactoryPath = config.HandlerFactoryPath,
            UserHostAddress = httpReq.UserHostAddress,
            HttpMethod = httpReq.Verb,
            AbsoluteUri = httpReq.AbsoluteUri,
            WebHostUrl = config.WebHostUrl,
            ApplicationBaseUrl = httpReq.GetBaseUrl(),
            ResolveAbsoluteUrl = appHost.ResolveAbsoluteUrl("~/resolve", httpReq),
            RootDirectoryPath = appHost.RootDirectory.RealPath,
            ContentRootDirectoryPath = appHost.ContentRootDirectory.RealPath,
            StripApplicationVirtualPath = config.StripApplicationVirtualPath,
            CurrentDirectory = Directory.GetCurrentDirectory(),
            RawUrl = httpReq.RawUrl,
            PathInfo = httpReq.PathInfo,
            OriginalPathInfo = httpReq.OriginalPathInfo,
            ContentType = httpReq.ContentType,
            Headers = ToDictionary(httpReq.Headers),
            QueryString = ToDictionary(httpReq.QueryString),
            FormData = ToDictionary(httpReq.FormData),
            AcceptTypes = new List<string>(httpReq.AcceptTypes ?? TypeConstants.EmptyStringArray),
            ContentLength = httpReq.ContentLength,
            OperationName = httpReq.OperationName,
            ResponseContentType = httpReq.ResponseContentType,
            RequestAttributes = httpReq.GetAttributes().ToString(),
            Ipv4Addresses = ipv4Addr,
            Ipv6Addresses = ipv6Address,
            PluginsLoaded = appHost.PluginsLoaded,
            StartUpErrors = appHost.StartUpErrors,
            AsyncErrors = appHost.AsyncErrors,
            LastRequestInfo = LastRequestInfo,
            VirtualPathProviderFiles = appHost.VirtualFileSources.GetAllMatchingFiles("*").Take(virtualPathCount).Map(x => x.RealPath),
            Stats = new Dictionary<string, string> {
                {"RawHttpHandlers", appHost.RawHttpHandlersArray.Length.ToString() },
                {"PreRequestFilters", appHost.PreRequestFiltersArray.Length.ToString() },
                {"RequestBinders", appHost.RequestBinders.Count.ToString() },
                {"GlobalRequestFilters", appHost.GlobalRequestFiltersArray.Length.ToString() },
                {"GlobalRequestFiltersAsync", appHost.GlobalRequestFiltersAsyncArray.Length.ToString() },
                {"GlobalResponseFilters", appHost.GlobalResponseFiltersArray.Length.ToString() },
                {"GlobalResponseFiltersAsync", appHost.GlobalResponseFiltersAsyncArray.Length.ToString() },
                {"CatchAllHandlers", appHost.CatchAllHandlersArray.Length.ToString() },
                {"Plugins", appHost.Plugins.Count.ToString() },
                {"ViewEngines", appHost.ViewEngines.Count.ToString() },
                {"RequestTypes", appHost.Metadata.RequestTypes.Count.ToString() },
                {"ResponseTypes", appHost.Metadata.ResponseTypes.Count.ToString() },
                {"ServiceTypes", appHost.Metadata.ServiceTypes.Count.ToString() },
                {"RestPaths", appHost.RestPaths.Count.ToString() },
                {"ContentTypes", appHost.ContentTypes.ContentTypeFormats.Count.ToString() },
                {"EnableFeatures", config.EnableFeatures.ToString() },
                {"VirtualPathProvider", appHost.VirtualFileSources.ToString() }
            },
        };
        return response;
    }
}
