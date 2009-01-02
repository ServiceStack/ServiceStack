using System.ServiceModel.Channels;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class SoapAsyncOneWayHandler : EndpointHandlerBase, IOneWay
	{
		public void SendOneWay(Message msg)
		{
			var xml = msg.GetReaderAtBodyContents().ReadOuterXml();
			ExecuteXmlService(xml);
		}
	}
}