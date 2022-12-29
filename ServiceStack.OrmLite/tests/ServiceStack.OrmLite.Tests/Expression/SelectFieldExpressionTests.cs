using System.Linq;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Expression
{
    [TestFixtureOrmLite]
    public class SelectFieldExpressionTests : OrmLiteProvidersTestBase
    {
        public SelectFieldExpressionTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_Select_Substring()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.InsertAll(Person.Rockstars);

                var results = db.Select<Person>(x => x.FirstName.Substring(1, 2) == "im");

                results.PrintDump();

                var expected = Person.Rockstars.Where(x => x.FirstName.Substring(1, 2) == "im").ToList();

                Assert.That(results.Count, Is.EqualTo(expected.Count));
                Assert.That(results, Is.EquivalentTo(expected));

                results = db.Select<Person>(x => x.FirstName.Substring(1) == "im");
                results.PrintDump();

                expected = Person.Rockstars.Where(x => x.FirstName.Substring(1) == "im").ToList();
                Assert.That(results.Count, Is.EqualTo(expected.Count));
                Assert.That(results, Is.EquivalentTo(expected));
            }
        }

        [Test]
        public void Can_select_value_as_null()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.InsertAll(Person.Rockstars);

                var firstName = "Kurt";
                string lastName = null;

                var results = db.Select(db.From<Person>()
                    .Where(x => firstName == null || x.FirstName == firstName)
                    .And(x => lastName == null || x.LastName == lastName));

                db.GetLastSql().Print();

                results.PrintDump();

                Assert.That(results[0].LastName, Is.EqualTo("Cobain"));
            }
        }

        [Test]
        public void Can_select_partial_list_of_fields()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.InsertAll(Person.Rockstars);

                var results = db.Select(db.From<Person>().Select(new[] { "Id", "FirstName", "Age" }));

                db.GetLastSql().Print();

                Assert.That(results.All(x => x.Id > 0));
                Assert.That(results.All(x => x.FirstName != null));
                Assert.That(results.All(x => x.LastName == null));
                Assert.That(results.Any(x => x.Age > 0));
            }
        }

        [Test]
        public void Can_select_partial_list_of_fields_case_insensitive()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.InsertAll(Person.Rockstars);

                var results = db.Select(db.From<Person>().Select(new[] { "id", "firstname", "age" }));

                db.GetLastSql().Print();

                Assert.That(results.All(x => x.Id > 0));
                Assert.That(results.All(x => x.FirstName != null));
                Assert.That(results.All(x => x.LastName == null));
                Assert.That(results.Any(x => x.Age > 0));
            }
        }

        [Test]
        public void Does_ignore_invalid_fields()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.InsertAll(Person.Rockstars);

                var results = db.Select(db.From<Person>().Select(new[] { null, "Id", "unknown", "FirstName", "Age" }));

                db.GetLastSql().Print();

                Assert.That(results.All(x => x.Id > 0));
                Assert.That(results.All(x => x.FirstName != null));
                Assert.That(results.All(x => x.LastName == null));
                Assert.That(results.Any(x => x.Age > 0));
            }
        }

        [Test]
        public void Can_select_fields_from_joined_table()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.InsertAll(Person.Rockstars);
                db.DropAndCreateTable<RockstarAlbum>();
                db.InsertAll(AutoQueryTests.SeedAlbums);

                var q = db.From<Person>()
                    .Join<RockstarAlbum>((p, a) => p.Id == a.RockstarId)
                    .Select(new[] { "Id", "FirstName", "Age", "RockstarAlbumName" });

                var results = db.Select<RockstarWithAlbum>(q);

                db.GetLastSql().Print();

                Assert.That(results.All(x => x.Id > 0));
                Assert.That(results.All(x => x.FirstName != null));
                Assert.That(results.All(x => x.LastName == null));
                Assert.That(results.All(x => x.Age > 0));
                Assert.That(results.All(x => x.RockstarId == 0));
                Assert.That(results.All(x => x.RockstarAlbumName != null));
            }
        }

        [Test]
        public void Can_select_fields_from_joined_table_case_insensitive()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Rockstar>();
                db.InsertAll(AutoQueryTests.SeedRockstars);
                db.DropAndCreateTable<RockstarAlbum>();
                db.InsertAll(AutoQueryTests.SeedAlbums);

                var q = db.From<Rockstar>()
                    .Join<RockstarAlbum>()
                    .Select(new[] { "id", "firstname", "age", "rockstaralbumname", "rockstaralbumid" });

                var results = db.Select<RockstarWithAlbum>(q);

                results.PrintDump();

                db.GetLastSql().Print();

                Assert.That(results.All(x => x.Id > 0));
                Assert.That(results.All(x => x.FirstName != null));
                Assert.That(results.All(x => x.LastName == null));
                Assert.That(results.All(x => x.Age > 0));
                Assert.That(results.All(x => x.RockstarId == 0));
                Assert.That(results.All(x => x.RockstarAlbumName != null));
                Assert.That(results.All(x => x.RockstarAlbumId >= 10));
            }
        }
    }
}