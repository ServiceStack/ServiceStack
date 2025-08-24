using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Models;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Async;

[TestFixtureOrmLite]
public class OrmLiteUpdateTestsAsync(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public async Task Can_filter_update_method1_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db, 2);

        await ResetUpdateDateAsync(db);
        await db.UpdateAsync(
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider),
            new DefaultValuesUpdate {Id = 1, DefaultInt = 45 }, new DefaultValuesUpdate {Id = 2, DefaultInt = 72 });
        await VerifyUpdateDateAsync(db);
        await VerifyUpdateDateAsync(db, 2);
    }

    private async Task<DefaultValuesUpdate> CreateAndInitializeAsync(IDbConnection db, int count = 1)
    {
        db.DropAndCreateTable<DefaultValuesUpdate>();
        db.GetLastSql().Print();

        DefaultValuesUpdate firstRow = null;
        for (var i = 1; i <= count; i++)
        {
            var defaultValues = new DefaultValuesUpdate { Id = i };
            await db.InsertAsync(defaultValues);

            var row = await db.SingleByIdAsync<DefaultValuesUpdate>(1);
            row.PrintDump();
            Assert.That(row.DefaultInt, Is.EqualTo(1));
            Assert.That(row.DefaultIntNoDefault, Is.EqualTo(0));
            Assert.That(row.NDefaultInt, Is.EqualTo(1));
            Assert.That(row.DefaultDouble, Is.EqualTo(1.1).Within(.1d));
            Assert.That(row.NDefaultDouble, Is.EqualTo(1.1).Within(.1d));
            Assert.That(row.DefaultString, Is.EqualTo("String"));

            if (firstRow == null)
                firstRow = row;
        }

        return firstRow;
    }

    private async Task ResetUpdateDateAsync(IDbConnection db)
    {
        var updateTime = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        await db.UpdateAsync<DefaultValuesUpdate>(new { UpdatedDateUtc = updateTime }, p => p.Id == 1);
    }

    private async Task VerifyUpdateDateAsync(IDbConnection db, int id = 1)
    {
        var row = await db.SingleByIdAsync<DefaultValuesUpdate>(id);
        row.PrintDump();
        Assert.That(row.UpdatedDateUtc, Is.GreaterThan(DateTime.UtcNow - TimeSpan.FromMinutes(5)));
    }

    [Test]
    public async Task Can_filter_update_method2_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);
        await db.UpdateAsync(new DefaultValuesUpdate { Id = 1, DefaultInt = 2342 }, p => p.Id == 1,
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));
        await VerifyUpdateDateAsync(db);
    }

    [Test]
    public async Task Can_filter_update_method3_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);
        var row = await db.SingleByIdAsync<DefaultValuesUpdate>(1);
        row.DefaultInt = 3245;
        row.DefaultDouble = 978.423;
        await db.UpdateAsync(row, cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));
        await VerifyUpdateDateAsync(db);
    }

    [Test]
    public async Task Can_filter_update_method4_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);
        await db.UpdateAsync<DefaultValuesUpdate>(new { DefaultInt = 765 }, p => p.Id == 1,
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));
        await VerifyUpdateDateAsync(db);
    }

    [Test]
    public async Task Can_filter_updateAll_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db, 2);

        await ResetUpdateDateAsync(db);
        db.UpdateAll(new[] { new DefaultValuesUpdate { Id = 1, DefaultInt = 45 }, new DefaultValuesUpdate { Id = 2, DefaultInt = 72 } },
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));
        await VerifyUpdateDateAsync(db);
        await VerifyUpdateDateAsync(db, 2);
    }

    [Test]
    public async Task Can_filter_updateOnly_method1_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);
        db.UpdateOnly(() => new DefaultValuesUpdate { DefaultInt = 345 }, p => p.Id == 1,
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));
        await VerifyUpdateDateAsync(db);
    }

    [Test]
    public async Task Can_filter_updateOnly_method2_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);
        await db.UpdateOnlyAsync(() => new DefaultValuesUpdate { DefaultInt = 345 }, db.From<DefaultValuesUpdate>().Where(p => p.Id == 1),
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));
        await VerifyUpdateDateAsync(db);
    }
        
    [Test]
    public async Task Can_filter_MySql_updateOnly_method2_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);
        await db.UpdateOnlyAsync(() => new DefaultValuesUpdate { DefaultInt = 345 }, db.From<DefaultValuesUpdate>().Where(p => p.Id == 1),
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));
        await VerifyUpdateDateAsync(db);
    }

    [Test]
    public async Task Can_filter_updateOnly_method3_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);
        var row = await db.SingleByIdAsync<DefaultValuesUpdate>(1);
        row.DefaultDouble = 978.423;
        await db.UpdateOnlyFieldsAsync(row, db.From<DefaultValuesUpdate>().Update(p => p.DefaultDouble),
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));
        await VerifyUpdateDateAsync(db);
    }

    [Test]
    public async Task Can_filter_updateOnly_method4_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);
        var row = await db.SingleByIdAsync<DefaultValuesUpdate>(1);
        row.DefaultDouble = 978.423;
        await db.UpdateOnlyFieldsAsync(row, p => p.DefaultDouble, p => p.Id == 1,
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));
        await VerifyUpdateDateAsync(db);
    }

    [Test]
    public async Task Can_filter_updateOnly_method5_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);
        var row = await db.SingleByIdAsync<DefaultValuesUpdate>(1);
        row.DefaultDouble = 978.423;
        await db.UpdateOnlyFieldsAsync(row, new[] { nameof(DefaultValuesUpdate.DefaultDouble) }, p => p.Id == 1,
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));
        await VerifyUpdateDateAsync(db);
    }

    [Test]
    public async Task Can_filter_updateAdd_expression_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);

        var count = await db.UpdateAddAsync(() => new DefaultValuesUpdate { DefaultInt = 5, DefaultDouble = 7.2 }, p => p.Id == 1,
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));

        Assert.That(count, Is.EqualTo(1));
        var row = await db.SingleByIdAsync<DefaultValuesUpdate>(1);
        Assert.That(row.DefaultInt, Is.EqualTo(6));
        Assert.That(row.DefaultDouble, Is.EqualTo(8.3).Within(0.1));
        await VerifyUpdateDateAsync(db);
    }

    [Test]
    public async Task Can_filter_updateAdd_SqlExpression_to_insert_date()
    {
        using var db = await OpenDbConnectionAsync();
        await CreateAndInitializeAsync(db);

        await ResetUpdateDateAsync(db);

        var where = db.From<DefaultValuesUpdate>().Where(p => p.Id == 1);
        var count = await db.UpdateAddAsync(() => new DefaultValuesUpdate { DefaultInt = 5, DefaultDouble = 7.2 }, @where,
            cmd => cmd.SetUpdateDate<DefaultValuesUpdate>(nameof(DefaultValuesUpdate.UpdatedDateUtc), DialectProvider));

        Assert.That(count, Is.EqualTo(1));
        var row = await db.SingleByIdAsync<DefaultValuesUpdate>(1);
        Assert.That(row.DefaultInt, Is.EqualTo(6));
        Assert.That(row.DefaultDouble, Is.EqualTo(8.3).Within(0.1));
        await VerifyUpdateDateAsync(db);
    }
        
    [Test]
    public async Task Can_updated_with_ExecuteSql_and_db_params_Async()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<PocoUpdateAsync>();

        db.Insert(new PocoUpdateAsync { Id = 1, Name = "A" });
        db.Insert(new PocoUpdateAsync { Id = 2, Name = "B" });

        var result = await db.ExecuteSqlAsync($"UPDATE {DialectProvider.QuoteTable("PocoUpdateAsync")} SET name = @name WHERE id = @id", new { id = 2, name = "UPDATED" });
        Assert.That(result, Is.EqualTo(1));

        var row = await db.SingleByIdAsync<PocoUpdateAsync>(2);
        Assert.That(row.Name, Is.EqualTo("UPDATED"));
    }

    [Test]
    public async Task Does_UpdateAdd_using_AssignmentExpression_async()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<Person>();
        await db.InsertAllAsync(Person.Rockstars);

        var count = await db.UpdateAddAsync(() => new Person { FirstName = "JJ", Age = 1 }, @where: p => p.LastName == "Hendrix");
        Assert.That(count, Is.EqualTo(1));

        var hendrix = Person.Rockstars.First(x => x.LastName == "Hendrix");
        var kurt = Person.Rockstars.First(x => x.LastName == "Cobain");

        var row = await db.SingleAsync<Person>(p => p.LastName == "Hendrix");
        Assert.That(row.FirstName, Is.EqualTo("JJ"));
        Assert.That(row.Age, Is.EqualTo(hendrix.Age + 1));

        count = await db.UpdateAddAsync(() => new Person { FirstName = "KC", Age = hendrix.Age + 1 }, @where: p => p.LastName == "Cobain");
        Assert.That(count, Is.EqualTo(1));

        row = await db.SingleAsync<Person>(p => p.LastName == "Cobain");
        Assert.That(row.FirstName, Is.EqualTo("KC"));
        Assert.That(row.Age, Is.EqualTo(kurt.Age + hendrix.Age + 1));
    }
        
    public class PocoUpdateAsync
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}