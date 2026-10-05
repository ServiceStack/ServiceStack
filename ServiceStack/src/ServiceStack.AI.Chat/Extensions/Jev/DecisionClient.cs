using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public sealed class JevDecisionException(string message,int status=502,JsonNode? raw=null):Exception(message)
{
    public int Status {get;}=status;
    public JsonNode? Raw {get;}=raw;
}
/// <summary>Jev's view of the shared Decisions API (<see cref="ChatDecisions"/>); never retries paid work.</summary>
public sealed class DecisionClient(ChatFeature feature,Func<HttpMessageHandler>? handlerFactory=null)
{
    public const string Endpoint=ChatDecisions.Endpoint;
    public const int MaxResponseBytes=ChatDecisions.MaxResponseBytes;
    readonly ChatDecisions api=new(feature,handlerFactory);
    public TimeSpan RequestTimeout {get=>api.RequestTimeout;set=>api.RequestTimeout=value;}
    public (ChatProvider? Provider,string Message) Status()=>api.Status();
    /// <summary>Jev compiles and validates recipes itself; the provider's status and raw response stay on the run.</summary>
    public async Task<(JsonNode Raw,JsonObject Answers)> DecideAsync(JsonObject payload,CancellationToken token)
    {
        try{return await api.SendAsync(payload,token).ConfigureAwait(false);}
        catch(ChatDecisionException e){throw new JevDecisionException(e.Message,e.ProviderStatus,e.Raw);}
    }
}

public sealed class DecisionExecutor
{
    readonly Func<JsonObject,CancellationToken,Task<(JsonNode Raw,JsonObject Answers)>> decide;
    readonly Dictionary<(string Root,string Id),(Task Task,CancellationTokenSource Cancellation)> tasks=[];
    readonly CancellationTokenSource shutdown=new();
    public string Owner {get;}=Guid.NewGuid().ToString();
    public TimeSpan ExecutionTimeout {get;set;}=TimeSpan.FromSeconds(55);
    bool stopped;
    Task? closing;
    public DecisionExecutor(DecisionClient client):this(client.DecideAsync){}
    public DecisionExecutor(Func<JsonObject,CancellationToken,Task<(JsonNode Raw,JsonObject Answers)>> decide)=>this.decide=decide;
    public void Launch(JevStore store,JsonObject run)
    {
        var key=(store.Root,run.GetString("id")!);lock(tasks){
            if(stopped){store.Finish(key.Item2,"interrupted",error:"The server stopped this request before it completed.",owner:Owner);return;}
            if(tasks.ContainsKey(key))return;
            var cancellation=CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);var snapshot=run.Clone();
            var task=Task.Run(async()=>{try{await ExecuteAsync(store,snapshot,cancellation.Token).ConfigureAwait(false);}finally{lock(tasks)tasks.Remove(key);cancellation.Dispose();}});
            tasks[key]=(task,cancellation);
        }
    }
    async Task ExecuteAsync(JevStore store,JsonObject run,CancellationToken token)
    {
        var id=run.GetString("id")!;if(!store.Start(id,Owner))return;
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(ExecutionTimeout);
        try{var (raw,answers)=await decide((JsonObject)run["request"]!,timeout.Token).WaitAsync(timeout.Token).ConfigureAwait(false);token.ThrowIfCancellationRequested();store.Finish(id,"succeeded",raw,answers,owner:Owner);}
        catch(OperationCanceledException){store.Finish(id,token.IsCancellationRequested?"interrupted":"failed",error:token.IsCancellationRequested?"The server stopped this request before it completed.":"The decision timed out. It has not been retried.",owner:Owner);}
        catch(JevDecisionException e){store.Finish(id,"failed",e.Raw,error:e.Message,owner:Owner);}
        catch(Exception){store.Finish(id,"failed",error:"The decision could not complete. Try again or check server configuration.",owner:Owner);}
    }
    public JsonObject? Cancel(JevStore store,string id)
    {
        store.Finish(id,"cancelled",error:"Stopped by you. OpenRouter may already have processed this request.");lock(tasks)if(tasks.TryGetValue((store.Root,id),out var task))task.Cancellation.Cancel();return store.Run(id);
    }
    public Task CloseAsync()
    {
        lock(tasks){if(closing!=null)return closing;stopped=true;shutdown.Cancel();var running=tasks.Values.Select(x=>x.Task).ToArray();return closing=JoinAsync(running);}
    }
    async Task JoinAsync(Task[] running){try{await Task.WhenAll(running).ConfigureAwait(false);}finally{shutdown.Dispose();}}
}
