/* Options:
Date: 2025-03-28 01:55:02
Version: 8.61
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:20000

//Package: 
//GlobalNamespace: dtos
//AddPropertyAccessors: True
//SettersReturnThis: True
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddDescriptionAsComments: True
//AddImplicitVersion: 
//IncludeTypes: 
//ExcludeTypes: 
//TreatTypesAsStrings: 
//DefaultImports: java.math.*,java.util.*,java.io.InputStream,net.servicestack.client.*,com.google.gson.annotations.*,com.google.gson.reflect.*
*/

import java.math.*;
import java.util.*;
import java.io.InputStream;
import net.servicestack.client.*;
import com.google.gson.annotations.*;
import com.google.gson.reflect.*;

public class dtos
{

    @Route(Path="/metadata/app")
    @DataContract
    public static class MetadataApp implements IReturn<AppMetadata>, IGet
    {
        @DataMember(Order=1)
        public String view = null;

        @DataMember(Order=2)
        public ArrayList<String> includeTypes = null;
        
        public String getView() { return view; }
        public MetadataApp setView(String value) { this.view = value; return this; }
        public ArrayList<String> getIncludeTypes() { return includeTypes; }
        public MetadataApp setIncludeTypes(ArrayList<String> value) { this.includeTypes = value; return this; }
        private static Object responseType = AppMetadata.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminCreateRole implements IReturn<IdResponse>, IPost
    {
        @DataMember(Order=1)
        public String name = null;
        
        public String getName() { return name; }
        public AdminCreateRole setName(String value) { this.name = value; return this; }
        private static Object responseType = IdResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminGetRoles implements IReturn<AdminGetRolesResponse>, IGet
    {
        
        private static Object responseType = AdminGetRolesResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminGetRole implements IReturn<AdminGetRoleResponse>, IGet
    {
        @DataMember(Order=1)
        public String id = null;
        
        public String getId() { return id; }
        public AdminGetRole setId(String value) { this.id = value; return this; }
        private static Object responseType = AdminGetRoleResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminUpdateRole implements IReturn<IdResponse>, IPost
    {
        @DataMember(Order=1)
        public String id = null;

        @DataMember(Order=2)
        public String name = null;

        @DataMember(Order=3)
        public ArrayList<Property> addClaims = null;

        @DataMember(Order=4)
        public ArrayList<Property> removeClaims = null;

        @DataMember(Order=5)
        public ResponseStatus responseStatus = null;
        
        public String getId() { return id; }
        public AdminUpdateRole setId(String value) { this.id = value; return this; }
        public String getName() { return name; }
        public AdminUpdateRole setName(String value) { this.name = value; return this; }
        public ArrayList<Property> getAddClaims() { return addClaims; }
        public AdminUpdateRole setAddClaims(ArrayList<Property> value) { this.addClaims = value; return this; }
        public ArrayList<Property> getRemoveClaims() { return removeClaims; }
        public AdminUpdateRole setRemoveClaims(ArrayList<Property> value) { this.removeClaims = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminUpdateRole setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
        private static Object responseType = IdResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminDeleteRole implements IReturnVoid, IDelete
    {
        @DataMember(Order=1)
        public String id = null;
        
        public String getId() { return id; }
        public AdminDeleteRole setId(String value) { this.id = value; return this; }
    }

    public static class AdminDashboard implements IReturn<AdminDashboardResponse>, IGet
    {
        
        private static Object responseType = AdminDashboardResponse.class;
        public Object getResponseType() { return responseType; }
    }

    /**
    * Sign In
    */
    @Route(Path="/auth", Verbs="GET,POST")
    // @Route(Path="/auth/{provider}", Verbs="GET,POST")
    @Api(Description="Sign In")
    @DataContract
    public static class Authenticate implements IReturn<AuthenticateResponse>, IPost
    {
        /**
        * AuthProvider, e.g. credentials
        */
        @DataMember(Order=1)
        public String provider = null;

        @DataMember(Order=2)
        public String userName = null;

        @DataMember(Order=3)
        public String password = null;

        @DataMember(Order=4)
        public Boolean rememberMe = null;

        @DataMember(Order=5)
        public String accessToken = null;

        @DataMember(Order=6)
        public String accessTokenSecret = null;

        @DataMember(Order=7)
        public String returnUrl = null;

        @DataMember(Order=8)
        public String errorView = null;

        @DataMember(Order=9)
        public HashMap<String,String> meta = null;
        
        public String getProvider() { return provider; }
        public Authenticate setProvider(String value) { this.provider = value; return this; }
        public String getUserName() { return userName; }
        public Authenticate setUserName(String value) { this.userName = value; return this; }
        public String getPassword() { return password; }
        public Authenticate setPassword(String value) { this.password = value; return this; }
        public Boolean isRememberMe() { return rememberMe; }
        public Authenticate setRememberMe(Boolean value) { this.rememberMe = value; return this; }
        public String getAccessToken() { return accessToken; }
        public Authenticate setAccessToken(String value) { this.accessToken = value; return this; }
        public String getAccessTokenSecret() { return accessTokenSecret; }
        public Authenticate setAccessTokenSecret(String value) { this.accessTokenSecret = value; return this; }
        public String getReturnUrl() { return returnUrl; }
        public Authenticate setReturnUrl(String value) { this.returnUrl = value; return this; }
        public String getErrorView() { return errorView; }
        public Authenticate setErrorView(String value) { this.errorView = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public Authenticate setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        private static Object responseType = AuthenticateResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @Route(Path="/assignroles", Verbs="POST")
    @DataContract
    public static class AssignRoles implements IReturn<AssignRolesResponse>, IPost
    {
        @DataMember(Order=1)
        public String userName = null;

        @DataMember(Order=2)
        public ArrayList<String> permissions = null;

        @DataMember(Order=3)
        public ArrayList<String> roles = null;

        @DataMember(Order=4)
        public HashMap<String,String> meta = null;
        
        public String getUserName() { return userName; }
        public AssignRoles setUserName(String value) { this.userName = value; return this; }
        public ArrayList<String> getPermissions() { return permissions; }
        public AssignRoles setPermissions(ArrayList<String> value) { this.permissions = value; return this; }
        public ArrayList<String> getRoles() { return roles; }
        public AssignRoles setRoles(ArrayList<String> value) { this.roles = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AssignRoles setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        private static Object responseType = AssignRolesResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @Route(Path="/unassignroles", Verbs="POST")
    @DataContract
    public static class UnAssignRoles implements IReturn<UnAssignRolesResponse>, IPost
    {
        @DataMember(Order=1)
        public String userName = null;

        @DataMember(Order=2)
        public ArrayList<String> permissions = null;

        @DataMember(Order=3)
        public ArrayList<String> roles = null;

        @DataMember(Order=4)
        public HashMap<String,String> meta = null;
        
        public String getUserName() { return userName; }
        public UnAssignRoles setUserName(String value) { this.userName = value; return this; }
        public ArrayList<String> getPermissions() { return permissions; }
        public UnAssignRoles setPermissions(ArrayList<String> value) { this.permissions = value; return this; }
        public ArrayList<String> getRoles() { return roles; }
        public UnAssignRoles setRoles(ArrayList<String> value) { this.roles = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public UnAssignRoles setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        private static Object responseType = UnAssignRolesResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminGetUser implements IReturn<AdminUserResponse>, IGet
    {
        @DataMember(Order=10)
        public String id = null;
        
        public String getId() { return id; }
        public AdminGetUser setId(String value) { this.id = value; return this; }
        private static Object responseType = AdminUserResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminQueryUsers implements IReturn<AdminUsersResponse>, IGet
    {
        @DataMember(Order=1)
        public String query = null;

        @DataMember(Order=2)
        public String orderBy = null;

        @DataMember(Order=3)
        public Integer skip = null;

        @DataMember(Order=4)
        public Integer take = null;
        
        public String getQuery() { return query; }
        public AdminQueryUsers setQuery(String value) { this.query = value; return this; }
        public String getOrderBy() { return orderBy; }
        public AdminQueryUsers setOrderBy(String value) { this.orderBy = value; return this; }
        public Integer getSkip() { return skip; }
        public AdminQueryUsers setSkip(Integer value) { this.skip = value; return this; }
        public Integer getTake() { return take; }
        public AdminQueryUsers setTake(Integer value) { this.take = value; return this; }
        private static Object responseType = AdminUsersResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminCreateUser extends AdminUserBase implements IReturn<AdminUserResponse>, IPost
    {
        @DataMember(Order=10)
        public ArrayList<String> roles = null;

        @DataMember(Order=11)
        public ArrayList<String> permissions = null;
        
        public ArrayList<String> getRoles() { return roles; }
        public AdminCreateUser setRoles(ArrayList<String> value) { this.roles = value; return this; }
        public ArrayList<String> getPermissions() { return permissions; }
        public AdminCreateUser setPermissions(ArrayList<String> value) { this.permissions = value; return this; }
        private static Object responseType = AdminUserResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminUpdateUser extends AdminUserBase implements IReturn<AdminUserResponse>, IPut
    {
        @DataMember(Order=10)
        public String id = null;

        @DataMember(Order=11)
        public Boolean lockUser = null;

        @DataMember(Order=12)
        public Boolean unlockUser = null;

        @DataMember(Order=13)
        public Date lockUserUntil = null;

        @DataMember(Order=14)
        public ArrayList<String> addRoles = null;

        @DataMember(Order=15)
        public ArrayList<String> removeRoles = null;

        @DataMember(Order=16)
        public ArrayList<String> addPermissions = null;

        @DataMember(Order=17)
        public ArrayList<String> removePermissions = null;

        @DataMember(Order=18)
        public ArrayList<Property> addClaims = null;

        @DataMember(Order=19)
        public ArrayList<Property> removeClaims = null;
        
        public String getId() { return id; }
        public AdminUpdateUser setId(String value) { this.id = value; return this; }
        public Boolean isLockUser() { return lockUser; }
        public AdminUpdateUser setLockUser(Boolean value) { this.lockUser = value; return this; }
        public Boolean isUnlockUser() { return unlockUser; }
        public AdminUpdateUser setUnlockUser(Boolean value) { this.unlockUser = value; return this; }
        public Date getLockUserUntil() { return lockUserUntil; }
        public AdminUpdateUser setLockUserUntil(Date value) { this.lockUserUntil = value; return this; }
        public ArrayList<String> getAddRoles() { return addRoles; }
        public AdminUpdateUser setAddRoles(ArrayList<String> value) { this.addRoles = value; return this; }
        public ArrayList<String> getRemoveRoles() { return removeRoles; }
        public AdminUpdateUser setRemoveRoles(ArrayList<String> value) { this.removeRoles = value; return this; }
        public ArrayList<String> getAddPermissions() { return addPermissions; }
        public AdminUpdateUser setAddPermissions(ArrayList<String> value) { this.addPermissions = value; return this; }
        public ArrayList<String> getRemovePermissions() { return removePermissions; }
        public AdminUpdateUser setRemovePermissions(ArrayList<String> value) { this.removePermissions = value; return this; }
        public ArrayList<Property> getAddClaims() { return addClaims; }
        public AdminUpdateUser setAddClaims(ArrayList<Property> value) { this.addClaims = value; return this; }
        public ArrayList<Property> getRemoveClaims() { return removeClaims; }
        public AdminUpdateUser setRemoveClaims(ArrayList<Property> value) { this.removeClaims = value; return this; }
        private static Object responseType = AdminUserResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminDeleteUser implements IReturn<AdminDeleteUserResponse>, IDelete
    {
        @DataMember(Order=10)
        public String id = null;
        
        public String getId() { return id; }
        public AdminDeleteUser setId(String value) { this.id = value; return this; }
        private static Object responseType = AdminDeleteUserResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class AdminQueryRequestLogs extends QueryDb<RequestLog> implements IReturn<QueryResponse<RequestLog>>
    {
        public Date month = null;
        
        public Date getMonth() { return month; }
        public AdminQueryRequestLogs setMonth(Date value) { this.month = value; return this; }
        private static Object responseType = new TypeToken<QueryResponse<RequestLog>>(){}.getType();
        public Object getResponseType() { return responseType; }
    }

    public static class AdminProfiling implements IReturn<AdminProfilingResponse>
    {
        public String source = null;
        public String eventType = null;
        public Integer threadId = null;
        public String traceId = null;
        public String userAuthId = null;
        public String sessionId = null;
        public String tag = null;
        public Integer skip = null;
        public Integer take = null;
        public String orderBy = null;
        public Boolean withErrors = null;
        public Boolean pending = null;
        
        public String getSource() { return source; }
        public AdminProfiling setSource(String value) { this.source = value; return this; }
        public String getEventType() { return eventType; }
        public AdminProfiling setEventType(String value) { this.eventType = value; return this; }
        public Integer getThreadId() { return threadId; }
        public AdminProfiling setThreadId(Integer value) { this.threadId = value; return this; }
        public String getTraceId() { return traceId; }
        public AdminProfiling setTraceId(String value) { this.traceId = value; return this; }
        public String getUserAuthId() { return userAuthId; }
        public AdminProfiling setUserAuthId(String value) { this.userAuthId = value; return this; }
        public String getSessionId() { return sessionId; }
        public AdminProfiling setSessionId(String value) { this.sessionId = value; return this; }
        public String getTag() { return tag; }
        public AdminProfiling setTag(String value) { this.tag = value; return this; }
        public Integer getSkip() { return skip; }
        public AdminProfiling setSkip(Integer value) { this.skip = value; return this; }
        public Integer getTake() { return take; }
        public AdminProfiling setTake(Integer value) { this.take = value; return this; }
        public String getOrderBy() { return orderBy; }
        public AdminProfiling setOrderBy(String value) { this.orderBy = value; return this; }
        public Boolean isWithErrors() { return withErrors; }
        public AdminProfiling setWithErrors(Boolean value) { this.withErrors = value; return this; }
        public Boolean isPending() { return pending; }
        public AdminProfiling setPending(Boolean value) { this.pending = value; return this; }
        private static Object responseType = AdminProfilingResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class AdminRedis implements IReturn<AdminRedisResponse>, IPost
    {
        public Integer db = null;
        public String query = null;
        public RedisEndpointInfo reconnect = null;
        public Integer take = null;
        public Integer position = null;
        public ArrayList<String> args = null;
        
        public Integer getDb() { return db; }
        public AdminRedis setDb(Integer value) { this.db = value; return this; }
        public String getQuery() { return query; }
        public AdminRedis setQuery(String value) { this.query = value; return this; }
        public RedisEndpointInfo getReconnect() { return reconnect; }
        public AdminRedis setReconnect(RedisEndpointInfo value) { this.reconnect = value; return this; }
        public Integer getTake() { return take; }
        public AdminRedis setTake(Integer value) { this.take = value; return this; }
        public Integer getPosition() { return position; }
        public AdminRedis setPosition(Integer value) { this.position = value; return this; }
        public ArrayList<String> getArgs() { return args; }
        public AdminRedis setArgs(ArrayList<String> value) { this.args = value; return this; }
        private static Object responseType = AdminRedisResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class AdminDatabase implements IReturn<AdminDatabaseResponse>, IGet
    {
        public String db = null;
        public String schema = null;
        public String table = null;
        public ArrayList<String> fields = null;
        public Integer take = null;
        public Integer skip = null;
        public String orderBy = null;
        public String include = null;
        
        public String getDb() { return db; }
        public AdminDatabase setDb(String value) { this.db = value; return this; }
        public String getSchema() { return schema; }
        public AdminDatabase setSchema(String value) { this.schema = value; return this; }
        public String getTable() { return table; }
        public AdminDatabase setTable(String value) { this.table = value; return this; }
        public ArrayList<String> getFields() { return fields; }
        public AdminDatabase setFields(ArrayList<String> value) { this.fields = value; return this; }
        public Integer getTake() { return take; }
        public AdminDatabase setTake(Integer value) { this.take = value; return this; }
        public Integer getSkip() { return skip; }
        public AdminDatabase setSkip(Integer value) { this.skip = value; return this; }
        public String getOrderBy() { return orderBy; }
        public AdminDatabase setOrderBy(String value) { this.orderBy = value; return this; }
        public String getInclude() { return include; }
        public AdminDatabase setInclude(String value) { this.include = value; return this; }
        private static Object responseType = AdminDatabaseResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class ViewCommands implements IReturn<ViewCommandsResponse>, IGet
    {
        public ArrayList<String> include = null;
        public Integer skip = null;
        public Integer take = null;
        
        public ArrayList<String> getInclude() { return include; }
        public ViewCommands setInclude(ArrayList<String> value) { this.include = value; return this; }
        public Integer getSkip() { return skip; }
        public ViewCommands setSkip(Integer value) { this.skip = value; return this; }
        public Integer getTake() { return take; }
        public ViewCommands setTake(Integer value) { this.take = value; return this; }
        private static Object responseType = ViewCommandsResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class ExecuteCommand implements IReturn<ExecuteCommandResponse>, IPost
    {
        public String command = null;
        public String requestJson = null;
        
        public String getCommand() { return command; }
        public ExecuteCommand setCommand(String value) { this.command = value; return this; }
        public String getRequestJson() { return requestJson; }
        public ExecuteCommand setRequestJson(String value) { this.requestJson = value; return this; }
        private static Object responseType = ExecuteCommandResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminQueryApiKeys implements IReturn<AdminApiKeysResponse>, IGet
    {
        @DataMember(Order=1)
        public Integer id = null;

        @DataMember(Order=2)
        public String apiKey = null;

        @DataMember(Order=3)
        public String search = null;

        @DataMember(Order=4)
        public String userId = null;

        @DataMember(Order=5)
        public String userName = null;

        @DataMember(Order=6)
        public String orderBy = null;

        @DataMember(Order=7)
        public Integer skip = null;

        @DataMember(Order=8)
        public Integer take = null;
        
        public Integer getId() { return id; }
        public AdminQueryApiKeys setId(Integer value) { this.id = value; return this; }
        public String getApiKey() { return apiKey; }
        public AdminQueryApiKeys setApiKey(String value) { this.apiKey = value; return this; }
        public String getSearch() { return search; }
        public AdminQueryApiKeys setSearch(String value) { this.search = value; return this; }
        public String getUserId() { return userId; }
        public AdminQueryApiKeys setUserId(String value) { this.userId = value; return this; }
        public String getUserName() { return userName; }
        public AdminQueryApiKeys setUserName(String value) { this.userName = value; return this; }
        public String getOrderBy() { return orderBy; }
        public AdminQueryApiKeys setOrderBy(String value) { this.orderBy = value; return this; }
        public Integer getSkip() { return skip; }
        public AdminQueryApiKeys setSkip(Integer value) { this.skip = value; return this; }
        public Integer getTake() { return take; }
        public AdminQueryApiKeys setTake(Integer value) { this.take = value; return this; }
        private static Object responseType = AdminApiKeysResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminCreateApiKey implements IReturn<AdminApiKeyResponse>, IPost
    {
        @DataMember(Order=1)
        public String name = null;

        @DataMember(Order=2)
        public String userId = null;

        @DataMember(Order=3)
        public String userName = null;

        @DataMember(Order=4)
        public ArrayList<String> scopes = null;

        @DataMember(Order=5)
        public ArrayList<String> features = null;

        @DataMember(Order=6)
        public ArrayList<String> restrictTo = null;

        @DataMember(Order=7)
        public Date expiryDate = null;

        @DataMember(Order=8)
        public String notes = null;

        @DataMember(Order=9)
        public Integer refId = null;

        @DataMember(Order=10)
        public String refIdStr = null;

        @DataMember(Order=11)
        public HashMap<String,String> meta = null;
        
        public String getName() { return name; }
        public AdminCreateApiKey setName(String value) { this.name = value; return this; }
        public String getUserId() { return userId; }
        public AdminCreateApiKey setUserId(String value) { this.userId = value; return this; }
        public String getUserName() { return userName; }
        public AdminCreateApiKey setUserName(String value) { this.userName = value; return this; }
        public ArrayList<String> getScopes() { return scopes; }
        public AdminCreateApiKey setScopes(ArrayList<String> value) { this.scopes = value; return this; }
        public ArrayList<String> getFeatures() { return features; }
        public AdminCreateApiKey setFeatures(ArrayList<String> value) { this.features = value; return this; }
        public ArrayList<String> getRestrictTo() { return restrictTo; }
        public AdminCreateApiKey setRestrictTo(ArrayList<String> value) { this.restrictTo = value; return this; }
        public Date getExpiryDate() { return expiryDate; }
        public AdminCreateApiKey setExpiryDate(Date value) { this.expiryDate = value; return this; }
        public String getNotes() { return notes; }
        public AdminCreateApiKey setNotes(String value) { this.notes = value; return this; }
        public Integer getRefId() { return refId; }
        public AdminCreateApiKey setRefId(Integer value) { this.refId = value; return this; }
        public String getRefIdStr() { return refIdStr; }
        public AdminCreateApiKey setRefIdStr(String value) { this.refIdStr = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AdminCreateApiKey setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        private static Object responseType = AdminApiKeyResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminUpdateApiKey implements IReturn<EmptyResponse>, IPatch
    {
        @DataMember(Order=1)
        @Validate(Validator="GreaterThan(0)")
        public Integer id = null;

        @DataMember(Order=2)
        public String name = null;

        @DataMember(Order=3)
        public String userId = null;

        @DataMember(Order=4)
        public String userName = null;

        @DataMember(Order=5)
        public ArrayList<String> scopes = null;

        @DataMember(Order=6)
        public ArrayList<String> features = null;

        @DataMember(Order=7)
        public ArrayList<String> restrictTo = null;

        @DataMember(Order=8)
        public Date expiryDate = null;

        @DataMember(Order=9)
        public Date cancelledDate = null;

        @DataMember(Order=10)
        public String notes = null;

        @DataMember(Order=11)
        public Integer refId = null;

        @DataMember(Order=12)
        public String refIdStr = null;

        @DataMember(Order=13)
        public HashMap<String,String> meta = null;

        @DataMember(Order=14)
        public ArrayList<String> reset = null;
        
        public Integer getId() { return id; }
        public AdminUpdateApiKey setId(Integer value) { this.id = value; return this; }
        public String getName() { return name; }
        public AdminUpdateApiKey setName(String value) { this.name = value; return this; }
        public String getUserId() { return userId; }
        public AdminUpdateApiKey setUserId(String value) { this.userId = value; return this; }
        public String getUserName() { return userName; }
        public AdminUpdateApiKey setUserName(String value) { this.userName = value; return this; }
        public ArrayList<String> getScopes() { return scopes; }
        public AdminUpdateApiKey setScopes(ArrayList<String> value) { this.scopes = value; return this; }
        public ArrayList<String> getFeatures() { return features; }
        public AdminUpdateApiKey setFeatures(ArrayList<String> value) { this.features = value; return this; }
        public ArrayList<String> getRestrictTo() { return restrictTo; }
        public AdminUpdateApiKey setRestrictTo(ArrayList<String> value) { this.restrictTo = value; return this; }
        public Date getExpiryDate() { return expiryDate; }
        public AdminUpdateApiKey setExpiryDate(Date value) { this.expiryDate = value; return this; }
        public Date getCancelledDate() { return cancelledDate; }
        public AdminUpdateApiKey setCancelledDate(Date value) { this.cancelledDate = value; return this; }
        public String getNotes() { return notes; }
        public AdminUpdateApiKey setNotes(String value) { this.notes = value; return this; }
        public Integer getRefId() { return refId; }
        public AdminUpdateApiKey setRefId(Integer value) { this.refId = value; return this; }
        public String getRefIdStr() { return refIdStr; }
        public AdminUpdateApiKey setRefIdStr(String value) { this.refIdStr = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AdminUpdateApiKey setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        public ArrayList<String> getReset() { return reset; }
        public AdminUpdateApiKey setReset(ArrayList<String> value) { this.reset = value; return this; }
        private static Object responseType = EmptyResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class AdminDeleteApiKey implements IReturn<EmptyResponse>, IDelete
    {
        @DataMember(Order=1)
        @Validate(Validator="GreaterThan(0)")
        public Integer id = null;
        
        public Integer getId() { return id; }
        public AdminDeleteApiKey setId(Integer value) { this.id = value; return this; }
        private static Object responseType = EmptyResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class AdminJobDashboard implements IReturn<AdminJobDashboardResponse>, IGet
    {
        public Date from = null;
        public Date to = null;
        
        public Date getFrom() { return from; }
        public AdminJobDashboard setFrom(Date value) { this.from = value; return this; }
        public Date getTo() { return to; }
        public AdminJobDashboard setTo(Date value) { this.to = value; return this; }
        private static Object responseType = AdminJobDashboardResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class AdminJobInfo implements IReturn<AdminJobInfoResponse>, IGet
    {
        public Date month = null;
        
        public Date getMonth() { return month; }
        public AdminJobInfo setMonth(Date value) { this.month = value; return this; }
        private static Object responseType = AdminJobInfoResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class AdminGetJob implements IReturn<AdminGetJobResponse>, IGet
    {
        public Long id = null;
        public String refId = null;
        
        public Long getId() { return id; }
        public AdminGetJob setId(Long value) { this.id = value; return this; }
        public String getRefId() { return refId; }
        public AdminGetJob setRefId(String value) { this.refId = value; return this; }
        private static Object responseType = AdminGetJobResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class AdminGetJobProgress implements IReturn<AdminGetJobProgressResponse>, IGet
    {
        @Validate(Validator="GreaterThan(0)")
        public Long id = null;

        public Integer logStart = null;
        
        public Long getId() { return id; }
        public AdminGetJobProgress setId(Long value) { this.id = value; return this; }
        public Integer getLogStart() { return logStart; }
        public AdminGetJobProgress setLogStart(Integer value) { this.logStart = value; return this; }
        private static Object responseType = AdminGetJobProgressResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class AdminQueryBackgroundJobs extends QueryDb<BackgroundJob> implements IReturn<QueryResponse<BackgroundJob>>
    {
        public Integer id = null;
        public String refId = null;
        
        public Integer getId() { return id; }
        public AdminQueryBackgroundJobs setId(Integer value) { this.id = value; return this; }
        public String getRefId() { return refId; }
        public AdminQueryBackgroundJobs setRefId(String value) { this.refId = value; return this; }
        private static Object responseType = new TypeToken<QueryResponse<BackgroundJob>>(){}.getType();
        public Object getResponseType() { return responseType; }
    }

    public static class AdminQueryJobSummary extends QueryDb<JobSummary> implements IReturn<QueryResponse<JobSummary>>
    {
        public Integer id = null;
        public String refId = null;
        
        public Integer getId() { return id; }
        public AdminQueryJobSummary setId(Integer value) { this.id = value; return this; }
        public String getRefId() { return refId; }
        public AdminQueryJobSummary setRefId(String value) { this.refId = value; return this; }
        private static Object responseType = new TypeToken<QueryResponse<JobSummary>>(){}.getType();
        public Object getResponseType() { return responseType; }
    }

    public static class AdminQueryScheduledTasks extends QueryDb<ScheduledTask> implements IReturn<QueryResponse<ScheduledTask>>
    {
        
        private static Object responseType = new TypeToken<QueryResponse<ScheduledTask>>(){}.getType();
        public Object getResponseType() { return responseType; }
    }

    public static class AdminQueryCompletedJobs extends QueryDb<CompletedJob> implements IReturn<QueryResponse<CompletedJob>>
    {
        public Date month = null;
        
        public Date getMonth() { return month; }
        public AdminQueryCompletedJobs setMonth(Date value) { this.month = value; return this; }
        private static Object responseType = new TypeToken<QueryResponse<CompletedJob>>(){}.getType();
        public Object getResponseType() { return responseType; }
    }

    public static class AdminQueryFailedJobs extends QueryDb<FailedJob> implements IReturn<QueryResponse<FailedJob>>
    {
        public Date month = null;
        
        public Date getMonth() { return month; }
        public AdminQueryFailedJobs setMonth(Date value) { this.month = value; return this; }
        private static Object responseType = new TypeToken<QueryResponse<FailedJob>>(){}.getType();
        public Object getResponseType() { return responseType; }
    }

    public static class AdminRequeueFailedJobs implements IReturn<AdminRequeueFailedJobsJobsResponse>
    {
        public ArrayList<Long> ids = null;
        
        public ArrayList<Long> getIds() { return ids; }
        public AdminRequeueFailedJobs setIds(ArrayList<Long> value) { this.ids = value; return this; }
        private static Object responseType = AdminRequeueFailedJobsJobsResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class AdminCancelJobs implements IReturn<AdminCancelJobsResponse>, IGet
    {
        public ArrayList<Long> ids = null;
        public String worker = null;
        public BackgroundJobState state = null;
        public String cancelWorker = null;
        
        public ArrayList<Long> getIds() { return ids; }
        public AdminCancelJobs setIds(ArrayList<Long> value) { this.ids = value; return this; }
        public String getWorker() { return worker; }
        public AdminCancelJobs setWorker(String value) { this.worker = value; return this; }
        public BackgroundJobState getState() { return state; }
        public AdminCancelJobs setState(BackgroundJobState value) { this.state = value; return this; }
        public String getCancelWorker() { return cancelWorker; }
        public AdminCancelJobs setCancelWorker(String value) { this.cancelWorker = value; return this; }
        private static Object responseType = AdminCancelJobsResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @Route(Path="/requestlogs")
    @DataContract
    public static class RequestLogs implements IReturn<RequestLogsResponse>, IGet
    {
        @DataMember(Order=1)
        public Integer beforeSecs = null;

        @DataMember(Order=2)
        public Integer afterSecs = null;

        @DataMember(Order=3)
        public String operationName = null;

        @DataMember(Order=4)
        public String ipAddress = null;

        @DataMember(Order=5)
        public String forwardedFor = null;

        @DataMember(Order=6)
        public String userAuthId = null;

        @DataMember(Order=7)
        public String sessionId = null;

        @DataMember(Order=8)
        public String referer = null;

        @DataMember(Order=9)
        public String pathInfo = null;

        @DataMember(Order=10)
        public String bearerToken = null;

        @DataMember(Order=11)
        public ArrayList<Long> ids = null;

        @DataMember(Order=12)
        public Integer beforeId = null;

        @DataMember(Order=13)
        public Integer afterId = null;

        @DataMember(Order=14)
        public Boolean hasResponse = null;

        @DataMember(Order=15)
        public Boolean withErrors = null;

        @DataMember(Order=16)
        public Boolean enableSessionTracking = null;

        @DataMember(Order=17)
        public Boolean enableResponseTracking = null;

        @DataMember(Order=18)
        public Boolean enableErrorTracking = null;

        @DataMember(Order=19)
        public TimeSpan durationLongerThan = null;

        @DataMember(Order=20)
        public TimeSpan durationLessThan = null;

        @DataMember(Order=21)
        public Integer skip = null;

        @DataMember(Order=22)
        public Integer take = null;

        @DataMember(Order=23)
        public String orderBy = null;

        @DataMember(Order=24)
        public Date month = null;
        
        public Integer getBeforeSecs() { return beforeSecs; }
        public RequestLogs setBeforeSecs(Integer value) { this.beforeSecs = value; return this; }
        public Integer getAfterSecs() { return afterSecs; }
        public RequestLogs setAfterSecs(Integer value) { this.afterSecs = value; return this; }
        public String getOperationName() { return operationName; }
        public RequestLogs setOperationName(String value) { this.operationName = value; return this; }
        public String getIpAddress() { return ipAddress; }
        public RequestLogs setIpAddress(String value) { this.ipAddress = value; return this; }
        public String getForwardedFor() { return forwardedFor; }
        public RequestLogs setForwardedFor(String value) { this.forwardedFor = value; return this; }
        public String getUserAuthId() { return userAuthId; }
        public RequestLogs setUserAuthId(String value) { this.userAuthId = value; return this; }
        public String getSessionId() { return sessionId; }
        public RequestLogs setSessionId(String value) { this.sessionId = value; return this; }
        public String getReferer() { return referer; }
        public RequestLogs setReferer(String value) { this.referer = value; return this; }
        public String getPathInfo() { return pathInfo; }
        public RequestLogs setPathInfo(String value) { this.pathInfo = value; return this; }
        public String getBearerToken() { return bearerToken; }
        public RequestLogs setBearerToken(String value) { this.bearerToken = value; return this; }
        public ArrayList<Long> getIds() { return ids; }
        public RequestLogs setIds(ArrayList<Long> value) { this.ids = value; return this; }
        public Integer getBeforeId() { return beforeId; }
        public RequestLogs setBeforeId(Integer value) { this.beforeId = value; return this; }
        public Integer getAfterId() { return afterId; }
        public RequestLogs setAfterId(Integer value) { this.afterId = value; return this; }
        public Boolean isHasResponse() { return hasResponse; }
        public RequestLogs setHasResponse(Boolean value) { this.hasResponse = value; return this; }
        public Boolean isWithErrors() { return withErrors; }
        public RequestLogs setWithErrors(Boolean value) { this.withErrors = value; return this; }
        public Boolean isEnableSessionTracking() { return enableSessionTracking; }
        public RequestLogs setEnableSessionTracking(Boolean value) { this.enableSessionTracking = value; return this; }
        public Boolean isEnableResponseTracking() { return enableResponseTracking; }
        public RequestLogs setEnableResponseTracking(Boolean value) { this.enableResponseTracking = value; return this; }
        public Boolean isEnableErrorTracking() { return enableErrorTracking; }
        public RequestLogs setEnableErrorTracking(Boolean value) { this.enableErrorTracking = value; return this; }
        public TimeSpan getDurationLongerThan() { return durationLongerThan; }
        public RequestLogs setDurationLongerThan(TimeSpan value) { this.durationLongerThan = value; return this; }
        public TimeSpan getDurationLessThan() { return durationLessThan; }
        public RequestLogs setDurationLessThan(TimeSpan value) { this.durationLessThan = value; return this; }
        public Integer getSkip() { return skip; }
        public RequestLogs setSkip(Integer value) { this.skip = value; return this; }
        public Integer getTake() { return take; }
        public RequestLogs setTake(Integer value) { this.take = value; return this; }
        public String getOrderBy() { return orderBy; }
        public RequestLogs setOrderBy(String value) { this.orderBy = value; return this; }
        public Date getMonth() { return month; }
        public RequestLogs setMonth(Date value) { this.month = value; return this; }
        private static Object responseType = RequestLogsResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class GetAnalyticsInfo implements IReturn<GetAnalyticsInfoResponse>, IGet
    {
        @DataMember(Order=1)
        public Date month = null;

        @DataMember(Order=2)
        public String type = null;

        @DataMember(Order=3)
        public String op = null;

        @DataMember(Order=4)
        public String apiKey = null;

        @DataMember(Order=5)
        public String userId = null;

        @DataMember(Order=6)
        public String ip = null;
        
        public Date getMonth() { return month; }
        public GetAnalyticsInfo setMonth(Date value) { this.month = value; return this; }
        public String getType() { return type; }
        public GetAnalyticsInfo setType(String value) { this.type = value; return this; }
        public String getOp() { return op; }
        public GetAnalyticsInfo setOp(String value) { this.op = value; return this; }
        public String getApiKey() { return apiKey; }
        public GetAnalyticsInfo setApiKey(String value) { this.apiKey = value; return this; }
        public String getUserId() { return userId; }
        public GetAnalyticsInfo setUserId(String value) { this.userId = value; return this; }
        public String getIp() { return ip; }
        public GetAnalyticsInfo setIp(String value) { this.ip = value; return this; }
        private static Object responseType = GetAnalyticsInfoResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class GetAnalyticsReports implements IReturn<GetAnalyticsReportsResponse>, IGet
    {
        @DataMember(Order=1)
        public Date month = null;

        @DataMember(Order=2)
        public String filter = null;

        @DataMember(Order=3)
        public String value = null;

        @DataMember(Order=4)
        public Boolean force = null;
        
        public Date getMonth() { return month; }
        public GetAnalyticsReports setMonth(Date value) { this.month = value; return this; }
        public String getFilter() { return filter; }
        public GetAnalyticsReports setFilter(String value) { this.filter = value; return this; }
        public String getValue() { return value; }
        public GetAnalyticsReports setValue(String value) { this.value = value; return this; }
        public Boolean isForce() { return force; }
        public GetAnalyticsReports setForce(Boolean value) { this.force = value; return this; }
        private static Object responseType = GetAnalyticsReportsResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @Route(Path="/validation/rules/{Type}")
    @DataContract
    public static class GetValidationRules implements IReturn<GetValidationRulesResponse>, IGet
    {
        @DataMember(Order=1)
        public String authSecret = null;

        @DataMember(Order=2)
        public String type = null;
        
        public String getAuthSecret() { return authSecret; }
        public GetValidationRules setAuthSecret(String value) { this.authSecret = value; return this; }
        public String getType() { return type; }
        public GetValidationRules setType(String value) { this.type = value; return this; }
        private static Object responseType = GetValidationRulesResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @Route(Path="/validation/rules")
    @DataContract
    public static class ModifyValidationRules implements IReturnVoid
    {
        @DataMember(Order=1)
        public String authSecret = null;

        @DataMember(Order=2)
        public ArrayList<ValidationRule> saveRules = null;

        @DataMember(Order=3)
        public ArrayList<Integer> deleteRuleIds = null;

        @DataMember(Order=4)
        public ArrayList<Integer> suspendRuleIds = null;

        @DataMember(Order=5)
        public ArrayList<Integer> unsuspendRuleIds = null;

        @DataMember(Order=6)
        public Boolean clearCache = null;
        
        public String getAuthSecret() { return authSecret; }
        public ModifyValidationRules setAuthSecret(String value) { this.authSecret = value; return this; }
        public ArrayList<ValidationRule> getSaveRules() { return saveRules; }
        public ModifyValidationRules setSaveRules(ArrayList<ValidationRule> value) { this.saveRules = value; return this; }
        public ArrayList<Integer> getDeleteRuleIds() { return deleteRuleIds; }
        public ModifyValidationRules setDeleteRuleIds(ArrayList<Integer> value) { this.deleteRuleIds = value; return this; }
        public ArrayList<Integer> getSuspendRuleIds() { return suspendRuleIds; }
        public ModifyValidationRules setSuspendRuleIds(ArrayList<Integer> value) { this.suspendRuleIds = value; return this; }
        public ArrayList<Integer> getUnsuspendRuleIds() { return unsuspendRuleIds; }
        public ModifyValidationRules setUnsuspendRuleIds(ArrayList<Integer> value) { this.unsuspendRuleIds = value; return this; }
        public Boolean isClearCache() { return clearCache; }
        public ModifyValidationRules setClearCache(Boolean value) { this.clearCache = value; return this; }
    }

    public static class AppMetadata
    {
        public Date date = null;
        public AppInfo app = null;
        public UiInfo ui = null;
        public ConfigInfo config = null;
        public HashMap<String,String> contentTypeFormats = null;
        public HashMap<String,String> httpHandlers = null;
        public PluginInfo plugins = null;
        public HashMap<String,CustomPluginInfo> customPlugins = null;
        public MetadataTypes api = null;
        public HashMap<String,String> meta = null;
        
        public Date getDate() { return date; }
        public AppMetadata setDate(Date value) { this.date = value; return this; }
        public AppInfo getApp() { return app; }
        public AppMetadata setApp(AppInfo value) { this.app = value; return this; }
        public UiInfo getUi() { return ui; }
        public AppMetadata setUi(UiInfo value) { this.ui = value; return this; }
        public ConfigInfo getConfig() { return config; }
        public AppMetadata setConfig(ConfigInfo value) { this.config = value; return this; }
        public HashMap<String,String> getContentTypeFormats() { return contentTypeFormats; }
        public AppMetadata setContentTypeFormats(HashMap<String,String> value) { this.contentTypeFormats = value; return this; }
        public HashMap<String,String> getHttpHandlers() { return httpHandlers; }
        public AppMetadata setHttpHandlers(HashMap<String,String> value) { this.httpHandlers = value; return this; }
        public PluginInfo getPlugins() { return plugins; }
        public AppMetadata setPlugins(PluginInfo value) { this.plugins = value; return this; }
        public HashMap<String,CustomPluginInfo> getCustomPlugins() { return customPlugins; }
        public AppMetadata setCustomPlugins(HashMap<String,CustomPluginInfo> value) { this.customPlugins = value; return this; }
        public MetadataTypes getApi() { return api; }
        public AppMetadata setApi(MetadataTypes value) { this.api = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AppMetadata setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    @DataContract
    public static class IdResponse
    {
        @DataMember(Order=1)
        public String id = null;

        @DataMember(Order=2)
        public ResponseStatus responseStatus = null;
        
        public String getId() { return id; }
        public IdResponse setId(String value) { this.id = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public IdResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class AdminGetRolesResponse
    {
        @DataMember(Order=1)
        public ArrayList<AdminRole> results = null;

        @DataMember(Order=2)
        public ResponseStatus responseStatus = null;
        
        public ArrayList<AdminRole> getResults() { return results; }
        public AdminGetRolesResponse setResults(ArrayList<AdminRole> value) { this.results = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminGetRolesResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class AdminGetRoleResponse
    {
        @DataMember(Order=1)
        public AdminRole result = null;

        @DataMember(Order=2)
        public ArrayList<Property> claims = null;

        @DataMember(Order=3)
        public ResponseStatus responseStatus = null;
        
        public AdminRole getResult() { return result; }
        public AdminGetRoleResponse setResult(AdminRole value) { this.result = value; return this; }
        public ArrayList<Property> getClaims() { return claims; }
        public AdminGetRoleResponse setClaims(ArrayList<Property> value) { this.claims = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminGetRoleResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class AdminDashboardResponse
    {
        public ServerStats serverStats = null;
        public ResponseStatus responseStatus = null;
        
        public ServerStats getServerStats() { return serverStats; }
        public AdminDashboardResponse setServerStats(ServerStats value) { this.serverStats = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminDashboardResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class AuthenticateResponse implements IHasSessionId, IHasBearerToken
    {
        @DataMember(Order=1)
        public String userId = null;

        @DataMember(Order=2)
        public String sessionId = null;

        @DataMember(Order=3)
        public String userName = null;

        @DataMember(Order=4)
        public String displayName = null;

        @DataMember(Order=5)
        public String referrerUrl = null;

        @DataMember(Order=6)
        public String bearerToken = null;

        @DataMember(Order=7)
        public String refreshToken = null;

        @DataMember(Order=8)
        public Date refreshTokenExpiry = null;

        @DataMember(Order=9)
        public String profileUrl = null;

        @DataMember(Order=10)
        public ArrayList<String> roles = null;

        @DataMember(Order=11)
        public ArrayList<String> permissions = null;

        @DataMember(Order=12)
        public String authProvider = null;

        @DataMember(Order=13)
        public ResponseStatus responseStatus = null;

        @DataMember(Order=14)
        public HashMap<String,String> meta = null;
        
        public String getUserId() { return userId; }
        public AuthenticateResponse setUserId(String value) { this.userId = value; return this; }
        public String getSessionId() { return sessionId; }
        public AuthenticateResponse setSessionId(String value) { this.sessionId = value; return this; }
        public String getUserName() { return userName; }
        public AuthenticateResponse setUserName(String value) { this.userName = value; return this; }
        public String getDisplayName() { return displayName; }
        public AuthenticateResponse setDisplayName(String value) { this.displayName = value; return this; }
        public String getReferrerUrl() { return referrerUrl; }
        public AuthenticateResponse setReferrerUrl(String value) { this.referrerUrl = value; return this; }
        public String getBearerToken() { return bearerToken; }
        public AuthenticateResponse setBearerToken(String value) { this.bearerToken = value; return this; }
        public String getRefreshToken() { return refreshToken; }
        public AuthenticateResponse setRefreshToken(String value) { this.refreshToken = value; return this; }
        public Date getRefreshTokenExpiry() { return refreshTokenExpiry; }
        public AuthenticateResponse setRefreshTokenExpiry(Date value) { this.refreshTokenExpiry = value; return this; }
        public String getProfileUrl() { return profileUrl; }
        public AuthenticateResponse setProfileUrl(String value) { this.profileUrl = value; return this; }
        public ArrayList<String> getRoles() { return roles; }
        public AuthenticateResponse setRoles(ArrayList<String> value) { this.roles = value; return this; }
        public ArrayList<String> getPermissions() { return permissions; }
        public AuthenticateResponse setPermissions(ArrayList<String> value) { this.permissions = value; return this; }
        public String getAuthProvider() { return authProvider; }
        public AuthenticateResponse setAuthProvider(String value) { this.authProvider = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AuthenticateResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AuthenticateResponse setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    @DataContract
    public static class AssignRolesResponse
    {
        @DataMember(Order=1)
        public ArrayList<String> allRoles = null;

        @DataMember(Order=2)
        public ArrayList<String> allPermissions = null;

        @DataMember(Order=3)
        public HashMap<String,String> meta = null;

        @DataMember(Order=4)
        public ResponseStatus responseStatus = null;
        
        public ArrayList<String> getAllRoles() { return allRoles; }
        public AssignRolesResponse setAllRoles(ArrayList<String> value) { this.allRoles = value; return this; }
        public ArrayList<String> getAllPermissions() { return allPermissions; }
        public AssignRolesResponse setAllPermissions(ArrayList<String> value) { this.allPermissions = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AssignRolesResponse setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AssignRolesResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class UnAssignRolesResponse
    {
        @DataMember(Order=1)
        public ArrayList<String> allRoles = null;

        @DataMember(Order=2)
        public ArrayList<String> allPermissions = null;

        @DataMember(Order=3)
        public HashMap<String,String> meta = null;

        @DataMember(Order=4)
        public ResponseStatus responseStatus = null;
        
        public ArrayList<String> getAllRoles() { return allRoles; }
        public UnAssignRolesResponse setAllRoles(ArrayList<String> value) { this.allRoles = value; return this; }
        public ArrayList<String> getAllPermissions() { return allPermissions; }
        public UnAssignRolesResponse setAllPermissions(ArrayList<String> value) { this.allPermissions = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public UnAssignRolesResponse setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public UnAssignRolesResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class AdminUserResponse
    {
        @DataMember(Order=1)
        public String id = null;

        @DataMember(Order=2)
        public HashMap<String,Object> result = null;

        @DataMember(Order=3)
        public ArrayList<HashMap<String,Object>> details = null;

        @DataMember(Order=4)
        public ArrayList<Property> claims = null;

        @DataMember(Order=5)
        public ResponseStatus responseStatus = null;
        
        public String getId() { return id; }
        public AdminUserResponse setId(String value) { this.id = value; return this; }
        public HashMap<String,Object> getResult() { return result; }
        public AdminUserResponse setResult(HashMap<String,Object> value) { this.result = value; return this; }
        public ArrayList<HashMap<String,Object>> getDetails() { return details; }
        public AdminUserResponse setDetails(ArrayList<HashMap<String,Object>> value) { this.details = value; return this; }
        public ArrayList<Property> getClaims() { return claims; }
        public AdminUserResponse setClaims(ArrayList<Property> value) { this.claims = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminUserResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class AdminUsersResponse
    {
        @DataMember(Order=1)
        public ArrayList<HashMap<String,Object>> results = null;

        @DataMember(Order=2)
        public ResponseStatus responseStatus = null;
        
        public ArrayList<HashMap<String,Object>> getResults() { return results; }
        public AdminUsersResponse setResults(ArrayList<HashMap<String,Object>> value) { this.results = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminUsersResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class AdminDeleteUserResponse
    {
        @DataMember(Order=1)
        public String id = null;

        @DataMember(Order=2)
        public ResponseStatus responseStatus = null;
        
        public String getId() { return id; }
        public AdminDeleteUserResponse setId(String value) { this.id = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminDeleteUserResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class QueryResponse<T>
    {
        @DataMember(Order=1)
        public Integer offset = null;

        @DataMember(Order=2)
        public Integer total = null;

        @DataMember(Order=3)
        public ArrayList<RequestLog> results = null;

        @DataMember(Order=4)
        public HashMap<String,String> meta = null;

        @DataMember(Order=5)
        public ResponseStatus responseStatus = null;
        
        public Integer getOffset() { return offset; }
        public QueryResponse<T> setOffset(Integer value) { this.offset = value; return this; }
        public Integer getTotal() { return total; }
        public QueryResponse<T> setTotal(Integer value) { this.total = value; return this; }
        public ArrayList<RequestLog> getResults() { return results; }
        public QueryResponse<T> setResults(ArrayList<RequestLog> value) { this.results = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public QueryResponse<T> setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public QueryResponse<T> setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class AdminProfilingResponse
    {
        public ArrayList<DiagnosticEntry> results = new ArrayList<DiagnosticEntry>();
        public Integer total = null;
        public ResponseStatus responseStatus = null;
        
        public ArrayList<DiagnosticEntry> getResults() { return results; }
        public AdminProfilingResponse setResults(ArrayList<DiagnosticEntry> value) { this.results = value; return this; }
        public Integer getTotal() { return total; }
        public AdminProfilingResponse setTotal(Integer value) { this.total = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminProfilingResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class AdminRedisResponse
    {
        public Long db = null;
        public ArrayList<RedisSearchResult> searchResults = null;
        public HashMap<String,String> info = null;
        public RedisEndpointInfo endpoint = null;
        public RedisText result = null;
        public ResponseStatus responseStatus = null;
        
        public Long getDb() { return db; }
        public AdminRedisResponse setDb(Long value) { this.db = value; return this; }
        public ArrayList<RedisSearchResult> getSearchResults() { return searchResults; }
        public AdminRedisResponse setSearchResults(ArrayList<RedisSearchResult> value) { this.searchResults = value; return this; }
        public HashMap<String,String> getInfo() { return info; }
        public AdminRedisResponse setInfo(HashMap<String,String> value) { this.info = value; return this; }
        public RedisEndpointInfo getEndpoint() { return endpoint; }
        public AdminRedisResponse setEndpoint(RedisEndpointInfo value) { this.endpoint = value; return this; }
        public RedisText getResult() { return result; }
        public AdminRedisResponse setResult(RedisText value) { this.result = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminRedisResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class AdminDatabaseResponse
    {
        public ArrayList<HashMap<String,Object>> results = new ArrayList<HashMap<String,Object>>();
        public Long total = null;
        public ArrayList<MetadataPropertyType> columns = null;
        public ResponseStatus responseStatus = null;
        
        public ArrayList<HashMap<String,Object>> getResults() { return results; }
        public AdminDatabaseResponse setResults(ArrayList<HashMap<String,Object>> value) { this.results = value; return this; }
        public Long getTotal() { return total; }
        public AdminDatabaseResponse setTotal(Long value) { this.total = value; return this; }
        public ArrayList<MetadataPropertyType> getColumns() { return columns; }
        public AdminDatabaseResponse setColumns(ArrayList<MetadataPropertyType> value) { this.columns = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminDatabaseResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class ViewCommandsResponse
    {
        public ArrayList<CommandSummary> commandTotals = new ArrayList<CommandSummary>();
        public ArrayList<CommandResult> latestCommands = new ArrayList<CommandResult>();
        public ArrayList<CommandResult> latestFailed = new ArrayList<CommandResult>();
        public ResponseStatus responseStatus = null;
        
        public ArrayList<CommandSummary> getCommandTotals() { return commandTotals; }
        public ViewCommandsResponse setCommandTotals(ArrayList<CommandSummary> value) { this.commandTotals = value; return this; }
        public ArrayList<CommandResult> getLatestCommands() { return latestCommands; }
        public ViewCommandsResponse setLatestCommands(ArrayList<CommandResult> value) { this.latestCommands = value; return this; }
        public ArrayList<CommandResult> getLatestFailed() { return latestFailed; }
        public ViewCommandsResponse setLatestFailed(ArrayList<CommandResult> value) { this.latestFailed = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public ViewCommandsResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class ExecuteCommandResponse
    {
        public CommandResult commandResult = null;
        public String result = null;
        public ResponseStatus responseStatus = null;
        
        public CommandResult getCommandResult() { return commandResult; }
        public ExecuteCommandResponse setCommandResult(CommandResult value) { this.commandResult = value; return this; }
        public String getResult() { return result; }
        public ExecuteCommandResponse setResult(String value) { this.result = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public ExecuteCommandResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class AdminApiKeysResponse
    {
        @DataMember(Order=1)
        public ArrayList<PartialApiKey> results = null;

        @DataMember(Order=2)
        public ResponseStatus responseStatus = null;
        
        public ArrayList<PartialApiKey> getResults() { return results; }
        public AdminApiKeysResponse setResults(ArrayList<PartialApiKey> value) { this.results = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminApiKeysResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class AdminApiKeyResponse
    {
        @DataMember(Order=1)
        public String result = null;

        @DataMember(Order=2)
        public ResponseStatus responseStatus = null;
        
        public String getResult() { return result; }
        public AdminApiKeyResponse setResult(String value) { this.result = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminApiKeyResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class EmptyResponse
    {
        @DataMember(Order=1)
        public ResponseStatus responseStatus = null;
        
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public EmptyResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class AdminJobDashboardResponse
    {
        public ArrayList<JobStatSummary> commands = new ArrayList<JobStatSummary>();
        public ArrayList<JobStatSummary> apis = new ArrayList<JobStatSummary>();
        public ArrayList<JobStatSummary> workers = new ArrayList<JobStatSummary>();
        public ArrayList<HourSummary> today = new ArrayList<HourSummary>();
        public ResponseStatus responseStatus = null;
        
        public ArrayList<JobStatSummary> getCommands() { return commands; }
        public AdminJobDashboardResponse setCommands(ArrayList<JobStatSummary> value) { this.commands = value; return this; }
        public ArrayList<JobStatSummary> getApis() { return apis; }
        public AdminJobDashboardResponse setApis(ArrayList<JobStatSummary> value) { this.apis = value; return this; }
        public ArrayList<JobStatSummary> getWorkers() { return workers; }
        public AdminJobDashboardResponse setWorkers(ArrayList<JobStatSummary> value) { this.workers = value; return this; }
        public ArrayList<HourSummary> getToday() { return today; }
        public AdminJobDashboardResponse setToday(ArrayList<HourSummary> value) { this.today = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminJobDashboardResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class AdminJobInfoResponse
    {
        public ArrayList<Date> monthDbs = new ArrayList<Date>();
        public HashMap<String,Integer> tableCounts = new HashMap<String,Integer>();
        public ArrayList<WorkerStats> workerStats = new ArrayList<WorkerStats>();
        public HashMap<String,Integer> queueCounts = new HashMap<String,Integer>();
        public HashMap<String,Integer> workerCounts = new HashMap<String,Integer>();
        public HashMap<BackgroundJobState,Integer> stateCounts = new HashMap<BackgroundJobState,Integer>();
        public ResponseStatus responseStatus = null;
        
        public ArrayList<Date> getMonthDbs() { return monthDbs; }
        public AdminJobInfoResponse setMonthDbs(ArrayList<Date> value) { this.monthDbs = value; return this; }
        public HashMap<String,Integer> getTableCounts() { return tableCounts; }
        public AdminJobInfoResponse setTableCounts(HashMap<String,Integer> value) { this.tableCounts = value; return this; }
        public ArrayList<WorkerStats> getWorkerStats() { return workerStats; }
        public AdminJobInfoResponse setWorkerStats(ArrayList<WorkerStats> value) { this.workerStats = value; return this; }
        public HashMap<String,Integer> getQueueCounts() { return queueCounts; }
        public AdminJobInfoResponse setQueueCounts(HashMap<String,Integer> value) { this.queueCounts = value; return this; }
        public HashMap<String,Integer> getWorkerCounts() { return workerCounts; }
        public AdminJobInfoResponse setWorkerCounts(HashMap<String,Integer> value) { this.workerCounts = value; return this; }
        public HashMap<BackgroundJobState,Integer> getStateCounts() { return stateCounts; }
        public AdminJobInfoResponse setStateCounts(HashMap<BackgroundJobState,Integer> value) { this.stateCounts = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminJobInfoResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class AdminGetJobResponse
    {
        public JobSummary result = null;
        public BackgroundJob queued = null;
        public CompletedJob completed = null;
        public FailedJob failed = null;
        public ResponseStatus responseStatus = null;
        
        public JobSummary getResult() { return result; }
        public AdminGetJobResponse setResult(JobSummary value) { this.result = value; return this; }
        public BackgroundJob getQueued() { return queued; }
        public AdminGetJobResponse setQueued(BackgroundJob value) { this.queued = value; return this; }
        public CompletedJob getCompleted() { return completed; }
        public AdminGetJobResponse setCompleted(CompletedJob value) { this.completed = value; return this; }
        public FailedJob getFailed() { return failed; }
        public AdminGetJobResponse setFailed(FailedJob value) { this.failed = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminGetJobResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class AdminGetJobProgressResponse
    {
        public BackgroundJobState state = null;
        public Double progress = null;
        public String status = null;
        public String logs = null;
        public Integer durationMs = null;
        public ResponseStatus error = null;
        public ResponseStatus responseStatus = null;
        
        public BackgroundJobState getState() { return state; }
        public AdminGetJobProgressResponse setState(BackgroundJobState value) { this.state = value; return this; }
        public Double getProgress() { return progress; }
        public AdminGetJobProgressResponse setProgress(Double value) { this.progress = value; return this; }
        public String getStatus() { return status; }
        public AdminGetJobProgressResponse setStatus(String value) { this.status = value; return this; }
        public String getLogs() { return logs; }
        public AdminGetJobProgressResponse setLogs(String value) { this.logs = value; return this; }
        public Integer getDurationMs() { return durationMs; }
        public AdminGetJobProgressResponse setDurationMs(Integer value) { this.durationMs = value; return this; }
        public ResponseStatus getError() { return error; }
        public AdminGetJobProgressResponse setError(ResponseStatus value) { this.error = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminGetJobProgressResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class AdminRequeueFailedJobsJobsResponse
    {
        public ArrayList<Long> results = new ArrayList<Long>();
        public HashMap<Long,String> errors = new HashMap<Long,String>();
        public ResponseStatus responseStatus = null;
        
        public ArrayList<Long> getResults() { return results; }
        public AdminRequeueFailedJobsJobsResponse setResults(ArrayList<Long> value) { this.results = value; return this; }
        public HashMap<Long,String> getErrors() { return errors; }
        public AdminRequeueFailedJobsJobsResponse setErrors(HashMap<Long,String> value) { this.errors = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminRequeueFailedJobsJobsResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    public static class AdminCancelJobsResponse
    {
        public ArrayList<Long> results = new ArrayList<Long>();
        public HashMap<Long,String> errors = new HashMap<Long,String>();
        public ResponseStatus responseStatus = null;
        
        public ArrayList<Long> getResults() { return results; }
        public AdminCancelJobsResponse setResults(ArrayList<Long> value) { this.results = value; return this; }
        public HashMap<Long,String> getErrors() { return errors; }
        public AdminCancelJobsResponse setErrors(HashMap<Long,String> value) { this.errors = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AdminCancelJobsResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class RequestLogsResponse
    {
        @DataMember(Order=1)
        public ArrayList<RequestLogEntry> results = null;

        @DataMember(Order=2)
        public HashMap<String,String> usage = null;

        @DataMember(Order=3)
        public Integer total = null;

        @DataMember(Order=4)
        public ResponseStatus responseStatus = null;
        
        public ArrayList<RequestLogEntry> getResults() { return results; }
        public RequestLogsResponse setResults(ArrayList<RequestLogEntry> value) { this.results = value; return this; }
        public HashMap<String,String> getUsage() { return usage; }
        public RequestLogsResponse setUsage(HashMap<String,String> value) { this.usage = value; return this; }
        public Integer getTotal() { return total; }
        public RequestLogsResponse setTotal(Integer value) { this.total = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public RequestLogsResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class GetAnalyticsInfoResponse
    {
        @DataMember(Order=1)
        public ArrayList<String> months = null;

        @DataMember(Order=2)
        public AnalyticsLogInfo result = null;

        @DataMember(Order=3)
        public ResponseStatus responseStatus = null;
        
        public ArrayList<String> getMonths() { return months; }
        public GetAnalyticsInfoResponse setMonths(ArrayList<String> value) { this.months = value; return this; }
        public AnalyticsLogInfo getResult() { return result; }
        public GetAnalyticsInfoResponse setResult(AnalyticsLogInfo value) { this.result = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public GetAnalyticsInfoResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class GetAnalyticsReportsResponse
    {
        @DataMember(Order=1)
        public AnalyticsReports result = null;

        @DataMember(Order=2)
        public ResponseStatus responseStatus = null;
        
        public AnalyticsReports getResult() { return result; }
        public GetAnalyticsReportsResponse setResult(AnalyticsReports value) { this.result = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public GetAnalyticsReportsResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class GetValidationRulesResponse
    {
        @DataMember(Order=1)
        public ArrayList<ValidationRule> results = null;

        @DataMember(Order=2)
        public ResponseStatus responseStatus = null;
        
        public ArrayList<ValidationRule> getResults() { return results; }
        public GetValidationRulesResponse setResults(ArrayList<ValidationRule> value) { this.results = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public GetValidationRulesResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class Property
    {
        @DataMember(Order=1)
        public String name = null;

        @DataMember(Order=2)
        public String value = null;
        
        public String getName() { return name; }
        public Property setName(String value) { this.name = value; return this; }
        public String getValue() { return value; }
        public Property setValue(String value) { this.value = value; return this; }
    }

    @DataContract
    public static class AdminUserBase
    {
        @DataMember(Order=1)
        public String userName = null;

        @DataMember(Order=2)
        public String firstName = null;

        @DataMember(Order=3)
        public String lastName = null;

        @DataMember(Order=4)
        public String displayName = null;

        @DataMember(Order=5)
        public String email = null;

        @DataMember(Order=6)
        public String password = null;

        @DataMember(Order=7)
        public String profileUrl = null;

        @DataMember(Order=8)
        public String phoneNumber = null;

        @DataMember(Order=9)
        public HashMap<String,String> userAuthProperties = null;

        @DataMember(Order=10)
        public HashMap<String,String> meta = null;
        
        public String getUserName() { return userName; }
        public AdminUserBase setUserName(String value) { this.userName = value; return this; }
        public String getFirstName() { return firstName; }
        public AdminUserBase setFirstName(String value) { this.firstName = value; return this; }
        public String getLastName() { return lastName; }
        public AdminUserBase setLastName(String value) { this.lastName = value; return this; }
        public String getDisplayName() { return displayName; }
        public AdminUserBase setDisplayName(String value) { this.displayName = value; return this; }
        public String getEmail() { return email; }
        public AdminUserBase setEmail(String value) { this.email = value; return this; }
        public String getPassword() { return password; }
        public AdminUserBase setPassword(String value) { this.password = value; return this; }
        public String getProfileUrl() { return profileUrl; }
        public AdminUserBase setProfileUrl(String value) { this.profileUrl = value; return this; }
        public String getPhoneNumber() { return phoneNumber; }
        public AdminUserBase setPhoneNumber(String value) { this.phoneNumber = value; return this; }
        public HashMap<String,String> getUserAuthProperties() { return userAuthProperties; }
        public AdminUserBase setUserAuthProperties(HashMap<String,String> value) { this.userAuthProperties = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AdminUserBase setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class QueryDb<T> extends QueryBase
    {
        
    }

    public static class RequestLog
    {
        public Long id = null;
        public String traceId = null;
        public String operationName = null;
        public Date dateTime = null;
        public Integer statusCode = null;
        public String statusDescription = null;
        public String httpMethod = null;
        public String absoluteUri = null;
        public String pathInfo = null;
        public String request = null;
        @StringLength(MaximumLength=2147483647)
        public String requestBody = null;

        public String userAuthId = null;
        public String sessionId = null;
        public String ipAddress = null;
        public String forwardedFor = null;
        public String referer = null;
        public HashMap<String,String> headers = new HashMap<String,String>();
        public HashMap<String,String> formData = null;
        public HashMap<String,String> items = new HashMap<String,String>();
        public HashMap<String,String> responseHeaders = null;
        public String response = null;
        public String responseBody = null;
        public String sessionBody = null;
        public ResponseStatus error = null;
        public String exceptionSource = null;
        public String exceptionDataBody = null;
        public TimeSpan requestDuration = null;
        public HashMap<String,String> meta = null;
        
        public Long getId() { return id; }
        public RequestLog setId(Long value) { this.id = value; return this; }
        public String getTraceId() { return traceId; }
        public RequestLog setTraceId(String value) { this.traceId = value; return this; }
        public String getOperationName() { return operationName; }
        public RequestLog setOperationName(String value) { this.operationName = value; return this; }
        public Date getDateTime() { return dateTime; }
        public RequestLog setDateTime(Date value) { this.dateTime = value; return this; }
        public Integer getStatusCode() { return statusCode; }
        public RequestLog setStatusCode(Integer value) { this.statusCode = value; return this; }
        public String getStatusDescription() { return statusDescription; }
        public RequestLog setStatusDescription(String value) { this.statusDescription = value; return this; }
        public String getHttpMethod() { return httpMethod; }
        public RequestLog setHttpMethod(String value) { this.httpMethod = value; return this; }
        public String getAbsoluteUri() { return absoluteUri; }
        public RequestLog setAbsoluteUri(String value) { this.absoluteUri = value; return this; }
        public String getPathInfo() { return pathInfo; }
        public RequestLog setPathInfo(String value) { this.pathInfo = value; return this; }
        public String getRequest() { return request; }
        public RequestLog setRequest(String value) { this.request = value; return this; }
        public String getRequestBody() { return requestBody; }
        public RequestLog setRequestBody(String value) { this.requestBody = value; return this; }
        public String getUserAuthId() { return userAuthId; }
        public RequestLog setUserAuthId(String value) { this.userAuthId = value; return this; }
        public String getSessionId() { return sessionId; }
        public RequestLog setSessionId(String value) { this.sessionId = value; return this; }
        public String getIpAddress() { return ipAddress; }
        public RequestLog setIpAddress(String value) { this.ipAddress = value; return this; }
        public String getForwardedFor() { return forwardedFor; }
        public RequestLog setForwardedFor(String value) { this.forwardedFor = value; return this; }
        public String getReferer() { return referer; }
        public RequestLog setReferer(String value) { this.referer = value; return this; }
        public HashMap<String,String> getHeaders() { return headers; }
        public RequestLog setHeaders(HashMap<String,String> value) { this.headers = value; return this; }
        public HashMap<String,String> getFormData() { return formData; }
        public RequestLog setFormData(HashMap<String,String> value) { this.formData = value; return this; }
        public HashMap<String,String> getItems() { return items; }
        public RequestLog setItems(HashMap<String,String> value) { this.items = value; return this; }
        public HashMap<String,String> getResponseHeaders() { return responseHeaders; }
        public RequestLog setResponseHeaders(HashMap<String,String> value) { this.responseHeaders = value; return this; }
        public String getResponse() { return response; }
        public RequestLog setResponse(String value) { this.response = value; return this; }
        public String getResponseBody() { return responseBody; }
        public RequestLog setResponseBody(String value) { this.responseBody = value; return this; }
        public String getSessionBody() { return sessionBody; }
        public RequestLog setSessionBody(String value) { this.sessionBody = value; return this; }
        public ResponseStatus getError() { return error; }
        public RequestLog setError(ResponseStatus value) { this.error = value; return this; }
        public String getExceptionSource() { return exceptionSource; }
        public RequestLog setExceptionSource(String value) { this.exceptionSource = value; return this; }
        public String getExceptionDataBody() { return exceptionDataBody; }
        public RequestLog setExceptionDataBody(String value) { this.exceptionDataBody = value; return this; }
        public TimeSpan getRequestDuration() { return requestDuration; }
        public RequestLog setRequestDuration(TimeSpan value) { this.requestDuration = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public RequestLog setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class RedisEndpointInfo
    {
        public String host = null;
        public Integer port = null;
        public Boolean ssl = null;
        public Long db = null;
        public String username = null;
        public String password = null;
        
        public String getHost() { return host; }
        public RedisEndpointInfo setHost(String value) { this.host = value; return this; }
        public Integer getPort() { return port; }
        public RedisEndpointInfo setPort(Integer value) { this.port = value; return this; }
        public Boolean isSsl() { return ssl; }
        public RedisEndpointInfo setSsl(Boolean value) { this.ssl = value; return this; }
        public Long getDb() { return db; }
        public RedisEndpointInfo setDb(Long value) { this.db = value; return this; }
        public String getUsername() { return username; }
        public RedisEndpointInfo setUsername(String value) { this.username = value; return this; }
        public String getPassword() { return password; }
        public RedisEndpointInfo setPassword(String value) { this.password = value; return this; }
    }

    public static class BackgroundJob extends BackgroundJobBase
    {
        public Long id = null;
        
        public Long getId() { return id; }
        public BackgroundJob setId(Long value) { this.id = value; return this; }
    }

    public static class JobSummary
    {
        public Long id = null;
        public Long parentId = null;
        public String refId = null;
        public String worker = null;
        public String tag = null;
        public String batchId = null;
        public Date createdDate = null;
        public String createdBy = null;
        public String requestType = null;
        public String command = null;
        public String request = null;
        public String response = null;
        public String userId = null;
        public String callback = null;
        public Date startedDate = null;
        public Date completedDate = null;
        public BackgroundJobState state = null;
        public Integer durationMs = null;
        public Integer attempts = null;
        public String errorCode = null;
        public String errorMessage = null;
        
        public Long getId() { return id; }
        public JobSummary setId(Long value) { this.id = value; return this; }
        public Long getParentId() { return parentId; }
        public JobSummary setParentId(Long value) { this.parentId = value; return this; }
        public String getRefId() { return refId; }
        public JobSummary setRefId(String value) { this.refId = value; return this; }
        public String getWorker() { return worker; }
        public JobSummary setWorker(String value) { this.worker = value; return this; }
        public String getTag() { return tag; }
        public JobSummary setTag(String value) { this.tag = value; return this; }
        public String getBatchId() { return batchId; }
        public JobSummary setBatchId(String value) { this.batchId = value; return this; }
        public Date getCreatedDate() { return createdDate; }
        public JobSummary setCreatedDate(Date value) { this.createdDate = value; return this; }
        public String getCreatedBy() { return createdBy; }
        public JobSummary setCreatedBy(String value) { this.createdBy = value; return this; }
        public String getRequestType() { return requestType; }
        public JobSummary setRequestType(String value) { this.requestType = value; return this; }
        public String getCommand() { return command; }
        public JobSummary setCommand(String value) { this.command = value; return this; }
        public String getRequest() { return request; }
        public JobSummary setRequest(String value) { this.request = value; return this; }
        public String getResponse() { return response; }
        public JobSummary setResponse(String value) { this.response = value; return this; }
        public String getUserId() { return userId; }
        public JobSummary setUserId(String value) { this.userId = value; return this; }
        public String getCallback() { return callback; }
        public JobSummary setCallback(String value) { this.callback = value; return this; }
        public Date getStartedDate() { return startedDate; }
        public JobSummary setStartedDate(Date value) { this.startedDate = value; return this; }
        public Date getCompletedDate() { return completedDate; }
        public JobSummary setCompletedDate(Date value) { this.completedDate = value; return this; }
        public BackgroundJobState getState() { return state; }
        public JobSummary setState(BackgroundJobState value) { this.state = value; return this; }
        public Integer getDurationMs() { return durationMs; }
        public JobSummary setDurationMs(Integer value) { this.durationMs = value; return this; }
        public Integer getAttempts() { return attempts; }
        public JobSummary setAttempts(Integer value) { this.attempts = value; return this; }
        public String getErrorCode() { return errorCode; }
        public JobSummary setErrorCode(String value) { this.errorCode = value; return this; }
        public String getErrorMessage() { return errorMessage; }
        public JobSummary setErrorMessage(String value) { this.errorMessage = value; return this; }
    }

    public static class ScheduledTask
    {
        public Long id = null;
        public String name = null;
        public TimeSpan interval = null;
        public String cronExpression = null;
        public String requestType = null;
        public String command = null;
        public String request = null;
        public String requestBody = null;
        public BackgroundJobOptions options = null;
        public Date lastRun = null;
        public Long lastJobId = null;
        
        public Long getId() { return id; }
        public ScheduledTask setId(Long value) { this.id = value; return this; }
        public String getName() { return name; }
        public ScheduledTask setName(String value) { this.name = value; return this; }
        public TimeSpan getInterval() { return interval; }
        public ScheduledTask setInterval(TimeSpan value) { this.interval = value; return this; }
        public String getCronExpression() { return cronExpression; }
        public ScheduledTask setCronExpression(String value) { this.cronExpression = value; return this; }
        public String getRequestType() { return requestType; }
        public ScheduledTask setRequestType(String value) { this.requestType = value; return this; }
        public String getCommand() { return command; }
        public ScheduledTask setCommand(String value) { this.command = value; return this; }
        public String getRequest() { return request; }
        public ScheduledTask setRequest(String value) { this.request = value; return this; }
        public String getRequestBody() { return requestBody; }
        public ScheduledTask setRequestBody(String value) { this.requestBody = value; return this; }
        public BackgroundJobOptions getOptions() { return options; }
        public ScheduledTask setOptions(BackgroundJobOptions value) { this.options = value; return this; }
        public Date getLastRun() { return lastRun; }
        public ScheduledTask setLastRun(Date value) { this.lastRun = value; return this; }
        public Long getLastJobId() { return lastJobId; }
        public ScheduledTask setLastJobId(Long value) { this.lastJobId = value; return this; }
    }

    public static class CompletedJob extends BackgroundJobBase
    {
        
    }

    public static class FailedJob extends BackgroundJobBase
    {
        
    }

    public static enum BackgroundJobState
    {
        Queued,
        Started,
        Executed,
        Completed,
        Failed,
        Cancelled;
    }

    public static class ValidationRule extends ValidateRule
    {
        public Integer id = null;
        @Required()
        public String type = null;

        public String field = null;
        public String createdBy = null;
        public Date createdDate = null;
        public String modifiedBy = null;
        public Date modifiedDate = null;
        public String suspendedBy = null;
        public Date suspendedDate = null;
        public String notes = null;
        
        public Integer getId() { return id; }
        public ValidationRule setId(Integer value) { this.id = value; return this; }
        public String getType() { return type; }
        public ValidationRule setType(String value) { this.type = value; return this; }
        public String getField() { return field; }
        public ValidationRule setField(String value) { this.field = value; return this; }
        public String getCreatedBy() { return createdBy; }
        public ValidationRule setCreatedBy(String value) { this.createdBy = value; return this; }
        public Date getCreatedDate() { return createdDate; }
        public ValidationRule setCreatedDate(Date value) { this.createdDate = value; return this; }
        public String getModifiedBy() { return modifiedBy; }
        public ValidationRule setModifiedBy(String value) { this.modifiedBy = value; return this; }
        public Date getModifiedDate() { return modifiedDate; }
        public ValidationRule setModifiedDate(Date value) { this.modifiedDate = value; return this; }
        public String getSuspendedBy() { return suspendedBy; }
        public ValidationRule setSuspendedBy(String value) { this.suspendedBy = value; return this; }
        public Date getSuspendedDate() { return suspendedDate; }
        public ValidationRule setSuspendedDate(Date value) { this.suspendedDate = value; return this; }
        public String getNotes() { return notes; }
        public ValidationRule setNotes(String value) { this.notes = value; return this; }
    }

    public static class AppInfo
    {
        public String baseUrl = null;
        public String serviceStackVersion = null;
        public String serviceName = null;
        public String apiVersion = null;
        public String serviceDescription = null;
        public String serviceIconUrl = null;
        public String brandUrl = null;
        public String brandImageUrl = null;
        public String textColor = null;
        public String linkColor = null;
        public String backgroundColor = null;
        public String backgroundImageUrl = null;
        public String iconUrl = null;
        public String jsTextCase = null;
        public String useSystemJson = null;
        public ArrayList<String> endpointRouting = null;
        public HashMap<String,String> meta = null;
        
        public String getBaseUrl() { return baseUrl; }
        public AppInfo setBaseUrl(String value) { this.baseUrl = value; return this; }
        public String getServiceStackVersion() { return serviceStackVersion; }
        public AppInfo setServiceStackVersion(String value) { this.serviceStackVersion = value; return this; }
        public String getServiceName() { return serviceName; }
        public AppInfo setServiceName(String value) { this.serviceName = value; return this; }
        public String getApiVersion() { return apiVersion; }
        public AppInfo setApiVersion(String value) { this.apiVersion = value; return this; }
        public String getServiceDescription() { return serviceDescription; }
        public AppInfo setServiceDescription(String value) { this.serviceDescription = value; return this; }
        public String getServiceIconUrl() { return serviceIconUrl; }
        public AppInfo setServiceIconUrl(String value) { this.serviceIconUrl = value; return this; }
        public String getBrandUrl() { return brandUrl; }
        public AppInfo setBrandUrl(String value) { this.brandUrl = value; return this; }
        public String getBrandImageUrl() { return brandImageUrl; }
        public AppInfo setBrandImageUrl(String value) { this.brandImageUrl = value; return this; }
        public String getTextColor() { return textColor; }
        public AppInfo setTextColor(String value) { this.textColor = value; return this; }
        public String getLinkColor() { return linkColor; }
        public AppInfo setLinkColor(String value) { this.linkColor = value; return this; }
        public String getBackgroundColor() { return backgroundColor; }
        public AppInfo setBackgroundColor(String value) { this.backgroundColor = value; return this; }
        public String getBackgroundImageUrl() { return backgroundImageUrl; }
        public AppInfo setBackgroundImageUrl(String value) { this.backgroundImageUrl = value; return this; }
        public String getIconUrl() { return iconUrl; }
        public AppInfo setIconUrl(String value) { this.iconUrl = value; return this; }
        public String getJsTextCase() { return jsTextCase; }
        public AppInfo setJsTextCase(String value) { this.jsTextCase = value; return this; }
        public String getUseSystemJson() { return useSystemJson; }
        public AppInfo setUseSystemJson(String value) { this.useSystemJson = value; return this; }
        public ArrayList<String> getEndpointRouting() { return endpointRouting; }
        public AppInfo setEndpointRouting(ArrayList<String> value) { this.endpointRouting = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AppInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class UiInfo
    {
        public ImageInfo brandIcon = null;
        public ArrayList<String> hideTags = null;
        public ArrayList<String> modules = null;
        public ArrayList<String> alwaysHideTags = null;
        public ArrayList<LinkInfo> adminLinks = null;
        public ThemeInfo theme = null;
        public LocodeUi locode = null;
        public ExplorerUi explorer = null;
        public AdminUi admin = null;
        public ApiFormat defaultFormats = null;
        public HashMap<String,String> meta = null;
        
        public ImageInfo getBrandIcon() { return brandIcon; }
        public UiInfo setBrandIcon(ImageInfo value) { this.brandIcon = value; return this; }
        public ArrayList<String> getHideTags() { return hideTags; }
        public UiInfo setHideTags(ArrayList<String> value) { this.hideTags = value; return this; }
        public ArrayList<String> getModules() { return modules; }
        public UiInfo setModules(ArrayList<String> value) { this.modules = value; return this; }
        public ArrayList<String> getAlwaysHideTags() { return alwaysHideTags; }
        public UiInfo setAlwaysHideTags(ArrayList<String> value) { this.alwaysHideTags = value; return this; }
        public ArrayList<LinkInfo> getAdminLinks() { return adminLinks; }
        public UiInfo setAdminLinks(ArrayList<LinkInfo> value) { this.adminLinks = value; return this; }
        public ThemeInfo getTheme() { return theme; }
        public UiInfo setTheme(ThemeInfo value) { this.theme = value; return this; }
        public LocodeUi getLocode() { return locode; }
        public UiInfo setLocode(LocodeUi value) { this.locode = value; return this; }
        public ExplorerUi getExplorer() { return explorer; }
        public UiInfo setExplorer(ExplorerUi value) { this.explorer = value; return this; }
        public AdminUi getAdmin() { return admin; }
        public UiInfo setAdmin(AdminUi value) { this.admin = value; return this; }
        public ApiFormat getDefaultFormats() { return defaultFormats; }
        public UiInfo setDefaultFormats(ApiFormat value) { this.defaultFormats = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public UiInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class ConfigInfo
    {
        public Boolean debugMode = null;
        public HashMap<String,String> meta = null;
        
        public Boolean isDebugMode() { return debugMode; }
        public ConfigInfo setDebugMode(Boolean value) { this.debugMode = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public ConfigInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class PluginInfo
    {
        public ArrayList<String> loaded = null;
        public AuthInfo auth = null;
        public ApiKeyInfo apiKey = null;
        public CommandsInfo commands = null;
        public AutoQueryInfo autoQuery = null;
        public ValidationInfo validation = null;
        public SharpPagesInfo sharpPages = null;
        public RequestLogsInfo requestLogs = null;
        public ProfilingInfo profiling = null;
        public FilesUploadInfo filesUpload = null;
        public AdminUsersInfo adminUsers = null;
        public AdminIdentityUsersInfo adminIdentityUsers = null;
        public AdminRedisInfo adminRedis = null;
        public AdminDatabaseInfo adminDatabase = null;
        public HashMap<String,String> meta = null;
        
        public ArrayList<String> getLoaded() { return loaded; }
        public PluginInfo setLoaded(ArrayList<String> value) { this.loaded = value; return this; }
        public AuthInfo getAuth() { return auth; }
        public PluginInfo setAuth(AuthInfo value) { this.auth = value; return this; }
        public ApiKeyInfo getApiKey() { return apiKey; }
        public PluginInfo setApiKey(ApiKeyInfo value) { this.apiKey = value; return this; }
        public CommandsInfo getCommands() { return commands; }
        public PluginInfo setCommands(CommandsInfo value) { this.commands = value; return this; }
        public AutoQueryInfo getAutoQuery() { return autoQuery; }
        public PluginInfo setAutoQuery(AutoQueryInfo value) { this.autoQuery = value; return this; }
        public ValidationInfo getValidation() { return validation; }
        public PluginInfo setValidation(ValidationInfo value) { this.validation = value; return this; }
        public SharpPagesInfo getSharpPages() { return sharpPages; }
        public PluginInfo setSharpPages(SharpPagesInfo value) { this.sharpPages = value; return this; }
        public RequestLogsInfo getRequestLogs() { return requestLogs; }
        public PluginInfo setRequestLogs(RequestLogsInfo value) { this.requestLogs = value; return this; }
        public ProfilingInfo getProfiling() { return profiling; }
        public PluginInfo setProfiling(ProfilingInfo value) { this.profiling = value; return this; }
        public FilesUploadInfo getFilesUpload() { return filesUpload; }
        public PluginInfo setFilesUpload(FilesUploadInfo value) { this.filesUpload = value; return this; }
        public AdminUsersInfo getAdminUsers() { return adminUsers; }
        public PluginInfo setAdminUsers(AdminUsersInfo value) { this.adminUsers = value; return this; }
        public AdminIdentityUsersInfo getAdminIdentityUsers() { return adminIdentityUsers; }
        public PluginInfo setAdminIdentityUsers(AdminIdentityUsersInfo value) { this.adminIdentityUsers = value; return this; }
        public AdminRedisInfo getAdminRedis() { return adminRedis; }
        public PluginInfo setAdminRedis(AdminRedisInfo value) { this.adminRedis = value; return this; }
        public AdminDatabaseInfo getAdminDatabase() { return adminDatabase; }
        public PluginInfo setAdminDatabase(AdminDatabaseInfo value) { this.adminDatabase = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public PluginInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class CustomPluginInfo
    {
        public String accessRole = null;
        public HashMap<String,ArrayList<String>> serviceRoutes = null;
        public ArrayList<String> enabled = null;
        public HashMap<String,String> meta = null;
        
        public String getAccessRole() { return accessRole; }
        public CustomPluginInfo setAccessRole(String value) { this.accessRole = value; return this; }
        public HashMap<String,ArrayList<String>> getServiceRoutes() { return serviceRoutes; }
        public CustomPluginInfo setServiceRoutes(HashMap<String,ArrayList<String>> value) { this.serviceRoutes = value; return this; }
        public ArrayList<String> getEnabled() { return enabled; }
        public CustomPluginInfo setEnabled(ArrayList<String> value) { this.enabled = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public CustomPluginInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class MetadataTypes
    {
        public MetadataTypesConfig config = null;
        public ArrayList<String> namespaces = null;
        public ArrayList<MetadataType> types = null;
        public ArrayList<MetadataOperationType> operations = null;
        
        public MetadataTypesConfig getConfig() { return config; }
        public MetadataTypes setConfig(MetadataTypesConfig value) { this.config = value; return this; }
        public ArrayList<String> getNamespaces() { return namespaces; }
        public MetadataTypes setNamespaces(ArrayList<String> value) { this.namespaces = value; return this; }
        public ArrayList<MetadataType> getTypes() { return types; }
        public MetadataTypes setTypes(ArrayList<MetadataType> value) { this.types = value; return this; }
        public ArrayList<MetadataOperationType> getOperations() { return operations; }
        public MetadataTypes setOperations(ArrayList<MetadataOperationType> value) { this.operations = value; return this; }
    }

    @DataContract
    public static class AdminRole
    {
        
    }

    public static class ServerStats
    {
        public HashMap<String,Long> redis = null;
        public HashMap<String,String> serverEvents = null;
        public String mqDescription = null;
        public HashMap<String,Long> mqWorkers = null;
        
        public HashMap<String,Long> getRedis() { return redis; }
        public ServerStats setRedis(HashMap<String,Long> value) { this.redis = value; return this; }
        public HashMap<String,String> getServerEvents() { return serverEvents; }
        public ServerStats setServerEvents(HashMap<String,String> value) { this.serverEvents = value; return this; }
        public String getMqDescription() { return mqDescription; }
        public ServerStats setMqDescription(String value) { this.mqDescription = value; return this; }
        public HashMap<String,Long> getMqWorkers() { return mqWorkers; }
        public ServerStats setMqWorkers(HashMap<String,Long> value) { this.mqWorkers = value; return this; }
    }

    public static class DiagnosticEntry
    {
        public Long id = null;
        public String traceId = null;
        public String source = null;
        public String eventType = null;
        public String message = null;
        public String operation = null;
        public Integer threadId = null;
        public ResponseStatus error = null;
        public String commandType = null;
        public String command = null;
        public String userAuthId = null;
        public String sessionId = null;
        public String arg = null;
        public ArrayList<String> args = null;
        public ArrayList<Long> argLengths = null;
        public HashMap<String,Object> namedArgs = null;
        public TimeSpan duration = null;
        public Long timestamp = null;
        public Date date = null;
        public String tag = null;
        public String stackTrace = null;
        public HashMap<String,String> meta = new HashMap<String,String>();
        
        public Long getId() { return id; }
        public DiagnosticEntry setId(Long value) { this.id = value; return this; }
        public String getTraceId() { return traceId; }
        public DiagnosticEntry setTraceId(String value) { this.traceId = value; return this; }
        public String getSource() { return source; }
        public DiagnosticEntry setSource(String value) { this.source = value; return this; }
        public String getEventType() { return eventType; }
        public DiagnosticEntry setEventType(String value) { this.eventType = value; return this; }
        public String getMessage() { return message; }
        public DiagnosticEntry setMessage(String value) { this.message = value; return this; }
        public String getOperation() { return operation; }
        public DiagnosticEntry setOperation(String value) { this.operation = value; return this; }
        public Integer getThreadId() { return threadId; }
        public DiagnosticEntry setThreadId(Integer value) { this.threadId = value; return this; }
        public ResponseStatus getError() { return error; }
        public DiagnosticEntry setError(ResponseStatus value) { this.error = value; return this; }
        public String getCommandType() { return commandType; }
        public DiagnosticEntry setCommandType(String value) { this.commandType = value; return this; }
        public String getCommand() { return command; }
        public DiagnosticEntry setCommand(String value) { this.command = value; return this; }
        public String getUserAuthId() { return userAuthId; }
        public DiagnosticEntry setUserAuthId(String value) { this.userAuthId = value; return this; }
        public String getSessionId() { return sessionId; }
        public DiagnosticEntry setSessionId(String value) { this.sessionId = value; return this; }
        public String getArg() { return arg; }
        public DiagnosticEntry setArg(String value) { this.arg = value; return this; }
        public ArrayList<String> getArgs() { return args; }
        public DiagnosticEntry setArgs(ArrayList<String> value) { this.args = value; return this; }
        public ArrayList<Long> getArgLengths() { return argLengths; }
        public DiagnosticEntry setArgLengths(ArrayList<Long> value) { this.argLengths = value; return this; }
        public HashMap<String,Object> getNamedArgs() { return namedArgs; }
        public DiagnosticEntry setNamedArgs(HashMap<String,Object> value) { this.namedArgs = value; return this; }
        public TimeSpan getDuration() { return duration; }
        public DiagnosticEntry setDuration(TimeSpan value) { this.duration = value; return this; }
        public Long getTimestamp() { return timestamp; }
        public DiagnosticEntry setTimestamp(Long value) { this.timestamp = value; return this; }
        public Date getDate() { return date; }
        public DiagnosticEntry setDate(Date value) { this.date = value; return this; }
        public String getTag() { return tag; }
        public DiagnosticEntry setTag(String value) { this.tag = value; return this; }
        public String getStackTrace() { return stackTrace; }
        public DiagnosticEntry setStackTrace(String value) { this.stackTrace = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public DiagnosticEntry setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class RedisSearchResult
    {
        public String id = null;
        public String type = null;
        public Long ttl = null;
        public Long size = null;
        
        public String getId() { return id; }
        public RedisSearchResult setId(String value) { this.id = value; return this; }
        public String getType() { return type; }
        public RedisSearchResult setType(String value) { this.type = value; return this; }
        public Long getTtl() { return ttl; }
        public RedisSearchResult setTtl(Long value) { this.ttl = value; return this; }
        public Long getSize() { return size; }
        public RedisSearchResult setSize(Long value) { this.size = value; return this; }
    }

    public static class RedisText
    {
        public String text = null;
        public ArrayList<RedisText> children = null;
        
        public String getText() { return text; }
        public RedisText setText(String value) { this.text = value; return this; }
        public ArrayList<RedisText> getChildren() { return children; }
        public RedisText setChildren(ArrayList<RedisText> value) { this.children = value; return this; }
    }

    public static class MetadataPropertyType
    {
        public String name = null;
        public String type = null;
        public String namespace = null;
        public Boolean isValueType = null;
        public Boolean isEnum = null;
        public Boolean isPrimaryKey = null;
        public ArrayList<String> genericArgs = null;
        public String value = null;
        public String description = null;
        public MetadataDataMember dataMember = null;
        public Boolean readOnly = null;
        public String paramType = null;
        public String displayType = null;
        public Boolean isRequired = null;
        public ArrayList<String> allowableValues = null;
        public Integer allowableMin = null;
        public Integer allowableMax = null;
        public ArrayList<MetadataAttribute> attributes = null;
        public String uploadTo = null;
        public InputInfo input = null;
        public FormatInfo format = null;
        public RefInfo ref = null;
        
        public String getName() { return name; }
        public MetadataPropertyType setName(String value) { this.name = value; return this; }
        public String getType() { return type; }
        public MetadataPropertyType setType(String value) { this.type = value; return this; }
        public String getNamespace() { return namespace; }
        public MetadataPropertyType setNamespace(String value) { this.namespace = value; return this; }
        public Boolean getIsValueType() { return isValueType; }
        public MetadataPropertyType setIsValueType(Boolean value) { this.isValueType = value; return this; }
        public Boolean getIsEnum() { return isEnum; }
        public MetadataPropertyType setIsEnum(Boolean value) { this.isEnum = value; return this; }
        public Boolean getIsPrimaryKey() { return isPrimaryKey; }
        public MetadataPropertyType setIsPrimaryKey(Boolean value) { this.isPrimaryKey = value; return this; }
        public ArrayList<String> getGenericArgs() { return genericArgs; }
        public MetadataPropertyType setGenericArgs(ArrayList<String> value) { this.genericArgs = value; return this; }
        public String getValue() { return value; }
        public MetadataPropertyType setValue(String value) { this.value = value; return this; }
        public String getDescription() { return description; }
        public MetadataPropertyType setDescription(String value) { this.description = value; return this; }
        public MetadataDataMember getDataMember() { return dataMember; }
        public MetadataPropertyType setDataMember(MetadataDataMember value) { this.dataMember = value; return this; }
        public Boolean isReadOnly() { return readOnly; }
        public MetadataPropertyType setReadOnly(Boolean value) { this.readOnly = value; return this; }
        public String getParamType() { return paramType; }
        public MetadataPropertyType setParamType(String value) { this.paramType = value; return this; }
        public String getDisplayType() { return displayType; }
        public MetadataPropertyType setDisplayType(String value) { this.displayType = value; return this; }
        public Boolean getIsRequired() { return isRequired; }
        public MetadataPropertyType setIsRequired(Boolean value) { this.isRequired = value; return this; }
        public ArrayList<String> getAllowableValues() { return allowableValues; }
        public MetadataPropertyType setAllowableValues(ArrayList<String> value) { this.allowableValues = value; return this; }
        public Integer getAllowableMin() { return allowableMin; }
        public MetadataPropertyType setAllowableMin(Integer value) { this.allowableMin = value; return this; }
        public Integer getAllowableMax() { return allowableMax; }
        public MetadataPropertyType setAllowableMax(Integer value) { this.allowableMax = value; return this; }
        public ArrayList<MetadataAttribute> getAttributes() { return attributes; }
        public MetadataPropertyType setAttributes(ArrayList<MetadataAttribute> value) { this.attributes = value; return this; }
        public String getUploadTo() { return uploadTo; }
        public MetadataPropertyType setUploadTo(String value) { this.uploadTo = value; return this; }
        public InputInfo getInput() { return input; }
        public MetadataPropertyType setInput(InputInfo value) { this.input = value; return this; }
        public FormatInfo getFormat() { return format; }
        public MetadataPropertyType setFormat(FormatInfo value) { this.format = value; return this; }
        public RefInfo getRef() { return ref; }
        public MetadataPropertyType setRef(RefInfo value) { this.ref = value; return this; }
    }

    public static class CommandSummary
    {
        public String type = null;
        public String name = null;
        public Integer count = null;
        public Integer failed = null;
        public Integer retries = null;
        public Integer totalMs = null;
        public Integer minMs = null;
        public Integer maxMs = null;
        public Double averageMs = null;
        public Double medianMs = null;
        public ResponseStatus lastError = null;
        public ConcurrentQueue<Integer> timings = null;
        
        public String getType() { return type; }
        public CommandSummary setType(String value) { this.type = value; return this; }
        public String getName() { return name; }
        public CommandSummary setName(String value) { this.name = value; return this; }
        public Integer getCount() { return count; }
        public CommandSummary setCount(Integer value) { this.count = value; return this; }
        public Integer getFailed() { return failed; }
        public CommandSummary setFailed(Integer value) { this.failed = value; return this; }
        public Integer getRetries() { return retries; }
        public CommandSummary setRetries(Integer value) { this.retries = value; return this; }
        public Integer getTotalMs() { return totalMs; }
        public CommandSummary setTotalMs(Integer value) { this.totalMs = value; return this; }
        public Integer getMinMs() { return minMs; }
        public CommandSummary setMinMs(Integer value) { this.minMs = value; return this; }
        public Integer getMaxMs() { return maxMs; }
        public CommandSummary setMaxMs(Integer value) { this.maxMs = value; return this; }
        public Double getAverageMs() { return averageMs; }
        public CommandSummary setAverageMs(Double value) { this.averageMs = value; return this; }
        public Double getMedianMs() { return medianMs; }
        public CommandSummary setMedianMs(Double value) { this.medianMs = value; return this; }
        public ResponseStatus getLastError() { return lastError; }
        public CommandSummary setLastError(ResponseStatus value) { this.lastError = value; return this; }
        public ConcurrentQueue<Integer> getTimings() { return timings; }
        public CommandSummary setTimings(ConcurrentQueue<Integer> value) { this.timings = value; return this; }
    }

    public static class CommandResult
    {
        public String type = null;
        public String name = null;
        public Long ms = null;
        public Date at = null;
        public String request = null;
        public Integer retries = null;
        public Integer attempt = null;
        public ResponseStatus error = null;
        
        public String getType() { return type; }
        public CommandResult setType(String value) { this.type = value; return this; }
        public String getName() { return name; }
        public CommandResult setName(String value) { this.name = value; return this; }
        public Long getMs() { return ms; }
        public CommandResult setMs(Long value) { this.ms = value; return this; }
        public Date getAt() { return at; }
        public CommandResult setAt(Date value) { this.at = value; return this; }
        public String getRequest() { return request; }
        public CommandResult setRequest(String value) { this.request = value; return this; }
        public Integer getRetries() { return retries; }
        public CommandResult setRetries(Integer value) { this.retries = value; return this; }
        public Integer getAttempt() { return attempt; }
        public CommandResult setAttempt(Integer value) { this.attempt = value; return this; }
        public ResponseStatus getError() { return error; }
        public CommandResult setError(ResponseStatus value) { this.error = value; return this; }
    }

    @DataContract
    public static class PartialApiKey
    {
        @DataMember(Order=1)
        public Integer id = null;

        @DataMember(Order=2)
        public String name = null;

        @DataMember(Order=3)
        public String userId = null;

        @DataMember(Order=4)
        public String userName = null;

        @DataMember(Order=5)
        public String visibleKey = null;

        @DataMember(Order=6)
        public String environment = null;

        @DataMember(Order=7)
        public Date createdDate = null;

        @DataMember(Order=8)
        public Date expiryDate = null;

        @DataMember(Order=9)
        public Date cancelledDate = null;

        @DataMember(Order=10)
        public Date lastUsedDate = null;

        @DataMember(Order=11)
        public ArrayList<String> scopes = null;

        @DataMember(Order=12)
        public ArrayList<String> features = null;

        @DataMember(Order=13)
        public ArrayList<String> restrictTo = null;

        @DataMember(Order=14)
        public String notes = null;

        @DataMember(Order=15)
        public Integer refId = null;

        @DataMember(Order=16)
        public String refIdStr = null;

        @DataMember(Order=17)
        public HashMap<String,String> meta = null;

        @DataMember(Order=18)
        public Boolean active = null;
        
        public Integer getId() { return id; }
        public PartialApiKey setId(Integer value) { this.id = value; return this; }
        public String getName() { return name; }
        public PartialApiKey setName(String value) { this.name = value; return this; }
        public String getUserId() { return userId; }
        public PartialApiKey setUserId(String value) { this.userId = value; return this; }
        public String getUserName() { return userName; }
        public PartialApiKey setUserName(String value) { this.userName = value; return this; }
        public String getVisibleKey() { return visibleKey; }
        public PartialApiKey setVisibleKey(String value) { this.visibleKey = value; return this; }
        public String getEnvironment() { return environment; }
        public PartialApiKey setEnvironment(String value) { this.environment = value; return this; }
        public Date getCreatedDate() { return createdDate; }
        public PartialApiKey setCreatedDate(Date value) { this.createdDate = value; return this; }
        public Date getExpiryDate() { return expiryDate; }
        public PartialApiKey setExpiryDate(Date value) { this.expiryDate = value; return this; }
        public Date getCancelledDate() { return cancelledDate; }
        public PartialApiKey setCancelledDate(Date value) { this.cancelledDate = value; return this; }
        public Date getLastUsedDate() { return lastUsedDate; }
        public PartialApiKey setLastUsedDate(Date value) { this.lastUsedDate = value; return this; }
        public ArrayList<String> getScopes() { return scopes; }
        public PartialApiKey setScopes(ArrayList<String> value) { this.scopes = value; return this; }
        public ArrayList<String> getFeatures() { return features; }
        public PartialApiKey setFeatures(ArrayList<String> value) { this.features = value; return this; }
        public ArrayList<String> getRestrictTo() { return restrictTo; }
        public PartialApiKey setRestrictTo(ArrayList<String> value) { this.restrictTo = value; return this; }
        public String getNotes() { return notes; }
        public PartialApiKey setNotes(String value) { this.notes = value; return this; }
        public Integer getRefId() { return refId; }
        public PartialApiKey setRefId(Integer value) { this.refId = value; return this; }
        public String getRefIdStr() { return refIdStr; }
        public PartialApiKey setRefIdStr(String value) { this.refIdStr = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public PartialApiKey setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        public Boolean isActive() { return active; }
        public PartialApiKey setActive(Boolean value) { this.active = value; return this; }
    }

    public static class JobStatSummary
    {
        public String name = null;
        public Integer total = null;
        public Integer completed = null;
        public Integer retries = null;
        public Integer failed = null;
        public Integer cancelled = null;
        
        public String getName() { return name; }
        public JobStatSummary setName(String value) { this.name = value; return this; }
        public Integer getTotal() { return total; }
        public JobStatSummary setTotal(Integer value) { this.total = value; return this; }
        public Integer getCompleted() { return completed; }
        public JobStatSummary setCompleted(Integer value) { this.completed = value; return this; }
        public Integer getRetries() { return retries; }
        public JobStatSummary setRetries(Integer value) { this.retries = value; return this; }
        public Integer getFailed() { return failed; }
        public JobStatSummary setFailed(Integer value) { this.failed = value; return this; }
        public Integer getCancelled() { return cancelled; }
        public JobStatSummary setCancelled(Integer value) { this.cancelled = value; return this; }
    }

    public static class HourSummary
    {
        public String hour = null;
        public Integer total = null;
        public Integer completed = null;
        public Integer failed = null;
        public Integer cancelled = null;
        
        public String getHour() { return hour; }
        public HourSummary setHour(String value) { this.hour = value; return this; }
        public Integer getTotal() { return total; }
        public HourSummary setTotal(Integer value) { this.total = value; return this; }
        public Integer getCompleted() { return completed; }
        public HourSummary setCompleted(Integer value) { this.completed = value; return this; }
        public Integer getFailed() { return failed; }
        public HourSummary setFailed(Integer value) { this.failed = value; return this; }
        public Integer getCancelled() { return cancelled; }
        public HourSummary setCancelled(Integer value) { this.cancelled = value; return this; }
    }

    public static class WorkerStats
    {
        public String name = null;
        public Long queued = null;
        public Long received = null;
        public Long completed = null;
        public Long retries = null;
        public Long failed = null;
        public Long runningJob = null;
        public TimeSpan runningTime = null;
        
        public String getName() { return name; }
        public WorkerStats setName(String value) { this.name = value; return this; }
        public Long getQueued() { return queued; }
        public WorkerStats setQueued(Long value) { this.queued = value; return this; }
        public Long getReceived() { return received; }
        public WorkerStats setReceived(Long value) { this.received = value; return this; }
        public Long getCompleted() { return completed; }
        public WorkerStats setCompleted(Long value) { this.completed = value; return this; }
        public Long getRetries() { return retries; }
        public WorkerStats setRetries(Long value) { this.retries = value; return this; }
        public Long getFailed() { return failed; }
        public WorkerStats setFailed(Long value) { this.failed = value; return this; }
        public Long getRunningJob() { return runningJob; }
        public WorkerStats setRunningJob(Long value) { this.runningJob = value; return this; }
        public TimeSpan getRunningTime() { return runningTime; }
        public WorkerStats setRunningTime(TimeSpan value) { this.runningTime = value; return this; }
    }

    public static class RequestLogEntry
    {
        public Long id = null;
        public String traceId = null;
        public String operationName = null;
        public Date dateTime = null;
        public Integer statusCode = null;
        public String statusDescription = null;
        public String httpMethod = null;
        public String absoluteUri = null;
        public String pathInfo = null;
        @StringLength(MaximumLength=2147483647)
        public String requestBody = null;

        public Object requestDto = null;
        public String userAuthId = null;
        public String sessionId = null;
        public String ipAddress = null;
        public String forwardedFor = null;
        public String referer = null;
        public HashMap<String,String> headers = null;
        public HashMap<String,String> formData = null;
        public HashMap<String,String> items = null;
        public HashMap<String,String> responseHeaders = null;
        public Object session = null;
        public Object responseDto = null;
        public Object errorResponse = null;
        public String exceptionSource = null;
        public IDictionary exceptionData = null;
        public TimeSpan requestDuration = null;
        public HashMap<String,String> meta = null;
        
        public Long getId() { return id; }
        public RequestLogEntry setId(Long value) { this.id = value; return this; }
        public String getTraceId() { return traceId; }
        public RequestLogEntry setTraceId(String value) { this.traceId = value; return this; }
        public String getOperationName() { return operationName; }
        public RequestLogEntry setOperationName(String value) { this.operationName = value; return this; }
        public Date getDateTime() { return dateTime; }
        public RequestLogEntry setDateTime(Date value) { this.dateTime = value; return this; }
        public Integer getStatusCode() { return statusCode; }
        public RequestLogEntry setStatusCode(Integer value) { this.statusCode = value; return this; }
        public String getStatusDescription() { return statusDescription; }
        public RequestLogEntry setStatusDescription(String value) { this.statusDescription = value; return this; }
        public String getHttpMethod() { return httpMethod; }
        public RequestLogEntry setHttpMethod(String value) { this.httpMethod = value; return this; }
        public String getAbsoluteUri() { return absoluteUri; }
        public RequestLogEntry setAbsoluteUri(String value) { this.absoluteUri = value; return this; }
        public String getPathInfo() { return pathInfo; }
        public RequestLogEntry setPathInfo(String value) { this.pathInfo = value; return this; }
        public String getRequestBody() { return requestBody; }
        public RequestLogEntry setRequestBody(String value) { this.requestBody = value; return this; }
        public Object getRequestDto() { return requestDto; }
        public RequestLogEntry setRequestDto(Object value) { this.requestDto = value; return this; }
        public String getUserAuthId() { return userAuthId; }
        public RequestLogEntry setUserAuthId(String value) { this.userAuthId = value; return this; }
        public String getSessionId() { return sessionId; }
        public RequestLogEntry setSessionId(String value) { this.sessionId = value; return this; }
        public String getIpAddress() { return ipAddress; }
        public RequestLogEntry setIpAddress(String value) { this.ipAddress = value; return this; }
        public String getForwardedFor() { return forwardedFor; }
        public RequestLogEntry setForwardedFor(String value) { this.forwardedFor = value; return this; }
        public String getReferer() { return referer; }
        public RequestLogEntry setReferer(String value) { this.referer = value; return this; }
        public HashMap<String,String> getHeaders() { return headers; }
        public RequestLogEntry setHeaders(HashMap<String,String> value) { this.headers = value; return this; }
        public HashMap<String,String> getFormData() { return formData; }
        public RequestLogEntry setFormData(HashMap<String,String> value) { this.formData = value; return this; }
        public HashMap<String,String> getItems() { return items; }
        public RequestLogEntry setItems(HashMap<String,String> value) { this.items = value; return this; }
        public HashMap<String,String> getResponseHeaders() { return responseHeaders; }
        public RequestLogEntry setResponseHeaders(HashMap<String,String> value) { this.responseHeaders = value; return this; }
        public Object getSession() { return session; }
        public RequestLogEntry setSession(Object value) { this.session = value; return this; }
        public Object getResponseDto() { return responseDto; }
        public RequestLogEntry setResponseDto(Object value) { this.responseDto = value; return this; }
        public Object getErrorResponse() { return errorResponse; }
        public RequestLogEntry setErrorResponse(Object value) { this.errorResponse = value; return this; }
        public String getExceptionSource() { return exceptionSource; }
        public RequestLogEntry setExceptionSource(String value) { this.exceptionSource = value; return this; }
        public IDictionary getExceptionData() { return exceptionData; }
        public RequestLogEntry setExceptionData(IDictionary value) { this.exceptionData = value; return this; }
        public TimeSpan getRequestDuration() { return requestDuration; }
        public RequestLogEntry setRequestDuration(TimeSpan value) { this.requestDuration = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public RequestLogEntry setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    @DataContract
    public static class AnalyticsLogInfo
    {
        @DataMember(Order=1)
        public Long id = null;

        @DataMember(Order=2)
        public Date dateTime = null;

        @DataMember(Order=3)
        public String browser = null;

        @DataMember(Order=4)
        public String device = null;

        @DataMember(Order=5)
        public String bot = null;

        @DataMember(Order=6)
        public String op = null;

        @DataMember(Order=7)
        public String userId = null;

        @DataMember(Order=8)
        public String userName = null;

        @DataMember(Order=9)
        public String apiKey = null;

        @DataMember(Order=10)
        public String ip = null;
        
        public Long getId() { return id; }
        public AnalyticsLogInfo setId(Long value) { this.id = value; return this; }
        public Date getDateTime() { return dateTime; }
        public AnalyticsLogInfo setDateTime(Date value) { this.dateTime = value; return this; }
        public String getBrowser() { return browser; }
        public AnalyticsLogInfo setBrowser(String value) { this.browser = value; return this; }
        public String getDevice() { return device; }
        public AnalyticsLogInfo setDevice(String value) { this.device = value; return this; }
        public String getBot() { return bot; }
        public AnalyticsLogInfo setBot(String value) { this.bot = value; return this; }
        public String getOp() { return op; }
        public AnalyticsLogInfo setOp(String value) { this.op = value; return this; }
        public String getUserId() { return userId; }
        public AnalyticsLogInfo setUserId(String value) { this.userId = value; return this; }
        public String getUserName() { return userName; }
        public AnalyticsLogInfo setUserName(String value) { this.userName = value; return this; }
        public String getApiKey() { return apiKey; }
        public AnalyticsLogInfo setApiKey(String value) { this.apiKey = value; return this; }
        public String getIp() { return ip; }
        public AnalyticsLogInfo setIp(String value) { this.ip = value; return this; }
    }

    @DataContract
    public static class AnalyticsReports
    {
        @DataMember(Order=1)
        public Long id = null;

        @DataMember(Order=2)
        public Date created = null;

        @DataMember(Order=3)
        public BigDecimal version = null;

        @DataMember(Order=4)
        public HashMap<String,RequestSummary> apis = null;

        @DataMember(Order=5)
        public HashMap<String,RequestSummary> users = null;

        @DataMember(Order=6)
        public HashMap<String,RequestSummary> tags = null;

        @DataMember(Order=7)
        public HashMap<String,RequestSummary> status = null;

        @DataMember(Order=8)
        public HashMap<String,RequestSummary> days = null;

        @DataMember(Order=9)
        public HashMap<String,RequestSummary> apiKeys = null;

        @DataMember(Order=10)
        public HashMap<String,RequestSummary> ips = null;

        @DataMember(Order=11)
        public HashMap<String,RequestSummary> browsers = null;

        @DataMember(Order=12)
        public HashMap<String,RequestSummary> devices = null;

        @DataMember(Order=13)
        public HashMap<String,RequestSummary> bots = null;

        @DataMember(Order=14)
        public HashMap<String,Long> durations = null;
        
        public Long getId() { return id; }
        public AnalyticsReports setId(Long value) { this.id = value; return this; }
        public Date getCreated() { return created; }
        public AnalyticsReports setCreated(Date value) { this.created = value; return this; }
        public BigDecimal getVersion() { return version; }
        public AnalyticsReports setVersion(BigDecimal value) { this.version = value; return this; }
        public HashMap<String,RequestSummary> getApis() { return apis; }
        public AnalyticsReports setApis(HashMap<String,RequestSummary> value) { this.apis = value; return this; }
        public HashMap<String,RequestSummary> getUsers() { return users; }
        public AnalyticsReports setUsers(HashMap<String,RequestSummary> value) { this.users = value; return this; }
        public HashMap<String,RequestSummary> getTags() { return tags; }
        public AnalyticsReports setTags(HashMap<String,RequestSummary> value) { this.tags = value; return this; }
        public HashMap<String,RequestSummary> getStatus() { return status; }
        public AnalyticsReports setStatus(HashMap<String,RequestSummary> value) { this.status = value; return this; }
        public HashMap<String,RequestSummary> getDays() { return days; }
        public AnalyticsReports setDays(HashMap<String,RequestSummary> value) { this.days = value; return this; }
        public HashMap<String,RequestSummary> getApiKeys() { return apiKeys; }
        public AnalyticsReports setApiKeys(HashMap<String,RequestSummary> value) { this.apiKeys = value; return this; }
        public HashMap<String,RequestSummary> getIps() { return ips; }
        public AnalyticsReports setIps(HashMap<String,RequestSummary> value) { this.ips = value; return this; }
        public HashMap<String,RequestSummary> getBrowsers() { return browsers; }
        public AnalyticsReports setBrowsers(HashMap<String,RequestSummary> value) { this.browsers = value; return this; }
        public HashMap<String,RequestSummary> getDevices() { return devices; }
        public AnalyticsReports setDevices(HashMap<String,RequestSummary> value) { this.devices = value; return this; }
        public HashMap<String,RequestSummary> getBots() { return bots; }
        public AnalyticsReports setBots(HashMap<String,RequestSummary> value) { this.bots = value; return this; }
        public HashMap<String,Long> getDurations() { return durations; }
        public AnalyticsReports setDurations(HashMap<String,Long> value) { this.durations = value; return this; }
    }

    @DataContract
    public static class QueryBase
    {
        @DataMember(Order=1)
        public Integer skip = null;

        @DataMember(Order=2)
        public Integer take = null;

        @DataMember(Order=3)
        public String orderBy = null;

        @DataMember(Order=4)
        public String orderByDesc = null;

        @DataMember(Order=5)
        public String include = null;

        @DataMember(Order=6)
        public String fields = null;

        @DataMember(Order=7)
        public HashMap<String,String> meta = null;
        
        public Integer getSkip() { return skip; }
        public QueryBase setSkip(Integer value) { this.skip = value; return this; }
        public Integer getTake() { return take; }
        public QueryBase setTake(Integer value) { this.take = value; return this; }
        public String getOrderBy() { return orderBy; }
        public QueryBase setOrderBy(String value) { this.orderBy = value; return this; }
        public String getOrderByDesc() { return orderByDesc; }
        public QueryBase setOrderByDesc(String value) { this.orderByDesc = value; return this; }
        public String getInclude() { return include; }
        public QueryBase setInclude(String value) { this.include = value; return this; }
        public String getFields() { return fields; }
        public QueryBase setFields(String value) { this.fields = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public QueryBase setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class BackgroundJobBase
    {
        public Long id = null;
        public Long parentId = null;
        public String refId = null;
        public String worker = null;
        public String tag = null;
        public String batchId = null;
        public String callback = null;
        public Long dependsOn = null;
        public Date runAfter = null;
        public Date createdDate = null;
        public String createdBy = null;
        public String requestId = null;
        public String requestType = null;
        public String command = null;
        public String request = null;
        public String requestBody = null;
        public String userId = null;
        public String response = null;
        public String responseBody = null;
        public BackgroundJobState state = null;
        public Date startedDate = null;
        public Date completedDate = null;
        public Date notifiedDate = null;
        public Integer retryLimit = null;
        public Integer attempts = null;
        public Integer durationMs = null;
        public Integer timeoutSecs = null;
        public Double progress = null;
        public String status = null;
        public String logs = null;
        public Date lastActivityDate = null;
        public String replyTo = null;
        public String errorCode = null;
        public ResponseStatus error = null;
        public HashMap<String,String> args = null;
        public HashMap<String,String> meta = null;
        
        public Long getId() { return id; }
        public BackgroundJobBase setId(Long value) { this.id = value; return this; }
        public Long getParentId() { return parentId; }
        public BackgroundJobBase setParentId(Long value) { this.parentId = value; return this; }
        public String getRefId() { return refId; }
        public BackgroundJobBase setRefId(String value) { this.refId = value; return this; }
        public String getWorker() { return worker; }
        public BackgroundJobBase setWorker(String value) { this.worker = value; return this; }
        public String getTag() { return tag; }
        public BackgroundJobBase setTag(String value) { this.tag = value; return this; }
        public String getBatchId() { return batchId; }
        public BackgroundJobBase setBatchId(String value) { this.batchId = value; return this; }
        public String getCallback() { return callback; }
        public BackgroundJobBase setCallback(String value) { this.callback = value; return this; }
        public Long getDependsOn() { return dependsOn; }
        public BackgroundJobBase setDependsOn(Long value) { this.dependsOn = value; return this; }
        public Date getRunAfter() { return runAfter; }
        public BackgroundJobBase setRunAfter(Date value) { this.runAfter = value; return this; }
        public Date getCreatedDate() { return createdDate; }
        public BackgroundJobBase setCreatedDate(Date value) { this.createdDate = value; return this; }
        public String getCreatedBy() { return createdBy; }
        public BackgroundJobBase setCreatedBy(String value) { this.createdBy = value; return this; }
        public String getRequestId() { return requestId; }
        public BackgroundJobBase setRequestId(String value) { this.requestId = value; return this; }
        public String getRequestType() { return requestType; }
        public BackgroundJobBase setRequestType(String value) { this.requestType = value; return this; }
        public String getCommand() { return command; }
        public BackgroundJobBase setCommand(String value) { this.command = value; return this; }
        public String getRequest() { return request; }
        public BackgroundJobBase setRequest(String value) { this.request = value; return this; }
        public String getRequestBody() { return requestBody; }
        public BackgroundJobBase setRequestBody(String value) { this.requestBody = value; return this; }
        public String getUserId() { return userId; }
        public BackgroundJobBase setUserId(String value) { this.userId = value; return this; }
        public String getResponse() { return response; }
        public BackgroundJobBase setResponse(String value) { this.response = value; return this; }
        public String getResponseBody() { return responseBody; }
        public BackgroundJobBase setResponseBody(String value) { this.responseBody = value; return this; }
        public BackgroundJobState getState() { return state; }
        public BackgroundJobBase setState(BackgroundJobState value) { this.state = value; return this; }
        public Date getStartedDate() { return startedDate; }
        public BackgroundJobBase setStartedDate(Date value) { this.startedDate = value; return this; }
        public Date getCompletedDate() { return completedDate; }
        public BackgroundJobBase setCompletedDate(Date value) { this.completedDate = value; return this; }
        public Date getNotifiedDate() { return notifiedDate; }
        public BackgroundJobBase setNotifiedDate(Date value) { this.notifiedDate = value; return this; }
        public Integer getRetryLimit() { return retryLimit; }
        public BackgroundJobBase setRetryLimit(Integer value) { this.retryLimit = value; return this; }
        public Integer getAttempts() { return attempts; }
        public BackgroundJobBase setAttempts(Integer value) { this.attempts = value; return this; }
        public Integer getDurationMs() { return durationMs; }
        public BackgroundJobBase setDurationMs(Integer value) { this.durationMs = value; return this; }
        public Integer getTimeoutSecs() { return timeoutSecs; }
        public BackgroundJobBase setTimeoutSecs(Integer value) { this.timeoutSecs = value; return this; }
        public Double getProgress() { return progress; }
        public BackgroundJobBase setProgress(Double value) { this.progress = value; return this; }
        public String getStatus() { return status; }
        public BackgroundJobBase setStatus(String value) { this.status = value; return this; }
        public String getLogs() { return logs; }
        public BackgroundJobBase setLogs(String value) { this.logs = value; return this; }
        public Date getLastActivityDate() { return lastActivityDate; }
        public BackgroundJobBase setLastActivityDate(Date value) { this.lastActivityDate = value; return this; }
        public String getReplyTo() { return replyTo; }
        public BackgroundJobBase setReplyTo(String value) { this.replyTo = value; return this; }
        public String getErrorCode() { return errorCode; }
        public BackgroundJobBase setErrorCode(String value) { this.errorCode = value; return this; }
        public ResponseStatus getError() { return error; }
        public BackgroundJobBase setError(ResponseStatus value) { this.error = value; return this; }
        public HashMap<String,String> getArgs() { return args; }
        public BackgroundJobBase setArgs(HashMap<String,String> value) { this.args = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public BackgroundJobBase setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class BackgroundJobOptions
    {
        public String refId = null;
        public Long parentId = null;
        public String worker = null;
        public Date runAfter = null;
        public String callback = null;
        public Long dependsOn = null;
        public String userId = null;
        public Integer retryLimit = null;
        public String replyTo = null;
        public String tag = null;
        public String batchId = null;
        public String createdBy = null;
        public Integer timeoutSecs = null;
        public TimeSpan timeout = null;
        public HashMap<String,String> args = null;
        public Boolean runCommand = null;
        
        public String getRefId() { return refId; }
        public BackgroundJobOptions setRefId(String value) { this.refId = value; return this; }
        public Long getParentId() { return parentId; }
        public BackgroundJobOptions setParentId(Long value) { this.parentId = value; return this; }
        public String getWorker() { return worker; }
        public BackgroundJobOptions setWorker(String value) { this.worker = value; return this; }
        public Date getRunAfter() { return runAfter; }
        public BackgroundJobOptions setRunAfter(Date value) { this.runAfter = value; return this; }
        public String getCallback() { return callback; }
        public BackgroundJobOptions setCallback(String value) { this.callback = value; return this; }
        public Long getDependsOn() { return dependsOn; }
        public BackgroundJobOptions setDependsOn(Long value) { this.dependsOn = value; return this; }
        public String getUserId() { return userId; }
        public BackgroundJobOptions setUserId(String value) { this.userId = value; return this; }
        public Integer getRetryLimit() { return retryLimit; }
        public BackgroundJobOptions setRetryLimit(Integer value) { this.retryLimit = value; return this; }
        public String getReplyTo() { return replyTo; }
        public BackgroundJobOptions setReplyTo(String value) { this.replyTo = value; return this; }
        public String getTag() { return tag; }
        public BackgroundJobOptions setTag(String value) { this.tag = value; return this; }
        public String getBatchId() { return batchId; }
        public BackgroundJobOptions setBatchId(String value) { this.batchId = value; return this; }
        public String getCreatedBy() { return createdBy; }
        public BackgroundJobOptions setCreatedBy(String value) { this.createdBy = value; return this; }
        public Integer getTimeoutSecs() { return timeoutSecs; }
        public BackgroundJobOptions setTimeoutSecs(Integer value) { this.timeoutSecs = value; return this; }
        public TimeSpan getTimeout() { return timeout; }
        public BackgroundJobOptions setTimeout(TimeSpan value) { this.timeout = value; return this; }
        public HashMap<String,String> getArgs() { return args; }
        public BackgroundJobOptions setArgs(HashMap<String,String> value) { this.args = value; return this; }
        public Boolean isRunCommand() { return runCommand; }
        public BackgroundJobOptions setRunCommand(Boolean value) { this.runCommand = value; return this; }
    }

    public static class ValidateRule
    {
        public String validator = null;
        public String condition = null;
        public String errorCode = null;
        public String message = null;
        
        public String getValidator() { return validator; }
        public ValidateRule setValidator(String value) { this.validator = value; return this; }
        public String getCondition() { return condition; }
        public ValidateRule setCondition(String value) { this.condition = value; return this; }
        public String getErrorCode() { return errorCode; }
        public ValidateRule setErrorCode(String value) { this.errorCode = value; return this; }
        public String getMessage() { return message; }
        public ValidateRule setMessage(String value) { this.message = value; return this; }
    }

    public static class ImageInfo
    {
        public String svg = null;
        public String uri = null;
        public String alt = null;
        public String cls = null;
        
        public String getSvg() { return svg; }
        public ImageInfo setSvg(String value) { this.svg = value; return this; }
        public String getUri() { return uri; }
        public ImageInfo setUri(String value) { this.uri = value; return this; }
        public String getAlt() { return alt; }
        public ImageInfo setAlt(String value) { this.alt = value; return this; }
        public String getCls() { return cls; }
        public ImageInfo setCls(String value) { this.cls = value; return this; }
    }

    public static class LinkInfo
    {
        public String id = null;
        public String href = null;
        public String label = null;
        public ImageInfo icon = null;
        public String show = null;
        public String hide = null;
        
        public String getId() { return id; }
        public LinkInfo setId(String value) { this.id = value; return this; }
        public String getHref() { return href; }
        public LinkInfo setHref(String value) { this.href = value; return this; }
        public String getLabel() { return label; }
        public LinkInfo setLabel(String value) { this.label = value; return this; }
        public ImageInfo getIcon() { return icon; }
        public LinkInfo setIcon(ImageInfo value) { this.icon = value; return this; }
        public String getShow() { return show; }
        public LinkInfo setShow(String value) { this.show = value; return this; }
        public String getHide() { return hide; }
        public LinkInfo setHide(String value) { this.hide = value; return this; }
    }

    public static class ThemeInfo
    {
        public String form = null;
        public ImageInfo modelIcon = null;
        
        public String getForm() { return form; }
        public ThemeInfo setForm(String value) { this.form = value; return this; }
        public ImageInfo getModelIcon() { return modelIcon; }
        public ThemeInfo setModelIcon(ImageInfo value) { this.modelIcon = value; return this; }
    }

    public static class LocodeUi
    {
        public ApiCss css = null;
        public AppTags tags = null;
        public Integer maxFieldLength = null;
        public Integer maxNestedFields = null;
        public Integer maxNestedFieldLength = null;
        
        public ApiCss getCss() { return css; }
        public LocodeUi setCss(ApiCss value) { this.css = value; return this; }
        public AppTags getTags() { return tags; }
        public LocodeUi setTags(AppTags value) { this.tags = value; return this; }
        public Integer getMaxFieldLength() { return maxFieldLength; }
        public LocodeUi setMaxFieldLength(Integer value) { this.maxFieldLength = value; return this; }
        public Integer getMaxNestedFields() { return maxNestedFields; }
        public LocodeUi setMaxNestedFields(Integer value) { this.maxNestedFields = value; return this; }
        public Integer getMaxNestedFieldLength() { return maxNestedFieldLength; }
        public LocodeUi setMaxNestedFieldLength(Integer value) { this.maxNestedFieldLength = value; return this; }
    }

    public static class ExplorerUi
    {
        public ApiCss css = null;
        public AppTags tags = null;
        
        public ApiCss getCss() { return css; }
        public ExplorerUi setCss(ApiCss value) { this.css = value; return this; }
        public AppTags getTags() { return tags; }
        public ExplorerUi setTags(AppTags value) { this.tags = value; return this; }
    }

    public static class AdminUi
    {
        public ApiCss css = null;
        
        public ApiCss getCss() { return css; }
        public AdminUi setCss(ApiCss value) { this.css = value; return this; }
    }

    public static class ApiFormat
    {
        public String locale = null;
        public Boolean assumeUtc = null;
        public FormatInfo number = null;
        public FormatInfo date = null;
        
        public String getLocale() { return locale; }
        public ApiFormat setLocale(String value) { this.locale = value; return this; }
        public Boolean isAssumeUtc() { return assumeUtc; }
        public ApiFormat setAssumeUtc(Boolean value) { this.assumeUtc = value; return this; }
        public FormatInfo getNumber() { return number; }
        public ApiFormat setNumber(FormatInfo value) { this.number = value; return this; }
        public FormatInfo getDate() { return date; }
        public ApiFormat setDate(FormatInfo value) { this.date = value; return this; }
    }

    public static class AuthInfo
    {
        public Boolean hasAuthSecret = null;
        public Boolean hasAuthRepository = null;
        public Boolean includesRoles = null;
        public Boolean includesOAuthTokens = null;
        public String htmlRedirect = null;
        public ArrayList<MetaAuthProvider> authProviders = null;
        public IdentityAuthInfo identityAuth = null;
        public HashMap<String,ArrayList<LinkInfo>> roleLinks = null;
        public HashMap<String,ArrayList<String>> serviceRoutes = null;
        public HashMap<String,String> meta = null;
        
        public Boolean isHasAuthSecret() { return hasAuthSecret; }
        public AuthInfo setHasAuthSecret(Boolean value) { this.hasAuthSecret = value; return this; }
        public Boolean isHasAuthRepository() { return hasAuthRepository; }
        public AuthInfo setHasAuthRepository(Boolean value) { this.hasAuthRepository = value; return this; }
        public Boolean isIncludesRoles() { return includesRoles; }
        public AuthInfo setIncludesRoles(Boolean value) { this.includesRoles = value; return this; }
        public Boolean isIncludesOAuthTokens() { return includesOAuthTokens; }
        public AuthInfo setIncludesOAuthTokens(Boolean value) { this.includesOAuthTokens = value; return this; }
        public String getHtmlRedirect() { return htmlRedirect; }
        public AuthInfo setHtmlRedirect(String value) { this.htmlRedirect = value; return this; }
        public ArrayList<MetaAuthProvider> getAuthProviders() { return authProviders; }
        public AuthInfo setAuthProviders(ArrayList<MetaAuthProvider> value) { this.authProviders = value; return this; }
        public IdentityAuthInfo getIdentityAuth() { return identityAuth; }
        public AuthInfo setIdentityAuth(IdentityAuthInfo value) { this.identityAuth = value; return this; }
        public HashMap<String,ArrayList<LinkInfo>> getRoleLinks() { return roleLinks; }
        public AuthInfo setRoleLinks(HashMap<String,ArrayList<LinkInfo>> value) { this.roleLinks = value; return this; }
        public HashMap<String,ArrayList<String>> getServiceRoutes() { return serviceRoutes; }
        public AuthInfo setServiceRoutes(HashMap<String,ArrayList<String>> value) { this.serviceRoutes = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AuthInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class ApiKeyInfo
    {
        public String label = null;
        public String httpHeader = null;
        public ArrayList<String> scopes = null;
        public ArrayList<String> features = null;
        public ArrayList<String> requestTypes = null;
        public ArrayList<KeyValuePair<String,String>> expiresIn = null;
        public ArrayList<String> hide = null;
        public HashMap<String,String> meta = null;
        
        public String getLabel() { return label; }
        public ApiKeyInfo setLabel(String value) { this.label = value; return this; }
        public String getHttpHeader() { return httpHeader; }
        public ApiKeyInfo setHttpHeader(String value) { this.httpHeader = value; return this; }
        public ArrayList<String> getScopes() { return scopes; }
        public ApiKeyInfo setScopes(ArrayList<String> value) { this.scopes = value; return this; }
        public ArrayList<String> getFeatures() { return features; }
        public ApiKeyInfo setFeatures(ArrayList<String> value) { this.features = value; return this; }
        public ArrayList<String> getRequestTypes() { return requestTypes; }
        public ApiKeyInfo setRequestTypes(ArrayList<String> value) { this.requestTypes = value; return this; }
        public ArrayList<KeyValuePair<String,String>> getExpiresIn() { return expiresIn; }
        public ApiKeyInfo setExpiresIn(ArrayList<KeyValuePair<String,String>> value) { this.expiresIn = value; return this; }
        public ArrayList<String> getHide() { return hide; }
        public ApiKeyInfo setHide(ArrayList<String> value) { this.hide = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public ApiKeyInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class CommandsInfo
    {
        public ArrayList<CommandInfo> commands = null;
        public HashMap<String,String> meta = null;
        
        public ArrayList<CommandInfo> getCommands() { return commands; }
        public CommandsInfo setCommands(ArrayList<CommandInfo> value) { this.commands = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public CommandsInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class AutoQueryInfo
    {
        public Integer maxLimit = null;
        public Boolean untypedQueries = null;
        public Boolean rawSqlFilters = null;
        public Boolean autoQueryViewer = null;
        public Boolean async = null;
        public Boolean orderByPrimaryKey = null;
        public Boolean crudEvents = null;
        public Boolean crudEventsServices = null;
        public String accessRole = null;
        public String namedConnection = null;
        public ArrayList<AutoQueryConvention> viewerConventions = null;
        public HashMap<String,String> meta = null;
        
        public Integer getMaxLimit() { return maxLimit; }
        public AutoQueryInfo setMaxLimit(Integer value) { this.maxLimit = value; return this; }
        public Boolean isUntypedQueries() { return untypedQueries; }
        public AutoQueryInfo setUntypedQueries(Boolean value) { this.untypedQueries = value; return this; }
        public Boolean isRawSqlFilters() { return rawSqlFilters; }
        public AutoQueryInfo setRawSqlFilters(Boolean value) { this.rawSqlFilters = value; return this; }
        public Boolean isAutoQueryViewer() { return autoQueryViewer; }
        public AutoQueryInfo setAutoQueryViewer(Boolean value) { this.autoQueryViewer = value; return this; }
        public Boolean isAsync() { return async; }
        public AutoQueryInfo setAsync(Boolean value) { this.async = value; return this; }
        public Boolean isOrderByPrimaryKey() { return orderByPrimaryKey; }
        public AutoQueryInfo setOrderByPrimaryKey(Boolean value) { this.orderByPrimaryKey = value; return this; }
        public Boolean isCrudEvents() { return crudEvents; }
        public AutoQueryInfo setCrudEvents(Boolean value) { this.crudEvents = value; return this; }
        public Boolean isCrudEventsServices() { return crudEventsServices; }
        public AutoQueryInfo setCrudEventsServices(Boolean value) { this.crudEventsServices = value; return this; }
        public String getAccessRole() { return accessRole; }
        public AutoQueryInfo setAccessRole(String value) { this.accessRole = value; return this; }
        public String getNamedConnection() { return namedConnection; }
        public AutoQueryInfo setNamedConnection(String value) { this.namedConnection = value; return this; }
        public ArrayList<AutoQueryConvention> getViewerConventions() { return viewerConventions; }
        public AutoQueryInfo setViewerConventions(ArrayList<AutoQueryConvention> value) { this.viewerConventions = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AutoQueryInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class ValidationInfo
    {
        public Boolean hasValidationSource = null;
        public Boolean hasValidationSourceAdmin = null;
        public HashMap<String,ArrayList<String>> serviceRoutes = null;
        public ArrayList<ScriptMethodType> typeValidators = null;
        public ArrayList<ScriptMethodType> propertyValidators = null;
        public String accessRole = null;
        public HashMap<String,String> meta = null;
        
        public Boolean isHasValidationSource() { return hasValidationSource; }
        public ValidationInfo setHasValidationSource(Boolean value) { this.hasValidationSource = value; return this; }
        public Boolean isHasValidationSourceAdmin() { return hasValidationSourceAdmin; }
        public ValidationInfo setHasValidationSourceAdmin(Boolean value) { this.hasValidationSourceAdmin = value; return this; }
        public HashMap<String,ArrayList<String>> getServiceRoutes() { return serviceRoutes; }
        public ValidationInfo setServiceRoutes(HashMap<String,ArrayList<String>> value) { this.serviceRoutes = value; return this; }
        public ArrayList<ScriptMethodType> getTypeValidators() { return typeValidators; }
        public ValidationInfo setTypeValidators(ArrayList<ScriptMethodType> value) { this.typeValidators = value; return this; }
        public ArrayList<ScriptMethodType> getPropertyValidators() { return propertyValidators; }
        public ValidationInfo setPropertyValidators(ArrayList<ScriptMethodType> value) { this.propertyValidators = value; return this; }
        public String getAccessRole() { return accessRole; }
        public ValidationInfo setAccessRole(String value) { this.accessRole = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public ValidationInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class SharpPagesInfo
    {
        public String apiPath = null;
        public String scriptAdminRole = null;
        public String metadataDebugAdminRole = null;
        public Boolean metadataDebug = null;
        public Boolean spaFallback = null;
        public HashMap<String,String> meta = null;
        
        public String getApiPath() { return apiPath; }
        public SharpPagesInfo setApiPath(String value) { this.apiPath = value; return this; }
        public String getScriptAdminRole() { return scriptAdminRole; }
        public SharpPagesInfo setScriptAdminRole(String value) { this.scriptAdminRole = value; return this; }
        public String getMetadataDebugAdminRole() { return metadataDebugAdminRole; }
        public SharpPagesInfo setMetadataDebugAdminRole(String value) { this.metadataDebugAdminRole = value; return this; }
        public Boolean isMetadataDebug() { return metadataDebug; }
        public SharpPagesInfo setMetadataDebug(Boolean value) { this.metadataDebug = value; return this; }
        public Boolean isSpaFallback() { return spaFallback; }
        public SharpPagesInfo setSpaFallback(Boolean value) { this.spaFallback = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public SharpPagesInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class RequestLogsInfo
    {
        public String accessRole = null;
        public String requestLogger = null;
        public Integer defaultLimit = null;
        public HashMap<String,ArrayList<String>> serviceRoutes = null;
        public RequestLogsAnalytics analytics = null;
        public HashMap<String,String> meta = null;
        
        public String getAccessRole() { return accessRole; }
        public RequestLogsInfo setAccessRole(String value) { this.accessRole = value; return this; }
        public String getRequestLogger() { return requestLogger; }
        public RequestLogsInfo setRequestLogger(String value) { this.requestLogger = value; return this; }
        public Integer getDefaultLimit() { return defaultLimit; }
        public RequestLogsInfo setDefaultLimit(Integer value) { this.defaultLimit = value; return this; }
        public HashMap<String,ArrayList<String>> getServiceRoutes() { return serviceRoutes; }
        public RequestLogsInfo setServiceRoutes(HashMap<String,ArrayList<String>> value) { this.serviceRoutes = value; return this; }
        public RequestLogsAnalytics getAnalytics() { return analytics; }
        public RequestLogsInfo setAnalytics(RequestLogsAnalytics value) { this.analytics = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public RequestLogsInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class ProfilingInfo
    {
        public String accessRole = null;
        public Integer defaultLimit = null;
        public ArrayList<String> summaryFields = null;
        public String tagLabel = null;
        public HashMap<String,String> meta = null;
        
        public String getAccessRole() { return accessRole; }
        public ProfilingInfo setAccessRole(String value) { this.accessRole = value; return this; }
        public Integer getDefaultLimit() { return defaultLimit; }
        public ProfilingInfo setDefaultLimit(Integer value) { this.defaultLimit = value; return this; }
        public ArrayList<String> getSummaryFields() { return summaryFields; }
        public ProfilingInfo setSummaryFields(ArrayList<String> value) { this.summaryFields = value; return this; }
        public String getTagLabel() { return tagLabel; }
        public ProfilingInfo setTagLabel(String value) { this.tagLabel = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public ProfilingInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class FilesUploadInfo
    {
        public String basePath = null;
        public ArrayList<FilesUploadLocation> locations = null;
        public HashMap<String,String> meta = null;
        
        public String getBasePath() { return basePath; }
        public FilesUploadInfo setBasePath(String value) { this.basePath = value; return this; }
        public ArrayList<FilesUploadLocation> getLocations() { return locations; }
        public FilesUploadInfo setLocations(ArrayList<FilesUploadLocation> value) { this.locations = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public FilesUploadInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class AdminUsersInfo
    {
        public String accessRole = null;
        public ArrayList<String> enabled = null;
        public MetadataType userAuth = null;
        public ArrayList<String> allRoles = null;
        public ArrayList<String> allPermissions = null;
        public ArrayList<String> queryUserAuthProperties = null;
        public ArrayList<MediaRule> queryMediaRules = null;
        public ArrayList<InputInfo> formLayout = null;
        public ApiCss css = null;
        public HashMap<String,String> meta = null;
        
        public String getAccessRole() { return accessRole; }
        public AdminUsersInfo setAccessRole(String value) { this.accessRole = value; return this; }
        public ArrayList<String> getEnabled() { return enabled; }
        public AdminUsersInfo setEnabled(ArrayList<String> value) { this.enabled = value; return this; }
        public MetadataType getUserAuth() { return userAuth; }
        public AdminUsersInfo setUserAuth(MetadataType value) { this.userAuth = value; return this; }
        public ArrayList<String> getAllRoles() { return allRoles; }
        public AdminUsersInfo setAllRoles(ArrayList<String> value) { this.allRoles = value; return this; }
        public ArrayList<String> getAllPermissions() { return allPermissions; }
        public AdminUsersInfo setAllPermissions(ArrayList<String> value) { this.allPermissions = value; return this; }
        public ArrayList<String> getQueryUserAuthProperties() { return queryUserAuthProperties; }
        public AdminUsersInfo setQueryUserAuthProperties(ArrayList<String> value) { this.queryUserAuthProperties = value; return this; }
        public ArrayList<MediaRule> getQueryMediaRules() { return queryMediaRules; }
        public AdminUsersInfo setQueryMediaRules(ArrayList<MediaRule> value) { this.queryMediaRules = value; return this; }
        public ArrayList<InputInfo> getFormLayout() { return formLayout; }
        public AdminUsersInfo setFormLayout(ArrayList<InputInfo> value) { this.formLayout = value; return this; }
        public ApiCss getCss() { return css; }
        public AdminUsersInfo setCss(ApiCss value) { this.css = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AdminUsersInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class AdminIdentityUsersInfo
    {
        public String accessRole = null;
        public ArrayList<String> enabled = null;
        public MetadataType identityUser = null;
        public ArrayList<String> allRoles = null;
        public ArrayList<String> allPermissions = null;
        public ArrayList<String> queryIdentityUserProperties = null;
        public ArrayList<MediaRule> queryMediaRules = null;
        public ArrayList<InputInfo> formLayout = null;
        public ApiCss css = null;
        public HashMap<String,String> meta = null;
        
        public String getAccessRole() { return accessRole; }
        public AdminIdentityUsersInfo setAccessRole(String value) { this.accessRole = value; return this; }
        public ArrayList<String> getEnabled() { return enabled; }
        public AdminIdentityUsersInfo setEnabled(ArrayList<String> value) { this.enabled = value; return this; }
        public MetadataType getIdentityUser() { return identityUser; }
        public AdminIdentityUsersInfo setIdentityUser(MetadataType value) { this.identityUser = value; return this; }
        public ArrayList<String> getAllRoles() { return allRoles; }
        public AdminIdentityUsersInfo setAllRoles(ArrayList<String> value) { this.allRoles = value; return this; }
        public ArrayList<String> getAllPermissions() { return allPermissions; }
        public AdminIdentityUsersInfo setAllPermissions(ArrayList<String> value) { this.allPermissions = value; return this; }
        public ArrayList<String> getQueryIdentityUserProperties() { return queryIdentityUserProperties; }
        public AdminIdentityUsersInfo setQueryIdentityUserProperties(ArrayList<String> value) { this.queryIdentityUserProperties = value; return this; }
        public ArrayList<MediaRule> getQueryMediaRules() { return queryMediaRules; }
        public AdminIdentityUsersInfo setQueryMediaRules(ArrayList<MediaRule> value) { this.queryMediaRules = value; return this; }
        public ArrayList<InputInfo> getFormLayout() { return formLayout; }
        public AdminIdentityUsersInfo setFormLayout(ArrayList<InputInfo> value) { this.formLayout = value; return this; }
        public ApiCss getCss() { return css; }
        public AdminIdentityUsersInfo setCss(ApiCss value) { this.css = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AdminIdentityUsersInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class AdminRedisInfo
    {
        public Integer queryLimit = null;
        public ArrayList<Integer> databases = null;
        public Boolean modifiableConnection = null;
        public RedisEndpointInfo endpoint = null;
        public HashMap<String,String> meta = null;
        
        public Integer getQueryLimit() { return queryLimit; }
        public AdminRedisInfo setQueryLimit(Integer value) { this.queryLimit = value; return this; }
        public ArrayList<Integer> getDatabases() { return databases; }
        public AdminRedisInfo setDatabases(ArrayList<Integer> value) { this.databases = value; return this; }
        public Boolean isModifiableConnection() { return modifiableConnection; }
        public AdminRedisInfo setModifiableConnection(Boolean value) { this.modifiableConnection = value; return this; }
        public RedisEndpointInfo getEndpoint() { return endpoint; }
        public AdminRedisInfo setEndpoint(RedisEndpointInfo value) { this.endpoint = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AdminRedisInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class AdminDatabaseInfo
    {
        public Integer queryLimit = null;
        public ArrayList<DatabaseInfo> databases = null;
        public HashMap<String,String> meta = null;
        
        public Integer getQueryLimit() { return queryLimit; }
        public AdminDatabaseInfo setQueryLimit(Integer value) { this.queryLimit = value; return this; }
        public ArrayList<DatabaseInfo> getDatabases() { return databases; }
        public AdminDatabaseInfo setDatabases(ArrayList<DatabaseInfo> value) { this.databases = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AdminDatabaseInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class MetadataTypesConfig
    {
        public String baseUrl = null;
        public String usePath = null;
        public Boolean makePartial = null;
        public Boolean makeVirtual = null;
        public Boolean makeInternal = null;
        public String baseClass = null;
        @SerializedName("package") public String Package = null;
        public Boolean addReturnMarker = null;
        public Boolean addDescriptionAsComments = null;
        public Boolean addDocAnnotations = null;
        public Boolean addDataContractAttributes = null;
        public Boolean addIndexesToDataMembers = null;
        public Boolean addGeneratedCodeAttributes = null;
        public Integer addImplicitVersion = null;
        public Boolean addResponseStatus = null;
        public Boolean addServiceStackTypes = null;
        public Boolean addModelExtensions = null;
        public Boolean addPropertyAccessors = null;
        public Boolean excludeGenericBaseTypes = null;
        public Boolean settersReturnThis = null;
        public Boolean addNullableAnnotations = null;
        public Boolean makePropertiesOptional = null;
        public Boolean exportAsTypes = null;
        public Boolean excludeImplementedInterfaces = null;
        public String addDefaultXmlNamespace = null;
        public Boolean makeDataContractsExtensible = null;
        public Boolean initializeCollections = null;
        public ArrayList<String> addNamespaces = null;
        public ArrayList<String> defaultNamespaces = null;
        public ArrayList<String> defaultImports = null;
        public ArrayList<String> includeTypes = null;
        public ArrayList<String> excludeTypes = null;
        public ArrayList<String> exportTags = null;
        public ArrayList<String> treatTypesAsStrings = null;
        public Boolean exportValueTypes = null;
        public String globalNamespace = null;
        public Boolean excludeNamespace = null;
        public String dataClass = null;
        public String dataClassJson = null;
        public ArrayList<Class> ignoreTypes = null;
        public ArrayList<Class> exportTypes = null;
        public ArrayList<Class> exportAttributes = null;
        public ArrayList<String> ignoreTypesInNamespaces = null;
        
        public String getBaseUrl() { return baseUrl; }
        public MetadataTypesConfig setBaseUrl(String value) { this.baseUrl = value; return this; }
        public String getUsePath() { return usePath; }
        public MetadataTypesConfig setUsePath(String value) { this.usePath = value; return this; }
        public Boolean isMakePartial() { return makePartial; }
        public MetadataTypesConfig setMakePartial(Boolean value) { this.makePartial = value; return this; }
        public Boolean isMakeVirtual() { return makeVirtual; }
        public MetadataTypesConfig setMakeVirtual(Boolean value) { this.makeVirtual = value; return this; }
        public Boolean isMakeInternal() { return makeInternal; }
        public MetadataTypesConfig setMakeInternal(Boolean value) { this.makeInternal = value; return this; }
        public String getBaseClass() { return baseClass; }
        public MetadataTypesConfig setBaseClass(String value) { this.baseClass = value; return this; }
        public String getPackage() { return Package; }
        public MetadataTypesConfig setPackage(String value) { this.Package = value; return this; }
        public Boolean isAddReturnMarker() { return addReturnMarker; }
        public MetadataTypesConfig setAddReturnMarker(Boolean value) { this.addReturnMarker = value; return this; }
        public Boolean isAddDescriptionAsComments() { return addDescriptionAsComments; }
        public MetadataTypesConfig setAddDescriptionAsComments(Boolean value) { this.addDescriptionAsComments = value; return this; }
        public Boolean isAddDocAnnotations() { return addDocAnnotations; }
        public MetadataTypesConfig setAddDocAnnotations(Boolean value) { this.addDocAnnotations = value; return this; }
        public Boolean isAddDataContractAttributes() { return addDataContractAttributes; }
        public MetadataTypesConfig setAddDataContractAttributes(Boolean value) { this.addDataContractAttributes = value; return this; }
        public Boolean isAddIndexesToDataMembers() { return addIndexesToDataMembers; }
        public MetadataTypesConfig setAddIndexesToDataMembers(Boolean value) { this.addIndexesToDataMembers = value; return this; }
        public Boolean isAddGeneratedCodeAttributes() { return addGeneratedCodeAttributes; }
        public MetadataTypesConfig setAddGeneratedCodeAttributes(Boolean value) { this.addGeneratedCodeAttributes = value; return this; }
        public Integer getAddImplicitVersion() { return addImplicitVersion; }
        public MetadataTypesConfig setAddImplicitVersion(Integer value) { this.addImplicitVersion = value; return this; }
        public Boolean isAddResponseStatus() { return addResponseStatus; }
        public MetadataTypesConfig setAddResponseStatus(Boolean value) { this.addResponseStatus = value; return this; }
        public Boolean isAddServiceStackTypes() { return addServiceStackTypes; }
        public MetadataTypesConfig setAddServiceStackTypes(Boolean value) { this.addServiceStackTypes = value; return this; }
        public Boolean isAddModelExtensions() { return addModelExtensions; }
        public MetadataTypesConfig setAddModelExtensions(Boolean value) { this.addModelExtensions = value; return this; }
        public Boolean isAddPropertyAccessors() { return addPropertyAccessors; }
        public MetadataTypesConfig setAddPropertyAccessors(Boolean value) { this.addPropertyAccessors = value; return this; }
        public Boolean isExcludeGenericBaseTypes() { return excludeGenericBaseTypes; }
        public MetadataTypesConfig setExcludeGenericBaseTypes(Boolean value) { this.excludeGenericBaseTypes = value; return this; }
        public Boolean isSettersReturnThis() { return settersReturnThis; }
        public MetadataTypesConfig setSettersReturnThis(Boolean value) { this.settersReturnThis = value; return this; }
        public Boolean isAddNullableAnnotations() { return addNullableAnnotations; }
        public MetadataTypesConfig setAddNullableAnnotations(Boolean value) { this.addNullableAnnotations = value; return this; }
        public Boolean isMakePropertiesOptional() { return makePropertiesOptional; }
        public MetadataTypesConfig setMakePropertiesOptional(Boolean value) { this.makePropertiesOptional = value; return this; }
        public Boolean isExportAsTypes() { return exportAsTypes; }
        public MetadataTypesConfig setExportAsTypes(Boolean value) { this.exportAsTypes = value; return this; }
        public Boolean isExcludeImplementedInterfaces() { return excludeImplementedInterfaces; }
        public MetadataTypesConfig setExcludeImplementedInterfaces(Boolean value) { this.excludeImplementedInterfaces = value; return this; }
        public String getAddDefaultXmlNamespace() { return addDefaultXmlNamespace; }
        public MetadataTypesConfig setAddDefaultXmlNamespace(String value) { this.addDefaultXmlNamespace = value; return this; }
        public Boolean isMakeDataContractsExtensible() { return makeDataContractsExtensible; }
        public MetadataTypesConfig setMakeDataContractsExtensible(Boolean value) { this.makeDataContractsExtensible = value; return this; }
        public Boolean isInitializeCollections() { return initializeCollections; }
        public MetadataTypesConfig setInitializeCollections(Boolean value) { this.initializeCollections = value; return this; }
        public ArrayList<String> getAddNamespaces() { return addNamespaces; }
        public MetadataTypesConfig setAddNamespaces(ArrayList<String> value) { this.addNamespaces = value; return this; }
        public ArrayList<String> getDefaultNamespaces() { return defaultNamespaces; }
        public MetadataTypesConfig setDefaultNamespaces(ArrayList<String> value) { this.defaultNamespaces = value; return this; }
        public ArrayList<String> getDefaultImports() { return defaultImports; }
        public MetadataTypesConfig setDefaultImports(ArrayList<String> value) { this.defaultImports = value; return this; }
        public ArrayList<String> getIncludeTypes() { return includeTypes; }
        public MetadataTypesConfig setIncludeTypes(ArrayList<String> value) { this.includeTypes = value; return this; }
        public ArrayList<String> getExcludeTypes() { return excludeTypes; }
        public MetadataTypesConfig setExcludeTypes(ArrayList<String> value) { this.excludeTypes = value; return this; }
        public ArrayList<String> getExportTags() { return exportTags; }
        public MetadataTypesConfig setExportTags(ArrayList<String> value) { this.exportTags = value; return this; }
        public ArrayList<String> getTreatTypesAsStrings() { return treatTypesAsStrings; }
        public MetadataTypesConfig setTreatTypesAsStrings(ArrayList<String> value) { this.treatTypesAsStrings = value; return this; }
        public Boolean isExportValueTypes() { return exportValueTypes; }
        public MetadataTypesConfig setExportValueTypes(Boolean value) { this.exportValueTypes = value; return this; }
        public String getGlobalNamespace() { return globalNamespace; }
        public MetadataTypesConfig setGlobalNamespace(String value) { this.globalNamespace = value; return this; }
        public Boolean isExcludeNamespace() { return excludeNamespace; }
        public MetadataTypesConfig setExcludeNamespace(Boolean value) { this.excludeNamespace = value; return this; }
        public String getDataClass() { return dataClass; }
        public MetadataTypesConfig setDataClass(String value) { this.dataClass = value; return this; }
        public String getDataClassJson() { return dataClassJson; }
        public MetadataTypesConfig setDataClassJson(String value) { this.dataClassJson = value; return this; }
        public ArrayList<Class> getIgnoreTypes() { return ignoreTypes; }
        public MetadataTypesConfig setIgnoreTypes(ArrayList<Class> value) { this.ignoreTypes = value; return this; }
        public ArrayList<Class> getExportTypes() { return exportTypes; }
        public MetadataTypesConfig setExportTypes(ArrayList<Class> value) { this.exportTypes = value; return this; }
        public ArrayList<Class> getExportAttributes() { return exportAttributes; }
        public MetadataTypesConfig setExportAttributes(ArrayList<Class> value) { this.exportAttributes = value; return this; }
        public ArrayList<String> getIgnoreTypesInNamespaces() { return ignoreTypesInNamespaces; }
        public MetadataTypesConfig setIgnoreTypesInNamespaces(ArrayList<String> value) { this.ignoreTypesInNamespaces = value; return this; }
    }

    public static class MetadataType
    {
        public String name = null;
        public String namespace = null;
        public ArrayList<String> genericArgs = null;
        public MetadataTypeName inherits = null;
        @SerializedName("implements") public ArrayList<MetadataTypeName> Implements = null;
        public String displayType = null;
        public String description = null;
        public String notes = null;
        public ImageInfo icon = null;
        public Boolean isNested = null;
        public Boolean isEnum = null;
        public Boolean isEnumInt = null;
        public Boolean isInterface = null;
        public Boolean isAbstract = null;
        public Boolean isGenericTypeDef = null;
        public MetadataDataContract dataContract = null;
        public ArrayList<MetadataPropertyType> properties = null;
        public ArrayList<MetadataAttribute> attributes = null;
        public ArrayList<MetadataTypeName> innerTypes = null;
        public ArrayList<String> enumNames = null;
        public ArrayList<String> enumValues = null;
        public ArrayList<String> enumMemberValues = null;
        public ArrayList<String> enumDescriptions = null;
        public HashMap<String,String> meta = null;
        
        public String getName() { return name; }
        public MetadataType setName(String value) { this.name = value; return this; }
        public String getNamespace() { return namespace; }
        public MetadataType setNamespace(String value) { this.namespace = value; return this; }
        public ArrayList<String> getGenericArgs() { return genericArgs; }
        public MetadataType setGenericArgs(ArrayList<String> value) { this.genericArgs = value; return this; }
        public MetadataTypeName getInherits() { return inherits; }
        public MetadataType setInherits(MetadataTypeName value) { this.inherits = value; return this; }
        public ArrayList<MetadataTypeName> getImplements() { return Implements; }
        public MetadataType setImplements(ArrayList<MetadataTypeName> value) { this.Implements = value; return this; }
        public String getDisplayType() { return displayType; }
        public MetadataType setDisplayType(String value) { this.displayType = value; return this; }
        public String getDescription() { return description; }
        public MetadataType setDescription(String value) { this.description = value; return this; }
        public String getNotes() { return notes; }
        public MetadataType setNotes(String value) { this.notes = value; return this; }
        public ImageInfo getIcon() { return icon; }
        public MetadataType setIcon(ImageInfo value) { this.icon = value; return this; }
        public Boolean getIsNested() { return isNested; }
        public MetadataType setIsNested(Boolean value) { this.isNested = value; return this; }
        public Boolean getIsEnum() { return isEnum; }
        public MetadataType setIsEnum(Boolean value) { this.isEnum = value; return this; }
        public Boolean getIsEnumInt() { return isEnumInt; }
        public MetadataType setIsEnumInt(Boolean value) { this.isEnumInt = value; return this; }
        public Boolean getIsInterface() { return isInterface; }
        public MetadataType setIsInterface(Boolean value) { this.isInterface = value; return this; }
        public Boolean getIsAbstract() { return isAbstract; }
        public MetadataType setIsAbstract(Boolean value) { this.isAbstract = value; return this; }
        public Boolean getIsGenericTypeDef() { return isGenericTypeDef; }
        public MetadataType setIsGenericTypeDef(Boolean value) { this.isGenericTypeDef = value; return this; }
        public MetadataDataContract getDataContract() { return dataContract; }
        public MetadataType setDataContract(MetadataDataContract value) { this.dataContract = value; return this; }
        public ArrayList<MetadataPropertyType> getProperties() { return properties; }
        public MetadataType setProperties(ArrayList<MetadataPropertyType> value) { this.properties = value; return this; }
        public ArrayList<MetadataAttribute> getAttributes() { return attributes; }
        public MetadataType setAttributes(ArrayList<MetadataAttribute> value) { this.attributes = value; return this; }
        public ArrayList<MetadataTypeName> getInnerTypes() { return innerTypes; }
        public MetadataType setInnerTypes(ArrayList<MetadataTypeName> value) { this.innerTypes = value; return this; }
        public ArrayList<String> getEnumNames() { return enumNames; }
        public MetadataType setEnumNames(ArrayList<String> value) { this.enumNames = value; return this; }
        public ArrayList<String> getEnumValues() { return enumValues; }
        public MetadataType setEnumValues(ArrayList<String> value) { this.enumValues = value; return this; }
        public ArrayList<String> getEnumMemberValues() { return enumMemberValues; }
        public MetadataType setEnumMemberValues(ArrayList<String> value) { this.enumMemberValues = value; return this; }
        public ArrayList<String> getEnumDescriptions() { return enumDescriptions; }
        public MetadataType setEnumDescriptions(ArrayList<String> value) { this.enumDescriptions = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public MetadataType setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class MetadataOperationType
    {
        public MetadataType request = null;
        public MetadataType response = null;
        public ArrayList<String> actions = null;
        public Boolean returnsVoid = null;
        public String method = null;
        public MetadataTypeName returnType = null;
        public ArrayList<MetadataRoute> routes = null;
        public MetadataTypeName dataModel = null;
        public MetadataTypeName viewModel = null;
        public Boolean requiresAuth = null;
        public Boolean requiresApiKey = null;
        public ArrayList<String> requiredRoles = null;
        public ArrayList<String> requiresAnyRole = null;
        public ArrayList<String> requiredPermissions = null;
        public ArrayList<String> requiresAnyPermission = null;
        public ArrayList<String> tags = null;
        public ApiUiInfo ui = null;
        
        public MetadataType getRequest() { return request; }
        public MetadataOperationType setRequest(MetadataType value) { this.request = value; return this; }
        public MetadataType getResponse() { return response; }
        public MetadataOperationType setResponse(MetadataType value) { this.response = value; return this; }
        public ArrayList<String> getActions() { return actions; }
        public MetadataOperationType setActions(ArrayList<String> value) { this.actions = value; return this; }
        public Boolean isReturnsVoid() { return returnsVoid; }
        public MetadataOperationType setReturnsVoid(Boolean value) { this.returnsVoid = value; return this; }
        public String getMethod() { return method; }
        public MetadataOperationType setMethod(String value) { this.method = value; return this; }
        public MetadataTypeName getReturnType() { return returnType; }
        public MetadataOperationType setReturnType(MetadataTypeName value) { this.returnType = value; return this; }
        public ArrayList<MetadataRoute> getRoutes() { return routes; }
        public MetadataOperationType setRoutes(ArrayList<MetadataRoute> value) { this.routes = value; return this; }
        public MetadataTypeName getDataModel() { return dataModel; }
        public MetadataOperationType setDataModel(MetadataTypeName value) { this.dataModel = value; return this; }
        public MetadataTypeName getViewModel() { return viewModel; }
        public MetadataOperationType setViewModel(MetadataTypeName value) { this.viewModel = value; return this; }
        public Boolean isRequiresAuth() { return requiresAuth; }
        public MetadataOperationType setRequiresAuth(Boolean value) { this.requiresAuth = value; return this; }
        public Boolean isRequiresApiKey() { return requiresApiKey; }
        public MetadataOperationType setRequiresApiKey(Boolean value) { this.requiresApiKey = value; return this; }
        public ArrayList<String> getRequiredRoles() { return requiredRoles; }
        public MetadataOperationType setRequiredRoles(ArrayList<String> value) { this.requiredRoles = value; return this; }
        public ArrayList<String> getRequiresAnyRole() { return requiresAnyRole; }
        public MetadataOperationType setRequiresAnyRole(ArrayList<String> value) { this.requiresAnyRole = value; return this; }
        public ArrayList<String> getRequiredPermissions() { return requiredPermissions; }
        public MetadataOperationType setRequiredPermissions(ArrayList<String> value) { this.requiredPermissions = value; return this; }
        public ArrayList<String> getRequiresAnyPermission() { return requiresAnyPermission; }
        public MetadataOperationType setRequiresAnyPermission(ArrayList<String> value) { this.requiresAnyPermission = value; return this; }
        public ArrayList<String> getTags() { return tags; }
        public MetadataOperationType setTags(ArrayList<String> value) { this.tags = value; return this; }
        public ApiUiInfo getUi() { return ui; }
        public MetadataOperationType setUi(ApiUiInfo value) { this.ui = value; return this; }
    }

    public static class MetadataDataMember
    {
        public String name = null;
        public Integer order = null;
        public Boolean isRequired = null;
        public Boolean emitDefaultValue = null;
        
        public String getName() { return name; }
        public MetadataDataMember setName(String value) { this.name = value; return this; }
        public Integer getOrder() { return order; }
        public MetadataDataMember setOrder(Integer value) { this.order = value; return this; }
        public Boolean getIsRequired() { return isRequired; }
        public MetadataDataMember setIsRequired(Boolean value) { this.isRequired = value; return this; }
        public Boolean isEmitDefaultValue() { return emitDefaultValue; }
        public MetadataDataMember setEmitDefaultValue(Boolean value) { this.emitDefaultValue = value; return this; }
    }

    public static class MetadataAttribute
    {
        public String name = null;
        public ArrayList<MetadataPropertyType> constructorArgs = null;
        public ArrayList<MetadataPropertyType> args = null;
        
        public String getName() { return name; }
        public MetadataAttribute setName(String value) { this.name = value; return this; }
        public ArrayList<MetadataPropertyType> getConstructorArgs() { return constructorArgs; }
        public MetadataAttribute setConstructorArgs(ArrayList<MetadataPropertyType> value) { this.constructorArgs = value; return this; }
        public ArrayList<MetadataPropertyType> getArgs() { return args; }
        public MetadataAttribute setArgs(ArrayList<MetadataPropertyType> value) { this.args = value; return this; }
    }

    public static class InputInfo
    {
        public String id = null;
        public String name = null;
        public String type = null;
        public String value = null;
        public String placeholder = null;
        public String help = null;
        public String label = null;
        public String title = null;
        public String size = null;
        public String pattern = null;
        public Boolean readOnly = null;
        public Boolean required = null;
        public Boolean disabled = null;
        public String autocomplete = null;
        public String autofocus = null;
        public String min = null;
        public String max = null;
        public String step = null;
        public Integer minLength = null;
        public Integer maxLength = null;
        public String accept = null;
        public String capture = null;
        public Boolean multiple = null;
        public ArrayList<String> allowableValues = null;
        public ArrayList<KeyValuePair<String, String>> allowableEntries = null;
        public String options = null;
        public Boolean ignore = null;
        public FieldCss css = null;
        public HashMap<String,String> meta = null;
        
        public String getId() { return id; }
        public InputInfo setId(String value) { this.id = value; return this; }
        public String getName() { return name; }
        public InputInfo setName(String value) { this.name = value; return this; }
        public String getType() { return type; }
        public InputInfo setType(String value) { this.type = value; return this; }
        public String getValue() { return value; }
        public InputInfo setValue(String value) { this.value = value; return this; }
        public String getPlaceholder() { return placeholder; }
        public InputInfo setPlaceholder(String value) { this.placeholder = value; return this; }
        public String getHelp() { return help; }
        public InputInfo setHelp(String value) { this.help = value; return this; }
        public String getLabel() { return label; }
        public InputInfo setLabel(String value) { this.label = value; return this; }
        public String getTitle() { return title; }
        public InputInfo setTitle(String value) { this.title = value; return this; }
        public String getSize() { return size; }
        public InputInfo setSize(String value) { this.size = value; return this; }
        public String getPattern() { return pattern; }
        public InputInfo setPattern(String value) { this.pattern = value; return this; }
        public Boolean isReadOnly() { return readOnly; }
        public InputInfo setReadOnly(Boolean value) { this.readOnly = value; return this; }
        public Boolean isRequired() { return required; }
        public InputInfo setRequired(Boolean value) { this.required = value; return this; }
        public Boolean isDisabled() { return disabled; }
        public InputInfo setDisabled(Boolean value) { this.disabled = value; return this; }
        public String getAutocomplete() { return autocomplete; }
        public InputInfo setAutocomplete(String value) { this.autocomplete = value; return this; }
        public String getAutofocus() { return autofocus; }
        public InputInfo setAutofocus(String value) { this.autofocus = value; return this; }
        public String getMin() { return min; }
        public InputInfo setMin(String value) { this.min = value; return this; }
        public String getMax() { return max; }
        public InputInfo setMax(String value) { this.max = value; return this; }
        public String getStep() { return step; }
        public InputInfo setStep(String value) { this.step = value; return this; }
        public Integer getMinLength() { return minLength; }
        public InputInfo setMinLength(Integer value) { this.minLength = value; return this; }
        public Integer getMaxLength() { return maxLength; }
        public InputInfo setMaxLength(Integer value) { this.maxLength = value; return this; }
        public String getAccept() { return accept; }
        public InputInfo setAccept(String value) { this.accept = value; return this; }
        public String getCapture() { return capture; }
        public InputInfo setCapture(String value) { this.capture = value; return this; }
        public Boolean isMultiple() { return multiple; }
        public InputInfo setMultiple(Boolean value) { this.multiple = value; return this; }
        public ArrayList<String> getAllowableValues() { return allowableValues; }
        public InputInfo setAllowableValues(ArrayList<String> value) { this.allowableValues = value; return this; }
        public ArrayList<KeyValuePair<String, String>> getAllowableEntries() { return allowableEntries; }
        public InputInfo setAllowableEntries(ArrayList<KeyValuePair<String, String>> value) { this.allowableEntries = value; return this; }
        public String getOptions() { return options; }
        public InputInfo setOptions(String value) { this.options = value; return this; }
        public Boolean isIgnore() { return ignore; }
        public InputInfo setIgnore(Boolean value) { this.ignore = value; return this; }
        public FieldCss getCss() { return css; }
        public InputInfo setCss(FieldCss value) { this.css = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public InputInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class FormatInfo
    {
        public String method = null;
        public String options = null;
        public String locale = null;
        
        public String getMethod() { return method; }
        public FormatInfo setMethod(String value) { this.method = value; return this; }
        public String getOptions() { return options; }
        public FormatInfo setOptions(String value) { this.options = value; return this; }
        public String getLocale() { return locale; }
        public FormatInfo setLocale(String value) { this.locale = value; return this; }
    }

    public static class RefInfo
    {
        public String model = null;
        public String selfId = null;
        public String refId = null;
        public String refLabel = null;
        public String queryApi = null;
        
        public String getModel() { return model; }
        public RefInfo setModel(String value) { this.model = value; return this; }
        public String getSelfId() { return selfId; }
        public RefInfo setSelfId(String value) { this.selfId = value; return this; }
        public String getRefId() { return refId; }
        public RefInfo setRefId(String value) { this.refId = value; return this; }
        public String getRefLabel() { return refLabel; }
        public RefInfo setRefLabel(String value) { this.refLabel = value; return this; }
        public String getQueryApi() { return queryApi; }
        public RefInfo setQueryApi(String value) { this.queryApi = value; return this; }
    }

    @DataContract
    public static class RequestSummary
    {
        @DataMember(Order=1)
        public String name = null;

        @DataMember(Order=2)
        public Long totalRequests = null;

        @DataMember(Order=3)
        public Long totalRequestLength = null;

        @DataMember(Order=4)
        public Long minRequestLength = null;

        @DataMember(Order=5)
        public Long maxRequestLength = null;

        @DataMember(Order=6)
        public Double totalDuration = null;

        @DataMember(Order=7)
        public Double minDuration = null;

        @DataMember(Order=8)
        public Double maxDuration = null;

        @DataMember(Order=9)
        public HashMap<Integer,Long> status = null;

        @DataMember(Order=10)
        public HashMap<String,Long> durations = null;

        @DataMember(Order=11)
        public HashMap<String,Long> apis = null;

        @DataMember(Order=12)
        public HashMap<String,Long> users = null;

        @DataMember(Order=13)
        public HashMap<String,Long> ips = null;

        @DataMember(Order=14)
        public HashMap<String,Long> apiKeys = null;
        
        public String getName() { return name; }
        public RequestSummary setName(String value) { this.name = value; return this; }
        public Long getTotalRequests() { return totalRequests; }
        public RequestSummary setTotalRequests(Long value) { this.totalRequests = value; return this; }
        public Long getTotalRequestLength() { return totalRequestLength; }
        public RequestSummary setTotalRequestLength(Long value) { this.totalRequestLength = value; return this; }
        public Long getMinRequestLength() { return minRequestLength; }
        public RequestSummary setMinRequestLength(Long value) { this.minRequestLength = value; return this; }
        public Long getMaxRequestLength() { return maxRequestLength; }
        public RequestSummary setMaxRequestLength(Long value) { this.maxRequestLength = value; return this; }
        public Double getTotalDuration() { return totalDuration; }
        public RequestSummary setTotalDuration(Double value) { this.totalDuration = value; return this; }
        public Double getMinDuration() { return minDuration; }
        public RequestSummary setMinDuration(Double value) { this.minDuration = value; return this; }
        public Double getMaxDuration() { return maxDuration; }
        public RequestSummary setMaxDuration(Double value) { this.maxDuration = value; return this; }
        public HashMap<Integer,Long> getStatus() { return status; }
        public RequestSummary setStatus(HashMap<Integer,Long> value) { this.status = value; return this; }
        public HashMap<String,Long> getDurations() { return durations; }
        public RequestSummary setDurations(HashMap<String,Long> value) { this.durations = value; return this; }
        public HashMap<String,Long> getApis() { return apis; }
        public RequestSummary setApis(HashMap<String,Long> value) { this.apis = value; return this; }
        public HashMap<String,Long> getUsers() { return users; }
        public RequestSummary setUsers(HashMap<String,Long> value) { this.users = value; return this; }
        public HashMap<String,Long> getIps() { return ips; }
        public RequestSummary setIps(HashMap<String,Long> value) { this.ips = value; return this; }
        public HashMap<String,Long> getApiKeys() { return apiKeys; }
        public RequestSummary setApiKeys(HashMap<String,Long> value) { this.apiKeys = value; return this; }
    }

    public static class ApiCss
    {
        public String form = null;
        public String fieldset = null;
        public String field = null;
        
        public String getForm() { return form; }
        public ApiCss setForm(String value) { this.form = value; return this; }
        public String getFieldset() { return fieldset; }
        public ApiCss setFieldset(String value) { this.fieldset = value; return this; }
        public String getField() { return field; }
        public ApiCss setField(String value) { this.field = value; return this; }
    }

    public static class AppTags
    {
        @SerializedName("default") public String Default = null;
        public String other = null;
        
        public String getDefault() { return Default; }
        public AppTags setDefault(String value) { this.Default = value; return this; }
        public String getOther() { return other; }
        public AppTags setOther(String value) { this.other = value; return this; }
    }

    public static class MetaAuthProvider
    {
        public String name = null;
        public String label = null;
        public String type = null;
        public NavItem navItem = null;
        public ImageInfo icon = null;
        public ArrayList<InputInfo> formLayout = null;
        public HashMap<String,String> meta = null;
        
        public String getName() { return name; }
        public MetaAuthProvider setName(String value) { this.name = value; return this; }
        public String getLabel() { return label; }
        public MetaAuthProvider setLabel(String value) { this.label = value; return this; }
        public String getType() { return type; }
        public MetaAuthProvider setType(String value) { this.type = value; return this; }
        public NavItem getNavItem() { return navItem; }
        public MetaAuthProvider setNavItem(NavItem value) { this.navItem = value; return this; }
        public ImageInfo getIcon() { return icon; }
        public MetaAuthProvider setIcon(ImageInfo value) { this.icon = value; return this; }
        public ArrayList<InputInfo> getFormLayout() { return formLayout; }
        public MetaAuthProvider setFormLayout(ArrayList<InputInfo> value) { this.formLayout = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public MetaAuthProvider setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class IdentityAuthInfo
    {
        public Boolean hasRefreshToken = null;
        public HashMap<String,String> meta = null;
        
        public Boolean isHasRefreshToken() { return hasRefreshToken; }
        public IdentityAuthInfo setHasRefreshToken(Boolean value) { this.hasRefreshToken = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public IdentityAuthInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class KeyValuePair<TKey, TValue>
    {
        public TKey key = null;
        public TValue value = null;
        
        public TKey getKey() { return key; }
        public KeyValuePair<TKey, TValue> setKey(TKey value) { this.key = value; return this; }
        public TValue getValue() { return value; }
        public KeyValuePair<TKey, TValue> setValue(TValue value) { this.value = value; return this; }
    }

    public static class CommandInfo
    {
        public String name = null;
        public String tag = null;
        public MetadataType request = null;
        public MetadataType response = null;
        
        public String getName() { return name; }
        public CommandInfo setName(String value) { this.name = value; return this; }
        public String getTag() { return tag; }
        public CommandInfo setTag(String value) { this.tag = value; return this; }
        public MetadataType getRequest() { return request; }
        public CommandInfo setRequest(MetadataType value) { this.request = value; return this; }
        public MetadataType getResponse() { return response; }
        public CommandInfo setResponse(MetadataType value) { this.response = value; return this; }
    }

    public static class AutoQueryConvention
    {
        public String name = null;
        public String value = null;
        public String types = null;
        public String valueType = null;
        
        public String getName() { return name; }
        public AutoQueryConvention setName(String value) { this.name = value; return this; }
        public String getValue() { return value; }
        public AutoQueryConvention setValue(String value) { this.value = value; return this; }
        public String getTypes() { return types; }
        public AutoQueryConvention setTypes(String value) { this.types = value; return this; }
        public String getValueType() { return valueType; }
        public AutoQueryConvention setValueType(String value) { this.valueType = value; return this; }
    }

    public static class ScriptMethodType
    {
        public String name = null;
        public ArrayList<String> paramNames = null;
        public ArrayList<String> paramTypes = null;
        public String returnType = null;
        
        public String getName() { return name; }
        public ScriptMethodType setName(String value) { this.name = value; return this; }
        public ArrayList<String> getParamNames() { return paramNames; }
        public ScriptMethodType setParamNames(ArrayList<String> value) { this.paramNames = value; return this; }
        public ArrayList<String> getParamTypes() { return paramTypes; }
        public ScriptMethodType setParamTypes(ArrayList<String> value) { this.paramTypes = value; return this; }
        public String getReturnType() { return returnType; }
        public ScriptMethodType setReturnType(String value) { this.returnType = value; return this; }
    }

    public static class RequestLogsAnalytics
    {
        public ArrayList<String> months = null;
        public HashMap<String,String> tabs = null;
        public Boolean disableAnalytics = null;
        public Boolean disableUserAnalytics = null;
        public Boolean disableApiKeyAnalytics = null;
        
        public ArrayList<String> getMonths() { return months; }
        public RequestLogsAnalytics setMonths(ArrayList<String> value) { this.months = value; return this; }
        public HashMap<String,String> getTabs() { return tabs; }
        public RequestLogsAnalytics setTabs(HashMap<String,String> value) { this.tabs = value; return this; }
        public Boolean isDisableAnalytics() { return disableAnalytics; }
        public RequestLogsAnalytics setDisableAnalytics(Boolean value) { this.disableAnalytics = value; return this; }
        public Boolean isDisableUserAnalytics() { return disableUserAnalytics; }
        public RequestLogsAnalytics setDisableUserAnalytics(Boolean value) { this.disableUserAnalytics = value; return this; }
        public Boolean isDisableApiKeyAnalytics() { return disableApiKeyAnalytics; }
        public RequestLogsAnalytics setDisableApiKeyAnalytics(Boolean value) { this.disableApiKeyAnalytics = value; return this; }
    }

    public static class FilesUploadLocation
    {
        public String name = null;
        public String readAccessRole = null;
        public String writeAccessRole = null;
        public ArrayList<String> allowExtensions = null;
        public String allowOperations = null;
        public Integer maxFileCount = null;
        public Long minFileBytes = null;
        public Long maxFileBytes = null;
        
        public String getName() { return name; }
        public FilesUploadLocation setName(String value) { this.name = value; return this; }
        public String getReadAccessRole() { return readAccessRole; }
        public FilesUploadLocation setReadAccessRole(String value) { this.readAccessRole = value; return this; }
        public String getWriteAccessRole() { return writeAccessRole; }
        public FilesUploadLocation setWriteAccessRole(String value) { this.writeAccessRole = value; return this; }
        public ArrayList<String> getAllowExtensions() { return allowExtensions; }
        public FilesUploadLocation setAllowExtensions(ArrayList<String> value) { this.allowExtensions = value; return this; }
        public String getAllowOperations() { return allowOperations; }
        public FilesUploadLocation setAllowOperations(String value) { this.allowOperations = value; return this; }
        public Integer getMaxFileCount() { return maxFileCount; }
        public FilesUploadLocation setMaxFileCount(Integer value) { this.maxFileCount = value; return this; }
        public Long getMinFileBytes() { return minFileBytes; }
        public FilesUploadLocation setMinFileBytes(Long value) { this.minFileBytes = value; return this; }
        public Long getMaxFileBytes() { return maxFileBytes; }
        public FilesUploadLocation setMaxFileBytes(Long value) { this.maxFileBytes = value; return this; }
    }

    public static class MediaRule
    {
        public String size = null;
        public String rule = null;
        public ArrayList<String> applyTo = null;
        public HashMap<String,String> meta = null;
        
        public String getSize() { return size; }
        public MediaRule setSize(String value) { this.size = value; return this; }
        public String getRule() { return rule; }
        public MediaRule setRule(String value) { this.rule = value; return this; }
        public ArrayList<String> getApplyTo() { return applyTo; }
        public MediaRule setApplyTo(ArrayList<String> value) { this.applyTo = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public MediaRule setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class DatabaseInfo
    {
        public String alias = null;
        public String name = null;
        public ArrayList<SchemaInfo> schemas = null;
        
        public String getAlias() { return alias; }
        public DatabaseInfo setAlias(String value) { this.alias = value; return this; }
        public String getName() { return name; }
        public DatabaseInfo setName(String value) { this.name = value; return this; }
        public ArrayList<SchemaInfo> getSchemas() { return schemas; }
        public DatabaseInfo setSchemas(ArrayList<SchemaInfo> value) { this.schemas = value; return this; }
    }

    public static class MetadataTypeName
    {
        public String name = null;
        public String namespace = null;
        public ArrayList<String> genericArgs = null;
        
        public String getName() { return name; }
        public MetadataTypeName setName(String value) { this.name = value; return this; }
        public String getNamespace() { return namespace; }
        public MetadataTypeName setNamespace(String value) { this.namespace = value; return this; }
        public ArrayList<String> getGenericArgs() { return genericArgs; }
        public MetadataTypeName setGenericArgs(ArrayList<String> value) { this.genericArgs = value; return this; }
    }

    public static class MetadataDataContract
    {
        public String name = null;
        public String namespace = null;
        
        public String getName() { return name; }
        public MetadataDataContract setName(String value) { this.name = value; return this; }
        public String getNamespace() { return namespace; }
        public MetadataDataContract setNamespace(String value) { this.namespace = value; return this; }
    }

    public static class MetadataRoute
    {
        public String path = null;
        public String verbs = null;
        public String notes = null;
        public String summary = null;
        
        public String getPath() { return path; }
        public MetadataRoute setPath(String value) { this.path = value; return this; }
        public String getVerbs() { return verbs; }
        public MetadataRoute setVerbs(String value) { this.verbs = value; return this; }
        public String getNotes() { return notes; }
        public MetadataRoute setNotes(String value) { this.notes = value; return this; }
        public String getSummary() { return summary; }
        public MetadataRoute setSummary(String value) { this.summary = value; return this; }
    }

    public static class ApiUiInfo
    {
        public ApiCss locodeCss = null;
        public ApiCss explorerCss = null;
        public ArrayList<InputInfo> formLayout = null;
        public HashMap<String,String> meta = null;
        
        public ApiCss getLocodeCss() { return locodeCss; }
        public ApiUiInfo setLocodeCss(ApiCss value) { this.locodeCss = value; return this; }
        public ApiCss getExplorerCss() { return explorerCss; }
        public ApiUiInfo setExplorerCss(ApiCss value) { this.explorerCss = value; return this; }
        public ArrayList<InputInfo> getFormLayout() { return formLayout; }
        public ApiUiInfo setFormLayout(ArrayList<InputInfo> value) { this.formLayout = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public ApiUiInfo setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class FieldCss
    {
        public String field = null;
        public String input = null;
        public String label = null;
        
        public String getField() { return field; }
        public FieldCss setField(String value) { this.field = value; return this; }
        public String getInput() { return input; }
        public FieldCss setInput(String value) { this.input = value; return this; }
        public String getLabel() { return label; }
        public FieldCss setLabel(String value) { this.label = value; return this; }
    }

    public static class NavItem
    {
        public String label = null;
        public String href = null;
        public Boolean exact = null;
        public String id = null;
        public String className = null;
        public String iconClass = null;
        public String iconSrc = null;
        public String show = null;
        public String hide = null;
        public ArrayList<NavItem> children = null;
        public HashMap<String,String> meta = null;
        
        public String getLabel() { return label; }
        public NavItem setLabel(String value) { this.label = value; return this; }
        public String getHref() { return href; }
        public NavItem setHref(String value) { this.href = value; return this; }
        public Boolean isExact() { return exact; }
        public NavItem setExact(Boolean value) { this.exact = value; return this; }
        public String getId() { return id; }
        public NavItem setId(String value) { this.id = value; return this; }
        public String getClassName() { return className; }
        public NavItem setClassName(String value) { this.className = value; return this; }
        public String getIconClass() { return iconClass; }
        public NavItem setIconClass(String value) { this.iconClass = value; return this; }
        public String getIconSrc() { return iconSrc; }
        public NavItem setIconSrc(String value) { this.iconSrc = value; return this; }
        public String getShow() { return show; }
        public NavItem setShow(String value) { this.show = value; return this; }
        public String getHide() { return hide; }
        public NavItem setHide(String value) { this.hide = value; return this; }
        public ArrayList<NavItem> getChildren() { return children; }
        public NavItem setChildren(ArrayList<NavItem> value) { this.children = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public NavItem setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    public static class SchemaInfo
    {
        public String alias = null;
        public String name = null;
        public ArrayList<String> tables = null;
        
        public String getAlias() { return alias; }
        public SchemaInfo setAlias(String value) { this.alias = value; return this; }
        public String getName() { return name; }
        public SchemaInfo setName(String value) { this.name = value; return this; }
        public ArrayList<String> getTables() { return tables; }
        public SchemaInfo setTables(ArrayList<String> value) { this.tables = value; return this; }
    }

}
