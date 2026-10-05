#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;

namespace ServiceStack.AiTests;

[TestFixture,Category("Integration")]
public class AiChatMigrationGeminiDialectTests
{
    [TestCase("mysql"),TestCase("postgresql"),TestCase("sqlserver")]
    [Explicit("Requires the local OrmLite database fixture containers")]
    public void Identity_detach_upgrade_unicode_filters_and_search_are_portable(string engine)
    {
        var (connection,dialect)=engine switch
        {
            "mysql" => (Environment.GetEnvironmentVariable("MYSQL_CONNECTION")??"Server=localhost;Port=48205;Database=test;UID=test;Password=p@55wOrd;SslMode=Required;Convert Zero Datetime=True",(IOrmLiteDialectProvider)MySqlDialect.Provider),
            "postgresql" => (Environment.GetEnvironmentVariable("PGSQL_CONNECTION")??"Server=localhost;Port=48303;User Id=test;Password=p@55wOrd;Database=test",PostgreSqlDialect.Provider),
            "sqlserver" => (Environment.GetEnvironmentVariable("MSSQL_CONNECTION")??"Server=localhost;User Id=sa;Password=p@55wOrd;Database=test;Encrypt=False;TrustServerCertificate=True",SqlServerDialect.Provider),
            _ => throw new ArgumentException(engine),
        };
        var user="ai-chat-migration-"+Guid.NewGuid().ToString("N");
        var db=new GeminiDb(new ChatDb(new OrmLiteConnectionFactory(connection,dialect)));db.InitSchema();
        var store=new ChatFilestore { User=user,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow,DisplayName=user };store.Id=db.InsertFilestore(store);
        try
        {
            ChatSource Source(string name)=>new() { User=user,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow,FilestoreId=store.Id,Name=name,Type="folder",Config=new JsonObject { ["manifestPath"]=Path.Combine(Path.GetTempPath(),user,name,"import.json") }.ToJsonString() };
            var a=Source("A");a.Id=db.InsertSource(a);var b=Source("B");b.Id=db.InsertSource(b);
            ChatDocument Doc(ChatSource source,string key,string category="docs")=>new() { User=user,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow,FilestoreId=store.Id,SourceId=source.Id,SourceManifestPath=ChatJson.ParseObject(source.Config!).GetString("manifestPath"),SourceKey=key,DisplayName=key,Name="remote/"+key,Category=category,Hash=user,UploadedAt=DateTime.UtcNow };
            var docs=new[]{Doc(a,"A.md"),Doc(a,"a.md","docs/api"),Doc(a,"é.md"),Doc(a,"é.md"),Doc(b,"A.md","docs-internal")};foreach(var doc in docs)doc.Id=db.InsertDocument(doc);
            Assert.That(()=>db.InsertDocument(Doc(a,"A.md")),Throws.Exception);
            Assert.That(db.SelectDocuments(new JsonObject { ["filestoreId"]=store.Id,["categoryUnder"]="docs" },user).Count,Is.EqualTo(4));
            Assert.That(db.SelectDocuments(new JsonObject { ["filestoreId"]=store.Id,["ids"]=new JsonArray() },user),Is.Empty);
            db.DeleteSource(a.Id,user);db.DeleteSource(b.Id,user);
            db.InitSchema();Assert.That(db.QueryAllDocuments(store.Id,user).Select(x=>x.Id),Is.EquivalentTo(docs.Select(x=>x.Id)));
            var reloaded=Source("A");reloaded.Id=db.InsertSource(reloaded);db.AttachSourceDocuments(reloaded.Id,store.Id,docs[0].SourceManifestPath!,user);
            Assert.That(db.QueryAllDocuments(store.Id,user).Count(x=>x.SourceId==reloaded.Id),Is.EqualTo(4));Assert.That(db.GetDocument(docs[4].Id,user)!.SourceId,Is.Null);
            Assert.That(db.GetDocument(docs[0].Id,user)!.Name,Is.EqualTo("remote/A.md"));
            var manual=new ChatDocument { User=user,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow,FilestoreId=store.Id,SourceKey="manual" };db.InsertDocument(manual);
            Assert.That(()=>db.InsertDocument(new ChatDocument { User=user,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow,FilestoreId=store.Id,SourceKey="manual",SourceManifestPath="" }),Throws.Exception);
            db.InsertDocument(new ChatDocument { User=user,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow,FilestoreId=store.Id });db.InsertDocument(new ChatDocument { User=user,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow,FilestoreId=store.Id });
            var indexed=db.GetDocument(docs[0].Id,user)!;indexed.Tags="[\"database\"]";db.SetSearchDesired(indexed);db.UpdateDocument(indexed);
            db.ReplaceSearchSections(indexed,GeminiSearch.SplitSections("# Portable search\n\nResilient database search preserves source identities.",indexed),indexed.SearchHash!);
            Assert.That(db.SearchSections(store.Id,"resilient",user,new JsonObject { ["tags"]="database" }).Select(x=>x.DocumentId),Does.Contain(indexed.Id));
            TestContext.WriteLine(engine+": "+db.SearchStats(store.Id,user).Provider);
        }
        finally { db.DeleteFilestore(store.Id,user,store.DisplayName); }
    }
}
