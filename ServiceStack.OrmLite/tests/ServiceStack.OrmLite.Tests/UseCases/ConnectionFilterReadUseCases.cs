using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

public class TenantCategory : IHasTenantId
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; }
}

public class TenantAuthor : IHasTenantId
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; }

    [Reference]
    public List<TenantBook> Books { get; set; }
}

public class TenantBook : IHasTenantId
{
    public int Id { get; set; }
    public int TenantAuthorId { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; }
    public bool IsDeleted { get; set; }
}

public class OrderCustomerName
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
}

/// <summary>
/// A connection's mandatory filters apply to every typed API OrmLite constructs SQL for, not just db.From&lt;T&gt;(),
/// so a filtered connection can't read other rows through APIs like SingleById(). Raw SQL isn't changed.
/// </summary>
[TestFixtureOrmLite]
public class ConnectionFilterReadUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    IDbConnection OpenForTenant(int tenantId)
    {
        using (var seed = OpenDbConnection())
            Tenants.Seed(seed);
        return OpenDbConnection().ForTenant(tenantId);
    }

    [Test]
    public async Task Apis_that_take_a_Type_are_filtered()
    {
        using var db = OpenForTenant(1);
        var orderType = typeof(TenantOrder);

        var api = db.CreateTypedApi(orderType);
        Assert.That(api.Select().Cast<TenantOrder>().Select(x => x.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
        Assert.That((await api.SelectAsync()).Count, Is.EqualTo(3));
        Assert.That(api.Select("Total > @total", new { total = 0 }).Count, Is.EqualTo(3));
        Assert.That((await api.SelectAsync("Total > @total", new { total = 0 })).Count, Is.EqualTo(3));
        Assert.That(api.SingleById(1), Is.Not.Null);
        Assert.That(api.SingleById(4), Is.Null);
        Assert.That(await api.SingleByIdAsync(4), Is.Null);
        Assert.That(api.Count(), Is.EqualTo(3));
        Assert.That(await api.CountAsync(), Is.EqualTo(3));

        Assert.That(db.Select<TenantOrder>(orderType).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));

        // Another tenant's rows can't be deleted
        Assert.That(api.DeleteById(4), Is.EqualTo(0));
        Assert.That(db.Delete(orderType, "Id = @id", new { id = 4 }), Is.EqualTo(0));
        Assert.That(api.DeleteAll(), Is.EqualTo(3));
        Assert.That(db.DeleteAll(orderType), Is.EqualTo(0));
        Assert.That(db.WithoutFilters().Select<TenantOrder>().Map(x => x.Id), Is.EqualTo(new[] { 4 }));
    }

    [Test]
    public void Typed_lambda_apis_are_filtered()
    {
        using var db = OpenForTenant(1);

        Assert.That(db.Select<TenantOrder>(x => x.Total > 0).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
        Assert.That(db.Single<TenantOrder>(x => x.Id == 4), Is.Null);
        Assert.That(db.Count<TenantOrder>(), Is.EqualTo(3));
        Assert.That(db.Count<TenantOrder>(x => x.Total > 60), Is.EqualTo(2));
        Assert.That(db.Exists<TenantOrder>(x => x.Id == 4), Is.False);
        Assert.That(db.Scalar<TenantOrder, decimal>(x => Sql.Max(x.Total)), Is.EqualTo(100m));
        Assert.That(db.WhereLazy<TenantOrder>(new { CustomerId = 3 }).Count(), Is.EqualTo(0));
    }

    [Test]
    public void Where_filter_and_anonymous_object_apis_are_filtered()
    {
        using var db = OpenForTenant(1);

        Assert.That(db.Select<TenantOrder>().Count, Is.EqualTo(3));
        Assert.That(db.Select<TenantOrder>("Total > @min", new { min = 0 }).Count, Is.EqualTo(3));
        Assert.That(db.Select<TenantOrder>(Sql.Fmt($"Total > {60}")).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 3 }));
        Assert.That(db.Single<TenantOrder>("Id = @id", new { id = 4 }), Is.Null);
        Assert.That(db.Where<TenantOrder>(new { CustomerId = 3 }), Is.Empty);
        Assert.That(db.Single<TenantOrder>(new { Id = 4 }), Is.Null);
        Assert.That(db.Exists<TenantOrder>(new { Id = 4 }), Is.False);

        // Complete SQL statements are raw SQL which isn't changed
        var table = db.GetQuotedTableName<TenantOrder>();
        Assert.That(db.Select<TenantOrder>($"SELECT * FROM {table}").Count, Is.EqualTo(4));
    }

    [Test]
    public void By_id_apis_are_filtered()
    {
        using var db = OpenForTenant(1);

        Assert.That(db.SingleById<TenantOrder>(1), Is.Not.Null);
        Assert.That(db.SingleById<TenantOrder>(4), Is.Null);
        Assert.That(db.SelectByIds<TenantOrder>(new[] { 1, 3, 4 }).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 3 }));
        Assert.That(db.ExistsById<TenantOrder>(1));
        Assert.That(db.ExistsById<TenantOrder>(4), Is.False);
        Assert.That(db.LoadSingleById<TenantOrder>(4), Is.Null);
    }

    [Test]
    public void Joined_tables_are_filtered_in_their_join_condition()
    {
        using var db = OpenForTenant(1);
        db.UseFilters(FilterSet.Create(f => f.Filter<TenantCustomer>(c => c.Name != "Globex")));

        // An INNER JOIN only returns orders whose customer matches the filter
        var inner = db.From<TenantOrder>().Join<TenantCustomer>((o, c) => o.CustomerId == c.Id);
        Assert.That(db.Select(inner).Map(x => x.Id), Is.EquivalentTo(new[] { 1 }));

        // A LEFT JOIN returns every order, without the columns of customers that don't match
        var left = db.From<TenantOrder>()
            .LeftJoin<TenantCustomer>((o, c) => o.CustomerId == c.Id)
            .Select<TenantOrder, TenantCustomer>((o, c) => new { o.Id, CustomerName = c.Name });
        var rows = db.Select<OrderCustomerName>(left).ToDictionary(x => x.Id);
        Assert.That(rows.Keys, Is.EquivalentTo(new[] { 1, 2, 3 }));
        Assert.That(rows[1].CustomerName, Is.EqualTo("Acme"));
        Assert.That(rows[2].CustomerName, Is.Null);
    }

    [Test]
    public void Recursive_queries_cant_walk_into_filtered_rows()
    {
        using var db = OpenForTenant(1);
        db.DropAndCreateTable<TenantCategory>();
        db.InsertAll(new List<TenantCategory> {
            new() { Id = 1, TenantId = 1, Name = "Books" },
            new() { Id = 2, TenantId = 1, ParentId = 1, Name = "Fiction" },
            new() { Id = 3, TenantId = 2, ParentId = 2, Name = "Other tenant" },  // e.g. inconsistent data
            new() { Id = 4, TenantId = 2, ParentId = 3, Name = "Other tenant's child" },
        });

        var q = db.From<TenantCategory>()
            .WithRecursive(
                seed: db.From<TenantCategory>().Where(x => x.Id == 1),
                recurse: (parent, child) => child.ParentId == parent.Id);

        Assert.That(db.Select(q).Map(x => x.Name), Is.EquivalentTo(new[] { "Books", "Fiction" }));
    }

    [Test]
    public void Top_rows_of_each_group_are_ranked_after_filtering()
    {
        using var db = OpenForTenant(1);
        db.UseFilters(FilterSet.Create(f => f.Filter<TenantOrder>(x => !x.IsDeleted)));

        var q = db.From<TenantOrder>()
            .OrderByDescending(x => x.Total)
            .TopPerGroup(x => x.CustomerId, take: 1);

        Assert.That(db.Select(q).Map(x => x.Id), Is.EquivalentTo(new[] { 1, 3 }));
    }

    [Test]
    public void Referenced_rows_are_filtered()
    {
        using var db = OpenForTenant(1);
        db.DropAndCreateTable<TenantAuthor>();
        db.DropAndCreateTable<TenantBook>();
        db.InsertAll(new List<TenantAuthor> {
            new() { Id = 1, TenantId = 1, Name = "Alice" },
            new() { Id = 2, TenantId = 2, Name = "Bob" },
        });
        db.InsertAll(new List<TenantBook> {
            new() { Id = 1, TenantAuthorId = 1, TenantId = 1, Title = "Published" },
            new() { Id = 2, TenantAuthorId = 1, TenantId = 1, Title = "Deleted", IsDeleted = true },
            new() { Id = 3, TenantAuthorId = 1, TenantId = 2, Title = "Other tenant" }, // e.g. inconsistent data
            new() { Id = 4, TenantAuthorId = 2, TenantId = 2, Title = "Bob's book" },
        });
        db.UseFilters(FilterSet.Create(f => f.Filter<TenantBook>(x => !x.IsDeleted)));

        var authors = db.LoadSelect(db.From<TenantAuthor>());
        Assert.That(authors.Map(x => x.Name), Is.EqualTo(new[] { "Alice" }));
        Assert.That(authors[0].Books.Map(x => x.Title), Is.EqualTo(new[] { "Published" }));

        var alice = db.LoadSingleById<TenantAuthor>(1);
        Assert.That(alice.Books.Map(x => x.Title), Is.EqualTo(new[] { "Published" }));
        Assert.That(db.LoadSingleById<TenantAuthor>(2), Is.Null);
    }

    [Test]
    public async Task Async_apis_are_filtered()
    {
        using var db = OpenForTenant(2);

        Assert.That((await db.SelectAsync<TenantOrder>(x => x.Total > 0)).Map(x => x.Id), Is.EqualTo(new[] { 4 }));
        Assert.That(await db.SingleByIdAsync<TenantOrder>(1), Is.Null);
        Assert.That(await db.ExistsByIdAsync<TenantOrder>(1), Is.False);
        Assert.That((await db.SelectAsync<TenantOrder>("Total > @min", new { min = 0 })).Count, Is.EqualTo(1));
        Assert.That(await db.CountAsync<TenantOrder>(x => x.Total > 0), Is.EqualTo(1));
        Assert.That((await db.SelectByIdsAsync<TenantOrder>(new[] { 1, 4 })).Map(x => x.Id), Is.EqualTo(new[] { 4 }));
    }
}
