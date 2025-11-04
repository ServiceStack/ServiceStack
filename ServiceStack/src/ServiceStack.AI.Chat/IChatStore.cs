using System.Data;
using ServiceStack.Web;

namespace ServiceStack.AI;

public interface IChatStore : IRequiresSchema
{
    IDbConnection OpenDb();
    IDbConnection OpenMonthDb(DateTime? month=null);
    List<DateTime> GetAvailableMonths(IDbConnection db);
    Task ChatCompletedAsync(ChatCompletion request, ChatResponse response, IRequest req);
    Task ChatFailedAsync(ChatCompletion request, Exception ex, IRequest req);
}