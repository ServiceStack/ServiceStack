using NUnit.Framework;
using ServiceStack.DataAnnotations;
using System.Linq;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class SelectColumnTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    /// <summary>
    /// The class in only to create the nullable column <see cref="Decimal"/>.
    /// </summary>
    [Alias(nameof(TestWithNullable))]
    public class TestWithNullable
    {
        public int Id { get; set; }
        public decimal? Decimal { get; set; }
    }

    [Alias(nameof(TestWithNullable))]
    public class Test
    {
        public int Id { get; set; }
        public decimal Decimal { get; set; }
    }

    [Test]
    public void Should_Return_Column_Value_From_Nullable_Column()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TestWithNullable>();

        var row1 = new TestWithNullable
        {
            Id = 0,
            Decimal = 1.1M,
        };
        db.Insert(row1);

        var row2 = new TestWithNullable
        {
            Id = 1,
            Decimal = null
        };
        db.Insert(row2);

        var q = db.From<Test>().OrderBy(x => x.Id);

        Assert.That(db.SqlColumn<(int, decimal)>(q).Sum(x => x.Item2), Is.EqualTo(row1.Decimal));
        Assert.That(db.SqlColumn<Test>(q).Sum(x => x.Decimal), Is.EqualTo(row1.Decimal));
                
        Assert.That(db.Column<decimal>(q.Select(x => x.Decimal)).Sum(), Is.EqualTo(row1.Decimal));
        Assert.That(db.SqlColumn<decimal>(q.Select(x => x.Decimal)).Sum(), Is.EqualTo(row1.Decimal));
        Assert.That(db.Scalar<decimal>(q.Select(x => x.Decimal)), Is.EqualTo(row1.Decimal));
    }
}