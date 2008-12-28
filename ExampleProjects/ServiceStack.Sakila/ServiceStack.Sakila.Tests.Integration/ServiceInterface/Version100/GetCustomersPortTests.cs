using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Sakila.DomainModel;
using Sakila.ServiceModel.Version100.Operations.SakilaService;
using Sakila.ServiceModel.Version100.Types;
using ServiceStack.Sakila.Tests.Integration.Support;

namespace ServiceStack.Sakila.Tests.Integration.ServiceInterface.Version100
{
	[TestFixture]
	public class GetCustomersPortTests : BaseIntegrationTest
	{
		[Test]
		public void Get_users_without_token_fails()
		{
			var existingCustomer = this.Customers[0];
			var requestDto = new GetCustomers { CustomerIds = new ArrayOfIntId(new[] { (int)existingCustomer.Id }) };
			var responseDto = (GetCustomersResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.EqualTo(ErrorCodes.InvalidOrExpiredSession.ToString()));
		}

		[Test]
		public void Get_users_with_token_getting_their_own_info()
		{
			var existingCustomer = this.Customers[0];

			var requestDto = new GetCustomers {
				CustomerIds = new ArrayOfIntId(new[] { (int)existingCustomer.Id }),
			};
			var responseDto = (GetCustomersResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(responseDto.Customers.Count, Is.EqualTo(1));
			Assert.That(responseDto.Customers[0].Id, Is.EqualTo(existingCustomer.Id));
		}

		/// <summary>
		/// Authenticated requests such as GetCustomers will only return their own results.
		/// Requesting another users ids will return an empty result set.
		/// </summary>
		[Test]
		public void Get_users_with_token_getting_their_someone_elses_info()
		{
			var existingCustomer = this.Customers[0];
			var anotherCustomer = this.Customers[1];

			var requestDto = new GetCustomers {
				CustomerIds = new ArrayOfIntId(new[] { (int)anotherCustomer.Id }),
			};
			var responseDto = (GetCustomersResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(responseDto.Customers.Count, Is.EqualTo(0));
		}

		/// <summary>
		/// Authenticated requests such as GetCustomers will only return their own results.
		/// Asking for multiple users ids will only return theirs.
		/// </summary>
		[Test]
		public void Get_users_with_token_getting_a_lot_of_users_info_only_returns_theirs()
		{
			var existingCustomer = this.Customers[0];
			var allCustomerIds = this.Customers.ConvertAll(x => (int)x.Id);

			var requestDto = new GetCustomers {
				CustomerIds = new ArrayOfIntId(allCustomerIds),
			};
			var responseDto = (GetCustomersResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(responseDto.Customers.Count, Is.EqualTo(1));
		}
	}
}