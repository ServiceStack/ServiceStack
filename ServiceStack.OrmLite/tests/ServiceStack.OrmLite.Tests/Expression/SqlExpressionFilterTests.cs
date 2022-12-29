using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.Expression
{
    [TestFixtureOrmLite]
    public class SqlExpressionFilterTests : OrmLiteProvidersTestBase
    {
        public SqlExpressionFilterTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_enhance_SqlExpression_with_Custom_SqlFilter()
        {
            if (Dialect != Dialect.SqlServer) return;

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();

                db.InsertAll(Person.Rockstars);

                var q = db.From<Person>()
                    .Where(x => x.Age == 27)
                    .WithSqlFilter(sql => sql + "\noption (recompile)");

                var rockstars = db.Select(q);
                Assert.That(rockstars.Count, Is.EqualTo(4));

                Assert.That(db.GetLastSql(), Does.Contain("option (recompile)"));
            }
        }
    }
}