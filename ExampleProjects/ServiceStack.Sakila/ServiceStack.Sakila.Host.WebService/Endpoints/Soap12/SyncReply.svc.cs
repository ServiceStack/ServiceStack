using System.IO;
using System.ServiceModel.Channels;
using System.Xml;
using Sakila.ServiceModel;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.Sakila.Host.WebService.Endpoints.Soap12
{
	public class SyncReply : ISyncReply
	{
		public Message Send(Message msg)
		{
			string action = msg.Headers.Action;
			string xml = msg.GetReaderAtBodyContents().ReadOuterXml();

			string responseXml = App.Instance.ExecuteXmlService(xml, ModelInfo.Instance);

			return Message.CreateMessage
				(
					MessageVersion.Default, action + "Response",
					XmlReader.Create(new StringReader(responseXml))
				);
		}
	}
}