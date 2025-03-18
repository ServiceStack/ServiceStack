using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Admin;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Jobs;


public class RequestLog : IMeta
{
    [AutoIncrement]
    public long Id { get; set; }
    public string TraceId { get; set; }
    public string OperationName { get; set; }
    public DateTime DateTime { get; set; }
    public int StatusCode { get; set; }
    public string? StatusDescription { get; set; }
    public string? HttpMethod { get; set; }
    public string? AbsoluteUri { get; set; }
    public string? PathInfo { get; set; }
    public string? Request { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? RequestBody { get; set; }
    public string? UserAuthId { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? ForwardedFor { get; set; }
    public string? Referer { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public Dictionary<string, string>? FormData { get; set; }
    public Dictionary<string, string> Items { get; set; } = [];
    public Dictionary<string, string>? ResponseHeaders { get; set; }
    public string? Response { get; set; }
    public string? ResponseBody { get; set; }
    public string? SessionBody { get; set; }
    public ResponseStatus? Error { get; set; }
    public string? ExceptionSource { get; set; }
    public string? ExceptionDataBody { get; set; }
    public TimeSpan RequestDuration { get; set; }
    public Dictionary<string, string>? Meta { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Admin), ExplicitAutoQuery]
[NamedConnection("requests.db")]
public class AdminQueryRequestLogs : QueryDb<RequestLog>
{
    public DateTime? Month { get; set; }
}

public class SqliteRequestLogsService(IRequestLogger requestLogger, IAutoQueryDb autoQuery) 
    : Service
{
    private RequestLogsFeature AssertRequiredRole()
    {
        var feature = AssertPlugin<RequestLogsFeature>();
        RequiredRoleAttribute.AssertRequiredRoles(Request, feature.AccessRole);
        return feature;
    }

    public object Any(AdminQueryRequestLogs request)
    {
        var feature = AssertRequiredRole();
        var sqliteLogger = (SqliteRequestLogger)requestLogger;
        var month = request.Month ?? DateTime.UtcNow;
        using var monthDb = sqliteLogger.OpenMonthDb(month);
        var q = autoQuery.CreateQuery(request, base.Request, monthDb);
        return autoQuery.Execute(request, q, base.Request, monthDb);        
    }
}

public class SqliteRequestLogger : InMemoryRollingRequestLogger, IRequiresSchema, 
    IRequireRegistration, IConfigureServices, IRequireAnalytics
{
    private static readonly object dbWrites = Locks.RequestsDb;
    public string DbDir { get; set; } = "App_Data/requests";
    public bool EnableAdmin { get; set; } = true;
    public AutoQueryFeature? AutoQueryFeature { get; set; }
    public Type[] IgnoreRequestTypes { get; set; } =
    [
        typeof(AdminGetJob),
        typeof(AdminJobInfo),
        typeof(AdminJobDashboard),
        typeof(AdminGetJobProgress),
        typeof(AdminQueryBackgroundJobs),
        typeof(AdminQueryJobSummary),
        typeof(AdminQueryScheduledTasks),
        typeof(AdminQueryCompletedJobs),
        typeof(AdminQueryFailedJobs),
        typeof(AdminQueryRequestLogs),
    ];

    public Func<IDbConnectionFactory, DateTime, IDbConnection> ResolveMonthDb { get; set; }
    public Func<DateTime, string> DbMonthFile { get; set; } = DefaultDbMonthFile;
    public Func<List<string>> ResolveMonthDbs { get; set; }
    public int MaxLimit { get; set; } = 5000;
    public bool AutoInitSchema { get; set; } = true;
    public IDbConnectionFactory DbFactory { get; set; } = null!;
    public IAppHostNetCore AppHost { get; set; } = null!;

    public SqliteRequestLogger()
    {
        ResolveMonthDb = DefaultResolveMonthDb;
        ResolveMonthDbs = DefaultResolveMonthDbs;
    }

    public override void Log(IRequest request, object requestDto, object response, TimeSpan requestDuration)
    {
        if (ShouldSkip(request, requestDto))
            return;

        var requestType = requestDto?.GetType();

        var entry = CreateEntry(request, requestDto, response, requestDuration, requestType);

        RequestLogFilter?.Invoke(request, entry);

        if (IgnoreFilter != null)
        {
            if (entry.RequestDto != null && IgnoreFilter(entry.RequestDto))
                entry.RequestDto = null;
            if (entry.ResponseDto != null && IgnoreFilter(entry.ResponseDto))
                entry.ResponseDto = null;
            if (entry.Session != null && IgnoreFilter(entry.Session))
                entry.Session = null;
            if (entry.ErrorResponse != null && IgnoreFilter(entry.ErrorResponse))
                entry.ErrorResponse = null;
            if (entry.ExceptionData != null)
            {
                List<object>? keysToRemove = null;
                foreach (var key in entry.ExceptionData.Keys)
                {
                    var val = entry.ExceptionData[key];
                    if (val != null && IgnoreFilter(val))
                    {
                        keysToRemove ??= [];
                        keysToRemove.Add(key);
                    }
                }
                keysToRemove?.Each(entry.ExceptionData.Remove);
            }
        }

        logEntries.Enqueue(entry);
    }

    public List<RequestLogEntry> GetLatestLogs(DateTime month, int? take)
    {
        using var db = OpenMonthDb(month);
        var dbLogs = db.Select(db.From<RequestLog>()
            .Take(take ?? MaxLimit)
            .OrderByDescending(x => x.Id));
        
        var to = dbLogs.Map(ToRequestLogEntry);
        return to;
    }

    public override List<RequestLogEntry> GetLatestLogs(int? take)
    {
        return GetLatestLogs(DateTime.UtcNow, take);
    }

    public void Tick(ILogger log)
    {
        if (logEntries.IsEmpty) return;
        
        if (log.IsEnabled(LogLevel.Debug))
            log.LogDebug("Saving {Count} Request Log Entries...", logEntries.Count);
        using var db = OpenMonthDb(DateTime.UtcNow);
        while (logEntries.TryDequeue(out var entry))
        {
            try
            {
                var dbEntry = ToRequestLog(entry);
                lock (dbWrites)
                {
                    db.Insert(dbEntry);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "Error while saving request log entry: {Message}", e.Message);
            }
        }
    }

    public void Configure(IServiceCollection services)
    {
        if (EnableAdmin)
        {
            services.RegisterService<SqliteRequestLogsService>();
            AutoQueryFeature ??= new() { MaxLimit = 1000 };
            AutoQueryFeature.RegisterAutoQueryDbIfNotExists();
        }
    }

    public void Register(IAppHost appHost)
    {
        DbFactory ??= appHost.TryResolve<IDbConnectionFactory>() 
            ?? new OrmLiteConnectionFactory("Data Source=:memory:", SqliteDialect.Provider);
        AppHost ??= (IAppHostNetCore)appHost;
        AppHost.HostingEnvironment.ContentRootPath.CombineWith(DbDir).AssertDir();

        if (IgnoreRequestTypes.Length > 0)
        {
            ExcludeRequestDtoTypes = ExcludeRequestDtoTypes.Union(IgnoreRequestTypes).ToArray(); 
        }

        if (AutoInitSchema)
        {
            InitSchema();
        }
    }

    public static string DefaultDbMonthFile(DateTime createdDate) => $"requests_{createdDate.Year}-{createdDate.Month:00}.db";

    public List<string> DefaultResolveMonthDbs()
    {
        var requestsDir = AppHost.HostingEnvironment.ContentRootPath.CombineWith(DbDir);
        var dir = new DirectoryInfo(requestsDir);
        var files = dir.GetMatchingFiles("requests_*.db");
        return files.ToList();
    }
    
    public IDbConnection DefaultResolveMonthDb(IDbConnectionFactory dbFactory, DateTime createdDate)
    {
        var monthDb = DbMonthFile(createdDate);
        if (!OrmLiteConnectionFactory.NamedConnections.ContainsKey(monthDb))
        {
            var dataSource = AppHost.HostingEnvironment.ContentRootPath.CombineWith(DbDir, monthDb);
            dbFactory.RegisterConnection(monthDb, $"DataSource={dataSource};Cache=Shared", SqliteDialect.Provider);
            var db = dbFactory.OpenDbConnection(monthDb);
            InitMonthDbSchema(db);
            return db;
        }
        return dbFactory.OpenDbConnection(monthDb);
    }

    public IDbConnection OpenMonthDb(DateTime createdDate) => ResolveMonthDb(DbFactory, createdDate);

    public void InitMonthDbSchema(IDbConnection db)
    {
        db.CreateTableIfNotExists<RequestLog>();
    }

    public void InitSchema()
    {
        using var monthDb = OpenMonthDb(DateTime.UtcNow);
        InitMonthDbSchema(monthDb);
    }
    
    public static RequestLog ToRequestLog(RequestLogEntry from)
    {
        return new RequestLog
        {
            Id = from.Id,
            TraceId = from.TraceId,
            OperationName = from.OperationName ?? from.RequestDto?.GetType().Name,
            DateTime = from.DateTime,
            StatusCode = from.StatusCode,
            StatusDescription = from.StatusDescription,
            HttpMethod = from.HttpMethod,
            AbsoluteUri = from.AbsoluteUri,
            PathInfo = from.PathInfo,
            Request = from.RequestDto?.GetType().Name,
            RequestBody = from.RequestBody ?? ClientConfig.ToJson(from.RequestDto),
            UserAuthId = from.UserAuthId,
            SessionId = from.SessionId,
            IpAddress = from.IpAddress,
            ForwardedFor = from.ForwardedFor,
            Referer = from.Referer,
            Headers = from.Headers,
            FormData = from.FormData,
            Items = from.Items,
            ResponseHeaders = from.ResponseHeaders,
            Response = from.ResponseDto?.GetType().Name,
            ResponseBody = from.ResponseDto != null ? ClientConfig.ToJson(from.ResponseDto) : null,
            SessionBody = from.Session != null ? ClientConfig.ToJson(from.Session) : null,
            Error = from.ErrorResponse.GetResponseStatus(),
            ExceptionSource = from.ExceptionSource,
            ExceptionDataBody = from.ExceptionData != null ? ClientConfig.ToJson(from.ExceptionData) : null,
            RequestDuration = from.RequestDuration,
            Meta = from.Meta,
        };
    }

    public static RequestLogEntry ToRequestLogEntry(RequestLog from)
    {
        // /admin-ui/logging expects string responses
        return new RequestLogEntry
        {
            Id = from.Id,
            TraceId = from.TraceId,
            OperationName = from.OperationName ?? from.Request,
            DateTime = from.DateTime,
            StatusCode = from.StatusCode,
            StatusDescription = from.StatusDescription,
            HttpMethod = from.HttpMethod,
            AbsoluteUri = from.AbsoluteUri,
            PathInfo = from.PathInfo,
            RequestDto = from.RequestBody,// != null ? JSON.parse(from.RequestBody) : null,
            RequestBody = from.RequestBody,
            UserAuthId = from.UserAuthId,
            SessionId = from.SessionId,
            IpAddress = from.IpAddress,
            ForwardedFor = from.ForwardedFor,
            Referer = from.Referer,
            Headers = from.Headers,
            FormData = from.FormData,
            Items = from.Items,
            ResponseHeaders = from.ResponseHeaders,
            ResponseDto = from.ResponseBody,// != null ? JSON.parse(from.ResponseBody) : null,
            Session = from.SessionBody,// != null ? JSON.parse(from.SessionBody) : null,
            ErrorResponse = from.Error,
            ExceptionSource = from.ExceptionSource,
            ExceptionData = from.ExceptionDataBody != null ? JSON.parse(from.ExceptionDataBody) as IDictionary : null,
            RequestDuration = from.RequestDuration,
            Meta = from.Meta,
        };
    }

    public AnalyticsInfo GetAnalyticInfo(AnalyticsConfig config)
    {
        var monthDbs = ResolveMonthDbs().Map(x => x.Replace('\\','/')); 
        return new AnalyticsInfo
        {
            Months = monthDbs.Map(x => x.LastRightPart('/').RightPart('_').LeftPart('.'))
                .OrderBy(x => x).ToList()
        };
    }

    public AnalyticsReports GetAnalyticsReports(AnalyticsConfig config, DateTime month)
    {
        using var db = OpenMonthDb(month);

        var tableExists = db.TableExists<AnalyticsReports>();
        var lastLogId = tableExists
            ? db.Single(db.From<RequestLog>().OrderByDescending(x => x.Id).Limit(1))?.Id
            : null;

        AnalyticsReports? cachedReport = null;
        try
        {
            // Ignore schema changes, table is recreated when cached
            cachedReport = lastLogId != null
                ? db.SingleById<AnalyticsReports>(lastLogId)
                : null;
        } catch (Exception ignore) {}

        if (cachedReport != null)
            return cachedReport;
        
        List<RequestLog> batch = [];
        long lastPk = 0;
        var metadata = HostContext.Metadata;
        var ret = new AnalyticsReports
        {
            Apis = new(),
            Users = new(),
            Tags = new(),
            Status = new(),
            Days = new(),
            ApiKeys = new(),
            IpAddresses = new(),
            Browsers = new(),
            Devices = new(),
            Bots = new(),
            DurationRange = new(),
        }; 

        void Add(Dictionary<string, RequestSummary> results, string name, RequestLog log)
        {
            var summary = results.TryGetValue(name, out var existing)
                ? existing
                : results[name] = new();
            
            summary.TotalRequests += 1;

            var len = log.RequestBody?.Length ?? 0;
            summary.TotalRequestLength += len;
            if (len > 0)
            {
                if (summary.MinRequestLength == 0 || len < summary.MinRequestLength)
                    summary.MinRequestLength = len;
                if (summary.MaxRequestLength == 0 || len > summary.MaxRequestLength)
                    summary.MaxRequestLength = len;
            }
            
            var duration = log.RequestDuration.TotalMilliseconds; 
            summary.TotalDuration += duration;
            if (duration > 0 && log.StatusCode is >= 200 and < 300)
            {
                if (summary.MinDuration == 0 || duration < summary.MinDuration)
                    summary.MinDuration = duration;
                if (summary.MaxDuration == 0 || duration > summary.MaxDuration)
                    summary.MaxDuration = duration;
            }
            
        }
        void AddSummary(Dictionary<string, RequestSummary> results, string name, RequestSummary apiSummary)
        {
            var summary = results.TryGetValue(name, out var existing)
                ? existing
                : results[name] = new();
            summary.TotalRequests += apiSummary.TotalRequests;
            summary.TotalDuration += apiSummary.TotalDuration;
            summary.TotalRequestLength += apiSummary.TotalRequestLength;
        }
        
        do {
            batch = db.Select(
                db.From<RequestLog>()
                    .Where(x => x.Id > lastPk)
                    .OrderBy(x => x.Id)
                    .Limit(config.BatchSize));
            foreach (var requestLog in batch)
            {
                Add(ret.Apis, requestLog.Request ?? requestLog.OperationName, requestLog);
                if (requestLog.StatusCode > 0)
                {
                    var apiLog = ret.Apis[requestLog.Request ?? requestLog.OperationName];
                    apiLog.Status ??= new();
                    apiLog.Status[requestLog.StatusCode] = apiLog.Status.TryGetValue(requestLog.StatusCode, out var existing)
                        ? existing + 1
                        : 1;
                }

                if (requestLog.UserAuthId != null)
                {
                    Add(ret.Users, requestLog.UserAuthId, requestLog);
                    if (requestLog.Meta?.TryGetValue("username", out var username) == true)
                    {
                        ret.Users[requestLog.UserAuthId].Name = username;
                    }
                }

                if (requestLog.StatusCode > 0)
                {
                    Add(ret.Status, requestLog.StatusCode.ToString(), requestLog);
                }

                Add(ret.Days, requestLog.DateTime.Day.ToString(), requestLog);

                var headers = new Dictionary<string, string>(requestLog.Headers ?? new(), StringComparer.OrdinalIgnoreCase);

                if ((headers.TryGetValue(HttpHeaders.Authorization, out var authorization) && authorization.StartsWith("ak-")) ||
                    requestLog.Meta?.TryGetValue("apikey", out authorization) == true)
                {
                    Add(ret.ApiKeys, authorization, requestLog);
                }
                
                if (requestLog.IpAddress != null)
                    Add(ret.IpAddresses, requestLog.IpAddress, requestLog);

                if (headers.TryGetValue(HttpHeaders.UserAgent, out var userAgent) && !string.IsNullOrEmpty(userAgent))
                {
                    if (UserAgentHelper.IsBotUserAgent(userAgent, out var botName))
                    {
                        Add(ret.Browsers, "Bot", requestLog);
                        Add(ret.Bots, botName, requestLog);
                    }
                    else
                    {
                        var (browser, version) = UserAgentHelper.GetBrowserInfo(userAgent);
                        Add(ret.Browsers, browser, requestLog);
                        Add(ret.Devices, UserAgentHelper.GetDeviceType(userAgent), requestLog);
                    }
                }
                else
                {
                    Add(ret.Browsers, "None", requestLog);
                    Add(ret.Devices, "None", requestLog);
                }

                var totalMs = (int)requestLog.RequestDuration.TotalMilliseconds;

                var added = false;
                foreach (var range in config.DurationRanges)
                {
                    if (totalMs < range)
                    {
                        ret.DurationRange[range.ToString()] = ret.DurationRange.TryGetValue(range.ToString(), out var duration)
                            ? duration + 1
                            : 1;
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    var lastRange = ">" + config.DurationRanges.Last();
                    ret.DurationRange[lastRange] = ret.DurationRange.TryGetValue(lastRange, out var duration)
                        ? duration + 1
                        : 1;
                }
                lastPk = requestLog.Id;
            }
        } while(batch.Count >= config.BatchSize);

        ret.Id = lastPk;
        ret.Created = DateTime.UtcNow;
        ret.Version = Env.ServiceStackVersion;

        foreach (var entry in ret.Status)
        {
            if (int.TryParse(entry.Key, out var status))
            {
                var desc = HttpStatus.GetStatusDescription(status);
                if (!string.IsNullOrEmpty(desc))
                {
                    entry.Value.Name = desc;
                }
            }
        }
        foreach (var requestDto in ret.Apis.Keys)
        {
            var requestType = metadata.GetRequestType(requestDto);
            if (requestType != null)
            {
                var op = metadata.GetOperation(requestType);
                if (op != null)
                {
                    foreach (var tag in op.Tags)
                    {
                        AddSummary(ret.Tags, tag, ret.Apis[requestDto]);
                    }
                }
            }
        }

        void Clean(Dictionary<string, RequestSummary> results)
        {
            foreach (var entry in results.Values)
            {
                entry.TotalDuration = Math.Floor(entry.TotalDuration);
            }
        }
        
        Clean(ret.Apis);
        Clean(ret.Users);
        Clean(ret.Tags);
        Clean(ret.Status);
        Clean(ret.Days);
        Clean(ret.ApiKeys);
        Clean(ret.IpAddresses);
        
        db.DropAndCreateTable<AnalyticsReports>();
        db.Insert(ret);
        
        return ret;
    }

    public Dictionary<string, long> GetApiAnalytics(AnalyticsConfig config, DateTime month, AnalyticsType type, string value)
    {
        using var db = OpenMonthDb(month);
        List<RequestLog> batch = [];
        long lastPk = 0;

        var ret = new Dictionary<string, long>();

        do
        {
            var q = db.From<RequestLog>()
                .Where(x => x.Id > lastPk);

            if (type == AnalyticsType.User)
            {
                q.And(x => x.UserAuthId == value);
            }
            else if (type == AnalyticsType.Day)
            {
                var day = value.ToInt();
                var from = month.WithDay(day).Date;
                var to = from.AddDays(1);
                q.And(x => x.DateTime >= from && x.DateTime < to);
            }
            else if (type == AnalyticsType.ApiKey)
            {
                q.And("Headers LIKE {0}", $"%Bearer {value}%");
            }
            else if (type == AnalyticsType.IpAddress)
            {
                q.And(x => x.IpAddress == value);
            }

            batch = db.Select(q
                .OrderBy(x => x.Id)
                .Limit(config.BatchSize));
            foreach (var requestLog in batch)
            {
                var op = requestLog.Request ?? requestLog.OperationName;
                ret[op] = ret.TryGetValue(op, out var existing)
                    ? existing + 1
                    : 1;
                lastPk = requestLog.Id;
            }
        } while(batch.Count >= config.BatchSize);
        return ret;
    }
}
