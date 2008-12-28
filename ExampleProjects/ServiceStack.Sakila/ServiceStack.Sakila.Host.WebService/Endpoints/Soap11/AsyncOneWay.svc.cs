using System.ServiceModel.Channels;
using Sakila.ServiceModel;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.Sakila.Host.WebService.Endpoints.Soap11
{
	public class AsyncOneWay : IOneWay
	{
		public void SendOneWay(Message msg)
		{
			var xml = msg.GetReaderAtBodyContents().ReadOuterXml();

			App.Instance.ExecuteXmlService(xml, ModelInfo.Instance);
		}
	}
}