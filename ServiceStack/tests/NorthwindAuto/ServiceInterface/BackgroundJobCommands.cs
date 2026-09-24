using ServiceStack;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface;

/*
 * Simulated Commands queued by the Background Jobs examples in BackgroundJobServices.cs.
 * They don't do any real work, instead they report progress, status and logs so you can
 * watch them in the Admin UI at /admin-ui/backgroundjobs.
 */

public static class JobQueueNames
{
    public const string Emails = "emails";
    public const string Imports = "imports";
    public const string Reports = "reports";
    public const string Orders = "orders";
    public const string Images = "images";
    public const string TenantSync = "tenant-sync";
    public const string ThirdPartyApi = "third-party-api";
}

public static class SimulatedWork
{
    /// <summary>Simulates work in steps, reporting progress to the Admin UI after each one</summary>
    public static async Task RunAsync(JobLogger log, int steps, int msPerStep, string description,
        CancellationToken token)
    {
        for (var i = 1; i <= steps; i++)
        {
            // Throws when the Job is cancelled from the Admin UI or exceeds its TimeoutSecs
            await Task.Delay(msPerStep, token);
            log.UpdateStatus(progress: (double)i / steps, status: $"{description} {i}/{steps}");
        }
    }
}

// Fire-and-forget with retries

public class SendWelcomeEmail
{
    public string Email { get; set; } = null!;
    public int FailAttempts { get; set; }
}

public class SendWelcomeEmailCommand(ILogger<SendWelcomeEmailCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<SendWelcomeEmail>
{
    protected override async Task RunAsync(SendWelcomeEmail request, CancellationToken token)
    {
        var job = Request.GetBackgroundJob();
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Attempt {Attempt}: sending welcome email to {Email}", job.Attempts, request.Email);
        await Task.Delay(1000, token);

        // A thrown Exception fails this attempt. The Job is retried with the RetryBackoff it was queued
        // with until it succeeds or exceeds its RetryLimit, and each failure is kept in its attempt history.
        if (job.Attempts <= request.FailAttempts)
            throw new Exception($"SMTP server unavailable (simulated failure {job.Attempts} of {request.FailAttempts})");

        log.LogInformation("Welcome email sent to {Email}", request.Email);
    }
}

// Long-running with progress

public class ImportProductsArgs
{
    public int Products { get; set; }
    public int MsPerProduct { get; set; }
}
public class ImportProductsResult
{
    public int Imported { get; set; }
}

public class ImportProductsCommand(ILogger<ImportProductsCommand> logger, IBackgroundJobs jobs)
    : AsyncCommandWithResult<ImportProductsArgs, ImportProductsResult>
{
    protected override async Task<ImportProductsResult> RunAsync(ImportProductsArgs request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Importing {Count} products", request.Products);
        for (var i = 1; i <= request.Products; i++)
        {
            // Passing the token lets the Job be cancelled from the Admin UI mid-import
            await Task.Delay(request.MsPerProduct, token);
            log.UpdateStatus(progress: (double)i / request.Products, status: $"Imported {i}/{request.Products}");
            if (i % 10 == 0)
                log.LogInformation("Imported {Count} products", i);
        }
        return new ImportProductsResult { Imported = request.Products };
    }
}

// Delayed Jobs and ReplyTo

public class GenerateReport
{
    public string ReportName { get; set; } = null!;
}
public class ReportResult
{
    public string ReportName { get; set; } = null!;
    public int Rows { get; set; }
    public string Url { get; set; } = null!;
}

public class GenerateReportCommand(ILogger<GenerateReportCommand> logger, IBackgroundJobs jobs)
    : AsyncCommandWithResult<GenerateReport, ReportResult>
{
    protected override async Task<ReportResult> RunAsync(GenerateReport request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Generating {Report}", request.ReportName);
        await SimulatedWork.RunAsync(log, steps:5, msPerStep:500, "Aggregating", token);
        var rows = Random.Shared.Next(100, 1000);
        log.LogInformation("Generated {Report} with {Rows} rows", request.ReportName, rows);
        return new ReportResult {
            ReportName = request.ReportName,
            Rows = rows,
            Url = $"/reports/{request.ReportName.GenerateSlug()}.pdf",
        };
    }
}

// Workflow of dependent Jobs

public class OrderStep
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public bool Fail { get; set; }
}
public class ChargePaymentResult
{
    public string TransactionId { get; set; } = null!;
}
public class ReserveInventoryResult
{
    public string WarehouseId { get; set; } = null!;
}
public class ShipOrderResult
{
    public int OrderId { get; set; }
    public string TrackingNumber { get; set; } = null!;
}

public class ChargePaymentCommand(ILogger<ChargePaymentCommand> logger, IBackgroundJobs jobs)
    : AsyncCommandWithResult<OrderStep, ChargePaymentResult>
{
    protected override async Task<ChargePaymentResult> RunAsync(OrderStep request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Charging {Amount:C} for Order {OrderId}", request.Amount, request.OrderId);
        await SimulatedWork.RunAsync(log, steps:3, msPerStep:500, "Authorising", token);
        if (request.Fail)
            throw new Exception("Card declined (simulated)");
        return new ChargePaymentResult { TransactionId = "txn_" + Guid.NewGuid().ToString("N")[..10] };
    }
}

/// <summary>
/// Runs on its own named Worker, so inventory updates are executed one at a time
/// </summary>
[Worker("inventory")]
public class ReserveInventoryCommand(ILogger<ReserveInventoryCommand> logger, IBackgroundJobs jobs)
    : AsyncCommandWithResult<OrderStep, ReserveInventoryResult>
{
    protected override async Task<ReserveInventoryResult> RunAsync(OrderStep request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        // A dependent Job can read the result of the Job it depends on
        var payment = Request.GetBackgroundJob().ParentJob?.ResponseBody?.FromJson<ChargePaymentResult>();
        log.LogInformation("Reserving stock for Order {OrderId}, paid with {TransactionId}",
            request.OrderId, payment?.TransactionId);
        await SimulatedWork.RunAsync(log, steps:3, msPerStep:500, "Reserving", token);
        if (request.Fail)
            throw new Exception("Out of stock (simulated)");
        return new ReserveInventoryResult { WarehouseId = "WH-" + Random.Shared.Next(1, 5) };
    }
}

public class ShipOrderCommand(ILogger<ShipOrderCommand> logger, IBackgroundJobs jobs)
    : AsyncCommandWithResult<OrderStep, ShipOrderResult>
{
    protected override async Task<ShipOrderResult> RunAsync(OrderStep request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        var reservation = Request.GetBackgroundJob().ParentJob?.ResponseBody?.FromJson<ReserveInventoryResult>();
        log.LogInformation("Shipping Order {OrderId} from {WarehouseId}", request.OrderId, reservation?.WarehouseId);
        await SimulatedWork.RunAsync(log, steps:3, msPerStep:500, "Packing", token);
        if (request.Fail)
            throw new Exception("Courier unavailable (simulated)");
        return new ShipOrderResult {
            OrderId = request.OrderId,
            TrackingNumber = "TRK" + Random.Shared.Next(100000, 999999),
        };
    }
}

/// <summary>
/// The Callback of the ShipOrder Job, which receives its Response once it completes
/// </summary>
public class OrderShippedCallbackCommand(ILogger<OrderShippedCallbackCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<ShipOrderResult>
{
    protected override Task RunAsync(ShipOrderResult request, CancellationToken token)
    {
        Request.CreateJobLogger(jobs, logger).LogInformation("Order {OrderId} shipped with tracking number {TrackingNumber}",
            request.OrderId, request.TrackingNumber);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Queued with DependsOnPolicy.OnFinished, so it runs once shipping has finished whether it
/// succeeded, failed or was cancelled
/// </summary>
public class NotifyCustomerCommand(ILogger<NotifyCustomerCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<OrderStep>
{
    protected override async Task RunAsync(OrderStep request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        var shipping = Request.GetBackgroundJob().ParentJob;
        if (shipping?.State == BackgroundJobState.Completed)
            log.LogInformation("Emailing customer: Order {OrderId} is on its way", request.OrderId);
        else
            log.LogInformation("Emailing customer: Order {OrderId} was delayed ({State}: {Error})",
                request.OrderId, shipping?.State, shipping?.Error?.Message);
        await Task.Delay(500, token);
    }
}

// Batches

public class ResizeImage
{
    public string ImageUrl { get; set; } = null!;
    public int Width { get; set; }
    public bool Fail { get; set; }
}

public class ResizeImageCommand(ILogger<ResizeImageCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<ResizeImage>
{
    protected override async Task RunAsync(ResizeImage request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Resizing {ImageUrl} to {Width}px", request.ImageUrl, request.Width);
        await SimulatedWork.RunAsync(log, steps:4, msPerStep:Random.Shared.Next(250, 750), "Resizing", token);
        if (request.Fail)
            throw new Exception($"Corrupt image {request.ImageUrl} (simulated)");
    }
}

public class CreateZipArchive
{
    public string BatchId { get; set; } = null!;
}

/// <summary>
/// Queued with DependsOnBatch, so it only runs once every image in the batch has finished
/// </summary>
public class CreateZipArchiveCommand(ILogger<CreateZipArchiveCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<CreateZipArchive>
{
    protected override async Task RunAsync(CreateZipArchive request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        var batch = jobs.GetJobBatch(request.BatchId);
        log.LogInformation("Zipping {Completed} resized images of batch {BatchId}", batch?.Completed, request.BatchId);
        await SimulatedWork.RunAsync(log, steps:3, msPerStep:500, "Compressing", token);
    }
}

/// <summary>A Batch Callback, runs with the JobBatch once every Job in it has finished</summary>
public class ImagesBatchFinishedCommand(ILogger<ImagesBatchFinishedCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<JobBatch>
{
    protected override Task RunAsync(JobBatch batch, CancellationToken token)
    {
        Request.CreateJobLogger(jobs, logger).LogInformation(
            "Batch {BatchId} finished: {Completed} completed, {Failed} failed, {Cancelled} cancelled",
            batch.Id, batch.Completed, batch.Failed, batch.Cancelled);
        return Task.CompletedTask;
    }
}

/// <summary>A Batch OnSuccess Callback, only runs if every Job in the batch completed</summary>
public class PublishGalleryCommand(ILogger<PublishGalleryCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<JobBatch>
{
    protected override Task RunAsync(JobBatch batch, CancellationToken token)
    {
        Request.CreateJobLogger(jobs, logger).LogInformation("Every image resized, publishing gallery {BatchId}", batch.Id);
        return Task.CompletedTask;
    }
}

// Per-tenant ordering

public class SyncTenantData
{
    public string Tenant { get; set; } = null!;
    public int Sequence { get; set; }
}

public class SyncTenantDataCommand(ILogger<SyncTenantDataCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<SyncTenantData>
{
    protected override async Task RunAsync(SyncTenantData request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Syncing {Tenant} #{Sequence}", request.Tenant, request.Sequence);
        await SimulatedWork.RunAsync(log, steps:4, msPerStep:750, $"Syncing {request.Tenant}", token);
    }
}

// De-duplication

public class RefreshCache
{
    public string CacheName { get; set; } = null!;
}

public class RefreshCacheCommand(ILogger<RefreshCacheCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<RefreshCache>
{
    protected override async Task RunAsync(RefreshCache request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Refreshing {CacheName} cache", request.CacheName);
        await SimulatedWork.RunAsync(log, steps:10, msPerStep:1000, "Loading", token);
    }
}

// Rate limited third-party API

public class CallExternalApi
{
    public int CallNumber { get; set; }
    public bool Urgent { get; set; }
}

public class CallExternalApiCommand(ILogger<CallExternalApiCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<CallExternalApi>
{
    protected override async Task RunAsync(CallExternalApi request, CancellationToken token)
    {
        Request.CreateJobLogger(jobs, logger).LogInformation("Calling third-party API: call #{CallNumber}{Urgent}",
            request.CallNumber, request.Urgent ? " (urgent)" : "");
        await Task.Delay(300, token);
    }
}

// Recurring task

public class CleanupTempFiles
{
    public string Directory { get; set; } = null!;
}

public class CleanupTempFilesCommand(ILogger<CleanupTempFilesCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<CleanupTempFiles>
{
    protected override async Task RunAsync(CleanupTempFiles request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        await SimulatedWork.RunAsync(log, steps:3, msPerStep:500, $"Scanning {request.Directory}", token);
        log.LogInformation("Deleted {Count} temp files from {Directory}", Random.Shared.Next(0, 50), request.Directory);
    }
}

// Transactional outbox

public class ProcessNewOrder
{
    public string OrderRef { get; set; } = null!;
    public string Customer { get; set; } = null!;
    public decimal Amount { get; set; }
}

public class SendOrderConfirmationCommand(ILogger<SendOrderConfirmationCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<ProcessNewOrder>
{
    protected override async Task RunAsync(ProcessNewOrder request, CancellationToken token)
    {
        Request.CreateJobLogger(jobs, logger).LogInformation("Emailing {Customer} confirmation of order {OrderRef}",
            request.Customer, request.OrderRef);
        await Task.Delay(500, token);
    }
}

public class ProcessNewOrderCommand(ILogger<ProcessNewOrderCommand> logger, IBackgroundJobs jobs)
    : AsyncCommand<ProcessNewOrder>
{
    protected override async Task RunAsync(ProcessNewOrder request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Processing order {OrderRef} of {Amount:C} for {Customer}",
            request.OrderRef, request.Amount, request.Customer);
        await SimulatedWork.RunAsync(log, steps:4, msPerStep:500, "Processing", token);
    }
}
