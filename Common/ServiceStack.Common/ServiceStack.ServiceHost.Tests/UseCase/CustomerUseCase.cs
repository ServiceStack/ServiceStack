using System.Data;
using Funq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceHost.Tests.TypeFactory;
using ServiceStack.ServiceHost.Tests.UseCase.Operations;
using ServiceStack.ServiceHost.Tests.UseCase.Services;

namespace ServiceStack.ServiceHost.Tests.UseCase
{
	[TestFixture]
	public class CustomerUseCase
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			OrmLiteExtensions.DialectProvider = new SqliteOrmLiteDialectProvider();
		}

		[Test]
		public void Using_Funq()
		{
			var serviceController = new ServiceController();

			using (var container = new Container())
			{
				RegisterFuncServices(container, serviceController);

				StoreCustomers(serviceController);

				var response = serviceController.Execute(new GetCustomer { CustomerId = 2 });

				Assert.That(response as GetCustomerResponse, Is.Not.Null);

				var customer = ((GetCustomerResponse)response).Customer;
				Assert.That(customer.FirstName, Is.EqualTo("Second"));
			}
		}

		private static void StoreCustomers(ServiceController serviceController)
		{
			var storeCustomers = new StoreCustomers {
				Customers = {
	            	new Customer { Id = 1, FirstName = "First", LastName = "Customer" },
	            	new Customer { Id = 2, FirstName = "Second", LastName = "Customer" },
	            	new Customer { Id = 3, FirstName = "Third", LastName = "Customer" },
	            }
			};

			serviceController.Execute(storeCustomers);
		}

		private static void RegisterFuncServices(Container container, ServiceController serviceController)
		{
			container.Register(c => ":memory:".OpenDbConnection())
				.ReusedWithin(ReuseScope.Container);

			container.Register(c => new StoreCustomersService(c.Resolve<IDbConnection>()));
			container.Register(c => new GetCustomerService(c.Resolve<IDbConnection>()));

			var funcTypeFactory = new FuncTypeFactory(container);
			serviceController.Register(typeof(StoreCustomers), typeof(StoreCustomersService), funcTypeFactory);
			serviceController.Register(typeof(GetCustomer), typeof(GetCustomerService), funcTypeFactory);
		}
	}
}