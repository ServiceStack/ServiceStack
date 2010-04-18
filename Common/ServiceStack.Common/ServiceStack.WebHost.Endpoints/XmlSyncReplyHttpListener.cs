using System;
using System.Net;
using System.Reflection;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	[Obsolete("Use AppHostHttpListenerBase")]
	public abstract class XmlSyncReplyHttpListener 
		: HttpListenerBase
	{
		protected XmlSyncReplyHttpListener()
		{
		}

		protected XmlSyncReplyHttpListener(string serviceName, params Assembly[] assembliesWithServices) 
			: base(serviceName, assembliesWithServices)
		{
		}

		protected override void ProcessRequest(HttpListenerContext context)
		{
			if (string.IsNullOrEmpty(context.Request.RawUrl)) return;

			var operationName = context.Request.GetOperationName();
			var request = CreateRequest(context.Request, operationName);

			const EndpointAttributes endpointAttributes = EndpointAttributes.SyncReply | EndpointAttributes.Xml;
			
			var result = ExecuteService(request, endpointAttributes);

			var response = new HttpListenerResponseWrapper(context.Response);
			response.WriteToResponse(result, x => DataContractSerializer.Instance.Parse(result), ContentType.Xml);
		}
	}

}