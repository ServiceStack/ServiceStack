#if NET8_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ServiceStack.Jobs;

/// <summary>
/// Thresholds that decide when Background Jobs is reported as Degraded or Unhealthy.
/// </summary>
public class JobsHealthCheckOptions
{
    /// <summary>Backlog size that reports Degraded, null to not check it</summary>
    public int? DegradedQueuedJobs { get; set; } = 1000;
    /// <summary>Backlog size that reports Unhealthy, null to not check it</summary>
    public int? UnhealthyQueuedJobs { get; set; } = 10_000;
    /// <summary>How long the oldest Job may be waiting before reporting Degraded</summary>
    public TimeSpan? DegradedWaitTime { get; set; } = TimeSpan.FromMinutes(5);
    /// <summary>How long the oldest Job may be waiting before reporting Unhealthy</summary>
    public TimeSpan? UnhealthyWaitTime { get; set; } = TimeSpan.FromMinutes(30);
    /// <summary>Report Unhealthy when no node has reported a heartbeat recently</summary>
    public bool CheckNodes { get; set; } = true;
}

/// <summary>
/// Reports Background Jobs health to ASP.NET's health checks, so a growing backlog or a queue that
/// stopped being processed shows up in the same place teams already watch.
/// Register with: services.AddHealthChecks().AddCheck&lt;JobsHealthCheck&gt;("background-jobs");
/// </summary>
public class JobsHealthCheck(IBackgroundJobs jobs, JobsHealthCheckOptions? options = null) : IHealthCheck
{
    private readonly JobsHealthCheckOptions options = options ?? new();

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (jobs is not IBackgroundJobsQueues queues)
                return Task.FromResult(HealthCheckResult.Healthy("Background Jobs does not report status"));

            var jobsStatus = queues.GetJobsStatus();
            var status = HealthStatus.Healthy;
            var reasons = new List<string>();
            var data = new Dictionary<string, object> {
                ["queued"] = jobsStatus.Queued,
                ["running"] = jobsStatus.Running,
                ["nodes"] = jobsStatus.Nodes,
                ["nodesAlive"] = jobsStatus.NodesAlive,
            };

            void Report(HealthStatus to, string reason)
            {
                if (to < status) // Unhealthy(0) < Degraded(1) < Healthy(2)
                    status = to;
                reasons.Add(reason);
            }

            if (options.UnhealthyQueuedJobs != null && jobsStatus.Queued >= options.UnhealthyQueuedJobs)
                Report(HealthStatus.Unhealthy, $"{jobsStatus.Queued} Jobs queued");
            else if (options.DegradedQueuedJobs != null && jobsStatus.Queued >= options.DegradedQueuedJobs)
                Report(HealthStatus.Degraded, $"{jobsStatus.Queued} Jobs queued");

            if (jobsStatus.OldestQueued is { } waiting)
            {
                data["oldestWaitingSecs"] = (int)waiting.TotalSeconds;
                if (options.UnhealthyWaitTime != null && waiting >= options.UnhealthyWaitTime)
                    Report(HealthStatus.Unhealthy, $"oldest Job waiting {waiting:g}");
                else if (options.DegradedWaitTime != null && waiting >= options.DegradedWaitTime)
                    Report(HealthStatus.Degraded, $"oldest Job waiting {waiting:g}");
            }

            // Only a problem when there's work waiting: an idle App with no live nodes is fine
            if (options.CheckNodes && jobsStatus.NodesAlive == 0 && jobsStatus.Nodes > 0
                && jobsStatus.Queued > 0)
            {
                Report(HealthStatus.Unhealthy, "no App Server is processing Jobs");
            }

            var description = reasons.Count > 0
                ? string.Join("; ", reasons)
                : "Background Jobs is processing normally";
            return Task.FromResult(new HealthCheckResult(status, description, data: data));
        }
        catch (Exception e)
        {
            return Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy,
                "Could not read Background Jobs state", e));
        }
    }
}
#endif
