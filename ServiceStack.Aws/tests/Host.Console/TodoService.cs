using ServiceStack;
using ServiceStack.Aws.DynamoDb;

namespace Todos
{
    [Route("/todos")]
    [Route("/todos/{Id}")]
    public class Todo
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public int Order { get; set; }
        public bool Done { get; set; }
    }

    public class TodoService : Service
    {
        public IPocoDynamo Dynamo { get; set; }

        public object Get(Todo todo)
        {
            if (todo.Id != default(long))
                return Dynamo.GetItem<Todo>(todo.Id);

            return Dynamo.GetAll<Todo>();
        }

        public Todo Post(Todo todo)
        {
            if (todo.Id == default(long))
                todo.Id = Dynamo.Sequences.Increment<Todo>();

            Dynamo.PutItem(todo);

            return todo;
        }

        public Todo Put(Todo todo)
        {
            return Post(todo);
        }

        public void Delete(Todo todo)
        {
            Dynamo.DeleteItem<Todo>(todo.Id);
        }
    }
}