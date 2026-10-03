using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// Objects written on a connection have the values its rules wrote, and state can be kept with a connection.
/// </summary>
[TestFixtureOrmLite]
public class ConnectionRuleOptionsUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    IDbConnection OpenForUser(string userId)
    {
        using (var seed = OpenDbConnection())
        {
            seed.DropAndCreateTable<TenantInvoice>();
        }
        return OpenDbConnection().ForUser(1, userId);
    }

    [Test]
    public void Inserted_objects_have_the_values_that_were_written()
    {
        using var db = OpenForUser("mythz");

        var invoice = new TenantInvoice { Customer = "Acme", Total = 100 };
        invoice.Id = (int)db.Insert(invoice, selectIdentity: true);

        // the object has what was saved, without querying for it
        Assert.That(invoice.TenantId, Is.EqualTo(1));
        Assert.That(invoice.CreatedBy, Is.EqualTo("mythz"));
        Assert.That(invoice.CreatedDate, Is.EqualTo(Invoices.Created));
        Assert.That(invoice.ModifiedBy, Is.Null); // only set when it's updated

        var row = db.SingleById<TenantInvoice>(invoice.Id);
        Assert.That(row.TenantId, Is.EqualTo(invoice.TenantId));
        Assert.That(row.CreatedBy, Is.EqualTo(invoice.CreatedBy));
    }

    [Test]
    public void Updated_objects_have_the_values_that_were_written()
    {
        using var db = OpenForUser("mythz");
        var invoice = new TenantInvoice { Customer = "Acme", Total = 100 };
        invoice.Id = (int)db.Insert(invoice, selectIdentity: true);

        invoice.Total = 200;
        db.Update(invoice);

        Assert.That(invoice.ModifiedBy, Is.EqualTo("mythz"));
        Assert.That(invoice.ModifiedDate, Is.EqualTo(Invoices.Modified));
    }

    [Test]
    public void Partially_updated_objects_have_the_values_that_were_written()
    {
        using var db = OpenForUser("mythz");
        var invoice = new TenantInvoice { Customer = "Acme", Total = 100 };
        invoice.Id = (int)db.Insert(invoice, selectIdentity: true);

        invoice.Total = 300;
        db.UpdateOnlyFields(invoice, onlyFields: x => x.Total, where: x => x.Id == invoice.Id);
        Assert.That(invoice.ModifiedBy, Is.EqualTo("mythz"));

        invoice.ModifiedBy = null;
        invoice.Reminders = 2;
        db.UpdateNonDefaults(invoice, x => x.Id == invoice.Id);
        Assert.That(invoice.ModifiedBy, Is.EqualTo("mythz"));
        Assert.That(db.SingleById<TenantInvoice>(invoice.Id).Reminders, Is.EqualTo(2));
    }

    [Test]
    public void Every_row_of_a_batch_has_the_values_that_were_written()
    {
        using var db = OpenForUser("mythz");
        var inserted = new[] { new TenantInvoice { Customer = "Acme" }, new TenantInvoice { Customer = "Globex" } };
        var bulkInserted = new[] { new TenantInvoice { Customer = "Initech" }, new TenantInvoice { Customer = "Umbrella" } };

        db.InsertAll(inserted);
        db.BulkInsert(bulkInserted);

        Assert.That(inserted.Concat(bulkInserted).Map(x => x.TenantId), Is.All.EqualTo(1));
        Assert.That(inserted.Concat(bulkInserted).Map(x => x.CreatedBy), Is.All.EqualTo("mythz"));

        var rows = db.Select<TenantInvoice>();
        rows.Each(x => x.Total = 10);
        db.UpdateAll(rows);
        Assert.That(rows.Map(x => x.ModifiedBy), Is.All.EqualTo("mythz"));
    }

    [Test]
    public async Task Objects_written_by_async_apis_have_the_values_that_were_written()
    {
        using var db = OpenForUser("mythz");

        var invoice = new TenantInvoice { Customer = "Acme", Total = 100 };
        invoice.Id = (int)await db.InsertAsync(invoice, selectIdentity: true);
        Assert.That(invoice.TenantId, Is.EqualTo(1));
        Assert.That(invoice.CreatedBy, Is.EqualTo("mythz"));

        await db.UpdateAsync(invoice);
        Assert.That(invoice.ModifiedBy, Is.EqualTo("mythz"));

        var other = new TenantInvoice { Customer = "Globex" };
        await db.InsertAllAsync(new[] { other });
        Assert.That(other.TenantId, Is.EqualTo(1));
    }

    [Test]
    public void Objects_rejected_by_a_rule_are_left_unchanged()
    {
        using var db = OpenForUser("mythz");

        var invoice = new TenantInvoice { TenantId = 2, Customer = "Acme" };
        Assert.Throws<InvalidOperationException>(() => db.Insert(invoice));

        Assert.That(invoice.TenantId, Is.EqualTo(2));
        Assert.That(invoice.CreatedBy, Is.Null);
    }

    [Test]
    public void Objects_written_WithoutFilters_are_left_unchanged()
    {
        using var db = OpenForUser("mythz");

        var adminInvoice = new TenantInvoice { TenantId = 2, Customer = "Admin", CreatedDate = Invoices.Created };
        db.WithoutFilters().Insert(adminInvoice);

        Assert.That(adminInvoice.TenantId, Is.EqualTo(2));
        Assert.That(adminInvoice.CreatedBy, Is.Null);
    }

    [Test]
    public void Items_are_kept_with_a_connection()
    {
        using var db = OpenForUser("mythz");

        Assert.That(db.GetItem<string>("UserId"), Is.Null);
        Assert.That(db.GetItem<int>("TenantId"), Is.EqualTo(0));

        db.SetItem("TenantId", 1).SetItem("UserId", "mythz");

        Assert.That(db.GetItem<int>("TenantId"), Is.EqualTo(1));
        Assert.That(db.GetItem<string>("UserId"), Is.EqualTo("mythz"));
        Assert.That(db.GetOrAddItem("Roles", () => new List<string> { "Admin" }), Is.SameAs(db.GetItem<List<string>>("Roles")));
    }

    [Test]
    public void Items_are_shared_with_the_connection_WithoutFilters()
    {
        using var db = OpenForUser("mythz");
        db.SetItem("TenantId", 1);

        var adminDb = db.WithoutFilters();

        Assert.That(adminDb.IsWithoutFilters());
        Assert.That(db.IsWithoutFilters(), Is.False);
        Assert.That(adminDb.GetItem<int>("TenantId"), Is.EqualTo(1));
        adminDb.SetItem("UserId", "admin");
        Assert.That(db.GetItem<string>("UserId"), Is.EqualTo("admin"));
    }

    [Test]
    public void Items_are_not_kept_for_the_next_open()
    {
        using (var db = OpenDbConnection())
            db.SetItem("TenantId", 1);

        using var next = OpenDbConnection();
        Assert.That(next.GetItem<int>("TenantId"), Is.EqualTo(0));
    }

    [Test]
    public void Filters_and_rules_of_a_connection_WithoutFilters_are_its_own()
    {
        using var db = OpenForUser("mythz");
        db.Insert(new TenantInvoice { Customer = "Acme" });
        db.WithoutFilters().Insert(new TenantInvoice { TenantId = 2, Customer = "Initech", CreatedDate = Invoices.Created });

        // e.g. a connection for an admin, that sees every tenant and records who is writing
        var adminDb = db.WithoutFilters();
        adminDb.UseFilters(FilterSet.Create(f => f.OnInsert<IAudit>(x => x.CreatedBy, () => "admin")));
        adminDb.Insert(new TenantInvoice { TenantId = 3, Customer = "Globex", CreatedDate = Invoices.Created });

        Assert.That(adminDb.Select<TenantInvoice>().Count, Is.EqualTo(3));
        Assert.That(adminDb.Single<TenantInvoice>(x => x.TenantId == 3).CreatedBy, Is.EqualTo("admin"));

        // which don't change the connection it was created from, or the next connection WithoutFilters
        Assert.That(db.Select<TenantInvoice>().Count, Is.EqualTo(1));
        db.Insert(new TenantInvoice { Customer = "Umbrella" });
        Assert.That(db.Single<TenantInvoice>(x => x.Customer == "Umbrella").CreatedBy, Is.EqualTo("mythz"));
        db.WithoutFilters().Insert(new TenantInvoice { TenantId = 4, Customer = "Hooli", CreatedDate = Invoices.Created });
        Assert.That(adminDb.Single<TenantInvoice>(x => x.TenantId == 4).CreatedBy, Is.Null);
    }
}
