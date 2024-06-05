/* Options:
Date: 2024-06-06 02:20:36
Version: 8.23
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: https://localhost:5001

//AddServiceStackTypes: True
//AddDocAnnotations: True
//AddDescriptionAsComments: True
IncludeTypes: QueryUserApiKeys.*,CreateUserApiKey.*,UpdateUserApiKey.*,DeleteUserApiKey.*
//ExcludeTypes: 
//DefaultImports: 
*/

"use strict";
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
export class UserApiKeysResponse {
    /** @param {{results?:PartialApiKey[],responseStatus?:ResponseStatus}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {PartialApiKey[]} */
    results;
    /** @type {ResponseStatus} */
    responseStatus;
}
export class UserApiKeyResponse {
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
export class QueryUserApiKeys {
    /** @param {{id?:number,search?:string,orderBy?:string,skip?:number,take?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    id;
    /** @type {string} */
    search;
    /** @type {string} */
    orderBy;
    /** @type {?number} */
    skip;
    /** @type {?number} */
    take;
    getTypeName() { return 'QueryUserApiKeys' }
    getMethod() { return 'GET' }
    createResponse() { return new UserApiKeysResponse() }
}
export class CreateUserApiKey {
    /** @param {{name?:string,scopes?:string[],features?:string[],restrictTo?:string[],expiryDate?:string,notes?:string,refId?:number,refIdStr?:string,meta?:{ [index: string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
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
    getTypeName() { return 'CreateUserApiKey' }
    getMethod() { return 'POST' }
    createResponse() { return new UserApiKeyResponse() }
}
export class UpdateUserApiKey {
    /** @param {{id?:number,name?:string,scopes?:string[],features?:string[],restrictTo?:string[],expiryDate?:string,cancelledDate?:string,notes?:string,refId?:number,refIdStr?:string,meta?:{ [index: string]: string; },reset?:string[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    name;
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
    getTypeName() { return 'UpdateUserApiKey' }
    getMethod() { return 'PATCH' }
    createResponse() { return new EmptyResponse() }
}
export class DeleteUserApiKey {
    /** @param {{id?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?number} */
    id;
    getTypeName() { return 'DeleteUserApiKey' }
    getMethod() { return 'DELETE' }
    createResponse() { return new EmptyResponse() }
}

