/* Options:
Date: 2025-01-11 18:31:20
SwiftVersion: 6.0
Version: 8.53
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

public class AdminDashboard : IReturn, IGet, Codable
{
    public typealias Return = AdminDashboardResponse

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

// @DataContract
public class AdminQueryApiKeys : IReturn, IGet, Codable
{
    public typealias Return = AdminApiKeysResponse

    // @DataMember(Order=1)
    public var id:Int?

    // @DataMember(Order=2)
    public var search:String?

    // @DataMember(Order=3)
    public var userId:String?

    // @DataMember(Order=4)
    public var userName:String?

    // @DataMember(Order=5)
    public var orderBy:String?

    // @DataMember(Order=6)
    public var skip:Int?

    // @DataMember(Order=7)
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
    public var ids:[Int]?

    // @DataMember(Order=11)
    public var beforeId:Int?

    // @DataMember(Order=12)
    public var afterId:Int?

    // @DataMember(Order=13)
    public var hasResponse:Bool?

    // @DataMember(Order=14)
    public var withErrors:Bool?

    // @DataMember(Order=15)
    public var enableSessionTracking:Bool?

    // @DataMember(Order=16)
    public var enableResponseTracking:Bool?

    // @DataMember(Order=17)
    public var enableErrorTracking:Bool?

    // @DataMember(Order=18)
    @TimeSpan public var durationLongerThan:TimeInterval?

    // @DataMember(Order=19)
    @TimeSpan public var durationLessThan:TimeInterval?

    // @DataMember(Order=20)
    public var skip:Int?

    // @DataMember(Order=21)
    public var take:Int?

    // @DataMember(Order=22)
    public var orderBy:String?

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

public class AdminDashboardResponse : Codable
{
    public var serverStats:ServerStats?
    public var responseStatus:ResponseStatus?

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
    public var monthDbs:[Date] = []
    public var tableCounts:[String:Int] = [:]
    public var workerStats:[WorkerStats] = []
    public var queueCounts:[String:Int] = [:]
    public var workerCounts:[String:Int] = [:]
    public var stateCounts:[BackgroundJobState:Int] = [:]
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
public class GetValidationRulesResponse : Codable
{
    // @DataMember(Order=1)
    public var results:[ValidationRule]?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

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
    public var responseBody:String?
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
    public var hideTags:[String]?
    public var modules:[String]?
    public var alwaysHideTags:[String]?
    public var adminLinks:[LinkInfo]?
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

public class ServerStats : Codable
{
    public var redis:[String:Int]?
    public var serverEvents:[String:String]?
    public var mqDescription:String?
    public var mqWorkers:[String:Int]?

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
    public var requestBody:String?
    public var userId:String?
    public var response:String?
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

    required public init(){}
}

public class AdminUi : Codable
{
    public var css:ApiCss?

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

public class MetadataTypesConfig : Codable
{
    public var baseUrl:String?
    public var usePath:String?
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

public class FieldCss : Codable
{
    public var field:String?
    public var input:String?
    public var label:String?

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


