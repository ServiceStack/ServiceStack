using System;
using System.Net;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.IntegrationTests.Tests
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
            return new HttpError(System.Net.HttpStatusCode.BadRequest, "CanNotExecute", "Failed to execute!");
        }

        public object Post(User request)
        {
            throw new HttpError(System.Net.HttpStatusCode.BadRequest, "CanNotExecute", "Failed to execute!");
        }

        public object Delete(User request)
        {
            throw new HttpError(System.Net.HttpStatusCode.Forbidden, "CanNotExecute", "Failed to execute!");
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

    [TestFixture]
    public class ExceptionHandlingTests
    {
        private const string ListeningOn = Config.ServiceStackBaseUri + "/";

        protected static IRestClient[] ServiceClients = 
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
                //IIS Express/WebDev server doesn't pass-thru custom HTTP StatusDescriptions
                //Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
                Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
                //Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
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
                //IIS Express/WebDev server doesn't pass-thru custom HTTP StatusDescriptions
                //Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
                Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
                //Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
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
                //IIS Express/WebDev server doesn't pass-thru custom HTTP StatusDescriptions
                //Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
                Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.Forbidden));
                //Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
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
				json.PrintDump();
                Assert.Fail("Should throw");
            }
            catch (WebException webEx)
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
                var body = errorResponse.GetResponseStream().ReadFully().FromUtf8Bytes();
                Assert.That(body, Is.StringStarting(
                    "{\"responseStatus\":{\"errorCode\":\"CustomException\",\"message\":\"User Defined Error\"")); //inc stacktrace
            }
        }

        [Test]
        public void Returns_empty_dto_when_NoResponseStatus()
        {
            try
            {
                var json = PredefinedJsonUrl<ExceptionNoResponseStatus>().GetJsonFromUrl();
				json.PrintDump();
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
				json.PrintDump();
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
                //IIS doesn't allow overriding of StatusDescription
                //Assert.That(errorResponse.StatusDescription, Is.EqualTo("HeaderErrorCode"));

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
                //IIS doesn't allow overriding of StatusDescription
                //Assert.That(errorResponse.StatusDescription, Is.EqualTo("CustomDescription"));
            }
        }
    }
}
