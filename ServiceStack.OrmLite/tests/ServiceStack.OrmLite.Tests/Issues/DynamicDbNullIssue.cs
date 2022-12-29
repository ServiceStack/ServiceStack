using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public class DynamicDbNullIssue : OrmLiteTestBase
    {
        public class Test
        {
            public int id { get; set; }

            public string name { get; set; }
        }

        [Test]
        public async Task Does_convert_DbNull_Values_in_Async_Results()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Test>();

                db.Insert(new Test { id = 1, name = "A" },
                    new Test { id = 2, name = null });

                var rows = db.Select<dynamic>("select id, name from Test");
                Assert.That(rows.Any(x => x.name == null));

                rows = await db.SelectAsync<dynamic>("select id, name from Test");
                Assert.That(rows.Any(x => x.name == null));
            }
        }
    }
}