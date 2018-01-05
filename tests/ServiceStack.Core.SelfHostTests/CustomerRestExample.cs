using System.Collections.Generic;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace NewApi.Customers
{
    public class AppHost : AppSelfHostBase
    {
        public AppHost() : base("Customer REST Example", typeof(CustomerService).GetAssembly()) { }

        public override void Configure(Container container)
        {
            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.CreateTableIfNotExists<Customer>();
            }
        }
    }

    [Route("/customers", "GET")]
    public class GetCustomers : IReturn<GetCustomersResponse> { }

    public class GetCustomersResponse
    {
        public List<Customer> Results { get; set; }
    }

    [Route("/customers/{Id}", "GET")]
    public class GetCustomer : IReturn<Customer>
    {
        public int Id { get; set; }
    }

    [Route("/customers", "POST")]
    public class CreateCustomer : IReturn<Customer>
    {
        public string Name { get; set; }
    }

    [Route("/customers/{Id}", "PUT")]
    public class UpdateCustomer : IReturn<Customer>
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [Route("/customers/{Id}", "DELETE")]
    public class DeleteCustomer : IReturnVoid
    {
        public int Id { get; set; }
    }

    public class Customer
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class CustomerService : Service
    {
        public object Get(GetCustomers request)
        {
            return new GetCustomersResponse { Results = Db.Select<Customer>() };
        }

        public object Get(GetCustomer request)
        {
            return Db.SingleById<Customer>(request.Id);
        }

        public object Post(CreateCustomer request)
        {
            var customer = new Customer { Name = request.Name };
            Db.Save(customer);
            return customer;
        }

        public object Put(UpdateCustomer request)
        {
            var customer = Db.SingleById<Customer>(request.Id);
            if (customer == null)
                throw HttpError.NotFound("Customer '{0}' does not exist".Fmt(request.Id));

            customer.Name = request.Name;
            Db.Update(customer);

            return customer;
        }

        public void Delete(DeleteCustomer request)
        {
            Db.DeleteById<Customer>(request.Id);
        }
    }


    [TestFixture]
    public class CustomerRestExample
    {
        const string BaseUri = "http://localhost:1337/";

        ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost()
                .Init()
                .Start(BaseUri);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown() => appHost.Dispose();

        [Test]
        public void Run_Customer_REST_Example()
        {
            var client = new JsonServiceClient(BaseUri);

            //GET /customers
            var all = client.Get(new GetCustomers());
            Assert.That(all.Results.Count, Is.EqualTo(0));

            //POST /customers
            var customer = client.Post(new CreateCustomer { Name = "Foo" });
            Assert.That(customer.Id, Is.EqualTo(1));
            //GET /customer/1
            customer = client.Get(new GetCustomer { Id = customer.Id });
            Assert.That(customer.Name, Is.EqualTo("Foo"));

            //GET /customers
            all = client.Get(new GetCustomers());
            Assert.That(all.Results.Count, Is.EqualTo(1));

            //PUT /customers/1
            customer = client.Put(new UpdateCustomer { Id = customer.Id, Name = "Bar" });
            Assert.That(customer.Name, Is.EqualTo("Bar"));

            //DELETE /customers/1
            client.Delete(new DeleteCustomer { Id = customer.Id });
            //GET /customers
            all = client.Get(new GetCustomers());
            Assert.That(all.Results.Count, Is.EqualTo(0));
        }
    }

}