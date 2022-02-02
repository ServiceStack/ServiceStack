using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class SelectParamTests : OrmLiteProvidersTestBase
    {
        public SelectParamTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_Select_with_Different_APIs()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.InsertAll(Person.Rockstars);

                Assert.That(db.Select<Person>(x => x.Age == 27).Count, Is.EqualTo(4));
                Assert.That(db.Select(db.From<Person>().Where(x => x.Age == 27)).Count, Is.EqualTo(4));
                Assert.That(db.Select<Person>("Age = @age", new { age = 27 }).Count, Is.EqualTo(4));
                Assert.That(db.Select<Person>("Age = @age", new Dictionary<string, object> { { "age", 27 } }).Count, Is.EqualTo(4));
                Assert.That(db.Select<Person>("Age = @age", new[] { db.CreateParam("age", 27) }).Count, Is.EqualTo(4));
                Assert.That(db.Select<Person>("SELECT * FROM Person WHERE Age = @age", new { age = 27 }).Count, Is.EqualTo(4));
                Assert.That(db.Select<Person>("SELECT * FROM Person WHERE Age = @age", new Dictionary<string, object> { { "age", 27 } }).Count, Is.EqualTo(4));
                Assert.That(db.Select<Person>("SELECT * FROM Person WHERE Age = @age", new[] { db.CreateParam("age", 27) }).Count, Is.EqualTo(4));

                Assert.That(db.Select<Person>("Age = @age", new { age = 27 }).Count, Is.EqualTo(4));

                Assert.That(db.SelectNonDefaults(new Person { Age = 27 }).Count, Is.EqualTo(4));

                Assert.That(db.SqlList<Person>("SELECT * FROM Person WHERE Age = @age", new { age = 27 }).Count, Is.EqualTo(4));
                Assert.That(db.SqlList<Person>("SELECT * FROM Person WHERE Age = @age", new Dictionary<string, object> { { "age", 27 } }).Count, Is.EqualTo(4));
                Assert.That(db.SqlList<Person>("SELECT * FROM Person WHERE Age = @age", new[] { db.CreateParam("age", 27) }).Count, Is.EqualTo(4));
            }
        }

        [Test]
        public void Can_Select_with_Null_param()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.InsertAll(Person.Rockstars);

                var dbCmd = db.CreateCommand();//demo code, just for dbCmd.CreateParam

                // for sqlite that works with parameter with DBNull.Value and usual null.
                Assert.That(db.Select<Person>("age = @age", new List<System.Data.IDbDataParameter> {
                    dbCmd.CreateParam("age", null)
                }).Count, Is.EqualTo(0));
            }
        }
    }
}