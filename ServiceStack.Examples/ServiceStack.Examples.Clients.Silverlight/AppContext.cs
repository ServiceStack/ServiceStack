using ServiceStack.Service;
using SilverlightStack.ServiceClient;

namespace ServiceStack.Examples.Clients.Silverlight
{
	public class AppContext
	{
		public static AppContext Instance = new AppContext();

		public IAsyncServiceClient ServiceClient { get; private set; }

		public ExampleContext ExampleContext { get; private set; }


		private AppContext()
		{
			this.ServiceClient = new XmlAsyncServiceClient("http://www.servicestack.net/ServiceStack.Examples.Host.Web/Public/Xml/SyncReply");

			this.ExampleContext = new ExampleContext(this.ServiceClient, this);
		}

	}
}