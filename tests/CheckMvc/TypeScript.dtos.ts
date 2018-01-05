/* Options:
Date: 2017-11-22 18:17:46
Version: 5.00
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:55799

//GlobalNamespace: 
//MakePropertiesOptional: True
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
    createResponse() : T;
}

export interface IReturnVoid
{
    createResponse() : void;
}

export interface IMeta
{
    meta?: { [index:string]: string; };
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

export interface IHasSessionId
{
    sessionId?: string;
}

export interface IHasVersion
{
    version?: number;
}

export class QueryBase
{
    // @DataMember(Order=1)
    skip: number;

    // @DataMember(Order=2)
    take: number;

    // @DataMember(Order=3)
    orderBy: string;

    // @DataMember(Order=4)
    orderByDesc: string;

    // @DataMember(Order=5)
    include: string;

    // @DataMember(Order=6)
    fields: string;

    // @DataMember(Order=7)
    meta: { [index:string]: string; };
}

export class QueryData<T> extends QueryBase
{
}

export class RequestLogEntry
{
    id: number;
    dateTime: string;
    statusCode: number;
    statusDescription: string;
    httpMethod: string;
    absoluteUri: string;
    pathInfo: string;
    requestBody: string;
    requestDto: Object;
    userAuthId: string;
    sessionId: string;
    ipAddress: string;
    forwardedFor: string;
    referer: string;
    headers: { [index:string]: string; };
    formData: { [index:string]: string; };
    items: { [index:string]: string; };
    session: Object;
    responseDto: Object;
    errorResponse: Object;
    exceptionSource: string;
    exceptionData: any;
    requestDuration: string;
}

// @DataContract
export class ResponseError
{
    // @DataMember(Order=1, EmitDefaultValue=false)
    errorCode: string;

    // @DataMember(Order=2, EmitDefaultValue=false)
    fieldName: string;

    // @DataMember(Order=3, EmitDefaultValue=false)
    message: string;

    // @DataMember(Order=4, EmitDefaultValue=false)
    meta: { [index:string]: string; };
}

// @DataContract
export class ResponseStatus
{
    // @DataMember(Order=1)
    errorCode: string;

    // @DataMember(Order=2)
    message: string;

    // @DataMember(Order=3)
    stackTrace: string;

    // @DataMember(Order=4)
    errors: ResponseError[];

    // @DataMember(Order=5)
    meta: { [index:string]: string; };
}

export class QueryDb_1<T> extends QueryBase
{
}

export class Rockstar
{
    /**
    * Идентификатор
    */
    id: number;
    /**
    * Фамилия
    */
    firstName: string;
    /**
    * Имя
    */
    lastName: string;
    /**
    * Возраст
    */
    age: number;
}

export class ObjectDesign
{
    id: number;
}

export class MetadataTestNestedChild
{
    name: string;
}

export class MetadataTestChild
{
    name: string;
    results: MetadataTestNestedChild[];
}

export class MenuItemExampleItem
{
    // @DataMember(Order=1)
    // @ApiMember()
    name1: string;
}

export class MenuItemExample
{
    // @DataMember(Order=1)
    // @ApiMember()
    name1: string;

    menuItemExampleItem: MenuItemExampleItem;
}

// @DataContract
export class MenuExample
{
    // @DataMember(Order=1)
    // @ApiMember()
    menuItemExample1: MenuItemExample;
}

export class MetadataTypeName
{
    name: string;
    namespace: string;
    genericArgs: string[];
}

export class MetadataRoute
{
    path: string;
    verbs: string;
    notes: string;
    summary: string;
}

export class MetadataDataContract
{
    name: string;
    namespace: string;
}

export class MetadataDataMember
{
    name: string;
    order: number;
    isRequired: boolean;
    emitDefaultValue: boolean;
}

export class MetadataAttribute
{
    name: string;
    constructorArgs: MetadataPropertyType[];
    args: MetadataPropertyType[];
}

export class MetadataPropertyType
{
    name: string;
    type: string;
    isValueType: boolean;
    isSystemType: boolean;
    isEnum: boolean;
    typeNamespace: string;
    genericArgs: string[];
    value: string;
    description: string;
    dataMember: MetadataDataMember;
    readOnly: boolean;
    paramType: string;
    displayType: string;
    isRequired: boolean;
    allowableValues: string[];
    allowableMin: number;
    allowableMax: number;
    attributes: MetadataAttribute[];
}

export class MetadataType
{
    name: string;
    namespace: string;
    genericArgs: string[];
    inherits: MetadataTypeName;
    implements: MetadataTypeName[];
    displayType: string;
    description: string;
    returnVoidMarker: boolean;
    isNested: boolean;
    isEnum: boolean;
    isEnumInt: boolean;
    isInterface: boolean;
    isAbstract: boolean;
    returnMarkerTypeName: MetadataTypeName;
    routes: MetadataRoute[];
    dataContract: MetadataDataContract;
    properties: MetadataPropertyType[];
    attributes: MetadataAttribute[];
    innerTypes: MetadataTypeName[];
    enumNames: string[];
    enumValues: string[];
    meta: { [index:string]: string; };
}

export class AutoQueryConvention
{
    name: string;
    value: string;
    types: string;
}

export class AutoQueryViewerConfig
{
    serviceBaseUrl: string;
    serviceName: string;
    serviceDescription: string;
    serviceIconUrl: string;
    formats: string[];
    maxLimit: number;
    isPublic: boolean;
    onlyShowAnnotatedServices: boolean;
    implicitConventions: AutoQueryConvention[];
    defaultSearchField: string;
    defaultSearchType: string;
    defaultSearchText: string;
    brandUrl: string;
    brandImageUrl: string;
    textColor: string;
    linkColor: string;
    backgroundColor: string;
    backgroundImageUrl: string;
    iconUrl: string;
    meta: { [index:string]: string; };
}

export class AutoQueryViewerUserInfo
{
    isAuthenticated: boolean;
    queryCount: number;
    meta: { [index:string]: string; };
}

export class AutoQueryOperation
{
    request: string;
    from: string;
    to: string;
    meta: { [index:string]: string; };
}

export class NativeTypesTestService
{
}

export class NestedClass
{
    value: string;
}

export class ListResult
{
    result: string;
}

export class OnlyInReturnListArg
{
    result: string;
}

export class ArrayResult
{
    result: string;
}

export type EnumType = "Value1" | "Value2";

export type EnumWithValues = "Value1" | "Value2";

// @Flags()
export enum EnumFlags
{
    Value1 = 1,
    Value2 = 2,
    Value3 = 4,
}

export class Poco
{
    name: string;
}

export class AllCollectionTypes
{
    intArray: number[];
    intList: number[];
    stringArray: string[];
    stringList: string[];
    pocoArray: Poco[];
    pocoList: Poco[];
    nullableByteArray: Uint8Array;
    nullableByteList: number[];
    nullableDateTimeArray: string[];
    nullableDateTimeList: string[];
    pocoLookup: { [index:string]: Poco[]; };
    pocoLookupMap: { [index:string]: { [index:string]: Poco; }[]; };
}

export class KeyValuePair<TKey, TValue>
{
    key: TKey;
    value: TValue;
}

export class SubType
{
    id: number;
    name: string;
}

export class HelloBase
{
    id: number;
}

export class HelloResponseBase
{
    refId: number;
}

export class HelloBase_1<T>
{
    items: T[];
    counts: number[];
}

export class Item
{
    value: string;
}

export class InheritedItem
{
    name: string;
}

export class HelloWithReturnResponse
{
    result: string;
}

export class HelloType
{
    result: string;
}

export interface IAuthTokens
{
    provider?: string;
    userId?: string;
    accessToken?: string;
    accessTokenSecret?: string;
    refreshToken?: string;
    refreshTokenExpiry?: string;
    requestToken?: string;
    requestTokenSecret?: string;
    items?: { [index:string]: string; };
}

// @DataContract
export class AuthUserSession
{
    // @DataMember(Order=1)
    referrerUrl: string;

    // @DataMember(Order=2)
    id: string;

    // @DataMember(Order=3)
    userAuthId: string;

    // @DataMember(Order=4)
    userAuthName: string;

    // @DataMember(Order=5)
    userName: string;

    // @DataMember(Order=6)
    twitterUserId: string;

    // @DataMember(Order=7)
    twitterScreenName: string;

    // @DataMember(Order=8)
    facebookUserId: string;

    // @DataMember(Order=9)
    facebookUserName: string;

    // @DataMember(Order=10)
    firstName: string;

    // @DataMember(Order=11)
    lastName: string;

    // @DataMember(Order=12)
    displayName: string;

    // @DataMember(Order=13)
    company: string;

    // @DataMember(Order=14)
    email: string;

    // @DataMember(Order=15)
    primaryEmail: string;

    // @DataMember(Order=16)
    phoneNumber: string;

    // @DataMember(Order=17)
    birthDate: string;

    // @DataMember(Order=18)
    birthDateRaw: string;

    // @DataMember(Order=19)
    address: string;

    // @DataMember(Order=20)
    address2: string;

    // @DataMember(Order=21)
    city: string;

    // @DataMember(Order=22)
    state: string;

    // @DataMember(Order=23)
    country: string;

    // @DataMember(Order=24)
    culture: string;

    // @DataMember(Order=25)
    fullName: string;

    // @DataMember(Order=26)
    gender: string;

    // @DataMember(Order=27)
    language: string;

    // @DataMember(Order=28)
    mailAddress: string;

    // @DataMember(Order=29)
    nickname: string;

    // @DataMember(Order=30)
    postalCode: string;

    // @DataMember(Order=31)
    timeZone: string;

    // @DataMember(Order=32)
    requestTokenSecret: string;

    // @DataMember(Order=33)
    createdAt: string;

    // @DataMember(Order=34)
    lastModified: string;

    // @DataMember(Order=35)
    roles: string[];

    // @DataMember(Order=36)
    permissions: string[];

    // @DataMember(Order=37)
    isAuthenticated: boolean;

    // @DataMember(Order=38)
    fromToken: boolean;

    // @DataMember(Order=39)
    profileUrl: string;

    // @DataMember(Order=40)
    sequence: string;

    // @DataMember(Order=41)
    tag: number;

    // @DataMember(Order=42)
    authProvider: string;

    // @DataMember(Order=43)
    providerOAuthAccess: IAuthTokens[];

    // @DataMember(Order=44)
    meta: { [index:string]: string; };
}

export interface IPoco
{
    name?: string;
}

export interface IEmptyInterface
{
}

export class EmptyClass
{
}

export interface ImplementsPoco
{
    name?: string;
}

export class TypeB
{
    foo: string;
}

export class TypeA
{
    bar: TypeB[];
}

export class InnerType
{
    id: number;
    name: string;
}

export type InnerEnum = "Foo" | "Bar" | "Baz";

export class InnerTypeItem
{
    id: number;
    name: string;
}

export type DayOfWeek = "Sunday" | "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday" | "Saturday";

// @DataContract
export type ScopeType = "Global" | "Sale";

export class Tuple_2<T1, T2>
{
    item1: T1;
    item2: T2;
}

export class Tuple_3<T1, T2, T3>
{
    item1: T1;
    item2: T2;
    item3: T3;
}

export interface IEcho
{
    sentence?: string;
}

export type MyColor = "Red" | "Green" | "Blue";

export class SwaggerNestedModel
{
    /**
    * NestedProperty description
    */
    // @ApiMember(Description="NestedProperty description")
    nestedProperty: boolean;
}

export class SwaggerNestedModel2
{
    /**
    * NestedProperty2 description
    */
    // @ApiMember(Description="NestedProperty2 description")
    nestedProperty2: boolean;

    /**
    * MultipleValues description
    */
    // @ApiMember(Description="MultipleValues description")
    multipleValues: string;

    /**
    * TestRange description
    */
    // @ApiMember(Description="TestRange description")
    testRange: number;
}

export type MyEnum = "A" | "B" | "C";

// @DataContract
export class UserApiKey
{
    // @DataMember(Order=1)
    key: string;

    // @DataMember(Order=2)
    keyType: string;

    // @DataMember(Order=3)
    expiryDate: string;
}

export class PgRockstar extends Rockstar
{
}

export class QueryDb_2<From, Into> extends QueryBase
{
}

export class CustomRockstar
{
    // @AutoQueryViewerField(Title="Name")
    firstName: string;

    // @AutoQueryViewerField(HideInSummary=true)
    lastName: string;

    age: number;
    // @AutoQueryViewerField(Title="Album")
    rockstarAlbumName: string;

    // @AutoQueryViewerField(Title="Genre")
    rockstarGenreName: string;
}

export interface IFilterRockstars
{
}

export class Movie
{
    id: number;
    imdbId: string;
    title: string;
    rating: string;
    score: number;
    director: string;
    releaseDate: string;
    tagLine: string;
    genres: string[];
}

export class RockstarAlbum
{
    id: number;
    rockstarId: number;
    name: string;
}

export class RockstarReference
{
    id: number;
    firstName: string;
    lastName: string;
    age: number;
    albums: RockstarAlbum[];
}

export class OnlyDefinedInGenericType
{
    id: number;
    name: string;
}

export class OnlyDefinedInGenericTypeFrom
{
    id: number;
    name: string;
}

export class OnlyDefinedInGenericTypeInto
{
    id: number;
    name: string;
}

export class TypesGroup
{
}

// @DataContract
export class QueryResponse<T>
{
    // @DataMember(Order=1)
    offset: number;

    // @DataMember(Order=2)
    total: number;

    // @DataMember(Order=3)
    results: T[];

    // @DataMember(Order=4)
    meta: { [index:string]: string; };

    // @DataMember(Order=5)
    responseStatus: ResponseStatus;
}

// @DataContract
export class UpdateEventSubscriberResponse
{
    // @DataMember(Order=1)
    responseStatus: ResponseStatus;
}

export class ChangeRequestResponse
{
    contentType: string;
    header: string;
    queryString: string;
    form: string;
    responseStatus: ResponseStatus;
}

export class CustomHttpErrorResponse
{
    custom: string;
    responseStatus: ResponseStatus;
}

// @Route("/alwaysthrows")
export class AlwaysThrows implements IReturn<AlwaysThrows>
{
    createResponse() { return new AlwaysThrows(); }
    getTypeName() { return "AlwaysThrows"; }
}

// @Route("/alwaysthrowsfilterattribute")
export class AlwaysThrowsFilterAttribute implements IReturn<AlwaysThrowsFilterAttribute>
{
    createResponse() { return new AlwaysThrowsFilterAttribute(); }
    getTypeName() { return "AlwaysThrowsFilterAttribute"; }
}

// @Route("/alwaysthrowsglobalfilter")
export class AlwaysThrowsGlobalFilter implements IReturn<AlwaysThrowsGlobalFilter>
{
    createResponse() { return new AlwaysThrowsGlobalFilter(); }
    getTypeName() { return "AlwaysThrowsGlobalFilter"; }
}

export class CustomFieldHttpErrorResponse
{
    custom: string;
    responseStatus: ResponseStatus;
}

export class NoRepeatResponse
{
    id: string;
}

export class BatchThrowsResponse
{
    result: string;
    responseStatus: ResponseStatus;
}

export class ObjectDesignResponse
{
    data: ObjectDesign;
}

export class MetadataTestResponse
{
    id: number;
    results: MetadataTestChild[];
}

// @DataContract
export class GetExampleResponse
{
    // @DataMember(Order=1)
    responseStatus: ResponseStatus;

    // @DataMember(Order=2)
    // @ApiMember()
    menuExample1: MenuExample;
}

export class AutoQueryMetadataResponse
{
    config: AutoQueryViewerConfig;
    userInfo: AutoQueryViewerUserInfo;
    operations: AutoQueryOperation[];
    types: MetadataType[];
    responseStatus: ResponseStatus;
    meta: { [index:string]: string; };
}

// @DataContract
export class HelloACodeGenTestResponse
{
    /**
    * Description for FirstResult
    */
    // @DataMember
    firstResult: number;

    /**
    * Description for SecondResult
    */
    // @DataMember
    // @ApiMember(Description="Description for SecondResult")
    secondResult: number;
}

export class HelloResponse
{
    result: string;
}

/**
* Description on HelloAllResponse type
*/
// @DataContract
export class HelloAnnotatedResponse
{
    // @DataMember
    result: string;
}

export class HelloList implements IReturn<Array<ListResult>>
{
    names: string[];
    createResponse() { return new Array<ListResult>(); }
    getTypeName() { return "HelloList"; }
}

export class HelloArray implements IReturn<Array<ArrayResult>>
{
    names: string[];
    createResponse() { return new Array<ArrayResult>(); }
    getTypeName() { return "HelloArray"; }
}

export class HelloExistingResponse
{
    helloList: HelloList;
    helloArray: HelloArray;
    arrayResults: ArrayResult[];
    listResults: ListResult[];
}

export class AllTypes implements IReturn<AllTypes>
{
    id: number;
    nullableId: number;
    byte: number;
    short: number;
    int: number;
    long: number;
    uShort: number;
    uInt: number;
    uLong: number;
    float: number;
    double: number;
    decimal: number;
    string: string;
    dateTime: string;
    timeSpan: string;
    dateTimeOffset: string;
    guid: string;
    char: string;
    keyValuePair: KeyValuePair<string, string>;
    nullableDateTime: string;
    nullableTimeSpan: string;
    stringList: string[];
    stringArray: string[];
    stringMap: { [index:string]: string; };
    intStringMap: { [index:number]: string; };
    subType: SubType;
    point: string;
    // @DataMember(Name="aliasedName")
    originalName: string;
    createResponse() { return new AllTypes(); }
    getTypeName() { return "AllTypes"; }
}

export class HelloAllTypesResponse
{
    result: string;
    allTypes: AllTypes;
    allCollectionTypes: AllCollectionTypes;
}

// @DataContract
export class HelloWithDataContractResponse
{
    // @DataMember(Name="result", Order=1, IsRequired=true, EmitDefaultValue=false)
    result: string;
}

/**
* Description on HelloWithDescriptionResponse type
*/
export class HelloWithDescriptionResponse
{
    result: string;
}

export class HelloWithInheritanceResponse extends HelloResponseBase
{
    result: string;
}

export class HelloWithAlternateReturnResponse extends HelloWithReturnResponse
{
    altResult: string;
}

export class HelloWithRouteResponse
{
    result: string;
}

export class HelloWithTypeResponse
{
    result: HelloType;
}

export class HelloStruct implements IReturn<HelloStruct>
{
    point: string;
    nullablePoint: string;
    createResponse() { return new HelloStruct(); }
    getTypeName() { return "HelloStruct"; }
}

export class HelloSessionResponse
{
    result: AuthUserSession;
}

export class HelloImplementsInterface implements IReturn<HelloImplementsInterface>, ImplementsPoco
{
    name: string;
    createResponse() { return new HelloImplementsInterface(); }
    getTypeName() { return "HelloImplementsInterface"; }
}

export class Request1Response
{
    test: TypeA;
}

export class Request2Response
{
    test: TypeA;
}

export class HelloInnerTypesResponse
{
    innerType: InnerType;
    innerEnum: InnerEnum;
    innerList: InnerTypeItem[];
}

export class CustomUserSession extends AuthUserSession
{
    // @DataMember
    customName: string;

    // @DataMember
    customInfo: string;
}

// @DataContract
export class QueryResponseTemplate<T>
{
    // @DataMember(Order=1)
    offset: number;

    // @DataMember(Order=2)
    total: number;

    // @DataMember(Order=3)
    results: T[];

    // @DataMember(Order=4)
    meta: { [index:string]: string; };

    // @DataMember(Order=5)
    responseStatus: ResponseStatus;
}

export class HelloVerbResponse
{
    result: string;
}

export class EnumResponse
{
    operator: ScopeType;
}

export class ExcludeTestNested
{
    id: number;
}

export class RestrictLocalhost implements IReturn<RestrictLocalhost>
{
    id: number;
    createResponse() { return new RestrictLocalhost(); }
    getTypeName() { return "RestrictLocalhost"; }
}

export class RestrictInternal implements IReturn<RestrictInternal>
{
    id: number;
    createResponse() { return new RestrictInternal(); }
    getTypeName() { return "RestrictInternal"; }
}

export class HelloTuple implements IReturn<HelloTuple>
{
    tuple2: Tuple_2<string, number>;
    tuple3: Tuple_3<string, number, boolean>;
    tuples2: Tuple_2<string,number>[];
    tuples3: Tuple_3<string,number,boolean>[];
    createResponse() { return new HelloTuple(); }
    getTypeName() { return "HelloTuple"; }
}

export class HelloAuthenticatedResponse
{
    version: number;
    sessionId: string;
    userName: string;
    email: string;
    isAuthenticated: boolean;
    responseStatus: ResponseStatus;
}

export class Echo
{
    sentence: string;
}

export class ThrowHttpErrorResponse
{
}

export class ThrowTypeResponse
{
    responseStatus: ResponseStatus;
}

export class ThrowValidationResponse
{
    age: number;
    required: string;
    email: string;
    responseStatus: ResponseStatus;
}

export class acsprofileResponse
{
    profileId: string;
}

export class ReturnedDto
{
    id: number;
}

// @Route("/matchroute/html")
export class MatchesHtml implements IReturn<MatchesHtml>
{
    name: string;
    createResponse() { return new MatchesHtml(); }
    getTypeName() { return "MatchesHtml"; }
}

// @Route("/matchroute/json")
export class MatchesJson implements IReturn<MatchesJson>
{
    name: string;
    createResponse() { return new MatchesJson(); }
    getTypeName() { return "MatchesJson"; }
}

export class TimestampData
{
    timestamp: number;
}

// @Route("/test/html")
export class TestHtml implements IReturn<TestHtml>
{
    name: string;
    createResponse() { return new TestHtml(); }
    getTypeName() { return "TestHtml"; }
}

export class SwaggerComplexResponse
{
    // @DataMember
    // @ApiMember()
    isRequired: boolean;

    // @DataMember
    // @ApiMember(IsRequired=true)
    arrayString: string[];

    // @DataMember
    // @ApiMember()
    arrayInt: number[];

    // @DataMember
    // @ApiMember()
    listString: string[];

    // @DataMember
    // @ApiMember()
    listInt: number[];

    // @DataMember
    // @ApiMember()
    dictionaryString: { [index:string]: string; };
}

/**
* Api GET All
*/
// @Route("/swaggerexamples", "GET")
// @Api(Description="Api GET All")
export class GetSwaggerExamples implements IReturn<GetSwaggerExamples>
{
    get: string;
    createResponse() { return new GetSwaggerExamples(); }
    getTypeName() { return "GetSwaggerExamples"; }
}

/**
* Api GET Id
*/
// @Route("/swaggerexamples/{Id}", "GET")
// @Api(Description="Api GET Id")
export class GetSwaggerExample implements IReturn<GetSwaggerExample>
{
    id: number;
    get: string;
    createResponse() { return new GetSwaggerExample(); }
    getTypeName() { return "GetSwaggerExample"; }
}

/**
* Api POST
*/
// @Route("/swaggerexamples", "POST")
// @Api(Description="Api POST")
export class PostSwaggerExamples implements IReturn<PostSwaggerExamples>
{
    post: string;
    createResponse() { return new PostSwaggerExamples(); }
    getTypeName() { return "PostSwaggerExamples"; }
}

/**
* Api PUT Id
*/
// @Route("/swaggerexamples/{Id}", "PUT")
// @Api(Description="Api PUT Id")
export class PutSwaggerExample implements IReturn<PutSwaggerExample>
{
    id: number;
    get: string;
    createResponse() { return new PutSwaggerExample(); }
    getTypeName() { return "PutSwaggerExample"; }
}

// @Route("/lists", "GET")
export class GetLists implements IReturn<GetLists>
{
    id: string;
    createResponse() { return new GetLists(); }
    getTypeName() { return "GetLists"; }
}

// @DataContract
export class AuthenticateResponse
{
    // @DataMember(Order=1)
    userId: string;

    // @DataMember(Order=2)
    sessionId: string;

    // @DataMember(Order=3)
    userName: string;

    // @DataMember(Order=4)
    displayName: string;

    // @DataMember(Order=5)
    referrerUrl: string;

    // @DataMember(Order=6)
    bearerToken: string;

    // @DataMember(Order=7)
    refreshToken: string;

    // @DataMember(Order=8)
    responseStatus: ResponseStatus;

    // @DataMember(Order=9)
    meta: { [index:string]: string; };
}

// @DataContract
export class AssignRolesResponse
{
    // @DataMember(Order=1)
    allRoles: string[];

    // @DataMember(Order=2)
    allPermissions: string[];

    // @DataMember(Order=3)
    responseStatus: ResponseStatus;
}

// @DataContract
export class UnAssignRolesResponse
{
    // @DataMember(Order=1)
    allRoles: string[];

    // @DataMember(Order=2)
    allPermissions: string[];

    // @DataMember(Order=3)
    responseStatus: ResponseStatus;
}

// @DataContract
export class GetApiKeysResponse
{
    // @DataMember(Order=1)
    results: UserApiKey[];

    // @DataMember(Order=2)
    responseStatus: ResponseStatus;
}

// @DataContract
export class RegisterResponse
{
    // @DataMember(Order=1)
    userId: string;

    // @DataMember(Order=2)
    sessionId: string;

    // @DataMember(Order=3)
    userName: string;

    // @DataMember(Order=4)
    referrerUrl: string;

    // @DataMember(Order=5)
    bearerToken: string;

    // @DataMember(Order=6)
    refreshToken: string;

    // @DataMember(Order=7)
    responseStatus: ResponseStatus;

    // @DataMember(Order=8)
    meta: { [index:string]: string; };
}

// @Route("/anontype")
export class AnonType
{
}

// @Route("/query/requestlogs")
// @Route("/query/requestlogs/{Date}")
export class QueryRequestLogs extends QueryData<RequestLogEntry> implements IReturn<QueryResponse<RequestLogEntry>>, IMeta
{
    date: string;
    viewErrors: boolean;
    createResponse() { return new QueryResponse<RequestLogEntry>(); }
    getTypeName() { return "QueryRequestLogs"; }
}

export class TodayLogs extends QueryData<RequestLogEntry> implements IReturn<QueryResponse<RequestLogEntry>>, IMeta
{
    createResponse() { return new QueryResponse<RequestLogEntry>(); }
    getTypeName() { return "TodayLogs"; }
}

export class TodayErrorLogs extends QueryData<RequestLogEntry> implements IReturn<QueryResponse<RequestLogEntry>>, IMeta
{
    createResponse() { return new QueryResponse<RequestLogEntry>(); }
    getTypeName() { return "TodayErrorLogs"; }
}

export class YesterdayLogs extends QueryData<RequestLogEntry> implements IReturn<QueryResponse<RequestLogEntry>>, IMeta
{
    createResponse() { return new QueryResponse<RequestLogEntry>(); }
    getTypeName() { return "YesterdayLogs"; }
}

export class YesterdayErrorLogs extends QueryData<RequestLogEntry> implements IReturn<QueryResponse<RequestLogEntry>>, IMeta
{
    createResponse() { return new QueryResponse<RequestLogEntry>(); }
    getTypeName() { return "YesterdayErrorLogs"; }
}

// @Route("/query/rockstars")
export class QueryRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryRockstars"; }
}

export class GetEventSubscribers implements IReturn<any>, IGet
{
    channels: string[];
    createResponse() { return new Object(); }
    getTypeName() { return "GetEventSubscribers"; }
}

// @Route("/event-subscribers/{Id}", "POST")
// @DataContract
export class UpdateEventSubscriber implements IReturn<UpdateEventSubscriberResponse>, IPost
{
    // @DataMember(Order=1)
    id: string;

    // @DataMember(Order=2)
    subscribeChannels: string[];

    // @DataMember(Order=3)
    unsubscribeChannels: string[];
    createResponse() { return new UpdateEventSubscriberResponse(); }
    getTypeName() { return "UpdateEventSubscriber"; }
}

// @Route("/changerequest/{Id}")
export class ChangeRequest implements IReturn<ChangeRequestResponse>
{
    id: string;
    createResponse() { return new ChangeRequestResponse(); }
    getTypeName() { return "ChangeRequest"; }
}

// @Route("/compress/{Path*}")
export class CompressFile
{
    path: string;
}

// @Route("/Routing/LeadPost.aspx")
export class LegacyLeadPost
{
    leadType: string;
    myId: number;
}

// @Route("/info/{Id}")
export class Info
{
    id: string;
}

export class CustomHttpError implements IReturn<CustomHttpErrorResponse>
{
    statusCode: number;
    statusDescription: string;
    createResponse() { return new CustomHttpErrorResponse(); }
    getTypeName() { return "CustomHttpError"; }
}

export class CustomFieldHttpError implements IReturn<CustomFieldHttpErrorResponse>
{
    createResponse() { return new CustomFieldHttpErrorResponse(); }
    getTypeName() { return "CustomFieldHttpError"; }
}

export class FallbackRoute
{
    pathInfo: string;
}

export class NoRepeat implements IReturn<NoRepeatResponse>
{
    id: string;
    createResponse() { return new NoRepeatResponse(); }
    getTypeName() { return "NoRepeat"; }
}

export class BatchThrows implements IReturn<BatchThrowsResponse>
{
    id: number;
    name: string;
    createResponse() { return new BatchThrowsResponse(); }
    getTypeName() { return "BatchThrows"; }
}

export class BatchThrowsAsync implements IReturn<BatchThrowsResponse>
{
    id: number;
    name: string;
    createResponse() { return new BatchThrowsResponse(); }
    getTypeName() { return "BatchThrowsAsync"; }
}

// @Route("/code/object", "GET")
export class ObjectId implements IReturn<ObjectDesignResponse>
{
    objectName: string;
    createResponse() { return new ObjectDesignResponse(); }
    getTypeName() { return "ObjectId"; }
}

export class MetadataTest implements IReturn<MetadataTestResponse>
{
    id: number;
    createResponse() { return new MetadataTestResponse(); }
    getTypeName() { return "MetadataTest"; }
}

// @Route("/example", "GET")
// @DataContract
export class GetExample implements IReturn<GetExampleResponse>
{
    createResponse() { return new GetExampleResponse(); }
    getTypeName() { return "GetExample"; }
}

export class MetadataRequest implements IReturn<AutoQueryMetadataResponse>
{
    metadataType: MetadataType;
    createResponse() { return new AutoQueryMetadataResponse(); }
    getTypeName() { return "MetadataRequest"; }
}

export class ExcludeMetadataProperty
{
    id: number;
}

// @Route("/namedconnection")
export class NamedConnection
{
    emailAddresses: string;
}

/**
* Description for HelloACodeGenTest
*/
export class HelloACodeGenTest implements IReturn<HelloACodeGenTestResponse>
{
    /**
    * Description for FirstField
    */
    firstField: number;
    secondFields: string[];
    createResponse() { return new HelloACodeGenTestResponse(); }
    getTypeName() { return "HelloACodeGenTest"; }
}

export class HelloInService implements IReturn<HelloResponse>
{
    name: string;
    createResponse() { return new HelloResponse(); }
    getTypeName() { return "NativeTypesTestService.HelloInService"; }
}

// @Route("/hello")
// @Route("/hello/{Name}")
export class Hello implements IReturn<HelloResponse>
{
    // @Required()
    name: string;

    title: string;
    createResponse() { return new HelloResponse(); }
    getTypeName() { return "Hello"; }
}

/**
* Description on HelloAll type
*/
// @DataContract
export class HelloAnnotated implements IReturn<HelloAnnotatedResponse>
{
    // @DataMember
    name: string;
    createResponse() { return new HelloAnnotatedResponse(); }
    getTypeName() { return "HelloAnnotated"; }
}

export class HelloWithNestedClass implements IReturn<HelloResponse>
{
    name: string;
    nestedClassProp: NestedClass;
    createResponse() { return new HelloResponse(); }
    getTypeName() { return "HelloWithNestedClass"; }
}

export class HelloReturnList implements IReturn<Array<OnlyInReturnListArg>>
{
    names: string[];
    createResponse() { return new Array<OnlyInReturnListArg>(); }
    getTypeName() { return "HelloReturnList"; }
}

export class HelloExisting implements IReturn<HelloExistingResponse>
{
    names: string[];
    createResponse() { return new HelloExistingResponse(); }
    getTypeName() { return "HelloExisting"; }
}

export class HelloWithEnum
{
    enumProp: EnumType;
    enumWithValues: EnumWithValues;
    nullableEnumProp: EnumType;
    enumFlags: EnumFlags;
}

export class RestrictedAttributes
{
    id: number;
    name: string;
    hello: Hello;
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
    // @DataMember
    // @Required()
    id: number;

    /**
    * Range Description
    */
    // @DataMember(Name="Aliased")
    // @ApiMember(DataType="double", Description="Range Description", IsRequired=true, ParameterType="path")
    range: number;
}

/**
* Multi Line Class
*/
// @Api(Description="Multi Line Class")
export class HelloMultiline
{
    /**
    * Multi Line Property
    */
    // @ApiMember(Description="Multi Line Property")
    overflow: string;
}

export class HelloAllTypes implements IReturn<HelloAllTypesResponse>
{
    name: string;
    allTypes: AllTypes;
    allCollectionTypes: AllCollectionTypes;
    createResponse() { return new HelloAllTypesResponse(); }
    getTypeName() { return "HelloAllTypes"; }
}

export class HelloString implements IReturn<string>
{
    name: string;
    createResponse() { return ""; }
    getTypeName() { return "HelloString"; }
}

export class HelloVoid implements IReturnVoid
{
    name: string;
    createResponse() {}
    getTypeName() { return "HelloVoid"; }
}

// @DataContract
export class HelloWithDataContract implements IReturn<HelloWithDataContractResponse>
{
    // @DataMember(Name="name", Order=1, IsRequired=true, EmitDefaultValue=false)
    name: string;

    // @DataMember(Name="id", Order=2, EmitDefaultValue=false)
    id: number;
    createResponse() { return new HelloWithDataContractResponse(); }
    getTypeName() { return "HelloWithDataContract"; }
}

/**
* Description on HelloWithDescription type
*/
export class HelloWithDescription implements IReturn<HelloWithDescriptionResponse>
{
    name: string;
    createResponse() { return new HelloWithDescriptionResponse(); }
    getTypeName() { return "HelloWithDescription"; }
}

export class HelloWithInheritance extends HelloBase implements IReturn<HelloWithInheritanceResponse>
{
    name: string;
    createResponse() { return new HelloWithInheritanceResponse(); }
    getTypeName() { return "HelloWithInheritance"; }
}

export class HelloWithGenericInheritance extends HelloBase_1<Poco>
{
    result: string;
}

export class HelloWithGenericInheritance2 extends HelloBase_1<Hello>
{
    result: string;
}

export class HelloWithNestedInheritance extends HelloBase_1<Item>
{
}

export class HelloWithListInheritance extends Array<InheritedItem>
{
}

export class HelloWithReturn implements IReturn<HelloWithAlternateReturnResponse>
{
    name: string;
    createResponse() { return new HelloWithAlternateReturnResponse(); }
    getTypeName() { return "HelloWithReturn"; }
}

// @Route("/helloroute")
export class HelloWithRoute implements IReturn<HelloWithRouteResponse>
{
    name: string;
    createResponse() { return new HelloWithRouteResponse(); }
    getTypeName() { return "HelloWithRoute"; }
}

export class HelloWithType implements IReturn<HelloWithTypeResponse>
{
    name: string;
    createResponse() { return new HelloWithTypeResponse(); }
    getTypeName() { return "HelloWithType"; }
}

export class HelloSession implements IReturn<HelloSessionResponse>
{
    createResponse() { return new HelloSessionResponse(); }
    getTypeName() { return "HelloSession"; }
}

export class HelloInterface
{
    poco: IPoco;
    emptyInterface: IEmptyInterface;
    emptyClass: EmptyClass;
    value: string;
}

export class Request1 implements IReturn<Request1Response>
{
    test: TypeA;
    createResponse() { return new Request1Response(); }
    getTypeName() { return "Request1"; }
}

export class Request2 implements IReturn<Request2Response>
{
    test: TypeA;
    createResponse() { return new Request2Response(); }
    getTypeName() { return "Request2"; }
}

export class HelloInnerTypes implements IReturn<HelloInnerTypesResponse>
{
    createResponse() { return new HelloInnerTypesResponse(); }
    getTypeName() { return "HelloInnerTypes"; }
}

export class GetUserSession implements IReturn<CustomUserSession>
{
    createResponse() { return new CustomUserSession(); }
    getTypeName() { return "GetUserSession"; }
}

export class QueryTemplate implements IReturn<QueryResponseTemplate<Poco>>
{
    createResponse() { return new QueryResponseTemplate<Poco>(); }
    getTypeName() { return "QueryTemplate"; }
}

export class HelloReserved
{
    class: string;
    type: string;
    extension: string;
}

export class HelloDictionary implements IReturn<any>
{
    key: string;
    value: string;
    createResponse() { return new Object(); }
    getTypeName() { return "HelloDictionary"; }
}

export class HelloBuiltin
{
    dayOfWeek: DayOfWeek;
}

export class HelloGet implements IReturn<HelloVerbResponse>, IGet
{
    id: number;
    createResponse() { return new HelloVerbResponse(); }
    getTypeName() { return "HelloGet"; }
}

export class HelloPost extends HelloBase implements IReturn<HelloVerbResponse>, IPost
{
    createResponse() { return new HelloVerbResponse(); }
    getTypeName() { return "HelloPost"; }
}

export class HelloPut implements IReturn<HelloVerbResponse>, IPut
{
    id: number;
    createResponse() { return new HelloVerbResponse(); }
    getTypeName() { return "HelloPut"; }
}

export class HelloDelete implements IReturn<HelloVerbResponse>, IDelete
{
    id: number;
    createResponse() { return new HelloVerbResponse(); }
    getTypeName() { return "HelloDelete"; }
}

export class HelloPatch implements IReturn<HelloVerbResponse>, IPatch
{
    id: number;
    createResponse() { return new HelloVerbResponse(); }
    getTypeName() { return "HelloPatch"; }
}

export class HelloReturnVoid implements IReturnVoid
{
    id: number;
    createResponse() {}
    getTypeName() { return "HelloReturnVoid"; }
}

export class EnumRequest implements IReturn<EnumResponse>, IPut
{
    operator: ScopeType;
    createResponse() { return new EnumResponse(); }
    getTypeName() { return "EnumRequest"; }
}

export class ExcludeTest1 implements IReturn<ExcludeTestNested>
{
    createResponse() { return new ExcludeTestNested(); }
    getTypeName() { return "ExcludeTest1"; }
}

export class ExcludeTest2 implements IReturn<string>
{
    excludeTestNested: ExcludeTestNested;
    createResponse() { return ""; }
    getTypeName() { return "ExcludeTest2"; }
}

export class HelloAuthenticated implements IReturn<HelloAuthenticatedResponse>, IHasSessionId
{
    sessionId: string;
    version: number;
    createResponse() { return new HelloAuthenticatedResponse(); }
    getTypeName() { return "HelloAuthenticated"; }
}

/**
* Echoes a sentence
*/
// @Route("/echoes", "POST")
// @Api(Description="Echoes a sentence")
export class Echoes implements IReturn<Echo>
{
    /**
    * The sentence to echo.
    */
    // @ApiMember(DataType="string", Description="The sentence to echo.", IsRequired=true, Name="Sentence", ParameterType="form")
    sentence: string;
    createResponse() { return new Echo(); }
    getTypeName() { return "Echoes"; }
}

export class CachedEcho implements IReturn<Echo>
{
    reload: boolean;
    sentence: string;
    createResponse() { return new Echo(); }
    getTypeName() { return "CachedEcho"; }
}

export class AsyncTest implements IReturn<Echo>
{
    createResponse() { return new Echo(); }
    getTypeName() { return "AsyncTest"; }
}

// @Route("/throwhttperror/{Status}")
export class ThrowHttpError implements IReturn<ThrowHttpErrorResponse>
{
    status: number;
    message: string;
    createResponse() { return new ThrowHttpErrorResponse(); }
    getTypeName() { return "ThrowHttpError"; }
}

// @Route("/throw404")
// @Route("/throw404/{Message}")
export class Throw404
{
    message: string;
}

// @Route("/return404")
export class Return404
{
}

// @Route("/return404result")
export class Return404Result
{
}

// @Route("/throw/{Type}")
export class ThrowType implements IReturn<ThrowTypeResponse>
{
    type: string;
    message: string;
    createResponse() { return new ThrowTypeResponse(); }
    getTypeName() { return "ThrowType"; }
}

// @Route("/throwvalidation")
export class ThrowValidation implements IReturn<ThrowValidationResponse>
{
    age: number;
    required: string;
    email: string;
    createResponse() { return new ThrowValidationResponse(); }
    getTypeName() { return "ThrowValidation"; }
}

// @Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")
// @Route("/api/acsprofiles/{profileId}")
export class ACSProfile implements IReturn<acsprofileResponse>, IHasVersion, IHasSessionId
{
    profileId: string;
    // @Required()
    // @StringLength(20)
    shortName: string;

    // @StringLength(60)
    longName: string;

    // @StringLength(20)
    regionId: string;

    // @StringLength(20)
    groupId: string;

    // @StringLength(12)
    deviceID: string;

    lastUpdated: string;
    enabled: boolean;
    version: number;
    sessionId: string;
    createResponse() { return new acsprofileResponse(); }
    getTypeName() { return "ACSProfile"; }
}

// @Route("/return/string")
export class ReturnString implements IReturn<string>
{
    data: string;
    createResponse() { return ""; }
    getTypeName() { return "ReturnString"; }
}

// @Route("/return/bytes")
export class ReturnBytes implements IReturn<Uint8Array>
{
    data: Uint8Array;
    createResponse() { return new Uint8Array(0); }
    getTypeName() { return "ReturnBytes"; }
}

// @Route("/return/stream")
export class ReturnStream implements IReturn<Blob>
{
    data: Uint8Array;
    createResponse() { return new Blob(); }
    getTypeName() { return "ReturnStream"; }
}

// @Route("/Request1/", "GET")
export class GetRequest1 implements IReturn<Array<ReturnedDto>>, IGet
{
    createResponse() { return new Array<ReturnedDto>(); }
    getTypeName() { return "GetRequest1"; }
}

// @Route("/Request3", "GET")
export class GetRequest2 implements IReturn<ReturnedDto>, IGet
{
    createResponse() { return new ReturnedDto(); }
    getTypeName() { return "GetRequest2"; }
}

// @Route("/matchlast/{Id}")
export class MatchesLastInt
{
    id: number;
}

// @Route("/matchlast/{Slug}")
export class MatchesNotLastInt
{
    slug: string;
}

// @Route("/matchregex/{Id}")
export class MatchesId
{
    id: number;
}

// @Route("/matchregex/{Slug}")
export class MatchesSlug
{
    slug: string;
}

// @Route("/{Version}/userdata", "GET")
export class SwaggerVersionTest
{
    version: string;
}

// @Route("/test/errorview")
export class TestErrorView
{
    id: string;
}

// @Route("/timestamp", "GET")
export class GetTimestamp implements IReturn<TimestampData>
{
    createResponse() { return new TimestampData(); }
    getTypeName() { return "GetTimestamp"; }
}

export class TestMiniverView
{
}

// @Route("/testexecproc")
export class TestExecProc
{
}

// @Route("/files/{Path*}")
export class GetFile
{
    path: string;
}

// @Route("/test/html2")
export class TestHtml2
{
    name: string;
}

// @Route("/views/request")
export class ViewRequest
{
    name: string;
}

// @Route("/index")
export class IndexPage
{
    pathInfo: string;
}

// @Route("/return/text")
export class ReturnText
{
    text: string;
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
    /**
    * Color Description
    */
    // @DataMember
    // @ApiMember(DataType="string", Description="Color Description", IsRequired=true, ParameterType="path")
    name: string;

    // @DataMember
    // @ApiMember()
    color: MyColor;

    /**
    * Aliased Description
    */
    // @DataMember(Name="Aliased")
    // @ApiMember(DataType="string", Description="Aliased Description", IsRequired=true)
    original: string;

    /**
    * Not Aliased Description
    */
    // @DataMember
    // @ApiMember(DataType="string", Description="Not Aliased Description", IsRequired=true)
    notAliased: string;

    /**
    * Format as password
    */
    // @DataMember
    // @ApiMember(DataType="password", Description="Format as password")
    password: string;

    // @DataMember
    // @ApiMember(AllowMultiple=true)
    myDateBetween: string[];

    /**
    * Nested model 1
    */
    // @DataMember
    // @ApiMember(DataType="SwaggerNestedModel", Description="Nested model 1")
    nestedModel1: SwaggerNestedModel;

    /**
    * Nested model 2
    */
    // @DataMember
    // @ApiMember(DataType="SwaggerNestedModel2", Description="Nested model 2")
    nestedModel2: SwaggerNestedModel2;
}

// @Route("/swaggertest2", "POST")
export class SwaggerTest2
{
    // @ApiMember()
    myEnumProperty: MyEnum;

    // @ApiMember(DataType="string", IsRequired=true, Name="Token", ParameterType="header")
    token: string;
}

// @Route("/swagger-complex", "POST")
export class SwaggerComplex implements IReturn<SwaggerComplexResponse>
{
    // @DataMember
    // @ApiMember()
    isRequired: boolean;

    // @DataMember
    // @ApiMember(IsRequired=true)
    arrayString: string[];

    // @DataMember
    // @ApiMember()
    arrayInt: number[];

    // @DataMember
    // @ApiMember()
    listString: string[];

    // @DataMember
    // @ApiMember()
    listInt: number[];

    // @DataMember
    // @ApiMember()
    dictionaryString: { [index:string]: string; };
    createResponse() { return new SwaggerComplexResponse(); }
    getTypeName() { return "SwaggerComplex"; }
}

// @Route("/swaggerpost/{Required1}", "GET")
// @Route("/swaggerpost/{Required1}/{Optional1}", "GET")
// @Route("/swaggerpost", "POST")
export class SwaggerPostTest implements IReturn<HelloResponse>
{
    // @ApiMember(Verb="POST")
    // @ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}", Verb="GET")
    // @ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}/{Optional1}", Verb="GET")
    required1: string;

    // @ApiMember(Verb="POST")
    // @ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}/{Optional1}", Verb="GET")
    optional1: string;
    createResponse() { return new HelloResponse(); }
    getTypeName() { return "SwaggerPostTest"; }
}

// @Route("/swaggerpost2/{Required1}/{Required2}", "GET")
// @Route("/swaggerpost2/{Required1}/{Required2}/{Optional1}", "GET")
// @Route("/swaggerpost2", "POST")
export class SwaggerPostTest2 implements IReturn<HelloResponse>
{
    // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}", Verb="GET")
    // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")
    required1: string;

    // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}", Verb="GET")
    // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")
    required2: string;

    // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")
    optional1: string;
    createResponse() { return new HelloResponse(); }
    getTypeName() { return "SwaggerPostTest2"; }
}

// @Route("/swagger/multiattrtest", "POST")
// @ApiResponse(Description="Code 1", StatusCode=400)
// @ApiResponse(Description="Code 2", StatusCode=402)
// @ApiResponse(Description="Code 3", StatusCode=401)
export class SwaggerMultiApiResponseTest implements IReturnVoid
{
    createResponse() {}
    getTypeName() { return "SwaggerMultiApiResponseTest"; }
}

// @Route("/dynamically/registered/{Name}")
export class DynamicallyRegistered
{
    name: string;
}

// @Route("/auth")
// @Route("/auth/{provider}")
// @Route("/authenticate")
// @Route("/authenticate/{provider}")
// @DataContract
export class Authenticate implements IReturn<AuthenticateResponse>, IPost, IMeta
{
    // @DataMember(Order=1)
    provider: string;

    // @DataMember(Order=2)
    state: string;

    // @DataMember(Order=3)
    oauth_token: string;

    // @DataMember(Order=4)
    oauth_verifier: string;

    // @DataMember(Order=5)
    userName: string;

    // @DataMember(Order=6)
    password: string;

    // @DataMember(Order=7)
    rememberMe: boolean;

    // @DataMember(Order=8)
    continue: string;

    // @DataMember(Order=9)
    nonce: string;

    // @DataMember(Order=10)
    uri: string;

    // @DataMember(Order=11)
    response: string;

    // @DataMember(Order=12)
    qop: string;

    // @DataMember(Order=13)
    nc: string;

    // @DataMember(Order=14)
    cnonce: string;

    // @DataMember(Order=15)
    useTokenCookie: boolean;

    // @DataMember(Order=16)
    accessToken: string;

    // @DataMember(Order=17)
    accessTokenSecret: string;

    // @DataMember(Order=18)
    meta: { [index:string]: string; };
    createResponse() { return new AuthenticateResponse(); }
    getTypeName() { return "Authenticate"; }
}

// @Route("/assignroles")
// @DataContract
export class AssignRoles implements IReturn<AssignRolesResponse>, IPost
{
    // @DataMember(Order=1)
    userName: string;

    // @DataMember(Order=2)
    permissions: string[];

    // @DataMember(Order=3)
    roles: string[];
    createResponse() { return new AssignRolesResponse(); }
    getTypeName() { return "AssignRoles"; }
}

// @Route("/unassignroles")
// @DataContract
export class UnAssignRoles implements IReturn<UnAssignRolesResponse>, IPost
{
    // @DataMember(Order=1)
    userName: string;

    // @DataMember(Order=2)
    permissions: string[];

    // @DataMember(Order=3)
    roles: string[];
    createResponse() { return new UnAssignRolesResponse(); }
    getTypeName() { return "UnAssignRoles"; }
}

// @Route("/apikeys")
// @Route("/apikeys/{Environment}")
// @DataContract
export class GetApiKeys implements IReturn<GetApiKeysResponse>, IGet
{
    // @DataMember(Order=1)
    environment: string;
    createResponse() { return new GetApiKeysResponse(); }
    getTypeName() { return "GetApiKeys"; }
}

// @Route("/apikeys/regenerate")
// @Route("/apikeys/regenerate/{Environment}")
// @DataContract
export class RegenerateApiKeys implements IReturn<GetApiKeysResponse>, IPost
{
    // @DataMember(Order=1)
    environment: string;
    createResponse() { return new GetApiKeysResponse(); }
    getTypeName() { return "RegenerateApiKeys"; }
}

// @Route("/register")
// @DataContract
export class Register implements IReturn<RegisterResponse>, IPost
{
    // @DataMember(Order=1)
    userName: string;

    // @DataMember(Order=2)
    firstName: string;

    // @DataMember(Order=3)
    lastName: string;

    // @DataMember(Order=4)
    displayName: string;

    // @DataMember(Order=5)
    email: string;

    // @DataMember(Order=6)
    password: string;

    // @DataMember(Order=7)
    autoLogin: boolean;

    // @DataMember(Order=8)
    continue: string;
    createResponse() { return new RegisterResponse(); }
    getTypeName() { return "Register"; }
}

// @Route("/pgsql/rockstars")
export class QueryPostgresRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryPostgresRockstars"; }
}

// @Route("/pgsql/pgrockstars")
export class QueryPostgresPgRockstars extends QueryDb_1<PgRockstar> implements IReturn<QueryResponse<PgRockstar>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<PgRockstar>(); }
    getTypeName() { return "QueryPostgresPgRockstars"; }
}

export class QueryRockstarsConventions extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    ids: number[];
    ageOlderThan: number;
    ageGreaterThanOrEqualTo: number;
    ageGreaterThan: number;
    greaterThanAge: number;
    firstNameStartsWith: string;
    lastNameEndsWith: string;
    lastNameContains: string;
    rockstarAlbumNameContains: string;
    rockstarIdAfter: number;
    rockstarIdOnOrAfter: number;
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryRockstarsConventions"; }
}

// @AutoQueryViewer(Description="Use this option to search for Rockstars!", Title="Search for Rockstars")
export class QueryCustomRockstars extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<CustomRockstar>(); }
    getTypeName() { return "QueryCustomRockstars"; }
}

// @Route("/customrockstars")
export class QueryRockstarAlbums extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>, IMeta
{
    age: number;
    rockstarAlbumName: string;
    createResponse() { return new QueryResponse<CustomRockstar>(); }
    getTypeName() { return "QueryRockstarAlbums"; }
}

export class QueryRockstarAlbumsImplicit extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>, IMeta
{
    createResponse() { return new QueryResponse<CustomRockstar>(); }
    getTypeName() { return "QueryRockstarAlbumsImplicit"; }
}

export class QueryRockstarAlbumsLeftJoin extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>, IMeta
{
    age: number;
    albumName: string;
    createResponse() { return new QueryResponse<CustomRockstar>(); }
    getTypeName() { return "QueryRockstarAlbumsLeftJoin"; }
}

export class QueryOverridedRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryOverridedRockstars"; }
}

export class QueryOverridedCustomRockstars extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<CustomRockstar>(); }
    getTypeName() { return "QueryOverridedCustomRockstars"; }
}

// @Route("/query-custom/rockstars")
export class QueryFieldRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    firstName: string;
    firstNames: string[];
    age: number;
    firstNameCaseInsensitive: string;
    firstNameStartsWith: string;
    lastNameEndsWith: string;
    firstNameBetween: string[];
    orLastName: string;
    firstNameContainsMulti: string[];
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryFieldRockstars"; }
}

export class QueryFieldRockstarsDynamic extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryFieldRockstarsDynamic"; }
}

export class QueryRockstarsFilter extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryRockstarsFilter"; }
}

export class QueryCustomRockstarsFilter extends QueryDb_2<Rockstar, CustomRockstar> implements IReturn<QueryResponse<CustomRockstar>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<CustomRockstar>(); }
    getTypeName() { return "QueryCustomRockstarsFilter"; }
}

export class QueryRockstarsIFilter extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta, IFilterRockstars
{
    age: number;
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryRockstarsIFilter"; }
}

// @Route("/OrRockstars")
export class QueryOrRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    age: number;
    firstName: string;
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryOrRockstars"; }
}

export class QueryGetRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    ids: number[];
    ages: number[];
    firstNames: string[];
    idsBetween: number[];
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryGetRockstars"; }
}

export class QueryGetRockstarsDynamic extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryGetRockstarsDynamic"; }
}

// @Route("/movies/search")
export class SearchMovies extends QueryDb_1<Movie> implements IReturn<QueryResponse<Movie>>, IMeta
{
    createResponse() { return new QueryResponse<Movie>(); }
    getTypeName() { return "SearchMovies"; }
}

// @Route("/movies")
export class QueryMovies extends QueryDb_1<Movie> implements IReturn<QueryResponse<Movie>>, IMeta
{
    ids: number[];
    imdbIds: string[];
    ratings: string[];
    createResponse() { return new QueryResponse<Movie>(); }
    getTypeName() { return "QueryMovies"; }
}

export class StreamMovies extends QueryDb_1<Movie> implements IReturn<QueryResponse<Movie>>, IMeta
{
    ratings: string[];
    createResponse() { return new QueryResponse<Movie>(); }
    getTypeName() { return "StreamMovies"; }
}

export class QueryUnknownRockstars extends QueryDb_1<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    unknownInt: number;
    unknownProperty: string;
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryUnknownRockstars"; }
}

// @Route("/query/rockstar-references")
export class QueryRockstarsWithReferences extends QueryDb_1<RockstarReference> implements IReturn<QueryResponse<RockstarReference>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<RockstarReference>(); }
    getTypeName() { return "QueryRockstarsWithReferences"; }
}

export class QueryPocoBase extends QueryDb_1<OnlyDefinedInGenericType> implements IReturn<QueryResponse<OnlyDefinedInGenericType>>, IMeta
{
    id: number;
    createResponse() { return new QueryResponse<OnlyDefinedInGenericType>(); }
    getTypeName() { return "QueryPocoBase"; }
}

export class QueryPocoIntoBase extends QueryDb_2<OnlyDefinedInGenericTypeFrom, OnlyDefinedInGenericTypeInto> implements IReturn<QueryResponse<OnlyDefinedInGenericTypeInto>>, IMeta
{
    id: number;
    createResponse() { return new QueryResponse<OnlyDefinedInGenericTypeInto>(); }
    getTypeName() { return "QueryPocoIntoBase"; }
}

// @Route("/query/alltypes")
export class QueryAllTypes extends QueryDb_1<AllTypes> implements IReturn<QueryResponse<AllTypes>>, IMeta
{
    createResponse() { return new QueryResponse<AllTypes>(); }
    getTypeName() { return "QueryAllTypes"; }
}

// @Route("/querydata/rockstars")
export class QueryDataRockstars extends QueryData<Rockstar> implements IReturn<QueryResponse<Rockstar>>, IMeta
{
    age: number;
    createResponse() { return new QueryResponse<Rockstar>(); }
    getTypeName() { return "QueryDataRockstars"; }
}
