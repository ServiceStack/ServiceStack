using System;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class SelectScalarTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class TestPerson
    {
        public Guid Id { get; set; }
        public long Long { get; set; }
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
    }

    [Test]
    public void Should_Return_Scalar_Value()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TestPerson>();

        var row = new TestPerson
        {
            Id = Guid.NewGuid(),
            Long = 1,
            Decimal = 1.1M,
            Double = 1.1,
            Float = 1.1f,
        };
        db.Insert(row);

        var q = db.From<TestPerson>().Where(x => x.Id == row.Id);

        Assert.That(db.Scalar<Guid>(q.Select(x => x.Id)), Is.EqualTo(row.Id));
        Assert.That(db.Scalar<long>(q.Select(x => x.Long)), Is.EqualTo(row.Long));
        Assert.That(db.Scalar<decimal>(q.Select(x => x.Decimal)), Is.EqualTo(row.Decimal));
        Assert.That(db.Scalar<double>(q.Select(x => x.Double)), Is.EqualTo(row.Double).Within(.1d));
        Assert.That(db.Scalar<float>(q.Select(x => x.Float)), Is.EqualTo(row.Float));
    }
}