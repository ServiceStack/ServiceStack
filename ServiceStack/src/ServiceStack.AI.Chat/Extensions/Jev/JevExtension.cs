using System.Text;
using System.Text.Json.Nodes;
using ServiceStack.Text;
using static ServiceStack.AI.DecisionRecipeValidator;

namespace ServiceStack.AI;

/// <summary>Decision Studio routes, partitioned by the host's authenticated identity.</summary>
public sealed class JevExtension():ChatExtension("jev")
{
    public Func<HttpMessageHandler>? DecisionHandlerFactory {get;set;}
    public Func<HttpMessageHandler>? DownloadHandlerFactory {get;set;}
    public DecisionClient Client {get;private set;}=null!;
    public DecisionExecutor Executor {get;private set;}=null!;
    public DecisionAuthoring Authoring {get;private set;}=null!;
    public DecisionImporter Importer {get;private set;}=null!;
    public DecisionSharing Sharing {get;private set;}=null!;
    public JsonObject? SeedRecipe {get;set;}
    JevStore Store(string user)=>new(Ctx.GetUserPath(user),SeedRecipe??(JevAssets.Read("ext/jev/recipes/sentiment.json") is { } seed?Validate(JsonNode.Parse(seed)):null));
    static JsonObject GetRecipe(JevStore store,string id){try{return store.Recipe(id)??throw HttpError.NotFound("Recipe not found.");}catch(JevValidationException){throw HttpError.NotFound("Recipe not found.");}}
    static JsonObject GetRun(JevStore store,string id){try{return store.Run(id)??throw HttpError.NotFound("Decision not found.");}catch(JevValidationException){throw HttpError.NotFound("Decision not found.");}}
    static ChatRouteHandler Guard(ChatRouteHandler handler)=>async req=>{
        try{return await handler(req).ConfigureAwait(false);}
        catch(JevValidationException e){return ChatResult.Json(new JsonObject {["responseStatus"]=new JsonObject {["errorCode"]="ValidationError",["message"]=e.Message,["errors"]=new JsonArray(new JsonObject {["fieldName"]=e.Path,["message"]=e.Detail})}},400);}
        catch(Exception e) when(e is JevConflictException or JevBusyException or JevStorageException){
            var status=e is JevBusyException?429:e is JevStorageException?500:409;var response=new JsonObject {["errorCode"]=e is JevRecipeExistsException?"RecipeExistsError":e is JevBusyException?"BusyError":e is JevStorageException?"StorageError":"ConflictError",["message"]=e.Message};
            if(e is JevRecipeExistsException exists)response["existingRecipe"]=exists.Recipe.Clone();return ChatResult.Json(new JsonObject {["responseStatus"]=response},status);
        }
    };
    static async Task<JsonObject> Body(ChatRequestContext req)
    {
        byte[] bytes;
        if(req.Request is Host.BasicRequest){var raw=await req.Request.GetRawBodyAsync().ConfigureAwait(false);Require(Encoding.UTF8.GetByteCount(raw??"")<=MaxBytes,"document","Keep the request under 512 KB.");bytes=Encoding.UTF8.GetBytes(raw??"");}
        else {
            using var output=new MemoryStream();var buffer=new byte[65536];while(true){var count=await req.Request.InputStream.ReadAsync(buffer,req.Request.RequestAborted).ConfigureAwait(false);if(count==0)break;Require(output.Length+count<=MaxBytes,"document","Keep the request under 512 KB.");output.Write(buffer,0,count);}bytes=output.ToArray();
        }
        JsonNode? value;try{value=JsonNode.Parse(new UTF8Encoding(false,true).GetString(bytes));}catch(Exception e) when(e is System.Text.Json.JsonException or DecoderFallbackException){throw new JevValidationException("document","Provide valid JSON.");}
        Require(value is JsonObject,"document","Expected a JSON object.");return (JsonObject)value!;
    }
    public override void Install(ExtensionContext ctx)
    {
        Client=new DecisionClient(ctx.Feature,DecisionHandlerFactory);Executor=new DecisionExecutor(Client);Authoring=new DecisionAuthoring(ctx);Importer=new DecisionImporter(ctx.Feature,DownloadHandlerFactory);Sharing=new DecisionSharing(ctx.Feature,Importer);
        ctx.RegisterShutdownHandler(_=>Executor.CloseAsync());
        ctx.AddGet("status",Guard(req=>{req.AssertUserName();var(provider,message)=Client.Status();return Task.FromResult<object?>(new JsonObject {["available"]=provider!=null,["message"]=message,["models"]=new JsonArray(Models.Select(m=>(JsonNode?)JsonValue.Create(m)).ToArray())});}));
        // Literal routes precede recipes/{id}, including the compatibility URL-preview endpoint.
        ctx.AddPost("recipes/preview-url",Guard(async req=>await Sharing.PreviewAsync(req.AssertUserName(),(await Body(req).ConfigureAwait(false))["url"],req.Request.RequestAborted).ConfigureAwait(false)));
        ctx.AddPost("shared-recipes/preview",Guard(async req=>await Sharing.PreviewAsync(req.AssertUserName(),(await Body(req).ConfigureAwait(false))["url"],req.Request.RequestAborted).ConfigureAwait(false)));
        ctx.AddPost("shared-recipes/import",Guard(async req=>{var user=req.AssertUserName();return await Sharing.ImportAsync(user,Store(user),await Body(req).ConfigureAwait(false),req.Request.RequestAborted).ConfigureAwait(false);}));
        ctx.AddGet("shared-recipes",Guard(async req=>await Sharing.CatalogAsync(req.AssertUserName(),req.Request.QueryString.AllKeys.Where(k=>k!=null).Select(k=>new KeyValuePair<string,string?>(k!,req.QueryString(k!))),req.Request.RequestAborted).ConfigureAwait(false)));
        ctx.AddGet("tags",Guard(async req=>await Sharing.TagsAsync(req.AssertUserName(),req.Request.RequestAborted).ConfigureAwait(false)));
        ctx.AddPut("shared-recipes/{reference}/star",Guard(async req=>await Sharing.StarAsync(req.AssertUserName(),req.GetPathParam("reference"),await Body(req).ConfigureAwait(false),req.Request.RequestAborted).ConfigureAwait(false)));
        ctx.AddGet("recipes",Guard(req=>{
            var store=Store(req.AssertUserName());var favourites=store.Favourites();var rows=store.Recipes();
            return Task.FromResult<object?>(new JsonObject {["items"]=new JsonArray(rows.Select(row=>(JsonNode)new JsonObject {["id"]=row["id"]?.DeepClone(),["filename"]=row["filename"]?.DeepClone(),["revision"]=row["revision"]?.DeepClone(),["previousIds"]=row["previousIds"]?.DeepClone()??new JsonArray(),["favourite"]=favourites.Contains(row.GetString("id")!),["name"]=row["document"]!["name"]?.DeepClone(),["description"]=row["document"]!["description"]?.DeepClone()??JsonValue.Create(""),["tags"]=row["document"]!["tags"]?.DeepClone()??new JsonArray(),["content"]=row["document"]!["content"]?.DeepClone()??JsonValue.Create(""),["questionCount"]=((JsonObject)row["document"]!["questions"]!).Count}).ToArray())});
        }));
        ctx.AddPost("recipes",Guard(SaveAsync));ctx.AddPut("recipes/{id}",Guard(SaveAsync));
        ctx.AddGet("recipes/{id}",Guard(req=>Task.FromResult<object?>(GetRecipe(Store(req.AssertUserName()),req.GetPathParam("id")))));
        ctx.AddDelete("recipes/{id}",Guard(req=>{var store=Store(req.AssertUserName());var id=req.GetPathParam("id");GetRecipe(store,id);store.DeleteRecipe(id);return Task.FromResult<object?>(new JsonObject {["deleted"]=true});}));
        ctx.AddPut("favourites/{id}",Guard(async req=>{var store=Store(req.AssertUserName());var id=req.GetPathParam("id");GetRecipe(store,id);var body=await Body(req).ConfigureAwait(false);Require(Boolean(body["enabled"],out var enabled),"enabled","Use true or false.");store.Favourite(id,enabled);return new JsonObject {["enabled"]=enabled};}));
        ctx.AddPost("validate",Guard(async req=>{req.AssertUserName();var body=await Body(req).ConfigureAwait(false);var doc=Validate(body["recipe"]);return new JsonObject {["recipe"]=doc,["request"]=body.ContainsKey("input")?Compile(doc,body["input"]):null};}));
        ctx.AddPost("runs",Guard(async req=>{
            var store=Store(req.AssertUserName());var body=await Body(req).ConfigureAwait(false);var submission=DecisionRecipeValidator.Text(body["submissionId"],"submissionId",100);var doc=Validate(body["recipe"]);var inputs=body["input"];var request=Compile(doc,inputs);var identity=String(body["recipeId"]);Require(body["recipeId"]==null||identity!=null&&identity.Length is >0 and <=240,"recipeId","Choose a saved recipe ID or omit it.");var saved=identity!=null?GetRecipe(store,identity):null;
            var (run,created)=store.Submit(submission,doc,inputs,request,Executor.Owner,identity,saved?.GetLong("revision"));if(created)Executor.Launch(store,run);return ChatResult.Json(run,created?202:200);
        }));
        ctx.AddGet("runs",Guard(req=>{
            var store=Store(req.AssertUserName());Require(int.TryParse(req.QueryString("limit")??"20",out var limit),"limit","Use an integer history limit.");return Task.FromResult<object?>(store.History(req.QueryString("recipeId"),req.QueryString("cursor"),Math.Clamp(limit,1,50)));
        }));
        ctx.AddGet("runs/{id}",Guard(req=>Task.FromResult<object?>(GetRun(Store(req.AssertUserName()),req.GetPathParam("id")))));
        ctx.AddPost("runs/{id}/cancel",Guard(req=>{var store=Store(req.AssertUserName());var id=req.GetPathParam("id");GetRun(store,id);return Task.FromResult<object?>(Executor.Cancel(store,id));}));
        ctx.AddPost("runs/{id}/example-name",Guard(async req=>{var user=req.AssertUserName();return await Authoring.NameAsync(user,GetRun(Store(user),req.GetPathParam("id")),req.Request.RequestAborted).ConfigureAwait(false);}));
        ctx.AddDelete("runs/{id}",Guard(req=>{var store=Store(req.AssertUserName());var id=req.GetPathParam("id");GetRun(store,id);return Task.FromResult<object?>(new JsonObject {["deleted"]=store.DeleteRun(id)});}));
        ctx.AddDelete("history",Guard(req=>Task.FromResult<object?>(new JsonObject {["deleted"]=Store(req.AssertUserName()).DeleteRun()})));
        ctx.AddPost("generate",Guard(async req=>await Authoring.GenerateAsync(req.AssertUserName(),await Body(req).ConfigureAwait(false),false,req.Request.RequestAborted).ConfigureAwait(false)));
        ctx.AddPost("improve",Guard(async req=>await Authoring.GenerateAsync(req.AssertUserName(),await Body(req).ConfigureAwait(false),true,req.Request.RequestAborted).ConfigureAwait(false)));
        ctx.AddGet("recipes/{id}/share",Guard(async req=>{var user=req.AssertUserName();return await Sharing.StatusAsync(user,Store(user),req.GetPathParam("id"),req.Request.RequestAborted).ConfigureAwait(false);}));
        ctx.AddPost("recipes/{id}/share",Guard(async req=>{var user=req.AssertUserName();return await Sharing.PublishAsync(user,Store(user),req.GetPathParam("id"),await Body(req).ConfigureAwait(false),req.Request.RequestAborted).ConfigureAwait(false);}));
        ctx.AddDelete("recipes/{id}/share",Guard(async req=>{var user=req.AssertUserName();return await Sharing.UnpublishAsync(user,Store(user),req.GetPathParam("id"),await Body(req).ConfigureAwait(false),req.Request.RequestAborted).ConfigureAwait(false);}));
    }
    async Task<object?> SaveAsync(ChatRequestContext req)
    {
        var store=Store(req.AssertUserName());var body=await Body(req).ConfigureAwait(false);var doc=Validate(body["document"]);var id=req.PathParams.GetValueOrDefault("id");long? revision=null;long? replacement=null;
        Require(body["filename"]==null||String(body["filename"]) is {Length:>0},"filename","Provide a recipe JSON filename.");
        if(id!=null){GetRecipe(store,id);Require(Integer(body["revision"],out var value)&&value>=1,"revision","Provide the saved recipe revision.");revision=value;}
        if(body["replaceRevision"]!=null){Require(id==null&&Integer(body["replaceRevision"],out var value)&&value>=0,"replaceRevision","Provide the recipe revision to replace when importing.");Integer(body["replaceRevision"],out var replace);replacement=replace;}
        var saved=store.Save(doc,id,revision,replacement,String(body["filename"]));return ChatResult.Json(saved,id!=null||replacement!=null?200:201);
    }
}
