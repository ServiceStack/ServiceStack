using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ProtoBuf.Grpc.Client;

namespace ServiceStack.Extensions.Tests;

[DataContract]
public class Todo
{
    [DataMember(Order = 1)]
    public long Id { get; set; }

    [DataMember(Order = 2)]
    public string Title { get; set; }

    [DataMember(Order = 3)]
    public int Order { get; set; }

    [DataMember(Order = 4)]
    public bool Completed { get; set; }
}

[Route("/todos", "GET")]
[DataContract]
public class GetTodos : IReturn<GetTodosResponse> {}
[DataContract]
public class GetTodosResponse
{
    [DataMember(Order = 1)]
    public List<Todo> Results { get; set; }
    [DataMember(Order = 2)]
    public ResponseStatus ResponseStatus { get; set; }
}

[Route("/todos/{Id}", "GET")]
[DataContract]
public class GetTodo : IReturn<GetTodoResponse>
{
    [DataMember(Order = 1)]
    public long Id { get; set; }
}
[DataContract]
public class GetTodoResponse
{
    [DataMember(Order = 1)]
    public Todo Result { get; set; }
    [DataMember(Order = 2)]
    public ResponseStatus ResponseStatus { get; set; }
}

[Route("/todos", "POST")]
[DataContract]
public class CreateTodo : IReturn<CreateTodoResponse>
{
    [DataMember(Order = 1)]
    public string Title { get; set; }

    [DataMember(Order = 2)]
    public int Order { get; set; }
}
[DataContract]
public class CreateTodoResponse
{
    [DataMember(Order = 1)]
    public Todo Result { get; set; }
    [DataMember(Order = 2)]
    public ResponseStatus ResponseStatus { get; set; }
}

[Route("/todos/{Id}", "PUT")]
[DataContract]
public class UpdateTodo : IReturnVoid
{
    [DataMember(Order = 1)]
    public long Id { get; set; }

    [DataMember(Order = 2)]
    public string Title { get; set; }

    [DataMember(Order = 3)]
    public int Order { get; set; }

    [DataMember(Order = 4)]
    public bool Completed { get; set; }
}

[Route("/todos/{Id}", "DELETE")]
[DataContract]
public class DeleteTodo : IReturnVoid
{
    [DataMember(Order = 1)]
    public long Id { get; set; }
}

[Route("/todos", "DELETE")]
[DataContract]
public class DeleteTodos : IReturnVoid
{
    [DataMember(Order = 1)]
    public List<long> Ids { get; set; }
}
    
[Route("/todos/reset", "POST")]
[DataContract]
public class ResetTodos : IReturnVoid {}
    
public class TodoServices : Service
{
    private static long Counter = 0;
    public static List<Todo> Todos { get; } = new List<Todo>();
        
    public IServerEvents ServerEvents { get; set; }
        
    public object Get(GetTodo request) => new GetTodoResponse { Result = Todos.FirstOrDefault(x => x.Id == request.Id) };

    public object Get(GetTodos request) => new GetTodosResponse { Results = Todos };

    public async Task<object> Post(CreateTodo request)
    {
        var todo = request.ConvertTo<Todo>();
        todo.Id = Interlocked.Increment(ref Counter);
        Todos.Add(todo);
        await ServerEvents.NotifyChannelAsync("todos", "todos.create", todo);
        return new CreateTodoResponse { Result = todo };
    }

    public Task Put(UpdateTodo request)
    {
        var todo = Todos.FirstOrDefault(x => x.Id == request.Id)
                   ?? throw HttpError.NotFound($"Todo with Id '{request.Id}' does not exit");
        todo.PopulateWith(request);
        return ServerEvents.NotifyChannelAsync("todos", "todos.update", todo);
    }

    public Task Delete(DeleteTodo request)
    {
        Todos.RemoveAll(x => x.Id == request.Id);
        return ServerEvents.NotifyChannelAsync("todos", "todos.delete", request.Id);
    }

    public Task Delete(DeleteTodos request)
    {
        if (request.Ids.IsEmpty())
            return Task.CompletedTask;
            
        Todos.RemoveAll(x => request.Ids.Contains(x.Id));
        var tasks = request.Ids.Map(x => ServerEvents.NotifyChannelAsync("todos", "todos.delete", x));
        return Task.WhenAll(tasks);
    }

    public void Post(ResetTodos request)
    {
        Counter = 0;
        Todos.Clear();
    }
}

public class GrpcTodoTests
{
    private readonly ServiceStackHost appHost;

    class AppHost : AppSelfHostBase
    {
        public AppHost() : base(nameof(GrpcTests), typeof(TodoServices).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new GrpcFeature(App));
            Plugins.Add(new ServerEventsFeature());
                
        }

        public override void ConfigureKestrel(KestrelServerOptions options)
        {
            options.ListenLocalhost(TestsConfig.Port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        }

        public override void Configure(IServiceCollection services)
        {
            services.AddServiceStackGrpc();
        }

        public override void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
        }
    }

    public GrpcTodoTests()
    {
        appHost = new AppHost()
            .Init()
            .Start(TestsConfig.BaseUri);

        GrpcClientFactory.AllowUnencryptedHttp2 = true;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    public IServiceClientAsync CreateClient() => new GrpcServiceClient(TestsConfig.BaseUri);

    [Test]
    public async Task Can_CreateTodo()
    {
        var client = CreateClient();
        await client.PostAsync(new ResetTodos());

        var response = await client.PostAsync(new CreateTodo {
            Title = "A",
            Order = 1,
        });

        Assert.That(response.Result.Id, Is.GreaterThan(0));
        Assert.That(response.Result.Title, Is.EqualTo("A"));
        Assert.That(response.Result.Order, Is.EqualTo(1));
        Assert.That(response.Result.Completed, Is.False);

        await client.SendAllAsync(new [] {
            new CreateTodo { Title = "B", Order = 2 }, 
            new CreateTodo { Title = "C", Order = 3 }, 
        });

        var allTodos = await client.GetAsync(new GetTodos());
        Assert.That(allTodos.Results.Map(x => x.Title), Is.EqualTo(new[]{"A","B","C"}));
    }

    [Test]
    public async Task Does_CRUD_Example()
    {
        var client = CreateClient();
        await client.PostAsync(new ResetTodos());

        //GET /todos
        var all = await client.GetAsync(new GetTodos());
        Assert.That(all.Results?.Count ?? 0, Is.EqualTo(0));

        //POST /todos
        var todo = (await client.PostAsync(new CreateTodo { Title = "ServiceStack" })).Result;
        Assert.That(todo.Id, Is.EqualTo(1));
        //GET /todos/1
        todo = (await client.GetAsync(new GetTodo { Id = todo.Id })).Result;
        Assert.That(todo.Title, Is.EqualTo("ServiceStack"));

        //GET /todos
        all = await client.GetAsync(new GetTodos());
        Assert.That(all.Results.Count, Is.EqualTo(1));

        //PUT /todos/1
        await client.PutAsync(new UpdateTodo { Id = todo.Id, Title = "gRPC" });
        todo = (await client.GetAsync(new GetTodo { Id = todo.Id })).Result;
        Assert.That(todo.Title, Is.EqualTo("gRPC"));

        //DELETE /todos/1
        await client.DeleteAsync(new DeleteTodo { Id = todo.Id });
        //GET /todos
        all = await client.GetAsync(new GetTodos());
        Assert.That(all.Results?.Count ?? 0, Is.EqualTo(0));
    }
}