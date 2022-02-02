using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public class CountDistinctTests : OrmLiteTestBase
    {
        public class CountDistinctIssueTable
        {
            public long AutoId { get; set; }
        }

        [Test]
        public void Ignores_paging_in_Scalar_queries()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<CountDistinctIssueTable>();

            var query = db.From<CountDistinctIssueTable>();
            query.Skip(0);

            db.Scalar<long>(query.Select(x => Sql.CountDistinct(x.AutoId)));
        }
    }
}