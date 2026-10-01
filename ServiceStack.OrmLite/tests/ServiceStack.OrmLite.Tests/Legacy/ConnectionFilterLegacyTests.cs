using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Legacy;
using ServiceStack.OrmLite.Tests.UseCases;

namespace ServiceStack.OrmLite.Tests.Legacy;

/// <summary>
/// The legacy typed APIs also apply a connection's mandatory filters. Complete SQL statements,
/// e.g. in ScalarFmt() and ColumnFmt(), and UpdateFmt() / DeleteFmt() with a table name aren't changed.
/// </summary>
[TestFixtureOrmLite]
public class ConnectionFilterLegacyTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    IDbConnection OpenForTenant(int tenantId)
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);
        return OpenDbConnection().ForTenant(tenantId);
    }

    void AssertOtherTenantsOrderIsUnchanged()
    {
        using var db = OpenDbConnection();
        var order = db.SingleById<TenantOrder>(4);
        Assert.That(order, Is.Not.Null);
        Assert.That(order.Total, Is.EqualTo(500m));
    }

#pragma warning disable 618
    [Test]
    public void Legacy_read_apis_are_filtered()
    {
        using var db = OpenForTenant(1);

        Assert.That(db.SelectFmt<TenantOrder>("Total > {0}", 0).Count, Is.EqualTo(3));
        Assert.That(db.SelectFmt<TenantOrder>("Total > {0} OR Total > {1}", 60, 400).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 3 }));
        Assert.That(db.SelectFmt<TenantOrder>(typeof(TenantOrder), "Total > {0}", 0).Count, Is.EqualTo(3));
        Assert.That(db.SelectLazyFmt<TenantOrder>("Total > {0}", 0).Count(), Is.EqualTo(3));
        Assert.That(db.SingleFmt<TenantOrder>("Id = {0}", 4), Is.Null);
        Assert.That(db.ExistsFmt<TenantOrder>("Id = {0}", 4), Is.False);
        Assert.That(db.ExistsFmt<TenantOrder>("Id = {0}", 1));

        Assert.That(db.Select<TenantOrder>(q => q.Where(x => x.Total > 0)).Count, Is.EqualTo(3));
        Assert.That(db.Single<TenantOrder>(q => q.Where(x => x.Id == 4)), Is.Null);
        Assert.That(db.Count<TenantOrder>(q => q.Where(x => x.Total > 0)), Is.EqualTo(3));
        Assert.That(db.Exists<TenantOrder>(q => q.Where(x => x.Id == 4)), Is.False);
        Assert.That(db.Select(db.SqlExpression<TenantOrder>()).Count, Is.EqualTo(3));
    }

    [Test]
    public void Legacy_write_apis_are_filtered()
    {
        using (var db = OpenForTenant(1))
        {
            Assert.That(db.UpdateFmt<TenantOrder>(set: "Total = 1", where: "Id = 1 OR Id = 4"), Is.EqualTo(1));
            Assert.That(db.UpdateOnly(new TenantOrder { Total = 2 }, q => q.Update(x => x.Total).Where(x => x.Id == 4)), Is.EqualTo(0));

            Assert.That(db.DeleteFmt<TenantOrder>("Id = {0}", 4), Is.EqualTo(0));
            Assert.That(db.DeleteFmt<TenantOrder>(where: "Id = 3 OR Id = 4"), Is.EqualTo(1));
            Assert.That(db.Delete<TenantOrder>(q => q.Where(x => x.Total > 400)), Is.EqualTo(0));
            Assert.That(db.Count<TenantOrder>(), Is.EqualTo(2));
        }

        AssertOtherTenantsOrderIsUnchanged();
    }

    [Test]
    public async Task Legacy_async_apis_are_filtered()
    {
        using (var db = OpenForTenant(1))
        {
            Assert.That((await db.SelectFmtAsync<TenantOrder>("Total > {0}", 0)).Count, Is.EqualTo(3));
            Assert.That(await db.SingleFmtAsync<TenantOrder>("Id = {0}", 4), Is.Null);
            Assert.That(await db.ExistsFmtAsync<TenantOrder>("Id = {0}", 4), Is.False);
            Assert.That((await db.SelectAsync<TenantOrder>(q => q.Where(x => x.Total > 0))).Count, Is.EqualTo(3));

            Assert.That(await db.UpdateFmtAsync<TenantOrder>(set: "Total = 1", where: "Id = 4"), Is.EqualTo(0));
            Assert.That(await db.DeleteFmtAsync<TenantOrder>("Id = {0}", 4), Is.EqualTo(0));
            Assert.That(await db.DeleteAsync<TenantOrder>(q => q.Where(x => x.Total > 400)), Is.EqualTo(0));
        }

        AssertOtherTenantsOrderIsUnchanged();
    }
#pragma warning restore 618
}
