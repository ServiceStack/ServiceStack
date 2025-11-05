/* Options:
Date: 2025-11-05 18:02:26
SwiftVersion: 6.0
Version: 8.91
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:20000

//BaseClass: 
//AddModelExtensions: True
//AddServiceStackTypes: True
//MakePropertiesOptional: True
//IncludeTypes: 
//ExcludeTypes: 
//ExcludeGenericBaseTypes: False
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
//InitializeCollections: False
//TreatTypesAsStrings: 
//DefaultImports: Foundation,ServiceStack
*/

import Foundation
import ServiceStack

// @Route("/metadata/app")
// @DataContract
public class MetadataApp : IReturn, IGet, Codable
{
    public typealias Return = AppMetadata

    // @DataMember(Order=1)
    public var view:String?

    // @DataMember(Order=2)
    public var includeTypes:[String]?

    required public init(){}
}

// @DataContract
public class AdminCreateRole : IReturn, IPost, Codable
{
    public typealias Return = IdResponse

    // @DataMember(Order=1)
    public var name:String?

    required public init(){}
}

// @DataContract
public class AdminGetRoles : IReturn, IGet, Codable
{
    public typealias Return = AdminGetRolesResponse

    required public init(){}
}

// @DataContract
public class AdminGetRole : IReturn, IGet, Codable
{
    public typealias Return = AdminGetRoleResponse

    // @DataMember(Order=1)
    public var id:String?

    required public init(){}
}

// @DataContract
public class AdminUpdateRole : IReturn, IPost, Codable
{
    public typealias Return = IdResponse

    // @DataMember(Order=1)
    public var id:String?

    // @DataMember(Order=2)
    public var name:String?

    // @DataMember(Order=3)
    public var addClaims:[Property]?

    // @DataMember(Order=4)
    public var removeClaims:[Property]?

    // @DataMember(Order=5)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class AdminDeleteRole : IReturnVoid, IDelete, Codable
{
    // @DataMember(Order=1)
    public var id:String?

    required public init(){}
}

public class AdminDashboard : IReturn, IGet, Codable
{
    public typealias Return = AdminDashboardResponse

    required public init(){}
}

// @DataContract
public class AdminQueryApiKeys : IReturn, IGet, Codable
{
    public typealias Return = AdminApiKeysResponse

    // @DataMember(Order=1)
    public var id:Int?

    // @DataMember(Order=2)
    public var apiKey:String?

    // @DataMember(Order=3)
    public var search:String?

    // @DataMember(Order=4)
    public var userId:String?

    // @DataMember(Order=5)
    public var userName:String?

    // @DataMember(Order=6)
    public var orderBy:String?

    // @DataMember(Order=7)
    public var skip:Int?

    // @DataMember(Order=8)
    public var take:Int?

    required public init(){}
}

// @DataContract
public class AdminCreateApiKey : IReturn, IPost, Codable
{
    public typealias Return = AdminApiKeyResponse

    // @DataMember(Order=1)
    public var name:String?

    // @DataMember(Order=2)
    public var userId:String?

    // @DataMember(Order=3)
    public var userName:String?

    // @DataMember(Order=4)
    public var scopes:[String]?

    // @DataMember(Order=5)
    public var features:[String]?

    // @DataMember(Order=6)
    public var restrictTo:[String]?

    // @DataMember(Order=7)
    public var expiryDate:Date?

    // @DataMember(Order=8)
    public var notes:String?

    // @DataMember(Order=9)
    public var refId:Int?

    // @DataMember(Order=10)
    public var refIdStr:String?

    // @DataMember(Order=11)
    public var meta:[String:String]?

    required public init(){}
}

// @DataContract
public class AdminUpdateApiKey : IReturn, IPatch, Codable
{
    public typealias Return = EmptyResponse

    // @DataMember(Order=1)
    // @Validate(Validator="GreaterThan(0)")
    public var id:Int?

    // @DataMember(Order=2)
    public var name:String?

    // @DataMember(Order=3)
    public var userId:String?

    // @DataMember(Order=4)
    public var userName:String?

    // @DataMember(Order=5)
    public var scopes:[String]?

    // @DataMember(Order=6)
    public var features:[String]?

    // @DataMember(Order=7)
    public var restrictTo:[String]?

    // @DataMember(Order=8)
    public var expiryDate:Date?

    // @DataMember(Order=9)
    public var cancelledDate:Date?

    // @DataMember(Order=10)
    public var notes:String?

    // @DataMember(Order=11)
    public var refId:Int?

    // @DataMember(Order=12)
    public var refIdStr:String?

    // @DataMember(Order=13)
    public var meta:[String:String]?

    // @DataMember(Order=14)
    public var reset:[String]?

    required public init(){}
}

// @DataContract
public class AdminDeleteApiKey : IReturn, IDelete, Codable
{
    public typealias Return = EmptyResponse

    // @DataMember(Order=1)
    // @Validate(Validator="GreaterThan(0)")
    public var id:Int?

    required public init(){}
}

/**
* Chat Completions API (OpenAI-Compatible)
*/
// @Route("/v1/chat/completions", "POST")
// @DataContract
public class ChatCompletion : IReturn, IPost, Codable
{
    public typealias Return = ChatResponse

    /**
    * The messages to generate chat completions for.
    */
    // @DataMember(Name="messages")
    public var messages:[AiMessage] = []

    /**
    * ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API
    */
    // @DataMember(Name="model")
    public var model:String?

    /**
    * Parameters for audio output. Required when audio output is requested with modalities: [audio]
    */
    // @DataMember(Name="audio")
    public var audio:AiChatAudio?

    /**
    * Modify the likelihood of specified tokens appearing in the completion.
    */
    // @DataMember(Name="logit_bias")
    public var logit_bias:[Int:Int]?

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    // @DataMember(Name="metadata")
    public var metadata:[String:String]?

    /**
    * Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.
    */
    // @DataMember(Name="reasoning_effort")
    public var reasoning_effort:String?

    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.
    */
    // @DataMember(Name="response_format")
    public var response_format:AiResponseFormat?

    /**
    * Specifies the processing type used for serving the request.
    */
    // @DataMember(Name="service_tier")
    public var service_tier:String?

    /**
    * A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.
    */
    // @DataMember(Name="safety_identifier")
    public var safety_identifier:String?

    /**
    * Up to 4 sequences where the API will stop generating further tokens.
    */
    // @DataMember(Name="stop")
    public var stop:[String]?

    /**
    * Output types that you would like the model to generate. Most models are capable of generating text, which is the default:
    */
    // @DataMember(Name="modalities")
    public var modalities:[String]?

    /**
    * Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.
    */
    // @DataMember(Name="prompt_cache_key")
    public var prompt_cache_key:String?

    /**
    * A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.
    */
    // @DataMember(Name="tools")
    public var tools:[Tool]?

    /**
    * Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.
    */
    // @DataMember(Name="verbosity")
    public var verbosity:String?

    /**
    * What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
    */
    // @DataMember(Name="temperature")
    public var temperature:Double?

    /**
    * An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.
    */
    // @DataMember(Name="max_completion_tokens")
    public var max_completion_tokens:Int?

    /**
    * An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
    */
    // @DataMember(Name="top_logprobs")
    public var top_logprobs:Int?

    /**
    * An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.
    */
    // @DataMember(Name="top_p")
    public var top_p:Double?

    /**
    * Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
    */
    // @DataMember(Name="frequency_penalty")
    public var frequency_penalty:Double?

    /**
    * Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
    */
    // @DataMember(Name="presence_penalty")
    public var presence_penalty:Double?

    /**
    * This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.
    */
    // @DataMember(Name="seed")
    public var seed:Int?

    /**
    * How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.
    */
    // @DataMember(Name="n")
    public var n:Int?

    /**
    * Whether or not to store the output of this chat completion request for use in our model distillation or evals products.
    */
    // @DataMember(Name="store")
    public var store:Bool?

    /**
    * Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
    */
    // @DataMember(Name="logprobs")
    public var logprobs:Bool?

    /**
    * Whether to enable parallel function calling during tool use.
    */
    // @DataMember(Name="parallel_tool_calls")
    public var parallel_tool_calls:Bool?

    /**
    * Whether to enable thinking mode for some Qwen models and providers.
    */
    // @DataMember(Name="enable_thinking")
    public var enable_thinking:Bool?

    /**
    * If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.
    */
    // @DataMember(Name="stream")
    public var stream:Bool?

    required public init(){}
}

public class AdminQueryChatCompletionLogs : QueryDb<ChatCompletionLog>, IReturn
{
    public typealias Return = QueryResponse<ChatCompletionLog>

    public var month:Date?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case month
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        month = try container.decodeIfPresent(Date.self, forKey: .month)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if month != nil { try container.encode(month, forKey: .month) }
    }
}

public class AdminMonthlyChatCompletionAnalytics : IReturn, IGet, Codable
{
    public typealias Return = AdminMonthlyChatCompletionAnalyticsResponse

    public var month:Date?

    required public init(){}
}

public class AdminDailyChatCompletionAnalytics : IReturn, IGet, Codable
{
    public typealias Return = AdminDailyChatCompletionAnalyticsResponse

    public var day:Date?

    required public init(){}
}

/**
* Sign In
*/
// @Route("/auth", "GET,POST")
// @Route("/auth/{provider}", "GET,POST")
// @Api(Description="Sign In")
// @DataContract
public class Authenticate : IReturn, IPost, Codable
{
    public typealias Return = AuthenticateResponse

    /**
    * AuthProvider, e.g. credentials
    */
    // @DataMember(Order=1)
    public var provider:String?

    // @DataMember(Order=2)
    public var userName:String?

    // @DataMember(Order=3)
    public var password:String?

    // @DataMember(Order=4)
    public var rememberMe:Bool?

    // @DataMember(Order=5)
    public var accessToken:String?

    // @DataMember(Order=6)
    public var accessTokenSecret:String?

    // @DataMember(Order=7)
    public var returnUrl:String?

    // @DataMember(Order=8)
    public var errorView:String?

    // @DataMember(Order=9)
    public var meta:[String:String]?

    required public init(){}
}

// @Route("/assignroles", "POST")
// @DataContract
public class AssignRoles : IReturn, IPost, Codable
{
    public typealias Return = AssignRolesResponse

    // @DataMember(Order=1)
    public var userName:String?

    // @DataMember(Order=2)
    public var permissions:[String]?

    // @DataMember(Order=3)
    public var roles:[String]?

    // @DataMember(Order=4)
    public var meta:[String:String]?

    required public init(){}
}

// @Route("/unassignroles", "POST")
// @DataContract
public class UnAssignRoles : IReturn, IPost, Codable
{
    public typealias Return = UnAssignRolesResponse

    // @DataMember(Order=1)
    public var userName:String?

    // @DataMember(Order=2)
    public var permissions:[String]?

    // @DataMember(Order=3)
    public var roles:[String]?

    // @DataMember(Order=4)
    public var meta:[String:String]?

    required public init(){}
}

// @DataContract
public class AdminGetUser : IReturn, IGet, Codable
{
    public typealias Return = AdminUserResponse

    // @DataMember(Order=10)
    public var id:String?

    required public init(){}
}

// @DataContract
public class AdminQueryUsers : IReturn, IGet, Codable
{
    public typealias Return = AdminUsersResponse

    // @DataMember(Order=1)
    public var query:String?

    // @DataMember(Order=2)
    public var orderBy:String?

    // @DataMember(Order=3)
    public var skip:Int?

    // @DataMember(Order=4)
    public var take:Int?

    required public init(){}
}

// @DataContract
public class AdminCreateUser : AdminUserBase, IReturn, IPost
{
    public typealias Return = AdminUserResponse

    // @DataMember(Order=10)
    public var roles:[String]?

    // @DataMember(Order=11)
    public var permissions:[String]?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case roles
        case permissions
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        roles = try container.decodeIfPresent([String].self, forKey: .roles) ?? []
        permissions = try container.decodeIfPresent([String].self, forKey: .permissions) ?? []
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if roles != nil { try container.encode(roles, forKey: .roles) }
        if permissions != nil { try container.encode(permissions, forKey: .permissions) }
    }
}

// @DataContract
public class AdminUpdateUser : AdminUserBase, IReturn, IPut
{
    public typealias Return = AdminUserResponse

    // @DataMember(Order=10)
    public var id:String?

    // @DataMember(Order=11)
    public var lockUser:Bool?

    // @DataMember(Order=12)
    public var unlockUser:Bool?

    // @DataMember(Order=13)
    public var lockUserUntil:Date?

    // @DataMember(Order=14)
    public var addRoles:[String]?

    // @DataMember(Order=15)
    public var removeRoles:[String]?

    // @DataMember(Order=16)
    public var addPermissions:[String]?

    // @DataMember(Order=17)
    public var removePermissions:[String]?

    // @DataMember(Order=18)
    public var addClaims:[Property]?

    // @DataMember(Order=19)
    public var removeClaims:[Property]?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case id
        case lockUser
        case unlockUser
        case lockUserUntil
        case addRoles
        case removeRoles
        case addPermissions
        case removePermissions
        case addClaims
        case removeClaims
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(String.self, forKey: .id)
        lockUser = try container.decodeIfPresent(Bool.self, forKey: .lockUser)
        unlockUser = try container.decodeIfPresent(Bool.self, forKey: .unlockUser)
        lockUserUntil = try container.decodeIfPresent(Date.self, forKey: .lockUserUntil)
        addRoles = try container.decodeIfPresent([String].self, forKey: .addRoles) ?? []
        removeRoles = try container.decodeIfPresent([String].self, forKey: .removeRoles) ?? []
        addPermissions = try container.decodeIfPresent([String].self, forKey: .addPermissions) ?? []
        removePermissions = try container.decodeIfPresent([String].self, forKey: .removePermissions) ?? []
        addClaims = try container.decodeIfPresent([Property].self, forKey: .addClaims) ?? []
        removeClaims = try container.decodeIfPresent([Property].self, forKey: .removeClaims) ?? []
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if id != nil { try container.encode(id, forKey: .id) }
        if lockUser != nil { try container.encode(lockUser, forKey: .lockUser) }
        if unlockUser != nil { try container.encode(unlockUser, forKey: .unlockUser) }
        if lockUserUntil != nil { try container.encode(lockUserUntil, forKey: .lockUserUntil) }
        if addRoles != nil { try container.encode(addRoles, forKey: .addRoles) }
        if removeRoles != nil { try container.encode(removeRoles, forKey: .removeRoles) }
        if addPermissions != nil { try container.encode(addPermissions, forKey: .addPermissions) }
        if removePermissions != nil { try container.encode(removePermissions, forKey: .removePermissions) }
        if addClaims != nil { try container.encode(addClaims, forKey: .addClaims) }
        if removeClaims != nil { try container.encode(removeClaims, forKey: .removeClaims) }
    }
}

// @DataContract
public class AdminDeleteUser : IReturn, IDelete, Codable
{
    public typealias Return = AdminDeleteUserResponse

    // @DataMember(Order=10)
    public var id:String?

    required public init(){}
}

public class AdminQueryRequestLogs : QueryDb<RequestLog>, IReturn
{
    public typealias Return = QueryResponse<RequestLog>

    public var month:Date?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case month
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        month = try container.decodeIfPresent(Date.self, forKey: .month)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if month != nil { try container.encode(month, forKey: .month) }
    }
}

public class AdminProfiling : IReturn, Codable
{
    public typealias Return = AdminProfilingResponse

    public var source:String?
    public var eventType:String?
    public var threadId:Int?
    public var traceId:String?
    public var userAuthId:String?
    public var sessionId:String?
    public var tag:String?
    public var skip:Int?
    public var take:Int?
    public var orderBy:String?
    public var withErrors:Bool?
    public var pending:Bool?

    required public init(){}
}

public class AdminRedis : IReturn, IPost, Codable
{
    public typealias Return = AdminRedisResponse

    public var db:Int?
    public var query:String?
    public var reconnect:RedisEndpointInfo?
    public var take:Int?
    public var position:Int?
    public var args:[String]?

    required public init(){}
}

public class AdminDatabase : IReturn, IGet, Codable
{
    public typealias Return = AdminDatabaseResponse

    public var db:String?
    public var schema:String?
    public var table:String?
    public var fields:[String]?
    public var take:Int?
    public var skip:Int?
    public var orderBy:String?
    public var include:String?

    required public init(){}
}

public class ViewCommands : IReturn, IGet, Codable
{
    public typealias Return = ViewCommandsResponse

    public var include:[String]?
    public var skip:Int?
    public var take:Int?

    required public init(){}
}

public class ExecuteCommand : IReturn, IPost, Codable
{
    public typealias Return = ExecuteCommandResponse

    public var command:String?
    public var requestJson:String?

    required public init(){}
}

public class AdminJobDashboard : IReturn, IGet, Codable
{
    public typealias Return = AdminJobDashboardResponse

    public var from:Date?
    public var to:Date?

    required public init(){}
}

public class AdminJobInfo : IReturn, IGet, Codable
{
    public typealias Return = AdminJobInfoResponse

    public var month:Date?

    required public init(){}
}

public class AdminGetJob : IReturn, IGet, Codable
{
    public typealias Return = AdminGetJobResponse

    public var id:Int?
    public var refId:String?

    required public init(){}
}

public class AdminGetJobProgress : IReturn, IGet, Codable
{
    public typealias Return = AdminGetJobProgressResponse

    // @Validate(Validator="GreaterThan(0)")
    public var id:Int?

    public var logStart:Int?

    required public init(){}
}

public class AdminQueryBackgroundJobs : QueryDb<BackgroundJob>, IReturn
{
    public typealias Return = QueryResponse<BackgroundJob>

    public var id:Int?
    public var refId:String?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case id
        case refId
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(Int.self, forKey: .id)
        refId = try container.decodeIfPresent(String.self, forKey: .refId)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if id != nil { try container.encode(id, forKey: .id) }
        if refId != nil { try container.encode(refId, forKey: .refId) }
    }
}

public class AdminQueryJobSummary : QueryDb<JobSummary>, IReturn
{
    public typealias Return = QueryResponse<JobSummary>

    public var id:Int?
    public var refId:String?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case id
        case refId
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(Int.self, forKey: .id)
        refId = try container.decodeIfPresent(String.self, forKey: .refId)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if id != nil { try container.encode(id, forKey: .id) }
        if refId != nil { try container.encode(refId, forKey: .refId) }
    }
}

public class AdminQueryScheduledTasks : QueryDb<ScheduledTask>, IReturn
{
    public typealias Return = QueryResponse<ScheduledTask>

    required public init(){ super.init() }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
    }
}

public class AdminQueryCompletedJobs : QueryDb<CompletedJob>, IReturn
{
    public typealias Return = QueryResponse<CompletedJob>

    public var month:Date?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case month
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        month = try container.decodeIfPresent(Date.self, forKey: .month)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if month != nil { try container.encode(month, forKey: .month) }
    }
}

public class AdminQueryFailedJobs : QueryDb<FailedJob>, IReturn
{
    public typealias Return = QueryResponse<FailedJob>

    public var month:Date?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case month
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        month = try container.decodeIfPresent(Date.self, forKey: .month)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if month != nil { try container.encode(month, forKey: .month) }
    }
}

public class AdminRequeueFailedJobs : IReturn, Codable
{
    public typealias Return = AdminRequeueFailedJobsJobsResponse

    public var ids:[Int]?

    required public init(){}
}

public class AdminCancelJobs : IReturn, IGet, Codable
{
    public typealias Return = AdminCancelJobsResponse

    public var ids:[Int]?
    public var worker:String?
    public var state:BackgroundJobState?
    public var cancelWorker:String?

    required public init(){}
}

// @Route("/requestlogs")
// @DataContract
public class RequestLogs : IReturn, IGet, Codable
{
    public typealias Return = RequestLogsResponse

    // @DataMember(Order=1)
    public var beforeSecs:Int?

    // @DataMember(Order=2)
    public var afterSecs:Int?

    // @DataMember(Order=3)
    public var operationName:String?

    // @DataMember(Order=4)
    public var ipAddress:String?

    // @DataMember(Order=5)
    public var forwardedFor:String?

    // @DataMember(Order=6)
    public var userAuthId:String?

    // @DataMember(Order=7)
    public var sessionId:String?

    // @DataMember(Order=8)
    public var referer:String?

    // @DataMember(Order=9)
    public var pathInfo:String?

    // @DataMember(Order=10)
    public var bearerToken:String?

    // @DataMember(Order=11)
    public var ids:[Int]?

    // @DataMember(Order=12)
    public var beforeId:Int?

    // @DataMember(Order=13)
    public var afterId:Int?

    // @DataMember(Order=14)
    public var hasResponse:Bool?

    // @DataMember(Order=15)
    public var withErrors:Bool?

    // @DataMember(Order=16)
    public var enableSessionTracking:Bool?

    // @DataMember(Order=17)
    public var enableResponseTracking:Bool?

    // @DataMember(Order=18)
    public var enableErrorTracking:Bool?

    // @DataMember(Order=19)
    @TimeSpan public var durationLongerThan:TimeInterval?

    // @DataMember(Order=20)
    @TimeSpan public var durationLessThan:TimeInterval?

    // @DataMember(Order=21)
    public var skip:Int?

    // @DataMember(Order=22)
    public var take:Int?

    // @DataMember(Order=23)
    public var orderBy:String?

    // @DataMember(Order=24)
    public var month:Date?

    required public init(){}
}

// @DataContract
public class GetAnalyticsInfo : IReturn, IGet, Codable
{
    public typealias Return = GetAnalyticsInfoResponse

    // @DataMember(Order=1)
    public var month:Date?

    // @DataMember(Order=2)
    public var type:String?

    // @DataMember(Order=3)
    public var op:String?

    // @DataMember(Order=4)
    public var apiKey:String?

    // @DataMember(Order=5)
    public var userId:String?

    // @DataMember(Order=6)
    public var ip:String?

    required public init(){}
}

// @DataContract
public class GetAnalyticsReports : IReturn, IGet, Codable
{
    public typealias Return = GetAnalyticsReportsResponse

    // @DataMember(Order=1)
    public var month:Date?

    // @DataMember(Order=2)
    public var filter:String?

    // @DataMember(Order=3)
    public var value:String?

    // @DataMember(Order=4)
    public var force:Bool?

    required public init(){}
}

// @Route("/validation/rules/{Type}")
// @DataContract
public class GetValidationRules : IReturn, IGet, Codable
{
    public typealias Return = GetValidationRulesResponse

    // @DataMember(Order=1)
    public var authSecret:String?

    // @DataMember(Order=2)
    public var type:String?

    required public init(){}
}

// @Route("/validation/rules")
// @DataContract
public class ModifyValidationRules : IReturnVoid, Codable
{
    // @DataMember(Order=1)
    public var authSecret:String?

    // @DataMember(Order=2)
    public var saveRules:[ValidationRule]?

    // @DataMember(Order=3)
    public var deleteRuleIds:[Int]?

    // @DataMember(Order=4)
    public var suspendRuleIds:[Int]?

    // @DataMember(Order=5)
    public var unsuspendRuleIds:[Int]?

    // @DataMember(Order=6)
    public var clearCache:Bool?

    required public init(){}
}

public class AppMetadata : Codable
{
    public var date:Date?
    public var app:AppInfo?
    public var ui:UiInfo?
    public var config:ConfigInfo?
    public var contentTypeFormats:[String:String]?
    public var httpHandlers:[String:String]?
    public var plugins:PluginInfo?
    public var customPlugins:[String:CustomPluginInfo]?
    public var api:MetadataTypes?
    public var meta:[String:String]?

    required public init(){}
}

// @DataContract
public class IdResponse : Codable
{
    // @DataMember(Order=1)
    public var id:String?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class AdminGetRolesResponse : Codable
{
    // @DataMember(Order=1)
    public var results:[AdminRole]?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class AdminGetRoleResponse : Codable
{
    // @DataMember(Order=1)
    public var result:AdminRole?

    // @DataMember(Order=2)
    public var claims:[Property]?

    // @DataMember(Order=3)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminDashboardResponse : Codable
{
    public var serverStats:ServerStats?
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class AdminApiKeysResponse : Codable
{
    // @DataMember(Order=1)
    public var results:[PartialApiKey]?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class AdminApiKeyResponse : Codable
{
    // @DataMember(Order=1)
    public var result:String?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class ChatResponse : Codable
{
    /**
    * A unique identifier for the chat completion.
    */
    // @DataMember(Name="id")
    public var id:String?

    /**
    * A list of chat completion choices. Can be more than one if n is greater than 1.
    */
    // @DataMember(Name="choices")
    public var choices:[Choice] = []

    /**
    * The Unix timestamp (in seconds) of when the chat completion was created.
    */
    // @DataMember(Name="created")
    public var created:Int?

    /**
    * The model used for the chat completion.
    */
    // @DataMember(Name="model")
    public var model:String?

    /**
    * This fingerprint represents the backend configuration that the model runs with.
    */
    // @DataMember(Name="system_fingerprint")
    public var system_fingerprint:String?

    /**
    * The object type, which is always chat.completion.
    */
    // @DataMember(Name="object")
    public var object:String?

    /**
    * Specifies the processing type used for serving the request.
    */
    // @DataMember(Name="service_tier")
    public var service_tier:String?

    /**
    * Usage statistics for the completion request.
    */
    // @DataMember(Name="usage")
    public var usage:AiUsage?

    /**
    * The provider used for the chat completion.
    */
    // @DataMember(Name="provider")
    public var provider:String?

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    // @DataMember(Name="metadata")
    public var metadata:[String:String]?

    // @DataMember(Name="responseStatus")
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminMonthlyChatCompletionAnalyticsResponse : Codable
{
    public var month:String?
    public var modelStats:[ChatCompletionStat] = []
    public var providerStats:[ChatCompletionStat] = []
    public var dailyStats:[ChatCompletionStat] = []

    required public init(){}
}

public class AdminDailyChatCompletionAnalyticsResponse : Codable
{
    public var modelStats:[ChatCompletionStat] = []
    public var providerStats:[ChatCompletionStat] = []

    required public init(){}
}

// @DataContract
public class AuthenticateResponse : IHasSessionId, IHasBearerToken, Codable
{
    // @DataMember(Order=1)
    public var userId:String?

    // @DataMember(Order=2)
    public var sessionId:String?

    // @DataMember(Order=3)
    public var userName:String?

    // @DataMember(Order=4)
    public var displayName:String?

    // @DataMember(Order=5)
    public var referrerUrl:String?

    // @DataMember(Order=6)
    public var bearerToken:String?

    // @DataMember(Order=7)
    public var refreshToken:String?

    // @DataMember(Order=8)
    public var refreshTokenExpiry:Date?

    // @DataMember(Order=9)
    public var profileUrl:String?

    // @DataMember(Order=10)
    public var roles:[String]?

    // @DataMember(Order=11)
    public var permissions:[String]?

    // @DataMember(Order=12)
    public var authProvider:String?

    // @DataMember(Order=13)
    public var responseStatus:ResponseStatus?

    // @DataMember(Order=14)
    public var meta:[String:String]?

    required public init(){}
}

// @DataContract
public class AssignRolesResponse : Codable
{
    // @DataMember(Order=1)
    public var allRoles:[String]?

    // @DataMember(Order=2)
    public var allPermissions:[String]?

    // @DataMember(Order=3)
    public var meta:[String:String]?

    // @DataMember(Order=4)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class UnAssignRolesResponse : Codable
{
    // @DataMember(Order=1)
    public var allRoles:[String]?

    // @DataMember(Order=2)
    public var allPermissions:[String]?

    // @DataMember(Order=3)
    public var meta:[String:String]?

    // @DataMember(Order=4)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class AdminUserResponse : Codable
{
    // @DataMember(Order=1)
    public var id:String?

    // @DataMember(Order=2)
    public var result:[String:String]?

    // @DataMember(Order=3)
    public var details:[[String:String]]?

    // @DataMember(Order=4)
    public var claims:[Property]?

    // @DataMember(Order=5)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class AdminUsersResponse : Codable
{
    // @DataMember(Order=1)
    public var results:[[String:String]]?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class AdminDeleteUserResponse : Codable
{
    // @DataMember(Order=1)
    public var id:String?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminProfilingResponse : Codable
{
    public var results:[DiagnosticEntry] = []
    public var total:Int?
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminRedisResponse : Codable
{
    public var db:Int?
    public var searchResults:[RedisSearchResult]?
    public var info:[String:String]?
    public var endpoint:RedisEndpointInfo?
    public var result:RedisText?
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminDatabaseResponse : Codable
{
    public var results:[[String:String]] = []
    public var total:Int?
    public var columns:[MetadataPropertyType]?
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class ViewCommandsResponse : Codable
{
    public var commandTotals:[CommandSummary] = []
    public var latestCommands:[CommandResult] = []
    public var latestFailed:[CommandResult] = []
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class ExecuteCommandResponse : Codable
{
    public var commandResult:CommandResult?
    public var result:String?
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminJobDashboardResponse : Codable
{
    public var commands:[JobStatSummary] = []
    public var apis:[JobStatSummary] = []
    public var workers:[JobStatSummary] = []
    public var today:[HourSummary] = []
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminJobInfoResponse : Codable
{
    public var monthDbs:[Date]?
    public var tableCounts:[String:Int]?
    public var workerStats:[WorkerStats]?
    public var queueCounts:[String:Int]?
    public var workerCounts:[String:Int]?
    public var stateCounts:[BackgroundJobState:Int]?
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminGetJobResponse : Codable
{
    public var result:JobSummary?
    public var queued:BackgroundJob?
    public var completed:CompletedJob?
    public var failed:FailedJob?
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminGetJobProgressResponse : Codable
{
    public var state:BackgroundJobState?
    public var progress:Double?
    public var status:String?
    public var logs:String?
    public var durationMs:Int?
    public var error:ResponseStatus?
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminRequeueFailedJobsJobsResponse : Codable
{
    public var results:[Int] = []
    public var errors:[Int:String] = [:]
    public var responseStatus:ResponseStatus?

    required public init(){}
}

public class AdminCancelJobsResponse : Codable
{
    public var results:[Int] = []
    public var errors:[Int:String] = [:]
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class RequestLogsResponse : Codable
{
    // @DataMember(Order=1)
    public var results:[RequestLogEntry]?

    // @DataMember(Order=2)
    public var usage:[String:String]?

    // @DataMember(Order=3)
    public var total:Int?

    // @DataMember(Order=4)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class GetAnalyticsInfoResponse : Codable
{
    // @DataMember(Order=1)
    public var months:[String]?

    // @DataMember(Order=2)
    public var result:AnalyticsLogInfo?

    // @DataMember(Order=3)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class GetAnalyticsReportsResponse : Codable
{
    // @DataMember(Order=1)
    public var result:AnalyticsReports?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class GetValidationRulesResponse : Codable
{
    // @DataMember(Order=1)
    public var results:[ValidationRule]?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class Property : Codable
{
    // @DataMember(Order=1)
    public var name:String?

    // @DataMember(Order=2)
    public var value:String?

    required public init(){}
}

/**
* A list of messages comprising the conversation so far.
*/
// @DataContract
public class AiMessage : Codable
{
    /**
    * The contents of the message.
    */
    // @DataMember(Name="content")
    public var content:[AiContent]?

    /**
    * The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.
    */
    // @DataMember(Name="role")
    public var role:String?

    /**
    * An optional name for the participant. Provides the model information to differentiate between participants of the same role.
    */
    // @DataMember(Name="name")
    public var name:String?

    /**
    * The tool calls generated by the model, such as function calls.
    */
    // @DataMember(Name="tool_calls")
    public var tool_calls:[ToolCall]?

    /**
    * Tool call that this message is responding to.
    */
    // @DataMember(Name="tool_call_id")
    public var tool_call_id:String?

    required public init(){}
}

/**
* Parameters for audio output. Required when audio output is requested with modalities: [audio]
*/
// @DataContract
public class AiChatAudio : Codable
{
    /**
    * Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.
    */
    // @DataMember(Name="format")
    public var format:String?

    /**
    * The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.
    */
    // @DataMember(Name="voice")
    public var voice:String?

    required public init(){}
}

// @DataContract
public class AiResponseFormat : Codable
{
    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.
    */
    // @DataMember(Name="response_format")
    public var response_format:ResponseFormat?

    required public init(){}
}

// @DataContract
public class Tool : Codable
{
    /**
    * The type of the tool. Currently, only function is supported.
    */
    // @DataMember(Name="type")
    public var type:ToolType?

    required public init(){}
}

public class ChatCompletionLog : Codable
{
    public var id:Int?
    public var refId:String?
    public var userId:String?
    public var apiKey:String?
    public var model:String?
    public var provider:String?
    public var userPrompt:String?
    public var answer:String?
    // @StringLength(Int32.max)
    public var requestBody:String?

    // @StringLength(Int32.max)
    public var responseBody:String?

    public var errorCode:String?
    public var error:ResponseStatus?
    public var createdDate:Date?
    public var tag:String?
    public var durationMs:Int?
    public var promptTokens:Int?
    public var completionTokens:Int?
    public var cost:Double?
    public var providerRef:String?
    public var providerModel:String?
    public var finishReason:String?
    public var usage:ModelUsage?
    public var threadId:String?
    public var title:String?
    public var meta:[String:String]?

    required public init(){}
}

// @DataContract
public class AdminUserBase : Codable
{
    // @DataMember(Order=1)
    public var userName:String?

    // @DataMember(Order=2)
    public var firstName:String?

    // @DataMember(Order=3)
    public var lastName:String?

    // @DataMember(Order=4)
    public var displayName:String?

    // @DataMember(Order=5)
    public var email:String?

    // @DataMember(Order=6)
    public var password:String?

    // @DataMember(Order=7)
    public var profileUrl:String?

    // @DataMember(Order=8)
    public var phoneNumber:String?

    // @DataMember(Order=9)
    public var userAuthProperties:[String:String]?

    // @DataMember(Order=10)
    public var meta:[String:String]?

    required public init(){}
}

public class RequestLog : Codable
{
    public var id:Int?
    public var traceId:String?
    public var operationName:String?
    public var dateTime:Date?
    public var statusCode:Int?
    public var statusDescription:String?
    public var httpMethod:String?
    public var absoluteUri:String?
    public var pathInfo:String?
    public var request:String?
    // @StringLength(Int32.max)
    public var requestBody:String?

    public var userAuthId:String?
    public var sessionId:String?
    public var ipAddress:String?
    public var forwardedFor:String?
    public var referer:String?
    public var headers:[String:String] = [:]
    public var formData:[String:String]?
    public var items:[String:String] = [:]
    public var responseHeaders:[String:String]?
    public var response:String?
    // @StringLength(Int32.max)
    public var responseBody:String?

    // @StringLength(Int32.max)
    public var sessionBody:String?

    public var error:ResponseStatus?
    public var exceptionSource:String?
    public var exceptionDataBody:String?
    @TimeSpan public var requestDuration:TimeInterval?
    public var meta:[String:String]?

    required public init(){}
}

public class RedisEndpointInfo : Codable
{
    public var host:String?
    public var port:Int?
    public var ssl:Bool?
    public var db:Int?
    public var username:String?
    public var password:String?

    required public init(){}
}

public class BackgroundJob : BackgroundJobBase
{

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case id
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(Int.self, forKey: .id)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if id != nil { try container.encode(id, forKey: .id) }
    }
}

public class JobSummary : Codable
{
    public var id:Int?
    public var parentId:Int?
    public var refId:String?
    public var worker:String?
    public var tag:String?
    public var batchId:String?
    public var createdDate:Date?
    public var createdBy:String?
    public var requestType:String?
    public var command:String?
    public var request:String?
    public var response:String?
    public var userId:String?
    public var callback:String?
    public var startedDate:Date?
    public var completedDate:Date?
    public var state:BackgroundJobState?
    public var durationMs:Int?
    public var attempts:Int?
    public var errorCode:String?
    public var errorMessage:String?

    required public init(){}
}

public class ScheduledTask : Codable
{
    public var id:Int?
    public var name:String?
    @TimeSpan public var interval:TimeInterval?
    public var cronExpression:String?
    public var requestType:String?
    public var command:String?
    public var request:String?
    public var requestBody:String?
    public var options:BackgroundJobOptions?
    public var lastRun:Date?
    public var lastJobId:Int?

    required public init(){}
}

public class CompletedJob : BackgroundJobBase
{
    required public init(){ super.init() }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
    }
}

public class FailedJob : BackgroundJobBase
{
    required public init(){ super.init() }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
    }
}

public enum BackgroundJobState : String, Codable
{
    case Queued
    case Started
    case Executed
    case Completed
    case Failed
    case Cancelled
}

public class ValidationRule : ValidateRule
{
    public var id:Int?
    // @Required()
    public var type:String?

    public var field:String?
    public var createdBy:String?
    public var createdDate:Date?
    public var modifiedBy:String?
    public var modifiedDate:Date?
    public var suspendedBy:String?
    public var suspendedDate:Date?
    public var notes:String?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case id
        case type
        case field
        case createdBy
        case createdDate
        case modifiedBy
        case modifiedDate
        case suspendedBy
        case suspendedDate
        case notes
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(Int.self, forKey: .id)
        type = try container.decodeIfPresent(String.self, forKey: .type)
        field = try container.decodeIfPresent(String.self, forKey: .field)
        createdBy = try container.decodeIfPresent(String.self, forKey: .createdBy)
        createdDate = try container.decodeIfPresent(Date.self, forKey: .createdDate)
        modifiedBy = try container.decodeIfPresent(String.self, forKey: .modifiedBy)
        modifiedDate = try container.decodeIfPresent(Date.self, forKey: .modifiedDate)
        suspendedBy = try container.decodeIfPresent(String.self, forKey: .suspendedBy)
        suspendedDate = try container.decodeIfPresent(Date.self, forKey: .suspendedDate)
        notes = try container.decodeIfPresent(String.self, forKey: .notes)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if id != nil { try container.encode(id, forKey: .id) }
        if type != nil { try container.encode(type, forKey: .type) }
        if field != nil { try container.encode(field, forKey: .field) }
        if createdBy != nil { try container.encode(createdBy, forKey: .createdBy) }
        if createdDate != nil { try container.encode(createdDate, forKey: .createdDate) }
        if modifiedBy != nil { try container.encode(modifiedBy, forKey: .modifiedBy) }
        if modifiedDate != nil { try container.encode(modifiedDate, forKey: .modifiedDate) }
        if suspendedBy != nil { try container.encode(suspendedBy, forKey: .suspendedBy) }
        if suspendedDate != nil { try container.encode(suspendedDate, forKey: .suspendedDate) }
        if notes != nil { try container.encode(notes, forKey: .notes) }
    }
}

public class AppInfo : Codable
{
    public var baseUrl:String?
    public var serviceStackVersion:String?
    public var serviceName:String?
    public var apiVersion:String?
    public var serviceDescription:String?
    public var serviceIconUrl:String?
    public var brandUrl:String?
    public var brandImageUrl:String?
    public var textColor:String?
    public var linkColor:String?
    public var backgroundColor:String?
    public var backgroundImageUrl:String?
    public var iconUrl:String?
    public var jsTextCase:String?
    public var useSystemJson:String?
    public var endpointRouting:[String]?
    public var meta:[String:String]?

    required public init(){}
}

public class UiInfo : Codable
{
    public var brandIcon:ImageInfo?
    public var userIcon:ImageInfo?
    public var hideTags:[String]?
    public var modules:[String]?
    public var alwaysHideTags:[String]?
    public var adminLinks:[LinkInfo]?
    public var adminLinksOrder:[String]?
    public var theme:ThemeInfo?
    public var locode:LocodeUi?
    public var explorer:ExplorerUi?
    public var admin:AdminUi?
    public var defaultFormats:ApiFormat?
    public var meta:[String:String]?

    required public init(){}
}

public class ConfigInfo : Codable
{
    public var debugMode:Bool?
    public var meta:[String:String]?

    required public init(){}
}

public class PluginInfo : Codable
{
    public var loaded:[String]?
    public var auth:AuthInfo?
    public var apiKey:ApiKeyInfo?
    public var commands:CommandsInfo?
    public var autoQuery:AutoQueryInfo?
    public var validation:ValidationInfo?
    public var sharpPages:SharpPagesInfo?
    public var requestLogs:RequestLogsInfo?
    public var profiling:ProfilingInfo?
    public var filesUpload:FilesUploadInfo?
    public var adminUsers:AdminUsersInfo?
    public var adminIdentityUsers:AdminIdentityUsersInfo?
    public var adminRedis:AdminRedisInfo?
    public var adminDatabase:AdminDatabaseInfo?
    public var adminChat:AdminChatInfo?
    public var meta:[String:String]?

    required public init(){}
}

public class CustomPluginInfo : Codable
{
    public var accessRole:String?
    public var serviceRoutes:[String:[String]]?
    public var enabled:[String]?
    public var meta:[String:String]?

    required public init(){}
}

public class MetadataTypes : Codable
{
    public var config:MetadataTypesConfig?
    public var namespaces:[String]?
    public var types:[MetadataType]?
    public var operations:[MetadataOperationType]?

    required public init(){}
}

// @DataContract
public class AdminRole : Codable
{
    required public init(){}
}

public class ServerStats : Codable
{
    public var redis:[String:Int]?
    public var serverEvents:[String:String]?
    public var mqDescription:String?
    public var mqWorkers:[String:Int]?

    required public init(){}
}

// @DataContract
public class PartialApiKey : Codable
{
    // @DataMember(Order=1)
    public var id:Int?

    // @DataMember(Order=2)
    public var name:String?

    // @DataMember(Order=3)
    public var userId:String?

    // @DataMember(Order=4)
    public var userName:String?

    // @DataMember(Order=5)
    public var visibleKey:String?

    // @DataMember(Order=6)
    public var environment:String?

    // @DataMember(Order=7)
    public var createdDate:Date?

    // @DataMember(Order=8)
    public var expiryDate:Date?

    // @DataMember(Order=9)
    public var cancelledDate:Date?

    // @DataMember(Order=10)
    public var lastUsedDate:Date?

    // @DataMember(Order=11)
    public var scopes:[String]?

    // @DataMember(Order=12)
    public var features:[String]?

    // @DataMember(Order=13)
    public var restrictTo:[String]?

    // @DataMember(Order=14)
    public var notes:String?

    // @DataMember(Order=15)
    public var refId:Int?

    // @DataMember(Order=16)
    public var refIdStr:String?

    // @DataMember(Order=17)
    public var meta:[String:String]?

    // @DataMember(Order=18)
    public var active:Bool?

    required public init(){}
}

// @DataContract
public class Choice : Codable
{
    /**
    * The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool
    */
    // @DataMember(Name="finish_reason")
    public var finish_reason:String?

    /**
    * The index of the choice in the list of choices.
    */
    // @DataMember(Name="index")
    public var index:Int?

    /**
    * A chat completion message generated by the model.
    */
    // @DataMember(Name="message")
    public var message:ChoiceMessage?

    required public init(){}
}

/**
* Usage statistics for the completion request.
*/
// @DataContract
public class AiUsage : Codable
{
    /**
    * Number of tokens in the generated completion.
    */
    // @DataMember(Name="completion_tokens")
    public var completion_tokens:Int?

    /**
    * Number of tokens in the prompt.
    */
    // @DataMember(Name="prompt_tokens")
    public var prompt_tokens:Int?

    /**
    * Total number of tokens used in the request (prompt + completion).
    */
    // @DataMember(Name="total_tokens")
    public var total_tokens:Int?

    /**
    * Breakdown of tokens used in a completion.
    */
    // @DataMember(Name="completion_tokens_details")
    public var completion_tokens_details:AiCompletionUsage?

    /**
    * Breakdown of tokens used in the prompt.
    */
    // @DataMember(Name="prompt_tokens_details")
    public var prompt_tokens_details:AiPromptUsage?

    required public init(){}
}

public class ChatCompletionStat : Codable
{
    public var name:String?
    public var requests:Int?
    public var inputTokens:Int?
    public var outputTokens:Int?
    public var cost:Double?

    required public init(){}
}

public class DiagnosticEntry : Codable
{
    public var id:Int?
    public var traceId:String?
    public var source:String?
    public var eventType:String?
    public var message:String?
    public var operation:String?
    public var threadId:Int?
    public var error:ResponseStatus?
    public var commandType:String?
    public var command:String?
    public var userAuthId:String?
    public var sessionId:String?
    public var arg:String?
    public var args:[String]?
    public var argLengths:[Int]?
    public var namedArgs:[String:String]?
    @TimeSpan public var duration:TimeInterval?
    public var timestamp:Int?
    public var date:Date?
    public var tag:String?
    public var stackTrace:String?
    public var meta:[String:String] = [:]

    required public init(){}
}

public class RedisSearchResult : Codable
{
    public var id:String?
    public var type:String?
    public var ttl:Int?
    public var size:Int?

    required public init(){}
}

public class RedisText : Codable
{
    public var text:String?
    public var children:[RedisText]?

    required public init(){}
}

public class MetadataPropertyType : Codable
{
    public var name:String?
    public var type:String?
    public var namespace:String?
    public var isValueType:Bool?
    public var isEnum:Bool?
    public var isPrimaryKey:Bool?
    public var genericArgs:[String]?
    public var value:String?
    public var Description:String?
    public var dataMember:MetadataDataMember?
    public var readOnly:Bool?
    public var paramType:String?
    public var displayType:String?
    public var isRequired:Bool?
    public var allowableValues:[String]?
    public var allowableMin:Int?
    public var allowableMax:Int?
    public var attributes:[MetadataAttribute]?
    public var uploadTo:String?
    public var input:InputInfo?
    public var format:FormatInfo?
    public var ref:RefInfo?

    required public init(){}
}

public class CommandSummary : Codable
{
    public var type:String?
    public var name:String?
    public var count:Int?
    public var failed:Int?
    public var retries:Int?
    public var totalMs:Int?
    public var minMs:Int?
    public var maxMs:Int?
    public var averageMs:Double?
    public var medianMs:Double?
    public var lastError:ResponseStatus?
    public var timings:ConcurrentQueue<Int>?

    required public init(){}
}

public class CommandResult : Codable
{
    public var type:String?
    public var name:String?
    public var ms:Int?
    public var at:Date?
    public var request:String?
    public var retries:Int?
    public var attempt:Int?
    public var error:ResponseStatus?

    required public init(){}
}

public class JobStatSummary : Codable
{
    public var name:String?
    public var total:Int?
    public var completed:Int?
    public var retries:Int?
    public var failed:Int?
    public var cancelled:Int?

    required public init(){}
}

public class HourSummary : Codable
{
    public var hour:String?
    public var total:Int?
    public var completed:Int?
    public var failed:Int?
    public var cancelled:Int?

    required public init(){}
}

public class WorkerStats : Codable
{
    public var name:String?
    public var queued:Int?
    public var received:Int?
    public var completed:Int?
    public var retries:Int?
    public var failed:Int?
    public var runningJob:Int?
    @TimeSpan public var runningTime:TimeInterval?

    required public init(){}
}

public class RequestLogEntry : Codable
{
    public var id:Int?
    public var traceId:String?
    public var operationName:String?
    public var dateTime:Date?
    public var statusCode:Int?
    public var statusDescription:String?
    public var httpMethod:String?
    public var absoluteUri:String?
    public var pathInfo:String?
    // @StringLength(Int32.max)
    public var requestBody:String?

    public var requestDto:String?
    public var userAuthId:String?
    public var sessionId:String?
    public var ipAddress:String?
    public var forwardedFor:String?
    public var referer:String?
    public var headers:[String:String]?
    public var formData:[String:String]?
    public var items:[String:String]?
    public var responseHeaders:[String:String]?
    public var session:String?
    public var responseDto:String?
    public var errorResponse:String?
    public var exceptionSource:String?
    public var exceptionData:[String:String]?
    @TimeSpan public var requestDuration:TimeInterval?
    public var meta:[String:String]?

    required public init(){}
}

// @DataContract
public class AnalyticsLogInfo : Codable
{
    // @DataMember(Order=1)
    public var id:Int?

    // @DataMember(Order=2)
    public var dateTime:Date?

    // @DataMember(Order=3)
    public var browser:String?

    // @DataMember(Order=4)
    public var device:String?

    // @DataMember(Order=5)
    public var bot:String?

    // @DataMember(Order=6)
    public var op:String?

    // @DataMember(Order=7)
    public var userId:String?

    // @DataMember(Order=8)
    public var userName:String?

    // @DataMember(Order=9)
    public var apiKey:String?

    // @DataMember(Order=10)
    public var ip:String?

    required public init(){}
}

// @DataContract
public class AnalyticsReports : Codable
{
    // @DataMember(Order=1)
    public var id:Int?

    // @DataMember(Order=2)
    public var created:Date?

    // @DataMember(Order=3)
    public var version:Double?

    // @DataMember(Order=4)
    public var apis:[String:RequestSummary]?

    // @DataMember(Order=5)
    public var users:[String:RequestSummary]?

    // @DataMember(Order=6)
    public var tags:[String:RequestSummary]?

    // @DataMember(Order=7)
    public var status:[String:RequestSummary]?

    // @DataMember(Order=8)
    public var days:[String:RequestSummary]?

    // @DataMember(Order=9)
    public var apiKeys:[String:RequestSummary]?

    // @DataMember(Order=10)
    public var ips:[String:RequestSummary]?

    // @DataMember(Order=11)
    public var browsers:[String:RequestSummary]?

    // @DataMember(Order=12)
    public var devices:[String:RequestSummary]?

    // @DataMember(Order=13)
    public var bots:[String:RequestSummary]?

    // @DataMember(Order=14)
    public var durations:[String:Int]?

    required public init(){}
}

// @DataContract
public class AiContent : Codable
{
    /**
    * The type of the content part.
    */
    // @DataMember(Name="type")
    public var type:String?

    required public init(){}
}

/**
* The tool calls generated by the model, such as function calls.
*/
// @DataContract
public class ToolCall : Codable
{
    /**
    * The ID of the tool call.
    */
    // @DataMember(Name="id")
    public var id:String?

    /**
    * The type of the tool. Currently, only `function` is supported.
    */
    // @DataMember(Name="type")
    public var type:String?

    /**
    * The function that the model called.
    */
    // @DataMember(Name="function")
    public var function:String?

    required public init(){}
}

public enum ResponseFormat : String, Codable
{
    case Text
    case JsonObject
}

public enum ToolType : String, Codable
{
    case Function
}

// @DataContract
public class ModelUsage : Codable
{
    // @DataMember
    public var cost:String?

    // @DataMember
    public var input:String?

    // @DataMember
    public var output:String?

    // @DataMember
    public var duration:Int?

    // @DataMember(Name="completion_tokens")
    public var completion_tokens:Int?

    // @DataMember
    public var inputCachedTokens:Int?

    // @DataMember
    public var outputCachedTokens:Int?

    // @DataMember(Name="audio_tokens")
    public var audio_tokens:Int?

    // @DataMember(Name="reasoning_tokens")
    public var reasoning_tokens:Int?

    // @DataMember
    public var totalTokens:Int?

    required public init(){}
}

public class BackgroundJobBase : Codable
{
    public var id:Int?
    public var parentId:Int?
    public var refId:String?
    public var worker:String?
    public var tag:String?
    public var batchId:String?
    public var callback:String?
    public var dependsOn:Int?
    public var runAfter:Date?
    public var createdDate:Date?
    public var createdBy:String?
    public var requestId:String?
    public var requestType:String?
    public var command:String?
    public var request:String?
    // @StringLength(Int32.max)
    public var requestBody:String?

    public var userId:String?
    public var response:String?
    // @StringLength(Int32.max)
    public var responseBody:String?

    public var state:BackgroundJobState?
    public var startedDate:Date?
    public var completedDate:Date?
    public var notifiedDate:Date?
    public var retryLimit:Int?
    public var attempts:Int?
    public var durationMs:Int?
    public var timeoutSecs:Int?
    public var progress:Double?
    public var status:String?
    // @StringLength(Int32.max)
    public var logs:String?

    public var lastActivityDate:Date?
    public var replyTo:String?
    public var errorCode:String?
    public var error:ResponseStatus?
    public var args:[String:String]?
    public var meta:[String:String]?

    required public init(){}
}

public class BackgroundJobOptions : Codable
{
    public var refId:String?
    public var parentId:Int?
    public var worker:String?
    public var runAfter:Date?
    public var callback:String?
    public var dependsOn:Int?
    public var userId:String?
    public var retryLimit:Int?
    public var replyTo:String?
    public var tag:String?
    public var batchId:String?
    public var createdBy:String?
    public var timeoutSecs:Int?
    @TimeSpan public var timeout:TimeInterval?
    public var args:[String:String]?
    public var runCommand:Bool?

    required public init(){}
}

public class ValidateRule : Codable
{
    public var validator:String?
    public var condition:String?
    public var errorCode:String?
    public var message:String?

    required public init(){}
}

public class ImageInfo : Codable
{
    public var svg:String?
    public var uri:String?
    public var alt:String?
    public var cls:String?

    required public init(){}
}

public class LinkInfo : Codable
{
    public var id:String?
    public var href:String?
    public var label:String?
    public var icon:ImageInfo?
    public var show:String?
    public var hide:String?

    required public init(){}
}

public class ThemeInfo : Codable
{
    public var form:String?
    public var modelIcon:ImageInfo?

    required public init(){}
}

public class LocodeUi : Codable
{
    public var css:ApiCss?
    public var tags:AppTags?
    public var maxFieldLength:Int?
    public var maxNestedFields:Int?
    public var maxNestedFieldLength:Int?

    required public init(){}
}

public class ExplorerUi : Codable
{
    public var css:ApiCss?
    public var tags:AppTags?
    public var jsConfig:String?

    required public init(){}
}

public class AdminUi : Codable
{
    public var css:ApiCss?
    public var pages:[PageInfo]?

    required public init(){}
}

public class ApiFormat : Codable
{
    public var locale:String?
    public var assumeUtc:Bool?
    public var number:FormatInfo?
    public var date:FormatInfo?

    required public init(){}
}

public class AuthInfo : Codable
{
    public var hasAuthSecret:Bool?
    public var hasAuthRepository:Bool?
    public var includesRoles:Bool?
    public var includesOAuthTokens:Bool?
    public var htmlRedirect:String?
    public var authProviders:[MetaAuthProvider]?
    public var identityAuth:IdentityAuthInfo?
    public var roleLinks:[String:[LinkInfo]]?
    public var serviceRoutes:[String:[String]]?
    public var meta:[String:String]?

    required public init(){}
}

public class ApiKeyInfo : Codable
{
    public var label:String?
    public var httpHeader:String?
    public var scopes:[String]?
    public var features:[String]?
    public var requestTypes:[String]?
    public var expiresIn:[KeyValuePair<String,String>]?
    public var hide:[String]?
    public var meta:[String:String]?

    required public init(){}
}

public class CommandsInfo : Codable
{
    public var commands:[CommandInfo]?
    public var meta:[String:String]?

    required public init(){}
}

public class AutoQueryInfo : Codable
{
    public var maxLimit:Int?
    public var untypedQueries:Bool?
    public var rawSqlFilters:Bool?
    public var autoQueryViewer:Bool?
    public var async:Bool?
    public var orderByPrimaryKey:Bool?
    public var crudEvents:Bool?
    public var crudEventsServices:Bool?
    public var accessRole:String?
    public var namedConnection:String?
    public var viewerConventions:[AutoQueryConvention]?
    public var meta:[String:String]?

    required public init(){}
}

public class ValidationInfo : Codable
{
    public var hasValidationSource:Bool?
    public var hasValidationSourceAdmin:Bool?
    public var serviceRoutes:[String:[String]]?
    public var typeValidators:[ScriptMethodType]?
    public var propertyValidators:[ScriptMethodType]?
    public var accessRole:String?
    public var meta:[String:String]?

    required public init(){}
}

public class SharpPagesInfo : Codable
{
    public var apiPath:String?
    public var scriptAdminRole:String?
    public var metadataDebugAdminRole:String?
    public var metadataDebug:Bool?
    public var spaFallback:Bool?
    public var meta:[String:String]?

    required public init(){}
}

public class RequestLogsInfo : Codable
{
    public var accessRole:String?
    public var requestLogger:String?
    public var defaultLimit:Int?
    public var serviceRoutes:[String:[String]]?
    public var analytics:RequestLogsAnalytics?
    public var meta:[String:String]?

    required public init(){}
}

public class ProfilingInfo : Codable
{
    public var accessRole:String?
    public var defaultLimit:Int?
    public var summaryFields:[String]?
    public var tagLabel:String?
    public var meta:[String:String]?

    required public init(){}
}

public class FilesUploadInfo : Codable
{
    public var basePath:String?
    public var locations:[FilesUploadLocation]?
    public var meta:[String:String]?

    required public init(){}
}

public class AdminUsersInfo : Codable
{
    public var accessRole:String?
    public var enabled:[String]?
    public var userAuth:MetadataType?
    public var allRoles:[String]?
    public var allPermissions:[String]?
    public var queryUserAuthProperties:[String]?
    public var queryMediaRules:[MediaRule]?
    public var formLayout:[InputInfo]?
    public var css:ApiCss?
    public var meta:[String:String]?

    required public init(){}
}

public class AdminIdentityUsersInfo : Codable
{
    public var accessRole:String?
    public var enabled:[String]?
    public var identityUser:MetadataType?
    public var allRoles:[String]?
    public var allPermissions:[String]?
    public var queryIdentityUserProperties:[String]?
    public var queryMediaRules:[MediaRule]?
    public var formLayout:[InputInfo]?
    public var css:ApiCss?
    public var meta:[String:String]?

    required public init(){}
}

public class AdminRedisInfo : Codable
{
    public var queryLimit:Int?
    public var databases:[Int]?
    public var modifiableConnection:Bool?
    public var endpoint:RedisEndpointInfo?
    public var meta:[String:String]?

    required public init(){}
}

public class AdminDatabaseInfo : Codable
{
    public var queryLimit:Int?
    public var databases:[DatabaseInfo]?
    public var meta:[String:String]?

    required public init(){}
}

public class AdminChatInfo : Codable
{
    public var accessRole:String?
    public var defaultLimit:Int?
    public var analytics:AiChatAnalytics?
    public var meta:[String:String]?

    required public init(){}
}

public class MetadataTypesConfig : Codable
{
    public var baseUrl:String?
    public var makePartial:Bool?
    public var makeVirtual:Bool?
    public var makeInternal:Bool?
    public var baseClass:String?
    public var package:String?
    public var addReturnMarker:Bool?
    public var addDescriptionAsComments:Bool?
    public var addDocAnnotations:Bool?
    public var addDataContractAttributes:Bool?
    public var addIndexesToDataMembers:Bool?
    public var addGeneratedCodeAttributes:Bool?
    public var addImplicitVersion:Int?
    public var addResponseStatus:Bool?
    public var addServiceStackTypes:Bool?
    public var addModelExtensions:Bool?
    public var addPropertyAccessors:Bool?
    public var excludeGenericBaseTypes:Bool?
    public var settersReturnThis:Bool?
    public var addNullableAnnotations:Bool?
    public var makePropertiesOptional:Bool?
    public var exportAsTypes:Bool?
    public var excludeImplementedInterfaces:Bool?
    public var addDefaultXmlNamespace:String?
    public var makeDataContractsExtensible:Bool?
    public var initializeCollections:Bool?
    public var addNamespaces:[String]?
    public var defaultNamespaces:[String]?
    public var defaultImports:[String]?
    public var includeTypes:[String]?
    public var excludeTypes:[String]?
    public var exportTags:[String]?
    public var treatTypesAsStrings:[String]?
    public var exportValueTypes:Bool?
    public var globalNamespace:String?
    public var excludeNamespace:Bool?
    public var dataClass:String?
    public var dataClassJson:String?
    public var ignoreTypes:[String]?
    public var exportTypes:[String]?
    public var exportAttributes:[String]?
    public var ignoreTypesInNamespaces:[String]?

    required public init(){}
}

public class MetadataType : Codable
{
    public var name:String?
    public var namespace:String?
    public var genericArgs:[String]?
    public var inherits:MetadataTypeName?
    public var implements:[MetadataTypeName]?
    public var displayType:String?
    public var Description:String?
    public var notes:String?
    public var icon:ImageInfo?
    public var isNested:Bool?
    public var isEnum:Bool?
    public var isEnumInt:Bool?
    public var isInterface:Bool?
    public var isAbstract:Bool?
    public var isGenericTypeDef:Bool?
    public var dataContract:MetadataDataContract?
    public var properties:[MetadataPropertyType]?
    public var attributes:[MetadataAttribute]?
    public var innerTypes:[MetadataTypeName]?
    public var enumNames:[String]?
    public var enumValues:[String]?
    public var enumMemberValues:[String]?
    public var enumDescriptions:[String]?
    public var meta:[String:String]?

    required public init(){}
}

public class MetadataOperationType : Codable
{
    public var request:MetadataType?
    public var response:MetadataType?
    public var actions:[String]?
    public var returnsVoid:Bool?
    public var method:String?
    public var returnType:MetadataTypeName?
    public var routes:[MetadataRoute]?
    public var dataModel:MetadataTypeName?
    public var viewModel:MetadataTypeName?
    public var requiresAuth:Bool?
    public var requiresApiKey:Bool?
    public var requiredRoles:[String]?
    public var requiresAnyRole:[String]?
    public var requiredPermissions:[String]?
    public var requiresAnyPermission:[String]?
    public var tags:[String]?
    public var ui:ApiUiInfo?

    required public init(){}
}

// @DataContract
public class ChoiceMessage : Codable
{
    /**
    * The contents of the message.
    */
    // @DataMember(Name="content")
    public var content:String?

    /**
    * The refusal message generated by the model.
    */
    // @DataMember(Name="refusal")
    public var refusal:String?

    /**
    * The reasoning process used by the model.
    */
    // @DataMember(Name="reasoning")
    public var reasoning:String?

    /**
    * The role of the author of this message.
    */
    // @DataMember(Name="role")
    public var role:String?

    /**
    * Annotations for the message, when applicable, as when using the web search tool.
    */
    // @DataMember(Name="annotations")
    public var annotations:[ChoiceAnnotation]?

    /**
    * If the audio output modality is requested, this object contains data about the audio response from the model.
    */
    // @DataMember(Name="audio")
    public var audio:ChoiceAudio?

    /**
    * The tool calls generated by the model, such as function calls.
    */
    // @DataMember(Name="tool_calls")
    public var tool_calls:[ToolCall]?

    required public init(){}
}

/**
* Usage statistics for the completion request.
*/
// @DataContract
public class AiCompletionUsage : Codable
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    // @DataMember(Name="accepted_prediction_tokens")
    public var accepted_prediction_tokens:Int?

    /**
    * Audio input tokens generated by the model.
    */
    // @DataMember(Name="audio_tokens")
    public var audio_tokens:Int?

    /**
    * Tokens generated by the model for reasoning.
    */
    // @DataMember(Name="reasoning_tokens")
    public var reasoning_tokens:Int?

    /**
    * When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.
    */
    // @DataMember(Name="rejected_prediction_tokens")
    public var rejected_prediction_tokens:Int?

    required public init(){}
}

/**
* Breakdown of tokens used in the prompt.
*/
// @DataContract
public class AiPromptUsage : Codable
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    // @DataMember(Name="accepted_prediction_tokens")
    public var accepted_prediction_tokens:Int?

    /**
    * Audio input tokens present in the prompt.
    */
    // @DataMember(Name="audio_tokens")
    public var audio_tokens:Int?

    /**
    * Cached tokens present in the prompt.
    */
    // @DataMember(Name="cached_tokens")
    public var cached_tokens:Int?

    required public init(){}
}

public class MetadataDataMember : Codable
{
    public var name:String?
    public var order:Int?
    public var isRequired:Bool?
    public var emitDefaultValue:Bool?

    required public init(){}
}

public class MetadataAttribute : Codable
{
    public var name:String?
    public var constructorArgs:[MetadataPropertyType]?
    public var args:[MetadataPropertyType]?

    required public init(){}
}

public class InputInfo : Codable
{
    public var id:String?
    public var name:String?
    public var type:String?
    public var value:String?
    public var placeholder:String?
    public var help:String?
    public var label:String?
    public var title:String?
    public var size:String?
    public var pattern:String?
    public var readOnly:Bool?
    public var required:Bool?
    public var disabled:Bool?
    public var autocomplete:String?
    public var autofocus:String?
    public var min:String?
    public var max:String?
    public var step:String?
    public var minLength:Int?
    public var maxLength:Int?
    public var accept:String?
    public var capture:String?
    public var multiple:Bool?
    public var allowableValues:[String]?
    public var allowableEntries:[KeyValuePair<String, String>]?
    public var options:String?
    public var ignore:Bool?
    public var css:FieldCss?
    public var meta:[String:String]?

    required public init(){}
}

public class FormatInfo : Codable
{
    public var method:String?
    public var options:String?
    public var locale:String?

    required public init(){}
}

public class RefInfo : Codable
{
    public var model:String?
    public var selfId:String?
    public var refId:String?
    public var refLabel:String?
    public var queryApi:String?

    required public init(){}
}

// @DataContract
public class RequestSummary : Codable
{
    // @DataMember(Order=1)
    public var name:String?

    // @DataMember(Order=2)
    public var totalRequests:Int?

    // @DataMember(Order=3)
    public var totalRequestLength:Int?

    // @DataMember(Order=4)
    public var minRequestLength:Int?

    // @DataMember(Order=5)
    public var maxRequestLength:Int?

    // @DataMember(Order=6)
    public var totalDuration:Double?

    // @DataMember(Order=7)
    public var minDuration:Double?

    // @DataMember(Order=8)
    public var maxDuration:Double?

    // @DataMember(Order=9)
    public var status:[Int:Int]?

    // @DataMember(Order=10)
    public var durations:[String:Int]?

    // @DataMember(Order=11)
    public var apis:[String:Int]?

    // @DataMember(Order=12)
    public var users:[String:Int]?

    // @DataMember(Order=13)
    public var ips:[String:Int]?

    // @DataMember(Order=14)
    public var apiKeys:[String:Int]?

    required public init(){}
}

/**
* Text content part
*/
// @DataContract
public class AiTextContent : AiContent
{
    /**
    * The text content.
    */
    // @DataMember(Name="text")
    public var text:String?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case text
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        text = try container.decodeIfPresent(String.self, forKey: .text)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if text != nil { try container.encode(text, forKey: .text) }
    }
}

/**
* Image content part
*/
// @DataContract
public class AiImageContent : AiContent
{
    /**
    * The image for this content.
    */
    // @DataMember(Name="image_url")
    public var image_url:AiImageUrl?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case image_url
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        image_url = try container.decodeIfPresent(AiImageUrl.self, forKey: .image_url)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if image_url != nil { try container.encode(image_url, forKey: .image_url) }
    }
}

/**
* Audio content part
*/
// @DataContract
public class AiAudioContent : AiContent
{
    /**
    * The audio input for this content.
    */
    // @DataMember(Name="input_audio")
    public var input_audio:AiInputAudio?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case input_audio
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        input_audio = try container.decodeIfPresent(AiInputAudio.self, forKey: .input_audio)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if input_audio != nil { try container.encode(input_audio, forKey: .input_audio) }
    }
}

/**
* File content part
*/
// @DataContract
public class AiFileContent : AiContent
{
    /**
    * The file input for this content.
    */
    // @DataMember(Name="file")
    public var file:AiFile?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case file
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        file = try container.decodeIfPresent(AiFile.self, forKey: .file)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if file != nil { try container.encode(file, forKey: .file) }
    }
}

public class ApiCss : Codable
{
    public var form:String?
    public var fieldset:String?
    public var field:String?

    required public init(){}
}

public class AppTags : Codable
{
    public var `default`:String?
    public var other:String?

    required public init(){}
}

public class PageInfo : Codable
{
    public var page:String?
    public var component:String?

    required public init(){}
}

public class MetaAuthProvider : Codable
{
    public var name:String?
    public var label:String?
    public var type:String?
    public var navItem:NavItem?
    public var icon:ImageInfo?
    public var formLayout:[InputInfo]?
    public var meta:[String:String]?

    required public init(){}
}

public class IdentityAuthInfo : Codable
{
    public var hasRefreshToken:Bool?
    public var meta:[String:String]?

    required public init(){}
}

public class KeyValuePair<TKey : Codable, TValue : Codable> : Codable
{
    public var key:TKey?
    public var value:TValue?

    required public init(){}
}

public class CommandInfo : Codable
{
    public var name:String?
    public var tag:String?
    public var request:MetadataType?
    public var response:MetadataType?

    required public init(){}
}

public class AutoQueryConvention : Codable
{
    public var name:String?
    public var value:String?
    public var types:String?
    public var valueType:String?

    required public init(){}
}

public class ScriptMethodType : Codable
{
    public var name:String?
    public var paramNames:[String]?
    public var paramTypes:[String]?
    public var returnType:String?

    required public init(){}
}

public class RequestLogsAnalytics : Codable
{
    public var months:[String]?
    public var tabs:[String:String]?
    public var disableAnalytics:Bool?
    public var disableUserAnalytics:Bool?
    public var disableApiKeyAnalytics:Bool?

    required public init(){}
}

public class FilesUploadLocation : Codable
{
    public var name:String?
    public var readAccessRole:String?
    public var writeAccessRole:String?
    public var allowExtensions:[String]?
    public var allowOperations:String?
    public var maxFileCount:Int?
    public var minFileBytes:Int?
    public var maxFileBytes:Int?

    required public init(){}
}

public class MediaRule : Codable
{
    public var size:String?
    public var rule:String?
    public var applyTo:[String]?
    public var meta:[String:String]?

    required public init(){}
}

public class DatabaseInfo : Codable
{
    public var alias:String?
    public var name:String?
    public var schemas:[SchemaInfo]?

    required public init(){}
}

public class AiChatAnalytics : Codable
{
    public var months:[String]?

    required public init(){}
}

public class MetadataTypeName : Codable
{
    public var name:String?
    public var namespace:String?
    public var genericArgs:[String]?

    required public init(){}
}

public class MetadataDataContract : Codable
{
    public var name:String?
    public var namespace:String?

    required public init(){}
}

public class MetadataRoute : Codable
{
    public var path:String?
    public var verbs:String?
    public var notes:String?
    public var summary:String?

    required public init(){}
}

public class ApiUiInfo : Codable
{
    public var locodeCss:ApiCss?
    public var explorerCss:ApiCss?
    public var formLayout:[InputInfo]?
    public var meta:[String:String]?

    required public init(){}
}

/**
* Annotations for the message, when applicable, as when using the web search tool.
*/
// @DataContract
public class ChoiceAnnotation : Codable
{
    /**
    * The type of the URL citation. Always url_citation.
    */
    // @DataMember(Name="type")
    public var type:String?

    /**
    * A URL citation when using web search.
    */
    // @DataMember(Name="url_citation")
    public var url_citation:UrlCitation?

    required public init(){}
}

/**
* If the audio output modality is requested, this object contains data about the audio response from the model.
*/
// @DataContract
public class ChoiceAudio : Codable
{
    /**
    * Base64 encoded audio bytes generated by the model, in the format specified in the request.
    */
    // @DataMember(Name="data")
    public var data:String?

    /**
    * The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.
    */
    // @DataMember(Name="expires_at")
    public var expires_at:Int?

    /**
    * Unique identifier for this audio response.
    */
    // @DataMember(Name="id")
    public var id:String?

    /**
    * Transcript of the audio generated by the model.
    */
    // @DataMember(Name="transcript")
    public var transcript:String?

    required public init(){}
}

public class FieldCss : Codable
{
    public var field:String?
    public var input:String?
    public var label:String?

    required public init(){}
}

// @DataContract
public class AiImageUrl : Codable
{
    /**
    * Either a URL of the image or the base64 encoded image data.
    */
    // @DataMember(Name="url")
    public var url:String?

    required public init(){}
}

/**
* Audio content part
*/
// @DataContract
public class AiInputAudio : Codable
{
    /**
    * URL or Base64 encoded audio data.
    */
    // @DataMember(Name="data")
    public var data:String?

    /**
    * The format of the encoded audio data. Currently supports 'wav' and 'mp3'.
    */
    // @DataMember(Name="format")
    public var format:String?

    required public init(){}
}

/**
* File content part
*/
// @DataContract
public class AiFile : Codable
{
    /**
    * The URL or base64 encoded file data, used when passing the file to the model as a string.
    */
    // @DataMember(Name="file_data")
    public var file_data:String?

    /**
    * The name of the file, used when passing the file to the model as a string.
    */
    // @DataMember(Name="filename")
    public var filename:String?

    /**
    * The ID of an uploaded file to use as input.
    */
    // @DataMember(Name="file_id")
    public var file_id:String?

    required public init(){}
}

public class NavItem : Codable
{
    public var label:String?
    public var href:String?
    public var exact:Bool?
    public var id:String?
    public var className:String?
    public var iconClass:String?
    public var iconSrc:String?
    public var show:String?
    public var hide:String?
    public var children:[NavItem]?
    public var meta:[String:String]?

    required public init(){}
}

public class SchemaInfo : Codable
{
    public var alias:String?
    public var name:String?
    public var tables:[String]?

    required public init(){}
}

/**
* Annotations for the message, when applicable, as when using the web search tool.
*/
// @DataContract
public class UrlCitation : Codable
{
    /**
    * The index of the last character of the URL citation in the message.
    */
    // @DataMember(Name="end_index")
    public var end_index:Int?

    /**
    * The index of the first character of the URL citation in the message.
    */
    // @DataMember(Name="start_index")
    public var start_index:Int?

    /**
    * The title of the web resource.
    */
    // @DataMember(Name="title")
    public var title:String?

    /**
    * The URL of the web resource.
    */
    // @DataMember(Name="url")
    public var url:String?

    required public init(){}
}


