using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class TypedFilterTests
    {
        private class Dto : IReturn<Dto>
        {
            public bool RequestFilter { get; set; }
            public bool ResponseFilter { get; set; }
        }

        private class DtoService : Service
        {
            public object Any(Dto r) => r;
        }

        private interface IDependency { }
        private class Dependency : IDependency { }

        private class TypedRequestFilter : ITypedFilter<Dto>
        {
            public TypedRequestFilter(IDependency dependency) => Dependency = dependency;

            public IDependency Dependency { get; }

            public void Invoke(IRequest req, IResponse res, Dto dto) => dto.RequestFilter = true;
        }

        private class TypedResponseFilter : ITypedFilter<Dto>
        {
            public TypedResponseFilter(IDependency dependency) => Dependency = dependency;

            public IDependency Dependency { get; }

            public void Invoke(IRequest req, IResponse res, Dto dto) => dto.ResponseFilter = true;
        }

        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void OnTestFixtureSetup()
        {
            appHost = new BasicAppHost(typeof(DtoService).Assembly)
            {
                ConfigureContainer = c =>
                {
                    c.RegisterAutoWiredAs<Dependency, IDependency>();
                    c.RegisterAutoWired<TypedRequestFilter>();
                    c.RegisterAutoWired<TypedResponseFilter>();
                }
            };
            appHost.RegisterTypedRequestFilter(c => c.Resolve<TypedRequestFilter>());
            appHost.RegisterTypedResponseFilter(c => c.Resolve<TypedResponseFilter>());
            appHost.Init();
        }

        [OneTimeTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Request_filter_auto_wired()
        {
            // Arrange
            var filter = appHost.Resolve<TypedRequestFilter>();

            // Assert
            Assert.NotNull(filter.Dependency);
        }

        [Test]
        public void Response_filter_auto_wired()
        {
            // Arrange
            var filter = appHost.Resolve<TypedResponseFilter>();

            // Assert
            Assert.NotNull(filter.Dependency);
        }

        [Test]
        public void Request_filter_executed()
        {
            // Arrange
            var dto = new Dto();
            var request = new BasicRequest(dto);

            // Act
            var response = appHost.ServiceController.Execute(dto, request, true) as Dto;

            // Assert
            Assert.NotNull(response);
            Assert.IsTrue(response.RequestFilter);
        }

        [Test]
        public void Response_filter_executed()
        {
            // Arrange
            var dto = new Dto();
            var request = new BasicRequest(dto);

            // Act
            var response = appHost.ServiceController.Execute(dto, request, true) as Dto;

            // Assert
            Assert.NotNull(response);
            Assert.IsTrue(response.ResponseFilter);
        }
    }
}
