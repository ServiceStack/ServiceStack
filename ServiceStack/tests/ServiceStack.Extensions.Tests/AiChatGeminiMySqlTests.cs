#nullable enable
using System;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;

namespace ServiceStack.Extensions.Tests;

[TestFixture, Category("Integration")]
public class AiChatGeminiMySqlTests
{
    const string User = ChatDb.DefaultUser;

    [Test, Explicit("Requires the servicestack-mysql MySQL/MariaDB container")]
    public void Local_search_uses_MySql_or_MariaDb_fulltext()
    {
        var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION")
            ?? "Server=localhost;Port=48205;Database=test;UID=test;Password=p@55wOrd;SslMode=Required;AllowLoadLocalInfile=true;Convert Zero Datetime=True";
        var factory = new OrmLiteConnectionFactory(connectionString, MySqlDialect.Provider);
        var db = new GeminiDb(new ChatDb(factory));
        db.InitSchema();

        var now = DateTime.Now;
        var marker = Guid.NewGuid().ToString("n");
        var store = new ChatFilestore
        {
            User = User,
            CreatedAt = now,
            UpdatedAt = now,
            Name = "test/" + marker,
            DisplayName = "MySQL Search " + marker,
        };
        store.Id = db.InsertFilestore(store);

        try
        {
            var doc = new ChatDocument
            {
                FilestoreId = store.Id,
                User = User,
                CreatedAt = now,
                UpdatedAt = now,
                DisplayName = "mysql-search.md",
                Filename = marker + ".md",
                Hash = marker.PadRight(64, '0')[..64],
                ContentHash = marker,
                Category = "guides",
                DocType = "guide",
                Versions = "[\"v1\"]",
                Tags = "[\"database\"]",
            };
            db.SetSearchDesired(doc);
            doc.Id = db.InsertDocument(doc);
            db.ReplaceSearchSections(doc,
                GeminiSearch.SplitSections(
                    "# MySQL and MariaDB Search\n\nNative fulltext integration supports resilient searching.", doc),
                doc.SearchHash!);

            var results = db.SearchSections(store.Id, "resilient search", User,
                new JsonObject
                {
                    ["category"] = "guides",
                    ["versions"] = "v1",
                    ["tags"] = "database",
                });
            var stats = db.SearchStats(store.Id, User);
            using var conn = db.OpenDb();
            var version = conn.Scalar<string>("SELECT VERSION()") ?? "";
            var expectedProvider = version.Contains("MariaDB", StringComparison.OrdinalIgnoreCase)
                ? "mariadb-fulltext"
                : "mysql-fulltext";

            Assert.Multiple(() =>
            {
                Assert.That(stats.Provider, Is.EqualTo(expectedProvider));
                Assert.That(results, Has.Count.EqualTo(1));
                Assert.That(results[0].Score, Is.GreaterThan(0));
                Assert.That(results[0].DocumentId, Is.EqualTo(doc.Id));
            });
        }
        finally
        {
            db.DeleteFilestore(store.Id, User, store.DisplayName);
        }
    }
}
