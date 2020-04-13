using System;
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{
    public class ThrowSync : IReturn<ThrowSync> { }
    public class ThrowAsync : IReturn<ThrowAsync> { }
    
    public class ServiceExceptionServices : Service
    {
        public object Any(ThrowSync request) => 
            throw new WebServiceException(nameof(ThrowSync));

        public Task<object> Any(ThrowAsync request) => 
            throw new WebServiceException(nameof(ThrowAsync));
    }
    
    public class ServiceExceptionTests
    {
        public static Exception ServiceEx;
        public static Exception UnHandledEx;
            
        class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(ServiceExceptionTests), typeof(ServiceExceptionServices).Assembly) {}
            
            public override void Configure(Container container)
            {
                ServiceExceptionHandlers.Add((req, dto, ex) => {
                    ServiceEx = ex;
                    return null;
                });
                
                UncaughtExceptionHandlers.Add((req, res, op, ex) => {
                    UnHandledEx = ex;
                });
            }
        }

        private readonly ServiceStackHost appHost;
        public ServiceExceptionTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public async Task ThrowSync_ServiceException_only_calls_ServiceExceptionHandlers()
        {
            ServiceEx = UnHandledEx = null;
            var client = new JsonHttpClient(Config.ListeningOn);

            try
            {
                var response = await client.GetAsync(new ThrowSync());
                Assert.Fail("Should fail");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ServiceEx.Message, Is.EqualTo(nameof(ThrowSync)));
                Assert.That(UnHandledEx, Is.Null);
                Assert.That(ex.Message, Is.EqualTo(nameof(ThrowSync)));
            }
        }

        [Test]
        public async Task ThrowAsync_ServiceException_only_calls_ServiceExceptionHandlers()
        {
            ServiceEx = UnHandledEx = null;
            var client = new JsonHttpClient(Config.ListeningOn);

            try
            {
                var response = await client.GetAsync(new ThrowAsync());
                Assert.Fail("Should fail");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ServiceEx.Message, Is.EqualTo(nameof(ThrowAsync)));
                Assert.That(UnHandledEx, Is.Null);
                Assert.That(ex.Message, Is.EqualTo(nameof(ThrowAsync)));
            }
        }
    }
}