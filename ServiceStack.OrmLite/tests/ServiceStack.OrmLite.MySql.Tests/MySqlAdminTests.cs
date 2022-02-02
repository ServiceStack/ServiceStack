using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.MySql.Tests
{
    public class Describe
    {
        [Alias("Field")]
        public string Name { get; set; }
        public string Type { get; set; }
        public string Null { get; set; }
        public string Key { get; set; }
        public string Default { get; set; }
        public string Extra { get; set; }
    }

    [TestFixture]
    public class MySqlAdminTests : OrmLiteTestBase
    {
        [Test]
        public void Can_describe_a_table()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Author>();

                var info = db.SqlList<Describe>("DESCRIBE Author");

                info.PrintDump();
            }
        }
    }
}