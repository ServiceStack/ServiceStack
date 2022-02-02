using NUnit.Framework;

namespace ServiceStack.Redis.Tests.Examples
{
    [TestFixture, Ignore("Integration"), Category("Integration")]
    public class TodoApp
    {
        [SetUp]
        public void SetUp()
        {
            new RedisClient().FlushAll();
        }

        public class Todo
        {
            public long Id { get; set; }
            public string Content { get; set; }
            public int Order { get; set; }
            public bool Done { get; set; }
        }

        [Test]
        public void Crud_TODO_App()
        {
            //Thread-safe client factory
            var redisManager = new PooledRedisClientManager(TestConfig.SingleHostConnectionString);

            redisManager.ExecAs<Todo>(redisTodos =>
            {
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

                redisTodos.DeleteById(savedTodo.Id);

                var allTodos = redisTodos.GetAll();

                Assert.That(allTodos.Count, Is.EqualTo(0));
            });
        }
    }
}