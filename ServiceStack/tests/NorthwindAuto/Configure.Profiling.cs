using System.Collections.Concurrent;
using System.Diagnostics;
using MyApp.ServiceInterface;
using ServiceStack;
using ServiceStack.Text;

[assembly: HostingStartup(typeof(MyApp.ConfigureProfiling))]

namespace MyApp;

public class ConfigureProfiling : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        var observer = new ExampleDiagnosticObserver();
        var subscription = DiagnosticListener.AllListeners.Subscribe(observer);
        
        builder.ConfigureAppHost(afterAppHostInit: host =>
        {
            host.ServiceController.Execute(new ProfileRedis());
        });
    }
}

public sealed class ExampleDiagnosticObserver : 
    IObserver<DiagnosticListener>, 
    IObserver<KeyValuePair<string, object>>
{
    private readonly List<IDisposable> subscriptions = new();

    void IObserver<DiagnosticListener>.OnNext(DiagnosticListener diagnosticListener)
    {
        Console.WriteLine($@"diagnosticListener: {diagnosticListener.Name}");
        if (diagnosticListener.Name is Diagnostics.Listeners.ServiceStack
            or Diagnostics.Listeners.OrmLite
            or Diagnostics.Listeners.Redis)
        {
            var subscription = diagnosticListener.Subscribe(this);
            subscriptions.Add(subscription);
        }
    }

    private ConcurrentDictionary<Guid, object> refs = new();

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object> kvp)
    {
        Console.WriteLine(kvp.Key);
        Console.WriteLine(kvp.Value);

        if (kvp.Key == Diagnostics.Events.OrmLite.WriteCommandBefore &&
            kvp.Value is OrmLiteDiagnosticEvent dbBefore)
        {
            refs[dbBefore.OperationId] = dbBefore;
        }
        
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteCommandAfter &&
            kvp.Value is OrmLiteDiagnosticEvent dbAfter)
        {
            $"{dbAfter.Operation} {dbAfter.Command.CommandText}".Print();
            if (refs.TryRemove(dbAfter.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent preEvent)
            {
                Console.WriteLine($@"Took: {(dbAfter.Timestamp - preEvent.Timestamp) / (double)Stopwatch.Frequency}s");
            }
        }

        if (kvp.Key == Diagnostics.Events.Redis.WriteCommandBefore &&
            kvp.Value is RedisDiagnosticEvent redisBefore)
        {
            refs[redisBefore.OperationId] = redisBefore;
        }
        
        if (kvp.Key == Diagnostics.Events.Redis.WriteCommandAfter &&
            kvp.Value is RedisDiagnosticEvent redisAfter)
        {
            var args = redisAfter.Command.TakeWhile(bytes => bytes.Length <= 100)
                .Map(bytes => bytes.FromUtf8Bytes());
            var argLengths = redisAfter.Command.Map(x => x.LongLength);
            
            $"{redisAfter.Operation} {string.Join(',', args)} :: {string.Join(',', argLengths)}".Print();
            if (refs.TryRemove(redisAfter.OperationId, out var orig) && orig is RedisDiagnosticEvent preEvent)
            {
                Console.WriteLine($@"Took: {(redisAfter.Timestamp - preEvent.Timestamp) / (double)Stopwatch.Frequency}s");
            }
        }
        
        Console.WriteLine();
    }

    void IObserver<DiagnosticListener>.OnError(Exception error)
    {
    }

    void IObserver<DiagnosticListener>.OnCompleted()
    {
        subscriptions.ForEach(x => x.Dispose());
        subscriptions.Clear();
    }
}
