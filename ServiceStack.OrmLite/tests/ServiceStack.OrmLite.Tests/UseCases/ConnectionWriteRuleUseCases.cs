using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

public interface IAudit
{
    string CreatedBy { get; }
    DateTime CreatedDate { get; }
    string ModifiedBy { get; }
    DateTime? ModifiedDate { get; }
}

public class TenantInvoice : IHasTenantId, IAudit
{
    [AutoIncrement]
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Customer { get; set; }
    [Default(0)]
    public decimal Total { get; set; }
    [Default(0)]
    public int Reminders { get; set; }
    [IgnoreOnUpdate] // created columns are only set when the row is inserted
    public string CreatedBy { get; set; }
    [IgnoreOnUpdate]
    public DateTime CreatedDate { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class TenantInvoiceArchive : IHasTenantId, IAudit
{
    [AutoIncrement]
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Customer { get; set; }
    [Default(0)]
    public decimal Total { get; set; }
    [IgnoreOnUpdate] // created columns are only set when the row is inserted
    public string CreatedBy { get; set; }
    [IgnoreOnUpdate]
    public DateTime CreatedDate { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

/// <summary>
/// Tables can also share their tenant and audit columns in a base class, with string ids that default to ""
/// </summary>
public abstract class WorkspaceAuditBase
{
    public string WorkspaceId { get; set; } = "";
    [IgnoreOnUpdate]
    public string CreatedBy { get; set; } = "system";
    public string ModifiedBy { get; set; } = "system";
}

public class WorkspaceDocument : WorkspaceAuditBase
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; }
}

public static class Invoices
{
    public static readonly DateTime Created = new(2026, 1, 2, 3, 4, 5);
    public static readonly DateTime Modified = new(2026, 2, 3, 4, 5, 6);

    /// <summary>
    /// The user of a tenant that a connection reads and writes rows for
    /// </summary>
    public record TenantUser(int TenantId, string UserId);

    /// <summary>
    /// The tenant's rows are the only rows a connection can read or write, and the rows it writes record who wrote
    /// them and when
    /// </summary>
    public static readonly FilterSet<TenantUser> UserRules = FilterSet.Create<TenantUser>(f => {
        f.Ensure<IHasTenantId>(x => x.TenantId, s => s.TenantId);
        f.OnInsert<IAudit>(x => x.CreatedBy, s => s.UserId);
        f.OnInsert<IAudit>(x => x.CreatedDate, _ => Created); // e.g. _ => DateTime.UtcNow
        f.OnUpdate<IAudit>(x => x.ModifiedBy, s => s.UserId);
        f.OnUpdate<IAudit>(x => x.ModifiedDate, _ => Modified);
    });

    /// <summary>
    /// e.g. an app's extension method to open a connection for a tenant's user
    /// </summary>
    public static IDbConnection ForUser(this IDbConnection db, int tenantId, string userId) =>
        db.UseFilters(UserRules.For(new TenantUser(tenantId, userId)));
}

/// <summary>
/// The write rules of a connection's FilterSets set columns of every row it writes: Ensure requires a column to have a
/// value, e.g. the tenant, while OnInsert and OnUpdate always set a column, e.g. for auditing who changed a row and when.
/// </summary>
[TestFixtureOrmLite]
public class ConnectionWriteRuleUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    // Creates the tables with an invoice of another tenant, and opens a connection for a user of tenant 1
    IDbConnection OpenForUser(string userId)
    {
        using (var seed = OpenDbConnection())
        {
            seed.DropAndCreateTable<TenantInvoice>();
            seed.DropAndCreateTable<TenantInvoiceArchive>();
            seed.Insert(new TenantInvoice {
                TenantId = 2, Customer = "Initech", Total = 500, CreatedBy = "other", CreatedDate = Invoices.Created,
            });
        }
        return OpenDbConnection().ForUser(1, userId);
    }

    static void AssertCreatedBy(IAudit row, string userId)
    {
        Assert.That(row.CreatedBy, Is.EqualTo(userId));
        Assert.That(row.CreatedDate, Is.EqualTo(Invoices.Created));
    }

    static void AssertModifiedBy(IAudit row, string userId)
    {
        Assert.That(row.ModifiedBy, Is.EqualTo(userId));
        Assert.That(row.ModifiedDate, Is.EqualTo(Invoices.Modified));
    }

    [Test]
    public void Inserts_set_the_tenant_and_who_created_the_row()
    {
        using var db = OpenForUser("alice");

        var invoice = new TenantInvoice { Customer = "Acme", Total = 100 };
        var id = db.Insert(invoice, selectIdentity: true);

        var row = db.SingleById<TenantInvoice>(id);
        Assert.That(row.TenantId, Is.EqualTo(1));
        AssertCreatedBy(row, "alice");
        Assert.That(row.ModifiedBy, Is.Null);
        Assert.That(row.ModifiedDate, Is.Null);

        // The object has the values that were written
        Assert.That(invoice.TenantId, Is.EqualTo(1));
        AssertCreatedBy(invoice, "alice");

        // A rule's value replaces a value from the app, so audit columns can be trusted
        id = db.Insert(new TenantInvoice { Customer = "Globex", CreatedBy = "mallory" }, selectIdentity: true);
        AssertCreatedBy(db.SingleById<TenantInvoice>(id), "alice");
    }

    [Test]
    public void Every_insert_api_applies_the_rules()
    {
        using var db = OpenForUser("alice");

        db.InsertAll(new[] {
            new TenantInvoice { Customer = "InsertAll 1" },
            new TenantInvoice { Customer = "InsertAll 2" },
        });
        db.InsertOnly(() => new TenantInvoice { Customer = "InsertOnly" });
        db.InsertOnly(new TenantInvoice { Customer = "InsertOnly fields", Total = 5 }, x => new { x.Customer, x.Total });
        db.Insert<TenantInvoice>(new Dictionary<string, object> { ["Customer"] = "Dictionary" });
        db.BulkInsert(new[] {
            new TenantInvoice { Customer = "BulkInsert 1" },
            new TenantInvoice { Customer = "BulkInsert 2" },
        }, new BulkInsertConfig { Mode = BulkInsertMode.Sql });

        var rows = db.Select<TenantInvoice>();
        Assert.That(rows.Map(x => x.Customer), Is.EquivalentTo(new[] {
            "InsertAll 1", "InsertAll 2", "InsertOnly", "InsertOnly fields", "Dictionary", "BulkInsert 1", "BulkInsert 2",
        }));
        foreach (var row in rows)
        {
            Assert.That(row.TenantId, Is.EqualTo(1), row.Customer);
            AssertCreatedBy(row, "alice");
        }
    }

    [Test]
    public void Rows_copied_with_InsertIntoSelect_apply_the_rules()
    {
        using var db = OpenForUser("alice");
        db.Insert(new TenantInvoice { Customer = "Acme", Total = 100 });
        db.Insert(new TenantInvoice { Customer = "Globex", Total = 50 });

        // Only the tenant's invoices are selected
        var q = db.From<TenantInvoice>().Select(x => new { x.Customer, x.Total });
        Assert.That(db.InsertIntoSelect<TenantInvoiceArchive>(q), Is.EqualTo(2));

        var rows = db.Select<TenantInvoiceArchive>();
        Assert.That(rows.Map(x => x.Customer), Is.EquivalentTo(new[] { "Acme", "Globex" }));
        foreach (var row in rows)
        {
            Assert.That(row.TenantId, Is.EqualTo(1));
            AssertCreatedBy(row, "alice");
        }

        // Columns set by rules can't also be selected
        Assert.Throws<NotSupportedException>(() => db.InsertIntoSelect<TenantInvoiceArchive>(
            db.From<TenantInvoice>().Select(x => new { x.Customer, x.CreatedBy })));
    }

    [Test]
    public void Updates_set_who_modified_the_row()
    {
        using var db = OpenForUser("alice");
        var id = (int)db.Insert(new TenantInvoice { Customer = "Acme", Total = 100 }, selectIdentity: true);

        var invoice = db.SingleById<TenantInvoice>(id);
        invoice.Total = 110;
        invoice.CreatedBy = "mallory";
        db.Update(invoice);

        var row = db.SingleById<TenantInvoice>(id);
        Assert.That(row.Total, Is.EqualTo(110m));
        AssertModifiedBy(row, "alice");
        AssertCreatedBy(row, "alice"); // [IgnoreOnUpdate] columns aren't updated

        // The object has the values that were written
        AssertModifiedBy(invoice, "alice");
    }

    [Test]
    public void Every_update_api_applies_the_rules()
    {
        using var db = OpenForUser("alice");

        // Each API updates a new invoice, which hasn't been modified
        TenantInvoice Updated(Action<int> update)
        {
            var id = (int)db.Insert(new TenantInvoice { Customer = "Acme", Total = 100 }, selectIdentity: true);
            update(id);
            var row = db.SingleById<TenantInvoice>(id);
            AssertModifiedBy(row, "alice");
            AssertCreatedBy(row, "alice");
            Assert.That(row.TenantId, Is.EqualTo(1));
            return row;
        }

        Assert.That(Updated(id => db.UpdateOnly(() => new TenantInvoice { Total = 1 }, where: x => x.Id == id)).Total, Is.EqualTo(1m));
        Assert.That(Updated(id => db.UpdateOnly(() => new TenantInvoice { ModifiedBy = "mallory" }, where: x => x.Id == id)).Total, Is.EqualTo(100m));
        Assert.That(Updated(id => db.UpdateOnlyFields(new TenantInvoice { Total = 2 }, x => x.Total, x => x.Id == id)).Total, Is.EqualTo(2m));
        Assert.That(Updated(id => db.UpdateNonDefaults(new TenantInvoice { Total = 3 }, x => x.Id == id)).Total, Is.EqualTo(3m));
        Assert.That(Updated(id => db.Update<TenantInvoice>(new { Total = 4m }, x => x.Id == id)).Total, Is.EqualTo(4m));
        Assert.That(Updated(id => db.UpdateOnly<TenantInvoice>(new Dictionary<string, object> { ["Id"] = id, ["Total"] = 5m })).Total, Is.EqualTo(5m));
        Assert.That(Updated(id => db.UpdateOnly<TenantInvoice>(new Dictionary<string, object> { ["Total"] = 6m }, x => x.Id == id)).Total, Is.EqualTo(6m));
        Assert.That(Updated(id => {
            var invoice = db.SingleById<TenantInvoice>(id);
            invoice.Total = 7;
            db.UpdateAll(new[] { invoice });
        }).Total, Is.EqualTo(7m));
        Assert.That(Updated(id => db.UpdateFrom(x => new TenantInvoice { Total = 8 }, db.From<TenantInvoice>().Where(x => x.Id == id))).Total, Is.EqualTo(8m));

        // UpdateAdd adds to numeric columns, while values of rules are set
        Assert.That(Updated(id => db.UpdateAdd(() => new TenantInvoice { Reminders = 2 }, where: x => x.Id == id)).Reminders, Is.EqualTo(2));
    }

    [Test]
    public void Rows_cant_be_written_for_another_tenant()
    {
        using var db = OpenForUser("alice");
        var id = (int)db.Insert(new TenantInvoice { Customer = "Acme", Total = 100 }, selectIdentity: true);

        // The tenant can be set to the connection's tenant, but not another
        db.Insert(new TenantInvoice { TenantId = 1, Customer = "Globex" });
        Assert.Throws<InvalidOperationException>(() => db.Insert(new TenantInvoice { TenantId = 2, Customer = "Hooli" }));
        Assert.Throws<InvalidOperationException>(() => db.InsertOnly(() => new TenantInvoice { TenantId = 2, Customer = "Hooli" }));
        Assert.Throws<InvalidOperationException>(() => db.BulkInsert(new[] { new TenantInvoice { TenantId = 2, Customer = "Hooli" } }));

        // Rows can't be moved to another tenant
        var invoice = db.SingleById<TenantInvoice>(id);
        invoice.TenantId = 2;
        Assert.Throws<InvalidOperationException>(() => db.Update(invoice));
        Assert.Throws<InvalidOperationException>(() => db.UpdateOnly(() => new TenantInvoice { TenantId = 2 }, where: x => x.Id == id));
        Assert.Throws<InvalidOperationException>(() => db.Update<TenantInvoice>(new { TenantId = 2 }, x => x.Id == id));
        Assert.Throws<InvalidOperationException>(() => db.Save(invoice));
        Assert.Throws<NotSupportedException>(() => db.UpdateFrom(x => new TenantInvoice { TenantId = 2 }, db.From<TenantInvoice>()));

        Assert.That(db.Select<TenantInvoice>().Map(x => x.Customer), Is.EquivalentTo(new[] { "Acme", "Globex" }));

        // Updating an object that doesn't have its tenant set keeps it
        db.Update(new TenantInvoice { Id = id, Customer = "Acme", Total = 1 });
        Assert.That(db.SingleById<TenantInvoice>(id).TenantId, Is.EqualTo(1));
    }

    [Test]
    public void Save_and_Upsert_keep_objects_in_sync_with_their_rows()
    {
        int id;
        using (var db = OpenForUser("alice"))
        {
            // Saving a new invoice also sets the values of the rules on the object
            var invoice = new TenantInvoice { Customer = "Acme", Total = 100 };
            db.Save(invoice);
            id = invoice.Id;
            Assert.That(invoice.TenantId, Is.EqualTo(1));
            AssertCreatedBy(invoice, "alice");
            Assert.That(invoice.ModifiedBy, Is.Null);

            var upserted = new TenantInvoice { Customer = "Globex" };
            db.Upsert(upserted);
            Assert.That(upserted.TenantId, Is.EqualTo(1));
            AssertCreatedBy(upserted, "alice");
        }

        using (var db = OpenDbConnection().ForUser(1, "bob"))
        {
            var invoice = db.SingleById<TenantInvoice>(id);
            invoice.Total = 110;
            db.Save(invoice);
            AssertModifiedBy(invoice, "bob");

            var row = db.SingleById<TenantInvoice>(id);
            Assert.That(row.Total, Is.EqualTo(110m));
            Assert.That(row.CreatedBy, Is.EqualTo("alice"));
            AssertModifiedBy(row, "bob");
        }

        using (var db = OpenDbConnection().ForUser(1, "carol"))
        {
            // Upsert of an existing row is an update
            var invoice = new TenantInvoice { Id = id, Customer = "Acme Inc", Total = 120 };
            db.Upsert(invoice, x => new { x.Customer, x.Total });
            Assert.That(invoice.TenantId, Is.EqualTo(0)); // only updated fields are set
            AssertModifiedBy(invoice, "carol");

            var row = db.SingleById<TenantInvoice>(id);
            Assert.That(row.Customer, Is.EqualTo("Acme Inc"));
            Assert.That(row.TenantId, Is.EqualTo(1));
            Assert.That(row.CreatedBy, Is.EqualTo("alice"));
            AssertModifiedBy(row, "carol");
        }
    }

    // e.g. to also record who modified a row when it's created, instead of leaving it empty until it's updated
    static readonly FilterSet<string> WriterRules = FilterSet.Create<string>(f => {
        f.Ensure<IHasTenantId>(x => x.TenantId, _ => 1);
        f.OnInsert<IAudit>(x => x.CreatedBy, userId => userId);
        f.OnInsert<IAudit>(x => x.CreatedDate, _ => Invoices.Created);
        f.OnWrite<IAudit>(x => x.ModifiedBy, userId => userId);
        f.OnWrite<IAudit>(x => x.ModifiedDate, _ => Invoices.Modified);
    });

    [Test]
    public void OnWrite_sets_columns_on_both_inserts_and_updates()
    {
        IDbConnection OpenAs(string userId) => OpenDbConnection().UseFilters(WriterRules.For(userId));

        int id;
        using (OpenForUser("seed")) {}
        using (var db = OpenAs("alice"))
        {
            id = (int)db.Insert(new TenantInvoice { Customer = "Acme", Total = 100 }, selectIdentity: true);

            var row = db.SingleById<TenantInvoice>(id);
            AssertCreatedBy(row, "alice");
            AssertModifiedBy(row, "alice");
        }

        using (var db = OpenAs("bob"))
        {
            db.UpdateOnly(() => new TenantInvoice { Total = 110 }, where: x => x.Id == id);

            var row = db.SingleById<TenantInvoice>(id);
            AssertCreatedBy(row, "alice");
            AssertModifiedBy(row, "bob");
        }
    }

    static readonly FilterSet<string> WorkspaceRules = FilterSet.Create<string>(f => {
        f.Ensure<WorkspaceAuditBase>(x => x.WorkspaceId, workspaceId => workspaceId);
        f.OnInsert<WorkspaceAuditBase>(x => x.CreatedBy, _ => "alice");
        f.OnWrite<WorkspaceAuditBase>(x => x.ModifiedBy, _ => "alice");
    });

    [Test]
    public void Filters_and_rules_on_a_base_class_apply_to_its_tables()
    {
        using (var seed = OpenDbConnection())
        {
            seed.DropAndCreateTable<WorkspaceDocument>();
            seed.Insert(new WorkspaceDocument { Id = "theirs", WorkspaceId = "w2", Name = "Theirs" });
        }

        using var db = OpenDbConnection().UseFilters(WorkspaceRules.For("w1"));

        // An empty string isn't a value that was set, so the workspace is set from the rule
        db.Insert(new WorkspaceDocument { Id = "mine", Name = "Mine" });

        var row = db.SingleById<WorkspaceDocument>("mine");
        Assert.That(row.WorkspaceId, Is.EqualTo("w1"));
        Assert.That(row.CreatedBy, Is.EqualTo("alice"));
        Assert.That(row.ModifiedBy, Is.EqualTo("alice"));

        Assert.That(db.Select<WorkspaceDocument>().Map(x => x.Id), Is.EqualTo(new[] { "mine" }));
        Assert.That(db.UpdateOnly(() => new WorkspaceDocument { Name = "Changed" }, where: x => x.Id == "theirs"), Is.EqualTo(0));
        Assert.Throws<InvalidOperationException>(() => db.Insert(new WorkspaceDocument { WorkspaceId = "w2", Name = "Other" }));
    }

    [Test]
    public void Rules_only_apply_to_their_tables_and_connection()
    {
        using (var db = OpenForUser("alice"))
        {
            db.Insert(new TenantInvoice { Customer = "Acme" });

            // TenantOrder has a tenant, but no audit columns
            db.DropAndCreateTable<TenantOrder>();
            db.Insert(new TenantOrder { Id = 1, Total = 10 });
            Assert.That(db.SingleById<TenantOrder>(1).TenantId, Is.EqualTo(1));

            // WithoutFilters() has no rules, e.g. for an admin task writing another tenant's rows
            var adminDb = db.WithoutFilters();
            var adminId = adminDb.Insert(new TenantInvoice { TenantId = 2, Customer = "Admin", CreatedDate = Invoices.Created }, selectIdentity: true);
            var adminRow = adminDb.SingleById<TenantInvoice>(adminId);
            Assert.That(adminRow.TenantId, Is.EqualTo(2));
            Assert.That(adminRow.CreatedBy, Is.Null);
        }

        // Connections opened later have no rules
        using var other = OpenDbConnection();
        var id = other.Insert(new TenantInvoice { TenantId = 3, Customer = "No rules", CreatedDate = Invoices.Created }, selectIdentity: true);
        var row = other.SingleById<TenantInvoice>(id);
        Assert.That(row.TenantId, Is.EqualTo(3));
        Assert.That(row.CreatedBy, Is.Null);
    }

    [Test]
    public void Using_the_same_rules_again_is_ignored()
    {
        using var db = OpenForUser("alice");

        // e.g. when a connection is configured more than once
        var rules = db.GetFilters();
        db.ForUser(1, "alice");
        Assert.That(db.GetFilters(), Is.SameAs(rules));

        var id = db.Insert(new TenantInvoice { Customer = "Acme" }, selectIdentity: true);
        AssertCreatedBy(db.SingleById<TenantInvoice>(id), "alice");

        // The rules can't be used again for another tenant or user, the connection would write rows for both
        Assert.Throws<InvalidOperationException>(() => db.ForUser(2, "alice"));
        Assert.Throws<InvalidOperationException>(() => db.ForUser(1, "bob"));
        Assert.That(db.GetFilters(), Is.SameAs(rules));
    }

    [Test]
    public void Invalid_rules_throw()
    {
        // Columns are selected with a property of the table or interface
        Assert.Throws<ArgumentException>(() => FilterSet.Create<Invoices.TenantUser>(f =>
            f.OnInsert<IAudit>(x => x.CreatedBy.Length, s => 1)));
        Assert.Throws<ArgumentException>(() => FilterSet.Create<Invoices.TenantUser>(f =>
            f.Ensure<IHasTenantId>(x => x.TenantId + 1, s => s.TenantId)));
    }

    [Test]
    public async Task Async_apis_apply_the_rules()
    {
        using var db = OpenForUser("alice");

        var invoice = new TenantInvoice { Customer = "Acme", Total = 100 };
        var id = (int)await db.InsertAsync(invoice, selectIdentity: true);
        AssertCreatedBy(invoice, "alice");

        await db.InsertAllAsync(new[] { new TenantInvoice { Customer = "Globex" } });
        await db.InsertOnlyAsync(() => new TenantInvoice { Customer = "Hooli" });
        await db.BulkInsertAsync(new[] { new TenantInvoice { Customer = "Umbrella" } }, new BulkInsertConfig { Mode = BulkInsertMode.Sql });
        var saved = new TenantInvoice { Customer = "Stark" };
        await db.SaveAsync(saved);
        AssertCreatedBy(saved, "alice");

        var rows = await db.SelectAsync<TenantInvoice>();
        Assert.That(rows.Count, Is.EqualTo(5));
        foreach (var row in rows)
        {
            Assert.That(row.TenantId, Is.EqualTo(1), row.Customer);
            AssertCreatedBy(row, "alice");
        }

        var existing = await db.SingleByIdAsync<TenantInvoice>(id);
        existing.Total = 110;
        await db.UpdateAsync(existing);
        AssertModifiedBy(await db.SingleByIdAsync<TenantInvoice>(id), "alice");

        await db.UpdateOnlyAsync(() => new TenantInvoice { Total = 1 }, where: x => x.Id == saved.Id);
        AssertModifiedBy(await db.SingleByIdAsync<TenantInvoice>(saved.Id), "alice");

        var upserted = new TenantInvoice { Id = id, Customer = "Acme Inc", Total = 120 };
        await db.UpsertAsync(upserted);
        AssertModifiedBy(upserted, "alice");

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.InsertAsync(new TenantInvoice { TenantId = 2, Customer = "Other" }));
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.UpdateOnlyAsync(() => new TenantInvoice { TenantId = 2 }, where: x => x.Id == id));
    }
}
