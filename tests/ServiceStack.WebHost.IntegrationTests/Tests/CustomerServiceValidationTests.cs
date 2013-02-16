using System.Collections;
using System.Linq;
using System.Net;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.Text;
using ServiceStack.ServiceClient.Web;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class CustomerServiceValidationTests
	{
		private const string ListeningOn = Config.ServiceStackBaseUri;

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
			"Length",
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

		public static IEnumerable ServiceClients
		{
			get
			{
				return new IServiceClient[] {
					new JsonServiceClient(ListeningOn),
					new JsvServiceClient(ListeningOn),
					new XmlServiceClient(ListeningOn),
				};
			}
		}


		[Test, TestCaseSource(typeof(CustomerServiceValidationTests), "ServiceClients")]
		public void Post_empty_request_throws_validation_exception(IServiceClient client)
		{
			try
			{
				var response = client.Send(new Customers());
				response.PrintDump();
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
		public void Get_empty_request_throws_validation_exception(IRestClient client)
		{
			try
			{
				var response = client.Get(new Customers());
				response.PrintDump();
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
		public void Post_ValidRequest_succeeds(IServiceClient client)
		{
			var response = client.Send(validRequest);
			Assert.That(response.ResponseStatus, Is.Null);
		}

	}

}