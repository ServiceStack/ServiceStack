using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCase
{
    public enum PhoneType
    {
        Home,
        Work,
        Mobile,
    }

    public enum AddressType
    {
        Home,
        Work,
        Other,
    }

    public class Address
    {
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string ZipCode { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class Customer
    {
        public Customer()
        {
            this.PhoneNumbers = new Dictionary<PhoneType, string>();
            this.Addresses = new Dictionary<AddressType, Address>();
        }

        [AutoIncrement] // Creates Auto primary key
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Index(Unique = true)] // Creates Unique Index
        public string Email { get; set; }

        public Dictionary<PhoneType, string> PhoneNumbers { get; set; }

        public Dictionary<AddressType, Address> Addresses { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class Order
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(Customer))] //Creates Foreign Key
        public int CustomerId { get; set; }

        [References(typeof(Employee))] //Creates Foreign Key
        public int EmployeeId { get; set; }

        public Address ShippingAddress { get; set; } //Blobbed (no Address table)

        public DateTime? OrderDate { get; set; }

        public DateTime? RequiredDate { get; set; }

        public DateTime? ShippedDate { get; set; }

        public int? ShipVia { get; set; }

        public decimal Freight { get; set; }

        public decimal Total { get; set; }
    }

    public class OrderDetail
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(Order))] //Creates Foreign Key
        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public decimal UnitPrice { get; set; }

        public short Quantity { get; set; }

        public decimal Discount { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal UnitPrice { get; set; }
    }

    [TestFixtureOrmLite]
    public class CustomerOrdersUseCase : OrmLiteProvidersTestBase
    {
        public CustomerOrdersUseCase(DialectContext context) : base(context) {}

        //Stand-alone class, No other configs, nothing but POCOs.
        [Test]
        public void Run()
        {
            //Non-intrusive: All extension methods hang off System.Data.* interfaces
            using (var db = OpenDbConnection())
            {
                //Re-Create all table schemas:
                RecreateTables(db);

                db.Insert(new Employee { Id = 1, Name = "Employee 1" });
                db.Insert(new Employee { Id = 2, Name = "Employee 2" });
                var product1 = new Product { Id = 1, Name = "Product 1", UnitPrice = 10 };
                var product2 = new Product { Id = 2, Name = "Product 2", UnitPrice = 20 };
                db.Save(product1, product2);

                var customer = new Customer {
                    FirstName = "Orm",
                    LastName = "Lite",
                    Email = "ormlite@servicestack.net",
                    PhoneNumbers =
                    {
                        { PhoneType.Home, "555-1234" },
                        { PhoneType.Work, "1-800-1234" },
                        { PhoneType.Mobile, "818-123-4567" },
                    },
                    Addresses =
                    {
                        { AddressType.Work, new Address { Line1 = "1 Street", Country = "US", State = "NY", City = "New York", ZipCode = "10101" } },
                    },
                    CreatedAt = DateTime.UtcNow,
                };

                var customerId = db.Insert(customer, selectIdentity: true); //Get Auto Inserted Id
                customer = db.Single<Customer>(new { customer.Email }); //Query
                Assert.That(customer.Id, Is.EqualTo(customerId));

                //Direct access to System.Data.Transactions:
                using (var trans = db.OpenTransaction(IsolationLevel.ReadCommitted))
                {
                    var order = new Order {
                        CustomerId = customer.Id,
                        EmployeeId = 1,
                        OrderDate = DateTime.UtcNow,
                        Freight = 10.50m,
                        ShippingAddress = new Address { Line1 = "3 Street", Country = "US", State = "NY", City = "New York", ZipCode = "12121" },
                    };
                    db.Save(order); //Inserts 1st time

                    //order.Id populated on Save().

                    var orderDetails = new[] {
                        new OrderDetail {
                            OrderId = order.Id,
                            ProductId = product1.Id,
                            Quantity = 2,
                            UnitPrice = product1.UnitPrice,
                        },
                        new OrderDetail {
                            OrderId = order.Id,
                            ProductId = product2.Id,
                            Quantity = 2,
                            UnitPrice = product2.UnitPrice,
                            Discount = .15m,
                        }
                    };

                    db.Save(orderDetails);

                    order.Total = orderDetails.Sum(x => x.UnitPrice * x.Quantity * x.Discount) + order.Freight;

                    db.Save(order); //Updates 2nd Time

                    trans.Commit();
                }
            }
        }

        public static void RecreateTables(IDbConnection db)
        {
            DropTables(db);

            db.CreateTable<Employee>();
            db.CreateTable<Product>();
            db.CreateTable<Customer>();
            db.CreateTable<Order>();
            db.CreateTable<OrderDetail>();
        }

        public static void DropTables(IDbConnection db)
        {
            db.DropTable<OrderDetail>();
            db.DropTable<Order>();
            db.DropTable<Customer>();
            db.DropTable<Product>();
            db.DropTable<Employee>();
        }
    }

}
