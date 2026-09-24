using System.Collections.Concurrent;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace MyApp.ServiceInterface;

/// <summary>
/// Examples of popular Background MQ use-cases. Each API queues simulated Jobs from
/// BackgroundJobCommands.cs that you can watch run in the Admin UI at /admin-ui/backgroundjobs.
/// </summary>
public class BackgroundJobServices(IBackgroundJobs jobs) : Service
{
    public object Any(QueueCheckUrl request)
    {
        var options = new BackgroundJobOptions().PopulateWith(request);
        var jobRef = jobs.EnqueueCommand<CheckUrlsCommand>(new CheckUrls { Urls = [request.Url] }, options);
        return new QueueCheckUrlResponse
        {
            Id = jobRef.Id,
            RefId = jobRef.RefId,
        };
    }

    static QueuedJob ToQueuedJob(BackgroundJobRef jobRef, string name) =>
        new() { Id = jobRef.Id, RefId = jobRef.RefId, Name = name };

    /// <summary>
    /// Fire-and-forget: the API returns immediately while the email is sent in the background,
    /// retried with exponential backoff and jitter if the email provider fails
    /// </summary>
    public object Post(QueueWelcomeEmail request)
    {
        var jobRef = jobs.EnqueueCommand<SendWelcomeEmailCommand>(new SendWelcomeEmail {
            Email = request.Email,
            FailAttempts = request.FailAttempts,
        }, new() {
            Queue = JobQueueNames.Emails,
            Tag = "welcome-emails",
            RetryLimit = request.RetryLimit,
            RetryBackoff = RetryBackoff.ExponentialJitter,
            RetryDelay = TimeSpan.FromSeconds(2),
            MaxRetryDelay = TimeSpan.FromSeconds(30),
        });
        return new QueueJobsResponse {
            Jobs = [ToQueuedJob(jobRef, "Send welcome email")],
            Message = "Each failed attempt is listed in the Job's attempt history",
        };
    }

    /// <summary>
    /// A long-running Job that reports its progress, status and logs, and stops when it's
    /// cancelled from the Admin UI or runs past its timeout
    /// </summary>
    public object Post(QueueImportProducts request)
    {
        var jobRef = jobs.EnqueueCommand<ImportProductsCommand>(new ImportProductsArgs {
            Products = request.Products,
            MsPerProduct = request.MsPerProduct,
        }, new() {
            Queue = JobQueueNames.Imports,
            TimeoutSecs = request.TimeoutSecs,
            CreatedBy = GetSession().UserAuthName,
        });
        return new QueueJobsResponse { Jobs = [ToQueuedJob(jobRef, "Import products")] };
    }

    /// <summary>
    /// A delayed Job that won't run late: it's cancelled with the JobExpired error code if it hasn't
    /// started within ExpiresIn. Its result can be POSTed to a ReplyTo URL when it completes.
    /// </summary>
    public object Post(QueueScheduledReport request)
    {
        var jobRef = jobs.ScheduleCommand<GenerateReportCommand>(new GenerateReport {
            ReportName = request.ReportName,
        }, TimeSpan.FromSeconds(request.DelaySecs), new() {
            Queue = JobQueueNames.Reports,
            ExpiresIn = request.ExpiresInSecs != null ? TimeSpan.FromSeconds(request.ExpiresInSecs.Value) : null,
            ReplyTo = request.UseWebhook
                ? Request.GetBaseUrl().CombineWith("/api", nameof(JobResultWebhook))
                : null,
        });
        return new QueueJobsResponse {
            Jobs = [ToQueuedJob(jobRef, $"Generate {request.ReportName} report")],
            Message = request.UseWebhook ? $"Its result will be listed by the {nameof(GetReceivedWebhooks)} API" : null,
        };
    }

    /// <summary>
    /// A workflow where each step only runs after the previous one succeeded. A failed step cancels
    /// the steps after it, while the customer is notified however shipping ends.
    /// </summary>
    public object Post(QueueOrderFulfillment request)
    {
        OrderStep Step(string name) => new() {
            OrderId = request.OrderId,
            Amount = request.Amount,
            Fail = request.FailStep == name,
        };
        // A shared Tag groups every Job of this order in the Admin UI
        var tag = $"order-{request.OrderId}";

        var charge = jobs.EnqueueCommand<ChargePaymentCommand>(Step("charge"), new() {
            Queue = JobQueueNames.Orders,
            Tag = tag,
            RetryLimit = 0,
        });
        var reserve = jobs.EnqueueCommand<ReserveInventoryCommand>(Step("reserve"), new() {
            DependsOn = charge.Id,
            Tag = tag,
            RetryLimit = 0,
        });
        var ship = jobs.EnqueueCommand<ShipOrderCommand>(Step("ship"), new() {
            Queue = JobQueueNames.Orders,
            DependsOn = reserve.Id,
            Tag = tag,
            RetryLimit = 0,
            Callback = nameof(OrderShippedCallbackCommand),
        });
        var notify = jobs.EnqueueCommand<NotifyCustomerCommand>(Step("notify"), new() {
            Queue = JobQueueNames.Emails,
            DependsOn = ship.Id,
            DependsOnPolicy = JobDependencyPolicy.OnFinished,
            Tag = tag,
        });

        return new QueueJobsResponse {
            Jobs = [
                ToQueuedJob(charge, "1. Charge payment"),
                ToQueuedJob(reserve, "2. Reserve inventory"),
                ToQueuedJob(ship, "3. Ship order"),
                ToQueuedJob(notify, "4. Notify customer"),
            ],
        };
    }

    /// <summary>
    /// Fan-out and fan-in: images are resized in parallel as a Batch whose progress is tracked in
    /// the Admin UI, then zipped once every image has finished
    /// </summary>
    public object Post(QueueResizeImages request)
    {
        var batchId = $"gallery-{Guid.NewGuid().ToString("N")[..8]}";
        // Registering the Total up-front is what lets the batch know when it's complete
        jobs.CreateJobBatch(batchId,
            total: request.Images,
            callback: nameof(ImagesBatchFinishedCommand),
            onSuccess: nameof(PublishGalleryCommand),
            description: $"Resize {request.Images} images");

        var to = new QueueJobsResponse { BatchId = batchId };
        for (var i = 1; i <= request.Images; i++)
        {
            var jobRef = jobs.EnqueueCommand<ResizeImageCommand>(new ResizeImage {
                ImageUrl = $"/uploads/photo-{i}.jpg",
                Width = 800,
                Fail = i <= request.FailImages,
            }, new() {
                Queue = JobQueueNames.Images,
                BatchId = batchId,
                RetryLimit = 0,
            });
            to.Jobs.Add(ToQueuedJob(jobRef, $"Resize photo-{i}.jpg"));
        }

        var zipRef = jobs.EnqueueCommand<CreateZipArchiveCommand>(new CreateZipArchive { BatchId = batchId },
            new() { DependsOnBatch = batchId });
        to.Jobs.Add(ToQueuedJob(zipRef, "Zip resized images"));
        return to;
    }

    /// <summary>
    /// Jobs sharing a ConcurrencyKey run one at a time, keeping each tenant's syncs in order
    /// while different tenants sync in parallel
    /// </summary>
    public object Post(QueueTenantSync request)
    {
        var to = new QueueJobsResponse();
        for (var i = 1; i <= request.JobsPerTenant; i++)
        {
            foreach (var tenant in request.Tenants)
            {
                var jobRef = jobs.EnqueueCommand<SyncTenantDataCommand>(new SyncTenantData {
                    Tenant = tenant,
                    Sequence = i,
                }, new() {
                    Queue = JobQueueNames.TenantSync,
                    ConcurrencyKey = $"tenant:{tenant}",
                    TenantId = tenant,
                });
                to.Jobs.Add(ToQueuedJob(jobRef, $"Sync {tenant} #{i}"));
            }
        }
        return to;
    }

    /// <summary>
    /// Two ways to avoid duplicate work. Call this API again while the Jobs are running to get the
    /// same Jobs back instead of new ones.
    /// </summary>
    public object Post(QueueDeduplicatedJobs request)
    {
        // SingletonKey: only one refresh of this cache can be queued or running at a time
        var refresh = jobs.EnqueueCommand<RefreshCacheCommand>(new RefreshCache { CacheName = request.CacheName },
            new() { SingletonKey = $"refresh-cache:{request.CacheName}" });

        // Idempotent RefId: an order is only ever charged once, however many times it's submitted
        var charge = jobs.EnqueueCommand<ChargePaymentCommand>(new OrderStep {
            OrderId = request.OrderId,
            Amount = 10,
        }, new() {
            Queue = JobQueueNames.Orders,
            RefId = $"charge-order-{request.OrderId}",
            DuplicateRefIdBehavior = DuplicateRefIdBehavior.ReturnExisting,
        });

        return new QueueJobsResponse {
            Jobs = [
                ToQueuedJob(refresh, $"Refresh {request.CacheName} cache"),
                ToQueuedJob(charge, $"Charge order {request.OrderId}"),
            ],
        };
    }

    /// <summary>
    /// A queue rate limited to what a third-party API allows, counted across every App Server.
    /// Higher Priority Jobs are started first.
    /// </summary>
    public object Post(QueueExternalApiCalls request)
    {
        jobs.SetJobQueueRateLimit(JobQueueNames.ThirdPartyApi, request.RateLimit,
            TimeSpan.FromSeconds(request.WindowSecs), modifiedBy: GetSession().UserAuthName);

        var to = new QueueJobsResponse {
            Message = $"{request.RateLimit} calls start every {request.WindowSecs}s, urgent calls first",
        };
        for (var i = 1; i <= request.Calls; i++)
        {
            var urgent = i % 3 == 0;
            var jobRef = jobs.EnqueueCommand<CallExternalApiCommand>(new CallExternalApi {
                CallNumber = i,
                Urgent = urgent,
            }, new() {
                Queue = JobQueueNames.ThirdPartyApi,
                Priority = urgent ? 10 : 0,
            });
            to.Jobs.Add(ToQueuedJob(jobRef, urgent ? $"Urgent call #{i}" : $"Call #{i}"));
        }
        return to;
    }

    /// <summary>
    /// Queue a Job then wait for its result, e.g. to offload work from a web server while keeping
    /// a synchronous API. A transient Command runs in-memory without being persisted.
    /// </summary>
    public async Task<object> Post(RunReportAndWait request)
    {
        var args = new GenerateReport { ReportName = request.ReportName };
        if (!request.Durable)
        {
            var transient = (ReportResult)(await jobs.RunCommandAsync<GenerateReportCommand>(args))!;
            return new RunReportAndWaitResponse {
                ReportName = transient.ReportName,
                Rows = transient.Rows,
                Url = transient.Url,
            };
        }

        var jobRef = jobs.EnqueueCommand<GenerateReportCommand>(args, new() { Queue = JobQueueNames.Reports });
        var result = await jobs.WaitForJobAsync(jobRef, TimeSpan.FromSeconds(request.TimeoutSecs),
            Request.GetCancellationToken());
        if (result.Summary.State != BackgroundJobState.Completed)
            throw new Exception($"Report {result.Summary.State}: {result.Summary.ErrorMessage}");

        var report = (ReportResult)jobs.CreateResponse(result)!;
        return new RunReportAndWaitResponse {
            JobId = jobRef.Id,
            ReportName = report.ReportName,
            Rows = report.Rows,
            Url = report.Url,
        };
    }

    const string CleanupTaskName = "Cleanup Temp Files";

    /// <summary>
    /// A recurring task that stops once it has run MaxRuns times. OverlapPolicy.Skip means an
    /// occurrence is skipped while the previous one is still running.
    /// </summary>
    public object Post(ScheduleRecurringCleanup request)
    {
        if (request.Delete)
        {
            jobs.DeleteRecurringTask(CleanupTaskName);
            return new QueueJobsResponse { Message = $"Deleted '{CleanupTaskName}'" };
        }

        var schedule = Schedule.Interval(TimeSpan.FromSeconds(request.IntervalSecs));
        schedule.MaxRuns = request.MaxRuns;
        schedule.OverlapPolicy = ScheduleOverlapPolicy.Skip;
        schedule.MisfirePolicy = ScheduleMisfirePolicy.Skip;
        jobs.RecurringCommand<CleanupTempFilesCommand>(CleanupTaskName, schedule,
            new CleanupTempFiles { Directory = "App_Data/tmp" });

        return new QueueJobsResponse {
            Message = $"'{CleanupTaskName}' runs every {request.IntervalSecs}s" +
                (request.MaxRuns != null ? $", {request.MaxRuns} times" : ""),
        };
    }

    /// <summary>
    /// Transactional outbox: the order's Jobs are written in the same transaction as the order,
    /// so they're only queued if it commits. Only supported by the RDBMS provider (DatabaseJobFeature).
    /// </summary>
    public object Post(QueuePlaceOrder request)
    {
        var order = new ProcessNewOrder {
            OrderRef = "ORD-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            Customer = request.Customer,
            Amount = request.Amount,
        };

        if (jobs is not IBackgroundJobsTransactional)
        {
            var jobRef = jobs.EnqueueCommand<ProcessNewOrderCommand>(order);
            return new QueueJobsResponse {
                Jobs = [ToQueuedJob(jobRef, $"Process {order.OrderRef}")],
                Message = "Transactional outbox requires the RDBMS provider, the Job was queued without a transaction",
            };
        }

        // The connection must be to the database the Jobs are stored in
        using var db = HostContext.AssertPlugin<DatabaseJobFeature>().OpenDb();
        using var trans = db.OpenTransaction();
        // ... save the order with db here ...
        var confirmation = jobs.EnqueueCommand<SendOrderConfirmationCommand>(db, order,
            new() { Queue = JobQueueNames.Emails });
        var process = jobs.EnqueueCommand<ProcessNewOrderCommand>(db, order,
            new() { Queue = JobQueueNames.Orders });

        if (request.RollbackTransaction)
        {
            // Disposing the transaction without committing rolls back the Jobs along with the order
            return new QueueJobsResponse {
                Message = $"Rolled back, Jobs {confirmation.Id} and {process.Id} were never queued",
            };
        }

        trans.Commit();
        return new QueueJobsResponse {
            Jobs = [
                ToQueuedJob(confirmation, $"Email confirmation of {order.OrderRef}"),
                ToQueuedJob(process, $"Process {order.OrderRef}"),
            ],
            Message = "Jobs queued in a transaction start on the next tick, once it has committed",
        };
    }

    static readonly ConcurrentQueue<ReceivedWebhook> ReceivedWebhooks = new();

    /// <summary>Receives the result of a Job queued with a ReplyTo URL</summary>
    public void Post(JobResultWebhook request)
    {
        ReceivedWebhooks.Enqueue(new ReceivedWebhook {
            JobId = Request.GetHeader("X-Job-Id"),
            RefId = Request.GetHeader("X-Job-RefId"),
            State = Request.GetHeader("X-Job-State"),
            ReportName = request.ReportName,
            Rows = request.Rows,
            ReceivedDate = DateTime.UtcNow,
        });
        while (ReceivedWebhooks.Count > 100)
            ReceivedWebhooks.TryDequeue(out _);
    }

    public object Get(GetReceivedWebhooks request) => new GetReceivedWebhooksResponse {
        Results = ReceivedWebhooks.Reverse().ToList(),
    };
}
