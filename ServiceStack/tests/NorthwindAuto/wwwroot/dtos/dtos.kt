/* Options:
Date: 2024-10-17 23:05:22
Version: 8.41
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:20000

//Package: 
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//InitializeCollections: True
//TreatTypesAsStrings: 
//DefaultImports: java.math.*,java.util.*,net.servicestack.client.*,com.google.gson.annotations.*,com.google.gson.reflect.*
*/

import java.math.*
import java.util.*
import net.servicestack.client.*
import com.google.gson.annotations.*
import com.google.gson.reflect.*


@Route("/metadata/app")
@DataContract
open class MetadataApp : IReturn<AppMetadata>, IGet
{
    @DataMember(Order=1)
    var view:String? = null

    @DataMember(Order=2)
    var includeTypes:ArrayList<String> = ArrayList<String>()
    companion object { private val responseType = AppMetadata::class.java }
    override fun getResponseType(): Any? = MetadataApp.responseType
}

open class AdminDashboard : IReturn<AdminDashboardResponse>, IGet
{
    companion object { private val responseType = AdminDashboardResponse::class.java }
    override fun getResponseType(): Any? = AdminDashboard.responseType
}

/**
* Sign In
*/
@Route(Path="/auth", Verbs="GET,POST")
// @Route(Path="/auth/{provider}", Verbs="GET,POST")
@Api(Description="Sign In")
@DataContract
open class Authenticate : IReturn<AuthenticateResponse>, IPost
{
    /**
    * AuthProvider, e.g. credentials
    */
    @DataMember(Order=1)
    var provider:String? = null

    @DataMember(Order=2)
    var userName:String? = null

    @DataMember(Order=3)
    var password:String? = null

    @DataMember(Order=4)
    var rememberMe:Boolean? = null

    @DataMember(Order=5)
    var accessToken:String? = null

    @DataMember(Order=6)
    var accessTokenSecret:String? = null

    @DataMember(Order=7)
    var returnUrl:String? = null

    @DataMember(Order=8)
    var errorView:String? = null

    @DataMember(Order=9)
    var meta:HashMap<String,String> = HashMap<String,String>()
    companion object { private val responseType = AuthenticateResponse::class.java }
    override fun getResponseType(): Any? = Authenticate.responseType
}

@Route(Path="/assignroles", Verbs="POST")
@DataContract
open class AssignRoles : IReturn<AssignRolesResponse>, IPost
{
    @DataMember(Order=1)
    var userName:String? = null

    @DataMember(Order=2)
    var permissions:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=3)
    var roles:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=4)
    var meta:HashMap<String,String> = HashMap<String,String>()
    companion object { private val responseType = AssignRolesResponse::class.java }
    override fun getResponseType(): Any? = AssignRoles.responseType
}

@Route(Path="/unassignroles", Verbs="POST")
@DataContract
open class UnAssignRoles : IReturn<UnAssignRolesResponse>, IPost
{
    @DataMember(Order=1)
    var userName:String? = null

    @DataMember(Order=2)
    var permissions:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=3)
    var roles:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=4)
    var meta:HashMap<String,String> = HashMap<String,String>()
    companion object { private val responseType = UnAssignRolesResponse::class.java }
    override fun getResponseType(): Any? = UnAssignRoles.responseType
}

@DataContract
open class AdminGetUser : IReturn<AdminUserResponse>, IGet
{
    @DataMember(Order=10)
    var id:String? = null
    companion object { private val responseType = AdminUserResponse::class.java }
    override fun getResponseType(): Any? = AdminGetUser.responseType
}

@DataContract
open class AdminQueryUsers : IReturn<AdminUsersResponse>, IGet
{
    @DataMember(Order=1)
    var query:String? = null

    @DataMember(Order=2)
    var orderBy:String? = null

    @DataMember(Order=3)
    var skip:Int? = null

    @DataMember(Order=4)
    var take:Int? = null
    companion object { private val responseType = AdminUsersResponse::class.java }
    override fun getResponseType(): Any? = AdminQueryUsers.responseType
}

@DataContract
open class AdminCreateUser : AdminUserBase(), IReturn<AdminUserResponse>, IPost
{
    @DataMember(Order=10)
    var roles:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=11)
    var permissions:ArrayList<String> = ArrayList<String>()
    companion object { private val responseType = AdminUserResponse::class.java }
    override fun getResponseType(): Any? = AdminCreateUser.responseType
}

@DataContract
open class AdminUpdateUser : AdminUserBase(), IReturn<AdminUserResponse>, IPut
{
    @DataMember(Order=10)
    var id:String? = null

    @DataMember(Order=11)
    var lockUser:Boolean? = null

    @DataMember(Order=12)
    var unlockUser:Boolean? = null

    @DataMember(Order=13)
    var lockUserUntil:Date? = null

    @DataMember(Order=14)
    var addRoles:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=15)
    var removeRoles:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=16)
    var addPermissions:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=17)
    var removePermissions:ArrayList<String> = ArrayList<String>()
    companion object { private val responseType = AdminUserResponse::class.java }
    override fun getResponseType(): Any? = AdminUpdateUser.responseType
}

@DataContract
open class AdminDeleteUser : IReturn<AdminDeleteUserResponse>, IDelete
{
    @DataMember(Order=10)
    var id:String? = null
    companion object { private val responseType = AdminDeleteUserResponse::class.java }
    override fun getResponseType(): Any? = AdminDeleteUser.responseType
}

open class AdminQueryRequestLogs : QueryDb<RequestLog>(), IReturn<QueryResponse<RequestLog>>
{
    var month:Date? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<RequestLog>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryRequestLogs.responseType
}

open class AdminProfiling : IReturn<AdminProfilingResponse>
{
    var source:String? = null
    var eventType:String? = null
    var threadId:Int? = null
    var traceId:String? = null
    var userAuthId:String? = null
    var sessionId:String? = null
    var tag:String? = null
    var skip:Int? = null
    var take:Int? = null
    var orderBy:String? = null
    var withErrors:Boolean? = null
    var pending:Boolean? = null
    companion object { private val responseType = AdminProfilingResponse::class.java }
    override fun getResponseType(): Any? = AdminProfiling.responseType
}

open class AdminRedis : IReturn<AdminRedisResponse>, IPost
{
    var db:Int? = null
    var query:String? = null
    var reconnect:RedisEndpointInfo? = null
    var take:Int? = null
    var position:Int? = null
    var args:ArrayList<String> = ArrayList<String>()
    companion object { private val responseType = AdminRedisResponse::class.java }
    override fun getResponseType(): Any? = AdminRedis.responseType
}

open class AdminDatabase : IReturn<AdminDatabaseResponse>, IGet
{
    var db:String? = null
    var schema:String? = null
    var table:String? = null
    var fields:ArrayList<String> = ArrayList<String>()
    var take:Int? = null
    var skip:Int? = null
    var orderBy:String? = null
    var include:String? = null
    companion object { private val responseType = AdminDatabaseResponse::class.java }
    override fun getResponseType(): Any? = AdminDatabase.responseType
}

open class ViewCommands : IReturn<ViewCommandsResponse>, IGet
{
    var include:ArrayList<String> = ArrayList<String>()
    var skip:Int? = null
    var take:Int? = null
    companion object { private val responseType = ViewCommandsResponse::class.java }
    override fun getResponseType(): Any? = ViewCommands.responseType
}

open class ExecuteCommand : IReturn<ExecuteCommandResponse>, IPost
{
    var command:String? = null
    var requestJson:String? = null
    companion object { private val responseType = ExecuteCommandResponse::class.java }
    override fun getResponseType(): Any? = ExecuteCommand.responseType
}

@DataContract
open class AdminQueryApiKeys : IReturn<AdminApiKeysResponse>, IGet
{
    @DataMember(Order=1)
    var id:Int? = null

    @DataMember(Order=2)
    var search:String? = null

    @DataMember(Order=3)
    var userId:String? = null

    @DataMember(Order=4)
    var userName:String? = null

    @DataMember(Order=5)
    var orderBy:String? = null

    @DataMember(Order=6)
    var skip:Int? = null

    @DataMember(Order=7)
    var take:Int? = null
    companion object { private val responseType = AdminApiKeysResponse::class.java }
    override fun getResponseType(): Any? = AdminQueryApiKeys.responseType
}

@DataContract
open class AdminCreateApiKey : IReturn<AdminApiKeyResponse>, IPost
{
    @DataMember(Order=1)
    var name:String? = null

    @DataMember(Order=2)
    var userId:String? = null

    @DataMember(Order=3)
    var userName:String? = null

    @DataMember(Order=4)
    var scopes:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=5)
    var features:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=6)
    var restrictTo:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=7)
    var expiryDate:Date? = null

    @DataMember(Order=8)
    var notes:String? = null

    @DataMember(Order=9)
    var refId:Int? = null

    @DataMember(Order=10)
    var refIdStr:String? = null

    @DataMember(Order=11)
    var meta:HashMap<String,String> = HashMap<String,String>()
    companion object { private val responseType = AdminApiKeyResponse::class.java }
    override fun getResponseType(): Any? = AdminCreateApiKey.responseType
}

@DataContract
open class AdminUpdateApiKey : IReturn<EmptyResponse>, IPatch
{
    @DataMember(Order=1)
    @Validate(Validator="GreaterThan(0)")
    var id:Int? = null

    @DataMember(Order=2)
    var name:String? = null

    @DataMember(Order=3)
    var userId:String? = null

    @DataMember(Order=4)
    var userName:String? = null

    @DataMember(Order=5)
    var scopes:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=6)
    var features:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=7)
    var restrictTo:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=8)
    var expiryDate:Date? = null

    @DataMember(Order=9)
    var cancelledDate:Date? = null

    @DataMember(Order=10)
    var notes:String? = null

    @DataMember(Order=11)
    var refId:Int? = null

    @DataMember(Order=12)
    var refIdStr:String? = null

    @DataMember(Order=13)
    var meta:HashMap<String,String> = HashMap<String,String>()

    @DataMember(Order=14)
    var reset:ArrayList<String> = ArrayList<String>()
    companion object { private val responseType = EmptyResponse::class.java }
    override fun getResponseType(): Any? = AdminUpdateApiKey.responseType
}

@DataContract
open class AdminDeleteApiKey : IReturn<EmptyResponse>, IDelete
{
    @DataMember(Order=1)
    @Validate(Validator="GreaterThan(0)")
    var id:Int? = null
    companion object { private val responseType = EmptyResponse::class.java }
    override fun getResponseType(): Any? = AdminDeleteApiKey.responseType
}

open class AdminJobDashboard : IReturn<AdminJobDashboardResponse>, IGet
{
    var from:Date? = null
    var to:Date? = null
    companion object { private val responseType = AdminJobDashboardResponse::class.java }
    override fun getResponseType(): Any? = AdminJobDashboard.responseType
}

open class AdminJobInfo : IReturn<AdminJobInfoResponse>, IGet
{
    var month:Date? = null
    companion object { private val responseType = AdminJobInfoResponse::class.java }
    override fun getResponseType(): Any? = AdminJobInfo.responseType
}

open class AdminGetJob : IReturn<AdminGetJobResponse>, IGet
{
    var id:Long? = null
    var refId:String? = null
    companion object { private val responseType = AdminGetJobResponse::class.java }
    override fun getResponseType(): Any? = AdminGetJob.responseType
}

open class AdminGetJobProgress : IReturn<AdminGetJobProgressResponse>, IGet
{
    @Validate(Validator="GreaterThan(0)")
    var id:Long? = null

    var logStart:Int? = null
    companion object { private val responseType = AdminGetJobProgressResponse::class.java }
    override fun getResponseType(): Any? = AdminGetJobProgress.responseType
}

open class AdminQueryBackgroundJobs : QueryDb<BackgroundJob>(), IReturn<QueryResponse<BackgroundJob>>
{
    var id:Int? = null
    var refId:String? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<BackgroundJob>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryBackgroundJobs.responseType
}

open class AdminQueryJobSummary : QueryDb<JobSummary>(), IReturn<QueryResponse<JobSummary>>
{
    var id:Int? = null
    var refId:String? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<JobSummary>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryJobSummary.responseType
}

open class AdminQueryScheduledTasks : QueryDb<ScheduledTask>(), IReturn<QueryResponse<ScheduledTask>>
{
    companion object { private val responseType = object : TypeToken<QueryResponse<ScheduledTask>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryScheduledTasks.responseType
}

open class AdminQueryCompletedJobs : QueryDb<CompletedJob>(), IReturn<QueryResponse<CompletedJob>>
{
    var month:Date? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<CompletedJob>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryCompletedJobs.responseType
}

open class AdminQueryFailedJobs : QueryDb<FailedJob>(), IReturn<QueryResponse<FailedJob>>
{
    var month:Date? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<FailedJob>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryFailedJobs.responseType
}

open class AdminRequeueFailedJobs : IReturn<AdminRequeueFailedJobsJobsResponse>
{
    var ids:ArrayList<Long> = ArrayList<Long>()
    companion object { private val responseType = AdminRequeueFailedJobsJobsResponse::class.java }
    override fun getResponseType(): Any? = AdminRequeueFailedJobs.responseType
}

open class AdminCancelJobs : IReturn<AdminCancelJobsResponse>, IGet
{
    var ids:ArrayList<Long> = ArrayList<Long>()
    var worker:String? = null
    companion object { private val responseType = AdminCancelJobsResponse::class.java }
    override fun getResponseType(): Any? = AdminCancelJobs.responseType
}

@Route("/requestlogs")
@DataContract
open class RequestLogs : IReturn<RequestLogsResponse>, IGet
{
    @DataMember(Order=1)
    var beforeSecs:Int? = null

    @DataMember(Order=2)
    var afterSecs:Int? = null

    @DataMember(Order=3)
    var operationName:String? = null

    @DataMember(Order=4)
    var ipAddress:String? = null

    @DataMember(Order=5)
    var forwardedFor:String? = null

    @DataMember(Order=6)
    var userAuthId:String? = null

    @DataMember(Order=7)
    var sessionId:String? = null

    @DataMember(Order=8)
    var referer:String? = null

    @DataMember(Order=9)
    var pathInfo:String? = null

    @DataMember(Order=10)
    var ids:ArrayList<Long>? = null

    @DataMember(Order=11)
    var beforeId:Int? = null

    @DataMember(Order=12)
    var afterId:Int? = null

    @DataMember(Order=13)
    var hasResponse:Boolean? = null

    @DataMember(Order=14)
    var withErrors:Boolean? = null

    @DataMember(Order=15)
    var enableSessionTracking:Boolean? = null

    @DataMember(Order=16)
    var enableResponseTracking:Boolean? = null

    @DataMember(Order=17)
    var enableErrorTracking:Boolean? = null

    @DataMember(Order=18)
    var durationLongerThan:TimeSpan? = null

    @DataMember(Order=19)
    var durationLessThan:TimeSpan? = null

    @DataMember(Order=20)
    var skip:Int? = null

    @DataMember(Order=21)
    var take:Int? = null

    @DataMember(Order=22)
    var orderBy:String? = null
    companion object { private val responseType = RequestLogsResponse::class.java }
    override fun getResponseType(): Any? = RequestLogs.responseType
}

@Route("/validation/rules/{Type}")
@DataContract
open class GetValidationRules : IReturn<GetValidationRulesResponse>, IGet
{
    @DataMember(Order=1)
    var authSecret:String? = null

    @DataMember(Order=2)
    @SerializedName("type") var Type:String? = null
    companion object { private val responseType = GetValidationRulesResponse::class.java }
    override fun getResponseType(): Any? = GetValidationRules.responseType
}

@Route("/validation/rules")
@DataContract
open class ModifyValidationRules : IReturnVoid
{
    @DataMember(Order=1)
    var authSecret:String? = null

    @DataMember(Order=2)
    var saveRules:ArrayList<ValidationRule> = ArrayList<ValidationRule>()

    @DataMember(Order=3)
    var deleteRuleIds:ArrayList<Int>? = null

    @DataMember(Order=4)
    var suspendRuleIds:ArrayList<Int>? = null

    @DataMember(Order=5)
    var unsuspendRuleIds:ArrayList<Int>? = null

    @DataMember(Order=6)
    var clearCache:Boolean? = null
}

open class AppMetadata
{
    var date:Date? = null
    var app:AppInfo? = null
    var ui:UiInfo? = null
    var config:ConfigInfo? = null
    var contentTypeFormats:HashMap<String,String> = HashMap<String,String>()
    var httpHandlers:HashMap<String,String> = HashMap<String,String>()
    var plugins:PluginInfo? = null
    var customPlugins:HashMap<String,CustomPluginInfo> = HashMap<String,CustomPluginInfo>()
    var api:MetadataTypes? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class AdminDashboardResponse
{
    var serverStats:ServerStats? = null
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class AuthenticateResponse : IHasSessionId, IHasBearerToken
{
    @DataMember(Order=1)
    var userId:String? = null

    @DataMember(Order=2)
    var sessionId:String? = null

    @DataMember(Order=3)
    var userName:String? = null

    @DataMember(Order=4)
    var displayName:String? = null

    @DataMember(Order=5)
    var referrerUrl:String? = null

    @DataMember(Order=6)
    var bearerToken:String? = null

    @DataMember(Order=7)
    var refreshToken:String? = null

    @DataMember(Order=8)
    var refreshTokenExpiry:Date? = null

    @DataMember(Order=9)
    var profileUrl:String? = null

    @DataMember(Order=10)
    var roles:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=11)
    var permissions:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=12)
    var authProvider:String? = null

    @DataMember(Order=13)
    var responseStatus:ResponseStatus? = null

    @DataMember(Order=14)
    var meta:HashMap<String,String> = HashMap<String,String>()
}

@DataContract
open class AssignRolesResponse
{
    @DataMember(Order=1)
    var allRoles:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=2)
    var allPermissions:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=3)
    var meta:HashMap<String,String> = HashMap<String,String>()

    @DataMember(Order=4)
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class UnAssignRolesResponse
{
    @DataMember(Order=1)
    var allRoles:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=2)
    var allPermissions:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=3)
    var meta:HashMap<String,String> = HashMap<String,String>()

    @DataMember(Order=4)
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminUserResponse
{
    @DataMember(Order=1)
    var id:String? = null

    @DataMember(Order=2)
    var result:HashMap<String,Object> = HashMap<String,Object>()

    @DataMember(Order=3)
    var details:ArrayList<HashMap<String,Object>> = ArrayList<HashMap<String,Object>>()

    @DataMember(Order=4)
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminUsersResponse
{
    @DataMember(Order=1)
    var results:ArrayList<HashMap<String,Object>> = ArrayList<HashMap<String,Object>>()

    @DataMember(Order=2)
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminDeleteUserResponse
{
    @DataMember(Order=1)
    var id:String? = null

    @DataMember(Order=2)
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class QueryResponse<RequestLog>
{
    @DataMember(Order=1)
    var offset:Int? = null

    @DataMember(Order=2)
    var total:Int? = null

    @DataMember(Order=3)
    var results:ArrayList<RequestLog> = ArrayList<RequestLog>()

    @DataMember(Order=4)
    var meta:HashMap<String,String> = HashMap<String,String>()

    @DataMember(Order=5)
    var responseStatus:ResponseStatus? = null
}

open class AdminProfilingResponse
{
    var results:ArrayList<DiagnosticEntry> = ArrayList<DiagnosticEntry>()
    var total:Int? = null
    var responseStatus:ResponseStatus? = null
}

open class AdminRedisResponse
{
    var db:Long? = null
    var searchResults:ArrayList<RedisSearchResult> = ArrayList<RedisSearchResult>()
    var info:HashMap<String,String> = HashMap<String,String>()
    var endpoint:RedisEndpointInfo? = null
    var result:RedisText? = null
    var responseStatus:ResponseStatus? = null
}

open class AdminDatabaseResponse
{
    var results:ArrayList<HashMap<String,Object>> = ArrayList<HashMap<String,Object>>()
    var total:Long? = null
    var columns:ArrayList<MetadataPropertyType> = ArrayList<MetadataPropertyType>()
    var responseStatus:ResponseStatus? = null
}

open class ViewCommandsResponse
{
    var commandTotals:ArrayList<CommandSummary> = ArrayList<CommandSummary>()
    var latestCommands:ArrayList<CommandResult> = ArrayList<CommandResult>()
    var latestFailed:ArrayList<CommandResult> = ArrayList<CommandResult>()
    var responseStatus:ResponseStatus? = null
}

open class ExecuteCommandResponse
{
    var commandResult:CommandResult? = null
    var result:String? = null
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminApiKeysResponse
{
    @DataMember(Order=1)
    var results:ArrayList<PartialApiKey> = ArrayList<PartialApiKey>()

    @DataMember(Order=2)
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminApiKeyResponse
{
    @DataMember(Order=1)
    var result:String? = null

    @DataMember(Order=2)
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class EmptyResponse
{
    @DataMember(Order=1)
    var responseStatus:ResponseStatus? = null
}

open class AdminJobDashboardResponse
{
    var commands:ArrayList<JobStatSummary> = ArrayList<JobStatSummary>()
    var apis:ArrayList<JobStatSummary> = ArrayList<JobStatSummary>()
    var workers:ArrayList<JobStatSummary> = ArrayList<JobStatSummary>()
    var today:ArrayList<HourSummary> = ArrayList<HourSummary>()
    var responseStatus:ResponseStatus? = null
}

open class AdminJobInfoResponse
{
    var monthDbs:ArrayList<Date> = ArrayList<Date>()
    var tableCounts:HashMap<String,Int> = HashMap<String,Int>()
    var workerStats:ArrayList<WorkerStats> = ArrayList<WorkerStats>()
    var queueCounts:HashMap<String,Int> = HashMap<String,Int>()
    var responseStatus:ResponseStatus? = null
}

open class AdminGetJobResponse
{
    var result:JobSummary? = null
    var queued:BackgroundJob? = null
    var completed:CompletedJob? = null
    var failed:FailedJob? = null
    var responseStatus:ResponseStatus? = null
}

open class AdminGetJobProgressResponse
{
    var state:BackgroundJobState? = null
    var progress:Double? = null
    var status:String? = null
    var logs:String? = null
    var durationMs:Int? = null
    var error:ResponseStatus? = null
    var responseStatus:ResponseStatus? = null
}

open class AdminRequeueFailedJobsJobsResponse
{
    var results:ArrayList<Long> = ArrayList<Long>()
    var errors:HashMap<Long,String> = HashMap<Long,String>()
    var responseStatus:ResponseStatus? = null
}

open class AdminCancelJobsResponse
{
    var results:ArrayList<Long> = ArrayList<Long>()
    var errors:HashMap<Long,String> = HashMap<Long,String>()
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class RequestLogsResponse
{
    @DataMember(Order=1)
    var results:ArrayList<RequestLogEntry> = ArrayList<RequestLogEntry>()

    @DataMember(Order=2)
    var usage:HashMap<String,String> = HashMap<String,String>()

    @DataMember(Order=3)
    var total:Int? = null

    @DataMember(Order=4)
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class GetValidationRulesResponse
{
    @DataMember(Order=1)
    var results:ArrayList<ValidationRule> = ArrayList<ValidationRule>()

    @DataMember(Order=2)
    var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminUserBase
{
    @DataMember(Order=1)
    var userName:String? = null

    @DataMember(Order=2)
    var firstName:String? = null

    @DataMember(Order=3)
    var lastName:String? = null

    @DataMember(Order=4)
    var displayName:String? = null

    @DataMember(Order=5)
    var email:String? = null

    @DataMember(Order=6)
    var password:String? = null

    @DataMember(Order=7)
    var profileUrl:String? = null

    @DataMember(Order=8)
    var phoneNumber:String? = null

    @DataMember(Order=9)
    var userAuthProperties:HashMap<String,String> = HashMap<String,String>()

    @DataMember(Order=10)
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class QueryDb<T> : QueryBase()
{
}

open class RequestLog
{
    var id:Long? = null
    var traceId:String? = null
    var operationName:String? = null
    var dateTime:Date? = null
    var statusCode:Int? = null
    var statusDescription:String? = null
    var httpMethod:String? = null
    var absoluteUri:String? = null
    var pathInfo:String? = null
    var request:String? = null
    @StringLength(2147483647)
    var requestBody:String? = null

    var userAuthId:String? = null
    var sessionId:String? = null
    var ipAddress:String? = null
    var forwardedFor:String? = null
    var referer:String? = null
    var headers:HashMap<String,String> = HashMap<String,String>()
    var formData:HashMap<String,String> = HashMap<String,String>()
    var items:HashMap<String,String> = HashMap<String,String>()
    var responseHeaders:HashMap<String,String> = HashMap<String,String>()
    var response:String? = null
    var responseBody:String? = null
    var sessionBody:String? = null
    var error:ResponseStatus? = null
    var exceptionSource:String? = null
    var exceptionDataBody:String? = null
    var requestDuration:TimeSpan? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class RedisEndpointInfo
{
    var host:String? = null
    var port:Int? = null
    var ssl:Boolean? = null
    var db:Long? = null
    var username:String? = null
    var password:String? = null
}

open class BackgroundJob : BackgroundJobBase()
{
    var id:Long? = null
}

open class JobSummary
{
    var id:Long? = null
    var parentId:Long? = null
    var refId:String? = null
    var worker:String? = null
    var tag:String? = null
    var batchId:String? = null
    var createdDate:Date? = null
    var createdBy:String? = null
    var requestType:String? = null
    var command:String? = null
    var request:String? = null
    var response:String? = null
    var userId:String? = null
    var callback:String? = null
    var startedDate:Date? = null
    var completedDate:Date? = null
    var state:BackgroundJobState? = null
    var durationMs:Int? = null
    var attempts:Int? = null
    var errorCode:String? = null
    var errorMessage:String? = null
}

open class ScheduledTask
{
    var id:Long? = null
    var name:String? = null
    var interval:TimeSpan? = null
    var cronExpression:String? = null
    var requestType:String? = null
    var command:String? = null
    var request:String? = null
    var requestBody:String? = null
    var options:BackgroundJobOptions? = null
    var lastRun:Date? = null
    var lastJobId:Long? = null
}

open class CompletedJob : BackgroundJobBase()
{
}

open class FailedJob : BackgroundJobBase()
{
}

open class ValidationRule : ValidateRule()
{
    var id:Int? = null
    @Required()
    @SerializedName("type") var Type:String? = null

    var field:String? = null
    var createdBy:String? = null
    var createdDate:Date? = null
    var modifiedBy:String? = null
    var modifiedDate:Date? = null
    var suspendedBy:String? = null
    var suspendedDate:Date? = null
    var notes:String? = null
}

open class AppInfo
{
    var baseUrl:String? = null
    var serviceStackVersion:String? = null
    var serviceName:String? = null
    var apiVersion:String? = null
    var serviceDescription:String? = null
    var serviceIconUrl:String? = null
    var brandUrl:String? = null
    var brandImageUrl:String? = null
    var textColor:String? = null
    var linkColor:String? = null
    var backgroundColor:String? = null
    var backgroundImageUrl:String? = null
    var iconUrl:String? = null
    var jsTextCase:String? = null
    var useSystemJson:String? = null
    var endpointRouting:ArrayList<String> = ArrayList<String>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class UiInfo
{
    var brandIcon:ImageInfo? = null
    var hideTags:ArrayList<String> = ArrayList<String>()
    var modules:ArrayList<String> = ArrayList<String>()
    var alwaysHideTags:ArrayList<String> = ArrayList<String>()
    var adminLinks:ArrayList<LinkInfo> = ArrayList<LinkInfo>()
    var theme:ThemeInfo? = null
    var locode:LocodeUi? = null
    var explorer:ExplorerUi? = null
    var admin:AdminUi? = null
    var defaultFormats:ApiFormat? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class ConfigInfo
{
    var debugMode:Boolean? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class PluginInfo
{
    var loaded:ArrayList<String> = ArrayList<String>()
    var auth:AuthInfo? = null
    var apiKey:ApiKeyInfo? = null
    var commands:CommandsInfo? = null
    var autoQuery:AutoQueryInfo? = null
    var validation:ValidationInfo? = null
    var sharpPages:SharpPagesInfo? = null
    var requestLogs:RequestLogsInfo? = null
    var profiling:ProfilingInfo? = null
    var filesUpload:FilesUploadInfo? = null
    var adminUsers:AdminUsersInfo? = null
    var adminIdentityUsers:AdminIdentityUsersInfo? = null
    var adminRedis:AdminRedisInfo? = null
    var adminDatabase:AdminDatabaseInfo? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class CustomPluginInfo
{
    var accessRole:String? = null
    var serviceRoutes:HashMap<String,ArrayList<String>> = HashMap<String,ArrayList<String>>()
    var enabled:ArrayList<String> = ArrayList<String>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class MetadataTypes
{
    var config:MetadataTypesConfig? = null
    var namespaces:ArrayList<String> = ArrayList<String>()
    var types:ArrayList<MetadataType> = ArrayList<MetadataType>()
    var operations:ArrayList<MetadataOperationType> = ArrayList<MetadataOperationType>()
}

open class ServerStats
{
    var redis:HashMap<String,Long> = HashMap<String,Long>()
    var serverEvents:HashMap<String,String> = HashMap<String,String>()
    var mqDescription:String? = null
    var mqWorkers:HashMap<String,Long> = HashMap<String,Long>()
}

open class DiagnosticEntry
{
    var id:Long? = null
    var traceId:String? = null
    var source:String? = null
    var eventType:String? = null
    var message:String? = null
    var operation:String? = null
    var threadId:Int? = null
    var error:ResponseStatus? = null
    var commandType:String? = null
    var command:String? = null
    var userAuthId:String? = null
    var sessionId:String? = null
    var arg:String? = null
    var args:ArrayList<String> = ArrayList<String>()
    var argLengths:ArrayList<Long> = ArrayList<Long>()
    var namedArgs:HashMap<String,Object> = HashMap<String,Object>()
    var duration:TimeSpan? = null
    var timestamp:Long? = null
    var date:Date? = null
    var tag:String? = null
    var stackTrace:String? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class RedisSearchResult
{
    var id:String? = null
    @SerializedName("type") var Type:String? = null
    var ttl:Long? = null
    var size:Long? = null
}

open class RedisText
{
    var text:String? = null
    var children:ArrayList<RedisText> = ArrayList<RedisText>()
}

open class MetadataPropertyType
{
    var name:String? = null
    @SerializedName("type") var Type:String? = null
    var namespace:String? = null
    var isValueType:Boolean? = null
    var isEnum:Boolean? = null
    var isPrimaryKey:Boolean? = null
    var genericArgs:ArrayList<String>? = null
    var value:String? = null
    var description:String? = null
    var dataMember:MetadataDataMember? = null
    var readOnly:Boolean? = null
    var paramType:String? = null
    var displayType:String? = null
    var isRequired:Boolean? = null
    var allowableValues:ArrayList<String>? = null
    var allowableMin:Int? = null
    var allowableMax:Int? = null
    var attributes:ArrayList<MetadataAttribute> = ArrayList<MetadataAttribute>()
    var uploadTo:String? = null
    var input:InputInfo? = null
    var format:FormatInfo? = null
    var ref:RefInfo? = null
}

open class CommandSummary
{
    @SerializedName("type") var Type:String? = null
    var name:String? = null
    var count:Int? = null
    var failed:Int? = null
    var retries:Int? = null
    var totalMs:Int? = null
    var minMs:Int? = null
    var maxMs:Int? = null
    var averageMs:Double? = null
    var medianMs:Double? = null
    var lastError:ResponseStatus? = null
    var timings:ConcurrentQueue<Int>? = null
}

open class CommandResult
{
    @SerializedName("type") var Type:String? = null
    var name:String? = null
    var ms:Long? = null
    var at:Date? = null
    var request:String? = null
    var retries:Int? = null
    var attempt:Int? = null
    var error:ResponseStatus? = null
}

@DataContract
open class PartialApiKey
{
    @DataMember(Order=1)
    var id:Int? = null

    @DataMember(Order=2)
    var name:String? = null

    @DataMember(Order=3)
    var userId:String? = null

    @DataMember(Order=4)
    var userName:String? = null

    @DataMember(Order=5)
    var visibleKey:String? = null

    @DataMember(Order=6)
    var environment:String? = null

    @DataMember(Order=7)
    var createdDate:Date? = null

    @DataMember(Order=8)
    var expiryDate:Date? = null

    @DataMember(Order=9)
    var cancelledDate:Date? = null

    @DataMember(Order=10)
    var lastUsedDate:Date? = null

    @DataMember(Order=11)
    var scopes:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=12)
    var features:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=13)
    var restrictTo:ArrayList<String> = ArrayList<String>()

    @DataMember(Order=14)
    var notes:String? = null

    @DataMember(Order=15)
    var refId:Int? = null

    @DataMember(Order=16)
    var refIdStr:String? = null

    @DataMember(Order=17)
    var meta:HashMap<String,String> = HashMap<String,String>()

    @DataMember(Order=18)
    var active:Boolean? = null
}

open class JobStatSummary
{
    var name:String? = null
    var total:Int? = null
    var completed:Int? = null
    var retries:Int? = null
    var failed:Int? = null
    var cancelled:Int? = null
}

open class HourSummary
{
    var hour:String? = null
    var total:Int? = null
    var completed:Int? = null
    var failed:Int? = null
    var cancelled:Int? = null
}

open class WorkerStats
{
    var name:String? = null
    var queued:Long? = null
    var received:Long? = null
    var completed:Long? = null
    var retries:Long? = null
    var failed:Long? = null
    var runningJob:Long? = null
    var runningTime:TimeSpan? = null
}

enum class BackgroundJobState
{
    Queued,
    Started,
    Executed,
    Completed,
    Failed,
    Cancelled,
}

open class RequestLogEntry
{
    var id:Long? = null
    var traceId:String? = null
    var operationName:String? = null
    var dateTime:Date? = null
    var statusCode:Int? = null
    var statusDescription:String? = null
    var httpMethod:String? = null
    var absoluteUri:String? = null
    var pathInfo:String? = null
    @StringLength(2147483647)
    var requestBody:String? = null

    var requestDto:Object? = null
    var userAuthId:String? = null
    var sessionId:String? = null
    var ipAddress:String? = null
    var forwardedFor:String? = null
    var referer:String? = null
    var headers:HashMap<String,String> = HashMap<String,String>()
    var formData:HashMap<String,String> = HashMap<String,String>()
    var items:HashMap<String,String> = HashMap<String,String>()
    var responseHeaders:HashMap<String,String> = HashMap<String,String>()
    var session:Object? = null
    var responseDto:Object? = null
    var errorResponse:Object? = null
    var exceptionSource:String? = null
    var exceptionData:IDictionary? = null
    var requestDuration:TimeSpan? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

@DataContract
open class QueryBase
{
    @DataMember(Order=1)
    var skip:Int? = null

    @DataMember(Order=2)
    var take:Int? = null

    @DataMember(Order=3)
    var orderBy:String? = null

    @DataMember(Order=4)
    var orderByDesc:String? = null

    @DataMember(Order=5)
    var include:String? = null

    @DataMember(Order=6)
    var fields:String? = null

    @DataMember(Order=7)
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class BackgroundJobBase
{
    var id:Long? = null
    var parentId:Long? = null
    var refId:String? = null
    var worker:String? = null
    var tag:String? = null
    var batchId:String? = null
    var callback:String? = null
    var dependsOn:Long? = null
    var runAfter:Date? = null
    var createdDate:Date? = null
    var createdBy:String? = null
    var requestId:String? = null
    var requestType:String? = null
    var command:String? = null
    var request:String? = null
    var requestBody:String? = null
    var userId:String? = null
    var response:String? = null
    var responseBody:String? = null
    var state:BackgroundJobState? = null
    var startedDate:Date? = null
    var completedDate:Date? = null
    var notifiedDate:Date? = null
    var retryLimit:Int? = null
    var attempts:Int? = null
    var durationMs:Int? = null
    var timeoutSecs:Int? = null
    var progress:Double? = null
    var status:String? = null
    var logs:String? = null
    var lastActivityDate:Date? = null
    var replyTo:String? = null
    var errorCode:String? = null
    var error:ResponseStatus? = null
    var args:HashMap<String,String> = HashMap<String,String>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class BackgroundJobOptions
{
    var refId:String? = null
    var parentId:Long? = null
    var worker:String? = null
    var runAfter:Date? = null
    var callback:String? = null
    var dependsOn:Long? = null
    var userId:String? = null
    var retryLimit:Int? = null
    var replyTo:String? = null
    var tag:String? = null
    var batchId:String? = null
    var createdBy:String? = null
    var timeoutSecs:Int? = null
    var timeout:TimeSpan? = null
    var args:HashMap<String,String> = HashMap<String,String>()
    var runCommand:Boolean? = null
}

open class ValidateRule
{
    var validator:String? = null
    var condition:String? = null
    var errorCode:String? = null
    var message:String? = null
}

open class ImageInfo
{
    var svg:String? = null
    var uri:String? = null
    var alt:String? = null
    var cls:String? = null
}

open class LinkInfo
{
    var id:String? = null
    var href:String? = null
    var label:String? = null
    var icon:ImageInfo? = null
    var show:String? = null
    var hide:String? = null
}

open class ThemeInfo
{
    var form:String? = null
    var modelIcon:ImageInfo? = null
}

open class LocodeUi
{
    var css:ApiCss? = null
    var tags:AppTags? = null
    var maxFieldLength:Int? = null
    var maxNestedFields:Int? = null
    var maxNestedFieldLength:Int? = null
}

open class ExplorerUi
{
    var css:ApiCss? = null
    var tags:AppTags? = null
}

open class AdminUi
{
    var css:ApiCss? = null
}

open class ApiFormat
{
    var locale:String? = null
    var assumeUtc:Boolean? = null
    var number:FormatInfo? = null
    var date:FormatInfo? = null
}

open class AuthInfo
{
    var hasAuthSecret:Boolean? = null
    var hasAuthRepository:Boolean? = null
    var includesRoles:Boolean? = null
    var includesOAuthTokens:Boolean? = null
    var htmlRedirect:String? = null
    var authProviders:ArrayList<MetaAuthProvider> = ArrayList<MetaAuthProvider>()
    var identityAuth:IdentityAuthInfo? = null
    var roleLinks:HashMap<String,ArrayList<LinkInfo>> = HashMap<String,ArrayList<LinkInfo>>()
    var serviceRoutes:HashMap<String,ArrayList<String>> = HashMap<String,ArrayList<String>>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class ApiKeyInfo
{
    var label:String? = null
    var httpHeader:String? = null
    var scopes:ArrayList<String> = ArrayList<String>()
    var features:ArrayList<String> = ArrayList<String>()
    var requestTypes:ArrayList<String> = ArrayList<String>()
    var expiresIn:ArrayList<KeyValuePair<String,String>> = ArrayList<KeyValuePair<String,String>>()
    var hide:ArrayList<String> = ArrayList<String>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class CommandsInfo
{
    var commands:ArrayList<CommandInfo> = ArrayList<CommandInfo>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class AutoQueryInfo
{
    var maxLimit:Int? = null
    var untypedQueries:Boolean? = null
    var rawSqlFilters:Boolean? = null
    var autoQueryViewer:Boolean? = null
    var async:Boolean? = null
    var orderByPrimaryKey:Boolean? = null
    var crudEvents:Boolean? = null
    var crudEventsServices:Boolean? = null
    var accessRole:String? = null
    var namedConnection:String? = null
    var viewerConventions:ArrayList<AutoQueryConvention> = ArrayList<AutoQueryConvention>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class ValidationInfo
{
    var hasValidationSource:Boolean? = null
    var hasValidationSourceAdmin:Boolean? = null
    var serviceRoutes:HashMap<String,ArrayList<String>> = HashMap<String,ArrayList<String>>()
    var typeValidators:ArrayList<ScriptMethodType> = ArrayList<ScriptMethodType>()
    var propertyValidators:ArrayList<ScriptMethodType> = ArrayList<ScriptMethodType>()
    var accessRole:String? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class SharpPagesInfo
{
    var apiPath:String? = null
    var scriptAdminRole:String? = null
    var metadataDebugAdminRole:String? = null
    var metadataDebug:Boolean? = null
    var spaFallback:Boolean? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class RequestLogsInfo
{
    var accessRole:String? = null
    var requestLogger:String? = null
    var defaultLimit:Int? = null
    var serviceRoutes:HashMap<String,ArrayList<String>> = HashMap<String,ArrayList<String>>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class ProfilingInfo
{
    var accessRole:String? = null
    var defaultLimit:Int? = null
    var summaryFields:ArrayList<String> = ArrayList<String>()
    var tagLabel:String? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class FilesUploadInfo
{
    var basePath:String? = null
    var locations:ArrayList<FilesUploadLocation> = ArrayList<FilesUploadLocation>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class AdminUsersInfo
{
    var accessRole:String? = null
    var enabled:ArrayList<String> = ArrayList<String>()
    var userAuth:MetadataType? = null
    var allRoles:ArrayList<String> = ArrayList<String>()
    var allPermissions:ArrayList<String> = ArrayList<String>()
    var queryUserAuthProperties:ArrayList<String> = ArrayList<String>()
    var queryMediaRules:ArrayList<MediaRule> = ArrayList<MediaRule>()
    var formLayout:ArrayList<InputInfo> = ArrayList<InputInfo>()
    var css:ApiCss? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class AdminIdentityUsersInfo
{
    var accessRole:String? = null
    var enabled:ArrayList<String> = ArrayList<String>()
    var identityUser:MetadataType? = null
    var allRoles:ArrayList<String> = ArrayList<String>()
    var allPermissions:ArrayList<String> = ArrayList<String>()
    var queryIdentityUserProperties:ArrayList<String> = ArrayList<String>()
    var queryMediaRules:ArrayList<MediaRule> = ArrayList<MediaRule>()
    var formLayout:ArrayList<InputInfo> = ArrayList<InputInfo>()
    var css:ApiCss? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class AdminRedisInfo
{
    var queryLimit:Int? = null
    var databases:ArrayList<Int> = ArrayList<Int>()
    var modifiableConnection:Boolean? = null
    var endpoint:RedisEndpointInfo? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class AdminDatabaseInfo
{
    var queryLimit:Int? = null
    var databases:ArrayList<DatabaseInfo> = ArrayList<DatabaseInfo>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class MetadataTypesConfig
{
    var baseUrl:String? = null
    var usePath:String? = null
    var makePartial:Boolean? = null
    var makeVirtual:Boolean? = null
    var makeInternal:Boolean? = null
    var baseClass:String? = null
    @SerializedName("package") var Package:String? = null
    var addReturnMarker:Boolean? = null
    var addDescriptionAsComments:Boolean? = null
    var addDocAnnotations:Boolean? = null
    var addDataContractAttributes:Boolean? = null
    var addIndexesToDataMembers:Boolean? = null
    var addGeneratedCodeAttributes:Boolean? = null
    var addImplicitVersion:Int? = null
    var addResponseStatus:Boolean? = null
    var addServiceStackTypes:Boolean? = null
    var addModelExtensions:Boolean? = null
    var addPropertyAccessors:Boolean? = null
    var excludeGenericBaseTypes:Boolean? = null
    var settersReturnThis:Boolean? = null
    var addNullableAnnotations:Boolean? = null
    var makePropertiesOptional:Boolean? = null
    var exportAsTypes:Boolean? = null
    var excludeImplementedInterfaces:Boolean? = null
    var addDefaultXmlNamespace:String? = null
    var makeDataContractsExtensible:Boolean? = null
    var initializeCollections:Boolean? = null
    var addNamespaces:ArrayList<String> = ArrayList<String>()
    var defaultNamespaces:ArrayList<String> = ArrayList<String>()
    var defaultImports:ArrayList<String> = ArrayList<String>()
    var includeTypes:ArrayList<String> = ArrayList<String>()
    var excludeTypes:ArrayList<String> = ArrayList<String>()
    var exportTags:ArrayList<String> = ArrayList<String>()
    var treatTypesAsStrings:ArrayList<String> = ArrayList<String>()
    var exportValueTypes:Boolean? = null
    var globalNamespace:String? = null
    var excludeNamespace:Boolean? = null
    var dataClass:String? = null
    var dataClassJson:String? = null
    var ignoreTypes:ArrayList<Class> = ArrayList<Class>()
    var exportTypes:ArrayList<Class> = ArrayList<Class>()
    var exportAttributes:ArrayList<Class> = ArrayList<Class>()
    var ignoreTypesInNamespaces:ArrayList<String> = ArrayList<String>()
}

open class MetadataType
{
    var name:String? = null
    var namespace:String? = null
    var genericArgs:ArrayList<String>? = null
    var inherits:MetadataTypeName? = null
    @SerializedName("implements") var Implements:ArrayList<MetadataTypeName>? = null
    var displayType:String? = null
    var description:String? = null
    var notes:String? = null
    var icon:ImageInfo? = null
    var isNested:Boolean? = null
    var isEnum:Boolean? = null
    var isEnumInt:Boolean? = null
    var isInterface:Boolean? = null
    var isAbstract:Boolean? = null
    var isGenericTypeDef:Boolean? = null
    var dataContract:MetadataDataContract? = null
    var properties:ArrayList<MetadataPropertyType> = ArrayList<MetadataPropertyType>()
    var attributes:ArrayList<MetadataAttribute> = ArrayList<MetadataAttribute>()
    var innerTypes:ArrayList<MetadataTypeName> = ArrayList<MetadataTypeName>()
    var enumNames:ArrayList<String> = ArrayList<String>()
    var enumValues:ArrayList<String> = ArrayList<String>()
    var enumMemberValues:ArrayList<String> = ArrayList<String>()
    var enumDescriptions:ArrayList<String> = ArrayList<String>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class MetadataOperationType
{
    var request:MetadataType? = null
    var response:MetadataType? = null
    var actions:ArrayList<String> = ArrayList<String>()
    var returnsVoid:Boolean? = null
    var method:String? = null
    var returnType:MetadataTypeName? = null
    var routes:ArrayList<MetadataRoute> = ArrayList<MetadataRoute>()
    var dataModel:MetadataTypeName? = null
    var viewModel:MetadataTypeName? = null
    var requiresAuth:Boolean? = null
    var requiresApiKey:Boolean? = null
    var requiredRoles:ArrayList<String> = ArrayList<String>()
    var requiresAnyRole:ArrayList<String> = ArrayList<String>()
    var requiredPermissions:ArrayList<String> = ArrayList<String>()
    var requiresAnyPermission:ArrayList<String> = ArrayList<String>()
    var tags:ArrayList<String> = ArrayList<String>()
    var ui:ApiUiInfo? = null
}

open class MetadataDataMember
{
    var name:String? = null
    var order:Int? = null
    var isRequired:Boolean? = null
    var emitDefaultValue:Boolean? = null
}

open class MetadataAttribute
{
    var name:String? = null
    var constructorArgs:ArrayList<MetadataPropertyType> = ArrayList<MetadataPropertyType>()
    var args:ArrayList<MetadataPropertyType> = ArrayList<MetadataPropertyType>()
}

open class InputInfo
{
    var id:String? = null
    var name:String? = null
    @SerializedName("type") var Type:String? = null
    var value:String? = null
    var placeholder:String? = null
    var help:String? = null
    var label:String? = null
    var title:String? = null
    var size:String? = null
    var pattern:String? = null
    var readOnly:Boolean? = null
    var required:Boolean? = null
    var disabled:Boolean? = null
    var autocomplete:String? = null
    var autofocus:String? = null
    var min:String? = null
    var max:String? = null
    var step:String? = null
    var minLength:Int? = null
    var maxLength:Int? = null
    var accept:String? = null
    var capture:String? = null
    var multiple:Boolean? = null
    var allowableValues:ArrayList<String>? = null
    var allowableEntries:ArrayList<KeyValuePair><String, String>? = null
    var options:String? = null
    var ignore:Boolean? = null
    var css:FieldCss? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class FormatInfo
{
    var method:String? = null
    var options:String? = null
    var locale:String? = null
}

open class RefInfo
{
    var model:String? = null
    var selfId:String? = null
    var refId:String? = null
    var refLabel:String? = null
    var queryApi:String? = null
}

open class ApiCss
{
    var form:String? = null
    var fieldset:String? = null
    var field:String? = null
}

open class AppTags
{
    @SerializedName("default") var Default:String? = null
    var other:String? = null
}

open class MetaAuthProvider
{
    var name:String? = null
    var label:String? = null
    @SerializedName("type") var Type:String? = null
    var navItem:NavItem? = null
    var icon:ImageInfo? = null
    var formLayout:ArrayList<InputInfo> = ArrayList<InputInfo>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class IdentityAuthInfo
{
    var hasRefreshToken:Boolean? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class KeyValuePair<TKey, TValue>
{
    var key:TKey? = null
    var value:TValue? = null
}

open class CommandInfo
{
    var name:String? = null
    var tag:String? = null
    var request:MetadataType? = null
    var response:MetadataType? = null
}

open class AutoQueryConvention
{
    var name:String? = null
    var value:String? = null
    var types:String? = null
    var valueType:String? = null
}

open class ScriptMethodType
{
    var name:String? = null
    var paramNames:ArrayList<String>? = null
    var paramTypes:ArrayList<String>? = null
    var returnType:String? = null
}

open class FilesUploadLocation
{
    var name:String? = null
    var readAccessRole:String? = null
    var writeAccessRole:String? = null
    var allowExtensions:ArrayList<String> = ArrayList<String>()
    var allowOperations:String? = null
    var maxFileCount:Int? = null
    var minFileBytes:Long? = null
    var maxFileBytes:Long? = null
}

open class MediaRule
{
    var size:String? = null
    var rule:String? = null
    var applyTo:ArrayList<String>? = null
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class DatabaseInfo
{
    var alias:String? = null
    var name:String? = null
    var schemas:ArrayList<SchemaInfo> = ArrayList<SchemaInfo>()
}

open class MetadataTypeName
{
    var name:String? = null
    var namespace:String? = null
    var genericArgs:ArrayList<String>? = null
}

open class MetadataDataContract
{
    var name:String? = null
    var namespace:String? = null
}

open class MetadataRoute
{
    var path:String? = null
    var verbs:String? = null
    var notes:String? = null
    var summary:String? = null
}

open class ApiUiInfo
{
    var locodeCss:ApiCss? = null
    var explorerCss:ApiCss? = null
    var formLayout:ArrayList<InputInfo> = ArrayList<InputInfo>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class FieldCss
{
    var field:String? = null
    var input:String? = null
    var label:String? = null
}

open class NavItem
{
    var label:String? = null
    var href:String? = null
    var exact:Boolean? = null
    var id:String? = null
    var className:String? = null
    var iconClass:String? = null
    var iconSrc:String? = null
    var show:String? = null
    var hide:String? = null
    var children:ArrayList<NavItem> = ArrayList<NavItem>()
    var meta:HashMap<String,String> = HashMap<String,String>()
}

open class SchemaInfo
{
    var alias:String? = null
    var name:String? = null
    var tables:ArrayList<String> = ArrayList<String>()
}
