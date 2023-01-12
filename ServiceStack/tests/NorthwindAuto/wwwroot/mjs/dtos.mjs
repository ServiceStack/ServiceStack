/* Options:
Date: 2023-01-12 15:41:37
Version: 6.51
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
    JobApplicationStatus["Applied"] = "Applied";
    JobApplicationStatus["PhoneScreening"] = "PhoneScreening";
    JobApplicationStatus["PhoneScreeningCompleted"] = "PhoneScreeningCompleted";
    JobApplicationStatus["Interview"] = "Interview";
    JobApplicationStatus["InterviewCompleted"] = "InterviewCompleted";
    JobApplicationStatus["Offer"] = "Offer";
    JobApplicationStatus["Disqualified"] = "Disqualified";
})(JobApplicationStatus || (JobApplicationStatus = {}));
export class AuditBase {
    /** @param {{createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
/** @typedef {'None'|'Marketing'|'Accounts'|'Legal'|'HumanResources'} */
export var Department;
(function (Department) {
    Department["None"] = "None";
    Department["Marketing"] = "Marketing";
    Department["Accounts"] = "Accounts";
    Department["Legal"] = "Legal";
    Department["HumanResources"] = "HumanResources";
})(Department || (Department = {}));
export class AppUser {
    /** @param {{id?:number,displayName?:string,email?:string,profileUrl?:string,department?:Department,title?:string,jobArea?:string,location?:string,salary?:number,about?:string,isArchived?:boolean,archivedDate?:string,lastLoginDate?:string,lastLoginIp?:string,userName?:string,primaryEmail?:string,firstName?:string,lastName?:string,company?:string,country?:string,phoneNumber?:string,birthDate?:string,birthDateRaw?:string,address?:string,address2?:string,city?:string,state?:string,culture?:string,fullName?:string,gender?:string,language?:string,mailAddress?:string,nickname?:string,postalCode?:string,timeZone?:string,salt?:string,passwordHash?:string,digestHa1Hash?:string,roles?:string[],permissions?:string[],createdDate?:string,modifiedDate?:string,invalidLoginAttempts?:number,lastLoginAttempt?:string,lockedDate?:string,recoveryToken?:string,refId?:number,refIdStr?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    displayName;
    /** @type {string} */
    email;
    /** @type {string} */
    profileUrl;
    /** @type {Department} */
    department;
    /** @type {string} */
    title;
    /** @type {string} */
    jobArea;
    /** @type {string} */
    location;
    /** @type {number} */
    salary;
    /** @type {string} */
    about;
    /** @type {boolean} */
    isArchived;
    /** @type {?string} */
    archivedDate;
    /** @type {?string} */
    lastLoginDate;
    /** @type {string} */
    lastLoginIp;
    /** @type {string} */
    userName;
    /** @type {string} */
    primaryEmail;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    company;
    /** @type {string} */
    country;
    /** @type {string} */
    phoneNumber;
    /** @type {?string} */
    birthDate;
    /** @type {string} */
    birthDateRaw;
    /** @type {string} */
    address;
    /** @type {string} */
    address2;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    culture;
    /** @type {string} */
    fullName;
    /** @type {string} */
    gender;
    /** @type {string} */
    language;
    /** @type {string} */
    mailAddress;
    /** @type {string} */
    nickname;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    timeZone;
    /** @type {string} */
    salt;
    /** @type {string} */
    passwordHash;
    /** @type {string} */
    digestHa1Hash;
    /** @type {string[]} */
    roles;
    /** @type {string[]} */
    permissions;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedDate;
    /** @type {number} */
    invalidLoginAttempts;
    /** @type {?string} */
    lastLoginAttempt;
    /** @type {?string} */
    lockedDate;
    /** @type {string} */
    recoveryToken;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class PhoneScreen extends AuditBase {
    /** @param {{id?:number,appUserId?:number,appUser?:AppUser,jobApplicationId?:number,applicationStatus?:JobApplicationStatus,notes?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {number} */
    appUserId;
    /** @type {AppUser} */
    appUser;
    /** @type {number} */
    jobApplicationId;
    /** @type {?JobApplicationStatus} */
    applicationStatus;
    /** @type {string} */
    notes;
}
export class Interview extends AuditBase {
    /** @param {{id?:number,bookingTime?:string,jobApplicationId?:number,appUserId?:number,appUser?:AppUser,applicationStatus?:JobApplicationStatus,notes?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    bookingTime;
    /** @type {number} */
    jobApplicationId;
    /** @type {number} */
    appUserId;
    /** @type {AppUser} */
    appUser;
    /** @type {?JobApplicationStatus} */
    applicationStatus;
    /** @type {string} */
    notes;
}
export class JobOffer extends AuditBase {
    /** @param {{id?:number,salaryOffer?:number,jobApplicationId?:number,appUserId?:number,appUser?:AppUser,notes?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {number} */
    salaryOffer;
    /** @type {number} */
    jobApplicationId;
    /** @type {number} */
    appUserId;
    /** @type {AppUser} */
    appUser;
    /** @type {string} */
    notes;
}
/** @typedef {'Transparent'|'Red'|'Green'|'Blue'} */
export var Colors;
(function (Colors) {
    Colors["Transparent"] = "Transparent";
    Colors["Red"] = "Red";
    Colors["Green"] = "Green";
    Colors["Blue"] = "Blue";
})(Colors || (Colors = {}));
export class Attachment {
    /** @param {{fileName?:string,filePath?:string,contentType?:string,contentLength?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    fileName;
    /** @type {string} */
    filePath;
    /** @type {string} */
    contentType;
    /** @type {number} */
    contentLength;
}
/** @typedef TKey {any} */
/** @typedef  TValue {any} */
export class KeyValuePair {
    /** @param {{key?:TKey,value?:TValue}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {TKey} */
    key;
    /** @type {TValue} */
    value;
}
export class SubType {
    /** @param {{id?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
}
export class Poco {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
}
export class QueryBase {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
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
export class QueryData extends QueryBase {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
}
/** @typedef {'FullTime'|'PartTime'|'Casual'|'Contract'} */
export var EmploymentType;
(function (EmploymentType) {
    EmploymentType["FullTime"] = "FullTime";
    EmploymentType["PartTime"] = "PartTime";
    EmploymentType["Casual"] = "Casual";
    EmploymentType["Contract"] = "Contract";
})(EmploymentType || (EmploymentType = {}));
export class Job extends AuditBase {
    /** @param {{id?:number,title?:string,employmentType?:EmploymentType,company?:string,location?:string,salaryRangeLower?:number,salaryRangeUpper?:number,description?:string,applications?:JobApplication[],closing?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
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
    applications;
    /** @type {string} */
    closing;
}
export class Contact extends AuditBase {
    /** @param {{id?:number,displayName?:string,profileUrl?:string,firstName?:string,lastName?:string,salaryExpectation?:number,jobType?:string,availabilityWeeks?:number,preferredWorkType?:EmploymentType,preferredLocation?:string,email?:string,phone?:string,about?:string,applications?:JobApplication[],createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
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
    applications;
}
export class JobApplicationComment extends AuditBase {
    /** @param {{id?:number,appUserId?:number,appUser?:AppUser,jobApplicationId?:number,comment?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {number} */
    appUserId;
    /** @type {AppUser} */
    appUser;
    /** @type {number} */
    jobApplicationId;
    /** @type {string} */
    comment;
}
export class JobApplicationAttachment {
    /** @param {{id?:number,jobApplicationId?:number,fileName?:string,filePath?:string,contentType?:string,contentLength?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
export class JobApplicationEvent extends AuditBase {
    /** @param {{id?:number,jobApplicationId?:number,appUserId?:number,appUser?:AppUser,description?:string,status?:JobApplicationStatus,eventDate?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {number} */
    jobApplicationId;
    /** @type {number} */
    appUserId;
    /** @type {AppUser} */
    appUser;
    /** @type {string} */
    description;
    /** @type {?JobApplicationStatus} */
    status;
    /** @type {string} */
    eventDate;
}
export class JobApplication extends AuditBase {
    /** @param {{id?:number,jobId?:number,contactId?:number,position?:Job,applicant?:Contact,comments?:JobApplicationComment[],appliedDate?:string,applicationStatus?:JobApplicationStatus,attachments?:JobApplicationAttachment[],events?:JobApplicationEvent[],phoneScreen?:PhoneScreen,interview?:Interview,jobOffer?:JobOffer,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
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
    comments;
    /** @type {string} */
    appliedDate;
    /** @type {JobApplicationStatus} */
    applicationStatus;
    /** @type {JobApplicationAttachment[]} */
    attachments;
    /** @type {JobApplicationEvent[]} */
    events;
    /** @type {PhoneScreen} */
    phoneScreen;
    /** @type {Interview} */
    interview;
    /** @type {JobOffer} */
    jobOffer;
}
/** @typedef {'Single'|'Double'|'Queen'|'Twin'|'Suite'} */
export var RoomType;
(function (RoomType) {
    RoomType["Single"] = "Single";
    RoomType["Double"] = "Double";
    RoomType["Queen"] = "Queen";
    RoomType["Twin"] = "Twin";
    RoomType["Suite"] = "Suite";
})(RoomType || (RoomType = {}));
export class Booking extends AuditBase {
    /** @param {{id?:number,name?:string,roomType?:RoomType,roomNumber?:number,bookingStartDate?:string,bookingEndDate?:string,cost?:number,notes?:string,cancelled?:boolean,timeAgo?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
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
    notes;
    /** @type {?boolean} */
    cancelled;
    /** @type {string} */
    timeAgo;
}
/** @typedef {'Public'|'Team'|'Private'} */
export var FileAccessType;
(function (FileAccessType) {
    FileAccessType["Public"] = "Public";
    FileAccessType["Team"] = "Team";
    FileAccessType["Private"] = "Private";
})(FileAccessType || (FileAccessType = {}));
export class FileSystemFile {
    /** @param {{id?:number,fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
export class FileSystemItem {
    /** @param {{id?:number,fileAccessType?:FileAccessType,file?:FileSystemFile,appUserId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {?FileAccessType} */
    fileAccessType;
    /** @type {FileSystemFile} */
    file;
    /** @type {number} */
    appUserId;
}
/** @typedef {'Home'|'Mobile'|'Work'} */
export var PhoneKind;
(function (PhoneKind) {
    PhoneKind["Home"] = "Home";
    PhoneKind["Mobile"] = "Mobile";
    PhoneKind["Work"] = "Work";
})(PhoneKind || (PhoneKind = {}));
export class Phone {
    /** @param {{kind?:PhoneKind,number?:string,ext?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {PhoneKind} */
    kind;
    /** @type {string} */
    number;
    /** @type {string} */
    ext;
}
export class PlayerGameItem {
    /** @param {{id?:number,playerId?:number,gameItemName?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    PlayerRole["Leader"] = "Leader";
    PlayerRole["Player"] = "Player";
    PlayerRole["NonPlayer"] = "NonPlayer";
})(PlayerRole || (PlayerRole = {}));
/** @typedef {number} */
export var PlayerRegion;
(function (PlayerRegion) {
    PlayerRegion[PlayerRegion["Africa"] = 1] = "Africa";
    PlayerRegion[PlayerRegion["Americas"] = 2] = "Americas";
    PlayerRegion[PlayerRegion["Asia"] = 3] = "Asia";
    PlayerRegion[PlayerRegion["Australasia"] = 4] = "Australasia";
    PlayerRegion[PlayerRegion["Europe"] = 5] = "Europe";
})(PlayerRegion || (PlayerRegion = {}));
export class Profile extends AuditBase {
    /** @param {{id?:number,role?:PlayerRole,region?:PlayerRegion,username?:string,highScore?:number,gamesPlayed?:number,energy?:number,profileUrl?:string,coverUrl?:string,meta?:{ [index: string]: string; },createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
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
    /** @type {?{ [index: string]: string; }} */
    meta;
}
export class Player extends AuditBase {
    /** @param {{id?:number,firstName?:string,lastName?:string,email?:string,phoneNumbers?:Phone[],gameItems?:PlayerGameItem[],profile?:Profile,profileId?:number,savedLevelId?:string,rowVersion?:number,capital?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    email;
    /** @type {Phone[]} */
    phoneNumbers;
    /** @type {PlayerGameItem[]} */
    gameItems;
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
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {string} */
    name;
    /** @type {string} */
    imageUrl;
    /** @type {?string} */
    description;
    /** @type {string} */
    dateAdded;
}
export class Product {
    /** @param {{id?:number,productName?:string,supplierId?:number,categoryId?:number,quantityPerUnit?:string,unitPrice?:number,unitsInStock?:number,unitsOnOrder?:number,reorderLevel?:number,discontinued?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    productName;
    /** @type {number} */
    supplierId;
    /** @type {number} */
    categoryId;
    /** @type {string} */
    quantityPerUnit;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    unitsInStock;
    /** @type {number} */
    unitsOnOrder;
    /** @type {number} */
    reorderLevel;
    /** @type {number} */
    discontinued;
}
export class Region {
    /** @param {{id?:number,regionDescription?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    regionDescription;
}
export class Shipper {
    /** @param {{id?:number,companyName?:string,phone?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    phone;
}
export class Supplier {
    /** @param {{id?:number,companyName?:string,contactName?:string,contactTitle?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,phone?:string,fax?:string,homePage?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    contactName;
    /** @type {string} */
    contactTitle;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    homePage;
}
export class Territory {
    /** @param {{id?:string,territoryDescription?:string,regionId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {string} */
    territoryDescription;
    /** @type {number} */
    regionId;
}
export class UserAuthRole {
    /** @param {{id?:number,userAuthId?:number,role?:string,permission?:string,createdDate?:string,modifiedDate?:string,refId?:number,refIdStr?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {number} */
    userAuthId;
    /** @type {string} */
    role;
    /** @type {string} */
    permission;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedDate;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class ValidateRule {
    /** @param {{validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    constructor(init) { super(init); Object.assign(this, init); }
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
export class Category {
    /** @param {{id?:number,categoryName?:string,description?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    categoryName;
    /** @type {string} */
    description;
}
export class Customer {
    /** @param {{id?:string,companyName?:string,contactName?:string,contactTitle?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,phone?:string,fax?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    contactName;
    /** @type {string} */
    contactTitle;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
}
export class Employee {
    /** @param {{id?:number,lastName?:string,firstName?:string,photoPath?:string,title?:string,reportsTo?:number,titleOfCourtesy?:string,birthDate?:string,hireDate?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,homePhone?:string,extension?:string,photo?:string,notes?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    lastName;
    /** @type {string} */
    firstName;
    /** @type {string} */
    photoPath;
    /** @type {string} */
    title;
    /** @type {?number} */
    reportsTo;
    /** @type {string} */
    titleOfCourtesy;
    /** @type {string} */
    birthDate;
    /** @type {string} */
    hireDate;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    homePhone;
    /** @type {string} */
    extension;
    /** @type {string} */
    photo;
    /** @type {string} */
    notes;
}
export class EmployeeTerritory {
    /** @param {{id?:string,employeeId?:number,territoryId?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {number} */
    employeeId;
    /** @type {string} */
    territoryId;
}
export class Migration {
    /** @param {{id?:number,name?:string,description?:string,createdDate?:string,completedDate?:string,connectionString?:string,namedConnection?:string,log?:string,errorCode?:string,errorMessage?:string,errorStackTrace?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
export class Order {
    /** @param {{id?:number,customerId?:string,employeeId?:number,orderDate?:string,requiredDate?:string,shippedDate?:string,shipVia?:number,freight?:number,shipName?:string,shipAddress?:string,shipCity?:string,shipRegion?:string,shipPostalCode?:string,shipCountry?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    customerId;
    /** @type {number} */
    employeeId;
    /** @type {string} */
    orderDate;
    /** @type {string} */
    requiredDate;
    /** @type {string} */
    shippedDate;
    /** @type {?number} */
    shipVia;
    /** @type {number} */
    freight;
    /** @type {string} */
    shipName;
    /** @type {string} */
    shipAddress;
    /** @type {string} */
    shipCity;
    /** @type {string} */
    shipRegion;
    /** @type {string} */
    shipPostalCode;
    /** @type {string} */
    shipCountry;
}
export class OrderDetail {
    /** @param {{id?:string,orderId?:number,productId?:number,unitPrice?:number,quantity?:number,discount?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {number} */
    orderId;
    /** @type {number} */
    productId;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    quantity;
    /** @type {number} */
    discount;
}
export class Albums {
    /** @param {{albumId?:number,title?:string,artistId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    albumId;
    /** @type {string} */
    title;
    /** @type {number} */
    artistId;
}
export class Artists {
    /** @param {{artistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    artistId;
    /** @type {string} */
    name;
}
export class Customers {
    /** @param {{customerId?:number,firstName?:string,lastName?:string,company?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string,supportRepId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    constructor(init) { Object.assign(this, init); }
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
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    genreId;
    /** @type {string} */
    name;
}
export class InvoiceItems {
    /** @param {{invoiceLineId?:number,invoiceId?:number,trackId?:number,unitPrice?:number,quantity?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    constructor(init) { Object.assign(this, init); }
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
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    mediaTypeId;
    /** @type {string} */
    name;
}
export class Playlists {
    /** @param {{playlistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    playlistId;
    /** @type {string} */
    name;
}
export class Tracks {
    /** @param {{trackId?:number,name?:string,albumId?:number,mediaTypeId?:number,genreId?:number,composer?:string,milliseconds?:number,bytes?:number,unitPrice?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
/** @typedef T {any} */
export class QueryDb extends QueryBase {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
}
export class Level {
    /** @param {{id?:string,data?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {string} */
    data;
}
export class ResponseError {
    /** @param {{errorCode?:string,fieldName?:string,message?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    constructor(init) { Object.assign(this, init); }
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
export class UserApiKey {
    /** @param {{key?:string,keyType?:string,expiryDate?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    key;
    /** @type {string} */
    keyType;
    /** @type {?string} */
    expiryDate;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class TalentStatsResponse {
    /** @param {{totalJobs?:number,totalContacts?:number,avgSalaryExpectation?:number,avgSalaryLower?:number,avgSalaryUpper?:number,preferredRemotePercentage?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
export class FormDataTest {
    /** @param {{hidden?:boolean,string?:string,int?:number,dateTime?:string,dateOnly?:string,timeSpan?:string,timeOnly?:string,password?:string,checkboxString?:string[],radioString?:string,radioColors?:Colors,checkboxColors?:Colors[],selectColors?:Colors,multiSelectColors?:Colors[],profileUrl?:string,attachments?:Attachment[]}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    attachments;
    getTypeName() { return 'FormDataTest'; };
    getMethod() { return 'POST'; };
    createResponse() { return new FormDataTest(); };
}
export class Movie {
    /** @param {{movieID?:string,movieNo?:number,name?:string,description?:string,movieRef?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    result;
    /** @type {?ResponseStatus} */
    responseStatus;
}
export class AllTypes {
    /** @param {{id?:number,nullableId?:number,byte?:number,short?:number,int?:number,long?:number,uShort?:number,uInt?:number,uLong?:number,float?:number,double?:number,decimal?:number,string?:string,dateTime?:string,timeSpan?:string,dateTimeOffset?:string,guid?:string,char?:string,keyValuePair?:KeyValuePair<string, string>,nullableDateTime?:string,nullableTimeSpan?:string,stringList?:string[],stringArray?:string[],stringMap?:{ [index: string]: string; },intStringMap?:{ [index: number]: string; },subType?:SubType}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    stringList;
    /** @type {string[]} */
    stringArray;
    /** @type {{ [index: string]: string; }} */
    stringMap;
    /** @type {{ [index: number]: string; }} */
    intStringMap;
    /** @type {SubType} */
    subType;
    getTypeName() { return 'AllTypes'; };
    getMethod() { return 'POST'; };
    createResponse() { return new AllTypes(); };
}
export class AllCollectionTypes {
    /** @param {{intArray?:number[],intList?:number[],stringArray?:string[],stringList?:string[],floatArray?:number[],doubleList?:number[],byteArray?:string,charArray?:string[],decimalList?:number[],pocoArray?:Poco[],pocoList?:Poco[],pocoLookup?:{ [index: string]: Poco[]; },pocoLookupMap?:{ [index: string]: { [index:string]: Poco; }[]; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number[]} */
    intArray;
    /** @type {number[]} */
    intList;
    /** @type {string[]} */
    stringArray;
    /** @type {string[]} */
    stringList;
    /** @type {number[]} */
    floatArray;
    /** @type {number[]} */
    doubleList;
    /** @type {string} */
    byteArray;
    /** @type {string[]} */
    charArray;
    /** @type {number[]} */
    decimalList;
    /** @type {Poco[]} */
    pocoArray;
    /** @type {Poco[]} */
    pocoList;
    /** @type {{ [index: string]: Poco[]; }} */
    pocoLookup;
    /** @type {{ [index: string]: { [index:string]: Poco; }[]; }} */
    pocoLookupMap;
    getTypeName() { return 'AllCollectionTypes'; };
    getMethod() { return 'POST'; };
    createResponse() { return new AllCollectionTypes(); };
}
export class ProfileGenResponse {
    constructor(init) { Object.assign(this, init); }
}
export class Todo {
    /** @param {{id?:number,text?:string,isFinished?:boolean}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    text;
    /** @type {boolean} */
    isFinished;
}
/** @typedef Todo {any} */
export class QueryResponse {
    /** @param {{offset?:number,total?:number,results?:Todo[],meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    offset;
    /** @type {number} */
    total;
    /** @type {Todo[]} */
    results;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class AuthenticateResponse {
    /** @param {{userId?:string,sessionId?:string,userName?:string,displayName?:string,referrerUrl?:string,bearerToken?:string,refreshToken?:string,profileUrl?:string,roles?:string[],permissions?:string[],responseStatus?:ResponseStatus,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    constructor(init) { Object.assign(this, init); }
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
    constructor(init) { Object.assign(this, init); }
    /** @type {string[]} */
    allRoles;
    /** @type {string[]} */
    allPermissions;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class GetAccessTokenResponse {
    /** @param {{accessToken?:string,meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    accessToken;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class GetApiKeysResponse {
    /** @param {{results?:UserApiKey[],meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {UserApiKey[]} */
    results;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class RegenerateApiKeysResponse {
    /** @param {{results?:UserApiKey[],meta?:{ [index: string]: string; },responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {UserApiKey[]} */
    results;
    /** @type {{ [index: string]: string; }} */
    meta;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class RegisterResponse {
    /** @param {{userId?:string,sessionId?:string,userName?:string,referrerUrl?:string,bearerToken?:string,refreshToken?:string,roles?:string[],permissions?:string[],responseStatus?:ResponseStatus,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    userId;
    /** @type {string} */
    sessionId;
    /** @type {string} */
    userName;
    /** @type {string} */
    referrerUrl;
    /** @type {string} */
    bearerToken;
    /** @type {string} */
    refreshToken;
    /** @type {string[]} */
    roles;
    /** @type {string[]} */
    permissions;
    /** @type {ResponseStatus} */
    responseStatus;
    /** @type {{ [index: string]: string; }} */
    meta;
}
export class IdResponse {
    /** @param {{id?:string,responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class CreatePhoneScreen {
    /** @param {{jobApplicationId?:number,appUserId?:number,applicationStatus?:JobApplicationStatus}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    jobApplicationId;
    /** @type {number} */
    appUserId;
    /** @type {JobApplicationStatus} */
    applicationStatus;
    getTypeName() { return 'CreatePhoneScreen'; };
    getMethod() { return 'POST'; };
    createResponse() { return new PhoneScreen(); };
}
export class UpdatePhoneScreen {
    /** @param {{id?:number,jobApplicationId?:number,notes?:string,applicationStatus?:JobApplicationStatus}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    /** @type {?string} */
    notes;
    /** @type {?JobApplicationStatus} */
    applicationStatus;
    getTypeName() { return 'UpdatePhoneScreen'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new PhoneScreen(); };
}
export class CreateInterview {
    /** @param {{bookingTime?:string,jobApplicationId?:number,appUserId?:number,applicationStatus?:JobApplicationStatus}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    bookingTime;
    /** @type {number} */
    jobApplicationId;
    /** @type {number} */
    appUserId;
    /** @type {JobApplicationStatus} */
    applicationStatus;
    getTypeName() { return 'CreateInterview'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Interview(); };
}
export class UpdateInterview {
    /** @param {{id?:number,jobApplicationId?:number,notes?:string,applicationStatus?:JobApplicationStatus}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    /** @type {?string} */
    notes;
    /** @type {?JobApplicationStatus} */
    applicationStatus;
    getTypeName() { return 'UpdateInterview'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new Interview(); };
}
export class CreateJobOffer {
    /** @param {{salaryOffer?:number,jobApplicationId?:number,applicationStatus?:JobApplicationStatus,notes?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    salaryOffer;
    /** @type {number} */
    jobApplicationId;
    /** @type {JobApplicationStatus} */
    applicationStatus;
    /** @type {string} */
    notes;
    getTypeName() { return 'CreateJobOffer'; };
    getMethod() { return 'POST'; };
    createResponse() { return new JobOffer(); };
}
export class TalentStats {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'TalentStats'; };
    getMethod() { return 'GET'; };
    createResponse() { return new TalentStatsResponse(); };
}
export class GetProfileImage {
    /** @param {{type?:string,size?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {?string} */
    type;
    /** @type {?string} */
    size;
    getTypeName() { return 'GetProfileImage'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Blob(); };
}
export class MovieGETRequest {
    /** @param {{movieID?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /**
     * @type {string}
     * @description Unique Id of the movie */
    movieID;
    getTypeName() { return 'MovieGETRequest'; };
    getMethod() { return 'GET'; };
    createResponse() { return new Movie(); };
}
export class MoviePOSTRequest extends Movie {
    /** @param {{movieID?:string,movieNo?:number,movieRef?:string,movieID?:string,movieNo?:number,name?:string,description?:string,movieRef?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {string} */
    movieID;
    /** @type {number} */
    movieNo;
    /** @type {?string} */
    movieRef;
    getTypeName() { return 'MoviePOSTRequest'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Movie(); };
}
export class Greet {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
    getTypeName() { return 'Greet'; };
    getMethod() { return 'GET'; };
    createResponse() { return new HelloResponse(); };
}
export class Hello {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
    getTypeName() { return 'Hello'; };
    getMethod() { return 'GET'; };
    createResponse() { return new HelloResponse(); };
}
export class HelloVeryLongOperationNameVersions {
    /** @param {{name?:string,names?:string[],ids?:number[]}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {?string} */
    name;
    /** @type {?string[]} */
    names;
    /** @type {?number[]} */
    ids;
    getTypeName() { return 'HelloVeryLongOperationNameVersions'; };
    getMethod() { return 'GET'; };
    createResponse() { return new HelloResponse(); };
}
export class HelloSecure {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
    getTypeName() { return 'HelloSecure'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new HelloResponse(); };
}
export class HelloBookingList {
    /** @param {{Alias?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    Alias;
    getTypeName() { return 'HelloBookingList'; };
    getMethod() { return 'POST'; };
    createResponse() { return []; };
}
export class ProfileGen {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'ProfileGen'; };
    getMethod() { return 'POST'; };
    createResponse() { return new ProfileGenResponse(); };
}
export class QueryTodos extends QueryData {
    /** @param {{id?:number,ids?:number[],textContains?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    /** @type {?number[]} */
    ids;
    /** @type {?string} */
    textContains;
    getTypeName() { return 'QueryTodos'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class CreateTodo {
    /** @param {{text?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    text;
    getTypeName() { return 'CreateTodo'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Todo(); };
}
export class UpdateTodo {
    /** @param {{id?:number,text?:string,isFinished?:boolean}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    text;
    /** @type {boolean} */
    isFinished;
    getTypeName() { return 'UpdateTodo'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new Todo(); };
}
export class DeleteTodo {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteTodo'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class DeleteTodos {
    /** @param {{ids?:number[]}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number[]} */
    ids;
    getTypeName() { return 'DeleteTodos'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class Authenticate {
    /** @param {{provider?:string,state?:string,oauth_token?:string,oauth_verifier?:string,userName?:string,password?:string,rememberMe?:boolean,errorView?:string,nonce?:string,uri?:string,response?:string,qop?:string,nc?:string,cnonce?:string,accessToken?:string,accessTokenSecret?:string,scope?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /**
     * @type {string}
     * @description AuthProvider, e.g. credentials */
    provider;
    /** @type {string} */
    state;
    /** @type {string} */
    oauth_token;
    /** @type {string} */
    oauth_verifier;
    /** @type {string} */
    userName;
    /** @type {string} */
    password;
    /** @type {?boolean} */
    rememberMe;
    /** @type {string} */
    errorView;
    /** @type {string} */
    nonce;
    /** @type {string} */
    uri;
    /** @type {string} */
    response;
    /** @type {string} */
    qop;
    /** @type {string} */
    nc;
    /** @type {string} */
    cnonce;
    /** @type {string} */
    accessToken;
    /** @type {string} */
    accessTokenSecret;
    /** @type {string} */
    scope;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'Authenticate'; };
    getMethod() { return 'POST'; };
    createResponse() { return new AuthenticateResponse(); };
}
export class AssignRoles {
    /** @param {{userName?:string,permissions?:string[],roles?:string[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    userName;
    /** @type {string[]} */
    permissions;
    /** @type {string[]} */
    roles;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'AssignRoles'; };
    getMethod() { return 'POST'; };
    createResponse() { return new AssignRolesResponse(); };
}
export class UnAssignRoles {
    /** @param {{userName?:string,permissions?:string[],roles?:string[],meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    userName;
    /** @type {string[]} */
    permissions;
    /** @type {string[]} */
    roles;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'UnAssignRoles'; };
    getMethod() { return 'POST'; };
    createResponse() { return new UnAssignRolesResponse(); };
}
export class GetAccessToken {
    /** @param {{refreshToken?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    refreshToken;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'GetAccessToken'; };
    getMethod() { return 'POST'; };
    createResponse() { return new GetAccessTokenResponse(); };
}
export class GetApiKeys {
    /** @param {{environment?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    environment;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'GetApiKeys'; };
    getMethod() { return 'GET'; };
    createResponse() { return new GetApiKeysResponse(); };
}
export class RegenerateApiKeys {
    /** @param {{environment?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    environment;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'RegenerateApiKeys'; };
    getMethod() { return 'POST'; };
    createResponse() { return new RegenerateApiKeysResponse(); };
}
export class Register {
    /** @param {{userName?:string,firstName?:string,lastName?:string,displayName?:string,email?:string,password?:string,confirmPassword?:string,autoLogin?:boolean,errorView?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    confirmPassword;
    /** @type {?boolean} */
    autoLogin;
    /** @type {string} */
    errorView;
    /** @type {{ [index: string]: string; }} */
    meta;
    getTypeName() { return 'Register'; };
    getMethod() { return 'POST'; };
    createResponse() { return new RegisterResponse(); };
}
export class CreateContact {
    /** @param {{firstName?:string,lastName?:string,profileUrl?:string,salaryExpectation?:number,jobType?:string,availabilityWeeks?:number,preferredWorkType?:EmploymentType,preferredLocation?:string,email?:string,phone?:string,about?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateContact'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Contact(); };
}
export class UpdateContact {
    /** @param {{id?:number,firstName?:string,lastName?:string,profileUrl?:string,salaryExpectation?:number,jobType?:string,availabilityWeeks?:number,preferredWorkType?:EmploymentType,preferredLocation?:string,email?:string,phone?:string,about?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateContact'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new Contact(); };
}
export class DeleteContact {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteContact'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateJob {
    /** @param {{title?:string,salaryRangeLower?:number,salaryRangeUpper?:number,description?:string,employmentType?:EmploymentType,company?:string,location?:string,closing?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateJob'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Job(); };
}
export class UpdateJob {
    /** @param {{id?:number,title?:string,salaryRangeLower?:number,salaryRangeUpper?:number,description?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateJob'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new Job(); };
}
export class DeleteJob {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteJob'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new Job(); };
}
export class CreateJobApplication {
    /** @param {{jobId?:number,contactId?:number,appliedDate?:string,applicationStatus?:JobApplicationStatus,attachments?:JobApplicationAttachment[]}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateJobApplication'; };
    getMethod() { return 'POST'; };
    createResponse() { return new JobApplication(); };
}
export class UpdateJobApplication {
    /** @param {{id?:number,jobId?:number,contactId?:number,appliedDate?:string,applicationStatus?:JobApplicationStatus,attachments?:JobApplicationAttachment[]}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateJobApplication'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new JobApplication(); };
}
export class DeleteJobApplication {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteJobApplication'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateJobApplicationEvent {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'CreateJobApplicationEvent'; };
    getMethod() { return 'POST'; };
    createResponse() { return new JobApplicationEvent(); };
}
export class UpdateJobApplicationEvent {
    /** @param {{id?:number,status?:JobApplicationStatus,description?:string,eventDate?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {?JobApplicationStatus} */
    status;
    /** @type {?string} */
    description;
    /** @type {?string} */
    eventDate;
    getTypeName() { return 'UpdateJobApplicationEvent'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new JobApplicationEvent(); };
}
export class DeleteJobApplicationEvent {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'DeleteJobApplicationEvent'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateJobApplicationComment {
    /** @param {{jobApplicationId?:number,comment?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    jobApplicationId;
    /** @type {string} */
    comment;
    getTypeName() { return 'CreateJobApplicationComment'; };
    getMethod() { return 'POST'; };
    createResponse() { return new JobApplicationComment(); };
}
export class UpdateJobApplicationComment {
    /** @param {{id?:number,jobApplicationId?:number,comment?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    /** @type {?string} */
    comment;
    getTypeName() { return 'UpdateJobApplicationComment'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new JobApplicationComment(); };
}
export class DeleteJobApplicationComment {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteJobApplicationComment'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateBooking {
    /** @param {{name?:string,roomType?:RoomType,roomNumber?:number,cost?:number,bookingStartDate?:string,bookingEndDate?:string,notes?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateBooking'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateBooking {
    /** @param {{id?:number,name?:string,roomType?:RoomType,roomNumber?:number,cost?:number,bookingStartDate?:string,bookingEndDate?:string,notes?:string,cancelled?:boolean}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
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
    /** @type {?boolean} */
    cancelled;
    getTypeName() { return 'UpdateBooking'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteBooking {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteBooking'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateFileSystemItem {
    /** @param {{fileAccessType?:FileAccessType,file?:FileSystemFile}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {?FileAccessType} */
    fileAccessType;
    /** @type {FileSystemFile} */
    file;
    getTypeName() { return 'CreateFileSystemItem'; };
    getMethod() { return 'POST'; };
    createResponse() { return new FileSystemItem(); };
}
export class CreatePlayer {
    /** @param {{firstName?:string,lastName?:string,email?:string,phoneNumbers?:Phone[],profileId?:number,savedLevelId?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreatePlayer'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class UpdatePlayer {
    /** @param {{id?:number,firstName?:string,lastName?:string,email?:string,phoneNumbers?:Phone[],profileId?:number,savedLevelId?:string,capital?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdatePlayer'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class DeletePlayer {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeletePlayer'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateProfile {
    /** @param {{role?:PlayerRole,region?:PlayerRegion,username?:string,highScore?:number,gamesPlayed?:number,energy?:number,profileUrl?:string,coverUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateProfile'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateProfile {
    /** @param {{id?:number,role?:PlayerRole,region?:PlayerRegion,username?:string,highScore?:number,gamesPlayed?:number,energy?:number,profileUrl?:string,coverUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateProfile'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteProfile {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteProfile'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateGameItem {
    /** @param {{name?:string,description?:string,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
    /** @type {string} */
    description;
    /** @type {string} */
    imageUrl;
    getTypeName() { return 'CreateGameItem'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateGameItem {
    /** @param {{name?:string,description?:string,imageUrl?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
    /** @type {string} */
    description;
    /** @type {?string} */
    imageUrl;
    getTypeName() { return 'UpdateGameItem'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteGameItem {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
    getTypeName() { return 'DeleteGameItem'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class CreateMqBooking extends AuditBase {
    /** @param {{name?:string,roomType?:RoomType,roomNumber?:number,cost?:number,bookingStartDate?:string,bookingEndDate?:string,notes?:string,createdDate?:string,createdBy?:string,modifiedDate?:string,modifiedBy?:string,deletedDate?:string,deletedBy?:string}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
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
    getTypeName() { return 'CreateMqBooking'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class PatchProduct {
    /** @param {{id?:number,productName?:string,supplierId?:number,categoryId?:number,quantityPerUnit?:string,unitPrice?:number,unitsInStock?:number,unitsOnOrder?:number,reorderLevel?:number,discontinued?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    productName;
    /** @type {number} */
    supplierId;
    /** @type {number} */
    categoryId;
    /** @type {string} */
    quantityPerUnit;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    unitsInStock;
    /** @type {number} */
    unitsOnOrder;
    /** @type {number} */
    reorderLevel;
    /** @type {number} */
    discontinued;
    getTypeName() { return 'PatchProduct'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchRegion {
    /** @param {{id?:number,regionDescription?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    regionDescription;
    getTypeName() { return 'PatchRegion'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchShipper {
    /** @param {{id?:number,companyName?:string,phone?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    phone;
    getTypeName() { return 'PatchShipper'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchSupplier {
    /** @param {{id?:number,companyName?:string,contactName?:string,contactTitle?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,phone?:string,fax?:string,homePage?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    contactName;
    /** @type {string} */
    contactTitle;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    homePage;
    getTypeName() { return 'PatchSupplier'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchTerritory {
    /** @param {{id?:string,territoryDescription?:string,regionId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {string} */
    territoryDescription;
    /** @type {number} */
    regionId;
    getTypeName() { return 'PatchTerritory'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchUserAuthRole {
    /** @param {{id?:number,userAuthId?:number,role?:string,permission?:string,createdDate?:string,modifiedDate?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {number} */
    userAuthId;
    /** @type {string} */
    role;
    /** @type {string} */
    permission;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedDate;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'PatchUserAuthRole'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchValidationRule {
    /** @param {{id?:number,type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'PatchValidationRule'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateAppUser {
    /** @param {{id?:number,displayName?:string,email?:string,profileUrl?:string,department?:string,title?:string,jobArea?:string,location?:string,salary?:number,about?:string,isArchived?:number,archivedDate?:string,lastLoginDate?:string,lastLoginIp?:string,userName?:string,primaryEmail?:string,firstName?:string,lastName?:string,company?:string,country?:string,phoneNumber?:string,birthDate?:string,birthDateRaw?:string,address?:string,address2?:string,city?:string,state?:string,culture?:string,fullName?:string,gender?:string,language?:string,mailAddress?:string,nickname?:string,postalCode?:string,timeZone?:string,salt?:string,passwordHash?:string,digestHa1Hash?:string,roles?:string,permissions?:string,createdDate?:string,modifiedDate?:string,invalidLoginAttempts?:number,lastLoginAttempt?:string,lockedDate?:string,recoveryToken?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    displayName;
    /** @type {string} */
    email;
    /** @type {string} */
    profileUrl;
    /** @type {string} */
    department;
    /** @type {string} */
    title;
    /** @type {string} */
    jobArea;
    /** @type {string} */
    location;
    /** @type {number} */
    salary;
    /** @type {string} */
    about;
    /** @type {number} */
    isArchived;
    /** @type {string} */
    archivedDate;
    /** @type {string} */
    lastLoginDate;
    /** @type {string} */
    lastLoginIp;
    /** @type {string} */
    userName;
    /** @type {string} */
    primaryEmail;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    company;
    /** @type {string} */
    country;
    /** @type {string} */
    phoneNumber;
    /** @type {string} */
    birthDate;
    /** @type {string} */
    birthDateRaw;
    /** @type {string} */
    address;
    /** @type {string} */
    address2;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    culture;
    /** @type {string} */
    fullName;
    /** @type {string} */
    gender;
    /** @type {string} */
    language;
    /** @type {string} */
    mailAddress;
    /** @type {string} */
    nickname;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    timeZone;
    /** @type {string} */
    salt;
    /** @type {string} */
    passwordHash;
    /** @type {string} */
    digestHa1Hash;
    /** @type {string} */
    roles;
    /** @type {string} */
    permissions;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedDate;
    /** @type {number} */
    invalidLoginAttempts;
    /** @type {string} */
    lastLoginAttempt;
    /** @type {string} */
    lockedDate;
    /** @type {string} */
    recoveryToken;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'UpdateAppUser'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateCategory {
    /** @param {{id?:number,categoryName?:string,description?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    categoryName;
    /** @type {string} */
    description;
    getTypeName() { return 'UpdateCategory'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateCustomer {
    /** @param {{id?:string,companyName?:string,contactName?:string,contactTitle?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,phone?:string,fax?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    contactName;
    /** @type {string} */
    contactTitle;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    getTypeName() { return 'UpdateCustomer'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateEmployee {
    /** @param {{id?:number,lastName?:string,firstName?:string,title?:string,titleOfCourtesy?:string,birthDate?:string,hireDate?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,homePhone?:string,extension?:string,photo?:string,notes?:string,reportsTo?:number,photoPath?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    lastName;
    /** @type {string} */
    firstName;
    /** @type {string} */
    title;
    /** @type {string} */
    titleOfCourtesy;
    /** @type {string} */
    birthDate;
    /** @type {string} */
    hireDate;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    homePhone;
    /** @type {string} */
    extension;
    /** @type {string} */
    photo;
    /** @type {string} */
    notes;
    /** @type {?number} */
    reportsTo;
    /** @type {string} */
    photoPath;
    getTypeName() { return 'UpdateEmployee'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateEmployeeTerritory {
    /** @param {{id?:string,employeeId?:number,territoryId?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {number} */
    employeeId;
    /** @type {string} */
    territoryId;
    getTypeName() { return 'UpdateEmployeeTerritory'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateFileSystemFile {
    /** @param {{id?:number,fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateFileSystemFile'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateFileSystemItem {
    /** @param {{id?:number,fileAccessType?:string,appUserId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    fileAccessType;
    /** @type {number} */
    appUserId;
    getTypeName() { return 'UpdateFileSystemItem'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateMigration {
    /** @param {{id?:number,name?:string,description?:string,createdDate?:string,completedDate?:string,connectionString?:string,namedConnection?:string,log?:string,errorCode?:string,errorMessage?:string,errorStackTrace?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateMigration'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateOrder {
    /** @param {{id?:number,customerId?:string,employeeId?:number,orderDate?:string,requiredDate?:string,shippedDate?:string,shipVia?:number,freight?:number,shipName?:string,shipAddress?:string,shipCity?:string,shipRegion?:string,shipPostalCode?:string,shipCountry?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    customerId;
    /** @type {number} */
    employeeId;
    /** @type {string} */
    orderDate;
    /** @type {string} */
    requiredDate;
    /** @type {string} */
    shippedDate;
    /** @type {?number} */
    shipVia;
    /** @type {number} */
    freight;
    /** @type {string} */
    shipName;
    /** @type {string} */
    shipAddress;
    /** @type {string} */
    shipCity;
    /** @type {string} */
    shipRegion;
    /** @type {string} */
    shipPostalCode;
    /** @type {string} */
    shipCountry;
    getTypeName() { return 'UpdateOrder'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateOrderDetail {
    /** @param {{id?:string,orderId?:number,productId?:number,unitPrice?:number,quantity?:number,discount?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {number} */
    orderId;
    /** @type {number} */
    productId;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    quantity;
    /** @type {number} */
    discount;
    getTypeName() { return 'UpdateOrderDetail'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateProduct {
    /** @param {{id?:number,productName?:string,supplierId?:number,categoryId?:number,quantityPerUnit?:string,unitPrice?:number,unitsInStock?:number,unitsOnOrder?:number,reorderLevel?:number,discontinued?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    productName;
    /** @type {number} */
    supplierId;
    /** @type {number} */
    categoryId;
    /** @type {string} */
    quantityPerUnit;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    unitsInStock;
    /** @type {number} */
    unitsOnOrder;
    /** @type {number} */
    reorderLevel;
    /** @type {number} */
    discontinued;
    getTypeName() { return 'UpdateProduct'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateRegion {
    /** @param {{id?:number,regionDescription?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    regionDescription;
    getTypeName() { return 'UpdateRegion'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateShipper {
    /** @param {{id?:number,companyName?:string,phone?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    phone;
    getTypeName() { return 'UpdateShipper'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateSupplier {
    /** @param {{id?:number,companyName?:string,contactName?:string,contactTitle?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,phone?:string,fax?:string,homePage?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    contactName;
    /** @type {string} */
    contactTitle;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    homePage;
    getTypeName() { return 'UpdateSupplier'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateTerritory {
    /** @param {{id?:string,territoryDescription?:string,regionId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {string} */
    territoryDescription;
    /** @type {number} */
    regionId;
    getTypeName() { return 'UpdateTerritory'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateUserAuthRole {
    /** @param {{id?:number,userAuthId?:number,role?:string,permission?:string,createdDate?:string,modifiedDate?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {number} */
    userAuthId;
    /** @type {string} */
    role;
    /** @type {string} */
    permission;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedDate;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'UpdateUserAuthRole'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateValidationRule {
    /** @param {{id?:number,type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateValidationRule'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class CreateAlbums {
    /** @param {{title?:string,artistId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    title;
    /** @type {number} */
    artistId;
    getTypeName() { return 'CreateAlbums'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateArtists {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
    getTypeName() { return 'CreateArtists'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateChinookCustomer {
    /** @param {{firstName?:string,lastName?:string,company?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string,supportRepId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateChinookCustomer'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateChinookEmployee {
    /** @param {{lastName?:string,firstName?:string,title?:string,reportsTo?:number,birthDate?:string,hireDate?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateChinookEmployee'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateGenres {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
    getTypeName() { return 'CreateGenres'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateInvoiceItems {
    /** @param {{invoiceId?:number,trackId?:number,unitPrice?:number,quantity?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    invoiceId;
    /** @type {number} */
    trackId;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    quantity;
    getTypeName() { return 'CreateInvoiceItems'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateInvoices {
    /** @param {{customerId?:number,invoiceDate?:string,billingAddress?:string,billingCity?:string,billingState?:string,billingCountry?:string,billingPostalCode?:string,total?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateInvoices'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateMediaTypes {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
    getTypeName() { return 'CreateMediaTypes'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreatePlaylists {
    /** @param {{name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    name;
    getTypeName() { return 'CreatePlaylists'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateTracks {
    /** @param {{name?:string,albumId?:number,mediaTypeId?:number,genreId?:number,composer?:string,milliseconds?:number,bytes?:number,unitPrice?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateTracks'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteAlbums {
    /** @param {{albumId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    albumId;
    getTypeName() { return 'DeleteAlbums'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteArtists {
    /** @param {{artistId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    artistId;
    getTypeName() { return 'DeleteArtists'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteChinookCustomer {
    /** @param {{customerId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    customerId;
    getTypeName() { return 'DeleteChinookCustomer'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteChinookEmployee {
    /** @param {{employeeId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    employeeId;
    getTypeName() { return 'DeleteChinookEmployee'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteGenres {
    /** @param {{genreId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    genreId;
    getTypeName() { return 'DeleteGenres'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteInvoiceItems {
    /** @param {{invoiceLineId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    invoiceLineId;
    getTypeName() { return 'DeleteInvoiceItems'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteInvoices {
    /** @param {{invoiceId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    invoiceId;
    getTypeName() { return 'DeleteInvoices'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteMediaTypes {
    /** @param {{mediaTypeId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    mediaTypeId;
    getTypeName() { return 'DeleteMediaTypes'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeletePlaylists {
    /** @param {{playlistId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    playlistId;
    getTypeName() { return 'DeletePlaylists'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteTracks {
    /** @param {{trackId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    trackId;
    getTypeName() { return 'DeleteTracks'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class PatchAlbums {
    /** @param {{albumId?:number,title?:string,artistId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    albumId;
    /** @type {string} */
    title;
    /** @type {number} */
    artistId;
    getTypeName() { return 'PatchAlbums'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchArtists {
    /** @param {{artistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    artistId;
    /** @type {string} */
    name;
    getTypeName() { return 'PatchArtists'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchChinookCustomer {
    /** @param {{customerId?:number,firstName?:string,lastName?:string,company?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string,supportRepId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'PatchChinookCustomer'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchChinookEmployee {
    /** @param {{employeeId?:number,lastName?:string,firstName?:string,title?:string,reportsTo?:number,birthDate?:string,hireDate?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'PatchChinookEmployee'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchGenres {
    /** @param {{genreId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    genreId;
    /** @type {string} */
    name;
    getTypeName() { return 'PatchGenres'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchInvoiceItems {
    /** @param {{invoiceLineId?:number,invoiceId?:number,trackId?:number,unitPrice?:number,quantity?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'PatchInvoiceItems'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchInvoices {
    /** @param {{invoiceId?:number,customerId?:number,invoiceDate?:string,billingAddress?:string,billingCity?:string,billingState?:string,billingCountry?:string,billingPostalCode?:string,total?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'PatchInvoices'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchMediaTypes {
    /** @param {{mediaTypeId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    mediaTypeId;
    /** @type {string} */
    name;
    getTypeName() { return 'PatchMediaTypes'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchPlaylists {
    /** @param {{playlistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    playlistId;
    /** @type {string} */
    name;
    getTypeName() { return 'PatchPlaylists'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchTracks {
    /** @param {{trackId?:number,name?:string,albumId?:number,mediaTypeId?:number,genreId?:number,composer?:string,milliseconds?:number,bytes?:number,unitPrice?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'PatchTracks'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateAlbums {
    /** @param {{albumId?:number,title?:string,artistId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    albumId;
    /** @type {string} */
    title;
    /** @type {number} */
    artistId;
    getTypeName() { return 'UpdateAlbums'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateArtists {
    /** @param {{artistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    artistId;
    /** @type {string} */
    name;
    getTypeName() { return 'UpdateArtists'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateChinookCustomer {
    /** @param {{customerId?:number,firstName?:string,lastName?:string,company?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string,supportRepId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateChinookCustomer'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateChinookEmployee {
    /** @param {{employeeId?:number,lastName?:string,firstName?:string,title?:string,reportsTo?:number,birthDate?:string,hireDate?:string,address?:string,city?:string,state?:string,country?:string,postalCode?:string,phone?:string,fax?:string,email?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateChinookEmployee'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateGenres {
    /** @param {{genreId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    genreId;
    /** @type {string} */
    name;
    getTypeName() { return 'UpdateGenres'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateInvoiceItems {
    /** @param {{invoiceLineId?:number,invoiceId?:number,trackId?:number,unitPrice?:number,quantity?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateInvoiceItems'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateInvoices {
    /** @param {{invoiceId?:number,customerId?:number,invoiceDate?:string,billingAddress?:string,billingCity?:string,billingState?:string,billingCountry?:string,billingPostalCode?:string,total?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateInvoices'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateMediaTypes {
    /** @param {{mediaTypeId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    mediaTypeId;
    /** @type {string} */
    name;
    getTypeName() { return 'UpdateMediaTypes'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdatePlaylists {
    /** @param {{playlistId?:number,name?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    playlistId;
    /** @type {string} */
    name;
    getTypeName() { return 'UpdatePlaylists'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class UpdateTracks {
    /** @param {{trackId?:number,name?:string,albumId?:number,mediaTypeId?:number,genreId?:number,composer?:string,milliseconds?:number,bytes?:number,unitPrice?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'UpdateTracks'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new IdResponse(); };
}
export class QueryAppUsers extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryAppUsers'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryCategories extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryCategories'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryCustomers extends QueryDb {
    /** @param {{id?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {string} */
    id;
    getTypeName() { return 'QueryCustomers'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryEmployees extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryEmployees'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryEmployeeTerritories extends QueryDb {
    /** @param {{id?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {string} */
    id;
    getTypeName() { return 'QueryEmployeeTerritories'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryMigrations extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryMigrations'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryOrderDetails extends QueryDb {
    /** @param {{id?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {string} */
    id;
    getTypeName() { return 'QueryOrderDetails'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryOrders extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryOrders'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryProducts extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryProducts'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryRegions extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryRegions'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryShippers extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryShippers'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QuerySuppliers extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QuerySuppliers'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryTerritories extends QueryDb {
    /** @param {{id?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {string} */
    id;
    getTypeName() { return 'QueryTerritories'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryUserAuthRoles extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryUserAuthRoles'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryValidationRules extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryValidationRules'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryAlbums extends QueryDb {
    /** @param {{albumId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    albumId;
    getTypeName() { return 'QueryAlbums'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryArtists extends QueryDb {
    /** @param {{artistId?:number,artistIdBetween?:number[],nameStartsWith?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    artistId;
    /** @type {number[]} */
    artistIdBetween;
    /** @type {string} */
    nameStartsWith;
    getTypeName() { return 'QueryArtists'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryChinookCustomers extends QueryDb {
    /** @param {{customerId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    customerId;
    getTypeName() { return 'QueryChinookCustomers'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryChinookEmployees extends QueryDb {
    /** @param {{employeeId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    employeeId;
    getTypeName() { return 'QueryChinookEmployees'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryGenres extends QueryDb {
    /** @param {{genreId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    genreId;
    getTypeName() { return 'QueryGenres'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryInvoiceItems extends QueryDb {
    /** @param {{invoiceLineId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    invoiceLineId;
    getTypeName() { return 'QueryInvoiceItems'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryInvoices extends QueryDb {
    /** @param {{invoiceId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    invoiceId;
    getTypeName() { return 'QueryInvoices'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryMediaTypes extends QueryDb {
    /** @param {{mediaTypeId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    mediaTypeId;
    getTypeName() { return 'QueryMediaTypes'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryPlaylists extends QueryDb {
    /** @param {{playlistId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    playlistId;
    getTypeName() { return 'QueryPlaylists'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryTracks extends QueryDb {
    /** @param {{trackId?:number,nameContains?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    trackId;
    /** @type {string} */
    nameContains;
    getTypeName() { return 'QueryTracks'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryJobApplicationAttachment extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryJobApplicationAttachment'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryContacts extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryContacts'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryJob extends QueryDb {
    /** @param {{id?:number,ids?:number[],skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    /** @type {?number[]} */
    ids;
    getTypeName() { return 'QueryJob'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryJobApplication extends QueryDb {
    /** @param {{id?:number,ids?:number[],jobId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    /** @type {?number[]} */
    ids;
    /** @type {?number} */
    jobId;
    getTypeName() { return 'QueryJobApplication'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryPhoneScreen extends QueryDb {
    /** @param {{id?:number,jobApplicationId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    getTypeName() { return 'QueryPhoneScreen'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryInterview extends QueryDb {
    /** @param {{id?:number,jobApplicationId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    getTypeName() { return 'QueryInterview'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryJobOffer extends QueryDb {
    /** @param {{id?:number,jobApplicationId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    /** @type {?number} */
    jobApplicationId;
    getTypeName() { return 'QueryJobOffer'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryJobAppEvents extends QueryDb {
    /** @param {{jobApplicationId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    jobApplicationId;
    getTypeName() { return 'QueryJobAppEvents'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryAppUser extends QueryDb {
    /** @param {{emailContains?:string,firstNameContains?:string,lastNameContains?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?string} */
    emailContains;
    /** @type {?string} */
    firstNameContains;
    /** @type {?string} */
    lastNameContains;
    getTypeName() { return 'QueryAppUser'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryJobApplicationComments extends QueryDb {
    /** @param {{jobApplicationId?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    jobApplicationId;
    getTypeName() { return 'QueryJobApplicationComments'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryBookings extends QueryDb {
    /** @param {{id?:number,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    getTypeName() { return 'QueryBookings'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryFileSystemItems extends QueryDb {
    /** @param {{appUserId?:number,fileAccessType?:FileAccessType,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    appUserId;
    /** @type {?FileAccessType} */
    fileAccessType;
    getTypeName() { return 'QueryFileSystemItems'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryFileSystemFiles extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QueryFileSystemFiles'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryPlayer extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QueryPlayer'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryProfile extends QueryDb {
    /** @param {{skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    getTypeName() { return 'QueryProfile'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryGameItem extends QueryDb {
    /** @param {{name?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {string} */
    name;
    getTypeName() { return 'QueryGameItem'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryPlayerGameItem extends QueryDb {
    /** @param {{id?:number,playerId?:number,gameItemName?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?number} */
    id;
    /** @type {?number} */
    playerId;
    /** @type {?string} */
    gameItemName;
    getTypeName() { return 'QueryPlayerGameItem'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class QueryLevel extends QueryDb {
    /** @param {{id?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init); }
    /** @type {?string} */
    id;
    getTypeName() { return 'QueryLevel'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class CreateAppUser {
    /** @param {{displayName?:string,email?:string,profileUrl?:string,department?:string,title?:string,jobArea?:string,location?:string,salary?:number,about?:string,isArchived?:number,archivedDate?:string,lastLoginDate?:string,lastLoginIp?:string,userName?:string,primaryEmail?:string,firstName?:string,lastName?:string,company?:string,country?:string,phoneNumber?:string,birthDate?:string,birthDateRaw?:string,address?:string,address2?:string,city?:string,state?:string,culture?:string,fullName?:string,gender?:string,language?:string,mailAddress?:string,nickname?:string,postalCode?:string,timeZone?:string,salt?:string,passwordHash?:string,digestHa1Hash?:string,roles?:string,permissions?:string,createdDate?:string,modifiedDate?:string,invalidLoginAttempts?:number,lastLoginAttempt?:string,lockedDate?:string,recoveryToken?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    displayName;
    /** @type {string} */
    email;
    /** @type {string} */
    profileUrl;
    /** @type {string} */
    department;
    /** @type {string} */
    title;
    /** @type {string} */
    jobArea;
    /** @type {string} */
    location;
    /** @type {number} */
    salary;
    /** @type {string} */
    about;
    /** @type {number} */
    isArchived;
    /** @type {string} */
    archivedDate;
    /** @type {string} */
    lastLoginDate;
    /** @type {string} */
    lastLoginIp;
    /** @type {string} */
    userName;
    /** @type {string} */
    primaryEmail;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    company;
    /** @type {string} */
    country;
    /** @type {string} */
    phoneNumber;
    /** @type {string} */
    birthDate;
    /** @type {string} */
    birthDateRaw;
    /** @type {string} */
    address;
    /** @type {string} */
    address2;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    culture;
    /** @type {string} */
    fullName;
    /** @type {string} */
    gender;
    /** @type {string} */
    language;
    /** @type {string} */
    mailAddress;
    /** @type {string} */
    nickname;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    timeZone;
    /** @type {string} */
    salt;
    /** @type {string} */
    passwordHash;
    /** @type {string} */
    digestHa1Hash;
    /** @type {string} */
    roles;
    /** @type {string} */
    permissions;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedDate;
    /** @type {number} */
    invalidLoginAttempts;
    /** @type {string} */
    lastLoginAttempt;
    /** @type {string} */
    lockedDate;
    /** @type {string} */
    recoveryToken;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'CreateAppUser'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateCategory {
    /** @param {{id?:number,categoryName?:string,description?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    categoryName;
    /** @type {string} */
    description;
    getTypeName() { return 'CreateCategory'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateCustomer {
    /** @param {{id?:string,companyName?:string,contactName?:string,contactTitle?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,phone?:string,fax?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    contactName;
    /** @type {string} */
    contactTitle;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    getTypeName() { return 'CreateCustomer'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateEmployee {
    /** @param {{id?:number,lastName?:string,firstName?:string,title?:string,titleOfCourtesy?:string,birthDate?:string,hireDate?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,homePhone?:string,extension?:string,photo?:string,notes?:string,reportsTo?:number,photoPath?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    lastName;
    /** @type {string} */
    firstName;
    /** @type {string} */
    title;
    /** @type {string} */
    titleOfCourtesy;
    /** @type {string} */
    birthDate;
    /** @type {string} */
    hireDate;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    homePhone;
    /** @type {string} */
    extension;
    /** @type {string} */
    photo;
    /** @type {string} */
    notes;
    /** @type {?number} */
    reportsTo;
    /** @type {string} */
    photoPath;
    getTypeName() { return 'CreateEmployee'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateEmployeeTerritory {
    /** @param {{id?:string,employeeId?:number,territoryId?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {number} */
    employeeId;
    /** @type {string} */
    territoryId;
    getTypeName() { return 'CreateEmployeeTerritory'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateFileSystemFile {
    /** @param {{fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateFileSystemFile'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateMigration {
    /** @param {{name?:string,description?:string,createdDate?:string,completedDate?:string,connectionString?:string,namedConnection?:string,log?:string,errorCode?:string,errorMessage?:string,errorStackTrace?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateMigration'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateOrder {
    /** @param {{id?:number,customerId?:string,employeeId?:number,orderDate?:string,requiredDate?:string,shippedDate?:string,shipVia?:number,freight?:number,shipName?:string,shipAddress?:string,shipCity?:string,shipRegion?:string,shipPostalCode?:string,shipCountry?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    customerId;
    /** @type {number} */
    employeeId;
    /** @type {string} */
    orderDate;
    /** @type {string} */
    requiredDate;
    /** @type {string} */
    shippedDate;
    /** @type {?number} */
    shipVia;
    /** @type {number} */
    freight;
    /** @type {string} */
    shipName;
    /** @type {string} */
    shipAddress;
    /** @type {string} */
    shipCity;
    /** @type {string} */
    shipRegion;
    /** @type {string} */
    shipPostalCode;
    /** @type {string} */
    shipCountry;
    getTypeName() { return 'CreateOrder'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateOrderDetail {
    /** @param {{id?:string,orderId?:number,productId?:number,unitPrice?:number,quantity?:number,discount?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {number} */
    orderId;
    /** @type {number} */
    productId;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    quantity;
    /** @type {number} */
    discount;
    getTypeName() { return 'CreateOrderDetail'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateProduct {
    /** @param {{id?:number,productName?:string,supplierId?:number,categoryId?:number,quantityPerUnit?:string,unitPrice?:number,unitsInStock?:number,unitsOnOrder?:number,reorderLevel?:number,discontinued?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    productName;
    /** @type {number} */
    supplierId;
    /** @type {number} */
    categoryId;
    /** @type {string} */
    quantityPerUnit;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    unitsInStock;
    /** @type {number} */
    unitsOnOrder;
    /** @type {number} */
    reorderLevel;
    /** @type {number} */
    discontinued;
    getTypeName() { return 'CreateProduct'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateRegion {
    /** @param {{id?:number,regionDescription?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    regionDescription;
    getTypeName() { return 'CreateRegion'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateShipper {
    /** @param {{id?:number,companyName?:string,phone?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    phone;
    getTypeName() { return 'CreateShipper'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateSupplier {
    /** @param {{id?:number,companyName?:string,contactName?:string,contactTitle?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,phone?:string,fax?:string,homePage?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    contactName;
    /** @type {string} */
    contactTitle;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    /** @type {string} */
    homePage;
    getTypeName() { return 'CreateSupplier'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateTerritory {
    /** @param {{id?:string,territoryDescription?:string,regionId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {string} */
    territoryDescription;
    /** @type {number} */
    regionId;
    getTypeName() { return 'CreateTerritory'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateUserAuthRole {
    /** @param {{userAuthId?:number,role?:string,permission?:string,createdDate?:string,modifiedDate?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    userAuthId;
    /** @type {string} */
    role;
    /** @type {string} */
    permission;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedDate;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'CreateUserAuthRole'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class CreateValidationRule {
    /** @param {{type?:string,field?:string,createdBy?:string,createdDate?:string,modifiedBy?:string,modifiedDate?:string,suspendedBy?:string,suspendedDate?:string,notes?:string,validator?:string,condition?:string,errorCode?:string,message?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'CreateValidationRule'; };
    getMethod() { return 'POST'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteAppUser {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteAppUser'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteCategory {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteCategory'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteCustomer {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    getTypeName() { return 'DeleteCustomer'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteEmployee {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteEmployee'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteEmployeeTerritory {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    getTypeName() { return 'DeleteEmployeeTerritory'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteFileSystemFile {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteFileSystemFile'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteFileSystemItem {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteFileSystemItem'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteMigration {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteMigration'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteOrder {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteOrder'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteOrderDetail {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    getTypeName() { return 'DeleteOrderDetail'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteProduct {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteProduct'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteRegion {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteRegion'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteShipper {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteShipper'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteSupplier {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteSupplier'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteTerritory {
    /** @param {{id?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    getTypeName() { return 'DeleteTerritory'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteUserAuthRole {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteUserAuthRole'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class DeleteValidationRule {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    getTypeName() { return 'DeleteValidationRule'; };
    getMethod() { return 'DELETE'; };
    createResponse() { return new IdResponse(); };
}
export class PatchAppUser {
    /** @param {{id?:number,displayName?:string,email?:string,profileUrl?:string,department?:string,title?:string,jobArea?:string,location?:string,salary?:number,about?:string,isArchived?:number,archivedDate?:string,lastLoginDate?:string,lastLoginIp?:string,userName?:string,primaryEmail?:string,firstName?:string,lastName?:string,company?:string,country?:string,phoneNumber?:string,birthDate?:string,birthDateRaw?:string,address?:string,address2?:string,city?:string,state?:string,culture?:string,fullName?:string,gender?:string,language?:string,mailAddress?:string,nickname?:string,postalCode?:string,timeZone?:string,salt?:string,passwordHash?:string,digestHa1Hash?:string,roles?:string,permissions?:string,createdDate?:string,modifiedDate?:string,invalidLoginAttempts?:number,lastLoginAttempt?:string,lockedDate?:string,recoveryToken?:string,refId?:number,refIdStr?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    displayName;
    /** @type {string} */
    email;
    /** @type {string} */
    profileUrl;
    /** @type {string} */
    department;
    /** @type {string} */
    title;
    /** @type {string} */
    jobArea;
    /** @type {string} */
    location;
    /** @type {number} */
    salary;
    /** @type {string} */
    about;
    /** @type {number} */
    isArchived;
    /** @type {string} */
    archivedDate;
    /** @type {string} */
    lastLoginDate;
    /** @type {string} */
    lastLoginIp;
    /** @type {string} */
    userName;
    /** @type {string} */
    primaryEmail;
    /** @type {string} */
    firstName;
    /** @type {string} */
    lastName;
    /** @type {string} */
    company;
    /** @type {string} */
    country;
    /** @type {string} */
    phoneNumber;
    /** @type {string} */
    birthDate;
    /** @type {string} */
    birthDateRaw;
    /** @type {string} */
    address;
    /** @type {string} */
    address2;
    /** @type {string} */
    city;
    /** @type {string} */
    state;
    /** @type {string} */
    culture;
    /** @type {string} */
    fullName;
    /** @type {string} */
    gender;
    /** @type {string} */
    language;
    /** @type {string} */
    mailAddress;
    /** @type {string} */
    nickname;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    timeZone;
    /** @type {string} */
    salt;
    /** @type {string} */
    passwordHash;
    /** @type {string} */
    digestHa1Hash;
    /** @type {string} */
    roles;
    /** @type {string} */
    permissions;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    modifiedDate;
    /** @type {number} */
    invalidLoginAttempts;
    /** @type {string} */
    lastLoginAttempt;
    /** @type {string} */
    lockedDate;
    /** @type {string} */
    recoveryToken;
    /** @type {?number} */
    refId;
    /** @type {string} */
    refIdStr;
    /** @type {string} */
    meta;
    getTypeName() { return 'PatchAppUser'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchCategory {
    /** @param {{id?:number,categoryName?:string,description?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    categoryName;
    /** @type {string} */
    description;
    getTypeName() { return 'PatchCategory'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchCustomer {
    /** @param {{id?:string,companyName?:string,contactName?:string,contactTitle?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,phone?:string,fax?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {string} */
    companyName;
    /** @type {string} */
    contactName;
    /** @type {string} */
    contactTitle;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    phone;
    /** @type {string} */
    fax;
    getTypeName() { return 'PatchCustomer'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchEmployee {
    /** @param {{id?:number,lastName?:string,firstName?:string,title?:string,titleOfCourtesy?:string,birthDate?:string,hireDate?:string,address?:string,city?:string,region?:string,postalCode?:string,country?:string,homePhone?:string,extension?:string,photo?:string,notes?:string,reportsTo?:number,photoPath?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    lastName;
    /** @type {string} */
    firstName;
    /** @type {string} */
    title;
    /** @type {string} */
    titleOfCourtesy;
    /** @type {string} */
    birthDate;
    /** @type {string} */
    hireDate;
    /** @type {string} */
    address;
    /** @type {string} */
    city;
    /** @type {string} */
    region;
    /** @type {string} */
    postalCode;
    /** @type {string} */
    country;
    /** @type {string} */
    homePhone;
    /** @type {string} */
    extension;
    /** @type {string} */
    photo;
    /** @type {string} */
    notes;
    /** @type {?number} */
    reportsTo;
    /** @type {string} */
    photoPath;
    getTypeName() { return 'PatchEmployee'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchEmployeeTerritory {
    /** @param {{id?:string,employeeId?:number,territoryId?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {number} */
    employeeId;
    /** @type {string} */
    territoryId;
    getTypeName() { return 'PatchEmployeeTerritory'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchFileSystemFile {
    /** @param {{id?:number,fileName?:string,filePath?:string,contentType?:string,contentLength?:number,fileSystemItemId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'PatchFileSystemFile'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchFileSystemItem {
    /** @param {{id?:number,fileAccessType?:string,appUserId?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    fileAccessType;
    /** @type {number} */
    appUserId;
    getTypeName() { return 'PatchFileSystemItem'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchMigration {
    /** @param {{id?:number,name?:string,description?:string,createdDate?:string,completedDate?:string,connectionString?:string,namedConnection?:string,log?:string,errorCode?:string,errorMessage?:string,errorStackTrace?:string,meta?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
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
    getTypeName() { return 'PatchMigration'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchOrder {
    /** @param {{id?:number,customerId?:string,employeeId?:number,orderDate?:string,requiredDate?:string,shippedDate?:string,shipVia?:number,freight?:number,shipName?:string,shipAddress?:string,shipCity?:string,shipRegion?:string,shipPostalCode?:string,shipCountry?:string}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {number} */
    id;
    /** @type {string} */
    customerId;
    /** @type {number} */
    employeeId;
    /** @type {string} */
    orderDate;
    /** @type {string} */
    requiredDate;
    /** @type {string} */
    shippedDate;
    /** @type {?number} */
    shipVia;
    /** @type {number} */
    freight;
    /** @type {string} */
    shipName;
    /** @type {string} */
    shipAddress;
    /** @type {string} */
    shipCity;
    /** @type {string} */
    shipRegion;
    /** @type {string} */
    shipPostalCode;
    /** @type {string} */
    shipCountry;
    getTypeName() { return 'PatchOrder'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}
export class PatchOrderDetail {
    /** @param {{id?:string,orderId?:number,productId?:number,unitPrice?:number,quantity?:number,discount?:number}} [init] */
    constructor(init) { Object.assign(this, init); }
    /** @type {string} */
    id;
    /** @type {number} */
    orderId;
    /** @type {number} */
    productId;
    /** @type {number} */
    unitPrice;
    /** @type {number} */
    quantity;
    /** @type {number} */
    discount;
    getTypeName() { return 'PatchOrderDetail'; };
    getMethod() { return 'PATCH'; };
    createResponse() { return new IdResponse(); };
}

