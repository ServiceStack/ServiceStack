using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

public interface IHasTenantId
{
    int TenantId { get; }
}

public class TenantCustomer : IHasTenantId
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; }
}

public class TenantOrder : IHasTenantId
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CustomerId { get; set; }
    public decimal Total { get; set; }
    public bool IsDeleted { get; set; }
}

public static class Tenants
{
    public static void Seed(IDbConnection db)
    {
        db.DropAndCreateTable<TenantCustomer>();
        db.DropAndCreateTable<TenantOrder>();
        db.InsertAll(new List<TenantCustomer> {
            new() { Id = 1, TenantId = 1, Name = "Acme" },
            new() { Id = 2, TenantId = 1, Name = "Globex" },
            new() { Id = 3, TenantId = 2, Name = "Initech" },
        });
        db.InsertAll(new List<TenantOrder> {
            new() { Id = 1, TenantId = 1, CustomerId = 1, Total = 100 },
            new() { Id = 2, TenantId = 1, CustomerId = 2, Total = 50, IsDeleted = true },
            new() { Id = 3, TenantId = 1, CustomerId = 2, Total = 75 },
            new() { Id = 4, TenantId = 2, CustomerId = 3, Total = 500 },
        });
    }

    /// <summary>
    /// e.g. an app's extension method to open a connection for a tenant
    /// </summary>
    public static IDbConnection ForTenant(this IDbConnection db, int tenantId) =>
        db.EnsureFilter<IHasTenantId>(x => x.TenantId == tenantId);
}

/// <summary>
/// Mandatory filters registered on a connection are applied to every query created for a table they apply to, either
/// the table itself or an interface it implements, e.g. to only return a tenant's rows.
/// </summary>
[TestFixtureOrmLite]
public class ConnectionFilterUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Filter_every_table_implementing_an_interface()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using var db = OpenDbConnection().ForTenant(1);

        Assert.That(db.Select(db.From<TenantOrder>()).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
        Assert.That(db.Select(db.From<TenantCustomer>()).Map(x => x.Name), Is.EquivalentTo(new[] { "Acme", "Globex" }));
        Assert.That(db.Count(db.From<TenantOrder>()), Is.EqualTo(3));
    }

    [Test]
    public void Filter_a_table_and_combine_filters()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using var db = OpenDbConnection().ForTenant(1);
        db.EnsureFilter<TenantOrder>(x => !x.IsDeleted);

        Assert.That(db.Select(db.From<TenantOrder>()).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 3 }));
        // The soft delete filter only applies to TenantOrder
        Assert.That(db.Select(db.From<TenantCustomer>()).Count, Is.EqualTo(2));
    }

    [Test]
    public void Filter_with_a_function_for_filters_that_change()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var tenantId = 1;
        var isAdmin = false;

        using var db = OpenDbConnection();
        // The function is called for each statement, no filter is applied when it returns null
        db.EnsureFilter<IHasTenantId>(() => {
            if (isAdmin)
                return null;
            return x => x.TenantId == tenantId;
        });
        Assert.That(db.Count<TenantOrder>(), Is.EqualTo(3));
        Assert.That(db.SingleById<TenantOrder>(4), Is.Null);

        tenantId = 2;
        Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EqualTo(new[] { 4 }));

        isAdmin = true;
        Assert.That(db.Count<TenantOrder>(), Is.EqualTo(4));
        Assert.That(db.Count<TenantCustomer>(), Is.EqualTo(3));
    }

    [Test]
    public void Captured_values_of_a_filter_are_read_for_each_statement()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var tenantId = 1;
        using var db = OpenDbConnection().EnsureFilter<IHasTenantId>(x => x.TenantId == tenantId);
        Assert.That(db.Count<TenantOrder>(), Is.EqualTo(3));

        tenantId = 2;
        Assert.That(db.Count<TenantOrder>(), Is.EqualTo(1));
    }

    [Test]
    public void Other_conditions_cant_widen_the_filter()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using var db = OpenDbConnection().ForTenant(1);

        // e.g. user-supplied conditions only filter the tenant's rows
        var q = db.From<TenantOrder>().Where(x => x.Total > 60).Or(x => x.Total > 400);
        Assert.That(db.Select(q).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 3 }));

        var fmt = db.From<TenantOrder>().Where(Sql.Fmt($"1 = {1}")).Or(Sql.Fmt($"1 = {1}"));
        Assert.That(db.Select(fmt).All(x => x.TenantId == 1));

        // Clearing the WHERE conditions keeps the filter
        var cleared = db.From<TenantOrder>().Where(x => x.Total > 60);
        cleared.Where();
        cleared.Where(x => x.Total > 0);
        Assert.That(db.Select(cleared).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Filters_stay_unambiguous_in_queries_with_joins_and_aliases()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using var db = OpenDbConnection().ForTenant(1);

        // Both tables have a TenantId column
        var q = db.From<TenantOrder>()
            .Join<TenantCustomer>((o, c) => o.CustomerId == c.Id)
            .Where<TenantCustomer>(c => c.Name == "Globex");
        Assert.That(db.Select(q).Map(x => x.Id), Is.EquivalentTo(new[] { 2, 3 }));

        var aliased = db.From<TenantOrder>(db.TableAlias("o")).Where(x => x.Total > 60);
        Assert.That(db.Select(aliased).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 3 }));

        // Changing the alias after the query is created updates the filter
        var realiased = db.From<TenantOrder>();
        realiased.SetTableAlias("o").Where(x => x.Total > 60);
        Assert.That(db.Select(realiased).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 3 }));
    }

    [Test]
    public void Sub_queries_and_set_operations_are_filtered()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using var db = OpenDbConnection().ForTenant(1);

        // Orders of customers named Initech: the tenant can't see that customer
        var orders = db.Select(db.From<TenantOrder>()
            .Where(x => Sql.In(x.CustomerId, db.From<TenantCustomer>().Where(c => c.Name == "Initech").Select(c => c.Id))));
        Assert.That(orders, Is.Empty);

        var totals = db.Column<decimal>(db.From<TenantOrder>().Where(x => x.Total > 90).Select(x => x.Total)
            .Union(db.From<TenantOrder>().Where(x => x.Total < 60).Select(x => x.Total)));
        Assert.That(totals, Is.EquivalentTo(new[] { 100m, 50m }));
    }

    [Test]
    public void Filters_only_apply_to_their_connection()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using (var db = OpenDbConnection().ForTenant(2))
        {
            Assert.That(db.Select(db.From<TenantOrder>()).Map(x => x.Id), Is.EqualTo(new[] { 4 }));

            // A nested open of the same shared connection, e.g. SQLite :memory:, is in the same scope
            using var nested = OpenDbConnection();
            if (ReferenceEquals(nested, db))
                Assert.That(nested.Select(nested.From<TenantOrder>()).Count, Is.EqualTo(1));
        }

        // Connections opened later aren't filtered, including shared connections
        using var other = OpenDbConnection();
        Assert.That(other.Select(other.From<TenantOrder>()).Count, Is.EqualTo(4));
    }

    [Test]
    public void WithoutFilters_for_admin_tasks()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using var db = OpenDbConnection().ForTenant(1);

        // The same connection without its filters, e.g. for a report across tenants
        using (var adminDb = db.WithoutFilters())
        {
            Assert.That(adminDb.Count<TenantOrder>(), Is.EqualTo(4));
            Assert.That(adminDb.SingleById<TenantOrder>(4), Is.Not.Null);
            Assert.That(adminDb.Select(adminDb.From<TenantOrder>().Where(x => x.Total > 400)).Count, Is.EqualTo(1));
        }

        // Disposing it doesn't close the connection, which is still filtered
        Assert.That(db.Count<TenantOrder>(), Is.EqualTo(3));
        Assert.That(db.SingleById<TenantOrder>(4), Is.Null);
    }

    [Test]
    public void WithoutFilters_shares_the_connections_transaction()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using var db = OpenDbConnection().ForTenant(1);
        var adminDb = db.WithoutFilters();

        using (var trans = db.OpenTransaction())
        {
            // Changes with and without filters are in the same transaction
            Assert.That(db.UpdateOnly(() => new TenantOrder { Total = 1 }), Is.EqualTo(3));
            Assert.That(adminDb.UpdateOnly(() => new TenantOrder { Total = 2 }, where: x => x.Id == 4), Is.EqualTo(1));
            Assert.That(adminDb.Select<TenantOrder>(x => x.Total <= 2).Count, Is.EqualTo(4));
            trans.Rollback();
        }
        Assert.That(adminDb.Select<TenantOrder>(x => x.Total <= 2), Is.Empty);

        // A transaction can also be opened from the unfiltered connection
        using (var trans = adminDb.OpenTransaction())
        {
            Assert.That(db.UpdateOnly(() => new TenantOrder { Total = 1 }), Is.EqualTo(3));
            trans.Commit();
        }
        Assert.That(adminDb.Select<TenantOrder>(x => x.Total == 1).Count, Is.EqualTo(3));
    }

    [Test]
    public async Task Async_APIs()
    {
        using (var seed = await OpenDbConnectionAsync())
            Tenants.Seed(seed);

        using var db = (await OpenDbConnectionAsync()).ForTenant(2);
        var orders = await db.SelectAsync(db.From<TenantOrder>());
        Assert.That(orders.Map(x => x.Id), Is.EqualTo(new[] { 4 }));
        Assert.That(await db.CountAsync(db.From<TenantCustomer>()), Is.EqualTo(1));
    }

    [Test]
    public void Invalid_filters_throw()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using var db = OpenDbConnection();
        // Interface filters can only use the interface's properties
        db.EnsureFilter<IHasTenantId>(x => x.ToString() == "1");
        Assert.Throws<NotSupportedException>(() => db.From<TenantOrder>());
    }
}
