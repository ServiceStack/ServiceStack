using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Examples
{
    [TestFixture, Ignore("Integration")]
    public class ServiceStack_Redis_UseCase
    {
        public class Todo
        {
            public long Id { get; set; }
            public string Content { get; set; }
            public int Order { get; set; }
            public bool Done { get; set; }
        }

        [Test]
        public void Can_Add_Update_and_Delete_Todo_item()
        {
            using (var redisManager = new PooledRedisClientManager())
            using (var redis = redisManager.GetClient())
            {
                var redisTodos = redis.As<Todo>();
                var todo = new Todo
                {
                    Id = redisTodos.GetNextSequence(),
                    Content = "Learn Redis",
                    Order = 1,
                };

                redisTodos.Store(todo);

                Todo savedTodo = redisTodos.GetById(todo.Id);
                savedTodo.Done = true;
                redisTodos.Store(savedTodo);

                "Updated Todo:".Print();
                redisTodos.GetAll().ToList().PrintDump();

                redisTodos.DeleteById(savedTodo.Id);

                "No more Todos:".Print();
                redisTodos.GetAll().ToList().PrintDump();
            }
        }
    }
}