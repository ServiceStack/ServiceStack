/* Options:
Date: 2024-01-27 18:37:48
Version: 8.01
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: https://localhost:5001

//AddServiceStackTypes: True
//AddDocAnnotations: True
//AddDescriptionAsComments: True
IncludeTypes: GetAccessToken
//ExcludeTypes: 
//DefaultImports: 
*/

"use strict";
export class AdminUserBase {
    /** @param {{userName?:string,firstName?:string,lastName?:string,displayName?:string,email?:string,password?:string,profileUrl?:string,phoneNumber?:string,userAuthProperties?:{ [index: string]: string; },meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userName;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    displayName;
    /** @type {string} */
    email;
    /** @type {string} */
    password;
    /** @type {string} */
    profileUrl;
    /** @type {string} */
    phoneNumber;
    /** @type {{ [index: string]: string; }} */
    userAuthProperties;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class ValidateRule {
    /** @param {{validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    validator;
    /** @type {string} */
    condition;
    /** @type {string} */
    errorCode;
    /** @type {string} */
    message;
}
export class ValidationRule extends ValidateRule {
    /** @param {{id?:number,type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    type;
    /** @type {string} */
    field;
    /** @type {string} */
    createdBy;
    /** @type {?string} */
    createdDate;
    /** @type {string} */
    modifiedBy;
    /** @type {?string} */
    modifiedDate;
    /** @type {string} */
    suspendedBy;
    /** @type {?string} */
    suspendedDate;
    /** @type {string} */
    notes;
}
export class AdminDashboardResponse {
    /** @param {{serverStats?:ServerStats,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ServerStats} */
    serverStats;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AdminUsersResponse {
    /** @param {{results?:{ [index:string]: Object; }[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {{ [index:string]: Object; }[]} */
    results;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AdminUserResponse {
    /** @param {{id?:string,result?:{ [index: string]: Object; },details?:{ [index:string]: Object; }[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {{ [index: string]: Object; }} */
    result;
    /** @type {{ [index:string]: Object; }[]} */
    details;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AdminDeleteUserResponse {
    /** @param {{id?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AuthenticateResponse {
    /** @param {{userId?:string,sessionId?:string,userName?:string,displayName?:string,referrerUrl?:string,bearerToken?:string,refreshToken?:string,refreshTokenExpiry?:string,profileUrl?:string,roles?:string[],permissions?:string[],responseStatus?:ResponseStatus,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userId;
    /** @type {string} */
    sessionId;
    /** @type {string} */
    userName;
    /** @type {string} */
    displayName;
    /** @type {string} */
    referrerUrl;
    /** @type {string} */
    bearerToken;
    /** @type {string} */
    refreshToken;
    /** @type {?string} */
    refreshTokenExpiry;
    /** @type {string} */
    profileUrl;
    /** @type {string[]} */
    roles;
    /** @type {string[]} */
    permissions;
    /** @type {ResponseStatus} */
    responseStatus;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class GetAccessTokenResponse {
    /** @param {{accessToken?:string,meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    accessToken;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AdminDatabaseResponse {
    /** @param {{results?:{ [index:string]: Object; }[],total?:number,columns?:MetadataPropertyType[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {{ [index:string]: Object; }[]} */
    results;
    /** @type {?number} */
    total;
    /** @type {?MetadataPropertyType[]} */
    columns;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AdminProfilingResponse {
    /** @param {{results?:DiagnosticEntry[],total?:number,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {DiagnosticEntry[]} */
    results;
    /** @type {number} */
    total;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AdminRedisResponse {
    /** @param {{db?:number,searchResults?:RedisSearchResult[],info?:{ [index: string]: string; },endpoint?:RedisEndpointInfo,result?:RedisText,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    db;
    /** @type {?RedisSearchResult[]} */
    searchResults;
    /** @type {?{ [index: string]: string; }} */
    info;
    /** @type {?RedisEndpointInfo} */
    endpoint;
    /** @type {?RedisText} */
    result;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class RequestLogsResponse {
    /** @param {{results?:RequestLogEntry[],usage?:{ [index: string]: string; },total?:number,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {RequestLogEntry[]} */
    results;
    /** @type {{ [index: string]: string; }} */
    usage;
    /** @type {number} */
    total;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class GetValidationRulesResponse {
    /** @param {{results?:ValidationRule[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ValidationRule[]} */
    results;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AdminDashboard {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'AdminDashboard' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminDashboardResponse() }
}
export class AdminQueryUsers {
    /** @param {{query?:string,orderBy?:string,skip?:number,take?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    query;
    /** @type {string} */
    orderBy;
    /** @type {?number} */
    skip;
    /** @type {?number} */
    take;
    getTypeName() { return 'AdminQueryUsers' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminUsersResponse() }
}
export class AdminGetUser {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'AdminGetUser' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminUserResponse() }
}
export class AdminCreateUser extends AdminUserBase {
    /** @param {{roles?:string[],permissions?:string[],userName?:string,firstName?:string,lastName?:string,displayName?:string,email?:string,password?:string,profileUrl?:string,phoneNumber?:string,userAuthProperties?:{ [index: string]: string; },meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string[]} */
    roles;
    /** @type {string[]} */
    permissions;
    getTypeName() { return 'AdminCreateUser' }
    getMethod() { return 'POST' }
    createResponse() { return new AdminUserResponse() }
}
export class AdminUpdateUser extends AdminUserBase {
    /** @param {{id?:string,lockUser?:boolean,unlockUser?:boolean,addRoles?:string[],removeRoles?:string[],addPermissions?:string[],removePermissions?:string[],userName?:string,firstName?:string,lastName?:string,displayName?:string,email?:string,password?:string,profileUrl?:string,phoneNumber?:string,userAuthProperties?:{ [index: string]: string; },meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {?boolean} */
    lockUser;
    /** @type {?boolean} */
    unlockUser;
    /** @type {string[]} */
    addRoles;
    /** @type {string[]} */
    removeRoles;
    /** @type {string[]} */
    addPermissions;
    /** @type {string[]} */
    removePermissions;
    getTypeName() { return 'AdminUpdateUser' }
    getMethod() { return 'PUT' }
    createResponse() { return new AdminUserResponse() }
}
export class AdminDeleteUser {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'AdminDeleteUser' }
    getMethod() { return 'DELETE' }
    createResponse() { return new AdminDeleteUserResponse() }
}
export class Authenticate {
    /** @param {{provider?:string,userName?:string,password?:string,rememberMe?:boolean,accessToken?:string,accessTokenSecret?:string,returnUrl?:string,errorView?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description AuthProvider, e.g. credentials */
    provider;
    /** @type {string} */
    userName;
    /** @type {string} */
    password;
    /** @type {?boolean} */
    rememberMe;
    /** @type {string} */
    accessToken;
    /** @type {string} */
    accessTokenSecret;
    /** @type {string} */
    returnUrl;
    /** @type {string} */
    errorView;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'Authenticate' }
    getMethod() { return 'POST' }
    createResponse() { return new AuthenticateResponse() }
}
export class GetAccessToken {
    /** @param {{refreshToken?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    refreshToken;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'GetAccessToken' }
    getMethod() { return 'POST' }
    createResponse() { return new GetAccessTokenResponse() }
}
export class AdminDatabase {
    /** @param {{db?:string,schema?:string,table?:string,fields?:string[],take?:number,skip?:number,orderBy?:string,include?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    db;
    /** @type {?string} */
    schema;
    /** @type {?string} */
    table;
    /** @type {?string[]} */
    fields;
    /** @type {?number} */
    take;
    /** @type {?number} */
    skip;
    /** @type {?string} */
    orderBy;
    /** @type {?string} */
    include;
    getTypeName() { return 'AdminDatabase' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminDatabaseResponse() }
}
export class AdminProfiling {
    /** @param {{source?:string,eventType?:string,threadId?:number,traceId?:string,userAuthId?:string,sessionId?:string,tag?:string,skip?:number,take?:number,orderBy?:string,withErrors?:boolean,pending?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    source;
    /** @type {?string} */
    eventType;
    /** @type {?number} */
    threadId;
    /** @type {?string} */
    traceId;
    /** @type {?string} */
    userAuthId;
    /** @type {?string} */
    sessionId;
    /** @type {?string} */
    tag;
    /** @type {number} */
    skip;
    /** @type {?number} */
    take;
    /** @type {?string} */
    orderBy;
    /** @type {?boolean} */
    withErrors;
    /** @type {?boolean} */
    pending;
    getTypeName() { return 'AdminProfiling' }
    getMethod() { return 'POST' }
    createResponse() { return new AdminProfilingResponse() }
}
export class AdminRedis {
    /** @param {{db?:number,query?:string,reconnect?:RedisEndpointInfo,take?:number,position?:number,args?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    db;
    /** @type {?string} */
    query;
    /** @type {?RedisEndpointInfo} */
    reconnect;
    /** @type {?number} */
    take;
    /** @type {?number} */
    position;
    /** @type {?string[]} */
    args;
    getTypeName() { return 'AdminRedis' }
    getMethod() { return 'POST' }
    createResponse() { return new AdminRedisResponse() }
}
export class RequestLogs {
    /** @param {{beforeSecs?:number,afterSecs?:number,operationName?:string,ipAddress?:string,forwardedFor?:string,userAuthId?:string,sessionId?:string,referer?:string,pathInfo?:string,ids?:number[],beforeId?:number,afterId?:number,hasResponse?:boolean,withErrors?:boolean,enableSessionTracking?:boolean,enableResponseTracking?:boolean,enableErrorTracking?:boolean,durationLongerThan?:string,durationLessThan?:string,skip?:number,take?:number,orderBy?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    beforeSecs;
    /** @type {?number} */
    afterSecs;
    /** @type {string} */
    operationName;
    /** @type {string} */
    ipAddress;
    /** @type {string} */
    forwardedFor;
    /** @type {string} */
    userAuthId;
    /** @type {string} */
    sessionId;
    /** @type {string} */
    referer;
    /** @type {string} */
    pathInfo;
    /** @type {number[]} */
    ids;
    /** @type {?number} */
    beforeId;
    /** @type {?number} */
    afterId;
    /** @type {?boolean} */
    hasResponse;
    /** @type {?boolean} */
    withErrors;
    /** @type {?boolean} */
    enableSessionTracking;
    /** @type {?boolean} */
    enableResponseTracking;
    /** @type {?boolean} */
    enableErrorTracking;
    /** @type {?string} */
    durationLongerThan;
    /** @type {?string} */
    durationLessThan;
    /** @type {number} */
    skip;
    /** @type {?number} */
    take;
    /** @type {string} */
    orderBy;
    getTypeName() { return 'RequestLogs' }
    getMethod() { return 'POST' }
    createResponse() { return new RequestLogsResponse() }
}
export class GetValidationRules {
    /** @param {{authSecret?:string,type?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    authSecret;
    /** @type {string} */
    type;
    getTypeName() { return 'GetValidationRules' }
    getMethod() { return 'GET' }
    createResponse() { return new GetValidationRulesResponse() }
}
export class ModifyValidationRules {
    /** @param {{authSecret?:string,saveRules?:ValidationRule[],deleteRuleIds?:number[],suspendRuleIds?:number[],unsuspendRuleIds?:number[],clearCache?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    authSecret;
    /** @type {ValidationRule[]} */
    saveRules;
    /** @type {number[]} */
    deleteRuleIds;
    /** @type {number[]} */
    suspendRuleIds;
    /** @type {number[]} */
    unsuspendRuleIds;
    /** @type {?boolean} */
    clearCache;
    getTypeName() { return 'ModifyValidationRules' }
    getMethod() { return 'POST' }
    createResponse() { }
}

