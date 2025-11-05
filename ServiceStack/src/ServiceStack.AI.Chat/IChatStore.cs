using System.Data;
using ServiceStack.Web;

namespace ServiceStack.AI;

public interface IChatStore : IRequiresSchema
{
    IDbConnection OpenDb();
    IDbConnection OpenMonthDb(DateTime? month=null);
    List<DateTime> GetAvailableMonths(IDbConnection db);
    Task ChatCompletedAsync(OpenAiProviderBase provider, ChatCompletion request, ChatResponse response, IRequest req);
    Task ChatFailedAsync(OpenAiProviderBase provider, ChatCompletion request, Exception ex, IRequest req);
}