using ServiceStack.Web;

namespace ServiceStack.AI;

public interface IChatStore : IRequiresSchema
{
    Task ChatCompletedAsync(ChatCompletion request, ChatResponse response, IRequest req);
    Task ChatFailedAsync(ChatCompletion request, Exception ex, IRequest req);
}