#nullable enable
using System;
using System.IO;
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
        AddDocument(db, filestoreId, "legacy-root.md", new string('4', 64), category: "");
        AddDocument(db, otherId, "three.md", new string('3', 64));

        var all = db.QueryDocuments(new JsonObject { ["filestoreId"] = filestoreId }, User);
        Assert.That(all.Count, Is.EqualTo(3));

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
        Assert.That(uncategorized.Map(x => x.DisplayName),
            Is.EquivalentTo(new[] { "two.md", "legacy-root.md" }));

        var emptyCategory = db.QueryDocuments(new JsonObject
        {
            ["filestoreId"] = filestoreId,
            ["category"] = "",
        }, User);
        Assert.That(emptyCategory.Map(x => x.DisplayName),
            Is.EquivalentTo(new[] { "two.md", "legacy-root.md" }));

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

    [Test]
    public void Source_key_identity_allows_identical_content_at_different_paths()
    {
        var db = CreateDb();
        var storeId = AddFilestore(db, "Docs");
        var hash = new string('a', 64);
        var now = DateTime.Now;
        var first = new ChatDocument { FilestoreId = storeId, User = User, CreatedAt = now,
            UpdatedAt = now, DisplayName = "LICENSE.md", SourceKey = "one/LICENSE.md", Hash = hash };
        var second = new ChatDocument { FilestoreId = storeId, User = User, CreatedAt = now,
            UpdatedAt = now, DisplayName = "LICENSE.md", SourceKey = "two/LICENSE.md", Hash = hash };

        first.Id = db.InsertDocument(first);
        second.Id = db.InsertDocument(second);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
        Assert.That(db.FindDocumentBySourceKey(storeId, null, "two/LICENSE.md", User)?.Id, Is.EqualTo(second.Id));
    }

    [Test]
    public void Migrates_the_legacy_filestore_hash_constraint_once()
    {
        var path = Path.Combine(Path.GetTempPath(), "gemini-migration-" + Guid.NewGuid().ToString("n") + ".sqlite");
        try
        {
            var factory = new OrmLiteConnectionFactory(path, SqliteDialect.Provider);
            using (var conn = factory.Open())
            {
                conn.ExecuteSql("""
                    CREATE TABLE "ChatDocument" (
                      "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
                      "FilestoreId" INTEGER NOT NULL,
                      "user" TEXT,
                      "CreatedAt" TEXT NOT NULL,
                      "UpdatedAt" TEXT NOT NULL,
                      "DisplayName" TEXT,
                      "Hash" TEXT,
                      CONSTRAINT "UC_ChatDocument_FilestoreId_Hash" UNIQUE ("FilestoreId","Hash")
                    )
                    """);
                conn.ExecuteSql("""
                    INSERT INTO "ChatDocument" ("FilestoreId","user","CreatedAt","UpdatedAt","DisplayName","Hash")
                    VALUES (1,'default','2026-01-01','2026-01-01','one.md','abc')
                    """);
                conn.ExecuteSql("""
                    INSERT INTO "ChatDocument" ("FilestoreId","user","CreatedAt","UpdatedAt","DisplayName","Hash")
                    VALUES (1,'default','2026-01-01','2026-01-01','one.md','different')
                    """);
            }
            var db = new GeminiDb(new ChatDb(factory));
            db.InitSchema();
            using (var conn = factory.Open())
            {
                var schema = conn.Scalar<string>(
                    "SELECT sql FROM sqlite_master WHERE type='table' AND name='ChatDocument'");
                Assert.That(schema, Does.Not.Contain("FilestoreId\",\"Hash"));
            }
            var migrated = db.QueryDocuments(new JsonObject { ["filestoreId"] = 1 }, User);
            Assert.That(migrated.Select(x => x.SourceKey), Is.EquivalentTo(new[] { "one.md", "one.md#2" }));
            db.InsertDocument(new ChatDocument
            {
                FilestoreId = 1, User = User, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
                DisplayName = "copy.md", SourceKey = "copy.md", Hash = "abc",
            });
            // The second InitSchema is a no-op migration, not another table rebuild.
            db.InitSchema();
            Assert.That(db.QueryDocuments(new JsonObject { ["filestoreId"] = 1 }, User).Count, Is.EqualTo(3));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    public void List_filters_test_membership_not_serialized_scalar_equality()
    {
        var db = CreateDb();
        var storeId = AddFilestore(db, "Docs");
        var id = AddDocument(db, storeId, "redis.md", new string('b', 64));
        var doc = db.GetDocument(id, User)!;
        doc.Tags = "redis";
        doc.Versions = "v2, v3";
        db.UpdateDocument(doc);

        Assert.That(db.QueryDocuments(new JsonObject { ["filestoreId"] = storeId, ["tags"] = "redis" }, User)
            .Map(x => x.Id), Is.EqualTo(new[] { id }));
        Assert.That(db.QueryDocuments(new JsonObject { ["filestoreId"] = storeId, ["versions"] = "v3" }, User)
            .Map(x => x.Id), Is.EqualTo(new[] { id }));
        Assert.That(db.GetDocument(id, User)!.Tags, Is.EqualTo("[\"redis\"]"));
        Assert.That(db.GetDocument(id, User)!.Versions, Is.EqualTo("[\"v2\",\"v3\"]"));
    }

    [Test]
    public void Metadata_wire_format_is_lowercase_wrapped_and_converges_after_float32_roundtrip()
    {
        var doc = new ChatDocument
        {
            Id = 4, Hash = "abc", Category = "guides/auth", DocType = "guide",
            CategoryPath = "[\"guides\",\"guides/auth\"]", Versions = "[\"v7\",\"v8\"]",
            Tags = "[\"security\"]", SourceUpdatedAt = 1730696874,
        };
        var sent = GeminiMetadata.ToCustomMetadata(doc);
        Assert.That(sent.OfType<JsonObject>().Select(x => x.GetString("key"))
            .All(x => x == x?.ToLowerInvariant()), Is.True);
        var versions = sent.OfType<JsonObject>().Single(x => x.GetString("key") == "versions");
        Assert.That(versions.GetObject("stringListValue")?.GetArray("values")?.Count, Is.EqualTo(2));

        var echoed = sent.Clone();
        echoed.OfType<JsonObject>().Single(x => x.GetString("key") == "updated_at")["numericValue"] =
            GeminiMetadata.GeminiNumeric(1730696874);
        Assert.That(GeminiMetadata.Differs(doc, echoed), Is.False);
        doc.DocType = "faq";
        Assert.That(GeminiMetadata.Differs(doc, echoed), Is.True);
    }

    [TestCase(1730696874, 1730696800d)]
    [TestCase(1688880329, 1688880400d)]
    [TestCase(1766722738, 1766722700d)]
    public void Predicts_Geminis_lossy_numeric_roundtrip(long sent, double returned) =>
        Assert.That(GeminiMetadata.GeminiNumeric(sent), Is.EqualTo(returned));

    [Test]
    public void Pending_metadata_excludes_documents_that_are_still_uploading()
    {
        var db = CreateDb();
        var storeId = AddFilestore(db, "Docs");
        var id = AddDocument(db, storeId, "one.md", new string('c', 64));
        Assert.That(db.PendingMetadata(storeId, User), Is.Empty);

        var doc = db.GetDocument(id, User)!;
        doc.UploadedAt = DateTime.Now;
        doc.CustomMetadata = GeminiRemoteDocument.CustomMetadataDto(GeminiMetadata.ToCustomMetadata(doc))!
            .ToJsonString(ChatJson.Options);
        db.UpdateDocument(doc);
        Assert.That(db.PendingMetadata(storeId, User), Is.Empty);
        doc.Tags = "[\"redis\"]";
        db.UpdateDocument(doc);
        Assert.That(db.PendingMetadata(storeId, User).Single().Fields, Does.Contain("tags"));
    }

    [Test]
    public void Bulk_preview_counts_documents_not_field_edits()
    {
        var db = CreateDb();
        var storeId = AddFilestore(db, "Docs");
        var a = AddDocument(db, storeId, "a.md", new string('d', 64));
        var b = AddDocument(db, storeId, "b.md", new string('e', 64));
        var first = db.GetDocument(a, User)!; first.DocType = "guide"; db.UpdateDocument(first);
        var docs = new[] { a, b }.Select(id => db.GetDocument(id, User)!).ToList();
        var changes = new JsonArray(
            new JsonObject { ["field"] = "docType", ["op"] = "fill", ["value"] = "faq" },
            new JsonObject { ["field"] = "status", ["op"] = "fill", ["value"] = "draft" });
        var preview = db.BulkPreview(docs, changes);
        Assert.That(preview.GetInt("change"), Is.EqualTo(2));
        Assert.That(preview.GetObject("fields")!.GetObject("docType")!.GetInt("change"), Is.EqualTo(1));
        Assert.That(preview.GetObject("fields")!.GetObject("status")!.GetInt("change"), Is.EqualTo(2));
    }

    [TestCase("docs/guides/auth/jwt.md", "docs", "guides/auth")]
    [TestCase("docs/index.md", "docs", "")]
    [TestCase("docs/a/b/c.md", "docs", "a/b")]
    public void Derives_categories_and_url_template_values(string path, string root, string expected)
    {
        Assert.That(GeminiIngest.DeriveCategory(path, root), Is.EqualTo(expected));
        var values = GeminiIngest.TemplateValues(path, expected, "Title", root);
        Assert.That(values.GetString("fullpath"), Is.EqualTo(path));
        Assert.That(GeminiIngest.ExpandTemplate("https://docs.example/{pathNoExt}", values),
            Is.EqualTo("https://docs.example/" + values.GetString("pathnoext")));
    }

    [Test]
    public void Max_depth_limits_files_in_the_import_plan()
    {
        var root = Path.Combine(Path.GetTempPath(), "gemini-depth-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path.Combine(root, "guides", "auth"));
        var body = "# Documentation\n\n" + string.Join(' ', Enumerable.Repeat("word", 60));
        try
        {
            File.WriteAllText(Path.Combine(root, "index.md"), body);
            File.WriteAllText(Path.Combine(root, "guides", "index.md"), body);
            File.WriteAllText(Path.Combine(root, "guides", "auth", "index.md"), body);
            var source = new ChatSource
            {
                Type = "folder", Config = new JsonObject { ["path"] = root }.ToJsonString(), ExtractorVer = "1",
            };

            source.Category = new JsonObject { ["maxDepth"] = 0 }.ToJsonString();
            var direct = GeminiIngest.BuildPlan(source, []);
            Assert.That(direct.Added.Select(x => x.SourceKey), Is.EqualTo(new[] { "index.md" }));
            Assert.That(direct.Discovered, Is.EqualTo(1));

            source.Category = new JsonObject { ["maxDepth"] = 1 }.ToJsonString();
            var oneLevel = GeminiIngest.BuildPlan(source, []);
            Assert.That(oneLevel.Added.Select(x => x.SourceKey),
                Is.EqualTo(new[] { "guides/index.md", "index.md" }));
            Assert.That(oneLevel.Discovered, Is.EqualTo(2));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public void Reimporting_an_unchanged_folder_is_free()
    {
        var root = Path.Combine(Path.GetTempPath(), "gemini-ingest-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path.Combine(root, "docs", "guides"));
        try
        {
            File.WriteAllText(Path.Combine(root, "docs", "guides", "auth.md"),
                "# Auth\n\n" + string.Join(' ', Enumerable.Repeat("documentation", 40)));
            var source = new ChatSource
            {
                Type = "folder", Config = new JsonObject { ["path"] = root }.ToJsonString(),
                Category = new JsonObject { ["root"] = "docs" }.ToJsonString(), ExtractorVer = "1",
            };
            var first = GeminiIngest.BuildPlan(source, []);
            Assert.That(first.Added.Count, Is.EqualTo(1));
            var existing = first.Added.Select((x, i) => new ChatDocument
            {
                Id = i + 1, SourceKey = x.SourceKey, ContentHash = x.ContentHash,
                MetadataHash = x.MetadataHash, ExtractorVer = x.ExtractorVer,
            }).ToList();
            var second = GeminiIngest.BuildPlan(source, existing);
            Assert.That(second.Embeds, Is.Zero);
            Assert.That(second.Unchanged.Count, Is.EqualTo(1));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public void Folder_imports_inherit_nested_import_json_metadata()
    {
        var root = Path.Combine(Path.GetTempPath(), "gemini-manifest-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path.Combine(root, "guides"));
        var body = "---\ntitle: Auth Guide\n---\n# Auth\n\n" + string.Join(' ', Enumerable.Repeat("documentation", 40));
        try
        {
            File.WriteAllText(Path.Combine(root, "import.json"), new JsonObject { ["metadata"] = new JsonObject
            { ["defaults"] = new JsonObject { ["product"] = "Docs", ["status"] = "draft" } } }.ToJsonString());
            File.WriteAllText(Path.Combine(root, "guides", "import.json"), new JsonObject { ["metadata"] = new JsonObject
            { ["defaults"] = new JsonObject { ["status"] = "published", ["tags"] = new JsonArray("guides") } } }.ToJsonString());
            File.WriteAllText(Path.Combine(root, "guides", "auth.md"), body);
            var source = new ChatSource { Type = "folder", Config = new JsonObject { ["path"] = root }.ToJsonString(), ExtractorVer = "1" };
            var plan = GeminiIngest.BuildPlan(source, []);
            Assert.That(plan.Added.Count, Is.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(plan.Added[0].DisplayName, Is.EqualTo("Auth Guide"));
                Assert.That(plan.Added[0].Metadata.GetString("product"), Is.EqualTo("Docs"));
                Assert.That(plan.Added[0].Metadata.GetString("status"), Is.EqualTo("published"));
                Assert.That(GeminiMetadata.AsList(plan.Added[0].Metadata["tags"]), Is.EqualTo(new[] { "guides" }));
                Assert.That(plan.Added.Any(x => x.SourceKey.EndsWith("import.json")), Is.False);
            });
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public void Html_extraction_honors_content_selector_and_removes_boilerplate()
    {
        var html = "<html><body><nav>Navigation</nav><main><h1>Docs</h1>"
            + "<p>The useful documentation lives in this content region with enough words to read.</p>"
            + "<p>Was this page helpful?</p></main><section>Outside content</section></body></html>";
        var extracted = GeminiIngest.Extract(System.Text.Encoding.UTF8.GetBytes(html), "index.html",
            new JsonObject { ["selector"] = "main", ["minWords"] = 0 });
        Assert.That(extracted.Skip, Is.Null);
        Assert.That(extracted.Text, Does.Contain("useful documentation"));
        Assert.That(extracted.Text, Does.Not.Contain("Navigation"));
        Assert.That(extracted.Text, Does.Not.Contain("Outside content"));
        Assert.That(extracted.Text, Does.Not.Contain("Was this page helpful"));
    }

    [Test]
    public void Html_to_markdown_preserves_text_boundaries_after_nested_blocks()
    {
        var html = "<dt><div><span>01</span></div>AI Ready</dt>"
            + "<dd>Start from well-known React templates.</dd>";
        var markdown = new HtmlToMarkdownParser().Parse(html);
        Assert.That(markdown, Does.Contain("01 AI Ready"));
        Assert.That(markdown, Does.Not.Contain("01AI"));
    }

    [Test]
    public void Crawler_html_to_markdown_emits_link_contents_as_plain_block_text()
    {
        var html = "<a href='/docs/autoquery/crud'><div>CRUD APIs</div>"
            + "<div>Develop full CRUD RDBMS APIs with declarative Request DTOs</div></a>"
            + "<a href='/docs/claude'><div>CLAUDE.md</div>"
            + "<div>Using CLAUDE.md and AGENTS.md files for AI-powered development with React .NET Templates</div></a>";

        var markdown = new HtmlToMarkdownParser(includeLinks: false).Parse(html);
        var lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.That(lines, Is.EqualTo(new[]
        {
            "CRUD APIs",
            "Develop full CRUD RDBMS APIs with declarative Request DTOs",
            "CLAUDE.md",
            "Using CLAUDE.md and AGENTS.md files for AI-powered development with React .NET Templates",
        }));
        Assert.That(markdown, Does.Not.Contain("](/docs/"));
    }
}
