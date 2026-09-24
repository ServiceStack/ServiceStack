using System.Collections.Concurrent;
using System.Data;
using Microsoft.Extensions.Logging;
using ServiceStack.Cronos;
using ServiceStack.OrmLite;

namespace ServiceStack.Jobs;

public partial class BackgroundJobs
{
    private ConcurrentDictionary<string, ScheduledTask> namedScheduledTasks = new();
    private ConcurrentDictionary<string, CronExpression> cronExpressions = new();

    public ICollection<ScheduledTask> ScheduledTasks => namedScheduledTasks.Values;
    public ICollection<CronExpression> CronExpressions => cronExpressions.Values;

    private DateTime lastScheduledTasksLoad = DateTime.MinValue;

    /// <summary>
    /// On Startup load all scheduled tasks into memory
    /// </summary>
    void LoadScheduledTasks() => ReloadScheduledTasks();

    /// <summary>
    /// Reloads Scheduled Tasks from the database so that changes made on other nodes (or directly
    /// in the database) are picked up without needing to restart this node.
    /// </summary>
    public void ReloadScheduledTasks()
    {
        lastScheduledTasksLoad = DateTime.UtcNow;
        using var db = feature.OpenDb();
        var tasks = db.Select<ScheduledTask>();
        var loadedNames = new HashSet<string>();
        foreach (var task in tasks)
        {
            if (task.Name == null)
                continue;
            loadedNames.Add(task.Name);

            if (task.NextRun == null && task.Enabled)
            {
                task.NextRun = task.GetFirstRun(DateTime.UtcNow);
                db.UpdateOnly(() => new ScheduledTask { NextRun = task.NextRun }, x => x.Id == task.Id);
            }
            namedScheduledTasks[task.Name] = task;
            if (task.CronExpression != null)
            {
                try
                {
                    cronExpressions.TryAdd(task.CronExpression, CronExpression.Parse(task.CronExpression));
                }
                catch (Exception ex)
                {
                    task.LastErrorCode = ex.GetType().Name;
                    task.LastErrorMessage = ex.Message;
                    db.UpdateOnly(() => new ScheduledTask
                    {
                        LastErrorCode = task.LastErrorCode,
                        LastErrorMessage = task.LastErrorMessage,
                    }, x => x.Id == task.Id);
                }
            }
        }

        // Drop tasks that were deleted on another node
        foreach (var name in namedScheduledTasks.Keys)
        {
            if (!loadedNames.Contains(name))
                namedScheduledTasks.TryRemove(name, out _);
        }
    }

    private void ReloadScheduledTasksIfDue()
    {
        if (DateTime.UtcNow - lastScheduledTasksLoad < TimeSpan.FromSeconds(feature.ReloadScheduledTasksSecs))
            return;
        try
        {
            ReloadScheduledTasks();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "JOBS Error reloading Scheduled Tasks");
        }
    }

    private void CreateOrUpdate(ScheduledTask task)
    {
        ValidateSchedule(task);
        var now = DateTime.UtcNow;
        task.Enabled = true;
        task.NextRun = task.GetFirstRun(now);
        task.LastErrorCode = null;
        task.LastErrorMessage = null;
        task.CreatedDate ??= now;
        task.ModifiedDate = now;

        using var db = feature.OpenDb();
        lock (db.GetWriteLock())
        {
            var updated = db.UpdateOnly(() => new ScheduledTask
            {
                Interval = task.Interval,
                CronExpression = task.CronExpression,
                RequestType = task.RequestType,
                Command = task.Command,
                Request = task.Request,
                RequestBody = task.RequestBody,
                Options = task.Options,
                Enabled = task.Enabled,
                NextRun = task.NextRun,
                TimeZoneId = task.TimeZoneId,
                MisfirePolicy = task.MisfirePolicy,
                OverlapPolicy = task.OverlapPolicy,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                MaxRuns = task.MaxRuns,
                LastErrorCode = null,
                LastErrorMessage = null,
                ModifiedDate = task.ModifiedDate,
            }, where: x => x.Name == task.Name);
            if (updated == 0)
            {
                task.Id = db.Insert(task, selectIdentity: true);
            }
            else
            {
                // Registered on every startup, so keep the progress the task has already made
                var current = db.Single<ScheduledTask>(x => x.Name == task.Name);
                if (current != null)
                {
                    task.Id = current.Id;
                    task.RunCount = current.RunCount;
                    task.LastRun = current.LastRun;
                    task.LastJobId = current.LastJobId;
                }
            }
        }
        namedScheduledTasks[task.Name!] = task;
    }

    public void RecurringCommand(string taskName, Schedule schedule, string commandName, object arg,
        BackgroundJobOptions? options = null)
    {
        var task = namedScheduledTasks.GetOrAdd(taskName, _ => new ScheduledTask { Name = taskName });
        var (interval, cronExpression) = schedule;
        task.Interval = interval;
        task.CronExpression = cronExpression;
        task.TimeZoneId = schedule.TimeZoneId;
        task.MisfirePolicy = schedule.MisfirePolicy;
        task.OverlapPolicy = schedule.OverlapPolicy;
        task.StartDate = schedule.StartDate;
        task.EndDate = schedule.EndDate;
        task.MaxRuns = schedule.MaxRuns;
        task.RequestType = CommandResult.Command;
        task.Command = commandName;
        task.Request = arg.GetType().Name;
        task.RequestBody = ClientConfig.ToJson(arg);
        task.Options = options;

        CreateRequestForCommand(task.Command!, task.Request, task.RequestBody); // Ensure Request DTO can be recreated
        CreateOrUpdate(task);
    }

    public void RecurringApi(string taskName, Schedule schedule, object requestDto,
        BackgroundJobOptions? options = null)
    {
        var task = namedScheduledTasks.GetOrAdd(taskName, _ => new ScheduledTask { Name = taskName });
        var (interval, cronExpression) = schedule;
        task.Interval = interval;
        task.CronExpression = cronExpression;
        task.TimeZoneId = schedule.TimeZoneId;
        task.MisfirePolicy = schedule.MisfirePolicy;
        task.OverlapPolicy = schedule.OverlapPolicy;
        task.StartDate = schedule.StartDate;
        task.EndDate = schedule.EndDate;
        task.MaxRuns = schedule.MaxRuns;
        task.RequestType = CommandResult.Api;
        task.Command = null;
        task.Request = requestDto.GetType().Name;
        task.RequestBody = ClientConfig.ToJson(requestDto);
        task.Options = options;

        if (feature.AppHost.Metadata.GetServiceTypeByRequest(requestDto.GetType()) == null)
            throw new NotSupportedException("Service not found for request type: " + requestDto.GetType().Name);
        CreateRequestForApi(task.Request, task.RequestBody); // Ensure Request DTO can be recreated
        CreateOrUpdate(task);
    }

    public void DeleteRecurringTask(string taskName)
    {
        namedScheduledTasks.Remove(taskName, out _);
        using var db = OpenDb();
        lock (db.GetWriteLock())
        {
            db.Delete<ScheduledTask>(x => x.Name == taskName);
        }
    }

    /// <summary>
    /// Pauses or resumes a recurring task. Updated by Id so that it also works for tasks that
    /// aren't registered on this node.
    /// </summary>
    public bool SetRecurringTaskEnabled(long taskId, bool enabled)
    {
        var now = DateTime.UtcNow;
        using var db = OpenDb();
        var existing = db.SingleById<ScheduledTask>(taskId);
        if (existing == null)
            return false;
        DateTime? nextRun = enabled ? existing.GetFirstRun(now) : null;
        int updated;
        lock (db.GetWriteLock())
        {
            updated = db.UpdateOnly(() => new ScheduledTask
            {
                Enabled = enabled,
                NextRun = nextRun,
                ModifiedDate = now,
                LastErrorCode = null,
                LastErrorMessage = null,
            }, x => x.Id == taskId);
        }
        if (updated == 0)
            return false;

        RefreshScheduledTask(db, taskId);
        return true;
    }

    /// <summary>Makes an enabled recurring task due immediately, without changing its schedule.</summary>
    public bool RunRecurringTaskNow(long taskId)
    {
        var now = DateTime.UtcNow;
        using var db = OpenDb();
        int updated;
        lock (db.GetWriteLock())
        {
            updated = db.UpdateOnly(() => new ScheduledTask { NextRun = now.ToScheduleTime() },
                x => x.Id == taskId && x.Enabled);
        }
        if (updated == 0)
            return false;

        RefreshScheduledTask(db, taskId);
        return true;
    }

    private void RefreshScheduledTask(IDbConnection db, long taskId)
    {
        var current = db.SingleById<ScheduledTask>(taskId);
        if (current?.Name != null)
            namedScheduledTasks[current.Name] = current;
    }

    void ExecuteDueScheduledTasks()
    {
        var now = DateTime.UtcNow;
        foreach (var task in namedScheduledTasks.Values.ToList())
        {
            if (!task.Enabled || task.NextRun == null || task.NextRun > now)
                continue;

            try
            {
                ExecuteScheduledOccurrence(task, now);
            }
            catch (Exception ex)
            {
                RecordScheduledTaskError(task, ex);
            }
        }
    }

    private void ExecuteScheduledOccurrence(ScheduledTask task, DateTime now)
    {
        var occurrence = DateTime.SpecifyKind(task.NextRun!.Value, DateTimeKind.Utc);
        if (task.HasEnded(now))
        {
            // Disabled instead of run once its EndDate or MaxRuns is reached
            AdvanceScheduledTask(task, occurrence, null, null, executed: false);
            return;
        }
        var nextAfterOccurrence = GetNextRun(task, occurrence);
        var nextRun = GetNextRun(task, now);

        // Skip only when at least one complete additional occurrence was missed. This avoids
        // treating normal polling latency as a misfire.
        if (task.MisfirePolicy == ScheduleMisfirePolicy.Skip && nextAfterOccurrence <= now)
        {
            AdvanceScheduledTask(task, occurrence, nextRun, null, executed: false);
            return;
        }

        BackgroundJobRef? jobRef = null;
        if (task.RequestType == CommandResult.Command)
        {
            var request = CreateRequestForCommand(task.Command!, task.Request, task.RequestBody);
            if (task.Options?.RunCommand == true)
            {
                // RunCommand is intentionally transient. SQLite is single-host, so it doesn't
                // need the durable occurrence de-duplication used by queued jobs.
                RunCommand(task.Command!, request, task.Options);
            }
            else
            {
                jobRef = EnqueueCommand(task.Command!, request, GetOccurrenceOptions(task, occurrence));
            }
        }
        else if (task.RequestType == CommandResult.Api)
        {
            var request = CreateRequestForApi(task.Request, task.RequestBody);
            jobRef = EnqueueApi(request, GetOccurrenceOptions(task, occurrence));
        }
        else throw new NotSupportedException("Unsupported RequestType: " + task.RequestType);

        AdvanceScheduledTask(task, occurrence, nextRun, jobRef?.Id, executed: true);
    }

    private BackgroundJobOptions GetOccurrenceOptions(ScheduledTask task, DateTime occurrence)
    {
        var options = task.Options.Copy();
        options.RefId = JobUtils.CreateScheduledRefId(task.Id, occurrence);
        options.DuplicateRefIdBehavior = DuplicateRefIdBehavior.ReturnExisting;
        // Enforced by a unique index, so an occurrence can't overlap the previous run even when
        // 2 nodes reach the same occurrence at the same time. The existing Job is returned instead.
        if (task.OverlapPolicy == ScheduleOverlapPolicy.Skip)
            options.SingletonKey = JobUtils.CreateScheduledSingletonKey(task.Id);
        return options;
    }

    /// <summary>
    /// Records the outcome of the Job a Scheduled Task last enqueued so the Admin UI doesn't need
    /// to join to JobSummary for every task.
    /// </summary>
    private void UpdateScheduledTaskRun(BackgroundJobBase job)
    {
        if (!JobUtils.TryGetScheduledTaskId(job.RefId, out var taskId))
            return;
        try
        {
            var state = job.State;
            var durationMs = job.DurationMs;
            using var db = feature.OpenDb();
            // Job Ids are monotonic, so this records the run without letting a late completion
            // overwrite a newer one. An exact LastJobId match would miss a Job that completed
            // before its occurrence was recorded.
            lock (db.GetWriteLock())
            {
                db.UpdateOnly(() => new ScheduledTask { LastRunState = state, LastRunDurationMs = durationMs },
                    where: x => x.Id == taskId && (x.LastJobId == null || x.LastJobId <= job.Id));
            }
            foreach (var task in namedScheduledTasks.Values)
            {
                if (task.Id == taskId && (task.LastJobId == null || task.LastJobId <= job.Id))
                {
                    task.LastRunState = state;
                    task.LastRunDurationMs = durationMs;
                }
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "JOBS Error recording run of Scheduled Task {TaskId}", taskId);
        }
    }

    private DateTime? GetNextRun(ScheduledTask task, DateTime fromUtc)
    {
        fromUtc = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
        if (task.Interval != null)
            return fromUtc.Add(task.Interval.Value).ToScheduleTime();
        if (task.CronExpression == null)
            return null;

        var cron = cronExpressions.GetOrAdd(task.CronExpression, CronExpression.Parse);
        return cron.GetNextOccurrence(fromUtc, GetTimeZone(task.TimeZoneId))?.ToScheduleTime();
    }

    private static readonly ConcurrentDictionary<string, TimeZoneInfo> timeZones = new();

    private static TimeZoneInfo GetTimeZone(string? timeZoneId) => timeZoneId == null
        ? TimeZoneInfo.Utc
        : timeZones.GetOrAdd(timeZoneId, TimeZoneInfo.FindSystemTimeZoneById);

    private static void ValidateSchedule(ScheduledTask task)
    {
        if (task.Interval is { } interval && interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(task.Interval), "Schedule interval must be greater than zero");
        if (task.Interval == null && task.CronExpression == null)
            throw new ArgumentException("An interval or cron expression is required");
        if (task.CronExpression != null)
            CronExpression.Parse(task.CronExpression);
        if (task.TimeZoneId != null)
            GetTimeZone(task.TimeZoneId);
    }

    private void AdvanceScheduledTask(ScheduledTask task, DateTime occurrence, DateTime? nextRun,
        long? jobId, bool executed)
    {
        var lastRun = executed ? DateTime.UtcNow : task.LastRun;
        var runCount = executed ? task.RunCount + 1 : task.RunCount;
        // A task that reached its EndDate or MaxRuns is disabled rather than scheduled again
        var ended = nextRun == null || (task.MaxRuns != null && runCount >= task.MaxRuns)
            || (task.EndDate != null && nextRun > task.EndDate);
        if (ended)
            nextRun = null;
        var enabled = !ended;
        using var db = feature.OpenDb();
        lock (db.GetWriteLock())
        {
            var updated = db.UpdateOnly(() => new ScheduledTask
            {
                LastRun = lastRun,
                LastJobId = executed && jobId != null ? jobId : task.LastJobId,
                NextRun = nextRun,
                RunCount = runCount,
                Enabled = enabled,
                LastErrorCode = null,
                LastErrorMessage = null,
                ModifiedDate = DateTime.UtcNow,
            }, where: x => x.Id == task.Id && x.Enabled && x.NextRun == occurrence);
            if (updated == 0)
            {
                var current = db.SingleById<ScheduledTask>(task.Id);
                if (current != null && current.Name != null)
                    namedScheduledTasks[current.Name] = current;
                return;
            }
        }

        task.LastRun = lastRun;
        if (executed && jobId != null)
            task.LastJobId = jobId;
        task.NextRun = nextRun;
        task.RunCount = runCount;
        task.Enabled = enabled;
        task.LastErrorCode = null;
        task.LastErrorMessage = null;
    }

    private void RecordScheduledTaskError(ScheduledTask task, Exception ex)
    {
        task.LastErrorCode = ex.GetType().Name;
        task.LastErrorMessage = ex.Message;
        using var db = feature.OpenDb();
        lock (db.GetWriteLock())
        {
            db.UpdateOnly(() => new ScheduledTask
            {
                LastErrorCode = task.LastErrorCode,
                LastErrorMessage = task.LastErrorMessage,
            }, x => x.Id == task.Id);
        }
    }

    public void ClearScheduledTasks()
    {
        namedScheduledTasks.Clear();
        cronExpressions.Clear();
    }
}
