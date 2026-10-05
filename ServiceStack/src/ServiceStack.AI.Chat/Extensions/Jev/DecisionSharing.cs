using System.Text.Json.Nodes;
using static ServiceStack.AI.DecisionRecipeValidator;

namespace ServiceStack.AI;

/// <summary>Immutable publication reservations, account-bound receipts and independent attributed imports.</summary>
public sealed class DecisionSharing(ChatFeature feature,DecisionImporter importer)
{
    PublisherClient Client(string user)=>feature.PublisherApi.Available?feature.PublisherApi.CreateClient(user):throw HttpError.ServiceUnavailable("Enable the publish extension to share and browse recipes.");
    static JsonObject Object(JsonNode? value)=>value as JsonObject??throw new JevValidationException("source","Expected a publication.");
    static JsonObject Meta(JsonObject index,string id)=>(JsonObject)index["recipes"]![id]!;
    // A share belongs to the publisher origin and account (Python parity), not to one API key:
    // reconnecting the same account with a rotated key must keep updating/revoking its shares.
    static bool Bound(JsonObject? binding,PublisherClient client)=>binding!=null&&binding.GetString("baseUrl")==client.BaseUrl&&
        binding["publisherAccount"]!=null&&JsonNode.DeepEquals(binding["publisherAccount"],client.Configuration["userId"]);
    static List<JsonObject> Eligible(JevStore store,string id,JsonObject doc)
    {
        var result=new List<JsonObject>();foreach(var (_,run) in store.ReadRuns().Where(r=>r.Row.GetString("recipeId")==id))try{result.Add(new JsonObject {["id"]=run["id"]?.DeepClone(),["execution"]=DecisionRecorded.Package(doc,run)});}catch(Exception e) when(e is JevValidationException or ArgumentException or InvalidOperationException or OverflowException){}
        return result.OrderByDescending(r=>r["execution"]!.GetValueKind()==System.Text.Json.JsonValueKind.Object?String(r["execution"]!["completedAt"]):"").ThenByDescending(r=>r.GetString("id"),StringComparer.Ordinal).ToList();
    }
    static JsonObject Usage(JevStore store,JsonObject index,string id)=>new(){["publisherStarred"]=((JsonArray)index["favourites"]!).Any(x=>String(x)==id),["publisherRunCount"]=store.ReadRuns().Count(r=>r.Row.GetString("recipeId")==id&&r.Row.GetString("status")!="pending")};
    public async Task<JsonObject> StatusAsync(string user,JevStore store,string id,CancellationToken token)
    {
        var client=Client(user);
        var captured=store.Transaction(index=>{
            var row=store.ReadRecipes(index).FirstOrDefault(r=>r.GetString("id")==id)??throw HttpError.NotFound("Recipe not found.");var metadata=Meta(index,id);
            var pending=metadata["pendingPublication"] as JsonObject;JsonObject? reservation=null;
            if(pending!=null){var key=pending.GetString("idempotencyKey")!;ValidateKey(key);reservation=new JsonObject {["reservation"]=new JsonObject {["savedRevision"]=pending["savedRevision"]?.DeepClone(),["sourceRunId"]=pending["sourceRunId"]?.DeepClone(),["includeAdditionalExamples"]=pending["includeAdditionalExamples"]?.DeepClone()},["payload"]=JevStore.ReadJson(Path.Combine(store.Root,"pending",key+".json"))};}
            return new JsonObject {["recipe"]=row,["publication"]=metadata["publication"]?.DeepClone(),["pendingPublication"]=reservation,["runs"]=new JsonArray(Eligible(store,id,(JsonObject)row["document"]!).Select(r=>(JsonNode)r).ToArray()),["usage"]=Usage(store,index,id)};
        });
        var binding=captured["publication"] as JsonObject;if(!Bound(binding,client))binding=null;
        if(binding!=null){
            var reference=PublisherClient.PublicReference(binding.GetString("externalRef")!);
            try{
                var remote=Object(await client.SendAsync(HttpMethod.Get,"/publish/decision/"+reference,null,false,token).ConfigureAwait(false));var changed=binding.GetBool("remoteChanged")||binding.GetString("contentHash")!=remote.GetString("contentHash");
                binding["publicRevision"]=remote["revision"]?.DeepClone();binding["contentHash"]=remote["contentHash"]?.DeepClone();binding["remoteChanged"]=changed;
                store.Transaction(index=>{if(index["recipes"]?[id]?["publication"] is JsonObject current&&current.GetString("externalRef")==reference){current["publicRevision"]=binding["publicRevision"]?.DeepClone();current["contentHash"]=binding["contentHash"]?.DeepClone();current["remoteChanged"]=changed;}return 0;});
            }catch(HttpError e) when((int)e.StatusCode==404){
                await client.SendAsync(HttpMethod.Delete,"/publish/decision/"+reference+"?revision="+binding.GetLong("publicRevision"),null,true,token).ConfigureAwait(false);
                store.Transaction(index=>{if(index["recipes"]?[id]?["publication"] is JsonObject current&&current.GetString("externalRef")==reference)((JsonObject)index["recipes"]![id]!).Remove("publication");return 0;});binding=null;
            }
        }
        captured["publication"]=binding?.DeepClone();var config=client.Configuration;if(config.GetString("apiKey") is {Length:>0} keyValue)config["apiKey"]=PublisherConfiguration.Mask(keyValue);captured["account"]=config;
        captured["message"]=((JsonArray)captured["runs"]!).Count>0?"":"Run this recipe first";var doc=(JsonObject)captured["recipe"]!["document"]!;
        captured["savedChangesNotShared"]=binding!=null&&(binding.GetBool("remoteChanged")||binding["includeAdditionalExamples"]?.GetValue<bool>()==false&&(doc["examples"] as JsonArray)?.Count>0||binding.GetString("savedDocumentHash")!=JevJson.Hash(doc)||((JsonObject)captured["usage"]!).Any(p=>!Equivalent(binding[p.Key],p.Value)));
        return captured;
    }
    static void ValidateKey(string key)=>Require(System.Text.RegularExpressions.Regex.IsMatch(key,@"\A[a-f0-9]{32}\z"),"publication","Pending publication journal is invalid.");
    public async Task<JsonObject> PublishAsync(string user,JevStore store,string id,JsonObject body,CancellationToken token)
    {
        var client=Client(user);
        var pair=store.Transaction(index=>{
            var row=store.ReadRecipes(index).FirstOrDefault(r=>r.GetString("id")==id);Require(row!=null,"recipe","Save this recipe first.");var metadata=Meta(index,id);
            if(metadata["pendingPublication"] is JsonObject reserved){Require(Bound(reserved,client),"account","Reconnect the account with the pending publication to recover its outcome.");var key=reserved.GetString("idempotencyKey")!;ValidateKey(key);var old=JevStore.ReadJson(Path.Combine(store.Root,"pending",key+".json"));Require(old is JsonObject,"publication","Pending publication journal is missing.");return(reserved.Clone(),(JsonObject)old!);}
            Require(Equivalent(row!["revision"],body["revision"]),"revision","Saved recipe changed. Refresh before sharing.");var document=Validate(row["document"]);var run=store.ReadRuns().FirstOrDefault(r=>r.Row.GetString("id")==body.GetString("runId")&&r.Row.GetString("recipeId")==id).Row;Require(run!=null,"runId","Select an execution from this recipe's local history.");
            var payload=new JsonObject {["filename"]=row["filename"]?.DeepClone(),["document"]=document.DeepClone(),["execution"]=DecisionRecorded.Package(document,run!)};foreach(var flag in Usage(store,index,id))payload[flag.Key]=flag.Value?.DeepClone();
            var pending=new JsonObject {["baseUrl"]=client.BaseUrl,["publisherAccount"]=client.Configuration["userId"]?.DeepClone(),["idempotencyKey"]=Guid.NewGuid().ToString("N"),["savedRevision"]=row["revision"]?.DeepClone(),["savedDocumentHash"]=JevJson.Hash(document),["sourceRunId"]=run!["id"]?.DeepClone(),["includeAdditionalExamples"]=true,["payloadHash"]=JevJson.Hash(payload),["publisherPayloadHash"]=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload.ToJsonString(ChatJson.Options)))).ToLowerInvariant()};
            if(metadata["publication"] is JsonObject binding&&Bound(binding,client)){Require(Equivalent(body["publishedRevision"],binding["publicRevision"]),"publishedRevision","Review the latest public revision before updating.");pending["externalRef"]=binding["externalRef"]?.DeepClone();pending["publicRevision"]=body["publishedRevision"]?.DeepClone();}
            pending["pendingPayloadRef"]="pending/"+pending.GetString("idempotencyKey")+".json";JevStore.WriteJson(Path.Combine(store.Root,"pending",pending.GetString("idempotencyKey")!+".json"),payload);metadata["pendingPublication"]=pending.Clone();return(pending,payload);
        });
        var (reservation,payload)=pair;JsonObject receipt;
        if(reservation.GetString("externalRef") is { } reference){
            reference=PublisherClient.PublicReference(reference);var current=Object(await client.SendAsync(HttpMethod.Get,"/publish/decision/"+reference,null,false,token).ConfigureAwait(false));
            var currentPayload=new JsonObject(payload.Select(p=>new KeyValuePair<string,JsonNode?>(p.Key,current[p.Key]?.DeepClone())));
            if(JevJson.Hash(currentPayload)==reservation.GetString("payloadHash") || reservation.GetString("publisherPayloadHash") is { } expectedHash && current.GetString("contentHash")==expectedHash)receipt=current;
            else if(!Equivalent(current["revision"],reservation["publicRevision"])){
                store.Transaction(index=>{if(index["recipes"]?[id] is JsonObject metadata&&metadata["pendingPublication"]?.GetValueKind()==System.Text.Json.JsonValueKind.Object&&String(metadata["pendingPublication"]!["idempotencyKey"])==reservation.GetString("idempotencyKey")){JevStore.WriteJson(Path.Combine(store.Root,"publication-conflicts",reservation.GetString("idempotencyKey")!+".json"),new JsonObject {["reservation"]=reservation.Clone(),["payload"]=payload.Clone(),["remote"]=current.Clone()});metadata.Remove("pendingPublication");File.Delete(Path.Combine(store.Root,"pending",reservation.GetString("idempotencyKey")!+".json"));if(metadata["publication"] is JsonObject binding){binding["publicRevision"]=current["revision"]?.DeepClone();binding["contentHash"]=current["contentHash"]?.DeepClone();binding["remoteChanged"]=true;}}return 0;});
                throw new JevConflictException("The public recipe changed. Review it before updating.");
            }else{var update=payload.Clone();update["revision"]=reservation["publicRevision"]?.DeepClone();receipt=Object(await client.SendAsync(HttpMethod.Put,"/publish/decision/"+reference,update,true,token).ConfigureAwait(false));}
        }else{var create=payload.Clone();create["idempotencyKey"]=reservation["idempotencyKey"]?.DeepClone();receipt=Object(await client.SendAsync(HttpMethod.Post,"/publish/decision",create,true,token).ConfigureAwait(false));}
        PublisherClient.PublicReference(receipt.GetString("externalRef")!);Require(Integer(receipt["revision"],out var revision)&&revision>0,"publication","Invalid publication receipt.");DecisionRecipeValidator.Text(receipt["contentHash"],"contentHash",64);
        Require(PublisherClient.ReferenceFromUrl(client.Configuration,receipt.GetString("publishedUrl")!)==receipt.GetString("externalRef"),"publication","Invalid published URL.");
        store.Transaction(index=>{
            store.ReadRecipes(index);if(index["recipes"]?[id] is JsonObject metadata&&metadata["pendingPublication"] is JsonObject pending&&pending.GetString("idempotencyKey")==reservation.GetString("idempotencyKey")){
                var binding=reservation.Clone();foreach(var key in new[]{"idempotencyKey","pendingPayloadRef","payloadHash","publisherPayloadHash"})binding.Remove(key);binding["externalRef"]=receipt["externalRef"]?.DeepClone();binding["publishedUrl"]=receipt["publishedUrl"]?.DeepClone();binding["publicRevision"]=receipt["revision"]?.DeepClone();binding["contentHash"]=receipt["contentHash"]?.DeepClone();foreach(var key in new[]{"publisherStarred","publisherRunCount"})binding[key]=payload[key]?.DeepClone();metadata["publication"]=binding;metadata.Remove("pendingPublication");
            }File.Delete(Path.Combine(store.Root,"pending",reservation.GetString("idempotencyKey")!+".json"));return 0;
        });return receipt;
    }
    public async Task<JsonObject> UnpublishAsync(string user,JevStore store,string id,JsonObject body,CancellationToken token)
    {
        var client=Client(user);var binding=store.Recipe(id)?["publication"] as JsonObject;Require(Bound(binding,client),"publication","No share belongs to this publisher account.");Require(Equivalent(body["publishedRevision"],binding!["publicRevision"]),"publishedRevision","Refresh the public revision first.");
        var reference=PublisherClient.PublicReference(binding!.GetString("externalRef")!);await client.SendAsync(HttpMethod.Delete,"/publish/decision/"+reference+"?revision="+binding.GetLong("publicRevision"),null,true,token).ConfigureAwait(false);
        store.Transaction(index=>{if(index["recipes"]?[id]?["publication"] is JsonObject current&&current.GetString("externalRef")==reference)((JsonObject)index["recipes"]![id]!).Remove("publication");return 0;});return new JsonObject {["unpublished"]=true};
    }
    static JsonObject Detail(JsonNode? value)
    {
        DecisionRecorded.Bounded(value);var detail=Object(value);JevPaths.Filename(detail.GetString("filename"));PublisherClient.PublicReference(detail.GetString("externalRef")!);Require(Integer(detail["revision"],out var revision)&&revision>0,"revision","Invalid public revision.");DecisionRecipeValidator.Text(detail["contentHash"],"contentHash",64);var doc=Validate(detail["document"]);detail["document"]=doc;detail["execution"]=DecisionRecorded.Validate(doc,detail["execution"]);return detail;
    }
    public async Task<JsonObject> PreviewAsync(string user,JsonNode? value,CancellationToken token)
    {
        var config=feature.PublisherApi.GetConfiguration(user);var url=DecisionImporter.Url(config,value);
        if(!feature.PublisherApi.Available)return await importer.DownloadAsync(url,token).ConfigureAwait(false);
        string reference;try{reference=PublisherClient.ReferenceFromUrl(config,url);}catch(HttpError){return await importer.DownloadAsync(url,token).ConfigureAwait(false);}
        var client=Client(user);var doc=Validate(await client.SendAsync(HttpMethod.Get,"/d/"+reference+".json",null,false,token).ConfigureAwait(false));var detail=Detail(await client.SendAsync(HttpMethod.Get,"/publish/decision/"+reference,null,false,token).ConfigureAwait(false));
        if(JevJson.Hash(doc)!=JevJson.Hash(detail["document"]))throw new JevConflictException("The public recipe changed. Refresh the preview before importing.");detail["document"]=doc;detail["downloadUrl"]=url;return detail;
    }
    public Task<JsonNode?> TagsAsync(string user,CancellationToken token)=>Client(user).SendAsync(HttpMethod.Get,"/publish/decisions/tags",null,false,token);
    public Task<JsonNode?> CatalogAsync(string user,IEnumerable<KeyValuePair<string,string?>> query,CancellationToken token)
    {
        var allowed=query.Where(p=>new[]{"q","tag","user","skip","take","orderBy"}.Contains(p.Key)).ToDictionary(p=>p.Key,p=>p.Value);
        Require(int.TryParse(allowed.GetValueOrDefault("take")??"20",out var take)&&int.TryParse(allowed.GetValueOrDefault("skip")??"0",out _),"pagination","Use integer pagination values.");var skip=int.Parse(allowed.GetValueOrDefault("skip")??"0");allowed["take"]=Math.Clamp(take,1,50).ToString();allowed["skip"]=Math.Clamp(skip,0,10000).ToString();var client=Client(user);
        var encoded=string.Join("&",allowed.Select(p=>System.Net.WebUtility.UrlEncode(p.Key)+"="+System.Net.WebUtility.UrlEncode(p.Value)));
        return client.SendAsync(HttpMethod.Get,"/publish/decisions?"+encoded,null,!string.IsNullOrEmpty(client.Configuration.GetString("apiKey")),token);
    }
    public Task<JsonNode?> StarAsync(string user,string reference,JsonObject body,CancellationToken token)
    {
        Require(Boolean(body["starred"],out var starred),"starred","Provide true or false.");return Client(user).SendAsync(HttpMethod.Put,"/publish/decision/"+PublisherClient.PublicReference(reference)+"/star",new JsonObject {["starred"]=starred},true,token);
    }
    public async Task<JsonObject> ImportAsync(string user,JevStore store,JsonObject body,CancellationToken token)
    {
        var client=Client(user);var reference=PublisherClient.PublicReference(body.GetString("externalRef")!);var detail=await PreviewAsync(user,JsonValue.Create(client.BaseUrl+"/d/"+reference),token).ConfigureAwait(false);
        if(!Equivalent(detail["revision"],body["publishedRevision"])||detail.GetString("contentHash")!=body.GetString("contentHash"))throw new JevConflictException("The public recipe changed. Refresh the preview before importing.");
        var filename=JevPaths.Filename(body.GetString("filename")??detail.GetString("filename"));long? replacement=null;if(body.ContainsKey("replaceRevision")){Require(Integer(body["replaceRevision"],out var revision)&&revision>=0,"replaceRevision","Provide the recipe revision to replace when importing.");replacement=revision;}
        var source=new JsonObject {["baseUrl"]=client.BaseUrl,["externalRef"]=reference,["publishedUrl"]=client.BaseUrl+"/d/"+reference,["author"]=detail["author"]?.DeepClone(),["publicRevision"]=detail["revision"]?.DeepClone(),["contentHash"]=detail["contentHash"]?.DeepClone(),["importedAt"]=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000.0,["exampleRef"]=Guid.NewGuid().ToString("N")+".json"};
        return store.ImportShared((JsonObject)detail["document"]!,filename,replacement,source,(JsonObject)detail["execution"]!);
    }
}
