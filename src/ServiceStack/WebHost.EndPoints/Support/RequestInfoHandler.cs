using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
	[DataContract]
	public class RequestInfo { }

	[DataContract]
	public class RequestInfoResponse
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public decimal Version { get; set; }

		[DataMember]
		public string UserHostAddress { get; set; }

		[DataMember]
		public string HttpMethod { get; set; }

		[DataMember]
		public string AbsoluteUri { get; set; }

		[DataMember]
		public string RawUrl { get; set; }

		[DataMember]
		public string PathInfo { get; set; }

		[DataMember]
		public string ContentType { get; set; }

		[DataMember]
		public Dictionary<string, string> Headers { get; set; }

		[DataMember]
		public Dictionary<string, string> QueryString { get; set; }

		[DataMember]
		public Dictionary<string, string> FormData { get; set; }

		[DataMember]
		public List<string> AcceptTypes { get; set; }

		[DataMember]
		public long ContentLength { get; set; }

		[DataMember]
		public string OperationName { get; set; }

		[DataMember]
		public string ResponseContentType { get; set; }
	}

	public class RequestInfoHandler
		: IServiceStackHttpHandler, IHttpHandler
	{
		public const string RestPath = "_requestinfo";

		public Dictionary<string, string> ToDictionary(NameValueCollection nvc)
		{
			var map = new Dictionary<string, string>();
			for (var i = 0; i < nvc.Count; i++)
			{
				map[nvc.GetKey(i)] = nvc.Get(i);
			}
			return map;
		}

		public string ToString(NameValueCollection nvc)
		{
			var map = ToDictionary(nvc);
			return TypeSerializer.SerializeToString(map);
		}

		public void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			var response = new RequestInfoResponse
			{
				Id = EndpointHost.Config.ServiceName,
				Version = Env.ServiceStackVersion,
				UserHostAddress = httpReq.UserHostAddress,
				HttpMethod = httpReq.HttpMethod,
				AbsoluteUri = httpReq.AbsoluteUri,
				RawUrl = httpReq.RawUrl,
				PathInfo = httpReq.PathInfo,
				ContentType = httpReq.ContentType,
				Headers = ToDictionary(httpReq.Headers),
				QueryString = ToDictionary(httpReq.QueryString),
				FormData = ToDictionary(httpReq.FormData),
				AcceptTypes = new List<string>(httpReq.AcceptTypes),
				ContentLength = httpReq.ContentLength,
				OperationName = httpReq.OperationName,
				ResponseContentType = httpReq.ResponseContentType,
			};

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

		public bool IsReusable
		{
			get { return true; }
		}
	}
}