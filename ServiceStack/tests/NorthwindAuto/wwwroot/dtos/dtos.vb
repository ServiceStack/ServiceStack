' Options:
'Date: 2024-10-17 23:05:22
'Version: 8.41
'Tip: To override a DTO option, remove "''" prefix before updating
'BaseUrl: http://localhost:20000
'
'''GlobalNamespace: 
'''MakePartial: True
'''MakeVirtual: True
'''MakeDataContractsExtensible: False
'''AddReturnMarker: True
'''AddDescriptionAsComments: True
'''AddDataContractAttributes: False
'''AddIndexesToDataMembers: False
'''AddGeneratedCodeAttributes: False
'''AddResponseStatus: False
'''AddImplicitVersion: 
'''InitializeCollections: True
'''ExportValueTypes: False
'''IncludeTypes: 
'''ExcludeTypes: 
'''AddNamespaces: 
'''AddDefaultXmlNamespace: http://schemas.servicestack.net/types

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.Serialization
Imports ServiceStack
Imports ServiceStack.DataAnnotations
Imports ServiceStack.Jobs

Namespace Global

    Namespace ServiceStack.Jobs

        Public Partial Class AdminCancelJobs
            Implements IReturn(Of AdminCancelJobsResponse)
            Implements IGet
            Public Sub New()
                Ids = New List(Of Long)
            End Sub

            Public Overridable Property Ids As List(Of Long)
            Public Overridable Property Worker As String
        End Class

        Public Partial Class AdminCancelJobsResponse
            Public Sub New()
                Results = New List(Of Long)
                Errors = New Dictionary(Of Long, String)
            End Sub

            Public Overridable Property Results As List(Of Long)
            Public Overridable Property Errors As Dictionary(Of Long, String)
            Public Overridable Property ResponseStatus As ResponseStatus
        End Class

        Public Partial Class AdminGetJob
            Implements IReturn(Of AdminGetJobResponse)
            Implements IGet
            Public Overridable Property Id As Nullable(Of Long)
            Public Overridable Property RefId As String
        End Class

        Public Partial Class AdminGetJobProgress
            Implements IReturn(Of AdminGetJobProgressResponse)
            Implements IGet
            <Validate(Validator:="GreaterThan(0)")>
            Public Overridable Property Id As Long

            Public Overridable Property LogStart As Nullable(Of Integer)
        End Class

        Public Partial Class AdminGetJobProgressResponse
            Public Overridable Property State As BackgroundJobState
            Public Overridable Property Progress As Nullable(Of Double)
            Public Overridable Property Status As String
            Public Overridable Property Logs As String
            Public Overridable Property DurationMs As Nullable(Of Integer)
            Public Overridable Property [Error] As ResponseStatus
            Public Overridable Property ResponseStatus As ResponseStatus
        End Class

        Public Partial Class AdminGetJobResponse
            Public Overridable Property Result As JobSummary
            Public Overridable Property Queued As BackgroundJob
            Public Overridable Property Completed As CompletedJob
            Public Overridable Property Failed As FailedJob
            Public Overridable Property ResponseStatus As ResponseStatus
        End Class

        Public Partial Class AdminJobDashboard
            Implements IReturn(Of AdminJobDashboardResponse)
            Implements IGet
            Public Overridable Property From As Nullable(Of Date)
            Public Overridable Property To As Nullable(Of Date)
        End Class

        Public Partial Class AdminJobDashboardResponse
            Public Sub New()
                Commands = New List(Of JobStatSummary)
                Apis = New List(Of JobStatSummary)
                Workers = New List(Of JobStatSummary)
                Today = New List(Of HourSummary)
            End Sub

            Public Overridable Property Commands As List(Of JobStatSummary)
            Public Overridable Property Apis As List(Of JobStatSummary)
            Public Overridable Property Workers As List(Of JobStatSummary)
            Public Overridable Property Today As List(Of HourSummary)
            Public Overridable Property ResponseStatus As ResponseStatus
        End Class

        Public Partial Class AdminJobInfo
            Implements IReturn(Of AdminJobInfoResponse)
            Implements IGet
            Public Overridable Property Month As Nullable(Of Date)
        End Class

        Public Partial Class AdminJobInfoResponse
            Public Sub New()
                MonthDbs = New List(Of Date)
                TableCounts = New Dictionary(Of String, Integer)
                WorkerStats = New List(Of WorkerStats)
                QueueCounts = New Dictionary(Of String, Integer)
            End Sub

            Public Overridable Property MonthDbs As List(Of Date)
            Public Overridable Property TableCounts As Dictionary(Of String, Integer)
            Public Overridable Property WorkerStats As List(Of WorkerStats)
            Public Overridable Property QueueCounts As Dictionary(Of String, Integer)
            Public Overridable Property ResponseStatus As ResponseStatus
        End Class

        Public Partial Class AdminQueryBackgroundJobs
            Inherits QueryDb(Of BackgroundJob)
            Implements IReturn(Of QueryResponse(Of BackgroundJob))
            Public Overridable Property Id As Nullable(Of Integer)
            Public Overridable Property RefId As String
        End Class

        Public Partial Class AdminQueryCompletedJobs
            Inherits QueryDb(Of CompletedJob)
            Implements IReturn(Of QueryResponse(Of CompletedJob))
            Public Overridable Property Month As Nullable(Of Date)
        End Class

        Public Partial Class AdminQueryFailedJobs
            Inherits QueryDb(Of FailedJob)
            Implements IReturn(Of QueryResponse(Of FailedJob))
            Public Overridable Property Month As Nullable(Of Date)
        End Class

        Public Partial Class AdminQueryJobSummary
            Inherits QueryDb(Of JobSummary)
            Implements IReturn(Of QueryResponse(Of JobSummary))
            Public Overridable Property Id As Nullable(Of Integer)
            Public Overridable Property RefId As String
        End Class

        Public Partial Class AdminQueryRequestLogs
            Inherits QueryDb(Of RequestLog)
            Implements IReturn(Of QueryResponse(Of RequestLog))
            Public Overridable Property Month As Nullable(Of Date)
        End Class

        Public Partial Class AdminQueryScheduledTasks
            Inherits QueryDb(Of ScheduledTask)
            Implements IReturn(Of QueryResponse(Of ScheduledTask))
        End Class

        Public Partial Class AdminRequeueFailedJobs
            Implements IReturn(Of AdminRequeueFailedJobsJobsResponse)
            Public Sub New()
                Ids = New List(Of Long)
            End Sub

            Public Overridable Property Ids As List(Of Long)
        End Class

        Public Partial Class AdminRequeueFailedJobsJobsResponse
            Public Sub New()
                Results = New List(Of Long)
                Errors = New Dictionary(Of Long, String)
            End Sub

            Public Overridable Property Results As List(Of Long)
            Public Overridable Property Errors As Dictionary(Of Long, String)
            Public Overridable Property ResponseStatus As ResponseStatus
        End Class

        Public Partial Class BackgroundJob
            Inherits BackgroundJobBase
            Public Overridable Property Id As Long
        End Class

        Public Partial Class BackgroundJobBase
            Implements IMeta
            Public Sub New()
                Args = New Dictionary(Of String, String)
                Meta = New Dictionary(Of String, String)
            End Sub

            Public Overridable Property Id As Long
            Public Overridable Property ParentId As Nullable(Of Long)
            Public Overridable Property RefId As String
            Public Overridable Property Worker As String
            Public Overridable Property Tag As String
            Public Overridable Property BatchId As String
            Public Overridable Property Callback As String
            Public Overridable Property DependsOn As Nullable(Of Long)
            Public Overridable Property RunAfter As Nullable(Of Date)
            Public Overridable Property CreatedDate As Date
            Public Overridable Property CreatedBy As String
            Public Overridable Property RequestId As String
            Public Overridable Property RequestType As String
            Public Overridable Property Command As String
            Public Overridable Property Request As String
            Public Overridable Property RequestBody As String
            Public Overridable Property UserId As String
            Public Overridable Property Response As String
            Public Overridable Property ResponseBody As String
            Public Overridable Property State As BackgroundJobState
            Public Overridable Property StartedDate As Nullable(Of Date)
            Public Overridable Property CompletedDate As Nullable(Of Date)
            Public Overridable Property NotifiedDate As Nullable(Of Date)
            Public Overridable Property RetryLimit As Nullable(Of Integer)
            Public Overridable Property Attempts As Integer
            Public Overridable Property DurationMs As Integer
            Public Overridable Property TimeoutSecs As Nullable(Of Integer)
            Public Overridable Property Progress As Nullable(Of Double)
            Public Overridable Property Status As String
            Public Overridable Property Logs As String
            Public Overridable Property LastActivityDate As Nullable(Of Date)
            Public Overridable Property ReplyTo As String
            Public Overridable Property ErrorCode As String
            Public Overridable Property [Error] As ResponseStatus
            Public Overridable Property Args As Dictionary(Of String, String)
            Public Overridable Property Meta As Dictionary(Of String, String)
        End Class

        Public Partial Class BackgroundJobOptions
            Public Sub New()
                Args = New Dictionary(Of String, String)
            End Sub

            Public Overridable Property RefId As String
            Public Overridable Property ParentId As Nullable(Of Long)
            Public Overridable Property Worker As String
            Public Overridable Property RunAfter As Nullable(Of Date)
            Public Overridable Property Callback As String
            Public Overridable Property DependsOn As Nullable(Of Long)
            Public Overridable Property UserId As String
            Public Overridable Property RetryLimit As Nullable(Of Integer)
            Public Overridable Property ReplyTo As String
            Public Overridable Property Tag As String
            Public Overridable Property BatchId As String
            Public Overridable Property CreatedBy As String
            Public Overridable Property TimeoutSecs As Nullable(Of Integer)
            Public Overridable Property Timeout As Nullable(Of TimeSpan)
            Public Overridable Property Args As Dictionary(Of String, String)
            Public Overridable Property RunCommand As Nullable(Of Boolean)
        End Class

        Public Enum BackgroundJobState
            Queued
            Started
            Executed
            Completed
            Failed
            Cancelled
        End Enum

        Public Partial Class CompletedJob
            Inherits BackgroundJobBase
        End Class

        Public Partial Class FailedJob
            Inherits BackgroundJobBase
        End Class

        Public Partial Class HourSummary
            Public Overridable Property Hour As String
            Public Overridable Property Total As Integer
            Public Overridable Property Completed As Integer
            Public Overridable Property Failed As Integer
            Public Overridable Property Cancelled As Integer
        End Class

        Public Partial Class JobStatSummary
            Public Overridable Property Name As String
            Public Overridable Property Total As Integer
            Public Overridable Property Completed As Integer
            Public Overridable Property Retries As Integer
            Public Overridable Property Failed As Integer
            Public Overridable Property Cancelled As Integer
        End Class

        Public Partial Class JobSummary
            Public Overridable Property Id As Long
            Public Overridable Property ParentId As Nullable(Of Long)
            Public Overridable Property RefId As String
            Public Overridable Property Worker As String
            Public Overridable Property Tag As String
            Public Overridable Property BatchId As String
            Public Overridable Property CreatedDate As Date
            Public Overridable Property CreatedBy As String
            Public Overridable Property RequestType As String
            Public Overridable Property Command As String
            Public Overridable Property Request As String
            Public Overridable Property Response As String
            Public Overridable Property UserId As String
            Public Overridable Property Callback As String
            Public Overridable Property StartedDate As Nullable(Of Date)
            Public Overridable Property CompletedDate As Nullable(Of Date)
            Public Overridable Property State As BackgroundJobState
            Public Overridable Property DurationMs As Integer
            Public Overridable Property Attempts As Integer
            Public Overridable Property ErrorCode As String
            Public Overridable Property ErrorMessage As String
        End Class

        Public Partial Class RequestLog
            Implements IMeta
            Public Sub New()
                Headers = New Dictionary(Of String, String)
                FormData = New Dictionary(Of String, String)
                Items = New Dictionary(Of String, String)
                ResponseHeaders = New Dictionary(Of String, String)
                Meta = New Dictionary(Of String, String)
            End Sub

            Public Overridable Property Id As Long
            Public Overridable Property TraceId As String
            Public Overridable Property OperationName As String
            Public Overridable Property DateTime As Date
            Public Overridable Property StatusCode As Integer
            Public Overridable Property StatusDescription As String
            Public Overridable Property HttpMethod As String
            Public Overridable Property AbsoluteUri As String
            Public Overridable Property PathInfo As String
            Public Overridable Property Request As String
            <StringLength(Integer.MaxValue)>
            Public Overridable Property RequestBody As String

            Public Overridable Property UserAuthId As String
            Public Overridable Property SessionId As String
            Public Overridable Property IpAddress As String
            Public Overridable Property ForwardedFor As String
            Public Overridable Property Referer As String
            Public Overridable Property Headers As Dictionary(Of String, String)
            Public Overridable Property FormData As Dictionary(Of String, String)
            Public Overridable Property Items As Dictionary(Of String, String)
            Public Overridable Property ResponseHeaders As Dictionary(Of String, String)
            Public Overridable Property Response As String
            Public Overridable Property ResponseBody As String
            Public Overridable Property SessionBody As String
            Public Overridable Property [Error] As ResponseStatus
            Public Overridable Property ExceptionSource As String
            Public Overridable Property ExceptionDataBody As String
            Public Overridable Property RequestDuration As TimeSpan
            Public Overridable Property Meta As Dictionary(Of String, String)
        End Class

        Public Partial Class ScheduledTask
            Public Overridable Property Id As Long
            Public Overridable Property Name As String
            Public Overridable Property Interval As Nullable(Of TimeSpan)
            Public Overridable Property CronExpression As String
            Public Overridable Property RequestType As String
            Public Overridable Property Command As String
            Public Overridable Property Request As String
            Public Overridable Property RequestBody As String
            Public Overridable Property Options As BackgroundJobOptions
            Public Overridable Property LastRun As Nullable(Of Date)
            Public Overridable Property LastJobId As Nullable(Of Long)
        End Class

        Public Partial Class WorkerStats
            Public Overridable Property Name As String
            Public Overridable Property Queued As Long
            Public Overridable Property Received As Long
            Public Overridable Property Completed As Long
            Public Overridable Property Retries As Long
            Public Overridable Property Failed As Long
            Public Overridable Property RunningJob As Nullable(Of Long)
            Public Overridable Property RunningTime As Nullable(Of TimeSpan)
        End Class
    End Namespace
End Namespace

