using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.Expression
{
    class PersonJoin
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
    }

    [TestFixtureOrmLite]
    public class SqlExpressionDeleteTests : OrmLiteProvidersTestBase
    {
        public SqlExpressionDeleteTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_delete_entity_with_join_expression()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.DropAndCreateTable<PersonJoin>();

                db.Insert(new Person { Id = 1, FirstName = "Foo" });
                db.Insert(new PersonJoin { Id = 1, PersonId = 1 });
                db.Insert(new PersonJoin { Id = 2, PersonId = 1 });

                var q = db.From<Person>()
                    .Join<PersonJoin>((x, y) => x.Id == y.PersonId)
                    .Where<PersonJoin>(x => x.Id == 2);

                var record = db.Single(q);

                Assert.That(record.Id, Is.EqualTo(1));

                db.Delete(q);

                Assert.That(db.Select<Person>().Count, Is.EqualTo(0));
            }
        }
    }
}