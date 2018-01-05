using System.Collections.Generic;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack;

namespace NewApi.Todos
{
    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("TODOs Tests", typeof(Todo).Assembly) {}

        public override void Configure(Container container)
        {
            container.Register(new TodoRepository());
        }
    }
    
    //REST Resource DTO
    [Route("/todos")]
    [Route("/todos/{Ids}")]
    public class Todos : IReturn<List<Todo>>
    {
        public long[] Ids { get; set; }

        public Todos() {}
        public Todos(params long[] ids)
        {
            this.Ids = ids;
        }
    }

    [Route("/todos", "POST")]
    [Route("/todos/{Id}", "PUT")]
    public class Todo : IReturn<Todo>
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public int Order { get; set; }
        public bool Done { get; set; }
    }

    public class TodosService : Service
    {
        public TodoRepository Repository { get; set; }  //Injected by IOC

        public object Get(Todos request)
        {
            return request.Ids.IsEmpty()
                ? Repository.GetAll()
                : Repository.GetByIds(request.Ids);
        }

        public object Post(Todo todo)
        {
            return Repository.Store(todo);
        }

        public object Put(Todo todo)
        {
            return Repository.Store(todo);
        }

        public void Delete(Todos request)
        {
            Repository.DeleteByIds(request.Ids);
        }
    }
    
    public class TodoRepository
    {
        List<Todo> todos = new List<Todo>();
        
        public List<Todo> GetByIds(long[] ids)
        {
            return todos.Where(x => ids.Contains(x.Id)).ToList();
        }

        public List<Todo> GetAll()
        {
            return todos;
        }

        public Todo Store(Todo todo)
        {
            var existing = todos.FirstOrDefault(x => x.Id == todo.Id);
            if (existing == null)
            {
                var newId = todos.Count > 0 ? todos.Max(x => x.Id) + 1 : 1;
                todo.Id = newId;
            }
            todos.Add(todo);
            return todo;
        }

        public void DeleteByIds(params long[] ids)
        {
            todos.RemoveAll(x => ids.Contains(x.Id));
        }
    }

    [TestFixture]
    public class NewApiTodosTests
    {
        const string BaseUri = "http://localhost:1337/";

        AppHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(BaseUri);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Run()
        {
            var restClient = new JsonServiceClient(BaseUri);
            List<Todo> all = restClient.Get(new Todos());
            Assert.That(all.Count, Is.EqualTo(0));

            var todo = restClient.Post(new Todo { Content = "New TODO", Order = 1 });
            Assert.That(todo.Id, Is.EqualTo(1));
            all = restClient.Get(new Todos());
            Assert.That(all.Count, Is.EqualTo(1));

            todo.Content = "Updated TODO";
            todo = restClient.Put(todo);
            Assert.That(todo.Content, Is.EqualTo("Updated TODO"));

            restClient.Delete(new Todos(todo.Id));
            all = restClient.Get(new Todos());
            Assert.That(all.Count, Is.EqualTo(0));
        }

    }
}
