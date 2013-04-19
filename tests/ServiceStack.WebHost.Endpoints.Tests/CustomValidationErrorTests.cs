using System;
using System.IO;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Validation;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class CustomValidationAppHost : AppHostHttpListenerBase
    {
        public CustomValidationAppHost() : base("Custom Error", typeof(CustomValidationAppHost).Assembly) {}

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

    public class CustomValidationService : ServiceInterface.Service
    {
        public object Get(CustomError request)
        {
            return request;
        }
    }

    [TestFixture]
    public class CustomValidationErrorTests
    {
        private CustomValidationAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new CustomValidationAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
            appHost = null;
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