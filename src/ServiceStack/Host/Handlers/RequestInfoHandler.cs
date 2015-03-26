using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Hosting;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    [DataContract]
    public class RequestInfo { }

    [DataContract]
    public class RequestInfoResponse
    {
        [DataMember]
        public string Usage { get; set; }

        [DataMember]
        public string Host { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

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
        public RequestHandlerInfo LastRequestInfo { get; set; }

        [DataMember]
        public Dictionary<string, string> Stats { get; set; }

        [DataMember]
        public List<string> VirtualPathProviderFiles { get; set; }
    }

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
            if (HttpContext.Current != null)
            {
                response.DebugString += HttpContext.Current.Request.GetType().FullName
                    + "|" + HttpContext.Current.Response.GetType().FullName;
            }
            if (HostContext.IsAspNetHost)
            {
                var aspReq = (HttpRequestBase)httpReq.OriginalRequest;
                response.GetLeftPath = aspReq.Url.GetLeftPart(UriPartial.Authority);
                response.Path = aspReq.Path;
                response.UserHostAddress = aspReq.UserHostAddress;
                response.ApplicationPath = aspReq.ApplicationPath;
                response.ApplicationVirtualPath = HostingEnvironment.ApplicationVirtualPath;
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
                            response.LogonUserInfo["User.AccountDomainSid"] = winUser.AccountDomainSid != null
                                ? winUser.AccountDomainSid.ToString()
                                : "null";
                            response.LogonUserInfo["User.IsAccountSid"] = winUser.IsAccountSid().ToString();
                        }
                    }
                }
            }

            var json = JsonSerializer.SerializeToString(response);
            httpRes.ContentType = MimeTypes.Json;
            httpRes.Write(json);
        }

        public override void ProcessRequest(HttpContextBase context)
        {
            var request = context.ToRequest(GetType().GetOperationName());
            ProcessRequestAsync(request, request.Response, request.OperationName);
        }

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
            var response = new RequestInfoResponse
            {
                Usage = "append '?debug=requestinfo' to any querystring",
                Host = HostContext.Config.DebugHttpListenerHostEnvironment + "_v" + Env.ServiceStackVersion + "_" + HostContext.ServiceName,
                Date = DateTime.UtcNow,
                ServiceName = HostContext.ServiceName,
                HandlerFactoryPath = HostContext.Config.HandlerFactoryPath,
                UserHostAddress = httpReq.UserHostAddress,
                HttpMethod = httpReq.Verb,
                AbsoluteUri = httpReq.AbsoluteUri,
                WebHostUrl = HostContext.Config.WebHostUrl,
                ApplicationBaseUrl = httpReq.GetBaseUrl(),
                ResolveAbsoluteUrl = HostContext.AppHost.ResolveAbsoluteUrl("~/resolve", httpReq),
                StripApplicationVirtualPath = HostContext.Config.StripApplicationVirtualPath,
                RawUrl = httpReq.RawUrl,
                ResolvedPathInfo = httpReq.PathInfo,
                ContentType = httpReq.ContentType,
                Headers = ToDictionary(httpReq.Headers),
                QueryString = ToDictionary(httpReq.QueryString),
                FormData = ToDictionary(httpReq.FormData),
                AcceptTypes = new List<string>(httpReq.AcceptTypes ?? new string[0]),
                ContentLength = httpReq.ContentLength,
                OperationName = httpReq.OperationName,
                ResponseContentType = httpReq.ResponseContentType,
                PluginsLoaded = HostContext.AppHost.PluginsLoaded,
                StartUpErrors = HostContext.AppHost.StartUpErrors,
                LastRequestInfo = LastRequestInfo,
                VirtualPathProviderFiles = HostContext.AppHost.VirtualPathProvider.GetAllMatchingFiles("*").Take(1000).Map(x => x.RealPath),
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
                    {"VirtualPathProvider", HostContext.AppHost.VirtualPathProvider.ToString() }
                },
            };
            return response;
        }
    }
}