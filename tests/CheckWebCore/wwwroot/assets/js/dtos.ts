/* Options:
Date: 2019-05-21 19:18:52
Version: 5.51
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: https://localhost:5001

//GlobalNamespace: 
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: 
*/


export interface IReturn<T>
{
    createResponse(): T;
}

export interface IReturnVoid
{
    createResponse(): void;
}

export interface IHasSessionId
{
    sessionId: string;
}

export interface IHasBearerToken
{
    bearerToken: string;
}

export interface IPost
{
}

// @DataContract
export class ResponseError
{
    // @DataMember(Order=1, EmitDefaultValue=false)
    public errorCode: string;

    // @DataMember(Order=2, EmitDefaultValue=false)
    public fieldName: string;

    // @DataMember(Order=3, EmitDefaultValue=false)
    public message: string;

    // @DataMember(Order=4, EmitDefaultValue=false)
    public meta: { [index:string]: string; };

    public constructor(init?:Partial<ResponseError>) { (Object as any).assign(this, init); }
}

// @DataContract
export class ResponseStatus
{
    // @DataMember(Order=1)
    public errorCode: string;

    // @DataMember(Order=2)
    public message: string;

    // @DataMember(Order=3)
    public stackTrace: string;

    // @DataMember(Order=4)
    public errors: ResponseError[];

    // @DataMember(Order=5)
    public meta: { [index:string]: string; };

    public constructor(init?:Partial<ResponseStatus>) { (Object as any).assign(this, init); }
}

export interface IAuthTokens
{
    provider: string;
    userId: string;
    accessToken: string;
    accessTokenSecret: string;
    refreshToken: string;
    refreshTokenExpiry?: string;
    requestToken: string;
    requestTokenSecret: string;
    items: { [index:string]: string; };
}

export enum Title
{
    Unspecified = 'Unspecified',
    Mr = 'Mr',
    Mrs = 'Mrs',
    Miss = 'Miss',
}

export class Contact
{
    public id: number;
    public userAuthId: number;
    public title: Title;
    public name: string;
    public color: string;
    public filmGenres: FilmGenres[];
    public age: number;

    public constructor(init?:Partial<Contact>) { (Object as any).assign(this, init); }
}

export enum FilmGenres
{
    Action = 'Action',
    Adventure = 'Adventure',
    Comedy = 'Comedy',
    Drama = 'Drama',
}

export class HelloResponse
{
    public result: string;
    public responseStatus: ResponseStatus;

    public constructor(init?:Partial<HelloResponse>) { (Object as any).assign(this, init); }
}

// @Route("/testauth")
export class TestAuth implements IReturn<TestAuth>
{

    public constructor(init?:Partial<TestAuth>) { (Object as any).assign(this, init); }
    public createResponse() { return new TestAuth(); }
    public getTypeName() { return 'TestAuth'; }
}

// @DataContract
export class AuthUserSession
{
    // @DataMember(Order=1)
    public referrerUrl: string;

    // @DataMember(Order=2)
    public id: string;

    // @DataMember(Order=3)
    public userAuthId: string;

    // @DataMember(Order=4)
    public userAuthName: string;

    // @DataMember(Order=5)
    public userName: string;

    // @DataMember(Order=6)
    public twitterUserId: string;

    // @DataMember(Order=7)
    public twitterScreenName: string;

    // @DataMember(Order=8)
    public facebookUserId: string;

    // @DataMember(Order=9)
    public facebookUserName: string;

    // @DataMember(Order=10)
    public firstName: string;

    // @DataMember(Order=11)
    public lastName: string;

    // @DataMember(Order=12)
    public displayName: string;

    // @DataMember(Order=13)
    public company: string;

    // @DataMember(Order=14)
    public email: string;

    // @DataMember(Order=15)
    public primaryEmail: string;

    // @DataMember(Order=16)
    public phoneNumber: string;

    // @DataMember(Order=17)
    public birthDate: string;

    // @DataMember(Order=18)
    public birthDateRaw: string;

    // @DataMember(Order=19)
    public address: string;

    // @DataMember(Order=20)
    public address2: string;

    // @DataMember(Order=21)
    public city: string;

    // @DataMember(Order=22)
    public state: string;

    // @DataMember(Order=23)
    public country: string;

    // @DataMember(Order=24)
    public culture: string;

    // @DataMember(Order=25)
    public fullName: string;

    // @DataMember(Order=26)
    public gender: string;

    // @DataMember(Order=27)
    public language: string;

    // @DataMember(Order=28)
    public mailAddress: string;

    // @DataMember(Order=29)
    public nickname: string;

    // @DataMember(Order=30)
    public postalCode: string;

    // @DataMember(Order=31)
    public timeZone: string;

    // @DataMember(Order=32)
    public requestTokenSecret: string;

    // @DataMember(Order=33)
    public createdAt: string;

    // @DataMember(Order=34)
    public lastModified: string;

    // @DataMember(Order=35)
    public roles: string[];

    // @DataMember(Order=36)
    public permissions: string[];

    // @DataMember(Order=37)
    public isAuthenticated: boolean;

    // @DataMember(Order=38)
    public fromToken: boolean;

    // @DataMember(Order=39)
    public profileUrl: string;

    // @DataMember(Order=40)
    public sequence: string;

    // @DataMember(Order=41)
    public tag: number;

    // @DataMember(Order=42)
    public authProvider: string;

    // @DataMember(Order=43)
    public providerOAuthAccess: IAuthTokens[];

    // @DataMember(Order=44)
    public meta: { [index:string]: string; };

    // @DataMember(Order=45)
    public audiences: string[];

    // @DataMember(Order=46)
    public scopes: string[];

    // @DataMember(Order=47)
    public dns: string;

    // @DataMember(Order=48)
    public rsa: string;

    // @DataMember(Order=49)
    public sid: string;

    // @DataMember(Order=50)
    public hash: string;

    // @DataMember(Order=51)
    public homePhone: string;

    // @DataMember(Order=52)
    public mobilePhone: string;

    // @DataMember(Order=53)
    public webpage: string;

    // @DataMember(Order=54)
    public emailConfirmed: boolean;

    // @DataMember(Order=55)
    public phoneNumberConfirmed: boolean;

    // @DataMember(Order=56)
    public twoFactorEnabled: boolean;

    // @DataMember(Order=57)
    public securityStamp: string;

    // @DataMember(Order=58)
    public type: string;

    public constructor(init?:Partial<AuthUserSession>) { (Object as any).assign(this, init); }
}

export class ImportDataResponse
{
    public responseStatus: ResponseStatus;

    public constructor(init?:Partial<ImportDataResponse>) { (Object as any).assign(this, init); }
}

export class GetContactsResponse
{
    public results: Contact[];
    public responseStatus: ResponseStatus;

    public constructor(init?:Partial<GetContactsResponse>) { (Object as any).assign(this, init); }
}

export class GetContactResponse
{
    public result: Contact;
    public responseStatus: ResponseStatus;

    public constructor(init?:Partial<GetContactResponse>) { (Object as any).assign(this, init); }
}

export class CreateContactResponse
{
    public result: Contact;
    public responseStatus: ResponseStatus;

    public constructor(init?:Partial<CreateContactResponse>) { (Object as any).assign(this, init); }
}

export class UpdateContactResponse
{
    public responseStatus: ResponseStatus;

    public constructor(init?:Partial<UpdateContactResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AuthenticateResponse implements IHasSessionId, IHasBearerToken
{
    // @DataMember(Order=1)
    public userId: string;

    // @DataMember(Order=2)
    public sessionId: string;

    // @DataMember(Order=3)
    public userName: string;

    // @DataMember(Order=4)
    public displayName: string;

    // @DataMember(Order=5)
    public referrerUrl: string;

    // @DataMember(Order=6)
    public bearerToken: string;

    // @DataMember(Order=7)
    public refreshToken: string;

    // @DataMember(Order=8)
    public profileUrl: string;

    // @DataMember(Order=9)
    public roles: string[];

    // @DataMember(Order=10)
    public permissions: string[];

    // @DataMember(Order=11)
    public responseStatus: ResponseStatus;

    // @DataMember(Order=12)
    public meta: { [index:string]: string; };

    public constructor(init?:Partial<AuthenticateResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AssignRolesResponse
{
    // @DataMember(Order=1)
    public allRoles: string[];

    // @DataMember(Order=2)
    public allPermissions: string[];

    // @DataMember(Order=3)
    public meta: { [index:string]: string; };

    // @DataMember(Order=4)
    public responseStatus: ResponseStatus;

    public constructor(init?:Partial<AssignRolesResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class UnAssignRolesResponse
{
    // @DataMember(Order=1)
    public allRoles: string[];

    // @DataMember(Order=2)
    public allPermissions: string[];

    // @DataMember(Order=3)
    public meta: { [index:string]: string; };

    // @DataMember(Order=4)
    public responseStatus: ResponseStatus;

    public constructor(init?:Partial<UnAssignRolesResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class ConvertSessionToTokenResponse
{
    // @DataMember(Order=1)
    public meta: { [index:string]: string; };

    // @DataMember(Order=2)
    public accessToken: string;

    // @DataMember(Order=3)
    public refreshToken: string;

    // @DataMember(Order=4)
    public responseStatus: ResponseStatus;

    public constructor(init?:Partial<ConvertSessionToTokenResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class GetAccessTokenResponse
{
    // @DataMember(Order=1)
    public accessToken: string;

    // @DataMember(Order=2)
    public meta: { [index:string]: string; };

    // @DataMember(Order=3)
    public responseStatus: ResponseStatus;

    public constructor(init?:Partial<GetAccessTokenResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class RegisterResponse
{
    // @DataMember(Order=1)
    public userId: string;

    // @DataMember(Order=2)
    public sessionId: string;

    // @DataMember(Order=3)
    public userName: string;

    // @DataMember(Order=4)
    public referrerUrl: string;

    // @DataMember(Order=5)
    public bearerToken: string;

    // @DataMember(Order=6)
    public refreshToken: string;

    // @DataMember(Order=7)
    public responseStatus: ResponseStatus;

    // @DataMember(Order=8)
    public meta: { [index:string]: string; };

    public constructor(init?:Partial<RegisterResponse>) { (Object as any).assign(this, init); }
}

// @Route("/hello")
// @Route("/hello/{Name}")
export class Hello implements IReturn<HelloResponse>
{
    public name: string;

    public constructor(init?:Partial<Hello>) { (Object as any).assign(this, init); }
    public createResponse() { return new HelloResponse(); }
    public getTypeName() { return 'Hello'; }
}

// @Route("/session")
export class Session implements IReturn<AuthUserSession>
{

    public constructor(init?:Partial<Session>) { (Object as any).assign(this, init); }
    public createResponse() { return new AuthUserSession(); }
    public getTypeName() { return 'Session'; }
}

// @Route("/throw")
export class Throw
{

    public constructor(init?:Partial<Throw>) { (Object as any).assign(this, init); }
}

// @Route("/api/data/import/{Month}", "POST")
export class ImportData implements IReturn<ImportDataResponse>
{
    public month: string;

    public constructor(init?:Partial<ImportData>) { (Object as any).assign(this, init); }
    public createResponse() { return new ImportDataResponse(); }
    public getTypeName() { return 'ImportData'; }
}

// @Route("/contacts", "GET")
export class GetContacts implements IReturn<GetContactsResponse>
{

    public constructor(init?:Partial<GetContacts>) { (Object as any).assign(this, init); }
    public createResponse() { return new GetContactsResponse(); }
    public getTypeName() { return 'GetContacts'; }
}

// @Route("/contacts/{Id}", "GET")
export class GetContact implements IReturn<GetContactResponse>
{
    public id: number;

    public constructor(init?:Partial<GetContact>) { (Object as any).assign(this, init); }
    public createResponse() { return new GetContactResponse(); }
    public getTypeName() { return 'GetContact'; }
}

// @Route("/contacts", "POST")
export class CreateContact implements IReturn<CreateContactResponse>
{
    public title: Title;
    public name: string;
    public color: string;
    public filmGenres: FilmGenres[];
    public age: number;
    public agree: boolean;
    public continue: string;
    public errorView: string;

    public constructor(init?:Partial<CreateContact>) { (Object as any).assign(this, init); }
    public createResponse() { return new CreateContactResponse(); }
    public getTypeName() { return 'CreateContact'; }
}

// @Route("/contacts/{Id}", "DELETE")
// @Route("/contacts/{Id}/delete", "POST")
export class DeleteContact implements IReturnVoid
{
    public id: number;
    public continue: string;

    public constructor(init?:Partial<DeleteContact>) { (Object as any).assign(this, init); }
    public createResponse() {}
    public getTypeName() { return 'DeleteContact'; }
}

// @Route("/contacts/{Id}", "POST PUT")
export class UpdateContact implements IReturn<UpdateContactResponse>
{
    public id: number;
    public title: Title;
    public name: string;
    public color: string;
    public filmGenres: FilmGenres[];
    public age: number;
    public continue: string;
    public errorView: string;

    public constructor(init?:Partial<UpdateContact>) { (Object as any).assign(this, init); }
    public createResponse() { return new UpdateContactResponse(); }
    public getTypeName() { return 'UpdateContact'; }
}

// @Route("/auth")
// @Route("/auth/{provider}")
// @Route("/authenticate")
// @Route("/authenticate/{provider}")
// @DataContract
export class Authenticate implements IReturn<AuthenticateResponse>, IPost
{
    // @DataMember(Order=1)
    public provider: string;

    // @DataMember(Order=2)
    public state: string;

    // @DataMember(Order=3)
    public oauth_token: string;

    // @DataMember(Order=4)
    public oauth_verifier: string;

    // @DataMember(Order=5)
    public userName: string;

    // @DataMember(Order=6)
    public password: string;

    // @DataMember(Order=7)
    public rememberMe: boolean;

    // @DataMember(Order=8)
    public continue: string;

    // @DataMember(Order=9)
    public errorView: string;

    // @DataMember(Order=10)
    public nonce: string;

    // @DataMember(Order=11)
    public uri: string;

    // @DataMember(Order=12)
    public response: string;

    // @DataMember(Order=13)
    public qop: string;

    // @DataMember(Order=14)
    public nc: string;

    // @DataMember(Order=15)
    public cnonce: string;

    // @DataMember(Order=16)
    public useTokenCookie: boolean;

    // @DataMember(Order=17)
    public accessToken: string;

    // @DataMember(Order=18)
    public accessTokenSecret: string;

    // @DataMember(Order=19)
    public meta: { [index:string]: string; };

    public constructor(init?:Partial<Authenticate>) { (Object as any).assign(this, init); }
    public createResponse() { return new AuthenticateResponse(); }
    public getTypeName() { return 'Authenticate'; }
}

// @Route("/assignroles")
// @DataContract
export class AssignRoles implements IReturn<AssignRolesResponse>, IPost
{
    // @DataMember(Order=1)
    public userName: string;

    // @DataMember(Order=2)
    public permissions: string[];

    // @DataMember(Order=3)
    public roles: string[];

    // @DataMember(Order=4)
    public meta: { [index:string]: string; };

    public constructor(init?:Partial<AssignRoles>) { (Object as any).assign(this, init); }
    public createResponse() { return new AssignRolesResponse(); }
    public getTypeName() { return 'AssignRoles'; }
}

// @Route("/unassignroles")
// @DataContract
export class UnAssignRoles implements IReturn<UnAssignRolesResponse>, IPost
{
    // @DataMember(Order=1)
    public userName: string;

    // @DataMember(Order=2)
    public permissions: string[];

    // @DataMember(Order=3)
    public roles: string[];

    // @DataMember(Order=4)
    public meta: { [index:string]: string; };

    public constructor(init?:Partial<UnAssignRoles>) { (Object as any).assign(this, init); }
    public createResponse() { return new UnAssignRolesResponse(); }
    public getTypeName() { return 'UnAssignRoles'; }
}

// @Route("/session-to-token")
// @DataContract
export class ConvertSessionToToken implements IReturn<ConvertSessionToTokenResponse>, IPost
{
    // @DataMember(Order=1)
    public preserveSession: boolean;

    // @DataMember(Order=2)
    public meta: { [index:string]: string; };

    public constructor(init?:Partial<ConvertSessionToToken>) { (Object as any).assign(this, init); }
    public createResponse() { return new ConvertSessionToTokenResponse(); }
    public getTypeName() { return 'ConvertSessionToToken'; }
}

// @Route("/access-token")
// @DataContract
export class GetAccessToken implements IReturn<GetAccessTokenResponse>, IPost
{
    // @DataMember(Order=1)
    public refreshToken: string;

    // @DataMember(Order=2)
    public meta: { [index:string]: string; };

    public constructor(init?:Partial<GetAccessToken>) { (Object as any).assign(this, init); }
    public createResponse() { return new GetAccessTokenResponse(); }
    public getTypeName() { return 'GetAccessToken'; }
}

// @Route("/register")
// @DataContract
export class Register implements IReturn<RegisterResponse>, IPost
{
    // @DataMember(Order=1)
    public userName: string;

    // @DataMember(Order=2)
    public firstName: string;

    // @DataMember(Order=3)
    public lastName: string;

    // @DataMember(Order=4)
    public displayName: string;

    // @DataMember(Order=5)
    public email: string;

    // @DataMember(Order=6)
    public password: string;

    // @DataMember(Order=7)
    public confirmPassword: string;

    // @DataMember(Order=8)
    public autoLogin: boolean;

    // @DataMember(Order=9)
    public continue: string;

    // @DataMember(Order=10)
    public errorView: string;

    // @DataMember(Order=11)
    public meta: { [index:string]: string; };

    public constructor(init?:Partial<Register>) { (Object as any).assign(this, init); }
    public createResponse() { return new RegisterResponse(); }
    public getTypeName() { return 'Register'; }
}

