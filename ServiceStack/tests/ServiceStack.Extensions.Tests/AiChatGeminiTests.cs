#nullable enable
using System;
using System.Linq;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Extensions.Tests;

/// <summary>
/// Exercises the gemini extension's SQL against SQLite: the schema (incl. its unique constraints),
/// the custom sorts the UI selects, the category rollup and the upload worker's pending query.
/// </summary>
public class AiChatGeminiTests
{
    const string User = ChatDb.DefaultUser;

    static GeminiDb CreateDb()
    {
        var dbFactory = new OrmLiteConnectionFactory(
            $"DataSource=file:gemini{Guid.NewGuid():n}?mode=memory&cache=shared", SqliteDialect.Provider);
        var db = new GeminiDb(new ChatDb(dbFactory));
        db.InitSchema();
        return db;
    }

    static long AddFilestore(GeminiDb db, string displayName)
    {
        var now = DateTime.Now;
        return db.InsertFilestore(new ChatFilestore
        {
            User = User,
            CreatedAt = now,
            UpdatedAt = now,
            Name = $"fileSearchStores/{displayName}-abc123",
            DisplayName = displayName,
        });
    }

    static long AddDocument(GeminiDb db, long filestoreId, string displayName, string hash,
        string? category = null, DateTime? uploadedAt = null, string? error = null, string? state = null)
    {
        var now = DateTime.Now;
        return db.InsertDocument(new ChatDocument
        {
            FilestoreId = filestoreId,
            User = User,
            CreatedAt = now,
            UpdatedAt = now,
            DisplayName = displayName,
            Filename = $"{hash}.md",
            Url = $"/~cache/{hash[..2]}/{hash}.md",
            Hash = hash,
            Size = displayName.Length,
            Category = category,
            UploadedAt = uploadedAt,
            Error = error,
            State = state,
        });
    }

    [Test]
    public void Creates_schema_and_round_trips_a_filestore()
    {
        var db = CreateDb();
        var id = AddFilestore(db, "Docs");

        var filestore = db.GetFilestore(id, User);
        Assert.That(filestore, Is.Not.Null);
        Assert.That(filestore!.DisplayName, Is.EqualTo("Docs"));

        var dto = filestore.ToDto();
        Assert.That(dto.GetString("displayName"), Is.EqualTo("Docs"));
        Assert.That(dto["id"]!.GetValue<long>(), Is.EqualTo(id));

        // other users can't see it
        Assert.That(db.GetFilestore(id, "someone-else"), Is.Null);
    }

    [Test]
    public void Applies_the_file_search_store_resource_to_a_filestore()
    {
        var db = CreateDb();
        var id = AddFilestore(db, "Docs");
        var filestore = db.GetFilestore(id, User)!;

        // as returned by the API: int64 fields are serialized as strings
        filestore.PopulateFrom(ChatJson.ParseObject("""
        {
            "name": "fileSearchStores/docs-xyz",
            "displayName": "Docs",
            "createTime": "2026-01-09T12:34:56.789Z",
            "updateTime": "2026-01-09T12:35:56.789Z",
            "activeDocumentsCount": "12",
            "pendingDocumentsCount": 1,
            "failedDocumentsCount": 0,
            "sizeBytes": "2048"
        }
        """));
        db.UpdateFilestore(filestore);

        var saved = db.GetFilestore(id, User)!;
        Assert.That(saved.Name, Is.EqualTo("fileSearchStores/docs-xyz"));
        Assert.That(saved.ActiveDocumentsCount, Is.EqualTo(12));
        Assert.That(saved.SizeBytes, Is.EqualTo(2048));
        // Gemini's RFC3339 is normalized to the same wire format as every other timestamp,
        // so assert against that rather than restating the format here.
        Assert.That(saved.CreateTime, Is.EqualTo(
            ChatDb.ToDateString(new DateTime(2026, 1, 9, 12, 34, 56, 789, DateTimeKind.Utc))));
    }

    [Test]
    public void Queries_documents_by_filestore_category_and_display_names()
    {
        var db = CreateDb();
        var filestoreId = AddFilestore(db, "Docs");
        var otherId = AddFilestore(db, "Other");
        AddDocument(db, filestoreId, "one.md", new string('1', 64), category: "guides");
        AddDocument(db, filestoreId, "two.md", new string('2', 64));
        AddDocument(db, otherId, "three.md", new string('3', 64));

        var all = db.QueryDocuments(new JsonObject { ["filestoreId"] = filestoreId }, User);
        Assert.That(all.Count, Is.EqualTo(2));

        var guides = db.QueryDocuments(new JsonObject
        {
            ["filestoreId"] = filestoreId,
            ["category"] = "guides",
        }, User);
        Assert.That(guides.Map(x => x.DisplayName), Is.EquivalentTo(new[] { "one.md" }));

        // ?null=category is how the UI selects "Uncategorized"
        var uncategorized = db.QueryDocuments(new JsonObject
        {
            ["filestoreId"] = filestoreId,
            ["null"] = "category",
        }, User);
        Assert.That(uncategorized.Map(x => x.DisplayName), Is.EquivalentTo(new[] { "two.md" }));

        var byName = db.QueryDocuments(new JsonObject { ["displayNames"] = "one.md,three.md" }, User);
        Assert.That(byName.Map(x => x.DisplayName), Is.EquivalentTo(new[] { "one.md", "three.md" }));

        var byId = db.QueryDocuments(new JsonObject { ["ids_in"] = $"{all[0].Id}" }, User);
        Assert.That(byId.Count, Is.EqualTo(1));

        var search = db.QueryDocuments(new JsonObject { ["q"] = "thre" }, User);
        Assert.That(search.Map(x => x.DisplayName), Is.EquivalentTo(new[] { "three.md" }));
    }

    [Test]
    public void Supports_the_custom_document_sorts()
    {
        var db = CreateDb();
        var filestoreId = AddFilestore(db, "Docs");
        var uploaded = AddDocument(db, filestoreId, "uploaded.md", new string('a', 64),
            uploadedAt: DateTime.Now.AddMinutes(-5), state: "STATE_ACTIVE");
        var pending = AddDocument(db, filestoreId, "pending.md", new string('b', 64));
        var failed = AddDocument(db, filestoreId, "failed.md", new string('c', 64), error: "boom");
        var issue = AddDocument(db, filestoreId, "issue.md", new string('d', 64),
            uploadedAt: DateTime.Now, state: "MISSING_FROM_REMOTE");

        var query = new JsonObject { ["filestoreId"] = filestoreId, ["sort"] = "uploading" };
        Assert.That(db.QueryDocuments(query, User).First().Id, Is.EqualTo(pending));

        query["sort"] = "failed";
        Assert.That(db.QueryDocuments(query, User).First().Id, Is.EqualTo(failed));

        query["sort"] = "issues";
        Assert.That(db.QueryDocuments(query, User).First().Id, Is.EqualTo(issue));

        query["sort"] = "displayName";
        Assert.That(db.QueryDocuments(query, User).Map(x => x.DisplayName),
            Is.EqualTo(new[] { "failed.md", "issue.md", "pending.md", "uploaded.md" }));

        query["sort"] = "-uploadedAt";
        Assert.That(db.QueryDocuments(query, User).First().Id, Is.EqualTo(issue));
        Assert.That(uploaded, Is.GreaterThan(0));
    }

    [Test]
    public void Rolls_up_document_categories()
    {
        var db = CreateDb();
        var filestoreId = AddFilestore(db, "Docs");
        AddDocument(db, filestoreId, "a.md", new string('a', 64), category: "guides");
        AddDocument(db, filestoreId, "bb.md", new string('b', 64), category: "guides");
        AddDocument(db, filestoreId, "ccc.md", new string('c', 64));

        var categories = db.DocumentCategories(filestoreId, User);
        Assert.That(categories.Count, Is.EqualTo(2));

        var dtos = categories.Map(x => x.ToDto());
        var uncategorized = dtos.First(x => x.GetString("category") == "");
        Assert.That(uncategorized["count"]!.GetValue<long>(), Is.EqualTo(1));
        Assert.That(uncategorized["size"]!.GetValue<long>(), Is.EqualTo("ccc.md".Length));

        var guides = dtos.First(x => x.GetString("category") == "guides");
        Assert.That(guides["count"]!.GetValue<long>(), Is.EqualTo(2));
        Assert.That(guides["size"]!.GetValue<long>(), Is.EqualTo("a.md".Length + "bb.md".Length));
    }

    [Test]
    public void Rolls_up_local_filestore_stats()
    {
        var db = CreateDb();
        var filestoreId = AddFilestore(db, "Docs");
        var emptyId = AddFilestore(db, "Empty");
        AddDocument(db, filestoreId, "a.md", new string('a', 64), category: "guides");
        AddDocument(db, filestoreId, "bb.md", new string('b', 64));

        var stats = db.FilestoreStats(filestoreId, User);
        Assert.That(stats.Count, Is.EqualTo(2));
        Assert.That(stats.Size, Is.EqualTo("a.md".Length + "bb.md".Length));

        // a store with no documents yet still reports zero rather than failing
        var empty = db.FilestoreStats(emptyId, User);
        Assert.That(empty.Count, Is.EqualTo(0));
        Assert.That(empty.Size ?? 0, Is.EqualTo(0));
    }

    [Test]
    public void Pending_documents_exclude_uploaded_and_failed()
    {
        var db = CreateDb();
        var filestoreId = AddFilestore(db, "Docs");
        var pending = AddDocument(db, filestoreId, "pending.md", new string('a', 64));
        AddDocument(db, filestoreId, "done.md", new string('b', 64), uploadedAt: DateTime.Now);
        var failed = AddDocument(db, filestoreId, "failed.md", new string('c', 64), error: "boom");

        Assert.That(db.GetPendingDocuments().Map(x => x.Id), Is.EqualTo(new[] { pending }));

        // retrying a failed upload requeues it
        db.ResetDocumentUpload(failed);
        Assert.That(db.GetPendingDocuments().Map(x => x.Id), Is.EquivalentTo(new[] { pending, failed }));
    }

    [Test]
    public void Deleting_a_filestore_deletes_its_documents()
    {
        var db = CreateDb();
        var filestoreId = AddFilestore(db, "Docs");
        var otherId = AddFilestore(db, "Other");
        AddDocument(db, filestoreId, "one.md", new string('1', 64));
        AddDocument(db, otherId, "two.md", new string('2', 64));

        db.DeleteFilestore(filestoreId, User);

        Assert.That(db.GetFilestore(filestoreId, User), Is.Null);
        Assert.That(db.QueryDocuments(new JsonObject { ["filestoreId"] = filestoreId }, User), Is.Empty);
        Assert.That(db.QueryDocuments(new JsonObject { ["filestoreId"] = otherId }, User).Count, Is.EqualTo(1));
    }

    [Test]
    public void Finds_documents_by_hash_for_dedupe()
    {
        var db = CreateDb();
        var filestoreId = AddFilestore(db, "Docs");
        var hash = new string('e', 64);
        var id = AddDocument(db, filestoreId, "one.md", hash);

        Assert.That(db.FindDocumentByHash(hash, User)?.Id, Is.EqualTo(id));
        Assert.That(db.FindDocumentByHash(new string('f', 64), User), Is.Null);
    }

    [Test]
    public void Maps_a_remote_document_onto_a_local_row()
    {
        var remote = GeminiRemoteDocument.From(ChatJson.ParseObject("""
        {
            "name": "fileSearchStores/docs-xyz/documents/one",
            "displayName": "one.md",
            "mimeType": "text/markdown",
            "sizeBytes": "123",
            "createTime": "2026-01-09T12:34:56.789Z",
            "updateTime": "2026-01-09T12:34:57.789Z",
            "state": "STATE_ACTIVE",
            "customMetadata": [
                { "key": "id", "numericValue": 7 },
                { "key": "hash", "stringValue": "abc" },
                { "key": "category", "stringValue": "guides" }
            ]
        }
        """));

        Assert.That(remote.MetadataId, Is.EqualTo(7));
        Assert.That(remote.MetadataHash, Is.EqualTo("abc"));
        Assert.That(remote.FileName(), Is.EqualTo("guides/one.md"));
        Assert.That(remote.CustomMetadata, Does.Contain("numeric_value"));

        var local = new ChatDocument { Id = 7, Hash = "abc", DisplayName = "one.md" };
        Assert.That(remote.Diff(local), Is.Not.Empty);

        remote.ApplyTo(local);
        Assert.That(remote.Diff(local), Is.Empty, "applying the remote doc should clear every difference");
        Assert.That(local.State, Is.EqualTo("STATE_ACTIVE"));
        Assert.That(local.SizeBytes, Is.EqualTo(123));
    }
}
