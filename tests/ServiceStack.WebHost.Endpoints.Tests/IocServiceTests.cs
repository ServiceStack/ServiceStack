using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class IocServiceTests
	{
		private const string ListeningOn = "http://localhost:82/";

		IocAppHost appHost;

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			appHost = new IocAppHost();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			if (appHost != null)
			{
				appHost.Dispose();
				appHost = null;
			}
		}

		[Test]
		public void Can_resolve_all_dependencies()
		{
			var restClient = new JsonServiceClient(ListeningOn);
			try
			{
				var response = restClient.Get<IocResponse>("ioc");
				var expected = new List<string> {
					typeof(FunqDepCtor).Name,
					typeof(AltDepCtor).Name,
					typeof(FunqDepProperty).Name,
					typeof(AltDepProperty).Name,
				};

				//Console.WriteLine(response.Results.Dump());
				Assert.That(expected.EquivalentTo(response.Results));				
			}
			catch (WebServiceException ex)
			{
				Assert.Fail(ex.ErrorMessage);
			}
		}

		[Test]
		public void Does_dispose_service()
		{
			IocService.DisposedCount = 0;
            IocService.ThrowErrors = false;

			var restClient = new JsonServiceClient(ListeningOn);
			var response = restClient.Get<IocResponse>("ioc");

			Assert.That(IocService.DisposedCount, Is.EqualTo(1));
		}

        [Test]
        public void Does_dispose_service_when_there_is_an_error()
        {
            IocService.DisposedCount = 0;
            IocService.ThrowErrors = true;

            var restClient = new JsonServiceClient(ListeningOn);
            Assert.Throws<WebServiceException>(() => restClient.Get<IocResponse>("ioc"));

            Assert.That(IocService.DisposedCount, Is.EqualTo(1));
        }
	}
}