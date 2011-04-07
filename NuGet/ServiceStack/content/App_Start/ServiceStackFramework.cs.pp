using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.Endpoints;

[assembly: WebActivator.PreApplicationStartMethod(typeof($rootnamespace$.App_Start.AppHost), "Start")]


/**
 * Entire ServiceStack Starter Template configured with a 'Hello' Web Service and a 'Todo' Rest Service.
 *
 * Auto-Generated Metadata API page at: /metadata
 * See other complete web service examples at: https://github.com/ServiceStack/ServiceStack.Examples
 */

namespace $rootnamespace$.App_Start
{
	public class AppHost
		: AppHostBase
	{		
		public AppHost() //Tell ServiceStack the name and where to find your web services
			: base("StarterTemplate ASP.NET Host", typeof(HelloService).Assembly) { }

		public override void Configure(Funq.Container container)
		{
			//Register all your dependencies
			container.Register(new TodoRepository());
			
			//Configure User Defined REST Paths
			Routes
			  .Add<Hello>("/hello")
			  .Add<Hello>("/hello/{Name*}")
			  .Add<Todo>("/todos")
			  .Add<Todo>("/todos/{Id}");

			//Uncomment to change the default ServiceStack configuration
			//var disableFeatures = Feature.Jsv | Feature.Soap;
			//SetConfig(new EndpointHostConfig
			//{
			//    EnableFeatures = Feature.All.Remove(disableFeatures),
			//    DebugMode = true, //Show StackTraces when developing
			//});
		}

		public static void Start()
		{
			new AppHost().Init();
		}
	}
}

namespace $rootnamespace$
{
	//Request DTO
	public class Hello
	{
		public string Name { get; set; }
	}

	//Response DTO
	public class HelloResponse : IHasResponseStatus
	{
		public string Result { get; set; }
		public ResponseStatus ResponseStatus { get; set; } //Where Exceptions get auto-serialized
	}

	//Can be called via any endpoint or format, see: http://servicestack.net/ServiceStack.Hello/
	public class HelloService : ServiceBase<Hello>
	{
		protected override object Run(Hello request)
		{
			return new HelloResponse { Result = "Hello, " + request.Name };
		}
	}


	//REST DTO
	public class Todo 
	{
		public long Id { get; set; }
		public string Content { get; set; }
		public int Order { get; set; }
		public bool Done { get; set; }
	}

	//Todo REST Service implementation
	public class TodoService : RestServiceBase<Todo>
	{
		public TodoRepository Repository { get; set; }  //Injected by IOC

		public override object OnGet(Todo request)
		{
			if (request.Id == default(long))
				return Repository.GetAll();

			return Repository.GetById(request.Id);
		}

		//Called for new and update
		public override object OnPost(Todo todo)
		{
			return Repository.Store(todo);
		}

		public override object OnDelete(Todo request)
		{
			Repository.DeleteById(request.Id);
			return null;
		}
	}


	/// <summary>
	/// In-memory repository, so we can run the TODO app without any external dependencies
	/// Registered in Funq as a singleton, auto injected on every request
	/// </summary>
	public class TodoRepository
	{
		private readonly List<Todo> todos = new List<Todo>();

		public List<Todo> GetAll()
		{
			return todos;
		}

		public Todo GetById(long id)
		{
			return todos.FirstOrDefault(x => x.Id == id);
		}

		public Todo Store(Todo todo)
		{
			if (todo.Id == default(long))
			{
				todo.Id = todos.Count == 0 ? 1 : todos.Max(x => x.Id) + 1;
			}
			else
			{
				for (var i = 0; i < todos.Count; i++)
				{
					if (todos[i].Id != todo.Id) continue;

					todos[i] = todo;
					return todo;
				}
			}

			todos.Add(todo);
			return todo;
		}

		public void DeleteById(long id)
		{
			todos.RemoveAll(x => x.Id == id);
		}
	}
}
