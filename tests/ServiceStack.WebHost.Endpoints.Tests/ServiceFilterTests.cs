using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    class HelloFilter : IReturn<HelloFilterResponse>
    {
        public string Name { get; set; }
        public bool Throw { get; set; }
    }

    class HelloFilterResponse
    {
        public string Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    
    class FiltersService : Service
    {
        public override void OnBeforeExecute(object requestDto)
        {
            Assert.That(Request, Is.Not.Null);
            if (requestDto is HelloFilter dto)
                dto.Name += $" OnBeforeExecute";
        }
        
        public object Any(HelloFilter request) => !request.Throw 
            ? new HelloFilterResponse { Result = $"Hi {request.Name}!" }
            : throw new ArgumentException(request.Name);

        public override object OnAfterExecute(object response)
        {
            Assert.That(Request, Is.Not.Null);
            if (response is HelloFilterResponse dto)
                dto.Result += $" OnAfterExecute"; 
            
            return new HttpResult(response) {
                Headers = {
                    ["X-Filter"] = nameof(OnAfterExecute)
                }
            };
        }
        
        public override Task<object> OnExceptionAsync(object requestDto, Exception ex)
        {
            Assert.That(Request, Is.Not.Null);
            var error = DtoUtils.CreateErrorResponse(requestDto, ex);
            if (error is IHttpError httpError)
            {                
                var errorStatus = httpError.Response.GetResponseStatus();
                errorStatus.Message += " OnExceptionAsync";
            }
            return Task.FromResult(error);
        }
    }
    
    public class ServiceFilterTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(ServiceFilterTests), typeof(FiltersService).Assembly) { }
            public override void Configure(Container container)
            {
            }
        }
        
        private ServiceStackHost appHost;

        [OneTimeSetUp] public void OneTimeSetUp() => appHost = new AppHost()
            .Init()
            .Start(Config.ListeningOn);

        [OneTimeTearDown] public void TestFixtureTearDown() => appHost.Dispose();

        [Test]
        public void Does_call_OnBeforeExecute_and_OnAfterExecute()
        {
            var client = new JsonServiceClient(Config.ListeningOn) {
                ResponseFilter = res => {
                    $"X-Filter: {res.Headers["X-Filter"]}".Print(); 
                    Assert.That(res.Headers["X-Filter"], Is.EqualTo("OnAfterExecute"));
                }
            };

            var request = new HelloFilter { Name = nameof(HelloFilter) };
            var response = client.Get(request);

            Assert.That(response.Result, Is.EqualTo($"Hi {request.Name} OnBeforeExecute! OnAfterExecute"));
        }

        [Test]
        public void Does_call_OnExceptionAsync_on_Error()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var request = new HelloFilter {
                Name = nameof(HelloFilter),
                Throw = true,
            };
            try
            {
                var response = client.Get(request);

                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.Message, Is.EqualTo($"{request.Name} OnBeforeExecute OnExceptionAsync"));
            }
        }
    }
    
}