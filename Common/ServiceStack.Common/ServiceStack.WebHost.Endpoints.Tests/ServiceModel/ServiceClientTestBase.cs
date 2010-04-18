using System;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.WebHost.Endpoints.Server;

namespace ServiceStack.WebHost.Endpoints.Tests.ServiceModel
{
	[TestFixture]
	public abstract class ServiceClientTestBase
	{
		private const string BaseUrl = "http://127.0.0.1:8080/";

		private AppHostHttpListenerBase listener;

		public abstract AppHostHttpListenerBase CreateListener();

		[TestFixtureSetUp]
		public virtual void TestFixtureSetUp()
		{
			listener = CreateListener();
			listener.Init();
			listener.Start(BaseUrl);
		}

		public void Send<TRes>(object request, Action<TRes> validate)
		{
			using (var xmlClient = new XmlServiceClient(BaseUrl))
			using (var jsonClient = new JsonServiceClient(BaseUrl))
			using (var jsvClient = new JsvServiceClient(BaseUrl))
			{
				var xmlResponse = xmlClient.Send<TRes>(request);
				validate(xmlResponse);

				var jsonResponse = jsonClient.Send<TRes>(request);
				validate(jsonResponse);

				var jsvResponse = jsvClient.Send<TRes>(request);
				validate(jsvResponse);
			}
		}
	}
}