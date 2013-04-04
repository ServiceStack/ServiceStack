using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using HttpRequestWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpRequestWrapper;
using HttpResponseWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpResponseWrapper;

namespace ServiceStack.WebHost.Endpoints.Support
{
	[DataContract]
	public class RequestInfo { }

	[DataContract]
	public class RequestInfoResponse
	{
		[DataMember]
		public string Host { get; set; }

		[DataMember]
		public DateTime Date { get; set; }

		[DataMember]
		public string ServiceName { get; set; }

		[DataMember]
		public string HandlerPath { get; set; }

		[DataMember]
		public string UserHostAddress { get; set; }

		[DataMember]
		public string HttpMethod { get; set; }

		[DataMember]
		public string PathInfo { get; set; }

		[DataMember]
		public string ResolvedPathInfo { get; set; }

		[DataMember]
		public string Path { get; set; }

		[DataMember]
		public string AbsoluteUri { get; set; }

		[DataMember]
		public string ApplicationPath { get; set; }

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
        public string DebugString { get; set; }

        [DataMember]
        public List<string> OperationNames { get; set; }

        [DataMember]
        public List<string> AllOperationNames { get; set; }

        [DataMember]
        public Dictionary<string, string> RequestResponseMap { get; set; }
    }

	public class RequestInfoHandler
		: IServiceStackHttpHandler, IHttpHandler
	{
		public const string RestPath = "_requestinfo";

		public RequestInfoResponse RequestInfo { get; set; }

		public void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			var response = this.RequestInfo ?? GetRequestInfo(httpReq);
			response.HandlerFactoryArgs = ServiceStackHttpHandlerFactory.DebugLastHandlerArgs;
			response.DebugString = "";
			if (HttpContext.Current != null)
			{
				response.DebugString += HttpContext.Current.Request.GetType().FullName
					+ "|" + HttpContext.Current.Response.GetType().FullName;
			}

			var json = JsonSerializer.SerializeToString(response);
			httpRes.ContentType = ContentType.Json;
			httpRes.Write(json);
		}

		public void ProcessRequest(HttpContext context)
		{
			ProcessRequest(
				new HttpRequestWrapper(typeof(RequestInfo).Name, context.Request),
				new HttpResponseWrapper(context.Response),
				typeof(RequestInfo).Name);
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

		public static RequestInfoResponse GetRequestInfo(IHttpRequest httpReq)
		{
			var response = new RequestInfoResponse
			{
				Host = EndpointHost.Config.DebugHttpListenerHostEnvironment + "_v" + Env.ServiceStackVersion + "_" + EndpointHost.Config.ServiceName,
				Date = DateTime.UtcNow,
				ServiceName = EndpointHost.Config.ServiceName,
				UserHostAddress = httpReq.UserHostAddress,
				HttpMethod = httpReq.HttpMethod,
				AbsoluteUri = httpReq.AbsoluteUri,
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
			};
			return response;
		}

		public bool IsReusable
		{
			get { return false; }
		}
	}
}