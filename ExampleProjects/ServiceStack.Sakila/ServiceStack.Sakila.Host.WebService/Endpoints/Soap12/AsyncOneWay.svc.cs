using System.ServiceModel.Channels;
using Sakila.ServiceModel;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.Sakila.Host.WebService.Endpoints.Soap12
{
	public class AsyncOneWay : IOneWay
	{
		public void SendOneWay(Message msg)
		{
			string xml = msg.GetReaderAtBodyContents().ReadOuterXml();

			App.Instance.ExecuteXmlService(xml, ModelInfo.Instance);
		}
	}
}