using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Sakila.ServiceModel.Version100.Operations.SakilaDb4oService;
using Sakila.ServiceModel.Version100.Types;
using ServiceStack.SakilaDb4o.Tests.Integration.Support;

namespace ServiceStack.SakilaDb4o.Tests.Integration.ServiceInterface.Version100
{
	[TestFixture]
	public class GetCustomersPortTests : IntegrationTestBase
	{
		[Test]
		public void Get_existing_customers()
		{
			var requestDto = new GetCustomers { CustomerIds = new ArrayOfIntId(new[] { (int)base.CustomerId }) };
			var responseDto = (GetCustomersResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.ResponseStatus.ErrorCode, Is.Null);
			Assert.That(responseDto.Customers.Count, Is.EqualTo(1));
			Assert.That(responseDto.Customers[0].Id, Is.EqualTo(base.CustomerId));
		}
	}
}