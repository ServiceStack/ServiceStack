using System.Collections.Concurrent;
using System.Diagnostics;
using ServiceStack;
using ServiceStack.OrmLite;
using ServiceStack.Text;

[assembly: HostingStartup(typeof(MyApp.ConfigureProfiling))]

namespace MyApp;

public class ConfigureProfiling : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        var observer = new ExampleDiagnosticObserver();
        var subscription = DiagnosticListener.AllListeners.Subscribe(observer);
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
        if (diagnosticListener.Name == Diagnostics.Listeners.OrmLite)
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
            kvp.Value is OrmLiteDiagnosticEvent pre)
        {
            refs[pre.OperationId] = pre;
        }
        
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteCommandAfter &&
            kvp.Value is OrmLiteDiagnosticEvent post)
        {
            post.Command.CommandText.Print();
            if (refs.TryRemove(post.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent preEvent)
            {
                Console.WriteLine($@"Took: {(post.Timestamp - preEvent.Timestamp) / (double)Stopwatch.Frequency}s");
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
