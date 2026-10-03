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
    /// Only the rows of the tenant, declared once and used by each connection with the tenant's id
    /// </summary>
    public static readonly FilterSet<int> TenantFilters = FilterSet.Create<int>(f =>
        f.Filter<IHasTenantId>((x, tenantId) => x.TenantId == tenantId));

    /// <summary>
    /// Orders that haven't been deleted, without a scope
    /// </summary>
    public static readonly FilterSet SoftDeletes = FilterSet.Create(f =>
        f.Filter<TenantOrder>(x => !x.IsDeleted));

    /// <summary>
    /// e.g. an app's extension method to open a connection for a tenant
    /// </summary>
    public static IDbConnection ForTenant(this IDbConnection db, int tenantId) =>
        db.UseFilters(TenantFilters.For(tenantId));
}

/// <summary>
/// The mandatory filters of the FilterSets a connection uses are applied to every query created for a table they apply
/// to, either the table itself or an interface it implements, e.g. to only return a tenant's rows.
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
        db.UseFilters(Tenants.SoftDeletes);

        Assert.That(db.Select(db.From<TenantOrder>()).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 3 }));
        // The soft delete filter only applies to TenantOrder
        Assert.That(db.Select(db.From<TenantCustomer>()).Count, Is.EqualTo(2));
    }

    public class TenantAccess
    {
        public int TenantId { get; set; }
        public bool IsAdmin { get; set; }
    }

    // Conditions that only read the scope decide if the rest of the filter applies
    static readonly FilterSet<TenantAccess> AccessFilters = FilterSet.Create<TenantAccess>(f =>
        f.Filter<IHasTenantId>((x, s) => s.IsAdmin || x.TenantId == s.TenantId));

    [Test]
    public void Filters_read_the_scope_for_each_statement()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var access = new TenantAccess { TenantId = 1 };
        using var db = OpenDbConnection().UseFilters(AccessFilters.For(access));
        Assert.That(db.Count<TenantOrder>(), Is.EqualTo(3));
        Assert.That(db.SingleById<TenantOrder>(4), Is.Null);

        access.TenantId = 2;
        Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EqualTo(new[] { 4 }));

        access.IsAdmin = true;
        Assert.That(db.Count<TenantOrder>(), Is.EqualTo(4));
        Assert.That(db.Count<TenantCustomer>(), Is.EqualTo(3));
    }

    public class TenantScope
    {
        public int? TenantId { get; set; }
        public int Calls { get; private set; }

        public int AssertTenantId()
        {
            Calls++;
            return TenantId ?? throw new InvalidOperationException("The tenant hasn't been resolved");
        }
    }

    static readonly FilterSet<TenantScope> ScopeFilters = FilterSet.Create<TenantScope>(f =>
        f.Filter<IHasTenantId>((x, s) => x.TenantId == s.AssertTenantId()));

    [Test]
    public void Filter_can_throw_until_its_tenant_is_known()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var scope = new TenantScope();
        using var db = OpenDbConnection().UseFilters(ScopeFilters.For(scope));

        // The exception of a method a filter calls is thrown as is, and the method is only called once
        var ex = Assert.Throws<InvalidOperationException>(() => db.Select<TenantOrder>());
        Assert.That(ex.Message, Is.EqualTo("The tenant hasn't been resolved"));
        Assert.That(scope.Calls, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => db.SingleById<TenantOrder>(1));
        Assert.Throws<InvalidOperationException>(() => db.Select<TenantOrder>(x => x.Total > 10));
        Assert.Throws<InvalidOperationException>(() => db.Delete<TenantOrder>(x => x.Id == 1));

        scope.TenantId = 2;
        Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EqualTo(new[] { 4 }));
    }

    [Test]
    public void Rules_cant_use_variables_from_outside_the_rule()
    {
        // Values are read from the scope, so a FilterSet is the same for every connection that uses it
        var tenantId = 1;
        var ex = Assert.Throws<ArgumentException>(() => FilterSet.Create<int>(f =>
            f.Filter<IHasTenantId>((x, s) => x.TenantId == tenantId)));
        Assert.That(ex.Message, Does.Contain("'tenantId'"));

        Assert.Throws<ArgumentException>(() => FilterSet.Create(f => f.Filter<IHasTenantId>(x => x.TenantId == tenantId)));
        Assert.Throws<ArgumentException>(() => FilterSet.Create<TenantScope>(f =>
            f.Ensure<IHasTenantId>(x => x.TenantId, s => tenantId)));
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
    public void Using_a_FilterSet_again_with_the_same_scope_is_ignored()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        // e.g. when a connection is configured more than once
        using var db = OpenDbConnection().ForTenant(1).ForTenant(1);
        db.UseFilters(Tenants.SoftDeletes);
        db.UseFilters(Tenants.SoftDeletes);

        var filters = db.GetFilters();
        Assert.That(db.From<TenantOrder>().Params.Count, Is.EqualTo(1));
        Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EquivalentTo(new[] { 1, 3 }));

        db.ForTenant(1);
        Assert.That(db.GetFilters(), Is.SameAs(filters));

        // With a different scope rows would need to match both
        Assert.Throws<InvalidOperationException>(() => db.ForTenant(2));
        Assert.That(db.GetFilters(), Is.SameAs(filters));
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

        // Interface filters can only use the interface's properties
        var filters = FilterSet.Create(f => f.Filter<IHasTenantId>(x => x.ToString() == "1"));
        using var db = OpenDbConnection().UseFilters(filters);
        Assert.Throws<NotSupportedException>(() => db.From<TenantOrder>());
    }
}
