using System;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests.Support
{
	public abstract class ServiceClientTestBase : IDisposable
	{
	    protected const string BaseUrl = "http://127.0.0.1:8083/";

		private AppHostHttpListenerBase appHost;

		public abstract AppHostHttpListenerBase CreateListener();

		[OneTimeSetUp]
		public virtual void TestFixtureSetUp()
		{
			appHost = CreateListener();
			appHost.Init();
			appHost.Start(BaseUrl);
		}

		[OneTimeTearDown]
		public void OnTestFixtureTearDown()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (appHost == null) return;
			appHost.Dispose();
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