using ServiceStack;
using ServiceStack.Model;

namespace MyApp.ServiceModel;

[Tag("todos")]
[Route("/todos", "GET")]
[ValidateApiKey]
public class QueryTodos : QueryData<Todo>
{
    public int? Id { get; set; }
    public List<long>? Ids { get; set; }
    public string? TextContains { get; set; }
}

[Tag("todos")]
[Route("/todos", "POST")]
[ValidateApiKey]
public class CreateTodo : IPost, IReturn<Todo>
{
    [ValidateNotEmpty]
    public string Text { get; set; }
}

[Tag("todos")]
[Route("/todos/{Id}", "PUT")]
[ValidateApiKey]
public class UpdateTodo : IPut, IReturn<Todo>
{
    public long Id { get; set; }
    [ValidateNotEmpty]
    public string Text { get; set; }
    public bool IsFinished { get; set; }
}

[Tag("todos")]
[Route("/todos", "DELETE")]
[ValidateApiKey]
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
