using System;
using System.Collections.Generic;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Shared.Tests;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class IocAppHost : AppHostHttpListenerBase
    {
        public IocAppHost()
            : base("IocApp Service", typeof(IocService).Assembly) { }

        public override void Configure(Container container)
        {
            IocShared.Configure(this);
        }

        public override void Release(object instance)
        {
            ((IRelease)Container.Adapter).Release(instance);
        }

        public override void OnEndRequest(IRequest request = null)
        {
            base.OnEndRequest(request);
        }
    }

    public class IocServiceAspNetTests : IocServiceTests
    {
        public override IServiceClient CreateClient(ResetIoc request = null)
        {
            var client = new JsonServiceClient(Config.AspNetServiceStackBaseUri);
            client.Post(request ?? new ResetIoc());
            return client;
        }
    }

    public class IocServiceHttpListenerTests : IocServiceTests
    {
        private const string ListeningOn = "http://localhost:1082/";

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
            }
        }

        public override IServiceClient CreateClient(ResetIoc request = null)
        {
            var client = new JsonServiceClient(ListeningOn);
            client.Post(request ?? new ResetIoc());
            return client;
        }
    }

    [TestFixture]
    public abstract class IocServiceTests
    {
        private const int WaitForRequestCleanup = 100;

        public abstract IServiceClient CreateClient(ResetIoc request = null);

        [Test]
        public void Can_resolve_all_dependencies()
        {
            var client = CreateClient();
            try
            {
                var response = client.Get<IocResponse>("ioc");
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
        public void Can_resolve_all_dependencies_Async()
        {
            var client = CreateClient();
            try
            {
                var response = client.Get<IocResponse>("iocasync");
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
            var client = CreateClient();
            client.Get<IocResponse>("ioc");

            var stats = client.Get(new IocStats());
            Assert.That(stats.IocService_DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Does_dispose_service_Async()
        {
            var client = CreateClient();
            client.Get<IocResponse>("iocasync");

            var stats = client.Get(new IocStats());
            Assert.That(stats.IocService_DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Does_dispose_service_when_there_is_an_error()
        {
            var client = CreateClient(new ResetIoc { ThrowErrors = true });
            Assert.Throws<WebServiceException>(() => client.Get<IocResponse>("ioc"));

            var stats = client.Get(new IocStats());
            Assert.That(stats.IocService_DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Does_dispose_service_when_there_is_an_error_Async()
        {
            var client = CreateClient(new ResetIoc { ThrowErrors = true });
            Assert.Throws<WebServiceException>(() => client.Get<IocResponse>("iocasync"));

            var stats = client.Get(new IocStats());
            Assert.That(stats.IocService_DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Does_create_correct_instances_per_scope()
        {
            var client = CreateClient();
            var response1 = client.Get<IocScopeResponse>("iocscope");
            var response2 = client.Get<IocScopeResponse>("iocscope");

            response1.PrintDump();

            Assert.That(response2.Results[typeof(FunqSingletonScope).Name], Is.EqualTo(1));
            Assert.That(response2.Results[typeof(FunqRequestScope).Name], Is.EqualTo(2));
            Assert.That(response2.Results[typeof(FunqNoneScope).Name], Is.EqualTo(4));

            Thread.Sleep(WaitForRequestCleanup);

            var stats = client.Get(new IocStats());
            Assert.That(stats.FunqRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.AltRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
        }

        [Test]
        public void Does_create_correct_instances_per_scope_Async()
        {
            var client = CreateClient();
            var response1 = client.Get<IocScopeResponse>("iocscopeasync");
            var response2 = client.Get<IocScopeResponse>("iocscopeasync");

            response1.PrintDump();
            response2.PrintDump();

            Assert.That(response2.Results[typeof(FunqSingletonScope).Name], Is.EqualTo(1));
            Assert.That(response2.Results[typeof(FunqRequestScope).Name], Is.EqualTo(2));
            Assert.That(response2.Results[typeof(FunqNoneScope).Name], Is.EqualTo(4));

            Thread.Sleep(WaitForRequestCleanup);

            var stats = client.Get(new IocStats());
            Assert.That(stats.FunqRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.AltRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
        }

        [Test]
        public void Does_create_correct_instances_per_scope_with_exception()
        {
            var client = CreateClient();
            try
            {
                client.Get<IocScopeResponse>("iocscope?Throw=true");
            }
            catch { }
            try
            {
                client.Get<IocScopeResponse>("iocscope?Throw=true");
            }
            catch { }

            Thread.Sleep(WaitForRequestCleanup);

            var stats = client.Get(new IocStats());
            Assert.That(stats.FunqRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.AltRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
        }

        [Test]
        public void Does_create_correct_instances_per_scope_with_exception_Async()
        {
            var client = CreateClient();
            try
            {
                client.Get<IocScopeResponse>("iocscopeasync?Throw=true");
            }
            catch { }
            try
            {
                client.Get<IocScopeResponse>("iocscopeasync?Throw=true");
            }
            catch { }

            Thread.Sleep(WaitForRequestCleanup);

            var stats = client.Get(new IocStats());
            Assert.That(stats.FunqRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.AltRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
        }

        [Test]
        public void Does_AutoWire_ActionLevel_RequestFilters()
        {
            try
            {
                var client = CreateClient();
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

        [Test]
        public void Does_AutoWire_ActionLevel_RequestFilters_Async()
        {
            try
            {
                var client = CreateClient();
                var response = client.Get(new ActionAttrAsync());

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

        [Test]
        public void Does_dispose_service_and_Request_and_None_scope_but_not_singletons()
        {
            var client = CreateClient();

            var response = client.Get(new IocDispose());
            response = client.Get(new IocDispose());
            Thread.Sleep(WaitForRequestCleanup);

            var stats = client.Get(new IocStats());
            Assert.That(stats.Container_disposablesCount, Is.EqualTo(0));
            Assert.That(stats.FunqSingletonScopeDisposable_DisposeCount, Is.EqualTo(0));

            Assert.That(stats.IocDisposableService_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.FunqRequestScopeDisposable_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.FunqNoneScopeDisposable_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.FunqRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.AltRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
        }

        [Test]
        public void Does_dispose_service_and_Request_and_None_scope_but_not_singletons_Async()
        {
            var client = CreateClient();

            var response = client.Get(new IocDisposeAsync());
            response = client.Get(new IocDisposeAsync());
            Thread.Sleep(WaitForRequestCleanup);

            var stats = client.Get(new IocStats());
            Assert.That(stats.Container_disposablesCount, Is.EqualTo(0));
            Assert.That(stats.FunqSingletonScopeDisposable_DisposeCount, Is.EqualTo(0));

            Assert.That(stats.IocDisposableService_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.FunqRequestScopeDisposable_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.FunqNoneScopeDisposable_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.FunqRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
            Assert.That(stats.AltRequestScopeDepDisposableProperty_DisposeCount, Is.EqualTo(2));
        }

    }
}