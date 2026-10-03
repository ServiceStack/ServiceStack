using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

public class SharedNote
{
    public int Id { get; set; }
    public string OwnerId { get; set; } // null for notes everyone can see
    public string Text { get; set; }
}

/// <summary>
/// A FilterSet's filters are translated to SQL once, then each statement only reads their values from the scope and
/// adds them as db params. Conditions that only read the scope are decided before the SQL is generated.
/// </summary>
[TestFixtureOrmLite]
public class FilterSqlUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class TenantScope
    {
        public int? TenantId { get; set; }
        public bool IsAdmin { get; set; }
        public List<int> OrderIds { get; set; } = [];

        public int AssertTenantId() => TenantId ?? throw new InvalidOperationException("The tenant hasn't been resolved");
    }

    static readonly FilterSet<TenantScope> TenantFilters = FilterSet.Create<TenantScope>(f => {
        f.Ensure<TenantOrder>(x => x.TenantId, s => s.AssertTenantId());
        f.Filter<TenantCustomer>((x, s) => s.TenantId == null || x.TenantId == s.TenantId);
    });

    static string WhereOf(string sql)
    {
        var where = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        return where >= 0 ? sql.Substring(where) : "";
    }

    [Test]
    public void Filters_are_translated_to_SQL_once()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        SqlExpression<TenantOrder> q1, q2;
        using (var db1 = OpenDbConnection().UseFilters(TenantFilters.For(new TenantScope { TenantId = 1 })))
        {
            q1 = db1.From<TenantOrder>().Where(x => x.Total > 10);
            Assert.That(db1.Select(q1).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
        }
        using (var db2 = OpenDbConnection().UseFilters(TenantFilters.For(new TenantScope { TenantId = 2 })))
        {
            q2 = db2.From<TenantOrder>().Where(x => x.Total > 10);
            Assert.That(db2.Select(q2).Map(x => x.Id), Is.EquivalentTo(new[] { 4 }));
        }

        // The same SQL with the scope's values as db params
        Assert.That(q2.ToSelectStatement(), Is.EqualTo(q1.ToSelectStatement()));
        Assert.That(q1.Params[0].Value, Is.EqualTo(1));
        Assert.That(q2.Params[0].Value, Is.EqualTo(2));
        Assert.That(TenantFilters.NotCachedReasons, Is.Empty);
    }

    [Test]
    public void Conditions_that_only_read_the_scope_are_decided_before_the_SQL_is_generated()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var scope = new TenantScope();
        using var db = OpenDbConnection().UseFilters(TenantFilters.For(scope));

        // s.TenantId == null, so customers aren't filtered
        var all = db.From<TenantCustomer>();
        Assert.That(WhereOf(all.ToSelectStatement()), Is.Empty);
        Assert.That(db.Count(all), Is.EqualTo(3));

        // Otherwise only by the tenant, not with a condition for when it's null
        scope.TenantId = 1;
        var tenants = db.From<TenantCustomer>();
        Assert.That(WhereOf(tenants.ToSelectStatement()).ToLower(), Does.Not.Contain("null"));
        Assert.That(db.Select(tenants).Map(x => x.Name), Is.EquivalentTo(new[] { "Acme", "Globex" }));
        Assert.That(TenantFilters.NotCachedReasons, Is.Empty);
    }

    // Conditions that only read the scope are evaluated as C# would, so the rest isn't evaluated when it can't match
    static readonly FilterSet<TenantScope> ResolvedFilters = FilterSet.Create<TenantScope>(f => {
        f.Filter<TenantOrder>((x, s) => s.TenantId != null && x.TenantId == s.AssertTenantId());
        f.Filter<TenantCustomer>((x, s) => s.IsAdmin ? true : x.TenantId == s.AssertTenantId());
    });

    [Test]
    public void Conditions_that_only_read_the_scope_short_circuit()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var scope = new TenantScope();
        using var db = OpenDbConnection().UseFilters(ResolvedFilters.For(scope));

        Assert.That(db.Select<TenantOrder>(), Is.Empty);
        Assert.Throws<InvalidOperationException>(() => db.Select<TenantCustomer>());

        scope.IsAdmin = true;
        Assert.That(db.Count<TenantCustomer>(), Is.EqualTo(3));

        scope.TenantId = 2;
        scope.IsAdmin = false;
        Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EqualTo(new[] { 4 }));
        Assert.That(db.Select<TenantCustomer>().Map(x => x.Name), Is.EqualTo(new[] { "Initech" }));
        Assert.That(ResolvedFilters.NotCachedReasons, Is.Empty);
    }

    public static class Limits
    {
        public static decimal MaxTotal = 1000;
    }

    // Values that aren't read from the scope can change too, e.g. static fields, DateTime.UtcNow
    static readonly FilterSet SmallOrders = FilterSet.Create(f =>
        f.Filter<TenantOrder>(x => x.Total < Limits.MaxTotal));

    [Test]
    public void Values_that_can_change_are_read_for_each_statement()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using var db = OpenDbConnection().UseFilters(SmallOrders);
        try
        {
            Assert.That(db.Count<TenantOrder>(), Is.EqualTo(4));
            Limits.MaxTotal = 80;
            Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EquivalentTo(new[] { 2, 3 }));
            Assert.That(SmallOrders.NotCachedReasons, Is.Empty);
        }
        finally
        {
            Limits.MaxTotal = 1000;
        }
    }

    public class NoteScope
    {
        public string OwnerId { get; set; }
    }

    static readonly FilterSet<NoteScope> NoteFilters = FilterSet.Create<NoteScope>(f =>
        f.Filter<SharedNote>((x, s) => x.OwnerId == s.OwnerId));

    [Test]
    public void Null_values_have_their_own_SQL()
    {
        using (var seed = OpenDbConnection())
        {
            seed.DropAndCreateTable<SharedNote>();
            seed.InsertAll(new List<SharedNote> {
                new() { Id = 1, OwnerId = null, Text = "Everyone" },
                new() { Id = 2, OwnerId = "u1", Text = "Mine" },
                new() { Id = 3, OwnerId = "u2", Text = "Theirs" },
            });
        }

        var scope = new NoteScope();
        using var db = OpenDbConnection().UseFilters(NoteFilters.For(scope));
        Assert.That(db.Select<SharedNote>().Map(x => x.Id), Is.EqualTo(new[] { 1 }));

        scope.OwnerId = "u1";
        Assert.That(db.Select<SharedNote>().Map(x => x.Id), Is.EqualTo(new[] { 2 }));

        scope.OwnerId = null;
        Assert.That(db.Select<SharedNote>().Map(x => x.Id), Is.EqualTo(new[] { 1 }));
        Assert.That(NoteFilters.NotCachedReasons, Is.Empty);
    }

    // A db param for each value of a collection, so its SQL changes with the collection's size
    static readonly FilterSet<TenantScope> OrderIdFilters = FilterSet.Create<TenantScope>(f =>
        f.Filter<TenantOrder>((x, s) => s.OrderIds.Contains(x.Id)));

    [Test]
    public void Filters_whose_SQL_cant_be_reused_are_translated_for_each_statement()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var scope = new TenantScope { OrderIds = [1, 2] };
        using var db = OpenDbConnection().UseFilters(OrderIdFilters.For(scope));
        Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EquivalentTo(new[] { 1, 2 }));

        scope.OrderIds = [2, 3, 4];
        Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EquivalentTo(new[] { 2, 3, 4 }));

        Assert.That(OrderIdFilters.NotCachedReasons.Single(), Does.StartWith("Filter TenantOrder for TenantOrder: "));
    }

    [Test]
    public void Filters_of_joined_tables_and_writes_reuse_their_SQL()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        using var db = OpenDbConnection().UseFilters(TenantFilters.For(new TenantScope { TenantId = 1 }));
        var q = db.From<TenantOrder>()
            .Join<TenantCustomer>((o, c) => o.CustomerId == c.Id)
            .Where<TenantCustomer>(c => c.Name != "Acme");
        Assert.That(db.Select(q).Map(x => x.Id), Is.EquivalentTo(new[] { 2, 3 }));

        Assert.That(db.UpdateOnly(() => new TenantOrder { Total = 1 }, where: x => x.Total > 60), Is.EqualTo(2));
        Assert.That(db.Delete<TenantOrder>(x => x.Total == 1), Is.EqualTo(2));
        Assert.That(db.Count<TenantOrder>(), Is.EqualTo(1));
        Assert.That(TenantFilters.NotCachedReasons, Is.Empty);
    }
}
