using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Legacy;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.Async.Legacy
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class ApiPostgreSqlLegacyTestsAsync : OrmLiteProvidersTestBase
    {
        public ApiPostgreSqlLegacyTestsAsync(DialectContext context) : base(context) {}
        
#pragma warning disable 618
        [Test]
        public async Task API_PostgreSql_Legacy_Examples_Async()
        {
            var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.DropAndCreateTable<PersonWithAutoId>();

            await db.InsertAsync(Person.Rockstars);

            await db.SelectAsync<Person>(q => q.Where(x => x.Age > 40).OrderBy(x => x.Id));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" \nFROM \"person\"\nWHERE (\"age\" > :0)\nORDER BY \"id\""));

            await db.SelectAsync<Person>(q => q.Where(x => x.Age > 40));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" \nFROM \"person\"\nWHERE (\"age\" > :0)"));

            await db.SingleAsync<Person>(q => q.Where(x => x.Age == 42));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" \nFROM \"person\"\nWHERE (\"age\" = :0)\nLIMIT 1"));

            await db.SelectFmtAsync<Person>("age > {0}", 40);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE age > 40"));

            await db.SelectFmtAsync<Person>("SELECT * FROM person WHERE age > {0}", 40);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM person WHERE age > 40"));

            await db.SelectFmtAsync<EntityWithId>(typeof(Person), "age > {0}", 40);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\" FROM \"person\" WHERE age > 40"));

            await db.SingleFmtAsync<Person>("age = {0}", 42);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE age = 42"));

            await db.ScalarFmtAsync<int>("SELECT COUNT(*) FROM person WHERE age > {0}", 40);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM person WHERE age > 40"));

            await db.ColumnFmtAsync<string>("SELECT last_name FROM person WHERE age = {0}", 27);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT last_name FROM person WHERE age = 27"));

            await db.ColumnDistinctFmtAsync<int>("SELECT age FROM person WHERE age < {0}", 50);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT age FROM person WHERE age < 50"));

            await db.LookupFmtAsync<int, string>("SELECT age, last_name FROM person WHERE age < {0}", 50);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT age, last_name FROM person WHERE age < 50"));

            await db.DictionaryFmtAsync<int, string>("SELECT id, last_name FROM person WHERE age < {0}", 50);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT id, last_name FROM person WHERE age < 50"));

            await db.ExistsFmtAsync<Person>("age = {0}", 42);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE age = 42"));
            await db.ExistsFmtAsync<Person>("SELECT * FROM person WHERE age = {0}", 42);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM person WHERE age = 42"));

            var rowsAffected = await db.ExecuteNonQueryAsync("UPDATE Person SET last_name={0} WHERE id={1}".SqlFmt(DialectProvider, "WaterHouse", 7));
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE Person SET last_name='WaterHouse' WHERE id=7"));

            await db.InsertOnlyAsync(new PersonWithAutoId { FirstName = "Amy", Age = 27 }, q => q.Insert(p => new { p.FirstName, p.Age }));
            Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"person_with_auto_id\" (\"first_name\",\"age\") VALUES (:FirstName,:Age)"));

            await db.InsertOnlyAsync(new PersonWithAutoId { FirstName = "Amy", Age = 27 }, q => q.Insert(p => new { p.FirstName, p.Age }));
            Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"person_with_auto_id\" (\"first_name\",\"age\") VALUES (:FirstName,:Age)"));

            await db.UpdateOnlyAsync(new Person { FirstName = "JJ", LastName = "Hendo" }, q => q.Update(p => p.FirstName));
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName"));

            await db.UpdateOnlyAsync(new Person { FirstName = "JJ" }, q => q.Update(p => p.FirstName).Where(x => x.FirstName == "Jimi"));
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName WHERE (\"first_name\" = :0)"));

            await db.UpdateFmtAsync<Person>(set: "first_name = {0}".SqlFmt(DialectProvider, "JJ"), where: "last_name = {0}".SqlFmt(DialectProvider, "Hendrix"));
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET first_name = 'JJ' WHERE last_name = 'Hendrix'"));

            await db.UpdateFmtAsync(table: "person", set: "first_name = {0}".SqlFmt(DialectProvider, "JJ"), where: "last_name = {0}".SqlFmt(DialectProvider, "Hendrix"));
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET first_name = 'JJ' WHERE last_name = 'Hendrix'"));

            await db.DeleteFmtAsync<Person>("age = {0}", 27);
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE age = 27"));

            await db.DeleteFmtAsync(typeof(Person), "age = {0}", 27);
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE age = 27"));

            await db.DeleteAsync<Person>(q => q.Where(p => p.Age == 27));
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE (\"age\" = :0)"));

            await db.DeleteFmtAsync<Person>(where: "age = {0}".SqlFmt(DialectProvider, 27));
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE age = 27"));

            await db.DeleteFmtAsync(table: "Person", where: "age = {0}".SqlFmt(DialectProvider, 27));
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE age = 27"));

            db.Dispose();
        }
#pragma warning restore 618
    }
}