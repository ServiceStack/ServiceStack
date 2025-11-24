using System.Collections;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Admin;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Jobs;

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

    public Action<SqliteOrmLiteDialectProviderBase>? ConfigureDialectProvider { get; set; }
    public SqliteOrmLiteDialectProviderBase DialectProvider { get; set; }
    public Action<IDbConnection>? ConfigureDb { get; set; }
    public Func<IDbConnectionFactory, DateTime, IDbConnection> ResolveMonthDb { get; set; }
    public Func<DateTime, string> DbMonthFile { get; set; } = DefaultDbMonthFile;
    public Func<List<string>> ResolveMonthDbs { get; set; }
    public int MaxLimit { get; set; } = 5000;
    public bool AutoInitSchema { get; set; } = true;
    public IDbConnectionFactory DbFactory { get; set; } = null!;
    public bool EnableWriterLock { get; set; } = true;
    public IAppHostNetCore AppHost { get; set; } = null!;

    public SqliteRequestLogger()
    {
        ResolveMonthDb = DefaultResolveMonthDb;
        ResolveMonthDbs = DefaultResolveMonthDbs;
        ConfigureDb = DefaultConfigureDb;
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

    public long GetTotal(DateTime month)
    {
        using var db = OpenMonthDb(month);
        return db.Count(db.From<RequestLog>());
    }

    public List<RequestLogEntry> QueryLogs(RequestLogs request)
    {
        using var db = OpenMonthDb(request.Month ?? DateTime.UtcNow);
        var now = DateTime.UtcNow;
        var take = request.Take ?? MaxLimit;
        
        var q = db.From<RequestLog>();
        var Headers = q.DialectProvider.GetQuotedColumnName(nameof(RequestLog.Headers));
        if (request.BeforeSecs.HasValue)
            q = q.Where(x => (now - x.DateTime) <= TimeSpan.FromSeconds(request.BeforeSecs.Value));
        if (request.AfterSecs.HasValue)
            q = q.Where(x => (now - x.DateTime) > TimeSpan.FromSeconds(request.AfterSecs.Value));
        if (!request.OperationName.IsNullOrEmpty())
            q = q.Where(x => x.OperationName == request.OperationName);
        if (!request.IpAddress.IsNullOrEmpty())
            q = q.Where(x => x.IpAddress == request.IpAddress);
        if (!request.ForwardedFor.IsNullOrEmpty())
            q = q.Where(x => x.ForwardedFor == request.ForwardedFor);
        if (!request.UserAuthId.IsNullOrEmpty())
            q = q.Where(x => x.UserAuthId == request.UserAuthId);
        if (!request.SessionId.IsNullOrEmpty())
            q = q.Where(x => x.SessionId == request.SessionId);
        if (!request.Referer.IsNullOrEmpty())
            q = q.Where(x => x.Referer == request.Referer);
        if (!request.PathInfo.IsNullOrEmpty())
            q = q.Where(x => x.PathInfo == request.PathInfo);
        if (!request.BearerToken.IsNullOrEmpty())
            q = q.Where(Headers + " LIKE {0}", $"%Bearer {request.BearerToken.SqlVerifyFragment()}%");
        if (!request.Ids.IsEmpty())
            q = q.Where(x => request.Ids.Contains(x.Id));
        if (request.BeforeId.HasValue)
            q = q.Where(x => x.Id <= request.BeforeId);
        if (request.AfterId.HasValue)
            q = q.Where(x => x.Id > request.AfterId);
        if (request.WithErrors.HasValue)
            q = request.WithErrors.Value
                ? q.Where(x => x.Error != null || x.StatusCode >= 400)
                : q.Where(x => x.Error == null);
        if (request.DurationLongerThan.HasValue)
            q = q.Where(x => x.RequestDuration > request.DurationLongerThan.Value);
        if (request.DurationLessThan.HasValue)
            q = q.Where(x => x.RequestDuration < request.DurationLessThan.Value);
        q = string.IsNullOrEmpty(request.OrderBy)
            ? q.OrderByDescending(x => x.Id)
            : q.OrderBy(request.OrderBy);
        q = request.Skip > 0
            ? q.Limit(request.Skip, take)
            : q.Limit(take);
        
        var results = db.Select(q);
        var to = results.Map(ToRequestLogEntry);
        return to;
    }

    public override List<RequestLogEntry> GetLatestLogs(int? take)
    {
        return QueryLogs(new RequestLogs { Take =  take });
    }

    public Task TickAsync(ILogger log, CancellationToken token = default)
    {
        Tick(log);
        return Task.CompletedTask;
    }
    
    public void Tick(ILogger log)
    {
        if (logEntries.IsEmpty) return;
        
        if (log.IsEnabled(LogLevel.Debug))
            log.LogDebug("Saving {Count} Request Log Entries...", logEntries.Count);
        var now = DateTime.UtcNow;
        using var db = OpenMonthDb(now);
        while (logEntries.TryDequeue(out var entry))
        {
            try
            {
                var dbEntry = ToRequestLog(entry);
                db.Insert(dbEntry);
            }
            catch (Exception e)
            {
                log.LogError("Error while saving request log entry: {Message}", e.Message);
                // Requeue and wait for next tick
                logEntries.Enqueue(entry);
                return;
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
        DialectProvider = SqliteConfiguration.Configure(SqliteDialect.Create());
        DialectProvider.EnableWriterLock = EnableWriterLock;
        ConfigureDialectProvider?.Invoke(DialectProvider);

        DbFactory ??= appHost.TryResolve<IDbConnectionFactory>() 
                      ?? new OrmLiteConnectionFactory("Data Source=:memory:", DialectProvider);
        AppHost ??= (IAppHostNetCore)appHost;        
        _ = GetDbDir().AssertDir();
        
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
        var requestsDir = GetDbDir();
        var dir = new DirectoryInfo(requestsDir);
        var files = dir.GetMatchingFiles("requests_*.db");
        return files.ToList();
    }
    
    public void DefaultConfigureDb(IDbConnection db) => db.WithTag(GetType().Name);
    
    public IDbConnection DefaultResolveMonthDb(IDbConnectionFactory dbFactory, DateTime createdDate)
    {
        var factory = (IDbConnectionFactoryExtended)dbFactory;
        var monthDb = DbMonthFile(createdDate);
        lock (this)
        {
            if (!OrmLiteConnectionFactory.NamedConnections.ContainsKey(monthDb))
            {
                var dataSource =  GetDbDir(monthDb);
                dbFactory.RegisterConnection(monthDb, $"DataSource={dataSource};Cache=Shared", DialectProvider);
                var db = factory.OpenDbConnection(monthDb, ConfigureDb);
                InitMonthDbSchema(db, createdDate);
                return db;
            }
        }
        return factory.OpenDbConnection(monthDb, ConfigureDb);
    }

    public IDbConnection OpenMonthDb(DateTime createdDate) => ResolveMonthDb(DbFactory, createdDate);

    public void InitMonthDbSchema(IDbConnection db, DateTime month)
    {
        db.CreateTableIfNotExists<RequestLog>();
    }

    public void InitSchema()
    {
        var now = DateTime.UtcNow;
        using var db = OpenMonthDb(now);
        InitMonthDbSchema(db, now);
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
        var ret = new AnalyticsInfo
        {
            Months = monthDbs.Map(x => x.LastRightPart('/').RightPart('_').LeftPart('.'))
                .OrderBy(x => x).ToList()
        };

        try
        {
            var sql = 
                """
                SELECT
                (SELECT 1 WHERE EXISTS(SELECT 1 from RequestLog where OperationName IS NOT NULL)) AS apis,
                (SELECT 1 WHERE EXISTS(SELECT 1 from RequestLog where UserAuthId IS NOT NULL)) AS users,
                (SELECT 1 WHERE EXISTS(SELECT 1 from RequestLog where Headers LIKE '%Bearer ak-%')) AS apiKeys,
                (SELECT 1 WHERE EXISTS(SELECT 1 from RequestLog where IpAddress IS NOT NULL)) AS ips
                """;
            using var db = OpenMonthDb(DateTime.UtcNow);
            var result = db.SqlList<(int? apis, int? users, int? apiKeys, bool? ips)>(sql).FirstOrDefault();
            ret.Tabs = new();
            if (result.apis == 1)
                ret.Tabs["APIs"] = "";
            if (result.users == 1)
                ret.Tabs["Users"] = "users";
            if (result.apis == 1)
                ret.Tabs["API Keys"] = "apiKeys";
            if (result.apis == 1)
                ret.Tabs["IP Addresses"] = "ips";
        }
        catch (Exception ignore) {}
        
        return ret;
    }

    public void ClearAnalyticsCaches(DateTime month)
    {
        using var db = OpenMonthDb(month);
        db.DropTable<AnalyticsReports>();
    }
    
    private static AnalyticsReports CreateAnalyticsReports()
    {
        var ret = new AnalyticsReports
        {
            Apis = new(),
            Users = new(),
            Tags = new(),
            Status = new(),
            Days = new(),
            ApiKeys = new(),
            Ips = new(),
            Browsers = new(),
            Devices = new(),
            Bots = new(),
            Durations = new(),
        };
        return ret;
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
        var ret = CreateAnalyticsReports();
        
        do {
            batch = db.Select(
                db.From<RequestLog>()
                    .Where(x => x.Id > lastPk)
                    .OrderBy(x => x.Id)
                    .Limit(config.BatchSize));
            
            foreach (var requestLog in batch)
            {
                ret.AddRequestLog(requestLog, config);
                lastPk = requestLog.Id;
            }
            
        } while(batch.Count >= config.BatchSize);

        ret.Id = lastPk;
        ret.CleanResults(config);

        db.DropAndCreateTable<AnalyticsReports>();
        db.Insert(ret);
        
        return ret;
    }

    public AnalyticsReports GetApiAnalytics(AnalyticsConfig config, DateTime month, string op)
    {
        using var db = OpenMonthDb(month);

        var tableExists = db.TableExists<ApiAnalytics>();
        var lastLogId = tableExists
            ? db.Single(db.From<RequestLog>()
                .Where(x => x.OperationName == op)
                .OrderByDescending(x => x.Id).Limit(1))?.Id
            : null;

        ApiAnalytics? apiAnalytics = null;
        try
        {
            // Ignore schema changes, table is recreated when cached
            apiAnalytics = lastLogId != null
                ? db.SingleById<ApiAnalytics>(lastLogId)
                : null;
        } catch (Exception ignore) {}

        if (apiAnalytics?.Report != null)
            return apiAnalytics.Report;
        
        List<RequestLog> batch = [];
        long lastPk = 0;
        var ret = CreateAnalyticsReports();
        
        do {
            batch = db.Select(
                db.From<RequestLog>()
                    .Where(x => x.Id > lastPk)
                    .And(x => x.OperationName == op)
                    .OrderBy(x => x.Id)
                    .Limit(config.BatchSize));
            
            foreach (var requestLog in batch)
            {
                ret.AddRequestLog(requestLog, config);
                lastPk = requestLog.Id;
            }
            
        } while(batch.Count >= config.BatchSize);

        ret.Id = lastPk;
        ret.CleanResults(config);

        if (ret.Users?.Count > 0)
        {
            db.CreateTableIfNotExists<ApiAnalytics>();
            db.Delete<ApiAnalytics>(x => x.Request == op);
                
            db.Insert(new ApiAnalytics
            {
                Id =  lastPk,
                Request = op,
                Created = DateTime.UtcNow,
                Version =  Env.ServiceStackVersion,
                Report = ret,
            });
        }
        
        return ret;
    }

    public AnalyticsReports GetUserAnalytics(AnalyticsConfig config, DateTime month, string userId)
    {
        using var db = OpenMonthDb(month);

        var tableExists = db.TableExists<UserAnalytics>();
        var lastLogId = tableExists
            ? db.Single(db.From<RequestLog>()
                .Where(x => x.UserAuthId == userId)
                .OrderByDescending(x => x.Id).Limit(1))?.Id
            : null;

        UserAnalytics? userAnalytics = null;
        try
        {
            // Ignore schema changes, table is recreated when cached
            userAnalytics = lastLogId != null
                ? db.SingleById<UserAnalytics>(lastLogId)
                : null;
        } catch (Exception ignore) {}

        if (userAnalytics?.Report != null)
            return userAnalytics.Report;
        
        List<RequestLog> batch = [];
        long lastPk = 0;
        var ret = CreateAnalyticsReports();
        
        do {
            batch = db.Select(
                db.From<RequestLog>()
                    .Where(x => x.Id > lastPk)
                    .And(x => x.UserAuthId == userId)
                    .OrderBy(x => x.Id)
                    .Limit(config.BatchSize));
            
            foreach (var requestLog in batch)
            {
                ret.AddRequestLog(requestLog, config);
                lastPk = requestLog.Id;
            }
            
        } while(batch.Count >= config.BatchSize);

        ret.Id = lastPk;
        ret.CleanResults(config);

        if (ret.Users?.Count > 0)
        {
            db.CreateTableIfNotExists<UserAnalytics>();
            db.Delete<UserAnalytics>(x => x.UserId == userId);
                
            db.Insert(new UserAnalytics
            {
                Id =  lastPk,
                UserId = userId,
                Created = DateTime.UtcNow,
                Version =  Env.ServiceStackVersion,
                Report = ret,
            });
        }
        
        return ret;
    }

    public AnalyticsReports GetApiKeyAnalytics(AnalyticsConfig config, DateTime month, string apiKey)
    {
        apiKey.SqlVerifyFragment();
        
        using var db = OpenMonthDb(month);
        var Headers = db.GetDialectProvider().GetQuotedColumnName(nameof(RequestLog.Headers));

        var tableExists = db.TableExists<ApiKeyAnalytics>();
        var lastLogId = tableExists
            ? db.Single(db.From<RequestLog>()
                .And(Headers + " LIKE {0}", $"%Bearer {apiKey}%")
                .OrderByDescending(x => x.Id).Limit(1))?.Id
            : null;

        ApiKeyAnalytics? apiKeyAnalytics = null;
        try
        {
            // Ignore schema changes, table is recreated when cached
            apiKeyAnalytics = lastLogId != null
                ? db.SingleById<ApiKeyAnalytics>(lastLogId)
                : null;
        } catch (Exception ignore) {}

        if (apiKeyAnalytics?.Report != null)
            return apiKeyAnalytics.Report;
        
        List<RequestLog> batch = [];
        long lastPk = 0;
        var ret = CreateAnalyticsReports();
        
        do {
            batch = db.Select(
                db.From<RequestLog>()
                    .Where(x => x.Id > lastPk)
                    .And(Headers + " LIKE {0}", $"%Bearer {apiKey}%")
                    .OrderBy(x => x.Id)
                    .Limit(config.BatchSize));
            
            foreach (var requestLog in batch)
            {
                ret.AddRequestLog(requestLog, config);
                lastPk = requestLog.Id;
            }
            
        } while(batch.Count >= config.BatchSize);

        ret.Id = lastPk;
        ret.CleanResults(config);

        if (ret.ApiKeys?.Count > 0)
        {
            db.CreateTableIfNotExists<ApiKeyAnalytics>();
            db.Delete<ApiKeyAnalytics>(x => x.ApiKey == apiKey);
                
            db.Insert(new ApiKeyAnalytics
            {
                Id =  lastPk,
                ApiKey = apiKey,
                Created = DateTime.UtcNow,
                Version =  Env.ServiceStackVersion,
                Report = ret,
            });
        }
        
        return ret;
    }

    public AnalyticsReports GetIpAnalytics(AnalyticsConfig config, DateTime month, string ip)
    {
        ip.SqlVerifyFragment();
        
        using var db = OpenMonthDb(month);

        var tableExists = db.TableExists<IpAnalytics>();
        var lastLogId = tableExists
            ? db.Single(db.From<RequestLog>()
                .And(x => x.IpAddress == ip)
                .OrderByDescending(x => x.Id).Limit(1))?.Id
            : null;

        IpAnalytics? apiKeyAnalytics = null;
        try
        {
            // Ignore schema changes, table is recreated when cached
            apiKeyAnalytics = lastLogId != null
                ? db.SingleById<IpAnalytics>(lastLogId)
                : null;
        } catch (Exception ignore) {}

        if (apiKeyAnalytics?.Report != null)
            return apiKeyAnalytics.Report;
        
        List<RequestLog> batch = [];
        long lastPk = 0;
        var ret = CreateAnalyticsReports();
        
        do {
            batch = db.Select(
                db.From<RequestLog>()
                    .Where(x => x.Id > lastPk)
                    .And(x => x.IpAddress == ip)
                    .OrderBy(x => x.Id)
                    .Limit(config.BatchSize));
            
            foreach (var requestLog in batch)
            {
                ret.AddRequestLog(requestLog, config);
                lastPk = requestLog.Id;
            }
            
        } while(batch.Count >= config.BatchSize);

        ret.Id = lastPk;
        ret.CleanResults(config);

        if (ret.ApiKeys?.Count > 0)
        {
            db.CreateTableIfNotExists<IpAnalytics>();
            db.Delete<IpAnalytics>(x => x.Ip == ip);
                
            db.Insert(new IpAnalytics
            {
                Id =  lastPk,
                Ip = ip,
                Created = DateTime.UtcNow,
                Version =  Env.ServiceStackVersion,
                Report = ret,
            });
        }

        return ret;
    }
    
    private string GetDbDir(string monthDb = "")
    {
        return Path.IsPathRooted(DbDir) 
            ? DbDir.CombineWith(monthDb)
            : AppHost.HostingEnvironment.ContentRootPath.CombineWith(DbDir, monthDb);
    }
}

public static class SqliteRequestLoggerUtils
{
    [Flags]
    enum Detail
    {
        None = 0,
        Apis = 1 << 0,
        Users = 1 << 1,
        Status = 1 << 2,
        ApiKeys = 1 << 3,
        Ips = 1 << 4,
        Durations = 1 << 5,
    }
    
    public static void AddRequestLog(this AnalyticsReports ret, RequestLog requestLog, AnalyticsConfig config)
    {
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

        void AddDetail(RequestSummary summary, RequestLog log, Dictionary<string,string> headers, Detail details)
        {
            if (details.HasFlag(Detail.Apis))
            {
                var op = log.Request ?? log.OperationName;
                if (!string.IsNullOrEmpty(op))
                {
                    summary.Apis ??= new();
                    summary.Apis[op] = summary.Apis.GetValueOrDefault(op) + 1;
                }
            }
            if (details.HasFlag(Detail.Users))
            {
                if (log.UserAuthId != null)
                {
                    summary.Users ??= new();
                    summary.Users[log.UserAuthId] = summary.Users.GetValueOrDefault(log.UserAuthId) + 1;
                }
            }
            if (details.HasFlag(Detail.Status))
            {
                summary.Status ??= new();
                summary.Status[log.StatusCode] = summary.Status.GetValueOrDefault(log.StatusCode) + 1;
            }
            if (details.HasFlag(Detail.ApiKeys))
            {
                var apiKey = log.GetApiKey(headers);
                if (apiKey != null)
                {
                    summary.ApiKeys ??= new();
                    summary.ApiKeys[apiKey] = summary.ApiKeys.GetValueOrDefault(apiKey) + 1;
                }
            }
            if (details.HasFlag(Detail.Ips))
            {
                if (log.IpAddress != null)
                {
                    summary.Ips ??= new();
                    summary.Ips[log.IpAddress] = summary.Ips.GetValueOrDefault(log.IpAddress) + 1;
                }
            }
            if (details.HasFlag(Detail.Durations))
            {
                summary.Durations ??= new();
                AddDurations(summary.Durations, (int)log.RequestDuration.TotalMilliseconds);
            }
        }

        void AddDurations(Dictionary<string, long> durations, int totalMs)
        {
            var added = false;
            foreach (var range in config.DurationRanges)
            {
                if (totalMs < range)
                {
                    durations[range.ToString()] = durations.TryGetValue(range.ToString(), out var duration)
                        ? duration + 1
                        : 1;
                    added = true;
                    break;
                }
            }
            if (!added)
            {
                var lastRange = ">" + config.DurationRanges.Last();
                durations[lastRange] = durations.TryGetValue(lastRange, out var duration)
                    ? duration + 1
                    : 1;
            }
        }
        
        var headers = new Dictionary<string, string>(requestLog.Headers ?? new(), StringComparer.OrdinalIgnoreCase);

        Add(ret.Apis, requestLog.Request ?? requestLog.OperationName, requestLog);
        if (requestLog.StatusCode > 0)
        {
            var apiLog = ret.Apis.GetValueOrDefault(requestLog.Request ?? requestLog.OperationName);
            if (apiLog != null)
            {
                AddDetail(apiLog, requestLog, headers, Detail.Status | Detail.Users | Detail.ApiKeys | Detail.Ips | Detail.Durations);
            }
        }

        if (requestLog.UserAuthId != null)
        {
            Add(ret.Users, requestLog.UserAuthId, requestLog);

            var userLog = ret.Users.GetValueOrDefault(requestLog.UserAuthId);
            if (userLog != null)
            {
                if (requestLog.Meta?.TryGetValue("username", out var username) == true)
                {
                    userLog.Name = username;
                }
                AddDetail(userLog, requestLog, headers, Detail.Status | Detail.Apis | Detail.ApiKeys | Detail.Ips | Detail.Durations);
            }
        }

        if (requestLog.StatusCode > 0)
        {
            Add(ret.Status, requestLog.StatusCode.ToString(), requestLog);
        }

        Add(ret.Days, requestLog.DateTime.Day.ToString(), requestLog);

        var apiKey = requestLog.GetApiKey(headers);
        if (apiKey != null)
        {
            Add(ret.ApiKeys, apiKey, requestLog);

            var apiKeyLog = ret.ApiKeys.GetValueOrDefault(apiKey);
            if (apiKeyLog != null)
            {
                if (requestLog.Meta?.TryGetValue("keyname", out var apiKeyName) == true)
                {
                    apiKeyLog.Name = apiKeyName;
                }
                AddDetail(apiKeyLog, requestLog, headers, Detail.Status | Detail.Apis | Detail.Users | Detail.Ips | Detail.Durations);
            }
        }

        if (requestLog.IpAddress != null)
        {
            Add(ret.Ips, requestLog.IpAddress, requestLog);
            var ipLog = ret.Ips.GetValueOrDefault(requestLog.IpAddress);
            if (ipLog != null)
            {
                AddDetail(ipLog, requestLog, headers, Detail.Status | Detail.Apis | Detail.Users | Detail.ApiKeys | Detail.Durations);
            }
        }

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

        if (requestLog.StatusCode is >= 200 and < 300)
        {
            AddDurations(ret.Durations, (int)requestLog.RequestDuration.TotalMilliseconds);
        }
    }

    public static string? GetApiKey(this RequestLog log, Dictionary<string, string>? headers= null)
    {
        if (log.Meta?.TryGetValue("apikey", out var authorization) == true)
            return authorization;
        headers ??= new Dictionary<string, string>(log.Headers ?? new(), StringComparer.OrdinalIgnoreCase);
        if (headers.TryGetValue(HttpHeaders.Authorization, out authorization) 
            && authorization.StartsWith("Bearer ak-", StringComparison.OrdinalIgnoreCase))
        {
            return authorization.RightPart(' ');
        }
        return null;
    }

    public static void CleanResults(this AnalyticsReports ret, AnalyticsConfig config)
    {
        void AddSummary(Dictionary<string, RequestSummary> results, string name, RequestSummary apiSummary)
        {
            var summary = results.TryGetValue(name, out var existing)
                ? existing
                : results[name] = new();
            summary.TotalRequests += apiSummary.TotalRequests;
            summary.TotalDuration += apiSummary.TotalDuration;
            summary.TotalRequestLength += apiSummary.TotalRequestLength;
        }

        var metadata = HostContext.Metadata;
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

        HashSet<string> GetTopKeys(Dictionary<string, long> results, int take) => results
            .OrderByDescending(kv => kv.Value)
            .Take(take)
            .Select(kv => kv.Key)
            .ToSet();
        
        void KeepOnly<V>(Dictionary<string, V> results, HashSet<string> keys)
        {
            foreach (var key in results.Keys)
            {
                if (keys.Contains(key))
                    continue;
                results.Remove(key);
            }
        }
    
        void Clean(Dictionary<string, RequestSummary> results)
        {
            foreach (var entry in results.Values)
            {
                entry.TotalDuration = Math.Floor(entry.TotalDuration);
                if (entry.Apis?.Count > config.DetailLimit)
                {
                    KeepOnly(entry.Apis, GetTopKeys(entry.Apis, config.DetailLimit));
                }
                if (entry.Users?.Count > config.DetailLimit)
                {
                    KeepOnly(entry.Users, GetTopKeys(entry.Users, config.DetailLimit));
                }
                if (entry.Ips?.Count > config.DetailLimit)
                {
                    KeepOnly(entry.Ips, GetTopKeys(entry.Ips, config.DetailLimit));
                }
                if (entry.ApiKeys?.Count > config.DetailLimit)
                {
                    KeepOnly(entry.ApiKeys, GetTopKeys(entry.ApiKeys, config.DetailLimit));
                }
            }
            if (results.Count > config.SummaryLimit)
            {
                var topKeys = results
                    .OrderByDescending(kv => kv.Value.TotalRequests)
                    .Take(config.SummaryLimit)
                    .Select(x => x.Key)
                    .ToSet();
                KeepOnly(results, topKeys);
            }
        }
    
        Clean(ret.Apis);
        Clean(ret.Users);
        Clean(ret.Tags);
        Clean(ret.Status);
        Clean(ret.Days);
        Clean(ret.ApiKeys);
        Clean(ret.Ips);
        Clean(ret.Browsers);
        Clean(ret.Devices);
        Clean(ret.Bots);
    }
}