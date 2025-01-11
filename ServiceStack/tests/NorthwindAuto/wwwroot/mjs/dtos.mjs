/* Options:
Date: 2025-01-11 16:32:08
Version: 8.53
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: https://localhost:5001

//AddServiceStackTypes: True
//AddDocAnnotations: True
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: 
*/

"use strict";
/** @typedef {'Applied'|'PhoneScreening'|'PhoneScreeningCompleted'|'Interview'|'InterviewCompleted'|'Offer'|'Disqualified'} */
export var JobApplicationStatus;
(function (JobApplicationStatus) {
    JobApplicationStatus["Applied"] = "Applied"
    JobApplicationStatus["PhoneScreening"] = "PhoneScreening"
    JobApplicationStatus["PhoneScreeningCompleted"] = "PhoneScreeningCompleted"
    JobApplicationStatus["Interview"] = "Interview"
    JobApplicationStatus["InterviewCompleted"] = "InterviewCompleted"
    JobApplicationStatus["Offer"] = "Offer"
    JobApplicationStatus["Disqualified"] = "Disqualified"
})(JobApplicationStatus || (JobApplicationStatus = {}));
export class AuditBase {
    /** @param {{createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    createdDate;
    /** @type {string} */
    createdBy;
    /** @type {string} */
    modifiedDate;
    /** @type {string} */
    modifiedBy;
    /** @type {?string} */
    deletedDate;
    /** @type {string} */
    deletedBy;
}
/** @typedef TKey {any} */
export class IdentityUser_1 {
    /** @param {{id?:TKey,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:boolean,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:boolean,twoFactorEnabled?:boolean,lockoutEnd?:string,lockoutEnabled?:boolean,accessFailedCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {TKey} */
    id;
    /** @type {?string} */
    userName;
    /** @type {?string} */
    normalizedUserName;
    /** @type {?string} */
    email;
    /** @type {?string} */
    normalizedEmail;
    /** @type {boolean} */
    emailConfirmed;
    /** @type {?string} */
    passwordHash;
    /** @type {?string} */
    securityStamp;
    /** @type {?string} */
    concurrencyStamp;
    /** @type {?string} */
    phoneNumber;
    /** @type {boolean} */
    phoneNumberConfirmed;
    /** @type {boolean} */
    twoFactorEnabled;
    /** @type {?string} */
    lockoutEnd;
    /** @type {boolean} */
    lockoutEnabled;
    /** @type {number} */
    accessFailedCount;
}
export class IdentityUser extends IdentityUser_1 {
    /** @param {{id?:TKey,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:boolean,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:boolean,twoFactorEnabled?:boolean,lockoutEnd?:string,lockoutEnabled?:boolean,accessFailedCount?:number}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
}
export class ApplicationUser extends IdentityUser {
    /** @param {{firstName?:string,lastName?:string,displayName?:string,profileUrl?:string,refreshToken?:string,refreshTokenExpiry?:string,id?:TKey,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:boolean,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:boolean,twoFactorEnabled?:boolean,lockoutEnd?:string,lockoutEnabled?:boolean,accessFailedCount?:number}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?string} */
    firstName;
    /** @type {?string} */
    lastName;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    profileUrl;
    /** @type {?string} */
    refreshToken;
    /** @type {?string} */
    refreshTokenExpiry;
}
export class PhoneScreen extends AuditBase {
    /** @param {{id?:number,applicationUserId?:string,applicationUser?:ApplicationUser,jobApplicationId?:number,applicationStatus?:JobApplicationStatus,notes?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    applicationUserId;
    /** @type {ApplicationUser} */
    applicationUser;
    /** @type {number} */
    jobApplicationId;
    /** @type {?JobApplicationStatus} */
    applicationStatus;
    /** @type {string} */
    notes;
}
export class Interview extends AuditBase {
    /** @param {{id?:number,bookingTime?:string,jobApplicationId?:number,applicationUserId?:string,applicationUser?:ApplicationUser,applicationStatus?:JobApplicationStatus,notes?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    bookingTime;
    /** @type {number} */
    jobApplicationId;
    /** @type {string} */
    applicationUserId;
    /** @type {ApplicationUser} */
    applicationUser;
    /** @type {?JobApplicationStatus} */
    applicationStatus;
    /** @type {string} */
    notes;
}
export class JobOffer extends AuditBase {
    /** @param {{id?:number,salaryOffer?:number,jobApplicationId?:number,applicationUserId?:string,applicationUser?:ApplicationUser,notes?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    salaryOffer;
    /** @type {number} */
    jobApplicationId;
    /** @type {string} */
    applicationUserId;
    /** @type {ApplicationUser} */
    applicationUser;
    /** @type {string} */
    notes;
}
export class SubType {
    /** @param {{id?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
}
/** @typedef {'Transparent'|'Red'|'Green'|'Blue'} */
export var Colors;
(function (Colors) {
    Colors["Transparent"] = "Transparent"
    Colors["Red"] = "Red"
    Colors["Green"] = "Green"
    Colors["Blue"] = "Blue"
})(Colors || (Colors = {}));
export class Attachment {
    /** @param {{fileName?:string,filePath?:string,contentType?:string,contentLength?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    fileName;
    /** @type {string} */
    filePath;
    /** @type {string} */
    contentType;
    /** @type {number} */
    contentLength;
}
export class BillingItem {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
}
export class PagedRequest {
    /** @param {{page?:number,pageSize?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    page;
    /** @type {number} */
    pageSize;
}
export class PagedAndOrderedRequest extends PagedRequest {
    /** @param {{orderBy?:string,page?:number,pageSize?:number}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /**
     * @type {string}
     * @description Comma- or semicolon separated list of fields to sort by. To change sort order add a '-' in front of the field */
    orderBy;
}
export class ToolCall {
    /** @param {{id?:string,type?:string,function?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The ID of the tool call. */
    id;
    /**
     * @type {string}
     * @description The type of the tool. Currently, only `function` is supported. */
    type;
    /**
     * @type {string}
     * @description The function that the model called. */
    function;
}
export class OpenAiMessage {
    /** @param {{content?:string,role?:string,name?:string,tool_calls?:ToolCall[],tool_call_id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The contents of the message. */
    content;
    /**
     * @type {string}
     * @description The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`. */
    role;
    /**
     * @type {?string}
     * @description An optional name for the participant. Provides the model information to differentiate between participants of the same role. */
    name;
    /**
     * @type {?ToolCall[]}
     * @description The tool calls generated by the model, such as function calls. */
    tool_calls;
    /**
     * @type {?string}
     * @description Tool call that this message is responding to. */
    tool_call_id;
}
/** @typedef {'text'|'json_object'} */
export var ResponseFormat;
(function (ResponseFormat) {
    ResponseFormat["Text"] = "text"
    ResponseFormat["JsonObject"] = "json_object"
})(ResponseFormat || (ResponseFormat = {}));
export class OpenAiResponseFormat {
    /** @param {{response_format?:ResponseFormat}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {ResponseFormat}
     * @description An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106. */
    response_format;
}
/** @typedef {'function'} */
export var OpenAiToolType;
(function (OpenAiToolType) {
    OpenAiToolType["Function"] = "function"
})(OpenAiToolType || (OpenAiToolType = {}));
export class OpenAiTools {
    /** @param {{type?:OpenAiToolType}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {OpenAiToolType}
     * @description The type of the tool. Currently, only function is supported. */
    type;
}
export class OpenAiChat {
    /** @param {{messages?:OpenAiMessage[],model?:string,frequency_penalty?:number,logit_bias?:{ [index:number]: number; },logprobs?:boolean,top_logprobs?:number,max_tokens?:number,n?:number,presence_penalty?:number,response_format?:OpenAiResponseFormat,seed?:number,stop?:string[],stream?:boolean,temperature?:number,top_p?:number,tools?:OpenAiTools[],user?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {OpenAiMessage[]}
     * @description A list of messages comprising the conversation so far. */
    messages = [];
    /**
     * @type {string}
     * @description ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API */
    model;
    /**
     * @type {?number}
     * @description Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim. */
    frequency_penalty;
    /**
     * @type {?{ [index:number]: number; }}
     * @description Modify the likelihood of specified tokens appearing in the completion. */
    logit_bias;
    /**
     * @type {?boolean}
     * @description Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message. */
    logprobs;
    /**
     * @type {?number}
     * @description An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used. */
    top_logprobs;
    /**
     * @type {?number}
     * @description The maximum number of tokens that can be generated in the chat completion. */
    max_tokens;
    /**
     * @type {?number}
     * @description How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs. */
    n;
    /**
     * @type {?number}
     * @description Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics. */
    presence_penalty;
    /**
     * @type {?OpenAiResponseFormat}
     * @description An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON. */
    response_format;
    /**
     * @type {?number}
     * @description This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend. */
    seed;
    /**
     * @type {?string[]}
     * @description Up to 4 sequences where the API will stop generating further tokens. */
    stop;
    /**
     * @type {?boolean}
     * @description If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message. */
    stream;
    /**
     * @type {?number}
     * @description What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic. */
    temperature;
    /**
     * @type {?number}
     * @description An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. */
    top_p;
    /**
     * @type {?OpenAiTools[]}
     * @description A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported. */
    tools;
    /**
     * @type {?string}
     * @description A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse. */
    user;
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
export class Poco {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
}
/** @typedef {'Value1'|'Value2'|'Value3'} */
export var EnumType;
(function (EnumType) {
    EnumType["Value1"] = "Value1"
    EnumType["Value2"] = "Value2"
    EnumType["Value3"] = "Value3"
})(EnumType || (EnumType = {}));
/** @typedef {number} */
export var EnumTypeFlags;
(function (EnumTypeFlags) {
    EnumTypeFlags[EnumTypeFlags["Value1"] = 0] = "Value1"
    EnumTypeFlags[EnumTypeFlags["Value2"] = 1] = "Value2"
    EnumTypeFlags[EnumTypeFlags["Value3"] = 2] = "Value3"
})(EnumTypeFlags || (EnumTypeFlags = {}));
/** @typedef {'None'|'Member 1'|'Value2'} */
export var EnumWithValues;
(function (EnumWithValues) {
    EnumWithValues["None"] = "None"
    EnumWithValues["Value1"] = "Member 1"
    EnumWithValues["Value2"] = "Value2"
})(EnumWithValues || (EnumWithValues = {}));
/** @typedef {number} */
export var EnumFlags;
(function (EnumFlags) {
    EnumFlags[EnumFlags["Value0"] = 0] = "Value0"
    EnumFlags[EnumFlags["Value1"] = 1] = "Value1"
    EnumFlags[EnumFlags["Value2"] = 2] = "Value2"
    EnumFlags[EnumFlags["Value3"] = 4] = "Value3"
    EnumFlags[EnumFlags["Value123"] = 7] = "Value123"
})(EnumFlags || (EnumFlags = {}));
/** @typedef {number} */
export var EnumAsInt;
(function (EnumAsInt) {
    EnumAsInt[EnumAsInt["Value1"] = 1000] = "Value1"
    EnumAsInt[EnumAsInt["Value2"] = 2000] = "Value2"
    EnumAsInt[EnumAsInt["Value3"] = 3000] = "Value3"
})(EnumAsInt || (EnumAsInt = {}));
/** @typedef {'lower'|'UPPER'|'PascalCase'|'camelCase'|'camelUPPER'|'PascalUPPER'} */
export var EnumStyle;
(function (EnumStyle) {
    EnumStyle["lower"] = "lower"
    EnumStyle["UPPER"] = "UPPER"
    EnumStyle["PascalCase"] = "PascalCase"
    EnumStyle["camelCase"] = "camelCase"
    EnumStyle["camelUPPER"] = "camelUPPER"
    EnumStyle["PascalUPPER"] = "PascalUPPER"
})(EnumStyle || (EnumStyle = {}));
/** @typedef {'lower'|'UPPER'|'PascalCase'|'camelCase'|'camelUPPER'|'PascalUPPER'} */
export var EnumStyleMembers;
(function (EnumStyleMembers) {
    EnumStyleMembers["Lower"] = "lower"
    EnumStyleMembers["Upper"] = "UPPER"
    EnumStyleMembers["PascalCase"] = "PascalCase"
    EnumStyleMembers["CamelCase"] = "camelCase"
    EnumStyleMembers["CamelUpper"] = "camelUPPER"
    EnumStyleMembers["PascalUpper"] = "PascalUPPER"
})(EnumStyleMembers || (EnumStyleMembers = {}));
export class AllTypesBase {
    /** @param {{id?:number,nullableId?:number,byte?:number,short?:number,int?:number,long?:number,uShort?:number,uInt?:number,uLong?:number,float?:number,double?:number,decimal?:number,string?:string,dateTime?:string,timeSpan?:string,dateTimeOffset?:string,guid?:string,char?:string,keyValuePair?:KeyValuePair<string, string>,nullableDateTime?:string,nullableTimeSpan?:string,stringList?:string[],stringArray?:string[],stringMap?:{ [index:string]: string; },intStringMap?:{ [index:number]: string; },subType?:SubType}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?number} */
    nullableId;
    /** @type {number} */
    byte;
    /** @type {number} */
    short;
    /** @type {number} */
    int;
    /** @type {number} */
    long;
    /** @type {number} */
    uShort;
    /** @type {number} */
    uInt;
    /** @type {number} */
    uLong;
    /** @type {number} */
    float;
    /** @type {number} */
    double;
    /** @type {number} */
    decimal;
    /** @type {string} */
    string;
    /** @type {string} */
    dateTime;
    /** @type {string} */
    timeSpan;
    /** @type {string} */
    dateTimeOffset;
    /** @type {string} */
    guid;
    /** @type {string} */
    char;
    /** @type {KeyValuePair<string, string>} */
    keyValuePair;
    /** @type {?string} */
    nullableDateTime;
    /** @type {?string} */
    nullableTimeSpan;
    /** @type {string[]} */
    stringList = [];
    /** @type {string[]} */
    stringArray = [];
    /** @type {{ [index:string]: string; }} */
    stringMap = {};
    /** @type {{ [index:number]: string; }} */
    intStringMap = {};
    /** @type {SubType} */
    subType;
}
/** @typedef T {any} */
export class HelloBase_1 {
    /** @param {{items?:T[],counts?:number[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {T[]} */
    items = [];
    /** @type {number[]} */
    counts = [];
}
export class HelloBase {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
}
export class QueryBase {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
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
    /** @type {{ [index:string]: string; }} */
    meta;
}
/** @typedef T {any} */
export class QueryDb extends QueryBase {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
}
export class Albums {
    /** @param {{albumId?:number,title?:string,artistId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    albumId;
    /** @type {string} */
    title;
    /** @type {number} */
    artistId;
}
export class Artists {
    /** @param {{artistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    artistId;
    /** @type {string} */
    name;
}
export class Customers {
    /** @param {{customerId?:number,firstName?:string,lastName?:string,company?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string,supportRepId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    customerId;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    company;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    country;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    email;
    /** @type {?number} */
    supportRepId;
}
export class Employees {
    /** @param {{employeeId?:number,lastName?:string,firstName?:string,title?:string,reportsTo?:number,birthDate?:string,hireDate?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    employeeId;
    /** @type {string} */
    lastName;
    /** @type {string} */
    firstName;
    /** @type {string} */
    title;
    /** @type {?number} */
    reportsTo;
    /** @type {?string} */
    birthDate;
    /** @type {?string} */
    hireDate;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    country;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    email;
}
export class Genres {
    /** @param {{genreId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    genreId;
    /** @type {string} */
    name;
}
export class InvoiceItems {
    /** @param {{invoiceLineId?:number,invoiceId?:number,trackId?:number,unitPrice?:number,quantity?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    invoiceLineId;
    /** @type {number} */
    invoiceId;
    /** @type {number} */
    trackId;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    quantity;
}
export class Invoices {
    /** @param {{invoiceId?:number,customerId?:number,invoiceDate?:string,billingAddress?:string,billingCity?:string,billingState?:string,billingCountry?:string,billingPostalCode?:string,total?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    invoiceId;
    /** @type {number} */
    customerId;
    /** @type {string} */
    invoiceDate;
    /** @type {string} */
    billingAddress;
    /** @type {string} */
    billingCity;
    /** @type {string} */
    billingState;
    /** @type {string} */
    billingCountry;
    /** @type {string} */
    billingPostalCode;
    /** @type {number} */
    total;
}
export class MediaTypes {
    /** @param {{mediaTypeId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    mediaTypeId;
    /** @type {string} */
    name;
}
export class Playlists {
    /** @param {{playlistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    playlistId;
    /** @type {string} */
    name;
}
export class Tracks {
    /** @param {{trackId?:number,name?:string,albumId?:number,mediaTypeId?:number,genreId?:number,composer?:string,milliseconds?:number,bytes?:number,unitPrice?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    trackId;
    /** @type {string} */
    name;
    /** @type {?number} */
    albumId;
    /** @type {number} */
    mediaTypeId;
    /** @type {?number} */
    genreId;
    /** @type {string} */
    composer;
    /** @type {number} */
    milliseconds;
    /** @type {?number} */
    bytes;
    /** @type {number} */
    unitPrice;
}
export class JobApplicationAttachment {
    /** @param {{id?:number,jobApplicationId?:number,fileName?:string,filePath?:string,contentType?:string,contentLength?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    jobApplicationId;
    /** @type {string} */
    fileName;
    /** @type {string} */
    filePath;
    /** @type {string} */
    contentType;
    /** @type {number} */
    contentLength;
}
/** @typedef {'Single'|'Double'|'Queen'|'Twin'|'Suite'} */
export var RoomType;
(function (RoomType) {
    RoomType["Single"] = "Single"
    RoomType["Double"] = "Double"
    RoomType["Queen"] = "Queen"
    RoomType["Twin"] = "Twin"
    RoomType["Suite"] = "Suite"
})(RoomType || (RoomType = {}));
export class Coupon {
    /** @param {{id?:string,description?:string,discount?:number,expiryDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    description;
    /** @type {number} */
    discount;
    /** @type {string} */
    expiryDate;
}
export class Address {
    /** @param {{id?:number,addressText?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    addressText;
}
export class User {
    /** @param {{id?:string,userName?:string,firstName?:string,lastName?:string,displayName?:string,profileUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    /** @type {?string} */
    userName;
    /** @type {?string} */
    firstName;
    /** @type {?string} */
    lastName;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    profileUrl;
}
export class Booking extends AuditBase {
    /** @param {{id?:number,name?:string,roomType?:RoomType,roomNumber?:number,bookingStartDate?:string,bookingEndDate?:string,cost?:number,couponId?:string,discount?:Coupon,notes?:string,cancelled?:boolean,permanentAddressId?:number,permanentAddress?:Address,postalAddressId?:number,postalAddress?:Address,employee?:User,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {RoomType} */
    roomType;
    /** @type {number} */
    roomNumber;
    /** @type {string} */
    bookingStartDate;
    /** @type {?string} */
    bookingEndDate;
    /** @type {number} */
    cost;
    /** @type {?string} */
    couponId;
    /** @type {Coupon} */
    discount;
    /** @type {?string} */
    notes;
    /** @type {?boolean} */
    cancelled;
    /** @type {?number} */
    permanentAddressId;
    /** @type {?Address} */
    permanentAddress;
    /** @type {?number} */
    postalAddressId;
    /** @type {?Address} */
    postalAddress;
    /** @type {?User} */
    employee;
}
/** @typedef {'Public'|'Team'|'Private'} */
export var FileAccessType;
(function (FileAccessType) {
    FileAccessType["Public"] = "Public"
    FileAccessType["Team"] = "Team"
    FileAccessType["Private"] = "Private"
})(FileAccessType || (FileAccessType = {}));
export class FileSystemFile {
    /** @param {{id?:number,fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    fileName;
    /** @type {string} */
    filePath;
    /** @type {string} */
    contentType;
    /** @type {number} */
    contentLength;
    /** @type {number} */
    fileSystemItemId;
}
/** @typedef {'Home'|'Mobile'|'Work'} */
export var PhoneKind;
(function (PhoneKind) {
    PhoneKind["Home"] = "Home"
    PhoneKind["Mobile"] = "Mobile"
    PhoneKind["Work"] = "Work"
})(PhoneKind || (PhoneKind = {}));
export class Phone {
    /** @param {{kind?:PhoneKind,number?:string,ext?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {PhoneKind} */
    kind;
    /** @type {string} */
    number;
    /** @type {string} */
    ext;
}
export class PlayerGameItem {
    /** @param {{id?:number,playerId?:number,gameItemName?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    playerId;
    /** @type {string} */
    gameItemName;
}
/** @typedef {'Leader'|'Player'|'NonPlayer'} */
export var PlayerRole;
(function (PlayerRole) {
    PlayerRole["Leader"] = "Leader"
    PlayerRole["Player"] = "Player"
    PlayerRole["NonPlayer"] = "NonPlayer"
})(PlayerRole || (PlayerRole = {}));
/** @typedef {number} */
export var PlayerRegion;
(function (PlayerRegion) {
    PlayerRegion[PlayerRegion["Africa"] = 1] = "Africa"
    PlayerRegion[PlayerRegion["Americas"] = 2] = "Americas"
    PlayerRegion[PlayerRegion["Asia"] = 3] = "Asia"
    PlayerRegion[PlayerRegion["Australasia"] = 4] = "Australasia"
    PlayerRegion[PlayerRegion["Europe"] = 5] = "Europe"
})(PlayerRegion || (PlayerRegion = {}));
export class Profile extends AuditBase {
    /** @param {{id?:number,role?:PlayerRole,region?:PlayerRegion,username?:string,highScore?:number,gamesPlayed?:number,energy?:number,profileUrl?:string,coverUrl?:string,meta?:{ [index:string]: string; },createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {PlayerRole} */
    role;
    /** @type {PlayerRegion} */
    region;
    /** @type {?string} */
    username;
    /** @type {number} */
    highScore;
    /** @type {number} */
    gamesPlayed;
    /** @type {number} */
    energy;
    /** @type {?string} */
    profileUrl;
    /** @type {?string} */
    coverUrl;
    /** @type {?{ [index:string]: string; }} */
    meta;
}
export class Player extends AuditBase {
    /** @param {{id?:number,firstName?:string,lastName?:string,email?:string,phoneNumbers?:Phone[],gameItems?:PlayerGameItem[],profile?:Profile,profileId?:number,savedLevelId?:string,rowVersion?:number,capital?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    email;
    /** @type {Phone[]} */
    phoneNumbers = [];
    /** @type {PlayerGameItem[]} */
    gameItems = [];
    /** @type {Profile} */
    profile;
    /** @type {number} */
    profileId;
    /** @type {string} */
    savedLevelId;
    /** @type {number} */
    rowVersion;
    /** @type {string} */
    capital;
}
export class GameItem extends AuditBase {
    /** @param {{name?:string,imageUrl?:string,description?:string,dateAdded?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    imageUrl;
    /** @type {?string} */
    description;
    /** @type {string} */
    dateAdded;
}
export class Level {
    /** @param {{id?:string,data?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    data = [];
}
export class ApiKey {
    /** @param {{id?:number,key?:string,name?:string,userId?:string,userName?:string,visibleKey?:string,createdDate?:string,expiryDate?:string,cancelledDate?:string,lastUsedDate?:string,scopes?:string,features?:string,restrictTo?:string,environment?:string,notes?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    key;
    /** @type {string} */
    name;
    /** @type {string} */
    userId;
    /** @type {string} */
    userName;
    /** @type {string} */
    visibleKey;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    expiryDate;
    /** @type {string} */
    cancelledDate;
    /** @type {string} */
    lastUsedDate;
    /** @type {string} */
    scopes;
    /** @type {string} */
    features;
    /** @type {string} */
    restrictTo;
    /** @type {string} */
    environment;
    /** @type {string} */
    notes;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
}
export class AspNetRoleClaims {
    /** @param {{id?:number,roleId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    roleId;
    /** @type {string} */
    claimType;
    /** @type {string} */
    claimValue;
}
export class AspNetRoles {
    /** @param {{id?:string,name?:string,normalizedName?:string,concurrencyStamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    normalizedName;
    /** @type {string} */
    concurrencyStamp;
}
export class AspNetUserClaims {
    /** @param {{id?:number,userId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    userId;
    /** @type {string} */
    claimType;
    /** @type {string} */
    claimValue;
}
export class AspNetUsers {
    /** @param {{id?:string,firstName?:string,lastName?:string,displayName?:string,profileUrl?:string,refreshToken?:string,refreshTokenExpiry?:string,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:number,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:number,twoFactorEnabled?:number,lockoutEnd?:string,lockoutEnabled?:number,accessFailedCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    displayName;
    /** @type {string} */
    profileUrl;
    /** @type {string} */
    refreshToken;
    /** @type {string} */
    refreshTokenExpiry;
    /** @type {string} */
    userName;
    /** @type {string} */
    normalizedUserName;
    /** @type {string} */
    email;
    /** @type {string} */
    normalizedEmail;
    /** @type {number} */
    emailConfirmed;
    /** @type {string} */
    passwordHash;
    /** @type {string} */
    securityStamp;
    /** @type {string} */
    concurrencyStamp;
    /** @type {string} */
    phoneNumber;
    /** @type {number} */
    phoneNumberConfirmed;
    /** @type {number} */
    twoFactorEnabled;
    /** @type {string} */
    lockoutEnd;
    /** @type {number} */
    lockoutEnabled;
    /** @type {number} */
    accessFailedCount;
}
export class CrudEvent {
    /** @param {{id?:number,eventType?:string,model?:string,modelId?:string,eventDate?:string,rowsUpdated?:number,requestType?:string,requestBody?:string,userAuthId?:string,userAuthName?:string,remoteIp?:string,urn?:string,refId?:number,refIdStr?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    eventType;
    /** @type {string} */
    model;
    /** @type {string} */
    modelId;
    /** @type {string} */
    eventDate;
    /** @type {?number} */
    rowsUpdated;
    /** @type {string} */
    requestType;
    /** @type {string} */
    requestBody;
    /** @type {string} */
    userAuthId;
    /** @type {string} */
    userAuthName;
    /** @type {string} */
    remoteIp;
    /** @type {string} */
    urn;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {{ [index:string]: string; }} */
    meta;
}
export class EFMigrationsHistory {
    /** @param {{migrationId?:string,productVersion?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    migrationId;
    /** @type {string} */
    productVersion;
}
export class Migration {
    /** @param {{id?:number,name?:string,description?:string,createdDate?:string,completedDate?:string,connectionString?:string,namedConnection?:string,log?:string,errorCode?:string,errorMessage?:string,errorStackTrace?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    description;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    completedDate;
    /** @type {string} */
    connectionString;
    /** @type {string} */
    namedConnection;
    /** @type {string} */
    log;
    /** @type {string} */
    errorCode;
    /** @type {string} */
    errorMessage;
    /** @type {string} */
    errorStackTrace;
    /** @type {string} */
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
/** @typedef {'FullTime'|'PartTime'|'Casual'|'Contract'} */
export var EmploymentType;
(function (EmploymentType) {
    EmploymentType["FullTime"] = "FullTime"
    EmploymentType["PartTime"] = "PartTime"
    EmploymentType["Casual"] = "Casual"
    EmploymentType["Contract"] = "Contract"
})(EmploymentType || (EmploymentType = {}));
export class Job extends AuditBase {
    /** @param {{id?:number,title?:string,employmentType?:EmploymentType,company?:string,location?:string,salaryRangeLower?:number,salaryRangeUpper?:number,description?:string,applications?:JobApplication[],closing?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    title;
    /** @type {EmploymentType} */
    employmentType;
    /** @type {string} */
    company;
    /** @type {string} */
    location;
    /** @type {number} */
    salaryRangeLower;
    /** @type {number} */
    salaryRangeUpper;
    /** @type {string} */
    description;
    /** @type {JobApplication[]} */
    applications = [];
    /** @type {string} */
    closing;
}
export class Contact extends AuditBase {
    /** @param {{id?:number,displayName?:string,profileUrl?:string,firstName?:string,lastName?:string,salaryExpectation?:number,jobType?:string,availabilityWeeks?:number,preferredWorkType?:EmploymentType,preferredLocation?:string,email?:string,phone?:string,about?:string,applications?:JobApplication[],createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    displayName;
    /** @type {string} */
    profileUrl;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {?number} */
    salaryExpectation;
    /** @type {string} */
    jobType;
    /** @type {number} */
    availabilityWeeks;
    /** @type {EmploymentType} */
    preferredWorkType;
    /** @type {string} */
    preferredLocation;
    /** @type {string} */
    email;
    /** @type {string} */
    phone;
    /** @type {string} */
    about;
    /** @type {JobApplication[]} */
    applications = [];
}
export class JobApplicationComment extends AuditBase {
    /** @param {{id?:number,applicationUserId?:string,applicationUser?:ApplicationUser,jobApplicationId?:number,comment?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    applicationUserId;
    /** @type {ApplicationUser} */
    applicationUser;
    /** @type {number} */
    jobApplicationId;
    /** @type {string} */
    comment;
}
export class JobApplicationEvent extends AuditBase {
    /** @param {{id?:number,jobApplicationId?:number,applicationUserId?:string,applicationUser?:ApplicationUser,description?:string,status?:JobApplicationStatus,eventDate?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    jobApplicationId;
    /** @type {string} */
    applicationUserId;
    /** @type {ApplicationUser} */
    applicationUser;
    /** @type {string} */
    description;
    /** @type {?JobApplicationStatus} */
    status;
    /** @type {string} */
    eventDate;
}
export class JobApplication extends AuditBase {
    /** @param {{id?:number,jobId?:number,contactId?:number,position?:Job,applicant?:Contact,comments?:JobApplicationComment[],appliedDate?:string,applicationStatus?:JobApplicationStatus,attachments?:JobApplicationAttachment[],events?:JobApplicationEvent[],phoneScreen?:PhoneScreen,interview?:Interview,jobOffer?:JobOffer,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    jobId;
    /** @type {number} */
    contactId;
    /** @type {Job} */
    position;
    /** @type {Contact} */
    applicant;
    /** @type {JobApplicationComment[]} */
    comments = [];
    /** @type {string} */
    appliedDate;
    /** @type {JobApplicationStatus} */
    applicationStatus;
    /** @type {JobApplicationAttachment[]} */
    attachments = [];
    /** @type {JobApplicationEvent[]} */
    events = [];
    /** @type {PhoneScreen} */
    phoneScreen;
    /** @type {Interview} */
    interview;
    /** @type {JobOffer} */
    jobOffer;
}
export class FileSystemItem {
    /** @param {{id?:number,fileAccessType?:FileAccessType,file?:FileSystemFile,applicationUserId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?FileAccessType} */
    fileAccessType;
    /** @type {FileSystemFile} */
    file;
    /** @type {string} */
    applicationUserId;
}
export class Todo {
    /** @param {{id?:number,text?:string,isFinished?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    text;
    /** @type {boolean} */
    isFinished;
}
export class ResponseError {
    /** @param {{errorCode?:string,fieldName?:string,message?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    errorCode;
    /** @type {string} */
    fieldName;
    /** @type {string} */
    message;
    /** @type {{ [index:string]: string; }} */
    meta;
}
export class ResponseStatus {
    /** @param {{errorCode?:string,message?:string,stackTrace?:string,errors?:ResponseError[],meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    errorCode;
    /** @type {string} */
    message;
    /** @type {string} */
    stackTrace;
    /** @type {ResponseError[]} */
    errors;
    /** @type {{ [index:string]: string; }} */
    meta;
}
export class BackgroundJobRef {
    /** @param {{id?:number,refId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    refId;
}
export class Item {
    /** @param {{name?:string,description?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    name;
    /** @type {?string} */
    description;
}
/** @typedef T {any} */
export class QueryResponseAlt {
    /** @param {{offset?:number,total?:number,results?:T[],meta?:{ [index:string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    offset;
    /** @type {number} */
    total;
    /** @type {T[]} */
    results = [];
    /** @type {{ [index:string]: string; }} */
    meta = {};
    /** @type {ResponseStatus} */
    responseStatus;
}
export class Forecast {
    /** @param {{date?:string,temperatureC?:number,summary?:string,temperatureF?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    date;
    /** @type {number} */
    temperatureC;
    /** @type {?string} */
    summary;
    /** @type {number} */
    temperatureF;
}
/** @typedef T {any} */
export class ResponseBase {
    /** @param {{responseStatus?:ResponseStatus,result?:T,results?:T[],total?:number,skip?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ResponseStatus} */
    responseStatus;
    /**
     * @type {T}
     * @description This will be returned when there is a single result available. (e.g. get single object by id) */
    result;
    /**
     * @type {T[]}
     * @description This will be returned when there is a multiple results available (e.g. search or listing requests). */
    results = [];
    /**
     * @type {?number}
     * @description This will be returned when there is a multiple results available (e.g. search or listing requests). */
    total;
    /**
     * @type {?number}
     * @description This will be return the amount of skipped rows when paginating */
    skip;
}
export class DigitalPrescriptionDMDResponse {
    /** @param {{name?:string,productId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {number} */
    productId;
}
export class FooDto {
    /** @param {{id?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
}
/** @typedef T {any} */
export class PagedResult {
    /** @param {{page?:number,pageSize?:number,totalResults?:number,results?:T[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    page;
    /** @type {number} */
    pageSize;
    /** @type {number} */
    totalResults;
    /** @type {T[]} */
    results = [];
}
export class WorkerStats {
    /** @param {{name?:string,queued?:number,received?:number,completed?:number,retries?:number,failed?:number,runningJob?:number,runningTime?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {number} */
    queued;
    /** @type {number} */
    received;
    /** @type {number} */
    completed;
    /** @type {number} */
    retries;
    /** @type {number} */
    failed;
    /** @type {?number} */
    runningJob;
    /** @type {?string} */
    runningTime;
}
export class ChoiceMessage {
    /** @param {{content?:string,tool_calls?:ToolCall[],role?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The contents of the message. */
    content;
    /**
     * @type {?ToolCall[]}
     * @description The tool calls generated by the model, such as function calls. */
    tool_calls;
    /**
     * @type {string}
     * @description The role of the author of this message. */
    role;
}
export class Choice {
    /** @param {{finish_reason?:string,index?:number,message?:ChoiceMessage}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool */
    finish_reason;
    /**
     * @type {number}
     * @description The index of the choice in the list of choices. */
    index;
    /**
     * @type {ChoiceMessage}
     * @description A chat completion message generated by the model. */
    message;
}
export class OpenAiUsage {
    /** @param {{completion_tokens?:number,prompt_tokens?:number,total_tokens?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {number}
     * @description Number of tokens in the generated completion. */
    completion_tokens;
    /**
     * @type {number}
     * @description Number of tokens in the prompt. */
    prompt_tokens;
    /**
     * @type {number}
     * @description Total number of tokens used in the request (prompt + completion). */
    total_tokens;
}
export class ArtifactOutput {
    /** @param {{url?:string,fileName?:string,provider?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {?string}
     * @description URL to access the generated image */
    url;
    /**
     * @type {?string}
     * @description Filename of the generated image */
    fileName;
    /**
     * @type {?string}
     * @description Provider used for image generation */
    provider;
}
export class TextOutput {
    /** @param {{text?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {?string}
     * @description The generated text */
    text;
}
export class ListResult {
    /** @param {{result?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    result;
}
/** @typedef T {any} */
export class QueryResponse {
    /** @param {{offset?:number,total?:number,results?:T[],meta?:{ [index:string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    offset;
    /** @type {number} */
    total;
    /** @type {T[]} */
    results;
    /** @type {{ [index:string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class GetContactsResponse {
    /** @param {{results?:Contact[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Contact[]} */
    results = [];
    /** @type {ResponseStatus} */
    responseStatus;
}
export class TalentStatsResponse {
    /** @param {{totalJobs?:number,totalContacts?:number,avgSalaryExpectation?:number,avgSalaryLower?:number,avgSalaryUpper?:number,preferredRemotePercentage?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    totalJobs;
    /** @type {number} */
    totalContacts;
    /** @type {number} */
    avgSalaryExpectation;
    /** @type {number} */
    avgSalaryLower;
    /** @type {number} */
    avgSalaryUpper;
    /** @type {number} */
    preferredRemotePercentage;
}
export class QueueCheckUrlResponse {
    /** @param {{id?:number,refId?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    refId;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class QueueCheckUrlsResponse {
    /** @param {{jobRef?:BackgroundJobRef}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {BackgroundJobRef} */
    jobRef;
}
export class Items {
    /** @param {{results?:Item[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Item[]} */
    results = [];
}
export class EchoComplexTypes {
    /** @param {{subType?:SubType,subTypes?:SubType[],subTypeMap?:{ [index:string]: SubType; },stringMap?:{ [index:string]: string; },intStringMap?:{ [index:number]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {SubType} */
    subType;
    /** @type {SubType[]} */
    subTypes = [];
    /** @type {{ [index:string]: SubType; }} */
    subTypeMap = {};
    /** @type {{ [index:string]: string; }} */
    stringMap = {};
    /** @type {{ [index:number]: string; }} */
    intStringMap = {};
    getTypeName() { return 'EchoComplexTypes' }
    getMethod() { return 'POST' }
    createResponse() { return new EchoComplexTypes() }
}
export class EchoCollections {
    /** @param {{stringList?:string[],stringArray?:string[],stringMap?:{ [index:string]: string; },intStringMap?:{ [index:number]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    stringList = [];
    /** @type {string[]} */
    stringArray = [];
    /** @type {{ [index:string]: string; }} */
    stringMap = {};
    /** @type {{ [index:number]: string; }} */
    intStringMap = {};
    getTypeName() { return 'EchoCollections' }
    getMethod() { return 'POST' }
    createResponse() { return new EchoCollections() }
}
export class FormDataTest {
    /** @param {{hidden?:boolean,string?:string,int?:number,dateTime?:string,dateOnly?:string,timeSpan?:string,timeOnly?:string,password?:string,checkboxString?:string[],radioString?:string,radioColors?:Colors,checkboxColors?:Colors[],selectColors?:Colors,multiSelectColors?:Colors[],profileUrl?:string,attachments?:Attachment[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {boolean} */
    hidden;
    /** @type {?string} */
    string;
    /** @type {number} */
    int;
    /** @type {string} */
    dateTime;
    /** @type {string} */
    dateOnly;
    /** @type {string} */
    timeSpan;
    /** @type {string} */
    timeOnly;
    /** @type {?string} */
    password;
    /** @type {?string[]} */
    checkboxString;
    /** @type {?string} */
    radioString;
    /** @type {Colors} */
    radioColors;
    /** @type {?Colors[]} */
    checkboxColors;
    /** @type {Colors} */
    selectColors;
    /** @type {?Colors[]} */
    multiSelectColors;
    /** @type {?string} */
    profileUrl;
    /** @type {Attachment[]} */
    attachments = [];
    getTypeName() { return 'FormDataTest' }
    getMethod() { return 'POST' }
    createResponse() { return new FormDataTest() }
}
export class ComboBoxExamples {
    /** @param {{singleClientValues?:string,multipleClientValues?:string[],singleServerValues?:string,multipleServerValues?:string[],singleServerEntries?:string,multipleServerEntries?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    singleClientValues;
    /** @type {?string[]} */
    multipleClientValues;
    /** @type {?string} */
    singleServerValues;
    /** @type {?string[]} */
    multipleServerValues;
    /** @type {?string} */
    singleServerEntries;
    /** @type {?string[]} */
    multipleServerEntries;
    getTypeName() { return 'ComboBoxExamples' }
    getMethod() { return 'POST' }
    createResponse() { return new ComboBoxExamples() }
}
export class SecuredResponse {
    /** @param {{result?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    result;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class CreateRefreshJwtResponse {
    /** @param {{token?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    token;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class EmptyResponse {
    /** @param {{responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ResponseStatus} */
    responseStatus;
}
export class Movie {
    /** @param {{movieID?:string,movieNo?:number,name?:string,description?:string,movieRef?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    movieID;
    /** @type {number} */
    movieNo;
    /** @type {?string} */
    name;
    /** @type {?string} */
    description;
    /** @type {?string} */
    movieRef;
}
export class HelloResponse {
    /** @param {{result?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    result;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class GetWorkerStatsResponse {
    /** @param {{results?:WorkerStats[],queueCounts?:{ [index:string]: number; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {WorkerStats[]} */
    results = [];
    /** @type {{ [index:string]: number; }} */
    queueCounts = {};
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class OpenAiChatResponse {
    /** @param {{id?:string,choices?:Choice[],created?:number,model?:string,system_fingerprint?:string,object?:string,usage?:OpenAiUsage,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description A unique identifier for the chat completion. */
    id;
    /**
     * @type {Choice[]}
     * @description A list of chat completion choices. Can be more than one if n is greater than 1. */
    choices = [];
    /**
     * @type {number}
     * @description The Unix timestamp (in seconds) of when the chat completion was created. */
    created;
    /**
     * @type {string}
     * @description The model used for the chat completion. */
    model;
    /**
     * @type {string}
     * @description This fingerprint represents the backend configuration that the model runs with. */
    system_fingerprint;
    /**
     * @type {string}
     * @description The object type, which is always chat.completion. */
    object;
    /**
     * @type {OpenAiUsage}
     * @description Usage statistics for the completion request. */
    usage;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class StringsResponse {
    /** @param {{results?:string[],meta?:{ [index:string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    results;
    /** @type {{ [index:string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class GenerationResponse {
    /** @param {{outputs?:ArtifactOutput[],textOutputs?:TextOutput[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {?ArtifactOutput[]}
     * @description List of generated outputs */
    outputs;
    /**
     * @type {?TextOutput[]}
     * @description List of generated text outputs */
    textOutputs;
    /**
     * @type {?ResponseStatus}
     * @description Detailed response status information */
    responseStatus;
}
export class SendVerbResponse {
    /** @param {{id?:number,pathInfo?:string,requestMethod?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    pathInfo;
    /** @type {string} */
    requestMethod;
}
export class TestAuthResponse {
    /** @param {{userId?:string,sessionId?:string,userName?:string,displayName?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userId;
    /** @type {string} */
    sessionId;
    /** @type {string} */
    userName;
    /** @type {string} */
    displayName;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class RequiresAdmin {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'RequiresAdmin' }
    getMethod() { return 'POST' }
    createResponse() { return new RequiresAdmin() }
}
export class AllTypes {
    /** @param {{id?:number,nullableId?:number,boolean?:boolean,byte?:number,short?:number,int?:number,long?:number,uShort?:number,uInt?:number,uLong?:number,float?:number,double?:number,decimal?:number,string?:string,dateTime?:string,timeSpan?:string,dateTimeOffset?:string,guid?:string,char?:string,keyValuePair?:KeyValuePair<string, string>,nullableDateTime?:string,nullableTimeSpan?:string,stringList?:string[],stringArray?:string[],stringMap?:{ [index:string]: string; },intStringMap?:{ [index:number]: string; },subType?:SubType,nullableBytes?:number[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?number} */
    nullableId;
    /** @type {boolean} */
    boolean;
    /** @type {number} */
    byte;
    /** @type {number} */
    short;
    /** @type {number} */
    int;
    /** @type {number} */
    long;
    /** @type {number} */
    uShort;
    /** @type {number} */
    uInt;
    /** @type {number} */
    uLong;
    /** @type {number} */
    float;
    /** @type {number} */
    double;
    /** @type {number} */
    decimal;
    /** @type {string} */
    string;
    /** @type {string} */
    dateTime;
    /** @type {string} */
    timeSpan;
    /** @type {string} */
    dateTimeOffset;
    /** @type {string} */
    guid;
    /** @type {string} */
    char;
    /** @type {KeyValuePair<string, string>} */
    keyValuePair;
    /** @type {?string} */
    nullableDateTime;
    /** @type {?string} */
    nullableTimeSpan;
    /** @type {string[]} */
    stringList = [];
    /** @type {string[]} */
    stringArray = [];
    /** @type {{ [index:string]: string; }} */
    stringMap = {};
    /** @type {{ [index:number]: string; }} */
    intStringMap = {};
    /** @type {SubType} */
    subType;
    /** @type {number[]} */
    nullableBytes = [];
    getTypeName() { return 'AllTypes' }
    getMethod() { return 'POST' }
    createResponse() { return new AllTypes() }
}
export class AllCollectionTypes {
    /** @param {{intArray?:number[],intList?:number[],stringArray?:string[],stringList?:string[],floatArray?:number[],doubleList?:number[],byteArray?:string,charArray?:string[],decimalList?:number[],pocoArray?:Poco[],pocoList?:Poco[],pocoLookup?:{ [index:string]: Poco[]; },pocoLookupMap?:{ [index:string]: { [index:string]: Poco; }[]; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number[]} */
    intArray = [];
    /** @type {number[]} */
    intList = [];
    /** @type {string[]} */
    stringArray = [];
    /** @type {string[]} */
    stringList = [];
    /** @type {number[]} */
    floatArray = [];
    /** @type {number[]} */
    doubleList = [];
    /** @type {string} */
    byteArray = [];
    /** @type {string[]} */
    charArray = [];
    /** @type {number[]} */
    decimalList = [];
    /** @type {Poco[]} */
    pocoArray = [];
    /** @type {Poco[]} */
    pocoList = [];
    /** @type {{ [index:string]: Poco[]; }} */
    pocoLookup = {};
    /** @type {{ [index:string]: { [index:string]: Poco; }[]; }} */
    pocoLookupMap = {};
    getTypeName() { return 'AllCollectionTypes' }
    getMethod() { return 'POST' }
    createResponse() { return new AllCollectionTypes() }
}
export class HelloAllTypesResponse {
    /** @param {{result?:string,allTypes?:AllTypes,allCollectionTypes?:AllCollectionTypes}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    result;
    /** @type {AllTypes} */
    allTypes;
    /** @type {AllCollectionTypes} */
    allCollectionTypes;
}
export class ThrowTypeResponse {
    /** @param {{responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ResponseStatus} */
    responseStatus;
}
export class ThrowValidationResponse {
    /** @param {{age?:number,required?:string,email?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    age;
    /** @type {string} */
    required;
    /** @type {string} */
    email;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AllNullableCollectionTypes {
    /** @param {{intArray?:number[],intList?:number[],stringArray?:string[],stringList?:string[],floatArray?:number[],doubleList?:number[],byteArray?:string,charArray?:string[],decimalList?:number[],pocoArray?:Poco[],pocoList?:Poco[],pocoLookup?:{ [index:string]: Poco[]; },pocoLookupMap?:{ [index:string]: { [index:string]: Poco; }[]; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number[]} */
    intArray;
    /** @type {?number[]} */
    intList;
    /** @type {?string[]} */
    stringArray;
    /** @type {?string[]} */
    stringList;
    /** @type {?number[]} */
    floatArray;
    /** @type {?number[]} */
    doubleList;
    /** @type {?string} */
    byteArray;
    /** @type {?string[]} */
    charArray;
    /** @type {?number[]} */
    decimalList;
    /** @type {?Poco[]} */
    pocoArray;
    /** @type {?Poco[]} */
    pocoList;
    /** @type {?{ [index:string]: Poco[]; }} */
    pocoLookup;
    /** @type {?{ [index:string]: { [index:string]: Poco; }[]; }} */
    pocoLookupMap;
    getTypeName() { return 'AllNullableCollectionTypes' }
    getMethod() { return 'POST' }
    createResponse() { return new AllNullableCollectionTypes() }
}
export class ProfileGenResponse {
    constructor(init) { Object.assign(this, init) }
}
export class EchoTypes {
    /** @param {{byte?:number,short?:number,int?:number,long?:number,uShort?:number,uInt?:number,uLong?:number,float?:number,double?:number,decimal?:number,string?:string,dateTime?:string,timeSpan?:string,dateTimeOffset?:string,guid?:string,char?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    byte;
    /** @type {number} */
    short;
    /** @type {number} */
    int;
    /** @type {number} */
    long;
    /** @type {number} */
    uShort;
    /** @type {number} */
    uInt;
    /** @type {number} */
    uLong;
    /** @type {number} */
    float;
    /** @type {number} */
    double;
    /** @type {number} */
    decimal;
    /** @type {string} */
    string;
    /** @type {string} */
    dateTime;
    /** @type {string} */
    timeSpan;
    /** @type {string} */
    dateTimeOffset;
    /** @type {string} */
    guid;
    /** @type {string} */
    char;
    getTypeName() { return 'EchoTypes' }
    getMethod() { return 'POST' }
    createResponse() { return new EchoTypes() }
}
export class SubAllTypes extends AllTypesBase {
    /** @param {{hierarchy?:number,id?:number,nullableId?:number,byte?:number,short?:number,int?:number,long?:number,uShort?:number,uInt?:number,uLong?:number,float?:number,double?:number,decimal?:number,string?:string,dateTime?:string,timeSpan?:string,dateTimeOffset?:string,guid?:string,char?:string,keyValuePair?:KeyValuePair<string, string>,nullableDateTime?:string,nullableTimeSpan?:string,stringList?:string[],stringArray?:string[],stringMap?:{ [index:string]: string; },intStringMap?:{ [index:number]: string; },subType?:SubType}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    hierarchy;
}
export class HelloWithGenericInheritance extends HelloBase_1 {
    /** @param {{result?:string,items?:T[],counts?:number[]}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    result;
    getTypeName() { return 'HelloWithGenericInheritance' }
    getMethod() { return 'POST' }
    createResponse() { return new HelloWithGenericInheritance() }
}
export class HelloPost extends HelloBase {
    /** @param {{id?:number}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'HelloPost' }
    getMethod() { return 'POST' }
    createResponse() { return new HelloPost() }
}
export class AuthenticateResponse {
    /** @param {{userId?:string,sessionId?:string,userName?:string,displayName?:string,referrerUrl?:string,bearerToken?:string,refreshToken?:string,refreshTokenExpiry?:string,profileUrl?:string,roles?:string[],permissions?:string[],authProvider?:string,responseStatus?:ResponseStatus,meta?:{ [index:string]: string; }}} [init] */
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
    /** @type {string} */
    authProvider;
    /** @type {ResponseStatus} */
    responseStatus;
    /** @type {{ [index:string]: string; }} */
    meta;
}
export class GetAccessTokenResponse {
    /** @param {{accessToken?:string,meta?:{ [index:string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    accessToken;
    /** @type {{ [index:string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class IdResponse {
    /** @param {{id?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class StoreContacts extends Array {
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'StoreContacts' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class GetContacts {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetContacts' }
    getMethod() { return 'GET' }
    createResponse() { return new GetContactsResponse() }
}
export class CreatePhoneScreen {
    /** @param {{jobApplicationId?:number,appUserId?:number,applicationStatus?:JobApplicationStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    jobApplicationId;
    /** @type {number} */
    appUserId;
    /** @type {JobApplicationStatus} */
    applicationStatus;
    getTypeName() { return 'CreatePhoneScreen' }
    getMethod() { return 'POST' }
    createResponse() { return new PhoneScreen() }
}
export class UpdatePhoneScreen {
    /** @param {{id?:number,jobApplicationId?:number,notes?:string,applicationStatus?:JobApplicationStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    /** @type {?string} */
    notes;
    /** @type {?JobApplicationStatus} */
    applicationStatus;
    getTypeName() { return 'UpdatePhoneScreen' }
    getMethod() { return 'PATCH' }
    createResponse() { return new PhoneScreen() }
}
export class CreateInterview {
    /** @param {{bookingTime?:string,jobApplicationId?:number,appUserId?:number,applicationStatus?:JobApplicationStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    bookingTime;
    /** @type {number} */
    jobApplicationId;
    /** @type {number} */
    appUserId;
    /** @type {JobApplicationStatus} */
    applicationStatus;
    getTypeName() { return 'CreateInterview' }
    getMethod() { return 'POST' }
    createResponse() { return new Interview() }
}
export class UpdateInterview {
    /** @param {{id?:number,jobApplicationId?:number,notes?:string,applicationStatus?:JobApplicationStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    /** @type {?string} */
    notes;
    /** @type {?JobApplicationStatus} */
    applicationStatus;
    getTypeName() { return 'UpdateInterview' }
    getMethod() { return 'PATCH' }
    createResponse() { return new Interview() }
}
export class CreateJobOffer {
    /** @param {{salaryOffer?:number,jobApplicationId?:number,applicationStatus?:JobApplicationStatus,notes?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    salaryOffer;
    /** @type {number} */
    jobApplicationId;
    /** @type {JobApplicationStatus} */
    applicationStatus;
    /** @type {string} */
    notes;
    getTypeName() { return 'CreateJobOffer' }
    getMethod() { return 'POST' }
    createResponse() { return new JobOffer() }
}
export class TalentStats {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'TalentStats' }
    getMethod() { return 'GET' }
    createResponse() { return new TalentStatsResponse() }
}
export class QueueCheckUrl {
    /** @param {{url?:string,refId?:string,parentId?:number,worker?:string,runAfter?:string,callback?:string,dependsOn?:number,userId?:string,retryLimit?:number,replyTo?:string,tag?:string,batchId?:string,createdBy?:string,timeoutSecs?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    url;
    /**
     * @type {?string}
     * @description Specify a user-defined UUID for the Job */
    refId;
    /**
     * @type {?number}
     * @description Maintain a Reference to a parent Job */
    parentId;
    /**
     * @type {?string}
     * @description Named Worker Thread to execute Job on */
    worker;
    /**
     * @type {?string}
     * @description Only run Job after date */
    runAfter;
    /**
     * @type {?string}
     * @description Command to Execute after successful completion of Job */
    callback;
    /**
     * @type {?number}
     * @description Only execute job after successful completion of Parent Job */
    dependsOn;
    /**
     * @type {?string}
     * @description The ASP .NET Identity Auth User Id to populate the IRequest Context ClaimsPrincipal and User Session */
    userId;
    /**
     * @type {?number}
     * @description How many times to attempt to retry Job on failure, default 2 */
    retryLimit;
    /**
     * @type {?string}
     * @description Maintain a reference to a callback URL */
    replyTo;
    /**
     * @type {?string}
     * @description Associate Job with a tag group */
    tag;
    /** @type {?string} */
    batchId;
    /** @type {?string} */
    createdBy;
    /** @type {?number} */
    timeoutSecs;
    getTypeName() { return 'QueueCheckUrl' }
    getMethod() { return 'POST' }
    createResponse() { return new QueueCheckUrlResponse() }
}
export class QueueCheckUrls {
    /** @param {{urls?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    urls;
    getTypeName() { return 'QueueCheckUrls' }
    getMethod() { return 'POST' }
    createResponse() { return new QueueCheckUrlsResponse() }
}
export class CompressFile {
    /** @param {{path?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    path;
    getTypeName() { return 'CompressFile' }
    getMethod() { return 'GET' }
    createResponse() { return new Blob() }
}
export class AltQueryItems {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    name;
    getTypeName() { return 'AltQueryItems' }
    getMethod() { return 'POST' }
    createResponse() { return new QueryResponseAlt() }
}
export class GetItems {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetItems' }
    getMethod() { return 'GET' }
    createResponse() { return new Items() }
}
export class GetNakedItems {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetNakedItems' }
    getMethod() { return 'GET' }
    createResponse() { return [] }
}
export class GetProfileImage {
    /** @param {{type?:string,size?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    type;
    /** @type {?string} */
    size;
    getTypeName() { return 'GetProfileImage' }
    getMethod() { return 'POST' }
    createResponse() { return new Blob() }
}
export class GetWeatherForecast {
    /** @param {{date?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    date;
    getTypeName() { return 'GetWeatherForecast' }
    getMethod() { return 'GET' }
    createResponse() { return [] }
}
export class Problem {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'Problem' }
    getMethod() { return 'POST' }
    createResponse() { return new ResponseBase() }
}
export class DigitalPrescriptionDMDRequest {
    /** @param {{term?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    term;
    getTypeName() { return 'DigitalPrescriptionDMDRequest' }
    getMethod() { return 'POST' }
    createResponse() { return new ResponseBase() }
}
export class GetDiscountCodeBillingItem {
    /** @param {{billingItem?:BillingItem,discountCodeId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {BillingItem} */
    billingItem;
    /** @type {string} */
    discountCodeId;
    getTypeName() { return 'GetDiscountCodeBillingItem' }
    getMethod() { return 'POST' }
    createResponse() { return new ResponseBase() }
}
export class GetFooDtos extends PagedAndOrderedRequest {
    /** @param {{query?:string,orderBy?:string,page?:number,pageSize?:number}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    query;
    getTypeName() { return 'GetFooDtos' }
    getMethod() { return 'GET' }
    createResponse() { return new PagedResult() }
}
export class Secured {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'Secured' }
    getMethod() { return 'POST' }
    createResponse() { return new SecuredResponse() }
}
export class CreateRefreshJwt {
    /** @param {{userAuthId?:string,jwtExpiry?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userAuthId;
    /** @type {?string} */
    jwtExpiry;
    getTypeName() { return 'CreateRefreshJwt' }
    getMethod() { return 'POST' }
    createResponse() { return new CreateRefreshJwtResponse() }
}
export class InvalidateLastAccessToken {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'InvalidateLastAccessToken' }
    getMethod() { return 'POST' }
    createResponse() { return new EmptyResponse() }
}
export class MovieGETRequest {
    /** @param {{movieID?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description Unique Id of the movie */
    movieID;
    getTypeName() { return 'MovieGETRequest' }
    getMethod() { return 'GET' }
    createResponse() { return new Movie() }
}
export class MoviePOSTRequest extends Movie {
    /** @param {{movieID?:string,movieNo?:number,movieRef?:string,movieID?:string,movieNo?:number,name?:string,description?:string,movieRef?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    movieID;
    /** @type {number} */
    movieNo;
    /** @type {?string} */
    movieRef;
    getTypeName() { return 'MoviePOSTRequest' }
    getMethod() { return 'POST' }
    createResponse() { return new Movie() }
}
export class CommandOperation {
    /** @param {{newTodo?:string,throwException?:string,throwArgumentException?:string,throwNotSupportedException?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    newTodo;
    /** @type {?string} */
    throwException;
    /** @type {?string} */
    throwArgumentException;
    /** @type {?string} */
    throwNotSupportedException;
    getTypeName() { return 'CommandOperation' }
    getMethod() { return 'POST' }
    createResponse() { return new EmptyResponse() }
}
export class FailedCommandTests {
    /** @param {{failNoRetryCommand?:boolean,failDefaultRetryCommand?:boolean,failTimes1Command?:boolean,failTimes4Command?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?boolean} */
    failNoRetryCommand;
    /** @type {?boolean} */
    failDefaultRetryCommand;
    /** @type {?boolean} */
    failTimes1Command;
    /** @type {?boolean} */
    failTimes4Command;
    getTypeName() { return 'FailedCommandTests' }
    getMethod() { return 'POST' }
    createResponse () { };
}
export class Greet {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'Greet' }
    getMethod() { return 'GET' }
    createResponse() { return new HelloResponse() }
}
export class Hello {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'Hello' }
    getMethod() { return 'GET' }
    createResponse() { return new HelloResponse() }
}
export class HelloVeryLongOperationNameVersions {
    /** @param {{name?:string,names?:string[],ids?:number[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    name;
    /** @type {?string[]} */
    names;
    /** @type {?number[]} */
    ids;
    getTypeName() { return 'HelloVeryLongOperationNameVersions' }
    getMethod() { return 'GET' }
    createResponse() { return new HelloResponse() }
}
export class HelloSecure {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'HelloSecure' }
    getMethod() { return 'PUT' }
    createResponse() { return new HelloResponse() }
}
export class HelloBookingList {
    /** @param {{Alias?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    Alias;
    getTypeName() { return 'HelloBookingList' }
    getMethod() { return 'POST' }
    createResponse() { return [] }
}
export class HelloString {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'HelloString' }
    getMethod() { return 'POST' }
    createResponse() { return '' }
}
export class ReturnString {
    /** @param {{data?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    data;
    getTypeName() { return 'ReturnString' }
    getMethod() { return 'POST' }
    createResponse() { return '' }
}
export class GetWorkerStats {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetWorkerStats' }
    getMethod() { return 'GET' }
    createResponse() { return new GetWorkerStatsResponse() }
}
export class OpenAiChatCompletion extends OpenAiChat {
    /** @param {{refId?:string,provider?:string,tag?:string,messages?:OpenAiMessage[],model?:string,frequency_penalty?:number,logit_bias?:{ [index:number]: number; },logprobs?:boolean,top_logprobs?:number,max_tokens?:number,n?:number,presence_penalty?:number,response_format?:OpenAiResponseFormat,seed?:number,stop?:string[],stream?:boolean,temperature?:number,top_p?:number,tools?:OpenAiTools[],user?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /**
     * @type {?string}
     * @description Provide a unique identifier to track requests */
    refId;
    /**
     * @type {?string}
     * @description Specify which AI Provider to use */
    provider;
    /**
     * @type {?string}
     * @description Categorize like requests under a common group */
    tag;
    getTypeName() { return 'OpenAiChatCompletion' }
    getMethod() { return 'POST' }
    createResponse() { return new OpenAiChatResponse() }
}
export class ActiveMediaModels {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'ActiveMediaModels' }
    getMethod() { return 'GET' }
    createResponse() { return new StringsResponse() }
}
export class TextToSpeech {
    /** @param {{input?:string,model?:string,seed?:number,refId?:string,tag?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The text to be converted to speech */
    input;
    /**
     * @type {?string}
     * @description Optional specific model and voice to use for speech generation */
    model;
    /**
     * @type {?number}
     * @description Optional seed for reproducible results in speech generation */
    seed;
    /**
     * @type {?string}
     * @description Optional client-provided identifier for the request */
    refId;
    /**
     * @type {?string}
     * @description Tag to identify the request */
    tag;
    getTypeName() { return 'TextToSpeech' }
    getMethod() { return 'POST' }
    createResponse() { return new GenerationResponse() }
}
export class SpeechToText {
    /** @param {{audio?:string,refId?:string,tag?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The audio stream containing the speech to be transcribed */
    audio;
    /**
     * @type {?string}
     * @description Optional client-provided identifier for the request */
    refId;
    /**
     * @type {?string}
     * @description Tag to identify the request */
    tag;
    getTypeName() { return 'SpeechToText' }
    getMethod() { return 'POST' }
    createResponse() { return new GenerationResponse() }
}
export class TextToImage {
    /** @param {{positivePrompt?:string,negativePrompt?:string,width?:number,height?:number,batchSize?:number,model?:string,seed?:number,refId?:string,tag?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The main prompt describing the desired image */
    positivePrompt;
    /**
     * @type {?string}
     * @description Optional prompt specifying what should not be in the image */
    negativePrompt;
    /**
     * @type {?number}
     * @description Desired width of the generated image */
    width;
    /**
     * @type {?number}
     * @description Desired height of the generated image */
    height;
    /**
     * @type {?number}
     * @description Number of images to generate in a single batch */
    batchSize;
    /**
     * @type {?string}
     * @description The AI model to use for image generation */
    model;
    /**
     * @type {?number}
     * @description Optional seed for reproducible results */
    seed;
    /**
     * @type {?string}
     * @description Optional client-provided identifier for the request */
    refId;
    /**
     * @type {?string}
     * @description Tag to identify the request */
    tag;
    getTypeName() { return 'TextToImage' }
    getMethod() { return 'POST' }
    createResponse() { return new GenerationResponse() }
}
export class ImageToImage {
    /** @param {{image?:string,positivePrompt?:string,negativePrompt?:string,denoise?:number,batchSize?:number,seed?:number,refId?:string,tag?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The image to use as input */
    image;
    /**
     * @type {string}
     * @description Prompt describing the desired output */
    positivePrompt;
    /**
     * @type {?string}
     * @description Negative prompt describing what should not be in the image */
    negativePrompt;
    /**
     * @type {?number}
     * @description Optional specific amount of denoise to apply */
    denoise;
    /**
     * @type {?number}
     * @description Number of images to generate in a single batch */
    batchSize;
    /**
     * @type {?number}
     * @description Optional seed for reproducible results in image generation */
    seed;
    /**
     * @type {?string}
     * @description Optional client-provided identifier for the request */
    refId;
    /**
     * @type {?string}
     * @description Tag to identify the request */
    tag;
    getTypeName() { return 'ImageToImage' }
    getMethod() { return 'POST' }
    createResponse() { return new GenerationResponse() }
}
export class ImageUpscale {
    /** @param {{image?:string,seed?:number,refId?:string,tag?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The image to upscale */
    image;
    /**
     * @type {?number}
     * @description Optional seed for reproducible results in image generation */
    seed;
    /**
     * @type {?string}
     * @description Optional client-provided identifier for the request */
    refId;
    /**
     * @type {?string}
     * @description Tag to identify the request */
    tag;
    getTypeName() { return 'ImageUpscale' }
    getMethod() { return 'POST' }
    createResponse() { return new GenerationResponse() }
}
export class ImageWithMask {
    /** @param {{positivePrompt?:string,negativePrompt?:string,image?:string,mask?:string,denoise?:number,seed?:number,refId?:string,tag?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description Prompt describing the desired output in the masked area */
    positivePrompt;
    /**
     * @type {?string}
     * @description Negative prompt describing what should not be in the masked area */
    negativePrompt;
    /**
     * @type {string}
     * @description The image to use as input */
    image;
    /**
     * @type {string}
     * @description The mask to use as input */
    mask;
    /**
     * @type {?number}
     * @description Optional specific amount of denoise to apply */
    denoise;
    /**
     * @type {?number}
     * @description Optional seed for reproducible results in image generation */
    seed;
    /**
     * @type {?string}
     * @description Optional client-provided identifier for the request */
    refId;
    /**
     * @type {?string}
     * @description Tag to identify the request */
    tag;
    getTypeName() { return 'ImageWithMask' }
    getMethod() { return 'POST' }
    createResponse() { return new GenerationResponse() }
}
export class ImageToText {
    /** @param {{image?:string,refId?:string,tag?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The image to convert to text */
    image;
    /**
     * @type {?string}
     * @description Optional client-provided identifier for the request */
    refId;
    /**
     * @type {?string}
     * @description Tag to identify the request */
    tag;
    getTypeName() { return 'ImageToText' }
    getMethod() { return 'POST' }
    createResponse() { return new GenerationResponse() }
}
export class SendJson {
    /** @param {{id?:number,name?:string,requestStream?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    name;
    /** @type {string} */
    requestStream;
    getTypeName() { return 'SendJson' }
    getMethod() { return 'POST' }
    createResponse() { return '' }
}
export class SendText {
    /** @param {{id?:number,name?:string,contentType?:string,requestStream?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    name;
    /** @type {?string} */
    contentType;
    /** @type {string} */
    requestStream;
    getTypeName() { return 'SendText' }
    getMethod() { return 'POST' }
    createResponse() { return '' }
}
export class SendRaw {
    /** @param {{id?:number,name?:string,contentType?:string,requestStream?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    name;
    /** @type {?string} */
    contentType;
    /** @type {string} */
    requestStream;
    getTypeName() { return 'SendRaw' }
    getMethod() { return 'POST' }
    createResponse() { return new Blob() }
}
export class SendDefault {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'SendDefault' }
    getMethod() { return 'POST' }
    createResponse() { return new SendVerbResponse() }
}
export class SendRestGet {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'SendRestGet' }
    getMethod() { return 'GET' }
    createResponse() { return new SendVerbResponse() }
}
export class SendGet {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'SendGet' }
    getMethod() { return 'GET' }
    createResponse() { return new SendVerbResponse() }
}
export class SendPost {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'SendPost' }
    getMethod() { return 'POST' }
    createResponse() { return new SendVerbResponse() }
}
export class SendPut {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'SendPut' }
    getMethod() { return 'PUT' }
    createResponse() { return new SendVerbResponse() }
}
export class SendReturnVoid {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'SendReturnVoid' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class HelloAuth {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'HelloAuth' }
    getMethod() { return 'POST' }
    createResponse() { return new HelloResponse() }
}
export class TestAuth {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'TestAuth' }
    getMethod() { return 'POST' }
    createResponse() { return new TestAuthResponse() }
}
export class HelloAllTypes {
    /** @param {{name?:string,allTypes?:AllTypes,allCollectionTypes?:AllCollectionTypes}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {AllTypes} */
    allTypes;
    /** @type {AllCollectionTypes} */
    allCollectionTypes;
    getTypeName() { return 'HelloAllTypes' }
    getMethod() { return 'POST' }
    createResponse() { return new HelloAllTypesResponse() }
}
export class ThrowType {
    /** @param {{type?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    type;
    /** @type {?string} */
    message;
    getTypeName() { return 'ThrowType' }
    getMethod() { return 'POST' }
    createResponse() { return new ThrowTypeResponse() }
}
export class ThrowValidation {
    /** @param {{age?:number,required?:string,email?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    age;
    /** @type {string} */
    required;
    /** @type {string} */
    email;
    getTypeName() { return 'ThrowValidation' }
    getMethod() { return 'POST' }
    createResponse() { return new ThrowValidationResponse() }
}
export class ProfileGen {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'ProfileGen' }
    getMethod() { return 'POST' }
    createResponse() { return new ProfileGenResponse() }
}
export class HelloReturnVoid {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'HelloReturnVoid' }
    getMethod() { return 'POST' }
    createResponse() { }
}
export class HelloList {
    /** @param {{names?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string[]} */
    names = [];
    getTypeName() { return 'HelloList' }
    getMethod() { return 'POST' }
    createResponse() { return [] }
}
export class HelloWithEnum {
    /** @param {{enumProp?:EnumType,enumTypeFlags?:EnumTypeFlags,enumWithValues?:EnumWithValues,nullableEnumProp?:EnumType,enumFlags?:EnumFlags,enumAsInt?:EnumAsInt,enumStyle?:EnumStyle,enumStyleMembers?:EnumStyleMembers}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {EnumType} */
    enumProp;
    /** @type {EnumTypeFlags} */
    enumTypeFlags;
    /** @type {EnumWithValues} */
    enumWithValues;
    /** @type {?EnumType} */
    nullableEnumProp;
    /** @type {EnumFlags} */
    enumFlags;
    /** @type {EnumAsInt} */
    enumAsInt;
    /** @type {EnumStyle} */
    enumStyle;
    /** @type {EnumStyleMembers} */
    enumStyleMembers;
    getTypeName() { return 'HelloWithEnum' }
    getMethod() { return 'POST' }
    createResponse () { };
}
export class HelloWithEnumList {
    /** @param {{enumProp?:EnumType[],enumWithValues?:EnumWithValues[],nullableEnumProp?:EnumType[],enumFlags?:EnumFlags[],enumStyle?:EnumStyle[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {EnumType[]} */
    enumProp = [];
    /** @type {EnumWithValues[]} */
    enumWithValues = [];
    /** @type {EnumType[]} */
    nullableEnumProp = [];
    /** @type {EnumFlags[]} */
    enumFlags = [];
    /** @type {EnumStyle[]} */
    enumStyle = [];
    getTypeName() { return 'HelloWithEnumList' }
    getMethod() { return 'POST' }
    createResponse () { };
}
export class HelloWithEnumMap {
    /** @param {{enumProp?:{ [index:string]: EnumType; },enumWithValues?:{ [index:string]: EnumWithValues; },nullableEnumProp?:{ [index:string]: EnumType; },enumFlags?:{ [index:string]: EnumFlags; },enumStyle?:{ [index:string]: EnumStyle; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {{ [index:string]: EnumType; }} */
    enumProp = {};
    /** @type {{ [index:string]: EnumWithValues; }} */
    enumWithValues = {};
    /** @type {{ [index:string]: EnumType; }} */
    nullableEnumProp = {};
    /** @type {{ [index:string]: EnumFlags; }} */
    enumFlags = {};
    /** @type {{ [index:string]: EnumStyle; }} */
    enumStyle = {};
    getTypeName() { return 'HelloWithEnumMap' }
    getMethod() { return 'POST' }
    createResponse () { };
}
export class HelloSubAllTypes extends AllTypesBase {
    /** @param {{hierarchy?:number,id?:number,nullableId?:number,byte?:number,short?:number,int?:number,long?:number,uShort?:number,uInt?:number,uLong?:number,float?:number,double?:number,decimal?:number,string?:string,dateTime?:string,timeSpan?:string,dateTimeOffset?:string,guid?:string,char?:string,keyValuePair?:KeyValuePair<string, string>,nullableDateTime?:string,nullableTimeSpan?:string,stringList?:string[],stringArray?:string[],stringMap?:{ [index:string]: string; },intStringMap?:{ [index:number]: string; },subType?:SubType}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    hierarchy;
    getTypeName() { return 'HelloSubAllTypes' }
    getMethod() { return 'POST' }
    createResponse() { return new SubAllTypes() }
}
export class Authenticate {
    /** @param {{provider?:string,userName?:string,password?:string,rememberMe?:boolean,accessToken?:string,accessTokenSecret?:string,returnUrl?:string,errorView?:string,meta?:{ [index:string]: string; }}} [init] */
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
    /** @type {{ [index:string]: string; }} */
    meta;
    getTypeName() { return 'Authenticate' }
    getMethod() { return 'POST' }
    createResponse() { return new AuthenticateResponse() }
}
export class GetAccessToken {
    /** @param {{refreshToken?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    refreshToken;
    /** @type {{ [index:string]: string; }} */
    meta;
    getTypeName() { return 'GetAccessToken' }
    getMethod() { return 'POST' }
    createResponse() { return new GetAccessTokenResponse() }
}
export class QueryAlbums extends QueryDb {
    /** @param {{albumId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    albumId;
    getTypeName() { return 'QueryAlbums' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryArtists extends QueryDb {
    /** @param {{artistId?:number,artistIdBetween?:number[],nameStartsWith?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    artistId;
    /** @type {number[]} */
    artistIdBetween;
    /** @type {string} */
    nameStartsWith;
    getTypeName() { return 'QueryArtists' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChinookCustomers extends QueryDb {
    /** @param {{customerId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    customerId;
    getTypeName() { return 'QueryChinookCustomers' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChinookEmployees extends QueryDb {
    /** @param {{employeeId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    employeeId;
    getTypeName() { return 'QueryChinookEmployees' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryGenres extends QueryDb {
    /** @param {{genreId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    genreId;
    getTypeName() { return 'QueryGenres' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryInvoiceItems extends QueryDb {
    /** @param {{invoiceLineId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    invoiceLineId;
    getTypeName() { return 'QueryInvoiceItems' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryInvoices extends QueryDb {
    /** @param {{invoiceId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    invoiceId;
    getTypeName() { return 'QueryInvoices' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryMediaTypes extends QueryDb {
    /** @param {{mediaTypeId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    mediaTypeId;
    getTypeName() { return 'QueryMediaTypes' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryPlaylists extends QueryDb {
    /** @param {{playlistId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    playlistId;
    getTypeName() { return 'QueryPlaylists' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryTracks extends QueryDb {
    /** @param {{trackId?:number,nameContains?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    trackId;
    /** @type {string} */
    nameContains;
    getTypeName() { return 'QueryTracks' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryJobApplicationAttachment extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryJobApplicationAttachment' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryContacts extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryContacts' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryJob extends QueryDb {
    /** @param {{id?:number,ids?:number[],skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?number[]} */
    ids;
    getTypeName() { return 'QueryJob' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryJobApplication extends QueryDb {
    /** @param {{id?:number,ids?:number[],jobId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?number[]} */
    ids;
    /** @type {?number} */
    jobId;
    getTypeName() { return 'QueryJobApplication' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryPhoneScreen extends QueryDb {
    /** @param {{id?:number,jobApplicationId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    getTypeName() { return 'QueryPhoneScreen' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryInterview extends QueryDb {
    /** @param {{id?:number,jobApplicationId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    getTypeName() { return 'QueryInterview' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryJobOffer extends QueryDb {
    /** @param {{id?:number,jobApplicationId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    getTypeName() { return 'QueryJobOffer' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryJobAppEvents extends QueryDb {
    /** @param {{jobApplicationId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    jobApplicationId;
    getTypeName() { return 'QueryJobAppEvents' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryApplicationUser extends QueryDb {
    /** @param {{emailContains?:string,firstNameContains?:string,lastNameContains?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?string} */
    emailContains;
    /** @type {?string} */
    firstNameContains;
    /** @type {?string} */
    lastNameContains;
    getTypeName() { return 'QueryApplicationUser' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryJobApplicationComments extends QueryDb {
    /** @param {{jobApplicationId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    jobApplicationId;
    getTypeName() { return 'QueryJobApplicationComments' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryBookings extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryBookings' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryCoupons extends QueryDb {
    /** @param {{id?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'QueryCoupons' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryAddresses extends QueryDb {
    /** @param {{ids?:number[],skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number[]} */
    ids;
    getTypeName() { return 'QueryAddresses' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryFileSystemItems extends QueryDb {
    /** @param {{appUserId?:number,fileAccessType?:FileAccessType,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    appUserId;
    /** @type {?FileAccessType} */
    fileAccessType;
    getTypeName() { return 'QueryFileSystemItems' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryFileSystemFiles extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryFileSystemFiles' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryPlayer extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryPlayer' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryProfile extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    getTypeName() { return 'QueryProfile' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryGameItem extends QueryDb {
    /** @param {{name?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'QueryGameItem' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryPlayerGameItem extends QueryDb {
    /** @param {{id?:number,playerId?:number,gameItemName?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?number} */
    playerId;
    /** @type {?string} */
    gameItemName;
    getTypeName() { return 'QueryPlayerGameItem' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryLevel extends QueryDb {
    /** @param {{id?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?string} */
    id;
    getTypeName() { return 'QueryLevel' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryTodos extends QueryDb {
    /** @param {{id?:number,ids?:number[],textContains?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?number[]} */
    ids;
    /** @type {?string} */
    textContains;
    getTypeName() { return 'QueryTodos' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryApiKeys extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryApiKeys' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryAspNetRoleClaims extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryAspNetRoleClaims' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryAspNetRoles extends QueryDb {
    /** @param {{id?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'QueryAspNetRoles' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryAspNetUserClaims extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryAspNetUserClaims' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryAspNetUsers extends QueryDb {
    /** @param {{id?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'QueryAspNetUsers' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryCrudEvents extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryCrudEvents' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryEFMigrationsHistories extends QueryDb {
    /** @param {{migrationId?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {string} */
    migrationId;
    getTypeName() { return 'QueryEFMigrationsHistories' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryMigrations extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryMigrations' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryValidationRules extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryValidationRules' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class CreateAlbums {
    /** @param {{title?:string,artistId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    title;
    /** @type {number} */
    artistId;
    getTypeName() { return 'CreateAlbums' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateArtists {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'CreateArtists' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChinookCustomer {
    /** @param {{firstName?:string,lastName?:string,company?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string,supportRepId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    company;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    country;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    email;
    /** @type {?number} */
    supportRepId;
    getTypeName() { return 'CreateChinookCustomer' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChinookEmployee {
    /** @param {{lastName?:string,firstName?:string,title?:string,reportsTo?:number,birthDate?:string,hireDate?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    lastName;
    /** @type {string} */
    firstName;
    /** @type {string} */
    title;
    /** @type {?number} */
    reportsTo;
    /** @type {?string} */
    birthDate;
    /** @type {?string} */
    hireDate;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    country;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    email;
    getTypeName() { return 'CreateChinookEmployee' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateGenres {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'CreateGenres' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateInvoiceItems {
    /** @param {{invoiceId?:number,trackId?:number,unitPrice?:number,quantity?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    invoiceId;
    /** @type {number} */
    trackId;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    quantity;
    getTypeName() { return 'CreateInvoiceItems' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateInvoices {
    /** @param {{customerId?:number,invoiceDate?:string,billingAddress?:string,billingCity?:string,billingState?:string,billingCountry?:string,billingPostalCode?:string,total?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    customerId;
    /** @type {string} */
    invoiceDate;
    /** @type {string} */
    billingAddress;
    /** @type {string} */
    billingCity;
    /** @type {string} */
    billingState;
    /** @type {string} */
    billingCountry;
    /** @type {string} */
    billingPostalCode;
    /** @type {number} */
    total;
    getTypeName() { return 'CreateInvoices' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateMediaTypes {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'CreateMediaTypes' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreatePlaylists {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'CreatePlaylists' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateTracks {
    /** @param {{name?:string,albumId?:number,mediaTypeId?:number,genreId?:number,composer?:string,milliseconds?:number,bytes?:number,unitPrice?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {?number} */
    albumId;
    /** @type {number} */
    mediaTypeId;
    /** @type {?number} */
    genreId;
    /** @type {string} */
    composer;
    /** @type {number} */
    milliseconds;
    /** @type {?number} */
    bytes;
    /** @type {number} */
    unitPrice;
    getTypeName() { return 'CreateTracks' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class DeleteAlbums {
    /** @param {{albumId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    albumId;
    getTypeName() { return 'DeleteAlbums' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteArtists {
    /** @param {{artistId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    artistId;
    getTypeName() { return 'DeleteArtists' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChinookCustomer {
    /** @param {{customerId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    customerId;
    getTypeName() { return 'DeleteChinookCustomer' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChinookEmployee {
    /** @param {{employeeId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    employeeId;
    getTypeName() { return 'DeleteChinookEmployee' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteGenres {
    /** @param {{genreId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    genreId;
    getTypeName() { return 'DeleteGenres' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteInvoiceItems {
    /** @param {{invoiceLineId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    invoiceLineId;
    getTypeName() { return 'DeleteInvoiceItems' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteInvoices {
    /** @param {{invoiceId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    invoiceId;
    getTypeName() { return 'DeleteInvoices' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteMediaTypes {
    /** @param {{mediaTypeId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    mediaTypeId;
    getTypeName() { return 'DeleteMediaTypes' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeletePlaylists {
    /** @param {{playlistId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    playlistId;
    getTypeName() { return 'DeletePlaylists' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteTracks {
    /** @param {{trackId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    trackId;
    getTypeName() { return 'DeleteTracks' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class PatchAlbums {
    /** @param {{albumId?:number,title?:string,artistId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    albumId;
    /** @type {string} */
    title;
    /** @type {number} */
    artistId;
    getTypeName() { return 'PatchAlbums' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchArtists {
    /** @param {{artistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    artistId;
    /** @type {string} */
    name;
    getTypeName() { return 'PatchArtists' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChinookCustomer {
    /** @param {{customerId?:number,firstName?:string,lastName?:string,company?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string,supportRepId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    customerId;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    company;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    country;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    email;
    /** @type {?number} */
    supportRepId;
    getTypeName() { return 'PatchChinookCustomer' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChinookEmployee {
    /** @param {{employeeId?:number,lastName?:string,firstName?:string,title?:string,reportsTo?:number,birthDate?:string,hireDate?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    employeeId;
    /** @type {string} */
    lastName;
    /** @type {string} */
    firstName;
    /** @type {string} */
    title;
    /** @type {?number} */
    reportsTo;
    /** @type {?string} */
    birthDate;
    /** @type {?string} */
    hireDate;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    country;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    email;
    getTypeName() { return 'PatchChinookEmployee' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchGenres {
    /** @param {{genreId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    genreId;
    /** @type {string} */
    name;
    getTypeName() { return 'PatchGenres' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchInvoiceItems {
    /** @param {{invoiceLineId?:number,invoiceId?:number,trackId?:number,unitPrice?:number,quantity?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    invoiceLineId;
    /** @type {number} */
    invoiceId;
    /** @type {number} */
    trackId;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    quantity;
    getTypeName() { return 'PatchInvoiceItems' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchInvoices {
    /** @param {{invoiceId?:number,customerId?:number,invoiceDate?:string,billingAddress?:string,billingCity?:string,billingState?:string,billingCountry?:string,billingPostalCode?:string,total?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    invoiceId;
    /** @type {number} */
    customerId;
    /** @type {string} */
    invoiceDate;
    /** @type {string} */
    billingAddress;
    /** @type {string} */
    billingCity;
    /** @type {string} */
    billingState;
    /** @type {string} */
    billingCountry;
    /** @type {string} */
    billingPostalCode;
    /** @type {number} */
    total;
    getTypeName() { return 'PatchInvoices' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchMediaTypes {
    /** @param {{mediaTypeId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    mediaTypeId;
    /** @type {string} */
    name;
    getTypeName() { return 'PatchMediaTypes' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchPlaylists {
    /** @param {{playlistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    playlistId;
    /** @type {string} */
    name;
    getTypeName() { return 'PatchPlaylists' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchTracks {
    /** @param {{trackId?:number,name?:string,albumId?:number,mediaTypeId?:number,genreId?:number,composer?:string,milliseconds?:number,bytes?:number,unitPrice?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    trackId;
    /** @type {string} */
    name;
    /** @type {?number} */
    albumId;
    /** @type {number} */
    mediaTypeId;
    /** @type {?number} */
    genreId;
    /** @type {string} */
    composer;
    /** @type {number} */
    milliseconds;
    /** @type {?number} */
    bytes;
    /** @type {number} */
    unitPrice;
    getTypeName() { return 'PatchTracks' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class UpdateAlbums {
    /** @param {{albumId?:number,title?:string,artistId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    albumId;
    /** @type {string} */
    title;
    /** @type {number} */
    artistId;
    getTypeName() { return 'UpdateAlbums' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateArtists {
    /** @param {{artistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    artistId;
    /** @type {string} */
    name;
    getTypeName() { return 'UpdateArtists' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChinookCustomer {
    /** @param {{customerId?:number,firstName?:string,lastName?:string,company?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string,supportRepId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    customerId;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    company;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    country;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    email;
    /** @type {?number} */
    supportRepId;
    getTypeName() { return 'UpdateChinookCustomer' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChinookEmployee {
    /** @param {{employeeId?:number,lastName?:string,firstName?:string,title?:string,reportsTo?:number,birthDate?:string,hireDate?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    employeeId;
    /** @type {string} */
    lastName;
    /** @type {string} */
    firstName;
    /** @type {string} */
    title;
    /** @type {?number} */
    reportsTo;
    /** @type {?string} */
    birthDate;
    /** @type {?string} */
    hireDate;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    country;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    email;
    getTypeName() { return 'UpdateChinookEmployee' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateGenres {
    /** @param {{genreId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    genreId;
    /** @type {string} */
    name;
    getTypeName() { return 'UpdateGenres' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateInvoiceItems {
    /** @param {{invoiceLineId?:number,invoiceId?:number,trackId?:number,unitPrice?:number,quantity?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    invoiceLineId;
    /** @type {number} */
    invoiceId;
    /** @type {number} */
    trackId;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    quantity;
    getTypeName() { return 'UpdateInvoiceItems' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateInvoices {
    /** @param {{invoiceId?:number,customerId?:number,invoiceDate?:string,billingAddress?:string,billingCity?:string,billingState?:string,billingCountry?:string,billingPostalCode?:string,total?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    invoiceId;
    /** @type {number} */
    customerId;
    /** @type {string} */
    invoiceDate;
    /** @type {string} */
    billingAddress;
    /** @type {string} */
    billingCity;
    /** @type {string} */
    billingState;
    /** @type {string} */
    billingCountry;
    /** @type {string} */
    billingPostalCode;
    /** @type {number} */
    total;
    getTypeName() { return 'UpdateInvoices' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateMediaTypes {
    /** @param {{mediaTypeId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    mediaTypeId;
    /** @type {string} */
    name;
    getTypeName() { return 'UpdateMediaTypes' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdatePlaylists {
    /** @param {{playlistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    playlistId;
    /** @type {string} */
    name;
    getTypeName() { return 'UpdatePlaylists' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateTracks {
    /** @param {{trackId?:number,name?:string,albumId?:number,mediaTypeId?:number,genreId?:number,composer?:string,milliseconds?:number,bytes?:number,unitPrice?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    trackId;
    /** @type {string} */
    name;
    /** @type {?number} */
    albumId;
    /** @type {number} */
    mediaTypeId;
    /** @type {?number} */
    genreId;
    /** @type {string} */
    composer;
    /** @type {number} */
    milliseconds;
    /** @type {?number} */
    bytes;
    /** @type {number} */
    unitPrice;
    getTypeName() { return 'UpdateTracks' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class CreateContact {
    /** @param {{firstName?:string,lastName?:string,profileUrl?:string,salaryExpectation?:number,jobType?:string,availabilityWeeks?:number,preferredWorkType?:EmploymentType,preferredLocation?:string,email?:string,phone?:string,about?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {?string} */
    profileUrl;
    /** @type {?number} */
    salaryExpectation;
    /** @type {string} */
    jobType;
    /** @type {number} */
    availabilityWeeks;
    /** @type {EmploymentType} */
    preferredWorkType;
    /** @type {string} */
    preferredLocation;
    /** @type {string} */
    email;
    /** @type {?string} */
    phone;
    /** @type {?string} */
    about;
    getTypeName() { return 'CreateContact' }
    getMethod() { return 'POST' }
    createResponse() { return new Contact() }
}
export class UpdateContact {
    /** @param {{id?:number,firstName?:string,lastName?:string,profileUrl?:string,salaryExpectation?:number,jobType?:string,availabilityWeeks?:number,preferredWorkType?:EmploymentType,preferredLocation?:string,email?:string,phone?:string,about?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {?string} */
    profileUrl;
    /** @type {?number} */
    salaryExpectation;
    /** @type {string} */
    jobType;
    /** @type {?number} */
    availabilityWeeks;
    /** @type {?EmploymentType} */
    preferredWorkType;
    /** @type {?string} */
    preferredLocation;
    /** @type {string} */
    email;
    /** @type {?string} */
    phone;
    /** @type {?string} */
    about;
    getTypeName() { return 'UpdateContact' }
    getMethod() { return 'PATCH' }
    createResponse() { return new Contact() }
}
export class DeleteContact {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteContact' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateJob {
    /** @param {{title?:string,salaryRangeLower?:number,salaryRangeUpper?:number,description?:string,employmentType?:EmploymentType,company?:string,location?:string,closing?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    title;
    /** @type {number} */
    salaryRangeLower;
    /** @type {number} */
    salaryRangeUpper;
    /** @type {string} */
    description;
    /** @type {EmploymentType} */
    employmentType;
    /** @type {string} */
    company;
    /** @type {string} */
    location;
    /** @type {string} */
    closing;
    getTypeName() { return 'CreateJob' }
    getMethod() { return 'POST' }
    createResponse() { return new Job() }
}
export class UpdateJob {
    /** @param {{id?:number,title?:string,salaryRangeLower?:number,salaryRangeUpper?:number,description?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    title;
    /** @type {?number} */
    salaryRangeLower;
    /** @type {?number} */
    salaryRangeUpper;
    /** @type {?string} */
    description;
    getTypeName() { return 'UpdateJob' }
    getMethod() { return 'PATCH' }
    createResponse() { return new Job() }
}
export class DeleteJob {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteJob' }
    getMethod() { return 'DELETE' }
    createResponse() { return new Job() }
}
export class CreateJobApplication {
    /** @param {{jobId?:number,contactId?:number,appliedDate?:string,applicationStatus?:JobApplicationStatus,attachments?:JobApplicationAttachment[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    jobId;
    /** @type {number} */
    contactId;
    /** @type {string} */
    appliedDate;
    /** @type {JobApplicationStatus} */
    applicationStatus;
    /** @type {JobApplicationAttachment[]} */
    attachments;
    getTypeName() { return 'CreateJobApplication' }
    getMethod() { return 'POST' }
    createResponse() { return new JobApplication() }
}
export class UpdateJobApplication {
    /** @param {{id?:number,jobId?:number,contactId?:number,appliedDate?:string,applicationStatus?:JobApplicationStatus,attachments?:JobApplicationAttachment[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?number} */
    jobId;
    /** @type {?number} */
    contactId;
    /** @type {?string} */
    appliedDate;
    /** @type {JobApplicationStatus} */
    applicationStatus;
    /** @type {?JobApplicationAttachment[]} */
    attachments;
    getTypeName() { return 'UpdateJobApplication' }
    getMethod() { return 'PATCH' }
    createResponse() { return new JobApplication() }
}
export class DeleteJobApplication {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteJobApplication' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateJobApplicationEvent {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'CreateJobApplicationEvent' }
    getMethod() { return 'POST' }
    createResponse() { return new JobApplicationEvent() }
}
export class UpdateJobApplicationEvent {
    /** @param {{id?:number,status?:JobApplicationStatus,description?:string,eventDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?JobApplicationStatus} */
    status;
    /** @type {?string} */
    description;
    /** @type {?string} */
    eventDate;
    getTypeName() { return 'UpdateJobApplicationEvent' }
    getMethod() { return 'PATCH' }
    createResponse() { return new JobApplicationEvent() }
}
export class DeleteJobApplicationEvent {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'DeleteJobApplicationEvent' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateJobApplicationComment {
    /** @param {{jobApplicationId?:number,comment?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    jobApplicationId;
    /** @type {string} */
    comment;
    getTypeName() { return 'CreateJobApplicationComment' }
    getMethod() { return 'POST' }
    createResponse() { return new JobApplicationComment() }
}
export class UpdateJobApplicationComment {
    /** @param {{id?:number,jobApplicationId?:number,comment?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    /** @type {?string} */
    comment;
    getTypeName() { return 'UpdateJobApplicationComment' }
    getMethod() { return 'PATCH' }
    createResponse() { return new JobApplicationComment() }
}
export class DeleteJobApplicationComment {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteJobApplicationComment' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateBooking {
    /** @param {{name?:string,roomType?:RoomType,roomNumber?:number,cost?:number,bookingStartDate?:string,bookingEndDate?:string,notes?:string,couponId?:string,permanentAddressId?:number,postalAddressId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description Name this Booking is for */
    name;
    /** @type {RoomType} */
    roomType;
    /** @type {number} */
    roomNumber;
    /** @type {number} */
    cost;
    /** @type {string} */
    bookingStartDate;
    /** @type {?string} */
    bookingEndDate;
    /** @type {?string} */
    notes;
    /** @type {?string} */
    couponId;
    /** @type {?number} */
    permanentAddressId;
    /** @type {?number} */
    postalAddressId;
    getTypeName() { return 'CreateBooking' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class UpdateBooking {
    /** @param {{id?:number,name?:string,roomType?:RoomType,roomNumber?:number,cost?:number,bookingStartDate?:string,bookingEndDate?:string,notes?:string,couponId?:string,cancelled?:boolean,permanentAddressId?:number,postalAddressId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    name;
    /** @type {?RoomType} */
    roomType;
    /** @type {?number} */
    roomNumber;
    /** @type {?number} */
    cost;
    /** @type {?string} */
    bookingStartDate;
    /** @type {?string} */
    bookingEndDate;
    /** @type {?string} */
    notes;
    /** @type {?string} */
    couponId;
    /** @type {?boolean} */
    cancelled;
    /** @type {?number} */
    permanentAddressId;
    /** @type {?number} */
    postalAddressId;
    getTypeName() { return 'UpdateBooking' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class DeleteBooking {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteBooking' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateCoupon {
    /** @param {{id?:string,description?:string,discount?:number,expiryDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    description;
    /** @type {number} */
    discount;
    /** @type {string} */
    expiryDate;
    getTypeName() { return 'CreateCoupon' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class UpdateCoupon {
    /** @param {{id?:string,description?:string,discount?:number,expiryDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    description;
    /** @type {number} */
    discount;
    /** @type {string} */
    expiryDate;
    getTypeName() { return 'UpdateCoupon' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class DeleteCoupon {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'DeleteCoupon' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateAddress {
    /** @param {{addressText?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    addressText;
    getTypeName() { return 'CreateAddress' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class UpdateAddress {
    /** @param {{id?:number,addressText?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    addressText;
    getTypeName() { return 'UpdateAddress' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class CreateFileSystemItem {
    /** @param {{fileAccessType?:FileAccessType,file?:FileSystemFile}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?FileAccessType} */
    fileAccessType;
    /** @type {FileSystemFile} */
    file;
    getTypeName() { return 'CreateFileSystemItem' }
    getMethod() { return 'POST' }
    createResponse() { return new FileSystemItem() }
}
export class CreatePlayer {
    /** @param {{firstName?:string,lastName?:string,email?:string,phoneNumbers?:Phone[],profileId?:number,savedLevelId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    firstName;
    /** @type {?string} */
    lastName;
    /** @type {?string} */
    email;
    /** @type {?Phone[]} */
    phoneNumbers;
    /** @type {number} */
    profileId;
    /** @type {?string} */
    savedLevelId;
    getTypeName() { return 'CreatePlayer' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class UpdatePlayer {
    /** @param {{id?:number,firstName?:string,lastName?:string,email?:string,phoneNumbers?:Phone[],profileId?:number,savedLevelId?:string,capital?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    firstName;
    /** @type {?string} */
    lastName;
    /** @type {?string} */
    email;
    /** @type {?Phone[]} */
    phoneNumbers;
    /** @type {?number} */
    profileId;
    /** @type {?string} */
    savedLevelId;
    /** @type {string} */
    capital;
    getTypeName() { return 'UpdatePlayer' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class DeletePlayer {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeletePlayer' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateProfile {
    /** @param {{role?:PlayerRole,region?:PlayerRegion,username?:string,highScore?:number,gamesPlayed?:number,energy?:number,profileUrl?:string,coverUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {PlayerRole} */
    role;
    /** @type {PlayerRegion} */
    region;
    /** @type {string} */
    username;
    /** @type {number} */
    highScore;
    /** @type {number} */
    gamesPlayed;
    /** @type {number} */
    energy;
    /** @type {?string} */
    profileUrl;
    /** @type {?string} */
    coverUrl;
    getTypeName() { return 'CreateProfile' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class UpdateProfile {
    /** @param {{id?:number,role?:PlayerRole,region?:PlayerRegion,username?:string,highScore?:number,gamesPlayed?:number,energy?:number,profileUrl?:string,coverUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?PlayerRole} */
    role;
    /** @type {?PlayerRegion} */
    region;
    /** @type {?string} */
    username;
    /** @type {?number} */
    highScore;
    /** @type {?number} */
    gamesPlayed;
    /** @type {?number} */
    energy;
    /** @type {?string} */
    profileUrl;
    /** @type {?string} */
    coverUrl;
    getTypeName() { return 'UpdateProfile' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class DeleteProfile {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteProfile' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateGameItem {
    /** @param {{name?:string,description?:string,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    description;
    /** @type {string} */
    imageUrl;
    getTypeName() { return 'CreateGameItem' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class UpdateGameItem {
    /** @param {{name?:string,description?:string,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    description;
    /** @type {?string} */
    imageUrl;
    getTypeName() { return 'UpdateGameItem' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class DeleteGameItem {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'DeleteGameItem' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class DeletePlayerGameItem {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'DeletePlayerGameItem' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreatePlayerGameItem {
    /** @param {{playerId?:number,gameItemName?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    playerId;
    /** @type {string} */
    gameItemName;
    getTypeName() { return 'CreatePlayerGameItem' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class DeleteLevel {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    getTypeName() { return 'DeleteLevel' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateTodo {
    /** @param {{text?:string,isFinished?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    text;
    /** @type {boolean} */
    isFinished;
    getTypeName() { return 'CreateTodo' }
    getMethod() { return 'POST' }
    createResponse() { return new Todo() }
}
export class UpdateTodo {
    /** @param {{id?:number,text?:string,isFinished?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    text;
    /** @type {boolean} */
    isFinished;
    getTypeName() { return 'UpdateTodo' }
    getMethod() { return 'PUT' }
    createResponse() { return new Todo() }
}
export class DeleteTodos {
    /** @param {{id?:number,ids?:number[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number[]} */
    ids;
    getTypeName() { return 'DeleteTodos' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class DeleteTodo {
    /** @param {{id?:number,ids?:number[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number[]} */
    ids;
    getTypeName() { return 'DeleteTodo' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class CreateMqBooking extends AuditBase {
    /** @param {{name?:string,roomType?:RoomType,roomNumber?:number,cost?:number,bookingStartDate?:string,bookingEndDate?:string,notes?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /**
     * @type {string}
     * @description Name this Booking is for */
    name;
    /** @type {RoomType} */
    roomType;
    /** @type {number} */
    roomNumber;
    /** @type {number} */
    cost;
    /** @type {string} */
    bookingStartDate;
    /** @type {?string} */
    bookingEndDate;
    /** @type {?string} */
    notes;
    getTypeName() { return 'CreateMqBooking' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateApiKey {
    /** @param {{key?:string,name?:string,userId?:string,userName?:string,visibleKey?:string,createdDate?:string,expiryDate?:string,cancelledDate?:string,lastUsedDate?:string,scopes?:string,features?:string,restrictTo?:string,environment?:string,notes?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    key;
    /** @type {string} */
    name;
    /** @type {string} */
    userId;
    /** @type {string} */
    userName;
    /** @type {string} */
    visibleKey;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    expiryDate;
    /** @type {string} */
    cancelledDate;
    /** @type {string} */
    lastUsedDate;
    /** @type {string} */
    scopes;
    /** @type {string} */
    features;
    /** @type {string} */
    restrictTo;
    /** @type {string} */
    environment;
    /** @type {string} */
    notes;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'CreateApiKey' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAspNetRoleClaims {
    /** @param {{roleId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    roleId;
    /** @type {string} */
    claimType;
    /** @type {string} */
    claimValue;
    getTypeName() { return 'CreateAspNetRoleClaims' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAspNetRoles {
    /** @param {{id?:string,name?:string,normalizedName?:string,concurrencyStamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    normalizedName;
    /** @type {string} */
    concurrencyStamp;
    getTypeName() { return 'CreateAspNetRoles' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAspNetUserClaims {
    /** @param {{userId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userId;
    /** @type {string} */
    claimType;
    /** @type {string} */
    claimValue;
    getTypeName() { return 'CreateAspNetUserClaims' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAspNetUsers {
    /** @param {{id?:string,firstName?:string,lastName?:string,displayName?:string,profileUrl?:string,refreshToken?:string,refreshTokenExpiry?:string,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:number,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:number,twoFactorEnabled?:number,lockoutEnd?:string,lockoutEnabled?:number,accessFailedCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    displayName;
    /** @type {string} */
    profileUrl;
    /** @type {string} */
    refreshToken;
    /** @type {string} */
    refreshTokenExpiry;
    /** @type {string} */
    userName;
    /** @type {string} */
    normalizedUserName;
    /** @type {string} */
    email;
    /** @type {string} */
    normalizedEmail;
    /** @type {number} */
    emailConfirmed;
    /** @type {string} */
    passwordHash;
    /** @type {string} */
    securityStamp;
    /** @type {string} */
    concurrencyStamp;
    /** @type {string} */
    phoneNumber;
    /** @type {number} */
    phoneNumberConfirmed;
    /** @type {number} */
    twoFactorEnabled;
    /** @type {string} */
    lockoutEnd;
    /** @type {number} */
    lockoutEnabled;
    /** @type {number} */
    accessFailedCount;
    getTypeName() { return 'CreateAspNetUsers' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateCrudEvent {
    /** @param {{eventType?:string,model?:string,modelId?:string,eventDate?:string,rowsUpdated?:number,requestType?:string,requestBody?:string,userAuthId?:string,userAuthName?:string,remoteIp?:string,urn?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    eventType;
    /** @type {string} */
    model;
    /** @type {string} */
    modelId;
    /** @type {string} */
    eventDate;
    /** @type {?number} */
    rowsUpdated;
    /** @type {string} */
    requestType;
    /** @type {string} */
    requestBody;
    /** @type {string} */
    userAuthId;
    /** @type {string} */
    userAuthName;
    /** @type {string} */
    remoteIp;
    /** @type {string} */
    urn;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'CreateCrudEvent' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateEFMigrationsHistory {
    /** @param {{migrationId?:string,productVersion?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    migrationId;
    /** @type {string} */
    productVersion;
    getTypeName() { return 'CreateEFMigrationsHistory' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateFileSystemFile {
    /** @param {{fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    fileName;
    /** @type {string} */
    filePath;
    /** @type {string} */
    contentType;
    /** @type {number} */
    contentLength;
    /** @type {number} */
    fileSystemItemId;
    getTypeName() { return 'CreateFileSystemFile' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateMigration {
    /** @param {{name?:string,description?:string,createdDate?:string,completedDate?:string,connectionString?:string,namedConnection?:string,log?:string,errorCode?:string,errorMessage?:string,errorStackTrace?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {string} */
    description;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    completedDate;
    /** @type {string} */
    connectionString;
    /** @type {string} */
    namedConnection;
    /** @type {string} */
    log;
    /** @type {string} */
    errorCode;
    /** @type {string} */
    errorMessage;
    /** @type {string} */
    errorStackTrace;
    /** @type {string} */
    meta;
    getTypeName() { return 'CreateMigration' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateValidationRule {
    /** @param {{type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    type;
    /** @type {string} */
    field;
    /** @type {string} */
    createdBy;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedBy;
    /** @type {string} */
    modifiedDate;
    /** @type {string} */
    suspendedBy;
    /** @type {string} */
    suspendedDate;
    /** @type {string} */
    notes;
    /** @type {string} */
    validator;
    /** @type {string} */
    condition;
    /** @type {string} */
    errorCode;
    /** @type {string} */
    message;
    getTypeName() { return 'CreateValidationRule' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class DeleteAddress {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteAddress' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteApiKey {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteApiKey' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteAspNetRoleClaims {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteAspNetRoleClaims' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteAspNetRoles {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'DeleteAspNetRoles' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteAspNetUserClaims {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteAspNetUserClaims' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteAspNetUsers {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    getTypeName() { return 'DeleteAspNetUsers' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteCrudEvent {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteCrudEvent' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteEFMigrationsHistory {
    /** @param {{migrationId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    migrationId;
    getTypeName() { return 'DeleteEFMigrationsHistory' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteFileSystemFile {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteFileSystemFile' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteFileSystemItem {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteFileSystemItem' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteMigration {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteMigration' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteValidationRule {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteValidationRule' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class PatchApiKey {
    /** @param {{id?:number,key?:string,name?:string,userId?:string,userName?:string,visibleKey?:string,createdDate?:string,expiryDate?:string,cancelledDate?:string,lastUsedDate?:string,scopes?:string,features?:string,restrictTo?:string,environment?:string,notes?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    key;
    /** @type {string} */
    name;
    /** @type {string} */
    userId;
    /** @type {string} */
    userName;
    /** @type {string} */
    visibleKey;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    expiryDate;
    /** @type {string} */
    cancelledDate;
    /** @type {string} */
    lastUsedDate;
    /** @type {string} */
    scopes;
    /** @type {string} */
    features;
    /** @type {string} */
    restrictTo;
    /** @type {string} */
    environment;
    /** @type {string} */
    notes;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'PatchApiKey' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAspNetRoleClaims {
    /** @param {{id?:number,roleId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    roleId;
    /** @type {string} */
    claimType;
    /** @type {string} */
    claimValue;
    getTypeName() { return 'PatchAspNetRoleClaims' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAspNetRoles {
    /** @param {{id?:string,name?:string,normalizedName?:string,concurrencyStamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    normalizedName;
    /** @type {string} */
    concurrencyStamp;
    getTypeName() { return 'PatchAspNetRoles' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAspNetUserClaims {
    /** @param {{id?:number,userId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    userId;
    /** @type {string} */
    claimType;
    /** @type {string} */
    claimValue;
    getTypeName() { return 'PatchAspNetUserClaims' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAspNetUsers {
    /** @param {{id?:string,firstName?:string,lastName?:string,displayName?:string,profileUrl?:string,refreshToken?:string,refreshTokenExpiry?:string,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:number,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:number,twoFactorEnabled?:number,lockoutEnd?:string,lockoutEnabled?:number,accessFailedCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    displayName;
    /** @type {string} */
    profileUrl;
    /** @type {string} */
    refreshToken;
    /** @type {string} */
    refreshTokenExpiry;
    /** @type {string} */
    userName;
    /** @type {string} */
    normalizedUserName;
    /** @type {string} */
    email;
    /** @type {string} */
    normalizedEmail;
    /** @type {number} */
    emailConfirmed;
    /** @type {string} */
    passwordHash;
    /** @type {string} */
    securityStamp;
    /** @type {string} */
    concurrencyStamp;
    /** @type {string} */
    phoneNumber;
    /** @type {number} */
    phoneNumberConfirmed;
    /** @type {number} */
    twoFactorEnabled;
    /** @type {string} */
    lockoutEnd;
    /** @type {number} */
    lockoutEnabled;
    /** @type {number} */
    accessFailedCount;
    getTypeName() { return 'PatchAspNetUsers' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchCrudEvent {
    /** @param {{id?:number,eventType?:string,model?:string,modelId?:string,eventDate?:string,rowsUpdated?:number,requestType?:string,requestBody?:string,userAuthId?:string,userAuthName?:string,remoteIp?:string,urn?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    eventType;
    /** @type {string} */
    model;
    /** @type {string} */
    modelId;
    /** @type {string} */
    eventDate;
    /** @type {?number} */
    rowsUpdated;
    /** @type {string} */
    requestType;
    /** @type {string} */
    requestBody;
    /** @type {string} */
    userAuthId;
    /** @type {string} */
    userAuthName;
    /** @type {string} */
    remoteIp;
    /** @type {string} */
    urn;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'PatchCrudEvent' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchEFMigrationsHistory {
    /** @param {{migrationId?:string,productVersion?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    migrationId;
    /** @type {string} */
    productVersion;
    getTypeName() { return 'PatchEFMigrationsHistory' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchFileSystemFile {
    /** @param {{id?:number,fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    fileName;
    /** @type {string} */
    filePath;
    /** @type {string} */
    contentType;
    /** @type {number} */
    contentLength;
    /** @type {number} */
    fileSystemItemId;
    getTypeName() { return 'PatchFileSystemFile' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchFileSystemItem {
    /** @param {{id?:number,fileAccessType?:string,applicationUserId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    fileAccessType;
    /** @type {string} */
    applicationUserId;
    getTypeName() { return 'PatchFileSystemItem' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchMigration {
    /** @param {{id?:number,name?:string,description?:string,createdDate?:string,completedDate?:string,connectionString?:string,namedConnection?:string,log?:string,errorCode?:string,errorMessage?:string,errorStackTrace?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    description;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    completedDate;
    /** @type {string} */
    connectionString;
    /** @type {string} */
    namedConnection;
    /** @type {string} */
    log;
    /** @type {string} */
    errorCode;
    /** @type {string} */
    errorMessage;
    /** @type {string} */
    errorStackTrace;
    /** @type {string} */
    meta;
    getTypeName() { return 'PatchMigration' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchTodo {
    /** @param {{id?:number,text?:string,isFinished?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    text;
    /** @type {number} */
    isFinished;
    getTypeName() { return 'PatchTodo' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchValidationRule {
    /** @param {{id?:number,type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    type;
    /** @type {string} */
    field;
    /** @type {string} */
    createdBy;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedBy;
    /** @type {string} */
    modifiedDate;
    /** @type {string} */
    suspendedBy;
    /** @type {string} */
    suspendedDate;
    /** @type {string} */
    notes;
    /** @type {string} */
    validator;
    /** @type {string} */
    condition;
    /** @type {string} */
    errorCode;
    /** @type {string} */
    message;
    getTypeName() { return 'PatchValidationRule' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class UpdateApiKey {
    /** @param {{id?:number,key?:string,name?:string,userId?:string,userName?:string,visibleKey?:string,createdDate?:string,expiryDate?:string,cancelledDate?:string,lastUsedDate?:string,scopes?:string,features?:string,restrictTo?:string,environment?:string,notes?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    key;
    /** @type {string} */
    name;
    /** @type {string} */
    userId;
    /** @type {string} */
    userName;
    /** @type {string} */
    visibleKey;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    expiryDate;
    /** @type {string} */
    cancelledDate;
    /** @type {string} */
    lastUsedDate;
    /** @type {string} */
    scopes;
    /** @type {string} */
    features;
    /** @type {string} */
    restrictTo;
    /** @type {string} */
    environment;
    /** @type {string} */
    notes;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'UpdateApiKey' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAspNetRoleClaims {
    /** @param {{id?:number,roleId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    roleId;
    /** @type {string} */
    claimType;
    /** @type {string} */
    claimValue;
    getTypeName() { return 'UpdateAspNetRoleClaims' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAspNetRoles {
    /** @param {{id?:string,name?:string,normalizedName?:string,concurrencyStamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    normalizedName;
    /** @type {string} */
    concurrencyStamp;
    getTypeName() { return 'UpdateAspNetRoles' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAspNetUserClaims {
    /** @param {{id?:number,userId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    userId;
    /** @type {string} */
    claimType;
    /** @type {string} */
    claimValue;
    getTypeName() { return 'UpdateAspNetUserClaims' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAspNetUsers {
    /** @param {{id?:string,firstName?:string,lastName?:string,displayName?:string,profileUrl?:string,refreshToken?:string,refreshTokenExpiry?:string,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:number,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:number,twoFactorEnabled?:number,lockoutEnd?:string,lockoutEnabled?:number,accessFailedCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    displayName;
    /** @type {string} */
    profileUrl;
    /** @type {string} */
    refreshToken;
    /** @type {string} */
    refreshTokenExpiry;
    /** @type {string} */
    userName;
    /** @type {string} */
    normalizedUserName;
    /** @type {string} */
    email;
    /** @type {string} */
    normalizedEmail;
    /** @type {number} */
    emailConfirmed;
    /** @type {string} */
    passwordHash;
    /** @type {string} */
    securityStamp;
    /** @type {string} */
    concurrencyStamp;
    /** @type {string} */
    phoneNumber;
    /** @type {number} */
    phoneNumberConfirmed;
    /** @type {number} */
    twoFactorEnabled;
    /** @type {string} */
    lockoutEnd;
    /** @type {number} */
    lockoutEnabled;
    /** @type {number} */
    accessFailedCount;
    getTypeName() { return 'UpdateAspNetUsers' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateCrudEvent {
    /** @param {{id?:number,eventType?:string,model?:string,modelId?:string,eventDate?:string,rowsUpdated?:number,requestType?:string,requestBody?:string,userAuthId?:string,userAuthName?:string,remoteIp?:string,urn?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    eventType;
    /** @type {string} */
    model;
    /** @type {string} */
    modelId;
    /** @type {string} */
    eventDate;
    /** @type {?number} */
    rowsUpdated;
    /** @type {string} */
    requestType;
    /** @type {string} */
    requestBody;
    /** @type {string} */
    userAuthId;
    /** @type {string} */
    userAuthName;
    /** @type {string} */
    remoteIp;
    /** @type {string} */
    urn;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'UpdateCrudEvent' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateEFMigrationsHistory {
    /** @param {{migrationId?:string,productVersion?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    migrationId;
    /** @type {string} */
    productVersion;
    getTypeName() { return 'UpdateEFMigrationsHistory' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateFileSystemFile {
    /** @param {{id?:number,fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    fileName;
    /** @type {string} */
    filePath;
    /** @type {string} */
    contentType;
    /** @type {number} */
    contentLength;
    /** @type {number} */
    fileSystemItemId;
    getTypeName() { return 'UpdateFileSystemFile' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateFileSystemItem {
    /** @param {{id?:number,fileAccessType?:string,applicationUserId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    fileAccessType;
    /** @type {string} */
    applicationUserId;
    getTypeName() { return 'UpdateFileSystemItem' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateMigration {
    /** @param {{id?:number,name?:string,description?:string,createdDate?:string,completedDate?:string,connectionString?:string,namedConnection?:string,log?:string,errorCode?:string,errorMessage?:string,errorStackTrace?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    description;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    completedDate;
    /** @type {string} */
    connectionString;
    /** @type {string} */
    namedConnection;
    /** @type {string} */
    log;
    /** @type {string} */
    errorCode;
    /** @type {string} */
    errorMessage;
    /** @type {string} */
    errorStackTrace;
    /** @type {string} */
    meta;
    getTypeName() { return 'UpdateMigration' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateValidationRule {
    /** @param {{id?:number,type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    type;
    /** @type {string} */
    field;
    /** @type {string} */
    createdBy;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedBy;
    /** @type {string} */
    modifiedDate;
    /** @type {string} */
    suspendedBy;
    /** @type {string} */
    suspendedDate;
    /** @type {string} */
    notes;
    /** @type {string} */
    validator;
    /** @type {string} */
    condition;
    /** @type {string} */
    errorCode;
    /** @type {string} */
    message;
    getTypeName() { return 'UpdateValidationRule' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}

