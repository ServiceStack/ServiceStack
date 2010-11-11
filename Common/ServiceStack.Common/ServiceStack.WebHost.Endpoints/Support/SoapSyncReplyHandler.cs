using System.ServiceModel.Channels;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class SoapSyncReplyHandler : EndpointHandlerBase, ISyncReply
	{
		public Message Send(Message requestMsg)
		{
			var endpointAttributes = EndpointAttributes.SyncReply;

			endpointAttributes |= GetType() == typeof(Soap11SyncReplyHandler)
				? EndpointAttributes.Soap11 : EndpointAttributes.Soap12;

			return ExecuteMessage(requestMsg, endpointAttributes);
		}
	}
}