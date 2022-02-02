using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [Alias("UserAuth")]
    [CompositeIndex(true, "CompanyId", "UserName")]
    public class MyCustomUserAuth
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(Company))]
        public int CompanyId { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }
    }

    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [TestFixtureOrmLite]
    public class CompositeIndexNamingStrategyIssue : OrmLiteProvidersTestBase
    {
        public CompositeIndexNamingStrategyIssue(DialectContext context) : base(context) {}

        [Test]
        public void Can_Create_composite_index_with_property_name_indexes()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<MyCustomUserAuth>();
                db.DropTable<Company>();
                db.CreateTable<Company>();
                db.CreateTable<MyCustomUserAuth>();
            }
        }
    }
}