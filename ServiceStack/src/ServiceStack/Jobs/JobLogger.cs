#if NET8_0_OR_GREATER
#nullable enable

using System;
using Microsoft.Extensions.Logging;

namespace ServiceStack.Jobs;

public struct JobLogger(IBackgroundJobs jobs, BackgroundJob job, ILogger? logger=null) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => logger?.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => logger?.IsEnabled(logLevel) == true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        logger?.Log(logLevel, eventId, state, exception, formatter);
        var message = state?.ToString();
        if (message != null)
        {
            jobs.UpdateJobStatus(new(job, progress:null, status:null, log:message));
        }
    }

    public void UpdateProgress(double progress) => jobs.UpdateJobStatus(new(job, progress));
    public void UpdateStatus(double? progress = null, string? status = null, string? log = null) =>
        jobs.UpdateJobStatus(new(job, progress, status, log));
    public void UpdateStatus(string? status = null, string? log = null) =>
        jobs.UpdateJobStatus(new(job, progress:null, status, log));
    public void UpdateLog(string log) => jobs.UpdateJobStatus(new(job, progress:null, null, log));
}

#endif
