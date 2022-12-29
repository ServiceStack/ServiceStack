using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public class Goal
    {
        [PrimaryKey]
        public long Id { get; set; }

        [Reference]
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        [PrimaryKey]
        public long Id { get; set; }

        [ForeignKey(typeof(Goal))]
        public long AnyGoalId { get; set; }

        [Alias("CorrectGoalId")]
        [ForeignKey(typeof(Goal))]
        public long GoalId { get; set; }
    }

    public class AliasedCustomer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        [Alias("id_home_address")]
        [ForeignKey(typeof(AliasedCustomerAddress))]
        public int? HomeAddressId { get; set; }

        [Alias("id_work_address")]
        [ForeignKey(typeof(AliasedCustomerAddress))]
        public int? WorkAddressId { get; set; }

        [Reference]
        public AliasedCustomerAddress HomeAddress { get; set; }

        [Reference]
        public AliasedCustomerAddress WorkAddress { get; set; }
    }

    public class AliasedCustomerAddress
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int AliasedCustomerId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    
    [TestFixtureOrmLite]
    public class MultiFieldReferenceTests : OrmLiteProvidersTestBase
    {
        public MultiFieldReferenceTests(DialectContext context) : base(context) {}

        [Test]
        public void Does_select_correct_reference_field()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<Item>();
                db.DropTable<Goal>();
                db.CreateTable<Goal>();
                db.CreateTable<Item>();

                var goal = new Goal { Id = 1 };
                db.Save(goal, references: true);

                var goalWithItems = new Goal
                {
                    Id = 2,
                    Items = new List<Item>
                    {
                        new Item { Id = 10, AnyGoalId = 1 },
                        new Item { Id = 11, AnyGoalId = 1 },
                    }
                };

                db.Save(goalWithItems, references: true);

                Assert.That(goalWithItems.Items[0].GoalId, Is.EqualTo(goalWithItems.Id));
                Assert.That(goalWithItems.Items[1].GoalId, Is.EqualTo(goalWithItems.Id));

                var dbGoals = db.LoadSelect<Goal>(x => x.Id == goalWithItems.Id).First();
                db.GetLastSql().Print();

                Assert.That(dbGoals.Items[0].GoalId, Is.EqualTo(goalWithItems.Id));
                Assert.That(dbGoals.Items[1].GoalId, Is.EqualTo(goalWithItems.Id));
            }
        }

        [Test]
        public void Does_fallback_to_reference_convention_when_alias_is_used()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<AliasedCustomer>();
                db.DropTable<AliasedCustomerAddress>();
                db.CreateTable<AliasedCustomerAddress>();
                db.CreateTable<AliasedCustomer>();

                var customer1 = new AliasedCustomer
                {
                    Name = "Name 1",
                    WorkAddress = new AliasedCustomerAddress
                    {
                        AddressLine1 = "1 Work Road",
                        Country = "UK",
                    },
                    HomeAddress = new AliasedCustomerAddress
                    {
                        AddressLine1 = "2 Home Street",
                        Country = "US",
                    }
                };

                var customer2 = new AliasedCustomer
                {
                    Name = "Name 2",
                    WorkAddress = new AliasedCustomerAddress
                    {
                        AddressLine1 = "3 Work Road",
                        Country = "AU",
                    },
                    HomeAddress = new AliasedCustomerAddress
                    {
                        AddressLine1 = "4 Home Street",
                        Country = "NZ",
                    }
                };

                db.Save(customer1, references: true);
                db.Save(customer2, references: true);

                db.Select<AliasedCustomer>().PrintDump();
                db.Select<AliasedCustomerAddress>().PrintDump();

                var dbCustomers = db.LoadSelect<AliasedCustomer>();
                dbCustomers.PrintDump();

                var dbCustomer1 = dbCustomers.First(x => x.Name == "Name 1");

                Assert.That(dbCustomer1.Name, Is.EqualTo("Name 1"));
                Assert.That(dbCustomer1.WorkAddress, Is.Not.Null);
                Assert.That(dbCustomer1.WorkAddress.Country, Is.EqualTo("UK"));
                Assert.That(dbCustomer1.HomeAddress, Is.Not.Null);
                Assert.That(dbCustomer1.HomeAddress.Country, Is.EqualTo("US"));

                var dbCustomer2 = db.LoadSingleById<AliasedCustomer>(customer2.Id);
                Assert.That(dbCustomer2.Name, Is.EqualTo("Name 2"));
                Assert.That(dbCustomer2.WorkAddress, Is.Not.Null);
                Assert.That(dbCustomer2.WorkAddress.Country, Is.EqualTo("AU"));
                Assert.That(dbCustomer2.HomeAddress, Is.Not.Null);
                Assert.That(dbCustomer2.HomeAddress.Country, Is.EqualTo("NZ"));
            }
        }

    }
}