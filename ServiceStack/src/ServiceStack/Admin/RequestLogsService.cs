using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.Web;

namespace ServiceStack.Admin;

[DataContract, ExcludeMetadata, Tag(TagNames.Admin)]
public class RequestLogs : IReturn<RequestLogsResponse>
{
    [DataMember(Order=1)] public int? BeforeSecs { get; set; }
    [DataMember(Order=2)] public int? AfterSecs { get; set; }
    [DataMember(Order=3)] public string OperationName { get; set; }
    [DataMember(Order=4)] public string IpAddress { get; set; }
    [DataMember(Order=5)] public string ForwardedFor { get; set; }
    [DataMember(Order=6)] public string UserAuthId { get; set; }
    [DataMember(Order=7)] public string SessionId { get; set; }
    [DataMember(Order=8)] public string Referer { get; set; }
    [DataMember(Order=9)] public string PathInfo { get; set; }
    [DataMember(Order=10)] public long[] Ids { get; set; }
    [DataMember(Order=11)] public int? BeforeId { get; set; }
    [DataMember(Order=12)] public int? AfterId { get; set; }
    [DataMember(Order=13)] public bool? HasResponse { get; set; }
    [DataMember(Order=14)] public bool? WithErrors { get; set; }
    [DataMember(Order=15)] public bool? EnableSessionTracking { get; set; }
    [DataMember(Order=16)] public bool? EnableResponseTracking { get; set; }
    [DataMember(Order=17)] public bool? EnableErrorTracking { get; set; }
    [DataMember(Order=18)] public TimeSpan? DurationLongerThan { get; set; }
    [DataMember(Order=19)] public TimeSpan? DurationLessThan { get; set; }
    [DataMember(Order=20)] public int Skip { get; set; }
    [DataMember(Order=21)] public int? Take { get; set; }
    [DataMember(Order=22)] public string OrderBy { get; set; }
}

[DataContract]
public class RequestLogsResponse
{
    public RequestLogsResponse()
    {
        this.Results = new List<RequestLogEntry>();
    }

    [DataMember(Order=1)] public List<RequestLogEntry> Results { get; set; }
    [DataMember(Order=2)] public Dictionary<string, string> Usage { get; set; }
    [DataMember(Order=3)] public int Total { get; set; }
    [DataMember(Order=4)] public ResponseStatus ResponseStatus { get; set; }
}

[DefaultRequest(typeof(RequestLogs))]
public class RequestLogsService : Service
{
    private static readonly Dictionary<string, string> Usage = new() {
        {"int BeforeSecs",      "Requests before elapsed time"},
        {"int AfterSecs",       "Requests after elapsed time"},
        {"string IpAddress",    "Requests matching Ip Address"},
        {"string ForwardedFor", "Requests matching Forwarded Ip Address"},
        {"string UserAuthId",   "Requests matching UserAuthId"},
        {"string SessionId",    "Requests matching SessionId"},
        {"string Referer",      "Requests matching Http Referer"},
        {"string PathInfo",     "Requests matching PathInfo"},
        {"int BeforeId",        "Requests before RequestLog Id"},
        {"int AfterId",         "Requests after RequestLog Id"},
        {"bool WithErrors",     "Requests with errors"},
        {"bool EnableSessionTracking",  "Turn On/Off Session Tracking"},
        {"bool EnableResponseTracking", "Turn On/Off Tracking of Responses"},
        {"bool EnableErrorTracking",    "Turn On/Off Tracking of Errors"},
        {"TimeSpan DurationLongerThan", "Requests with a duration longer than"},
        {"TimeSpan DurationLessThan", "Requests with a duration less than"},
        {"int Skip",            "Skip past N results"},
        {"int Take",            "Only look at last N results"},
        {"string OrderBy",      "Order results by specified fields, e.g. SessionId,-Id"},
    };

    public IRequestLogger RequestLogger { get; set; }

    public async Task<object> Any(RequestLogs request)
    {
        if (RequestLogger == null)
            throw new Exception("No IRequestLogger is registered");

        if (!HostContext.DebugMode)
            await RequiredRoleAttribute.AssertRequiredRolesAsync(Request, RequestLogger.RequiredRoles);

        if (request.EnableSessionTracking.HasValue)
            RequestLogger.EnableSessionTracking = request.EnableSessionTracking.Value;

        var feature = GetPlugin<RequestLogsFeature>();
        var defaultLimit = feature?.DefaultLimit ?? 100;

        var now = DateTime.UtcNow;
        var snapshot = RequestLogger.GetLatestLogs(null);
        var logs = snapshot.AsQueryable();

        if (request.BeforeSecs.HasValue)
            logs = logs.Where(x => (now - x.DateTime) <= TimeSpan.FromSeconds(request.BeforeSecs.Value));
        if (request.AfterSecs.HasValue)
            logs = logs.Where(x => (now - x.DateTime) > TimeSpan.FromSeconds(request.AfterSecs.Value));
        if (!request.OperationName.IsNullOrEmpty())
            logs = logs.Where(x => x.OperationName == request.OperationName);
        if (!request.IpAddress.IsNullOrEmpty())
            logs = logs.Where(x => x.IpAddress == request.IpAddress);
        if (!request.ForwardedFor.IsNullOrEmpty())
            logs = logs.Where(x => x.ForwardedFor == request.ForwardedFor);
        if (!request.UserAuthId.IsNullOrEmpty())
            logs = logs.Where(x => x.UserAuthId == request.UserAuthId);
        if (!request.SessionId.IsNullOrEmpty())
            logs = logs.Where(x => x.SessionId == request.SessionId);
        if (!request.Referer.IsNullOrEmpty())
            logs = logs.Where(x => x.Referer == request.Referer);
        if (!request.PathInfo.IsNullOrEmpty())
            logs = logs.Where(x => x.PathInfo == request.PathInfo);
        if (!request.Ids.IsEmpty())
            logs = logs.Where(x => request.Ids.Contains(x.Id));
        if (request.BeforeId.HasValue)
            logs = logs.Where(x => x.Id <= request.BeforeId);
        if (request.AfterId.HasValue)
            logs = logs.Where(x => x.Id > request.AfterId);
        if (request.WithErrors.HasValue)
            logs = request.WithErrors.Value
                ? logs.Where(x => x.ErrorResponse != null || x.StatusCode >= 400)
                : logs.Where(x => x.ErrorResponse == null);
        if (request.DurationLongerThan.HasValue)
            logs = logs.Where(x => x.RequestDuration > request.DurationLongerThan.Value);
        if (request.DurationLessThan.HasValue)
            logs = logs.Where(x => x.RequestDuration < request.DurationLessThan.Value);

        var query = string.IsNullOrEmpty(request.OrderBy)
            ? logs.OrderByDescending(x => x.Id)
            : logs.OrderBy(request.OrderBy);

        var results = query.Skip(request.Skip);
        results = results.Take(request.Take.GetValueOrDefault(defaultLimit));

        return new RequestLogsResponse {
            Results = results.ToList(),
            Total = snapshot.Count,
            Usage = Usage,
        };
    }
}