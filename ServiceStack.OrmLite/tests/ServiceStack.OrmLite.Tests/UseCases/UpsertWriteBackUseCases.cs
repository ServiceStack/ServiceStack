using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// A wiki page with database generated values: a creation date set by the database and a row version
/// </summary>
public class WikiPage
{
    public int Id { get; set; }
    public string Title { get; set; }

    [Default(OrmLiteVariables.SystemUtc), IgnoreOnUpdate, ReturnOnInsert]
    public DateTime CreatedAt { get; set; }

    [RowVersion]
    public ulong RowVersion { get; set; }
}

public class StickyNote
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Text { get; set; }

    [RowVersion]
    public ulong RowVersion { get; set; }
}

/// <summary>
/// Like Save(), Upsert() keeps the objects it persists in sync with the database: auto-incremented ids,
/// [RowVersion] and [ReturnOnInsert] fields are populated after both inserts and updates, using RETURNING or
/// OUTPUT in the same statement where supported, so the object can be used in further optimistic concurrency updates.
/// </summary>
[TestFixtureOrmLite]
public class UpsertWriteBackUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Upsert_populates_database_generated_values()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<WikiPage>();

        // Inserts a new row
        var doc = new WikiPage { Id = 1, Title = "Draft" };
        db.Upsert(doc);

        var dbDoc = db.SingleById<WikiPage>(1);
        Assert.That(doc.CreatedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(doc.CreatedAt, Is.EqualTo(dbDoc.CreatedAt));
        Assert.That(doc.RowVersion, Is.Not.EqualTo(0));
        Assert.That(doc.RowVersion, Is.EqualTo(dbDoc.RowVersion));

        // Updates the existing row, refreshing its row version
        var insertedVersion = doc.RowVersion;
        doc.Title = "Final";
        db.Upsert(doc);

        dbDoc = db.SingleById<WikiPage>(1);
        Assert.That(dbDoc.Title, Is.EqualTo("Final"));
        Assert.That(doc.RowVersion, Is.Not.EqualTo(insertedVersion));
        Assert.That(doc.RowVersion, Is.EqualTo(dbDoc.RowVersion));
        Assert.That(doc.CreatedAt, Is.EqualTo(dbDoc.CreatedAt));
    }

    [Test]
    public void Upserted_objects_can_be_used_in_optimistic_concurrency_updates()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<WikiPage>();

        var doc = new WikiPage { Id = 1, Title = "Draft" };
        db.Upsert(doc);

        // The row version is current, so the update succeeds
        doc.Title = "Reviewed";
        db.Update(doc);

        // Another update of the same row makes this copy stale
        var stale = db.SingleById<WikiPage>(1);
        doc = db.SingleById<WikiPage>(1);
        doc.Title = "Published";
        db.Upsert(doc);

        stale.Title = "Overwritten";
        Assert.Throws<OptimisticConcurrencyException>(() => db.Update(stale));

        // The upserted object still has the latest row version
        doc.Title = "Archived";
        db.Update(doc);
        Assert.That(db.SingleById<WikiPage>(1).Title, Is.EqualTo("Archived"));
    }

    [Test]
    public void Upsert_only_selected_fields()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<WikiPage>();
        db.Upsert(new WikiPage { Id = 1, Title = "Draft" });

        var doc = new WikiPage { Id = 1, Title = "Final" };
        db.Upsert(doc, updateOnly: x => new { x.Title });

        var dbDoc = db.SingleById<WikiPage>(1);
        Assert.That(dbDoc.Title, Is.EqualTo("Final"));
        Assert.That(doc.RowVersion, Is.EqualTo(dbDoc.RowVersion));
        Assert.That(doc.CreatedAt, Is.EqualTo(dbDoc.CreatedAt));
    }

    [Test]
    public void Upsert_without_an_id_inserts_and_populates_the_new_id()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<StickyNote>();

        var note = new StickyNote { Text = "Buy milk" };
        db.Upsert(note);

        Assert.That(note.Id, Is.GreaterThan(0));
        var dbNote = db.SingleById<StickyNote>(note.Id);
        Assert.That(note.RowVersion, Is.EqualTo(dbNote.RowVersion));

        note.Text = "Buy oat milk";
        db.Upsert(note);
        Assert.That(db.Count<StickyNote>(), Is.EqualTo(1));
        Assert.That(note.RowVersion, Is.EqualTo(db.SingleById<StickyNote>(note.Id).RowVersion));
    }

    [Test]
    public void UpsertAll_populates_every_object()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<WikiPage>();
        db.Upsert(new WikiPage { Id = 1, Title = "Existing" });

        var docs = new List<WikiPage> {
            new() { Id = 1, Title = "Updated" },
            new() { Id = 2, Title = "New" },
        };
        db.UpsertAll(docs);

        var dbDocs = db.SelectByIds<WikiPage>(new[] { 1, 2 }).ToDictionary(x => x.Id);
        foreach (var doc in docs)
        {
            Assert.That(doc.RowVersion, Is.EqualTo(dbDocs[doc.Id].RowVersion));
            Assert.That(doc.CreatedAt, Is.EqualTo(dbDocs[doc.Id].CreatedAt));
        }
    }

    [Test]
    public async Task Async_APIs()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<WikiPage>();
        db.DropAndCreateTable<StickyNote>();

        var doc = new WikiPage { Id = 1, Title = "Draft" };
        await db.UpsertAsync(doc);
        Assert.That(doc.CreatedAt, Is.Not.EqualTo(default(DateTime)));

        doc.Title = "Final";
        await db.UpsertAsync(doc);

        var dbDoc = await db.SingleByIdAsync<WikiPage>(1);
        Assert.That(doc.RowVersion, Is.EqualTo(dbDoc.RowVersion));
        Assert.That(doc.CreatedAt, Is.EqualTo(dbDoc.CreatedAt));

        var note = new StickyNote { Text = "Async" };
        await db.UpsertAsync(note);
        Assert.That(note.Id, Is.GreaterThan(0));
        Assert.That(note.RowVersion, Is.EqualTo((await db.SingleByIdAsync<StickyNote>(note.Id)).RowVersion));
    }
}
