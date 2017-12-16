using System;
using System.Net;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class ExceptionHandlingTestsAsync
    {
        readonly AppHost appHost;
        public ExceptionHandlingTestsAsync()
        {
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
            appHost.UncaughtExceptionHandlers = null;
        }

        public class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(ExceptionHandlingTestsAsync), typeof(UserService).Assembly) { }

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig { DebugMode = false });

                //Custom global uncaught exception handling strategy
                this.UncaughtExceptionHandlersAsync.Add(async (req, res, operationName, ex) =>
                {
                    await res.WriteAsync($"UncaughtException {ex.GetType().Name}");
                    res.EndRequest(skipHeaders: true);
                });

                this.ServiceExceptionHandlersAsync.Add(async (httpReq, request, ex) =>
                {
                    await Task.Yield();

                    if (request is UncatchedException || request is UncatchedExceptionAsync)
                        throw ex;

                    if (request is CaughtException || request is CaughtExceptionAsync)
                        return DtoUtils.CreateErrorResponse(request, new ArgumentException("ExceptionCaught"));

                    return null;
                });
            }

            public override Task OnUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
            {
                "In OnUncaughtException...".Print();
                return base.OnUncaughtException(httpReq, httpRes, operationName, ex);
            }
        }

        public string PredefinedJsonUrl<T>()
        {
            return Config.ListeningOn + "json/reply/" + typeof(T).Name;
        }
        
        [Test]
        public void Can_override_global_exception_handling()
        {
            var req = (HttpWebRequest)WebRequest.Create(PredefinedJsonUrl<UncatchedException>());
            var res = req.GetResponse().ReadToEnd();
            Assert.AreEqual("UncaughtException ArgumentException", res);
        }

        [Test]
        public void Can_override_global_exception_handling_async()
        {
            var req = (HttpWebRequest)WebRequest.Create(PredefinedJsonUrl<UncatchedExceptionAsync>());
            var res = req.GetResponse().ReadToEnd();
            Assert.AreEqual("UncaughtException ArgumentException", res);
        }

        [Test]
        public void Can_override_caught_exception()
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(PredefinedJsonUrl<CaughtException>());
                var res = req.GetResponse().ReadToEnd();
                Assert.Fail("Should Throw");
            }
            catch (WebException ex)
            {
                Assert.That(ex.IsAny400());
                var json = ex.GetResponseBody();
                var response = json.FromJson<ErrorResponse>();
                Assert.That(response.ResponseStatus.Message, Is.EqualTo("ExceptionCaught"));
            }
        }

        [Test]
        public void Can_override_caught_exception_async()
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(PredefinedJsonUrl<CaughtExceptionAsync>());
                var res = req.GetResponse().ReadToEnd();
                Assert.Fail("Should Throw");
            }
            catch (WebException ex)
            {
                Assert.That(ex.IsAny400());
                var json = ex.GetResponseBody();
                var response = json.FromJson<ErrorResponse>();
                Assert.That(response.ResponseStatus.Message, Is.EqualTo("ExceptionCaught"));
            }
        }

        [Test]
        public void Request_binding_error_raises_UncaughtException()
        {
            var response = PredefinedJsonUrl<ExceptionWithRequestBinding>()
                .AddQueryParam("Id", "NaN")
                .GetStringFromUrl();

            Assert.That(response, Is.EqualTo("UncaughtException SerializationException"));
        }
    }
}