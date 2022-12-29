using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [Alias("ProUser")]
    public class DomainUser
    {
        public int UserType { get; set; }
        public string Id { get; set; }

        [Reference]
        public Address HomeAddress { get; set; }

        [Reference]
        public List<ProOrder> Orders { get; set; }

        [DataAnnotations.Ignore]
        public UserType UserTypeEnum
        {
            get { return (UserType)UserType; }
            set { UserType = (int)value; }
        }
    }

    public class Address
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string ProUserId { get; set; }
        public string StreetName { get; set; }
    }

    public enum UserType
    {
        Normal = 1,
        Domain = 2
    }

    public class ProOrder
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string ProUserId { get; set; }

        public string Details { get; set; }

    }

    [TestFixtureOrmLite]
    public class SaveDomainUserReferencesIssue : OrmLiteProvidersTestBase
    {
        public SaveDomainUserReferencesIssue(DialectContext context) : base(context) {}

        [Test]
        public void Can_save_DomainUser_references()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<ProOrder>();
                db.DropTable<Address>();
                db.DropTable<DomainUser>();

                db.CreateTable<DomainUser>();
                db.CreateTable<ProOrder>();
                db.CreateTable<Address>();

                var user = new DomainUser
                {
                    Id = "UserId",
                    UserType = 1,
                    HomeAddress = new Address
                    {
                        StreetName = "1 Street"
                    },
                    Orders = new List<ProOrder>
                    {
                        new ProOrder { Details = "Order1 Details" },
                        new ProOrder { Details = "Order2 Details" },
                    }
                };

                using (var trans = db.OpenTransaction(IsolationLevel.ReadCommitted))
                {
                    //Same as below in 1 line
                    //db.Save(user, references: true);
                    db.Save(user);
                    db.SaveReferences(user, user.HomeAddress);
                    db.SaveReferences(user, user.Orders);

                    trans.Commit();
                }

                user = db.LoadSingleById<DomainUser>("UserId");
                user.PrintDump();
                Assert.That(user.Orders.Count, Is.EqualTo(2));

                user.UserTypeEnum = UserType.Domain;
                user.HomeAddress.StreetName = "Some new street";
                user.Orders[1].Details = "Nestle Chocolates";
                user.Orders.Add(new ProOrder
                {
                    ProUserId = user.Id,
                    Details = "Reese",
                });

                using (var trans = db.OpenTransaction(IsolationLevel.ReadCommitted))
                {
                    //Same as below in 1 line
                    //db.Save(user, references: true);
                    db.Save(user);
                    db.SaveReferences(user, user.HomeAddress);
                    db.SaveReferences(user, user.Orders);

                    trans.Commit();
                }

                user = db.LoadSingleById<DomainUser>("UserId");
                user.PrintDump();

                Assert.That(user.Orders.Count, Is.EqualTo(3));
            }
        }
    }
}