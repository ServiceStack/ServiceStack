using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.Async
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class ApiPostgreSqlTestsAsync : OrmLiteProvidersTestBase
    {
        public ApiPostgreSqlTestsAsync(DialectContext context) : base(context) {}

        [Test]
        public async Task API_PostgreSql_Examples_Async()
        {
            var db = OpenDbConnection();
            db.DropAndCreateTable<Person>();
            db.DropAndCreateTable<PersonWithAutoId>();

            await db.InsertAsync(Person.Rockstars);

            await db.SelectAsync<Person>(x => x.Age > 40);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" \nFROM \"person\"\nWHERE (\"age\" > :0)"));

            await db.SelectAsync(db.From<Person>().Where(x => x.Age > 40).OrderBy(x => x.Id));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" \nFROM \"person\"\nWHERE (\"age\" > :0)\nORDER BY \"id\""));

            await db.SelectAsync(db.From<Person>().Where(x => x.Age > 40));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" \nFROM \"person\"\nWHERE (\"age\" > :0)"));

            await db.SingleAsync<Person>(x => x.Age == 42);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" \nFROM \"person\"\nWHERE (\"age\" = :0)\nLIMIT 1"));

            await db.SingleAsync(db.From<Person>().Where(x => x.Age == 42));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" \nFROM \"person\"\nWHERE (\"age\" = :0)\nLIMIT 1"));

            await db.ScalarAsync<Person, int>(x => Sql.Max(x.Age));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Max(\"age\") \nFROM \"person\""));

            await db.ScalarAsync<Person, int>(x => Sql.Max(x.Age), x => x.Age < 50);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Max(\"age\") \nFROM \"person\"\nWHERE (\"age\" < :0)"));

            await db.CountAsync<Person>(x => x.Age < 50);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM \"person\"\nWHERE (\"age\" < :0)"));

            await db.CountAsync(db.From<Person>().Where(x => x.Age < 50));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM \"person\"\nWHERE (\"age\" < :0)"));

            await db.SelectAsync<Person>("age > 40");
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE age > 40"));

            await db.SelectAsync<Person>("SELECT * FROM person WHERE age > 40");
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM person WHERE age > 40"));

            await db.SelectAsync<Person>("age > :Age", new { age = 40 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE age > :Age"));

            await db.SelectAsync<Person>("SELECT * FROM person WHERE age > :Age", new { age = 40 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM person WHERE age > :Age"));

            await db.SelectAsync<Person>("SELECT * FROM person WHERE age > :Age", new[] { db.CreateParam("age", 40) });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM person WHERE age > :Age"));

            await db.SelectAsync<Person>("age > :Age", new Dictionary<string, object> { { "age", 40 } });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE age > :Age"));

            await db.SelectAsync<Person>("SELECT * FROM person WHERE age > :Age", new Dictionary<string, object> { { "age", 40 } });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM person WHERE age > :Age"));

            await db.SelectAsync<EntityWithId>(typeof(Person));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\" FROM \"person\""));

            await db.SelectAsync<EntityWithId>(typeof(Person), "age > @age", new { age = 40 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\" FROM \"person\" WHERE age > @age"));

            await db.WhereAsync<Person>("Age", 27);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE \"age\" = :Age"));

            await db.WhereAsync<Person>(new { Age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE \"age\" = :Age"));

            await db.SelectByIdsAsync<Person>(new[] { 1, 2, 3 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE \"id\" IN (:0,:1,:2)"));

            await db.SelectNonDefaultsAsync(new Person { Id = 1 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE \"id\" = :Id"));

            await db.SelectNonDefaultsAsync("age > :Age", new Person { Age = 40 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE age > :Age"));

            await db.SingleByIdAsync<Person>(1);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE \"id\" = :Id"));

            await db.SingleAsync<Person>(new { Age = 42 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE \"age\" = :Age"));

            await db.SingleAsync<Person>("age = :Age", new { age = 42 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE age = :Age"));

            await db.SingleAsync<Person>("age = :Age", new[] { db.CreateParam("age", 42) });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE age = :Age"));

            await db.SingleByIdAsync<Person>(1);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE \"id\" = :Id"));

            await db.SingleWhereAsync<Person>("Age", 42);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE \"age\" = :Age"));

            await db.ScalarAsync<int>(db.From<Person>().Select(Sql.Count("*")).Where(q => q.Age > 40));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM \"person\"\nWHERE (\"age\" > :0)"));
            await db.ScalarAsync<int>(db.From<Person>().Select(x => Sql.Count("*")).Where(q => q.Age > 40));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Count(*) \nFROM \"person\"\nWHERE (\"age\" > :0)"));

            await db.ScalarAsync<int>("SELECT COUNT(*) FROM person WHERE age > :Age", new { age = 40 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM person WHERE age > :Age"));

            await db.ScalarAsync<int>("SELECT COUNT(*) FROM person WHERE age > :Age", new[] { db.CreateParam("age", 40) });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM person WHERE age > :Age"));

            await db.ColumnAsync<string>(db.From<Person>().Select(x => x.LastName).Where(q => q.Age == 27));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"last_name\" \nFROM \"person\"\nWHERE (\"age\" = :0)"));

            await db.ColumnAsync<string>("SELECT last_name FROM person WHERE age = :Age", new { age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT last_name FROM person WHERE age = :Age"));

            await db.ColumnAsync<string>("SELECT last_name FROM person WHERE age = :Age", new[] { db.CreateParam("age", 27) });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT last_name FROM person WHERE age = :Age"));

            await db.ColumnDistinctAsync<int>(db.From<Person>().Select(x => x.Age).Where(q => q.Age < 50));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"age\" \nFROM \"person\"\nWHERE (\"age\" < :0)"));

            await db.ColumnDistinctAsync<int>("SELECT age FROM person WHERE age < :Age", new { age = 50 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT age FROM person WHERE age < :Age"));

            await db.ColumnDistinctAsync<int>("SELECT age FROM person WHERE age < :Age", new[] { db.CreateParam("age", 50) });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT age FROM person WHERE age < :Age"));

            await db.LookupAsync<int, string>(db.From<Person>().Select(x => new { x.Age, x.LastName }).Where(q => q.Age < 50));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"age\", \"last_name\" \nFROM \"person\"\nWHERE (\"age\" < :0)"));

            await db.LookupAsync<int, string>("SELECT age, last_name FROM person WHERE age < :Age", new { age = 50 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT age, last_name FROM person WHERE age < :Age"));

            await db.LookupAsync<int, string>("SELECT age, last_name FROM person WHERE age < :Age", new[] { db.CreateParam("age", 50) });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT age, last_name FROM person WHERE age < :Age"));

            await db.DictionaryAsync<int, string>(db.From<Person>().Select(x => new { x.Id, x.LastName }).Where(x => x.Age < 50));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"last_name\" \nFROM \"person\"\nWHERE (\"age\" < :0)"));

            await db.DictionaryAsync<int, string>("SELECT id, last_name FROM person WHERE age < :Age", new { age = 50 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT id, last_name FROM person WHERE age < :Age"));

            await db.DictionaryAsync<int, string>("SELECT id, last_name FROM person WHERE age < :Age", new[] { db.CreateParam("age", 50) });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT id, last_name FROM person WHERE age < :Age"));

            await db.ExistsAsync<Person>(x => x.Age < 50);
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT 'exists' \nFROM \"person\"\nWHERE (\"age\" < :0)\nLIMIT 1"));

            await db.ExistsAsync(db.From<Person>().Where(x => x.Age < 50));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT 'exists' \nFROM \"person\"\nWHERE (\"age\" < :0)\nLIMIT 1"));

            await db.ExistsAsync<Person>(new { Age = 42 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE \"age\" = :Age"));

            await db.ExistsAsync<Person>("age = :Age", new { age = 42 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"id\", \"first_name\", \"last_name\", \"age\" FROM \"person\" WHERE age = :Age"));
            await db.ExistsAsync<Person>("SELECT * FROM person WHERE age = :Age", new { age = 42 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM person WHERE age = :Age"));

            await db.SqlListAsync<Person>(db.From<Person>().Select("*").Where(q => q.Age < 50));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * \nFROM \"person\"\nWHERE (\"age\" < :0)"));

            await db.SqlListAsync<Person>("SELECT * FROM person WHERE age < :Age", new { age = 50 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM person WHERE age < :Age"));

            await db.SqlListAsync<Person>("SELECT * FROM person WHERE age < :Age", new[] { db.CreateParam("age", 50) });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM person WHERE age < :Age"));

            await db.SqlListAsync<Person>("SELECT * FROM person WHERE age < :Age", new Dictionary<string, object> { { "age", 50 } });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM person WHERE age < :Age"));

            await db.SqlColumnAsync<string>(db.From<Person>().Select(x => x.LastName).Where(q => q.Age < 50));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"last_name\" \nFROM \"person\"\nWHERE (\"age\" < :0)"));

            await db.SqlColumnAsync<string>("SELECT last_name FROM person WHERE age < :Age", new { age = 50 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT last_name FROM person WHERE age < :Age"));

            await db.SqlColumnAsync<string>("SELECT last_name FROM person WHERE age < :Age", new[] { db.CreateParam("age", 50) });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT last_name FROM person WHERE age < :Age"));

            await db.SqlColumnAsync<string>("SELECT last_name FROM person WHERE age < :Age", new Dictionary<string, object> { { "age", 50 } });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT last_name FROM person WHERE age < :Age"));

            await db.SqlScalarAsync<int>(db.From<Person>().Select(Sql.Count("*")).Where(q => q.Age < 50));
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM \"person\"\nWHERE (\"age\" < :0)"));

            await db.SqlScalarAsync<int>("SELECT COUNT(*) FROM person WHERE age < :Age", new { age = 50 });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM person WHERE age < :Age"));

            await db.SqlScalarAsync<int>("SELECT COUNT(*) FROM person WHERE age < :Age", new[] { db.CreateParam("age", 50) });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM person WHERE age < :Age"));

            await db.SqlScalarAsync<int>("SELECT COUNT(*) FROM person WHERE age < :Age", new Dictionary<string, object> { { "age", 50 } });
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM person WHERE age < :Age"));

            var rowsAffected = await db.ExecuteNonQueryAsync("UPDATE Person SET last_name=@name WHERE id=:Id", new { name = "WaterHouse", id = 7 });
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE Person SET last_name=@name WHERE id=:Id"));


            await db.InsertAsync(new Person { Id = 7, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"person\" (\"id\",\"first_name\",\"last_name\",\"age\") VALUES (:Id,:FirstName,:LastName,:Age)"));

            await db.InsertAsync(new Person { Id = 8, FirstName = "Tupac", LastName = "Shakur", Age = 25 },
                      new Person { Id = 9, FirstName = "Tupac", LastName = "Shakur2", Age = 26 });

            Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"person\" (\"id\",\"first_name\",\"last_name\",\"age\") VALUES (:Id,:FirstName,:LastName,:Age)"));


            await db.InsertAllAsync(new[] { new Person { Id = 10, FirstName = "Biggie", LastName = "Smalls", Age = 24 } });
            Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"person\" (\"id\",\"first_name\",\"last_name\",\"age\") VALUES (:Id,:FirstName,:LastName,:Age)"));

            await db.InsertOnlyAsync(new PersonWithAutoId { FirstName = "Amy", Age = 27 }, p => new { p.FirstName, p.Age });
            Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"person_with_auto_id\" (\"first_name\",\"age\") VALUES (:FirstName,:Age)"));

            await db.InsertOnlyAsync(() => new PersonWithAutoId { FirstName = "Amy", Age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"person_with_auto_id\" (\"first_name\",\"age\") VALUES (:FirstName,:Age)"));

            await db.UpdateAsync(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName, \"last_name\"=:LastName, \"age\"=:Age WHERE \"id\"=:Id"));

            await db.UpdateAsync(new Person { Id = 8, FirstName = "Tupac", LastName = "Shakur3", Age = 27 },
                      new Person { Id = 9, FirstName = "Tupac", LastName = "Shakur4", Age = 28 });
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName, \"last_name\"=:LastName, \"age\"=:Age WHERE \"id\"=:Id"));

            await db.UpdateAsync(new[] { new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 } });
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName, \"last_name\"=:LastName, \"age\"=:Age WHERE \"id\"=:Id"));

            await db.UpdateAllAsync(new[] { new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 } });
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName, \"last_name\"=:LastName, \"age\"=:Age WHERE \"id\"=:Id"));

            await db.UpdateAsync(new Person { Id = 1, FirstName = "JJ", Age = 27 }, p => p.LastName == "Hendrix");
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"id\"=:Id, \"first_name\"=:FirstName, \"last_name\"=:LastName, \"age\"=:Age WHERE (\"last_name\" = :0)"));

            await db.UpdateAsync<Person>(new { FirstName = "JJ" }, p => p.LastName == "Hendrix");
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName WHERE (\"last_name\" = :0)"));

            await db.UpdateNonDefaultsAsync(new Person { FirstName = "JJ" }, p => p.LastName == "Hendrix");
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName WHERE (\"last_name\" = :0)"));

            await db.UpdateOnlyFieldsAsync(new Person { FirstName = "JJ" }, p => p.FirstName);
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName"));

            await db.UpdateOnlyAsync(() => new Person { FirstName = "JJ" });
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName"));

            await db.UpdateOnlyFieldsAsync(new Person { FirstName = "JJ" }, p => p.FirstName, p => p.LastName == "Hendrix");
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName WHERE (\"last_name\" = :0)"));

            await db.UpdateOnlyAsync(() => new Person { FirstName = "JJ" }, p => p.LastName == "Hendrix");
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName WHERE (\"last_name\" = :0)"));

            await db.UpdateOnlyFieldsAsync(new Person { FirstName = "JJ", LastName = "Hendo" }, db.From<Person>().Update(p => p.FirstName));
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName"));

            await db.UpdateOnlyFieldsAsync(new Person { FirstName = "JJ" }, db.From<Person>().Update(p => p.FirstName).Where(x => x.FirstName == "Jimi"));
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName WHERE (\"first_name\" = :0)"));

            await db.UpdateAddAsync(() => new Person { Age = 3 });
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"age\"=\"age\"+:Age"));

            await db.UpdateAddAsync(() => new Person { Age = 5 }, where: x => x.LastName == "Presley");
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"age\"=\"age\"+:Age WHERE (\"last_name\" = :0)"));

            await db.DeleteAsync<Person>(new { FirstName = "Jimi", Age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE \"first_name\"=:FirstName AND \"age\"=:Age"));

            await db.DeleteAsync(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE \"id\"=:Id AND \"first_name\"=:FirstName AND \"last_name\"=:LastName AND \"age\"=:Age"));

            await db.DeleteNonDefaultsAsync(new Person { FirstName = "Jimi", Age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE \"first_name\"=:FirstName AND \"age\"=:Age"));

            await db.DeleteNonDefaultsAsync(new Person { FirstName = "Jimi", Age = 27 },
                                 new Person { FirstName = "Janis", Age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE \"first_name\"=:FirstName AND \"age\"=:Age"));

            await db.DeleteByIdAsync<Person>(1);
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE \"id\" = :0"));

            await db.DeleteByIdsAsync<Person>(new[] { 1, 2, 3 });
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE \"id\" IN (:0,:1,:2)"));

            await db.DeleteAsync<Person>("age = @age", new { age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE age = @age"));

            await db.DeleteAsync(typeof(Person), "age = @age", new { age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE age = @age"));

            await db.DeleteAsync<Person>(p => p.Age == 27);
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE (\"age\" = :0)"));

            await db.DeleteAsync(db.From<Person>().Where(p => p.Age == 27));
            Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"person\" WHERE (\"age\" = :0)"));

            await db.SaveAsync(new Person { Id = 11, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"person\" (\"id\",\"first_name\",\"last_name\",\"age\") VALUES (:Id,:FirstName,:LastName,:Age)"));
            await db.SaveAsync(new Person { Id = 11, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
            Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"person\" SET \"first_name\"=:FirstName, \"last_name\"=:LastName, \"age\"=:Age WHERE \"id\"=:Id"));

            await db.SaveAsync(new Person { Id = 12, FirstName = "Amy", LastName = "Winehouse", Age = 27 },
                    new Person { Id = 13, FirstName = "Amy", LastName = "Winehouse", Age = 27 });

            await db.SaveAllAsync(new[]{ new Person { Id = 14, FirstName = "Amy", LastName = "Winehouse", Age = 27 },
                              new Person { Id = 15, FirstName = "Amy", LastName = "Winehouse", Age = 27 } });

            db.Dispose();
        }
    }
}