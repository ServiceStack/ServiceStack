using System;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using Funq;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceHost.Tests.Support;
using ServiceStack.ServiceHost.Tests.TypeFactory;
using ServiceStack.ServiceHost.Tests.UseCase.Operations;
using ServiceStack.ServiceHost.Tests.UseCase.Services;

namespace ServiceStack.ServiceHost.Tests.UseCase
{
    [Ignore]
    [TestFixture]
    public class CustomerUseCase
    {
        private const int Times = 100000;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            OrmLite.OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
        }

        public const bool UseCache = false;
        private ServiceController serviceController;

        [SetUp]
        public void OnBeforeEachTest()
        {
            serviceController = new ServiceController(null);
        }

        [Test]
        public void Perf_All_IOC()
        {
            AutoWiredFunq_Perf();
            NativeFunq_Perf();
        }


        [Test]
        public void NativeFunq_Perf()
        {
            RegisterServices(serviceController, GetNativeFunqTypeFactory());

            StoreAndGetCustomers(serviceController);

            var request = new GetCustomer { CustomerId = 2 };
            Console.WriteLine("NativeFunq_Perf(): {0}", Measure(() => serviceController.Execute(request), Times));
        }

        [Test]
        public void AutoWiredFunq_Perf()
        {
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
        public void Using_NativeFunq()
        {
            RegisterServices(serviceController, GetNativeFunqTypeFactory());

            StoreAndGetCustomers(serviceController);
        }

        [Test]
        public void Using_AutoWiredFunq()
        {
            RegisterServices(serviceController, GetAutoWiredFunqTypeFactory());

            StoreAndGetCustomers(serviceController);
        }

        private static void StoreAndGetCustomers(ServiceController serviceController)
        {
            var storeCustomers = new StoreCustomers
            {
                Customers = {
                    new Customer { Id = 1, FirstName = "First", LastName = "Customer" },
                    new Customer { Id = 2, FirstName = "Second", LastName = "Customer" },
                }
            };
            serviceController.Execute(storeCustomers);

            storeCustomers = new StoreCustomers
            {
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

        private static void RegisterServices(ServiceController serviceController, ITypeFactory typeFactory)
        {
            serviceController.RegisterServiceExecutor(typeof(StoreCustomers), typeof(StoreCustomersService), typeFactory);
            serviceController.RegisterServiceExecutor(typeof(GetCustomer), typeof(GetCustomerService), typeFactory);
        }

        public static ITypeFactory GetNativeFunqTypeFactory()
        {
            var container = GetContainerWithDependencies();

            container.Register(c => new StoreCustomersService(c.Resolve<IDbConnection>()))
                .ReusedWithin(ReuseScope.None);

            container.Register(c =>
                    new GetCustomerService(c.Resolve<IDbConnection>(), c.Resolve<CustomerUseCaseConfig>())
                    {
                        CacheClient = c.TryResolve<ICacheClient>()
                    }
                )
                .ReusedWithin(ReuseScope.None);

            return new FuncTypeFactory(container);
        }

        public static ITypeFactory GetAutoWiredFunqTypeFactory()
        {
            var container = GetContainerWithDependencies();

            container.RegisterAutoWiredType(typeof(StoreCustomersService), typeof(GetCustomerService));

            return new ContainerResolveCache(container);
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
