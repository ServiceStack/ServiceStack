using System;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NUnit.Framework;
using Funq;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/users")]
    public class User { }
    public class UserResponse : IHasResponseStatus
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class UserService : Service
    {
        public object Get(User request)
        {
            return new HttpError(HttpStatusCode.BadRequest, "CanNotExecute", "Failed to execute!");
        }

        public object Post(User request)
        {
            throw new HttpError(HttpStatusCode.BadRequest, "CanNotExecute", "Failed to execute!");
        }

        public object Delete(User request)
        {
            throw new HttpError(HttpStatusCode.Forbidden, "CanNotExecute", "Failed to execute!");
        }

        public object Put(User request)
        {
            throw new ArgumentException();
        }
    }

    public class CustomException : ArgumentException
    {
        public CustomException() : base("User Defined Error") { }
    }

    public class ExceptionWithResponseStatus { }
    public class ExceptionWithResponseStatusResponse
    {
        public ResponseStatus ResponseStatus { get; set; }
    }
    public class ExceptionWithResponseStatusService : Service
    {
        public object Any(ExceptionWithResponseStatus request)
        {
            throw new CustomException();
        }
    }

    public class ExceptionNoResponseStatus { }
    public class ExceptionNoResponseStatusResponse { }
    public class ExceptionNoResponseStatusService : Service
    {
        public object Any(ExceptionNoResponseStatus request)
        {
            throw new CustomException();
        }
    }

    public class ExceptionNoResponseDto { }
    public class ExceptionNoResponseDtoService : Service
    {
        public object Any(ExceptionNoResponseDto request)
        {
            throw new CustomException();
        }
    }

    public class ExceptionReturnVoid : IReturnVoid { }
    public class ExceptionReturnVoidService : Service
    {
        public void Any(ExceptionReturnVoid request)
        {
            throw new CustomException();
        }
    }

    public class CaughtException { }
    public class CaughtExceptionAsync { }
    public class CaughtExceptionService : Service
    {
        public object Any(CaughtException request)
        {
            throw new ArgumentException();
        }

        public async Task<object> Any(CaughtExceptionAsync request)
        {
            await Task.Yield();
            throw new ArgumentException();
        }
    }

    public class UncatchedException { }
    public class UncatchedExceptionAsync { }
    public class UncatchedExceptionResponse { }
    public class UncatchedExceptionService : Service
    {
        public object Any(UncatchedException request)
        {
            //We don't wrap a try..catch block around the service (which happens with ServiceBase<> automatically)
            //so the global exception handling strategy is invoked
            throw new ArgumentException();
        }

        public async Task<object> Any(UncatchedExceptionAsync request)
        {
            await Task.Yield();
            throw new ArgumentException();
        }
    }

    [Route("/binding-error/{Id}")]
    public class ExceptionWithRequestBinding
    {
        public int Id { get; set; }
    }

    public class ExceptionWithRequestBindingService : Service
    {
        public object Any(ExceptionWithRequestBinding request)
        {
            return request;
        }
    }

    public class CustomHttpError
    {
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; }
    }
    public class CustomHttpErrorResponse
    {
        public string Custom { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    public class CustomHttpErrorService : Service
    {
        public object Any(CustomHttpError request)
        {
            throw new HttpError(request.StatusCode, request.StatusDescription);
        }
    }

    public class CustomFieldHttpError { }
    public class CustomFieldHttpErrorResponse
    {
        public string Custom { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    public class CustomFieldHttpErrorService : Service
    {
        public object Any(CustomFieldHttpError request)
        {
            throw new HttpError(new CustomFieldHttpErrorResponse
            {
                Custom = "Ignored",
                ResponseStatus = new ResponseStatus("StatusErrorCode", "StatusErrorMessage")
            },
            500,
            "HeaderErrorCode");
        }
    }


    public class DirectHttpError { }
    public class DirectResponseService : Service
    {
        public object Any(DirectHttpError request)
        {
            base.Response.StatusCode = 500;
            base.Response.StatusDescription = "HeaderErrorCode";

            return new CustomFieldHttpErrorResponse
            {
                Custom = "Not Ignored",
                ResponseStatus = new ResponseStatus("StatusErrorCode", "StatusErrorMessage")
            };
        }
    }

    [TestFixture]
    public class ExceptionHandlingTests
    {
        private const string ListeningOn = "http://localhost:1337/";

        public class ExceptionHandlingAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public ExceptionHandlingAppHostHttpListener()
                : base("Exception handling tests", typeof(UserService).Assembly) { }

            public override void Configure(Container container)
            {
                JsConfig.EmitCamelCaseNames = true;

                SetConfig(new HostConfig { DebugMode = false });

                //Custom global uncaught exception handling strategy
                this.UncaughtExceptionHandlers.Add((req, res, operationName, ex) =>
                {
                    res.Write(string.Format("UncaughtException {0}", ex.GetType().Name));
                    res.EndRequest(skipHeaders: true);
                });

                this.ServiceExceptionHandlers.Add((httpReq, request, ex) =>
                {
                    if (request is UncatchedException || request is UncatchedExceptionAsync)
                        throw ex;

                    if (request is CaughtException || request is CaughtExceptionAsync)
                    {
                        return DtoUtils.CreateErrorResponse(request, new ArgumentException("ExceptionCaught"));
                    }

                    return null;
                });
            }

            public override void OnExceptionTypeFilter(Exception ex, ResponseStatus responseStatus)
            {
                "In OnExceptionTypeFilter...".Print();
                base.OnExceptionTypeFilter(ex, responseStatus);
            }

            public override void OnUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
            {
                "In OnUncaughtException...".Print();
                base.OnUncaughtException(httpReq, httpRes, operationName, ex);
            }
        }

        ExceptionHandlingAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ExceptionHandlingAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
            appHost.UncaughtExceptionHandlers = null;
        }

        static IRestClient[] ServiceClients = 
		{
			new JsonServiceClient(ListeningOn),
			new XmlServiceClient(ListeningOn),
			new JsvServiceClient(ListeningOn)
			//SOAP not supported in HttpListener
			//new Soap11ServiceClient(ServiceClientBaseUri),
			//new Soap12ServiceClient(ServiceClientBaseUri)
		};


        [Test, TestCaseSource("ServiceClients")]
        public void Handles_Returned_Http_Error(IRestClient client)
        {
            try
            {
                client.Get<UserResponse>("/users");
                Assert.Fail();
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
                Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
                Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
            }
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Handles_Thrown_Http_Error(IRestClient client)
        {
            try
            {
                client.Post<UserResponse>("/users", new User());
                Assert.Fail();
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
                Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
                Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
            }
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Handles_Thrown_Http_Error_With_Forbidden_status_code(IRestClient client)
        {
            try
            {
                client.Delete<UserResponse>("/users");
                Assert.Fail();
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
                Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.Forbidden));
                Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
            }
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Handles_Normal_Exception(IRestClient client)
        {
            try
            {
                client.Put<UserResponse>("/users", new User());
                Assert.Fail();
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("ArgumentException"));
                Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
            }
        }

        public string PredefinedJsonUrl<T>()
        {
            return ListeningOn + "json/reply/" + typeof(T).Name;
        }

        [Test]
        public void Returns_populated_dto_when_has_ResponseStatus()
        {
            try
            {
                var json = PredefinedJsonUrl<ExceptionWithResponseStatus>().GetJsonFromUrl();
                Assert.Fail("Should throw");
            }
            catch (WebException webEx)
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
                var body = errorResponse.GetResponseStream().ReadFully().FromUtf8Bytes();
                Assert.That(body, Is.EqualTo(
                    "{\"responseStatus\":{\"errorCode\":\"CustomException\",\"message\":\"User Defined Error\",\"errors\":[]}}"));
            }
        }

        [Test]
        public void Returns_empty_dto_when_NoResponseStatus()
        {
            try
            {
                var json = PredefinedJsonUrl<ExceptionNoResponseStatus>().GetJsonFromUrl();
                Assert.Fail("Should throw");
            }
            catch (WebException webEx)
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
                var body = errorResponse.GetResponseStream().ReadFully().FromUtf8Bytes();
                Assert.That(body, Is.EqualTo("{}"));
            }
        }

        [Test]
        public void Returns_no_body_when_NoResponseDto()
        {
            try
            {
                var json = PredefinedJsonUrl<ExceptionNoResponseDto>().GetJsonFromUrl();
                Assert.Fail("Should throw");
            }
            catch (WebException webEx)
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
                var body = errorResponse.GetResponseStream().ReadFully().FromUtf8Bytes();
                Assert.That(body, Is.StringStarting("{\"responseStatus\":{\"errorCode\":\"CustomException\",\"message\":\"User Defined Error\""));
            }
        }

        [Test]
        public void Returns_exception_when_ReturnVoid()
        {
            try
            {
                var json = PredefinedJsonUrl<ExceptionReturnVoid>().GetJsonFromUrl();
                Assert.Fail("Should throw");
            }
            catch (WebException webEx)
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
                var body = errorResponse.GetResponseStream().ReadFully().FromUtf8Bytes();
                Assert.That(body, Is.StringStarting("{\"responseStatus\":{\"errorCode\":\"CustomException\",\"message\":\"User Defined Error\""));
            }

            try
            {
                var client = new JsonServiceClient(ListeningOn);
                client.Get(new ExceptionReturnVoid());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo(typeof(CustomException).Name));
                Assert.That(ex.ErrorCode, Is.EqualTo(typeof(CustomException).Name));
                Assert.That(ex.ErrorMessage, Is.EqualTo("User Defined Error"));
                Assert.That(ex.ResponseBody, Is.StringStarting("{\"responseStatus\":{\"errorCode\":\"CustomException\",\"message\":\"User Defined Error\""));
            }
        }

        [Test]
        public void Returns_custom_ResponseStatus_with_CustomFieldHttpError()
        {
            try
            {
                var json = PredefinedJsonUrl<CustomFieldHttpError>().GetJsonFromUrl();
                Assert.Fail("Should throw");
            }
            catch (WebException webEx)
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
                Assert.That((int)errorResponse.StatusCode, Is.EqualTo(500));
                Assert.That(errorResponse.StatusDescription, Is.EqualTo("HeaderErrorCode"));

                var body = errorResponse.GetResponseStream().ReadFully().FromUtf8Bytes();
                var customResponse = body.FromJson<CustomFieldHttpErrorResponse>();
                var errorStatus = customResponse.ResponseStatus;
                Assert.That(errorStatus.ErrorCode, Is.EqualTo("StatusErrorCode"));
                Assert.That(errorStatus.Message, Is.EqualTo("StatusErrorMessage"));
                Assert.That(customResponse.Custom, Is.Null);
            }
        }

        [Test]
        public void Returns_custom_Status_and_Description_with_CustomHttpError()
        {
            try
            {
                var json = PredefinedJsonUrl<CustomHttpError>()
                    .AddQueryParam("StatusCode", 406)
                    .AddQueryParam("StatusDescription", "CustomDescription")
                    .GetJsonFromUrl();
                Assert.Fail("Should throw");
            }
            catch (WebException webEx)
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
                Assert.That((int)errorResponse.StatusCode, Is.EqualTo(406));
                Assert.That(errorResponse.StatusDescription, Is.EqualTo("CustomDescription"));
            }
        }

        [Test]
        public void Returns_custom_ResponseStatus_with_DirectHttpError()
        {
            try
            {
                var json = PredefinedJsonUrl<DirectHttpError>().GetJsonFromUrl();
                Assert.Fail("Should throw");
            }
            catch (WebException webEx)
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
                Assert.That((int)errorResponse.StatusCode, Is.EqualTo(500));
                Assert.That(errorResponse.StatusDescription, Is.EqualTo("HeaderErrorCode"));

                var body = errorResponse.GetResponseStream().ReadFully().FromUtf8Bytes();
                var customResponse = body.FromJson<CustomFieldHttpErrorResponse>();
                var errorStatus = customResponse.ResponseStatus;
                Assert.That(errorStatus.ErrorCode, Is.EqualTo("StatusErrorCode"));
                Assert.That(errorStatus.Message, Is.EqualTo("StatusErrorMessage"));
                Assert.That(customResponse.Custom, Is.EqualTo("Not Ignored"));
            }
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
