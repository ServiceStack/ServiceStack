using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Web;
using ServiceStack.Host.AspNet;
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

    public class RequestInfoHandler : HttpAsyncTaskHandler
	{
		public const string RestPath = "_requestinfo";

		public RequestInfoResponse RequestInfo { get; set; }

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

            var json = JsonSerializer.SerializeToString(response);
            httpRes.ContentType = MimeTypes.Json;
			httpRes.Write(json);
		}

		public override void ProcessRequest(HttpContextBase context)
		{
		    var request = context.ToRequest(GetType().Name);
			ProcessRequestAsync(request, request.Response, request.OperationName);
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
			var response = new RequestInfoResponse
			{
				Host = HostContext.Config.DebugHttpListenerHostEnvironment + "_v" + Env.ServiceStackVersion + "_" + HostContext.ServiceName,
				Date = DateTime.UtcNow,
				ServiceName = HostContext.ServiceName,
				UserHostAddress = httpReq.UserHostAddress,
				HttpMethod = httpReq.Verb,
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
	}
}