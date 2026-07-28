using System.Data;
using System.Globalization;
using System.Text.Json.Nodes;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

/// <summary>Chat requests logged by the App extension, filtered to a month or a single day</summary>
[ExcludeMetadata, Tag(TagNames.Admin), ExplicitAutoQuery]
public class AdminQueryChatRequests : QueryDb<ChatRequest>
{
    /// <summary>Only include requests created in this month, formatted yyyy-MM</summary>
    public string? Month { get; set; }
    /// <summary>Only include requests created on this day, formatted yyyy-MM-dd</summary>
    public string? Day { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminMonthlyChatAnalytics : IGet, IReturn<AdminMonthlyChatAnalyticsResponse>
{
    /// <summary>Month to report on, formatted yyyy-MM (defaults to the current month)</summary>
    public string? Month { get; set; }
}
public class AdminMonthlyChatAnalyticsResponse
{
    public string Month { get; set; } = null!;
    public List<string> AvailableMonths { get; set; } = [];
    public List<ChatRequestStat> ModelStats { get; set; } = [];
    public List<ChatRequestStat> ProviderStats { get; set; } = [];
    /// <summary>One stat per day of the month, Name is the zero-padded day, e.g. "01"</summary>
    public List<ChatRequestStat> DailyStats { get; set; } = [];
}

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminDailyChatAnalytics : IGet, IReturn<AdminDailyChatAnalyticsResponse>
{
    /// <summary>Day to report on, formatted yyyy-MM-dd (defaults to today)</summary>
    public string? Day { get; set; }
}
public class AdminDailyChatAnalyticsResponse
{
    public List<ChatRequestStat> ModelStats { get; set; } = [];
    public List<ChatRequestStat> ProviderStats { get; set; } = [];
}

/// <summary>The conversation a logged request belongs to, for the Activity detail panel</summary>
[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminGetChatThread : IGet, IReturn<AdminGetChatThreadResponse>
{
    public long Id { get; set; }
}
public class AdminGetChatThreadResponse
{
    public long Id { get; set; }
    public string? User { get; set; }
    public string? Title { get; set; }
    public string? Model { get; set; }
    public string? Provider { get; set; }
    public string? SystemPrompt { get; set; }
    public List<ChatThreadMessage> Messages { get; set; } = [];
}
public class ChatThreadMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public class ChatRequestStat
{
    public string Name { get; set; } = null!;
    public int Requests { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public double Cost { get; set; }
    /// <summary>Total request duration in seconds (ChatRequest.Duration)</summary>
    public long Duration { get; set; }
}

/// <summary>
/// Analytics + activity for the /admin-ui/chat dashboard, rolled up from the ChatRequest table
/// the App extension writes each completion to (the same rows /chat's analytics ext reports on,
/// only unfiltered by user).
/// </summary>
public class AdminChatServices(IAutoQueryDb autoQuery) : Service
{
    /// <summary>Stats are grouped under this name when a request has no model/provider recorded</summary>
    public const string Unattributed = "(none)";

    ChatDb AssertChatDb()
    {
        var feature = AssertPlugin<ChatFeature>();
        RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);
        return feature.ChatDb
            ?? throw new Exception("ChatDb is not configured");
    }

    public async Task<object> Any(AdminQueryChatRequests request)
    {
        var chatDb = AssertChatDb();
        using var db = chatDb.OpenDb();
        var q = autoQuery.CreateQuery(request, base.Request, db);
        var (from, to) = ParsePeriod(request.Month, request.Day);
        q.Ensure(x => x.CreatedAt >= from && x.CreatedAt < to);
        return await autoQuery.ExecuteAsync(request, q, base.Request, db);
    }

    public object Any(AdminMonthlyChatAnalytics request)
    {
        var chatDb = AssertChatDb();
        var from = ParseMonth(request.Month) ?? StartOfMonth(DateTime.Now);
        var to = from.AddMonths(1);

        using var db = chatDb.OpenDb();
        var rows = SelectPeriod(db, from, to);

        return new AdminMonthlyChatAnalyticsResponse
        {
            Month = from.ToString("yyyy-MM"),
            AvailableMonths = GetAvailableMonths(db),
            ModelStats = StatsBy(rows, x => x.Model),
            ProviderStats = StatsBy(rows, x => x.Provider),
            DailyStats = StatsBy(rows, x => x.CreatedAt.Day.ToString("00"), sortByName: true),
        };
    }

    public object Any(AdminDailyChatAnalytics request)
    {
        var chatDb = AssertChatDb();
        var from = ParseDay(request.Day) ?? DateTime.Now.Date;
        var to = from.AddDays(1);

        using var db = chatDb.OpenDb();
        var rows = SelectPeriod(db, from, to);

        return new AdminDailyChatAnalyticsResponse
        {
            ModelStats = StatsBy(rows, x => x.Model),
            ProviderStats = StatsBy(rows, x => x.Provider),
        };
    }

    public object Any(AdminGetChatThread request)
    {
        var chatDb = AssertChatDb();
        // admins can read any user's thread, so it's looked up without a user filter
        var thread = chatDb.GetThread(request.Id, user: null)
            ?? throw HttpError.NotFound("Thread not found");

        return new AdminGetChatThreadResponse
        {
            Id = thread.Id,
            User = thread.User,
            Title = thread.Title,
            Model = thread.Model,
            Provider = thread.Provider,
            SystemPrompt = thread.SystemPrompt,
            Messages = ToMessages(thread.Messages),
        };
    }

    /// <summary>Only the columns the rollups need, so a busy month doesn't read every message</summary>
    static List<ChatRequest> SelectPeriod(IDbConnection db, DateTime from, DateTime to) =>
        db.Select(db.From<ChatRequest>()
            .Where(x => x.CreatedAt >= from && x.CreatedAt < to)
            .Select(x => new {
                x.CreatedAt, x.Model, x.Provider, x.Cost, x.InputTokens, x.OutputTokens, x.Duration,
            }));

    static List<ChatRequestStat> StatsBy(List<ChatRequest> rows, Func<ChatRequest, string?> keyFn,
        bool sortByName = false)
    {
        var stats = rows
            .GroupBy(x => keyFn(x) is { Length: > 0 } key ? key : Unattributed)
            .Select(g => new ChatRequestStat
            {
                Name = g.Key,
                Requests = g.Count(),
                InputTokens = g.Sum(x => x.InputTokens ?? 0),
                OutputTokens = g.Sum(x => x.OutputTokens ?? 0),
                Cost = g.Sum(x => x.Cost ?? 0),
                Duration = g.Sum(x => x.Duration ?? 0),
            });
        return sortByName
            ? stats.OrderBy(x => x.Name, StringComparer.Ordinal).ToList()
            // largest slice first, so the pie charts read consistently
            : stats.OrderByDescending(x => x.Cost).ThenByDescending(x => x.Requests)
                .ThenBy(x => x.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>Every month from the first logged request to now, for the month selector</summary>
    static List<string> GetAvailableMonths(IDbConnection db)
    {
        var currentMonth = StartOfMonth(DateTime.Now);
        var firstRequest = db.Scalar<DateTime?>(db.From<ChatRequest>().Select(x => Sql.Min(x.CreatedAt)));
        var lastRequest = db.Scalar<DateTime?>(db.From<ChatRequest>().Select(x => Sql.Max(x.CreatedAt)));

        var from = firstRequest != null ? StartOfMonth(firstRequest.Value) : currentMonth;
        var to = lastRequest != null ? StartOfMonth(lastRequest.Value) : currentMonth;
        if (to < currentMonth)
            to = currentMonth;

        var months = new List<string>();
        for (var month = from; month <= to; month = month.AddMonths(1))
            months.Add(month.ToString("yyyy-MM"));
        return months;
    }

    static List<ChatThreadMessage> ToMessages(string? messagesJson)
    {
        var to = new List<ChatThreadMessage>();
        if (string.IsNullOrEmpty(messagesJson))
            return to;
        JsonArray? messages;
        try
        {
            messages = ChatJson.Parse(messagesJson) as JsonArray;
        }
        catch (System.Text.Json.JsonException)
        {
            return to;
        }
        foreach (var messageNode in messages ?? [])
        {
            if (messageNode is not JsonObject message)
                continue;
            to.Add(new ChatThreadMessage
            {
                Role = message.GetString("role"),
                // content is either a string or an array of parts (text/image/audio/file)
                Content = ChatMessages.ContentToText(message["content"]),
            });
        }
        return to;
    }

    static DateTime StartOfMonth(DateTime date) => new(date.Year, date.Month, 1);

    /// <summary>yyyy-MM</summary>
    static DateTime? ParseMonth(string? month) =>
        month is { Length: > 0 } && DateTime.TryParse($"{month}-01", CultureInfo.InvariantCulture, out var date)
            ? StartOfMonth(date)
            : null;

    /// <summary>yyyy-MM-dd</summary>
    static DateTime? ParseDay(string? day) =>
        day is { Length: > 0 } && DateTime.TryParse(day, CultureInfo.InvariantCulture, out var date)
            ? date.Date
            : null;

    /// <summary>The narrowest [from,to) range the request asks for, defaulting to the current month</summary>
    static (DateTime From, DateTime To) ParsePeriod(string? month, string? day)
    {
        if (ParseDay(day) is { } dayStart)
            return (dayStart, dayStart.AddDays(1));
        var monthStart = ParseMonth(month) ?? StartOfMonth(DateTime.Now);
        return (monthStart, monthStart.AddMonths(1));
    }
}
