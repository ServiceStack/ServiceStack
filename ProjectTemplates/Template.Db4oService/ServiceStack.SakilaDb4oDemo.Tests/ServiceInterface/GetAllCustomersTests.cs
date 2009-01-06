using Moq;
using NUnit.Framework;
using Sakila.DomainModel;
using Sakila.ServiceModel.Version100.Operations;
using ServiceStack.DataAccess;
using ServiceStack.SakilaDb4oDemo.ServiceInterface.Version100;
using ServiceStack.SakilaDb4oDemo.Tests.Support;

namespace ServiceStack.SakilaDb4oDemo.Tests.ServiceInterface
{
	[TestFixture]
	public class GetAllCustomersTests : TestBase
	{
		
		[Test]
		public void GetAllCustomersTest()
		{
			var provider = new Mock<IPersistenceProvider>();
			provider.Expect(x => x.GetAll<Customer>());
			RegisterPersistenceProvider(provider.Object);

			var request = new GetAllCustomers();
			var operationContext = CreateOperationContext(request);
			
			var port = new GetAllCustomersHandler();
			port.Execute(operationContext);
			
			provider.Verify();
		}

	}
}