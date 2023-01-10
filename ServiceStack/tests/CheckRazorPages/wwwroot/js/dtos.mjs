/* Options:
Date: 2023-01-10 13:41:27
Version: 6.51
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: https://localhost:5001

//AddServiceStackTypes: True
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: 
*/

"use strict";
export var Title;
(function (Title) {
    Title["Unspecified"] = "Unspecified";
    Title["Mr"] = "Mr";
    Title["Mrs"] = "Mrs";
    Title["Miss"] = "Miss";
})(Title || (Title = {}));
export var FilmGenres;
(function (FilmGenres) {
    FilmGenres["Action"] = "Action";
    FilmGenres["Adventure"] = "Adventure";
    FilmGenres["Comedy"] = "Comedy";
    FilmGenres["Drama"] = "Drama";
})(FilmGenres || (FilmGenres = {}));
export class QueryBase {
    constructor(init) { Object.assign(this, init); }
    skip;
    take;
    orderBy;
    orderByDesc;
    include;
    fields;
    meta;
}
export class QueryData extends QueryBase {
    constructor(init) { super(init); Object.assign(this, init); }
}
export class ResponseError {
    constructor(init) { Object.assign(this, init); }
    errorCode;
    fieldName;
    message;
    meta;
}
export class ResponseStatus {
    constructor(init) { Object.assign(this, init); }
    errorCode;
    message;
    stackTrace;
    errors;
    meta;
}
export class Contact {
    constructor(init) { Object.assign(this, init); }
    id;
    userAuthId;
    title;
    name;
    color;
    filmGenres;
    age;
}
export class GetContactsResponse {
    constructor(init) { Object.assign(this, init); }
    responseStatus;
    results;
}
export class GetContactResponse {
    constructor(init) { Object.assign(this, init); }
    result;
    responseStatus;
}
export class CreateContactResponse {
    constructor(init) { Object.assign(this, init); }
    result;
    responseStatus;
}
export class UpdateContactResponse {
    constructor(init) { Object.assign(this, init); }
    responseStatus;
}
export class HelloResponse {
    constructor(init) { Object.assign(this, init); }
    result;
}
export class Todo {
    constructor(init) { Object.assign(this, init); }
    id;
    text;
    isFinished;
}
export class QueryResponse {
    constructor(init) { Object.assign(this, init); }
    offset;
    total;
    results;
    meta;
    responseStatus;
}
export class AuthenticateResponse {
    constructor(init) { Object.assign(this, init); }
    userId;
    sessionId;
    userName;
    displayName;
    referrerUrl;
    bearerToken;
    refreshToken;
    profileUrl;
    roles;
    permissions;
    responseStatus;
    meta;
}
export class AssignRolesResponse {
    constructor(init) { Object.assign(this, init); }
    allRoles;
    allPermissions;
    meta;
    responseStatus;
}
export class UnAssignRolesResponse {
    constructor(init) { Object.assign(this, init); }
    allRoles;
    allPermissions;
    meta;
    responseStatus;
}
export class RegisterResponse {
    constructor(init) { Object.assign(this, init); }
    userId;
    sessionId;
    userName;
    referrerUrl;
    bearerToken;
    refreshToken;
    roles;
    permissions;
    responseStatus;
    meta;
}
export class GetContacts {
    constructor(init) { Object.assign(this, init); }
    getTypeName() { return 'GetContacts'; };
    getMethod() { return 'POST'; };
    createResponse() { return new GetContactsResponse(); };
}
export class GetContact {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'GetContact'; };
    getMethod() { return 'POST'; };
    createResponse() { return new GetContactResponse(); };
}
export class CreateContact {
    constructor(init) { Object.assign(this, init); }
    title;
    name;
    color;
    filmGenres;
    age;
    agree;
    continue;
    errorView;
    getTypeName() { return 'CreateContact'; };
    getMethod() { return 'ANYHTML'; };
    createResponse() { return new CreateContactResponse(); };
}
export class DeleteContact {
    constructor(init) { Object.assign(this, init); }
    id;
    continue;
    getTypeName() { return 'DeleteContact'; };
    getMethod() { return 'POSTHTML'; };
    createResponse() { };
}
export class UpdateContact {
    constructor(init) { Object.assign(this, init); }
    id;
    title;
    name;
    color;
    filmGenres;
    age;
    continue;
    errorView;
    getTypeName() { return 'UpdateContact'; };
    getMethod() { return 'POST'; };
    createResponse() { return new UpdateContactResponse(); };
}
export class Hello {
    constructor(init) { Object.assign(this, init); }
    name;
    getTypeName() { return 'Hello'; };
    getMethod() { return 'POST'; };
    createResponse() { return new HelloResponse(); };
}
export class QueryTodos extends QueryData {
    constructor(init) { super(init); Object.assign(this, init); }
    id;
    ids;
    textContains;
    getTypeName() { return 'QueryTodos'; };
    getMethod() { return 'GET'; };
    createResponse() { return new QueryResponse(); };
}
export class CreateTodo {
    constructor(init) { Object.assign(this, init); }
    text;
    getTypeName() { return 'CreateTodo'; };
    getMethod() { return 'POST'; };
    createResponse() { return new Todo(); };
}
export class UpdateTodo {
    constructor(init) { Object.assign(this, init); }
    id;
    text;
    isFinished;
    getTypeName() { return 'UpdateTodo'; };
    getMethod() { return 'PUT'; };
    createResponse() { return new Todo(); };
}
export class DeleteTodo {
    constructor(init) { Object.assign(this, init); }
    id;
    getTypeName() { return 'DeleteTodo'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class DeleteTodos {
    constructor(init) { Object.assign(this, init); }
    ids;
    getTypeName() { return 'DeleteTodos'; };
    getMethod() { return 'DELETE'; };
    createResponse() { };
}
export class Authenticate {
    constructor(init) { Object.assign(this, init); }
    provider;
    state;
    oauth_token;
    oauth_verifier;
    userName;
    password;
    rememberMe;
    errorView;
    nonce;
    uri;
    response;
    qop;
    nc;
    cnonce;
    accessToken;
    accessTokenSecret;
    scope;
    meta;
    getTypeName() { return 'Authenticate'; };
    getMethod() { return 'POST'; };
    createResponse() { return new AuthenticateResponse(); };
}
export class AssignRoles {
    constructor(init) { Object.assign(this, init); }
    userName;
    permissions;
    roles;
    meta;
    getTypeName() { return 'AssignRoles'; };
    getMethod() { return 'POST'; };
    createResponse() { return new AssignRolesResponse(); };
}
export class UnAssignRoles {
    constructor(init) { Object.assign(this, init); }
    userName;
    permissions;
    roles;
    meta;
    getTypeName() { return 'UnAssignRoles'; };
    getMethod() { return 'POST'; };
    createResponse() { return new UnAssignRolesResponse(); };
}
export class Register {
    constructor(init) { Object.assign(this, init); }
    userName;
    firstName;
    lastName;
    displayName;
    email;
    password;
    confirmPassword;
    autoLogin;
    errorView;
    meta;
    getTypeName() { return 'Register'; };
    getMethod() { return 'POST'; };
    createResponse() { return new RegisterResponse(); };
}

