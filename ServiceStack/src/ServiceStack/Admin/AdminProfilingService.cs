#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Admin;

[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminProfiling : IReturn<AdminProfilingResponse>
{
    public string? Source { get; set; }
    public string? EventType { get; set; }
    public int? ThreadId { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
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
    public string? ExternalTraceUrl { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[DefaultRequest(typeof(AdminProfiling))]
public class AdminProfilingService : Service
{
    private async Task<ProfilingFeature> AssertRequiredRole()
    {
        var feature = AssertPlugin<ProfilingFeature>();
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AccessRole);
        return feature;
    }
    
    public async Task<object> Any(AdminProfiling request)
    {
        var feature = await AssertRequiredRole().ConfigAwait();
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
        if (!request.SpanId.IsNullOrEmpty())
            logs = logs.Where(x => x.SpanId == request.SpanId);
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

        var isTrace = !request.TraceId.IsNullOrEmpty();
        var query = string.IsNullOrEmpty(request.OrderBy)
            ? isTrace ? logs.OrderBy(x => x.Date) : logs.OrderByDescending(x => x.Id)
            : logs.OrderBy(request.OrderBy);

        // Traces are read in full, so return the whole trace unless a page size was requested
        var take = request.Take ?? (isTrace ? feature.Capacity : feature.DefaultLimit);
        var total = logs.Count();
        var results = query.Skip(System.Math.Max(0, request.Skip));
        results = results.Take(System.Math.Max(1, System.Math.Min(take, feature.Capacity)));
        
        return new AdminProfilingResponse
        {
            Results = results.ToList(),
            Total = total,
            ExternalTraceUrl = ProfilingFeature.GetExternalTraceUrl(feature.ExternalTraceUrlTemplate, request.TraceId),
        };
    }
}
