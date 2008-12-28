using System.Collections.Generic;
using System.Web;
using System.Web.UI;

namespace ServiceStack.WebHost.Endpoints.Support.Endpoints.Controls
{
	internal class Soap12OperationControl : OperationControl 
    {
		public Soap12OperationControl()
		{
			EndpointType = "Soap12";
		}

		public override string RequestUri
		{
			get
			{
				var endpontFileName = ResponseMessage != null ? "SyncReply.svc" : "AsyncOneWay.svc";
				return string.Format("/Endpoints/{0}/{1}", EndpointType, endpontFileName);
			}
		}

		public override string HttpRequestTemplate
		{
			get
			{
				return string.Format(
@"POST {0} HTTP/1.1 
Host: {1} 
Content-Type: text/xml; charset=utf-8
Content-Length: <span class=""value"">length</span>

{2}", RequestUri, HostName, HttpUtility.HtmlEncode(RequestMessage));
			}
		}

		public override string HttpResponseTemplate
		{
			get
			{
				return string.Format(
@"HTTP/1.1 200 OK
Content-Type: text/xml; charset=utf-8
Content-Length: length

{0}", HttpUtility.HtmlEncode(ResponseMessage));
			}
		}

    }
}