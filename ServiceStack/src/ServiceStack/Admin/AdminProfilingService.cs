#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Admin;

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminProfiling : IReturn<AdminProfilingResponse>
{
    public string? Source { get; set; }
    public string? EventType { get; set; }
    public int? ThreadId { get; set; }
    public string? TraceId { get; set; }
    public string? UserAuthId { get; set; }
    public string? SessionId { get; set; }
    public string? Tag { get; set; }
    public int Skip { get; set; }
    public int? Take { get; set; }
    public string? OrderBy { get; set; }
    public bool? WithErrors { get; set; }
    public bool? Pending { get; set; }
}

public class AdminProfilingResponse
{
    public List<DiagnosticEntry> Results { get; set; }
    public int Total { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[DefaultRequest(typeof(AdminProfiling))]
public class AdminProfilingService : Service
{
    public async Task<object> Any(AdminProfiling request)
    {
        var feature = HostContext.AppHost.AssertPlugin<ProfilingFeature>();

        if (!HostContext.DebugMode)
            await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AccessRole);

        var snapshot = request.Pending != true 
            ? feature.Observer.GetLatestEntries(null)
            : feature.Observer.GetPendingEntries(null);
        
        var logs = snapshot.AsQueryable();
        
        if (!request.Source.IsNullOrEmpty())
            logs = logs.Where(x => x.Source == request.Source);
        if (!request.EventType.IsNullOrEmpty())
            logs = logs.Where(x => x.EventType == request.EventType);
        if (!request.TraceId.IsNullOrEmpty())
            logs = logs.Where(x => x.TraceId == request.TraceId);
        if (request.ThreadId != null)
            logs = logs.Where(x => x.ThreadId == request.ThreadId.Value);
        if (!request.UserAuthId.IsNullOrEmpty())
            logs = logs.Where(x => x.UserAuthId == request.UserAuthId);
        if (!request.SessionId.IsNullOrEmpty())
            logs = logs.Where(x => x.SessionId == request.SessionId);
        if (!request.Tag.IsNullOrEmpty())
            logs = logs.Where(x => x.Tag == request.Tag);
        if (request.WithErrors.HasValue)
            logs = request.WithErrors.Value
                ? logs.Where(x => x.Error != null)
                : logs.Where(x => x.Error == null);

        var query = string.IsNullOrEmpty(request.OrderBy)
            ? logs.OrderByDescending(x => x.Id)
            : logs.OrderBy(request.OrderBy);

        var results = query.Skip(request.Skip);
        results = results.Take(request.Take.GetValueOrDefault(feature.DefaultLimit));
        
        return new AdminProfilingResponse
        {
            Results = results.ToList(),
            Total = snapshot.Count,
        };
    }
}