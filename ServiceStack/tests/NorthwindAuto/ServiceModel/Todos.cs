using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

public class Todo
{
    [AutoIncrement]
    public long Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsFinished { get; set; }
}

[Tag("todos")]
[Route("/todos", "GET")]
public class QueryTodos : QueryDb<Todo>
{
    public int? Id { get; set; }
    public List<long>? Ids { get; set; }
    public string? TextContains { get; set; }
}

[Tag("todos")]
[Route("/todos", "POST")]
public class CreateTodo : ICreateDb<Todo>, IReturn<Todo>
{
    [ValidateNotEmpty]
    public string Text { get; set; } = string.Empty;
}

[Tag("todos")]
[Route("/todos/{Id}", "PUT")]
public class UpdateTodo : IUpdateDb<Todo>, IReturn<Todo>
{
    public long Id { get; set; }
    [ValidateNotEmpty]
    public string Text { get; set; } = string.Empty;
    public bool IsFinished { get; set; }
}

[Tag("todos")]
[Route("/todos", "DELETE")]
[Route("/todos/{Id}", "DELETE")]
public class DeleteTodos : IDeleteDb<Todo>, IReturnVoid
{
    public long Id { get; set; }
    public List<long> Ids { get; set; } = new();
}


[Tag("todos")]
public class DeleteTodo : IDeleteDb<Todo>, IReturnVoid
{
    public long Id { get; set; }
    public List<long> Ids { get; set; } = new();
}
