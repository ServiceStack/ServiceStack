using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite]
    public class SaveAllReferencesIssues : OrmLiteProvidersTestBase
    {
        public SaveAllReferencesIssues(DialectContext context) : base(context) {}

        public class BranchRef
        {
            [AutoId]
            public Guid Id { get; set; }

            [Reference]
            public AddressRef Address { get; set; }
        }

        public class AddressRef
        {
            [AutoId]
            public Guid Id { get; set; }

            [ForeignKey(typeof(BranchRef), OnDelete = "CASCADE")]
            [Required]
            public Guid BranchRefId { get; set; }

            [Required]
            public string StreetAddress { get; set; }

            [Required]
            public string City { get; set; }

            [Required]
            public string State { get; set; }

            [Required]
            public string ZipCode { get; set; }
        }
        
        private static void CreateRefTables(IDbConnection db)
        {
            if (db.TableExists<BranchRef>())
                db.DeleteAll<BranchRef>();

            if (db.TableExists<AddressRef>())
                db.DeleteAll<AddressRef>();

            db.DropTable<AddressRef>();
            db.DropTable<BranchRef>();

            db.CreateTable<BranchRef>();
            db.CreateTable<AddressRef>();
        }
        
        [Test]
        public void Can_use_Save_References_with_ForeignKey()
        {
            using (var db = OpenDbConnection())
            {
                CreateRefTables(db);
                
                //Generate dummy data
                var branch = new BranchRef
                {
                    Address = new AddressRef
                    {
                        StreetAddress = "2100 Gotham Lane",
                        City = "Gotham",
                        State = "NJ",
                        ZipCode = "12345"
                    }
                };

                db.Save(branch, references: true);

                Assert.That(branch.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(branch.Address.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(branch.Id, Is.EqualTo(branch.Address.BranchRefId));
            }
        }

        public class BranchSelfRef
        {
            [AutoId]
            public Guid Id { get; set; }

            [Reference]
            public AddressSelfRef Address { get; set; }

            [ForeignKey(typeof(AddressSelfRef), OnDelete = "CASCADE")]
            public Guid? AddressSelfRefId { get; set; }
        }

        public class AddressSelfRef
        {
            [AutoId]
            public Guid Id { get; set; }

            [Required]
            public string StreetAddress { get; set; }

            [Required]
            public string City { get; set; }

            [Required]
            public string State { get; set; }

            [Required]
            public string ZipCode { get; set; }
        }
        
        private static void CreateSelfRefTables(IDbConnection db)
        {
            if (db.TableExists<AddressSelfRef>())
                db.DeleteAll<AddressSelfRef>();

            if (db.TableExists<BranchSelfRef>())
                db.DeleteAll<BranchSelfRef>();

            db.DropTable<BranchSelfRef>();
            db.DropTable<AddressSelfRef>();

            db.CreateTable<AddressSelfRef>();
            db.CreateTable<BranchSelfRef>();
        }
        
        [Test]
        public void Can_use_Save_References_with_ForeignKey_using_Self_Reference_Id()
        {
            using (var db = OpenDbConnection())
            {
                CreateSelfRefTables(db);
                
                var branch = new BranchSelfRef
                {
                    Address = new AddressSelfRef
                    {
                        StreetAddress = "2100 Gotham Lane",
                        City = "Gotham",
                        State = "NJ",
                        ZipCode = "12345"
                    }
                };

                db.Save(branch, references: true);

                Assert.That(branch.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(branch.AddressSelfRefId, Is.Not.EqualTo(Guid.Empty));
                Assert.That(branch.AddressSelfRefId, Is.EqualTo(branch.Address.Id));
            }
        }
         
    }
}