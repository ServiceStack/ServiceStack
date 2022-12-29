using NUnit.Framework;

namespace ServiceStack.OrmLite.SqlServerTests.Issues
{
    [TestFixture]
    public class CountDistinctTests : OrmLiteTestBase
    {
        public class CountDistinctIssueTable
        {
            public long AutoId { get; set; }
        }

        [Test]
        public void CountDistinctTest()
        {
            Execute(SqlServerDialect.Provider);
        }

        [Test]
        public void CountDistinctTest2008()
        {
            Execute(SqlServer2008Dialect.Provider);
        }

        [Test]
        public void CountDistinctTest2012()
        {
            Execute(SqlServer2012Dialect.Provider);
        }

        private void Execute(IOrmLiteDialectProvider dialectProvider)
        {
            using var db = OpenDbConnection(null, dialectProvider);
            db.DropAndCreateTable<CountDistinctIssueTable>();

            var query = db.From<CountDistinctIssueTable>();
            query.Skip(0);

            db.Scalar<long>(query.Select(x => Sql.CountDistinct(x.AutoId)));
        }
    }
}