using System.ServiceModel.Channels;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class SoapAsyncOneWayHandler : EndpointHandlerBase, IOneWay
	{
		public void SendOneWay(Message requestMsg)
		{
			var xml = requestMsg.GetReaderAtBodyContents().ReadOuterXml();

			var endpointAttributes = EndpointAttributes.AsyncOneWay;

			endpointAttributes |= GetType() == typeof(Soap11AsyncOneWayHandler)
				? EndpointAttributes.Soap11 : EndpointAttributes.Soap12;
			
			ExecuteXmlService(xml, GetRequestType(requestMsg, xml), endpointAttributes);
		}
	}
}