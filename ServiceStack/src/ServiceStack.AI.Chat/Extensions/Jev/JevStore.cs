using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public class JevConflictException(string message):InvalidOperationException(message);
public sealed class JevRecipeExistsException(JsonObject recipe):JevConflictException($"The recipe \"{recipe.GetString("id")}\" already exists. Replacing it will clear its history.")
{
    public JsonObject Recipe {get;}=new(){["id"]=recipe["id"]?.DeepClone(),["revision"]=recipe["revision"]?.DeepClone()};
}
public sealed class JevBusyException():InvalidOperationException("Two decisions are already running. Wait for one to finish.");
public sealed class JevStorageException(string message):IOException(message);

/// <summary>Portable per-user JSON recipes and readable immutable run snapshots; no database-specific runtime store.</summary>
public sealed class JevStore
{
    static readonly ConcurrentDictionary<string,object> Gates=new(OperatingSystem.IsWindows()?StringComparer.OrdinalIgnoreCase:StringComparer.Ordinal);
    public string Root {get;}
    public string RecipesPath=>Path.Combine(Root,"recipes");
    public string HistoryPath=>Path.Combine(Root,"history");
    string IndexPath=>Path.Combine(Root,"index.json");
    public TimeProvider Clock {get;set;}=TimeProvider.System;
    double Now=>Clock.GetUtcNow().ToUnixTimeMilliseconds()/1000.0;
    public JevStore(string userPath,JsonObject? sentiment=null)
    {
        Root=Path.GetFullPath(Path.Combine(userPath,"jev"));Directory.CreateDirectory(Root);CheckPath(Root);
        var receipt=Path.Combine(userPath,".jev-initialized.json");
        Transaction(index=>{
            var rows=ReadRecipes(index);
            if(!index.GetBool("initialized")){
                // Legacy SQLite remains an upstream export concern; do not silently read host SQLite files.
                if(sentiment!=null&&!rows.Any(r=>r.GetString("templateId") is "01-sentiment" or "sentiment.json" or "sentiment"))
                    SaveCore(index,sentiment,null,null,null,"sentiment.json",true);
                index["initialized"]=true;
            }return 0;
        });
        if(!File.Exists(receipt))WriteJson(receipt,new JsonObject {["version"]=1});
    }
    public T Transaction<T>(Func<JsonObject,T> action,bool wait=false)
    {
        var gate=Gates.GetOrAdd(Root,_=>new object());
        if(!Monitor.TryEnter(gate,wait?TimeSpan.FromSeconds(5):TimeSpan.Zero))throw new JevConflictException("Another Jev operation is in progress. Try again shortly.");
        try {
            CheckPath(Root);
            var stored=ReadJson(IndexPath);
            if(File.Exists(IndexPath) && stored is not JsonObject)throw new JevStorageException("The saved Jev index is invalid. Restore its JSON from a backup.");
            var index=stored as JsonObject??new JsonObject {["recipes"]=new JsonObject(),["favourites"]=new JsonArray()};
            if(index["recipes"] is not JsonObject||index["favourites"] is not JsonArray)throw new JevStorageException("The saved Jev index is invalid. Restore its JSON from a backup.");
            var original=JevJson.Encode(index);
            if(ReadJson(Path.Combine(Root,".shared-import.json")) is JsonObject journal)ApplyJournal(index,journal);
            RecoverRenames(index);
            var result=action(index);
            if(JevJson.Encode(index)!=original)WriteJson(IndexPath,index);
            Delete(Path.Combine(Root,".shared-import.json"));Delete(Path.Combine(Root,".renames.json"));
            if(index.GetBool("filenameStemIdentities"))Delete(Path.Combine(Root,".references.json"));
            return result;
        }catch(Exception e) when(e is System.Text.Json.JsonException or InvalidDataException){throw new JevStorageException("A saved Jev JSON file is invalid. Restore it from a backup or correct its JSON.");}
        finally{Monitor.Exit(gate);}
    }
    public static void CheckPath(string path)
    {
        var current=Path.GetFullPath(path);
        while(current!=null){if((File.Exists(current)||Directory.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new JevStorageException("Linked Jev storage paths are not supported.");current=Path.GetDirectoryName(current);}
    }
    public static JsonNode? ReadJson(string path)
    {
        CheckPath(path);if(!File.Exists(path))return null;
        try{return JsonNode.Parse(File.ReadAllText(path,Encoding.UTF8));}
        catch(Exception e) when(e is System.Text.Json.JsonException or DecoderFallbackException){throw new JevStorageException("A saved Jev JSON file is invalid. Restore it from a backup or correct its JSON.");}
    }
    public static void WriteJson(string path,JsonNode value)=>WriteText(path,value.ToJsonString(ChatJson.Indented)+"\n");
    public static void WriteText(string path,string value)
    {
        CheckPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temporary=Path.Combine(Path.GetDirectoryName(path)!,".jev-"+Guid.NewGuid().ToString("N"));
        try{using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){var bytes=Encoding.UTF8.GetBytes(value);stream.Write(bytes);stream.Flush(true);}File.Move(temporary,path,true);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    static void Delete(string path){CheckPath(path);File.Delete(path);}
    const string Marker="\n<!-- jev-record -->\n```json\n";
    public static JsonObject ReadHistory(string path)
    {
        CheckPath(path);if(Path.GetExtension(path).Equals(".json",StringComparison.OrdinalIgnoreCase))return ReadJson(path) as JsonObject??throw new JevStorageException("Invalid Jev history record.");
        var text=File.ReadAllText(path,Encoding.UTF8);var pos=text.LastIndexOf(Marker,StringComparison.Ordinal);
        if(pos<0||!text.EndsWith("\n```\n",StringComparison.Ordinal))throw new JevStorageException("A saved Jev history record is invalid. Restore it from a backup or correct its record JSON.");
        return JsonNode.Parse(text[(pos+Marker.Length)..^5]) as JsonObject??throw new JevStorageException("Invalid Jev history record.");
    }
    public static void WriteHistory(string path,JsonObject row)
    {
        if(Path.GetExtension(path).Equals(".json",StringComparison.OrdinalIgnoreCase)){WriteJson(path,row);return;}
        string Pretty(JsonNode? value)=>value?.ToJsonString(ChatJson.Indented)??"null";
        DecisionRecipeValidator.Number(row["createdAt"],out var created);var name=DecisionRecipeValidator.String(row["recipe"]!["name"])!.Replace('\n',' ').Replace('\r',' ');
        WriteText(path,$"# {row.GetString("id")}\n\n- Recipe: {name}\n- Status: {row.GetString("status")}\n- Created: {DateTimeOffset.FromUnixTimeMilliseconds((long)(created*1000)):yyyy-MM-ddTHH:mm:ss+00:00}\n- Model: {DecisionRecipeValidator.String(row["request"]!["model"])}\n\n## Input\n\n```json\n{Pretty(row["input"])}\n```\n\n## Results\n\n```json\n{Pretty(row["answers"])}\n```\n\n## Full record\n"+Marker+Pretty(row)+"\n```\n");
    }
    public List<(string Path,JsonObject Row)> ReadRuns()
    {
        var result=new List<(string,JsonObject)>();if(!Directory.Exists(HistoryPath))return result;CheckPath(HistoryPath);
        foreach(var directory in Directory.EnumerateDirectories(HistoryPath)){
            CheckPath(directory);foreach(var path in Directory.EnumerateFiles(directory).Where(p=>Path.GetExtension(p).ToLowerInvariant() is ".json" or ".md")){
                var row=ReadHistory(path);
                if(Active(row)&&DecisionRecipeValidator.Number(row["leaseUntil"],out var expiry)&&expiry<Now){row["status"]="interrupted";row["completedAt"]=Now;row["error"]="The server stopped before this request completed. It has not been automatically retried.";WriteHistory(path,row);}result.Add((path,row));
            }
        }return result;
    }
    public static bool Active(JsonObject row)=>row.GetString("status") is "pending" or "running";
    static double Number(JsonNode? value){DecisionRecipeValidator.Number(value,out var number);return number;}
    static JsonObject Metadata(JsonObject index)=>(JsonObject)index["recipes"]!;
    public List<JsonObject> ReadRecipes(JsonObject index)
    {
        var rows=new List<JsonObject>();var old=Metadata(index).Clone();var migrating=!index.GetBool("filenameStemIdentities");var mapping=new Dictionary<string,string>();
        if(Directory.Exists(RecipesPath)){
            CheckPath(RecipesPath);
            foreach(var path in Directory.EnumerateFiles(RecipesPath).Where(p=>Path.GetExtension(p).Equals(".json",StringComparison.OrdinalIgnoreCase))) {
                var filename=JevPaths.Filename(Path.GetFileName(path));var id=JevPaths.Reference(filename[..^5]);
                if(rows.Any(r=>JevFilenameCase.Fold(r.GetString("id")!)==JevFilenameCase.Fold(id)))throw new JevConflictException($"The recipe file \"{id}\" already exists with different casing.");
                var document=DecisionRecipeValidator.Validate(ReadJson(path));var hash=JevJson.Hash(document);
                var meta=old[migrating&&index.GetBool("filenameIdentities")?filename:id] as JsonObject;
                if(meta==null){var timestamp=new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeMilliseconds()/1000.0;meta=new JsonObject {["revision"]=1,["createdAt"]=timestamp,["updatedAt"]=timestamp,["hash"]=hash};}
                else if(meta.GetString("hash")!=hash){meta["revision"]=(long)Number(meta["revision"])+1;meta["updatedAt"]=Now;meta["hash"]=hash;}
                if(migrating){if(index.GetBool("filenameIdentities"))mapping[filename]=id;var previous=(meta["previousIds"] as JsonArray??new JsonArray()).Select(DecisionRecipeValidator.String).Append(filename).Where(x=>x!=id).Distinct();meta["previousIds"]=new JsonArray(previous.Select(x=>(JsonNode?)JsonValue.Create(x)).ToArray());}
                Metadata(index)[id]=meta.Clone();rows.Add(RecipeRow(id,filename,document,meta));
            }
        }
        if(migrating) {
            index["favourites"]=new JsonArray(((JsonArray)index["favourites"]!).Select(x=>(JsonNode?)JsonValue.Create(mapping.GetValueOrDefault(x!.GetValue<string>(),x.GetValue<string>()))).ToArray());
            var references=ReadJson(Path.Combine(Root,".references.json")) as JsonObject;
            var runs=ReadRuns();
            if(references==null){references=new JsonObject();foreach(var (_,row) in runs)if(row.GetString("recipeId") is { } oldId)references[row.GetString("id")!]=JevPaths.Reference(mapping.GetValueOrDefault(oldId,index.GetBool("filenameIdentities")&&oldId.EndsWith(".json",StringComparison.OrdinalIgnoreCase)?oldId[..^5]:oldId));if(references.Count>0)WriteJson(Path.Combine(Root,".references.json"),references);}
            foreach(var (path,row) in runs)if(row.GetString("recipeId")!=null){var id=JevPaths.Reference(references.GetString(row.GetString("id")!)!);row["recipeId"]=id;var target=Path.Combine(HistoryPath,id,Path.GetFileName(path));WriteHistory(target,row);if(!SamePath(target,path))Delete(path);}
            index["filenameStemIdentities"]=true;
        }
        var existing=rows.Select(r=>r.GetString("id")!).ToHashSet(StringComparer.Ordinal);foreach(var key in Metadata(index).Select(p=>p.Key).Where(k=>!existing.Contains(k)).ToArray())Metadata(index).Remove(key);
        index["favourites"]=new JsonArray(((JsonArray)index["favourites"]!).Where(x=>existing.Contains(x!.GetValue<string>())).Select(x=>x!.DeepClone()).ToArray());
        return rows.OrderByDescending(r=>Number(r["updatedAt"])).ThenBy(r=>r.GetString("id"),StringComparer.Ordinal).ToList();
    }
    static bool SamePath(string a,string b)=>string.Equals(Path.GetFullPath(a),Path.GetFullPath(b),OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal);
    static JsonObject RecipeRow(string id,string filename,JsonObject document,JsonObject metadata)
    {
        var row=new JsonObject {["id"]=id,["filename"]=filename,["document"]=document.DeepClone()};foreach(var pair in metadata)if(pair.Key is not ("hash" or "pendingPublication"))row[pair.Key]=pair.Value?.DeepClone();return row;
    }
    public List<JsonObject> Recipes()=>Transaction(ReadRecipes);
    public JsonObject? Recipe(string identity)
    {
        JevPaths.Reference(identity);return Transaction(index=>{
            var row=ReadRecipes(index).FirstOrDefault(r=>r.GetString("id")==identity);
            var reference=DecisionRecipeValidator.String(row?["importSource"]?["exampleRef"]);
            if(reference!=null&&System.Text.RegularExpressions.Regex.IsMatch(reference,@"\A[a-f0-9]{32}\.json\z")&&ReadJson(Path.Combine(Root,"source-examples",reference)) is { } snapshot) {
                row!["sourceExample"]=(snapshot as JsonObject)?.ContainsKey("execution")==true?snapshot["execution"]?.DeepClone():snapshot.DeepClone();
                row["sourceDocument"]=(snapshot as JsonObject)?.ContainsKey("document")==true?snapshot["document"]?.DeepClone():row["document"]?.DeepClone();
            }return row;
        });
    }
    void RecoverRenames(JsonObject index)
    {
        if(ReadJson(Path.Combine(Root,".renames.json")) is not JsonObject renames)return;
        foreach(var (oldId,value) in renames){
            JevPaths.Reference(oldId);var pending=(JsonObject)value!;var document=DecisionRecipeValidator.Validate(pending["document"]);var id=JevPaths.Reference(document.GetString("name")!);
            WriteJson(Path.Combine(RecipesPath,id+".json"),document);
            foreach(var (path,row) in ReadRuns().Where(r=>r.Row.GetString("recipeId")==oldId)){row["recipeId"]=id;var target=Path.Combine(HistoryPath,id,Path.GetFileName(path));WriteHistory(target,row);if(!SamePath(path,target))Delete(path);}
            index["favourites"]=new JsonArray(((JsonArray)index["favourites"]!).Select(x=>(JsonNode?)JsonValue.Create(x!.GetValue<string>()==oldId?id:x.GetValue<string>())).ToArray());Metadata(index).Remove(oldId);Metadata(index)[id]=pending["metadata"]?.DeepClone();
            if(!SamePath(Path.Combine(RecipesPath,oldId+".json"),Path.Combine(RecipesPath,id+".json")))Delete(Path.Combine(RecipesPath,oldId+".json"));
        }
    }
    public JsonObject Save(JsonObject document,string? identity=null,long? revision=null,long? replacement=null,string? filename=null)=>Transaction(index=>SaveCore(index,DecisionRecipeValidator.Validate(document),identity,revision,replacement,filename));
    JsonObject SaveCore(JsonObject index,JsonObject document,string? identity,long? revision,long? replacement,string? filename,bool initializing=false,bool dryRun=false)
    {
        var rows=ReadRecipes(index);JsonObject? existing;
        if(identity!=null){JevPaths.Reference(identity);existing=rows.FirstOrDefault(r=>r.GetString("id")==identity);if(existing==null||Number(existing["revision"])!=revision)throw new JevConflictException("This recipe changed in another tab or on disk. Reload it or save your edits as a copy.");}
        else {
            filename=JevPaths.Filename(filename??JevPaths.DefaultFilename(document.GetString("name")!));identity=JevPaths.Reference(filename[..^5]);existing=rows.FirstOrDefault(r=>JevFilenameCase.Fold(r.GetString("id")!)==JevFilenameCase.Fold(identity!));
            if(existing!=null){if(initializing)return existing;if(replacement==null)throw new JevRecipeExistsException(existing);if(Number(existing["revision"])!=replacement)throw new JevConflictException("This recipe changed before replacement. Import it again to review the warning.");identity=existing.GetString("id")!;}
            else if(replacement is not (null or 0))throw new JevConflictException("This recipe changed before replacement. Import it again to review the warning.");
        }
        var history=ReadRuns().Where(r=>r.Row.GetString("recipeId")==identity).ToList();
        if(existing==null&&history.Count>0&&replacement==null&&!initializing)throw new JevRecipeExistsException(new JsonObject {["id"]=identity,["revision"]=0});
        if(replacement!=null&&history.Any(r=>Active(r.Row)))throw new JevConflictException("Stop this recipe's active decisions before replacing it.");
        var old=Metadata(index)[identity] as JsonObject??new JsonObject();var meta=old.Clone();meta["revision"]=existing!=null?(long)Number(existing["revision"])+1:1;meta["createdAt"]=existing?["createdAt"]?.DeepClone()??JsonValue.Create(Now);meta["updatedAt"]=Now;meta["hash"]=JevJson.Hash(document);
        if(revision==null)foreach(var key in new[]{"publication","pendingPublication","importSource"})meta.Remove(key);
        if(initializing)meta["templateId"]="sentiment";filename=existing?.GetString("filename")??filename!;
        var journal=new JsonObject {["identity"]=identity,["filename"]=filename,["document"]=document.DeepClone(),["metadata"]=meta,["oldMetadata"]=revision==null?old.Clone():new JsonObject(),["historyPaths"]=new JsonArray((replacement!=null?history.Select(r=>Path.GetRelativePath(HistoryPath,r.Path).Replace(Path.DirectorySeparatorChar,'/')):[]).Select(x=>(JsonNode?)JsonValue.Create(x)).ToArray())};
        if(dryRun)return journal;
        WriteJson(Path.Combine(Root,".shared-import.json"),journal);ApplyJournal(index,journal);return RecipeRow(identity,filename,document,meta);
    }
    void ApplyJournal(JsonObject index,JsonObject journal)
    {
        var id=JevPaths.Reference(journal.GetString("identity"));var filename=JevPaths.Filename(journal.GetString("filename"));if(filename[..^5]!=id)throw new JevStorageException("The shared import journal has an invalid filename.");
        var paths=new List<string>();foreach(var value in (JsonArray)journal["historyPaths"]!){
            var relative=value!.GetValue<string>();var parts=relative.Split('/');
            if(parts.Length!=2||parts[0]!=id||Path.GetExtension(parts[1]) is not (".json" or ".md"))throw new JevStorageException("The shared import journal has an invalid history path.");JevPaths.RunId(Path.GetFileNameWithoutExtension(parts[1]));paths.Add(Path.Combine(HistoryPath,parts[0],parts[1]));
        }
        var document=DecisionRecipeValidator.Validate(journal["document"]);WriteJson(Path.Combine(RecipesPath,filename),document);foreach(var path in paths)Delete(path);ClearSidecars(journal["oldMetadata"] as JsonObject??new JsonObject());Metadata(index)[id]=journal["metadata"]?.DeepClone();
    }
    public JsonObject ImportShared(JsonObject document,string filename,long? replacement,JsonObject source,JsonObject execution)=>Transaction(index=>{
        var journal=SaveCore(index,document,null,null,replacement,filename,dryRun:true);journal["metadata"]!["importSource"]=source.DeepClone();
        var example=source.GetString("exampleRef");if(example==null||!System.Text.RegularExpressions.Regex.IsMatch(example,@"\A[a-f0-9]{32}\.json\z"))throw new JevStorageException("Invalid source example reference.");
        WriteJson(Path.Combine(Root,"source-examples",example),new JsonObject {["document"]=document.DeepClone(),["execution"]=execution.DeepClone()});WriteJson(Path.Combine(Root,".shared-import.json"),journal);ApplyJournal(index,journal);
        var row=RecipeRow(journal.GetString("identity")!,filename,document,(JsonObject)journal["metadata"]!);row["sourceExample"]=execution.DeepClone();row["sourceDocument"]=document.DeepClone();return row;
    });
    void ClearSidecars(JsonObject metadata)
    {
        if(DecisionRecipeValidator.String(metadata["importSource"]?["exampleRef"]) is { } example&&System.Text.RegularExpressions.Regex.IsMatch(example,@"\A[a-f0-9]{32}\.json\z"))Delete(Path.Combine(Root,"source-examples",example));
        if(DecisionRecipeValidator.String(metadata["pendingPublication"]?["idempotencyKey"]) is { } pending&&System.Text.RegularExpressions.Regex.IsMatch(pending,@"\A[a-f0-9]{32}\z"))Delete(Path.Combine(Root,"pending",pending+".json"));
    }
    public int DeleteRecipe(string identity)
    {
        JevPaths.Reference(identity);return Transaction(index=>{var row=ReadRecipes(index).FirstOrDefault(r=>r.GetString("id")==identity);if(row!=null)Delete(Path.Combine(RecipesPath,row.GetString("filename")!));ClearSidecars(Metadata(index)[identity] as JsonObject??new JsonObject());Metadata(index).Remove(identity);index["favourites"]=new JsonArray(((JsonArray)index["favourites"]!).Where(x=>x!.GetValue<string>()!=identity).Select(x=>x!.DeepClone()).ToArray());return row!=null?1:0;});
    }
    public List<string> Favourites()=>Transaction(index=>{ReadRecipes(index);return ((JsonArray)index["favourites"]!).Select(x=>x!.GetValue<string>()).ToList();});
    public void Favourite(string identity,bool enabled)=>Transaction(index=>{
        if(!ReadRecipes(index).Any(r=>r.GetString("id")==identity))throw HttpError.NotFound("Recipe not found.");
        var list=((JsonArray)index["favourites"]!).Where(x=>x!.GetValue<string>()!=identity).Select(x=>x!.DeepClone()).ToList();if(enabled)list.Add(JsonValue.Create(identity)!);index["favourites"]=new JsonArray(list.ToArray());return 0;
    });
    public (JsonObject Run,bool Created) Submit(string submission,JsonObject recipe,JsonNode? inputs,JsonObject request,string owner,string? recipeId=null,long? revision=null)
    {
        var fingerprint=JevJson.Fingerprint(new JsonObject {["recipe"]=recipe.DeepClone(),["input"]=inputs?.DeepClone()});
        return Transaction(index=>{
            var rows=ReadRuns();var old=rows.FirstOrDefault(r=>r.Row.GetString("submissionId")==submission).Row;
            if(old!=null){if(old.GetString("requestHash")!=fingerprint)throw new JevConflictException("This submission ID already belongs to different input.");return (RunRow(old),false);}
            if(recipeId!=null){var source=ReadRecipes(index).FirstOrDefault(r=>r.GetString("id")==recipeId);if(source==null||Number(source["revision"])!=revision)throw new JevConflictException("This recipe changed before submission. Reload it before running again.");}
            if(rows.Count(r=>Active(r.Row))>=2)throw new JevBusyException();
            var group=recipeId!=null?JevPaths.Reference(recipeId):"_drafts";var counters=index["historySequences"] as JsonObject??new JsonObject();index["historySequences"]=counters;var last=(long)Number(counters[group]);var prefix=group+"-";
            foreach(var (_,row) in rows)if(row.GetString("id") is { } id&&id.StartsWith(prefix,StringComparison.Ordinal)&&long.TryParse(id[prefix.Length..],NumberStyles.None,CultureInfo.InvariantCulture,out var sequence))last=Math.Max(last,sequence);
            var identity=JevPaths.RunId(prefix+(last+1).ToString("D5",CultureInfo.InvariantCulture));counters[group]=last+1;WriteJson(IndexPath,index);
            var now=Now;var record=new JsonObject {["id"]=identity,["submissionId"]=submission,["requestHash"]=fingerprint,["recipeId"]=recipeId,["recipeRevision"]=revision,["recipe"]=recipe.DeepClone(),["input"]=inputs?.DeepClone(),["request"]=request.DeepClone(),["status"]="pending",["owner"]=owner,["leaseUntil"]=now+90,["createdAt"]=now,["completedAt"]=null,["durationMs"]=null,["response"]=null,["answers"]=null,["error"]=null};
            WriteHistory(Path.Combine(HistoryPath,group,identity+".md"),record);return (RunRow(record),true);
        });
    }
    public static JsonObject RunRow(JsonObject record,bool summary=false)
    {
        var result=record.Clone();result["name"]=result["recipe"]!["name"]?.DeepClone();result["model"]=(result["response"] as JsonObject)?["model"]?.DeepClone()??result["request"]!["model"]?.DeepClone();result["usage"]=result["response"] is JsonObject response&&response["usage"] is JsonObject?response["usage"]?.DeepClone():null;
        foreach(var name in new[]{"owner","leaseUntil","requestHash","submissionId"})result.Remove(name);if(summary)foreach(var name in new[]{"recipe","input","request","response","answers"})result.Remove(name);return result;
    }
    public JsonObject? Run(string identity){JevPaths.RunId(identity);return Transaction(_=>ReadRuns().Where(r=>r.Row.GetString("id")==identity).Select(r=>RunRow(r.Row)).FirstOrDefault());}
    public JsonObject History(string? recipeId=null,string? cursor=null,int limit=20)
    {
        if(recipeId!=null)JevPaths.Reference(recipeId);(double Stamp,string Id)? boundary=null;
        if(cursor!=null){var parts=cursor.Split(':',2);if(parts.Length!=2||!double.TryParse(parts[0],NumberStyles.Float,CultureInfo.InvariantCulture,out var stamp)||!double.IsFinite(stamp))throw new JevValidationException("cursor","Invalid history cursor.");boundary=(stamp,JevPaths.RunId(parts[1]));}
        return Transaction(_=>{
            var rows=ReadRuns().Select(r=>r.Row).Where(r=>(recipeId==null||r.GetString("recipeId")==recipeId)&&(boundary==null||Number(r["createdAt"])<boundary.Value.Stamp||Number(r["createdAt"])==boundary.Value.Stamp&&string.CompareOrdinal(r.GetString("id"),boundary.Value.Id)<0)).OrderByDescending(r=>Number(r["createdAt"])).ThenByDescending(r=>r.GetString("id"),StringComparer.Ordinal).ToList();var page=rows.Take(Math.Clamp(limit,1,50)).ToList();
            return new JsonObject {["items"]=new JsonArray(page.Select(r=>(JsonNode)RunRow(r,true)).ToArray()),["cursor"]=rows.Count>page.Count?Number(page[^1]["createdAt"]).ToString("R",CultureInfo.InvariantCulture)+":"+page[^1].GetString("id"):null};
        });
    }
    public bool Start(string identity,string owner)=>Transition(identity,row=>row.GetString("owner")==owner&&row.GetString("status")=="pending",row=>row["status"]="running");
    public bool Finish(string identity,string status,JsonNode? response=null,JsonObject? answers=null,string? error=null,string? owner=null)=>Transition(identity,row=>Active(row)&&(owner==null||row.GetString("owner")==owner),row=>{
        var now=Now;row["status"]=status;row["response"]=response?.DeepClone();row["answers"]=answers?.DeepClone();row["error"]=error;row["completedAt"]=now;row["durationMs"]=(long)((now-Number(row["createdAt"]))*1000);
    });
    bool Transition(string identity,Func<JsonObject,bool> condition,Action<JsonObject> mutate)
    {
        JevPaths.RunId(identity);return Transaction(_=>{foreach(var (path,row) in ReadRuns())if(row.GetString("id")==identity&&condition(row)){mutate(row);WriteHistory(path,row);return true;}return false;},true);
    }
    public int DeleteRun(string? identity=null)
    {
        if(identity!=null)JevPaths.RunId(identity);return Transaction(_=>{var count=0;foreach(var (path,row) in ReadRuns()){if(identity!=null&&row.GetString("id")!=identity)continue;if(Active(row)){if(identity!=null)throw new JevConflictException("Stop this decision before deleting it.");continue;}Delete(path);count++;}return count;});
    }
}
