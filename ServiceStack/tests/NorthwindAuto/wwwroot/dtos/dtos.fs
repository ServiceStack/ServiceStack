(* Options:
Date: 2024-10-17 23:05:22
Version: 8.41
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:20000

//GlobalNamespace: 
//MakeDataContractsExtensible: False
//AddReturnMarker: True
//AddDescriptionAsComments: True
//AddDataContractAttributes: False
//AddIndexesToDataMembers: False
//AddGeneratedCodeAttributes: False
//AddResponseStatus: False
//AddImplicitVersion: 
//ExportValueTypes: False
//IncludeTypes: 
//ExcludeTypes: 
//InitializeCollections: True
//AddNamespaces: 
*)

namespace ServiceStack

open System
open System.Collections
open System.Collections.Generic
open System.Runtime.Serialization
open ServiceStack
open ServiceStack.DataAnnotations

    [<AllowNullLiteral>]
    type RequestLog() = 
        member val Id:Int64 = new Int64() with get,set
        member val TraceId:String = null with get,set
        member val OperationName:String = null with get,set
        member val DateTime:DateTime = new DateTime() with get,set
        member val StatusCode:Int32 = new Int32() with get,set
        member val StatusDescription:String = null with get,set
        member val HttpMethod:String = null with get,set
        member val AbsoluteUri:String = null with get,set
        member val PathInfo:String = null with get,set
        member val Request:String = null with get,set
        [<StringLength(Int32.MaxValue)>]
        member val RequestBody:String = null with get,set

        member val UserAuthId:String = null with get,set
        member val SessionId:String = null with get,set
        member val IpAddress:String = null with get,set
        member val ForwardedFor:String = null with get,set
        member val Referer:String = null with get,set
        member val Headers:Dictionary<String, String> = new Dictionary<String, String>() with get,set
        member val FormData:Dictionary<String, String> = new Dictionary<String, String>() with get,set
        member val Items:Dictionary<String, String> = new Dictionary<String, String>() with get,set
        member val ResponseHeaders:Dictionary<String, String> = new Dictionary<String, String>() with get,set
        member val Response:String = null with get,set
        member val ResponseBody:String = null with get,set
        member val SessionBody:String = null with get,set
        member val Error:ResponseStatus = null with get,set
        member val ExceptionSource:String = null with get,set
        member val ExceptionDataBody:String = null with get,set
        member val RequestDuration:TimeSpan = new TimeSpan() with get,set
        member val Meta:Dictionary<String, String> = new Dictionary<String, String>() with get,set

    [<AllowNullLiteral>]
    type JobStatSummary() = 
        member val Name:String = null with get,set
        member val Total:Int32 = new Int32() with get,set
        member val Completed:Int32 = new Int32() with get,set
        member val Retries:Int32 = new Int32() with get,set
        member val Failed:Int32 = new Int32() with get,set
        member val Cancelled:Int32 = new Int32() with get,set

    [<AllowNullLiteral>]
    type HourSummary() = 
        member val Hour:String = null with get,set
        member val Total:Int32 = new Int32() with get,set
        member val Completed:Int32 = new Int32() with get,set
        member val Failed:Int32 = new Int32() with get,set
        member val Cancelled:Int32 = new Int32() with get,set

    [<AllowNullLiteral>]
    type WorkerStats() = 
        member val Name:String = null with get,set
        member val Queued:Int64 = new Int64() with get,set
        member val Received:Int64 = new Int64() with get,set
        member val Completed:Int64 = new Int64() with get,set
        member val Retries:Int64 = new Int64() with get,set
        member val Failed:Int64 = new Int64() with get,set
        member val RunningJob:Nullable<Int64> = new Nullable<Int64>() with get,set
        member val RunningTime:Nullable<TimeSpan> = new Nullable<TimeSpan>() with get,set

    type BackgroundJobState =
        | Queued = 0
        | Started = 1
        | Executed = 2
        | Completed = 3
        | Failed = 4
        | Cancelled = 5

    [<AllowNullLiteral>]
    type JobSummary() = 
        member val Id:Int64 = new Int64() with get,set
        member val ParentId:Nullable<Int64> = new Nullable<Int64>() with get,set
        member val RefId:String = null with get,set
        member val Worker:String = null with get,set
        member val Tag:String = null with get,set
        member val BatchId:String = null with get,set
        member val CreatedDate:DateTime = new DateTime() with get,set
        member val CreatedBy:String = null with get,set
        member val RequestType:String = null with get,set
        member val Command:String = null with get,set
        member val Request:String = null with get,set
        member val Response:String = null with get,set
        member val UserId:String = null with get,set
        member val Callback:String = null with get,set
        member val StartedDate:Nullable<DateTime> = new Nullable<DateTime>() with get,set
        member val CompletedDate:Nullable<DateTime> = new Nullable<DateTime>() with get,set
        member val State:BackgroundJobState = new BackgroundJobState() with get,set
        member val DurationMs:Int32 = new Int32() with get,set
        member val Attempts:Int32 = new Int32() with get,set
        member val ErrorCode:String = null with get,set
        member val ErrorMessage:String = null with get,set

    [<AllowNullLiteral>]
    type BackgroundJobBase() = 
        member val Id:Int64 = new Int64() with get,set
        member val ParentId:Nullable<Int64> = new Nullable<Int64>() with get,set
        member val RefId:String = null with get,set
        member val Worker:String = null with get,set
        member val Tag:String = null with get,set
        member val BatchId:String = null with get,set
        member val Callback:String = null with get,set
        member val DependsOn:Nullable<Int64> = new Nullable<Int64>() with get,set
        member val RunAfter:Nullable<DateTime> = new Nullable<DateTime>() with get,set
        member val CreatedDate:DateTime = new DateTime() with get,set
        member val CreatedBy:String = null with get,set
        member val RequestId:String = null with get,set
        member val RequestType:String = null with get,set
        member val Command:String = null with get,set
        member val Request:String = null with get,set
        member val RequestBody:String = null with get,set
        member val UserId:String = null with get,set
        member val Response:String = null with get,set
        member val ResponseBody:String = null with get,set
        member val State:BackgroundJobState = new BackgroundJobState() with get,set
        member val StartedDate:Nullable<DateTime> = new Nullable<DateTime>() with get,set
        member val CompletedDate:Nullable<DateTime> = new Nullable<DateTime>() with get,set
        member val NotifiedDate:Nullable<DateTime> = new Nullable<DateTime>() with get,set
        member val RetryLimit:Nullable<Int32> = new Nullable<Int32>() with get,set
        member val Attempts:Int32 = new Int32() with get,set
        member val DurationMs:Int32 = new Int32() with get,set
        member val TimeoutSecs:Nullable<Int32> = new Nullable<Int32>() with get,set
        member val Progress:Nullable<Double> = new Nullable<Double>() with get,set
        member val Status:String = null with get,set
        member val Logs:String = null with get,set
        member val LastActivityDate:Nullable<DateTime> = new Nullable<DateTime>() with get,set
        member val ReplyTo:String = null with get,set
        member val ErrorCode:String = null with get,set
        member val Error:ResponseStatus = null with get,set
        member val Args:Dictionary<String, String> = new Dictionary<String, String>() with get,set
        member val Meta:Dictionary<String, String> = new Dictionary<String, String>() with get,set

    [<AllowNullLiteral>]
    type BackgroundJob() = 
        inherit BackgroundJobBase()
        member val Id:Int64 = new Int64() with get,set

    [<AllowNullLiteral>]
    type CompletedJob() = 
        inherit BackgroundJobBase()

    [<AllowNullLiteral>]
    type FailedJob() = 
        inherit BackgroundJobBase()

    [<AllowNullLiteral>]
    type BackgroundJobOptions() = 
        member val RefId:String = null with get,set
        member val ParentId:Nullable<Int64> = new Nullable<Int64>() with get,set
        member val Worker:String = null with get,set
        member val RunAfter:Nullable<DateTime> = new Nullable<DateTime>() with get,set
        member val Callback:String = null with get,set
        member val DependsOn:Nullable<Int64> = new Nullable<Int64>() with get,set
        member val UserId:String = null with get,set
        member val RetryLimit:Nullable<Int32> = new Nullable<Int32>() with get,set
        member val ReplyTo:String = null with get,set
        member val Tag:String = null with get,set
        member val BatchId:String = null with get,set
        member val CreatedBy:String = null with get,set
        member val TimeoutSecs:Nullable<Int32> = new Nullable<Int32>() with get,set
        member val Timeout:Nullable<TimeSpan> = new Nullable<TimeSpan>() with get,set
        member val Args:Dictionary<String, String> = new Dictionary<String, String>() with get,set
        member val RunCommand:Nullable<Boolean> = new Nullable<Boolean>() with get,set

    [<AllowNullLiteral>]
    type ScheduledTask() = 
        member val Id:Int64 = new Int64() with get,set
        member val Name:String = null with get,set
        member val Interval:Nullable<TimeSpan> = new Nullable<TimeSpan>() with get,set
        member val CronExpression:String = null with get,set
        member val RequestType:String = null with get,set
        member val Command:String = null with get,set
        member val Request:String = null with get,set
        member val RequestBody:String = null with get,set
        member val Options:BackgroundJobOptions = null with get,set
        member val LastRun:Nullable<DateTime> = new Nullable<DateTime>() with get,set
        member val LastJobId:Nullable<Int64> = new Nullable<Int64>() with get,set

    [<AllowNullLiteral>]
    type AdminJobDashboardResponse() = 
        member val Commands:ResizeArray<JobStatSummary> = new ResizeArray<JobStatSummary>() with get,set
        member val Apis:ResizeArray<JobStatSummary> = new ResizeArray<JobStatSummary>() with get,set
        member val Workers:ResizeArray<JobStatSummary> = new ResizeArray<JobStatSummary>() with get,set
        member val Today:ResizeArray<HourSummary> = new ResizeArray<HourSummary>() with get,set
        member val ResponseStatus:ResponseStatus = null with get,set

    [<AllowNullLiteral>]
    type AdminJobInfoResponse() = 
        member val MonthDbs:ResizeArray<DateTime> = new ResizeArray<DateTime>() with get,set
        member val TableCounts:Dictionary<String, Int32> = new Dictionary<String, Int32>() with get,set
        member val WorkerStats:ResizeArray<WorkerStats> = new ResizeArray<WorkerStats>() with get,set
        member val QueueCounts:Dictionary<String, Int32> = new Dictionary<String, Int32>() with get,set
        member val ResponseStatus:ResponseStatus = null with get,set

    [<AllowNullLiteral>]
    type AdminGetJobResponse() = 
        member val Result:JobSummary = null with get,set
        member val Queued:BackgroundJob = null with get,set
        member val Completed:CompletedJob = null with get,set
        member val Failed:FailedJob = null with get,set
        member val ResponseStatus:ResponseStatus = null with get,set

    [<AllowNullLiteral>]
    type AdminGetJobProgressResponse() = 
        member val State:BackgroundJobState = new BackgroundJobState() with get,set
        member val Progress:Nullable<Double> = new Nullable<Double>() with get,set
        member val Status:String = null with get,set
        member val Logs:String = null with get,set
        member val DurationMs:Nullable<Int32> = new Nullable<Int32>() with get,set
        member val Error:ResponseStatus = null with get,set
        member val ResponseStatus:ResponseStatus = null with get,set

    [<AllowNullLiteral>]
    type AdminRequeueFailedJobsJobsResponse() = 
        member val Results:ResizeArray<Int64> = new ResizeArray<Int64>() with get,set
        member val Errors:Dictionary<Int64, String> = new Dictionary<Int64, String>() with get,set
        member val ResponseStatus:ResponseStatus = null with get,set

    [<AllowNullLiteral>]
    type AdminCancelJobsResponse() = 
        member val Results:ResizeArray<Int64> = new ResizeArray<Int64>() with get,set
        member val Errors:Dictionary<Int64, String> = new Dictionary<Int64, String>() with get,set
        member val ResponseStatus:ResponseStatus = null with get,set

    [<AllowNullLiteral>]
    type AdminQueryRequestLogs() = 
        inherit QueryDb<RequestLog>()
        interface IReturn<QueryResponse<RequestLog>>
        member val Month:Nullable<DateTime> = new Nullable<DateTime>() with get,set

    [<AllowNullLiteral>]
    type AdminJobDashboard() = 
        interface IReturn<AdminJobDashboardResponse>
        interface IGet
        member val From:Nullable<DateTime> = new Nullable<DateTime>() with get,set
        member val To:Nullable<DateTime> = new Nullable<DateTime>() with get,set

    [<AllowNullLiteral>]
    type AdminJobInfo() = 
        interface IReturn<AdminJobInfoResponse>
        interface IGet
        member val Month:Nullable<DateTime> = new Nullable<DateTime>() with get,set

    [<AllowNullLiteral>]
    type AdminGetJob() = 
        interface IReturn<AdminGetJobResponse>
        interface IGet
        member val Id:Nullable<Int64> = new Nullable<Int64>() with get,set
        member val RefId:String = null with get,set

    [<AllowNullLiteral>]
    type AdminGetJobProgress() = 
        interface IReturn<AdminGetJobProgressResponse>
        interface IGet
        [<Validate(Validator="GreaterThan(0)")>]
        member val Id:Int64 = new Int64() with get,set

        member val LogStart:Nullable<Int32> = new Nullable<Int32>() with get,set

    [<AllowNullLiteral>]
    type AdminQueryBackgroundJobs() = 
        inherit QueryDb<BackgroundJob>()
        interface IReturn<QueryResponse<BackgroundJob>>
        member val Id:Nullable<Int32> = new Nullable<Int32>() with get,set
        member val RefId:String = null with get,set

    [<AllowNullLiteral>]
    type AdminQueryJobSummary() = 
        inherit QueryDb<JobSummary>()
        interface IReturn<QueryResponse<JobSummary>>
        member val Id:Nullable<Int32> = new Nullable<Int32>() with get,set
        member val RefId:String = null with get,set

    [<AllowNullLiteral>]
    type AdminQueryScheduledTasks() = 
        inherit QueryDb<ScheduledTask>()
        interface IReturn<QueryResponse<ScheduledTask>>

    [<AllowNullLiteral>]
    type AdminQueryCompletedJobs() = 
        inherit QueryDb<CompletedJob>()
        interface IReturn<QueryResponse<CompletedJob>>
        member val Month:Nullable<DateTime> = new Nullable<DateTime>() with get,set

    [<AllowNullLiteral>]
    type AdminQueryFailedJobs() = 
        inherit QueryDb<FailedJob>()
        interface IReturn<QueryResponse<FailedJob>>
        member val Month:Nullable<DateTime> = new Nullable<DateTime>() with get,set

    [<AllowNullLiteral>]
    type AdminRequeueFailedJobs() = 
        interface IReturn<AdminRequeueFailedJobsJobsResponse>
        member val Ids:ResizeArray<Int64> = new ResizeArray<Int64>() with get,set

    [<AllowNullLiteral>]
    type AdminCancelJobs() = 
        interface IReturn<AdminCancelJobsResponse>
        interface IGet
        member val Ids:ResizeArray<Int64> = new ResizeArray<Int64>() with get,set
        member val Worker:String = null with get,set

