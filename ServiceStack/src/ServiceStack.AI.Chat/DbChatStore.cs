using System.Data;
using Microsoft.Extensions.Logging;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// 
/// </summary>
/// <param name="log"></param>
/// <param name="dbFactory"></param>
public class DbChatStore(ILogger<DbChatStore> log, IDbConnectionFactory dbFactory) : IChatStore
{
    public string? NamedConnection { get; set; }
    public Action<IDbConnection> Configure { get; set; } = DefaultConfigureDb;
    public static void DefaultConfigureDb(IDbConnection db) => db.WithTag(nameof(DbChatStore));
    public IDbConnection OpenDb() => NamedConnection != null 
        ? dbFactory.Open(NamedConnection)
        : dbFactory.Open(Configure);
    
    public IDbConnection OpenMonthDb(DateTime? month=null) => OpenDb();
    public List<DateTime> GetAvailableMonths(IDbConnection db)
    {
        var dialect = db.GetDialectProvider();
        var q = db.From<ChatCompletionLog>();
        var dateTimeColumn = q.Column<ChatCompletionLog>(c => c.CreatedDate);
        var months = db.SqlColumn<string>(q
            .Select(x => new {
                Month = dialect.SqlDateFormat(dateTimeColumn, "%Y-%m"),
            }));

        var ret = months
            .Where(x => x.Contains('_'))
            .Select(x => 
                DateTime.TryParse(x.RightPart('_').LeftPart('.') + "-01", out var date) ? date : (DateTime?)null)
            .Where(x => x != null)
            .Select(x => x!.Value)
            .OrderDescending()
            .ToList();
        
        return ret;
    }

    public async Task ChatCompletedAsync(OpenAiProviderBase provider, ChatCompletion request, ChatResponse response, IRequest req)
    {
        log.LogDebug("ChatCompletedAsync:\n{Request}\nAnswer: {Answer}", ClientConfig.ToSystemJson(request), response.GetAnswer());
        var date = DateTime.UtcNow;
        using var db = OpenMonthDb(date);
        await db.InsertAsync(req.ToChatCompletionLog(provider, request, response));
    }

    public async Task ChatFailedAsync(OpenAiProviderBase provider, ChatCompletion request, Exception ex, IRequest req)
    {
        log.LogWarning(ex, "ChatFailedAsync:\n{Request}", ClientConfig.ToSystemJson(request));
        var date = DateTime.UtcNow;
        using var db = OpenMonthDb(date);
        await db.InsertAsync(req.ToChatCompletionLog(provider, request, ex));
    }

    public void InitSchema()
    {
        using var db = OpenDb();
        db.CreateTableIfNotExists<ChatCompletionLog>();
    }
}
