using System;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ProtoBuf.Grpc.Client;

namespace ServiceStack.Extensions.Tests.Protoc
{
    public class ProtocTodoTests
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

        public ProtocTodoTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(TestsConfig.BaseUri);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        private static GrpcServices.GrpcServicesClient GetClient(Action<GrpcClientConfig> init = null) =>
            ProtocTests.GetClient(init);

        [Test]
        public async Task Can_CreateTodo()
        {
            var client = GetClient();
            await client.PostResetTodosAsync(new ResetTodos());

            var response = await client.PostCreateTodoAsync(new CreateTodo {
                Title = "A",
                Order = 1,
            });

            Assert.That(response.Result.Id, Is.GreaterThan(0));
            Assert.That(response.Result.Title, Is.EqualTo("A"));
            Assert.That(response.Result.Order, Is.EqualTo(1));
            Assert.That(response.Result.Completed, Is.False);
        }

        [Test]
        public async Task Does_CRUD_Example()
        {
            var client = GetClient();
            await client.PostResetTodosAsync(new ResetTodos());

            //GET /todos
            var all = await client.CallGetTodosAsync(new GetTodos());
            Assert.That(all.Results?.Count ?? 0, Is.EqualTo(0));

            //POST /todos
            var todo = (await client.PostCreateTodoAsync(new CreateTodo { Title = "ServiceStack" })).Result;
            Assert.That(todo.Id, Is.EqualTo(1));
            //GET /todos/1
            todo = (await client.CallGetTodoAsync(new GetTodo { Id = todo.Id })).Result;
            Assert.That(todo.Title, Is.EqualTo("ServiceStack"));

            //GET /todos
            all = await client.CallGetTodosAsync(new GetTodos());
            Assert.That(all.Results.Count, Is.EqualTo(1));

            //PUT /todos/1
            await client.PutUpdateTodoAsync(new UpdateTodo { Id = todo.Id, Title = "gRPC" });
            todo = (await client.CallGetTodoAsync(new GetTodo { Id = todo.Id })).Result;
            Assert.That(todo.Title, Is.EqualTo("gRPC"));

            //DELETE /todos/1
            await client.CallDeleteTodoAsync(new DeleteTodo { Id = todo.Id });
            //GET /todos
            all = await client.CallGetTodosAsync(new GetTodos());
            Assert.That(all.Results?.Count ?? 0, Is.EqualTo(0));
        }
    }
}