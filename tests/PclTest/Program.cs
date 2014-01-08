using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using PclTest.ServiceModel;
using ServiceStack;
using ServiceStack.Text;

namespace PclTest
{
    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost()
            : base("Pcl Test", typeof(WebServices).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new CorsFeature());

            Routes.AddFromAssembly(typeof(WebServices).Assembly);

            container.Register(new TodoRepository());
        }
    }

    public class WebServices : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse { Result = "Hello, " + request.Name };
        }
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
        readonly List<Todo> todos = new List<Todo>();

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


    class Program
    {
        static void Main(string[] args)
        {
            new AppHost()
                .Init()
                .Start("http://*:81/");

            "http://localhost:81/".Print();
            Console.ReadLine();
        }
    }
}
