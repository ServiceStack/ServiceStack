using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.OrmLite;
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
    IRequireRegistration, IConfigureServices
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
    public int MaxLimit { get; set; } = 5000;
    public bool AutoInitSchema { get; set; } = true;
    public IDbConnectionFactory DbFactory { get; set; } = null!;
    public IAppHostNetCore AppHost { get; set; } = null!;

    public SqliteRequestLogger()
    {
        ResolveMonthDb = DefaultResolveMonthDb;
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

    public override List<RequestLogEntry> GetLatestLogs(int? take)
    {
        using var db = OpenMonthDb(DateTime.UtcNow);

        var dbLogs = db.Select(db.From<RequestLog>()
            .Where(x => x.DateTime >= DateTime.UtcNow.Date)
            .Take(take ?? MaxLimit)
            .OrderByDescending(x => x.Id));
        
        var to = dbLogs.Map(ToRequestLogEntry);
        return to;
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
            OperationName = from.OperationName,
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
            OperationName = from.OperationName,
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
}
