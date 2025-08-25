using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Expression;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class SqlExpressionJoinWithRowversionTests(DialectContext context) : ExpressionsTestBase(context)
{
    public class TableA
    {
        public int Id { get; set; }
        public bool Bool { get; set; }
        public string Name { get; set; }

        public ulong RowVersion { get; set; }
    }

    public class TableB
    {
        public int Id { get; set; }
        public int TableAId { get; set; }
        public string Name { get; set; }
        public ulong RowVersion { get; set; }
    }

    private class JoinSelectResults2
    {
        // From TableA
        public int Id { get; set; }
        public bool Bool { get; set; }
        public string Name { get; set; }

        public ulong RowVersion { get; set; }


        // From TableB
        public int TableBId { get; set; }
        public string TableBName { get; set; }

        public override bool Equals(object obj)
        {
            var other = (JoinSelectResults2)obj;
            return Id == other.Id && Bool == other.Bool && Name == other.Name && TableBId == other.TableBId && TableBName == other.TableBName;
        }

        public override int GetHashCode() =>
            ((23 * 37 + Id) * 37 + TableBId)
            ^ Name.GetHashCode()
            ^ TableBName.GetHashCode()
            ^ (Bool ? 65535 : 0);

    }

    [Test]
    public void Can_select_entire_tables_with_rowversion()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TableA>();
        db.DropAndCreateTable<TableB>();

        db.Insert(new TableA { Id = 1, Bool = false, Name = "NameA1" });
        db.Insert(new TableA { Id = 2, Bool = true, Name = "NameA2" });
        db.Insert(new TableB { Id = 1, TableAId = 1, Name = "NameB1" });
        db.Insert(new TableB { Id = 2, TableAId = 2, Name = "NameB2" });
        db.Insert(new TableB { Id = 3, TableAId = 2, Name = "NameB3" });

        var q1 = db.From<TableA>()
            .Join<TableB>();

        var results = db.Select<JoinSelectResults2>(q1);
        Assert.That(results.Count, Is.EqualTo(3));

        var q2 = db.From<TableA>()
            .Join<TableB>()
            .Select<TableA, TableB>((a, b) => new { a, TableBId = b.Id, TableBName = b.Name });

        results = db.Select<JoinSelectResults2>(q2);
        Assert.That(results.Count, Is.EqualTo(3));
    }
}