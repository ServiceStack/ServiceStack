using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Sakila.DomainModel;
using Sakila.ServiceModel.Version100.Operations.SakilaNHibernateService;
using ServiceStack.DataAccess;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface;
using ServiceStack.SakilaNHibernate.ServiceInterface.Version100;
using ServiceStack.SakilaNHibernate.Tests.Support;

namespace ServiceStack.SakilaNHibernate.Tests.ServiceInterface
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

			var facade = new Mock<ISakilaNHibernateServiceFacade>();
			var customers = new[] { new Customer { Id = 1 } }.ToList();
			facade.Expect(x => x.GetAllCustomers()).Returns(customers);

			var request = new GetAllCustomers();
			var operationContext = CreateOperationContext(request, facade.Object);

			var port = new GetAllCustomersHandler();
			var response = (GetAllCustomersResponse)port.Execute(operationContext);

			Assert.That(response.Customers.Count, Is.EqualTo(customers.Count));
			Assert.That(response.Customers[0].Id, Is.EqualTo(customers[0].Id));
			provider.Verify();
		}

	}
}