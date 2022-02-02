using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public class DistinctColumn
    {
        public int Id { get; set; }
        public int Foo { get; set; }
        public int Bar { get; set; }
    }

    public class DistinctJoinColumn
    {
        public int Id { get; set; }
        public int DistinctColumnId { get; set; }
        public string Name { get; set; }
    }

    [Alias("t1")]
    class TableWithAliases
    {
        public int Id { get; set; }

        [Alias("n1")]
        public string Name { get; set; }
        [Alias("n2")]
        public string Name1 { get; set; }
        [Alias("n3")]
        public string Name2 { get; set; }
    }

    [TestFixtureOrmLite]
    public class SelectDistinctTests : OrmLiteProvidersTestBase
    {
        public SelectDistinctTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_Select_Multiple_Distinct_Columns()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<DistinctColumn>();
                db.DropAndCreateTable<DistinctJoinColumn>();

                db.InsertAll(new[] {
                    new DistinctColumn { Id = 1, Foo = 1, Bar = 42 },
                    new DistinctColumn { Id = 2, Foo = 2, Bar = 55 },
                });

                db.InsertAll(new[] {
                    new DistinctJoinColumn { DistinctColumnId = 1, Name = "Foo", Id = 1 },
                    new DistinctJoinColumn { DistinctColumnId = 1, Name = "Foo", Id = 2 },
                    new DistinctJoinColumn { DistinctColumnId = 2, Name = "Bar", Id = 3 },
                });

                var q = db.From<DistinctColumn>()
                    .Join<DistinctJoinColumn>()
                    .SelectDistinct(dt => new { dt.Bar, dt.Foo });

                var result = db.Select(q);
                db.GetLastSql().Print();

                Assert.That(result.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void Does_select_alias_in_custom_select()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TableWithAliases>();

                for (var i = 1; i <= 5; i++)
                {
                    db.Insert(new TableWithAliases { Id = i, Name = "foo" + i, Name1 = "bar" + i, Name2 = "qux" + i });
                }

                var uniqueTrackNames = db.ColumnDistinct<string>(
                    db.From<TableWithAliases>().Select(x => x.Name));

                Assert.That(uniqueTrackNames, Is.EquivalentTo(new []
                {
                    "foo1",
                    "foo2",
                    "foo3",
                    "foo4",
                    "foo5",
                }));
            }
        }
    }
}