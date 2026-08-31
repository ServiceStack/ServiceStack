using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.MySql.Tests;

[TestFixture]
[NonParallelizable]
public class PortableUpsertTests : PortableUpsertTestsBase
{
    protected override string NativeUpsertSqlFragment => "ON DUPLICATE KEY UPDATE";

    protected override IDbConnection OpenUpsertDbConnection() =>
        new OrmLiteConnectionFactory(MySqlConfig.ConnectionString, MySqlConfig.DialectProvider).OpenDbConnection();

    [Test]
    public void Can_disable_native_upsert_for_strict_primary_key_matching()
    {
        var dialect = MySqlDialect.Instance;
        var previousUseNativeUpsert = dialect.UseNativeUpsert;
        try
        {
            MySqlDialect.Instance.UseNativeUpsert = false;

            using var db = new OrmLiteConnectionFactory(MySqlConfig.ConnectionString, dialect).OpenDbConnection();
            db.DropAndCreateTable<UpsertCustomer>();

            db.Upsert(new UpsertCustomer { Id = 1, Name = "Initial", Email = "initial@example.org" });
            db.Upsert(new UpsertCustomer { Id = 1, Name = "Updated", Email = "ignored@example.org" },
                updateOnly: x => new { x.Name });

            var saved = db.SingleById<UpsertCustomer>(1);
            Assert.Multiple(() =>
            {
                Assert.That(dialect.SupportsUpsert, Is.False);
                Assert.That(saved.Name, Is.EqualTo("Updated"));
                Assert.That(saved.Email, Is.EqualTo("initial@example.org"));
            });
        }
        finally
        {
            dialect.UseNativeUpsert = previousUseNativeUpsert;
        }
    }
}
