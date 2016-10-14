using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Web;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    [Exclude(Feature.Soap)]
    [DataContract]
    public class RequestInfo { }

    [Exclude(Feature.Soap)]
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
        public string ResolvedPathInfo { get; set; }

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

    [Exclude(Feature.Soap)]
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

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var response = this.RequestInfo ?? GetRequestInfo(httpReq);
            response.HandlerFactoryArgs = HttpHandlerFactory.DebugLastHandlerArgs;
            response.DebugString = "";
#if !NETSTANDARD1_6
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
            httpRes.Write(json);
            httpRes.EndHttpHandlerRequest(skipHeaders:true);
        }

#if !NETSTANDARD1_6
        public override void ProcessRequest(HttpContextBase context)
        {
            var request = context.ToRequest(GetType().GetOperationName());
            ProcessRequestAsync(request, request.Response, request.OperationName);
        }
#endif
        public static Dictionary<string, string> ToDictionary(INameValueCollection nvc)
        {
            var map = new Dictionary<string, string>();
            for (var i = 0; i < nvc.Count; i++)
            {
                map[nvc.GetKey(i)] = nvc.Get(i);
            }
            return map;
        }

        public static string ToString(INameValueCollection nvc)
        {
            var map = ToDictionary(nvc);
            return TypeSerializer.SerializeToString(map);
        }

        public static RequestInfoResponse GetRequestInfo(IRequest httpReq)
        {
            int virtualPathCount = 0;
            int.TryParse(httpReq.QueryString["virtualPathCount"], out virtualPathCount);
            var hostType = HostContext.AppHost.GetType();

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

            var response = new RequestInfoResponse
            {
                Usage = "append '?debug=requestinfo' to any querystring. Optional params: virtualPathCount",
                Host = HostContext.ServiceName + "_" + HostContext.Config.DebugHttpListenerHostEnvironment + "_" + Env.ServerUserAgent,
                HostType = "{0} ({1})".Fmt(HostContext.IsAspNetHost ? "ASP.NET" : "SelfHost", hostType.BaseType()?.Name ?? hostType.Name),
                StartedAt = HostContext.AppHost.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ServiceName = HostContext.ServiceName,
                HandlerFactoryPath = HostContext.Config.HandlerFactoryPath,
                UserHostAddress = httpReq.UserHostAddress,
                HttpMethod = httpReq.Verb,
                AbsoluteUri = httpReq.AbsoluteUri,
                WebHostUrl = HostContext.Config.WebHostUrl,
                ApplicationBaseUrl = httpReq.GetBaseUrl(),
                ResolveAbsoluteUrl = HostContext.AppHost.ResolveAbsoluteUrl("~/resolve", httpReq),
                RootDirectoryPath = HostContext.VirtualFileSources.RootDirectory.RealPath,
                StripApplicationVirtualPath = HostContext.Config.StripApplicationVirtualPath,
                CurrentDirectory = Directory.GetCurrentDirectory(),
                RawUrl = httpReq.RawUrl,
                ResolvedPathInfo = httpReq.PathInfo,
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
                PluginsLoaded = HostContext.AppHost.PluginsLoaded,
                StartUpErrors = HostContext.AppHost.StartUpErrors,
                AsyncErrors = HostContext.AppHost.AsyncErrors,
                LastRequestInfo = LastRequestInfo,
                VirtualPathProviderFiles = HostContext.AppHost.VirtualFileSources.GetAllMatchingFiles("*").Take(virtualPathCount).Map(x => x.RealPath),
                Stats = new Dictionary<string, string> {
                    {"RawHttpHandlers", HostContext.AppHost.RawHttpHandlers.Count.ToString() },
                    {"PreRequestFilters", HostContext.AppHost.PreRequestFilters.Count.ToString() },
                    {"RequestBinders", HostContext.AppHost.RequestBinders.Count.ToString() },
                    {"GlobalRequestFilters", HostContext.AppHost.GlobalRequestFilters.Count.ToString() },
                    {"GlobalResponseFilters", HostContext.AppHost.GlobalResponseFilters.Count.ToString() },
                    {"CatchAllHandlers", HostContext.AppHost.CatchAllHandlers.Count.ToString() },
                    {"Plugins", HostContext.AppHost.Plugins.Count.ToString() },
                    {"ViewEngines", HostContext.AppHost.ViewEngines.Count.ToString() },
                    {"RequestTypes", HostContext.AppHost.Metadata.RequestTypes.Count.ToString() },
                    {"ResponseTypes", HostContext.AppHost.Metadata.ResponseTypes.Count.ToString() },
                    {"ServiceTypes", HostContext.AppHost.Metadata.ServiceTypes.Count.ToString() },
                    {"RestPaths", HostContext.AppHost.RestPaths.Count.ToString() },
                    {"ContentTypes", HostContext.AppHost.ContentTypes.ContentTypeFormats.Count.ToString() },
                    {"EnableFeatures", HostContext.Config.EnableFeatures.ToString() },
                    {"VirtualPathProvider", HostContext.AppHost.VirtualFileSources.ToString() }
                },
            };
            return response;
        }
    }
}