using System.Collections.Generic;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack;

public class GetAdminDashboard {}
public class GetAdminDashboardResponse : IHasResponseStatus
{
    public ServerStats ServerStats { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

public class ServerStats
{
    public Dictionary<string, long> Redis { get; set; }
    public Dictionary<string, string> ServerEvents { get; set; }
    public string MqDescription { get; set; }
    public Dictionary<string, long> MqWorkers { get; set; }
}


[DefaultRequest(typeof(GetAdminDashboard))]
[Restrict(VisibilityTo = RequestAttributes.Localhost)]
public class GetAdminDashboardServices : Service
{
    public object Any(GetAdminDashboard request)
    {
        var mqServer = TryResolve<IMessageService>();
        var mqWorker = mqServer?.GetStats();
        var stats = new ServerStats {
            Redis = (TryResolve<IRedisClientsManager>() as IHasStats)?.Stats,
            ServerEvents = TryResolve<IServerEvents>()?.GetStats(),
            MqDescription = mqServer?.GetStatsDescription(),
            MqWorkers = mqWorker.ToDictionary(),
        };
        
        return new GetAdminDashboardResponse
        {
            ServerStats = stats,
        };
    }
}

public static class AdminStatsUtils
{
    public static Dictionary<string, long> ToDictionary(this IMessageHandlerStats stats)
    {
        if (stats == null)
            return null;
        
        var to = new Dictionary<string, long>
        {
            [nameof(stats.TotalMessagesProcessed)] = stats.TotalMessagesProcessed,
            [nameof(stats.TotalMessagesFailed)] = stats.TotalMessagesFailed,
            [nameof(stats.TotalRetries)] = stats.TotalRetries,
            [nameof(stats.TotalNormalMessagesReceived)] = stats.TotalNormalMessagesReceived,
            [nameof(stats.TotalPriorityMessagesReceived)] = stats.TotalPriorityMessagesReceived,
        };
        if (stats.LastMessageProcessed != null)
            to[nameof(stats.LastMessageProcessed)] = stats.LastMessageProcessed.Value.ToUnixTime();
        
        return to;
    }
}