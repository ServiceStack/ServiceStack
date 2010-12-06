using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using ServiceStack.Common.Web;

namespace ServiceStack.WebHost.Endpoints.Support.Metadata.Controls
{
	internal class Soap12OperationControl : OperationControl 
    {
		public Soap12OperationControl()
		{
			EndpointType = EndpointType.Soap12;
		}

		public override string RequestUri
		{
			get
			{
				var endpointConfig = MetadataConfig.GetEndpointConfig(EndpointType);
				var endpontPath = ResponseMessage != null ? endpointConfig.SyncReplyUri : endpointConfig.AsyncOneWayUri;
				return string.Format("/{0}", endpontPath);
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

    }
}