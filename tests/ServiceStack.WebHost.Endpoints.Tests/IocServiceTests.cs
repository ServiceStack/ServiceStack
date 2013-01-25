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
		private const string ListeningOn = "http://localhost:1082/";
        
        private const int WaitForRequestCleanup = 100;
        
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
					typeof(FunqDepDisposableProperty).Name,
					typeof(AltDepProperty).Name,
					typeof(AltDepDisposableProperty).Name,
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
			restClient.Get<IocResponse>("ioc");

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

        [Test]
        public void Does_create_correct_instances_per_scope()
        {
            FunqRequestScopeDepDisposableProperty.DisposeCount = 0;
            AltRequestScopeDepDisposableProperty.DisposeCount = 0;

            var restClient = new JsonServiceClient(ListeningOn);
            var response1 = restClient.Get<IocScopeResponse>("iocscope");
            var response2 = restClient.Get<IocScopeResponse>("iocscope");

            response1.PrintDump();

            Assert.That(response2.Results[typeof(FunqSingletonScope).Name], Is.EqualTo(1));
            Assert.That(response2.Results[typeof(FunqRequestScope).Name], Is.EqualTo(2));
            Assert.That(response2.Results[typeof(FunqNoneScope).Name], Is.EqualTo(4));

            Assert.That(FunqRequestScopeDepDisposableProperty.DisposeCount, Is.EqualTo(2));
            Assert.That(AltRequestScopeDepDisposableProperty.DisposeCount, Is.EqualTo(2));
        }

        [Test]
        public void Does_create_correct_instances_per_scope_with_exception()
        {
            FunqRequestScopeDepDisposableProperty.DisposeCount = 0;
            AltRequestScopeDepDisposableProperty.DisposeCount = 0;

            var restClient = new JsonServiceClient(ListeningOn);
            try {
                restClient.Get<IocScopeResponse>("iocscope?Throw=true");
            } catch { }
            try {
                restClient.Get<IocScopeResponse>("iocscope?Throw=true");
            } catch { }

            Thread.Sleep(WaitForRequestCleanup);

            Assert.That(FunqRequestScopeDepDisposableProperty.DisposeCount, Is.EqualTo(2));
            Assert.That(AltRequestScopeDepDisposableProperty.DisposeCount, Is.EqualTo(2));
        }

	    [Test]
	    public void Does_AutoWire_ActionLevel_RequestFilters()
	    {
            try
            {
                var client = new JsonServiceClient(ListeningOn);
                var response = client.Get(new ActionAttr());

                var expected = new List<string> {
					typeof(FunqDepProperty).Name,
					typeof(FunqDepDisposableProperty).Name,
					typeof(AltDepProperty).Name,
					typeof(AltDepDisposableProperty).Name,
				};

                response.Results.PrintDump();

                Assert.That(expected.EquivalentTo(response.Results));

            }
            catch (Exception ex)
            {
                ex.Message.Print();
                throw;
            }
        }

        private static void ResetDisposables()
        {
            IocService.DisposedCount =
            IocDisposableService.DisposeCount =
            FunqSingletonScopeDisposable.DisposeCount =
            FunqRequestScopeDisposable.DisposeCount =
            FunqNoneScopeDisposable.DisposeCount = 
            FunqRequestScopeDepDisposableProperty.DisposeCount =
            AltRequestScopeDepDisposableProperty.DisposeCount =
                0;
        }

        [Test]
        public void Does_dispose_service_and_Request_and_None_scope_but_not_singletons()
        {
            ResetDisposables();

            var restClient = new JsonServiceClient(ListeningOn);
            var response = restClient.Get(new IocDispose());
            response = restClient.Get(new IocDispose());
            Thread.Sleep(WaitForRequestCleanup);

            Assert.That(appHost.Container.disposablesCount, Is.EqualTo(0));
            Assert.That(FunqSingletonScopeDisposable.DisposeCount, Is.EqualTo(0));

            Assert.That(IocDisposableService.DisposeCount, Is.EqualTo(2));
            Assert.That(FunqRequestScopeDisposable.DisposeCount, Is.EqualTo(2));
            Assert.That(FunqNoneScopeDisposable.DisposeCount, Is.EqualTo(2));
            Assert.That(FunqRequestScopeDepDisposableProperty.DisposeCount, Is.EqualTo(2));
            Assert.That(AltRequestScopeDepDisposableProperty.DisposeCount, Is.EqualTo(2));
        }

    }
}