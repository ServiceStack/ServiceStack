using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite]
    public class GeneratedSqlIssues : OrmLiteProvidersTestBase
    {
        public GeneratedSqlIssues(DialectContext context) : base(context) {}

        [Test]
        public void Does_generate_valid_sql_when_param_contains_dollar_char()
        {
            using (var db = OpenDbConnection())
            {
                var model = new Poco
                {
                    Id = 1,
                    Name = "Guest$"
                };

                var sql = db.ToUpdateStatement(model);
                Assert.That(sql, Is.EqualTo($"UPDATE {DialectProvider.GetQuotedTableName("Poco")} SET {DialectProvider.GetQuotedColumnName("Name")}='Guest$' WHERE {DialectProvider.GetQuotedColumnName("Id")}=1"));
            }
        }
    }
}