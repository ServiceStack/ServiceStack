using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests.Issues
{
    public class SimpleModel
    {
        public int Id { get; set; }
        public int Foo { get; set; }
        public int Bar { get; set; }
    }

    [TestFixture]
    public class CustomSelectWithPagingIssue
        : OrmLiteTestBase
    {
        [Test]
        public void Can_CustomSelect_With_Paging()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<SimpleModel>();

                db.InsertAll(new[] {
                    new SimpleModel { Id = 1, Foo = 1, Bar = 42 },
                    new SimpleModel { Id = 2, Foo = 2, Bar = 55 },
                });

                var q = db.From<SimpleModel>()
                    .UnsafeSelect("SimpleModel.Id, CASE WHEN Foo IN (1,3) THEN 1 ELSE 0 END AS Foo, Bar")
                    .Take(1)
                    .Skip(1);

                var result = db.Select<SimpleModel>(q);
                db.GetLastSql().Print();

                Assert.That(result.Count, Is.EqualTo(1));
            }
        }
    }
}