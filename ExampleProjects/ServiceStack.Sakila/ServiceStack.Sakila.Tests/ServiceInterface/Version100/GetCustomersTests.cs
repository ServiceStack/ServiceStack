using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Sakila.DomainModel;
using Sakila.ServiceModel.Version100.Operations.SakilaService;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.Sakila.Logic.LogicInterface.Requests;
using ServiceStack.Sakila.ServiceInterface.Version100;
using ServiceStack.Sakila.Tests.Support;
using ServiceStack.ServiceInterface;
using DtoTypes = Sakila.ServiceModel.Version100.Types;

namespace ServiceStack.Sakila.Tests.ServiceInterface.Version100
{
	[TestFixture]
	public class GetCustomersTests : BaseAppTestFixture
	{
		public GetCustomersTests() 
			: base(new TestParameters())
		{
		}

		private Mock<ISakilaServiceFacade> MoqFacade { get; set; }

		private CallContext CallContext { get; set; }

		private List<Customer> FacadeResult { get; set; }

		[SetUp]
		public void SetUp()
		{
			this.MoqFacade = new Mock<ISakilaServiceFacade>();
			this.CallContext = base.CreateCallContext(this.MoqFacade.Object, null);
		}

		[TearDown]
		public void TearDown()
		{
			this.CallContext = null;
			this.MoqFacade = null;
		}

		[Test]
		public void GetCustomersExecute()
		{
			// Create request DTO and insert into call context
			this.CallContext.Request.Dto = new GetCustomers { CustomerIds = new DtoTypes.ArrayOfIntId { 1, 2, 3 } };
			
			// Set return value upon successful call to the moq
			List<Customer> returnValue = new List<Customer> { new Customer { Id = 1 }, new Customer { Id = 2 }, new Customer { Id = 3 } };

			// Set facade to expect provided values
			this.MoqFacade.Expect(facade => facade.GetCustomers(It.Is<CustomersRequest>(req => req.CustomerIds.Count == 3 && req.CustomerIds.Contains(1) && req.CustomerIds.Contains(2) && req.CustomerIds.Contains(3))))
				 .Returns(returnValue)
				 .AtMostOnce();

			// Execute port
			var response = (GetCustomersResponse) new GetCustomersPort().Execute(this.CallContext);

			this.MoqFacade.VerifyAll();

			Assert.That(response.Customers.Count == 3);
		}
	}
}