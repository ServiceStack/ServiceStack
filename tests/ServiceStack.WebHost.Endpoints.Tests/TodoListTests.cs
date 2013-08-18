using System.Collections.Generic;
using Funq;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	public class Todo
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Content { get; set; }
		public bool Done { get; set; }

		public bool Equals(Todo other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.Id == Id && Equals(other.Name, Name) && Equals(other.Content, Content) && other.Done.Equals(Done);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (Todo)) return false;
			return Equals((Todo) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = Id;
				result = (result*397) ^ (Name != null ? Name.GetHashCode() : 0);
				result = (result*397) ^ (Content != null ? Content.GetHashCode() : 0);
				result = (result*397) ^ Done.GetHashCode();
				return result;
			}
		}
	}

    [Route("/todolist")]
	public class TodoList : List<Todo>
	{
		public TodoList() {}
		public TodoList(IEnumerable<Todo> collection) : base(collection) {}
	}

	public class TodoListResponse
	{
		public TodoList Results { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

    [DefaultRequest(typeof(TodoList))]
	public class TodoListService : ServiceInterface.Service
	{
		public object Get(TodoList request)
		{
			return new TodoListResponse { Results = request };
		}

		public object Post(TodoList request)
		{
			return new TodoListResponse { Results = request };
		}

		public object Put(TodoList request)
		{
			return new TodoListResponse { Results = request };
		}

		public object Delete(TodoList request)
		{
			return new TodoListResponse { Results = request };
		}
	}

	[TestFixture]
	public class TodoListTests
	{
		private const string ListeningOn = "http://localhost:8082/";

		public class TodoListAppHostHttpListener
			: AppHostHttpListenerBase
		{
			public TodoListAppHostHttpListener()
				: base("TodoList Tests", typeof(TodoList).Assembly) { }

			public override void Configure(Container container) {}
		}

		TodoListAppHostHttpListener appHost;

		readonly Todo[] Todos = new[] {
			new Todo { Id = 1, Name = "Todo1", Content = "Content1", Done = false},
			new Todo { Id = 2, Name = "Todo2", Content = "Content2", Done = true},
			new Todo { Id = 3, Name = "Todo3", Content = "Content3", Done = false},
		};

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			appHost = new TodoListAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			appHost.Dispose();
		}

		[Test]
		public void Can_Send_TodoList()
		{
			var serviceClient = new JsonServiceClient(ListeningOn);
			var response = serviceClient.Send<TodoListResponse>(new TodoList(Todos));
			Assert.That(response.Results, Is.EquivalentTo(Todos));
		}

		[Test]
		public void Can_Post_TodoList()
		{
			var serviceClient = new JsonServiceClient(ListeningOn);
			var response = serviceClient.Post<TodoListResponse>("/todolist", new TodoList(Todos));
			Assert.That(response.Results, Is.EquivalentTo(Todos));
		}

		[Test]
		public void Can_Get_TodoList()
		{
			var serviceClient = new JsonServiceClient(ListeningOn);
			var response = serviceClient.Get<TodoListResponse>("/todolist");
			Assert.That(response.Results.Count, Is.EqualTo(0));
		}
	}
}