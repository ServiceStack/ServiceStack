using System;
using System.Net;
using ServiceStack.FluentValidation;
using NUnit.Framework;
using System.Collections;
using Funq;
using ServiceStack.Validation;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/uservalidation")]
    [Route("/uservalidation/{Id}")]
    public class UserValidation
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public interface IAddressValidator
    {
        bool ValidAddress(string address);
    }

    public class UserValidator : AbstractValidator<UserValidation>, IRequiresRequest
    {
        public IAddressValidator AddressValidator { get; set; }
        public IRequest Request { get; set; }

        public UserValidator()
        {
            RuleFor(x => x.FirstName).Must(f =>
            {
                if (Request == null)
                    Assert.Fail();

                return true;
            });
            RuleFor(x => x.LastName).NotEmpty().WithErrorCode("ShouldNotBeEmpty");
            RuleSet(ApplyTo.Post | ApplyTo.Put, () =>
            {
                RuleFor(x => x.FirstName).NotEmpty().WithMessage("Please specify a first name");
            });
        }
    }

    //Not matching the naming convention ([Request DTO Name] + "Response")
    public class OperationResponse
    {
        public UserValidation Result { get; set; }
    }

    public class UserValidationService : Service
    {
        public object Get(UserValidation request)
        {
            return new OperationResponse { Result = request };
        }
    }

    [TestFixture]
    public class UserServiceValidationTests
    {
        private const string ListeningOn = "http://localhost:1337/";

        public class UserAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public UserAppHostHttpListener()
                : base("Validation Tests", typeof(UserValidationService).Assembly) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new ValidationFeature());
                container.RegisterValidators(typeof(UserValidator).Assembly);
            }
        }

        static UserAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new UserAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        private static string ExpectedErrorCode = "ShouldNotBeEmpty";

        protected static IServiceClient UnitTestServiceClient()
        {
            return new DirectServiceClient(appHost.ServiceController);
        }

        public static IEnumerable ServiceClients
        {
            get
            {
                //Seriously retarded workaround for some devs idea who thought this should
                //be run for all test fixtures, not just this one.

                return new Func<IServiceClient>[] {
                    () => UnitTestServiceClient(),
                    () => new JsonServiceClient(ListeningOn),
                    () => new JsonHttpClient(ListeningOn),
                    () => new JsvServiceClient(ListeningOn),
                    () => new XmlServiceClient(ListeningOn),
                };
            }
        }

        [Test, TestCaseSource(typeof(UserServiceValidationTests), "ServiceClients")]
        public void Get_empty_request_throws_validation_exception(Func<IServiceClient> factory)
        {
            try
            {
                var client = (IRestClient)factory();
                var response = client.Get<OperationResponse>("UserValidation");
                Assert.Fail("Should throw Validation Exception");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(ex.StatusDescription, Is.EqualTo(ExpectedErrorCode));
            }
        }

        public static IEnumerable RestClients
        {
            get
            {
                //Seriously retarded workaround for some devs idea who thought this should
                //be run for all test fixtures, not just this one.

                return new Func<IServiceClient>[] {
                    () => new JsonServiceClient(ListeningOn),
                    () => new JsonHttpClient(ListeningOn),
                    () => new JsvServiceClient(ListeningOn),
                    () => new XmlServiceClient(ListeningOn),
                };
            }
        }

        [Test, TestCaseSource(typeof(UserServiceValidationTests), "RestClients")]
        public void Throws_validation_exception_even_if_AlwaysSendBasicAuthHeader_is_false(Func<IServiceClient> factory)
        {
            try
            {
                var client = factory();
                var serviceClient = client as ServiceClientBase;
                if (serviceClient != null)
                    serviceClient.AlwaysSendBasicAuthHeader = false;
                var httpClient = client as JsonHttpClient;
                if (httpClient != null)
                    httpClient.AlwaysSendBasicAuthHeader = false;

                var response = client.Get<OperationResponse>("UserValidation");
                Assert.Fail("Should throw Validation Exception");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(ex.StatusDescription, Is.EqualTo(ExpectedErrorCode));
            }
        }
    }
}
