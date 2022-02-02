using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Async.Issues
{
    [TestFixtureOrmLite]
    public class PredicateBuilderIssue : OrmLiteProvidersTestBase
    {
        public PredicateBuilderIssue(DialectContext context) : base(context) {}

        public class ItemList
        {
            public int Id { get; set; }
            public string Brand { get; set; }
            public string ItemDescription { get; set; }
            public string Serial { get; set; }
            public string ItemStatus { get; set; }
        }

        public class Filter
        {
            public string Keyword { get; set; }
            public string ItemStatus { get; set; }
        }

        [Test]
        public async Task Can_filter_with_predicate()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ItemList>();
                db.Insert(new ItemList { Id = 1, Brand = "foobar" });

                var filter = new Filter { Keyword = "foo" };
                var q = PredicateBuilder.True<ItemList>();

                if (!string.IsNullOrWhiteSpace(filter.Keyword))
                {
                    q = q.And(i => i.Brand.Contains(filter.Keyword) || i.ItemDescription.Contains(filter.Keyword) || i.Serial.Contains(filter.Keyword));
                }

                if (!string.IsNullOrWhiteSpace(filter.ItemStatus))
                {
                    q = q.And(i => i.ItemStatus == filter.ItemStatus);
                }

                var count = await db.CountAsync(db.From<ItemList>().Where(q));
                Assert.That(count, Is.EqualTo(1));
            }
        }
    }
}