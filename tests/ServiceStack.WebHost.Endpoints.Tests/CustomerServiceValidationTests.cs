using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Funq;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.FluentValidation;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceInterface.Validation;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Tests;
using ServiceStack.WebHost.Endpoints.Tests.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[Route("/customers")]
	[Route("/customers/{Id}")]
	public class Customers
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

	public class CustomersValidator : AbstractValidator<Customers>
	{
		public IAddressValidator AddressValidator { get; set; }

		public CustomersValidator()
		{
			RuleFor(x => x.Id).NotEqual(default(int));

			RuleSet(ApplyTo.Post | ApplyTo.Put, () => {
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

	public class CustomersResponse
	{
		public Customers Result { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	public class CustomerService : RestServiceBase<Customers>
	{
		public override object OnGet(Customers request)
		{
			return new CustomersResponse { Result = request };
		}

		public override object OnPost(Customers request)
		{
			return new CustomersResponse { Result = request };
		}

		public override object OnPut(Customers request)
		{
			return new CustomersResponse { Result = request };
		}

		public override object OnDelete(Customers request)
		{
			return new CustomersResponse { Result = request };
		}
	}

	[TestFixture]
	public class CustomerServiceValidationTests
	{
		private const string ListeningOn = "http://localhost:82/";

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

		ValidationAppHostHttpListener appHost;

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
		    EndpointHandlerBase.ServiceManager = null;
		}

		private static List<ResponseError> GetValidationFieldErrors(string httpMethod, Customers request)
		{
			var validator = (IValidator)new CustomersValidator {
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

		Customers validRequest;

		[SetUp]
		public void SetUp()
		{
			validRequest = new Customers {
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
            var old = appHost.RequestFilters.Count; 
            appHost.LoadPlugin(new ValidationFeature());
            Assert.That(old, Is.EqualTo(appHost.RequestFilters.Count));
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
			var request = new Customers();
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
			var request = new Customers();
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
			var request = new Customers();
			var errorFields = GetValidationFieldErrors(HttpMethods.Get, request);

			Assert.That(errorFields.Count, Is.EqualTo(1));
			Assert.That(errorFields[0].ErrorCode, Is.EqualTo("NotEqual"));
			Assert.That(errorFields[0].FieldName, Is.EqualTo("Id"));
		}

		[Test]
		public void Validates_empty_request_on_Delete()
		{
			var request = new Customers();
			var errorFields = GetValidationFieldErrors(HttpMethods.Delete, request);

			Assert.That(errorFields.Count, Is.EqualTo(1));
			Assert.That(errorFields[0].ErrorCode, Is.EqualTo("NotEqual"));
			Assert.That(errorFields[0].FieldName, Is.EqualTo("Id"));
		}

		protected static IServiceClient UnitTestServiceClient()
		{
			EndpointHandlerBase.ServiceManager = new ServiceManager(true, typeof(SecureService).Assembly);
			return new DirectServiceClient(EndpointHandlerBase.ServiceManager);
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
				var response = client.Send<CustomersResponse>(new Customers());
				Assert.Fail("Should throw Validation Exception");
			}
			catch (WebServiceException ex)
			{
				var response = (CustomersResponse)ex.ResponseDto;

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
				var response = client.Get<CustomersResponse>("Customers");
				Assert.Fail("Should throw Validation Exception");
			}
			catch (WebServiceException ex)
			{
				var response = (CustomersResponse)ex.ResponseDto;

				var errorFields = response.ResponseStatus.Errors;
				Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
				Assert.That(errorFields.Count, Is.EqualTo(1));
				Assert.That(errorFields[0].ErrorCode, Is.EqualTo("NotEqual"));
				Assert.That(errorFields[0].FieldName, Is.EqualTo("Id"));
			}
		}

		[Test, TestCaseSource(typeof(CustomerServiceValidationTests), "ServiceClients")]
		public void Post_ValidRequest_succeeds(Func<IServiceClient> factory)
		{
			var client = factory();
			var response = client.Send<CustomersResponse>(validRequest);
			Assert.That(response.ResponseStatus, Is.Null);
		}

	}
}