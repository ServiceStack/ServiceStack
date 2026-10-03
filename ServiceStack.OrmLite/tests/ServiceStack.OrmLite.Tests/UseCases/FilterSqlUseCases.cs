using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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

    // A db param for each value of a collection, so it has SQL for each size of the collection
    static readonly FilterSet<TenantScope> OrderIdFilters = FilterSet.Create<TenantScope>(f =>
        f.Filter<TenantOrder>((x, s) => s.OrderIds.Contains(x.Id)));

    [Test]
    public void Collections_have_SQL_for_each_size()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var scope = new TenantScope { OrderIds = [1, 2] };
        using var db = OpenDbConnection().UseFilters(OrderIdFilters.For(scope));
        Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EquivalentTo(new[] { 1, 2 }));

        scope.OrderIds = [2, 3, 4];
        Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EquivalentTo(new[] { 2, 3, 4 }));
        scope.OrderIds = [3, 4];
        Assert.That(db.Select<TenantOrder>().Map(x => x.Id), Is.EquivalentTo(new[] { 3, 4 }));
        scope.OrderIds = [];
        Assert.That(db.Select<TenantOrder>(), Is.Empty);
        Assert.That(OrderIdFilters.NotCachedReasons, Is.Empty);
    }

    public class NameScope
    {
        public string Prefix { get; set; }
    }

    // LIKE has an ESCAPE when the text has wildcards, so its SQL changes with the value
    static readonly FilterSet<NameScope> NameFilters = FilterSet.Create<NameScope>(f =>
        f.Filter<TenantCustomer>((x, s) => x.Name.StartsWith(s.Prefix)));

    [Test]
    public void Filters_whose_SQL_cant_be_reused_are_translated_for_each_statement()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var scope = new NameScope { Prefix = "G" };
        using var db = OpenDbConnection().UseFilters(NameFilters.For(scope));
        Assert.That(db.Select<TenantCustomer>().Map(x => x.Name), Is.EqualTo(new[] { "Globex" }));
        scope.Prefix = "In";
        Assert.That(db.Select<TenantCustomer>().Map(x => x.Name), Is.EqualTo(new[] { "Initech" }));

        Assert.That(NameFilters.NotCachedReasons.Single(), Does.StartWith("Filter TenantCustomer for TenantCustomer: "));
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

        // The joined table's filter params are added to the query's: the order's tenant, the customer's and the name
        var sql = q.ToSelectStatement();
        Assert.That(q.Params.Map(x => x.Value), Is.EqualTo(new object[] { 1, 1, "Acme" }));
        Assert.That(q.Params.All(p => sql.Contains(p.ParameterName)));

        Assert.That(db.UpdateOnly(() => new TenantOrder { Total = 1 }, where: x => x.Total > 60), Is.EqualTo(2));
        Assert.That(db.Delete<TenantOrder>(x => x.Total == 1), Is.EqualTo(2));
        Assert.That(db.Count<TenantOrder>(), Is.EqualTo(1));
        Assert.That(TenantFilters.NotCachedReasons, Is.Empty);
    }

    static void AssertSameSql<T>(BoundQuery<T> compiled, SqlExpression<T> query)
    {
        Assert.That(compiled.SelectInto<T>(), Is.EqualTo(query.SelectInto<T>()));
        Assert.That(compiled.Params.Map(x => x.ParameterName), Is.EqualTo(query.Params.Map(x => x.ParameterName)));
        Assert.That(compiled.Params.Map(x => x.Value), Is.EqualTo(query.Params.Map(x => x.Value)));
        Assert.That(compiled.Params.Map(x => x.DbType), Is.EqualTo(query.Params.Map(x => x.DbType)));
    }

    [Test]
    public void Compiled_queries_reuse_their_SQL_on_filtered_tables()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var query = OrmLiteQuery.Compile<TenantOrder, decimal>((q, min) => q.Where(x => x.Total >= min).OrderBy(x => x.Id));
        foreach (var (tenantId, expected) in new[] { (1, new[] { 1, 3 }), (2, new[] { 4 }), (1, new[] { 1, 3 }) })
        {
            using var db = OpenDbConnection().UseFilters(TenantFilters.For(new TenantScope { TenantId = tenantId }));
            Assert.That(db.Select(query, 60m).Map(x => x.Id), Is.EqualTo(expected));
            AssertSameSql(query.Bind(db, 60m), db.From<TenantOrder>().Where(x => x.Total >= 60m).OrderBy(x => x.Id));
        }

        // One statement for every tenant, with its TenantId as a db param
        Assert.That(query.CachedStatements, Is.EqualTo(1));
        Assert.That(query.NotCachedReason, Is.Null);
    }

    [Test]
    public void Compiled_queries_have_a_statement_for_each_case_of_a_scope_condition()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var query = OrmLiteQuery.Compile<TenantCustomer>(q => q.OrderBy(x => x.Id));
        var scope = new TenantScope();
        using var db = OpenDbConnection().UseFilters(TenantFilters.For(scope));

        // s.TenantId == null || x.TenantId == s.TenantId
        Assert.That(db.Select(query).Map(x => x.Id), Is.EqualTo(new[] { 1, 2, 3 }));
        scope.TenantId = 2;
        Assert.That(db.Select(query).Map(x => x.Id), Is.EqualTo(new[] { 3 }));
        AssertSameSql(query.Bind(db), db.From<TenantCustomer>().OrderBy(x => x.Id));
        scope.TenantId = 1;
        Assert.That(db.Select(query).Map(x => x.Id), Is.EqualTo(new[] { 1, 2 }));
        scope.TenantId = null;
        Assert.That(db.Count(query.Bind(db)), Is.EqualTo(3));

        Assert.That(query.CachedStatements, Is.EqualTo(3)); // all customers, one tenant's customers and the count
        Assert.That(query.NotCachedReason, Is.Null);
    }

    [Test]
    public void Compiled_queries_read_the_scope_for_each_statement()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var query = OrmLiteQuery.Compile<TenantOrder>(q => q.OrderBy(x => x.Id));
        var scope = new TenantScope();
        using var db = OpenDbConnection().UseFilters(TenantFilters.For(scope));

        // Ensure's AssertTenantId() throws until the tenant is known
        Assert.Throws<InvalidOperationException>(() => db.Select(query));
        scope.TenantId = 2;
        Assert.That(db.Select(query).Map(x => x.Id), Is.EqualTo(new[] { 4 }));
        scope.TenantId = 1;
        Assert.That(db.Select(query).Map(x => x.Id), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(query.CachedStatements, Is.EqualTo(1));
    }

    [Test]
    public void Compiled_queries_have_a_statement_for_each_size_of_a_filters_collection()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var query = OrmLiteQuery.Compile<TenantOrder>(q => q.OrderBy(x => x.Id));
        var scope = new TenantScope { OrderIds = [1, 2] };
        using var db = OpenDbConnection().UseFilters(OrderIdFilters.For(scope));
        Assert.That(db.Select(query).Map(x => x.Id), Is.EqualTo(new[] { 1, 2 }));
        scope.OrderIds = [3, 4];
        Assert.That(db.Select(query).Map(x => x.Id), Is.EqualTo(new[] { 3, 4 }));
        AssertSameSql(query.Bind(db), db.From<TenantOrder>().OrderBy(x => x.Id));
        scope.OrderIds = [1, 3, 4];
        Assert.That(db.Select(query).Map(x => x.Id), Is.EqualTo(new[] { 1, 3, 4 }));

        Assert.That(query.CachedStatements, Is.EqualTo(2)); // for 2 and 3 values
        Assert.That(query.NotCachedReason, Is.Null);
    }

    [Test]
    public void Compiled_queries_with_filters_whose_SQL_cant_be_reused_generate_it_each_time()
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);

        var query = OrmLiteQuery.Compile<TenantCustomer>(q => q.OrderBy(x => x.Id));
        var scope = new NameScope { Prefix = "G" };
        using var db = OpenDbConnection().UseFilters(NameFilters.For(scope));
        Assert.That(db.Select(query).Map(x => x.Name), Is.EqualTo(new[] { "Globex" }));
        scope.Prefix = "In";
        Assert.That(db.Select(query).Map(x => x.Name), Is.EqualTo(new[] { "Initech" }));

        Assert.That(query.CachedStatements, Is.EqualTo(0));
        Assert.That(query.NotCachedReason, Does.Contain("FilterSet.NotCachedReasons"));
    }

    [Test]
    public async Task Compiled_updates_and_deletes_apply_the_connections_filters_and_rules()
    {
        using (var seed = OpenDbConnection())
        {
            seed.DropAndCreateTable<TenantInvoice>();
            seed.InsertAll(new List<TenantInvoice> {
                new() { TenantId = 1, Customer = "Acme", Total = 100, CreatedBy = "seed", CreatedDate = Invoices.Created },
                new() { TenantId = 1, Customer = "Globex", Total = 50, CreatedBy = "seed", CreatedDate = Invoices.Created },
                new() { TenantId = 2, Customer = "Initech", Total = 500, CreatedBy = "seed", CreatedDate = Invoices.Created },
            });
        }
        var byMinTotal = OrmLiteQuery.Compile<TenantInvoice, decimal>((q, min) => q.Where(x => x.Total >= min));

        using (var db = OpenDbConnection().ForUser(1, "alice"))
        {
            // Only the tenant's rows, with the audit columns of its user
            Assert.That(db.UpdateOnly(() => new TenantInvoice { Reminders = 1 }, byMinTotal.Bind(db, 0m)), Is.EqualTo(2));
            var compiledSql = db.GetLastSql();
            db.UpdateOnly(() => new TenantInvoice { Reminders = 1 }, db.From<TenantInvoice>().Where(x => x.Total >= 0m));
            Assert.That(compiledSql, Is.EqualTo(db.GetLastSql()));
        }
        using (var db = OpenDbConnection().ForUser(2, "bob"))
        {
            Assert.That(await db.UpdateAddAsync(() => new TenantInvoice { Reminders = 2 }, byMinTotal.Bind(db, 0m)), Is.EqualTo(1));
            Assert.That(db.Single<TenantInvoice>(x => x.Customer == "Initech").ModifiedBy, Is.EqualTo("bob"));
            Assert.That(await db.DeleteAsync(byMinTotal, 0m), Is.EqualTo(1));
        }
        using (var db = OpenDbConnection())
        {
            var rows = db.Select<TenantInvoice>(x => x.TenantId == 1);
            Assert.That(rows.Map(x => x.Reminders), Is.EqualTo(new[] { 1, 1 }));
            Assert.That(rows.All(x => x.ModifiedBy == "alice" && x.ModifiedDate == Invoices.Modified && x.CreatedBy == "seed"));
            Assert.That(db.Count<TenantInvoice>(x => x.TenantId == 2), Is.EqualTo(0));
        }

        // The WHERE clause and the DELETE statement, shared by every tenant
        Assert.That(byMinTotal.CachedStatements, Is.EqualTo(2));
        Assert.That(byMinTotal.NotCachedReason, Is.Null);
    }
}
