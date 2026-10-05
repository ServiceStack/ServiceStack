using System.Data;
using System.Text.Json.Nodes;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

/// <summary>Exact, collation-independent identity reservations; no dialect-specific partial indexes.</summary>
public class ChatDocumentIdentity
{
    [AutoIncrement] public long Id { get; set; }
    [Required, StringLength(64), Index(Unique=true)] public string Key { get; set; } = "";
    [Required, StringLength(StringLengthAttribute.MaxText)] public string Identity { get; set; } = "";
}

public partial class GeminiDb
{
    // Short database-only critical sections. This shares the single-host ownership assumption of
    // App_Data; network/extraction work is never inside the identity lock or transaction.
    static readonly object IdentitySync=new();
    static string IdentityOf(ChatDocument document)
    {
        var owner=document.User ?? "";
        // SQL Server treats NULL as a comparable value in a unique constraint. Unkeyed
        // documents therefore need their own reservation on every dialect, preserving the
        // Python contract that NULL source keys do not impose a shared document identity.
        if(document.SourceKey==null)return new JsonArray("unkeyed",owner,document.FilestoreId,
            document.Id>0?JsonValue.Create(document.Id):JsonValue.Create(Guid.NewGuid().ToString("N"))).ToJsonString();
        var manifest=string.IsNullOrEmpty(document.SourceManifestPath)?"":GeminiIngest.ResolvePath(document.SourceManifestPath);
        if(OperatingSystem.IsWindows())manifest=manifest.ToUpperInvariant();
        return new JsonArray(document.SourceId.HasValue?"source":"manifest",owner,document.FilestoreId,
            document.SourceId.HasValue?JsonValue.Create(document.SourceId.Value):JsonValue.Create(manifest),document.SourceKey).ToJsonString();
    }
    static void AssignIdentity(IDbConnection conn,ChatDocument document)
    {
        var identity=IdentityOf(document);var key=GitProcess.Hash(identity);
        var reservation=conn.Single<ChatDocumentIdentity>(x=>x.Key==key);
        if(reservation==null) {reservation=new ChatDocumentIdentity { Key=key,Identity=identity };reservation.Id=conn.Insert(reservation,selectIdentity:true);}
        if(reservation.Identity!=identity)throw new InvalidOperationException("Gemini document identity hash collision");
        // Negative reservation IDs cannot collide with legacy positive SourceId scopes. Including
        // the exact key in the reservation also avoids case-insensitive SourceKey index collisions.
        document.SourceScopeId=-reservation.Id;
    }
    void InitDocumentIdentity(IDbConnection conn)
    {
        lock(IdentitySync) {
            conn.CreateTableIfNotExists<ChatDocumentIdentity>();
            // Every persisted row receives a negative reservation scope. Once none remain unassigned,
            // the one-time backfill is done; don't rescan every document on each startup.
            if(!conn.Exists<ChatDocument>(x=>x.SourceScopeId>=0))return;
            var documents=conn.Select<ChatDocument>().OrderBy(x=>x.Id).ToArray();
            var sources=conn.Select<ChatSource>().ToDictionary(x=>x.Id);
            foreach(var document in documents) {
                if(document.SourceId is { } id && document.SourceManifestPath==null && sources.TryGetValue(id,out var source) && (source.User??"")==(document.User??"")) {
                    var config=ChatJson.TryParseObject(source.Config);
                    var path=config.GetString("manifestPath");
                    if(path==null && source.Type=="folder" && config.GetString("path") is { } directory)path=Path.Combine(directory,GeminiImportManifest.Filename);
                    if(path!=null)document.SourceManifestPath=GeminiIngest.ResolvePath(path);
                }
            }
            var duplicates=documents.Where(x=>x.SourceKey!=null).GroupBy(IdentityOf,StringComparer.Ordinal).FirstOrDefault(x=>x.Count()>1);
            if(duplicates!=null)throw new InvalidOperationException("Duplicate Gemini document identity on rows "+string.Join(", ",duplicates.Select(x=>x.Id))+". Back up the database and resolve the conflict before migration; no documents were merged.");
            using var transaction=conn.OpenTransaction();
            foreach(var document in documents) {AssignIdentity(conn,document);conn.UpdateOnly(()=>new ChatDocument { SourceScopeId=document.SourceScopeId,SourceManifestPath=document.SourceManifestPath },x=>x.Id==document.Id);}
            transaction.Commit();
        }
    }
    public void AttachSourceDocuments(long sourceId,long filestoreId,string manifest,string? user)
    {
        manifest=GeminiIngest.ResolvePath(manifest);
        lock(IdentitySync) {
            using var conn=OpenDb();using var transaction=conn.OpenTransaction();
            var q=conn.From<ChatDocument>().Where(x=>x.FilestoreId==filestoreId && x.SourceId==null);if(user!=null)ChatDb.ApplyUserFilter(q,user);
            foreach(var document in conn.Select(q).Where(x=>!string.IsNullOrEmpty(x.SourceManifestPath) && string.Equals(GeminiIngest.ResolvePath(x.SourceManifestPath!),manifest,OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal))) {document.SourceId=sourceId;AssignIdentity(conn,document);conn.Update(document);}
            transaction.Commit();
        }
    }
    public void RelocateSourceDocuments(string original,string manifest,string? user)
    {
        original=GeminiIngest.ResolvePath(original);manifest=GeminiIngest.ResolvePath(manifest);
        lock(IdentitySync) {
            using var conn=OpenDb();using var transaction=conn.OpenTransaction();var q=conn.From<ChatDocument>();if(user!=null)ChatDb.ApplyUserFilter(q,user);
            foreach(var document in conn.Select(q).Where(x=>x.SourceManifestPath!=null && GeminiIngest.ResolvePath(x.SourceManifestPath)==original)) {document.SourceManifestPath=manifest;AssignIdentity(conn,document);conn.Update(document);}
            transaction.Commit();
        }
    }
}
