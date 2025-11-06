using System.Data;
using Microsoft.Extensions.Logging;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Use Partitioned DB Table in PostgreSQL
/// </summary>
/// <param name="log"></param>
/// <param name="dbFactory"></param>
public class PostgresChatStore(ILogger<PostgresChatStore> log, IDbConnectionFactory dbFactory) : IChatStore
{
    public string? NamedConnection { get; set; }
    public Action<IDbConnection> Configure { get; set; } = DefaultConfigureDb;
    public static void DefaultConfigureDb(IDbConnection db) => db.WithTag(nameof(DbChatStore));
    public IDbConnection OpenDb() => NamedConnection != null 
        ? dbFactory.Open(NamedConnection, Configure)
        : dbFactory.Open(Configure);
    
    public IDbConnection OpenMonthDb(DateTime? month=null)
    {
        month ??= DateTime.UtcNow;
        var db = OpenDb();
        return PostgresUtils.OpenMonthDb<ChatCompletionLog>(db, month.Value, Configure);
    }

    public List<DateTime> GetAvailableMonths(IDbConnection db)
    {
        var dialect = db.GetDialectProvider();
        return PostgresUtils.GetTableMonths(dialect, db, typeof(ChatCompletionLog));
    }

    public async Task ChatCompletedAsync(OpenAiProviderBase provider, ChatCompletion request, ChatResponse response, IRequest req)
    {
        log.LogDebug("ChatCompletedAsync:\n{Request}\nAnswer: {Answer}", ClientConfig.ToSystemJson(request), response.GetAnswer());
        var date = DateTime.UtcNow;
        using var db = OpenMonthDb(date);
        var entry = req.ToChatCompletionLog(provider, request, response);
        await db.InsertAsync(entry);
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
        PostgresUtils.CreatePartitionTableIfNotExists<ChatCompletionLog>(db, x => x.CreatedDate);
    }
}
