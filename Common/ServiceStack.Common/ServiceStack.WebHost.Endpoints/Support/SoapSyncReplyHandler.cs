using System.IO;
using System.ServiceModel.Channels;
using System.Xml;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class SoapSyncReplyHandler : EndpointHandlerBase, ISyncReply
	{
		public Message Send(Message msg)
		{
			string action = msg.Headers.Action;
			string xml = msg.GetReaderAtBodyContents().ReadOuterXml();

			var endpointAttributes = EndpointAttributes.SyncReply;
			endpointAttributes |= GetType() == typeof(Soap11SyncReplyHandler)
				? EndpointAttributes.Soap11 : EndpointAttributes.Soap12;
			string responseXml = ExecuteXmlService(xml, endpointAttributes);

			return Message.CreateMessage(msg.Version, action + "Response", XmlReader.Create(new StringReader(responseXml)));
		}
	}
}