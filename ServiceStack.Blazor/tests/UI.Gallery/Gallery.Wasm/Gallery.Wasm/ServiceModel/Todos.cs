using ServiceStack;
using ServiceStack.Model;
using System.Collections.Generic;

namespace MyApp.ServiceModel;

[Tag("todos")]
[Route("/todos", "GET")]
public class QueryTodos : QueryData<Todo>
{
    public int? Id { get; set; }
    public List<long>? Ids { get; set; }
    public string? TextContains { get; set; }
}

[Tag("todos")]
[Route("/todos", "POST")]
public class CreateTodo : IPost, IReturn<Todo>
{
    [ValidateNotEmpty]
    public string Text { get; set; }
}

[Tag("todos")]
[Route("/todos/{Id}", "PUT")]
public class UpdateTodo : IPut, IReturn<Todo>
{
    public long Id { get; set; }
    [ValidateNotEmpty]
    public string Text { get; set; }
    public bool IsFinished { get; set; }
}

[Tag("todos")]
[Route("/todos/{Id}", "DELETE")]
public class DeleteTodo : IDelete, IReturnVoid
{
    public long Id { get; set; }
}

[Tag("todos")]
[Route("/todos", "DELETE")]
public class DeleteTodos : IDelete, IReturnVoid
{
    public List<long> Ids { get; set; }
}

public class Todo : IHasId<long>
{
    public long Id { get; set; }
    public string Text { get; set; }
    public bool IsFinished { get; set; }
}
