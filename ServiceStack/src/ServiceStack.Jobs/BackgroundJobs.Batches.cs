using System.Data;
using Microsoft.Extensions.Logging;
using ServiceStack.OrmLite;

namespace ServiceStack.Jobs;

/// <summary>
/// SQLite specifics of the Job Batch, Queue and node registry features shared with the RDBMS
/// provider in BackgroundJobsProviderBase
/// </summary>
public partial class BackgroundJobs
{
    protected override IBackgroundJobsOptions Options => feature;
    protected override CancellationToken JobsToken => ct;
    protected override List<DateTime> GetArchiveMonths(IDbConnection db) => feature.GetTableMonths(db);
    protected override void DropArchive(DateTime month) => feature.DeleteMonthDb(month);

    /// <summary>SQLite allows a single writer, so writes take the database's write lock</summary>
    protected override T Write<T>(IDbConnection db, Func<T> fn)
    {
        lock (db.GetWriteLock())
        {
            return fn();
        }
    }

    protected override int GetConcurrencySlotSecs(BackgroundJob job) =>
        Math.Max(feature.DefaultTimeoutSecs, job.TimeoutSecs ?? feature.DefaultTimeoutSecs);

    /// <summary>
    /// Takes a Job's ConcurrencyKey slot before dispatching it. A Job that can't get the slot is
    /// returned to the queue unclaimed, so a later tick picks it up once the key is free.
    /// </summary>
    private bool TryClaimConcurrencySlot(BackgroundJob job)
    {
        if (job.ConcurrencyKey == null)
            return true;
        try
        {
            using var db = feature.OpenDb();
            if (TryAcquireConcurrencySlot(db, job, DateTime.UtcNow))
                return true;

            var jobId = job.Id;
            Write(db, () => db.UpdateOnly(() => new BackgroundJob { RequestId = null }, where: x => x.Id == jobId));
            job.RequestId = null;
            return false;
        }
        catch (Exception e)
        {
            log.LogError(e, "JOBS Error claiming concurrency slot for Job {Id}", job.Id);
            return false;
        }
    }
}
