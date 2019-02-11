/* Options:
Date: 2019-02-11 10:08:04
Version: 5.41
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:55799

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

export interface IGet
{
}

export interface IPost
{
}

export interface IPut
{
}

export interface IDelete
{
}

export interface IPatch
{
}

export interface IHasVersion
{
    version: number;
}

export class QueryBase
{
    public constructor(init?:Partial<QueryBase>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public skip: number;

    // @DataMember(Order=2)
    public take: number;

    // @DataMember(Order=3)
    public orderBy: string;

    // @DataMember(Order=4)
    public orderByDesc: string;

    // @DataMember(Order=5)
    public include: string;

    // @DataMember(Order=6)
    public fields: string;

    // @DataMember(Order=7)
    public meta: { [index:string]: string; };
}

export class QueryData<T> extends QueryBase
{
    public constructor(init?:Partial<QueryData<T>>) { super(init); Object.assign(this, init); }
}

export class RequestLogEntry
{
    public constructor(init?:Partial<RequestLogEntry>) { Object.assign(this, init); }
    public id: number;
    public dateTime: string;
    public statusCode: number;
    public statusDescription: string;
    public httpMethod: string;
    public absoluteUri: string;
    public pathInfo: string;
    // @StringLength(2147483647)
    public requestBody: string;

    public requestDto: Object;
    public userAuthId: string;
    public sessionId: string;
    public ipAddress: string;
    public forwardedFor: string;
    public referer: string;
    public headers: { [index:string]: string; };
    public formData: { [index:string]: string; };
    public items: { [index:string]: string; };
    public session: Object;
    public responseDto: Object;
    public errorResponse: Object;
    public exceptionSource: string;
    public exceptionData: any;
    public requestDuration: string;
    public meta: { [index:string]: string; };
}

// @DataContract
export class ResponseError
{
    public constructor(init?:Partial<ResponseError>) { Object.assign(this, init); }
    // @DataMember(Order=1, EmitDefaultValue=false)
    public errorCode: string;

    // @DataMember(Order=2, EmitDefaultValue=false)
    public fieldName: string;

    // @DataMember(Order=3, EmitDefaultValue=false)
    public message: string;

    // @DataMember(Order=4, EmitDefaultValue=false)
    public meta: { [index:string]: string; };
}

// @DataContract
export class ResponseStatus
{
    public constructor(init?:Partial<ResponseStatus>) { Object.assign(this, init); }
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
}

export class QueryDb_1<T> extends QueryBase
{
    public constructor(init?:Partial<QueryDb_1<T>>) { super(init); Object.assign(this, init); }
}

export class Rockstar
{
    public constructor(init?:Partial<Rockstar>) { Object.assign(this, init); }
    /**
     * Идентификатор
     */
    public id: number;
    /**
     * Фамилия
     */
    public firstName: string;
    /**
     * Имя
     */
    public lastName: string;
    /**
     * Возраст
     */
    public age: number;
}

export class ArrayElementInDictionary
{
    public constructor(init?:Partial<ArrayElementInDictionary>) { Object.assign(this, init); }
    public id: number;
}

export class ObjectDesign
{
    public constructor(init?:Partial<ObjectDesign>) { Object.assign(this, init); }
    public id: number;
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

// @DataContract
export class AuthUserSession
{
    public constructor(init?:Partial<AuthUserSession>) { Object.assign(this, init); }
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
}

export class MetadataTestNestedChild
{
    public constructor(init?:Partial<MetadataTestNestedChild>) { Object.assign(this, init); }
    public name: string;
}

export class MetadataTestChild
{
    public constructor(init?:Partial<MetadataTestChild>) { Object.assign(this, init); }
    public name: string;
    public results: MetadataTestNestedChild[];
}

export class MenuItemExampleItem
{
    public constructor(init?:Partial<MenuItemExampleItem>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    // @ApiMember()
    public name1: string;
}

export class MenuItemExample
{
    public constructor(init?:Partial<MenuItemExample>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    // @ApiMember()
    public name1: string;

    public menuItemExampleItem: MenuItemExampleItem;
}

// @DataContract
export class MenuExample
{
    public constructor(init?:Partial<MenuExample>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    // @ApiMember()
    public menuItemExample1: MenuItemExample;
}

export class MetadataTypeName
{
    public constructor(init?:Partial<MetadataTypeName>) { Object.assign(this, init); }
    public name: string;
    public namespace: string;
    public genericArgs: string[];
}

export class MetadataRoute
{
    public constructor(init?:Partial<MetadataRoute>) { Object.assign(this, init); }
    public path: string;
    public verbs: string;
    public notes: string;
    public summary: string;
}

export class MetadataDataContract
{
    public constructor(init?:Partial<MetadataDataContract>) { Object.assign(this, init); }
    public name: string;
    public namespace: string;
}

export class MetadataDataMember
{
    public constructor(init?:Partial<MetadataDataMember>) { Object.assign(this, init); }
    public name: string;
    public order: number;
    public isRequired: boolean;
    public emitDefaultValue: boolean;
}

export class MetadataAttribute
{
    public constructor(init?:Partial<MetadataAttribute>) { Object.assign(this, init); }
    public name: string;
    public constructorArgs: MetadataPropertyType[];
    public args: MetadataPropertyType[];
}

export class MetadataPropertyType
{
    public constructor(init?:Partial<MetadataPropertyType>) { Object.assign(this, init); }
    public name: string;
    public type: string;
    public isValueType: boolean;
    public isSystemType: boolean;
    public isEnum: boolean;
    public typeNamespace: string;
    public genericArgs: string[];
    public value: string;
    public description: string;
    public dataMember: MetadataDataMember;
    public readOnly: boolean;
    public paramType: string;
    public displayType: string;
    public isRequired: boolean;
    public allowableValues: string[];
    public allowableMin: number;
    public allowableMax: number;
    public attributes: MetadataAttribute[];
}

export class MetadataType
{
    public constructor(init?:Partial<MetadataType>) { Object.assign(this, init); }
    public name: string;
    public namespace: string;
    public genericArgs: string[];
    public inherits: MetadataTypeName;
    public implements: MetadataTypeName[];
    public displayType: string;
    public description: string;
    public returnVoidMarker: boolean;
    public isNested: boolean;
    public isEnum: boolean;
    public isEnumInt: boolean;
    public isInterface: boolean;
    public isAbstract: boolean;
    public returnMarkerTypeName: MetadataTypeName;
    public routes: MetadataRoute[];
    public dataContract: MetadataDataContract;
    public properties: MetadataPropertyType[];
    public attributes: MetadataAttribute[];
    public innerTypes: MetadataTypeName[];
    public enumNames: string[];
    public enumValues: string[];
    public meta: { [index:string]: string; };
}

export class AutoQueryConvention
{
    public constructor(init?:Partial<AutoQueryConvention>) { Object.assign(this, init); }
    public name: string;
    public value: string;
    public types: string;
}

export class AutoQueryViewerConfig
{
    public constructor(init?:Partial<AutoQueryViewerConfig>) { Object.assign(this, init); }
    public serviceBaseUrl: string;
    public serviceName: string;
    public serviceDescription: string;
    public serviceIconUrl: string;
    public formats: string[];
    public maxLimit: number;
    public isPublic: boolean;
    public onlyShowAnnotatedServices: boolean;
    public implicitConventions: AutoQueryConvention[];
    public defaultSearchField: string;
    public defaultSearchType: string;
    public defaultSearchText: string;
    public brandUrl: string;
    public brandImageUrl: string;
    public textColor: string;
    public linkColor: string;
    public backgroundColor: string;
    public backgroundImageUrl: string;
    public iconUrl: string;
    public meta: { [index:string]: string; };
}

export class AutoQueryViewerUserInfo
{
    public constructor(init?:Partial<AutoQueryViewerUserInfo>) { Object.assign(this, init); }
    public isAuthenticated: boolean;
    public queryCount: number;
    public meta: { [index:string]: string; };
}

export class AutoQueryOperation
{
    public constructor(init?:Partial<AutoQueryOperation>) { Object.assign(this, init); }
    public request: string;
    public from: string;
    public to: string;
    public meta: { [index:string]: string; };
}

export class RecursiveNode implements IReturn<RecursiveNode>
{
    public constructor(init?:Partial<RecursiveNode>) { Object.assign(this, init); }
    public id: number;
    public text: string;
    public children: RecursiveNode[];
    public createResponse() { return new RecursiveNode(); }
    public getTypeName() { return 'RecursiveNode'; }
}

export class NativeTypesTestService
{
    public constructor(init?:Partial<NativeTypesTestService>) { Object.assign(this, init); }
}

export class NestedClass
{
    public constructor(init?:Partial<NestedClass>) { Object.assign(this, init); }
    public value: string;
}

export class ListResult
{
    public constructor(init?:Partial<ListResult>) { Object.assign(this, init); }
    public result: string;
}

export class OnlyInReturnListArg
{
    public constructor(init?:Partial<OnlyInReturnListArg>) { Object.assign(this, init); }
    public result: string;
}

export class ArrayResult
{
    public constructor(init?:Partial<ArrayResult>) { Object.assign(this, init); }
    public result: string;
}

export enum EnumType
{
    Value1 = 'Value1',
    Value2 = 'Value2',
}

export enum EnumWithValues
{
    Value1 = '1',
    Value2 = '2',
}

// @Flags()
export enum EnumFlags
{
    Value0 = 0,
    Value1 = 1,
    Value2 = 2,
    Value3 = 3,
    Value123 = 3,
}

export enum EnumStyle
{
    lower = 'lower',
    UPPER = 'UPPER',
    PascalCase = 'PascalCase',
    camelCase = 'camelCase',
    camelUPPER = 'camelUPPER',
    PascalUPPER = 'PascalUPPER',
}

export class Poco
{
    public constructor(init?:Partial<Poco>) { Object.assign(this, init); }
    public name: string;
}

export class AllCollectionTypes
{
    public constructor(init?:Partial<AllCollectionTypes>) { Object.assign(this, init); }
    public intArray: number[];
    public intList: number[];
    public stringArray: string[];
    public stringList: string[];
    public pocoArray: Poco[];
    public pocoList: Poco[];
    public nullableByteArray: Uint8Array;
    public nullableByteList: number[];
    public nullableDateTimeArray: string[];
    public nullableDateTimeList: string[];
    public pocoLookup: { [index:string]: Poco[]; };
    public pocoLookupMap: { [index:string]: { [index:string]: Poco; }[]; };
}

export class KeyValuePair<TKey, TValue>
{
    public constructor(init?:Partial<KeyValuePair<TKey, TValue>>) { Object.assign(this, init); }
    public key: TKey;
    public value: TValue;
}

export class SubType
{
    public constructor(init?:Partial<SubType>) { Object.assign(this, init); }
    public id: number;
    public name: string;
}

export class HelloBase
{
    public constructor(init?:Partial<HelloBase>) { Object.assign(this, init); }
    public id: number;
}

export class HelloResponseBase
{
    public constructor(init?:Partial<HelloResponseBase>) { Object.assign(this, init); }
    public refId: number;
}

export class HelloBase_1<T>
{
    public constructor(init?:Partial<HelloBase_1<T>>) { Object.assign(this, init); }
    public items: T[];
    public counts: number[];
}

export class Item
{
    public constructor(init?:Partial<Item>) { Object.assign(this, init); }
    public value: string;
}

export class InheritedItem
{
    public constructor(init?:Partial<InheritedItem>) { Object.assign(this, init); }
    public name: string;
}

export class HelloWithReturnResponse
{
    public constructor(init?:Partial<HelloWithReturnResponse>) { Object.assign(this, init); }
    public result: string;
}

export class HelloType
{
    public constructor(init?:Partial<HelloType>) { Object.assign(this, init); }
    public result: string;
}

export interface IPoco
{
    name: string;
}

export interface IEmptyInterface
{
}

export class EmptyClass
{
    public constructor(init?:Partial<EmptyClass>) { Object.assign(this, init); }
}

export interface ImplementsPoco
{
    name: string;
}

export class TypeB
{
    public constructor(init?:Partial<TypeB>) { Object.assign(this, init); }
    public foo: string;
}

export class TypeA
{
    public constructor(init?:Partial<TypeA>) { Object.assign(this, init); }
    public bar: TypeB[];
}

export class InnerType
{
    public constructor(init?:Partial<InnerType>) { Object.assign(this, init); }
    public id: number;
    public name: string;
}

export enum InnerEnum
{
    Foo = 'Foo',
    Bar = 'Bar',
    Baz = 'Baz',
}

export class InnerTypeItem
{
    public constructor(init?:Partial<InnerTypeItem>) { Object.assign(this, init); }
    public id: number;
    public name: string;
}

export enum DayOfWeek
{
    Sunday = 'Sunday',
    Monday = 'Monday',
    Tuesday = 'Tuesday',
    Wednesday = 'Wednesday',
    Thursday = 'Thursday',
    Friday = 'Friday',
    Saturday = 'Saturday',
}

// @DataContract
export enum ShortDays
{
    Monday = 'MON',
    Tuesday = 'TUE',
    Wednesday = 'WED',
    Thursday = 'THU',
    Friday = 'FRI',
    Saturday = 'SAT',
    Sunday = 'SUN',
}

// @DataContract
export enum ScopeType
{
    Global = '1',
    Sale = '2',
}

export class Tuple_2<T1, T2>
{
    public constructor(init?:Partial<Tuple_2<T1, T2>>) { Object.assign(this, init); }
    public item1: T1;
    public item2: T2;
}

export class Tuple_3<T1, T2, T3>
{
    public constructor(init?:Partial<Tuple_3<T1, T2, T3>>) { Object.assign(this, init); }
    public item1: T1;
    public item2: T2;
    public item3: T3;
}

export interface IEcho
{
    sentence: string;
}

// @Flags()
export enum CacheControl
{
    None = 0,
    Public = 1,
    Private = 2,
    MustRevalidate = 4,
    NoCache = 8,
    NoStore = 16,
    NoTransform = 32,
    ProxyRevalidate = 64,
}

export enum MyColor
{
    Red = 'Red',
    Green = 'Green',
    Blue = 'Blue',
}

export class SwaggerNestedModel
{
    public constructor(init?:Partial<SwaggerNestedModel>) { Object.assign(this, init); }
    /**
     * NestedProperty description
     */
        // @ApiMember(Description="NestedProperty description")
    public nestedProperty: boolean;
}

export class SwaggerNestedModel2
{
    public constructor(init?:Partial<SwaggerNestedModel2>) { Object.assign(this, init); }
    /**
     * NestedProperty2 description
     */
        // @ApiMember(Description="NestedProperty2 description")
    public nestedProperty2: boolean;

    /**
     * MultipleValues description
     */
        // @ApiMember(Description="MultipleValues description")
    public multipleValues: string;

    /**
     * TestRange description
     */
        // @ApiMember(Description="TestRange description")
    public testRange: number;
}

export enum MyEnum
{
    A = 'A',
    B = 'B',
    C = 'C',
}

// @DataContract
export class UserApiKey
{
    public constructor(init?:Partial<UserApiKey>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public key: string;

    // @DataMember(Order=2)
    public keyType: string;

    // @DataMember(Order=3)
    public expiryDate: string;
}

export class PgRockstar extends Rockstar
{
    public constructor(init?:Partial<PgRockstar>) { super(init); Object.assign(this, init); }
}

export class QueryDb_2<From, Into> extends QueryBase
{
    public constructor(init?:Partial<QueryDb_2<From, Into>>) { super(init); Object.assign(this, init); }
}

export class CustomRockstar
{
    public constructor(init?:Partial<CustomRockstar>) { Object.assign(this, init); }
    // @AutoQueryViewerField(Title="Name")
    public firstName: string;

    // @AutoQueryViewerField(HideInSummary=true)
    public lastName: string;

    public age: number;
    // @AutoQueryViewerField(Title="Album")
    public rockstarAlbumName: string;

    // @AutoQueryViewerField(Title="Genre")
    public rockstarGenreName: string;
}

export interface IFilterRockstars
{
}

export class Movie
{
    public constructor(init?:Partial<Movie>) { Object.assign(this, init); }
    public id: number;
    public imdbId: string;
    public title: string;
    public rating: string;
    public score: number;
    public director: string;
    public releaseDate: string;
    public tagLine: string;
    public genres: string[];
}

export class RockstarAlbum
{
    public constructor(init?:Partial<RockstarAlbum>) { Object.assign(this, init); }
    public id: number;
    public rockstarId: number;
    public name: string;
}

export class RockstarReference
{
    public constructor(init?:Partial<RockstarReference>) { Object.assign(this, init); }
    public id: number;
    public firstName: string;
    public lastName: string;
    public age: number;
    public albums: RockstarAlbum[];
}

export class OnlyDefinedInGenericType
{
    public constructor(init?:Partial<OnlyDefinedInGenericType>) { Object.assign(this, init); }
    public id: number;
    public name: string;
}

export class OnlyDefinedInGenericTypeFrom
{
    public constructor(init?:Partial<OnlyDefinedInGenericTypeFrom>) { Object.assign(this, init); }
    public id: number;
    public name: string;
}

export class OnlyDefinedInGenericTypeInto
{
    public constructor(init?:Partial<OnlyDefinedInGenericTypeInto>) { Object.assign(this, init); }
    public id: number;
    public name: string;
}

export class TypesGroup
{
    public constructor(init?:Partial<TypesGroup>) { Object.assign(this, init); }
}

// @DataContract
export class QueryResponse<T>
{
    public constructor(init?:Partial<QueryResponse<T>>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public offset: number;

    // @DataMember(Order=2)
    public total: number;

    // @DataMember(Order=3)
    public results: T[];

    // @DataMember(Order=4)
    public meta: { [index:string]: string; };

    // @DataMember(Order=5)
    public responseStatus: ResponseStatus;
}

export class ChangeRequestResponse
{
    public constructor(init?:Partial<ChangeRequestResponse>) { Object.assign(this, init); }
    public contentType: string;
    public header: string;
    public queryString: string;
    public form: string;
    public responseStatus: ResponseStatus;
}

export class DiscoverTypes implements IReturn<DiscoverTypes>
{
    public constructor(init?:Partial<DiscoverTypes>) { Object.assign(this, init); }
    public elementInDictionary: { [index:string]: ArrayElementInDictionary[]; };
    public createResponse() { return new DiscoverTypes(); }
    public getTypeName() { return 'DiscoverTypes'; }
}

export class CustomHttpErrorResponse
{
    public constructor(init?:Partial<CustomHttpErrorResponse>) { Object.assign(this, init); }
    public custom: string;
    public responseStatus: ResponseStatus;
}

// @Route("/alwaysthrows")
export class AlwaysThrows implements IReturn<AlwaysThrows>
{
    public constructor(init?:Partial<AlwaysThrows>) { Object.assign(this, init); }
    public createResponse() { return new AlwaysThrows(); }
    public getTypeName() { return 'AlwaysThrows'; }
}

// @Route("/alwaysthrowsfilterattribute")
export class AlwaysThrowsFilterAttribute implements IReturn<AlwaysThrowsFilterAttribute>
{
    public constructor(init?:Partial<AlwaysThrowsFilterAttribute>) { Object.assign(this, init); }
    public createResponse() { return new AlwaysThrowsFilterAttribute(); }
    public getTypeName() { return 'AlwaysThrowsFilterAttribute'; }
}

// @Route("/alwaysthrowsglobalfilter")
export class AlwaysThrowsGlobalFilter implements IReturn<AlwaysThrowsGlobalFilter>
{
    public constructor(init?:Partial<AlwaysThrowsGlobalFilter>) { Object.assign(this, init); }
    public createResponse() { return new AlwaysThrowsGlobalFilter(); }
    public getTypeName() { return 'AlwaysThrowsGlobalFilter'; }
}

export class CustomFieldHttpErrorResponse
{
    public constructor(init?:Partial<CustomFieldHttpErrorResponse>) { Object.assign(this, init); }
    public custom: string;
    public responseStatus: ResponseStatus;
}

export class NoRepeatResponse
{
    public constructor(init?:Partial<NoRepeatResponse>) { Object.assign(this, init); }
    public id: string;
}

export class BatchThrowsResponse
{
    public constructor(init?:Partial<BatchThrowsResponse>) { Object.assign(this, init); }
    public result: string;
    public responseStatus: ResponseStatus;
}

export class ObjectDesignResponse
{
    public constructor(init?:Partial<ObjectDesignResponse>) { Object.assign(this, init); }
    public data: ObjectDesign;
}

export class CreateJwtResponse
{
    public constructor(init?:Partial<CreateJwtResponse>) { Object.assign(this, init); }
    public token: string;
    public responseStatus: ResponseStatus;
}

export class CreateRefreshJwtResponse
{
    public constructor(init?:Partial<CreateRefreshJwtResponse>) { Object.assign(this, init); }
    public token: string;
    public responseStatus: ResponseStatus;
}

export class MetadataTestResponse
{
    public constructor(init?:Partial<MetadataTestResponse>) { Object.assign(this, init); }
    public id: number;
    public results: MetadataTestChild[];
}

// @DataContract
export class GetExampleResponse
{
    public constructor(init?:Partial<GetExampleResponse>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public responseStatus: ResponseStatus;

    // @DataMember(Order=2)
    // @ApiMember()
    public menuExample1: MenuExample;
}

export class AutoQueryMetadataResponse
{
    public constructor(init?:Partial<AutoQueryMetadataResponse>) { Object.assign(this, init); }
    public config: AutoQueryViewerConfig;
    public userInfo: AutoQueryViewerUserInfo;
    public operations: AutoQueryOperation[];
    public types: MetadataType[];
    public responseStatus: ResponseStatus;
    public meta: { [index:string]: string; };
}

export class TestAttributeExport implements IReturn<TestAttributeExport>
{
    public constructor(init?:Partial<TestAttributeExport>) { Object.assign(this, init); }
    // @Display(AutoGenerateField=true, AutoGenerateFilter=true, ShortName="UnitMeasKey")
    public unitMeasKey: number;
    public createResponse() { return new TestAttributeExport(); }
    public getTypeName() { return 'TestAttributeExport'; }
}

// @DataContract
export class HelloACodeGenTestResponse
{
    public constructor(init?:Partial<HelloACodeGenTestResponse>) { Object.assign(this, init); }
    /**
     * Description for FirstResult
     */
        // @DataMember
    public firstResult: number;

    /**
     * Description for SecondResult
     */
        // @DataMember
        // @ApiMember(Description="Description for SecondResult")
    public secondResult: number;
}

export class HelloResponse
{
    public constructor(init?:Partial<HelloResponse>) { Object.assign(this, init); }
    public result: string;
}

/**
 * Description on HelloAllResponse type
 */
// @DataContract
export class HelloAnnotatedResponse
{
    public constructor(init?:Partial<HelloAnnotatedResponse>) { Object.assign(this, init); }
    // @DataMember
    public result: string;
}

export class HelloList implements IReturn<ListResult[]>
{
    public constructor(init?:Partial<HelloList>) { Object.assign(this, init); }
    public names: string[];
    public createResponse() { return new Array<ListResult>(); }
    public getTypeName() { return 'HelloList'; }
}

export class HelloArray implements IReturn<ArrayResult[]>
{
    public constructor(init?:Partial<HelloArray>) { Object.assign(this, init); }
    public names: string[];
    public createResponse() { return new Array<ArrayResult>(); }
    public getTypeName() { return 'HelloArray'; }
}

export class HelloExistingResponse
{
    public constructor(init?:Partial<HelloExistingResponse>) { Object.assign(this, init); }
    public helloList: HelloList;
    public helloArray: HelloArray;
    public arrayResults: ArrayResult[];
    public listResults: ListResult[];
}

export class AllTypes implements IReturn<AllTypes>
{
    public constructor(init?:Partial<AllTypes>) { Object.assign(this, init); }
    public id: number;
    public nullableId: number;
    public byte: number;
    public short: number;
    public int: number;
    public long: number;
    public uShort: number;
    public uInt: number;
    public uLong: number;
    public float: number;
    public double: number;
    public decimal: number;
    public string: string;
    public dateTime: string;
    public timeSpan: string;
    public dateTimeOffset: string;
    public guid: string;
    public char: string;
    public keyValuePair: KeyValuePair<string, string>;
    public nullableDateTime: string;
    public nullableTimeSpan: string;
    public stringList: string[];
    public stringArray: string[];
    public stringMap: { [index:string]: string; };
    public intStringMap: { [index:number]: string; };
    public subType: SubType;
    public point: string;
    // @DataMember(Name="aliasedName")
    public originalName: string;
    public createResponse() { return new AllTypes(); }
    public getTypeName() { return 'AllTypes'; }
}

export class HelloAllTypesResponse
{
    public constructor(init?:Partial<HelloAllTypesResponse>) { Object.assign(this, init); }
    public result: string;
    public allTypes: AllTypes;
    public allCollectionTypes: AllCollectionTypes;
}

// @DataContract
export class HelloWithDataContractResponse
{
    public constructor(init?:Partial<HelloWithDataContractResponse>) { Object.assign(this, init); }
    // @DataMember(Name="result", Order=1, IsRequired=true, EmitDefaultValue=false)
    public result: string;
}

/**
 * Description on HelloWithDescriptionResponse type
 */
export class HelloWithDescriptionResponse
{
    public constructor(init?:Partial<HelloWithDescriptionResponse>) { Object.assign(this, init); }
    public result: string;
}

export class HelloWithInheritanceResponse extends HelloResponseBase
{
    public constructor(init?:Partial<HelloWithInheritanceResponse>) { super(init); Object.assign(this, init); }
    public result: string;
}

export class HelloWithAlternateReturnResponse extends HelloWithReturnResponse
{
    public constructor(init?:Partial<HelloWithAlternateReturnResponse>) { super(init); Object.assign(this, init); }
    public altResult: string;
}

export class HelloWithRouteResponse
{
    public constructor(init?:Partial<HelloWithRouteResponse>) { Object.assign(this, init); }
    public result: string;
}

export class HelloWithTypeResponse
{
    public constructor(init?:Partial<HelloWithTypeResponse>) { Object.assign(this, init); }
    public result: HelloType;
}

export class HelloStruct implements IReturn<HelloStruct>
{
    public constructor(init?:Partial<HelloStruct>) { Object.assign(this, init); }
    public point: string;
    public nullablePoint: string;
    public createResponse() { return new HelloStruct(); }
    public getTypeName() { return 'HelloStruct'; }
}

export class HelloSessionResponse
{
    public constructor(init?:Partial<HelloSessionResponse>) { Object.assign(this, init); }
    public result: AuthUserSession;
}

export class HelloImplementsInterface implements IReturn<HelloImplementsInterface>, ImplementsPoco
{
    public constructor(init?:Partial<HelloImplementsInterface>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new HelloImplementsInterface(); }
    public getTypeName() { return 'HelloImplementsInterface'; }
}

export class Request1Response
{
    public constructor(init?:Partial<Request1Response>) { Object.assign(this, init); }
    public test: TypeA;
}

export class Request2Response
{
    public constructor(init?:Partial<Request2Response>) { Object.assign(this, init); }
    public test: TypeA;
}

export class HelloInnerTypesResponse
{
    public constructor(init?:Partial<HelloInnerTypesResponse>) { Object.assign(this, init); }
    public innerType: InnerType;
    public innerEnum: InnerEnum;
    public innerList: InnerTypeItem[];
}

export class CustomUserSession extends AuthUserSession
{
    public constructor(init?:Partial<CustomUserSession>) { super(init); Object.assign(this, init); }
    // @DataMember
    public customName: string;

    // @DataMember
    public customInfo: string;
}

// @DataContract
export class QueryResponseTemplate<T>
{
    public constructor(init?:Partial<QueryResponseTemplate<T>>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public offset: number;

    // @DataMember(Order=2)
    public total: number;

    // @DataMember(Order=3)
    public results: T[];

    // @DataMember(Order=4)
    public meta: { [index:string]: string; };

    // @DataMember(Order=5)
    public responseStatus: ResponseStatus;
}

export class HelloVerbResponse
{
    public constructor(init?:Partial<HelloVerbResponse>) { Object.assign(this, init); }
    public result: string;
}

export class EnumResponse
{
    public constructor(init?:Partial<EnumResponse>) { Object.assign(this, init); }
    public operator: ScopeType;
}

export class ExcludeTestNested
{
    public constructor(init?:Partial<ExcludeTestNested>) { Object.assign(this, init); }
    public id: number;
}

export class RestrictLocalhost implements IReturn<RestrictLocalhost>
{
    public constructor(init?:Partial<RestrictLocalhost>) { Object.assign(this, init); }
    public id: number;
    public createResponse() { return new RestrictLocalhost(); }
    public getTypeName() { return 'RestrictLocalhost'; }
}

export class RestrictInternal implements IReturn<RestrictInternal>
{
    public constructor(init?:Partial<RestrictInternal>) { Object.assign(this, init); }
    public id: number;
    public createResponse() { return new RestrictInternal(); }
    public getTypeName() { return 'RestrictInternal'; }
}

export class HelloTuple implements IReturn<HelloTuple>
{
    public constructor(init?:Partial<HelloTuple>) { Object.assign(this, init); }
    public tuple2: Tuple_2<string, number>;
    public tuple3: Tuple_3<string, number, boolean>;
    public tuples2: Tuple_2<string,number>[];
    public tuples3: Tuple_3<string,number,boolean>[];
    public createResponse() { return new HelloTuple(); }
    public getTypeName() { return 'HelloTuple'; }
}

export class HelloAuthenticatedResponse
{
    public constructor(init?:Partial<HelloAuthenticatedResponse>) { Object.assign(this, init); }
    public version: number;
    public sessionId: string;
    public userName: string;
    public email: string;
    public isAuthenticated: boolean;
    public responseStatus: ResponseStatus;
}

export class Echo implements IEcho
{
    public constructor(init?:Partial<Echo>) { Object.assign(this, init); }
    public sentence: string;
}

export class ThrowHttpErrorResponse
{
    public constructor(init?:Partial<ThrowHttpErrorResponse>) { Object.assign(this, init); }
}

export class ThrowTypeResponse
{
    public constructor(init?:Partial<ThrowTypeResponse>) { Object.assign(this, init); }
    public responseStatus: ResponseStatus;
}

export class ThrowValidationResponse
{
    public constructor(init?:Partial<ThrowValidationResponse>) { Object.assign(this, init); }
    public age: number;
    public required: string;
    public email: string;
    public responseStatus: ResponseStatus;
}

export class acsprofileResponse
{
    public constructor(init?:Partial<acsprofileResponse>) { Object.assign(this, init); }
    public profileId: string;
}

export class ReturnedDto
{
    public constructor(init?:Partial<ReturnedDto>) { Object.assign(this, init); }
    public id: number;
}

// @Route("/matchroute/html")
export class MatchesHtml implements IReturn<MatchesHtml>
{
    public constructor(init?:Partial<MatchesHtml>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new MatchesHtml(); }
    public getTypeName() { return 'MatchesHtml'; }
}

// @Route("/matchroute/json")
export class MatchesJson implements IReturn<MatchesJson>
{
    public constructor(init?:Partial<MatchesJson>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new MatchesJson(); }
    public getTypeName() { return 'MatchesJson'; }
}

export class TimestampData
{
    public constructor(init?:Partial<TimestampData>) { Object.assign(this, init); }
    public timestamp: number;
}

// @Route("/test/html")
export class TestHtml implements IReturn<TestHtml>
{
    public constructor(init?:Partial<TestHtml>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new TestHtml(); }
    public getTypeName() { return 'TestHtml'; }
}

export class ViewResponse
{
    public constructor(init?:Partial<ViewResponse>) { Object.assign(this, init); }
    public result: string;
}

// @Route("/swagger/model")
export class SwaggerModel implements IReturn<SwaggerModel>
{
    public constructor(init?:Partial<SwaggerModel>) { Object.assign(this, init); }
    public int: number;
    public string: string;
    public dateTime: string;
    public dateTimeOffset: string;
    public timeSpan: string;
    public createResponse() { return new SwaggerModel(); }
    public getTypeName() { return 'SwaggerModel'; }
}

// @Route("/plain-dto")
export class PlainDto implements IReturn<PlainDto>
{
    public constructor(init?:Partial<PlainDto>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new PlainDto(); }
    public getTypeName() { return 'PlainDto'; }
}

// @Route("/httpresult-dto")
export class HttpResultDto implements IReturn<HttpResultDto>
{
    public constructor(init?:Partial<HttpResultDto>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new HttpResultDto(); }
    public getTypeName() { return 'HttpResultDto'; }
}

// @Route("/restrict/mq")
export class TestMqRestriction implements IReturn<TestMqRestriction>
{
    public constructor(init?:Partial<TestMqRestriction>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new TestMqRestriction(); }
    public getTypeName() { return 'TestMqRestriction'; }
}

// @Route("/set-cache")
export class SetCache implements IReturn<SetCache>
{
    public constructor(init?:Partial<SetCache>) { Object.assign(this, init); }
    public eTag: string;
    public age: string;
    public maxAge: string;
    public expires: string;
    public lastModified: string;
    public cacheControl: CacheControl;
    public createResponse() { return new SetCache(); }
    public getTypeName() { return 'SetCache'; }
}

export class SwaggerComplexResponse
{
    public constructor(init?:Partial<SwaggerComplexResponse>) { Object.assign(this, init); }
    // @DataMember
    // @ApiMember()
    public isRequired: boolean;

    // @DataMember
    // @ApiMember(IsRequired=true)
    public arrayString: string[];

    // @DataMember
    // @ApiMember()
    public arrayInt: number[];

    // @DataMember
    // @ApiMember()
    public listString: string[];

    // @DataMember
    // @ApiMember()
    public listInt: number[];

    // @DataMember
    // @ApiMember()
    public dictionaryString: { [index:string]: string; };
}

/**
 * Api GET All
 */
// @Route("/swaggerexamples", "GET")
// @Api(Description="Api GET All")
export class GetSwaggerExamples implements IReturn<GetSwaggerExamples>
{
    public constructor(init?:Partial<GetSwaggerExamples>) { Object.assign(this, init); }
    public get: string;
    public createResponse() { return new GetSwaggerExamples(); }
    public getTypeName() { return 'GetSwaggerExamples'; }
}

/**
 * Api GET Id
 */
// @Route("/swaggerexamples/{Id}", "GET")
// @Api(Description="Api GET Id")
export class GetSwaggerExample implements IReturn<GetSwaggerExample>
{
    public constructor(init?:Partial<GetSwaggerExample>) { Object.assign(this, init); }
    public id: number;
    public get: string;
    public createResponse() { return new GetSwaggerExample(); }
    public getTypeName() { return 'GetSwaggerExample'; }
}

/**
 * Api POST
 */
// @Route("/swaggerexamples", "POST")
// @Api(Description="Api POST")
export class PostSwaggerExamples implements IReturn<PostSwaggerExamples>
{
    public constructor(init?:Partial<PostSwaggerExamples>) { Object.assign(this, init); }
    public post: string;
    public createResponse() { return new PostSwaggerExamples(); }
    public getTypeName() { return 'PostSwaggerExamples'; }
}

/**
 * Api PUT Id
 */
// @Route("/swaggerexamples/{Id}", "PUT")
// @Api(Description="Api PUT Id")
export class PutSwaggerExample implements IReturn<PutSwaggerExample>
{
    public constructor(init?:Partial<PutSwaggerExample>) { Object.assign(this, init); }
    public id: number;
    public get: string;
    public createResponse() { return new PutSwaggerExample(); }
    public getTypeName() { return 'PutSwaggerExample'; }
}

// @Route("/lists", "GET")
export class GetLists implements IReturn<GetLists>
{
    public constructor(init?:Partial<GetLists>) { Object.assign(this, init); }
    public id: string;
    public createResponse() { return new GetLists(); }
    public getTypeName() { return 'GetLists'; }
}

// @DataContract
export class AuthenticateResponse implements IHasSessionId, IHasBearerToken
{
    public constructor(init?:Partial<AuthenticateResponse>) { Object.assign(this, init); }
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
    public responseStatus: ResponseStatus;

    // @DataMember(Order=9)
    public meta: { [index:string]: string; };
}

// @DataContract
export class AssignRolesResponse
{
    public constructor(init?:Partial<AssignRolesResponse>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public allRoles: string[];

    // @DataMember(Order=2)
    public allPermissions: string[];

    // @DataMember(Order=3)
    public responseStatus: ResponseStatus;
}

// @DataContract
export class UnAssignRolesResponse
{
    public constructor(init?:Partial<UnAssignRolesResponse>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public allRoles: string[];

    // @DataMember(Order=2)
    public allPermissions: string[];

    // @DataMember(Order=3)
    public responseStatus: ResponseStatus;
}

// @DataContract
export class ConvertSessionToTokenResponse
{
    public constructor(init?:Partial<ConvertSessionToTokenResponse>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public meta: { [index:string]: string; };

    // @DataMember(Order=2)
    public accessToken: string;

    // @DataMember(Order=3)
    public responseStatus: ResponseStatus;
}

// @DataContract
export class GetAccessTokenResponse
{
    public constructor(init?:Partial<GetAccessTokenResponse>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public accessToken: string;

    // @DataMember(Order=2)
    public responseStatus: ResponseStatus;
}

// @DataContract
export class GetApiKeysResponse
{
    public constructor(init?:Partial<GetApiKeysResponse>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public results: UserApiKey[];

    // @DataMember(Order=2)
    public responseStatus: ResponseStatus;
}

// @DataContract
export class RegenerateApiKeysResponse
{
    public constructor(init?:Partial<RegenerateApiKeysResponse>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public results: UserApiKey[];

    // @DataMember(Order=2)
    public responseStatus: ResponseStatus;
}

// @DataContract
export class RegisterResponse
{
    public constructor(init?:Partial<RegisterResponse>) { Object.assign(this, init); }
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
}

// @Route("/anontype")
export class AnonType
{
    public constructor(init?:Partial<AnonType>) { Object.assign(this, init); }
}

// @Route("/query/requestlogs")
// @Route("/query/requestlogs/{Date}")
export class QueryRequestLogs extends QueryData<RequestLogEntry> implements IReturn<QueryResponse<RequestLogEntry>>
{
    public constructor(init?:Partial<QueryRequestLogs>) { super(init); Object.assign(this, init); }
    public date: string;
    public viewErrors: boolean;
    public createResponse() { return new QueryResponse<RequestLogEntry>(); }
    public getTypeName() { return 'QueryRequestLogs'; }
}

// @AutoQueryViewer(Name="Today\'s Logs", Title="Logs from Today")
export class TodayLogs extends QueryData<RequestLogEntry> implements IReturn<QueryResponse<RequestLogEntry>>
{
    public constructor(init?:Partial<TodayLogs>) { super(init); Object.assign(this, init); }
    public createResponse() { return new QueryResponse<RequestLogEntry>(); }
    public getTypeName() { return 'TodayLogs'; }
}

export class TodayErrorLogs extends QueryData<RequestLogEntry> implements IReturn<QueryResponse<RequestLogEntry>>
{
    public constructor(init?:Partial<TodayErrorLogs>) { super(init); Object.assign(this, init); }
    public createResponse() { return new QueryResponse<RequestLogEntry>(); }
    public getTypeName() { return 'TodayErrorLogs'; }
}

export class YesterdayLogs extends QueryData<RequestLogEntry> implements IReturn<QueryResponse<RequestLogEntry>>
{
    public constructor(init?:Partial<YesterdayLogs>) { super(init); Object.assign(this, init); }
    public createResponse() { return new QueryResponse<RequestLogEntry>(); }
    public getTypeName() { return 'YesterdayLogs'; }
}

export class YesterdayErrorLogs extends QueryData<RequestLogEntry> implements IReturn<QueryResponse<RequestLogEntry>>
{
    public constructor(init?:Partial<YesterdayErrorLogs>) { super(init); Object.assign(this, init); }
    public createResponse() { return new QueryResponse<RequestLogEntry>(); }
    public getTypeName() { return 'YesterdayErrorLogs'; }
}

// @Route("/query/rockstars")
export class QueryRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryRockstars>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryRockstars'; }
}

// @Route("/query/rockstars/cached")
export class QueryRockstarsCached extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryRockstarsCached>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryRockstarsCached'; }
}

// @Route("/changerequest/{Id}")
export class ChangeRequest implements IReturn<ChangeRequestResponse>
{
    public constructor(init?:Partial<ChangeRequest>) { Object.assign(this, init); }
    public id: string;
    public createResponse() { return new ChangeRequestResponse(); }
    public getTypeName() { return 'ChangeRequest'; }
}

// @Route("/compress/{Path*}")
export class CompressFile
{
    public constructor(init?:Partial<CompressFile>) { Object.assign(this, init); }
    public path: string;
}

// @Route("/Routing/LeadPost.aspx")
export class LegacyLeadPost
{
    public constructor(init?:Partial<LegacyLeadPost>) { Object.assign(this, init); }
    public leadType: string;
    public myId: number;
}

// @Route("/info/{Id}")
export class Info
{
    public constructor(init?:Partial<Info>) { Object.assign(this, init); }
    public id: string;
}

export class CustomHttpError implements IReturn<CustomHttpErrorResponse>
{
    public constructor(init?:Partial<CustomHttpError>) { Object.assign(this, init); }
    public statusCode: number;
    public statusDescription: string;
    public createResponse() { return new CustomHttpErrorResponse(); }
    public getTypeName() { return 'CustomHttpError'; }
}

export class CustomFieldHttpError implements IReturn<CustomFieldHttpErrorResponse>
{
    public constructor(init?:Partial<CustomFieldHttpError>) { Object.assign(this, init); }
    public createResponse() { return new CustomFieldHttpErrorResponse(); }
    public getTypeName() { return 'CustomFieldHttpError'; }
}

export class FallbackRoute
{
    public constructor(init?:Partial<FallbackRoute>) { Object.assign(this, init); }
    public pathInfo: string;
}

export class NoRepeat implements IReturn<NoRepeatResponse>
{
    public constructor(init?:Partial<NoRepeat>) { Object.assign(this, init); }
    public id: string;
    public createResponse() { return new NoRepeatResponse(); }
    public getTypeName() { return 'NoRepeat'; }
}

export class BatchThrows implements IReturn<BatchThrowsResponse>
{
    public constructor(init?:Partial<BatchThrows>) { Object.assign(this, init); }
    public id: number;
    public name: string;
    public createResponse() { return new BatchThrowsResponse(); }
    public getTypeName() { return 'BatchThrows'; }
}

export class BatchThrowsAsync implements IReturn<BatchThrowsResponse>
{
    public constructor(init?:Partial<BatchThrowsAsync>) { Object.assign(this, init); }
    public id: number;
    public name: string;
    public createResponse() { return new BatchThrowsResponse(); }
    public getTypeName() { return 'BatchThrowsAsync'; }
}

// @Route("/code/object", "GET")
export class ObjectId implements IReturn<ObjectDesignResponse>
{
    public constructor(init?:Partial<ObjectId>) { Object.assign(this, init); }
    public objectName: string;
    public createResponse() { return new ObjectDesignResponse(); }
    public getTypeName() { return 'ObjectId'; }
}

// @Route("/jwt")
export class CreateJwt extends AuthUserSession implements IReturn<CreateJwtResponse>
{
    public constructor(init?:Partial<CreateJwt>) { super(init); Object.assign(this, init); }
    public jwtExpiry: string;
    public createResponse() { return new CreateJwtResponse(); }
    public getTypeName() { return 'CreateJwt'; }
}

// @Route("/jwt-refresh")
export class CreateRefreshJwt implements IReturn<CreateRefreshJwtResponse>
{
    public constructor(init?:Partial<CreateRefreshJwt>) { Object.assign(this, init); }
    public userAuthId: string;
    public jwtExpiry: string;
    public createResponse() { return new CreateRefreshJwtResponse(); }
    public getTypeName() { return 'CreateRefreshJwt'; }
}

export class MetadataTest implements IReturn<MetadataTestResponse>
{
    public constructor(init?:Partial<MetadataTest>) { Object.assign(this, init); }
    public id: number;
    public createResponse() { return new MetadataTestResponse(); }
    public getTypeName() { return 'MetadataTest'; }
}

// @Route("/example", "GET")
// @DataContract
export class GetExample implements IReturn<GetExampleResponse>
{
    public constructor(init?:Partial<GetExample>) { Object.assign(this, init); }
    public createResponse() { return new GetExampleResponse(); }
    public getTypeName() { return 'GetExample'; }
}

export class MetadataRequest implements IReturn<AutoQueryMetadataResponse>
{
    public constructor(init?:Partial<MetadataRequest>) { Object.assign(this, init); }
    public metadataType: MetadataType;
    public createResponse() { return new AutoQueryMetadataResponse(); }
    public getTypeName() { return 'MetadataRequest'; }
}

export class ExcludeMetadataProperty
{
    public constructor(init?:Partial<ExcludeMetadataProperty>) { Object.assign(this, init); }
    public id: number;
}

// @Route("/namedconnection")
export class NamedConnection
{
    public constructor(init?:Partial<NamedConnection>) { Object.assign(this, init); }
    public emailAddresses: string;
}

/**
 * Description for HelloACodeGenTest
 */
export class HelloACodeGenTest implements IReturn<HelloACodeGenTestResponse>
{
    public constructor(init?:Partial<HelloACodeGenTest>) { Object.assign(this, init); }
    /**
     * Description for FirstField
     */
    public firstField: number;
    public secondFields: string[];
    public createResponse() { return new HelloACodeGenTestResponse(); }
    public getTypeName() { return 'HelloACodeGenTest'; }
}

export class HelloInService implements IReturn<HelloResponse>
{
    public constructor(init?:Partial<HelloInService>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new HelloResponse(); }
    public getTypeName() { return 'NativeTypesTestService.HelloInService'; }
}

// @Route("/hello")
// @Route("/hello/{Name}")
export class Hello implements IReturn<HelloResponse>
{
    public constructor(init?:Partial<Hello>) { Object.assign(this, init); }
    // @Required()
    public name: string;

    public title: string;
    public createResponse() { return new HelloResponse(); }
    public getTypeName() { return 'Hello'; }
}

/**
 * Description on HelloAll type
 */
// @DataContract
export class HelloAnnotated implements IReturn<HelloAnnotatedResponse>
{
    public constructor(init?:Partial<HelloAnnotated>) { Object.assign(this, init); }
    // @DataMember
    public name: string;
    public createResponse() { return new HelloAnnotatedResponse(); }
    public getTypeName() { return 'HelloAnnotated'; }
}

export class HelloWithNestedClass implements IReturn<HelloResponse>
{
    public constructor(init?:Partial<HelloWithNestedClass>) { Object.assign(this, init); }
    public name: string;
    public nestedClassProp: NestedClass;
    public createResponse() { return new HelloResponse(); }
    public getTypeName() { return 'HelloWithNestedClass'; }
}

export class HelloReturnList implements IReturn<OnlyInReturnListArg[]>
{
    public constructor(init?:Partial<HelloReturnList>) { Object.assign(this, init); }
    public names: string[];
    public createResponse() { return new Array<OnlyInReturnListArg>(); }
    public getTypeName() { return 'HelloReturnList'; }
}

export class HelloExisting implements IReturn<HelloExistingResponse>
{
    public constructor(init?:Partial<HelloExisting>) { Object.assign(this, init); }
    public names: string[];
    public createResponse() { return new HelloExistingResponse(); }
    public getTypeName() { return 'HelloExisting'; }
}

export class HelloWithEnum
{
    public constructor(init?:Partial<HelloWithEnum>) { Object.assign(this, init); }
    public enumProp: EnumType;
    public enumWithValues: EnumWithValues;
    public nullableEnumProp: EnumType;
    public enumFlags: EnumFlags;
    public enumStyle: EnumStyle;
}

export class RestrictedAttributes
{
    public constructor(init?:Partial<RestrictedAttributes>) { Object.assign(this, init); }
    public id: number;
    public name: string;
    public hello: Hello;
}

/**
 * AllowedAttributes Description
 */
// @Route("/allowed-attributes", "GET")
// @Api(Description="AllowedAttributes Description")
// @ApiResponse(Description="Your request was not understood", StatusCode=400)
// @DataContract
export class AllowedAttributes
{
    public constructor(init?:Partial<AllowedAttributes>) { Object.assign(this, init); }
    // @DataMember
    // @Required()
    public id: number;

    /**
     * Range Description
     */
        // @DataMember(Name="Aliased")
        // @ApiMember(DataType="double", Description="Range Description", IsRequired=true, ParameterType="path")
    public range: number;
}

/**
 * Multi Line Class
 */
// @Api(Description="Multi \r\nLine \r\nClass")
export class HelloAttributeStringTest
{
    public constructor(init?:Partial<HelloAttributeStringTest>) { Object.assign(this, init); }
    /**
     * Multi Line Property
     */
        // @ApiMember(Description="Multi \r\nLine \r\nProperty")
    public overflow: string;

    /**
     * Some \ escaped 	  chars
     */
        // @ApiMember(Description="Some \\ escaped \t \n chars")
    public escapedChars: string;
}

export class HelloAllTypes implements IReturn<HelloAllTypesResponse>
{
    public constructor(init?:Partial<HelloAllTypes>) { Object.assign(this, init); }
    public name: string;
    public allTypes: AllTypes;
    public allCollectionTypes: AllCollectionTypes;
    public createResponse() { return new HelloAllTypesResponse(); }
    public getTypeName() { return 'HelloAllTypes'; }
}

export class HelloString implements IReturn<string>
{
    public constructor(init?:Partial<HelloString>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return ''; }
    public getTypeName() { return 'HelloString'; }
}

export class HelloVoid implements IReturnVoid
{
    public constructor(init?:Partial<HelloVoid>) { Object.assign(this, init); }
    public name: string;
    public createResponse() {}
    public getTypeName() { return 'HelloVoid'; }
}

// @DataContract
export class HelloWithDataContract implements IReturn<HelloWithDataContractResponse>
{
    public constructor(init?:Partial<HelloWithDataContract>) { Object.assign(this, init); }
    // @DataMember(Name="name", Order=1, IsRequired=true, EmitDefaultValue=false)
    public name: string;

    // @DataMember(Name="id", Order=2, EmitDefaultValue=false)
    public id: number;
    public createResponse() { return new HelloWithDataContractResponse(); }
    public getTypeName() { return 'HelloWithDataContract'; }
}

/**
 * Description on HelloWithDescription type
 */
export class HelloWithDescription implements IReturn<HelloWithDescriptionResponse>
{
    public constructor(init?:Partial<HelloWithDescription>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new HelloWithDescriptionResponse(); }
    public getTypeName() { return 'HelloWithDescription'; }
}

export class HelloWithInheritance extends HelloBase implements IReturn<HelloWithInheritanceResponse>
{
    public constructor(init?:Partial<HelloWithInheritance>) { super(init); Object.assign(this, init); }
    public name: string;
    public createResponse() { return new HelloWithInheritanceResponse(); }
    public getTypeName() { return 'HelloWithInheritance'; }
}

export class HelloWithGenericInheritance extends HelloBase_1<Poco>
{
    public constructor(init?:Partial<HelloWithGenericInheritance>) { super(init); Object.assign(this, init); }
    public result: string;
}

export class HelloWithGenericInheritance2 extends HelloBase_1<Hello>
{
    public constructor(init?:Partial<HelloWithGenericInheritance2>) { super(init); Object.assign(this, init); }
    public result: string;
}

export class HelloWithNestedInheritance extends HelloBase_1<Item>
{
    public constructor(init?:Partial<HelloWithNestedInheritance>) { super(init); Object.assign(this, init); }
}

export class HelloWithListInheritance extends Array<InheritedItem>
{
    public constructor(init?:Partial<HelloWithListInheritance>) { super(); Object.assign(this, init); }
}

export class HelloWithReturn implements IReturn<HelloWithAlternateReturnResponse>
{
    public constructor(init?:Partial<HelloWithReturn>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new HelloWithAlternateReturnResponse(); }
    public getTypeName() { return 'HelloWithReturn'; }
}

// @Route("/helloroute")
export class HelloWithRoute implements IReturn<HelloWithRouteResponse>
{
    public constructor(init?:Partial<HelloWithRoute>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new HelloWithRouteResponse(); }
    public getTypeName() { return 'HelloWithRoute'; }
}

export class HelloWithType implements IReturn<HelloWithTypeResponse>
{
    public constructor(init?:Partial<HelloWithType>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new HelloWithTypeResponse(); }
    public getTypeName() { return 'HelloWithType'; }
}

export class HelloSession implements IReturn<HelloSessionResponse>
{
    public constructor(init?:Partial<HelloSession>) { Object.assign(this, init); }
    public createResponse() { return new HelloSessionResponse(); }
    public getTypeName() { return 'HelloSession'; }
}

export class HelloInterface
{
    public constructor(init?:Partial<HelloInterface>) { Object.assign(this, init); }
    public poco: IPoco;
    public emptyInterface: IEmptyInterface;
    public emptyClass: EmptyClass;
    public value: string;
}

export class Request1 implements IReturn<Request1Response>
{
    public constructor(init?:Partial<Request1>) { Object.assign(this, init); }
    public test: TypeA;
    public createResponse() { return new Request1Response(); }
    public getTypeName() { return 'Request1'; }
}

export class Request2 implements IReturn<Request2Response>
{
    public constructor(init?:Partial<Request2>) { Object.assign(this, init); }
    public test: TypeA;
    public createResponse() { return new Request2Response(); }
    public getTypeName() { return 'Request2'; }
}

export class HelloInnerTypes implements IReturn<HelloInnerTypesResponse>
{
    public constructor(init?:Partial<HelloInnerTypes>) { Object.assign(this, init); }
    public createResponse() { return new HelloInnerTypesResponse(); }
    public getTypeName() { return 'HelloInnerTypes'; }
}

export class GetUserSession implements IReturn<CustomUserSession>
{
    public constructor(init?:Partial<GetUserSession>) { Object.assign(this, init); }
    public createResponse() { return new CustomUserSession(); }
    public getTypeName() { return 'GetUserSession'; }
}

export class QueryTemplate implements IReturn<QueryResponseTemplate<Poco>>
{
    public constructor(init?:Partial<QueryTemplate>) { Object.assign(this, init); }
    public createResponse() { return new QueryResponseTemplate<Poco>(); }
    public getTypeName() { return 'QueryTemplate'; }
}

export class HelloReserved
{
    public constructor(init?:Partial<HelloReserved>) { Object.assign(this, init); }
    public class: string;
    public type: string;
    public extension: string;
}

export class HelloDictionary implements IReturn<any>
{
    public constructor(init?:Partial<HelloDictionary>) { Object.assign(this, init); }
    public key: string;
    public value: string;
    public createResponse() { return {}; }
    public getTypeName() { return 'HelloDictionary'; }
}

export class HelloBuiltin
{
    public constructor(init?:Partial<HelloBuiltin>) { Object.assign(this, init); }
    public dayOfWeek: DayOfWeek;
    public shortDays: ShortDays;
}

export class HelloGet implements IReturn<HelloVerbResponse>, IGet
{
    public constructor(init?:Partial<HelloGet>) { Object.assign(this, init); }
    public id: number;
    public createResponse() { return new HelloVerbResponse(); }
    public getTypeName() { return 'HelloGet'; }
}

export class HelloPost extends HelloBase implements IReturn<HelloVerbResponse>, IPost
{
    public constructor(init?:Partial<HelloPost>) { super(init); Object.assign(this, init); }
    public createResponse() { return new HelloVerbResponse(); }
    public getTypeName() { return 'HelloPost'; }
}

export class HelloPut implements IReturn<HelloVerbResponse>, IPut
{
    public constructor(init?:Partial<HelloPut>) { Object.assign(this, init); }
    public id: number;
    public createResponse() { return new HelloVerbResponse(); }
    public getTypeName() { return 'HelloPut'; }
}

export class HelloDelete implements IReturn<HelloVerbResponse>, IDelete
{
    public constructor(init?:Partial<HelloDelete>) { Object.assign(this, init); }
    public id: number;
    public createResponse() { return new HelloVerbResponse(); }
    public getTypeName() { return 'HelloDelete'; }
}

export class HelloPatch implements IReturn<HelloVerbResponse>, IPatch
{
    public constructor(init?:Partial<HelloPatch>) { Object.assign(this, init); }
    public id: number;
    public createResponse() { return new HelloVerbResponse(); }
    public getTypeName() { return 'HelloPatch'; }
}

export class HelloReturnVoid implements IReturnVoid
{
    public constructor(init?:Partial<HelloReturnVoid>) { Object.assign(this, init); }
    public id: number;
    public createResponse() {}
    public getTypeName() { return 'HelloReturnVoid'; }
}

export class EnumRequest implements IReturn<EnumResponse>, IPut
{
    public constructor(init?:Partial<EnumRequest>) { Object.assign(this, init); }
    public operator: ScopeType;
    public createResponse() { return new EnumResponse(); }
    public getTypeName() { return 'EnumRequest'; }
}

export class ExcludeTest1 implements IReturn<ExcludeTestNested>
{
    public constructor(init?:Partial<ExcludeTest1>) { Object.assign(this, init); }
    public createResponse() { return new ExcludeTestNested(); }
    public getTypeName() { return 'ExcludeTest1'; }
}

export class ExcludeTest2 implements IReturn<string>
{
    public constructor(init?:Partial<ExcludeTest2>) { Object.assign(this, init); }
    public excludeTestNested: ExcludeTestNested;
    public createResponse() { return ''; }
    public getTypeName() { return 'ExcludeTest2'; }
}

export class HelloAuthenticated implements IReturn<HelloAuthenticatedResponse>, IHasSessionId
{
    public constructor(init?:Partial<HelloAuthenticated>) { Object.assign(this, init); }
    public sessionId: string;
    public version: number;
    public createResponse() { return new HelloAuthenticatedResponse(); }
    public getTypeName() { return 'HelloAuthenticated'; }
}

/**
 * Echoes a sentence
 */
// @Route("/echoes", "POST")
// @Api(Description="Echoes a sentence")
export class Echoes implements IReturn<Echo>
{
    public constructor(init?:Partial<Echoes>) { Object.assign(this, init); }
    /**
     * The sentence to echo.
     */
        // @ApiMember(DataType="string", Description="The sentence to echo.", IsRequired=true, Name="Sentence", ParameterType="form")
    public sentence: string;
    public createResponse() { return new Echo(); }
    public getTypeName() { return 'Echoes'; }
}

export class CachedEcho implements IReturn<Echo>
{
    public constructor(init?:Partial<CachedEcho>) { Object.assign(this, init); }
    public reload: boolean;
    public sentence: string;
    public createResponse() { return new Echo(); }
    public getTypeName() { return 'CachedEcho'; }
}

export class AsyncTest implements IReturn<Echo>
{
    public constructor(init?:Partial<AsyncTest>) { Object.assign(this, init); }
    public createResponse() { return new Echo(); }
    public getTypeName() { return 'AsyncTest'; }
}

// @Route("/throwhttperror/{Status}")
export class ThrowHttpError implements IReturn<ThrowHttpErrorResponse>
{
    public constructor(init?:Partial<ThrowHttpError>) { Object.assign(this, init); }
    public status: number;
    public message: string;
    public createResponse() { return new ThrowHttpErrorResponse(); }
    public getTypeName() { return 'ThrowHttpError'; }
}

// @Route("/throw404")
// @Route("/throw404/{Message}")
export class Throw404
{
    public constructor(init?:Partial<Throw404>) { Object.assign(this, init); }
    public message: string;
}

// @Route("/return404")
export class Return404
{
    public constructor(init?:Partial<Return404>) { Object.assign(this, init); }
}

// @Route("/return404result")
export class Return404Result
{
    public constructor(init?:Partial<Return404Result>) { Object.assign(this, init); }
}

// @Route("/throw/{Type}")
export class ThrowType implements IReturn<ThrowTypeResponse>
{
    public constructor(init?:Partial<ThrowType>) { Object.assign(this, init); }
    public type: string;
    public message: string;
    public createResponse() { return new ThrowTypeResponse(); }
    public getTypeName() { return 'ThrowType'; }
}

// @Route("/throwvalidation")
export class ThrowValidation implements IReturn<ThrowValidationResponse>
{
    public constructor(init?:Partial<ThrowValidation>) { Object.assign(this, init); }
    public age: number;
    public required: string;
    public email: string;
    public createResponse() { return new ThrowValidationResponse(); }
    public getTypeName() { return 'ThrowValidation'; }
}

// @Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")
// @Route("/api/acsprofiles/{profileId}")
export class ACSProfile implements IReturn<acsprofileResponse>, IHasVersion, IHasSessionId
{
    public constructor(init?:Partial<ACSProfile>) { Object.assign(this, init); }
    public profileId: string;
    // @Required()
    // @StringLength(20)
    public shortName: string;

    // @StringLength(60)
    public longName: string;

    // @StringLength(20)
    public regionId: string;

    // @StringLength(20)
    public groupId: string;

    // @StringLength(12)
    public deviceID: string;

    public lastUpdated: string;
    public enabled: boolean;
    public version: number;
    public sessionId: string;
    public createResponse() { return new acsprofileResponse(); }
    public getTypeName() { return 'ACSProfile'; }
}

// @Route("/return/string")
export class ReturnString implements IReturn<string>
{
    public constructor(init?:Partial<ReturnString>) { Object.assign(this, init); }
    public data: string;
    public createResponse() { return ''; }
    public getTypeName() { return 'ReturnString'; }
}

// @Route("/return/bytes")
export class ReturnBytes implements IReturn<Uint8Array>
{
    public constructor(init?:Partial<ReturnBytes>) { Object.assign(this, init); }
    public data: Uint8Array;
    public createResponse() { return new Uint8Array(0); }
    public getTypeName() { return 'ReturnBytes'; }
}

// @Route("/return/stream")
export class ReturnStream implements IReturn<Blob>
{
    public constructor(init?:Partial<ReturnStream>) { Object.assign(this, init); }
    public data: Uint8Array;
    public createResponse() { return new Blob(); }
    public getTypeName() { return 'ReturnStream'; }
}

// @Route("/Request1/", "GET")
export class GetRequest1 implements IReturn<ReturnedDto[]>, IGet
{
    public constructor(init?:Partial<GetRequest1>) { Object.assign(this, init); }
    public createResponse() { return new Array<ReturnedDto>(); }
    public getTypeName() { return 'GetRequest1'; }
}

// @Route("/Request3", "GET")
export class GetRequest2 implements IReturn<ReturnedDto>, IGet
{
    public constructor(init?:Partial<GetRequest2>) { Object.assign(this, init); }
    public createResponse() { return new ReturnedDto(); }
    public getTypeName() { return 'GetRequest2'; }
}

// @Route("/matchlast/{Id}")
export class MatchesLastInt
{
    public constructor(init?:Partial<MatchesLastInt>) { Object.assign(this, init); }
    public id: number;
}

// @Route("/matchlast/{Slug}")
export class MatchesNotLastInt
{
    public constructor(init?:Partial<MatchesNotLastInt>) { Object.assign(this, init); }
    public slug: string;
}

// @Route("/matchregex/{Id}")
export class MatchesId
{
    public constructor(init?:Partial<MatchesId>) { Object.assign(this, init); }
    public id: number;
}

// @Route("/matchregex/{Slug}")
export class MatchesSlug
{
    public constructor(init?:Partial<MatchesSlug>) { Object.assign(this, init); }
    public slug: string;
}

// @Route("/{Version}/userdata", "GET")
export class SwaggerVersionTest
{
    public constructor(init?:Partial<SwaggerVersionTest>) { Object.assign(this, init); }
    public version: string;
}

// @Route("/swagger/range")
export class SwaggerRangeTest
{
    public constructor(init?:Partial<SwaggerRangeTest>) { Object.assign(this, init); }
    public intRange: string;
    public doubleRange: string;
}

// @Route("/test/errorview")
export class TestErrorView
{
    public constructor(init?:Partial<TestErrorView>) { Object.assign(this, init); }
    public id: string;
}

// @Route("/timestamp", "GET")
export class GetTimestamp implements IReturn<TimestampData>
{
    public constructor(init?:Partial<GetTimestamp>) { Object.assign(this, init); }
    public createResponse() { return new TimestampData(); }
    public getTypeName() { return 'GetTimestamp'; }
}

export class TestMiniverView
{
    public constructor(init?:Partial<TestMiniverView>) { Object.assign(this, init); }
}

// @Route("/testexecproc")
export class TestExecProc
{
    public constructor(init?:Partial<TestExecProc>) { Object.assign(this, init); }
}

// @Route("/files/{Path*}")
export class GetFile
{
    public constructor(init?:Partial<GetFile>) { Object.assign(this, init); }
    public path: string;
}

// @Route("/test/html2")
export class TestHtml2
{
    public constructor(init?:Partial<TestHtml2>) { Object.assign(this, init); }
    public name: string;
}

// @Route("/views/request")
export class ViewRequest implements IReturn<ViewResponse>
{
    public constructor(init?:Partial<ViewRequest>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return new ViewResponse(); }
    public getTypeName() { return 'ViewRequest'; }
}

// @Route("/index")
export class IndexPage
{
    public constructor(init?:Partial<IndexPage>) { Object.assign(this, init); }
    public pathInfo: string;
}

// @Route("/return/text")
export class ReturnText
{
    public constructor(init?:Partial<ReturnText>) { Object.assign(this, init); }
    public text: string;
}

// @Route("/gzip/{FileName}")
export class DownloadGzipFile implements IReturn<Uint8Array>
{
    public constructor(init?:Partial<DownloadGzipFile>) { Object.assign(this, init); }
    public fileName: string;
    public createResponse() { return new Uint8Array(0); }
    public getTypeName() { return 'DownloadGzipFile'; }
}

// @Route("/match/{Language}/{Name*}")
export class MatchName implements IReturn<HelloResponse>
{
    public constructor(init?:Partial<MatchName>) { Object.assign(this, init); }
    public language: string;
    public name: string;
    public createResponse() { return new HelloResponse(); }
    public getTypeName() { return 'MatchName'; }
}

// @Route("/match/{Language*}")
export class MatchLang implements IReturn<HelloResponse>
{
    public constructor(init?:Partial<MatchLang>) { Object.assign(this, init); }
    public language: string;
    public createResponse() { return new HelloResponse(); }
    public getTypeName() { return 'MatchLang'; }
}

// @Route("/reqlogstest/{Name}")
export class RequestLogsTest implements IReturn<string>
{
    public constructor(init?:Partial<RequestLogsTest>) { Object.assign(this, init); }
    public name: string;
    public createResponse() { return ''; }
    public getTypeName() { return 'RequestLogsTest'; }
}

export class InProcRequest1
{
    public constructor(init?:Partial<InProcRequest1>) { Object.assign(this, init); }
}

export class InProcRequest2
{
    public constructor(init?:Partial<InProcRequest2>) { Object.assign(this, init); }
}

/**
 * SwaggerTest Service Description
 */
// @Route("/swagger", "GET")
// @Route("/swagger/{Name}", "GET")
// @Route("/swagger/{Name}", "POST")
// @Api(Description="SwaggerTest Service Description")
// @ApiResponse(Description="Your request was not understood", StatusCode=400)
// @ApiResponse(Description="Oops, something broke", StatusCode=500)
// @DataContract
export class SwaggerTest
{
    public constructor(init?:Partial<SwaggerTest>) { Object.assign(this, init); }
    /**
     * Color Description
     */
        // @DataMember
        // @ApiMember(DataType="string", Description="Color Description", IsRequired=true, ParameterType="path")
    public name: string;

    // @DataMember
    // @ApiMember()
    public color: MyColor;

    /**
     * Aliased Description
     */
        // @DataMember(Name="Aliased")
        // @ApiMember(DataType="string", Description="Aliased Description", IsRequired=true)
    public original: string;

    /**
     * Not Aliased Description
     */
        // @DataMember
        // @ApiMember(DataType="string", Description="Not Aliased Description", IsRequired=true)
    public notAliased: string;

    /**
     * Format as password
     */
        // @DataMember
        // @ApiMember(DataType="password", Description="Format as password")
    public password: string;

    // @DataMember
    // @ApiMember(AllowMultiple=true)
    public myDateBetween: string[];

    /**
     * Nested model 1
     */
        // @DataMember
        // @ApiMember(DataType="SwaggerNestedModel", Description="Nested model 1")
    public nestedModel1: SwaggerNestedModel;

    /**
     * Nested model 2
     */
        // @DataMember
        // @ApiMember(DataType="SwaggerNestedModel2", Description="Nested model 2")
    public nestedModel2: SwaggerNestedModel2;
}

// @Route("/swaggertest2", "POST")
export class SwaggerTest2
{
    public constructor(init?:Partial<SwaggerTest2>) { Object.assign(this, init); }
    // @ApiMember()
    public myEnumProperty: MyEnum;

    // @ApiMember(DataType="string", IsRequired=true, Name="Token", ParameterType="header")
    public token: string;
}

// @Route("/swagger-complex", "POST")
export class SwaggerComplex implements IReturn<SwaggerComplexResponse>
{
    public constructor(init?:Partial<SwaggerComplex>) { Object.assign(this, init); }
    // @DataMember
    // @ApiMember()
    public isRequired: boolean;

    // @DataMember
    // @ApiMember(IsRequired=true)
    public arrayString: string[];

    // @DataMember
    // @ApiMember()
    public arrayInt: number[];

    // @DataMember
    // @ApiMember()
    public listString: string[];

    // @DataMember
    // @ApiMember()
    public listInt: number[];

    // @DataMember
    // @ApiMember()
    public dictionaryString: { [index:string]: string; };
    public createResponse() { return new SwaggerComplexResponse(); }
    public getTypeName() { return 'SwaggerComplex'; }
}

// @Route("/swaggerpost/{Required1}", "GET")
// @Route("/swaggerpost/{Required1}/{Optional1}", "GET")
// @Route("/swaggerpost", "POST")
export class SwaggerPostTest implements IReturn<HelloResponse>
{
    public constructor(init?:Partial<SwaggerPostTest>) { Object.assign(this, init); }
    // @ApiMember(Verb="POST")
    // @ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}", Verb="GET")
    // @ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}/{Optional1}", Verb="GET")
    public required1: string;

    // @ApiMember(Verb="POST")
    // @ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}/{Optional1}", Verb="GET")
    public optional1: string;
    public createResponse() { return new HelloResponse(); }
    public getTypeName() { return 'SwaggerPostTest'; }
}

// @Route("/swaggerpost2/{Required1}/{Required2}", "GET")
// @Route("/swaggerpost2/{Required1}/{Required2}/{Optional1}", "GET")
// @Route("/swaggerpost2", "POST")
export class SwaggerPostTest2 implements IReturn<HelloResponse>
{
    public constructor(init?:Partial<SwaggerPostTest2>) { Object.assign(this, init); }
    // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}", Verb="GET")
    // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")
    public required1: string;

    // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}", Verb="GET")
    // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")
    public required2: string;

    // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")
    public optional1: string;
    public createResponse() { return new HelloResponse(); }
    public getTypeName() { return 'SwaggerPostTest2'; }
}

// @Route("/swagger/multiattrtest", "POST")
// @ApiResponse(Description="Code 1", StatusCode=400)
// @ApiResponse(Description="Code 2", StatusCode=402)
// @ApiResponse(Description="Code 3", StatusCode=401)
export class SwaggerMultiApiResponseTest implements IReturnVoid
{
    public constructor(init?:Partial<SwaggerMultiApiResponseTest>) { Object.assign(this, init); }
    public createResponse() {}
    public getTypeName() { return 'SwaggerMultiApiResponseTest'; }
}

// @Route("/defaultview/class")
export class DefaultViewAttr
{
    public constructor(init?:Partial<DefaultViewAttr>) { Object.assign(this, init); }
}

// @Route("/defaultview/action")
export class DefaultViewActionAttr
{
    public constructor(init?:Partial<DefaultViewActionAttr>) { Object.assign(this, init); }
}

// @Route("/dynamically/registered/{Name}")
export class DynamicallyRegistered
{
    public constructor(init?:Partial<DynamicallyRegistered>) { Object.assign(this, init); }
    public name: string;
}

// @Route("/auth")
// @Route("/auth/{provider}")
// @Route("/authenticate")
// @Route("/authenticate/{provider}")
// @DataContract
export class Authenticate implements IReturn<AuthenticateResponse>, IPost
{
    public constructor(init?:Partial<Authenticate>) { Object.assign(this, init); }
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
    public createResponse() { return new AuthenticateResponse(); }
    public getTypeName() { return 'Authenticate'; }
}

// @Route("/assignroles")
// @DataContract
export class AssignRoles implements IReturn<AssignRolesResponse>, IPost
{
    public constructor(init?:Partial<AssignRoles>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public userName: string;

    // @DataMember(Order=2)
    public permissions: string[];

    // @DataMember(Order=3)
    public roles: string[];
    public createResponse() { return new AssignRolesResponse(); }
    public getTypeName() { return 'AssignRoles'; }
}

// @Route("/unassignroles")
// @DataContract
export class UnAssignRoles implements IReturn<UnAssignRolesResponse>, IPost
{
    public constructor(init?:Partial<UnAssignRoles>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public userName: string;

    // @DataMember(Order=2)
    public permissions: string[];

    // @DataMember(Order=3)
    public roles: string[];
    public createResponse() { return new UnAssignRolesResponse(); }
    public getTypeName() { return 'UnAssignRoles'; }
}

// @Route("/session-to-token")
// @DataContract
export class ConvertSessionToToken implements IReturn<ConvertSessionToTokenResponse>, IPost
{
    public constructor(init?:Partial<ConvertSessionToToken>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public preserveSession: boolean;
    public createResponse() { return new ConvertSessionToTokenResponse(); }
    public getTypeName() { return 'ConvertSessionToToken'; }
}

// @Route("/access-token")
// @DataContract
export class GetAccessToken implements IReturn<GetAccessTokenResponse>, IPost
{
    public constructor(init?:Partial<GetAccessToken>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public refreshToken: string;
    public createResponse() { return new GetAccessTokenResponse(); }
    public getTypeName() { return 'GetAccessToken'; }
}

// @Route("/apikeys")
// @Route("/apikeys/{Environment}")
// @DataContract
export class GetApiKeys implements IReturn<GetApiKeysResponse>, IGet
{
    public constructor(init?:Partial<GetApiKeys>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public environment: string;
    public createResponse() { return new GetApiKeysResponse(); }
    public getTypeName() { return 'GetApiKeys'; }
}

// @Route("/apikeys/regenerate")
// @Route("/apikeys/regenerate/{Environment}")
// @DataContract
export class RegenerateApiKeys implements IReturn<RegenerateApiKeysResponse>, IPost
{
    public constructor(init?:Partial<RegenerateApiKeys>) { Object.assign(this, init); }
    // @DataMember(Order=1)
    public environment: string;
    public createResponse() { return new RegenerateApiKeysResponse(); }
    public getTypeName() { return 'RegenerateApiKeys'; }
}

// @Route("/register")
// @DataContract
export class Register implements IReturn<RegisterResponse>, IPost
{
    public constructor(init?:Partial<Register>) { Object.assign(this, init); }
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
    public createResponse() { return new RegisterResponse(); }
    public getTypeName() { return 'Register'; }
}

// @Route("/pgsql/rockstars")
export class QueryPostgresRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryPostgresRockstars>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryPostgresRockstars'; }
}

// @Route("/pgsql/pgrockstars")
export class QueryPostgresPgRockstars extends QueryDb_1<PgRockstar> implements IReturn<QueryResponse<PgRockstar>>
{
    public constructor(init?:Partial<QueryPostgresPgRockstars>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<PgRockstar>(); }
    public getTypeName() { return 'QueryPostgresPgRockstars'; }
}

export class QueryRockstarsConventions extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryRockstarsConventions>) { super(init); Object.assign(this, init); }
    public ids: number[];
    public ageOlderThan: number;
    public ageGreaterThanOrEqualTo: number;
    public ageGreaterThan: number;
    public greaterThanAge: number;
    public firstNameStartsWith: string;
    public lastNameEndsWith: string;
    public lastNameContains: string;
    public rockstarAlbumNameContains: string;
    public rockstarIdAfter: number;
    public rockstarIdOnOrAfter: number;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryRockstarsConventions'; }
}

// @AutoQueryViewer(Description="Use this option to search for Rockstars!", Title="Search for Rockstars")
export class QueryCustomRockstars extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>
{
    public constructor(init?:Partial<QueryCustomRockstars>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<CustomRockstar>(); }
    public getTypeName() { return 'QueryCustomRockstars'; }
}

// @Route("/customrockstars")
export class QueryRockstarAlbums extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>
{
    public constructor(init?:Partial<QueryRockstarAlbums>) { super(init); Object.assign(this, init); }
    public age: number;
    public rockstarAlbumName: string;
    public createResponse() { return new QueryResponse<CustomRockstar>(); }
    public getTypeName() { return 'QueryRockstarAlbums'; }
}

export class QueryRockstarAlbumsImplicit extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>
{
    public constructor(init?:Partial<QueryRockstarAlbumsImplicit>) { super(init); Object.assign(this, init); }
    public createResponse() { return new QueryResponse<CustomRockstar>(); }
    public getTypeName() { return 'QueryRockstarAlbumsImplicit'; }
}

export class QueryRockstarAlbumsLeftJoin extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>
{
    public constructor(init?:Partial<QueryRockstarAlbumsLeftJoin>) { super(init); Object.assign(this, init); }
    public age: number;
    public albumName: string;
    public createResponse() { return new QueryResponse<CustomRockstar>(); }
    public getTypeName() { return 'QueryRockstarAlbumsLeftJoin'; }
}

export class QueryOverridedRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryOverridedRockstars>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryOverridedRockstars'; }
}

export class QueryOverridedCustomRockstars extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>
{
    public constructor(init?:Partial<QueryOverridedCustomRockstars>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<CustomRockstar>(); }
    public getTypeName() { return 'QueryOverridedCustomRockstars'; }
}

// @Route("/query-custom/rockstars")
export class QueryFieldRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryFieldRockstars>) { super(init); Object.assign(this, init); }
    public firstName: string;
    public firstNames: string[];
    public age: number;
    public firstNameCaseInsensitive: string;
    public firstNameStartsWith: string;
    public lastNameEndsWith: string;
    public firstNameBetween: string[];
    public orLastName: string;
    public firstNameContainsMulti: string[];
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryFieldRockstars'; }
}

export class QueryFieldRockstarsDynamic extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryFieldRockstarsDynamic>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryFieldRockstarsDynamic'; }
}

export class QueryRockstarsFilter extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryRockstarsFilter>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryRockstarsFilter'; }
}

export class QueryCustomRockstarsFilter extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>
{
    public constructor(init?:Partial<QueryCustomRockstarsFilter>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<CustomRockstar>(); }
    public getTypeName() { return 'QueryCustomRockstarsFilter'; }
}

export class QueryRockstarsIFilter extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IFilterRockstars
{
    public constructor(init?:Partial<QueryRockstarsIFilter>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryRockstarsIFilter'; }
}

// @Route("/OrRockstars")
export class QueryOrRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryOrRockstars>) { super(init); Object.assign(this, init); }
    public age: number;
    public firstName: string;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryOrRockstars'; }
}

export class QueryGetRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryGetRockstars>) { super(init); Object.assign(this, init); }
    public ids: number[];
    public ages: number[];
    public firstNames: string[];
    public idsBetween: number[];
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryGetRockstars'; }
}

export class QueryGetRockstarsDynamic extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryGetRockstarsDynamic>) { super(init); Object.assign(this, init); }
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryGetRockstarsDynamic'; }
}

// @Route("/movies/search")
export class SearchMovies extends QueryDb_1<Movie> implements IReturn<QueryResponse<Movie>>
{
    public constructor(init?:Partial<SearchMovies>) { super(init); Object.assign(this, init); }
    public createResponse() { return new QueryResponse<Movie>(); }
    public getTypeName() { return 'SearchMovies'; }
}

// @Route("/movies")
export class QueryMovies extends QueryDb_1<Movie> implements IReturn<QueryResponse<Movie>>
{
    public constructor(init?:Partial<QueryMovies>) { super(init); Object.assign(this, init); }
    public ids: number[];
    public imdbIds: string[];
    public ratings: string[];
    public createResponse() { return new QueryResponse<Movie>(); }
    public getTypeName() { return 'QueryMovies'; }
}

export class StreamMovies extends QueryDb_1<Movie> implements IReturn<QueryResponse<Movie>>
{
    public constructor(init?:Partial<StreamMovies>) { super(init); Object.assign(this, init); }
    public ratings: string[];
    public createResponse() { return new QueryResponse<Movie>(); }
    public getTypeName() { return 'StreamMovies'; }
}

export class QueryUnknownRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryUnknownRockstars>) { super(init); Object.assign(this, init); }
    public unknownInt: number;
    public unknownProperty: string;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryUnknownRockstars'; }
}

// @Route("/query/rockstar-references")
export class QueryRockstarsWithReferences extends QueryDb_1<RockstarReference> implements IReturn<QueryResponse<RockstarReference>>
{
    public constructor(init?:Partial<QueryRockstarsWithReferences>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<RockstarReference>(); }
    public getTypeName() { return 'QueryRockstarsWithReferences'; }
}

export class QueryPocoBase extends QueryDb_1<OnlyDefinedInGenericType> implements IReturn<QueryResponse<OnlyDefinedInGenericType>>
{
    public constructor(init?:Partial<QueryPocoBase>) { super(init); Object.assign(this, init); }
    public id: number;
    public createResponse() { return new QueryResponse<OnlyDefinedInGenericType>(); }
    public getTypeName() { return 'QueryPocoBase'; }
}

export class QueryPocoIntoBase extends QueryDb_2<OnlyDefinedInGenericTypeFrom, OnlyDefinedInGenericTypeInto> implements IReturn<QueryResponse<OnlyDefinedInGenericTypeInto>>
{
    public constructor(init?:Partial<QueryPocoIntoBase>) { super(init); Object.assign(this, init); }
    public id: number;
    public createResponse() { return new QueryResponse<OnlyDefinedInGenericTypeInto>(); }
    public getTypeName() { return 'QueryPocoIntoBase'; }
}

// @Route("/query/alltypes")
export class QueryAllTypes extends QueryDb_1<AllTypes> implements IReturn<QueryResponse<AllTypes>>
{
    public constructor(init?:Partial<QueryAllTypes>) { super(init); Object.assign(this, init); }
    public createResponse() { return new QueryResponse<AllTypes>(); }
    public getTypeName() { return 'QueryAllTypes'; }
}

// @Route("/querydata/rockstars")
export class QueryDataRockstars extends QueryData<Rockstar> implements IReturn<QueryResponse<Rockstar>>
{
    public constructor(init?:Partial<QueryDataRockstars>) { super(init); Object.assign(this, init); }
    public age: number;
    public createResponse() { return new QueryResponse<Rockstar>(); }
    public getTypeName() { return 'QueryDataRockstars'; }
}
