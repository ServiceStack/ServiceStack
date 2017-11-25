using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class CustomValidationAppHost : AppHostHttpListenerBase
    {
        public CustomValidationAppHost() : base("Custom Error", typeof(CustomValidationAppHost).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new ValidationFeature { ErrorResponseFilter = CustomValidationError });
            container.RegisterValidators(typeof(MyValidator).Assembly);
        }

        public static object CustomValidationError(ValidationResult validationResult, object errorDto)
        {
            var firstError = validationResult.Errors[0];
            var dto = new MyCustomErrorDto { code = firstError.ErrorCode, error = firstError.ErrorMessage };
            return new HttpError(dto, HttpStatusCode.BadRequest, dto.code, dto.error);
        }
    }

    public class MyCustomErrorDto
    {
        public string code { get; set; }
        public string error { get; set; }
    }

    [Route("/customerror")]
    public class CustomError
    {
        public int Age { get; set; }
        public string Company { get; set; }
    }

    public class MyValidator : AbstractValidator<CustomError>
    {
        public MyValidator()
        {
            RuleFor(x => x.Age).GreaterThan(0);
            RuleFor(x => x.Company).NotEmpty();
        }
    }

    [Route("/customrequesterror/{Name}")]
    public class CustomRequestError
    {
        public string Name { get; set; }

        public List<CustomRequestItem> Items { get; set; }
    }

    public class CustomRequestItem
    {
        public string Name { get; set; }
    }

    public class MyRequestValidator : AbstractValidator<CustomRequestError>
    {
        public MyRequestValidator()
        {
            RuleSet(ApplyTo.Post | ApplyTo.Put | ApplyTo.Get, () =>
            {
                var req = base.Request;
                RuleFor(c => c.Name)
                    .Must(x => !base.Request.PathInfo.ContainsAny("-", ".", " "));

                RuleFor(x => x.Items).SetCollectionValidator(new MyRequestItemValidator());
            });
        }
    }

    public class MyRequestItemValidator : AbstractValidator<CustomRequestItem>
    {
        public MyRequestItemValidator()
        {
            RuleFor(x => x.Name)
                .Must(x => !base.Request.QueryString["Items"].ContainsAny("-", ".", " "));
        }
    }

    public class CustomValidationErrorService : Service
    {
        public object Get(CustomError request)
        {
            return request;
        }

        public object Any(CustomRequestError request)
        {
            return request;
        }
    }

    [Route("/errorrequestbinding")]
    public class ErrorRequestBinding : IReturn<ErrorRequestBinding>
    {
        public int Int { get; set; }
        public decimal Decimal { get; set; }
    }

    public class TestRequestBindingService : Service
    {
        public object Any(ErrorRequestBinding errorRequest)
        {
            return errorRequest;
        }
    }

    [TestFixture]
    public class CustomValidationErrorTests
    {
        private CustomValidationAppHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new CustomValidationAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_create_custom_validation_error()
        {
            try
            {
                var response = "{0}/customerror".Fmt(Config.ServiceStackBaseUri).GetJsonFromUrl();
                Assert.Fail("Should throw HTTP Error");
            }
            catch (Exception ex)
            {
                var body = ex.GetResponseBody();
                Assert.That(body, Is.EqualTo("{\"code\":\"GreaterThan\",\"error\":\"'Age' must be greater than '0'.\"}"));
            }
        }

        [Test]
        public void Can_access_Request_in_Validator()
        {
            try
            {
                var response = "{0}/customrequesterror/the.name".Fmt(Config.ServiceStackBaseUri)
                    .GetJsonFromUrl();
                Assert.Fail("Should throw HTTP Error");
            }
            catch (Exception ex)
            {
                var body = ex.GetResponseBody();
                Assert.That(body, Is.EquivalentTo("{\"code\":\"Predicate\",\"error\":\"The specified condition was not met for 'Name'.\"}"));
            }
        }

        [Test]
        public void Can_access_Request_in_item_collection_Validator()
        {
            try
            {
                var response = (Config.ServiceStackBaseUri + "/customrequesterror/thename?items=[{name:item.name}]")
                    .GetJsonFromUrl();
                Assert.Fail("Should throw HTTP Error");
            }
            catch (Exception ex)
            {
                var body = ex.GetResponseBody();
                Assert.That(body, Is.EqualTo("{\"code\":\"Predicate\",\"error\":\"The specified condition was not met for 'Name'.\"}"));
            }
        }

        [Test]
        public void RequestBindingException_QueryString_returns_populated_FieldError()
        {
            var client = new JsonServiceClient(Config.ServiceStackBaseUri);
            try
            {
                var response = client.Get<ErrorRequestBinding>("/errorrequestbinding?Int=string&Decimal=string");
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ResponseStatus.Message,
                    Is.EqualTo("Unable to bind to request 'ErrorRequestBinding'"));

                var intFieldError = ex.GetFieldErrors()[0];
                Assert.That(intFieldError.FieldName, Is.EqualTo("Int"));
                Assert.That(intFieldError.ErrorCode, Is.EqualTo(typeof(SerializationException).Name));
                Assert.That(intFieldError.Message, Is.EqualTo("'string' is an Invalid value for 'Int'"));

                var decimalFieldError = ex.GetFieldErrors()[1];
                Assert.That(decimalFieldError.FieldName, Is.EqualTo("Decimal"));
                Assert.That(decimalFieldError.ErrorCode, Is.EqualTo(typeof(SerializationException).Name));
                Assert.That(decimalFieldError.Message, Is.EqualTo("'string' is an Invalid value for 'Decimal'"));
            }
        }

        [Test]
        public void RequestBindingException_QueryString_predefined_route_returns_populated_FieldError()
        {
            try
            {
                var response = Config.ServiceStackBaseUri.CombineWith("/json/reply/ErrorRequestBinding?Int=string&Decimal=string")
                    .GetJsonFromUrl();
                Assert.Fail("Should throw");
            }
            catch (WebException ex)
            {
                AssertErrorRequestBindingResponse(ex);
            }
        }

        [Test]
        public void RequestBindingException_FormData_returns_populated_FieldError()
        {
            try
            {
                var response = Config.ServiceStackBaseUri.CombineWith("errorrequestbinding")
                    .PostStringToUrl("Int=string&Decimal=string", contentType: MimeTypes.FormUrlEncoded, accept: MimeTypes.Json);
                Assert.Fail("Should throw");
            }
            catch (WebException ex)
            {
                AssertErrorRequestBindingResponse(ex);
            }
        }

        [Test]
        public void RequestBindingException_FormData_predefined_route_returns_populated_FieldError()
        {
            try
            {
                var response = Config.ServiceStackBaseUri.CombineWith("/json/reply/ErrorRequestBinding")
                    .PostStringToUrl("Int=string&Decimal=string", contentType: MimeTypes.FormUrlEncoded, accept: MimeTypes.Json);
                Assert.Fail("Should throw");
            }
            catch (WebException ex)
            {
                AssertErrorRequestBindingResponse(ex);
            }
        }

        private static void AssertErrorRequestBindingResponse(WebException ex)
        {
            var responseBody = ex.GetResponseBody();
            var status = responseBody.FromJson<ErrorResponse>().ResponseStatus;

            Assert.That(status.Message,
                Is.EqualTo("Unable to bind to request 'ErrorRequestBinding'"));

            var fieldError = status.Errors[0];
            Assert.That(fieldError.FieldName, Is.EqualTo("Int"));
            Assert.That(fieldError.ErrorCode, Is.EqualTo(typeof(SerializationException).Name));
            Assert.That(fieldError.Message, Is.EqualTo("'string' is an Invalid value for 'Int'"));

            var fieldError2 = status.Errors[1];
            Assert.That(fieldError2.FieldName, Is.EqualTo("Decimal"));
            Assert.That(fieldError2.ErrorCode, Is.EqualTo(typeof(SerializationException).Name));
            Assert.That(fieldError2.Message, Is.EqualTo("'string' is an Invalid value for 'Decimal'"));
        }
    }

    public static class WebRequestUtils
    {
        public static string GetResponseBody(this Exception ex)
        {
            var webEx = ex as WebException;
            if (webEx == null || webEx.Status != WebExceptionStatus.ProtocolError) return null;

            var errorResponse = ((HttpWebResponse)webEx.Response);
            using (var reader = new StreamReader(errorResponse.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}