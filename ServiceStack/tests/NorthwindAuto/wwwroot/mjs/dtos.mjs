/* Options:
Date: 2026-09-02 11:42:29
Version: 10.15
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
    /** @type {?string} */
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
export class OrderItemOption {
    /** @param {{type?:string,name?:string,quantity?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description Option group from the menu, e.g. Milks, Syrups, Sweeteners or Toppings */
    type;
    /**
     * @type {string}
     * @description Exact option name from that menu option group */
    name;
    /**
     * @type {?string}
     * @description Optional quantity label: no, light, regular or extra. Use only where the menu allows quantity */
    quantity;
}
export class OrderItemRequest {
    /** @param {{productId?:number,quantity?:number,size?:string,temperature?:string,options?:OrderItemOption[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {number}
     * @description Product ID returned by GetCoffeeShopMenu */
    productId;
    /**
     * @type {number}
     * @description Number of this configured item to order */
    quantity;
    /**
     * @type {?string}
     * @description Exact size supported by the product category; omit to use its default */
    size;
    /**
     * @type {?string}
     * @description Exact temperature supported by the product category; omit to use its default */
    temperature;
    /**
     * @type {OrderItemOption[]}
     * @description Requested customizations. Each option must be valid for the product category */
    options = [];
}
export class SubType {
    /** @param {{id?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
}
export class Data1 {
    /** @param {{value?:number,optionalValue?:number,text?:string,optionalText?:string,texts?:string[],optionalTexts?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    value;
    /** @type {?number} */
    optionalValue;
    /** @type {string} */
    text;
    /** @type {?string} */
    optionalText;
    /** @type {string[]} */
    texts = [];
    /** @type {?string[]} */
    optionalTexts;
}
export class Data2 {
    /** @param {{value?:number,optionalValue?:number,text?:string,optionalText?:string,texts?:string[],optionalTexts?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    value;
    /** @type {number} */
    optionalValue;
    /** @type {string} */
    text;
    /** @type {string} */
    optionalText;
    /** @type {string[]} */
    texts = [];
    /** @type {string[]} */
    optionalTexts = [];
}
export class Data3 {
    /** @param {{value?:number,optionalValue?:number,text?:string,text2?:string,nText?:string,nText2?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    value;
    /** @type {?number} */
    optionalValue;
    /** @type {string} */
    text;
    /** @type {string} */
    text2;
    /** @type {string} */
    nText;
    /** @type {?string} */
    nText2;
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
export class OptionalClass {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
}
/** @typedef {'Value1'} */
export var OptionalEnum;
(function (OptionalEnum) {
    OptionalEnum["Value1"] = "Value1"
})(OptionalEnum || (OptionalEnum = {}));
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
export class AiContent {
    /** @param {{type?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The type of the content part. */
    type;
}
export class ToolFunction {
    /** @param {{name?:string,arguments?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The name of the function to call. */
    name;
    /**
     * @type {string}
     * @description The arguments to call the function with, as generated by the model in JSON format. Note that the model does not always generate valid JSON, and may hallucinate parameters not defined by your function schema. Validate the arguments in your code before calling your function. */
    arguments;
}
export class ToolCall {
    /** @param {{id?:string,type?:string,function?:ToolFunction}} [init] */
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
     * @type {ToolFunction}
     * @description The function that the model called. */
    function;
}
export class AiMessage {
    /** @param {{content?:AiContent[],role?:string,name?:string,tool_calls?:ToolCall[],tool_call_id?:string,reasoning?:string,reasoning_content?:string,timestamp?:number,images?:AiContent[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {?AiContent[]}
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
    /**
     * @type {?string}
     * @description The reasoning an assistant message was generated with, normalized per provider when replayed as history. */
    reasoning;
    /**
     * @type {?string}
     * @description The reasoning an assistant message was generated with, as emitted by Gemini and most OpenAI-compatible providers. */
    reasoning_content;
    /**
     * @type {?number}
     * @description Unix timestamp (in milliseconds) the message was generated. */
    timestamp;
    /**
     * @type {?AiContent[]}
     * @description Images attached to the message. Folded into `content` parts before sending to a provider. */
    images;
}
export class AiChatAudio {
    /** @param {{format?:string,voice?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16. */
    format;
    /**
     * @type {string}
     * @description The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer. */
    voice;
}
/** @typedef {'text'|'json_object'} */
export var ResponseFormat;
(function (ResponseFormat) {
    ResponseFormat["Text"] = "text"
    ResponseFormat["JsonObject"] = "json_object"
})(ResponseFormat || (ResponseFormat = {}));
export class AiResponseFormat {
    /** @param {{type?:ResponseFormat}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {ResponseFormat}
     * @description An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106. */
    type;
}
/** @typedef {'function'} */
export var ToolType;
(function (ToolType) {
    ToolType["Function"] = "function"
})(ToolType || (ToolType = {}));
export class AiToolFunction {
    /** @param {{name?:string,description?:string,parameters?:{ [index:string]: Object; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {?string}
     * @description The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64. */
    name;
    /**
     * @type {?string}
     * @description A description of what the function does, used by the model to choose when and how to call the function. */
    description;
    /**
     * @type {?{ [index:string]: Object; }}
     * @description The parameters the functions accepts, described as a JSON Schema object. See the guide for examples, and the JSON Schema reference for documentation about the format. */
    parameters;
}
export class Tool {
    /** @param {{type?:ToolType,function?:AiToolFunction}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {ToolType}
     * @description The type of the tool. Currently, only function is supported. */
    type;
    /**
     * @type {?AiToolFunction}
     * @description The function definition the model may call. */
    function;
}
export class QueryBase {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    skip;
    /** @type {?number} */
    take;
    /** @type {?string} */
    orderBy;
    /** @type {?string} */
    orderByDesc;
    /** @type {?string} */
    include;
    /** @type {?string} */
    fields;
    /** @type {?{ [index:string]: string; }} */
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
export class AgentRun {
    /** @param {{id?:number,threadId?:number,user?:string,status?:string,nextAction?:string,model?:string,stepCount?:number,sliceCount?:number,maxSteps?:number,contextTokens?:number,contextLimit?:number,leaseOwner?:string,leaseExpiresAt?:string,nextAttemptAt?:string,error?:string,createdAt?:string,updatedAt?:string,completedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {string} */
    status;
    /** @type {?string} */
    nextAction;
    /** @type {?string} */
    model;
    /** @type {number} */
    stepCount;
    /** @type {number} */
    sliceCount;
    /** @type {number} */
    maxSteps;
    /** @type {?number} */
    contextTokens;
    /** @type {?number} */
    contextLimit;
    /** @type {?string} */
    leaseOwner;
    /** @type {?string} */
    leaseExpiresAt;
    /** @type {?string} */
    nextAttemptAt;
    /** @type {?string} */
    error;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    completedAt;
}
export class AgentStep {
    /** @param {{id?:number,runId?:number,sequence?:number,type?:string,status?:string,input?:string,output?:string,idempotencyKey?:string,attempt?:number,error?:string,startedAt?:string,completedAt?:string,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    runId;
    /** @type {number} */
    sequence;
    /** @type {string} */
    type;
    /** @type {string} */
    status;
    /** @type {?string} */
    input;
    /** @type {?string} */
    output;
    /** @type {string} */
    idempotencyKey;
    /** @type {number} */
    attempt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {string} */
    createdAt;
}
export class AichatDocument {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,filename?:string,url?:string,hash?:string,size?:number,displayName?:string,name?:string,customMetadata?:string,createTime?:string,updateTime?:string,sizeBytes?:number,mimeType?:string,state?:string,category?:string,tags?:string,startedAt?:string,uploadedAt?:string,metadata?:string,error?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    filename;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?number} */
    size;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    name;
    /** @type {?string} */
    customMetadata;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    mimeType;
    /** @type {?string} */
    state;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    uploadedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
}
export class AichatFilestore {
    /** @param {{id?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,displayName?:string,createTime?:string,updateTime?:string,activeDocumentsCount?:number,pendingDocumentsCount?:number,failedDocumentsCount?:number,sizeBytes?:number,metadata?:string,error?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    activeDocumentsCount;
    /** @type {?number} */
    pendingDocumentsCount;
    /** @type {?number} */
    failedDocumentsCount;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
}
export class AichatMedia {
    /** @param {{id?:number,user?:string,name?:string,type?:string,prompt?:string,model?:string,created?:string,cost?:number,seed?:number,url?:string,hash?:string,aspectRatio?:string,width?:number,height?:number,size?:number,duration?:number,reactions?:string,caption?:string,description?:string,phash?:string,color?:string,category?:string,tags?:string,rating?:string,ratings?:string,objects?:string,variantId?:string,variantName?:string,publishedAt?:string,publishedUrl?:string,metadata?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {?string} */
    prompt;
    /** @type {?string} */
    model;
    /** @type {string} */
    created;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    seed;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?string} */
    aspectRatio;
    /** @type {?number} */
    width;
    /** @type {?number} */
    height;
    /** @type {?number} */
    size;
    /** @type {?number} */
    duration;
    /** @type {?string} */
    reactions;
    /** @type {?string} */
    caption;
    /** @type {?string} */
    description;
    /** @type {?string} */
    phash;
    /** @type {?string} */
    color;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    rating;
    /** @type {?string} */
    ratings;
    /** @type {?string} */
    objects;
    /** @type {?string} */
    variantId;
    /** @type {?string} */
    variantName;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    /** @type {?string} */
    metadata;
}
export class AspNetRoleClaims {
    /** @param {{id?:number,roleId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    roleId;
    /** @type {?string} */
    claimType;
    /** @type {?string} */
    claimValue;
}
export class AspNetRoles {
    /** @param {{id?:string,name?:string,normalizedName?:string,concurrencyStamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    /** @type {?string} */
    name;
    /** @type {?string} */
    normalizedName;
    /** @type {?string} */
    concurrencyStamp;
}
export class AspNetUserClaims {
    /** @param {{id?:number,userId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    userId;
    /** @type {?string} */
    claimType;
    /** @type {?string} */
    claimValue;
}
export class AspNetUsers {
    /** @param {{id?:string,firstName?:string,lastName?:string,displayName?:string,profileUrl?:string,refreshToken?:string,refreshTokenExpiry?:string,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:number,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:number,twoFactorEnabled?:number,lockoutEnd?:string,lockoutEnabled?:number,accessFailedCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
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
    /** @type {?string} */
    userName;
    /** @type {?string} */
    normalizedUserName;
    /** @type {?string} */
    email;
    /** @type {?string} */
    normalizedEmail;
    /** @type {number} */
    emailConfirmed;
    /** @type {?string} */
    passwordHash;
    /** @type {?string} */
    securityStamp;
    /** @type {?string} */
    concurrencyStamp;
    /** @type {?string} */
    phoneNumber;
    /** @type {number} */
    phoneNumberConfirmed;
    /** @type {number} */
    twoFactorEnabled;
    /** @type {?string} */
    lockoutEnd;
    /** @type {number} */
    lockoutEnabled;
    /** @type {number} */
    accessFailedCount;
}
export class Product {
    /** @param {{id?:number,categoryId?:number,name?:string,cost?:number,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    categoryId;
    /** @type {string} */
    name;
    /** @type {number} */
    cost;
    /** @type {?string} */
    imageUrl;
}
export class CategoryOption {
    /** @param {{id?:number,categoryId?:number,optionId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    categoryId;
    /** @type {number} */
    optionId;
}
export class Category {
    /** @param {{id?:number,name?:string,description?:string,temperatures?:string[],defaultTemperature?:string,sizes?:string[],defaultSize?:string,imageUrl?:string,products?:Product[],categoryOptions?:CategoryOption[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    description;
    /** @type {?string[]} */
    temperatures;
    /** @type {?string} */
    defaultTemperature;
    /** @type {?string[]} */
    sizes;
    /** @type {?string} */
    defaultSize;
    /** @type {?string} */
    imageUrl;
    /** @type {Product[]} */
    products = [];
    /** @type {CategoryOption[]} */
    categoryOptions = [];
}
export class ChatAssistantConversation {
    /** @param {{id?:number,assistantId?:number,user?:string,createdAt?:string,updatedAt?:string,sessionId?:string,origin?:string,pageUrl?:string,userAgent?:string,title?:string,status?:string,messageCount?:number,lastMessage?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    assistantId;
    /** @type {?string} */
    user;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    sessionId;
    /** @type {?string} */
    origin;
    /** @type {?string} */
    pageUrl;
    /** @type {?string} */
    userAgent;
    /** @type {?string} */
    title;
    /** @type {?string} */
    status;
    /** @type {number} */
    messageCount;
    /** @type {?string} */
    lastMessage;
}
export class ChatAssistantMessage {
    /** @param {{id?:number,conversationId?:number,createdAt?:string,role?:string,content?:string,citations?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    conversationId;
    /** @type {string} */
    createdAt;
    /** @type {?string} */
    role;
    /** @type {?string} */
    content;
    /** @type {?string} */
    citations;
    /** @type {?string} */
    error;
}
export class ChatAssistant {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,publicId?:string,enabled?:boolean,publishedAt?:string,config?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    publicId;
    /** @type {boolean} */
    enabled;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    config;
}
export class ChatDocument {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,filename?:string,url?:string,hash?:string,size?:number,displayName?:string,name?:string,customMetadata?:string,createTime?:string,updateTime?:string,sizeBytes?:number,mimeType?:string,state?:string,category?:string,sourceUrl?:string,sourceId?:number,sourceScopeId?:number,sourceKey?:string,sourceEtag?:string,contentHash?:string,metadataHash?:string,extractorVer?:string,tombstonedAt?:string,categoryPath?:string,docType?:string,status?:string,locale?:string,product?:string,versions?:string,sourceUpdatedAt?:number,tags?:string,startedAt?:string,uploadedAt?:string,metadata?:string,error?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    filename;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?number} */
    size;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    name;
    /** @type {?string} */
    customMetadata;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    mimeType;
    /** @type {?string} */
    state;
    /** @type {?string} */
    category;
    /** @type {?string} */
    sourceUrl;
    /** @type {?number} */
    sourceId;
    /** @type {number} */
    sourceScopeId;
    /** @type {?string} */
    sourceKey;
    /** @type {?string} */
    sourceEtag;
    /** @type {?string} */
    contentHash;
    /** @type {?string} */
    metadataHash;
    /** @type {?string} */
    extractorVer;
    /** @type {?string} */
    tombstonedAt;
    /** @type {?string} */
    categoryPath;
    /** @type {?string} */
    docType;
    /** @type {?string} */
    status;
    /** @type {?string} */
    locale;
    /** @type {?string} */
    product;
    /** @type {?string} */
    versions;
    /** @type {?number} */
    sourceUpdatedAt;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    uploadedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
}
export class ChatFilestore {
    /** @param {{id?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,displayName?:string,createTime?:string,updateTime?:string,activeDocumentsCount?:number,pendingDocumentsCount?:number,failedDocumentsCount?:number,sizeBytes?:number,metadata?:string,error?:string,ref?:string,visibility?:string,facets?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    activeDocumentsCount;
    /** @type {?number} */
    pendingDocumentsCount;
    /** @type {?number} */
    failedDocumentsCount;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    visibility;
    /** @type {?string} */
    facets;
}
export class ChatMedia {
    /** @param {{id?:number,user?:string,name?:string,type?:string,prompt?:string,model?:string,created?:string,cost?:number,seed?:number,url?:string,hash?:string,aspectRatio?:string,width?:number,height?:number,size?:number,duration?:number,reactions?:string,caption?:string,description?:string,phash?:string,color?:string,category?:string,tags?:string,rating?:string,ratings?:string,objects?:string,variantId?:string,variantName?:string,publishedAt?:string,publishedUrl?:string,metadata?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {?string} */
    prompt;
    /** @type {?string} */
    model;
    /** @type {string} */
    created;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    seed;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?string} */
    aspectRatio;
    /** @type {?number} */
    width;
    /** @type {?number} */
    height;
    /** @type {?number} */
    size;
    /** @type {?number} */
    duration;
    /** @type {?string} */
    reactions;
    /** @type {?string} */
    caption;
    /** @type {?string} */
    description;
    /** @type {?string} */
    phash;
    /** @type {?string} */
    color;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    rating;
    /** @type {?string} */
    ratings;
    /** @type {?string} */
    objects;
    /** @type {?string} */
    variantId;
    /** @type {?string} */
    variantName;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    /** @type {?string} */
    metadata;
}
export class ChatMessage {
    /** @param {{id?:number,threadId?:number,sequence?:number,runId?:number,stepId?:number,role?:string,message?:string,timestamp?:number,toolCallId?:string,toolName?:string,tokenCount?:number,active?:boolean,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    threadId;
    /** @type {number} */
    sequence;
    /** @type {?number} */
    runId;
    /** @type {?number} */
    stepId;
    /** @type {string} */
    role;
    /** @type {string} */
    message;
    /** @type {?number} */
    timestamp;
    /** @type {?string} */
    toolCallId;
    /** @type {?string} */
    toolName;
    /** @type {?number} */
    tokenCount;
    /** @type {boolean} */
    active;
    /** @type {string} */
    createdAt;
}
export class ChatRequest {
    /** @param {{id?:number,user?:string,threadId?:number,createdAt?:string,updatedAt?:string,title?:string,model?:string,duration?:number,cost?:number,inputPrice?:number,inputTokens?:number,inputCachedTokens?:number,outputPrice?:number,outputTokens?:number,totalTokens?:number,usage?:string,provider?:string,providerModel?:string,providerRef?:string,finishReason?:string,startedAt?:string,completedAt?:string,error?:string,stackTrace?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?number} */
    threadId;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    title;
    /** @type {?string} */
    model;
    /** @type {?number} */
    duration;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    inputPrice;
    /** @type {?number} */
    inputTokens;
    /** @type {?number} */
    inputCachedTokens;
    /** @type {?number} */
    outputPrice;
    /** @type {?number} */
    outputTokens;
    /** @type {?number} */
    totalTokens;
    /** @type {?string} */
    usage;
    /** @type {?string} */
    provider;
    /** @type {?string} */
    providerModel;
    /** @type {?string} */
    providerRef;
    /** @type {?string} */
    finishReason;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    stackTrace;
    /** @type {?string} */
    ref;
}
export class ChatSourceRun {
    /** @param {{id?:number,sourceId?:number,user?:string,startedAt?:string,completedAt?:string,status?:string,dryRun?:boolean,discovered?:number,added?:number,changed?:number,metadataOnly?:number,unchanged?:number,removed?:number,skipped?:number,failed?:number,bytes?:number,plan?:string,log?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    sourceId;
    /** @type {?string} */
    user;
    /** @type {string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    status;
    /** @type {boolean} */
    dryRun;
    /** @type {number} */
    discovered;
    /** @type {number} */
    added;
    /** @type {number} */
    changed;
    /** @type {number} */
    metadataOnly;
    /** @type {number} */
    unchanged;
    /** @type {number} */
    removed;
    /** @type {number} */
    skipped;
    /** @type {number} */
    failed;
    /** @type {number} */
    bytes;
    /** @type {?string} */
    plan;
    /** @type {?string} */
    log;
    /** @type {?string} */
    error;
}
export class ChatSource {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,type?:string,enabled?:boolean,config?:string,category?:string,rules?:string,include?:string,exclude?:string,extract?:string,chunking?:string,volatile?:string,extractorVer?:string,schedule?:string,onDelete?:string,cursor?:string,lastRunId?:number,lastRunAt?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {boolean} */
    enabled;
    /** @type {?string} */
    config;
    /** @type {?string} */
    category;
    /** @type {?string} */
    rules;
    /** @type {?string} */
    include;
    /** @type {?string} */
    exclude;
    /** @type {?string} */
    extract;
    /** @type {?string} */
    chunking;
    /** @type {?string} */
    volatile;
    /** @type {?string} */
    extractorVer;
    /** @type {?string} */
    schedule;
    /** @type {?string} */
    onDelete;
    /** @type {?string} */
    cursor;
    /** @type {?number} */
    lastRunId;
    /** @type {?string} */
    lastRunAt;
    /** @type {?string} */
    error;
}
export class ChatThread {
    /** @param {{id?:number,user?:string,createdAt?:string,updatedAt?:string,title?:string,systemPrompt?:string,model?:string,modelInfo?:string,modalities?:string,messages?:string,streamingMessage?:string,args?:string,tools?:string,toolHistory?:string,cost?:number,inputTokens?:number,outputTokens?:number,stats?:string,provider?:string,providerModel?:string,startedAt?:string,completedAt?:string,metadata?:string,status?:string,error?:string,ref?:string,providerResponse?:string,contextTokens?:number,parentId?:number,publishedAt?:string,publishedUrl?:string,sig?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    title;
    /** @type {?string} */
    systemPrompt;
    /** @type {?string} */
    model;
    /** @type {?string} */
    modelInfo;
    /** @type {?string} */
    modalities;
    /** @type {?string} */
    messages;
    /** @type {?string} */
    streamingMessage;
    /** @type {?string} */
    args;
    /** @type {?string} */
    tools;
    /** @type {?string} */
    toolHistory;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    inputTokens;
    /** @type {?number} */
    outputTokens;
    /** @type {?string} */
    stats;
    /** @type {?string} */
    provider;
    /** @type {?string} */
    providerModel;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    status;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    providerResponse;
    /** @type {?number} */
    contextTokens;
    /** @type {?number} */
    parentId;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    /** @type {string} */
    sig;
}
export class ChatToolApprovalBatch {
    /** @param {{id?:string,threadId?:number,user?:string,status?:string,createdAt?:string,updatedAt?:string,completedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {string} */
    status;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    completedAt;
}
export class ChatToolApproval {
    /** @param {{id?:number,batchId?:string,threadId?:number,user?:string,toolCallId?:string,toolName?:string,apiName?:string,requestType?:string,method?:string,route?:string,safety?:string,status?:string,sequence?:number,description?:string,schema?:string,proposedArgs?:string,effectiveArgs?:string,result?:string,toolResult?:string,error?:string,reason?:string,createdAt?:string,updatedAt?:string,resolvedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    batchId;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {string} */
    toolCallId;
    /** @type {string} */
    toolName;
    /** @type {string} */
    apiName;
    /** @type {?string} */
    requestType;
    /** @type {?string} */
    method;
    /** @type {?string} */
    route;
    /** @type {string} */
    safety;
    /** @type {string} */
    status;
    /** @type {number} */
    sequence;
    /** @type {?string} */
    description;
    /** @type {string} */
    schema;
    /** @type {string} */
    proposedArgs;
    /** @type {?string} */
    effectiveArgs;
    /** @type {?string} */
    result;
    /** @type {?string} */
    toolResult;
    /** @type {?string} */
    error;
    /** @type {?string} */
    reason;
    /** @type {string} */
    createdAt;
    /** @type {string} */
    updatedAt;
    /** @type {?string} */
    resolvedAt;
}
export class CoffeeShopOrderItem {
    /** @param {{id?:number,coffeeShopOrderId?:number,productId?:number,productName?:string,quantity?:number,size?:string,temperature?:string,optionsJson?:string,unitPrice?:number,lineTotal?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    coffeeShopOrderId;
    /** @type {number} */
    productId;
    /** @type {string} */
    productName;
    /** @type {number} */
    quantity;
    /** @type {?string} */
    size;
    /** @type {?string} */
    temperature;
    /** @type {?string} */
    optionsJson;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    lineTotal;
}
export class CoffeeShopOrder {
    /** @param {{id?:number,orderNumber?:string,customerName?:string,customerUserId?:string,status?:string,notes?:string,subtotal?:number,createdDate?:string,items?:CoffeeShopOrderItem[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    orderNumber;
    /** @type {string} */
    customerName;
    /** @type {?string} */
    customerUserId;
    /** @type {string} */
    status;
    /** @type {?string} */
    notes;
    /** @type {number} */
    subtotal;
    /** @type {string} */
    createdDate;
    /** @type {CoffeeShopOrderItem[]} */
    items = [];
}
export class ContextSnapshot {
    /** @param {{id?:number,threadId?:number,runId?:number,version?:number,fromSequence?:number,toSequence?:number,summary?:string,tokenCount?:number,model?:string,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    threadId;
    /** @type {?number} */
    runId;
    /** @type {number} */
    version;
    /** @type {number} */
    fromSequence;
    /** @type {number} */
    toSequence;
    /** @type {string} */
    summary;
    /** @type {?number} */
    tokenCount;
    /** @type {?string} */
    model;
    /** @type {string} */
    createdAt;
}
export class EFMigrationsHistory {
    /** @param {{migrationId?:string,productVersion?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    migrationId;
    /** @type {string} */
    productVersion;
}
export class EFMigrationsLock {
    /** @param {{id?:number,timestamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    timestamp;
}
export class Migration {
    /** @param {{id?:number,name?:string,description?:string,createdDate?:string,completedDate?:string,connectionString?:string,namedConnection?:string,log?:string,errorCode?:string,errorMessage?:string,errorStackTrace?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    name;
    /** @type {?string} */
    description;
    /** @type {string} */
    createdDate;
    /** @type {?string} */
    completedDate;
    /** @type {?string} */
    connectionString;
    /** @type {?string} */
    namedConnection;
    /** @type {?string} */
    log;
    /** @type {?string} */
    errorCode;
    /** @type {?string} */
    errorMessage;
    /** @type {?string} */
    errorStackTrace;
    /** @type {?string} */
    meta;
}
export class OptionQuantity {
    /** @param {{id?:number,name?:string,value?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {number} */
    value;
}
export class Option {
    /** @param {{id?:number,type?:string,names?:string[],allowQuantity?:boolean,quantityLabel?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    type;
    /** @type {string[]} */
    names = [];
    /** @type {?boolean} */
    allowQuantity;
    /** @type {?string} */
    quantityLabel;
}
export class ValidateRule {
    /** @param {{validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    validator;
    /** @type {?string} */
    condition;
    /** @type {?string} */
    errorCode;
    /** @type {?string} */
    message;
}
export class ValidationRule extends ValidateRule {
    /** @param {{id?:number,type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    type;
    /** @type {?string} */
    field;
    /** @type {?string} */
    createdBy;
    /** @type {?string} */
    createdDate;
    /** @type {?string} */
    modifiedBy;
    /** @type {?string} */
    modifiedDate;
    /** @type {?string} */
    suspendedBy;
    /** @type {?string} */
    suspendedDate;
    /** @type {?string} */
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
    /** @type {?boolean} */
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
    /** @type {?{ [index:string]: string; }} */
    meta;
}
export class ResponseStatus {
    /** @param {{errorCode?:string,message?:string,stackTrace?:string,errors?:ResponseError[],meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    errorCode;
    /** @type {?string} */
    message;
    /** @type {?string} */
    stackTrace;
    /** @type {?ResponseError[]} */
    errors;
    /** @type {?{ [index:string]: string; }} */
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
export class MenuProduct {
    /** @param {{id?:number,name?:string,cost?:number,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {number} */
    cost;
    /** @type {?string} */
    imageUrl;
}
export class MenuOption {
    /** @param {{type?:string,names?:string[],allowQuantity?:boolean,quantityLabel?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    type;
    /** @type {string[]} */
    names = [];
    /** @type {boolean} */
    allowQuantity;
    /** @type {?string} */
    quantityLabel;
}
export class MenuCategory {
    /** @param {{id?:number,name?:string,description?:string,temperatures?:string[],defaultTemperature?:string,sizes?:string[],defaultSize?:string,imageUrl?:string,products?:MenuProduct[],options?:MenuOption[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
    /** @type {string} */
    description;
    /** @type {string[]} */
    temperatures = [];
    /** @type {?string} */
    defaultTemperature;
    /** @type {string[]} */
    sizes = [];
    /** @type {?string} */
    defaultSize;
    /** @type {?string} */
    imageUrl;
    /** @type {MenuProduct[]} */
    products = [];
    /** @type {MenuOption[]} */
    options = [];
}
export class PricedOrderItem {
    /** @param {{productId?:number,productName?:string,quantity?:number,size?:string,temperature?:string,options?:OrderItemOption[],unitPrice?:number,lineTotal?:number,summary?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    productId;
    /** @type {string} */
    productName;
    /** @type {number} */
    quantity;
    /** @type {?string} */
    size;
    /** @type {?string} */
    temperature;
    /** @type {OrderItemOption[]} */
    options = [];
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    lineTotal;
    /** @type {string} */
    summary;
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
export class ListResult {
    /** @param {{result?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    result;
}
export class UrlCitation {
    /** @param {{end_index?:number,start_index?:number,title?:string,url?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {number}
     * @description The index of the last character of the URL citation in the message. */
    end_index;
    /**
     * @type {number}
     * @description The index of the first character of the URL citation in the message. */
    start_index;
    /**
     * @type {string}
     * @description The title of the web resource. */
    title;
    /**
     * @type {string}
     * @description The URL of the web resource. */
    url;
}
export class ChoiceAnnotation {
    /** @param {{type?:string,url_citation?:UrlCitation}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The type of the URL citation. Always url_citation. */
    type;
    /**
     * @type {UrlCitation}
     * @description A URL citation when using web search. */
    url_citation;
}
export class ChoiceAudio {
    /** @param {{data?:string,expires_at?:number,id?:string,transcript?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description Base64 encoded audio bytes generated by the model, in the format specified in the request. */
    data;
    /**
     * @type {number}
     * @description The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations. */
    expires_at;
    /**
     * @type {string}
     * @description Unique identifier for this audio response. */
    id;
    /**
     * @type {string}
     * @description Transcript of the audio generated by the model. */
    transcript;
}
export class ChoiceMessage {
    /** @param {{content?:string,refusal?:string,reasoning?:string,reasoning_content?:string,thinking?:string,role?:string,timestamp?:number,tool_call_id?:string,images?:AiContent[],audios?:AiContent[],files?:AiContent[],annotations?:ChoiceAnnotation[],audio?:ChoiceAudio,tool_calls?:ToolCall[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The contents of the message. */
    content;
    /**
     * @type {?string}
     * @description The refusal message generated by the model. */
    refusal;
    /**
     * @type {?string}
     * @description The reasoning process used by the model. */
    reasoning;
    /**
     * @type {?string}
     * @description The reasoning process used by the model, as emitted by Gemini and most OpenAI-compatible providers. */
    reasoning_content;
    /**
     * @type {?string}
     * @description The reasoning process used by the model, as emitted by Anthropic. */
    thinking;
    /**
     * @type {string}
     * @description The role of the author of this message. */
    role;
    /**
     * @type {?number}
     * @description Unix timestamp (in milliseconds) the message was generated. */
    timestamp;
    /**
     * @type {?string}
     * @description The tool call this message is responding to, set on `tool` role messages in tool_history. */
    tool_call_id;
    /**
     * @type {?AiContent[]}
     * @description Images generated by the model or produced by a tool call. */
    images;
    /**
     * @type {?AiContent[]}
     * @description Audio generated by the model or produced by a tool call. */
    audios;
    /**
     * @type {?AiContent[]}
     * @description Files produced by a tool call. */
    files;
    /**
     * @type {?ChoiceAnnotation[]}
     * @description Annotations for the message, when applicable, as when using the web search tool. */
    annotations;
    /**
     * @type {?ChoiceAudio}
     * @description If the audio output modality is requested, this object contains data about the audio response from the model. */
    audio;
    /**
     * @type {?ToolCall[]}
     * @description The tool calls generated by the model, such as function calls. */
    tool_calls;
}
export class LogprobItem {
    /** @param {{token?:string,logprob?:number,bytes?:string,top_logprobs?:LogprobItem[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The token. */
    token;
    /**
     * @type {number}
     * @description The log probability of this token, if it is within the top 20 most likely tokens. Otherwise, the value `-9999`.0 is used to signify that the token is very unlikely. */
    logprob;
    /**
     * @type {string}
     * @description A list of integers representing the UTF-8 bytes representation of the token. Useful in instances where characters are represented by multiple tokens and their byte representations must be combined to generate the correct text representation. Can be `null` if there is no bytes representation for the token. */
    bytes = [];
    /**
     * @type {LogprobItem[]}
     * @description List of the most likely tokens and their log probability, at this token position. In rare cases, there may be fewer than the number of requested `top_logprobs` returned. */
    top_logprobs = [];
}
export class Logprobs {
    /** @param {{content?:LogprobItem[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {LogprobItem[]}
     * @description A list of message content tokens with log probability information. */
    content = [];
}
export class Choice {
    /** @param {{finish_reason?:string,index?:number,message?:ChoiceMessage,logprobs?:Logprobs}} [init] */
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
    /**
     * @type {?Logprobs}
     * @description Log probability information for the choice. */
    logprobs;
}
export class AiCompletionUsage {
    /** @param {{accepted_prediction_tokens?:number,audio_tokens?:number,reasoning_tokens?:number,rejected_prediction_tokens?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {number}
     * @description When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.

 */
    accepted_prediction_tokens;
    /**
     * @type {number}
     * @description Audio input tokens generated by the model. */
    audio_tokens;
    /**
     * @type {number}
     * @description Tokens generated by the model for reasoning. */
    reasoning_tokens;
    /**
     * @type {number}
     * @description When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion. */
    rejected_prediction_tokens;
}
export class AiPromptUsage {
    /** @param {{accepted_prediction_tokens?:number,audio_tokens?:number,cached_tokens?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {number}
     * @description When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.

 */
    accepted_prediction_tokens;
    /**
     * @type {number}
     * @description Audio input tokens present in the prompt. */
    audio_tokens;
    /**
     * @type {number}
     * @description Cached tokens present in the prompt. */
    cached_tokens;
}
export class AiUsage {
    /** @param {{completion_tokens?:number,prompt_tokens?:number,total_tokens?:number,completion_tokens_details?:AiCompletionUsage,prompt_tokens_details?:AiPromptUsage,duration?:number}} [init] */
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
    /**
     * @type {?AiCompletionUsage}
     * @description Breakdown of tokens used in a completion. */
    completion_tokens_details;
    /**
     * @type {?AiPromptUsage}
     * @description Breakdown of tokens used in the prompt. */
    prompt_tokens_details;
    /**
     * @type {?number}
     * @description Seconds spent servicing the completion, including every request in the tool loop. */
    duration;
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
    results = [];
    /** @type {?{ [index:string]: string; }} */
    meta;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AiTextContent extends AiContent {
    /** @param {{text?:string,type?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /**
     * @type {string}
     * @description The text content. */
    text;
}
export class AiImageUrl {
    /** @param {{url?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description Either a URL of the image or the base64 encoded image data. */
    url;
}
export class AiImageContent extends AiContent {
    /** @param {{image_url?:AiImageUrl,type?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /**
     * @type {AiImageUrl}
     * @description The image for this content. */
    image_url;
}
export class AiInputAudio {
    /** @param {{data?:string,format?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description URL or Base64 encoded audio data. */
    data;
    /**
     * @type {string}
     * @description The format of the encoded audio data. Currently supports 'wav' and 'mp3'. */
    format;
}
export class AiAudioContent extends AiContent {
    /** @param {{input_audio?:AiInputAudio,type?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /**
     * @type {AiInputAudio}
     * @description The audio input for this content. */
    input_audio;
}
export class AiFile {
    /** @param {{file_data?:string,filename?:string,file_id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description The URL or base64 encoded file data, used when passing the file to the model as a string. */
    file_data;
    /**
     * @type {string}
     * @description The name of the file, used when passing the file to the model as a string. */
    filename;
    /**
     * @type {?string}
     * @description The ID of an uploaded file to use as input. */
    file_id;
}
export class AiFileContent extends AiContent {
    /** @param {{file?:AiFile,type?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /**
     * @type {AiFile}
     * @description The file input for this content. */
    file;
}
export class AiAudioUrl {
    /** @param {{url?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description Either a URL of the audio or the base64 encoded audio data. */
    url;
}
export class AiAudioUrlContent extends AiContent {
    /** @param {{audio_url?:AiAudioUrl,type?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /**
     * @type {AiAudioUrl}
     * @description The audio for this content. */
    audio_url;
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
export class GetAccountResponse {
    /** @param {{userId?:string,username?:string,email?:string,displayName?:string,roles?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    userId;
    /** @type {string} */
    username;
    /** @type {string} */
    email;
    /** @type {string} */
    displayName;
    /** @type {string[]} */
    roles = [];
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
export class CheckUrlResponse {
    /** @param {{url?:string,result?:boolean,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    url;
    /** @type {boolean} */
    result;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class GetCoffeeShopMenuResponse {
    /** @param {{results?:MenuCategory[],optionQuantities?:string[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {MenuCategory[]} */
    results = [];
    /** @type {string[]} */
    optionQuantities = [];
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class PreviewCoffeeShopOrderResponse {
    /** @param {{customerName?:string,notes?:string,items?:PricedOrderItem[],subtotal?:number,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    customerName;
    /** @type {?string} */
    notes;
    /** @type {PricedOrderItem[]} */
    items = [];
    /** @type {number} */
    subtotal;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class CreateCoffeeShopOrderResponse {
    /** @param {{result?:CoffeeShopOrder,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {CoffeeShopOrder} */
    result;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class GetCoffeeShopOrderResponse {
    /** @param {{result?:CoffeeShopOrder,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {CoffeeShopOrder} */
    result;
    /** @type {?ResponseStatus} */
    responseStatus;
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
    /** @type {?ResponseStatus} */
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
export class OptionalTest {
    /** @param {{int?:number,nInt?:number,nRequiredInt?:number,string?:string,nString?:string,nRequiredString?:string,optionalClass?:OptionalClass,nOptionalClass?:OptionalClass,nRequiredOptionalClass?:OptionalClass,optionalEnum?:OptionalEnum,nOptionalEnum?:OptionalEnum,nRequiredOptionalEnum?:OptionalEnum}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    int;
    /** @type {?number} */
    nInt;
    /** @type {number} */
    nRequiredInt;
    /** @type {string} */
    string;
    /** @type {?string} */
    nString;
    /** @type {string} */
    nRequiredString;
    /** @type {OptionalClass} */
    optionalClass;
    /** @type {?OptionalClass} */
    nOptionalClass;
    /** @type {OptionalClass} */
    nRequiredOptionalClass;
    /** @type {OptionalEnum} */
    optionalEnum;
    /** @type {?OptionalEnum} */
    nOptionalEnum;
    /** @type {OptionalEnum} */
    nRequiredOptionalEnum;
    getTypeName() { return 'OptionalTest' }
    getMethod() { return 'POST' }
    createResponse() { return new OptionalTest() }
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
export class ChatResponse {
    /** @param {{id?:string,choices?:Choice[],created?:number,model?:string,system_fingerprint?:string,object?:string,service_tier?:string,usage?:AiUsage,provider?:string,cost?:number,tool_history?:ChoiceMessage[],metadata?:{ [index:string]: string; },responseStatus?:ResponseStatus}} [init] */
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
     * @type {?string}
     * @description This fingerprint represents the backend configuration that the model runs with. */
    system_fingerprint;
    /**
     * @type {string}
     * @description The object type, which is always chat.completion. */
    object;
    /**
     * @type {?string}
     * @description Specifies the processing type used for serving the request. */
    service_tier;
    /**
     * @type {AiUsage}
     * @description Usage statistics for the completion request. */
    usage;
    /**
     * @type {?string}
     * @description The provider used for the chat completion. */
    provider;
    /**
     * @type {?number}
     * @description Total cost of the completion in USD, accumulated across every request in the tool loop. */
    cost;
    /**
     * @type {?ChoiceMessage[]}
     * @description The assistant and tool messages exchanged during the tool-execution loop, in order. */
    tool_history;
    /**
     * @type {?{ [index:string]: string; }}
     * @description Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format. */
    metadata;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AuthenticateResponse {
    /** @param {{userId?:string,sessionId?:string,userName?:string,displayName?:string,referrerUrl?:string,bearerToken?:string,refreshToken?:string,refreshTokenExpiry?:string,profileUrl?:string,roles?:string[],permissions?:string[],authProvider?:string,responseStatus?:ResponseStatus,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    userId;
    /** @type {?string} */
    sessionId;
    /** @type {?string} */
    userName;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    referrerUrl;
    /** @type {?string} */
    bearerToken;
    /** @type {?string} */
    refreshToken;
    /** @type {?string} */
    refreshTokenExpiry;
    /** @type {?string} */
    profileUrl;
    /** @type {?string[]} */
    roles;
    /** @type {?string[]} */
    permissions;
    /** @type {?string} */
    authProvider;
    /** @type {?ResponseStatus} */
    responseStatus;
    /** @type {?{ [index:string]: string; }} */
    meta;
}
export class IdResponse {
    /** @param {{id?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    id;
    /** @type {?ResponseStatus} */
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
export class GetAccount {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetAccount' }
    getMethod() { return 'GET' }
    createResponse() { return new GetAccountResponse() }
}
export class GetKey {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetKey' }
    getMethod() { return 'GET' }
    createResponse() { return '' }
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
export class CheckUrl {
    /** @param {{url?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    url;
    getTypeName() { return 'CheckUrl' }
    getMethod() { return 'POST' }
    createResponse() { return new CheckUrlResponse() }
}
export class QueueCheckUrlApi {
    /** @param {{url?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    url;
    getTypeName() { return 'QueueCheckUrlApi' }
    getMethod() { return 'POST' }
    createResponse() { return new QueueCheckUrlsResponse() }
}
export class GetCoffeeShopMenu {
    constructor(init) { Object.assign(this, init) }
    getTypeName() { return 'GetCoffeeShopMenu' }
    getMethod() { return 'GET' }
    createResponse() { return new GetCoffeeShopMenuResponse() }
}
export class PreviewCoffeeShopOrder {
    /** @param {{customerName?:string,notes?:string,items?:OrderItemRequest[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description Name to put on the order */
    customerName;
    /**
     * @type {?string}
     * @description Optional instructions applying to the whole order */
    notes;
    /**
     * @type {OrderItemRequest[]}
     * @description One or more products from the current menu */
    items = [];
    getTypeName() { return 'PreviewCoffeeShopOrder' }
    getMethod() { return 'POST' }
    createResponse() { return new PreviewCoffeeShopOrderResponse() }
}
export class CreateCoffeeShopOrder {
    /** @param {{customerName?:string,notes?:string,items?:OrderItemRequest[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {string}
     * @description Name to put on the order */
    customerName;
    /**
     * @type {?string}
     * @description Optional instructions applying to the whole order */
    notes;
    /**
     * @type {OrderItemRequest[]}
     * @description Final order items. The approval form lets the user edit these before submission */
    items = [];
    getTypeName() { return 'CreateCoffeeShopOrder' }
    getMethod() { return 'POST' }
    createResponse() { return new CreateCoffeeShopOrderResponse() }
}
export class GetCoffeeShopOrder {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'GetCoffeeShopOrder' }
    getMethod() { return 'GET' }
    createResponse() { return new GetCoffeeShopOrderResponse() }
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
export class EchoData {
    /** @param {{data1?:Data1,data2?:Data2,data3?:Data3}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {Data1} */
    data1;
    /** @type {Data2} */
    data2;
    /** @type {Data3} */
    data3;
    getTypeName() { return 'EchoData' }
    getMethod() { return 'POST' }
    createResponse () { };
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
export class GetCertificateOfParticipationPdf {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    getTypeName() { return 'GetCertificateOfParticipationPdf' }
    getMethod() { return 'GET' }
    createResponse() { return new Blob() }
}
export class ChatCompletion {
    /** @param {{messages?:AiMessage[],model?:string,audio?:AiChatAudio,logit_bias?:{ [index:number]: number; },metadata?:{ [index:string]: string; },reasoning_effort?:string,response_format?:AiResponseFormat,service_tier?:string,safety_identifier?:string,stop?:string[],modalities?:string[],prompt_cache_key?:string,tools?:Tool[],verbosity?:string,temperature?:number,max_completion_tokens?:number,top_logprobs?:number,top_p?:number,frequency_penalty?:number,presence_penalty?:number,seed?:number,n?:number,store?:boolean,logprobs?:boolean,parallel_tool_calls?:boolean,enable_thinking?:boolean,stream?:boolean}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {AiMessage[]}
     * @description The messages to generate chat completions for. */
    messages = [];
    /**
     * @type {string}
     * @description ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API */
    model;
    /**
     * @type {?AiChatAudio}
     * @description Parameters for audio output. Required when audio output is requested with modalities: [audio] */
    audio;
    /**
     * @type {?{ [index:number]: number; }}
     * @description Modify the likelihood of specified tokens appearing in the completion. */
    logit_bias;
    /**
     * @type {?{ [index:string]: string; }}
     * @description Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format. */
    metadata;
    /**
     * @type {?string}
     * @description Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response. */
    reasoning_effort;
    /**
     * @type {?AiResponseFormat}
     * @description An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON. */
    response_format;
    /**
     * @type {?string}
     * @description Specifies the processing type used for serving the request. */
    service_tier;
    /**
     * @type {?string}
     * @description A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user. */
    safety_identifier;
    /**
     * @type {?string[]}
     * @description Up to 4 sequences where the API will stop generating further tokens. */
    stop;
    /**
     * @type {?string[]}
     * @description Output types that you would like the model to generate. Most models are capable of generating text, which is the default: */
    modalities;
    /**
     * @type {?string}
     * @description Used by OpenAI to cache responses for similar requests to optimize your cache hit rates. */
    prompt_cache_key;
    /**
     * @type {?Tool[]}
     * @description A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported. */
    tools;
    /**
     * @type {?string}
     * @description Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high. */
    verbosity;
    /**
     * @type {?number}
     * @description What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic. */
    temperature;
    /**
     * @type {?number}
     * @description An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens. */
    max_completion_tokens;
    /**
     * @type {?number}
     * @description An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used. */
    top_logprobs;
    /**
     * @type {?number}
     * @description An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. */
    top_p;
    /**
     * @type {?number}
     * @description Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim. */
    frequency_penalty;
    /**
     * @type {?number}
     * @description Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics. */
    presence_penalty;
    /**
     * @type {?number}
     * @description This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend. */
    seed;
    /**
     * @type {?number}
     * @description How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs. */
    n;
    /**
     * @type {?boolean}
     * @description Whether or not to store the output of this chat completion request for use in our model distillation or evals products. */
    store;
    /**
     * @type {?boolean}
     * @description Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message. */
    logprobs;
    /**
     * @type {?boolean}
     * @description Whether to enable parallel function calling during tool use. */
    parallel_tool_calls;
    /**
     * @type {?boolean}
     * @description Whether to enable thinking mode for some Qwen models and providers. */
    enable_thinking;
    /**
     * @type {?boolean}
     * @description If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message. */
    stream;
    getTypeName() { return 'ChatCompletion' }
    getMethod() { return 'POST' }
    createResponse() { return new ChatResponse() }
}
export class Authenticate {
    /** @param {{provider?:string,userName?:string,password?:string,rememberMe?:boolean,accessToken?:string,accessTokenSecret?:string,returnUrl?:string,errorView?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /**
     * @type {?string}
     * @description AuthProvider, e.g. credentials */
    provider;
    /** @type {?string} */
    userName;
    /** @type {?string} */
    password;
    /** @type {?boolean} */
    rememberMe;
    /** @type {?string} */
    accessToken;
    /** @type {?string} */
    accessTokenSecret;
    /** @type {?string} */
    returnUrl;
    /** @type {?string} */
    errorView;
    /** @type {?{ [index:string]: string; }} */
    meta;
    getTypeName() { return 'Authenticate' }
    getMethod() { return 'POST' }
    createResponse() { return new AuthenticateResponse() }
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
export class QueryAgentRuns extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryAgentRuns' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryAgentSteps extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryAgentSteps' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryAichatDocuments extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryAichatDocuments' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryAichatFilestores extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryAichatFilestores' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryAichatMedias extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryAichatMedias' }
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
    /** @type {?string} */
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
    /** @type {?string} */
    id;
    getTypeName() { return 'QueryAspNetUsers' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryCategories extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryCategories' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryCategoryOptions extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryCategoryOptions' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatAssistantConversations extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatAssistantConversations' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatAssistantMessages extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatAssistantMessages' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatAssistants extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatAssistants' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatDocuments extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatDocuments' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatFilestores extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatFilestores' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatMedias extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatMedias' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatMessages extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatMessages' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatRequests extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatRequests' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatSourceRuns extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatSourceRuns' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatSources extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatSources' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatThreads extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatThreads' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatToolApprovalBatches extends QueryDb {
    /** @param {{id?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?string} */
    id;
    getTypeName() { return 'QueryChatToolApprovalBatches' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryChatToolApprovals extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryChatToolApprovals' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryCoffeeShopOrderItems extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryCoffeeShopOrderItems' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryCoffeeShopOrders extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryCoffeeShopOrders' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryContextSnapshots extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryContextSnapshots' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryEFMigrationsHistories extends QueryDb {
    /** @param {{migrationId?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?string} */
    migrationId;
    getTypeName() { return 'QueryEFMigrationsHistories' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryEFMigrationsLocks extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryEFMigrationsLocks' }
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
export class QueryOptionQuantities extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryOptionQuantities' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryOptions extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryOptions' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class QueryProducts extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryProducts' }
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
    /** @type {?boolean} */
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
    /** @type {?boolean} */
    isFinished;
    getTypeName() { return 'UpdateTodo' }
    getMethod() { return 'PUT' }
    createResponse() { return new Todo() }
}
export class DeleteTodos {
    /** @param {{id?:number,ids?:number[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {?number[]} */
    ids;
    getTypeName() { return 'DeleteTodos' }
    getMethod() { return 'DELETE' }
    createResponse() { }
}
export class DeleteTodo {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
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
export class CreateAgentRun {
    /** @param {{threadId?:number,user?:string,status?:string,nextAction?:string,model?:string,stepCount?:number,sliceCount?:number,maxSteps?:number,contextTokens?:number,contextLimit?:number,leaseOwner?:string,leaseExpiresAt?:string,nextAttemptAt?:string,error?:string,createdAt?:string,updatedAt?:string,completedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    status;
    /** @type {?string} */
    nextAction;
    /** @type {?string} */
    model;
    /** @type {number} */
    stepCount;
    /** @type {number} */
    sliceCount;
    /** @type {number} */
    maxSteps;
    /** @type {?number} */
    contextTokens;
    /** @type {?number} */
    contextLimit;
    /** @type {?string} */
    leaseOwner;
    /** @type {?string} */
    leaseExpiresAt;
    /** @type {?string} */
    nextAttemptAt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    completedAt;
    getTypeName() { return 'CreateAgentRun' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAgentStep {
    /** @param {{runId?:number,sequence?:number,type?:string,status?:string,input?:string,output?:string,idempotencyKey?:string,attempt?:number,error?:string,startedAt?:string,completedAt?:string,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    runId;
    /** @type {number} */
    sequence;
    /** @type {?string} */
    type;
    /** @type {?string} */
    status;
    /** @type {?string} */
    input;
    /** @type {?string} */
    output;
    /** @type {?string} */
    idempotencyKey;
    /** @type {number} */
    attempt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    createdAt;
    getTypeName() { return 'CreateAgentStep' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAichatDocument {
    /** @param {{filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,filename?:string,url?:string,hash?:string,size?:number,displayName?:string,name?:string,customMetadata?:string,createTime?:string,updateTime?:string,sizeBytes?:number,mimeType?:string,state?:string,category?:string,tags?:string,startedAt?:string,uploadedAt?:string,metadata?:string,error?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    filename;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?number} */
    size;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    name;
    /** @type {?string} */
    customMetadata;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    mimeType;
    /** @type {?string} */
    state;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    uploadedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    getTypeName() { return 'CreateAichatDocument' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAichatFilestore {
    /** @param {{user?:string,createdAt?:string,updatedAt?:string,name?:string,displayName?:string,createTime?:string,updateTime?:string,activeDocumentsCount?:number,pendingDocumentsCount?:number,failedDocumentsCount?:number,sizeBytes?:number,metadata?:string,error?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    activeDocumentsCount;
    /** @type {?number} */
    pendingDocumentsCount;
    /** @type {?number} */
    failedDocumentsCount;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    getTypeName() { return 'CreateAichatFilestore' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAichatMedia {
    /** @param {{user?:string,name?:string,type?:string,prompt?:string,model?:string,created?:string,cost?:number,seed?:number,url?:string,hash?:string,aspectRatio?:string,width?:number,height?:number,size?:number,duration?:number,reactions?:string,caption?:string,description?:string,phash?:string,color?:string,category?:string,tags?:string,rating?:string,ratings?:string,objects?:string,variantId?:string,variantName?:string,publishedAt?:string,publishedUrl?:string,metadata?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    user;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {?string} */
    prompt;
    /** @type {?string} */
    model;
    /** @type {?string} */
    created;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    seed;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?string} */
    aspectRatio;
    /** @type {?number} */
    width;
    /** @type {?number} */
    height;
    /** @type {?number} */
    size;
    /** @type {?number} */
    duration;
    /** @type {?string} */
    reactions;
    /** @type {?string} */
    caption;
    /** @type {?string} */
    description;
    /** @type {?string} */
    phash;
    /** @type {?string} */
    color;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    rating;
    /** @type {?string} */
    ratings;
    /** @type {?string} */
    objects;
    /** @type {?string} */
    variantId;
    /** @type {?string} */
    variantName;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    /** @type {?string} */
    metadata;
    getTypeName() { return 'CreateAichatMedia' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAspNetRoleClaims {
    /** @param {{roleId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    roleId;
    /** @type {?string} */
    claimType;
    /** @type {?string} */
    claimValue;
    getTypeName() { return 'CreateAspNetRoleClaims' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAspNetRoles {
    /** @param {{id?:string,name?:string,normalizedName?:string,concurrencyStamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    /** @type {?string} */
    name;
    /** @type {?string} */
    normalizedName;
    /** @type {?string} */
    concurrencyStamp;
    getTypeName() { return 'CreateAspNetRoles' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAspNetUserClaims {
    /** @param {{userId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    userId;
    /** @type {?string} */
    claimType;
    /** @type {?string} */
    claimValue;
    getTypeName() { return 'CreateAspNetUserClaims' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateAspNetUsers {
    /** @param {{id?:string,firstName?:string,lastName?:string,displayName?:string,profileUrl?:string,refreshToken?:string,refreshTokenExpiry?:string,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:number,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:number,twoFactorEnabled?:number,lockoutEnd?:string,lockoutEnabled?:number,accessFailedCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
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
    /** @type {?string} */
    userName;
    /** @type {?string} */
    normalizedUserName;
    /** @type {?string} */
    email;
    /** @type {?string} */
    normalizedEmail;
    /** @type {number} */
    emailConfirmed;
    /** @type {?string} */
    passwordHash;
    /** @type {?string} */
    securityStamp;
    /** @type {?string} */
    concurrencyStamp;
    /** @type {?string} */
    phoneNumber;
    /** @type {number} */
    phoneNumberConfirmed;
    /** @type {number} */
    twoFactorEnabled;
    /** @type {?string} */
    lockoutEnd;
    /** @type {number} */
    lockoutEnabled;
    /** @type {number} */
    accessFailedCount;
    getTypeName() { return 'CreateAspNetUsers' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateCategory {
    /** @param {{name?:string,description?:string,temperatures?:string,defaultTemperature?:string,sizes?:string,defaultSize?:string,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    name;
    /** @type {?string} */
    description;
    /** @type {?string} */
    temperatures;
    /** @type {?string} */
    defaultTemperature;
    /** @type {?string} */
    sizes;
    /** @type {?string} */
    defaultSize;
    /** @type {?string} */
    imageUrl;
    getTypeName() { return 'CreateCategory' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateCategoryOption {
    /** @param {{categoryId?:number,optionId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    categoryId;
    /** @type {number} */
    optionId;
    getTypeName() { return 'CreateCategoryOption' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatAssistant {
    /** @param {{filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,publicId?:string,enabled?:number,publishedAt?:string,config?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    publicId;
    /** @type {number} */
    enabled;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    config;
    getTypeName() { return 'CreateChatAssistant' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatAssistantConversation {
    /** @param {{assistantId?:number,user?:string,createdAt?:string,updatedAt?:string,sessionId?:string,origin?:string,pageUrl?:string,userAgent?:string,title?:string,status?:string,messageCount?:number,lastMessage?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    assistantId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    sessionId;
    /** @type {?string} */
    origin;
    /** @type {?string} */
    pageUrl;
    /** @type {?string} */
    userAgent;
    /** @type {?string} */
    title;
    /** @type {?string} */
    status;
    /** @type {number} */
    messageCount;
    /** @type {?string} */
    lastMessage;
    getTypeName() { return 'CreateChatAssistantConversation' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatAssistantMessage {
    /** @param {{conversationId?:number,createdAt?:string,role?:string,content?:string,citations?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    conversationId;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    role;
    /** @type {?string} */
    content;
    /** @type {?string} */
    citations;
    /** @type {?string} */
    error;
    getTypeName() { return 'CreateChatAssistantMessage' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatDocument {
    /** @param {{filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,filename?:string,url?:string,hash?:string,size?:number,displayName?:string,name?:string,customMetadata?:string,createTime?:string,updateTime?:string,sizeBytes?:number,mimeType?:string,state?:string,category?:string,tags?:string,startedAt?:string,uploadedAt?:string,metadata?:string,error?:string,ref?:string,sourceUrl?:string,sourceId?:number,sourceScopeId?:number,sourceKey?:string,sourceEtag?:string,contentHash?:string,metadataHash?:string,extractorVer?:string,tombstonedAt?:string,categoryPath?:string,docType?:string,status?:string,locale?:string,product?:string,versions?:string,sourceUpdatedAt?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    filename;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?number} */
    size;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    name;
    /** @type {?string} */
    customMetadata;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    mimeType;
    /** @type {?string} */
    state;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    uploadedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    sourceUrl;
    /** @type {?number} */
    sourceId;
    /** @type {number} */
    sourceScopeId;
    /** @type {?string} */
    sourceKey;
    /** @type {?string} */
    sourceEtag;
    /** @type {?string} */
    contentHash;
    /** @type {?string} */
    metadataHash;
    /** @type {?string} */
    extractorVer;
    /** @type {?string} */
    tombstonedAt;
    /** @type {?string} */
    categoryPath;
    /** @type {?string} */
    docType;
    /** @type {?string} */
    status;
    /** @type {?string} */
    locale;
    /** @type {?string} */
    product;
    /** @type {?string} */
    versions;
    /** @type {?number} */
    sourceUpdatedAt;
    getTypeName() { return 'CreateChatDocument' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatFilestore {
    /** @param {{user?:string,createdAt?:string,updatedAt?:string,name?:string,displayName?:string,createTime?:string,updateTime?:string,activeDocumentsCount?:number,pendingDocumentsCount?:number,failedDocumentsCount?:number,sizeBytes?:number,metadata?:string,error?:string,ref?:string,visibility?:string,facets?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    activeDocumentsCount;
    /** @type {?number} */
    pendingDocumentsCount;
    /** @type {?number} */
    failedDocumentsCount;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    visibility;
    /** @type {?string} */
    facets;
    getTypeName() { return 'CreateChatFilestore' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatMedia {
    /** @param {{user?:string,name?:string,type?:string,prompt?:string,model?:string,created?:string,cost?:number,seed?:number,url?:string,hash?:string,aspectRatio?:string,width?:number,height?:number,size?:number,duration?:number,reactions?:string,caption?:string,description?:string,phash?:string,color?:string,category?:string,tags?:string,rating?:string,ratings?:string,objects?:string,variantId?:string,variantName?:string,publishedAt?:string,publishedUrl?:string,metadata?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    user;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {?string} */
    prompt;
    /** @type {?string} */
    model;
    /** @type {?string} */
    created;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    seed;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?string} */
    aspectRatio;
    /** @type {?number} */
    width;
    /** @type {?number} */
    height;
    /** @type {?number} */
    size;
    /** @type {?number} */
    duration;
    /** @type {?string} */
    reactions;
    /** @type {?string} */
    caption;
    /** @type {?string} */
    description;
    /** @type {?string} */
    phash;
    /** @type {?string} */
    color;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    rating;
    /** @type {?string} */
    ratings;
    /** @type {?string} */
    objects;
    /** @type {?string} */
    variantId;
    /** @type {?string} */
    variantName;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    /** @type {?string} */
    metadata;
    getTypeName() { return 'CreateChatMedia' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatMessage {
    /** @param {{threadId?:number,sequence?:number,runId?:number,stepId?:number,role?:string,message?:string,timestamp?:number,toolCallId?:string,toolName?:string,tokenCount?:number,active?:number,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    threadId;
    /** @type {number} */
    sequence;
    /** @type {?number} */
    runId;
    /** @type {?number} */
    stepId;
    /** @type {?string} */
    role;
    /** @type {?string} */
    message;
    /** @type {?number} */
    timestamp;
    /** @type {?string} */
    toolCallId;
    /** @type {?string} */
    toolName;
    /** @type {?number} */
    tokenCount;
    /** @type {number} */
    active;
    /** @type {?string} */
    createdAt;
    getTypeName() { return 'CreateChatMessage' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatRequest {
    /** @param {{user?:string,threadId?:number,createdAt?:string,updatedAt?:string,title?:string,model?:string,duration?:number,cost?:number,inputPrice?:number,inputTokens?:number,inputCachedTokens?:number,outputPrice?:number,outputTokens?:number,totalTokens?:number,usage?:string,provider?:string,providerModel?:string,providerRef?:string,finishReason?:string,startedAt?:string,completedAt?:string,error?:string,stackTrace?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    user;
    /** @type {?number} */
    threadId;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    title;
    /** @type {?string} */
    model;
    /** @type {?number} */
    duration;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    inputPrice;
    /** @type {?number} */
    inputTokens;
    /** @type {?number} */
    inputCachedTokens;
    /** @type {?number} */
    outputPrice;
    /** @type {?number} */
    outputTokens;
    /** @type {?number} */
    totalTokens;
    /** @type {?string} */
    usage;
    /** @type {?string} */
    provider;
    /** @type {?string} */
    providerModel;
    /** @type {?string} */
    providerRef;
    /** @type {?string} */
    finishReason;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    stackTrace;
    /** @type {?string} */
    ref;
    getTypeName() { return 'CreateChatRequest' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatSource {
    /** @param {{filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,type?:string,enabled?:number,config?:string,category?:string,rules?:string,include?:string,exclude?:string,extract?:string,chunking?:string,volatile?:string,extractorVer?:string,schedule?:string,onDelete?:string,cursor?:string,lastRunId?:number,lastRunAt?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {number} */
    enabled;
    /** @type {?string} */
    config;
    /** @type {?string} */
    category;
    /** @type {?string} */
    rules;
    /** @type {?string} */
    include;
    /** @type {?string} */
    exclude;
    /** @type {?string} */
    extract;
    /** @type {?string} */
    chunking;
    /** @type {?string} */
    volatile;
    /** @type {?string} */
    extractorVer;
    /** @type {?string} */
    schedule;
    /** @type {?string} */
    onDelete;
    /** @type {?string} */
    cursor;
    /** @type {?number} */
    lastRunId;
    /** @type {?string} */
    lastRunAt;
    /** @type {?string} */
    error;
    getTypeName() { return 'CreateChatSource' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatSourceRun {
    /** @param {{sourceId?:number,user?:string,startedAt?:string,completedAt?:string,status?:string,dryRun?:number,discovered?:number,added?:number,changed?:number,metadataOnly?:number,unchanged?:number,removed?:number,skipped?:number,failed?:number,bytes?:number,plan?:string,log?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    sourceId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    status;
    /** @type {number} */
    dryRun;
    /** @type {number} */
    discovered;
    /** @type {number} */
    added;
    /** @type {number} */
    changed;
    /** @type {number} */
    metadataOnly;
    /** @type {number} */
    unchanged;
    /** @type {number} */
    removed;
    /** @type {number} */
    skipped;
    /** @type {number} */
    failed;
    /** @type {number} */
    bytes;
    /** @type {?string} */
    plan;
    /** @type {?string} */
    log;
    /** @type {?string} */
    error;
    getTypeName() { return 'CreateChatSourceRun' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatThread {
    /** @param {{user?:string,createdAt?:string,updatedAt?:string,title?:string,systemPrompt?:string,model?:string,modelInfo?:string,modalities?:string,messages?:string,streamingMessage?:string,args?:string,tools?:string,toolHistory?:string,cost?:number,inputTokens?:number,outputTokens?:number,stats?:string,provider?:string,providerModel?:string,startedAt?:string,completedAt?:string,metadata?:string,status?:string,error?:string,ref?:string,providerResponse?:string,contextTokens?:number,parentId?:number,publishedAt?:string,publishedUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    title;
    /** @type {?string} */
    systemPrompt;
    /** @type {?string} */
    model;
    /** @type {?string} */
    modelInfo;
    /** @type {?string} */
    modalities;
    /** @type {?string} */
    messages;
    /** @type {?string} */
    streamingMessage;
    /** @type {?string} */
    args;
    /** @type {?string} */
    tools;
    /** @type {?string} */
    toolHistory;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    inputTokens;
    /** @type {?number} */
    outputTokens;
    /** @type {?string} */
    stats;
    /** @type {?string} */
    provider;
    /** @type {?string} */
    providerModel;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    status;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    providerResponse;
    /** @type {?number} */
    contextTokens;
    /** @type {?number} */
    parentId;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    getTypeName() { return 'CreateChatThread' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatToolApproval {
    /** @param {{batchId?:string,threadId?:number,user?:string,toolCallId?:string,toolName?:string,apiName?:string,requestType?:string,method?:string,route?:string,safety?:string,status?:string,sequence?:number,description?:string,schema?:string,proposedArgs?:string,effectiveArgs?:string,result?:string,toolResult?:string,error?:string,reason?:string,createdAt?:string,updatedAt?:string,resolvedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    batchId;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    toolCallId;
    /** @type {?string} */
    toolName;
    /** @type {?string} */
    apiName;
    /** @type {?string} */
    requestType;
    /** @type {?string} */
    method;
    /** @type {?string} */
    route;
    /** @type {?string} */
    safety;
    /** @type {?string} */
    status;
    /** @type {number} */
    sequence;
    /** @type {?string} */
    description;
    /** @type {?string} */
    schema;
    /** @type {?string} */
    proposedArgs;
    /** @type {?string} */
    effectiveArgs;
    /** @type {?string} */
    result;
    /** @type {?string} */
    toolResult;
    /** @type {?string} */
    error;
    /** @type {?string} */
    reason;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    resolvedAt;
    getTypeName() { return 'CreateChatToolApproval' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateChatToolApprovalBatch {
    /** @param {{id?:string,threadId?:number,user?:string,status?:string,createdAt?:string,updatedAt?:string,completedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    status;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    completedAt;
    getTypeName() { return 'CreateChatToolApprovalBatch' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateCoffeeShopOrderItem {
    /** @param {{coffeeShopOrderId?:number,productId?:number,productName?:string,quantity?:number,size?:string,temperature?:string,optionsJson?:string,unitPrice?:number,lineTotal?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    coffeeShopOrderId;
    /** @type {number} */
    productId;
    /** @type {?string} */
    productName;
    /** @type {number} */
    quantity;
    /** @type {?string} */
    size;
    /** @type {?string} */
    temperature;
    /** @type {?string} */
    optionsJson;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    lineTotal;
    getTypeName() { return 'CreateCoffeeShopOrderItem' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateContextSnapshot {
    /** @param {{threadId?:number,runId?:number,version?:number,fromSequence?:number,toSequence?:number,summary?:string,tokenCount?:number,model?:string,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    threadId;
    /** @type {?number} */
    runId;
    /** @type {number} */
    version;
    /** @type {number} */
    fromSequence;
    /** @type {number} */
    toSequence;
    /** @type {?string} */
    summary;
    /** @type {?number} */
    tokenCount;
    /** @type {?string} */
    model;
    /** @type {?string} */
    createdAt;
    getTypeName() { return 'CreateContextSnapshot' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateEFMigrationsHistory {
    /** @param {{migrationId?:string,productVersion?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    migrationId;
    /** @type {?string} */
    productVersion;
    getTypeName() { return 'CreateEFMigrationsHistory' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateEFMigrationsLock {
    /** @param {{id?:number,timestamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    timestamp;
    getTypeName() { return 'CreateEFMigrationsLock' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateFileSystemFile {
    /** @param {{fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    fileName;
    /** @type {?string} */
    filePath;
    /** @type {?string} */
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
    /** @type {?string} */
    name;
    /** @type {?string} */
    description;
    /** @type {?string} */
    createdDate;
    /** @type {?string} */
    completedDate;
    /** @type {?string} */
    connectionString;
    /** @type {?string} */
    namedConnection;
    /** @type {?string} */
    log;
    /** @type {?string} */
    errorCode;
    /** @type {?string} */
    errorMessage;
    /** @type {?string} */
    errorStackTrace;
    /** @type {?string} */
    meta;
    getTypeName() { return 'CreateMigration' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateOption {
    /** @param {{type?:string,names?:string,allowQuantity?:number,quantityLabel?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    type;
    /** @type {?string} */
    names;
    /** @type {?number} */
    allowQuantity;
    /** @type {?string} */
    quantityLabel;
    getTypeName() { return 'CreateOption' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateOptionQuantity {
    /** @param {{name?:string,value?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    name;
    /** @type {number} */
    value;
    getTypeName() { return 'CreateOptionQuantity' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateProduct {
    /** @param {{categoryId?:number,name?:string,cost?:number,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    categoryId;
    /** @type {?string} */
    name;
    /** @type {number} */
    cost;
    /** @type {?string} */
    imageUrl;
    getTypeName() { return 'CreateProduct' }
    getMethod() { return 'POST' }
    createResponse() { return new IdResponse() }
}
export class CreateValidationRule {
    /** @param {{type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    type;
    /** @type {?string} */
    field;
    /** @type {?string} */
    createdBy;
    /** @type {?string} */
    createdDate;
    /** @type {?string} */
    modifiedBy;
    /** @type {?string} */
    modifiedDate;
    /** @type {?string} */
    suspendedBy;
    /** @type {?string} */
    suspendedDate;
    /** @type {?string} */
    notes;
    /** @type {?string} */
    validator;
    /** @type {?string} */
    condition;
    /** @type {?string} */
    errorCode;
    /** @type {?string} */
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
export class DeleteAgentRun {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteAgentRun' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteAgentStep {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteAgentStep' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteAichatDocument {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteAichatDocument' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteAichatFilestore {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteAichatFilestore' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteAichatMedia {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteAichatMedia' }
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
    /** @type {?string} */
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
    /** @type {?string} */
    id;
    getTypeName() { return 'DeleteAspNetUsers' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteCategory {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteCategory' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteCategoryOption {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteCategoryOption' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatAssistant {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatAssistant' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatAssistantConversation {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatAssistantConversation' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatAssistantMessage {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatAssistantMessage' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatDocument {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatDocument' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatFilestore {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatFilestore' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatMedia {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatMedia' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatMessage {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatMessage' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatRequest {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatRequest' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatSource {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatSource' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatSourceRun {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatSourceRun' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatThread {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatThread' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatToolApproval {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteChatToolApproval' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteChatToolApprovalBatch {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    getTypeName() { return 'DeleteChatToolApprovalBatch' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteCoffeeShopOrder {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteCoffeeShopOrder' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteCoffeeShopOrderItem {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteCoffeeShopOrderItem' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteContextSnapshot {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteContextSnapshot' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteEFMigrationsHistory {
    /** @param {{migrationId?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    migrationId;
    getTypeName() { return 'DeleteEFMigrationsHistory' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteEFMigrationsLock {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteEFMigrationsLock' }
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
export class DeleteOption {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteOption' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteOptionQuantity {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteOptionQuantity' }
    getMethod() { return 'DELETE' }
    createResponse() { return new IdResponse() }
}
export class DeleteProduct {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteProduct' }
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
export class PatchAgentRun {
    /** @param {{id?:number,threadId?:number,user?:string,status?:string,nextAction?:string,model?:string,stepCount?:number,sliceCount?:number,maxSteps?:number,contextTokens?:number,contextLimit?:number,leaseOwner?:string,leaseExpiresAt?:string,nextAttemptAt?:string,error?:string,createdAt?:string,updatedAt?:string,completedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    status;
    /** @type {?string} */
    nextAction;
    /** @type {?string} */
    model;
    /** @type {number} */
    stepCount;
    /** @type {number} */
    sliceCount;
    /** @type {number} */
    maxSteps;
    /** @type {?number} */
    contextTokens;
    /** @type {?number} */
    contextLimit;
    /** @type {?string} */
    leaseOwner;
    /** @type {?string} */
    leaseExpiresAt;
    /** @type {?string} */
    nextAttemptAt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    completedAt;
    getTypeName() { return 'PatchAgentRun' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAgentStep {
    /** @param {{id?:number,runId?:number,sequence?:number,type?:string,status?:string,input?:string,output?:string,idempotencyKey?:string,attempt?:number,error?:string,startedAt?:string,completedAt?:string,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    runId;
    /** @type {number} */
    sequence;
    /** @type {?string} */
    type;
    /** @type {?string} */
    status;
    /** @type {?string} */
    input;
    /** @type {?string} */
    output;
    /** @type {?string} */
    idempotencyKey;
    /** @type {number} */
    attempt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    createdAt;
    getTypeName() { return 'PatchAgentStep' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAichatDocument {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,filename?:string,url?:string,hash?:string,size?:number,displayName?:string,name?:string,customMetadata?:string,createTime?:string,updateTime?:string,sizeBytes?:number,mimeType?:string,state?:string,category?:string,tags?:string,startedAt?:string,uploadedAt?:string,metadata?:string,error?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    filename;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?number} */
    size;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    name;
    /** @type {?string} */
    customMetadata;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    mimeType;
    /** @type {?string} */
    state;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    uploadedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    getTypeName() { return 'PatchAichatDocument' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAichatFilestore {
    /** @param {{id?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,displayName?:string,createTime?:string,updateTime?:string,activeDocumentsCount?:number,pendingDocumentsCount?:number,failedDocumentsCount?:number,sizeBytes?:number,metadata?:string,error?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    activeDocumentsCount;
    /** @type {?number} */
    pendingDocumentsCount;
    /** @type {?number} */
    failedDocumentsCount;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    getTypeName() { return 'PatchAichatFilestore' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAichatMedia {
    /** @param {{id?:number,user?:string,name?:string,type?:string,prompt?:string,model?:string,created?:string,cost?:number,seed?:number,url?:string,hash?:string,aspectRatio?:string,width?:number,height?:number,size?:number,duration?:number,reactions?:string,caption?:string,description?:string,phash?:string,color?:string,category?:string,tags?:string,rating?:string,ratings?:string,objects?:string,variantId?:string,variantName?:string,publishedAt?:string,publishedUrl?:string,metadata?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {?string} */
    prompt;
    /** @type {?string} */
    model;
    /** @type {?string} */
    created;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    seed;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?string} */
    aspectRatio;
    /** @type {?number} */
    width;
    /** @type {?number} */
    height;
    /** @type {?number} */
    size;
    /** @type {?number} */
    duration;
    /** @type {?string} */
    reactions;
    /** @type {?string} */
    caption;
    /** @type {?string} */
    description;
    /** @type {?string} */
    phash;
    /** @type {?string} */
    color;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    rating;
    /** @type {?string} */
    ratings;
    /** @type {?string} */
    objects;
    /** @type {?string} */
    variantId;
    /** @type {?string} */
    variantName;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    /** @type {?string} */
    metadata;
    getTypeName() { return 'PatchAichatMedia' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAspNetRoleClaims {
    /** @param {{id?:number,roleId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    roleId;
    /** @type {?string} */
    claimType;
    /** @type {?string} */
    claimValue;
    getTypeName() { return 'PatchAspNetRoleClaims' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAspNetRoles {
    /** @param {{id?:string,name?:string,normalizedName?:string,concurrencyStamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    /** @type {?string} */
    name;
    /** @type {?string} */
    normalizedName;
    /** @type {?string} */
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
    /** @type {?string} */
    userId;
    /** @type {?string} */
    claimType;
    /** @type {?string} */
    claimValue;
    getTypeName() { return 'PatchAspNetUserClaims' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchAspNetUsers {
    /** @param {{id?:string,firstName?:string,lastName?:string,displayName?:string,profileUrl?:string,refreshToken?:string,refreshTokenExpiry?:string,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:number,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:number,twoFactorEnabled?:number,lockoutEnd?:string,lockoutEnabled?:number,accessFailedCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
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
    /** @type {?string} */
    userName;
    /** @type {?string} */
    normalizedUserName;
    /** @type {?string} */
    email;
    /** @type {?string} */
    normalizedEmail;
    /** @type {number} */
    emailConfirmed;
    /** @type {?string} */
    passwordHash;
    /** @type {?string} */
    securityStamp;
    /** @type {?string} */
    concurrencyStamp;
    /** @type {?string} */
    phoneNumber;
    /** @type {number} */
    phoneNumberConfirmed;
    /** @type {number} */
    twoFactorEnabled;
    /** @type {?string} */
    lockoutEnd;
    /** @type {number} */
    lockoutEnabled;
    /** @type {number} */
    accessFailedCount;
    getTypeName() { return 'PatchAspNetUsers' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchCategory {
    /** @param {{id?:number,name?:string,description?:string,temperatures?:string,defaultTemperature?:string,sizes?:string,defaultSize?:string,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    name;
    /** @type {?string} */
    description;
    /** @type {?string} */
    temperatures;
    /** @type {?string} */
    defaultTemperature;
    /** @type {?string} */
    sizes;
    /** @type {?string} */
    defaultSize;
    /** @type {?string} */
    imageUrl;
    getTypeName() { return 'PatchCategory' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchCategoryOption {
    /** @param {{id?:number,categoryId?:number,optionId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    categoryId;
    /** @type {number} */
    optionId;
    getTypeName() { return 'PatchCategoryOption' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatAssistant {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,publicId?:string,enabled?:number,publishedAt?:string,config?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    publicId;
    /** @type {number} */
    enabled;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    config;
    getTypeName() { return 'PatchChatAssistant' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatAssistantConversation {
    /** @param {{id?:number,assistantId?:number,user?:string,createdAt?:string,updatedAt?:string,sessionId?:string,origin?:string,pageUrl?:string,userAgent?:string,title?:string,status?:string,messageCount?:number,lastMessage?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    assistantId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    sessionId;
    /** @type {?string} */
    origin;
    /** @type {?string} */
    pageUrl;
    /** @type {?string} */
    userAgent;
    /** @type {?string} */
    title;
    /** @type {?string} */
    status;
    /** @type {number} */
    messageCount;
    /** @type {?string} */
    lastMessage;
    getTypeName() { return 'PatchChatAssistantConversation' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatAssistantMessage {
    /** @param {{id?:number,conversationId?:number,createdAt?:string,role?:string,content?:string,citations?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    conversationId;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    role;
    /** @type {?string} */
    content;
    /** @type {?string} */
    citations;
    /** @type {?string} */
    error;
    getTypeName() { return 'PatchChatAssistantMessage' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatDocument {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,filename?:string,url?:string,hash?:string,size?:number,displayName?:string,name?:string,customMetadata?:string,createTime?:string,updateTime?:string,sizeBytes?:number,mimeType?:string,state?:string,category?:string,tags?:string,startedAt?:string,uploadedAt?:string,metadata?:string,error?:string,ref?:string,sourceUrl?:string,sourceId?:number,sourceScopeId?:number,sourceKey?:string,sourceEtag?:string,contentHash?:string,metadataHash?:string,extractorVer?:string,tombstonedAt?:string,categoryPath?:string,docType?:string,status?:string,locale?:string,product?:string,versions?:string,sourceUpdatedAt?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    filename;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?number} */
    size;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    name;
    /** @type {?string} */
    customMetadata;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    mimeType;
    /** @type {?string} */
    state;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    uploadedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    sourceUrl;
    /** @type {?number} */
    sourceId;
    /** @type {number} */
    sourceScopeId;
    /** @type {?string} */
    sourceKey;
    /** @type {?string} */
    sourceEtag;
    /** @type {?string} */
    contentHash;
    /** @type {?string} */
    metadataHash;
    /** @type {?string} */
    extractorVer;
    /** @type {?string} */
    tombstonedAt;
    /** @type {?string} */
    categoryPath;
    /** @type {?string} */
    docType;
    /** @type {?string} */
    status;
    /** @type {?string} */
    locale;
    /** @type {?string} */
    product;
    /** @type {?string} */
    versions;
    /** @type {?number} */
    sourceUpdatedAt;
    getTypeName() { return 'PatchChatDocument' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatFilestore {
    /** @param {{id?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,displayName?:string,createTime?:string,updateTime?:string,activeDocumentsCount?:number,pendingDocumentsCount?:number,failedDocumentsCount?:number,sizeBytes?:number,metadata?:string,error?:string,ref?:string,visibility?:string,facets?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    activeDocumentsCount;
    /** @type {?number} */
    pendingDocumentsCount;
    /** @type {?number} */
    failedDocumentsCount;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    visibility;
    /** @type {?string} */
    facets;
    getTypeName() { return 'PatchChatFilestore' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatMedia {
    /** @param {{id?:number,user?:string,name?:string,type?:string,prompt?:string,model?:string,created?:string,cost?:number,seed?:number,url?:string,hash?:string,aspectRatio?:string,width?:number,height?:number,size?:number,duration?:number,reactions?:string,caption?:string,description?:string,phash?:string,color?:string,category?:string,tags?:string,rating?:string,ratings?:string,objects?:string,variantId?:string,variantName?:string,publishedAt?:string,publishedUrl?:string,metadata?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {?string} */
    prompt;
    /** @type {?string} */
    model;
    /** @type {?string} */
    created;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    seed;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?string} */
    aspectRatio;
    /** @type {?number} */
    width;
    /** @type {?number} */
    height;
    /** @type {?number} */
    size;
    /** @type {?number} */
    duration;
    /** @type {?string} */
    reactions;
    /** @type {?string} */
    caption;
    /** @type {?string} */
    description;
    /** @type {?string} */
    phash;
    /** @type {?string} */
    color;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    rating;
    /** @type {?string} */
    ratings;
    /** @type {?string} */
    objects;
    /** @type {?string} */
    variantId;
    /** @type {?string} */
    variantName;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    /** @type {?string} */
    metadata;
    getTypeName() { return 'PatchChatMedia' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatMessage {
    /** @param {{id?:number,threadId?:number,sequence?:number,runId?:number,stepId?:number,role?:string,message?:string,timestamp?:number,toolCallId?:string,toolName?:string,tokenCount?:number,active?:number,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    threadId;
    /** @type {number} */
    sequence;
    /** @type {?number} */
    runId;
    /** @type {?number} */
    stepId;
    /** @type {?string} */
    role;
    /** @type {?string} */
    message;
    /** @type {?number} */
    timestamp;
    /** @type {?string} */
    toolCallId;
    /** @type {?string} */
    toolName;
    /** @type {?number} */
    tokenCount;
    /** @type {number} */
    active;
    /** @type {?string} */
    createdAt;
    getTypeName() { return 'PatchChatMessage' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatRequest {
    /** @param {{id?:number,user?:string,threadId?:number,createdAt?:string,updatedAt?:string,title?:string,model?:string,duration?:number,cost?:number,inputPrice?:number,inputTokens?:number,inputCachedTokens?:number,outputPrice?:number,outputTokens?:number,totalTokens?:number,usage?:string,provider?:string,providerModel?:string,providerRef?:string,finishReason?:string,startedAt?:string,completedAt?:string,error?:string,stackTrace?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?number} */
    threadId;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    title;
    /** @type {?string} */
    model;
    /** @type {?number} */
    duration;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    inputPrice;
    /** @type {?number} */
    inputTokens;
    /** @type {?number} */
    inputCachedTokens;
    /** @type {?number} */
    outputPrice;
    /** @type {?number} */
    outputTokens;
    /** @type {?number} */
    totalTokens;
    /** @type {?string} */
    usage;
    /** @type {?string} */
    provider;
    /** @type {?string} */
    providerModel;
    /** @type {?string} */
    providerRef;
    /** @type {?string} */
    finishReason;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    stackTrace;
    /** @type {?string} */
    ref;
    getTypeName() { return 'PatchChatRequest' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatSource {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,type?:string,enabled?:number,config?:string,category?:string,rules?:string,include?:string,exclude?:string,extract?:string,chunking?:string,volatile?:string,extractorVer?:string,schedule?:string,onDelete?:string,cursor?:string,lastRunId?:number,lastRunAt?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {number} */
    enabled;
    /** @type {?string} */
    config;
    /** @type {?string} */
    category;
    /** @type {?string} */
    rules;
    /** @type {?string} */
    include;
    /** @type {?string} */
    exclude;
    /** @type {?string} */
    extract;
    /** @type {?string} */
    chunking;
    /** @type {?string} */
    volatile;
    /** @type {?string} */
    extractorVer;
    /** @type {?string} */
    schedule;
    /** @type {?string} */
    onDelete;
    /** @type {?string} */
    cursor;
    /** @type {?number} */
    lastRunId;
    /** @type {?string} */
    lastRunAt;
    /** @type {?string} */
    error;
    getTypeName() { return 'PatchChatSource' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatSourceRun {
    /** @param {{id?:number,sourceId?:number,user?:string,startedAt?:string,completedAt?:string,status?:string,dryRun?:number,discovered?:number,added?:number,changed?:number,metadataOnly?:number,unchanged?:number,removed?:number,skipped?:number,failed?:number,bytes?:number,plan?:string,log?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    sourceId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    status;
    /** @type {number} */
    dryRun;
    /** @type {number} */
    discovered;
    /** @type {number} */
    added;
    /** @type {number} */
    changed;
    /** @type {number} */
    metadataOnly;
    /** @type {number} */
    unchanged;
    /** @type {number} */
    removed;
    /** @type {number} */
    skipped;
    /** @type {number} */
    failed;
    /** @type {number} */
    bytes;
    /** @type {?string} */
    plan;
    /** @type {?string} */
    log;
    /** @type {?string} */
    error;
    getTypeName() { return 'PatchChatSourceRun' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatThread {
    /** @param {{id?:number,user?:string,createdAt?:string,updatedAt?:string,title?:string,systemPrompt?:string,model?:string,modelInfo?:string,modalities?:string,messages?:string,streamingMessage?:string,args?:string,tools?:string,toolHistory?:string,cost?:number,inputTokens?:number,outputTokens?:number,stats?:string,provider?:string,providerModel?:string,startedAt?:string,completedAt?:string,metadata?:string,status?:string,error?:string,ref?:string,providerResponse?:string,contextTokens?:number,parentId?:number,publishedAt?:string,publishedUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    title;
    /** @type {?string} */
    systemPrompt;
    /** @type {?string} */
    model;
    /** @type {?string} */
    modelInfo;
    /** @type {?string} */
    modalities;
    /** @type {?string} */
    messages;
    /** @type {?string} */
    streamingMessage;
    /** @type {?string} */
    args;
    /** @type {?string} */
    tools;
    /** @type {?string} */
    toolHistory;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    inputTokens;
    /** @type {?number} */
    outputTokens;
    /** @type {?string} */
    stats;
    /** @type {?string} */
    provider;
    /** @type {?string} */
    providerModel;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    status;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    providerResponse;
    /** @type {?number} */
    contextTokens;
    /** @type {?number} */
    parentId;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    getTypeName() { return 'PatchChatThread' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatToolApproval {
    /** @param {{id?:number,batchId?:string,threadId?:number,user?:string,toolCallId?:string,toolName?:string,apiName?:string,requestType?:string,method?:string,route?:string,safety?:string,status?:string,sequence?:number,description?:string,schema?:string,proposedArgs?:string,effectiveArgs?:string,result?:string,toolResult?:string,error?:string,reason?:string,createdAt?:string,updatedAt?:string,resolvedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    batchId;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    toolCallId;
    /** @type {?string} */
    toolName;
    /** @type {?string} */
    apiName;
    /** @type {?string} */
    requestType;
    /** @type {?string} */
    method;
    /** @type {?string} */
    route;
    /** @type {?string} */
    safety;
    /** @type {?string} */
    status;
    /** @type {number} */
    sequence;
    /** @type {?string} */
    description;
    /** @type {?string} */
    schema;
    /** @type {?string} */
    proposedArgs;
    /** @type {?string} */
    effectiveArgs;
    /** @type {?string} */
    result;
    /** @type {?string} */
    toolResult;
    /** @type {?string} */
    error;
    /** @type {?string} */
    reason;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    resolvedAt;
    getTypeName() { return 'PatchChatToolApproval' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchChatToolApprovalBatch {
    /** @param {{id?:string,threadId?:number,user?:string,status?:string,createdAt?:string,updatedAt?:string,completedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    status;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    completedAt;
    getTypeName() { return 'PatchChatToolApprovalBatch' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchCoffeeShopOrder {
    /** @param {{id?:number,orderNumber?:string,customerName?:string,customerUserId?:string,status?:string,notes?:string,subtotal?:number,createdDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    orderNumber;
    /** @type {?string} */
    customerName;
    /** @type {?string} */
    customerUserId;
    /** @type {?string} */
    status;
    /** @type {?string} */
    notes;
    /** @type {number} */
    subtotal;
    /** @type {?string} */
    createdDate;
    getTypeName() { return 'PatchCoffeeShopOrder' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchCoffeeShopOrderItem {
    /** @param {{id?:number,coffeeShopOrderId?:number,productId?:number,productName?:string,quantity?:number,size?:string,temperature?:string,optionsJson?:string,unitPrice?:number,lineTotal?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    coffeeShopOrderId;
    /** @type {number} */
    productId;
    /** @type {?string} */
    productName;
    /** @type {number} */
    quantity;
    /** @type {?string} */
    size;
    /** @type {?string} */
    temperature;
    /** @type {?string} */
    optionsJson;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    lineTotal;
    getTypeName() { return 'PatchCoffeeShopOrderItem' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchContextSnapshot {
    /** @param {{id?:number,threadId?:number,runId?:number,version?:number,fromSequence?:number,toSequence?:number,summary?:string,tokenCount?:number,model?:string,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    threadId;
    /** @type {?number} */
    runId;
    /** @type {number} */
    version;
    /** @type {number} */
    fromSequence;
    /** @type {number} */
    toSequence;
    /** @type {?string} */
    summary;
    /** @type {?number} */
    tokenCount;
    /** @type {?string} */
    model;
    /** @type {?string} */
    createdAt;
    getTypeName() { return 'PatchContextSnapshot' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchEFMigrationsHistory {
    /** @param {{migrationId?:string,productVersion?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    migrationId;
    /** @type {?string} */
    productVersion;
    getTypeName() { return 'PatchEFMigrationsHistory' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchEFMigrationsLock {
    /** @param {{id?:number,timestamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    timestamp;
    getTypeName() { return 'PatchEFMigrationsLock' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchFileSystemFile {
    /** @param {{id?:number,fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    fileName;
    /** @type {?string} */
    filePath;
    /** @type {?string} */
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
    /** @type {?string} */
    fileAccessType;
    /** @type {?string} */
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
    /** @type {?string} */
    name;
    /** @type {?string} */
    description;
    /** @type {?string} */
    createdDate;
    /** @type {?string} */
    completedDate;
    /** @type {?string} */
    connectionString;
    /** @type {?string} */
    namedConnection;
    /** @type {?string} */
    log;
    /** @type {?string} */
    errorCode;
    /** @type {?string} */
    errorMessage;
    /** @type {?string} */
    errorStackTrace;
    /** @type {?string} */
    meta;
    getTypeName() { return 'PatchMigration' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchOption {
    /** @param {{id?:number,type?:string,names?:string,allowQuantity?:number,quantityLabel?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    type;
    /** @type {?string} */
    names;
    /** @type {?number} */
    allowQuantity;
    /** @type {?string} */
    quantityLabel;
    getTypeName() { return 'PatchOption' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchOptionQuantity {
    /** @param {{id?:number,name?:string,value?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    name;
    /** @type {number} */
    value;
    getTypeName() { return 'PatchOptionQuantity' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchProduct {
    /** @param {{id?:number,categoryId?:number,name?:string,cost?:number,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    categoryId;
    /** @type {?string} */
    name;
    /** @type {number} */
    cost;
    /** @type {?string} */
    imageUrl;
    getTypeName() { return 'PatchProduct' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class PatchValidationRule {
    /** @param {{id?:number,type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    type;
    /** @type {?string} */
    field;
    /** @type {?string} */
    createdBy;
    /** @type {?string} */
    createdDate;
    /** @type {?string} */
    modifiedBy;
    /** @type {?string} */
    modifiedDate;
    /** @type {?string} */
    suspendedBy;
    /** @type {?string} */
    suspendedDate;
    /** @type {?string} */
    notes;
    /** @type {?string} */
    validator;
    /** @type {?string} */
    condition;
    /** @type {?string} */
    errorCode;
    /** @type {?string} */
    message;
    getTypeName() { return 'PatchValidationRule' }
    getMethod() { return 'PATCH' }
    createResponse() { return new IdResponse() }
}
export class UpdateAgentRun {
    /** @param {{id?:number,threadId?:number,user?:string,status?:string,nextAction?:string,model?:string,stepCount?:number,sliceCount?:number,maxSteps?:number,contextTokens?:number,contextLimit?:number,leaseOwner?:string,leaseExpiresAt?:string,nextAttemptAt?:string,error?:string,createdAt?:string,updatedAt?:string,completedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    status;
    /** @type {?string} */
    nextAction;
    /** @type {?string} */
    model;
    /** @type {number} */
    stepCount;
    /** @type {number} */
    sliceCount;
    /** @type {number} */
    maxSteps;
    /** @type {?number} */
    contextTokens;
    /** @type {?number} */
    contextLimit;
    /** @type {?string} */
    leaseOwner;
    /** @type {?string} */
    leaseExpiresAt;
    /** @type {?string} */
    nextAttemptAt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    completedAt;
    getTypeName() { return 'UpdateAgentRun' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAgentStep {
    /** @param {{id?:number,runId?:number,sequence?:number,type?:string,status?:string,input?:string,output?:string,idempotencyKey?:string,attempt?:number,error?:string,startedAt?:string,completedAt?:string,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    runId;
    /** @type {number} */
    sequence;
    /** @type {?string} */
    type;
    /** @type {?string} */
    status;
    /** @type {?string} */
    input;
    /** @type {?string} */
    output;
    /** @type {?string} */
    idempotencyKey;
    /** @type {number} */
    attempt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    createdAt;
    getTypeName() { return 'UpdateAgentStep' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAichatDocument {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,filename?:string,url?:string,hash?:string,size?:number,displayName?:string,name?:string,customMetadata?:string,createTime?:string,updateTime?:string,sizeBytes?:number,mimeType?:string,state?:string,category?:string,tags?:string,startedAt?:string,uploadedAt?:string,metadata?:string,error?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    filename;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?number} */
    size;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    name;
    /** @type {?string} */
    customMetadata;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    mimeType;
    /** @type {?string} */
    state;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    uploadedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    getTypeName() { return 'UpdateAichatDocument' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAichatFilestore {
    /** @param {{id?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,displayName?:string,createTime?:string,updateTime?:string,activeDocumentsCount?:number,pendingDocumentsCount?:number,failedDocumentsCount?:number,sizeBytes?:number,metadata?:string,error?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    activeDocumentsCount;
    /** @type {?number} */
    pendingDocumentsCount;
    /** @type {?number} */
    failedDocumentsCount;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    getTypeName() { return 'UpdateAichatFilestore' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAichatMedia {
    /** @param {{id?:number,user?:string,name?:string,type?:string,prompt?:string,model?:string,created?:string,cost?:number,seed?:number,url?:string,hash?:string,aspectRatio?:string,width?:number,height?:number,size?:number,duration?:number,reactions?:string,caption?:string,description?:string,phash?:string,color?:string,category?:string,tags?:string,rating?:string,ratings?:string,objects?:string,variantId?:string,variantName?:string,publishedAt?:string,publishedUrl?:string,metadata?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {?string} */
    prompt;
    /** @type {?string} */
    model;
    /** @type {?string} */
    created;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    seed;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?string} */
    aspectRatio;
    /** @type {?number} */
    width;
    /** @type {?number} */
    height;
    /** @type {?number} */
    size;
    /** @type {?number} */
    duration;
    /** @type {?string} */
    reactions;
    /** @type {?string} */
    caption;
    /** @type {?string} */
    description;
    /** @type {?string} */
    phash;
    /** @type {?string} */
    color;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    rating;
    /** @type {?string} */
    ratings;
    /** @type {?string} */
    objects;
    /** @type {?string} */
    variantId;
    /** @type {?string} */
    variantName;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    /** @type {?string} */
    metadata;
    getTypeName() { return 'UpdateAichatMedia' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAspNetRoleClaims {
    /** @param {{id?:number,roleId?:string,claimType?:string,claimValue?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    roleId;
    /** @type {?string} */
    claimType;
    /** @type {?string} */
    claimValue;
    getTypeName() { return 'UpdateAspNetRoleClaims' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAspNetRoles {
    /** @param {{id?:string,name?:string,normalizedName?:string,concurrencyStamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    /** @type {?string} */
    name;
    /** @type {?string} */
    normalizedName;
    /** @type {?string} */
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
    /** @type {?string} */
    userId;
    /** @type {?string} */
    claimType;
    /** @type {?string} */
    claimValue;
    getTypeName() { return 'UpdateAspNetUserClaims' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateAspNetUsers {
    /** @param {{id?:string,firstName?:string,lastName?:string,displayName?:string,profileUrl?:string,refreshToken?:string,refreshTokenExpiry?:string,userName?:string,normalizedUserName?:string,email?:string,normalizedEmail?:string,emailConfirmed?:number,passwordHash?:string,securityStamp?:string,concurrencyStamp?:string,phoneNumber?:string,phoneNumberConfirmed?:number,twoFactorEnabled?:number,lockoutEnd?:string,lockoutEnabled?:number,accessFailedCount?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
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
    /** @type {?string} */
    userName;
    /** @type {?string} */
    normalizedUserName;
    /** @type {?string} */
    email;
    /** @type {?string} */
    normalizedEmail;
    /** @type {number} */
    emailConfirmed;
    /** @type {?string} */
    passwordHash;
    /** @type {?string} */
    securityStamp;
    /** @type {?string} */
    concurrencyStamp;
    /** @type {?string} */
    phoneNumber;
    /** @type {number} */
    phoneNumberConfirmed;
    /** @type {number} */
    twoFactorEnabled;
    /** @type {?string} */
    lockoutEnd;
    /** @type {number} */
    lockoutEnabled;
    /** @type {number} */
    accessFailedCount;
    getTypeName() { return 'UpdateAspNetUsers' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateCategory {
    /** @param {{id?:number,name?:string,description?:string,temperatures?:string,defaultTemperature?:string,sizes?:string,defaultSize?:string,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    name;
    /** @type {?string} */
    description;
    /** @type {?string} */
    temperatures;
    /** @type {?string} */
    defaultTemperature;
    /** @type {?string} */
    sizes;
    /** @type {?string} */
    defaultSize;
    /** @type {?string} */
    imageUrl;
    getTypeName() { return 'UpdateCategory' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateCategoryOption {
    /** @param {{id?:number,categoryId?:number,optionId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    categoryId;
    /** @type {number} */
    optionId;
    getTypeName() { return 'UpdateCategoryOption' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatAssistant {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,publicId?:string,enabled?:number,publishedAt?:string,config?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    publicId;
    /** @type {number} */
    enabled;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    config;
    getTypeName() { return 'UpdateChatAssistant' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatAssistantConversation {
    /** @param {{id?:number,assistantId?:number,user?:string,createdAt?:string,updatedAt?:string,sessionId?:string,origin?:string,pageUrl?:string,userAgent?:string,title?:string,status?:string,messageCount?:number,lastMessage?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    assistantId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    sessionId;
    /** @type {?string} */
    origin;
    /** @type {?string} */
    pageUrl;
    /** @type {?string} */
    userAgent;
    /** @type {?string} */
    title;
    /** @type {?string} */
    status;
    /** @type {number} */
    messageCount;
    /** @type {?string} */
    lastMessage;
    getTypeName() { return 'UpdateChatAssistantConversation' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatAssistantMessage {
    /** @param {{id?:number,conversationId?:number,createdAt?:string,role?:string,content?:string,citations?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    conversationId;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    role;
    /** @type {?string} */
    content;
    /** @type {?string} */
    citations;
    /** @type {?string} */
    error;
    getTypeName() { return 'UpdateChatAssistantMessage' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatDocument {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,filename?:string,url?:string,hash?:string,size?:number,displayName?:string,name?:string,customMetadata?:string,createTime?:string,updateTime?:string,sizeBytes?:number,mimeType?:string,state?:string,category?:string,tags?:string,startedAt?:string,uploadedAt?:string,metadata?:string,error?:string,ref?:string,sourceUrl?:string,sourceId?:number,sourceScopeId?:number,sourceKey?:string,sourceEtag?:string,contentHash?:string,metadataHash?:string,extractorVer?:string,tombstonedAt?:string,categoryPath?:string,docType?:string,status?:string,locale?:string,product?:string,versions?:string,sourceUpdatedAt?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    filename;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?number} */
    size;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    name;
    /** @type {?string} */
    customMetadata;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    mimeType;
    /** @type {?string} */
    state;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    uploadedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    sourceUrl;
    /** @type {?number} */
    sourceId;
    /** @type {number} */
    sourceScopeId;
    /** @type {?string} */
    sourceKey;
    /** @type {?string} */
    sourceEtag;
    /** @type {?string} */
    contentHash;
    /** @type {?string} */
    metadataHash;
    /** @type {?string} */
    extractorVer;
    /** @type {?string} */
    tombstonedAt;
    /** @type {?string} */
    categoryPath;
    /** @type {?string} */
    docType;
    /** @type {?string} */
    status;
    /** @type {?string} */
    locale;
    /** @type {?string} */
    product;
    /** @type {?string} */
    versions;
    /** @type {?number} */
    sourceUpdatedAt;
    getTypeName() { return 'UpdateChatDocument' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatFilestore {
    /** @param {{id?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,displayName?:string,createTime?:string,updateTime?:string,activeDocumentsCount?:number,pendingDocumentsCount?:number,failedDocumentsCount?:number,sizeBytes?:number,metadata?:string,error?:string,ref?:string,visibility?:string,facets?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    displayName;
    /** @type {?string} */
    createTime;
    /** @type {?string} */
    updateTime;
    /** @type {?number} */
    activeDocumentsCount;
    /** @type {?number} */
    pendingDocumentsCount;
    /** @type {?number} */
    failedDocumentsCount;
    /** @type {?number} */
    sizeBytes;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    visibility;
    /** @type {?string} */
    facets;
    getTypeName() { return 'UpdateChatFilestore' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatMedia {
    /** @param {{id?:number,user?:string,name?:string,type?:string,prompt?:string,model?:string,created?:string,cost?:number,seed?:number,url?:string,hash?:string,aspectRatio?:string,width?:number,height?:number,size?:number,duration?:number,reactions?:string,caption?:string,description?:string,phash?:string,color?:string,category?:string,tags?:string,rating?:string,ratings?:string,objects?:string,variantId?:string,variantName?:string,publishedAt?:string,publishedUrl?:string,metadata?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {?string} */
    prompt;
    /** @type {?string} */
    model;
    /** @type {?string} */
    created;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    seed;
    /** @type {?string} */
    url;
    /** @type {?string} */
    hash;
    /** @type {?string} */
    aspectRatio;
    /** @type {?number} */
    width;
    /** @type {?number} */
    height;
    /** @type {?number} */
    size;
    /** @type {?number} */
    duration;
    /** @type {?string} */
    reactions;
    /** @type {?string} */
    caption;
    /** @type {?string} */
    description;
    /** @type {?string} */
    phash;
    /** @type {?string} */
    color;
    /** @type {?string} */
    category;
    /** @type {?string} */
    tags;
    /** @type {?string} */
    rating;
    /** @type {?string} */
    ratings;
    /** @type {?string} */
    objects;
    /** @type {?string} */
    variantId;
    /** @type {?string} */
    variantName;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    /** @type {?string} */
    metadata;
    getTypeName() { return 'UpdateChatMedia' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatMessage {
    /** @param {{id?:number,threadId?:number,sequence?:number,runId?:number,stepId?:number,role?:string,message?:string,timestamp?:number,toolCallId?:string,toolName?:string,tokenCount?:number,active?:number,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    threadId;
    /** @type {number} */
    sequence;
    /** @type {?number} */
    runId;
    /** @type {?number} */
    stepId;
    /** @type {?string} */
    role;
    /** @type {?string} */
    message;
    /** @type {?number} */
    timestamp;
    /** @type {?string} */
    toolCallId;
    /** @type {?string} */
    toolName;
    /** @type {?number} */
    tokenCount;
    /** @type {number} */
    active;
    /** @type {?string} */
    createdAt;
    getTypeName() { return 'UpdateChatMessage' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatRequest {
    /** @param {{id?:number,user?:string,threadId?:number,createdAt?:string,updatedAt?:string,title?:string,model?:string,duration?:number,cost?:number,inputPrice?:number,inputTokens?:number,inputCachedTokens?:number,outputPrice?:number,outputTokens?:number,totalTokens?:number,usage?:string,provider?:string,providerModel?:string,providerRef?:string,finishReason?:string,startedAt?:string,completedAt?:string,error?:string,stackTrace?:string,ref?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?number} */
    threadId;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    title;
    /** @type {?string} */
    model;
    /** @type {?number} */
    duration;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    inputPrice;
    /** @type {?number} */
    inputTokens;
    /** @type {?number} */
    inputCachedTokens;
    /** @type {?number} */
    outputPrice;
    /** @type {?number} */
    outputTokens;
    /** @type {?number} */
    totalTokens;
    /** @type {?string} */
    usage;
    /** @type {?string} */
    provider;
    /** @type {?string} */
    providerModel;
    /** @type {?string} */
    providerRef;
    /** @type {?string} */
    finishReason;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    error;
    /** @type {?string} */
    stackTrace;
    /** @type {?string} */
    ref;
    getTypeName() { return 'UpdateChatRequest' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatSource {
    /** @param {{id?:number,filestoreId?:number,user?:string,createdAt?:string,updatedAt?:string,name?:string,type?:string,enabled?:number,config?:string,category?:string,rules?:string,include?:string,exclude?:string,extract?:string,chunking?:string,volatile?:string,extractorVer?:string,schedule?:string,onDelete?:string,cursor?:string,lastRunId?:number,lastRunAt?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    filestoreId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    name;
    /** @type {?string} */
    type;
    /** @type {number} */
    enabled;
    /** @type {?string} */
    config;
    /** @type {?string} */
    category;
    /** @type {?string} */
    rules;
    /** @type {?string} */
    include;
    /** @type {?string} */
    exclude;
    /** @type {?string} */
    extract;
    /** @type {?string} */
    chunking;
    /** @type {?string} */
    volatile;
    /** @type {?string} */
    extractorVer;
    /** @type {?string} */
    schedule;
    /** @type {?string} */
    onDelete;
    /** @type {?string} */
    cursor;
    /** @type {?number} */
    lastRunId;
    /** @type {?string} */
    lastRunAt;
    /** @type {?string} */
    error;
    getTypeName() { return 'UpdateChatSource' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatSourceRun {
    /** @param {{id?:number,sourceId?:number,user?:string,startedAt?:string,completedAt?:string,status?:string,dryRun?:number,discovered?:number,added?:number,changed?:number,metadataOnly?:number,unchanged?:number,removed?:number,skipped?:number,failed?:number,bytes?:number,plan?:string,log?:string,error?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    sourceId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    status;
    /** @type {number} */
    dryRun;
    /** @type {number} */
    discovered;
    /** @type {number} */
    added;
    /** @type {number} */
    changed;
    /** @type {number} */
    metadataOnly;
    /** @type {number} */
    unchanged;
    /** @type {number} */
    removed;
    /** @type {number} */
    skipped;
    /** @type {number} */
    failed;
    /** @type {number} */
    bytes;
    /** @type {?string} */
    plan;
    /** @type {?string} */
    log;
    /** @type {?string} */
    error;
    getTypeName() { return 'UpdateChatSourceRun' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatThread {
    /** @param {{id?:number,user?:string,createdAt?:string,updatedAt?:string,title?:string,systemPrompt?:string,model?:string,modelInfo?:string,modalities?:string,messages?:string,streamingMessage?:string,args?:string,tools?:string,toolHistory?:string,cost?:number,inputTokens?:number,outputTokens?:number,stats?:string,provider?:string,providerModel?:string,startedAt?:string,completedAt?:string,metadata?:string,status?:string,error?:string,ref?:string,providerResponse?:string,contextTokens?:number,parentId?:number,publishedAt?:string,publishedUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    user;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    title;
    /** @type {?string} */
    systemPrompt;
    /** @type {?string} */
    model;
    /** @type {?string} */
    modelInfo;
    /** @type {?string} */
    modalities;
    /** @type {?string} */
    messages;
    /** @type {?string} */
    streamingMessage;
    /** @type {?string} */
    args;
    /** @type {?string} */
    tools;
    /** @type {?string} */
    toolHistory;
    /** @type {?number} */
    cost;
    /** @type {?number} */
    inputTokens;
    /** @type {?number} */
    outputTokens;
    /** @type {?string} */
    stats;
    /** @type {?string} */
    provider;
    /** @type {?string} */
    providerModel;
    /** @type {?string} */
    startedAt;
    /** @type {?string} */
    completedAt;
    /** @type {?string} */
    metadata;
    /** @type {?string} */
    status;
    /** @type {?string} */
    error;
    /** @type {?string} */
    ref;
    /** @type {?string} */
    providerResponse;
    /** @type {?number} */
    contextTokens;
    /** @type {?number} */
    parentId;
    /** @type {?string} */
    publishedAt;
    /** @type {?string} */
    publishedUrl;
    getTypeName() { return 'UpdateChatThread' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatToolApproval {
    /** @param {{id?:number,batchId?:string,threadId?:number,user?:string,toolCallId?:string,toolName?:string,apiName?:string,requestType?:string,method?:string,route?:string,safety?:string,status?:string,sequence?:number,description?:string,schema?:string,proposedArgs?:string,effectiveArgs?:string,result?:string,toolResult?:string,error?:string,reason?:string,createdAt?:string,updatedAt?:string,resolvedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    batchId;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    toolCallId;
    /** @type {?string} */
    toolName;
    /** @type {?string} */
    apiName;
    /** @type {?string} */
    requestType;
    /** @type {?string} */
    method;
    /** @type {?string} */
    route;
    /** @type {?string} */
    safety;
    /** @type {?string} */
    status;
    /** @type {number} */
    sequence;
    /** @type {?string} */
    description;
    /** @type {?string} */
    schema;
    /** @type {?string} */
    proposedArgs;
    /** @type {?string} */
    effectiveArgs;
    /** @type {?string} */
    result;
    /** @type {?string} */
    toolResult;
    /** @type {?string} */
    error;
    /** @type {?string} */
    reason;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    resolvedAt;
    getTypeName() { return 'UpdateChatToolApproval' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateChatToolApprovalBatch {
    /** @param {{id?:string,threadId?:number,user?:string,status?:string,createdAt?:string,updatedAt?:string,completedAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    id;
    /** @type {number} */
    threadId;
    /** @type {?string} */
    user;
    /** @type {?string} */
    status;
    /** @type {?string} */
    createdAt;
    /** @type {?string} */
    updatedAt;
    /** @type {?string} */
    completedAt;
    getTypeName() { return 'UpdateChatToolApprovalBatch' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateCoffeeShopOrder {
    /** @param {{id?:number,orderNumber?:string,customerName?:string,customerUserId?:string,status?:string,notes?:string,subtotal?:number,createdDate?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    orderNumber;
    /** @type {?string} */
    customerName;
    /** @type {?string} */
    customerUserId;
    /** @type {?string} */
    status;
    /** @type {?string} */
    notes;
    /** @type {number} */
    subtotal;
    /** @type {?string} */
    createdDate;
    getTypeName() { return 'UpdateCoffeeShopOrder' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateCoffeeShopOrderItem {
    /** @param {{id?:number,coffeeShopOrderId?:number,productId?:number,productName?:string,quantity?:number,size?:string,temperature?:string,optionsJson?:string,unitPrice?:number,lineTotal?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    coffeeShopOrderId;
    /** @type {number} */
    productId;
    /** @type {?string} */
    productName;
    /** @type {number} */
    quantity;
    /** @type {?string} */
    size;
    /** @type {?string} */
    temperature;
    /** @type {?string} */
    optionsJson;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    lineTotal;
    getTypeName() { return 'UpdateCoffeeShopOrderItem' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateContextSnapshot {
    /** @param {{id?:number,threadId?:number,runId?:number,version?:number,fromSequence?:number,toSequence?:number,summary?:string,tokenCount?:number,model?:string,createdAt?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    threadId;
    /** @type {?number} */
    runId;
    /** @type {number} */
    version;
    /** @type {number} */
    fromSequence;
    /** @type {number} */
    toSequence;
    /** @type {?string} */
    summary;
    /** @type {?number} */
    tokenCount;
    /** @type {?string} */
    model;
    /** @type {?string} */
    createdAt;
    getTypeName() { return 'UpdateContextSnapshot' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateEFMigrationsHistory {
    /** @param {{migrationId?:string,productVersion?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    migrationId;
    /** @type {?string} */
    productVersion;
    getTypeName() { return 'UpdateEFMigrationsHistory' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateEFMigrationsLock {
    /** @param {{id?:number,timestamp?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    timestamp;
    getTypeName() { return 'UpdateEFMigrationsLock' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateFileSystemFile {
    /** @param {{id?:number,fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    fileName;
    /** @type {?string} */
    filePath;
    /** @type {?string} */
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
    /** @type {?string} */
    fileAccessType;
    /** @type {?string} */
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
    /** @type {?string} */
    name;
    /** @type {?string} */
    description;
    /** @type {?string} */
    createdDate;
    /** @type {?string} */
    completedDate;
    /** @type {?string} */
    connectionString;
    /** @type {?string} */
    namedConnection;
    /** @type {?string} */
    log;
    /** @type {?string} */
    errorCode;
    /** @type {?string} */
    errorMessage;
    /** @type {?string} */
    errorStackTrace;
    /** @type {?string} */
    meta;
    getTypeName() { return 'UpdateMigration' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateOption {
    /** @param {{id?:number,type?:string,names?:string,allowQuantity?:number,quantityLabel?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    type;
    /** @type {?string} */
    names;
    /** @type {?number} */
    allowQuantity;
    /** @type {?string} */
    quantityLabel;
    getTypeName() { return 'UpdateOption' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateOptionQuantity {
    /** @param {{id?:number,name?:string,value?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    name;
    /** @type {number} */
    value;
    getTypeName() { return 'UpdateOptionQuantity' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateProduct {
    /** @param {{id?:number,categoryId?:number,name?:string,cost?:number,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {number} */
    categoryId;
    /** @type {?string} */
    name;
    /** @type {number} */
    cost;
    /** @type {?string} */
    imageUrl;
    getTypeName() { return 'UpdateProduct' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}
export class UpdateValidationRule {
    /** @param {{id?:number,type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {?string} */
    type;
    /** @type {?string} */
    field;
    /** @type {?string} */
    createdBy;
    /** @type {?string} */
    createdDate;
    /** @type {?string} */
    modifiedBy;
    /** @type {?string} */
    modifiedDate;
    /** @type {?string} */
    suspendedBy;
    /** @type {?string} */
    suspendedDate;
    /** @type {?string} */
    notes;
    /** @type {?string} */
    validator;
    /** @type {?string} */
    condition;
    /** @type {?string} */
    errorCode;
    /** @type {?string} */
    message;
    getTypeName() { return 'UpdateValidationRule' }
    getMethod() { return 'PUT' }
    createResponse() { return new IdResponse() }
}

