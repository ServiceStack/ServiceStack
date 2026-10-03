using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

public class TenantNote : IHasTenantId
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Text { get; set; }

    [RowVersion]
    public ulong RowVersion { get; set; }
}

/// <summary>
/// A connection's mandatory filters also apply to every typed update and delete API, so a filtered connection can't
/// change rows it can't see. Rows that don't match the filter are treated like rows that don't exist.
/// </summary>
[TestFixtureOrmLite]
public class ConnectionFilterWriteUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    const string NoReturning = "MySQL doesn't support returning rows from UPDATE and DELETE statements";

    IDbConnection OpenForTenant(int tenantId)
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);
        return OpenDbConnection().ForTenant(tenantId);
    }

    // Every tenant's orders, read after the filtered connection is closed
    Dictionary<int, TenantOrder> AllOrders()
    {
        using var db = OpenDbConnection();
        return db.Select<TenantOrder>().ToDictionary(x => x.Id);
    }

    void AssertOtherTenantsOrderIsUnchanged(Dictionary<int, TenantOrder> orders)
    {
        Assert.That(orders[4].TenantId, Is.EqualTo(2));
        Assert.That(orders[4].CustomerId, Is.EqualTo(3));
        Assert.That(orders[4].Total, Is.EqualTo(500m));
        Assert.That(orders[4].IsDeleted, Is.False);
    }

    [Test]
    public void Updating_objects_only_updates_rows_matching_the_filter()
    {
        using (var db = OpenForTenant(1))
        {
            Assert.That(db.Update(new TenantOrder { Id = 1, TenantId = 1, CustomerId = 1, Total = 110 }), Is.EqualTo(1));

            // Another tenant's order isn't updated, the same as an order that doesn't exist
            Assert.That(db.Update(new TenantOrder { Id = 4, TenantId = 1, CustomerId = 1, Total = 1 }), Is.EqualTo(0));

            Assert.That(db.UpdateAll(new[] {
                new TenantOrder { Id = 3, TenantId = 1, CustomerId = 2, Total = 80 },
                new TenantOrder { Id = 4, TenantId = 1, CustomerId = 1, Total = 1 },
            }), Is.EqualTo(1));
        }

        var orders = AllOrders();
        Assert.That(orders[1].Total, Is.EqualTo(110m));
        Assert.That(orders[3].Total, Is.EqualTo(80m));
        AssertOtherTenantsOrderIsUnchanged(orders);
    }

    [Test]
    public void Update_expressions_only_update_rows_matching_the_filter()
    {
        using (var db = OpenForTenant(1))
        {
            // Without a where condition only the tenant's orders are updated
            Assert.That(db.UpdateOnly(() => new TenantOrder { CustomerId = 1 }), Is.EqualTo(3));

            // Other conditions can't widen the filter
            Assert.That(db.UpdateOnly(() => new TenantOrder { Total = 1 }, where: x => x.Id == 1 || x.Id == 4), Is.EqualTo(1));
            Assert.That(db.UpdateOnly(() => new TenantOrder { Total = 2 }, db.From<TenantOrder>().Where(x => x.Total > 400)), Is.EqualTo(0));
            Assert.That(db.UpdateOnly(() => new TenantOrder { Total = 3 }, "WHERE Id = 3 OR Id = 4", Array.Empty<IDbDataParameter>()), Is.EqualTo(1));
            Assert.That(db.UpdateAdd(() => new TenantOrder { Total = 10 }, where: x => x.Id == 2 || x.Id == 4), Is.EqualTo(1));

            Assert.That(db.UpdateNonDefaults(new TenantOrder { Total = 4 }, x => x.Id == 4), Is.EqualTo(0));
            Assert.That(db.Update<TenantOrder>(new { Total = 5 }, x => x.Id == 4), Is.EqualTo(0));
            Assert.That(db.Update(new TenantOrder { Id = 4, TenantId = 1, Total = 6 }, x => x.Id == 4), Is.EqualTo(0));
            Assert.That(db.UpdateOnlyFields(new TenantOrder { Total = 7 }, onlyFields: x => x.Total, where: x => x.Id == 4), Is.EqualTo(0));
            Assert.That(db.UpdateOnly<TenantOrder>(new Dictionary<string, object> { ["Id"] = 4, ["Total"] = 8m }), Is.EqualTo(0));
            Assert.That(db.UpdateOnly<TenantOrder>(new Dictionary<string, object> { ["Total"] = 9m }, x => x.Id == 4), Is.EqualTo(0));
        }

        var orders = AllOrders();
        Assert.That(orders[1].Total, Is.EqualTo(1m));
        Assert.That(orders[2].Total, Is.EqualTo(60m));
        Assert.That(orders[3].Total, Is.EqualTo(3m));
        Assert.That(new[] { 1, 2, 3 }.All(id => orders[id].CustomerId == 1));
        AssertOtherTenantsOrderIsUnchanged(orders);
    }

    [Test]
    public void Soft_deleted_rows_cant_be_changed()
    {
        using (var db = OpenForTenant(1))
        {
            db.UseFilters(FilterSet.Create(f => f.Filter<TenantOrder>(x => !x.IsDeleted)));

            // Order 2 is already deleted
            Assert.That(db.UpdateOnly(() => new TenantOrder { Total = 0 }), Is.EqualTo(2));

            // Soft delete an order, which the connection can no longer see or change
            Assert.That(db.UpdateOnly(() => new TenantOrder { IsDeleted = true }, where: x => x.Id == 1), Is.EqualTo(1));
            Assert.That(db.SingleById<TenantOrder>(1), Is.Null);
            Assert.That(db.UpdateOnly(() => new TenantOrder { IsDeleted = false }, where: x => x.Id == 1), Is.EqualTo(0));
            Assert.That(db.DeleteById<TenantOrder>(1), Is.EqualTo(0));
        }

        var orders = AllOrders();
        Assert.That(orders[1].IsDeleted);
        Assert.That(orders[2].Total, Is.EqualTo(50m));
        Assert.That(orders[3].Total, Is.EqualTo(0m));
        AssertOtherTenantsOrderIsUnchanged(orders);
    }

    [Test]
    public void Deletes_only_delete_rows_matching_the_filter()
    {
        using (var db = OpenForTenant(1))
        {
            Assert.That(db.DeleteById<TenantOrder>(4), Is.EqualTo(0));
            Assert.That(db.DeleteByIds<TenantOrder>(new[] { 3, 4 }), Is.EqualTo(1));
            Assert.That(db.Delete<TenantOrder>(x => x.Id == 4 || x.Total > 90), Is.EqualTo(1));

            Assert.That(db.Delete(db.From<TenantOrder>().Where(x => x.Id == 4)), Is.EqualTo(0));
            Assert.That(db.Delete<TenantOrder>(new { Id = 4 }), Is.EqualTo(0));
            Assert.That(db.DeleteNonDefaults(new TenantOrder { Id = 4 }), Is.EqualTo(0));
            Assert.That(db.DeleteAll(new[] { new TenantOrder { Id = 4 } }), Is.EqualTo(0));
            Assert.That(db.Delete<TenantOrder>("Total > @min", new { min = 400 }), Is.EqualTo(0));
            Assert.That(db.DeleteWhere<TenantOrder>("Total > {0}", [400]), Is.EqualTo(0));

            // Deleting all rows only deletes the tenant's remaining order
            Assert.That(db.DeleteAll<TenantOrder>(), Is.EqualTo(1));
        }

        var orders = AllOrders();
        Assert.That(orders.Keys, Is.EquivalentTo(new[] { 4 }));
        AssertOtherTenantsOrderIsUnchanged(orders);
    }

    [Test]
    public void Save_and_Upsert_cant_overwrite_rows_that_dont_match_the_filter()
    {
        using (var db = OpenForTenant(1))
        {
            // Existing orders are updated and new orders are inserted
            Assert.That(db.Save(new TenantOrder { Id = 1, TenantId = 1, CustomerId = 1, Total = 120 }), Is.False);
            Assert.That(db.Save(new TenantOrder { Id = 5, TenantId = 1, CustomerId = 1, Total = 10 }), Is.True);
            db.Upsert(new TenantOrder { Id = 3, TenantId = 1, CustomerId = 2, Total = 85 });
            db.Upsert(new TenantOrder { Id = 6, TenantId = 1, CustomerId = 2, Total = 20 });

            // Another tenant's order isn't seen as an existing row, so it's inserted, which fails on its primary key
            Assert.That(() => db.Save(new TenantOrder { Id = 4, TenantId = 1, CustomerId = 1, Total = 1 }), Throws.Exception);
            Assert.That(() => db.Upsert(new TenantOrder { Id = 4, TenantId = 1, CustomerId = 1, Total = 1 }), Throws.Exception);
        }

        var orders = AllOrders();
        Assert.That(orders.Keys, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(orders[1].Total, Is.EqualTo(120m));
        Assert.That(orders[3].Total, Is.EqualTo(85m));
        AssertOtherTenantsOrderIsUnchanged(orders);
    }

    [Test]
    public void Row_version_updates_of_rows_that_dont_match_the_filter_throw()
    {
        TenantNote otherTenantsNote;
        using (var seed = OpenDbConnection())
        {
            seed.DropAndCreateTable<TenantNote>();
            seed.Insert(new TenantNote { Id = 1, TenantId = 1, Text = "Mine" });
            seed.Insert(new TenantNote { Id = 2, TenantId = 2, Text = "Theirs" });
            otherTenantsNote = seed.SingleById<TenantNote>(2);
        }

        using (var db = OpenDbConnection().ForTenant(1))
        {
            var note = db.SingleById<TenantNote>(1);
            note.Text = "Updated";
            Assert.That(db.Update(note), Is.EqualTo(1));

            // No row is updated or deleted, which optimistic concurrency reports as a conflict
            otherTenantsNote.Text = "Overwritten";
            Assert.Throws<OptimisticConcurrencyException>(() => db.Update(otherTenantsNote));
            Assert.Throws<OptimisticConcurrencyException>(() => db.DeleteById<TenantNote>(2, otherTenantsNote.RowVersion));

            // Its row version can't be read either
            Assert.That(db.GetRowVersion<TenantNote>(1), Is.Not.EqualTo(0));
            Assert.That(db.GetRowVersion<TenantNote>(2), Is.EqualTo(0));
        }

        using var all = OpenDbConnection();
        Assert.That(all.SingleById<TenantNote>(1).Text, Is.EqualTo("Updated"));
        Assert.That(all.SingleById<TenantNote>(2).Text, Is.EqualTo("Theirs"));
    }

    [Test]
    [IgnoreDialect(Dialect.AnyMySql, NoReturning)]
    public void Returning_apis_only_change_and_return_rows_matching_the_filter()
    {
        using (var db = OpenForTenant(1))
        {
            var updated = db.UpdateOnlyReturning(() => new TenantOrder { Total = 1 }, where: x => x.Total > 90);
            Assert.That(updated.Map(x => x.Id), Is.EqualTo(new[] { 1 }));

            var deleted = db.DeleteReturning<TenantOrder>(x => x.Total > 0);
            Assert.That(deleted.Map(x => x.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        var orders = AllOrders();
        Assert.That(orders.Keys, Is.EquivalentTo(new[] { 4 }));
        AssertOtherTenantsOrderIsUnchanged(orders);
    }

    [Test]
    public void UpdateFrom_only_updates_rows_matching_the_filter()
    {
        using (var db = OpenForTenant(1))
        {
            // Every order that isn't Acme's, which for this tenant is only Globex's orders
            var q = db.From<TenantOrder>()
                .Join<TenantCustomer>((o, c) => o.CustomerId == c.Id)
                .Where<TenantCustomer>(c => c.Name != "Acme");

            Assert.That(db.UpdateFrom(o => new TenantOrder { Total = 0 }, q), Is.EqualTo(2));
        }

        var orders = AllOrders();
        Assert.That(orders[1].Total, Is.EqualTo(100m));
        Assert.That(orders[2].Total, Is.EqualTo(0m));
        Assert.That(orders[3].Total, Is.EqualTo(0m));
        AssertOtherTenantsOrderIsUnchanged(orders);
    }

    [Test]
    public void Complete_SQL_statements_arent_changed()
    {
        using (var db = OpenForTenant(1))
        {
            // Raw SQL is the app's responsibility
            var table = db.GetQuotedTableName<TenantOrder>();
            Assert.That(db.Delete<TenantOrder>($"DELETE FROM {table}", anonType: null), Is.EqualTo(4));
        }

        Assert.That(AllOrders(), Is.Empty);
    }

    [Test]
    public async Task Async_apis_are_filtered()
    {
        using (var db = OpenForTenant(1))
        {
            Assert.That(await db.UpdateAsync(new TenantOrder { Id = 4, TenantId = 1, CustomerId = 1, Total = 1 }), Is.EqualTo(0));
            Assert.That(await db.UpdateOnlyAsync(() => new TenantOrder { Total = 2 }, where: x => x.Id == 1 || x.Id == 4), Is.EqualTo(1));
            Assert.That(await db.UpdateAddAsync(() => new TenantOrder { Total = 10 }, where: x => x.Id == 4), Is.EqualTo(0));
            Assert.That(await db.DeleteByIdAsync<TenantOrder>(4), Is.EqualTo(0));
            Assert.That(await db.DeleteByIdsAsync<TenantOrder>(new[] { 3, 4 }), Is.EqualTo(1));
            Assert.That(await db.DeleteAsync<TenantOrder>(x => x.Total > 400), Is.EqualTo(0));
            Assert.That(await db.SaveAsync(new TenantOrder { Id = 2, TenantId = 1, CustomerId = 2, Total = 55 }), Is.False);
            Assert.That(async () => await db.UpsertAsync(new TenantOrder { Id = 4, TenantId = 1, Total = 1 }), Throws.Exception);
            Assert.That(await db.DeleteAllAsync<TenantOrder>(), Is.EqualTo(2));
        }

        var orders = AllOrders();
        Assert.That(orders.Keys, Is.EquivalentTo(new[] { 4 }));
        AssertOtherTenantsOrderIsUnchanged(orders);
    }
}
