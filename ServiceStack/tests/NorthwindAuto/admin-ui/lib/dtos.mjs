/* Options:
Date: 2024-09-01 13:46:12
Version: 8.31
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:20000

//AddServiceStackTypes: True
//AddDocAnnotations: True
//AddDescriptionAsComments: True
//IncludeTypes: 
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
export class RedisEndpointInfo {
    /** @param {{host?:string,port?:number,ssl?:boolean,db?:number,username?:string,password?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    host;
    /** @type {number} */
    port;
    /** @type {?boolean} */
    ssl;
    /** @type {number} */
    db;
    /** @type {string} */
    username;
    /** @type {string} */
    password;
}
export class QueryBase {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    skip;
    /** @type {?number} */
    take;
    /** @type {string} */
    orderBy;
    /** @type {string} */
    orderByDesc;
    /** @type {string} */
    include;
    /** @type {string} */
    fields;
    /** @type {{ [index: string]: string; }} */
    meta;
}
/** @typedef T {any} */
export class QueryDb extends QueryBase {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
}
/** @typedef {'Queued'|'Started'|'Executed'|'Completed'|'Failed'|'Cancelled'} */
export var BackgroundJobState;
(function (BackgroundJobState) {
    BackgroundJobState["Queued"] = "Queued"
    BackgroundJobState["Started"] = "Started"
    BackgroundJobState["Executed"] = "Executed"
    BackgroundJobState["Completed"] = "Completed"
    BackgroundJobState["Failed"] = "Failed"
    BackgroundJobState["Cancelled"] = "Cancelled"
})(BackgroundJobState || (BackgroundJobState = {}));
export class ResponseError {
    /** @param {{errorCode?:string,fieldName?:string,message?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    errorCode;
    /** @type {string} */
    fieldName;
    /** @type {string} */
    message;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class ResponseStatus {
    /** @param {{errorCode?:string,message?:string,stackTrace?:string,errors?:ResponseError[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    errorCode;
    /** @type {string} */
    message;
    /** @type {string} */
    stackTrace;
    /** @type {ResponseError[]} */
    errors;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class BackgroundJobBase {
    /** @param {{id?:number,parentId?:number,refId?:string,worker?:string,tag?:string,batchId?:string,callback?:string,dependsOn?:number,runAfter?:string,createdDate?:string,createdBy?:string,requestId?:string,requestType?:string,command?:string,request?:string,requestBody?:string,userId?:string,response?:string,responseBody?:string,state?:BackgroundJobState,startedDate?:string,completedDate?:string,notifiedDate?:string,retryLimit?:number,attempts?:number,durationMs?:number,timeoutSecs?:number,progress?:number,status?:string,logs?:string,lastActivityDate?:string,replyTo?:string,errorCode?:string,error?:ResponseStatus,args?:{ [index: string]: string; },meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?number} */
    parentId;
    /** @type {?string} */
    refId;
    /** @type {?string} */
    worker;
    /** @type {?string} */
    tag;
    /** @type {?string} */
    batchId;
    /** @type {?string} */
    callback;
    /** @type {?number} */
    dependsOn;
    /** @type {?string} */
    runAfter;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    createdBy;
    /** @type {?string} */
    requestId;
    /** @type {string} */
    requestType;
    /** @type {?string} */
    command;
    /** @type {string} */
    request;
    /** @type {string} */
    requestBody;
    /** @type {?string} */
    userId;
    /** @type {?string} */
    response;
    /** @type {?string} */
    responseBody;
    /** @type {BackgroundJobState} */
    state;
    /** @type {?string} */
    startedDate;
    /** @type {?string} */
    completedDate;
    /** @type {?string} */
    notifiedDate;
    /** @type {?number} */
    retryLimit;
    /** @type {number} */
    attempts;
    /** @type {number} */
    durationMs;
    /** @type {?number} */
    timeoutSecs;
    /** @type {?number} */
    progress;
    /** @type {?string} */
    status;
    /** @type {?string} */
    logs;
    /** @type {?string} */
    lastActivityDate;
    /** @type {?string} */
    replyTo;
    /** @type {?string} */
    errorCode;
    /** @type {?ResponseStatus} */
    error;
    /** @type {?{ [index: string]: string; }} */
    args;
    /** @type {?{ [index: string]: string; }} */
    meta;
}
export class BackgroundJob extends BackgroundJobBase {
    /** @param {{id?:number,id?:number,parentId?:number,refId?:string,worker?:string,tag?:string,batchId?:string,callback?:string,dependsOn?:number,runAfter?:string,createdDate?:string,createdBy?:string,requestId?:string,requestType?:string,command?:string,request?:string,requestBody?:string,userId?:string,response?:string,responseBody?:string,state?:BackgroundJobState,startedDate?:string,completedDate?:string,notifiedDate?:string,retryLimit?:number,attempts?:number,durationMs?:number,timeoutSecs?:number,progress?:number,status?:string,logs?:string,lastActivityDate?:string,replyTo?:string,errorCode?:string,error?:ResponseStatus,args?:{ [index: string]: string; },meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
}
export class JobSummary {
    /** @param {{id?:number,parentId?:number,refId?:string,worker?:string,tag?:string,batchId?:string,createdDate?:string,createdBy?:string,requestType?:string,command?:string,request?:string,response?:string,userId?:string,callback?:string,startedDate?:string,completedDate?:string,state?:BackgroundJobState,durationMs?:number,attempts?:number,errorCode?:string,errorMessage?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?number} */
    parentId;
    /** @type {?string} */
    refId;
    /** @type {?string} */
    worker;
    /** @type {?string} */
    tag;
    /** @type {?string} */
    batchId;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    createdBy;
    /** @type {string} */
    requestType;
    /** @type {?string} */
    command;
    /** @type {string} */
    request;
    /** @type {?string} */
    response;
    /** @type {?string} */
    userId;
    /** @type {?string} */
    callback;
    /** @type {?string} */
    startedDate;
    /** @type {?string} */
    completedDate;
    /** @type {BackgroundJobState} */
    state;
    /** @type {number} */
    durationMs;
    /** @type {number} */
    attempts;
    /** @type {?string} */
    errorCode;
    /** @type {?string} */
    errorMessage;
}
export class BackgroundJobOptions {
    /** @param {{refId?:string,parentId?:number,worker?:string,runAfter?:string,callback?:string,dependsOn?:number,userId?:string,retryLimit?:number,replyTo?:string,tag?:string,batchId?:string,createdBy?:string,timeoutSecs?:number,args?:{ [index: string]: string; },runCommand?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    refId;
    /** @type {?number} */
    parentId;
    /** @type {?string} */
    worker;
    /** @type {?string} */
    runAfter;
    /** @type {?string} */
    callback;
    /** @type {?number} */
    dependsOn;
    /** @type {?string} */
    userId;
    /** @type {?number} */
    retryLimit;
    /** @type {?string} */
    replyTo;
    /** @type {?string} */
    tag;
    /** @type {?string} */
    batchId;
    /** @type {?string} */
    createdBy;
    /** @type {?number} */
    timeoutSecs;
    /** @type {?{ [index: string]: string; }} */
    args;
    /** @type {?boolean} */
    runCommand;
}
export class ScheduledTask {
    /** @param {{id?:number,name?:string,interval?:string,cronExpression?:string,requestType?:string,command?:string,request?:string,requestBody?:string,options?:BackgroundJobOptions,lastRun?:string,lastJobId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {?string} */
    interval;
    /** @type {?string} */
    cronExpression;
    /** @type {string} */
    requestType;
    /** @type {?string} */
    command;
    /** @type {string} */
    request;
    /** @type {string} */
    requestBody;
    /** @type {?BackgroundJobOptions} */
    options;
    /** @type {?string} */
    lastRun;
    /** @type {?number} */
    lastJobId;
}
export class CompletedJob extends BackgroundJobBase {
    /** @param {{id?:number,parentId?:number,refId?:string,worker?:string,tag?:string,batchId?:string,callback?:string,dependsOn?:number,runAfter?:string,createdDate?:string,createdBy?:string,requestId?:string,requestType?:string,command?:string,request?:string,requestBody?:string,userId?:string,response?:string,responseBody?:string,state?:BackgroundJobState,startedDate?:string,completedDate?:string,notifiedDate?:string,retryLimit?:number,attempts?:number,durationMs?:number,timeoutSecs?:number,progress?:number,status?:string,logs?:string,lastActivityDate?:string,replyTo?:string,errorCode?:string,error?:ResponseStatus,args?:{ [index: string]: string; },meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
}
export class FailedJob extends BackgroundJobBase {
    /** @param {{id?:number,parentId?:number,refId?:string,worker?:string,tag?:string,batchId?:string,callback?:string,dependsOn?:number,runAfter?:string,createdDate?:string,createdBy?:string,requestId?:string,requestType?:string,command?:string,request?:string,requestBody?:string,userId?:string,response?:string,responseBody?:string,state?:BackgroundJobState,startedDate?:string,completedDate?:string,notifiedDate?:string,retryLimit?:number,attempts?:number,durationMs?:number,timeoutSecs?:number,progress?:number,status?:string,logs?:string,lastActivityDate?:string,replyTo?:string,errorCode?:string,error?:ResponseStatus,args?:{ [index: string]: string; },meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
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
export class AppInfo {
    /** @param {{baseUrl?:string,serviceStackVersion?:string,serviceName?:string,apiVersion?:string,serviceDescription?:string,serviceIconUrl?:string,brandUrl?:string,brandImageUrl?:string,textColor?:string,linkColor?:string,backgroundColor?:string,backgroundImageUrl?:string,iconUrl?:string,jsTextCase?:string,useSystemJson?:string,endpointRouting?:string[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    baseUrl;
    /** @type {string} */
    serviceStackVersion;
    /** @type {string} */
    serviceName;
    /** @type {string} */
    apiVersion;
    /** @type {string} */
    serviceDescription;
    /** @type {string} */
    serviceIconUrl;
    /** @type {string} */
    brandUrl;
    /** @type {string} */
    brandImageUrl;
    /** @type {string} */
    textColor;
    /** @type {string} */
    linkColor;
    /** @type {string} */
    backgroundColor;
    /** @type {string} */
    backgroundImageUrl;
    /** @type {string} */
    iconUrl;
    /** @type {string} */
    jsTextCase;
    /** @type {string} */
    useSystemJson;
    /** @type {?string[]} */
    endpointRouting;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class ImageInfo {
    /** @param {{svg?:string,uri?:string,alt?:string,cls?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    svg;
    /** @type {string} */
    uri;
    /** @type {string} */
    alt;
    /** @type {string} */
    cls;
}
export class LinkInfo {
    /** @param {{id?:string,href?:string,label?:string,icon?:ImageInfo,show?:string,hide?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    href;
    /** @type {string} */
    label;
    /** @type {ImageInfo} */
    icon;
    /** @type {string} */
    show;
    /** @type {string} */
    hide;
}
export class ThemeInfo {
    /** @param {{form?:string,modelIcon?:ImageInfo}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    form;
    /** @type {ImageInfo} */
    modelIcon;
}
export class ApiCss {
    /** @param {{form?:string,fieldset?:string,field?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    form;
    /** @type {string} */
    fieldset;
    /** @type {string} */
    field;
}
export class AppTags {
    /** @param {{default?:string,other?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    default;
    /** @type {string} */
    other;
}
export class LocodeUi {
    /** @param {{css?:ApiCss,tags?:AppTags,maxFieldLength?:number,maxNestedFields?:number,maxNestedFieldLength?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ApiCss} */
    css;
    /** @type {AppTags} */
    tags;
    /** @type {number} */
    maxFieldLength;
    /** @type {number} */
    maxNestedFields;
    /** @type {number} */
    maxNestedFieldLength;
}
export class ExplorerUi {
    /** @param {{css?:ApiCss,tags?:AppTags}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ApiCss} */
    css;
    /** @type {AppTags} */
    tags;
}
export class AdminUi {
    /** @param {{css?:ApiCss}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ApiCss} */
    css;
}
export class FormatInfo {
    /** @param {{method?:string,options?:string,locale?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    method;
    /** @type {string} */
    options;
    /** @type {string} */
    locale;
}
export class ApiFormat {
    /** @param {{locale?:string,assumeUtc?:boolean,number?:FormatInfo,date?:FormatInfo}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    locale;
    /** @type {boolean} */
    assumeUtc;
    /** @type {FormatInfo} */
    number;
    /** @type {FormatInfo} */
    date;
}
export class UiInfo {
    /** @param {{brandIcon?:ImageInfo,hideTags?:string[],modules?:string[],alwaysHideTags?:string[],adminLinks?:LinkInfo[],theme?:ThemeInfo,locode?:LocodeUi,explorer?:ExplorerUi,admin?:AdminUi,defaultFormats?:ApiFormat,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ImageInfo} */
    brandIcon;
    /** @type {string[]} */
    hideTags;
    /** @type {string[]} */
    modules;
    /** @type {string[]} */
    alwaysHideTags;
    /** @type {LinkInfo[]} */
    adminLinks;
    /** @type {ThemeInfo} */
    theme;
    /** @type {LocodeUi} */
    locode;
    /** @type {ExplorerUi} */
    explorer;
    /** @type {AdminUi} */
    admin;
    /** @type {ApiFormat} */
    defaultFormats;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class ConfigInfo {
    /** @param {{debugMode?:boolean,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?boolean} */
    debugMode;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class NavItem {
    /** @param {{label?:string,href?:string,exact?:boolean,id?:string,className?:string,iconClass?:string,iconSrc?:string,show?:string,hide?:string,children?:NavItem[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    label;
    /** @type {string} */
    href;
    /** @type {?boolean} */
    exact;
    /** @type {string} */
    id;
    /** @type {string} */
    className;
    /** @type {string} */
    iconClass;
    /** @type {string} */
    iconSrc;
    /** @type {string} */
    show;
    /** @type {string} */
    hide;
    /** @type {NavItem[]} */
    children;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class FieldCss {
    /** @param {{field?:string,input?:string,label?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    field;
    /** @type {string} */
    input;
    /** @type {string} */
    label;
}
export class InputInfo {
    /** @param {{id?:string,name?:string,type?:string,value?:string,placeholder?:string,help?:string,label?:string,title?:string,size?:string,pattern?:string,readOnly?:boolean,required?:boolean,disabled?:boolean,autocomplete?:string,autofocus?:string,min?:string,max?:string,step?:string,minLength?:number,maxLength?:number,accept?:string,capture?:string,multiple?:boolean,allowableValues?:string[],allowableEntries?:KeyValuePair<string, string>[],options?:string,ignore?:boolean,css?:FieldCss,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    type;
    /** @type {string} */
    value;
    /** @type {string} */
    placeholder;
    /** @type {string} */
    help;
    /** @type {string} */
    label;
    /** @type {string} */
    title;
    /** @type {string} */
    size;
    /** @type {string} */
    pattern;
    /** @type {?boolean} */
    readOnly;
    /** @type {?boolean} */
    required;
    /** @type {?boolean} */
    disabled;
    /** @type {string} */
    autocomplete;
    /** @type {string} */
    autofocus;
    /** @type {string} */
    min;
    /** @type {string} */
    max;
    /** @type {string} */
    step;
    /** @type {?number} */
    minLength;
    /** @type {?number} */
    maxLength;
    /** @type {string} */
    accept;
    /** @type {string} */
    capture;
    /** @type {?boolean} */
    multiple;
    /** @type {string[]} */
    allowableValues;
    /** @type {KeyValuePair<string, string>[]} */
    allowableEntries;
    /** @type {string} */
    options;
    /** @type {?boolean} */
    ignore;
    /** @type {FieldCss} */
    css;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class MetaAuthProvider {
    /** @param {{name?:string,label?:string,type?:string,navItem?:NavItem,icon?:ImageInfo,formLayout?:InputInfo[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    label;
    /** @type {string} */
    type;
    /** @type {NavItem} */
    navItem;
    /** @type {ImageInfo} */
    icon;
    /** @type {InputInfo[]} */
    formLayout;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class IdentityAuthInfo {
    /** @param {{hasRefreshToken?:boolean,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?boolean} */
    hasRefreshToken;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class AuthInfo {
    /** @param {{hasAuthSecret?:boolean,hasAuthRepository?:boolean,includesRoles?:boolean,includesOAuthTokens?:boolean,htmlRedirect?:string,authProviders?:MetaAuthProvider[],identityAuth?:IdentityAuthInfo,roleLinks?:{ [index: string]: LinkInfo[]; },serviceRoutes?:{ [index: string]: string[]; },meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?boolean} */
    hasAuthSecret;
    /** @type {?boolean} */
    hasAuthRepository;
    /** @type {?boolean} */
    includesRoles;
    /** @type {?boolean} */
    includesOAuthTokens;
    /** @type {string} */
    htmlRedirect;
    /** @type {MetaAuthProvider[]} */
    authProviders;
    /** @type {IdentityAuthInfo} */
    identityAuth;
    /** @type {{ [index: string]: LinkInfo[]; }} */
    roleLinks;
    /** @type {{ [index: string]: string[]; }} */
    serviceRoutes;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class ApiKeyInfo {
    /** @param {{label?:string,httpHeader?:string,scopes?:string[],features?:string[],requestTypes?:string[],expiresIn?:KeyValuePair<string,string>[],hide?:string[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    label;
    /** @type {string} */
    httpHeader;
    /** @type {string[]} */
    scopes;
    /** @type {string[]} */
    features;
    /** @type {string[]} */
    requestTypes;
    /** @type {KeyValuePair<string,string>[]} */
    expiresIn;
    /** @type {string[]} */
    hide;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class MetadataTypeName {
    /** @param {{name?:string,namespace?:string,genericArgs?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    namespace;
    /** @type {string[]} */
    genericArgs;
}
export class MetadataDataContract {
    /** @param {{name?:string,namespace?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    namespace;
}
export class MetadataDataMember {
    /** @param {{name?:string,order?:number,isRequired?:boolean,emitDefaultValue?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {?number} */
    order;
    /** @type {?boolean} */
    isRequired;
    /** @type {?boolean} */
    emitDefaultValue;
}
export class MetadataAttribute {
    /** @param {{name?:string,constructorArgs?:MetadataPropertyType[],args?:MetadataPropertyType[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {MetadataPropertyType[]} */
    constructorArgs;
    /** @type {MetadataPropertyType[]} */
    args;
}
export class RefInfo {
    /** @param {{model?:string,selfId?:string,refId?:string,refLabel?:string,queryApi?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    model;
    /** @type {string} */
    selfId;
    /** @type {string} */
    refId;
    /** @type {string} */
    refLabel;
    /** @type {string} */
    queryApi;
}
export class MetadataPropertyType {
    /** @param {{name?:string,type?:string,namespace?:string,isValueType?:boolean,isEnum?:boolean,isPrimaryKey?:boolean,genericArgs?:string[],value?:string,description?:string,dataMember?:MetadataDataMember,readOnly?:boolean,paramType?:string,displayType?:string,isRequired?:boolean,allowableValues?:string[],allowableMin?:number,allowableMax?:number,attributes?:MetadataAttribute[],uploadTo?:string,input?:InputInfo,format?:FormatInfo,ref?:RefInfo}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    type;
    /** @type {string} */
    namespace;
    /** @type {?boolean} */
    isValueType;
    /** @type {?boolean} */
    isEnum;
    /** @type {?boolean} */
    isPrimaryKey;
    /** @type {string[]} */
    genericArgs;
    /** @type {string} */
    value;
    /** @type {string} */
    description;
    /** @type {MetadataDataMember} */
    dataMember;
    /** @type {?boolean} */
    readOnly;
    /** @type {string} */
    paramType;
    /** @type {string} */
    displayType;
    /** @type {?boolean} */
    isRequired;
    /** @type {string[]} */
    allowableValues;
    /** @type {?number} */
    allowableMin;
    /** @type {?number} */
    allowableMax;
    /** @type {MetadataAttribute[]} */
    attributes;
    /** @type {string} */
    uploadTo;
    /** @type {InputInfo} */
    input;
    /** @type {FormatInfo} */
    format;
    /** @type {RefInfo} */
    ref;
}
export class MetadataType {
    /** @param {{name?:string,namespace?:string,genericArgs?:string[],inherits?:MetadataTypeName,implements?:MetadataTypeName[],displayType?:string,description?:string,notes?:string,icon?:ImageInfo,isNested?:boolean,isEnum?:boolean,isEnumInt?:boolean,isInterface?:boolean,isAbstract?:boolean,isGenericTypeDef?:boolean,dataContract?:MetadataDataContract,properties?:MetadataPropertyType[],attributes?:MetadataAttribute[],innerTypes?:MetadataTypeName[],enumNames?:string[],enumValues?:string[],enumMemberValues?:string[],enumDescriptions?:string[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    namespace;
    /** @type {string[]} */
    genericArgs;
    /** @type {MetadataTypeName} */
    inherits;
    /** @type {MetadataTypeName[]} */
    implements;
    /** @type {string} */
    displayType;
    /** @type {string} */
    description;
    /** @type {string} */
    notes;
    /** @type {ImageInfo} */
    icon;
    /** @type {?boolean} */
    isNested;
    /** @type {?boolean} */
    isEnum;
    /** @type {?boolean} */
    isEnumInt;
    /** @type {?boolean} */
    isInterface;
    /** @type {?boolean} */
    isAbstract;
    /** @type {?boolean} */
    isGenericTypeDef;
    /** @type {MetadataDataContract} */
    dataContract;
    /** @type {MetadataPropertyType[]} */
    properties;
    /** @type {MetadataAttribute[]} */
    attributes;
    /** @type {MetadataTypeName[]} */
    innerTypes;
    /** @type {string[]} */
    enumNames;
    /** @type {string[]} */
    enumValues;
    /** @type {string[]} */
    enumMemberValues;
    /** @type {string[]} */
    enumDescriptions;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class CommandInfo {
    /** @param {{name?:string,tag?:string,request?:MetadataType,response?:MetadataType}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    tag;
    /** @type {MetadataType} */
    request;
    /** @type {MetadataType} */
    response;
}
export class CommandsInfo {
    /** @param {{commands?:CommandInfo[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {CommandInfo[]} */
    commands;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class AutoQueryConvention {
    /** @param {{name?:string,value?:string,types?:string,valueType?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    value;
    /** @type {string} */
    types;
    /** @type {string} */
    valueType;
}
export class AutoQueryInfo {
    /** @param {{maxLimit?:number,untypedQueries?:boolean,rawSqlFilters?:boolean,autoQueryViewer?:boolean,async?:boolean,orderByPrimaryKey?:boolean,crudEvents?:boolean,crudEventsServices?:boolean,accessRole?:string,namedConnection?:string,viewerConventions?:AutoQueryConvention[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    maxLimit;
    /** @type {?boolean} */
    untypedQueries;
    /** @type {?boolean} */
    rawSqlFilters;
    /** @type {?boolean} */
    autoQueryViewer;
    /** @type {?boolean} */
    async;
    /** @type {?boolean} */
    orderByPrimaryKey;
    /** @type {?boolean} */
    crudEvents;
    /** @type {?boolean} */
    crudEventsServices;
    /** @type {string} */
    accessRole;
    /** @type {string} */
    namedConnection;
    /** @type {AutoQueryConvention[]} */
    viewerConventions;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class ScriptMethodType {
    /** @param {{name?:string,paramNames?:string[],paramTypes?:string[],returnType?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string[]} */
    paramNames;
    /** @type {string[]} */
    paramTypes;
    /** @type {string} */
    returnType;
}
export class ValidationInfo {
    /** @param {{hasValidationSource?:boolean,hasValidationSourceAdmin?:boolean,serviceRoutes?:{ [index: string]: string[]; },typeValidators?:ScriptMethodType[],propertyValidators?:ScriptMethodType[],accessRole?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?boolean} */
    hasValidationSource;
    /** @type {?boolean} */
    hasValidationSourceAdmin;
    /** @type {{ [index: string]: string[]; }} */
    serviceRoutes;
    /** @type {ScriptMethodType[]} */
    typeValidators;
    /** @type {ScriptMethodType[]} */
    propertyValidators;
    /** @type {string} */
    accessRole;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class SharpPagesInfo {
    /** @param {{apiPath?:string,scriptAdminRole?:string,metadataDebugAdminRole?:string,metadataDebug?:boolean,spaFallback?:boolean,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    apiPath;
    /** @type {string} */
    scriptAdminRole;
    /** @type {string} */
    metadataDebugAdminRole;
    /** @type {?boolean} */
    metadataDebug;
    /** @type {?boolean} */
    spaFallback;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class RequestLogsInfo {
    /** @param {{accessRole?:string,requiredRoles?:string[],requestLogger?:string,defaultLimit?:number,serviceRoutes?:{ [index: string]: string[]; },meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    accessRole;
    /** @type {string[]} */
    requiredRoles;
    /** @type {string} */
    requestLogger;
    /** @type {number} */
    defaultLimit;
    /** @type {{ [index: string]: string[]; }} */
    serviceRoutes;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class ProfilingInfo {
    /** @param {{accessRole?:string,defaultLimit?:number,summaryFields?:string[],tagLabel?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    accessRole;
    /** @type {number} */
    defaultLimit;
    /** @type {string[]} */
    summaryFields;
    /** @type {?string} */
    tagLabel;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class FilesUploadLocation {
    /** @param {{name?:string,readAccessRole?:string,writeAccessRole?:string,allowExtensions?:string[],allowOperations?:string,maxFileCount?:number,minFileBytes?:number,maxFileBytes?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    readAccessRole;
    /** @type {string} */
    writeAccessRole;
    /** @type {string[]} */
    allowExtensions;
    /** @type {string} */
    allowOperations;
    /** @type {?number} */
    maxFileCount;
    /** @type {?number} */
    minFileBytes;
    /** @type {?number} */
    maxFileBytes;
}
export class FilesUploadInfo {
    /** @param {{basePath?:string,locations?:FilesUploadLocation[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    basePath;
    /** @type {FilesUploadLocation[]} */
    locations;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class MediaRule {
    /** @param {{size?:string,rule?:string,applyTo?:string[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    size;
    /** @type {string} */
    rule;
    /** @type {string[]} */
    applyTo;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class AdminUsersInfo {
    /** @param {{accessRole?:string,enabled?:string[],userAuth?:MetadataType,allRoles?:string[],allPermissions?:string[],queryUserAuthProperties?:string[],queryMediaRules?:MediaRule[],formLayout?:InputInfo[],css?:ApiCss,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    accessRole;
    /** @type {string[]} */
    enabled;
    /** @type {MetadataType} */
    userAuth;
    /** @type {string[]} */
    allRoles;
    /** @type {string[]} */
    allPermissions;
    /** @type {string[]} */
    queryUserAuthProperties;
    /** @type {MediaRule[]} */
    queryMediaRules;
    /** @type {InputInfo[]} */
    formLayout;
    /** @type {ApiCss} */
    css;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class AdminIdentityUsersInfo {
    /** @param {{accessRole?:string,enabled?:string[],identityUser?:MetadataType,allRoles?:string[],allPermissions?:string[],queryIdentityUserProperties?:string[],queryMediaRules?:MediaRule[],formLayout?:InputInfo[],css?:ApiCss,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    accessRole;
    /** @type {string[]} */
    enabled;
    /** @type {MetadataType} */
    identityUser;
    /** @type {string[]} */
    allRoles;
    /** @type {string[]} */
    allPermissions;
    /** @type {string[]} */
    queryIdentityUserProperties;
    /** @type {MediaRule[]} */
    queryMediaRules;
    /** @type {InputInfo[]} */
    formLayout;
    /** @type {ApiCss} */
    css;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class AdminRedisInfo {
    /** @param {{queryLimit?:number,databases?:number[],modifiableConnection?:boolean,endpoint?:RedisEndpointInfo,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    queryLimit;
    /** @type {number[]} */
    databases;
    /** @type {?boolean} */
    modifiableConnection;
    /** @type {RedisEndpointInfo} */
    endpoint;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class SchemaInfo {
    /** @param {{alias?:string,name?:string,tables?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    alias;
    /** @type {string} */
    name;
    /** @type {string[]} */
    tables;
}
export class DatabaseInfo {
    /** @param {{alias?:string,name?:string,schemas?:SchemaInfo[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    alias;
    /** @type {string} */
    name;
    /** @type {SchemaInfo[]} */
    schemas;
}
export class AdminDatabaseInfo {
    /** @param {{queryLimit?:number,databases?:DatabaseInfo[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    queryLimit;
    /** @type {DatabaseInfo[]} */
    databases;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class PluginInfo {
    /** @param {{loaded?:string[],auth?:AuthInfo,apiKey?:ApiKeyInfo,commands?:CommandsInfo,autoQuery?:AutoQueryInfo,validation?:ValidationInfo,sharpPages?:SharpPagesInfo,requestLogs?:RequestLogsInfo,profiling?:ProfilingInfo,filesUpload?:FilesUploadInfo,adminUsers?:AdminUsersInfo,adminIdentityUsers?:AdminIdentityUsersInfo,adminRedis?:AdminRedisInfo,adminDatabase?:AdminDatabaseInfo,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    loaded;
    /** @type {AuthInfo} */
    auth;
    /** @type {ApiKeyInfo} */
    apiKey;
    /** @type {CommandsInfo} */
    commands;
    /** @type {AutoQueryInfo} */
    autoQuery;
    /** @type {ValidationInfo} */
    validation;
    /** @type {SharpPagesInfo} */
    sharpPages;
    /** @type {RequestLogsInfo} */
    requestLogs;
    /** @type {ProfilingInfo} */
    profiling;
    /** @type {FilesUploadInfo} */
    filesUpload;
    /** @type {AdminUsersInfo} */
    adminUsers;
    /** @type {AdminIdentityUsersInfo} */
    adminIdentityUsers;
    /** @type {AdminRedisInfo} */
    adminRedis;
    /** @type {AdminDatabaseInfo} */
    adminDatabase;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class CustomPluginInfo {
    /** @param {{accessRole?:string,serviceRoutes?:{ [index: string]: string[]; },enabled?:string[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    accessRole;
    /** @type {{ [index: string]: string[]; }} */
    serviceRoutes;
    /** @type {string[]} */
    enabled;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class MetadataTypesConfig {
    /** @param {{baseUrl?:string,usePath?:string,makePartial?:boolean,makeVirtual?:boolean,makeInternal?:boolean,baseClass?:string,package?:string,addReturnMarker?:boolean,addDescriptionAsComments?:boolean,addDocAnnotations?:boolean,addDataContractAttributes?:boolean,addIndexesToDataMembers?:boolean,addGeneratedCodeAttributes?:boolean,addImplicitVersion?:number,addResponseStatus?:boolean,addServiceStackTypes?:boolean,addModelExtensions?:boolean,addPropertyAccessors?:boolean,excludeGenericBaseTypes?:boolean,settersReturnThis?:boolean,addNullableAnnotations?:boolean,makePropertiesOptional?:boolean,exportAsTypes?:boolean,excludeImplementedInterfaces?:boolean,addDefaultXmlNamespace?:string,makeDataContractsExtensible?:boolean,initializeCollections?:boolean,addNamespaces?:string[],defaultNamespaces?:string[],defaultImports?:string[],includeTypes?:string[],excludeTypes?:string[],exportTags?:string[],treatTypesAsStrings?:string[],exportValueTypes?:boolean,globalNamespace?:string,excludeNamespace?:boolean,dataClass?:string,dataClassJson?:string,ignoreTypes?:string[],exportTypes?:string[],exportAttributes?:string[],ignoreTypesInNamespaces?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    baseUrl;
    /** @type {string} */
    usePath;
    /** @type {boolean} */
    makePartial;
    /** @type {boolean} */
    makeVirtual;
    /** @type {boolean} */
    makeInternal;
    /** @type {string} */
    baseClass;
    /** @type {string} */
    package;
    /** @type {boolean} */
    addReturnMarker;
    /** @type {boolean} */
    addDescriptionAsComments;
    /** @type {boolean} */
    addDocAnnotations;
    /** @type {boolean} */
    addDataContractAttributes;
    /** @type {boolean} */
    addIndexesToDataMembers;
    /** @type {boolean} */
    addGeneratedCodeAttributes;
    /** @type {?number} */
    addImplicitVersion;
    /** @type {boolean} */
    addResponseStatus;
    /** @type {boolean} */
    addServiceStackTypes;
    /** @type {boolean} */
    addModelExtensions;
    /** @type {boolean} */
    addPropertyAccessors;
    /** @type {boolean} */
    excludeGenericBaseTypes;
    /** @type {boolean} */
    settersReturnThis;
    /** @type {boolean} */
    addNullableAnnotations;
    /** @type {boolean} */
    makePropertiesOptional;
    /** @type {boolean} */
    exportAsTypes;
    /** @type {boolean} */
    excludeImplementedInterfaces;
    /** @type {string} */
    addDefaultXmlNamespace;
    /** @type {boolean} */
    makeDataContractsExtensible;
    /** @type {boolean} */
    initializeCollections;
    /** @type {string[]} */
    addNamespaces;
    /** @type {string[]} */
    defaultNamespaces;
    /** @type {string[]} */
    defaultImports;
    /** @type {string[]} */
    includeTypes;
    /** @type {string[]} */
    excludeTypes;
    /** @type {string[]} */
    exportTags;
    /** @type {string[]} */
    treatTypesAsStrings;
    /** @type {boolean} */
    exportValueTypes;
    /** @type {string} */
    globalNamespace;
    /** @type {boolean} */
    excludeNamespace;
    /** @type {string} */
    dataClass;
    /** @type {string} */
    dataClassJson;
    /** @type {string[]} */
    ignoreTypes;
    /** @type {string[]} */
    exportTypes;
    /** @type {string[]} */
    exportAttributes;
    /** @type {string[]} */
    ignoreTypesInNamespaces;
}
export class MetadataRoute {
    /** @param {{path?:string,verbs?:string,notes?:string,summary?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    path;
    /** @type {string} */
    verbs;
    /** @type {string} */
    notes;
    /** @type {string} */
    summary;
}
export class ApiUiInfo {
    /** @param {{locodeCss?:ApiCss,explorerCss?:ApiCss,formLayout?:InputInfo[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ApiCss} */
    locodeCss;
    /** @type {ApiCss} */
    explorerCss;
    /** @type {InputInfo[]} */
    formLayout;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class MetadataOperationType {
    /** @param {{request?:MetadataType,response?:MetadataType,actions?:string[],returnsVoid?:boolean,method?:string,returnType?:MetadataTypeName,routes?:MetadataRoute[],dataModel?:MetadataTypeName,viewModel?:MetadataTypeName,requiresAuth?:boolean,requiresApiKey?:boolean,requiredRoles?:string[],requiresAnyRole?:string[],requiredPermissions?:string[],requiresAnyPermission?:string[],tags?:string[],ui?:ApiUiInfo}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {MetadataType} */
    request;
    /** @type {MetadataType} */
    response;
    /** @type {string[]} */
    actions;
    /** @type {?boolean} */
    returnsVoid;
    /** @type {string} */
    method;
    /** @type {MetadataTypeName} */
    returnType;
    /** @type {MetadataRoute[]} */
    routes;
    /** @type {MetadataTypeName} */
    dataModel;
    /** @type {MetadataTypeName} */
    viewModel;
    /** @type {?boolean} */
    requiresAuth;
    /** @type {?boolean} */
    requiresApiKey;
    /** @type {string[]} */
    requiredRoles;
    /** @type {string[]} */
    requiresAnyRole;
    /** @type {string[]} */
    requiredPermissions;
    /** @type {string[]} */
    requiresAnyPermission;
    /** @type {string[]} */
    tags;
    /** @type {ApiUiInfo} */
    ui;
}
export class MetadataTypes {
    /** @param {{config?:MetadataTypesConfig,namespaces?:string[],types?:MetadataType[],operations?:MetadataOperationType[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {MetadataTypesConfig} */
    config;
    /** @type {string[]} */
    namespaces;
    /** @type {MetadataType[]} */
    types;
    /** @type {MetadataOperationType[]} */
    operations;
}
export class ServerStats {
    /** @param {{redis?:{ [index: string]: number; },serverEvents?:{ [index: string]: string; },mqDescription?:string,mqWorkers?:{ [index: string]: number; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {{ [index: string]: number; }} */
    redis;
    /** @type {{ [index: string]: string; }} */
    serverEvents;
    /** @type {string} */
    mqDescription;
    /** @type {{ [index: string]: number; }} */
    mqWorkers;
}
export class DiagnosticEntry {
    /** @param {{id?:number,traceId?:string,source?:string,eventType?:string,message?:string,operation?:string,threadId?:number,error?:ResponseStatus,commandType?:string,command?:string,userAuthId?:string,sessionId?:string,arg?:string,args?:string[],argLengths?:number[],namedArgs?:{ [index: string]: Object; },duration?:string,timestamp?:number,date?:string,tag?:string,stackTrace?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    traceId;
    /** @type {string} */
    source;
    /** @type {string} */
    eventType;
    /** @type {string} */
    message;
    /** @type {string} */
    operation;
    /** @type {number} */
    threadId;
    /** @type {?ResponseStatus} */
    error;
    /** @type {string} */
    commandType;
    /** @type {string} */
    command;
    /** @type {?string} */
    userAuthId;
    /** @type {?string} */
    sessionId;
    /** @type {?string} */
    arg;
    /** @type {?string[]} */
    args;
    /** @type {?number[]} */
    argLengths;
    /** @type {?{ [index: string]: Object; }} */
    namedArgs;
    /** @type {?string} */
    duration;
    /** @type {number} */
    timestamp;
    /** @type {string} */
    date;
    /** @type {?string} */
    tag;
    /** @type {?string} */
    stackTrace;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class RedisSearchResult {
    /** @param {{id?:string,type?:string,ttl?:number,size?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    type;
    /** @type {number} */
    ttl;
    /** @type {number} */
    size;
}
export class RedisText {
    /** @param {{text?:string,children?:RedisText[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    text;
    /** @type {RedisText[]} */
    children;
}
export class CommandSummary {
    /** @param {{type?:string,name?:string,count?:number,failed?:number,retries?:number,totalMs?:number,minMs?:number,maxMs?:number,averageMs?:number,medianMs?:number,lastError?:ResponseStatus,timings?:ConcurrentQueue<number>}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    type;
    /** @type {string} */
    name;
    /** @type {number} */
    count;
    /** @type {number} */
    failed;
    /** @type {number} */
    retries;
    /** @type {number} */
    totalMs;
    /** @type {number} */
    minMs;
    /** @type {number} */
    maxMs;
    /** @type {number} */
    averageMs;
    /** @type {number} */
    medianMs;
    /** @type {?ResponseStatus} */
    lastError;
    /** @type {ConcurrentQueue<number>} */
    timings;
}
export class CommandResult {
    /** @param {{type?:string,name?:string,ms?:number,at?:string,request?:string,retries?:number,attempt?:number,error?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    type;
    /** @type {string} */
    name;
    /** @type {?number} */
    ms;
    /** @type {string} */
    at;
    /** @type {string} */
    request;
    /** @type {?number} */
    retries;
    /** @type {number} */
    attempt;
    /** @type {?ResponseStatus} */
    error;
}
export class PartialApiKey {
    /** @param {{id?:number,name?:string,userId?:string,userName?:string,visibleKey?:string,environment?:string,createdDate?:string,expiryDate?:string,cancelledDate?:string,lastUsedDate?:string,scopes?:string[],features?:string[],restrictTo?:string[],notes?:string,refId?:number,refIdStr?:string,meta?:{ [index: string]: string; },active?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    userId;
    /** @type {string} */
    userName;
    /** @type {string} */
    visibleKey;
    /** @type {string} */
    environment;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    expiryDate;
    /** @type {?string} */
    cancelledDate;
    /** @type {?string} */
    lastUsedDate;
    /** @type {string[]} */
    scopes;
    /** @type {string[]} */
    features;
    /** @type {string[]} */
    restrictTo;
    /** @type {string} */
    notes;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {boolean} */
    active;
}
export class JobStatSummary {
    /** @param {{name?:string,total?:number,completed?:number,retries?:number,failed?:number,cancelled?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {number} */
    total;
    /** @type {number} */
    completed;
    /** @type {number} */
    retries;
    /** @type {number} */
    failed;
    /** @type {number} */
    cancelled;
}
export class HourSummary {
    /** @param {{hour?:string,total?:number,completed?:number,failed?:number,cancelled?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    hour;
    /** @type {number} */
    total;
    /** @type {number} */
    completed;
    /** @type {number} */
    failed;
    /** @type {number} */
    cancelled;
}
export class RequestLogEntry {
    /** @param {{id?:number,traceId?:string,operationName?:string,dateTime?:string,statusCode?:number,statusDescription?:string,httpMethod?:string,absoluteUri?:string,pathInfo?:string,requestBody?:string,requestDto?:Object,userAuthId?:string,sessionId?:string,ipAddress?:string,forwardedFor?:string,referer?:string,headers?:{ [index: string]: string; },formData?:{ [index: string]: string; },items?:{ [index: string]: string; },responseHeaders?:{ [index: string]: string; },session?:Object,responseDto?:Object,errorResponse?:Object,exceptionSource?:string,exceptionData?:any,requestDuration?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    traceId;
    /** @type {string} */
    operationName;
    /** @type {string} */
    dateTime;
    /** @type {number} */
    statusCode;
    /** @type {string} */
    statusDescription;
    /** @type {string} */
    httpMethod;
    /** @type {string} */
    absoluteUri;
    /** @type {string} */
    pathInfo;
    /** @type {string} */
    requestBody;
    /** @type {Object} */
    requestDto;
    /** @type {string} */
    userAuthId;
    /** @type {string} */
    sessionId;
    /** @type {string} */
    ipAddress;
    /** @type {string} */
    forwardedFor;
    /** @type {string} */
    referer;
    /** @type {{ [index: string]: string; }} */
    headers;
    /** @type {{ [index: string]: string; }} */
    formData;
    /** @type {{ [index: string]: string; }} */
    items;
    /** @type {{ [index: string]: string; }} */
    responseHeaders;
    /** @type {Object} */
    session;
    /** @type {Object} */
    responseDto;
    /** @type {Object} */
    errorResponse;
    /** @type {string} */
    exceptionSource;
    /** @type {any} */
    exceptionData;
    /** @type {string} */
    requestDuration;
    /** @type {{ [index: string]: string; }} */
    meta;
}
/** @typedef TKey {any} */
/** @typedef  TValue {any} */
export class KeyValuePair {
    /** @param {{key?:TKey,value?:TValue}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {TKey} */
    key;
    /** @type {TValue} */
    value;
}
export class AppMetadata {
    /** @param {{date?:string,app?:AppInfo,ui?:UiInfo,config?:ConfigInfo,contentTypeFormats?:{ [index: string]: string; },httpHandlers?:{ [index: string]: string; },plugins?:PluginInfo,customPlugins?:{ [index: string]: CustomPluginInfo; },api?:MetadataTypes,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    date;
    /** @type {AppInfo} */
    app;
    /** @type {UiInfo} */
    ui;
    /** @type {ConfigInfo} */
    config;
    /** @type {{ [index: string]: string; }} */
    contentTypeFormats;
    /** @type {{ [index: string]: string; }} */
    httpHandlers;
    /** @type {PluginInfo} */
    plugins;
    /** @type {{ [index: string]: CustomPluginInfo; }} */
    customPlugins;
    /** @type {MetadataTypes} */
    api;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class AdminDashboardResponse {
    /** @param {{serverStats?:ServerStats,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ServerStats} */
    serverStats;
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
export class AssignRolesResponse {
    /** @param {{allRoles?:string[],allPermissions?:string[],meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    allRoles;
    /** @type {string[]} */
    allPermissions;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class UnAssignRolesResponse {
    /** @param {{allRoles?:string[],allPermissions?:string[],meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    allRoles;
    /** @type {string[]} */
    allPermissions;
    /** @type {{ [index: string]: string; }} */
    meta;
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
export class AdminUsersResponse {
    /** @param {{results?:{ [index:string]: Object; }[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {{ [index:string]: Object; }[]} */
    results;
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
export class ViewCommandsResponse {
    /** @param {{commandTotals?:CommandSummary[],latestCommands?:CommandResult[],latestFailed?:CommandResult[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {CommandSummary[]} */
    commandTotals;
    /** @type {CommandResult[]} */
    latestCommands;
    /** @type {CommandResult[]} */
    latestFailed;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class ExecuteCommandResponse {
    /** @param {{commandResult?:CommandResult,result?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?CommandResult} */
    commandResult;
    /** @type {?string} */
    result;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AdminApiKeysResponse {
    /** @param {{results?:PartialApiKey[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {PartialApiKey[]} */
    results;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AdminApiKeyResponse {
    /** @param {{result?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    result;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class EmptyResponse {
    /** @param {{responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AdminJobDashboardResponse {
    /** @param {{commands?:JobStatSummary[],apis?:JobStatSummary[],workers?:JobStatSummary[],today?:HourSummary[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {JobStatSummary[]} */
    commands;
    /** @type {JobStatSummary[]} */
    apis;
    /** @type {JobStatSummary[]} */
    workers;
    /** @type {HourSummary[]} */
    today;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AdminJobInfoResponse {
    /** @param {{monthDbs?:string[],tableCounts?:{ [index: string]: number; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    monthDbs;
    /** @type {{ [index: string]: number; }} */
    tableCounts;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AdminGetJobResponse {
    /** @param {{result?:JobSummary,queued?:BackgroundJob,completed?:CompletedJob,failed?:FailedJob,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {JobSummary} */
    result;
    /** @type {?BackgroundJob} */
    queued;
    /** @type {?CompletedJob} */
    completed;
    /** @type {?FailedJob} */
    failed;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AdminGetJobProgressResponse {
    /** @param {{state?:BackgroundJobState,progress?:number,status?:string,logs?:string,durationMs?:number,error?:ResponseStatus,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {BackgroundJobState} */
    state;
    /** @type {?number} */
    progress;
    /** @type {?string} */
    status;
    /** @type {?string} */
    logs;
    /** @type {?number} */
    durationMs;
    /** @type {?ResponseStatus} */
    error;
    /** @type {?ResponseStatus} */
    responseStatus;
}
/** @typedef T {any} */
export class QueryResponse {
    /** @param {{offset?:number,total?:number,results?:T[],meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    offset;
    /** @type {number} */
    total;
    /** @type {T[]} */
    results;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AdminRequeueFailedJobsJobsResponse {
    /** @param {{results?:number[],errors?:{ [index: number]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number[]} */
    results;
    /** @type {{ [index: number]: string; }} */
    errors;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AdminCancelJobsResponse {
    /** @param {{results?:number[],errors?:{ [index: number]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number[]} */
    results;
    /** @type {{ [index: number]: string; }} */
    errors;
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
export class MetadataApp {
    /** @param {{view?:string,includeTypes?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    view;
    /** @type {string[]} */
    includeTypes;
    getTypeName() { return 'MetadataApp' }
    getMethod() { return 'GET' }
    createResponse() { return new AppMetadata() }
}
export class AdminDashboard {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'AdminDashboard' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminDashboardResponse() }
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
export class AssignRoles {
    /** @param {{userName?:string,permissions?:string[],roles?:string[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userName;
    /** @type {string[]} */
    permissions;
    /** @type {string[]} */
    roles;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'AssignRoles' }
    getMethod() { return 'POST' }
    createResponse() { return new AssignRolesResponse() }
}
export class UnAssignRoles {
    /** @param {{userName?:string,permissions?:string[],roles?:string[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userName;
    /** @type {string[]} */
    permissions;
    /** @type {string[]} */
    roles;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'UnAssignRoles' }
    getMethod() { return 'POST' }
    createResponse() { return new UnAssignRolesResponse() }
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
    /** @param {{id?:string,lockUser?:boolean,unlockUser?:boolean,lockUserUntil?:string,addRoles?:string[],removeRoles?:string[],addPermissions?:string[],removePermissions?:string[],userName?:string,firstName?:string,lastName?:string,displayName?:string,email?:string,password?:string,profileUrl?:string,phoneNumber?:string,userAuthProperties?:{ [index: string]: string; },meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {?boolean} */
    lockUser;
    /** @type {?boolean} */
    unlockUser;
    /** @type {?string} */
    lockUserUntil;
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
export class ViewCommands {
    /** @param {{include?:string[],skip?:number,take?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string[]} */
    include;
    /** @type {?number} */
    skip;
    /** @type {?number} */
    take;
    getTypeName() { return 'ViewCommands' }
    getMethod() { return 'GET' }
    createResponse() { return new ViewCommandsResponse() }
}
export class ExecuteCommand {
    /** @param {{command?:string,requestJson?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    command;
    /** @type {?string} */
    requestJson;
    getTypeName() { return 'ExecuteCommand' }
    getMethod() { return 'POST' }
    createResponse() { return new ExecuteCommandResponse() }
}
export class AdminQueryApiKeys {
    /** @param {{id?:number,search?:string,userId?:string,userName?:string,orderBy?:string,skip?:number,take?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {string} */
    search;
    /** @type {string} */
    userId;
    /** @type {string} */
    userName;
    /** @type {string} */
    orderBy;
    /** @type {?number} */
    skip;
    /** @type {?number} */
    take;
    getTypeName() { return 'AdminQueryApiKeys' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminApiKeysResponse() }
}
export class AdminCreateApiKey {
    /** @param {{name?:string,userId?:string,userName?:string,scopes?:string[],features?:string[],restrictTo?:string[],expiryDate?:string,notes?:string,refId?:number,refIdStr?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    userId;
    /** @type {string} */
    userName;
    /** @type {string[]} */
    scopes;
    /** @type {string[]} */
    features;
    /** @type {string[]} */
    restrictTo;
    /** @type {?string} */
    expiryDate;
    /** @type {string} */
    notes;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'AdminCreateApiKey' }
    getMethod() { return 'POST' }
    createResponse() { return new AdminApiKeyResponse() }
}
export class AdminUpdateApiKey {
    /** @param {{id?:number,name?:string,userId?:string,userName?:string,scopes?:string[],features?:string[],restrictTo?:string[],expiryDate?:string,cancelledDate?:string,notes?:string,refId?:number,refIdStr?:string,meta?:{ [index: string]: string; },reset?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    userId;
    /** @type {string} */
    userName;
    /** @type {string[]} */
    scopes;
    /** @type {string[]} */
    features;
    /** @type {string[]} */
    restrictTo;
    /** @type {?string} */
    expiryDate;
    /** @type {?string} */
    cancelledDate;
    /** @type {string} */
    notes;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {string[]} */
    reset;
    getTypeName() { return 'AdminUpdateApiKey' }
    getMethod() { return 'PATCH' }
    createResponse() { return new EmptyResponse() }
}
export class AdminDeleteApiKey {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'AdminDeleteApiKey' }
    getMethod() { return 'DELETE' }
    createResponse() { return new EmptyResponse() }
}
export class AdminJobDashboard {
    /** @param {{from?:string,to?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    from;
    /** @type {?string} */
    to;
    getTypeName() { return 'AdminJobDashboard' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminJobDashboardResponse() }
}
export class AdminJobInfo {
    /** @param {{month?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    month;
    getTypeName() { return 'AdminJobInfo' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminJobInfoResponse() }
}
export class AdminGetJob {
    /** @param {{id?:number,refId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?string} */
    refId;
    getTypeName() { return 'AdminGetJob' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminGetJobResponse() }
}
export class AdminGetJobProgress {
    /** @param {{id?:number,logStart?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?number} */
    logStart;
    getTypeName() { return 'AdminGetJobProgress' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminGetJobProgressResponse() }
}
export class AdminQueryBackgroundJobs extends QueryDb {
    /** @param {{id?:number,refId?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?string} */
    refId;
    getTypeName() { return 'AdminQueryBackgroundJobs' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class AdminQueryJobSummary extends QueryDb {
    /** @param {{id?:number,refId?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?string} */
    refId;
    getTypeName() { return 'AdminQueryJobSummary' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class AdminQueryScheduledTasks extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'AdminQueryScheduledTasks' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class AdminQueryCompletedJobs extends QueryDb {
    /** @param {{month?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?string} */
    month;
    getTypeName() { return 'AdminQueryCompletedJobs' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class AdminQueryFailedJobs extends QueryDb {
    /** @param {{month?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?string} */
    month;
    getTypeName() { return 'AdminQueryFailedJobs' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class AdminRequeueFailedJobs {
    /** @param {{ids?:number[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number[]} */
    ids;
    getTypeName() { return 'AdminRequeueFailedJobs' }
    getMethod() { return 'POST' }
    createResponse() { return new AdminRequeueFailedJobsJobsResponse() }
}
export class AdminCancelJobs {
    /** @param {{ids?:number[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number[]} */
    ids;
    getTypeName() { return 'AdminCancelJobs' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminCancelJobsResponse() }
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

