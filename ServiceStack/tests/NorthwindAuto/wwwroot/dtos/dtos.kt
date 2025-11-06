/* Options:
Date: 2025-11-06 11:47:32
Version: 8.91
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:20000

//Package: 
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//InitializeCollections: False
//TreatTypesAsStrings: 
//DefaultImports: java.math.*,java.util.*,java.io.InputStream,net.servicestack.client.*,com.google.gson.annotations.*,com.google.gson.reflect.*
*/

import java.math.*
import java.util.*
import java.io.InputStream
import net.servicestack.client.*
import com.google.gson.annotations.*
import com.google.gson.reflect.*


@Route(Path="/metadata/app")
@DataContract
open class MetadataApp : IReturn<AppMetadata>, IGet
{
    @DataMember(Order=1)
    open var view:String? = null

    @DataMember(Order=2)
    open var includeTypes:ArrayList<String>? = null
    companion object { private val responseType = AppMetadata::class.java }
    override fun getResponseType(): Any? = MetadataApp.responseType
}

@DataContract
open class AdminCreateRole : IReturn<IdResponse>, IPost
{
    @DataMember(Order=1)
    open var name:String? = null
    companion object { private val responseType = IdResponse::class.java }
    override fun getResponseType(): Any? = AdminCreateRole.responseType
}

@DataContract
open class AdminGetRoles : IReturn<AdminGetRolesResponse>, IGet
{
    companion object { private val responseType = AdminGetRolesResponse::class.java }
    override fun getResponseType(): Any? = AdminGetRoles.responseType
}

@DataContract
open class AdminGetRole : IReturn<AdminGetRoleResponse>, IGet
{
    @DataMember(Order=1)
    open var id:String? = null
    companion object { private val responseType = AdminGetRoleResponse::class.java }
    override fun getResponseType(): Any? = AdminGetRole.responseType
}

@DataContract
open class AdminUpdateRole : IReturn<IdResponse>, IPost
{
    @DataMember(Order=1)
    open var id:String? = null

    @DataMember(Order=2)
    open var name:String? = null

    @DataMember(Order=3)
    open var addClaims:ArrayList<Property>? = null

    @DataMember(Order=4)
    open var removeClaims:ArrayList<Property>? = null

    @DataMember(Order=5)
    open var responseStatus:ResponseStatus? = null
    companion object { private val responseType = IdResponse::class.java }
    override fun getResponseType(): Any? = AdminUpdateRole.responseType
}

@DataContract
open class AdminDeleteRole : IReturnVoid, IDelete
{
    @DataMember(Order=1)
    open var id:String? = null
}

open class AdminDashboard : IReturn<AdminDashboardResponse>, IGet
{
    companion object { private val responseType = AdminDashboardResponse::class.java }
    override fun getResponseType(): Any? = AdminDashboard.responseType
}

@DataContract
open class AdminQueryApiKeys : IReturn<AdminApiKeysResponse>, IGet
{
    @DataMember(Order=1)
    open var id:Int? = null

    @DataMember(Order=2)
    open var apiKey:String? = null

    @DataMember(Order=3)
    open var search:String? = null

    @DataMember(Order=4)
    open var userId:String? = null

    @DataMember(Order=5)
    open var userName:String? = null

    @DataMember(Order=6)
    open var orderBy:String? = null

    @DataMember(Order=7)
    open var skip:Int? = null

    @DataMember(Order=8)
    open var take:Int? = null
    companion object { private val responseType = AdminApiKeysResponse::class.java }
    override fun getResponseType(): Any? = AdminQueryApiKeys.responseType
}

@DataContract
open class AdminCreateApiKey : IReturn<AdminApiKeyResponse>, IPost
{
    @DataMember(Order=1)
    open var name:String? = null

    @DataMember(Order=2)
    open var userId:String? = null

    @DataMember(Order=3)
    open var userName:String? = null

    @DataMember(Order=4)
    open var scopes:ArrayList<String>? = null

    @DataMember(Order=5)
    open var features:ArrayList<String>? = null

    @DataMember(Order=6)
    open var restrictTo:ArrayList<String>? = null

    @DataMember(Order=7)
    open var expiryDate:Date? = null

    @DataMember(Order=8)
    open var notes:String? = null

    @DataMember(Order=9)
    open var refId:Int? = null

    @DataMember(Order=10)
    open var refIdStr:String? = null

    @DataMember(Order=11)
    open var meta:HashMap<String,String>? = null
    companion object { private val responseType = AdminApiKeyResponse::class.java }
    override fun getResponseType(): Any? = AdminCreateApiKey.responseType
}

@DataContract
open class AdminUpdateApiKey : IReturn<EmptyResponse>, IPatch
{
    @DataMember(Order=1)
    @Validate(Validator="GreaterThan(0)")
    open var id:Int? = null

    @DataMember(Order=2)
    open var name:String? = null

    @DataMember(Order=3)
    open var userId:String? = null

    @DataMember(Order=4)
    open var userName:String? = null

    @DataMember(Order=5)
    open var scopes:ArrayList<String>? = null

    @DataMember(Order=6)
    open var features:ArrayList<String>? = null

    @DataMember(Order=7)
    open var restrictTo:ArrayList<String>? = null

    @DataMember(Order=8)
    open var expiryDate:Date? = null

    @DataMember(Order=9)
    open var cancelledDate:Date? = null

    @DataMember(Order=10)
    open var notes:String? = null

    @DataMember(Order=11)
    open var refId:Int? = null

    @DataMember(Order=12)
    open var refIdStr:String? = null

    @DataMember(Order=13)
    open var meta:HashMap<String,String>? = null

    @DataMember(Order=14)
    open var reset:ArrayList<String>? = null
    companion object { private val responseType = EmptyResponse::class.java }
    override fun getResponseType(): Any? = AdminUpdateApiKey.responseType
}

@DataContract
open class AdminDeleteApiKey : IReturn<EmptyResponse>, IDelete
{
    @DataMember(Order=1)
    @Validate(Validator="GreaterThan(0)")
    open var id:Int? = null
    companion object { private val responseType = EmptyResponse::class.java }
    override fun getResponseType(): Any? = AdminDeleteApiKey.responseType
}

/**
* Chat Completions API (OpenAI-Compatible)
*/
@Route(Path="/v1/chat/completions", Verbs="POST")
@DataContract
open class ChatCompletion : IReturn<ChatResponse>, IPost
{
    /**
    * The messages to generate chat completions for.
    */
    @DataMember(Name="messages")
    @SerializedName("messages")
    open var messages:ArrayList<AiMessage> = ArrayList<AiMessage>()

    /**
    * ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API
    */
    @DataMember(Name="model")
    @SerializedName("model")
    open var model:String? = null

    /**
    * Parameters for audio output. Required when audio output is requested with modalities: [audio]
    */
    @DataMember(Name="audio")
    @SerializedName("audio")
    open var audio:AiChatAudio? = null

    /**
    * Modify the likelihood of specified tokens appearing in the completion.
    */
    @DataMember(Name="logit_bias")
    @SerializedName("logit_bias")
    open var logitBias:HashMap<Int,Int>? = null

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    @DataMember(Name="metadata")
    @SerializedName("metadata")
    open var metadata:HashMap<String,String>? = null

    /**
    * Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.
    */
    @DataMember(Name="reasoning_effort")
    @SerializedName("reasoning_effort")
    open var reasoningEffort:String? = null

    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.
    */
    @DataMember(Name="response_format")
    @SerializedName("response_format")
    open var responseFormat:AiResponseFormat? = null

    /**
    * Specifies the processing type used for serving the request.
    */
    @DataMember(Name="service_tier")
    @SerializedName("service_tier")
    open var serviceTier:String? = null

    /**
    * A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.
    */
    @DataMember(Name="safety_identifier")
    @SerializedName("safety_identifier")
    open var safetyIdentifier:String? = null

    /**
    * Up to 4 sequences where the API will stop generating further tokens.
    */
    @DataMember(Name="stop")
    @SerializedName("stop")
    open var stop:ArrayList<String>? = null

    /**
    * Output types that you would like the model to generate. Most models are capable of generating text, which is the default:
    */
    @DataMember(Name="modalities")
    @SerializedName("modalities")
    open var modalities:ArrayList<String>? = null

    /**
    * Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.
    */
    @DataMember(Name="prompt_cache_key")
    @SerializedName("prompt_cache_key")
    open var promptCacheKey:String? = null

    /**
    * A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.
    */
    @DataMember(Name="tools")
    @SerializedName("tools")
    open var tools:ArrayList<Tool>? = null

    /**
    * Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.
    */
    @DataMember(Name="verbosity")
    @SerializedName("verbosity")
    open var verbosity:String? = null

    /**
    * What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
    */
    @DataMember(Name="temperature")
    @SerializedName("temperature")
    open var temperature:Double? = null

    /**
    * An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.
    */
    @DataMember(Name="max_completion_tokens")
    @SerializedName("max_completion_tokens")
    open var maxCompletionTokens:Int? = null

    /**
    * An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
    */
    @DataMember(Name="top_logprobs")
    @SerializedName("top_logprobs")
    open var topLogprobs:Int? = null

    /**
    * An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.
    */
    @DataMember(Name="top_p")
    @SerializedName("top_p")
    open var topP:Double? = null

    /**
    * Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
    */
    @DataMember(Name="frequency_penalty")
    @SerializedName("frequency_penalty")
    open var frequencyPenalty:Double? = null

    /**
    * Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
    */
    @DataMember(Name="presence_penalty")
    @SerializedName("presence_penalty")
    open var presencePenalty:Double? = null

    /**
    * This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.
    */
    @DataMember(Name="seed")
    @SerializedName("seed")
    open var seed:Int? = null

    /**
    * How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.
    */
    @DataMember(Name="n")
    @SerializedName("n")
    open var n:Int? = null

    /**
    * Whether or not to store the output of this chat completion request for use in our model distillation or evals products.
    */
    @DataMember(Name="store")
    @SerializedName("store")
    open var store:Boolean? = null

    /**
    * Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
    */
    @DataMember(Name="logprobs")
    @SerializedName("logprobs")
    open var logprobs:Boolean? = null

    /**
    * Whether to enable parallel function calling during tool use.
    */
    @DataMember(Name="parallel_tool_calls")
    @SerializedName("parallel_tool_calls")
    open var parallelToolCalls:Boolean? = null

    /**
    * Whether to enable thinking mode for some Qwen models and providers.
    */
    @DataMember(Name="enable_thinking")
    @SerializedName("enable_thinking")
    open var enableThinking:Boolean? = null

    /**
    * If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.
    */
    @DataMember(Name="stream")
    @SerializedName("stream")
    open var stream:Boolean? = null
    companion object { private val responseType = ChatResponse::class.java }
    override fun getResponseType(): Any? = ChatCompletion.responseType
}

open class AdminQueryChatCompletionLogs : QueryDb<ChatCompletionLog>(), IReturn<QueryResponse<ChatCompletionLog>>
{
    open var month:Date? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<ChatCompletionLog>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryChatCompletionLogs.responseType
}

open class AdminMonthlyChatCompletionAnalytics : IReturn<AdminMonthlyChatCompletionAnalyticsResponse>, IGet
{
    open var month:Date? = null
    companion object { private val responseType = AdminMonthlyChatCompletionAnalyticsResponse::class.java }
    override fun getResponseType(): Any? = AdminMonthlyChatCompletionAnalytics.responseType
}

open class AdminDailyChatCompletionAnalytics : IReturn<AdminDailyChatCompletionAnalyticsResponse>, IGet
{
    open var day:Date? = null
    companion object { private val responseType = AdminDailyChatCompletionAnalyticsResponse::class.java }
    override fun getResponseType(): Any? = AdminDailyChatCompletionAnalytics.responseType
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
    open var provider:String? = null

    @DataMember(Order=2)
    open var userName:String? = null

    @DataMember(Order=3)
    open var password:String? = null

    @DataMember(Order=4)
    open var rememberMe:Boolean? = null

    @DataMember(Order=5)
    open var accessToken:String? = null

    @DataMember(Order=6)
    open var accessTokenSecret:String? = null

    @DataMember(Order=7)
    open var returnUrl:String? = null

    @DataMember(Order=8)
    open var errorView:String? = null

    @DataMember(Order=9)
    open var meta:HashMap<String,String>? = null
    companion object { private val responseType = AuthenticateResponse::class.java }
    override fun getResponseType(): Any? = Authenticate.responseType
}

@Route(Path="/assignroles", Verbs="POST")
@DataContract
open class AssignRoles : IReturn<AssignRolesResponse>, IPost
{
    @DataMember(Order=1)
    open var userName:String? = null

    @DataMember(Order=2)
    open var permissions:ArrayList<String>? = null

    @DataMember(Order=3)
    open var roles:ArrayList<String>? = null

    @DataMember(Order=4)
    open var meta:HashMap<String,String>? = null
    companion object { private val responseType = AssignRolesResponse::class.java }
    override fun getResponseType(): Any? = AssignRoles.responseType
}

@Route(Path="/unassignroles", Verbs="POST")
@DataContract
open class UnAssignRoles : IReturn<UnAssignRolesResponse>, IPost
{
    @DataMember(Order=1)
    open var userName:String? = null

    @DataMember(Order=2)
    open var permissions:ArrayList<String>? = null

    @DataMember(Order=3)
    open var roles:ArrayList<String>? = null

    @DataMember(Order=4)
    open var meta:HashMap<String,String>? = null
    companion object { private val responseType = UnAssignRolesResponse::class.java }
    override fun getResponseType(): Any? = UnAssignRoles.responseType
}

@DataContract
open class AdminGetUser : IReturn<AdminUserResponse>, IGet
{
    @DataMember(Order=10)
    open var id:String? = null
    companion object { private val responseType = AdminUserResponse::class.java }
    override fun getResponseType(): Any? = AdminGetUser.responseType
}

@DataContract
open class AdminQueryUsers : IReturn<AdminUsersResponse>, IGet
{
    @DataMember(Order=1)
    open var query:String? = null

    @DataMember(Order=2)
    open var orderBy:String? = null

    @DataMember(Order=3)
    open var skip:Int? = null

    @DataMember(Order=4)
    open var take:Int? = null
    companion object { private val responseType = AdminUsersResponse::class.java }
    override fun getResponseType(): Any? = AdminQueryUsers.responseType
}

@DataContract
open class AdminCreateUser : AdminUserBase(), IReturn<AdminUserResponse>, IPost
{
    @DataMember(Order=10)
    open var roles:ArrayList<String>? = null

    @DataMember(Order=11)
    open var permissions:ArrayList<String>? = null
    companion object { private val responseType = AdminUserResponse::class.java }
    override fun getResponseType(): Any? = AdminCreateUser.responseType
}

@DataContract
open class AdminUpdateUser : AdminUserBase(), IReturn<AdminUserResponse>, IPut
{
    @DataMember(Order=10)
    open var id:String? = null

    @DataMember(Order=11)
    open var lockUser:Boolean? = null

    @DataMember(Order=12)
    open var unlockUser:Boolean? = null

    @DataMember(Order=13)
    open var lockUserUntil:Date? = null

    @DataMember(Order=14)
    open var addRoles:ArrayList<String>? = null

    @DataMember(Order=15)
    open var removeRoles:ArrayList<String>? = null

    @DataMember(Order=16)
    open var addPermissions:ArrayList<String>? = null

    @DataMember(Order=17)
    open var removePermissions:ArrayList<String>? = null

    @DataMember(Order=18)
    open var addClaims:ArrayList<Property>? = null

    @DataMember(Order=19)
    open var removeClaims:ArrayList<Property>? = null
    companion object { private val responseType = AdminUserResponse::class.java }
    override fun getResponseType(): Any? = AdminUpdateUser.responseType
}

@DataContract
open class AdminDeleteUser : IReturn<AdminDeleteUserResponse>, IDelete
{
    @DataMember(Order=10)
    open var id:String? = null
    companion object { private val responseType = AdminDeleteUserResponse::class.java }
    override fun getResponseType(): Any? = AdminDeleteUser.responseType
}

open class AdminQueryRequestLogs : QueryDb<RequestLog>(), IReturn<QueryResponse<RequestLog>>
{
    open var month:Date? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<RequestLog>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryRequestLogs.responseType
}

open class AdminProfiling : IReturn<AdminProfilingResponse>
{
    open var source:String? = null
    open var eventType:String? = null
    open var threadId:Int? = null
    open var traceId:String? = null
    open var userAuthId:String? = null
    open var sessionId:String? = null
    open var tag:String? = null
    open var skip:Int? = null
    open var take:Int? = null
    open var orderBy:String? = null
    open var withErrors:Boolean? = null
    open var pending:Boolean? = null
    companion object { private val responseType = AdminProfilingResponse::class.java }
    override fun getResponseType(): Any? = AdminProfiling.responseType
}

open class AdminRedis : IReturn<AdminRedisResponse>, IPost
{
    open var db:Int? = null
    open var query:String? = null
    open var reconnect:RedisEndpointInfo? = null
    open var take:Int? = null
    open var position:Int? = null
    open var args:ArrayList<String>? = null
    companion object { private val responseType = AdminRedisResponse::class.java }
    override fun getResponseType(): Any? = AdminRedis.responseType
}

open class AdminDatabase : IReturn<AdminDatabaseResponse>, IGet
{
    open var db:String? = null
    open var schema:String? = null
    open var table:String? = null
    open var fields:ArrayList<String>? = null
    open var take:Int? = null
    open var skip:Int? = null
    open var orderBy:String? = null
    open var include:String? = null
    companion object { private val responseType = AdminDatabaseResponse::class.java }
    override fun getResponseType(): Any? = AdminDatabase.responseType
}

open class ViewCommands : IReturn<ViewCommandsResponse>, IGet
{
    open var include:ArrayList<String>? = null
    open var skip:Int? = null
    open var take:Int? = null
    companion object { private val responseType = ViewCommandsResponse::class.java }
    override fun getResponseType(): Any? = ViewCommands.responseType
}

open class ExecuteCommand : IReturn<ExecuteCommandResponse>, IPost
{
    open var command:String? = null
    open var requestJson:String? = null
    companion object { private val responseType = ExecuteCommandResponse::class.java }
    override fun getResponseType(): Any? = ExecuteCommand.responseType
}

open class AdminJobDashboard : IReturn<AdminJobDashboardResponse>, IGet
{
    open var from:Date? = null
    open var to:Date? = null
    companion object { private val responseType = AdminJobDashboardResponse::class.java }
    override fun getResponseType(): Any? = AdminJobDashboard.responseType
}

open class AdminJobInfo : IReturn<AdminJobInfoResponse>, IGet
{
    open var month:Date? = null
    companion object { private val responseType = AdminJobInfoResponse::class.java }
    override fun getResponseType(): Any? = AdminJobInfo.responseType
}

open class AdminGetJob : IReturn<AdminGetJobResponse>, IGet
{
    open var id:Long? = null
    open var refId:String? = null
    companion object { private val responseType = AdminGetJobResponse::class.java }
    override fun getResponseType(): Any? = AdminGetJob.responseType
}

open class AdminGetJobProgress : IReturn<AdminGetJobProgressResponse>, IGet
{
    @Validate(Validator="GreaterThan(0)")
    open var id:Long? = null

    open var logStart:Int? = null
    companion object { private val responseType = AdminGetJobProgressResponse::class.java }
    override fun getResponseType(): Any? = AdminGetJobProgress.responseType
}

open class AdminQueryBackgroundJobs : QueryDb<BackgroundJob>(), IReturn<QueryResponse<BackgroundJob>>
{
    open var id:Int? = null
    open var refId:String? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<BackgroundJob>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryBackgroundJobs.responseType
}

open class AdminQueryJobSummary : QueryDb<JobSummary>(), IReturn<QueryResponse<JobSummary>>
{
    open var id:Int? = null
    open var refId:String? = null
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
    open var month:Date? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<CompletedJob>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryCompletedJobs.responseType
}

open class AdminQueryFailedJobs : QueryDb<FailedJob>(), IReturn<QueryResponse<FailedJob>>
{
    open var month:Date? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<FailedJob>>(){}.type }
    override fun getResponseType(): Any? = AdminQueryFailedJobs.responseType
}

open class AdminRequeueFailedJobs : IReturn<AdminRequeueFailedJobsJobsResponse>
{
    open var ids:ArrayList<Long>? = null
    companion object { private val responseType = AdminRequeueFailedJobsJobsResponse::class.java }
    override fun getResponseType(): Any? = AdminRequeueFailedJobs.responseType
}

open class AdminCancelJobs : IReturn<AdminCancelJobsResponse>, IGet
{
    open var ids:ArrayList<Long>? = null
    open var worker:String? = null
    open var state:BackgroundJobState? = null
    open var cancelWorker:String? = null
    companion object { private val responseType = AdminCancelJobsResponse::class.java }
    override fun getResponseType(): Any? = AdminCancelJobs.responseType
}

@Route(Path="/requestlogs")
@DataContract
open class RequestLogs : IReturn<RequestLogsResponse>, IGet
{
    @DataMember(Order=1)
    open var beforeSecs:Int? = null

    @DataMember(Order=2)
    open var afterSecs:Int? = null

    @DataMember(Order=3)
    open var operationName:String? = null

    @DataMember(Order=4)
    open var ipAddress:String? = null

    @DataMember(Order=5)
    open var forwardedFor:String? = null

    @DataMember(Order=6)
    open var userAuthId:String? = null

    @DataMember(Order=7)
    open var sessionId:String? = null

    @DataMember(Order=8)
    open var referer:String? = null

    @DataMember(Order=9)
    open var pathInfo:String? = null

    @DataMember(Order=10)
    open var bearerToken:String? = null

    @DataMember(Order=11)
    open var ids:ArrayList<Long>? = null

    @DataMember(Order=12)
    open var beforeId:Int? = null

    @DataMember(Order=13)
    open var afterId:Int? = null

    @DataMember(Order=14)
    open var hasResponse:Boolean? = null

    @DataMember(Order=15)
    open var withErrors:Boolean? = null

    @DataMember(Order=16)
    open var enableSessionTracking:Boolean? = null

    @DataMember(Order=17)
    open var enableResponseTracking:Boolean? = null

    @DataMember(Order=18)
    open var enableErrorTracking:Boolean? = null

    @DataMember(Order=19)
    open var durationLongerThan:TimeSpan? = null

    @DataMember(Order=20)
    open var durationLessThan:TimeSpan? = null

    @DataMember(Order=21)
    open var skip:Int? = null

    @DataMember(Order=22)
    open var take:Int? = null

    @DataMember(Order=23)
    open var orderBy:String? = null

    @DataMember(Order=24)
    open var month:Date? = null
    companion object { private val responseType = RequestLogsResponse::class.java }
    override fun getResponseType(): Any? = RequestLogs.responseType
}

@DataContract
open class GetAnalyticsInfo : IReturn<GetAnalyticsInfoResponse>, IGet
{
    @DataMember(Order=1)
    open var month:Date? = null

    @DataMember(Order=2)
    @SerializedName("type") open var Type:String? = null

    @DataMember(Order=3)
    open var op:String? = null

    @DataMember(Order=4)
    open var apiKey:String? = null

    @DataMember(Order=5)
    open var userId:String? = null

    @DataMember(Order=6)
    open var ip:String? = null
    companion object { private val responseType = GetAnalyticsInfoResponse::class.java }
    override fun getResponseType(): Any? = GetAnalyticsInfo.responseType
}

@DataContract
open class GetAnalyticsReports : IReturn<GetAnalyticsReportsResponse>, IGet
{
    @DataMember(Order=1)
    open var month:Date? = null

    @DataMember(Order=2)
    open var filter:String? = null

    @DataMember(Order=3)
    open var value:String? = null

    @DataMember(Order=4)
    open var force:Boolean? = null
    companion object { private val responseType = GetAnalyticsReportsResponse::class.java }
    override fun getResponseType(): Any? = GetAnalyticsReports.responseType
}

@Route(Path="/validation/rules/{Type}")
@DataContract
open class GetValidationRules : IReturn<GetValidationRulesResponse>, IGet
{
    @DataMember(Order=1)
    open var authSecret:String? = null

    @DataMember(Order=2)
    @SerializedName("type") open var Type:String? = null
    companion object { private val responseType = GetValidationRulesResponse::class.java }
    override fun getResponseType(): Any? = GetValidationRules.responseType
}

@Route(Path="/validation/rules")
@DataContract
open class ModifyValidationRules : IReturnVoid
{
    @DataMember(Order=1)
    open var authSecret:String? = null

    @DataMember(Order=2)
    open var saveRules:ArrayList<ValidationRule>? = null

    @DataMember(Order=3)
    open var deleteRuleIds:ArrayList<Int>? = null

    @DataMember(Order=4)
    open var suspendRuleIds:ArrayList<Int>? = null

    @DataMember(Order=5)
    open var unsuspendRuleIds:ArrayList<Int>? = null

    @DataMember(Order=6)
    open var clearCache:Boolean? = null
}

open class AppMetadata
{
    open var date:Date? = null
    open var app:AppInfo? = null
    open var ui:UiInfo? = null
    open var config:ConfigInfo? = null
    open var contentTypeFormats:HashMap<String,String>? = null
    open var httpHandlers:HashMap<String,String>? = null
    open var plugins:PluginInfo? = null
    open var customPlugins:HashMap<String,CustomPluginInfo>? = null
    open var api:MetadataTypes? = null
    open var meta:HashMap<String,String>? = null
}

@DataContract
open class IdResponse
{
    @DataMember(Order=1)
    open var id:String? = null

    @DataMember(Order=2)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminGetRolesResponse
{
    @DataMember(Order=1)
    open var results:ArrayList<AdminRole>? = null

    @DataMember(Order=2)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminGetRoleResponse
{
    @DataMember(Order=1)
    open var result:AdminRole? = null

    @DataMember(Order=2)
    open var claims:ArrayList<Property>? = null

    @DataMember(Order=3)
    open var responseStatus:ResponseStatus? = null
}

open class AdminDashboardResponse
{
    open var serverStats:ServerStats? = null
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminApiKeysResponse
{
    @DataMember(Order=1)
    open var results:ArrayList<PartialApiKey>? = null

    @DataMember(Order=2)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminApiKeyResponse
{
    @DataMember(Order=1)
    open var result:String? = null

    @DataMember(Order=2)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class EmptyResponse
{
    @DataMember(Order=1)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class ChatResponse
{
    /**
    * A unique identifier for the chat completion.
    */
    @DataMember(Name="id")
    @SerializedName("id")
    open var id:String? = null

    /**
    * A list of chat completion choices. Can be more than one if n is greater than 1.
    */
    @DataMember(Name="choices")
    @SerializedName("choices")
    open var choices:ArrayList<Choice> = ArrayList<Choice>()

    /**
    * The Unix timestamp (in seconds) of when the chat completion was created.
    */
    @DataMember(Name="created")
    @SerializedName("created")
    open var created:Long? = null

    /**
    * The model used for the chat completion.
    */
    @DataMember(Name="model")
    @SerializedName("model")
    open var model:String? = null

    /**
    * This fingerprint represents the backend configuration that the model runs with.
    */
    @DataMember(Name="system_fingerprint")
    @SerializedName("system_fingerprint")
    open var systemFingerprint:String? = null

    /**
    * The object type, which is always chat.completion.
    */
    @DataMember(Name="object")
    @SerializedName("object")
    open var Object:String? = null

    /**
    * Specifies the processing type used for serving the request.
    */
    @DataMember(Name="service_tier")
    @SerializedName("service_tier")
    open var serviceTier:String? = null

    /**
    * Usage statistics for the completion request.
    */
    @DataMember(Name="usage")
    @SerializedName("usage")
    open var usage:AiUsage? = null

    /**
    * The provider used for the chat completion.
    */
    @DataMember(Name="provider")
    @SerializedName("provider")
    open var provider:String? = null

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    @DataMember(Name="metadata")
    @SerializedName("metadata")
    open var metadata:HashMap<String,String>? = null

    @DataMember(Name="responseStatus")
    @SerializedName("responseStatus")
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class QueryResponse<T>
{
    @DataMember(Order=1)
    open var offset:Int? = null

    @DataMember(Order=2)
    open var total:Int? = null

    @DataMember(Order=3)
    open var results:ArrayList<ChatCompletionLog> = ArrayList<ChatCompletionLog>()

    @DataMember(Order=4)
    open var meta:HashMap<String,String>? = null

    @DataMember(Order=5)
    open var responseStatus:ResponseStatus? = null
}

open class AdminMonthlyChatCompletionAnalyticsResponse
{
    open var month:String? = null
    open var availableMonths:ArrayList<String> = ArrayList<String>()
    open var modelStats:ArrayList<ChatCompletionStat> = ArrayList<ChatCompletionStat>()
    open var providerStats:ArrayList<ChatCompletionStat> = ArrayList<ChatCompletionStat>()
    open var dailyStats:ArrayList<ChatCompletionStat> = ArrayList<ChatCompletionStat>()
}

open class AdminDailyChatCompletionAnalyticsResponse
{
    open var modelStats:ArrayList<ChatCompletionStat> = ArrayList<ChatCompletionStat>()
    open var providerStats:ArrayList<ChatCompletionStat> = ArrayList<ChatCompletionStat>()
}

@DataContract
open class AuthenticateResponse : IHasSessionId, IHasBearerToken
{
    @DataMember(Order=1)
    open var userId:String? = null

    @DataMember(Order=2)
    open var sessionId:String? = null

    @DataMember(Order=3)
    open var userName:String? = null

    @DataMember(Order=4)
    open var displayName:String? = null

    @DataMember(Order=5)
    open var referrerUrl:String? = null

    @DataMember(Order=6)
    open var bearerToken:String? = null

    @DataMember(Order=7)
    open var refreshToken:String? = null

    @DataMember(Order=8)
    open var refreshTokenExpiry:Date? = null

    @DataMember(Order=9)
    open var profileUrl:String? = null

    @DataMember(Order=10)
    open var roles:ArrayList<String>? = null

    @DataMember(Order=11)
    open var permissions:ArrayList<String>? = null

    @DataMember(Order=12)
    open var authProvider:String? = null

    @DataMember(Order=13)
    open var responseStatus:ResponseStatus? = null

    @DataMember(Order=14)
    open var meta:HashMap<String,String>? = null
}

@DataContract
open class AssignRolesResponse
{
    @DataMember(Order=1)
    open var allRoles:ArrayList<String>? = null

    @DataMember(Order=2)
    open var allPermissions:ArrayList<String>? = null

    @DataMember(Order=3)
    open var meta:HashMap<String,String>? = null

    @DataMember(Order=4)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class UnAssignRolesResponse
{
    @DataMember(Order=1)
    open var allRoles:ArrayList<String>? = null

    @DataMember(Order=2)
    open var allPermissions:ArrayList<String>? = null

    @DataMember(Order=3)
    open var meta:HashMap<String,String>? = null

    @DataMember(Order=4)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminUserResponse
{
    @DataMember(Order=1)
    open var id:String? = null

    @DataMember(Order=2)
    open var result:HashMap<String,Object>? = null

    @DataMember(Order=3)
    open var details:ArrayList<HashMap<String,Object>>? = null

    @DataMember(Order=4)
    open var claims:ArrayList<Property>? = null

    @DataMember(Order=5)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminUsersResponse
{
    @DataMember(Order=1)
    open var results:ArrayList<HashMap<String,Object>>? = null

    @DataMember(Order=2)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class AdminDeleteUserResponse
{
    @DataMember(Order=1)
    open var id:String? = null

    @DataMember(Order=2)
    open var responseStatus:ResponseStatus? = null
}

open class AdminProfilingResponse
{
    open var results:ArrayList<DiagnosticEntry> = ArrayList<DiagnosticEntry>()
    open var total:Int? = null
    open var responseStatus:ResponseStatus? = null
}

open class AdminRedisResponse
{
    open var db:Long? = null
    open var searchResults:ArrayList<RedisSearchResult>? = null
    open var info:HashMap<String,String>? = null
    open var endpoint:RedisEndpointInfo? = null
    open var result:RedisText? = null
    open var responseStatus:ResponseStatus? = null
}

open class AdminDatabaseResponse
{
    open var results:ArrayList<HashMap<String,Object>> = ArrayList<HashMap<String,Object>>()
    open var total:Long? = null
    open var columns:ArrayList<MetadataPropertyType>? = null
    open var responseStatus:ResponseStatus? = null
}

open class ViewCommandsResponse
{
    open var commandTotals:ArrayList<CommandSummary> = ArrayList<CommandSummary>()
    open var latestCommands:ArrayList<CommandResult> = ArrayList<CommandResult>()
    open var latestFailed:ArrayList<CommandResult> = ArrayList<CommandResult>()
    open var responseStatus:ResponseStatus? = null
}

open class ExecuteCommandResponse
{
    open var commandResult:CommandResult? = null
    open var result:String? = null
    open var responseStatus:ResponseStatus? = null
}

open class AdminJobDashboardResponse
{
    open var commands:ArrayList<JobStatSummary> = ArrayList<JobStatSummary>()
    open var apis:ArrayList<JobStatSummary> = ArrayList<JobStatSummary>()
    open var workers:ArrayList<JobStatSummary> = ArrayList<JobStatSummary>()
    open var today:ArrayList<HourSummary> = ArrayList<HourSummary>()
    open var responseStatus:ResponseStatus? = null
}

open class AdminJobInfoResponse
{
    open var monthDbs:ArrayList<Date>? = null
    open var tableCounts:HashMap<String,Int>? = null
    open var workerStats:ArrayList<WorkerStats>? = null
    open var queueCounts:HashMap<String,Int>? = null
    open var workerCounts:HashMap<String,Int>? = null
    open var stateCounts:HashMap<BackgroundJobState,Int>? = null
    open var responseStatus:ResponseStatus? = null
}

open class AdminGetJobResponse
{
    open var result:JobSummary? = null
    open var queued:BackgroundJob? = null
    open var completed:CompletedJob? = null
    open var failed:FailedJob? = null
    open var responseStatus:ResponseStatus? = null
}

open class AdminGetJobProgressResponse
{
    open var state:BackgroundJobState? = null
    open var progress:Double? = null
    open var status:String? = null
    open var logs:String? = null
    open var durationMs:Int? = null
    open var error:ResponseStatus? = null
    open var responseStatus:ResponseStatus? = null
}

open class AdminRequeueFailedJobsJobsResponse
{
    open var results:ArrayList<Long> = ArrayList<Long>()
    open var errors:HashMap<Long,String> = HashMap<Long,String>()
    open var responseStatus:ResponseStatus? = null
}

open class AdminCancelJobsResponse
{
    open var results:ArrayList<Long> = ArrayList<Long>()
    open var errors:HashMap<Long,String> = HashMap<Long,String>()
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class RequestLogsResponse
{
    @DataMember(Order=1)
    open var results:ArrayList<RequestLogEntry>? = null

    @DataMember(Order=2)
    open var usage:HashMap<String,String>? = null

    @DataMember(Order=3)
    open var total:Int? = null

    @DataMember(Order=4)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class GetAnalyticsInfoResponse
{
    @DataMember(Order=1)
    open var months:ArrayList<String>? = null

    @DataMember(Order=2)
    open var result:AnalyticsLogInfo? = null

    @DataMember(Order=3)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class GetAnalyticsReportsResponse
{
    @DataMember(Order=1)
    open var result:AnalyticsReports? = null

    @DataMember(Order=2)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class GetValidationRulesResponse
{
    @DataMember(Order=1)
    open var results:ArrayList<ValidationRule>? = null

    @DataMember(Order=2)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class Property
{
    @DataMember(Order=1)
    open var name:String? = null

    @DataMember(Order=2)
    open var value:String? = null
}

/**
* A list of messages comprising the conversation so far.
*/
@DataContract
open class AiMessage
{
    /**
    * The contents of the message.
    */
    @DataMember(Name="content")
    @SerializedName("content")
    open var content:ArrayList<AiContent>? = null

    /**
    * The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.
    */
    @DataMember(Name="role")
    @SerializedName("role")
    open var role:String? = null

    /**
    * An optional name for the participant. Provides the model information to differentiate between participants of the same role.
    */
    @DataMember(Name="name")
    @SerializedName("name")
    open var name:String? = null

    /**
    * The tool calls generated by the model, such as function calls.
    */
    @DataMember(Name="tool_calls")
    @SerializedName("tool_calls")
    open var toolCalls:ArrayList<ToolCall>? = null

    /**
    * Tool call that this message is responding to.
    */
    @DataMember(Name="tool_call_id")
    @SerializedName("tool_call_id")
    open var toolCallId:String? = null
}

/**
* Parameters for audio output. Required when audio output is requested with modalities: [audio]
*/
@DataContract
open class AiChatAudio
{
    /**
    * Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.
    */
    @DataMember(Name="format")
    @SerializedName("format")
    open var format:String? = null

    /**
    * The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.
    */
    @DataMember(Name="voice")
    @SerializedName("voice")
    open var voice:String? = null
}

@DataContract
open class AiResponseFormat
{
    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.
    */
    @DataMember(Name="response_format")
    @SerializedName("response_format")
    open var Type:ResponseFormat? = null
}

@DataContract
open class Tool
{
    /**
    * The type of the tool. Currently, only function is supported.
    */
    @DataMember(Name="type")
    @SerializedName("type")
    open var Type:ToolType? = null
}

open class QueryDb<T> : QueryBase()
{
}

open class ChatCompletionLog
{
    open var id:Long? = null
    open var refId:String? = null
    open var userId:String? = null
    open var apiKey:String? = null
    open var model:String? = null
    open var provider:String? = null
    open var userPrompt:String? = null
    open var answer:String? = null
    @StringLength(MaximumLength=2147483647)
    open var requestBody:String? = null

    @StringLength(MaximumLength=2147483647)
    open var responseBody:String? = null

    open var errorCode:String? = null
    open var error:ResponseStatus? = null
    open var createdDate:Date? = null
    open var tag:String? = null
    open var durationMs:Int? = null
    open var promptTokens:Int? = null
    open var completionTokens:Int? = null
    open var cost:BigDecimal? = null
    open var providerRef:String? = null
    open var providerModel:String? = null
    open var finishReason:String? = null
    open var usage:ModelUsage? = null
    open var threadId:String? = null
    open var title:String? = null
    open var meta:HashMap<String,String>? = null
}

@DataContract
open class AdminUserBase
{
    @DataMember(Order=1)
    open var userName:String? = null

    @DataMember(Order=2)
    open var firstName:String? = null

    @DataMember(Order=3)
    open var lastName:String? = null

    @DataMember(Order=4)
    open var displayName:String? = null

    @DataMember(Order=5)
    open var email:String? = null

    @DataMember(Order=6)
    open var password:String? = null

    @DataMember(Order=7)
    open var profileUrl:String? = null

    @DataMember(Order=8)
    open var phoneNumber:String? = null

    @DataMember(Order=9)
    open var userAuthProperties:HashMap<String,String>? = null

    @DataMember(Order=10)
    open var meta:HashMap<String,String>? = null
}

open class RequestLog
{
    open var id:Long? = null
    open var traceId:String? = null
    open var operationName:String? = null
    open var dateTime:Date? = null
    open var statusCode:Int? = null
    open var statusDescription:String? = null
    open var httpMethod:String? = null
    open var absoluteUri:String? = null
    open var pathInfo:String? = null
    open var request:String? = null
    @StringLength(MaximumLength=2147483647)
    open var requestBody:String? = null

    open var userAuthId:String? = null
    open var sessionId:String? = null
    open var ipAddress:String? = null
    open var forwardedFor:String? = null
    open var referer:String? = null
    open var headers:HashMap<String,String> = HashMap<String,String>()
    open var formData:HashMap<String,String>? = null
    open var items:HashMap<String,String> = HashMap<String,String>()
    open var responseHeaders:HashMap<String,String>? = null
    open var response:String? = null
    @StringLength(MaximumLength=2147483647)
    open var responseBody:String? = null

    @StringLength(MaximumLength=2147483647)
    open var sessionBody:String? = null

    open var error:ResponseStatus? = null
    open var exceptionSource:String? = null
    open var exceptionDataBody:String? = null
    open var requestDuration:TimeSpan? = null
    open var meta:HashMap<String,String>? = null
}

open class RedisEndpointInfo
{
    open var host:String? = null
    open var port:Int? = null
    open var ssl:Boolean? = null
    open var db:Long? = null
    open var username:String? = null
    open var password:String? = null
}

open class BackgroundJob : BackgroundJobBase()
{
    override var id:Long? = null
}

open class JobSummary
{
    open var id:Long? = null
    open var parentId:Long? = null
    open var refId:String? = null
    open var worker:String? = null
    open var tag:String? = null
    open var batchId:String? = null
    open var createdDate:Date? = null
    open var createdBy:String? = null
    open var requestType:String? = null
    open var command:String? = null
    open var request:String? = null
    open var response:String? = null
    open var userId:String? = null
    open var callback:String? = null
    open var startedDate:Date? = null
    open var completedDate:Date? = null
    open var state:BackgroundJobState? = null
    open var durationMs:Int? = null
    open var attempts:Int? = null
    open var errorCode:String? = null
    open var errorMessage:String? = null
}

open class ScheduledTask
{
    open var id:Long? = null
    open var name:String? = null
    open var interval:TimeSpan? = null
    open var cronExpression:String? = null
    open var requestType:String? = null
    open var command:String? = null
    open var request:String? = null
    open var requestBody:String? = null
    open var options:BackgroundJobOptions? = null
    open var lastRun:Date? = null
    open var lastJobId:Long? = null
}

open class CompletedJob : BackgroundJobBase()
{
}

open class FailedJob : BackgroundJobBase()
{
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

open class ValidationRule : ValidateRule()
{
    open var id:Int? = null
    @Required()
    @SerializedName("type") open var Type:String? = null

    open var field:String? = null
    open var createdBy:String? = null
    open var createdDate:Date? = null
    open var modifiedBy:String? = null
    open var modifiedDate:Date? = null
    open var suspendedBy:String? = null
    open var suspendedDate:Date? = null
    open var notes:String? = null
}

open class AppInfo
{
    open var baseUrl:String? = null
    open var serviceStackVersion:String? = null
    open var serviceName:String? = null
    open var apiVersion:String? = null
    open var serviceDescription:String? = null
    open var serviceIconUrl:String? = null
    open var brandUrl:String? = null
    open var brandImageUrl:String? = null
    open var textColor:String? = null
    open var linkColor:String? = null
    open var backgroundColor:String? = null
    open var backgroundImageUrl:String? = null
    open var iconUrl:String? = null
    open var jsTextCase:String? = null
    open var useSystemJson:String? = null
    open var endpointRouting:ArrayList<String>? = null
    open var meta:HashMap<String,String>? = null
}

open class UiInfo
{
    open var brandIcon:ImageInfo? = null
    open var userIcon:ImageInfo? = null
    open var hideTags:ArrayList<String>? = null
    open var modules:ArrayList<String>? = null
    open var alwaysHideTags:ArrayList<String>? = null
    open var adminLinks:ArrayList<LinkInfo>? = null
    open var adminLinksOrder:ArrayList<String>? = null
    open var theme:ThemeInfo? = null
    open var locode:LocodeUi? = null
    open var explorer:ExplorerUi? = null
    open var admin:AdminUi? = null
    open var defaultFormats:ApiFormat? = null
    open var meta:HashMap<String,String>? = null
}

open class ConfigInfo
{
    open var debugMode:Boolean? = null
    open var meta:HashMap<String,String>? = null
}

open class PluginInfo
{
    open var loaded:ArrayList<String>? = null
    open var auth:AuthInfo? = null
    open var apiKey:ApiKeyInfo? = null
    open var commands:CommandsInfo? = null
    open var autoQuery:AutoQueryInfo? = null
    open var validation:ValidationInfo? = null
    open var sharpPages:SharpPagesInfo? = null
    open var requestLogs:RequestLogsInfo? = null
    open var profiling:ProfilingInfo? = null
    open var filesUpload:FilesUploadInfo? = null
    open var adminUsers:AdminUsersInfo? = null
    open var adminIdentityUsers:AdminIdentityUsersInfo? = null
    open var adminRedis:AdminRedisInfo? = null
    open var adminDatabase:AdminDatabaseInfo? = null
    open var meta:HashMap<String,String>? = null
}

open class CustomPluginInfo
{
    open var accessRole:String? = null
    open var serviceRoutes:HashMap<String,ArrayList<String>>? = null
    open var enabled:ArrayList<String>? = null
    open var meta:HashMap<String,String>? = null
}

open class MetadataTypes
{
    open var config:MetadataTypesConfig? = null
    open var namespaces:ArrayList<String>? = null
    open var types:ArrayList<MetadataType>? = null
    open var operations:ArrayList<MetadataOperationType>? = null
}

@DataContract
open class AdminRole
{
}

open class ServerStats
{
    open var redis:HashMap<String,Long>? = null
    open var serverEvents:HashMap<String,String>? = null
    open var mqDescription:String? = null
    open var mqWorkers:HashMap<String,Long>? = null
}

@DataContract
open class PartialApiKey
{
    @DataMember(Order=1)
    open var id:Int? = null

    @DataMember(Order=2)
    open var name:String? = null

    @DataMember(Order=3)
    open var userId:String? = null

    @DataMember(Order=4)
    open var userName:String? = null

    @DataMember(Order=5)
    open var visibleKey:String? = null

    @DataMember(Order=6)
    open var environment:String? = null

    @DataMember(Order=7)
    open var createdDate:Date? = null

    @DataMember(Order=8)
    open var expiryDate:Date? = null

    @DataMember(Order=9)
    open var cancelledDate:Date? = null

    @DataMember(Order=10)
    open var lastUsedDate:Date? = null

    @DataMember(Order=11)
    open var scopes:ArrayList<String>? = null

    @DataMember(Order=12)
    open var features:ArrayList<String>? = null

    @DataMember(Order=13)
    open var restrictTo:ArrayList<String>? = null

    @DataMember(Order=14)
    open var notes:String? = null

    @DataMember(Order=15)
    open var refId:Int? = null

    @DataMember(Order=16)
    open var refIdStr:String? = null

    @DataMember(Order=17)
    open var meta:HashMap<String,String>? = null

    @DataMember(Order=18)
    open var active:Boolean? = null
}

@DataContract
open class Choice
{
    /**
    * The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool
    */
    @DataMember(Name="finish_reason")
    @SerializedName("finish_reason")
    open var finishReason:String? = null

    /**
    * The index of the choice in the list of choices.
    */
    @DataMember(Name="index")
    @SerializedName("index")
    open var index:Int? = null

    /**
    * A chat completion message generated by the model.
    */
    @DataMember(Name="message")
    @SerializedName("message")
    open var message:ChoiceMessage? = null
}

/**
* Usage statistics for the completion request.
*/
@DataContract
open class AiUsage
{
    /**
    * Number of tokens in the generated completion.
    */
    @DataMember(Name="completion_tokens")
    @SerializedName("completion_tokens")
    open var completionTokens:Int? = null

    /**
    * Number of tokens in the prompt.
    */
    @DataMember(Name="prompt_tokens")
    @SerializedName("prompt_tokens")
    open var promptTokens:Int? = null

    /**
    * Total number of tokens used in the request (prompt + completion).
    */
    @DataMember(Name="total_tokens")
    @SerializedName("total_tokens")
    open var totalTokens:Int? = null

    /**
    * Breakdown of tokens used in a completion.
    */
    @DataMember(Name="completion_tokens_details")
    @SerializedName("completion_tokens_details")
    open var completionTokensDetails:AiCompletionUsage? = null

    /**
    * Breakdown of tokens used in the prompt.
    */
    @DataMember(Name="prompt_tokens_details")
    @SerializedName("prompt_tokens_details")
    open var promptTokensDetails:AiPromptUsage? = null
}

open class ChatCompletionStat
{
    open var name:String? = null
    open var requests:Int? = null
    open var inputTokens:Int? = null
    open var outputTokens:Int? = null
    open var cost:BigDecimal? = null
}

open class DiagnosticEntry
{
    open var id:Long? = null
    open var traceId:String? = null
    open var source:String? = null
    open var eventType:String? = null
    open var message:String? = null
    open var operation:String? = null
    open var threadId:Int? = null
    open var error:ResponseStatus? = null
    open var commandType:String? = null
    open var command:String? = null
    open var userAuthId:String? = null
    open var sessionId:String? = null
    open var arg:String? = null
    open var args:ArrayList<String>? = null
    open var argLengths:ArrayList<Long>? = null
    open var namedArgs:HashMap<String,Object>? = null
    open var duration:TimeSpan? = null
    open var timestamp:Long? = null
    open var date:Date? = null
    open var tag:String? = null
    open var stackTrace:String? = null
    open var meta:HashMap<String,String> = HashMap<String,String>()
}

open class RedisSearchResult
{
    open var id:String? = null
    @SerializedName("type") open var Type:String? = null
    open var ttl:Long? = null
    open var size:Long? = null
}

open class RedisText
{
    open var text:String? = null
    open var children:ArrayList<RedisText>? = null
}

open class MetadataPropertyType
{
    open var name:String? = null
    @SerializedName("type") open var Type:String? = null
    open var namespace:String? = null
    open var isValueType:Boolean? = null
    open var isEnum:Boolean? = null
    open var isPrimaryKey:Boolean? = null
    open var genericArgs:ArrayList<String>? = null
    open var value:String? = null
    open var description:String? = null
    open var dataMember:MetadataDataMember? = null
    open var readOnly:Boolean? = null
    open var paramType:String? = null
    open var displayType:String? = null
    open var isRequired:Boolean? = null
    open var allowableValues:ArrayList<String>? = null
    open var allowableMin:Int? = null
    open var allowableMax:Int? = null
    open var attributes:ArrayList<MetadataAttribute>? = null
    open var uploadTo:String? = null
    open var input:InputInfo? = null
    open var format:FormatInfo? = null
    open var ref:RefInfo? = null
}

open class CommandSummary
{
    @SerializedName("type") open var Type:String? = null
    open var name:String? = null
    open var count:Int? = null
    open var failed:Int? = null
    open var retries:Int? = null
    open var totalMs:Int? = null
    open var minMs:Int? = null
    open var maxMs:Int? = null
    open var averageMs:Double? = null
    open var medianMs:Double? = null
    open var lastError:ResponseStatus? = null
    open var timings:ConcurrentQueue<Int>? = null
}

open class CommandResult
{
    @SerializedName("type") open var Type:String? = null
    open var name:String? = null
    open var ms:Long? = null
    open var at:Date? = null
    open var request:String? = null
    open var retries:Int? = null
    open var attempt:Int? = null
    open var error:ResponseStatus? = null
}

open class JobStatSummary
{
    open var name:String? = null
    open var total:Int? = null
    open var completed:Int? = null
    open var retries:Int? = null
    open var failed:Int? = null
    open var cancelled:Int? = null
}

open class HourSummary
{
    open var hour:String? = null
    open var total:Int? = null
    open var completed:Int? = null
    open var failed:Int? = null
    open var cancelled:Int? = null
}

open class WorkerStats
{
    open var name:String? = null
    open var queued:Long? = null
    open var received:Long? = null
    open var completed:Long? = null
    open var retries:Long? = null
    open var failed:Long? = null
    open var runningJob:Long? = null
    open var runningTime:TimeSpan? = null
}

open class RequestLogEntry
{
    open var id:Long? = null
    open var traceId:String? = null
    open var operationName:String? = null
    open var dateTime:Date? = null
    open var statusCode:Int? = null
    open var statusDescription:String? = null
    open var httpMethod:String? = null
    open var absoluteUri:String? = null
    open var pathInfo:String? = null
    @StringLength(MaximumLength=2147483647)
    open var requestBody:String? = null

    open var requestDto:Object? = null
    open var userAuthId:String? = null
    open var sessionId:String? = null
    open var ipAddress:String? = null
    open var forwardedFor:String? = null
    open var referer:String? = null
    open var headers:HashMap<String,String>? = null
    open var formData:HashMap<String,String>? = null
    open var items:HashMap<String,String>? = null
    open var responseHeaders:HashMap<String,String>? = null
    open var session:Object? = null
    open var responseDto:Object? = null
    open var errorResponse:Object? = null
    open var exceptionSource:String? = null
    open var exceptionData:HashMap<String,Object>? = null
    open var requestDuration:TimeSpan? = null
    open var meta:HashMap<String,String>? = null
}

@DataContract
open class AnalyticsLogInfo
{
    @DataMember(Order=1)
    open var id:Long? = null

    @DataMember(Order=2)
    open var dateTime:Date? = null

    @DataMember(Order=3)
    open var browser:String? = null

    @DataMember(Order=4)
    open var device:String? = null

    @DataMember(Order=5)
    open var bot:String? = null

    @DataMember(Order=6)
    open var op:String? = null

    @DataMember(Order=7)
    open var userId:String? = null

    @DataMember(Order=8)
    open var userName:String? = null

    @DataMember(Order=9)
    open var apiKey:String? = null

    @DataMember(Order=10)
    open var ip:String? = null
}

@DataContract
open class AnalyticsReports
{
    @DataMember(Order=1)
    open var id:Long? = null

    @DataMember(Order=2)
    open var created:Date? = null

    @DataMember(Order=3)
    open var version:BigDecimal? = null

    @DataMember(Order=4)
    open var apis:HashMap<String,RequestSummary>? = null

    @DataMember(Order=5)
    open var users:HashMap<String,RequestSummary>? = null

    @DataMember(Order=6)
    open var tags:HashMap<String,RequestSummary>? = null

    @DataMember(Order=7)
    open var status:HashMap<String,RequestSummary>? = null

    @DataMember(Order=8)
    open var days:HashMap<String,RequestSummary>? = null

    @DataMember(Order=9)
    open var apiKeys:HashMap<String,RequestSummary>? = null

    @DataMember(Order=10)
    open var ips:HashMap<String,RequestSummary>? = null

    @DataMember(Order=11)
    open var browsers:HashMap<String,RequestSummary>? = null

    @DataMember(Order=12)
    open var devices:HashMap<String,RequestSummary>? = null

    @DataMember(Order=13)
    open var bots:HashMap<String,RequestSummary>? = null

    @DataMember(Order=14)
    open var durations:HashMap<String,Long>? = null
}

@DataContract
open class AiContent
{
    /**
    * The type of the content part.
    */
    @DataMember(Name="type")
    @SerializedName("type")
    open var Type:String? = null
}

/**
* The tool calls generated by the model, such as function calls.
*/
@DataContract
open class ToolCall
{
    /**
    * The ID of the tool call.
    */
    @DataMember(Name="id")
    @SerializedName("id")
    open var id:String? = null

    /**
    * The type of the tool. Currently, only `function` is supported.
    */
    @DataMember(Name="type")
    @SerializedName("type")
    open var Type:String? = null

    /**
    * The function that the model called.
    */
    @DataMember(Name="function")
    @SerializedName("function")
    open var function:String? = null
}

enum class ResponseFormat
{
    Text,
    JsonObject,
}

enum class ToolType
{
    Function,
}

@DataContract
open class QueryBase
{
    @DataMember(Order=1)
    open var skip:Int? = null

    @DataMember(Order=2)
    open var take:Int? = null

    @DataMember(Order=3)
    open var orderBy:String? = null

    @DataMember(Order=4)
    open var orderByDesc:String? = null

    @DataMember(Order=5)
    open var include:String? = null

    @DataMember(Order=6)
    open var fields:String? = null

    @DataMember(Order=7)
    open var meta:HashMap<String,String>? = null
}

@DataContract
open class ModelUsage
{
    @DataMember
    open var cost:String? = null

    @DataMember
    open var input:String? = null

    @DataMember
    open var output:String? = null

    @DataMember
    open var duration:Int? = null

    @DataMember(Name="completion_tokens")
    @SerializedName("completion_tokens")
    open var completionTokens:Int? = null

    @DataMember
    open var inputCachedTokens:Int? = null

    @DataMember
    open var outputCachedTokens:Int? = null

    @DataMember(Name="audio_tokens")
    @SerializedName("audio_tokens")
    open var audioTokens:Int? = null

    @DataMember(Name="reasoning_tokens")
    @SerializedName("reasoning_tokens")
    open var reasoningTokens:Int? = null

    @DataMember
    open var totalTokens:Int? = null
}

open class BackgroundJobBase
{
    open var id:Long? = null
    open var parentId:Long? = null
    open var refId:String? = null
    open var worker:String? = null
    open var tag:String? = null
    open var batchId:String? = null
    open var callback:String? = null
    open var dependsOn:Long? = null
    open var runAfter:Date? = null
    open var createdDate:Date? = null
    open var createdBy:String? = null
    open var requestId:String? = null
    open var requestType:String? = null
    open var command:String? = null
    open var request:String? = null
    @StringLength(MaximumLength=2147483647)
    open var requestBody:String? = null

    open var userId:String? = null
    open var response:String? = null
    @StringLength(MaximumLength=2147483647)
    open var responseBody:String? = null

    open var state:BackgroundJobState? = null
    open var startedDate:Date? = null
    open var completedDate:Date? = null
    open var notifiedDate:Date? = null
    open var retryLimit:Int? = null
    open var attempts:Int? = null
    open var durationMs:Int? = null
    open var timeoutSecs:Int? = null
    open var progress:Double? = null
    open var status:String? = null
    @StringLength(MaximumLength=2147483647)
    open var logs:String? = null

    open var lastActivityDate:Date? = null
    open var replyTo:String? = null
    open var errorCode:String? = null
    open var error:ResponseStatus? = null
    open var args:HashMap<String,String>? = null
    open var meta:HashMap<String,String>? = null
}

open class BackgroundJobOptions
{
    open var refId:String? = null
    open var parentId:Long? = null
    open var worker:String? = null
    open var runAfter:Date? = null
    open var callback:String? = null
    open var dependsOn:Long? = null
    open var userId:String? = null
    open var retryLimit:Int? = null
    open var replyTo:String? = null
    open var tag:String? = null
    open var batchId:String? = null
    open var createdBy:String? = null
    open var timeoutSecs:Int? = null
    open var timeout:TimeSpan? = null
    open var args:HashMap<String,String>? = null
    open var runCommand:Boolean? = null
}

open class ValidateRule
{
    open var validator:String? = null
    open var condition:String? = null
    open var errorCode:String? = null
    open var message:String? = null
}

open class ImageInfo
{
    open var svg:String? = null
    open var uri:String? = null
    open var alt:String? = null
    open var cls:String? = null
}

open class LinkInfo
{
    open var id:String? = null
    open var href:String? = null
    open var label:String? = null
    open var icon:ImageInfo? = null
    open var show:String? = null
    open var hide:String? = null
}

open class ThemeInfo
{
    open var form:String? = null
    open var modelIcon:ImageInfo? = null
}

open class LocodeUi
{
    open var css:ApiCss? = null
    open var tags:AppTags? = null
    open var maxFieldLength:Int? = null
    open var maxNestedFields:Int? = null
    open var maxNestedFieldLength:Int? = null
}

open class ExplorerUi
{
    open var css:ApiCss? = null
    open var tags:AppTags? = null
    open var jsConfig:String? = null
}

open class AdminUi
{
    open var css:ApiCss? = null
    open var pages:ArrayList<PageInfo>? = null
}

open class ApiFormat
{
    open var locale:String? = null
    open var assumeUtc:Boolean? = null
    open var number:FormatInfo? = null
    open var date:FormatInfo? = null
}

open class AuthInfo
{
    open var hasAuthSecret:Boolean? = null
    open var hasAuthRepository:Boolean? = null
    open var includesRoles:Boolean? = null
    open var includesOAuthTokens:Boolean? = null
    open var htmlRedirect:String? = null
    open var authProviders:ArrayList<MetaAuthProvider>? = null
    open var identityAuth:IdentityAuthInfo? = null
    open var roleLinks:HashMap<String,ArrayList<LinkInfo>>? = null
    open var serviceRoutes:HashMap<String,ArrayList<String>>? = null
    open var meta:HashMap<String,String>? = null
}

open class ApiKeyInfo
{
    open var label:String? = null
    open var httpHeader:String? = null
    open var scopes:ArrayList<String>? = null
    open var features:ArrayList<String>? = null
    open var requestTypes:ArrayList<String>? = null
    open var expiresIn:ArrayList<KeyValuePair<String,String>>? = null
    open var hide:ArrayList<String>? = null
    open var meta:HashMap<String,String>? = null
}

open class CommandsInfo
{
    open var commands:ArrayList<CommandInfo>? = null
    open var meta:HashMap<String,String>? = null
}

open class AutoQueryInfo
{
    open var maxLimit:Int? = null
    open var untypedQueries:Boolean? = null
    open var rawSqlFilters:Boolean? = null
    open var autoQueryViewer:Boolean? = null
    open var async:Boolean? = null
    open var orderByPrimaryKey:Boolean? = null
    open var crudEvents:Boolean? = null
    open var crudEventsServices:Boolean? = null
    open var accessRole:String? = null
    open var namedConnection:String? = null
    open var viewerConventions:ArrayList<AutoQueryConvention>? = null
    open var meta:HashMap<String,String>? = null
}

open class ValidationInfo
{
    open var hasValidationSource:Boolean? = null
    open var hasValidationSourceAdmin:Boolean? = null
    open var serviceRoutes:HashMap<String,ArrayList<String>>? = null
    open var typeValidators:ArrayList<ScriptMethodType>? = null
    open var propertyValidators:ArrayList<ScriptMethodType>? = null
    open var accessRole:String? = null
    open var meta:HashMap<String,String>? = null
}

open class SharpPagesInfo
{
    open var apiPath:String? = null
    open var scriptAdminRole:String? = null
    open var metadataDebugAdminRole:String? = null
    open var metadataDebug:Boolean? = null
    open var spaFallback:Boolean? = null
    open var meta:HashMap<String,String>? = null
}

open class RequestLogsInfo
{
    open var accessRole:String? = null
    open var requestLogger:String? = null
    open var defaultLimit:Int? = null
    open var serviceRoutes:HashMap<String,ArrayList<String>>? = null
    open var analytics:RequestLogsAnalytics? = null
    open var meta:HashMap<String,String>? = null
}

open class ProfilingInfo
{
    open var accessRole:String? = null
    open var defaultLimit:Int? = null
    open var summaryFields:ArrayList<String>? = null
    open var tagLabel:String? = null
    open var meta:HashMap<String,String>? = null
}

open class FilesUploadInfo
{
    open var basePath:String? = null
    open var locations:ArrayList<FilesUploadLocation>? = null
    open var meta:HashMap<String,String>? = null
}

open class AdminUsersInfo
{
    open var accessRole:String? = null
    open var enabled:ArrayList<String>? = null
    open var userAuth:MetadataType? = null
    open var allRoles:ArrayList<String>? = null
    open var allPermissions:ArrayList<String>? = null
    open var queryUserAuthProperties:ArrayList<String>? = null
    open var queryMediaRules:ArrayList<MediaRule>? = null
    open var formLayout:ArrayList<InputInfo>? = null
    open var css:ApiCss? = null
    open var meta:HashMap<String,String>? = null
}

open class AdminIdentityUsersInfo
{
    open var accessRole:String? = null
    open var enabled:ArrayList<String>? = null
    open var identityUser:MetadataType? = null
    open var allRoles:ArrayList<String>? = null
    open var allPermissions:ArrayList<String>? = null
    open var queryIdentityUserProperties:ArrayList<String>? = null
    open var queryMediaRules:ArrayList<MediaRule>? = null
    open var formLayout:ArrayList<InputInfo>? = null
    open var css:ApiCss? = null
    open var meta:HashMap<String,String>? = null
}

open class AdminRedisInfo
{
    open var queryLimit:Int? = null
    open var databases:ArrayList<Int>? = null
    open var modifiableConnection:Boolean? = null
    open var endpoint:RedisEndpointInfo? = null
    open var meta:HashMap<String,String>? = null
}

open class AdminDatabaseInfo
{
    open var queryLimit:Int? = null
    open var databases:ArrayList<DatabaseInfo>? = null
    open var meta:HashMap<String,String>? = null
}

open class MetadataTypesConfig
{
    open var baseUrl:String? = null
    open var makePartial:Boolean? = null
    open var makeVirtual:Boolean? = null
    open var makeInternal:Boolean? = null
    open var baseClass:String? = null
    @SerializedName("package") open var Package:String? = null
    open var addReturnMarker:Boolean? = null
    open var addDescriptionAsComments:Boolean? = null
    open var addDocAnnotations:Boolean? = null
    open var addDataContractAttributes:Boolean? = null
    open var addIndexesToDataMembers:Boolean? = null
    open var addGeneratedCodeAttributes:Boolean? = null
    open var addImplicitVersion:Int? = null
    open var addResponseStatus:Boolean? = null
    open var addServiceStackTypes:Boolean? = null
    open var addModelExtensions:Boolean? = null
    open var addPropertyAccessors:Boolean? = null
    open var excludeGenericBaseTypes:Boolean? = null
    open var settersReturnThis:Boolean? = null
    open var addNullableAnnotations:Boolean? = null
    open var makePropertiesOptional:Boolean? = null
    open var exportAsTypes:Boolean? = null
    open var excludeImplementedInterfaces:Boolean? = null
    open var addDefaultXmlNamespace:String? = null
    open var makeDataContractsExtensible:Boolean? = null
    open var initializeCollections:Boolean? = null
    open var addNamespaces:ArrayList<String>? = null
    open var defaultNamespaces:ArrayList<String>? = null
    open var defaultImports:ArrayList<String>? = null
    open var includeTypes:ArrayList<String>? = null
    open var excludeTypes:ArrayList<String>? = null
    open var exportTags:ArrayList<String>? = null
    open var treatTypesAsStrings:ArrayList<String>? = null
    open var exportValueTypes:Boolean? = null
    open var globalNamespace:String? = null
    open var excludeNamespace:Boolean? = null
    open var dataClass:String? = null
    open var dataClassJson:String? = null
    open var ignoreTypes:ArrayList<Class>? = null
    open var exportTypes:ArrayList<Class>? = null
    open var exportAttributes:ArrayList<Class>? = null
    open var ignoreTypesInNamespaces:ArrayList<String>? = null
}

open class MetadataType
{
    open var name:String? = null
    open var namespace:String? = null
    open var genericArgs:ArrayList<String>? = null
    open var inherits:MetadataTypeName? = null
    @SerializedName("implements") open var Implements:ArrayList<MetadataTypeName>? = null
    open var displayType:String? = null
    open var description:String? = null
    open var notes:String? = null
    open var icon:ImageInfo? = null
    open var isNested:Boolean? = null
    open var isEnum:Boolean? = null
    open var isEnumInt:Boolean? = null
    open var isInterface:Boolean? = null
    open var isAbstract:Boolean? = null
    open var isGenericTypeDef:Boolean? = null
    open var dataContract:MetadataDataContract? = null
    open var properties:ArrayList<MetadataPropertyType>? = null
    open var attributes:ArrayList<MetadataAttribute>? = null
    open var innerTypes:ArrayList<MetadataTypeName>? = null
    open var enumNames:ArrayList<String>? = null
    open var enumValues:ArrayList<String>? = null
    open var enumMemberValues:ArrayList<String>? = null
    open var enumDescriptions:ArrayList<String>? = null
    open var meta:HashMap<String,String>? = null
}

open class MetadataOperationType
{
    open var request:MetadataType? = null
    open var response:MetadataType? = null
    open var actions:ArrayList<String>? = null
    open var returnsVoid:Boolean? = null
    open var method:String? = null
    open var returnType:MetadataTypeName? = null
    open var routes:ArrayList<MetadataRoute>? = null
    open var dataModel:MetadataTypeName? = null
    open var viewModel:MetadataTypeName? = null
    open var requiresAuth:Boolean? = null
    open var requiresApiKey:Boolean? = null
    open var requiredRoles:ArrayList<String>? = null
    open var requiresAnyRole:ArrayList<String>? = null
    open var requiredPermissions:ArrayList<String>? = null
    open var requiresAnyPermission:ArrayList<String>? = null
    open var tags:ArrayList<String>? = null
    open var ui:ApiUiInfo? = null
}

@DataContract
open class ChoiceMessage
{
    /**
    * The contents of the message.
    */
    @DataMember(Name="content")
    @SerializedName("content")
    open var content:String? = null

    /**
    * The refusal message generated by the model.
    */
    @DataMember(Name="refusal")
    @SerializedName("refusal")
    open var refusal:String? = null

    /**
    * The reasoning process used by the model.
    */
    @DataMember(Name="reasoning")
    @SerializedName("reasoning")
    open var reasoning:String? = null

    /**
    * The role of the author of this message.
    */
    @DataMember(Name="role")
    @SerializedName("role")
    open var role:String? = null

    /**
    * Annotations for the message, when applicable, as when using the web search tool.
    */
    @DataMember(Name="annotations")
    @SerializedName("annotations")
    open var annotations:ArrayList<ChoiceAnnotation>? = null

    /**
    * If the audio output modality is requested, this object contains data about the audio response from the model.
    */
    @DataMember(Name="audio")
    @SerializedName("audio")
    open var audio:ChoiceAudio? = null

    /**
    * The tool calls generated by the model, such as function calls.
    */
    @DataMember(Name="tool_calls")
    @SerializedName("tool_calls")
    open var toolCalls:ArrayList<ToolCall>? = null
}

/**
* Usage statistics for the completion request.
*/
@DataContract
open class AiCompletionUsage
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    @DataMember(Name="accepted_prediction_tokens")
    @SerializedName("accepted_prediction_tokens")
    open var acceptedPredictionTokens:Int? = null

    /**
    * Audio input tokens generated by the model.
    */
    @DataMember(Name="audio_tokens")
    @SerializedName("audio_tokens")
    open var audioTokens:Int? = null

    /**
    * Tokens generated by the model for reasoning.
    */
    @DataMember(Name="reasoning_tokens")
    @SerializedName("reasoning_tokens")
    open var reasoningTokens:Int? = null

    /**
    * When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.
    */
    @DataMember(Name="rejected_prediction_tokens")
    @SerializedName("rejected_prediction_tokens")
    open var rejectedPredictionTokens:Int? = null
}

/**
* Breakdown of tokens used in the prompt.
*/
@DataContract
open class AiPromptUsage
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    @DataMember(Name="accepted_prediction_tokens")
    @SerializedName("accepted_prediction_tokens")
    open var acceptedPredictionTokens:Int? = null

    /**
    * Audio input tokens present in the prompt.
    */
    @DataMember(Name="audio_tokens")
    @SerializedName("audio_tokens")
    open var audioTokens:Int? = null

    /**
    * Cached tokens present in the prompt.
    */
    @DataMember(Name="cached_tokens")
    @SerializedName("cached_tokens")
    open var cachedTokens:Int? = null
}

open class MetadataDataMember
{
    open var name:String? = null
    open var order:Int? = null
    open var isRequired:Boolean? = null
    open var emitDefaultValue:Boolean? = null
}

open class MetadataAttribute
{
    open var name:String? = null
    open var constructorArgs:ArrayList<MetadataPropertyType>? = null
    open var args:ArrayList<MetadataPropertyType>? = null
}

open class InputInfo
{
    open var id:String? = null
    open var name:String? = null
    @SerializedName("type") open var Type:String? = null
    open var value:String? = null
    open var placeholder:String? = null
    open var help:String? = null
    open var label:String? = null
    open var title:String? = null
    open var size:String? = null
    open var pattern:String? = null
    open var readOnly:Boolean? = null
    open var required:Boolean? = null
    open var disabled:Boolean? = null
    open var autocomplete:String? = null
    open var autofocus:String? = null
    open var min:String? = null
    open var max:String? = null
    open var step:String? = null
    open var minLength:Int? = null
    open var maxLength:Int? = null
    open var accept:String? = null
    open var capture:String? = null
    open var multiple:Boolean? = null
    open var allowableValues:ArrayList<String>? = null
    open var allowableEntries:ArrayList<KeyValuePair<String, String>>? = null
    open var options:String? = null
    open var ignore:Boolean? = null
    open var css:FieldCss? = null
    open var meta:HashMap<String,String>? = null
}

open class FormatInfo
{
    open var method:String? = null
    open var options:String? = null
    open var locale:String? = null
}

open class RefInfo
{
    open var model:String? = null
    open var selfId:String? = null
    open var refId:String? = null
    open var refLabel:String? = null
    open var queryApi:String? = null
}

@DataContract
open class RequestSummary
{
    @DataMember(Order=1)
    open var name:String? = null

    @DataMember(Order=2)
    open var totalRequests:Long? = null

    @DataMember(Order=3)
    open var totalRequestLength:Long? = null

    @DataMember(Order=4)
    open var minRequestLength:Long? = null

    @DataMember(Order=5)
    open var maxRequestLength:Long? = null

    @DataMember(Order=6)
    open var totalDuration:Double? = null

    @DataMember(Order=7)
    open var minDuration:Double? = null

    @DataMember(Order=8)
    open var maxDuration:Double? = null

    @DataMember(Order=9)
    open var status:HashMap<Int,Long>? = null

    @DataMember(Order=10)
    open var durations:HashMap<String,Long>? = null

    @DataMember(Order=11)
    open var apis:HashMap<String,Long>? = null

    @DataMember(Order=12)
    open var users:HashMap<String,Long>? = null

    @DataMember(Order=13)
    open var ips:HashMap<String,Long>? = null

    @DataMember(Order=14)
    open var apiKeys:HashMap<String,Long>? = null
}

/**
* Text content part
*/
@DataContract
open class AiTextContent : AiContent()
{
    /**
    * The text content.
    */
    @DataMember(Name="text")
    @SerializedName("text")
    open var text:String? = null
}

/**
* Image content part
*/
@DataContract
open class AiImageContent : AiContent()
{
    /**
    * The image for this content.
    */
    @DataMember(Name="image_url")
    @SerializedName("image_url")
    open var imageUrl:AiImageUrl? = null
}

/**
* Audio content part
*/
@DataContract
open class AiAudioContent : AiContent()
{
    /**
    * The audio input for this content.
    */
    @DataMember(Name="input_audio")
    @SerializedName("input_audio")
    open var inputAudio:AiInputAudio? = null
}

/**
* File content part
*/
@DataContract
open class AiFileContent : AiContent()
{
    /**
    * The file input for this content.
    */
    @DataMember(Name="file")
    @SerializedName("file")
    open var file:AiFile? = null
}

open class ApiCss
{
    open var form:String? = null
    open var fieldset:String? = null
    open var field:String? = null
}

open class AppTags
{
    @SerializedName("default") open var Default:String? = null
    open var other:String? = null
}

open class PageInfo
{
    open var page:String? = null
    open var component:String? = null
}

open class MetaAuthProvider
{
    open var name:String? = null
    open var label:String? = null
    @SerializedName("type") open var Type:String? = null
    open var navItem:NavItem? = null
    open var icon:ImageInfo? = null
    open var formLayout:ArrayList<InputInfo>? = null
    open var meta:HashMap<String,String>? = null
}

open class IdentityAuthInfo
{
    open var hasRefreshToken:Boolean? = null
    open var meta:HashMap<String,String>? = null
}

open class KeyValuePair<TKey, TValue>
{
    open var key:TKey? = null
    open var value:TValue? = null
}

open class CommandInfo
{
    open var name:String? = null
    open var tag:String? = null
    open var request:MetadataType? = null
    open var response:MetadataType? = null
}

open class AutoQueryConvention
{
    open var name:String? = null
    open var value:String? = null
    open var types:String? = null
    open var valueType:String? = null
}

open class ScriptMethodType
{
    open var name:String? = null
    open var paramNames:ArrayList<String>? = null
    open var paramTypes:ArrayList<String>? = null
    open var returnType:String? = null
}

open class RequestLogsAnalytics
{
    open var months:ArrayList<String>? = null
    open var tabs:HashMap<String,String>? = null
    open var disableAnalytics:Boolean? = null
    open var disableUserAnalytics:Boolean? = null
    open var disableApiKeyAnalytics:Boolean? = null
}

open class FilesUploadLocation
{
    open var name:String? = null
    open var readAccessRole:String? = null
    open var writeAccessRole:String? = null
    open var allowExtensions:ArrayList<String>? = null
    open var allowOperations:String? = null
    open var maxFileCount:Int? = null
    open var minFileBytes:Long? = null
    open var maxFileBytes:Long? = null
}

open class MediaRule
{
    open var size:String? = null
    open var rule:String? = null
    open var applyTo:ArrayList<String>? = null
    open var meta:HashMap<String,String>? = null
}

open class DatabaseInfo
{
    open var alias:String? = null
    open var name:String? = null
    open var schemas:ArrayList<SchemaInfo>? = null
}

open class MetadataTypeName
{
    open var name:String? = null
    open var namespace:String? = null
    open var genericArgs:ArrayList<String>? = null
}

open class MetadataDataContract
{
    open var name:String? = null
    open var namespace:String? = null
}

open class MetadataRoute
{
    open var path:String? = null
    open var verbs:String? = null
    open var notes:String? = null
    open var summary:String? = null
}

open class ApiUiInfo
{
    open var locodeCss:ApiCss? = null
    open var explorerCss:ApiCss? = null
    open var formLayout:ArrayList<InputInfo>? = null
    open var meta:HashMap<String,String>? = null
}

/**
* Annotations for the message, when applicable, as when using the web search tool.
*/
@DataContract
open class ChoiceAnnotation
{
    /**
    * The type of the URL citation. Always url_citation.
    */
    @DataMember(Name="type")
    @SerializedName("type")
    open var Type:String? = null

    /**
    * A URL citation when using web search.
    */
    @DataMember(Name="url_citation")
    @SerializedName("url_citation")
    open var urlCitation:UrlCitation? = null
}

/**
* If the audio output modality is requested, this object contains data about the audio response from the model.
*/
@DataContract
open class ChoiceAudio
{
    /**
    * Base64 encoded audio bytes generated by the model, in the format specified in the request.
    */
    @DataMember(Name="data")
    @SerializedName("data")
    open var Data:String? = null

    /**
    * The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.
    */
    @DataMember(Name="expires_at")
    @SerializedName("expires_at")
    open var expiresAt:Int? = null

    /**
    * Unique identifier for this audio response.
    */
    @DataMember(Name="id")
    @SerializedName("id")
    open var id:String? = null

    /**
    * Transcript of the audio generated by the model.
    */
    @DataMember(Name="transcript")
    @SerializedName("transcript")
    open var transcript:String? = null
}

open class FieldCss
{
    open var field:String? = null
    open var input:String? = null
    open var label:String? = null
}

@DataContract
open class AiImageUrl
{
    /**
    * Either a URL of the image or the base64 encoded image data.
    */
    @DataMember(Name="url")
    @SerializedName("url")
    open var url:String? = null
}

/**
* Audio content part
*/
@DataContract
open class AiInputAudio
{
    /**
    * URL or Base64 encoded audio data.
    */
    @DataMember(Name="data")
    @SerializedName("data")
    open var Data:String? = null

    /**
    * The format of the encoded audio data. Currently supports 'wav' and 'mp3'.
    */
    @DataMember(Name="format")
    @SerializedName("format")
    open var format:String? = null
}

/**
* File content part
*/
@DataContract
open class AiFile
{
    /**
    * The URL or base64 encoded file data, used when passing the file to the model as a string.
    */
    @DataMember(Name="file_data")
    @SerializedName("file_data")
    open var fileData:String? = null

    /**
    * The name of the file, used when passing the file to the model as a string.
    */
    @DataMember(Name="filename")
    @SerializedName("filename")
    open var filename:String? = null

    /**
    * The ID of an uploaded file to use as input.
    */
    @DataMember(Name="file_id")
    @SerializedName("file_id")
    open var fileId:String? = null
}

open class NavItem
{
    open var label:String? = null
    open var href:String? = null
    open var exact:Boolean? = null
    open var id:String? = null
    open var className:String? = null
    open var iconClass:String? = null
    open var iconSrc:String? = null
    open var show:String? = null
    open var hide:String? = null
    open var children:ArrayList<NavItem>? = null
    open var meta:HashMap<String,String>? = null
}

open class SchemaInfo
{
    open var alias:String? = null
    open var name:String? = null
    open var tables:ArrayList<String>? = null
}

/**
* Annotations for the message, when applicable, as when using the web search tool.
*/
@DataContract
open class UrlCitation
{
    /**
    * The index of the last character of the URL citation in the message.
    */
    @DataMember(Name="end_index")
    @SerializedName("end_index")
    open var endIndex:Int? = null

    /**
    * The index of the first character of the URL citation in the message.
    */
    @DataMember(Name="start_index")
    @SerializedName("start_index")
    open var startIndex:Int? = null

    /**
    * The title of the web resource.
    */
    @DataMember(Name="title")
    @SerializedName("title")
    open var title:String? = null

    /**
    * The URL of the web resource.
    */
    @DataMember(Name="url")
    @SerializedName("url")
    open var url:String? = null
}
