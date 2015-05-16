using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Funq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.WebHost.Endpoints.Tests.Support;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/validcustomers")]
    [Route("/validcustomers/{Id}")]
    public class ValidCustomers
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public decimal Discount { get; set; }
        public string Address { get; set; }
        public string Postcode { get; set; }
        public bool HasDiscount { get; set; }
    }

    public interface IAddressValidator
    {
        bool ValidAddress(string address);
    }

    public class AddressValidator : IAddressValidator
    {
        public bool ValidAddress(string address)
        {
            return address != null
                && address.Length >= 20
                && address.Length <= 250;
        }
    }

    public class CustomersValidator : AbstractValidator<ValidCustomers>
    {
        public IAddressValidator AddressValidator { get; set; }

        public CustomersValidator()
        {
            RuleFor(x => x.Id).NotEqual(default(int));

            RuleSet(ApplyTo.Post | ApplyTo.Put, () =>
            {
                RuleFor(x => x.LastName).NotEmpty().WithErrorCode("ShouldNotBeEmpty");
                RuleFor(x => x.FirstName).NotEmpty().WithMessage("Please specify a first name");
                RuleFor(x => x.Company).NotNull();
                RuleFor(x => x.Discount).NotEqual(0).When(x => x.HasDiscount);
                RuleFor(x => x.Address).Must(x => AddressValidator.ValidAddress(x));
                RuleFor(x => x.Postcode).Must(BeAValidPostcode).WithMessage("Please specify a valid postcode");
            });
        }

        static readonly Regex UsPostCodeRegEx = new Regex(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);

        private bool BeAValidPostcode(string postcode)
        {
            return !string.IsNullOrEmpty(postcode) && UsPostCodeRegEx.IsMatch(postcode);
        }
    }

    public class ValidCustomersResponse
    {
        public ValidCustomers Result { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [DefaultRequest(typeof(ValidCustomers))]
    public class CustomerService : Service
    {
        public object Get(ValidCustomers request)
        {
            return new ValidCustomersResponse { Result = request };
        }

        public object Post(ValidCustomers request)
        {
            return new ValidCustomersResponse { Result = request };
        }

        public object Put(ValidCustomers request)
        {
            return new ValidCustomersResponse { Result = request };
        }

        public object Delete(ValidCustomers request)
        {
            return new ValidCustomersResponse { Result = request };
        }
    }

    [TestFixture]
    public class CustomerServiceValidationTests
    {
        private const string ListeningOn = "http://localhost:1337/";

        public class ValidationAppHostHttpListener
            : AppHostHttpListenerBase
        {
            public ValidationAppHostHttpListener()
                : base("Validation Tests", typeof(CustomerService).Assembly) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new ValidationFeature());
                container.Register<IAddressValidator>(new AddressValidator());
                container.RegisterValidators(typeof(CustomersValidator).Assembly);
            }
        }

        static ValidationAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ValidationAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        private static List<ResponseError> GetValidationFieldErrors(string httpMethod, ValidCustomers request)
        {
            var validator = (IValidator)new CustomersValidator
            {
                AddressValidator = new AddressValidator()
            };

            var validationResult = validator.Validate(
            new ValidationContext(request, null, new MultiRuleSetValidatorSelector(httpMethod)));

            var responseStatus = validationResult.ToErrorResult().ToResponseStatus();

            var errorFields = responseStatus.Errors;
            return errorFields ?? new List<ResponseError>();
        }

        private string[] ExpectedPostErrorFields = new[] {
			"Id",
			"LastName",
			"FirstName",
			"Company",
			"Address",
			"Postcode",
		};

        private string[] ExpectedPostErrorCodes = new[] {
			"NotEqual",
			"ShouldNotBeEmpty",
			"NotEmpty",
			"NotNull",
			"Predicate",
			"Predicate",
		};

        ValidCustomers validRequest;

        [SetUp]
        public void SetUp()
        {
            validRequest = new ValidCustomers
            {
                Id = 1,
                FirstName = "FirstName",
                LastName = "LastName",
                Address = "12345 Address St, New York",
                Company = "Company",
                Discount = 10,
                HasDiscount = true,
                Postcode = "11215",
            };
        }

        [Test]
        public void ValidationFeature_add_request_filter_once()
        {
            var old = appHost.GlobalRequestFilters.Count;
            appHost.LoadPlugin(new ValidationFeature());
            Assert.That(old, Is.EqualTo(appHost.GlobalRequestFilters.Count));
        }

        [Test]
        public void Validates_ValidRequest_request_on_Post()
        {
            var errorFields = GetValidationFieldErrors(HttpMethods.Post, validRequest);
            Assert.That(errorFields.Count, Is.EqualTo(0));
        }

        [Test]
        public void Validates_ValidRequest_request_on_Get()
        {
            var errorFields = GetValidationFieldErrors(HttpMethods.Get, validRequest);
            Assert.That(errorFields.Count, Is.EqualTo(0));
        }

        [Test]
        public void Validates_Conditional_Request_request_on_Post()
        {
            validRequest.Discount = 0;
            validRequest.HasDiscount = true;

            var errorFields = GetValidationFieldErrors(HttpMethods.Post, validRequest);
            Assert.That(errorFields.Count, Is.EqualTo(1));
            Assert.That(errorFields[0].FieldName, Is.EqualTo("Discount"));
        }

        [Test]
        public void Validates_empty_request_on_Post()
        {
            var request = new ValidCustomers();
            var errorFields = GetValidationFieldErrors(HttpMethods.Post, request);

            var fieldNames = errorFields.Select(x => x.FieldName).ToArray();
            var fieldErrorCodes = errorFields.Select(x => x.ErrorCode).ToArray();

            Assert.That(errorFields.Count, Is.EqualTo(ExpectedPostErrorFields.Length));
            Assert.That(fieldNames, Is.EquivalentTo(ExpectedPostErrorFields));
            Assert.That(fieldErrorCodes, Is.EquivalentTo(ExpectedPostErrorCodes));
        }

        [Test]
        public void Validates_empty_request_on_Put()
        {
            var request = new ValidCustomers();
            var errorFields = GetValidationFieldErrors(HttpMethods.Put, request);

            var fieldNames = errorFields.Select(x => x.FieldName).ToArray();
            var fieldErrorCodes = errorFields.Select(x => x.ErrorCode).ToArray();

            Assert.That(errorFields.Count, Is.EqualTo(ExpectedPostErrorFields.Length));
            Assert.That(fieldNames, Is.EquivalentTo(ExpectedPostErrorFields));
            Assert.That(fieldErrorCodes, Is.EquivalentTo(ExpectedPostErrorCodes));
        }

        [Test]
        public void Validates_empty_request_on_Get()
        {
            var request = new ValidCustomers();
            var errorFields = GetValidationFieldErrors(HttpMethods.Get, request);

            Assert.That(errorFields.Count, Is.EqualTo(1));
            Assert.That(errorFields[0].ErrorCode, Is.EqualTo("NotEqual"));
            Assert.That(errorFields[0].FieldName, Is.EqualTo("Id"));
        }

        [Test]
        public void Validates_empty_request_on_Delete()
        {
            var request = new ValidCustomers();
            var errorFields = GetValidationFieldErrors(HttpMethods.Delete, request);

            Assert.That(errorFields.Count, Is.EqualTo(1));
            Assert.That(errorFields[0].ErrorCode, Is.EqualTo("NotEqual"));
            Assert.That(errorFields[0].FieldName, Is.EqualTo("Id"));
        }

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
					() => new JsvServiceClient(ListeningOn),
					() => new XmlServiceClient(ListeningOn),
				};
            }
        }

        [Test, TestCaseSource(typeof(CustomerServiceValidationTests), "ServiceClients")]
        public void Post_empty_request_throws_validation_exception(Func<IServiceClient> factory)
        {
            try
            {
                var client = factory();
                var response = client.Send<ValidCustomersResponse>(new ValidCustomers());
                Assert.Fail("Should throw Validation Exception");
            }
            catch (WebServiceException ex)
            {
                var response = (ValidCustomersResponse)ex.ResponseDto;

                var errorFields = response.ResponseStatus.Errors;
                var fieldNames = errorFields.Select(x => x.FieldName).ToArray();
                var fieldErrorCodes = errorFields.Select(x => x.ErrorCode).ToArray();

                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(errorFields.Count, Is.EqualTo(ExpectedPostErrorFields.Length));
                Assert.That(fieldNames, Is.EquivalentTo(ExpectedPostErrorFields));
                Assert.That(fieldErrorCodes, Is.EquivalentTo(ExpectedPostErrorCodes));
            }
        }

        [Test, TestCaseSource(typeof(CustomerServiceValidationTests), "ServiceClients")]
        public void Get_empty_request_throws_validation_exception(Func<IServiceClient> factory)
        {
            try
            {
                var client = (IRestClient)factory();
                var response = client.Get<ValidCustomersResponse>("ValidCustomers");
                Assert.Fail("Should throw Validation Exception");
            }
            catch (WebServiceException ex)
            {
                var response = (ValidCustomersResponse)ex.ResponseDto;

                var errorFields = response.ResponseStatus.Errors;
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
                Assert.That(ex.StatusDescription, Is.EqualTo("NotEqual"));
                Assert.That(response.ResponseStatus.Message, Is.EqualTo("'Id' should not be equal to '0'."));
                Assert.That(errorFields.Count, Is.EqualTo(1));
                Assert.That(errorFields[0].ErrorCode, Is.EqualTo("NotEqual"));
                Assert.That(errorFields[0].FieldName, Is.EqualTo("Id"));
                Assert.That(errorFields[0].Message, Is.EqualTo("'Id' should not be equal to '0'."));
            }
        }

        [Test, TestCaseSource(typeof(CustomerServiceValidationTests), "ServiceClients")]
        public void Post_ValidRequest_succeeds(Func<IServiceClient> factory)
        {
            var client = factory();
            var response = client.Send<ValidCustomersResponse>(validRequest);
            Assert.That(response.ResponseStatus, Is.Null);
        }

    }
}
