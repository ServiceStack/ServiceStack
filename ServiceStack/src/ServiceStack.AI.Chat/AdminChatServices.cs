using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

[ExcludeMetadata, Tag(TagNames.Admin), ExplicitAutoQuery]
public class AdminQueryChatCompletionLogs : QueryDb<ChatCompletionLog>
{
    public DateTime? Month { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminMonthlyChatCompletionAnalytics : IGet, IReturn<AdminMonthlyChatCompletionAnalyticsResponse>
{
    public DateTime? Month { get; set; }
}
public class AdminMonthlyChatCompletionAnalyticsResponse
{
    public string Month { get; set; }
    public List<string> AvailableMonths { get; set; }
    public List<ChatCompletionStat> ModelStats { get; set; }
    public List<ChatCompletionStat> ProviderStats { get; set; }
    public List<ChatCompletionStat> DailyStats { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminDailyChatCompletionAnalytics : IGet, IReturn<AdminDailyChatCompletionAnalyticsResponse>
{
    public DateTime? Day { get; set; }
}
public class AdminDailyChatCompletionAnalyticsResponse
{
    public List<ChatCompletionStat> ModelStats { get; set; }
    public List<ChatCompletionStat> ProviderStats { get; set; }
}

public class ChatCompletionStat
{
    public string Name { get; set; }
    public int Requests { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal Cost { get; set; }
}

public class AdminChatServices(IAutoQueryDb autoQuery) 
    : Service
{
    private (ChatFeature,IChatStore) AssertRequiredRole()
    {
        var feature = AssertPlugin<ChatFeature>();
        RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);
        var chatStore = feature.ChatStore
            ?? throw new Exception("ChatStore is not configured");
        return (feature, chatStore);
    }

    public static List<string> AvailableMonths { get; set; } = [];

    public List<string> GetAvailableMonths(IChatStore chatStore)
    {
        using var db = chatStore.OpenDb();
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        if (AvailableMonths.Count == 0 || !AvailableMonths.Contains(currentMonth))
        {
            var months = chatStore.GetAvailableMonths(db)
                .Map(x => x.ToString("yyyy-MM"));
            if (months.Count == 0)
                months.Add(currentMonth);
            AvailableMonths = months;
        }
        return AvailableMonths;
    }
    
    public async Task<object> Any(AdminQueryChatCompletionLogs request)
    {
        var (feature, chatStore) = AssertRequiredRole();
        var monthDate = request.Month ?? DateTime.UtcNow;
        var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
        using var monthDb = chatStore.OpenMonthDb(monthDate);
        var q = autoQuery.CreateQuery(request, base.Request, monthDb);
        q.Ensure(x => x.CreatedDate >= monthStart && x.CreatedDate < monthStart.AddMonths(1));
        return await autoQuery.ExecuteAsync(request, q, base.Request, monthDb);        
    }

    public async Task<object> Any(AdminMonthlyChatCompletionAnalytics request)
    {
        var (feature, chatStore) = AssertRequiredRole();
        var monthDate = (request.Month ?? DateTime.UtcNow); 
        var month = new DateTime(monthDate.Year, monthDate.Month, 1);
        using var monthDb = chatStore.OpenMonthDb(month);
        
        var modelStats = await monthDb.SelectAsync<ChatCompletionStat>(monthDb.From<ChatCompletionLog>()
            .Where(x => x.CreatedDate >= month && x.CreatedDate < month.AddMonths(1))
            .GroupBy(x => x.Model)
            .Select(x => new {
                Name = x.Model,
                Requests = Sql.Count("*"),
                InputTokens = Sql.Sum(x.PromptTokens ?? 0),
                OutputTokens = Sql.Sum(x.CompletionTokens ?? 0),
                Cost = Sql.Sum(x.Cost),
            }));
        var providerStats = await monthDb.SelectAsync<ChatCompletionStat>(monthDb.From<ChatCompletionLog>()
            .Where(x => x.CreatedDate >= month && x.CreatedDate < month.AddMonths(1))
            .GroupBy(x => x.Provider)
            .Select(x => new {
                Name = x.Provider,
                Requests = Sql.Count("*"),
                InputTokens = Sql.Sum(x.PromptTokens ?? 0),
                OutputTokens = Sql.Sum(x.CompletionTokens ?? 0),
                Cost = Sql.Sum(x.Cost),
            }));

        var q = monthDb.From<ChatCompletionLog>();
        var createdDate = q.Column<ChatCompletionLog>(c => c.CreatedDate);
        var dailyStatsForMonth = await monthDb.SelectAsync<ChatCompletionStat>(q
            .Where(x => x.CreatedDate >= month && x.CreatedDate < month.AddMonths(1))
            .GroupBy(x => q.sql.DateFormat(createdDate, "%d"))
            .Select(x => new {
                Name = Sql.As(q.sql.DateFormat(createdDate, "%d"), "'Name'"),
                Requests = Sql.Count("*"),
                InputTokens = Sql.Sum(x.PromptTokens ?? 0),
                OutputTokens = Sql.Sum(x.CompletionTokens ?? 0),
                Cost = Sql.Sum(x.Cost),
            }));
        
        var availableMonths = GetAvailableMonths(chatStore);
        
        return new AdminMonthlyChatCompletionAnalyticsResponse
        {
            AvailableMonths = availableMonths,
            Month = month.ToString("yyyy-MM"),
            ModelStats = modelStats,
            ProviderStats = providerStats,
            DailyStats = dailyStatsForMonth,
        };
    }

    public async Task<object> Any(AdminDailyChatCompletionAnalytics request)
    {
        var (feature, chatStore) = AssertRequiredRole();
        var dayDate = (request.Day ?? DateTime.UtcNow); 
        var month = new DateTime(dayDate.Year, dayDate.Month, dayDate.Day);
        using var monthDb = chatStore.OpenMonthDb(month);
        
        var modelStats = await monthDb.SelectAsync<ChatCompletionStat>(monthDb.From<ChatCompletionLog>()
            .Where(x => x.CreatedDate >= month && x.CreatedDate < month.AddDays(1))
            .GroupBy(x => x.Model)
            .Select(x => new {
                Name = x.Model,
                Requests = Sql.Count("*"),
                InputTokens = Sql.Sum(x.PromptTokens ?? 0),
                OutputTokens = Sql.Sum(x.CompletionTokens ?? 0),
                Cost = Sql.Sum(x.Cost),
            }));
        var providerStats = await monthDb.SelectAsync<ChatCompletionStat>(monthDb.From<ChatCompletionLog>()
            .Where(x => x.CreatedDate >= month && x.CreatedDate < month.AddDays(1))
            .GroupBy(x => x.Provider)
            .Select(x => new {
                Name = x.Provider,
                Requests = Sql.Count("*"),
                InputTokens = Sql.Sum(x.PromptTokens ?? 0),
                OutputTokens = Sql.Sum(x.CompletionTokens ?? 0),
                Cost = Sql.Sum(x.Cost),
            }));
        
        return new AdminDailyChatCompletionAnalyticsResponse
        {
            ModelStats = modelStats,
            ProviderStats = providerStats,
        };
    }
}
