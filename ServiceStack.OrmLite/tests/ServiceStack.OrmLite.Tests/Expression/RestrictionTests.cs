using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Expression
{
    [TestFixtureOrmLite]
    public class ExpressionTests : OrmLiteProvidersTestBase
    {
        public ExpressionTests(DialectContext context) : base(context) {}

        public class Person
        {
            public Person() {}

            public Person(int id, string firstName, string lastName, int age)
            {
                Id = id;
                FirstName = firstName;
                LastName = lastName;
                Age = age;
            }

            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int? Age { get; set; }
        }

        public Person[] People = {
            new Person(1, "Jimi", "Hendrix", 27),
            new Person(2, "Janis", "Joplin", 27),
            new Person(3, "Jim", "Morrisson", 27),
            new Person(4, "Kurt", "Cobain", 27),
            new Person(5, "Elvis", "Presley", 42),
            new Person(6, "Michael", "Jackson", 50),
        };

        [Test]
        public void Can_Chain_Expressions_Using_Or_and_And()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.InsertAll(People);

                var q = db.From<Person>();

                q.Where(x => x.FirstName.StartsWith("Jim")).Or(x => x.LastName.StartsWith("Cob"));

                var results = db.Select(q);
                Assert.AreEqual(3, results.Count);

                q.Where(); //clear where expression

                q.Where(x => x.FirstName.StartsWith("Jim")).And(x => x.LastName.StartsWith("Hen"));
                results = db.Select(q);

                Assert.AreEqual(1, results.Count);
            }
        }

        [Test]
        public void Can_get_rowcount_from_expression_visitor()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.InsertAll(People);

                var q = db.From<Person>();

                q.Where(x => x.FirstName.StartsWith("Jim")).Or(x => x.LastName.StartsWith("Cob"));

                var count = db.Count(q);

                var results = db.Select(q);
                Assert.AreEqual(count, results.Count);
            }
        }
    }
}