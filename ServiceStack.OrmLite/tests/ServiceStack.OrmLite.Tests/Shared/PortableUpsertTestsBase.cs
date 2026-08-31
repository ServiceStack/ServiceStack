using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests;

/// <summary>
/// Executable coverage for every usage example in docs.servicestack.net's ormlite/upsert.md.
/// Every supported provider runs these assertions against a real database.
/// </summary>
public abstract class PortableUpsertTestsBase
{
    private static readonly System.DateTime OriginalCreatedDate = new(2020, 1, 1);
    private static readonly System.DateTime ChangedCreatedDate = new(2021, 1, 1);
    private static readonly System.DateTime NewCreatedDate = new(2022, 1, 1);

    [Alias("upsert_customer")]
    public class UpsertCustomer
    {
        [PrimaryKey]
        [Alias("customer_id")]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        [IgnoreOnUpdate]
        public System.DateTime CreatedDate { get; set; } = System.DateTime.UtcNow;
    }

    [Alias("upsert_auto_customer")]
    public class AutoUpsertCustomer
    {
        [AutoIncrement]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    protected abstract IDbConnection OpenUpsertDbConnection();
    protected abstract string NativeUpsertSqlFragment { get; }

    [Test]
    public void Can_insert_and_update_by_primary_key_with_native_Upsert()
    {
        using var db = OpenUpsertDbConnection();
        db.DropAndCreateTable<UpsertCustomer>();

        var customer = new UpsertCustomer
        {
            Id = 1,
            Name = "Initial Name",
            Email = "initial@example.org",
            CreatedDate = OriginalCreatedDate,
        };

        db.Upsert(customer);
        StringAssert.Contains(NativeUpsertSqlFragment, db.GetLastSql());

        customer.Name = "Updated Name";
        customer.Email = "updated@example.org";
        customer.CreatedDate = ChangedCreatedDate;
        db.Upsert(customer);

        var saved = db.SingleById<UpsertCustomer>(1);
        Assert.Multiple(() =>
        {
            Assert.That(db.Count<UpsertCustomer>(), Is.EqualTo(1));
            Assert.That(saved.Name, Is.EqualTo("Updated Name"));
            Assert.That(saved.Email, Is.EqualTo("updated@example.org"));
            Assert.That(saved.CreatedDate, Is.EqualTo(OriginalCreatedDate));
        });
    }

    [Test]
    public void Can_Upsert_with_typed_updateOnly_fields()
    {
        using var db = OpenUpsertDbConnection();
        db.DropAndCreateTable<UpsertCustomer>();

        var customer = new UpsertCustomer
        {
            Id = 1,
            Name = "Initial Name",
            Email = "initial@example.org",
            CreatedDate = OriginalCreatedDate,
        };
        db.Upsert(customer);

        customer.Name = "Updated Name";
        customer.Email = "updated@example.org";
        customer.CreatedDate = ChangedCreatedDate;
        db.Upsert(customer, updateOnly: x => new { x.Name, x.Email });

        var saved = db.SingleById<UpsertCustomer>(1);
        Assert.Multiple(() =>
        {
            Assert.That(saved.Name, Is.EqualTo("Updated Name"));
            Assert.That(saved.Email, Is.EqualTo("updated@example.org"));
            Assert.That(saved.CreatedDate, Is.EqualTo(OriginalCreatedDate));
        });
    }

    [Test]
    public void Can_Upsert_with_runtime_string_updateOnly_fields()
    {
        using var db = OpenUpsertDbConnection();
        db.DropAndCreateTable<UpsertCustomer>();
        db.Upsert(new UpsertCustomer
        {
            Id = 1,
            Name = "Initial",
            Email = "initial@example.org",
            CreatedDate = OriginalCreatedDate,
        });

        var customer = new UpsertCustomer
        {
            Id = 1,
            Name = "Name Only",
            Email = "should-not-update@example.org",
            CreatedDate = ChangedCreatedDate,
        };
        var includeEmail = false;
        var fields = includeEmail
            ? new[] { nameof(UpsertCustomer.Name), nameof(UpsertCustomer.Email) }
            : new[] { nameof(UpsertCustomer.Name) };

        db.Upsert(customer, updateOnly: fields);
        Assert.That(db.SingleById<UpsertCustomer>(1).Email, Is.EqualTo("initial@example.org"));

        includeEmail = true;
        fields = includeEmail
            ? new[] { nameof(UpsertCustomer.Name), nameof(UpsertCustomer.Email) }
            : new[] { nameof(UpsertCustomer.Name) };
        db.Upsert(customer, updateOnly: fields);

        var saved = db.SingleById<UpsertCustomer>(1);
        Assert.Multiple(() =>
        {
            Assert.That(saved.Name, Is.EqualTo("Name Only"));
            Assert.That(saved.Email, Is.EqualTo("should-not-update@example.org"));
        });
    }

    [Test]
    public void Can_UpsertAll_new_and_existing_rows()
    {
        using var db = OpenUpsertDbConnection();
        db.DropAndCreateTable<UpsertCustomer>();
        db.Upsert(new UpsertCustomer
        {
            Id = 1,
            Name = "Initial",
            Email = "one@example.org",
            CreatedDate = OriginalCreatedDate,
        });

        db.UpsertAll(new List<UpsertCustomer>
        {
            new() { Id = 1, Name = "Updated", Email = "one-updated@example.org", CreatedDate = ChangedCreatedDate },
            new() { Id = 2, Name = "Inserted", Email = "two@example.org", CreatedDate = NewCreatedDate },
        });

        var rows = db.Select<UpsertCustomer>();
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows.Find(x => x.Id == 1).Name, Is.EqualTo("Updated"));
            Assert.That(rows.Find(x => x.Id == 1).Email, Is.EqualTo("one-updated@example.org"));
            Assert.That(rows.Find(x => x.Id == 2).Name, Is.EqualTo("Inserted"));
            Assert.That(rows.Find(x => x.Id == 2).Email, Is.EqualTo("two@example.org"));
        });
    }

    [Test]
    public void Can_UpsertAll_with_typed_updateOnly_fields()
    {
        using var db = OpenUpsertDbConnection();
        db.DropAndCreateTable<UpsertCustomer>();
        db.Upsert(new UpsertCustomer
        {
            Id = 1,
            Name = "Initial",
            Email = "one@example.org",
            CreatedDate = OriginalCreatedDate,
        });

        var customers = new[]
        {
            new UpsertCustomer { Id = 1, Name = "Updated", Email = "one-updated@example.org", CreatedDate = ChangedCreatedDate },
            new UpsertCustomer { Id = 2, Name = "Inserted", Email = "two@example.org", CreatedDate = NewCreatedDate },
        };

        db.UpsertAll(customers, updateOnly: x => new { x.Name, x.Email });

        var rows = db.Select<UpsertCustomer>();
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows.Find(x => x.Id == 1).Email, Is.EqualTo("one-updated@example.org"));
            Assert.That(rows.Find(x => x.Id == 1).CreatedDate, Is.EqualTo(OriginalCreatedDate));
            Assert.That(rows.Find(x => x.Id == 2).Email, Is.EqualTo("two@example.org"));
            Assert.That(rows.Find(x => x.Id == 2).CreatedDate, Is.EqualTo(NewCreatedDate));
        });
    }

    [Test]
    public void Can_UpsertAll_with_runtime_string_updateOnly_fields()
    {
        using var db = OpenUpsertDbConnection();
        db.DropAndCreateTable<UpsertCustomer>();
        db.Upsert(new UpsertCustomer
        {
            Id = 1,
            Name = "Initial",
            Email = "one@example.org",
            CreatedDate = OriginalCreatedDate,
        });

        var customers = new[]
        {
            new UpsertCustomer { Id = 1, Name = "Updated", Email = "should-not-update@example.org", CreatedDate = ChangedCreatedDate },
            new UpsertCustomer { Id = 2, Name = "Inserted", Email = "two@example.org", CreatedDate = NewCreatedDate },
        };
        var fields = new[] { nameof(UpsertCustomer.Name) };

        db.UpsertAll(customers, updateOnly: fields);

        Assert.Multiple(() =>
        {
            Assert.That(db.SingleById<UpsertCustomer>(1).Email, Is.EqualTo("one@example.org"));
            Assert.That(db.SingleById<UpsertCustomer>(2).Email, Is.EqualTo("two@example.org"));
        });
    }

    [Test]
    public async Task Can_use_documented_UpsertAsync_and_UpsertAllAsync_APIs()
    {
        using var db = OpenUpsertDbConnection();
        db.DropAndCreateTable<UpsertCustomer>();

        var customer = new UpsertCustomer
        {
            Id = 1,
            Name = "Initial",
            Email = "initial@example.org",
            CreatedDate = OriginalCreatedDate,
        };
        await db.UpsertAsync(customer);

        customer.Name = "Updated";
        customer.Email = "updated@example.org";
        var cancellationToken = CancellationToken.None;
        await db.UpsertAsync(customer,
            updateOnly: x => new { x.Name, x.Email },
            token: cancellationToken);

        var customers = new[]
        {
            new UpsertCustomer { Id = 1, Name = "Updated Again", Email = "one@example.org", CreatedDate = ChangedCreatedDate },
            new UpsertCustomer { Id = 2, Name = "Inserted", Email = "two@example.org", CreatedDate = NewCreatedDate },
        };
        await db.UpsertAllAsync(customers,
            updateOnly: x => new { x.Name, x.Email },
            token: cancellationToken);

        var rows = db.Select<UpsertCustomer>();
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows.Find(x => x.Id == 1).Name, Is.EqualTo("Updated Again"));
            Assert.That(rows.Find(x => x.Id == 1).Email, Is.EqualTo("one@example.org"));
            Assert.That(rows.Find(x => x.Id == 2).Email, Is.EqualTo("two@example.org"));
        });
    }

    [Test]
    public void Can_Upsert_default_and_explicit_auto_increment_primary_key()
    {
        using var db = OpenUpsertDbConnection();
        db.DropAndCreateTable<AutoUpsertCustomer>();

        var customer = new AutoUpsertCustomer
        {
            Name = "New Customer",
            Email = "new@example.org",
        };
        db.Upsert(customer);
        Assert.That(customer.Id, Is.GreaterThan(0));

        customer.Name = "Updated Customer";
        db.Upsert(customer);

        var explicitId = new AutoUpsertCustomer
        {
            Id = 1001,
            Name = "Explicit ID",
            Email = "explicit@example.org",
        };
        db.Upsert(explicitId);
        explicitId.Name = "Explicit ID Updated";
        db.Upsert(explicitId);

        Assert.Multiple(() =>
        {
            Assert.That(db.Count<AutoUpsertCustomer>(), Is.EqualTo(2));
            Assert.That(db.SingleById<AutoUpsertCustomer>(customer.Id).Name, Is.EqualTo("Updated Customer"));
            Assert.That(explicitId.Id, Is.EqualTo(1001));
            Assert.That(db.SingleById<AutoUpsertCustomer>(explicitId.Id).Name, Is.EqualTo("Explicit ID Updated"));
        });
    }
}
