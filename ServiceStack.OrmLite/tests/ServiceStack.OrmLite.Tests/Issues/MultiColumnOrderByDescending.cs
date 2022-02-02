using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite]
    public class MultiColumnOrderByDescending : OrmLiteProvidersTestBase
    {
        public MultiColumnOrderByDescending(DialectContext context) : base(context) {}

        private List<Person> _people;

        [OneTimeSetUp]
        public new void TestFixtureSetUp()
        {
            _people = new List<Person>
            {
                new Person
                {
                    Id = 1,
                    FirstName = "Aaron",
                    LastName = "Anderson"
                },
                new Person
                {
                    Id = 2,
                    FirstName = "Zack",
                    LastName = "Zimmerman"
                }
            };

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.SaveAll(_people);
            }
        }

        [Test]
        public void Can_sort_multiple_columns_in_descending_order()
        {
            using (var db = OpenDbConnection())
            {
                var q = db.From<Person>()
                    .OrderByDescending(p => p.LastName)
                    .ThenByDescending(p => p.FirstName);

                var result = db.Select(q);

                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result[0].Id, Is.EqualTo(2));
            }
        }

        [Test]
        public void Does_orderbydescending_multiple_columns_using_orderby()
        {
            using (var db = OpenDbConnection())
            {
                var q = db.From<Person>()
                    .OrderBy(rn => new {
                        sortA = Sql.Desc(rn.LastName), 
                        sortB = Sql.Desc(rn.FirstName)
                    });

                var result = db.Select(q);

                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result[0].Id, Is.EqualTo(2));
            }
        }

        [Test]
        public void Does_orderbydescending_multiple_columns_using_orderbydescending()
        {
            using (var db = OpenDbConnection())
            {
                var q = db.From<Person>()
                    .OrderByDescending(p => new { p.LastName, p.FirstName });

                var result = db.Select(q);

                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result[0].Id, Is.EqualTo(2));
            }
        }

        public class Record
        {
            public int Id { get; set; }
            public double Value { get; set; }
            public DateTime? Time { get; set; }
        }

        [Test]
        public void Can_OrderBy_DateTime()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Record>();

                db.InsertAll(new[] {
                    new Record
                    {
                        Id = 1,
                        Value = 50,
                        Time = DateTime.Now,
                    },
                });

                var q = db.From<Record>();
                q.OrderByDescending(x => x.Time ?? DateTime.UtcNow);

                var results = db.Select(q);

                results.PrintDump();
            }
        }
    }
}