using System;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Expression
{
    [TestFixtureOrmLite]
    public class LimitExpressionTests : OrmLiteProvidersTestBase
    {
        public LimitExpressionTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_Skip_and_Take_UserAuth_Join()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UserAuth>();
                db.DropAndCreateTable<UserAuthRole>();

                var now = DateTime.UtcNow;
                5.Times(i =>
                {
                    var id = (int)db.Insert(new UserAuth
                    {
                        DisplayName = "Name" + i,
                        CreatedDate = now,
                        ModifiedDate = now,
                    }, selectIdentity: true);

                    db.Insert(new UserAuthRole
                    {
                        UserAuthId = id,
                        Role = "role",
                        CreatedDate = now,
                        ModifiedDate = now,
                    });
                });

                var query = db.From<UserAuth>()
                    .LeftJoin<UserAuthRole>((a, b) => a.Id == b.UserAuthId)
                    .Where<UserAuthRole>(r => "role" == r.Role)
                    .Skip(2)
                    .Take(2)
                    .OrderBy("displayName".SqlColumn(DialectProvider))
                    .SelectDistinct();

                var results = db.Select(query);
                Assert.That(results.Count, Is.EqualTo(2));
                Assert.That(results.Map(x => x.DisplayName), Is.EqualTo(new[] { "Name2", "Name3" }));

                db.GetLastSql().Print();
                results.PrintDump();
            }
        }
    }
}