using System.ServiceModel.Channels;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class SoapAsyncOneWayHandler : EndpointHandlerBase, IOneWay
	{
		public void SendOneWay(Message msg)
		{
			var xml = msg.GetReaderAtBodyContents().ReadOuterXml();

			var endpointAttributes = EndpointAttributes.AsyncOneWay;

			endpointAttributes |= GetType() == typeof(Soap11AsyncOneWayHandler)
				? EndpointAttributes.Soap11 : EndpointAttributes.Soap12;
			
			ExecuteXmlService(xml, endpointAttributes);
		}
	}
}