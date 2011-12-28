using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Funq;
using NUnit.Framework;
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
	[RestService("/customers")]
	[RestService("/customers/{Id}")]
	public class Customers
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Company { get; set; }
		public decimal Discount { get; set; }
		public string Address { get; set; }
		public string Postcode { get; set; }
		public bool HasDiscount { get; set; }
	}

	public class CustomersValidator : AbstractValidator<Customers>
	{
		public CustomersValidator()
		{
			RuleSet(ApplyTo.Post | ApplyTo.Put, () => {
				RuleFor(x => x.LastName).NotEmpty().WithErrorCode("ShouldNotBeEmpty");
				RuleFor(x => x.FirstName).NotEmpty().WithMessage("Please specify a first name");
				RuleFor(x => x.Company).NotNull();
				RuleFor(x => x.Discount).NotEqual(0).When(x => x.HasDiscount);
				RuleFor(x => x.Address).Length(20, 250);
				RuleFor(x => x.Postcode).Must(BeAValidPostcode).WithMessage("Please specify a valid postcode");
			});
		}

		static readonly Regex UsPostCodeRegEx = new Regex(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);

		private bool BeAValidPostcode(string postcode)
		{
			return UsPostCodeRegEx.IsMatch(postcode);
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
				ValidationFeature.Init(this);
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
		}

		protected IServiceClient CreateNewServiceClient()
		{
			EndpointHandlerBase.ServiceManager = new ServiceManager(true, typeof(SecureService).Assembly);
			return new DirectServiceClient(EndpointHandlerBase.ServiceManager);
		}

		[Test]
		public void UnitTest_Post_empty_request_throws_validation_exception()
		{
			try
			{
				var client = CreateNewServiceClient();
				var response = client.Send<Customers>(new Customers());
				Assert.Fail("Should throw Validation Exception");
			}
			catch (WebServiceException ex)
			{
				throw ex;
			}
		}

		[Test]
		public void Json_Post_empty_request_throws_validation_exception()
		{
			try
			{
				var client = new JsonServiceClient(ListeningOn);
				var response = client.Send<Customers>(new Customers());
				Assert.Fail("Should throw Validation Exception");
			}
			catch (WebServiceException ex)
			{
				throw ex;
			}
		}
	}
}