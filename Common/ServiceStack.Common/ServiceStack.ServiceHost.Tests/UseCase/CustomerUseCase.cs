using System;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using Funq;
using Hiro;
using Hiro.Implementations;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceHost.Tests.Support;
using ServiceStack.ServiceHost.Tests.TypeFactory;
using ServiceStack.ServiceHost.Tests.UseCase.Operations;
using ServiceStack.ServiceHost.Tests.UseCase.Services;

namespace ServiceStack.ServiceHost.Tests.UseCase
{
	[TestFixture]
	public class CustomerUseCase
	{
		private const int Times = 100000;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			OrmLiteExtensions.DialectProvider = new SqliteOrmLiteDialectProvider();
		}

		public const bool UseCache = false;

		[Test]
		public void Perf_All_IOC()
		{
			Hiro_Perf();
			AutoWiredFunq_Perf();
			NativeFunq_Perf();
		}

		[Test]
		public void Hiro_Perf()
		{
			var serviceController = new ServiceController();

			RegisterServices(serviceController, GetHiroTypeFactory());

			StoreAndGetCustomers(serviceController);

			var request = new GetCustomer { CustomerId = 2 };
			Console.WriteLine("Hiro_Perf(): {0}", Measure(() => serviceController.Execute(request), Times));
		}

		[Test]
		public void NativeFunq_Perf()
		{
			var serviceController = new ServiceController();

			RegisterServices(serviceController, GetNativeFunqTypeFactory());

			StoreAndGetCustomers(serviceController);

			var request = new GetCustomer { CustomerId = 2 };
			Console.WriteLine("NativeFunq_Perf(): {0}", Measure(() => serviceController.Execute(request), Times));
		}

		[Test]
		public void AutoWiredFunq_Perf()
		{
			var serviceController = new ServiceController();

			RegisterServices(serviceController, GetAutoWiredFunqTypeFactory());

			StoreAndGetCustomers(serviceController);

			var request = new GetCustomer { CustomerId = 2 };
			Console.WriteLine("AutoWiredFunq_Perf(): {0}", Measure(() => serviceController.Execute(request), Times));
		}

		private static long Measure(Action action, int iterations)
		{
			GC.Collect();
			var watch = Stopwatch.StartNew();

			for (int i = 0; i < iterations; i++)
			{
				action();
			}

			return watch.ElapsedTicks;
		}


		[Test]
		public void Using_Hiro()
		{
			var serviceController = new ServiceController();

			RegisterServices(serviceController, GetHiroTypeFactory());

			StoreAndGetCustomers(serviceController);
		}

		[Test]
		public void Using_NativeFunq()
		{
			var serviceController = new ServiceController();

			RegisterServices(serviceController, GetNativeFunqTypeFactory());

			StoreAndGetCustomers(serviceController);
		}

		[Test]
		public void Using_AutoWiredFunq()
		{
			var serviceController = new ServiceController();

			RegisterServices(serviceController, GetAutoWiredFunqTypeFactory());

			StoreAndGetCustomers(serviceController);
		}

		private static void StoreAndGetCustomers(ServiceController serviceController)
		{
			var storeCustomers = new StoreCustomers {
				Customers = {
	            	new Customer { Id = 1, FirstName = "First", LastName = "Customer" },
	            	new Customer { Id = 2, FirstName = "Second", LastName = "Customer" },
	            }
			};
			serviceController.Execute(storeCustomers);

			storeCustomers = new StoreCustomers {
				Customers = {
					new Customer {Id = 3, FirstName = "Third", LastName = "Customer"},
				}
			};
			serviceController.Execute(storeCustomers);

			var response = serviceController.Execute(new GetCustomer { CustomerId = 2 });

			Assert.That(response as GetCustomerResponse, Is.Not.Null);

			var customer = ((GetCustomerResponse)response).Customer;
			Assert.That(customer.FirstName, Is.EqualTo("Second"));
		}

		private static ITypeFactory GetHiroTypeFactory()
		{
			var map = new DependencyMap();

			map.AddSingletonService(typeof(IDbConnection), typeof(InMemoryDbConnection));
			map.AddSingletonService(typeof(ICacheClient), typeof(MemoryCacheClient));
			map.AddSingletonService(typeof(CustomerUseCaseConfig), typeof(CustomerUseCaseConfig));

			var injector = new PropertyInjectionCall(new TransientType(typeof(GetCustomerService), map));
			map.AddService(new Dependency(typeof(GetCustomerService)), injector);
			map.AddService(typeof(GetCustomerService), typeof(GetCustomerService));

			injector = new PropertyInjectionCall(new TransientType(typeof(StoreCustomersService), map));
			map.AddService(new Dependency(typeof(StoreCustomersService)), injector);
			map.AddService(typeof(StoreCustomersService), typeof(StoreCustomersService));

			var container = map.CreateContainer();

			return new HiroTypeFactory(container);
		}

		private static void RegisterServices(ServiceController serviceController, ITypeFactory typeFactory)
		{
			serviceController.Register(typeof(StoreCustomers), typeof(StoreCustomersService), typeFactory);
			serviceController.Register(typeof(GetCustomer), typeof(GetCustomerService), typeFactory);
		}

		public static ITypeFactory GetNativeFunqTypeFactory()
		{
			var container = GetContainerWithDependencies();

			container.Register(c => new StoreCustomersService(c.Resolve<IDbConnection>()))
				.ReusedWithin(ReuseScope.None);

			container.Register(c =>
					new GetCustomerService(c.Resolve<IDbConnection>(), c.Resolve<CustomerUseCaseConfig>()) {
						CacheClient = c.TryResolve<ICacheClient>()
					}
				)
				.ReusedWithin(ReuseScope.None);

			return new FuncTypeFactory(container);
		}

		public static ITypeFactory GetAutoWiredFunqTypeFactory()
		{
			var container = GetContainerWithDependencies();

			var typeFactory = new ExpressionTypeFunqContainer(container);
			typeFactory.Register(typeof(StoreCustomersService), typeof(GetCustomerService));

			return typeFactory;
		}

		private static Container GetContainerWithDependencies()
		{
			var container = new Container();

			container.Register(c => ":memory:".OpenDbConnection())
				.ReusedWithin(ReuseScope.Container);
			container.Register<ICacheClient>(c => new MemoryCacheClient())
				.ReusedWithin(ReuseScope.Container);
			container.Register(c => new CustomerUseCaseConfig())
				.ReusedWithin(ReuseScope.Container);

			return container;
		}
	}

}
