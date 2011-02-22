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
		public List<RequestInfoItem> Results { get; set; }
	}

	[DataContract]
	public class RequestInfoItem
	{
		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public string Value { get; set; }
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
				Results = new List<RequestInfoItem>
				{
					new RequestInfoItem { Name = "UserHostAddress", Value = httpReq.UserHostAddress },
					new RequestInfoItem { Name = "HttpMethod", Value = httpReq.HttpMethod },
					new RequestInfoItem { Name = "AbsoluteUri", Value = httpReq.AbsoluteUri },
					new RequestInfoItem { Name = "RawUrl", Value = httpReq.RawUrl },
					new RequestInfoItem { Name = "PathInfo", Value = httpReq.PathInfo },
					new RequestInfoItem { Name = "ContentType", Value = httpReq.ContentType },
					new RequestInfoItem { Name = "QueryString", Value = ToString(httpReq.QueryString) },
					new RequestInfoItem { Name = "Headers", Value = ToString(httpReq.Headers) },
					new RequestInfoItem { Name = "AcceptTypes", Value = TypeSerializer.SerializeToString(httpReq.AcceptTypes) },
					new RequestInfoItem { Name = "ContentLength", Value = httpReq.ContentLength.ToString() },
					new RequestInfoItem { Name = "OperationName", Value = httpReq.OperationName },
					new RequestInfoItem { Name = "ResponseContentType", Value = httpReq.ResponseContentType },
				}
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