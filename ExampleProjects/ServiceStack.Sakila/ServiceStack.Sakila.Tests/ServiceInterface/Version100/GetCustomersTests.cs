using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Sakila.DomainModel;
using Sakila.ServiceModel.Version100.Operations.SakilaService;
using ServiceStack.DataAccess;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.Sakila.Logic.LogicInterface.Requests;
using ServiceStack.Sakila.ServiceInterface.Version100;
using ServiceStack.Sakila.Tests.Support;
using ServiceStack.ServiceInterface;
using DtoTypes = Sakila.ServiceModel.Version100.Types;

namespace ServiceStack.Sakila.Tests.ServiceInterface.Version100
{
	[TestFixture]
	public class GetCustomersTests : TestBase
	{

		[Test]
		public void GetAllCustomersTest()
		{
			var provider = new Mock<IPersistenceProvider>();
			provider.Expect(x => x.GetAll<Customer>());
			RegisterPersistenceProvider(provider.Object);

			var facade = new Mock<ISakilaServiceFacade>();
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