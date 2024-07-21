using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.UseCase;

public class Todo
{
    [AutoIncrement]
    public long Id { get; set; }
    public string Content { get; set; }
    public int Order { get; set; }
    public bool Done { get; set; }
}

[TestFixtureOrmLite]
public class ServiceStack_OrmLite_UseCase(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_Add_Update_and_Delete_Todo_item()
    {
        using (IDbConnection db = OpenDbConnection())
        {
            db.DropAndCreateTable<Todo>();
            var todo = new Todo
            {
                Content = "Learn OrmLite",
                Order = 1,
            };

            db.Save(todo);

            var savedTodo = db.SingleById<Todo>(todo.Id);
            savedTodo.Content = "Updated";
            db.Save(savedTodo);

            "Updated Todo:".Print();
            db.Select<Todo>(q => q.Content == "Updated").PrintDump();
                
            db.DeleteById<Todo>(savedTodo.Id);

            "No more Todos:".Print();
            db.Select<Todo>().PrintDump();
        }
    }
}