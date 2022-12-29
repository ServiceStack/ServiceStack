using System;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public class AddressAudit
    {
        [PrimaryKey, AutoIncrement]
        [Alias("AddressId")]
        public int Id { get; set; }

        [Alias("AddressLinkId")]
        public int AddressId { get; set; }
    }

    public interface ILookup
    {
        Guid Id { get; set; }
        string Name { get; }
    }

    public class TestLookup : ILookup
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public static class TestLookupExtensions
    {
        public static T Lookup<T>(this IDbConnection db, T record) where T : ILookup, new()
        {
            var lkp = db.Single<T>(r => r.Name == record.Name);
            if (lkp != null)
                return lkp;

            if (record.Id == Guid.Empty)
                record.Id = Guid.NewGuid();
            db.Insert(record);
            return record;
        }
    }

    [TestFixtureOrmLite]
    public class SelectAliasIssue : OrmLiteProvidersTestBase
    {
        public SelectAliasIssue(DialectContext context) : base(context) {}

        [Test]
        public void Does_populate_table_with_Aliases_having_same_name_as_alternative_field()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<AddressAudit>();

                db.Insert(new AddressAudit { AddressId = 11 });
                db.Insert(new AddressAudit { AddressId = 12 });
                db.Insert(new AddressAudit { AddressId = 13 });

                var rows = db.Select<AddressAudit>();
                Assert.That(rows.All(x => x.Id > 0));

                var debtor = db.SingleById<AddressAudit>(2);
                var row = db.Single<AddressAudit>(audit => audit.AddressId == debtor.AddressId);

                row.PrintDump();
            }
        }
        
        [Test]
        public void Select_against_interface_in_generic_method()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<TestLookup>();
                
                var newRecord = new TestLookup {Name = "new"};
                
                var lkp = db.Single<TestLookup>(r => r.Name == newRecord.Name);

                
                var lookup = db.Lookup(newRecord);
                Assert.That(lookup.Id != Guid.Empty);
            }
        }
    }
}