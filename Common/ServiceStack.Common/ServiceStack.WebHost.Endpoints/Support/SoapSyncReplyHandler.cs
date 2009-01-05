using System.IO;
using System.ServiceModel.Channels;
using System.Xml;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class SoapSyncReplyHandler : EndpointHandlerBase, ISyncReply
	{
		public Message Send(Message msg)
		{
			string action = msg.Headers.Action;
			string xml = msg.GetReaderAtBodyContents().ReadOuterXml();

			string responseXml = ExecuteXmlService(xml);

			return Message.CreateMessage(msg.Version, action + "Response", XmlReader.Create(new StringReader(responseXml)));
		}
	}
}