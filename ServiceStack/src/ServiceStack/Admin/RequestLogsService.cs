using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Admin;

[Icon(Svg = SvgIcons.Logs)]
[DataContract, ExcludeMetadata, Tag(TagNames.Admin)]
public class RequestLogs : IGet, IReturn<RequestLogsResponse>
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
    [DataMember(Order=10)] public string BearerToken { get; set; }
    [DataMember(Order=11)] public long[] Ids { get; set; }
    [DataMember(Order=12)] public int? BeforeId { get; set; }
    [DataMember(Order=13)] public int? AfterId { get; set; }
    [DataMember(Order=14)] public bool? HasResponse { get; set; }
    [DataMember(Order=15)] public bool? WithErrors { get; set; }
    [DataMember(Order=16)] public bool? EnableSessionTracking { get; set; }
    [DataMember(Order=17)] public bool? EnableResponseTracking { get; set; }
    [DataMember(Order=18)] public bool? EnableErrorTracking { get; set; }
    [DataMember(Order=19)] public TimeSpan? DurationLongerThan { get; set; }
    [DataMember(Order=20)] public TimeSpan? DurationLessThan { get; set; }
    [DataMember(Order=21)] public int Skip { get; set; }
    [DataMember(Order=22)] public int? Take { get; set; }
    [DataMember(Order=23)] public string OrderBy { get; set; }
    [DataMember(Order=24)] public DateTime? Month { get; set; }
}

[DataContract]
public class RequestLogsResponse
{
    [DataMember(Order=1)] public List<RequestLogEntry> Results { get; set; } = [];
    [DataMember(Order=2)] public Dictionary<string, string> Usage { get; set; }
    [DataMember(Order=3)] public int Total { get; set; }
    [DataMember(Order=4)] public ResponseStatus ResponseStatus { get; set; }
}

[DataContract]
public class GetAnalyticsInfo : IGet, IReturn<GetAnalyticsInfoResponse>
{
    [DataMember(Order=1)] 
    public DateTime? Month { get; set; }
    [DataMember(Order=2)] 
    public string Type { get; set; }
    [DataMember(Order=3)] 
    public string Op { get; set; }
    [DataMember(Order=4)] 
    public string ApiKey { get; set; }
    [DataMember(Order=5)] 
    public string UserId { get; set; }
    [DataMember(Order=6)] 
    public string Ip { get; set; }
}
[DataContract]
public class AnalyticsLogInfo
{
    [DataMember(Order=1)]
    public long Id { get; set; }
    [DataMember(Order=2)]
    public DateTime DateTime { get; set; }
    [DataMember(Order=3)]
    public string Browser { get; set; }
    [DataMember(Order=4)]
    public string Device { get; set; }
    [DataMember(Order=5)]
    public string Bot { get; set; }
    [DataMember(Order=6)]
    public string Op { get; set; }
    [DataMember(Order=7)]
    public string UserId { get; set; }
    [DataMember(Order=8)]
    public string UserName { get; set; }
    [DataMember(Order=9)]
    public string ApiKey { get; set; }
    [DataMember(Order=10)]
    public string Ip { get; set; }
}
[DataContract]
public class GetAnalyticsInfoResponse
{
    [DataMember(Order=1)]
    public List<string> Months { get; set; }
    [DataMember(Order=2)]
    public AnalyticsLogInfo Result { get; set; }
    [DataMember(Order=3)]
    public ResponseStatus ResponseStatus { get; set; }
}

[DataContract]
public class GetAnalyticsReports : IGet, IReturn<GetAnalyticsReportsResponse>
{
    [DataMember(Order=1)] 
    public DateTime? Month { get; set; }

    [DataMember(Order=2)] 
    public string Filter { get; set; }

    [DataMember(Order=3)] 
    public string Value { get; set; }

    [DataMember(Order=4)] 
    public bool? Force { get; set; }
}
[DataContract]
public class GetAnalyticsReportsResponse
{
    [DataMember(Order=1)]
    public AnalyticsReports Result { get; set; } = new();
    
    [DataMember(Order=2)]
    public ResponseStatus ResponseStatus { get; set; }
}

[DataContract]
public class AnalyticsReports
{
    [DataMember(Order=1)] public long Id { get; set; } // Use last Id of RequestLog
    [DataMember(Order=2)] public DateTime Created { get; set; } // When it was created
    [DataMember(Order=3)] public decimal Version { get; set; } // ServiceStack Version
    [DataMember(Order=4)] public Dictionary<string, RequestSummary> Apis { get; set; }
    [DataMember(Order=5)] public Dictionary<string, RequestSummary> Users { get; set; }
    [DataMember(Order=6)] public Dictionary<string, RequestSummary> Tags { get; set; }
    [DataMember(Order=7)] public Dictionary<string, RequestSummary> Status { get; set; }
    [DataMember(Order=8)] public Dictionary<string, RequestSummary> Days { get; set; }
    [DataMember(Order=9)] public Dictionary<string, RequestSummary> ApiKeys { get; set; }
    [DataMember(Order=10)] public Dictionary<string, RequestSummary> Ips { get; set; }
    [DataMember(Order=11)] public Dictionary<string, RequestSummary> Browsers { get; set; }
    [DataMember(Order=12)] public Dictionary<string, RequestSummary> Devices { get; set; }
    [DataMember(Order=13)] public Dictionary<string, RequestSummary> Bots { get; set; }
    [DataMember(Order=14)] public Dictionary<string, long> Durations { get; set; }
}

[DataContract]
public class UserAnalytics
{
    [DataMember(Order=1)] public long Id { get; set; } // Use last Id of RequestLog
    [DataMember(Order=2)] public string UserId { get; set; }
    [DataMember(Order=3)] public DateTime Created { get; set; } // When it was created
    [DataMember(Order=4)] public decimal Version { get; set; } // ServiceStack Version
    [DataMember(Order=5)] public AnalyticsReports Report { get; set; }
}

[DataContract]
public class ApiKeyAnalytics
{
    [DataMember(Order=1)] public long Id { get; set; } // Use last Id of RequestLog
    [DataMember(Order=2)] public string ApiKey { get; set; }
    [DataMember(Order=3)] public DateTime Created { get; set; } // When it was created
    [DataMember(Order=4)] public decimal Version { get; set; } // ServiceStack Version
    [DataMember(Order=5)] public AnalyticsReports Report { get; set; }
}

[DataContract]
public class IpAnalytics
{
    [DataMember(Order=1)] public long Id { get; set; } // Use last Id of RequestLog
    [DataMember(Order=2)] public string Ip { get; set; }
    [DataMember(Order=3)] public DateTime Created { get; set; } // When it was created
    [DataMember(Order=4)] public decimal Version { get; set; } // ServiceStack Version
    [DataMember(Order=5)] public AnalyticsReports Report { get; set; }
}

[DataContract]
public class RequestSummary
{
    // op,user,tag,status,day,apikey,time(ms 0-50,51-100,101-200ms,1-2s,2s-5s,5s+)
    // public string Type { get; set; }
    [DataMember(Order=1)] public string Name { get; set; }
    [DataMember(Order=2)] public long TotalRequests { get; set; }
    [DataMember(Order=3)] public long TotalRequestLength { get; set; }
    [DataMember(Order=4)] public long MinRequestLength { get; set; }
    [DataMember(Order=5)] public long MaxRequestLength { get; set; }
    [DataMember(Order=6)] public double TotalDuration { get; set; }
    [DataMember(Order=7)] public double MinDuration { get; set; }
    [DataMember(Order=8)] public double MaxDuration { get; set; }
    [DataMember(Order=9)] public Dictionary<int,long> Status { get; set; }
    [DataMember(Order=10)] public Dictionary<string, long> Durations { get; set; }
    [DataMember(Order=11)] public Dictionary<string, long> Apis { get; set; }
    [DataMember(Order=12)] public Dictionary<string, long> Users { get; set; }
    [DataMember(Order=13)] public Dictionary<string, long> Ips { get; set; }
    [DataMember(Order=14)] public Dictionary<string, long> ApiKeys { get; set; }
}

[DefaultRequest(typeof(RequestLogs))]
public class RequestLogsService(IRequestLogger requestLogger) : Service
{
    private static readonly Dictionary<string, string> Usage = new() {
        {"int BeforeSecs",              "Requests before elapsed time"},
        {"int AfterSecs",               "Requests after elapsed time"},
        {"string IpAddress",            "Requests matching Ip Address"},
        {"string ForwardedFor",         "Requests matching Forwarded Ip Address"},
        {"string UserAuthId",           "Requests matching UserAuthId"},
        {"string SessionId",            "Requests matching SessionId"},
        {"string Referer",              "Requests matching Http Referer"},
        {"string PathInfo",             "Requests matching PathInfo"},
        {"int BeforeId",                "Requests before RequestLog Id"},
        {"int AfterId",                 "Requests after RequestLog Id"},
        {"bool WithErrors",             "Requests with errors"},
        {"bool EnableSessionTracking",  "Turn On/Off Session Tracking"},
        {"bool EnableResponseTracking", "Turn On/Off Tracking of Responses"},
        {"bool EnableErrorTracking",    "Turn On/Off Tracking of Errors"},
        {"TimeSpan DurationLongerThan", "Requests with a duration longer than"},
        {"TimeSpan DurationLessThan",   "Requests with a duration less than"},
        {"int Skip",                    "Skip past N results"},
        {"int Take",                    "Only look at last N results"},
        {"string OrderBy",              "Order results by specified fields, e.g. SessionId,-Id"},
    };

    private async Task<RequestLogsFeature> AssertRequiredRole()
    {
        var feature = AssertPlugin<RequestLogsFeature>();
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AccessRole);
        return feature;
    }
    
    private async Task<(RequestLogsFeature, IRequireAnalytics)> AssertRequireAnalytics()
    {
        var feature = await AssertRequiredRole().ConfigAwait();
        if (feature.RequestLogger is not IRequireAnalytics analytics)
            throw new NotSupportedException(feature.RequestLogger + " does not support IRequireAnalytics");
        return (feature, analytics);
    }

    public async Task<object> Any(RequestLogs request)
    {
        var feature = await AssertRequiredRole().ConfigAwait();
        if (request.EnableSessionTracking.HasValue)
            requestLogger.EnableSessionTracking = request.EnableSessionTracking.Value;

        request.Take ??= feature.DefaultLimit;

        if (requestLogger is IRequireAnalytics analytics)
        {
            var results = analytics.QueryLogs(request);
            return new RequestLogsResponse {
                Results = results.ToList(),
                Total = (int)analytics.GetTotal(request.Month ?? DateTime.UtcNow),
                Usage = Usage,
            };
        }

        var snapshot =  requestLogger.GetLatestLogs(null);
        var logs = snapshot.AsQueryable();
        var now = DateTime.UtcNow;
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

        query = query.Skip(request.Skip);
        query = query.Take(request.Take.Value);

        return new RequestLogsResponse {
            Results = query.ToList(),
            Total = snapshot.Count,
            Usage = Usage,
        };
    }

    public async Task<object> Any(GetAnalyticsInfo request)
    {
        var (feature, analytics) = await AssertRequireAnalytics().ConfigAwait();
        if (request.Type == "info")
        {
            return new GetAnalyticsInfoResponse
            {
                Months = analytics.GetAnalyticInfo(feature.AnalyticsConfig).Months,
            };
        }

        var query = new RequestLogs
        {
            Month = request.Month,
            OperationName = request.Op,
            UserAuthId = request.UserId,
            BearerToken = request.ApiKey,
            IpAddress = request.Ip,
            Take = 1,
        };
        var results = analytics.QueryLogs(query);
        if (results.Count > 0)
        {
            var log = results[0];
            var info = new AnalyticsLogInfo
            {
                Id = log.Id,
                DateTime = log.DateTime,
                Op = log.OperationName ?? log.RequestDto?.GetType().Name,
                UserId = log.UserAuthId,
                Ip = log.IpAddress,
            };

            var headers = new Dictionary<string, string>(log.Headers ?? new(), StringComparer.OrdinalIgnoreCase);
            if (headers.TryGetValue(HttpHeaders.UserAgent, out var userAgent) && !string.IsNullOrEmpty(userAgent))
            {
                if (UserAgentHelper.IsBotUserAgent(userAgent, out var botName))
                {
                    info.Browser = "Bot";
                    info.Bot = botName;
                }
                else
                {
                    var (browser, version) = UserAgentHelper.GetBrowserInfo(userAgent);
                    info.Browser = browser;
                    info.Device = UserAgentHelper.GetDeviceType(userAgent);
                }
            }
            if (log.Meta?.TryGetValue("username", out var username) == true)
            {
                info.UserName = username;
            }
            if (headers.TryGetValue(HttpHeaders.Authorization, out var authorization) 
                && authorization.StartsWith("Bearer ak-", StringComparison.OrdinalIgnoreCase))
            {
                info.ApiKey = authorization.RightPart(' ');
            }
            
            return new GetAnalyticsInfoResponse
            {
                Result = info,
            };
        }
        return new GetAnalyticsInfoResponse();
    }

    public async Task<object> Any(GetAnalyticsReports request)
    {
        var (feature, analytics) = await AssertRequireAnalytics().ConfigAwait();
        if (request.Force == true)
        {
            if (request.Month != null)
            {
                analytics.ClearAnalyticsCaches(request.Month.Value);
            }
            else
            {
                var months = analytics.GetAnalyticInfo(feature.AnalyticsConfig).Months;
                foreach (var month in months)
                {
                    analytics.ClearAnalyticsCaches(DateTime.Parse(month + "-01"));
                }
            }
        }

        var filter = request.Filter?.ToLower();
        if (filter == "user")
        {
            if (string.IsNullOrEmpty(request.Value))
                throw new ArgumentNullException(nameof(request.Value));
            var userAnalytics = analytics.GetUserAnalytics(feature.AnalyticsConfig, request.Month ?? DateTime.UtcNow, request.Value);
            return new GetAnalyticsReportsResponse
            {
                Result = userAnalytics,
            };
        }
        if (filter == "apikey")
        {
            if (string.IsNullOrEmpty(request.Value))
                throw new ArgumentNullException(nameof(request.Value));
            var apiKeyAnalytics = analytics.GetApiKeyAnalytics(feature.AnalyticsConfig, request.Month ?? DateTime.UtcNow, request.Value);
            return new GetAnalyticsReportsResponse
            {
                Result = apiKeyAnalytics,
            };
        }
        if (filter == "ip")
        {
            if (string.IsNullOrEmpty(request.Value))
                throw new ArgumentNullException(nameof(request.Value));
            var ipAnalytics = analytics.GetIpAnalytics(feature.AnalyticsConfig, request.Month ?? DateTime.UtcNow, request.Value);
            return new GetAnalyticsReportsResponse
            {
                Result = ipAnalytics,
            };
        }

        var ret = analytics.GetAnalyticsReports(feature.AnalyticsConfig, request.Month ?? DateTime.UtcNow);
        foreach (var item in ret.Ips.ToList())
        {
            item.Value.Name = item.Key;
        }

        var userResolver = Request?.TryResolve<IUserResolver>();
        if (userResolver != null)
        {
            var allUserIds = ret.Users.Where(x => x.Value.Name == null)
                .Map(x => x.Key);
            var allUsers = await userResolver.GetUsersByIdsAsync(Request, allUserIds).ConfigAwait();
            var allUsersMap = new Dictionary<string, string>();
            foreach (var user in allUsers)
            {
                if (user.TryGetValue(nameof(IUserAuth.Id), out var oId)
                    && user.TryGetValue(nameof(IUserAuth.UserName), out var oUserName))
                {
                    if (oId != null && oUserName != null)
                    {
                        allUsersMap[oId.ToString()!] = oUserName.ToString();
                    }
                }
            }
            foreach (var user in ret.Users)
            {
                if (user.Value.Name == null && allUsersMap.TryGetValue(user.Key, out var userName))
                {
                    user.Value.Name = userName;
                }
            }
        }

        var results = filter switch
        {
            "apis" => new AnalyticsReports { Apis = ret.Apis },
            "users" => new AnalyticsReports { Users = ret.Users },
            "tags" => new AnalyticsReports { Tags = ret.Tags },
            "status" => new AnalyticsReports { Status = ret.Status },
            "days" => new AnalyticsReports { Days = ret.Days },
            "apikeys" => new AnalyticsReports { ApiKeys = ret.ApiKeys },
            "ips" => new AnalyticsReports { Ips = ret.Ips },
            "browsers" => new AnalyticsReports { Browsers = ret.Browsers, Bots = ret.Bots, Devices = ret.Devices },
            "durations" => new AnalyticsReports { Durations = ret.Durations },
            _ => ret,
        };

        results.Id = ret.Id;
        results.Created = ret.Created;
        results.Version = ret.Version;

        return new GetAnalyticsReportsResponse
        {
            Result = results,
        };
    }

}