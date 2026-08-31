using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.Tests;

[TestFixture]
[NonParallelizable]
public class PortableUpsertTests : PortableUpsertTestsBase
{
    private class FallbackSqliteDialectProvider : SqliteOrmLiteDialectProvider
    {
        public override bool SupportsUpsert => false;
    }

    protected override string NativeUpsertSqlFragment => "ON CONFLICT";

    protected override IDbConnection OpenUpsertDbConnection() =>
        new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider).OpenDbConnection();

    [Test]
    public void Can_fallback_to_primary_key_exists_then_insert_or_update()
    {
        using var db = new OrmLiteConnectionFactory(":memory:", new FallbackSqliteDialectProvider())
            .OpenDbConnection();
        db.CreateTable<UpsertCustomer>();

        var customer = new UpsertCustomer
        {
            Id = 1,
            Name = "Initial",
            Email = "initial@example.org",
        };
        db.Upsert(customer);

        customer.Name = "Updated";
        customer.Email = "should-not-update@example.org";
        db.Upsert(customer, updateOnly: x => new { x.Name });

        var saved = db.SingleById<UpsertCustomer>(customer.Id);
        Assert.Multiple(() =>
        {
            Assert.That(db.Count<UpsertCustomer>(), Is.EqualTo(1));
            Assert.That(saved.Name, Is.EqualTo("Updated"));
            Assert.That(saved.Email, Is.EqualTo("initial@example.org"));
        });
    }
}
