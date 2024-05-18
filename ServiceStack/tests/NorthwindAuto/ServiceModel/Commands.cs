using ServiceStack;

namespace MyApp.ServiceModel;

public class CommandOperation : IPost, IReturn<EmptyResponse>
{
    public string? NewTodo { get; set; }
    public string? ThrowException { get; set; }
    public string? ThrowArgumentException { get; set; }
    public string? ThrowNotSupportedException { get; set; }
}
