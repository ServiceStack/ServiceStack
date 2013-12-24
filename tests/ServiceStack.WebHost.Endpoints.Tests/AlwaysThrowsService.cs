using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [DataContract]
    public class AlwaysThrows
    {
        [DataMember]
        public int? StatusCode { get; set; }
        [DataMember]
        public string Value { get; set; }
    }

    [Route("/throwslist/{StatusCode}/{Value}")]
    [DataContract]
    public class AlwaysThrowsList
    {
        [DataMember]
        public int? StatusCode { get; set; }
        [DataMember]
        public string Value { get; set; }
    }

    [Route("/throwsvalidation")]
    [DataContract]
    public class AlwaysThrowsValidation
    {
        [DataMember]
        public string Value { get; set; }
    }

    public class AlwaysThrowsValidator : AbstractValidator<AlwaysThrowsValidation>
    {
        public AlwaysThrowsValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }

    [DataContract]
	public class AlwaysThrowsResponse : IHasResponseStatus
	{
		public AlwaysThrowsResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public string Result { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class AlwaysThrowsService : Service
	{
        public object Any(AlwaysThrows request)
        {
            if (request.StatusCode.HasValue)
            {
                throw new HttpError(
                    request.StatusCode.Value,
                    typeof(NotImplementedException).Name,
                    GetErrorMessage(request.Value));
            }

            throw new NotImplementedException(GetErrorMessage(request.Value));
        }

        public List<AlwaysThrows> Any(AlwaysThrowsList request)
        {
            Any(request.ConvertTo<AlwaysThrows>());

            return new List<AlwaysThrows>();
        }

        public List<AlwaysThrows> Any(AlwaysThrowsValidation request)
        {
            return new List<AlwaysThrows>();
        }

        public static string GetErrorMessage(string value)
		{
			return value + " is not implemented";
		}
	}

    public class AlwaysThrowsAppHost : AppHostHttpListenerBase
    {
        public AlwaysThrowsAppHost() 
            : base("Always Throws Service", typeof(AlwaysThrowsService).Assembly) {}

        public override void Configure(Container container)
        {
            Plugins.Add(new ValidationFeature());

            container.RegisterValidators(typeof(AlwaysThrowsValidator).Assembly);
        }
    }


    /// <summary>
    /// This base class executes all Web Services ignorant of the endpoints its hosted on.
    /// The same tests below are re-used by the Unit and Integration TestFixture's declared below
    /// </summary>
    [TestFixture]
    public abstract class WebServicesTests
        //: TestBase
    {
        public const string ListeningOn = "http://localhost:82/";
        private const string TestString = "ServiceStack";

        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AlwaysThrowsAppHost()
                .Init()
                .Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        protected abstract IServiceClient CreateNewServiceClient();

        [Test]
        public void Can_Handle_Exception_from_AlwaysThrowService()
        {
            var client = CreateNewServiceClient();
            try
            {
                var response = client.Send<AlwaysThrowsResponse>(
                    new AlwaysThrows { Value = TestString });

                response.PrintDump();
                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                var response = (AlwaysThrowsResponse)webEx.ResponseDto;
                var expectedError = AlwaysThrowsService.GetErrorMessage(TestString);
                Assert.That(response.ResponseStatus.ErrorCode,
                    Is.EqualTo(typeof(NotImplementedException).Name));
                Assert.That(response.ResponseStatus.Message,
                    Is.EqualTo(expectedError));
            }
        }

        [Test]
        public void Can_Handle_Exception_from_AlwaysThrowsList_with_GET_route()
        {
            var client = CreateNewServiceClient();
            if (client is WcfServiceClient) return;
            try
            {
                var response = client.Get<List<AlwaysThrows>>("/throwslist/404/{0}".Fmt(TestString));

                response.PrintDump();
                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(404));

                var response = (ErrorResponse)webEx.ResponseDto;
                var expectedError = AlwaysThrowsService.GetErrorMessage(TestString);
                Assert.That(response.ResponseStatus.ErrorCode,
                    Is.EqualTo(typeof(NotImplementedException).Name));
                Assert.That(response.ResponseStatus.Message,
                    Is.EqualTo(expectedError));
            }
        }

        [Test]
        public void Can_Handle_Exception_from_AlwaysThrowsValidation()
        {
            var client = CreateNewServiceClient();
            if (client is WcfServiceClient) return;
            try
            {
                var response = client.Send<List<AlwaysThrows>>(
                    new AlwaysThrowsValidation());

                response.PrintDump();
                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                var response = (ErrorResponse)webEx.ResponseDto;
                var status = response.ResponseStatus;
                Assert.That(status.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Message, Is.EqualTo("'Value' should not be empty."));
                Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Errors[0].FieldName, Is.EqualTo("Value"));
                Assert.That(status.Errors[0].Message, Is.EqualTo("'Value' should not be empty."));
            }
        }
    }

    public class XmlIntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new XmlServiceClient(ListeningOn);
        }
    }

    public class JsonIntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new JsonServiceClient(ListeningOn);
        }
    }

    public class JsvIntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new JsvServiceClient(ListeningOn);
        }
    }

    public class Soap11IntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new Soap11ServiceClient(ListeningOn);
        }
    }

    public class Soap12IntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new Soap12ServiceClient(ListeningOn);
        }
    }
}