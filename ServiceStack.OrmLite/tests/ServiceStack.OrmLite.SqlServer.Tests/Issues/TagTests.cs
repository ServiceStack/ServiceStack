using NUnit.Framework;

namespace ServiceStack.OrmLite.SqlServerTests.Issues
{
    [TestFixture]
    public class TagTests : OrmLiteTestBase
    {
        [Test]
        public void Can_Tag_Expression_That_Has_Limit()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<SimpleModel>();

                db.Insert(new SimpleModel { Id = 1, Foo = 1, Bar = 2 });
                db.Insert(new SimpleModel { Id = 2, Foo = 3, Bar = 4 });

                var query = db.From<SimpleModel>()
                              .Limit(1)
                              .TagWith("AnAwesomeQuery");

                var x = query.ToSelectStatement();
                Assert.That(x, Does.StartWith("-- AnAwesomeQuery"));

                var results = db.Select(query);
                Assert.That(results.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void Can_Tag_Distinct_Expression_That_Has_Limit()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<SimpleModel>();

                db.Insert(new SimpleModel { Id = 1, Foo = 1, Bar = 2 });
                db.Insert(new SimpleModel { Id = 2, Foo = 3, Bar = 4 });

                var query = db.From<SimpleModel>()
                              .Limit(1)
                              .TagWith("AnAwesomeQuery")
                              .SelectDistinct();

                Assert.That(query.ToSelectStatement(), Does.StartWith("-- AnAwesomeQuery"));

                var results = db.Select(query);
                Assert.That(results.Count, Is.EqualTo(1));
            }
        }
    }
}
