using System.Linq;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite]
    public class ColumnGuessingTests : OrmLiteProvidersTestBase
    {
        public ColumnGuessingTests(DialectContext context) : base(context) {}

        private class Person
        {
            public string Name { get; set; }
            public string LastName { get; set; }
            public string NameAtBirth { get; set; }
        }

        [Test]
        public void Dont_guess_extra_columns_when_match_found()
        {
            // Only the specified column should be selected. The value of Name should not be written to FirstName nor NameAtBirth,
            // since there is already a match for the requested column in the data model class.

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.Insert(new Person { LastName = "Smith", Name = "John", NameAtBirth = "Black" });

                var row = db.Select(db.From<Person>().Select(r => new { r.LastName })).Single();
                AssertPerson(new Person { LastName = "Smith" }, row);

                row = db.Select(db.From<Person>().Select(r => new { r.Name })).Single();
                AssertPerson(new Person { Name = "John" }, row);

                row = db.Select(db.From<Person>().Select(r => new { r.NameAtBirth })).Single();
                AssertPerson(new Person { NameAtBirth = "Black" }, row);
            }
        }

        private static void AssertPerson(Person expected, Person actual)
        {
            Assert.AreEqual(expected.Name, actual.Name, "Name differs");
            Assert.AreEqual(expected.LastName, actual.LastName, "LastName differs");
            Assert.AreEqual(expected.NameAtBirth, actual.NameAtBirth, "NameAtBirth differs");
        }
    }
}