using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
    [TestFixture]
    public class ErrorRestTests : IntegrationTestBase
    {
        [Test]
        public void ReproduceErrorTest()
        {
            var restClient = new JsonServiceClient(BaseUrl);

            var errorList = restClient.Get<ErrorCollectionResponse>("error");
            Assert.That(errorList.Result.Count, Is.EqualTo(1));

            var error = restClient.Post<ErrorResponse>("error", new Error { Id = "Test" });
            Assert.That(error, !Is.Null);
        }

        [Test]
        public void UseSameRestClientError()
        {
            var restClient = new JsonServiceClient(BaseUrl);
            var errorList = restClient.Get<ErrorCollectionResponse>("error");
            Assert.That(errorList.Result.Count, Is.EqualTo(1));

            var error = restClient.Get<ErrorResponse>("error/Test");
            Assert.That(error, !Is.Null);
        }

        [Test]
        public void Handles_error_from_Filter()
        {
            try
            {
                var client = new JsonServiceClient(BaseUrl);
                client.Post(new ActionError { Id = "ActionError" });
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(500));
                Assert.That(ex.StatusDescription, Is.EqualTo("NullReferenceException"));
                Assert.That(ex.Message, Is.EqualTo("NullReferenceException"));
            }
        }

        [Test]
        public void Handles_error_from_Filter_async()
        {
            try
            {
                var client = new JsonServiceClient(BaseUrl);
                client.PostAsync(new ActionError { Id = "ActionError" }).Wait();
            }
            catch (AggregateException aex)
            {
                var ex = (WebServiceException)aex.UnwrapIfSingleException();
                Assert.That(ex.StatusCode, Is.EqualTo(500));
                Assert.That(ex.StatusDescription, Is.EqualTo("NullReferenceException"));
                Assert.That(ex.Message, Is.EqualTo("NullReferenceException"));
            }
        }

        [Test]
        public void Does_handle_304_NotModified_Response()
        {
            var client = new JsonServiceClient(BaseUrl);
            try
            {
                var response = client.Get(new EchoCustomResponse
                {
                    StatusCode = (int)HttpStatusCode.NotModified,
                    StatusDescription = "NotModified",
                    Body = "NOT MODIFIED"
                });

                Assert.Fail("304 Throws");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("NotModified"));
            }
        }

        [Test]
        public void Does_handle_304_NotModified_Response_JsonHttpClient()
        {
            var client = new JsonHttpClient(BaseUrl);
            try
            {
                var response = client.Get(new EchoCustomResponse
                {
                    StatusCode = (int)HttpStatusCode.NotModified,
                    StatusDescription = "NotModified",
                    Body = "NOT MODIFIED"
                });

                Assert.Fail("304 Throws");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo("NotModified"));
            }
        }

        [Test]
        public void Does_handle_304_NotModified_Response_HttpUtils()
        {
            var url = BaseUrl.CombineWith("/customresponse/304?StatusDescription=NotModified&Body=NOT+MODIFIED");
            try
            {
                var response = url.GetStringFromUrl();

                Assert.Fail("304 Throws");
            }
            catch (WebException ex)
            {
                Assert.That(ex.GetStatus().Value, Is.EqualTo(HttpStatusCode.NotModified));
            }
        }
    }

    [Route("/error")]
    [Route("/error/{Id}")]
    public class Error
    {
        public Error()
        {
        }

        public string Id { get; set; }
        public Error Inner { get; set; }
    }

    [Route("/actionerror")]
    public class ActionError : IReturn<ActionError>
    {
        public string Id { get; set; }
    }

    public class ActionErrorFilter : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            throw new NullReferenceException();
        }
    }

    public class ErrorService : Service
    {
        public object Get(Error request)
        {
            if (request != null && !string.IsNullOrEmpty(request.Id))
                return new ErrorResponse(new Error { Id = "Test" });

            return new ErrorCollectionResponse(new List<Error> { new Error { Id = "TestCollection" } });
        }

        public object Post(Error request)
        {
            return new ErrorResponse(request);
        }

        [ActionErrorFilter]
        public object Any(ActionError request)
        {
            return new ActionError();
        }

        public string Any(EchoCustomResponse request)
        {
            base.Response.StatusCode = request.StatusCode;
            base.Response.StatusDescription = request.StatusDescription;
            base.Response.ContentType = request.ContentType ?? MimeTypes.PlainText;

            if (request.Body != null)
                base.Response.Write(request.Body);

            base.Response.EndRequest(skipHeaders:true);

            return request.Body;
        }
    }

    public class ErrorResponse : IHasResponseStatus
    {
        public ErrorResponse(Error result)
        {
            Result = result;
            ResponseStatus = new ResponseStatus();
        }

        public Error Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class ErrorCollectionResponse : IHasResponseStatus
    {
        public ErrorCollectionResponse(IList<Error> result)
        {
            Result = new Collection<Error>(result);
            ResponseStatus = new ResponseStatus();
        }

        public Collection<Error> Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }


    [Route("/customresponse/{StatusCode}")]
    public class EchoCustomResponse : IReturn<string>
    {
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public string ContentType { get; set; }
        public string Body { get; set; }
    }
}