/* Options:
Date: 2026-09-02 11:42:29
Version: 10.15
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: https://localhost:5001

//GlobalNamespace: 
//MakePropertiesOptional: False
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: 
*/

// @ts-nocheck

export interface IReturn<T>
{
    createResponse(): T;
}

export interface IReturnVoid
{
    createResponse(): void;
}

export interface IGet
{
}

export interface IPost
{
}

export interface IHasSessionId
{
    sessionId?: string;
}

export interface IHasBearerToken
{
    bearerToken?: string;
}

export interface ICreateDb<Table>
{
}

export interface IPatchDb<Table>
{
}

export interface IPut
{
}

export interface IPatch
{
}

export interface IDelete
{
}

export interface IDeleteDb<Table>
{
}

export interface IUpdateDb<Table>
{
}

export enum JobApplicationStatus
{
    Applied = 'Applied',
    PhoneScreening = 'PhoneScreening',
    PhoneScreeningCompleted = 'PhoneScreeningCompleted',
    Interview = 'Interview',
    InterviewCompleted = 'InterviewCompleted',
    Offer = 'Offer',
    Disqualified = 'Disqualified',
}

// @DataContract
export class AuditBase
{
    // @DataMember(Order=1)
    public createdDate: string;

    // @DataMember(Order=2)
    // @Required()
    public createdBy: string;

    // @DataMember(Order=3)
    public modifiedDate: string;

    // @DataMember(Order=4)
    // @Required()
    public modifiedBy: string;

    // @DataMember(Order=5)
    public deletedDate?: string;

    // @DataMember(Order=6)
    public deletedBy?: string;

    public constructor(init?: Partial<AuditBase>) { (Object as any).assign(this, init); }
}

export class IdentityUser_1<TKey>
{
    public id: TKey;
    public userName?: string;
    public normalizedUserName?: string;
    public email?: string;
    public normalizedEmail?: string;
    public emailConfirmed: boolean;
    public passwordHash?: string;
    public securityStamp?: string;
    public concurrencyStamp?: string;
    public phoneNumber?: string;
    public phoneNumberConfirmed: boolean;
    public twoFactorEnabled: boolean;
    public lockoutEnd?: string;
    public lockoutEnabled: boolean;
    public accessFailedCount: number;

    public constructor(init?: Partial<IdentityUser_1<TKey>>) { (Object as any).assign(this, init); }
}

export class IdentityUser extends IdentityUser_1<string>
{

    public constructor(init?: Partial<IdentityUser>) { super(init); (Object as any).assign(this, init); }
}

export class ApplicationUser extends IdentityUser
{
    public firstName?: string;
    public lastName?: string;
    public displayName?: string;
    public profileUrl?: string;
    public refreshToken?: string;
    public refreshTokenExpiry?: string;

    public constructor(init?: Partial<ApplicationUser>) { super(init); (Object as any).assign(this, init); }
}

export class PhoneScreen extends AuditBase
{
    public id: number;
    // @References("typeof(MyApp.Data.ApplicationUser)")
    public applicationUserId: string;

    public applicationUser: ApplicationUser;
    // @References("typeof(TalentBlazor.ServiceModel.JobApplication)")
    public jobApplicationId: number;

    public applicationStatus?: JobApplicationStatus;
    // @StringLength(2147483647)
    public notes: string;

    public constructor(init?: Partial<PhoneScreen>) { super(init); (Object as any).assign(this, init); }
}

export class Interview extends AuditBase
{
    public id: number;
    public bookingTime: string;
    // @References("typeof(TalentBlazor.ServiceModel.JobApplication)")
    public jobApplicationId: number;

    // @References("typeof(MyApp.Data.ApplicationUser)")
    public applicationUserId: string;

    public applicationUser: ApplicationUser;
    public applicationStatus?: JobApplicationStatus;
    // @StringLength(2147483647)
    public notes: string;

    public constructor(init?: Partial<Interview>) { super(init); (Object as any).assign(this, init); }
}

export class JobOffer extends AuditBase
{
    public id: number;
    public salaryOffer: number;
    // @References("typeof(TalentBlazor.ServiceModel.JobApplication)")
    public jobApplicationId: number;

    // @References("typeof(MyApp.Data.ApplicationUser)")
    public applicationUserId: string;

    public applicationUser: ApplicationUser;
    // @StringLength(2147483647)
    public notes: string;

    public constructor(init?: Partial<JobOffer>) { super(init); (Object as any).assign(this, init); }
}

export class OrderItemOption
{
    /** @description Option group from the menu, e.g. Milks, Syrups, Sweeteners or Toppings */
    // @Validate(Validator="NotEmpty")
    public type: string;

    /** @description Exact option name from that menu option group */
    // @Validate(Validator="NotEmpty")
    public name: string;

    /** @description Optional quantity label: no, light, regular or extra. Use only where the menu allows quantity */
    public quantity?: string;

    public constructor(init?: Partial<OrderItemOption>) { (Object as any).assign(this, init); }
}

export class OrderItemRequest
{
    /** @description Product ID returned by GetCoffeeShopMenu */
    // @Validate(Validator="GreaterThan(0)")
    public productId: number;

    /** @description Number of this configured item to order */
    // @Validate(Validator="GreaterThan(0)")
    public quantity: number;

    /** @description Exact size supported by the product category; omit to use its default */
    public size?: string;
    /** @description Exact temperature supported by the product category; omit to use its default */
    public temperature?: string;
    /** @description Requested customizations. Each option must be valid for the product category */
    public options: OrderItemOption[] = [];

    public constructor(init?: Partial<OrderItemRequest>) { (Object as any).assign(this, init); }
}

export class SubType
{
    public id: number;
    public name: string;

    public constructor(init?: Partial<SubType>) { (Object as any).assign(this, init); }
}

export class Data1
{
    public value: number;
    public optionalValue?: number;
    public text: string;
    public optionalText?: string;
    public texts: string[] = [];
    public optionalTexts?: string[];

    public constructor(init?: Partial<Data1>) { (Object as any).assign(this, init); }
}

export class Data2
{
    // @Required()
    public value: number;

    // @Required()
    public optionalValue: number;

    // @Required()
    public text: string;

    // @Required()
    public optionalText: string;

    // @Required()
    public texts: string[] = [];

    // @Required()
    public optionalTexts: string[] = [];

    public constructor(init?: Partial<Data2>) { (Object as any).assign(this, init); }
}

export class Data3
{
    public value: number;
    public optionalValue?: number;
    // @Required()
    public text: string;

    public text2: string;
    // @Required()
    public nText: string;

    public nText2?: string;

    public constructor(init?: Partial<Data3>) { (Object as any).assign(this, init); }
}

export enum Colors
{
    Transparent = 'Transparent',
    Red = 'Red',
    Green = 'Green',
    Blue = 'Blue',
}

export class Attachment
{
    public fileName: string;
    public filePath: string;
    public contentType: string;
    public contentLength: number;

    public constructor(init?: Partial<Attachment>) { (Object as any).assign(this, init); }
}

export class BillingItem
{
    public name: string;

    public constructor(init?: Partial<BillingItem>) { (Object as any).assign(this, init); }
}

export class PagedRequest
{
    public page: number;
    public pageSize: number;

    public constructor(init?: Partial<PagedRequest>) { (Object as any).assign(this, init); }
}

export class PagedAndOrderedRequest extends PagedRequest
{
    /** @description Comma- or semicolon separated list of fields to sort by. To change sort order add a '-' in front of the field */
    // @ApiMember(DataType="string", Description="Comma- or semicolon separated list of fields to sort by. To change sort order add a '-' in front of the field", Name="OrderBy", ParameterType="query", Verb="GET")
    public orderBy: string;

    public constructor(init?: Partial<PagedAndOrderedRequest>) { super(init); (Object as any).assign(this, init); }
}

export class OptionalClass
{
    public id: number;

    public constructor(init?: Partial<OptionalClass>) { (Object as any).assign(this, init); }
}

export enum OptionalEnum
{
    Value1 = 'Value1',
}

export class KeyValuePair<TKey, TValue>
{
    public key: TKey;
    public value: TValue;

    public constructor(init?: Partial<KeyValuePair<TKey, TValue>>) { (Object as any).assign(this, init); }
}

export class Poco
{
    public name: string;

    public constructor(init?: Partial<Poco>) { (Object as any).assign(this, init); }
}

export enum EnumType
{
    Value1 = 'Value1',
    Value2 = 'Value2',
    Value3 = 'Value3',
}

// @Flags()
export enum EnumTypeFlags
{
    Value1 = 0,
    Value2 = 1,
    Value3 = 2,
}

export enum EnumWithValues
{
    None = 'None',
    Value1 = 'Member 1',
    Value2 = 'Value2',
}

// @Flags()
export enum EnumFlags
{
    Value0 = 0,
    Value1 = 1,
    Value2 = 2,
    Value3 = 4,
    Value123 = 7,
}

export enum EnumAsInt
{
    Value1 = 1000,
    Value2 = 2000,
    Value3 = 3000,
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

export enum EnumStyleMembers
{
    Lower = 'lower',
    Upper = 'UPPER',
    PascalCase = 'PascalCase',
    CamelCase = 'camelCase',
    CamelUpper = 'camelUPPER',
    PascalUpper = 'PascalUPPER',
}

export class AllTypesBase
{
    public id: number;
    public nullableId?: number;
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
    public nullableDateTime?: string;
    public nullableTimeSpan?: string;
    public stringList: string[] = [];
    public stringArray: string[] = [];
    public stringMap: { [index:string]: string; } = {};
    public intStringMap: { [index:number]: string; } = {};
    public subType: SubType;

    public constructor(init?: Partial<AllTypesBase>) { (Object as any).assign(this, init); }
}

export class HelloBase_1<T>
{
    public items: T[] = [];
    public counts: number[] = [];

    public constructor(init?: Partial<HelloBase_1<T>>) { (Object as any).assign(this, init); }
}

export class HelloBase
{
    public id: number;

    public constructor(init?: Partial<HelloBase>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AiContent
{
    /** @description The type of the content part. */
    // @DataMember(Name="type")
    public type: string;

    public constructor(init?: Partial<AiContent>) { (Object as any).assign(this, init); }
}

/** @description The function that the model called. */
// @DataContract
export class ToolFunction
{
    /** @description The name of the function to call. */
    // @DataMember(Name="name")
    public name: string;

    /** @description The arguments to call the function with, as generated by the model in JSON format. Note that the model does not always generate valid JSON, and may hallucinate parameters not defined by your function schema. Validate the arguments in your code before calling your function. */
    // @DataMember(Name="arguments")
    public arguments: string;

    public constructor(init?: Partial<ToolFunction>) { (Object as any).assign(this, init); }
}

/** @description The tool calls generated by the model, such as function calls. */
// @DataContract
export class ToolCall
{
    /** @description The ID of the tool call. */
    // @DataMember(Name="id")
    public id: string;

    /** @description The type of the tool. Currently, only `function` is supported. */
    // @DataMember(Name="type")
    public type: string;

    /** @description The function that the model called. */
    // @DataMember(Name="function")
    public function: ToolFunction;

    public constructor(init?: Partial<ToolCall>) { (Object as any).assign(this, init); }
}

/** @description A list of messages comprising the conversation so far. */
// @DataContract
export class AiMessage
{
    /** @description The contents of the message. */
    // @DataMember(Name="content")
    public content?: AiContent[];

    /** @description The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`. */
    // @DataMember(Name="role")
    public role: string;

    /** @description An optional name for the participant. Provides the model information to differentiate between participants of the same role. */
    // @DataMember(Name="name")
    public name?: string;

    /** @description The tool calls generated by the model, such as function calls. */
    // @DataMember(Name="tool_calls")
    public tool_calls?: ToolCall[];

    /** @description Tool call that this message is responding to. */
    // @DataMember(Name="tool_call_id")
    public tool_call_id?: string;

    /** @description The reasoning an assistant message was generated with, normalized per provider when replayed as history. */
    // @DataMember(Name="reasoning")
    public reasoning?: string;

    /** @description The reasoning an assistant message was generated with, as emitted by Gemini and most OpenAI-compatible providers. */
    // @DataMember(Name="reasoning_content")
    public reasoning_content?: string;

    /** @description Unix timestamp (in milliseconds) the message was generated. */
    // @DataMember(Name="timestamp")
    public timestamp?: number;

    /** @description Images attached to the message. Folded into `content` parts before sending to a provider. */
    // @DataMember(Name="images")
    public images?: AiContent[];

    public constructor(init?: Partial<AiMessage>) { (Object as any).assign(this, init); }
}

/** @description Parameters for audio output. Required when audio output is requested with modalities: [audio] */
// @DataContract
export class AiChatAudio
{
    /** @description Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16. */
    // @DataMember(Name="format")
    public format: string;

    /** @description The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer. */
    // @DataMember(Name="voice")
    public voice: string;

    public constructor(init?: Partial<AiChatAudio>) { (Object as any).assign(this, init); }
}

export enum ResponseFormat
{
    Text = 'text',
    JsonObject = 'json_object',
}

// @DataContract
export class AiResponseFormat
{
    /** @description An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106. */
    // @DataMember(Name="type")
    public type: ResponseFormat;

    public constructor(init?: Partial<AiResponseFormat>) { (Object as any).assign(this, init); }
}

export enum ToolType
{
    Function = 'function',
}

// @DataContract
export class AiToolFunction
{
    /** @description The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64. */
    // @DataMember(Name="name")
    public name?: string;

    /** @description A description of what the function does, used by the model to choose when and how to call the function. */
    // @DataMember(Name="description")
    public description?: string;

    /** @description The parameters the functions accepts, described as a JSON Schema object. See the guide for examples, and the JSON Schema reference for documentation about the format. */
    // @DataMember(Name="parameters")
    public parameters?: { [index:string]: Object; };

    public constructor(init?: Partial<AiToolFunction>) { (Object as any).assign(this, init); }
}

// @DataContract
export class Tool
{
    /** @description The type of the tool. Currently, only function is supported. */
    // @DataMember(Name="type")
    public type: ToolType;

    /** @description The function definition the model may call. */
    // @DataMember(Name="function")
    public function?: AiToolFunction;

    public constructor(init?: Partial<Tool>) { (Object as any).assign(this, init); }
}

// @DataContract
export class QueryBase
{
    // @DataMember(Order=1)
    public skip?: number;

    // @DataMember(Order=2)
    public take?: number;

    // @DataMember(Order=3)
    public orderBy?: string;

    // @DataMember(Order=4)
    public orderByDesc?: string;

    // @DataMember(Order=5)
    public include?: string;

    // @DataMember(Order=6)
    public fields?: string;

    // @DataMember(Order=7)
    public meta?: { [index:string]: string; };

    public constructor(init?: Partial<QueryBase>) { (Object as any).assign(this, init); }
}

export class QueryDb<T> extends QueryBase
{

    public constructor(init?: Partial<QueryDb<T>>) { super(init); (Object as any).assign(this, init); }
}

export class Albums
{
    public albumId: number;
    // @Required()
    public title: string;

    public artistId: number;

    public constructor(init?: Partial<Albums>) { (Object as any).assign(this, init); }
}

export class Artists
{
    public artistId: number;
    public name: string;

    public constructor(init?: Partial<Artists>) { (Object as any).assign(this, init); }
}

export class Customers
{
    public customerId: number;
    // @Required()
    public firstName: string;

    // @Required()
    public lastName: string;

    public company: string;
    public address: string;
    public city: string;
    public state: string;
    public country: string;
    public postalCode: string;
    public phone: string;
    public fax: string;
    // @Required()
    public email: string;

    public supportRepId?: number;

    public constructor(init?: Partial<Customers>) { (Object as any).assign(this, init); }
}

export class Employees
{
    public employeeId: number;
    // @Required()
    public lastName: string;

    // @Required()
    public firstName: string;

    public title: string;
    public reportsTo?: number;
    public birthDate?: string;
    public hireDate?: string;
    public address: string;
    public city: string;
    public state: string;
    public country: string;
    public postalCode: string;
    public phone: string;
    public fax: string;
    public email: string;

    public constructor(init?: Partial<Employees>) { (Object as any).assign(this, init); }
}

export class Genres
{
    public genreId: number;
    public name: string;

    public constructor(init?: Partial<Genres>) { (Object as any).assign(this, init); }
}

export class InvoiceItems
{
    public invoiceLineId: number;
    public invoiceId: number;
    public trackId: number;
    public unitPrice: number;
    public quantity: number;

    public constructor(init?: Partial<InvoiceItems>) { (Object as any).assign(this, init); }
}

export class Invoices
{
    public invoiceId: number;
    public customerId: number;
    public invoiceDate: string;
    public billingAddress: string;
    public billingCity: string;
    public billingState: string;
    public billingCountry: string;
    public billingPostalCode: string;
    public total: number;

    public constructor(init?: Partial<Invoices>) { (Object as any).assign(this, init); }
}

export class MediaTypes
{
    public mediaTypeId: number;
    public name: string;

    public constructor(init?: Partial<MediaTypes>) { (Object as any).assign(this, init); }
}

export class Playlists
{
    public playlistId: number;
    public name: string;

    public constructor(init?: Partial<Playlists>) { (Object as any).assign(this, init); }
}

export class Tracks
{
    public trackId: number;
    // @Required()
    public name: string;

    public albumId?: number;
    public mediaTypeId: number;
    public genreId?: number;
    public composer: string;
    public milliseconds: number;
    public bytes?: number;
    public unitPrice: number;

    public constructor(init?: Partial<Tracks>) { (Object as any).assign(this, init); }
}

export class JobApplicationAttachment
{
    public id: number;
    // @References("typeof(TalentBlazor.ServiceModel.JobApplication)")
    public jobApplicationId: number;

    public fileName: string;
    public filePath: string;
    public contentType: string;
    public contentLength: number;

    public constructor(init?: Partial<JobApplicationAttachment>) { (Object as any).assign(this, init); }
}

export enum RoomType
{
    Single = 'Single',
    Double = 'Double',
    Queen = 'Queen',
    Twin = 'Twin',
    Suite = 'Suite',
}

/** @description Discount Coupons */
export class Coupon
{
    public id: string;
    public description: string;
    public discount: number;
    public expiryDate: string;

    public constructor(init?: Partial<Coupon>) { (Object as any).assign(this, init); }
}

export class Address
{
    public id: number;
    public addressText?: string;

    public constructor(init?: Partial<Address>) { (Object as any).assign(this, init); }
}

export class User
{
    public id?: string;
    public userName?: string;
    public firstName?: string;
    public lastName?: string;
    public displayName?: string;
    public profileUrl?: string;

    public constructor(init?: Partial<User>) { (Object as any).assign(this, init); }
}

/** @description Booking Details */
export class Booking extends AuditBase
{
    public id: number;
    public name: string;
    public roomType: RoomType;
    public roomNumber: number;
    public bookingStartDate: string;
    public bookingEndDate?: string;
    public cost: number;
    // @References("typeof(MyApp.ServiceModel.Coupon)")
    public couponId?: string;

    public discount: Coupon;
    public notes?: string;
    public cancelled?: boolean;
    // @References("typeof(MyApp.ServiceModel.Address)")
    public permanentAddressId?: number;

    public permanentAddress?: Address;
    // @References("typeof(MyApp.ServiceModel.Address)")
    public postalAddressId?: number;

    public postalAddress?: Address;
    public employee?: User;

    public constructor(init?: Partial<Booking>) { super(init); (Object as any).assign(this, init); }
}

export enum FileAccessType
{
    Public = 'Public',
    Team = 'Team',
    Private = 'Private',
}

export class FileSystemFile implements IFile
{
    public id: number;
    public fileName: string;
    public filePath: string;
    public contentType: string;
    public contentLength: number;
    // @References("typeof(MyApp.ServiceModel.FileSystemItem)")
    public fileSystemItemId: number;

    public constructor(init?: Partial<FileSystemFile>) { (Object as any).assign(this, init); }
}

export enum PhoneKind
{
    Home = 'Home',
    Mobile = 'Mobile',
    Work = 'Work',
}

export class Phone
{
    public kind: PhoneKind;
    public number: string;
    public ext: string;

    public constructor(init?: Partial<Phone>) { (Object as any).assign(this, init); }
}

export class PlayerGameItem
{
    public id: number;
    // @References("typeof(MyApp.ServiceModel.Player)")
    public playerId: number;

    // @References("typeof(MyApp.ServiceModel.GameItem)")
    public gameItemName: string;

    public constructor(init?: Partial<PlayerGameItem>) { (Object as any).assign(this, init); }
}

export enum PlayerRole
{
    Leader = 'Leader',
    Player = 'Player',
    NonPlayer = 'NonPlayer',
}

export enum PlayerRegion
{
    Africa = 1,
    Americas = 2,
    Asia = 3,
    Australasia = 4,
    Europe = 5,
}

export class Profile extends AuditBase
{
    public id: number;
    public role: PlayerRole;
    public region: PlayerRegion;
    public username?: string;
    public highScore: number;
    public gamesPlayed: number;
    public energy: number;
    public profileUrl?: string;
    public coverUrl?: string;
    public meta?: { [index:string]: string; };

    public constructor(init?: Partial<Profile>) { super(init); (Object as any).assign(this, init); }
}

export class Player extends AuditBase
{
    public id: number;
    // @Required()
    public firstName: string;

    public lastName: string;
    public email: string;
    public phoneNumbers: Phone[] = [];
    public gameItems: PlayerGameItem[] = [];
    public profile: Profile;
    public profileId: number;
    public savedLevelId: string;
    public rowVersion: number;
    public capital: string;

    public constructor(init?: Partial<Player>) { super(init); (Object as any).assign(this, init); }
}

export class GameItem extends AuditBase
{
    // @StringLength(50)
    public name: string;

    public imageUrl: string;
    // @StringLength(2147483647)
    public description?: string;

    public dateAdded: string;

    public constructor(init?: Partial<GameItem>) { super(init); (Object as any).assign(this, init); }
}

export class Level
{
    public id: string;
    public data: string = [];

    public constructor(init?: Partial<Level>) { (Object as any).assign(this, init); }
}

export class AgentRun
{
    public id: number;
    public threadId: number;
    public user?: string;
    public status: string;
    public nextAction?: string;
    public model?: string;
    public stepCount: number;
    public sliceCount: number;
    public maxSteps: number;
    public contextTokens?: number;
    public contextLimit?: number;
    public leaseOwner?: string;
    public leaseExpiresAt?: string;
    public nextAttemptAt?: string;
    // @StringLength(2147483647)
    public error?: string;

    public createdAt: string;
    public updatedAt: string;
    public completedAt?: string;

    public constructor(init?: Partial<AgentRun>) { (Object as any).assign(this, init); }
}

export class AgentStep
{
    public id: number;
    public runId: number;
    public sequence: number;
    public type: string;
    public status: string;
    // @StringLength(2147483647)
    public input?: string;

    // @StringLength(2147483647)
    public output?: string;

    public idempotencyKey: string;
    public attempt: number;
    // @StringLength(2147483647)
    public error?: string;

    public startedAt?: string;
    public completedAt?: string;
    public createdAt: string;

    public constructor(init?: Partial<AgentStep>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AichatDocument
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    // @Required()
    public createdAt: string;

    // @DataMember(Order=5)
    // @Required()
    public updatedAt: string;

    // @DataMember(Order=6)
    public filename?: string;

    // @DataMember(Order=7)
    public url?: string;

    // @DataMember(Order=8)
    public hash?: string;

    // @DataMember(Order=9)
    public size?: number;

    // @DataMember(Order=10)
    public displayName?: string;

    // @DataMember(Order=11)
    public name?: string;

    // @DataMember(Order=12)
    public customMetadata?: string;

    // @DataMember(Order=13)
    public createTime?: string;

    // @DataMember(Order=14)
    public updateTime?: string;

    // @DataMember(Order=15)
    public sizeBytes?: number;

    // @DataMember(Order=16)
    public mimeType?: string;

    // @DataMember(Order=17)
    public state?: string;

    // @DataMember(Order=18)
    public category?: string;

    // @DataMember(Order=19)
    public tags?: string;

    // @DataMember(Order=20)
    public startedAt?: string;

    // @DataMember(Order=21)
    public uploadedAt?: string;

    // @DataMember(Order=22)
    public metadata?: string;

    // @DataMember(Order=23)
    public error?: string;

    // @DataMember(Order=24)
    public ref?: string;

    public constructor(init?: Partial<AichatDocument>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AichatFilestore
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    // @Required()
    public createdAt: string;

    // @DataMember(Order=4)
    // @Required()
    public updatedAt: string;

    // @DataMember(Order=5)
    public name?: string;

    // @DataMember(Order=6)
    public displayName?: string;

    // @DataMember(Order=7)
    public createTime?: string;

    // @DataMember(Order=8)
    public updateTime?: string;

    // @DataMember(Order=9)
    public activeDocumentsCount?: number;

    // @DataMember(Order=10)
    public pendingDocumentsCount?: number;

    // @DataMember(Order=11)
    public failedDocumentsCount?: number;

    // @DataMember(Order=12)
    public sizeBytes?: number;

    // @DataMember(Order=13)
    public metadata?: string;

    // @DataMember(Order=14)
    public error?: string;

    // @DataMember(Order=15)
    public ref?: string;

    public constructor(init?: Partial<AichatFilestore>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AichatMedia
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public name?: string;

    // @DataMember(Order=4)
    public type?: string;

    // @DataMember(Order=5)
    public prompt?: string;

    // @DataMember(Order=6)
    public model?: string;

    // @DataMember(Order=7)
    // @Required()
    public created: string;

    // @DataMember(Order=8)
    public cost?: number;

    // @DataMember(Order=9)
    public seed?: number;

    // @DataMember(Order=10)
    public url?: string;

    // @DataMember(Order=11)
    public hash?: string;

    // @DataMember(Order=12)
    public aspectRatio?: string;

    // @DataMember(Order=13)
    public width?: number;

    // @DataMember(Order=14)
    public height?: number;

    // @DataMember(Order=15)
    public size?: number;

    // @DataMember(Order=16)
    public duration?: number;

    // @DataMember(Order=17)
    public reactions?: string;

    // @DataMember(Order=18)
    public caption?: string;

    // @DataMember(Order=19)
    public description?: string;

    // @DataMember(Order=20)
    public phash?: string;

    // @DataMember(Order=21)
    public color?: string;

    // @DataMember(Order=22)
    public category?: string;

    // @DataMember(Order=23)
    public tags?: string;

    // @DataMember(Order=24)
    public rating?: string;

    // @DataMember(Order=25)
    public ratings?: string;

    // @DataMember(Order=26)
    public objects?: string;

    // @DataMember(Order=27)
    public variantId?: string;

    // @DataMember(Order=28)
    public variantName?: string;

    // @DataMember(Order=29)
    public publishedAt?: string;

    // @DataMember(Order=30)
    public publishedUrl?: string;

    // @DataMember(Order=31)
    public metadata?: string;

    public constructor(init?: Partial<AichatMedia>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AspNetRoleClaims
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    // @Required()
    public roleId: string;

    // @DataMember(Order=3)
    public claimType?: string;

    // @DataMember(Order=4)
    public claimValue?: string;

    public constructor(init?: Partial<AspNetRoleClaims>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AspNetRoles
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public normalizedName?: string;

    // @DataMember(Order=4)
    public concurrencyStamp?: string;

    public constructor(init?: Partial<AspNetRoles>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AspNetUserClaims
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    // @Required()
    public userId: string;

    // @DataMember(Order=3)
    public claimType?: string;

    // @DataMember(Order=4)
    public claimValue?: string;

    public constructor(init?: Partial<AspNetUserClaims>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AspNetUsers
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public firstName?: string;

    // @DataMember(Order=3)
    public lastName?: string;

    // @DataMember(Order=4)
    public displayName?: string;

    // @DataMember(Order=5)
    public profileUrl?: string;

    // @DataMember(Order=6)
    public refreshToken?: string;

    // @DataMember(Order=7)
    public refreshTokenExpiry?: string;

    // @DataMember(Order=8)
    public userName?: string;

    // @DataMember(Order=9)
    public normalizedUserName?: string;

    // @DataMember(Order=10)
    public email?: string;

    // @DataMember(Order=11)
    public normalizedEmail?: string;

    // @DataMember(Order=12)
    public emailConfirmed: number;

    // @DataMember(Order=13)
    public passwordHash?: string;

    // @DataMember(Order=14)
    public securityStamp?: string;

    // @DataMember(Order=15)
    public concurrencyStamp?: string;

    // @DataMember(Order=16)
    public phoneNumber?: string;

    // @DataMember(Order=17)
    public phoneNumberConfirmed: number;

    // @DataMember(Order=18)
    public twoFactorEnabled: number;

    // @DataMember(Order=19)
    public lockoutEnd?: string;

    // @DataMember(Order=20)
    public lockoutEnabled: number;

    // @DataMember(Order=21)
    public accessFailedCount: number;

    public constructor(init?: Partial<AspNetUsers>) { (Object as any).assign(this, init); }
}

export class Product
{
    public id: number;
    // @References("typeof(MyApp.ServiceModel.Category)")
    public categoryId: number;

    public name: string;
    public cost: number;
    public imageUrl?: string;

    public constructor(init?: Partial<Product>) { (Object as any).assign(this, init); }
}

export class CategoryOption
{
    public id: number;
    // @References("typeof(MyApp.ServiceModel.Category)")
    public categoryId: number;

    // @References("typeof(MyApp.ServiceModel.Option)")
    public optionId: number;

    public constructor(init?: Partial<CategoryOption>) { (Object as any).assign(this, init); }
}

export class Category
{
    public id: number;
    public name: string;
    public description: string;
    public temperatures?: string[];
    public defaultTemperature?: string;
    public sizes?: string[];
    public defaultSize?: string;
    public imageUrl?: string;
    public products: Product[] = [];
    public categoryOptions: CategoryOption[] = [];

    public constructor(init?: Partial<Category>) { (Object as any).assign(this, init); }
}

export class ChatAssistantConversation
{
    public id: number;
    public assistantId: number;
    public user?: string;
    public createdAt: string;
    public updatedAt: string;
    public sessionId?: string;
    public origin?: string;
    public pageUrl?: string;
    public userAgent?: string;
    public title?: string;
    public status?: string;
    public messageCount: number;
    // @StringLength(2147483647)
    public lastMessage?: string;

    public constructor(init?: Partial<ChatAssistantConversation>) { (Object as any).assign(this, init); }
}

export class ChatAssistantMessage
{
    public id: number;
    public conversationId: number;
    public createdAt: string;
    public role?: string;
    // @StringLength(2147483647)
    public content?: string;

    // @StringLength(2147483647)
    public citations?: string;

    // @StringLength(2147483647)
    public error?: string;

    public constructor(init?: Partial<ChatAssistantMessage>) { (Object as any).assign(this, init); }
}

export class ChatAssistant
{
    public id: number;
    public filestoreId: number;
    public user?: string;
    public createdAt: string;
    public updatedAt: string;
    public name?: string;
    public publicId?: string;
    public enabled: boolean;
    public publishedAt?: string;
    // @StringLength(2147483647)
    public config?: string;

    public constructor(init?: Partial<ChatAssistant>) { (Object as any).assign(this, init); }
}

export class ChatDocument
{
    public id: number;
    public filestoreId: number;
    public user?: string;
    public createdAt: string;
    public updatedAt: string;
    public filename?: string;
    public url?: string;
    public hash?: string;
    public size?: number;
    public displayName?: string;
    public name?: string;
    public customMetadata?: string;
    public createTime?: string;
    public updateTime?: string;
    public sizeBytes?: number;
    public mimeType?: string;
    public state?: string;
    public category?: string;
    public sourceUrl?: string;
    public sourceId?: number;
    public sourceScopeId: number;
    public sourceKey?: string;
    public sourceEtag?: string;
    public contentHash?: string;
    public metadataHash?: string;
    public extractorVer?: string;
    public tombstonedAt?: string;
    public categoryPath?: string;
    public docType?: string;
    public status?: string;
    public locale?: string;
    public product?: string;
    public versions?: string;
    public sourceUpdatedAt?: number;
    public tags?: string;
    public startedAt?: string;
    public uploadedAt?: string;
    public metadata?: string;
    // @StringLength(2147483647)
    public error?: string;

    public ref?: string;

    public constructor(init?: Partial<ChatDocument>) { (Object as any).assign(this, init); }
}

export class ChatFilestore
{
    public id: number;
    public user?: string;
    public createdAt: string;
    public updatedAt: string;
    public name?: string;
    public displayName?: string;
    public createTime?: string;
    public updateTime?: string;
    public activeDocumentsCount?: number;
    public pendingDocumentsCount?: number;
    public failedDocumentsCount?: number;
    public sizeBytes?: number;
    public metadata?: string;
    // @StringLength(2147483647)
    public error?: string;

    public ref?: string;
    public visibility?: string;
    public facets?: string;

    public constructor(init?: Partial<ChatFilestore>) { (Object as any).assign(this, init); }
}

export class ChatMedia
{
    public id: number;
    public user?: string;
    public name?: string;
    public type?: string;
    // @StringLength(2147483647)
    public prompt?: string;

    public model?: string;
    public created: string;
    public cost?: number;
    public seed?: number;
    public url?: string;
    public hash?: string;
    public aspectRatio?: string;
    public width?: number;
    public height?: number;
    public size?: number;
    public duration?: number;
    public reactions?: string;
    public caption?: string;
    // @StringLength(2147483647)
    public description?: string;

    public phash?: string;
    public color?: string;
    public category?: string;
    public tags?: string;
    public rating?: string;
    public ratings?: string;
    public objects?: string;
    public variantId?: string;
    public variantName?: string;
    public publishedAt?: string;
    public publishedUrl?: string;
    public metadata?: string;

    public constructor(init?: Partial<ChatMedia>) { (Object as any).assign(this, init); }
}

export class ChatMessage
{
    public id: number;
    public threadId: number;
    public sequence: number;
    public runId?: number;
    public stepId?: number;
    public role: string;
    // @StringLength(2147483647)
    public message: string;

    public timestamp?: number;
    public toolCallId?: string;
    public toolName?: string;
    public tokenCount?: number;
    public active: boolean;
    public createdAt: string;

    public constructor(init?: Partial<ChatMessage>) { (Object as any).assign(this, init); }
}

export class ChatRequest
{
    public id: number;
    public user?: string;
    public threadId?: number;
    public createdAt: string;
    public updatedAt: string;
    public title?: string;
    public model?: string;
    public duration?: number;
    public cost?: number;
    public inputPrice?: number;
    public inputTokens?: number;
    public inputCachedTokens?: number;
    public outputPrice?: number;
    public outputTokens?: number;
    public totalTokens?: number;
    public usage?: string;
    public provider?: string;
    public providerModel?: string;
    public providerRef?: string;
    public finishReason?: string;
    public startedAt?: string;
    public completedAt?: string;
    // @StringLength(2147483647)
    public error?: string;

    // @StringLength(2147483647)
    public stackTrace?: string;

    public ref?: string;

    public constructor(init?: Partial<ChatRequest>) { (Object as any).assign(this, init); }
}

export class ChatSourceRun
{
    public id: number;
    public sourceId: number;
    public user?: string;
    public startedAt: string;
    public completedAt?: string;
    public status?: string;
    public dryRun: boolean;
    public discovered: number;
    public added: number;
    public changed: number;
    public metadataOnly: number;
    public unchanged: number;
    public removed: number;
    public skipped: number;
    public failed: number;
    public bytes: number;
    // @StringLength(2147483647)
    public plan?: string;

    // @StringLength(2147483647)
    public log?: string;

    // @StringLength(2147483647)
    public error?: string;

    public constructor(init?: Partial<ChatSourceRun>) { (Object as any).assign(this, init); }
}

export class ChatSource
{
    public id: number;
    public filestoreId: number;
    public user?: string;
    public createdAt: string;
    public updatedAt: string;
    public name?: string;
    public type?: string;
    public enabled: boolean;
    public config?: string;
    public category?: string;
    public rules?: string;
    public include?: string;
    public exclude?: string;
    public extract?: string;
    public chunking?: string;
    public volatile?: string;
    public extractorVer?: string;
    public schedule?: string;
    public onDelete?: string;
    public cursor?: string;
    public lastRunId?: number;
    public lastRunAt?: string;
    // @StringLength(2147483647)
    public error?: string;

    public constructor(init?: Partial<ChatSource>) { (Object as any).assign(this, init); }
}

export class ChatThread
{
    public id: number;
    public user?: string;
    public createdAt: string;
    public updatedAt: string;
    public title?: string;
    // @StringLength(2147483647)
    public systemPrompt?: string;

    public model?: string;
    // @StringLength(2147483647)
    public modelInfo?: string;

    public modalities?: string;
    // @StringLength(2147483647)
    public messages?: string;

    // @StringLength(2147483647)
    public streamingMessage?: string;

    public args?: string;
    // @StringLength(2147483647)
    public tools?: string;

    // @StringLength(2147483647)
    public toolHistory?: string;

    public cost?: number;
    public inputTokens?: number;
    public outputTokens?: number;
    public stats?: string;
    public provider?: string;
    public providerModel?: string;
    public startedAt?: string;
    public completedAt?: string;
    public metadata?: string;
    public status?: string;
    // @StringLength(2147483647)
    public error?: string;

    public ref?: string;
    // @StringLength(2147483647)
    public providerResponse?: string;

    public contextTokens?: number;
    public parentId?: number;
    public publishedAt?: string;
    public publishedUrl?: string;
    // @Ignore()
    public sig: string;

    public constructor(init?: Partial<ChatThread>) { (Object as any).assign(this, init); }
}

export class ChatToolApprovalBatch
{
    public id: string;
    public threadId: number;
    public user?: string;
    public status: string;
    public createdAt: string;
    public updatedAt: string;
    public completedAt?: string;

    public constructor(init?: Partial<ChatToolApprovalBatch>) { (Object as any).assign(this, init); }
}

export class ChatToolApproval
{
    public id: number;
    public batchId: string;
    public threadId: number;
    public user?: string;
    public toolCallId: string;
    public toolName: string;
    public apiName: string;
    public requestType?: string;
    public method?: string;
    public route?: string;
    public safety: string;
    public status: string;
    public sequence: number;
    public description?: string;
    // @StringLength(2147483647)
    public schema: string;

    // @StringLength(2147483647)
    public proposedArgs: string;

    // @StringLength(2147483647)
    public effectiveArgs?: string;

    // @StringLength(2147483647)
    public result?: string;

    // @StringLength(2147483647)
    public toolResult?: string;

    // @StringLength(2147483647)
    public error?: string;

    public reason?: string;
    public createdAt: string;
    public updatedAt: string;
    public resolvedAt?: string;

    public constructor(init?: Partial<ChatToolApproval>) { (Object as any).assign(this, init); }
}

export class CoffeeShopOrderItem
{
    public id: number;
    // @References("typeof(MyApp.ServiceModel.CoffeeShopOrder)")
    public coffeeShopOrderId: number;

    // @References("typeof(MyApp.ServiceModel.Product)")
    public productId: number;

    public productName: string;
    public quantity: number;
    public size?: string;
    public temperature?: string;
    public optionsJson?: string;
    public unitPrice: number;
    public lineTotal: number;

    public constructor(init?: Partial<CoffeeShopOrderItem>) { (Object as any).assign(this, init); }
}

export class CoffeeShopOrder
{
    public id: number;
    public orderNumber: string;
    public customerName: string;
    public customerUserId?: string;
    public status: string;
    public notes?: string;
    public subtotal: number;
    public createdDate: string;
    public items: CoffeeShopOrderItem[] = [];

    public constructor(init?: Partial<CoffeeShopOrder>) { (Object as any).assign(this, init); }
}

export class ContextSnapshot
{
    public id: number;
    public threadId: number;
    public runId?: number;
    public version: number;
    public fromSequence: number;
    public toSequence: number;
    // @StringLength(2147483647)
    public summary: string;

    public tokenCount?: number;
    public model?: string;
    public createdAt: string;

    public constructor(init?: Partial<ContextSnapshot>) { (Object as any).assign(this, init); }
}

// @DataContract
export class EFMigrationsHistory
{
    // @DataMember(Order=1)
    public migrationId?: string;

    // @DataMember(Order=2)
    // @Required()
    public productVersion: string;

    public constructor(init?: Partial<EFMigrationsHistory>) { (Object as any).assign(this, init); }
}

// @DataContract
export class EFMigrationsLock
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    // @Required()
    public timestamp: string;

    public constructor(init?: Partial<EFMigrationsLock>) { (Object as any).assign(this, init); }
}

// @DataContract
export class Migration
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public description?: string;

    // @DataMember(Order=4)
    // @Required()
    public createdDate: string;

    // @DataMember(Order=5)
    public completedDate?: string;

    // @DataMember(Order=6)
    public connectionString?: string;

    // @DataMember(Order=7)
    public namedConnection?: string;

    // @DataMember(Order=8)
    public log?: string;

    // @DataMember(Order=9)
    public errorCode?: string;

    // @DataMember(Order=10)
    public errorMessage?: string;

    // @DataMember(Order=11)
    public errorStackTrace?: string;

    // @DataMember(Order=12)
    public meta?: string;

    public constructor(init?: Partial<Migration>) { (Object as any).assign(this, init); }
}

export class OptionQuantity
{
    public id: number;
    public name: string;
    public value: number;

    public constructor(init?: Partial<OptionQuantity>) { (Object as any).assign(this, init); }
}

export class Option
{
    public id: number;
    public type: string;
    public names: string[] = [];
    public allowQuantity?: boolean;
    public quantityLabel?: string;

    public constructor(init?: Partial<Option>) { (Object as any).assign(this, init); }
}

export class ValidateRule
{
    public validator: string;
    public condition?: string;
    public errorCode?: string;
    public message?: string;

    public constructor(init?: Partial<ValidateRule>) { (Object as any).assign(this, init); }
}

export class ValidationRule extends ValidateRule
{
    public id: number;
    // @Required()
    public type: string;

    public field?: string;
    public createdBy?: string;
    public createdDate?: string;
    public modifiedBy?: string;
    public modifiedDate?: string;
    public suspendedBy?: string;
    public suspendedDate?: string;
    public notes?: string;

    public constructor(init?: Partial<ValidationRule>) { super(init); (Object as any).assign(this, init); }
}

export enum EmploymentType
{
    FullTime = 'FullTime',
    PartTime = 'PartTime',
    Casual = 'Casual',
    Contract = 'Contract',
}

export class Job extends AuditBase
{
    public id: number;
    public title: string;
    public employmentType: EmploymentType;
    public company: string;
    public location: string;
    public salaryRangeLower: number;
    public salaryRangeUpper: number;
    // @StringLength(2147483647)
    public description: string;

    public applications: JobApplication[] = [];
    public closing: string;

    public constructor(init?: Partial<Job>) { super(init); (Object as any).assign(this, init); }
}

export class Contact extends AuditBase
{
    public id: number;
    // @Computed()
    public displayName: string;

    public profileUrl: string;
    public firstName: string;
    public lastName: string;
    public salaryExpectation?: number;
    public jobType: string;
    public availabilityWeeks: number;
    public preferredWorkType: EmploymentType;
    public preferredLocation: string;
    public email: string;
    public phone: string;
    // @StringLength(2147483647)
    public about: string;

    public applications: JobApplication[] = [];

    public constructor(init?: Partial<Contact>) { super(init); (Object as any).assign(this, init); }
}

export class JobApplicationComment extends AuditBase
{
    public id: number;
    // @References("typeof(MyApp.Data.ApplicationUser)")
    public applicationUserId: string;

    public applicationUser: ApplicationUser;
    // @References("typeof(TalentBlazor.ServiceModel.JobApplication)")
    public jobApplicationId: number;

    // @StringLength(2147483647)
    public comment: string;

    public constructor(init?: Partial<JobApplicationComment>) { super(init); (Object as any).assign(this, init); }
}

export class JobApplicationEvent extends AuditBase
{
    public id: number;
    // @References("typeof(TalentBlazor.ServiceModel.JobApplication)")
    public jobApplicationId: number;

    // @References("typeof(MyApp.Data.ApplicationUser)")
    public applicationUserId: string;

    public applicationUser: ApplicationUser;
    // @StringLength(2147483647)
    public description: string;

    public status?: JobApplicationStatus;
    public eventDate: string;

    public constructor(init?: Partial<JobApplicationEvent>) { super(init); (Object as any).assign(this, init); }
}

export class JobApplication extends AuditBase
{
    public id: number;
    // @References("typeof(TalentBlazor.ServiceModel.Job)")
    public jobId: number;

    // @References("typeof(TalentBlazor.ServiceModel.Contact)")
    public contactId: number;

    public position: Job;
    public applicant: Contact;
    public comments: JobApplicationComment[] = [];
    public appliedDate: string;
    public applicationStatus: JobApplicationStatus;
    public attachments: JobApplicationAttachment[] = [];
    public events: JobApplicationEvent[] = [];
    public phoneScreen: PhoneScreen;
    public interview: Interview;
    public jobOffer: JobOffer;

    public constructor(init?: Partial<JobApplication>) { super(init); (Object as any).assign(this, init); }
}

export class FileSystemItem implements IFileItem
{
    public id: number;
    public fileAccessType?: FileAccessType;
    public file: FileSystemFile;
    public applicationUserId: string;

    public constructor(init?: Partial<FileSystemItem>) { (Object as any).assign(this, init); }
}

export interface IFileItem
{
    fileAccessType?: FileAccessType;
}

export class Todo
{
    public id: number;
    public text: string;
    public isFinished?: boolean;

    public constructor(init?: Partial<Todo>) { (Object as any).assign(this, init); }
}

// @DataContract
export class ResponseError
{
    // @DataMember(Order=1)
    public errorCode: string;

    // @DataMember(Order=2)
    public fieldName: string;

    // @DataMember(Order=3)
    public message: string;

    // @DataMember(Order=4)
    public meta?: { [index:string]: string; };

    public constructor(init?: Partial<ResponseError>) { (Object as any).assign(this, init); }
}

// @DataContract
export class ResponseStatus
{
    // @DataMember(Order=1)
    public errorCode: string;

    // @DataMember(Order=2)
    public message?: string;

    // @DataMember(Order=3)
    public stackTrace?: string;

    // @DataMember(Order=4)
    public errors?: ResponseError[];

    // @DataMember(Order=5)
    public meta?: { [index:string]: string; };

    public constructor(init?: Partial<ResponseStatus>) { (Object as any).assign(this, init); }
}

export class BackgroundJobRef
{
    public id: number;
    public refId: string;

    public constructor(init?: Partial<BackgroundJobRef>) { (Object as any).assign(this, init); }
}

export class MenuProduct
{
    public id: number;
    public name: string;
    public cost: number;
    public imageUrl?: string;

    public constructor(init?: Partial<MenuProduct>) { (Object as any).assign(this, init); }
}

export class MenuOption
{
    public type: string;
    public names: string[] = [];
    public allowQuantity: boolean;
    public quantityLabel?: string;

    public constructor(init?: Partial<MenuOption>) { (Object as any).assign(this, init); }
}

export class MenuCategory
{
    public id: number;
    public name: string;
    public description: string;
    public temperatures: string[] = [];
    public defaultTemperature?: string;
    public sizes: string[] = [];
    public defaultSize?: string;
    public imageUrl?: string;
    public products: MenuProduct[] = [];
    public options: MenuOption[] = [];

    public constructor(init?: Partial<MenuCategory>) { (Object as any).assign(this, init); }
}

export class PricedOrderItem
{
    public productId: number;
    public productName: string;
    public quantity: number;
    public size?: string;
    public temperature?: string;
    public options: OrderItemOption[] = [];
    public unitPrice: number;
    public lineTotal: number;
    public summary: string;

    public constructor(init?: Partial<PricedOrderItem>) { (Object as any).assign(this, init); }
}

export class Item
{
    public name?: string;
    public description?: string;

    public constructor(init?: Partial<Item>) { (Object as any).assign(this, init); }
}

export class QueryResponseAlt<T>
{
    public offset: number;
    public total: number;
    public results: T[] = [];
    public meta: { [index:string]: string; } = {};
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<QueryResponseAlt<T>>) { (Object as any).assign(this, init); }
}

export class Forecast implements IGet
{
    public date: string;
    public temperatureC: number;
    public summary?: string;
    public temperatureF: number;

    public constructor(init?: Partial<Forecast>) { (Object as any).assign(this, init); }
}

export class ResponseBase<T>
{
    // @ApiMember(ExcludeInSchema=true)
    public responseStatus: ResponseStatus;

    /** @description This will be returned when there is a single result available. (e.g. get single object by id) */
    // @ApiMember(Description="This will be returned when there is a single result available. (e.g. get single object by id)")
    public result: T;

    /** @description This will be returned when there is a multiple results available (e.g. search or listing requests). */
    // @ApiMember(Description="This will be returned when there is a multiple results available (e.g. search or listing requests).")
    public results: T[] = [];

    /** @description This will be returned when there is a multiple results available (e.g. search or listing requests). */
    // @ApiMember(Description="This will be returned when there is a multiple results available (e.g. search or listing requests).")
    public total?: number;

    /** @description This will be return the amount of skipped rows when paginating */
    // @ApiMember(Description="This will be return the amount of skipped rows when paginating")
    public skip?: number;

    public constructor(init?: Partial<ResponseBase<T>>) { (Object as any).assign(this, init); }
}

export class DigitalPrescriptionDMDResponse
{
    public name: string;
    public productId: number;

    public constructor(init?: Partial<DigitalPrescriptionDMDResponse>) { (Object as any).assign(this, init); }
}

export class FooDto
{
    public id: number;
    public name: string;

    public constructor(init?: Partial<FooDto>) { (Object as any).assign(this, init); }
}

export class PagedResult<T>
{
    public page: number;
    public pageSize: number;
    public totalResults: number;
    public results: T[] = [];

    public constructor(init?: Partial<PagedResult<T>>) { (Object as any).assign(this, init); }
}

export class ListResult
{
    public result: string;

    public constructor(init?: Partial<ListResult>) { (Object as any).assign(this, init); }
}

/** @description Annotations for the message, when applicable, as when using the web search tool. */
// @DataContract
export class UrlCitation
{
    /** @description The index of the last character of the URL citation in the message. */
    // @DataMember(Name="end_index")
    public end_index: number;

    /** @description The index of the first character of the URL citation in the message. */
    // @DataMember(Name="start_index")
    public start_index: number;

    /** @description The title of the web resource. */
    // @DataMember(Name="title")
    public title: string;

    /** @description The URL of the web resource. */
    // @DataMember(Name="url")
    public url: string;

    public constructor(init?: Partial<UrlCitation>) { (Object as any).assign(this, init); }
}

/** @description Annotations for the message, when applicable, as when using the web search tool. */
// @DataContract
export class ChoiceAnnotation
{
    /** @description The type of the URL citation. Always url_citation. */
    // @DataMember(Name="type")
    public type: string;

    /** @description A URL citation when using web search. */
    // @DataMember(Name="url_citation")
    public url_citation: UrlCitation;

    public constructor(init?: Partial<ChoiceAnnotation>) { (Object as any).assign(this, init); }
}

/** @description If the audio output modality is requested, this object contains data about the audio response from the model. */
// @DataContract
export class ChoiceAudio
{
    /** @description Base64 encoded audio bytes generated by the model, in the format specified in the request. */
    // @DataMember(Name="data")
    public data: string;

    /** @description The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations. */
    // @DataMember(Name="expires_at")
    public expires_at: number;

    /** @description Unique identifier for this audio response. */
    // @DataMember(Name="id")
    public id: string;

    /** @description Transcript of the audio generated by the model. */
    // @DataMember(Name="transcript")
    public transcript: string;

    public constructor(init?: Partial<ChoiceAudio>) { (Object as any).assign(this, init); }
}

// @DataContract
export class ChoiceMessage
{
    /** @description The contents of the message. */
    // @DataMember(Name="content")
    public content: string;

    /** @description The refusal message generated by the model. */
    // @DataMember(Name="refusal")
    public refusal?: string;

    /** @description The reasoning process used by the model. */
    // @DataMember(Name="reasoning")
    public reasoning?: string;

    /** @description The reasoning process used by the model, as emitted by Gemini and most OpenAI-compatible providers. */
    // @DataMember(Name="reasoning_content")
    public reasoning_content?: string;

    /** @description The reasoning process used by the model, as emitted by Anthropic. */
    // @DataMember(Name="thinking")
    public thinking?: string;

    /** @description The role of the author of this message. */
    // @DataMember(Name="role")
    public role: string;

    /** @description Unix timestamp (in milliseconds) the message was generated. */
    // @DataMember(Name="timestamp")
    public timestamp?: number;

    /** @description The tool call this message is responding to, set on `tool` role messages in tool_history. */
    // @DataMember(Name="tool_call_id")
    public tool_call_id?: string;

    /** @description Images generated by the model or produced by a tool call. */
    // @DataMember(Name="images")
    public images?: AiContent[];

    /** @description Audio generated by the model or produced by a tool call. */
    // @DataMember(Name="audios")
    public audios?: AiContent[];

    /** @description Files produced by a tool call. */
    // @DataMember(Name="files")
    public files?: AiContent[];

    /** @description Annotations for the message, when applicable, as when using the web search tool. */
    // @DataMember(Name="annotations")
    public annotations?: ChoiceAnnotation[];

    /** @description If the audio output modality is requested, this object contains data about the audio response from the model. */
    // @DataMember(Name="audio")
    public audio?: ChoiceAudio;

    /** @description The tool calls generated by the model, such as function calls. */
    // @DataMember(Name="tool_calls")
    public tool_calls?: ToolCall[];

    public constructor(init?: Partial<ChoiceMessage>) { (Object as any).assign(this, init); }
}

/** @description A list of message content tokens with log probability information. */
// @DataContract
export class LogprobItem
{
    /** @description The token. */
    // @DataMember(Name="token")
    public token: string;

    /** @description The log probability of this token, if it is within the top 20 most likely tokens. Otherwise, the value `-9999`.0 is used to signify that the token is very unlikely. */
    // @DataMember(Name="logprob")
    public logprob: number;

    /** @description A list of integers representing the UTF-8 bytes representation of the token. Useful in instances where characters are represented by multiple tokens and their byte representations must be combined to generate the correct text representation. Can be `null` if there is no bytes representation for the token. */
    // @DataMember(Name="bytes")
    public bytes: string = [];

    /** @description List of the most likely tokens and their log probability, at this token position. In rare cases, there may be fewer than the number of requested `top_logprobs` returned. */
    // @DataMember(Name="top_logprobs")
    public top_logprobs: LogprobItem[] = [];

    public constructor(init?: Partial<LogprobItem>) { (Object as any).assign(this, init); }
}

/** @description Log probability information for the choice. */
// @DataContract
export class Logprobs
{
    /** @description A list of message content tokens with log probability information. */
    // @DataMember(Name="content")
    public content: LogprobItem[] = [];

    public constructor(init?: Partial<Logprobs>) { (Object as any).assign(this, init); }
}

// @DataContract
export class Choice
{
    /** @description The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool */
    // @DataMember(Name="finish_reason")
    public finish_reason: string;

    /** @description The index of the choice in the list of choices. */
    // @DataMember(Name="index")
    public index: number;

    /** @description A chat completion message generated by the model. */
    // @DataMember(Name="message")
    public message: ChoiceMessage;

    /** @description Log probability information for the choice. */
    // @DataMember(Name="logprobs")
    public logprobs?: Logprobs;

    public constructor(init?: Partial<Choice>) { (Object as any).assign(this, init); }
}

/** @description Usage statistics for the completion request. */
// @DataContract
export class AiCompletionUsage
{
    /** @description When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion. */
    // @DataMember(Name="accepted_prediction_tokens")
    public accepted_prediction_tokens: number;

    /** @description Audio input tokens generated by the model. */
    // @DataMember(Name="audio_tokens")
    public audio_tokens: number;

    /** @description Tokens generated by the model for reasoning. */
    // @DataMember(Name="reasoning_tokens")
    public reasoning_tokens: number;

    /** @description When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion. */
    // @DataMember(Name="rejected_prediction_tokens")
    public rejected_prediction_tokens: number;

    public constructor(init?: Partial<AiCompletionUsage>) { (Object as any).assign(this, init); }
}

/** @description Breakdown of tokens used in the prompt. */
// @DataContract
export class AiPromptUsage
{
    /** @description When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion. */
    // @DataMember(Name="accepted_prediction_tokens")
    public accepted_prediction_tokens: number;

    /** @description Audio input tokens present in the prompt. */
    // @DataMember(Name="audio_tokens")
    public audio_tokens: number;

    /** @description Cached tokens present in the prompt. */
    // @DataMember(Name="cached_tokens")
    public cached_tokens: number;

    public constructor(init?: Partial<AiPromptUsage>) { (Object as any).assign(this, init); }
}

/** @description Usage statistics for the completion request. */
// @DataContract
export class AiUsage
{
    /** @description Number of tokens in the generated completion. */
    // @DataMember(Name="completion_tokens")
    public completion_tokens: number;

    /** @description Number of tokens in the prompt. */
    // @DataMember(Name="prompt_tokens")
    public prompt_tokens: number;

    /** @description Total number of tokens used in the request (prompt + completion). */
    // @DataMember(Name="total_tokens")
    public total_tokens: number;

    /** @description Breakdown of tokens used in a completion. */
    // @DataMember(Name="completion_tokens_details")
    public completion_tokens_details?: AiCompletionUsage;

    /** @description Breakdown of tokens used in the prompt. */
    // @DataMember(Name="prompt_tokens_details")
    public prompt_tokens_details?: AiPromptUsage;

    /** @description Seconds spent servicing the completion, including every request in the tool loop. */
    // @DataMember(Name="duration")
    public duration?: number;

    public constructor(init?: Partial<AiUsage>) { (Object as any).assign(this, init); }
}

// @DataContract
export class QueryResponse<T>
{
    // @DataMember(Order=1)
    public offset: number;

    // @DataMember(Order=2)
    public total: number;

    // @DataMember(Order=3)
    public results: T[] = [];

    // @DataMember(Order=4)
    public meta?: { [index:string]: string; };

    // @DataMember(Order=5)
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<QueryResponse<T>>) { (Object as any).assign(this, init); }
}

export interface IFile
{
    id: number;
    fileName: string;
    filePath: string;
    contentType: string;
    contentLength: number;
}

/** @description Text content part */
// @DataContract
export class AiTextContent extends AiContent
{
    /** @description The text content. */
    // @DataMember(Name="text")
    public text: string;

    public constructor(init?: Partial<AiTextContent>) { super(init); (Object as any).assign(this, init); }
}

// @DataContract
export class AiImageUrl
{
    /** @description Either a URL of the image or the base64 encoded image data. */
    // @DataMember(Name="url")
    public url: string;

    public constructor(init?: Partial<AiImageUrl>) { (Object as any).assign(this, init); }
}

/** @description Image content part */
// @DataContract
export class AiImageContent extends AiContent
{
    /** @description The image for this content. */
    // @DataMember(Name="image_url")
    public image_url: AiImageUrl;

    public constructor(init?: Partial<AiImageContent>) { super(init); (Object as any).assign(this, init); }
}

/** @description Audio content part */
// @DataContract
export class AiInputAudio
{
    /** @description URL or Base64 encoded audio data. */
    // @DataMember(Name="data")
    public data: string;

    /** @description The format of the encoded audio data. Currently supports 'wav' and 'mp3'. */
    // @DataMember(Name="format")
    public format: string;

    public constructor(init?: Partial<AiInputAudio>) { (Object as any).assign(this, init); }
}

/** @description Audio content part */
// @DataContract
export class AiAudioContent extends AiContent
{
    /** @description The audio input for this content. */
    // @DataMember(Name="input_audio")
    public input_audio: AiInputAudio;

    public constructor(init?: Partial<AiAudioContent>) { super(init); (Object as any).assign(this, init); }
}

/** @description File content part */
// @DataContract
export class AiFile
{
    /** @description The URL or base64 encoded file data, used when passing the file to the model as a string. */
    // @DataMember(Name="file_data")
    public file_data: string;

    /** @description The name of the file, used when passing the file to the model as a string. */
    // @DataMember(Name="filename")
    public filename: string;

    /** @description The ID of an uploaded file to use as input. */
    // @DataMember(Name="file_id")
    public file_id?: string;

    public constructor(init?: Partial<AiFile>) { (Object as any).assign(this, init); }
}

/** @description File content part */
// @DataContract
export class AiFileContent extends AiContent
{
    /** @description The file input for this content. */
    // @DataMember(Name="file")
    public file: AiFile;

    public constructor(init?: Partial<AiFileContent>) { super(init); (Object as any).assign(this, init); }
}

// @DataContract
export class AiAudioUrl
{
    /** @description Either a URL of the audio or the base64 encoded audio data. */
    // @DataMember(Name="url")
    public url: string;

    public constructor(init?: Partial<AiAudioUrl>) { (Object as any).assign(this, init); }
}

/** @description Generated audio content part, referenced by URL (emitted by tool calls and audio models) */
// @DataContract
export class AiAudioUrlContent extends AiContent
{
    /** @description The audio for this content. */
    // @DataMember(Name="audio_url")
    public audio_url: AiAudioUrl;

    public constructor(init?: Partial<AiAudioUrlContent>) { super(init); (Object as any).assign(this, init); }
}

export class GetContactsResponse
{
    public results: Contact[] = [];
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<GetContactsResponse>) { (Object as any).assign(this, init); }
}

export class TalentStatsResponse
{
    public totalJobs: number;
    public totalContacts: number;
    public avgSalaryExpectation: number;
    public avgSalaryLower: number;
    public avgSalaryUpper: number;
    public preferredRemotePercentage: number;

    public constructor(init?: Partial<TalentStatsResponse>) { (Object as any).assign(this, init); }
}

export class GetAccountResponse
{
    public userId: string;
    public username: string;
    public email: string;
    public displayName: string;
    public roles: string[] = [];

    public constructor(init?: Partial<GetAccountResponse>) { (Object as any).assign(this, init); }
}

export class QueueCheckUrlResponse
{
    public id: number;
    public refId: string;
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<QueueCheckUrlResponse>) { (Object as any).assign(this, init); }
}

export class QueueCheckUrlsResponse
{
    public jobRef: BackgroundJobRef;

    public constructor(init?: Partial<QueueCheckUrlsResponse>) { (Object as any).assign(this, init); }
}

export class CheckUrlResponse
{
    public url: string;
    public result: boolean;
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<CheckUrlResponse>) { (Object as any).assign(this, init); }
}

export class GetCoffeeShopMenuResponse
{
    public results: MenuCategory[] = [];
    public optionQuantities: string[] = [];
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<GetCoffeeShopMenuResponse>) { (Object as any).assign(this, init); }
}

export class PreviewCoffeeShopOrderResponse
{
    public customerName: string;
    public notes?: string;
    public items: PricedOrderItem[] = [];
    public subtotal: number;
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<PreviewCoffeeShopOrderResponse>) { (Object as any).assign(this, init); }
}

export class CreateCoffeeShopOrderResponse
{
    public result: CoffeeShopOrder;
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<CreateCoffeeShopOrderResponse>) { (Object as any).assign(this, init); }
}

export class GetCoffeeShopOrderResponse
{
    public result: CoffeeShopOrder;
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<GetCoffeeShopOrderResponse>) { (Object as any).assign(this, init); }
}

export class Items
{
    public results: Item[] = [];

    public constructor(init?: Partial<Items>) { (Object as any).assign(this, init); }
}

// @Route("/echo/complex")
export class EchoComplexTypes implements IReturn<EchoComplexTypes>
{
    public subType: SubType;
    public subTypes: SubType[] = [];
    public subTypeMap: { [index:string]: SubType; } = {};
    public stringMap: { [index:string]: string; } = {};
    public intStringMap: { [index:number]: string; } = {};

    public constructor(init?: Partial<EchoComplexTypes>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'EchoComplexTypes'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new EchoComplexTypes(); }
}

// @Route("/echo/collections")
export class EchoCollections implements IReturn<EchoCollections>
{
    public stringList: string[] = [];
    public stringArray: string[] = [];
    public stringMap: { [index:string]: string; } = {};
    public intStringMap: { [index:number]: string; } = {};

    public constructor(init?: Partial<EchoCollections>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'EchoCollections'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new EchoCollections(); }
}

export class FormDataTest implements IReturn<FormDataTest>
{
    public hidden: boolean;
    public string?: string;
    public int: number;
    public dateTime: string;
    public dateOnly: string;
    public timeSpan: string;
    public timeOnly: string;
    public password?: string;
    public checkboxString?: string[];
    public radioString?: string;
    public radioColors: Colors;
    public checkboxColors?: Colors[];
    public selectColors: Colors;
    public multiSelectColors?: Colors[];
    public profileUrl?: string;
    public attachments: Attachment[] = [];

    public constructor(init?: Partial<FormDataTest>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'FormDataTest'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new FormDataTest(); }
}

export class ComboBoxExamples implements IReturn<ComboBoxExamples>, IPost
{
    public singleClientValues?: string;
    public multipleClientValues?: string[];
    public singleServerValues?: string;
    public multipleServerValues?: string[];
    public singleServerEntries?: string;
    public multipleServerEntries?: string[];

    public constructor(init?: Partial<ComboBoxExamples>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'ComboBoxExamples'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new ComboBoxExamples(); }
}

export class SecuredResponse
{
    public result: string;
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<SecuredResponse>) { (Object as any).assign(this, init); }
}

export class CreateRefreshJwtResponse
{
    public token: string;
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<CreateRefreshJwtResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class EmptyResponse
{
    // @DataMember(Order=1)
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<EmptyResponse>) { (Object as any).assign(this, init); }
}

export class Movie
{
    public movieID: string;
    public movieNo: number;
    public name?: string;
    public description?: string;
    public movieRef?: string;

    public constructor(init?: Partial<Movie>) { (Object as any).assign(this, init); }
}

export class HelloResponse
{
    public result: string;
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<HelloResponse>) { (Object as any).assign(this, init); }
}

export class OptionalTest implements IReturn<OptionalTest>
{
    public int: number;
    public nInt?: number;
    // @Validate(Validator="NotNull")
    public nRequiredInt: number;

    public string: string;
    public nString?: string;
    // @Validate(Validator="NotEmpty")
    public nRequiredString: string;

    public optionalClass: OptionalClass;
    public nOptionalClass?: OptionalClass;
    // @Validate(Validator="NotNull")
    public nRequiredOptionalClass: OptionalClass;

    public optionalEnum: OptionalEnum;
    public nOptionalEnum?: OptionalEnum;
    // @Validate(Validator="NotNull")
    public nRequiredOptionalEnum: OptionalEnum;

    public constructor(init?: Partial<OptionalTest>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'OptionalTest'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new OptionalTest(); }
}

export class SendVerbResponse
{
    public id: number;
    public pathInfo: string;
    public requestMethod: string;

    public constructor(init?: Partial<SendVerbResponse>) { (Object as any).assign(this, init); }
}

export class TestAuthResponse
{
    public userId: string;
    public sessionId: string;
    public userName: string;
    public displayName: string;
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<TestAuthResponse>) { (Object as any).assign(this, init); }
}

export class RequiresAdmin implements IReturn<RequiresAdmin>
{
    public id: number;

    public constructor(init?: Partial<RequiresAdmin>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'RequiresAdmin'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new RequiresAdmin(); }
}

export class AllTypes implements IReturn<AllTypes>
{
    public id: number;
    public nullableId?: number;
    public boolean: boolean;
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
    public nullableDateTime?: string;
    public nullableTimeSpan?: string;
    public stringList: string[] = [];
    public stringArray: string[] = [];
    public stringMap: { [index:string]: string; } = {};
    public intStringMap: { [index:number]: string; } = {};
    public subType: SubType;
    public nullableBytes: number[] = [];

    public constructor(init?: Partial<AllTypes>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AllTypes'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AllTypes(); }
}

export class AllCollectionTypes implements IReturn<AllCollectionTypes>
{
    public intArray: number[] = [];
    public intList: number[] = [];
    public stringArray: string[] = [];
    public stringList: string[] = [];
    public floatArray: number[] = [];
    public doubleList: number[] = [];
    public byteArray: string = [];
    public charArray: string[] = [];
    public decimalList: number[] = [];
    public pocoArray: Poco[] = [];
    public pocoList: Poco[] = [];
    public pocoLookup: { [index:string]: Poco[]; } = {};
    public pocoLookupMap: { [index:string]: { [index:string]: Poco; }[]; } = {};

    public constructor(init?: Partial<AllCollectionTypes>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AllCollectionTypes'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AllCollectionTypes(); }
}

export class HelloAllTypesResponse
{
    public result: string;
    public allTypes: AllTypes;
    public allCollectionTypes: AllCollectionTypes;

    public constructor(init?: Partial<HelloAllTypesResponse>) { (Object as any).assign(this, init); }
}

export class ThrowTypeResponse
{
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<ThrowTypeResponse>) { (Object as any).assign(this, init); }
}

export class ThrowValidationResponse
{
    public age: number;
    public required: string;
    public email: string;
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<ThrowValidationResponse>) { (Object as any).assign(this, init); }
}

export class AllNullableCollectionTypes implements IReturn<AllNullableCollectionTypes>
{
    public intArray?: number[];
    public intList?: number[];
    public stringArray?: string[];
    public stringList?: string[];
    public floatArray?: number[];
    public doubleList?: number[];
    public byteArray?: string;
    public charArray?: string[];
    public decimalList?: number[];
    public pocoArray?: Poco[];
    public pocoList?: Poco[];
    public pocoLookup?: { [index:string]: Poco[]; };
    public pocoLookupMap?: { [index:string]: { [index:string]: Poco; }[]; };

    public constructor(init?: Partial<AllNullableCollectionTypes>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AllNullableCollectionTypes'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AllNullableCollectionTypes(); }
}

export class ProfileGenResponse
{

    public constructor(init?: Partial<ProfileGenResponse>) { (Object as any).assign(this, init); }
}

// @Route("/echo/types")
export class EchoTypes implements IReturn<EchoTypes>
{
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

    public constructor(init?: Partial<EchoTypes>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'EchoTypes'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new EchoTypes(); }
}

export class SubAllTypes extends AllTypesBase
{
    public hierarchy: number;

    public constructor(init?: Partial<SubAllTypes>) { super(init); (Object as any).assign(this, init); }
}

export class HelloWithGenericInheritance extends HelloBase_1<Poco> implements IReturn<HelloWithGenericInheritance>
{
    public result: string;

    public constructor(init?: Partial<HelloWithGenericInheritance>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloWithGenericInheritance'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new HelloWithGenericInheritance(); }
}

export class HelloPost extends HelloBase implements IReturn<HelloPost>, IPost
{

    public constructor(init?: Partial<HelloPost>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloPost'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new HelloPost(); }
}

// @DataContract
export class ChatResponse
{
    /** @description A unique identifier for the chat completion. */
    // @DataMember(Name="id")
    public id: string;

    /** @description A list of chat completion choices. Can be more than one if n is greater than 1. */
    // @DataMember(Name="choices")
    public choices: Choice[] = [];

    /** @description The Unix timestamp (in seconds) of when the chat completion was created. */
    // @DataMember(Name="created")
    public created: number;

    /** @description The model used for the chat completion. */
    // @DataMember(Name="model")
    public model: string;

    /** @description This fingerprint represents the backend configuration that the model runs with. */
    // @DataMember(Name="system_fingerprint")
    public system_fingerprint?: string;

    /** @description The object type, which is always chat.completion. */
    // @DataMember(Name="object")
    public object: string;

    /** @description Specifies the processing type used for serving the request. */
    // @DataMember(Name="service_tier")
    public service_tier?: string;

    /** @description Usage statistics for the completion request. */
    // @DataMember(Name="usage")
    public usage: AiUsage;

    /** @description The provider used for the chat completion. */
    // @DataMember(Name="provider")
    public provider?: string;

    /** @description Total cost of the completion in USD, accumulated across every request in the tool loop. */
    // @DataMember(Name="cost")
    public cost?: number;

    /** @description The assistant and tool messages exchanged during the tool-execution loop, in order. */
    // @DataMember(Name="tool_history")
    public tool_history?: ChoiceMessage[];

    /** @description Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format. */
    // @DataMember(Name="metadata")
    public metadata?: { [index:string]: string; };

    // @DataMember(Name="responseStatus")
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<ChatResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AuthenticateResponse implements IHasSessionId, IHasBearerToken
{
    // @DataMember(Order=1)
    public userId?: string;

    // @DataMember(Order=2)
    public sessionId?: string;

    // @DataMember(Order=3)
    public userName?: string;

    // @DataMember(Order=4)
    public displayName?: string;

    // @DataMember(Order=5)
    public referrerUrl?: string;

    // @DataMember(Order=6)
    public bearerToken?: string;

    // @DataMember(Order=7)
    public refreshToken?: string;

    // @DataMember(Order=8)
    public refreshTokenExpiry?: string;

    // @DataMember(Order=9)
    public profileUrl?: string;

    // @DataMember(Order=10)
    public roles?: string[];

    // @DataMember(Order=11)
    public permissions?: string[];

    // @DataMember(Order=12)
    public authProvider?: string;

    // @DataMember(Order=13)
    public responseStatus?: ResponseStatus;

    // @DataMember(Order=14)
    public meta?: { [index:string]: string; };

    public constructor(init?: Partial<AuthenticateResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class IdResponse
{
    // @DataMember(Order=1)
    public id: string;

    // @DataMember(Order=2)
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<IdResponse>) { (Object as any).assign(this, init); }
}

// @Route("/contacts", "POST")
export class StoreContacts extends Array<Contact> implements IReturnVoid
{

    public constructor(init?: Partial<StoreContacts>) { super(); (Object as any).assign(this, init); }
    public getTypeName() { return 'StoreContacts'; }
    public getMethod() { return 'POST'; }
    public createResponse() {}
}

// @Route("/contacts", "GET")
export class GetContacts implements IReturn<GetContactsResponse>
{

    public constructor(init?: Partial<GetContacts>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetContacts'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new GetContactsResponse(); }
}

export class CreatePhoneScreen implements IReturn<PhoneScreen>, ICreateDb<PhoneScreen>
{
    // @Validate(Validator="GreaterThan(0)")
    public jobApplicationId: number;

    // @Validate(Validator="GreaterThan(0)", Message="An employee to perform the phone screening must be selected.")
    public appUserId: number;

    public applicationStatus: JobApplicationStatus;

    public constructor(init?: Partial<CreatePhoneScreen>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreatePhoneScreen'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new PhoneScreen(); }
}

export class UpdatePhoneScreen implements IReturn<PhoneScreen>, IPatchDb<PhoneScreen>
{
    public id: number;
    public jobApplicationId?: number;
    public notes?: string;
    public applicationStatus?: JobApplicationStatus;

    public constructor(init?: Partial<UpdatePhoneScreen>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdatePhoneScreen'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new PhoneScreen(); }
}

export class CreateInterview implements IReturn<Interview>, ICreateDb<Interview>
{
    // @Validate(Validator="NotNull")
    public bookingTime: string;

    // @Validate(Validator="GreaterThan(0)")
    public jobApplicationId: number;

    // @Validate(Validator="GreaterThan(0)", Message="An employee to perform interview must be selected.")
    public appUserId: number;

    public applicationStatus: JobApplicationStatus;

    public constructor(init?: Partial<CreateInterview>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateInterview'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new Interview(); }
}

export class UpdateInterview implements IReturn<Interview>, IPatchDb<Interview>
{
    // @Validate(Validator="GreaterThan(0)")
    public id: number;

    public jobApplicationId?: number;
    public notes?: string;
    public applicationStatus?: JobApplicationStatus;

    public constructor(init?: Partial<UpdateInterview>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateInterview'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new Interview(); }
}

export class CreateJobOffer implements IReturn<JobOffer>, ICreateDb<JobOffer>
{
    // @Validate(Validator="GreaterThan(0)")
    public salaryOffer: number;

    // @Validate(Validator="GreaterThan(0)")
    public jobApplicationId: number;

    public applicationStatus: JobApplicationStatus;
    // @Validate(Validator="NotEmpty")
    public notes: string;

    public constructor(init?: Partial<CreateJobOffer>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateJobOffer'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new JobOffer(); }
}

export class TalentStats implements IReturn<TalentStatsResponse>, IGet
{

    public constructor(init?: Partial<TalentStats>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'TalentStats'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new TalentStatsResponse(); }
}

export class GetAccount implements IReturn<GetAccountResponse>, IGet
{

    public constructor(init?: Partial<GetAccount>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetAccount'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new GetAccountResponse(); }
}

export class GetKey implements IReturn<string>, IGet
{

    public constructor(init?: Partial<GetKey>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetKey'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return ''; }
}

export class QueueCheckUrl implements IReturn<QueueCheckUrlResponse>, IPost
{
    // @Validate(Validator="NotEmpty")
    public url: string;

    /** @description Specify a user-defined UUID for the Job */
    // @ApiMember(Description="Specify a user-defined UUID for the Job")
    public refId?: string;

    /** @description Maintain a Reference to a parent Job */
    // @ApiMember(Description="Maintain a Reference to a parent Job")
    public parentId?: number;

    /** @description Named Worker Thread to execute Job on */
    // @ApiMember(Description="Named Worker Thread to execute Job on")
    public worker?: string;

    /** @description Only run Job after date */
    // @ApiMember(Description="Only run Job after date")
    public runAfter?: string;

    /** @description Command to Execute after successful completion of Job */
    // @ApiMember(Description="Command to Execute after successful completion of Job")
    public callback?: string;

    /** @description Only execute job after successful completion of Parent Job */
    // @ApiMember(Description="Only execute job after successful completion of Parent Job")
    public dependsOn?: number;

    /** @description The ASP .NET Identity Auth User Id to populate the IRequest Context ClaimsPrincipal and User Session */
    // @ApiMember(Description="The ASP .NET Identity Auth User Id to populate the IRequest Context ClaimsPrincipal and User Session")
    public userId?: string;

    /** @description How many times to attempt to retry Job on failure, default 2 */
    // @ApiMember(Description="How many times to attempt to retry Job on failure, default 2")
    public retryLimit?: number;

    /** @description Maintain a reference to a callback URL */
    // @ApiMember(Description="Maintain a reference to a callback URL")
    public replyTo?: string;

    /** @description Associate Job with a tag group */
    // @ApiMember(Description="Associate Job with a tag group")
    public tag?: string;

    public batchId?: string;
    public createdBy?: string;
    public timeoutSecs?: number;

    public constructor(init?: Partial<QueueCheckUrl>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'QueueCheckUrl'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new QueueCheckUrlResponse(); }
}

export class QueueCheckUrls implements IReturn<QueueCheckUrlsResponse>
{
    public urls: string;

    public constructor(init?: Partial<QueueCheckUrls>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'QueueCheckUrls'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new QueueCheckUrlsResponse(); }
}

export class CheckUrl implements IReturn<CheckUrlResponse>
{
    // @Validate(Validator="NotEmpty")
    public url: string;

    public constructor(init?: Partial<CheckUrl>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CheckUrl'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new CheckUrlResponse(); }
}

export class QueueCheckUrlApi implements IReturn<QueueCheckUrlsResponse>
{
    // @Validate(Validator="NotEmpty")
    public url: string;

    public constructor(init?: Partial<QueueCheckUrlApi>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'QueueCheckUrlApi'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new QueueCheckUrlsResponse(); }
}

/** @description Returns the complete coffee shop menu with product IDs, prices, valid sizes, temperatures and customization options */
// @Route("/coffee-shop/menu", "GET")
export class GetCoffeeShopMenu implements IReturn<GetCoffeeShopMenuResponse>, IGet
{

    public constructor(init?: Partial<GetCoffeeShopMenu>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetCoffeeShopMenu'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new GetCoffeeShopMenuResponse(); }
}

/** @description Validates and prices a proposed order without saving it. Returns normalized defaults and actionable validation errors */
// @Route("/coffee-shop/orders/preview", "POST")
export class PreviewCoffeeShopOrder implements IReturn<PreviewCoffeeShopOrderResponse>, IPost
{
    /** @description Name to put on the order */
    // @Validate(Validator="NotEmpty")
    public customerName: string;

    /** @description Optional instructions applying to the whole order */
    public notes?: string;
    /** @description One or more products from the current menu */
    // @Validate(Validator="NotEmpty")
    public items: OrderItemRequest[] = [];

    public constructor(init?: Partial<PreviewCoffeeShopOrder>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PreviewCoffeeShopOrder'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new PreviewCoffeeShopOrderResponse(); }
}

/** @description Submits and charges a coffee shop order. Product names and prices are always resolved from the database. */
// @Route("/coffee-shop/orders", "POST")
export class CreateCoffeeShopOrder implements IReturn<CreateCoffeeShopOrderResponse>, IPost
{
    /** @description Name to put on the order */
    // @Validate(Validator="NotEmpty")
    public customerName: string;

    /** @description Optional instructions applying to the whole order */
    public notes?: string;
    /** @description Final order items. The approval form lets the user edit these before submission */
    // @Validate(Validator="NotEmpty")
    public items: OrderItemRequest[] = [];

    public constructor(init?: Partial<CreateCoffeeShopOrder>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateCoffeeShopOrder'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new CreateCoffeeShopOrderResponse(); }
}

/** @description Returns a previously submitted coffee shop order by ID */
// @Route("/coffee-shop/orders/{Id}", "GET")
export class GetCoffeeShopOrder implements IReturn<GetCoffeeShopOrderResponse>, IGet
{
    // @Validate(Validator="GreaterThan(0)")
    public id: number;

    public constructor(init?: Partial<GetCoffeeShopOrder>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetCoffeeShopOrder'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new GetCoffeeShopOrderResponse(); }
}

// @Route("/compress/{**Path}")
export class CompressFile implements IReturn<Blob>, IGet
{
    public path: string;

    public constructor(init?: Partial<CompressFile>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CompressFile'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new Blob(); }
}

export class AltQueryItems implements IReturn<QueryResponseAlt<Item>>
{
    public name?: string;

    public constructor(init?: Partial<AltQueryItems>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AltQueryItems'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new QueryResponseAlt<Item>(); }
}

export class GetItems implements IReturn<Items>
{

    public constructor(init?: Partial<GetItems>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetItems'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new Items(); }
}

export class GetNakedItems implements IReturn<Item[]>
{

    public constructor(init?: Partial<GetNakedItems>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetNakedItems'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new Array<Item>(); }
}

export class EchoData
{
    public data1: Data1;
    public data2: Data2;
    public data3: Data3;

    public constructor(init?: Partial<EchoData>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'EchoData'; }
    public getMethod() { return 'POST'; }
    public createResponse() {}
}

// @Route("/profile-image")
// @Route("/profile-image/{Type}")
// @Route("/profile-image/{Type}/{Size}")
export class GetProfileImage implements IReturn<Blob>
{
    public type?: string;
    public size?: string;

    public constructor(init?: Partial<GetProfileImage>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetProfileImage'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new Blob(); }
}

export class GetWeatherForecast implements IReturn<Forecast[]>, IGet
{
    public date?: string;

    public constructor(init?: Partial<GetWeatherForecast>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetWeatherForecast'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new Array<Forecast>(); }
}

export class Problem implements IReturn<ResponseBase<{ [index:string]: HelloResponse[]; }>>
{

    public constructor(init?: Partial<Problem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'Problem'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new ResponseBase<{ [index:string]: HelloResponse[]; }>(); }
}

export class DigitalPrescriptionDMDRequest implements IReturn<ResponseBase<DigitalPrescriptionDMDResponse>>
{
    public term: string;

    public constructor(init?: Partial<DigitalPrescriptionDMDRequest>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DigitalPrescriptionDMDRequest'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new ResponseBase<DigitalPrescriptionDMDResponse>(); }
}

// @Route("/getDiscountCodesBillingItem", "POST")
export class GetDiscountCodeBillingItem implements IReturn<ResponseBase<BillingItem>>
{
    public billingItem: BillingItem;
    public discountCodeId: string;

    public constructor(init?: Partial<GetDiscountCodeBillingItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetDiscountCodeBillingItem'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new ResponseBase<BillingItem>(); }
}

// @Route("/foos", "GET")
export class GetFooDtos extends PagedAndOrderedRequest implements IReturn<PagedResult<FooDto>>
{
    public query: string;

    public constructor(init?: Partial<GetFooDtos>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'GetFooDtos'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new PagedResult<FooDto>(); }
}

// @Route("/secured")
// @ValidateRequest(Validator="IsAuthenticated")
export class Secured implements IReturn<SecuredResponse>
{
    public name: string;

    public constructor(init?: Partial<Secured>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'Secured'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new SecuredResponse(); }
}

// @Route("/jwt-refresh")
export class CreateRefreshJwt implements IReturn<CreateRefreshJwtResponse>
{
    public userAuthId: string;
    public jwtExpiry?: string;

    public constructor(init?: Partial<CreateRefreshJwt>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateRefreshJwt'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new CreateRefreshJwtResponse(); }
}

// @Route("/jwt-invalidate")
export class InvalidateLastAccessToken implements IReturn<EmptyResponse>
{

    public constructor(init?: Partial<InvalidateLastAccessToken>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'InvalidateLastAccessToken'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new EmptyResponse(); }
}

export class MovieGETRequest implements IReturn<Movie>
{
    /** @description Unique Id of the movie */
    // @Validate(Validator="NotEmpty")
    public movieID: string;

    public constructor(init?: Partial<MovieGETRequest>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'MovieGETRequest'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new Movie(); }
}

export class MoviePOSTRequest extends Movie implements IReturn<Movie>
{
    public movieID: string;
    public movieNo: number;
    public movieRef?: string;

    public constructor(init?: Partial<MoviePOSTRequest>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'MoviePOSTRequest'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new Movie(); }
}

export class CommandOperation implements IReturn<EmptyResponse>, IPost
{
    public newTodo?: string;
    public throwException?: string;
    public throwArgumentException?: string;
    public throwNotSupportedException?: string;

    public constructor(init?: Partial<CommandOperation>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CommandOperation'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new EmptyResponse(); }
}

export class FailedCommandTests
{
    public failNoRetryCommand?: boolean;
    public failDefaultRetryCommand?: boolean;
    public failTimes1Command?: boolean;
    public failTimes4Command?: boolean;

    public constructor(init?: Partial<FailedCommandTests>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'FailedCommandTests'; }
    public getMethod() { return 'POST'; }
    public createResponse() {}
}

// @Route("/greet/{Name}")
export class Greet implements IReturn<HelloResponse>
{
    public name: string;

    public constructor(init?: Partial<Greet>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'Greet'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new HelloResponse(); }
}

// @Route("/hello")
// @Route("/hello/{Name}")
export class Hello implements IReturn<HelloResponse>, IGet
{
    public name: string;

    public constructor(init?: Partial<Hello>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'Hello'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new HelloResponse(); }
}

// @Route("/hello-long/{Name}", "PATCH,PUT")
// @Route("/hello-very-long/{Name}", "GET,POST,PUT")
// @ValidateRequest(Validator="HasRole(`Employee`)")
// @ValidateRequest(Validator="HasPermission(`ThePermission`)")
// @ValidateRequest(Validator="IsAuthenticated")
export class HelloVeryLongOperationNameVersions implements IReturn<HelloResponse>, IGet, IPost, IPut, IPatch
{
    public name?: string;
    public names?: string[];
    public ids?: number[];

    public constructor(init?: Partial<HelloVeryLongOperationNameVersions>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloVeryLongOperationNameVersions'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new HelloResponse(); }
}

// @Route("/hellosecure/{Name}", "PUT")
// @ValidateRequest(Validator="IsAuthenticated")
export class HelloSecure implements IReturn<HelloResponse>
{
    public name: string;

    public constructor(init?: Partial<HelloSecure>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloSecure'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new HelloResponse(); }
}

// @DataContract
export class HelloBookingList implements IReturn<Booking[]>
{
    // @DataMember(Name="Alias", Order=1)
    public Alias: string;

    public constructor(init?: Partial<HelloBookingList>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloBookingList'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new Array<Booking>(); }
}

export class HelloString implements IReturn<string>
{
    public name: string;

    public constructor(init?: Partial<HelloString>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloString'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return ''; }
}

// @Route("/return/string")
export class ReturnString implements IReturn<string>
{
    public data: string;

    public constructor(init?: Partial<ReturnString>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'ReturnString'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return ''; }
}

// @Route("/sendjson")
export class SendJson implements IReturn<string>
{
    public id: number;
    public name?: string;
    public requestStream: string;

    public constructor(init?: Partial<SendJson>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'SendJson'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return ''; }
}

// @Route("/sendtext")
export class SendText implements IReturn<string>
{
    public id: number;
    public name?: string;
    public contentType?: string;
    public requestStream: string;

    public constructor(init?: Partial<SendText>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'SendText'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return ''; }
}

// @Route("/sendraw")
export class SendRaw implements IReturn<Blob>
{
    public id: number;
    public name?: string;
    public contentType?: string;
    public requestStream: string;

    public constructor(init?: Partial<SendRaw>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'SendRaw'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new Blob(); }
}

export class SendDefault implements IReturn<SendVerbResponse>
{
    public id: number;

    public constructor(init?: Partial<SendDefault>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'SendDefault'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new SendVerbResponse(); }
}

// @Route("/sendrestget/{Id}", "GET")
export class SendRestGet implements IReturn<SendVerbResponse>, IGet
{
    public id: number;

    public constructor(init?: Partial<SendRestGet>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'SendRestGet'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new SendVerbResponse(); }
}

export class SendGet implements IReturn<SendVerbResponse>, IGet
{
    public id: number;

    public constructor(init?: Partial<SendGet>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'SendGet'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new SendVerbResponse(); }
}

export class SendPost implements IReturn<SendVerbResponse>, IPost
{
    public id: number;

    public constructor(init?: Partial<SendPost>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'SendPost'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new SendVerbResponse(); }
}

export class SendPut implements IReturn<SendVerbResponse>, IPut
{
    public id: number;

    public constructor(init?: Partial<SendPut>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'SendPut'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new SendVerbResponse(); }
}

export class SendReturnVoid implements IReturnVoid
{
    public id: number;

    public constructor(init?: Partial<SendReturnVoid>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'SendReturnVoid'; }
    public getMethod() { return 'POST'; }
    public createResponse() {}
}

export class HelloAuth implements IReturn<HelloResponse>
{
    public name: string;

    public constructor(init?: Partial<HelloAuth>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloAuth'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new HelloResponse(); }
}

// @Route("/testauth")
export class TestAuth implements IReturn<TestAuthResponse>
{

    public constructor(init?: Partial<TestAuth>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'TestAuth'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new TestAuthResponse(); }
}

export class HelloAllTypes implements IReturn<HelloAllTypesResponse>
{
    public name: string;
    public allTypes: AllTypes;
    public allCollectionTypes: AllCollectionTypes;

    public constructor(init?: Partial<HelloAllTypes>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloAllTypes'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new HelloAllTypesResponse(); }
}

// @Route("/throw/{Type}")
export class ThrowType implements IReturn<ThrowTypeResponse>
{
    public type?: string;
    public message?: string;

    public constructor(init?: Partial<ThrowType>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'ThrowType'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new ThrowTypeResponse(); }
}

// @Route("/throwvalidation")
export class ThrowValidation implements IReturn<ThrowValidationResponse>
{
    public age: number;
    public required: string;
    public email: string;

    public constructor(init?: Partial<ThrowValidation>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'ThrowValidation'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new ThrowValidationResponse(); }
}

export class ProfileGen implements IReturn<ProfileGenResponse>
{

    public constructor(init?: Partial<ProfileGen>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'ProfileGen'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new ProfileGenResponse(); }
}

export class HelloReturnVoid implements IReturnVoid
{
    public id: number;

    public constructor(init?: Partial<HelloReturnVoid>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloReturnVoid'; }
    public getMethod() { return 'POST'; }
    public createResponse() {}
}

export class HelloList implements IReturn<ListResult[]>
{
    public names: string[] = [];

    public constructor(init?: Partial<HelloList>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloList'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new Array<ListResult>(); }
}

export class HelloWithEnum
{
    public enumProp: EnumType;
    public enumTypeFlags: EnumTypeFlags;
    public enumWithValues: EnumWithValues;
    public nullableEnumProp?: EnumType;
    public enumFlags: EnumFlags;
    public enumAsInt: EnumAsInt;
    public enumStyle: EnumStyle;
    public enumStyleMembers: EnumStyleMembers;

    public constructor(init?: Partial<HelloWithEnum>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloWithEnum'; }
    public getMethod() { return 'POST'; }
    public createResponse() {}
}

export class HelloWithEnumList
{
    public enumProp: EnumType[] = [];
    public enumWithValues: EnumWithValues[] = [];
    public nullableEnumProp: EnumType[] = [];
    public enumFlags: EnumFlags[] = [];
    public enumStyle: EnumStyle[] = [];

    public constructor(init?: Partial<HelloWithEnumList>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloWithEnumList'; }
    public getMethod() { return 'POST'; }
    public createResponse() {}
}

export class HelloWithEnumMap
{
    public enumProp: { [index:string]: EnumType; } = {};
    public enumWithValues: { [index:string]: EnumWithValues; } = {};
    public nullableEnumProp: { [index:string]: EnumType; } = {};
    public enumFlags: { [index:string]: EnumFlags; } = {};
    public enumStyle: { [index:string]: EnumStyle; } = {};

    public constructor(init?: Partial<HelloWithEnumMap>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloWithEnumMap'; }
    public getMethod() { return 'POST'; }
    public createResponse() {}
}

export class HelloSubAllTypes extends AllTypesBase implements IReturn<SubAllTypes>
{
    public hierarchy: number;

    public constructor(init?: Partial<HelloSubAllTypes>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'HelloSubAllTypes'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new SubAllTypes(); }
}

// @Route("/certificate/pdf")
export class GetCertificateOfParticipationPdf implements IReturn<Blob>, IGet
{
    // @Validate(Validator="NotEmpty")
    public name: string;

    public constructor(init?: Partial<GetCertificateOfParticipationPdf>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetCertificateOfParticipationPdf'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new Blob(); }
}

/** @description Chat Completions API (OpenAI-Compatible) */
// @Route("/v1/chat/completions", "POST")
// @DataContract
export class ChatCompletion implements IReturn<ChatResponse>, IPost
{
    /** @description The messages to generate chat completions for. */
    // @DataMember(Name="messages")
    public messages: AiMessage[] = [];

    /** @description ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API */
    // @DataMember(Name="model")
    public model: string;

    /** @description Parameters for audio output. Required when audio output is requested with modalities: [audio] */
    // @DataMember(Name="audio")
    public audio?: AiChatAudio;

    /** @description Modify the likelihood of specified tokens appearing in the completion. */
    // @DataMember(Name="logit_bias")
    public logit_bias?: { [index:number]: number; };

    /** @description Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format. */
    // @DataMember(Name="metadata")
    public metadata?: { [index:string]: string; };

    /** @description Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response. */
    // @DataMember(Name="reasoning_effort")
    public reasoning_effort?: string;

    /** @description An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON. */
    // @DataMember(Name="response_format")
    public response_format?: AiResponseFormat;

    /** @description Specifies the processing type used for serving the request. */
    // @DataMember(Name="service_tier")
    public service_tier?: string;

    /** @description A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user. */
    // @DataMember(Name="safety_identifier")
    public safety_identifier?: string;

    /** @description Up to 4 sequences where the API will stop generating further tokens. */
    // @DataMember(Name="stop")
    public stop?: string[];

    /** @description Output types that you would like the model to generate. Most models are capable of generating text, which is the default: */
    // @DataMember(Name="modalities")
    public modalities?: string[];

    /** @description Used by OpenAI to cache responses for similar requests to optimize your cache hit rates. */
    // @DataMember(Name="prompt_cache_key")
    public prompt_cache_key?: string;

    /** @description A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported. */
    // @DataMember(Name="tools")
    public tools?: Tool[];

    /** @description Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high. */
    // @DataMember(Name="verbosity")
    public verbosity?: string;

    /** @description What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic. */
    // @DataMember(Name="temperature")
    public temperature?: number;

    /** @description An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens. */
    // @DataMember(Name="max_completion_tokens")
    public max_completion_tokens?: number;

    /** @description An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used. */
    // @DataMember(Name="top_logprobs")
    public top_logprobs?: number;

    /** @description An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. */
    // @DataMember(Name="top_p")
    public top_p?: number;

    /** @description Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim. */
    // @DataMember(Name="frequency_penalty")
    public frequency_penalty?: number;

    /** @description Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics. */
    // @DataMember(Name="presence_penalty")
    public presence_penalty?: number;

    /** @description This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend. */
    // @DataMember(Name="seed")
    public seed?: number;

    /** @description How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs. */
    // @DataMember(Name="n")
    public n?: number;

    /** @description Whether or not to store the output of this chat completion request for use in our model distillation or evals products. */
    // @DataMember(Name="store")
    public store?: boolean;

    /** @description Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message. */
    // @DataMember(Name="logprobs")
    public logprobs?: boolean;

    /** @description Whether to enable parallel function calling during tool use. */
    // @DataMember(Name="parallel_tool_calls")
    public parallel_tool_calls?: boolean;

    /** @description Whether to enable thinking mode for some Qwen models and providers. */
    // @DataMember(Name="enable_thinking")
    public enable_thinking?: boolean;

    /** @description If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message. */
    // @DataMember(Name="stream")
    public stream?: boolean;

    public constructor(init?: Partial<ChatCompletion>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'ChatCompletion'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new ChatResponse(); }
}

/** @description Sign In */
// @Route("/auth", "GET,POST")
// @Route("/auth/{provider}", "POST")
// @Api(Description="Sign In")
// @DataContract
export class Authenticate implements IReturn<AuthenticateResponse>, IPost
{
    /** @description AuthProvider, e.g. credentials */
    // @DataMember(Order=1)
    public provider?: string;

    // @DataMember(Order=2)
    public userName?: string;

    // @DataMember(Order=3)
    public password?: string;

    // @DataMember(Order=4)
    public rememberMe?: boolean;

    // @DataMember(Order=5)
    public accessToken?: string;

    // @DataMember(Order=6)
    public accessTokenSecret?: string;

    // @DataMember(Order=7)
    public returnUrl?: string;

    // @DataMember(Order=8)
    public errorView?: string;

    // @DataMember(Order=9)
    public meta?: { [index:string]: string; };

    public constructor(init?: Partial<Authenticate>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'Authenticate'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AuthenticateResponse(); }
}

// @Route("/albums", "GET")
// @Route("/albums/{AlbumId}", "GET")
export class QueryAlbums extends QueryDb<Albums> implements IReturn<QueryResponse<Albums>>
{
    public albumId?: number;

    public constructor(init?: Partial<QueryAlbums>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAlbums'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Albums>(); }
}

// @Route("/artists", "GET")
// @Route("/artists/{ArtistId}", "GET")
export class QueryArtists extends QueryDb<Artists> implements IReturn<QueryResponse<Artists>>
{
    public artistId?: number;
    public artistIdBetween: number[];
    public nameStartsWith: string;

    public constructor(init?: Partial<QueryArtists>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryArtists'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Artists>(); }
}

// @Route("/chinook/customers", "GET")
// @Route("/chinook/customers/{CustomerId}", "GET")
export class QueryChinookCustomers extends QueryDb<Customers> implements IReturn<QueryResponse<Customers>>
{
    public customerId?: number;

    public constructor(init?: Partial<QueryChinookCustomers>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChinookCustomers'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Customers>(); }
}

// @Route("/chinook/employees", "GET")
// @Route("/chinook/employees/{EmployeeId}", "GET")
export class QueryChinookEmployees extends QueryDb<Employees> implements IReturn<QueryResponse<Employees>>
{
    public employeeId?: number;

    public constructor(init?: Partial<QueryChinookEmployees>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChinookEmployees'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Employees>(); }
}

// @Route("/genres", "GET")
// @Route("/genres/{GenreId}", "GET")
export class QueryGenres extends QueryDb<Genres> implements IReturn<QueryResponse<Genres>>
{
    public genreId?: number;

    public constructor(init?: Partial<QueryGenres>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryGenres'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Genres>(); }
}

// @Route("/invoiceitems", "GET")
// @Route("/invoiceitems/{InvoiceLineId}", "GET")
export class QueryInvoiceItems extends QueryDb<InvoiceItems> implements IReturn<QueryResponse<InvoiceItems>>
{
    public invoiceLineId?: number;

    public constructor(init?: Partial<QueryInvoiceItems>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryInvoiceItems'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<InvoiceItems>(); }
}

// @Route("/invoices", "GET")
// @Route("/invoices/{InvoiceId}", "GET")
export class QueryInvoices extends QueryDb<Invoices> implements IReturn<QueryResponse<Invoices>>
{
    public invoiceId?: number;

    public constructor(init?: Partial<QueryInvoices>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryInvoices'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Invoices>(); }
}

// @Route("/mediatypes", "GET")
// @Route("/mediatypes/{MediaTypeId}", "GET")
export class QueryMediaTypes extends QueryDb<MediaTypes> implements IReturn<QueryResponse<MediaTypes>>
{
    public mediaTypeId?: number;

    public constructor(init?: Partial<QueryMediaTypes>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryMediaTypes'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<MediaTypes>(); }
}

// @Route("/playlists", "GET")
// @Route("/playlists/{PlaylistId}", "GET")
export class QueryPlaylists extends QueryDb<Playlists> implements IReturn<QueryResponse<Playlists>>
{
    public playlistId?: number;

    public constructor(init?: Partial<QueryPlaylists>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryPlaylists'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Playlists>(); }
}

// @Route("/tracks", "GET")
// @Route("/tracks/{TrackId}", "GET")
export class QueryTracks extends QueryDb<Tracks> implements IReturn<QueryResponse<Tracks>>
{
    public trackId?: number;
    public nameContains: string;

    public constructor(init?: Partial<QueryTracks>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryTracks'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Tracks>(); }
}

export class QueryJobApplicationAttachment extends QueryDb<JobApplicationAttachment> implements IReturn<QueryResponse<JobApplicationAttachment>>
{
    public id?: number;

    public constructor(init?: Partial<QueryJobApplicationAttachment>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryJobApplicationAttachment'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<JobApplicationAttachment>(); }
}

export class QueryContacts extends QueryDb<Contact> implements IReturn<QueryResponse<Contact>>
{
    public id?: number;

    public constructor(init?: Partial<QueryContacts>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryContacts'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Contact>(); }
}

export class QueryJob extends QueryDb<Job> implements IReturn<QueryResponse<Job>>
{
    public id?: number;
    public ids?: number[];

    public constructor(init?: Partial<QueryJob>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryJob'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Job>(); }
}

export class QueryJobApplication extends QueryDb<JobApplication> implements IReturn<QueryResponse<JobApplication>>
{
    public id?: number;
    public ids?: number[];
    public jobId?: number;

    public constructor(init?: Partial<QueryJobApplication>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryJobApplication'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<JobApplication>(); }
}

export class QueryPhoneScreen extends QueryDb<PhoneScreen> implements IReturn<QueryResponse<PhoneScreen>>
{
    public id?: number;
    public jobApplicationId?: number;

    public constructor(init?: Partial<QueryPhoneScreen>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryPhoneScreen'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<PhoneScreen>(); }
}

export class QueryInterview extends QueryDb<Interview> implements IReturn<QueryResponse<Interview>>
{
    public id?: number;
    public jobApplicationId?: number;

    public constructor(init?: Partial<QueryInterview>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryInterview'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Interview>(); }
}

export class QueryJobOffer extends QueryDb<JobOffer> implements IReturn<QueryResponse<JobOffer>>
{
    public id?: number;
    public jobApplicationId?: number;

    public constructor(init?: Partial<QueryJobOffer>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryJobOffer'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<JobOffer>(); }
}

export class QueryJobAppEvents extends QueryDb<JobApplicationEvent> implements IReturn<QueryResponse<JobApplicationEvent>>
{
    public jobApplicationId?: number;

    public constructor(init?: Partial<QueryJobAppEvents>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryJobAppEvents'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<JobApplicationEvent>(); }
}

// @ValidateRequest(Validator="IsAuthenticated")
export class QueryApplicationUser extends QueryDb<ApplicationUser> implements IReturn<QueryResponse<ApplicationUser>>
{
    public emailContains?: string;
    public firstNameContains?: string;
    public lastNameContains?: string;

    public constructor(init?: Partial<QueryApplicationUser>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryApplicationUser'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ApplicationUser>(); }
}

export class QueryJobApplicationComments extends QueryDb<JobApplicationComment> implements IReturn<QueryResponse<JobApplicationComment>>
{
    public jobApplicationId?: number;

    public constructor(init?: Partial<QueryJobApplicationComments>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryJobApplicationComments'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<JobApplicationComment>(); }
}

/** @description Find Bookings */
// @Route("/bookings", "GET")
// @Route("/bookings/{Id}", "GET")
export class QueryBookings extends QueryDb<Booking> implements IReturn<QueryResponse<Booking>>
{
    public id?: number;

    public constructor(init?: Partial<QueryBookings>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryBookings'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Booking>(); }
}

/** @description Find Coupons */
// @Route("/coupons", "GET")
export class QueryCoupons extends QueryDb<Coupon> implements IReturn<QueryResponse<Coupon>>
{
    public id: string;

    public constructor(init?: Partial<QueryCoupons>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryCoupons'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Coupon>(); }
}

export class QueryAddresses extends QueryDb<Address> implements IReturn<QueryResponse<Address>>
{
    public ids: number[];

    public constructor(init?: Partial<QueryAddresses>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAddresses'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Address>(); }
}

export class QueryFileSystemItems extends QueryDb<FileSystemItem> implements IReturn<QueryResponse<FileSystemItem>>
{
    public appUserId?: number;
    public fileAccessType?: FileAccessType;

    public constructor(init?: Partial<QueryFileSystemItems>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryFileSystemItems'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<FileSystemItem>(); }
}

export class QueryFileSystemFiles extends QueryDb<FileSystemFile> implements IReturn<QueryResponse<FileSystemFile>>
{

    public constructor(init?: Partial<QueryFileSystemFiles>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryFileSystemFiles'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<FileSystemFile>(); }
}

export class QueryPlayer extends QueryDb<Player> implements IReturn<QueryResponse<Player>>
{

    public constructor(init?: Partial<QueryPlayer>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryPlayer'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Player>(); }
}

export class QueryProfile extends QueryDb<Profile> implements IReturn<QueryResponse<Profile>>
{

    public constructor(init?: Partial<QueryProfile>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryProfile'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Profile>(); }
}

export class QueryGameItem extends QueryDb<GameItem> implements IReturn<QueryResponse<GameItem>>
{
    public name: string;

    public constructor(init?: Partial<QueryGameItem>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryGameItem'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<GameItem>(); }
}

export class QueryPlayerGameItem extends QueryDb<PlayerGameItem> implements IReturn<QueryResponse<PlayerGameItem>>
{
    public id?: number;
    public playerId?: number;
    public gameItemName?: string;

    public constructor(init?: Partial<QueryPlayerGameItem>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryPlayerGameItem'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<PlayerGameItem>(); }
}

export class QueryLevel extends QueryDb<Level> implements IReturn<QueryResponse<Level>>
{
    public id?: string;

    public constructor(init?: Partial<QueryLevel>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryLevel'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Level>(); }
}

// @Route("/todos", "GET")
export class QueryTodos extends QueryDb<Todo> implements IReturn<QueryResponse<Todo>>
{
    public id?: number;
    public ids?: number[];
    public textContains?: string;

    public constructor(init?: Partial<QueryTodos>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryTodos'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Todo>(); }
}

// @Route("/agentruns", "GET")
// @Route("/agentruns/{Id}", "GET")
// @DataContract
export class QueryAgentRuns extends QueryDb<AgentRun> implements IReturn<QueryResponse<AgentRun>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryAgentRuns>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAgentRuns'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<AgentRun>(); }
}

// @Route("/agentsteps", "GET")
// @Route("/agentsteps/{Id}", "GET")
// @DataContract
export class QueryAgentSteps extends QueryDb<AgentStep> implements IReturn<QueryResponse<AgentStep>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryAgentSteps>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAgentSteps'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<AgentStep>(); }
}

// @Route("/aichatdocuments", "GET")
// @Route("/aichatdocuments/{Id}", "GET")
// @DataContract
export class QueryAichatDocuments extends QueryDb<AichatDocument> implements IReturn<QueryResponse<AichatDocument>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryAichatDocuments>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAichatDocuments'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<AichatDocument>(); }
}

// @Route("/aichatfilestores", "GET")
// @Route("/aichatfilestores/{Id}", "GET")
// @DataContract
export class QueryAichatFilestores extends QueryDb<AichatFilestore> implements IReturn<QueryResponse<AichatFilestore>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryAichatFilestores>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAichatFilestores'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<AichatFilestore>(); }
}

// @Route("/aichatmedias", "GET")
// @Route("/aichatmedias/{Id}", "GET")
// @DataContract
export class QueryAichatMedias extends QueryDb<AichatMedia> implements IReturn<QueryResponse<AichatMedia>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryAichatMedias>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAichatMedias'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<AichatMedia>(); }
}

// @Route("/aspnetroleclaims", "GET")
// @Route("/aspnetroleclaims/{Id}", "GET")
// @DataContract
export class QueryAspNetRoleClaims extends QueryDb<AspNetRoleClaims> implements IReturn<QueryResponse<AspNetRoleClaims>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryAspNetRoleClaims>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAspNetRoleClaims'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<AspNetRoleClaims>(); }
}

// @Route("/aspnetroles", "GET")
// @Route("/aspnetroles/{Id}", "GET")
// @DataContract
export class QueryAspNetRoles extends QueryDb<AspNetRoles> implements IReturn<QueryResponse<AspNetRoles>>, IGet
{
    // @DataMember(Order=1)
    public id?: string;

    public constructor(init?: Partial<QueryAspNetRoles>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAspNetRoles'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<AspNetRoles>(); }
}

// @Route("/aspnetuserclaims", "GET")
// @Route("/aspnetuserclaims/{Id}", "GET")
// @ValidateRequest(Validator="IsAdmin")
// @DataContract
export class QueryAspNetUserClaims extends QueryDb<AspNetUserClaims> implements IReturn<QueryResponse<AspNetUserClaims>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryAspNetUserClaims>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAspNetUserClaims'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<AspNetUserClaims>(); }
}

// @Route("/aspnetusers", "GET")
// @Route("/aspnetusers/{Id}", "GET")
// @ValidateRequest(Validator="IsAdmin")
// @DataContract
export class QueryAspNetUsers extends QueryDb<AspNetUsers> implements IReturn<QueryResponse<AspNetUsers>>, IGet
{
    // @DataMember(Order=1)
    public id?: string;

    public constructor(init?: Partial<QueryAspNetUsers>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryAspNetUsers'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<AspNetUsers>(); }
}

// @Route("/categories", "GET")
// @Route("/categories/{Id}", "GET")
// @DataContract
export class QueryCategories extends QueryDb<Category> implements IReturn<QueryResponse<Category>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryCategories>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryCategories'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Category>(); }
}

// @Route("/categoryoptions", "GET")
// @Route("/categoryoptions/{Id}", "GET")
// @DataContract
export class QueryCategoryOptions extends QueryDb<CategoryOption> implements IReturn<QueryResponse<CategoryOption>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryCategoryOptions>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryCategoryOptions'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<CategoryOption>(); }
}

// @Route("/chatassistantconversations", "GET")
// @Route("/chatassistantconversations/{Id}", "GET")
// @DataContract
export class QueryChatAssistantConversations extends QueryDb<ChatAssistantConversation> implements IReturn<QueryResponse<ChatAssistantConversation>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatAssistantConversations>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatAssistantConversations'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatAssistantConversation>(); }
}

// @Route("/chatassistantmessages", "GET")
// @Route("/chatassistantmessages/{Id}", "GET")
// @DataContract
export class QueryChatAssistantMessages extends QueryDb<ChatAssistantMessage> implements IReturn<QueryResponse<ChatAssistantMessage>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatAssistantMessages>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatAssistantMessages'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatAssistantMessage>(); }
}

// @Route("/chatassistants", "GET")
// @Route("/chatassistants/{Id}", "GET")
// @DataContract
export class QueryChatAssistants extends QueryDb<ChatAssistant> implements IReturn<QueryResponse<ChatAssistant>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatAssistants>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatAssistants'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatAssistant>(); }
}

// @Route("/chatdocuments", "GET")
// @Route("/chatdocuments/{Id}", "GET")
// @DataContract
export class QueryChatDocuments extends QueryDb<ChatDocument> implements IReturn<QueryResponse<ChatDocument>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatDocuments>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatDocuments'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatDocument>(); }
}

// @Route("/chatfilestores", "GET")
// @Route("/chatfilestores/{Id}", "GET")
// @DataContract
export class QueryChatFilestores extends QueryDb<ChatFilestore> implements IReturn<QueryResponse<ChatFilestore>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatFilestores>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatFilestores'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatFilestore>(); }
}

// @Route("/chatmedias", "GET")
// @Route("/chatmedias/{Id}", "GET")
// @DataContract
export class QueryChatMedias extends QueryDb<ChatMedia> implements IReturn<QueryResponse<ChatMedia>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatMedias>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatMedias'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatMedia>(); }
}

// @Route("/chatmessages", "GET")
// @Route("/chatmessages/{Id}", "GET")
// @DataContract
export class QueryChatMessages extends QueryDb<ChatMessage> implements IReturn<QueryResponse<ChatMessage>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatMessages>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatMessages'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatMessage>(); }
}

// @Route("/chatrequests", "GET")
// @Route("/chatrequests/{Id}", "GET")
// @DataContract
export class QueryChatRequests extends QueryDb<ChatRequest> implements IReturn<QueryResponse<ChatRequest>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatRequests>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatRequests'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatRequest>(); }
}

// @Route("/chatsourceruns", "GET")
// @Route("/chatsourceruns/{Id}", "GET")
// @DataContract
export class QueryChatSourceRuns extends QueryDb<ChatSourceRun> implements IReturn<QueryResponse<ChatSourceRun>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatSourceRuns>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatSourceRuns'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatSourceRun>(); }
}

// @Route("/chatsources", "GET")
// @Route("/chatsources/{Id}", "GET")
// @DataContract
export class QueryChatSources extends QueryDb<ChatSource> implements IReturn<QueryResponse<ChatSource>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatSources>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatSources'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatSource>(); }
}

// @Route("/chatthreads", "GET")
// @Route("/chatthreads/{Id}", "GET")
// @DataContract
export class QueryChatThreads extends QueryDb<ChatThread> implements IReturn<QueryResponse<ChatThread>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatThreads>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatThreads'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatThread>(); }
}

// @Route("/chattoolapprovalbatches", "GET")
// @Route("/chattoolapprovalbatches/{Id}", "GET")
// @DataContract
export class QueryChatToolApprovalBatches extends QueryDb<ChatToolApprovalBatch> implements IReturn<QueryResponse<ChatToolApprovalBatch>>, IGet
{
    // @DataMember(Order=1)
    public id?: string;

    public constructor(init?: Partial<QueryChatToolApprovalBatches>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatToolApprovalBatches'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatToolApprovalBatch>(); }
}

// @Route("/chattoolapprovals", "GET")
// @Route("/chattoolapprovals/{Id}", "GET")
// @DataContract
export class QueryChatToolApprovals extends QueryDb<ChatToolApproval> implements IReturn<QueryResponse<ChatToolApproval>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryChatToolApprovals>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryChatToolApprovals'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ChatToolApproval>(); }
}

// @Route("/coffeeshoporderitems", "GET")
// @Route("/coffeeshoporderitems/{Id}", "GET")
// @DataContract
export class QueryCoffeeShopOrderItems extends QueryDb<CoffeeShopOrderItem> implements IReturn<QueryResponse<CoffeeShopOrderItem>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryCoffeeShopOrderItems>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryCoffeeShopOrderItems'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<CoffeeShopOrderItem>(); }
}

// @Route("/coffeeshoporders", "GET")
// @Route("/coffeeshoporders/{Id}", "GET")
// @DataContract
export class QueryCoffeeShopOrders extends QueryDb<CoffeeShopOrder> implements IReturn<QueryResponse<CoffeeShopOrder>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryCoffeeShopOrders>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryCoffeeShopOrders'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<CoffeeShopOrder>(); }
}

// @Route("/contextsnapshots", "GET")
// @Route("/contextsnapshots/{Id}", "GET")
// @DataContract
export class QueryContextSnapshots extends QueryDb<ContextSnapshot> implements IReturn<QueryResponse<ContextSnapshot>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryContextSnapshots>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryContextSnapshots'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ContextSnapshot>(); }
}

// @Route("/efmigrationshistories", "GET")
// @Route("/efmigrationshistories/{MigrationId}", "GET")
// @DataContract
export class QueryEFMigrationsHistories extends QueryDb<EFMigrationsHistory> implements IReturn<QueryResponse<EFMigrationsHistory>>, IGet
{
    // @DataMember(Order=1)
    public migrationId?: string;

    public constructor(init?: Partial<QueryEFMigrationsHistories>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryEFMigrationsHistories'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<EFMigrationsHistory>(); }
}

// @Route("/efmigrationslocks", "GET")
// @Route("/efmigrationslocks/{Id}", "GET")
// @DataContract
export class QueryEFMigrationsLocks extends QueryDb<EFMigrationsLock> implements IReturn<QueryResponse<EFMigrationsLock>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryEFMigrationsLocks>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryEFMigrationsLocks'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<EFMigrationsLock>(); }
}

// @Route("/migrations", "GET")
// @Route("/migrations/{Id}", "GET")
// @DataContract
export class QueryMigrations extends QueryDb<Migration> implements IReturn<QueryResponse<Migration>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryMigrations>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryMigrations'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Migration>(); }
}

// @Route("/optionquantities", "GET")
// @Route("/optionquantities/{Id}", "GET")
// @DataContract
export class QueryOptionQuantities extends QueryDb<OptionQuantity> implements IReturn<QueryResponse<OptionQuantity>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryOptionQuantities>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryOptionQuantities'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<OptionQuantity>(); }
}

// @Route("/options", "GET")
// @Route("/options/{Id}", "GET")
// @DataContract
export class QueryOptions extends QueryDb<Option> implements IReturn<QueryResponse<Option>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryOptions>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryOptions'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Option>(); }
}

// @Route("/products", "GET")
// @Route("/products/{Id}", "GET")
// @DataContract
export class QueryProducts extends QueryDb<Product> implements IReturn<QueryResponse<Product>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryProducts>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryProducts'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<Product>(); }
}

// @Route("/validationrules", "GET")
// @Route("/validationrules/{Id}", "GET")
// @DataContract
export class QueryValidationRules extends QueryDb<ValidationRule> implements IReturn<QueryResponse<ValidationRule>>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    public constructor(init?: Partial<QueryValidationRules>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'QueryValidationRules'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ValidationRule>(); }
}

// @Route("/albums", "POST")
export class CreateAlbums implements IReturn<IdResponse>, IPost, ICreateDb<Albums>
{
    // @Validate(Validator="NotEmpty")
    public title: string;

    // @Validate(Validator="GreaterThan(0)")
    public artistId: number;

    public constructor(init?: Partial<CreateAlbums>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAlbums'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/artists", "POST")
export class CreateArtists implements IReturn<IdResponse>, IPost, ICreateDb<Artists>
{
    public name: string;

    public constructor(init?: Partial<CreateArtists>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateArtists'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chinook/customers", "POST")
export class CreateChinookCustomer implements IReturn<IdResponse>, IPost, ICreateDb<Customers>
{
    public firstName: string;
    public lastName: string;
    public company: string;
    public address: string;
    public city: string;
    public state: string;
    public country: string;
    public postalCode: string;
    public phone: string;
    public fax: string;
    public email: string;
    public supportRepId?: number;

    public constructor(init?: Partial<CreateChinookCustomer>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChinookCustomer'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chinook/employees", "POST")
export class CreateChinookEmployee implements IReturn<IdResponse>, IPost, ICreateDb<Employees>
{
    public lastName: string;
    public firstName: string;
    public title: string;
    public reportsTo?: number;
    public birthDate?: string;
    public hireDate?: string;
    public address: string;
    public city: string;
    public state: string;
    public country: string;
    public postalCode: string;
    public phone: string;
    public fax: string;
    public email: string;

    public constructor(init?: Partial<CreateChinookEmployee>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChinookEmployee'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/genres", "POST")
export class CreateGenres implements IReturn<IdResponse>, IPost, ICreateDb<Genres>
{
    // @Validate(Validator="NotEmpty")
    public name: string;

    public constructor(init?: Partial<CreateGenres>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateGenres'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/invoiceitems", "POST")
export class CreateInvoiceItems implements IReturn<IdResponse>, IPost, ICreateDb<InvoiceItems>
{
    public invoiceId: number;
    public trackId: number;
    public unitPrice: number;
    public quantity: number;

    public constructor(init?: Partial<CreateInvoiceItems>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateInvoiceItems'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/invoices", "POST")
export class CreateInvoices implements IReturn<IdResponse>, IPost, ICreateDb<Invoices>
{
    public customerId: number;
    public invoiceDate: string;
    public billingAddress: string;
    public billingCity: string;
    public billingState: string;
    public billingCountry: string;
    public billingPostalCode: string;
    public total: number;

    public constructor(init?: Partial<CreateInvoices>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateInvoices'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/mediatypes", "POST")
export class CreateMediaTypes implements IReturn<IdResponse>, IPost, ICreateDb<MediaTypes>
{
    public name: string;

    public constructor(init?: Partial<CreateMediaTypes>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateMediaTypes'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/playlists", "POST")
export class CreatePlaylists implements IReturn<IdResponse>, IPost, ICreateDb<Playlists>
{
    public name: string;

    public constructor(init?: Partial<CreatePlaylists>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreatePlaylists'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/tracks", "POST")
export class CreateTracks implements IReturn<IdResponse>, IPost, ICreateDb<Tracks>
{
    public name: string;
    public albumId?: number;
    public mediaTypeId: number;
    public genreId?: number;
    public composer: string;
    public milliseconds: number;
    public bytes?: number;
    public unitPrice: number;

    public constructor(init?: Partial<CreateTracks>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateTracks'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/albums/{AlbumId}", "DELETE")
export class DeleteAlbums implements IReturn<IdResponse>, IDelete, IDeleteDb<Albums>
{
    public albumId: number;

    public constructor(init?: Partial<DeleteAlbums>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAlbums'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/artists/{ArtistId}", "DELETE")
export class DeleteArtists implements IReturn<IdResponse>, IDelete, IDeleteDb<Artists>
{
    public artistId: number;

    public constructor(init?: Partial<DeleteArtists>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteArtists'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chinook/customers/{CustomerId}", "DELETE")
export class DeleteChinookCustomer implements IReturn<IdResponse>, IDelete, IDeleteDb<Customers>
{
    public customerId: number;

    public constructor(init?: Partial<DeleteChinookCustomer>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChinookCustomer'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chinook/employees/{EmployeeId}", "DELETE")
export class DeleteChinookEmployee implements IReturn<IdResponse>, IDelete, IDeleteDb<Employees>
{
    public employeeId: number;

    public constructor(init?: Partial<DeleteChinookEmployee>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChinookEmployee'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/genres/{GenreId}", "DELETE")
export class DeleteGenres implements IReturn<IdResponse>, IDelete, IDeleteDb<Genres>
{
    public genreId: number;

    public constructor(init?: Partial<DeleteGenres>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteGenres'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/invoiceitems/{InvoiceLineId}", "DELETE")
export class DeleteInvoiceItems implements IReturn<IdResponse>, IDelete, IDeleteDb<InvoiceItems>
{
    public invoiceLineId: number;

    public constructor(init?: Partial<DeleteInvoiceItems>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteInvoiceItems'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/invoices/{InvoiceId}", "DELETE")
export class DeleteInvoices implements IReturn<IdResponse>, IDelete, IDeleteDb<Invoices>
{
    public invoiceId: number;

    public constructor(init?: Partial<DeleteInvoices>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteInvoices'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/mediatypes/{MediaTypeId}", "DELETE")
export class DeleteMediaTypes implements IReturn<IdResponse>, IDelete, IDeleteDb<MediaTypes>
{
    public mediaTypeId: number;

    public constructor(init?: Partial<DeleteMediaTypes>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteMediaTypes'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/playlists/{PlaylistId}", "DELETE")
export class DeletePlaylists implements IReturn<IdResponse>, IDelete, IDeleteDb<Playlists>
{
    public playlistId: number;

    public constructor(init?: Partial<DeletePlaylists>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeletePlaylists'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/tracks/{TrackId}", "DELETE")
export class DeleteTracks implements IReturn<IdResponse>, IDelete, IDeleteDb<Tracks>
{
    public trackId: number;

    public constructor(init?: Partial<DeleteTracks>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteTracks'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/albums/{AlbumId}", "PATCH")
export class PatchAlbums implements IReturn<IdResponse>, IPatch, IPatchDb<Albums>
{
    public albumId: number;
    public title: string;
    public artistId: number;

    public constructor(init?: Partial<PatchAlbums>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchAlbums'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/artists/{ArtistId}", "PATCH")
export class PatchArtists implements IReturn<IdResponse>, IPatch, IPatchDb<Artists>
{
    public artistId: number;
    public name: string;

    public constructor(init?: Partial<PatchArtists>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchArtists'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chinook/customers/{CustomerId}", "PATCH")
export class PatchChinookCustomer implements IReturn<IdResponse>, IPatch, IPatchDb<Customers>
{
    public customerId: number;
    public firstName: string;
    public lastName: string;
    public company: string;
    public address: string;
    public city: string;
    public state: string;
    public country: string;
    public postalCode: string;
    public phone: string;
    public fax: string;
    public email: string;
    public supportRepId?: number;

    public constructor(init?: Partial<PatchChinookCustomer>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChinookCustomer'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chinook/employees/{EmployeeId}", "PATCH")
export class PatchChinookEmployee implements IReturn<IdResponse>, IPatch, IPatchDb<Employees>
{
    public employeeId: number;
    public lastName: string;
    public firstName: string;
    public title: string;
    public reportsTo?: number;
    public birthDate?: string;
    public hireDate?: string;
    public address: string;
    public city: string;
    public state: string;
    public country: string;
    public postalCode: string;
    public phone: string;
    public fax: string;
    public email: string;

    public constructor(init?: Partial<PatchChinookEmployee>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChinookEmployee'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/genres/{GenreId}", "PATCH")
export class PatchGenres implements IReturn<IdResponse>, IPatch, IPatchDb<Genres>
{
    public genreId: number;
    public name: string;

    public constructor(init?: Partial<PatchGenres>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchGenres'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/invoiceitems/{InvoiceLineId}", "PATCH")
export class PatchInvoiceItems implements IReturn<IdResponse>, IPatch, IPatchDb<InvoiceItems>
{
    public invoiceLineId: number;
    public invoiceId: number;
    public trackId: number;
    public unitPrice: number;
    public quantity: number;

    public constructor(init?: Partial<PatchInvoiceItems>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchInvoiceItems'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/invoices/{InvoiceId}", "PATCH")
export class PatchInvoices implements IReturn<IdResponse>, IPatch, IPatchDb<Invoices>
{
    public invoiceId: number;
    public customerId: number;
    public invoiceDate: string;
    public billingAddress: string;
    public billingCity: string;
    public billingState: string;
    public billingCountry: string;
    public billingPostalCode: string;
    public total: number;

    public constructor(init?: Partial<PatchInvoices>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchInvoices'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/mediatypes/{MediaTypeId}", "PATCH")
export class PatchMediaTypes implements IReturn<IdResponse>, IPatch, IPatchDb<MediaTypes>
{
    public mediaTypeId: number;
    public name: string;

    public constructor(init?: Partial<PatchMediaTypes>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchMediaTypes'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/playlists/{PlaylistId}", "PATCH")
export class PatchPlaylists implements IReturn<IdResponse>, IPatch, IPatchDb<Playlists>
{
    public playlistId: number;
    public name: string;

    public constructor(init?: Partial<PatchPlaylists>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchPlaylists'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/tracks/{TrackId}", "PATCH")
export class PatchTracks implements IReturn<IdResponse>, IPatch, IPatchDb<Tracks>
{
    public trackId: number;
    public name: string;
    public albumId?: number;
    public mediaTypeId: number;
    public genreId?: number;
    public composer: string;
    public milliseconds: number;
    public bytes?: number;
    public unitPrice: number;

    public constructor(init?: Partial<PatchTracks>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchTracks'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/albums/{AlbumId}", "PUT")
export class UpdateAlbums implements IReturn<IdResponse>, IPut, IUpdateDb<Albums>
{
    public albumId: number;
    public title: string;
    public artistId: number;

    public constructor(init?: Partial<UpdateAlbums>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAlbums'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/artists/{ArtistId}", "PUT")
export class UpdateArtists implements IReturn<IdResponse>, IPut, IUpdateDb<Artists>
{
    public artistId: number;
    public name: string;

    public constructor(init?: Partial<UpdateArtists>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateArtists'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chinook/customers/{CustomerId}", "PUT")
export class UpdateChinookCustomer implements IReturn<IdResponse>, IPut, IUpdateDb<Customers>
{
    public customerId: number;
    public firstName: string;
    public lastName: string;
    public company: string;
    public address: string;
    public city: string;
    public state: string;
    public country: string;
    public postalCode: string;
    public phone: string;
    public fax: string;
    public email: string;
    public supportRepId?: number;

    public constructor(init?: Partial<UpdateChinookCustomer>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChinookCustomer'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chinook/employees/{EmployeeId}", "PUT")
export class UpdateChinookEmployee implements IReturn<IdResponse>, IPut, IUpdateDb<Employees>
{
    public employeeId: number;
    public lastName: string;
    public firstName: string;
    public title: string;
    public reportsTo?: number;
    public birthDate?: string;
    public hireDate?: string;
    public address: string;
    public city: string;
    public state: string;
    public country: string;
    public postalCode: string;
    public phone: string;
    public fax: string;
    public email: string;

    public constructor(init?: Partial<UpdateChinookEmployee>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChinookEmployee'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/genres/{GenreId}", "PUT")
export class UpdateGenres implements IReturn<IdResponse>, IPut, IUpdateDb<Genres>
{
    public genreId: number;
    public name: string;

    public constructor(init?: Partial<UpdateGenres>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateGenres'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/invoiceitems/{InvoiceLineId}", "PUT")
export class UpdateInvoiceItems implements IReturn<IdResponse>, IPut, IUpdateDb<InvoiceItems>
{
    public invoiceLineId: number;
    public invoiceId: number;
    public trackId: number;
    public unitPrice: number;
    public quantity: number;

    public constructor(init?: Partial<UpdateInvoiceItems>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateInvoiceItems'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/invoices/{InvoiceId}", "PUT")
export class UpdateInvoices implements IReturn<IdResponse>, IPut, IUpdateDb<Invoices>
{
    public invoiceId: number;
    public customerId: number;
    public invoiceDate: string;
    public billingAddress: string;
    public billingCity: string;
    public billingState: string;
    public billingCountry: string;
    public billingPostalCode: string;
    public total: number;

    public constructor(init?: Partial<UpdateInvoices>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateInvoices'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/mediatypes/{MediaTypeId}", "PUT")
export class UpdateMediaTypes implements IReturn<IdResponse>, IPut, IUpdateDb<MediaTypes>
{
    public mediaTypeId: number;
    public name: string;

    public constructor(init?: Partial<UpdateMediaTypes>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateMediaTypes'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/playlists/{PlaylistId}", "PUT")
export class UpdatePlaylists implements IReturn<IdResponse>, IPut, IUpdateDb<Playlists>
{
    public playlistId: number;
    public name: string;

    public constructor(init?: Partial<UpdatePlaylists>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdatePlaylists'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/tracks/{TrackId}", "PUT")
export class UpdateTracks implements IReturn<IdResponse>, IPut, IUpdateDb<Tracks>
{
    public trackId: number;
    public name: string;
    public albumId?: number;
    public mediaTypeId: number;
    public genreId?: number;
    public composer: string;
    public milliseconds: number;
    public bytes?: number;
    public unitPrice: number;

    public constructor(init?: Partial<UpdateTracks>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateTracks'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

export class CreateContact implements IReturn<Contact>, ICreateDb<Contact>
{
    // @Validate(Validator="NotEmpty")
    public firstName: string;

    // @Validate(Validator="NotEmpty")
    public lastName: string;

    public profileUrl?: string;
    public salaryExpectation?: number;
    // @Validate(Validator="NotEmpty")
    public jobType: string;

    public availabilityWeeks: number;
    public preferredWorkType: EmploymentType;
    // @Validate(Validator="NotEmpty")
    public preferredLocation: string;

    // @Validate(Validator="NotEmpty")
    public email: string;

    public phone?: string;
    public about?: string;

    public constructor(init?: Partial<CreateContact>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateContact'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new Contact(); }
}

export class UpdateContact implements IReturn<Contact>, IPatchDb<Contact>
{
    public id: number;
    // @Validate(Validator="NotEmpty")
    public firstName: string;

    // @Validate(Validator="NotEmpty")
    public lastName: string;

    public profileUrl?: string;
    public salaryExpectation?: number;
    // @Validate(Validator="NotEmpty")
    public jobType: string;

    public availabilityWeeks?: number;
    public preferredWorkType?: EmploymentType;
    public preferredLocation?: string;
    // @Validate(Validator="NotEmpty")
    public email: string;

    public phone?: string;
    public about?: string;

    public constructor(init?: Partial<UpdateContact>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateContact'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new Contact(); }
}

export class DeleteContact implements IReturnVoid, IDeleteDb<Contact>
{
    public id: number;

    public constructor(init?: Partial<DeleteContact>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteContact'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class CreateJob implements IReturn<Job>, ICreateDb<Job>
{
    public title: string;
    // @Validate(Validator="GreaterThan(0)")
    public salaryRangeLower: number;

    // @Validate(Validator="GreaterThan(0)")
    public salaryRangeUpper: number;

    public description: string;
    public employmentType: EmploymentType;
    public company: string;
    public location: string;
    public closing: string;

    public constructor(init?: Partial<CreateJob>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateJob'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new Job(); }
}

export class UpdateJob implements IReturn<Job>, IPatchDb<Job>
{
    public id: number;
    public title?: string;
    public salaryRangeLower?: number;
    public salaryRangeUpper?: number;
    public description?: string;

    public constructor(init?: Partial<UpdateJob>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateJob'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new Job(); }
}

export class DeleteJob implements IReturn<Job>, IDeleteDb<Job>
{
    public id: number;

    public constructor(init?: Partial<DeleteJob>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteJob'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new Job(); }
}

export class CreateJobApplication implements IReturn<JobApplication>, ICreateDb<JobApplication>
{
    // @Validate(Validator="GreaterThan(0)")
    public jobId: number;

    // @Validate(Validator="GreaterThan(0)")
    public contactId: number;

    public appliedDate: string;
    public applicationStatus: JobApplicationStatus;
    public attachments: JobApplicationAttachment[];

    public constructor(init?: Partial<CreateJobApplication>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateJobApplication'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new JobApplication(); }
}

export class UpdateJobApplication implements IReturn<JobApplication>, IPatchDb<JobApplication>
{
    public id: number;
    public jobId?: number;
    public contactId?: number;
    public appliedDate?: string;
    public applicationStatus: JobApplicationStatus;
    public attachments?: JobApplicationAttachment[];

    public constructor(init?: Partial<UpdateJobApplication>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateJobApplication'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new JobApplication(); }
}

export class DeleteJobApplication implements IReturnVoid, IDeleteDb<JobApplication>
{
    public id: number;

    public constructor(init?: Partial<DeleteJobApplication>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteJobApplication'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class CreateJobApplicationEvent implements IReturn<JobApplicationEvent>, ICreateDb<JobApplicationEvent>
{

    public constructor(init?: Partial<CreateJobApplicationEvent>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateJobApplicationEvent'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new JobApplicationEvent(); }
}

export class UpdateJobApplicationEvent implements IReturn<JobApplicationEvent>, IPatchDb<JobApplicationEvent>
{
    public id: number;
    public status?: JobApplicationStatus;
    public description?: string;
    public eventDate?: string;

    public constructor(init?: Partial<UpdateJobApplicationEvent>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateJobApplicationEvent'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new JobApplicationEvent(); }
}

export class DeleteJobApplicationEvent implements IReturnVoid, IDeleteDb<JobApplicationEvent>
{

    public constructor(init?: Partial<DeleteJobApplicationEvent>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteJobApplicationEvent'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class CreateJobApplicationComment implements IReturn<JobApplicationComment>, ICreateDb<JobApplicationComment>
{
    // @Validate(Validator="GreaterThan(0)")
    public jobApplicationId: number;

    // @Validate(Validator="NotEmpty")
    public comment: string;

    public constructor(init?: Partial<CreateJobApplicationComment>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateJobApplicationComment'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new JobApplicationComment(); }
}

export class UpdateJobApplicationComment implements IReturn<JobApplicationComment>, IPatchDb<JobApplicationComment>
{
    public id: number;
    public jobApplicationId?: number;
    public comment?: string;

    public constructor(init?: Partial<UpdateJobApplicationComment>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateJobApplicationComment'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new JobApplicationComment(); }
}

export class DeleteJobApplicationComment implements IReturnVoid, IDeleteDb<JobApplicationComment>
{
    public id: number;

    public constructor(init?: Partial<DeleteJobApplicationComment>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteJobApplicationComment'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

/** @description Create a new Booking */
// @Route("/bookings", "POST")
// @ValidateRequest(Validator="HasRole(`Employee`)")
export class CreateBooking implements IReturn<IdResponse>, ICreateDb<Booking>
{
    /** @description Name this Booking is for */
    // @Validate(Validator="NotEmpty")
    public name: string;

    public roomType: RoomType;
    // @Validate(Validator="GreaterThan(0)")
    public roomNumber: number;

    // @Validate(Validator="GreaterThan(0)")
    public cost: number;

    // @Required()
    public bookingStartDate: string;

    public bookingEndDate?: string;
    public notes?: string;
    public couponId?: string;
    public permanentAddressId?: number;
    public postalAddressId?: number;

    public constructor(init?: Partial<CreateBooking>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateBooking'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

/** @description Update an existing Booking */
// @Route("/booking/{Id}", "PATCH")
// @ValidateRequest(Validator="HasRole(`Employee`)")
// @ValidateRequest(Validator="HasRole(`Manager`)")
export class UpdateBooking implements IReturn<IdResponse>, IPatchDb<Booking>
{
    public id: number;
    public name?: string;
    public roomType?: RoomType;
    // @Validate(Validator="GreaterThan(0)")
    public roomNumber?: number;

    // @Validate(Validator="GreaterThan(0)")
    public cost?: number;

    public bookingStartDate?: string;
    public bookingEndDate?: string;
    public notes?: string;
    public couponId?: string;
    public cancelled?: boolean;
    public permanentAddressId?: number;
    public postalAddressId?: number;

    public constructor(init?: Partial<UpdateBooking>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateBooking'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

/** @description Delete a Booking */
// @Route("/booking/{Id}", "DELETE")
export class DeleteBooking implements IReturnVoid, IDeleteDb<Booking>
{
    public id: number;

    public constructor(init?: Partial<DeleteBooking>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteBooking'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

// @Route("/coupons", "POST")
// @ValidateRequest(Validator="HasRole(`Employee`)")
export class CreateCoupon implements IReturn<IdResponse>, ICreateDb<Coupon>
{
    // @Validate(Validator="NotEmpty")
    public id: string;

    // @Validate(Validator="NotEmpty")
    public description: string;

    // @Validate(Validator="GreaterThan(0)")
    public discount: number;

    // @Validate(Validator="NotNull")
    public expiryDate: string;

    public constructor(init?: Partial<CreateCoupon>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateCoupon'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/coupons/{Id}", "PATCH")
// @ValidateRequest(Validator="HasRole(`Employee`)")
export class UpdateCoupon implements IReturn<IdResponse>, IPatchDb<Coupon>
{
    public id: string;
    // @Validate(Validator="NotEmpty")
    public description: string;

    // @Validate(Validator="NotNull")
    // @Validate(Validator="GreaterThan(0)")
    public discount: number;

    // @Validate(Validator="NotNull")
    public expiryDate: string;

    public constructor(init?: Partial<UpdateCoupon>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateCoupon'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

/** @description Delete a Coupon */
// @Route("/coupons/{Id}", "DELETE")
// @ValidateRequest(Validator="HasRole(`Manager`)")
export class DeleteCoupon implements IReturnVoid, IDeleteDb<Coupon>
{
    public id: string;

    public constructor(init?: Partial<DeleteCoupon>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteCoupon'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class CreateAddress implements IReturn<IdResponse>, ICreateDb<Address>
{
    public addressText?: string;

    public constructor(init?: Partial<CreateAddress>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAddress'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

export class UpdateAddress implements IReturn<IdResponse>, IPatchDb<Address>
{
    public id: number;
    public addressText?: string;

    public constructor(init?: Partial<UpdateAddress>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAddress'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

export class CreateFileSystemItem implements IReturn<FileSystemItem>, ICreateDb<FileSystemItem>, IFileItem
{
    public fileAccessType?: FileAccessType;
    public file: FileSystemFile;

    public constructor(init?: Partial<CreateFileSystemItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateFileSystemItem'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new FileSystemItem(); }
}

export class CreatePlayer implements IReturn<IdResponse>, ICreateDb<Player>
{
    // @Validate(Validator="NotEmpty")
    public firstName: string;

    public lastName?: string;
    public email?: string;
    public phoneNumbers?: Phone[];
    // @Validate(Validator="NotNull")
    public profileId: number;

    public savedLevelId?: string;

    public constructor(init?: Partial<CreatePlayer>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreatePlayer'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

export class UpdatePlayer implements IReturn<IdResponse>, IPatchDb<Player>
{
    public id: number;
    // @Validate(Validator="NotEmpty")
    public firstName: string;

    public lastName?: string;
    public email?: string;
    public phoneNumbers?: Phone[];
    public profileId?: number;
    public savedLevelId?: string;
    public capital: string;

    public constructor(init?: Partial<UpdatePlayer>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdatePlayer'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

export class DeletePlayer implements IReturnVoid, IDeleteDb<Player>
{
    public id: number;

    public constructor(init?: Partial<DeletePlayer>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeletePlayer'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class CreateProfile implements IReturn<IdResponse>, ICreateDb<Profile>
{
    public role: PlayerRole;
    public region: PlayerRegion;
    // @Validate(Validator="NotEmpty")
    public username: string;

    public highScore: number;
    public gamesPlayed: number;
    // @Validate(Validator="InclusiveBetween(0,100)")
    public energy: number;

    public profileUrl?: string;
    public coverUrl?: string;

    public constructor(init?: Partial<CreateProfile>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateProfile'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

export class UpdateProfile implements IReturn<IdResponse>, IPatchDb<Profile>
{
    public id: number;
    public role?: PlayerRole;
    public region?: PlayerRegion;
    public username?: string;
    public highScore?: number;
    public gamesPlayed?: number;
    // @Validate(Validator="InclusiveBetween(0,100)")
    public energy?: number;

    public profileUrl?: string;
    public coverUrl?: string;

    public constructor(init?: Partial<UpdateProfile>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateProfile'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

export class DeleteProfile implements IReturnVoid, IDeleteDb<Profile>
{
    public id: number;

    public constructor(init?: Partial<DeleteProfile>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteProfile'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class CreateGameItem implements IReturn<IdResponse>, ICreateDb<GameItem>
{
    // @Validate(Validator="NotEmpty")
    public name: string;

    // @Validate(Validator="NotEmpty")
    public description: string;

    // @Validate(Validator="NotEmpty")
    public imageUrl: string;

    public constructor(init?: Partial<CreateGameItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateGameItem'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

export class UpdateGameItem implements IReturn<IdResponse>, IPatchDb<GameItem>
{
    // @Validate(Validator="NotEmpty")
    public name: string;

    // @Validate(Validator="NotEmpty")
    public description: string;

    public imageUrl?: string;

    public constructor(init?: Partial<UpdateGameItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateGameItem'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

export class DeleteGameItem implements IReturnVoid, IDeleteDb<GameItem>
{
    // @Validate(Validator="NotEmpty")
    public name: string;

    public constructor(init?: Partial<DeleteGameItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteGameItem'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class DeletePlayerGameItem implements IReturnVoid, IDeleteDb<PlayerGameItem>
{
    public id?: number;

    public constructor(init?: Partial<DeletePlayerGameItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeletePlayerGameItem'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class CreatePlayerGameItem implements IReturn<IdResponse>, ICreateDb<PlayerGameItem>
{
    public playerId: number;
    public gameItemName: string;

    public constructor(init?: Partial<CreatePlayerGameItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreatePlayerGameItem'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

export class DeleteLevel implements IReturnVoid, IDeleteDb<Level>
{
    public id?: string;

    public constructor(init?: Partial<DeleteLevel>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteLevel'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

// @Route("/todos", "POST")
export class CreateTodo implements IReturn<Todo>, ICreateDb<Todo>
{
    // @Validate(Validator="NotEmpty")
    public text: string;

    public isFinished?: boolean;

    public constructor(init?: Partial<CreateTodo>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateTodo'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new Todo(); }
}

// @Route("/todos/{Id}", "PUT")
export class UpdateTodo implements IReturn<Todo>, IPatchDb<Todo>
{
    public id: number;
    // @Validate(Validator="NotEmpty")
    public text: string;

    public isFinished?: boolean;

    public constructor(init?: Partial<UpdateTodo>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateTodo'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new Todo(); }
}

// @Route("/todos", "DELETE")
// @Route("/todos/{Id}", "DELETE")
export class DeleteTodos implements IReturnVoid, IDeleteDb<Todo>
{
    public id?: number;
    public ids?: number[];

    public constructor(init?: Partial<DeleteTodos>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteTodos'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class DeleteTodo implements IReturnVoid, IDeleteDb<Todo>
{
    public id: number;

    public constructor(init?: Partial<DeleteTodo>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteTodo'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class CreateMqBooking extends AuditBase implements IReturn<IdResponse>, ICreateDb<Booking>
{
    /** @description Name this Booking is for */
    // @Validate(Validator="NotEmpty")
    public name: string;

    public roomType: RoomType;
    // @Validate(Validator="GreaterThan(0)")
    public roomNumber: number;

    // @Validate(Validator="GreaterThan(0)")
    public cost: number;

    public bookingStartDate: string;
    public bookingEndDate?: string;
    public notes?: string;

    public constructor(init?: Partial<CreateMqBooking>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateMqBooking'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/agentruns", "POST")
// @DataContract
export class CreateAgentRun implements IReturn<IdResponse>, IPost, ICreateDb<AgentRun>
{
    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public status?: string;

    // @DataMember(Order=5)
    public nextAction?: string;

    // @DataMember(Order=6)
    public model?: string;

    // @DataMember(Order=7)
    public stepCount: number;

    // @DataMember(Order=8)
    public sliceCount: number;

    // @DataMember(Order=9)
    public maxSteps: number;

    // @DataMember(Order=10)
    public contextTokens?: number;

    // @DataMember(Order=11)
    public contextLimit?: number;

    // @DataMember(Order=12)
    public leaseOwner?: string;

    // @DataMember(Order=13)
    public leaseExpiresAt?: string;

    // @DataMember(Order=14)
    public nextAttemptAt?: string;

    // @DataMember(Order=15)
    public error?: string;

    // @DataMember(Order=16)
    public createdAt?: string;

    // @DataMember(Order=17)
    public updatedAt?: string;

    // @DataMember(Order=18)
    public completedAt?: string;

    public constructor(init?: Partial<CreateAgentRun>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAgentRun'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/agentsteps", "POST")
// @DataContract
export class CreateAgentStep implements IReturn<IdResponse>, IPost, ICreateDb<AgentStep>
{
    // @DataMember(Order=2)
    public runId: number;

    // @DataMember(Order=3)
    public sequence: number;

    // @DataMember(Order=4)
    public type?: string;

    // @DataMember(Order=5)
    public status?: string;

    // @DataMember(Order=6)
    public input?: string;

    // @DataMember(Order=7)
    public output?: string;

    // @DataMember(Order=8)
    public idempotencyKey?: string;

    // @DataMember(Order=9)
    public attempt: number;

    // @DataMember(Order=10)
    public error?: string;

    // @DataMember(Order=11)
    public startedAt?: string;

    // @DataMember(Order=12)
    public completedAt?: string;

    // @DataMember(Order=13)
    public createdAt?: string;

    public constructor(init?: Partial<CreateAgentStep>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAgentStep'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatdocuments", "POST")
// @DataContract
export class CreateAichatDocument implements IReturn<IdResponse>, IPost, ICreateDb<AichatDocument>
{
    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public filename?: string;

    // @DataMember(Order=7)
    public url?: string;

    // @DataMember(Order=8)
    public hash?: string;

    // @DataMember(Order=9)
    public size?: number;

    // @DataMember(Order=10)
    public displayName?: string;

    // @DataMember(Order=11)
    public name?: string;

    // @DataMember(Order=12)
    public customMetadata?: string;

    // @DataMember(Order=13)
    public createTime?: string;

    // @DataMember(Order=14)
    public updateTime?: string;

    // @DataMember(Order=15)
    public sizeBytes?: number;

    // @DataMember(Order=16)
    public mimeType?: string;

    // @DataMember(Order=17)
    public state?: string;

    // @DataMember(Order=18)
    public category?: string;

    // @DataMember(Order=19)
    public tags?: string;

    // @DataMember(Order=20)
    public startedAt?: string;

    // @DataMember(Order=21)
    public uploadedAt?: string;

    // @DataMember(Order=22)
    public metadata?: string;

    // @DataMember(Order=23)
    public error?: string;

    // @DataMember(Order=24)
    public ref?: string;

    public constructor(init?: Partial<CreateAichatDocument>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAichatDocument'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatfilestores", "POST")
// @DataContract
export class CreateAichatFilestore implements IReturn<IdResponse>, IPost, ICreateDb<AichatFilestore>
{
    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public updatedAt?: string;

    // @DataMember(Order=5)
    public name?: string;

    // @DataMember(Order=6)
    public displayName?: string;

    // @DataMember(Order=7)
    public createTime?: string;

    // @DataMember(Order=8)
    public updateTime?: string;

    // @DataMember(Order=9)
    public activeDocumentsCount?: number;

    // @DataMember(Order=10)
    public pendingDocumentsCount?: number;

    // @DataMember(Order=11)
    public failedDocumentsCount?: number;

    // @DataMember(Order=12)
    public sizeBytes?: number;

    // @DataMember(Order=13)
    public metadata?: string;

    // @DataMember(Order=14)
    public error?: string;

    // @DataMember(Order=15)
    public ref?: string;

    public constructor(init?: Partial<CreateAichatFilestore>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAichatFilestore'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatmedias", "POST")
// @DataContract
export class CreateAichatMedia implements IReturn<IdResponse>, IPost, ICreateDb<AichatMedia>
{
    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public name?: string;

    // @DataMember(Order=4)
    public type?: string;

    // @DataMember(Order=5)
    public prompt?: string;

    // @DataMember(Order=6)
    public model?: string;

    // @DataMember(Order=7)
    public created?: string;

    // @DataMember(Order=8)
    public cost?: number;

    // @DataMember(Order=9)
    public seed?: number;

    // @DataMember(Order=10)
    public url?: string;

    // @DataMember(Order=11)
    public hash?: string;

    // @DataMember(Order=12)
    public aspectRatio?: string;

    // @DataMember(Order=13)
    public width?: number;

    // @DataMember(Order=14)
    public height?: number;

    // @DataMember(Order=15)
    public size?: number;

    // @DataMember(Order=16)
    public duration?: number;

    // @DataMember(Order=17)
    public reactions?: string;

    // @DataMember(Order=18)
    public caption?: string;

    // @DataMember(Order=19)
    public description?: string;

    // @DataMember(Order=20)
    public phash?: string;

    // @DataMember(Order=21)
    public color?: string;

    // @DataMember(Order=22)
    public category?: string;

    // @DataMember(Order=23)
    public tags?: string;

    // @DataMember(Order=24)
    public rating?: string;

    // @DataMember(Order=25)
    public ratings?: string;

    // @DataMember(Order=26)
    public objects?: string;

    // @DataMember(Order=27)
    public variantId?: string;

    // @DataMember(Order=28)
    public variantName?: string;

    // @DataMember(Order=29)
    public publishedAt?: string;

    // @DataMember(Order=30)
    public publishedUrl?: string;

    // @DataMember(Order=31)
    public metadata?: string;

    public constructor(init?: Partial<CreateAichatMedia>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAichatMedia'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetroleclaims", "POST")
// @DataContract
export class CreateAspNetRoleClaims implements IReturn<IdResponse>, IPost, ICreateDb<AspNetRoleClaims>
{
    // @DataMember(Order=2)
    public roleId?: string;

    // @DataMember(Order=3)
    public claimType?: string;

    // @DataMember(Order=4)
    public claimValue?: string;

    public constructor(init?: Partial<CreateAspNetRoleClaims>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAspNetRoleClaims'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetroles", "POST")
// @DataContract
export class CreateAspNetRoles implements IReturn<IdResponse>, IPost, ICreateDb<AspNetRoles>
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public normalizedName?: string;

    // @DataMember(Order=4)
    public concurrencyStamp?: string;

    public constructor(init?: Partial<CreateAspNetRoles>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAspNetRoles'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetuserclaims", "POST")
// @ValidateRequest(Validator="IsAdmin")
// @DataContract
export class CreateAspNetUserClaims implements IReturn<IdResponse>, IPost, ICreateDb<AspNetUserClaims>
{
    // @DataMember(Order=2)
    public userId?: string;

    // @DataMember(Order=3)
    public claimType?: string;

    // @DataMember(Order=4)
    public claimValue?: string;

    public constructor(init?: Partial<CreateAspNetUserClaims>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAspNetUserClaims'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetusers", "POST")
// @ValidateRequest(Validator="IsAdmin")
// @DataContract
export class CreateAspNetUsers implements IReturn<IdResponse>, IPost, ICreateDb<AspNetUsers>
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public firstName?: string;

    // @DataMember(Order=3)
    public lastName?: string;

    // @DataMember(Order=4)
    public displayName?: string;

    // @DataMember(Order=5)
    public profileUrl?: string;

    // @DataMember(Order=6)
    public refreshToken?: string;

    // @DataMember(Order=7)
    public refreshTokenExpiry?: string;

    // @DataMember(Order=8)
    public userName?: string;

    // @DataMember(Order=9)
    public normalizedUserName?: string;

    // @DataMember(Order=10)
    public email?: string;

    // @DataMember(Order=11)
    public normalizedEmail?: string;

    // @DataMember(Order=12)
    public emailConfirmed: number;

    // @DataMember(Order=13)
    public passwordHash?: string;

    // @DataMember(Order=14)
    public securityStamp?: string;

    // @DataMember(Order=15)
    public concurrencyStamp?: string;

    // @DataMember(Order=16)
    public phoneNumber?: string;

    // @DataMember(Order=17)
    public phoneNumberConfirmed: number;

    // @DataMember(Order=18)
    public twoFactorEnabled: number;

    // @DataMember(Order=19)
    public lockoutEnd?: string;

    // @DataMember(Order=20)
    public lockoutEnabled: number;

    // @DataMember(Order=21)
    public accessFailedCount: number;

    public constructor(init?: Partial<CreateAspNetUsers>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateAspNetUsers'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/categories", "POST")
// @DataContract
export class CreateCategory implements IReturn<IdResponse>, IPost, ICreateDb<Category>
{
    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public description?: string;

    // @DataMember(Order=4)
    public temperatures?: string;

    // @DataMember(Order=5)
    public defaultTemperature?: string;

    // @DataMember(Order=6)
    public sizes?: string;

    // @DataMember(Order=7)
    public defaultSize?: string;

    // @DataMember(Order=8)
    public imageUrl?: string;

    public constructor(init?: Partial<CreateCategory>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateCategory'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/categoryoptions", "POST")
// @DataContract
export class CreateCategoryOption implements IReturn<IdResponse>, IPost, ICreateDb<CategoryOption>
{
    // @DataMember(Order=2)
    public categoryId: number;

    // @DataMember(Order=3)
    public optionId: number;

    public constructor(init?: Partial<CreateCategoryOption>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateCategoryOption'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistants", "POST")
// @DataContract
export class CreateChatAssistant implements IReturn<IdResponse>, IPost, ICreateDb<ChatAssistant>
{
    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public name?: string;

    // @DataMember(Order=7)
    public publicId?: string;

    // @DataMember(Order=8)
    public enabled: number;

    // @DataMember(Order=9)
    public publishedAt?: string;

    // @DataMember(Order=10)
    public config?: string;

    public constructor(init?: Partial<CreateChatAssistant>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatAssistant'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistantconversations", "POST")
// @DataContract
export class CreateChatAssistantConversation implements IReturn<IdResponse>, IPost, ICreateDb<ChatAssistantConversation>
{
    // @DataMember(Order=2)
    public assistantId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public sessionId?: string;

    // @DataMember(Order=7)
    public origin?: string;

    // @DataMember(Order=8)
    public pageUrl?: string;

    // @DataMember(Order=9)
    public userAgent?: string;

    // @DataMember(Order=10)
    public title?: string;

    // @DataMember(Order=11)
    public status?: string;

    // @DataMember(Order=12)
    public messageCount: number;

    // @DataMember(Order=13)
    public lastMessage?: string;

    public constructor(init?: Partial<CreateChatAssistantConversation>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatAssistantConversation'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistantmessages", "POST")
// @DataContract
export class CreateChatAssistantMessage implements IReturn<IdResponse>, IPost, ICreateDb<ChatAssistantMessage>
{
    // @DataMember(Order=2)
    public conversationId: number;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public role?: string;

    // @DataMember(Order=5)
    public content?: string;

    // @DataMember(Order=6)
    public citations?: string;

    // @DataMember(Order=7)
    public error?: string;

    public constructor(init?: Partial<CreateChatAssistantMessage>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatAssistantMessage'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatdocuments", "POST")
// @DataContract
export class CreateChatDocument implements IReturn<IdResponse>, IPost, ICreateDb<ChatDocument>
{
    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public filename?: string;

    // @DataMember(Order=7)
    public url?: string;

    // @DataMember(Order=8)
    public hash?: string;

    // @DataMember(Order=9)
    public size?: number;

    // @DataMember(Order=10)
    public displayName?: string;

    // @DataMember(Order=11)
    public name?: string;

    // @DataMember(Order=12)
    public customMetadata?: string;

    // @DataMember(Order=13)
    public createTime?: string;

    // @DataMember(Order=14)
    public updateTime?: string;

    // @DataMember(Order=15)
    public sizeBytes?: number;

    // @DataMember(Order=16)
    public mimeType?: string;

    // @DataMember(Order=17)
    public state?: string;

    // @DataMember(Order=18)
    public category?: string;

    // @DataMember(Order=19)
    public tags?: string;

    // @DataMember(Order=20)
    public startedAt?: string;

    // @DataMember(Order=21)
    public uploadedAt?: string;

    // @DataMember(Order=22)
    public metadata?: string;

    // @DataMember(Order=23)
    public error?: string;

    // @DataMember(Order=24)
    public ref?: string;

    // @DataMember(Order=25)
    public sourceUrl?: string;

    // @DataMember(Order=26)
    public sourceId?: number;

    // @DataMember(Order=27)
    public sourceScopeId: number;

    // @DataMember(Order=28)
    public sourceKey?: string;

    // @DataMember(Order=29)
    public sourceEtag?: string;

    // @DataMember(Order=30)
    public contentHash?: string;

    // @DataMember(Order=31)
    public metadataHash?: string;

    // @DataMember(Order=32)
    public extractorVer?: string;

    // @DataMember(Order=33)
    public tombstonedAt?: string;

    // @DataMember(Order=34)
    public categoryPath?: string;

    // @DataMember(Order=35)
    public docType?: string;

    // @DataMember(Order=36)
    public status?: string;

    // @DataMember(Order=37)
    public locale?: string;

    // @DataMember(Order=38)
    public product?: string;

    // @DataMember(Order=39)
    public versions?: string;

    // @DataMember(Order=40)
    public sourceUpdatedAt?: number;

    public constructor(init?: Partial<CreateChatDocument>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatDocument'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatfilestores", "POST")
// @DataContract
export class CreateChatFilestore implements IReturn<IdResponse>, IPost, ICreateDb<ChatFilestore>
{
    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public updatedAt?: string;

    // @DataMember(Order=5)
    public name?: string;

    // @DataMember(Order=6)
    public displayName?: string;

    // @DataMember(Order=7)
    public createTime?: string;

    // @DataMember(Order=8)
    public updateTime?: string;

    // @DataMember(Order=9)
    public activeDocumentsCount?: number;

    // @DataMember(Order=10)
    public pendingDocumentsCount?: number;

    // @DataMember(Order=11)
    public failedDocumentsCount?: number;

    // @DataMember(Order=12)
    public sizeBytes?: number;

    // @DataMember(Order=13)
    public metadata?: string;

    // @DataMember(Order=14)
    public error?: string;

    // @DataMember(Order=15)
    public ref?: string;

    // @DataMember(Order=16)
    public visibility?: string;

    // @DataMember(Order=17)
    public facets?: string;

    public constructor(init?: Partial<CreateChatFilestore>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatFilestore'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatmedias", "POST")
// @DataContract
export class CreateChatMedia implements IReturn<IdResponse>, IPost, ICreateDb<ChatMedia>
{
    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public name?: string;

    // @DataMember(Order=4)
    public type?: string;

    // @DataMember(Order=5)
    public prompt?: string;

    // @DataMember(Order=6)
    public model?: string;

    // @DataMember(Order=7)
    public created?: string;

    // @DataMember(Order=8)
    public cost?: number;

    // @DataMember(Order=9)
    public seed?: number;

    // @DataMember(Order=10)
    public url?: string;

    // @DataMember(Order=11)
    public hash?: string;

    // @DataMember(Order=12)
    public aspectRatio?: string;

    // @DataMember(Order=13)
    public width?: number;

    // @DataMember(Order=14)
    public height?: number;

    // @DataMember(Order=15)
    public size?: number;

    // @DataMember(Order=16)
    public duration?: number;

    // @DataMember(Order=17)
    public reactions?: string;

    // @DataMember(Order=18)
    public caption?: string;

    // @DataMember(Order=19)
    public description?: string;

    // @DataMember(Order=20)
    public phash?: string;

    // @DataMember(Order=21)
    public color?: string;

    // @DataMember(Order=22)
    public category?: string;

    // @DataMember(Order=23)
    public tags?: string;

    // @DataMember(Order=24)
    public rating?: string;

    // @DataMember(Order=25)
    public ratings?: string;

    // @DataMember(Order=26)
    public objects?: string;

    // @DataMember(Order=27)
    public variantId?: string;

    // @DataMember(Order=28)
    public variantName?: string;

    // @DataMember(Order=29)
    public publishedAt?: string;

    // @DataMember(Order=30)
    public publishedUrl?: string;

    // @DataMember(Order=31)
    public metadata?: string;

    public constructor(init?: Partial<CreateChatMedia>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatMedia'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatmessages", "POST")
// @DataContract
export class CreateChatMessage implements IReturn<IdResponse>, IPost, ICreateDb<ChatMessage>
{
    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public sequence: number;

    // @DataMember(Order=4)
    public runId?: number;

    // @DataMember(Order=5)
    public stepId?: number;

    // @DataMember(Order=6)
    public role?: string;

    // @DataMember(Order=7)
    public message?: string;

    // @DataMember(Order=8)
    public timestamp?: number;

    // @DataMember(Order=9)
    public toolCallId?: string;

    // @DataMember(Order=10)
    public toolName?: string;

    // @DataMember(Order=11)
    public tokenCount?: number;

    // @DataMember(Order=12)
    public active: number;

    // @DataMember(Order=13)
    public createdAt?: string;

    public constructor(init?: Partial<CreateChatMessage>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatMessage'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatrequests", "POST")
// @DataContract
export class CreateChatRequest implements IReturn<IdResponse>, IPost, ICreateDb<ChatRequest>
{
    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public threadId?: number;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public title?: string;

    // @DataMember(Order=7)
    public model?: string;

    // @DataMember(Order=8)
    public duration?: number;

    // @DataMember(Order=9)
    public cost?: number;

    // @DataMember(Order=10)
    public inputPrice?: number;

    // @DataMember(Order=11)
    public inputTokens?: number;

    // @DataMember(Order=12)
    public inputCachedTokens?: number;

    // @DataMember(Order=13)
    public outputPrice?: number;

    // @DataMember(Order=14)
    public outputTokens?: number;

    // @DataMember(Order=15)
    public totalTokens?: number;

    // @DataMember(Order=16)
    public usage?: string;

    // @DataMember(Order=17)
    public provider?: string;

    // @DataMember(Order=18)
    public providerModel?: string;

    // @DataMember(Order=19)
    public providerRef?: string;

    // @DataMember(Order=20)
    public finishReason?: string;

    // @DataMember(Order=21)
    public startedAt?: string;

    // @DataMember(Order=22)
    public completedAt?: string;

    // @DataMember(Order=23)
    public error?: string;

    // @DataMember(Order=24)
    public stackTrace?: string;

    // @DataMember(Order=25)
    public ref?: string;

    public constructor(init?: Partial<CreateChatRequest>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatRequest'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatsources", "POST")
// @DataContract
export class CreateChatSource implements IReturn<IdResponse>, IPost, ICreateDb<ChatSource>
{
    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public name?: string;

    // @DataMember(Order=7)
    public type?: string;

    // @DataMember(Order=8)
    public enabled: number;

    // @DataMember(Order=9)
    public config?: string;

    // @DataMember(Order=10)
    public category?: string;

    // @DataMember(Order=11)
    public rules?: string;

    // @DataMember(Order=12)
    public include?: string;

    // @DataMember(Order=13)
    public exclude?: string;

    // @DataMember(Order=14)
    public extract?: string;

    // @DataMember(Order=15)
    public chunking?: string;

    // @DataMember(Order=16)
    public volatile?: string;

    // @DataMember(Order=17)
    public extractorVer?: string;

    // @DataMember(Order=18)
    public schedule?: string;

    // @DataMember(Order=19)
    public onDelete?: string;

    // @DataMember(Order=20)
    public cursor?: string;

    // @DataMember(Order=21)
    public lastRunId?: number;

    // @DataMember(Order=22)
    public lastRunAt?: string;

    // @DataMember(Order=23)
    public error?: string;

    public constructor(init?: Partial<CreateChatSource>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatSource'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatsourceruns", "POST")
// @DataContract
export class CreateChatSourceRun implements IReturn<IdResponse>, IPost, ICreateDb<ChatSourceRun>
{
    // @DataMember(Order=2)
    public sourceId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public startedAt?: string;

    // @DataMember(Order=5)
    public completedAt?: string;

    // @DataMember(Order=6)
    public status?: string;

    // @DataMember(Order=7)
    public dryRun: number;

    // @DataMember(Order=8)
    public discovered: number;

    // @DataMember(Order=9)
    public added: number;

    // @DataMember(Order=10)
    public changed: number;

    // @DataMember(Order=11)
    public metadataOnly: number;

    // @DataMember(Order=12)
    public unchanged: number;

    // @DataMember(Order=13)
    public removed: number;

    // @DataMember(Order=14)
    public skipped: number;

    // @DataMember(Order=15)
    public failed: number;

    // @DataMember(Order=16)
    public bytes: number;

    // @DataMember(Order=17)
    public plan?: string;

    // @DataMember(Order=18)
    public log?: string;

    // @DataMember(Order=19)
    public error?: string;

    public constructor(init?: Partial<CreateChatSourceRun>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatSourceRun'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatthreads", "POST")
// @DataContract
export class CreateChatThread implements IReturn<IdResponse>, IPost, ICreateDb<ChatThread>
{
    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public updatedAt?: string;

    // @DataMember(Order=5)
    public title?: string;

    // @DataMember(Order=6)
    public systemPrompt?: string;

    // @DataMember(Order=7)
    public model?: string;

    // @DataMember(Order=8)
    public modelInfo?: string;

    // @DataMember(Order=9)
    public modalities?: string;

    // @DataMember(Order=10)
    public messages?: string;

    // @DataMember(Order=11)
    public streamingMessage?: string;

    // @DataMember(Order=12)
    public args?: string;

    // @DataMember(Order=13)
    public tools?: string;

    // @DataMember(Order=14)
    public toolHistory?: string;

    // @DataMember(Order=15)
    public cost?: number;

    // @DataMember(Order=16)
    public inputTokens?: number;

    // @DataMember(Order=17)
    public outputTokens?: number;

    // @DataMember(Order=18)
    public stats?: string;

    // @DataMember(Order=19)
    public provider?: string;

    // @DataMember(Order=20)
    public providerModel?: string;

    // @DataMember(Order=21)
    public startedAt?: string;

    // @DataMember(Order=22)
    public completedAt?: string;

    // @DataMember(Order=23)
    public metadata?: string;

    // @DataMember(Order=24)
    public status?: string;

    // @DataMember(Order=25)
    public error?: string;

    // @DataMember(Order=26)
    public ref?: string;

    // @DataMember(Order=27)
    public providerResponse?: string;

    // @DataMember(Order=28)
    public contextTokens?: number;

    // @DataMember(Order=29)
    public parentId?: number;

    // @DataMember(Order=30)
    public publishedAt?: string;

    // @DataMember(Order=31)
    public publishedUrl?: string;

    public constructor(init?: Partial<CreateChatThread>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatThread'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chattoolapprovals", "POST")
// @DataContract
export class CreateChatToolApproval implements IReturn<IdResponse>, IPost, ICreateDb<ChatToolApproval>
{
    // @DataMember(Order=2)
    public batchId?: string;

    // @DataMember(Order=3)
    public threadId: number;

    // @DataMember(Order=4)
    public user?: string;

    // @DataMember(Order=5)
    public toolCallId?: string;

    // @DataMember(Order=6)
    public toolName?: string;

    // @DataMember(Order=7)
    public apiName?: string;

    // @DataMember(Order=8)
    public requestType?: string;

    // @DataMember(Order=9)
    public method?: string;

    // @DataMember(Order=10)
    public route?: string;

    // @DataMember(Order=11)
    public safety?: string;

    // @DataMember(Order=12)
    public status?: string;

    // @DataMember(Order=13)
    public sequence: number;

    // @DataMember(Order=14)
    public description?: string;

    // @DataMember(Order=15)
    public schema?: string;

    // @DataMember(Order=16)
    public proposedArgs?: string;

    // @DataMember(Order=17)
    public effectiveArgs?: string;

    // @DataMember(Order=18)
    public result?: string;

    // @DataMember(Order=19)
    public toolResult?: string;

    // @DataMember(Order=20)
    public error?: string;

    // @DataMember(Order=21)
    public reason?: string;

    // @DataMember(Order=22)
    public createdAt?: string;

    // @DataMember(Order=23)
    public updatedAt?: string;

    // @DataMember(Order=24)
    public resolvedAt?: string;

    public constructor(init?: Partial<CreateChatToolApproval>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatToolApproval'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chattoolapprovalbatches", "POST")
// @DataContract
export class CreateChatToolApprovalBatch implements IReturn<IdResponse>, IPost, ICreateDb<ChatToolApprovalBatch>
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public status?: string;

    // @DataMember(Order=5)
    public createdAt?: string;

    // @DataMember(Order=6)
    public updatedAt?: string;

    // @DataMember(Order=7)
    public completedAt?: string;

    public constructor(init?: Partial<CreateChatToolApprovalBatch>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateChatToolApprovalBatch'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/coffeeshoporderitems", "POST")
// @DataContract
export class CreateCoffeeShopOrderItem implements IReturn<IdResponse>, IPost, ICreateDb<CoffeeShopOrderItem>
{
    // @DataMember(Order=2)
    public coffeeShopOrderId: number;

    // @DataMember(Order=3)
    public productId: number;

    // @DataMember(Order=4)
    public productName?: string;

    // @DataMember(Order=5)
    public quantity: number;

    // @DataMember(Order=6)
    public size?: string;

    // @DataMember(Order=7)
    public temperature?: string;

    // @DataMember(Order=8)
    public optionsJson?: string;

    // @DataMember(Order=9)
    public unitPrice: number;

    // @DataMember(Order=10)
    public lineTotal: number;

    public constructor(init?: Partial<CreateCoffeeShopOrderItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateCoffeeShopOrderItem'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/contextsnapshots", "POST")
// @DataContract
export class CreateContextSnapshot implements IReturn<IdResponse>, IPost, ICreateDb<ContextSnapshot>
{
    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public runId?: number;

    // @DataMember(Order=4)
    public version: number;

    // @DataMember(Order=5)
    public fromSequence: number;

    // @DataMember(Order=6)
    public toSequence: number;

    // @DataMember(Order=7)
    public summary?: string;

    // @DataMember(Order=8)
    public tokenCount?: number;

    // @DataMember(Order=9)
    public model?: string;

    // @DataMember(Order=10)
    public createdAt?: string;

    public constructor(init?: Partial<CreateContextSnapshot>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateContextSnapshot'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/efmigrationshistories", "POST")
// @DataContract
export class CreateEFMigrationsHistory implements IReturn<IdResponse>, IPost, ICreateDb<EFMigrationsHistory>
{
    // @DataMember(Order=1)
    public migrationId?: string;

    // @DataMember(Order=2)
    public productVersion?: string;

    public constructor(init?: Partial<CreateEFMigrationsHistory>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateEFMigrationsHistory'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/efmigrationslocks", "POST")
// @DataContract
export class CreateEFMigrationsLock implements IReturn<IdResponse>, IPost, ICreateDb<EFMigrationsLock>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public timestamp?: string;

    public constructor(init?: Partial<CreateEFMigrationsLock>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateEFMigrationsLock'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/filesystemfiles", "POST")
// @DataContract
export class CreateFileSystemFile implements IReturn<IdResponse>, IPost, ICreateDb<FileSystemFile>
{
    // @DataMember(Order=2)
    public fileName?: string;

    // @DataMember(Order=3)
    public filePath?: string;

    // @DataMember(Order=4)
    public contentType?: string;

    // @DataMember(Order=5)
    public contentLength: number;

    // @DataMember(Order=6)
    public fileSystemItemId: number;

    public constructor(init?: Partial<CreateFileSystemFile>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateFileSystemFile'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/migrations", "POST")
// @DataContract
export class CreateMigration implements IReturn<IdResponse>, IPost, ICreateDb<Migration>
{
    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public description?: string;

    // @DataMember(Order=4)
    public createdDate?: string;

    // @DataMember(Order=5)
    public completedDate?: string;

    // @DataMember(Order=6)
    public connectionString?: string;

    // @DataMember(Order=7)
    public namedConnection?: string;

    // @DataMember(Order=8)
    public log?: string;

    // @DataMember(Order=9)
    public errorCode?: string;

    // @DataMember(Order=10)
    public errorMessage?: string;

    // @DataMember(Order=11)
    public errorStackTrace?: string;

    // @DataMember(Order=12)
    public meta?: string;

    public constructor(init?: Partial<CreateMigration>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateMigration'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/options", "POST")
// @DataContract
export class CreateOption implements IReturn<IdResponse>, IPost, ICreateDb<Option>
{
    // @DataMember(Order=2)
    public type?: string;

    // @DataMember(Order=3)
    public names?: string;

    // @DataMember(Order=4)
    public allowQuantity?: number;

    // @DataMember(Order=5)
    public quantityLabel?: string;

    public constructor(init?: Partial<CreateOption>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateOption'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/optionquantities", "POST")
// @DataContract
export class CreateOptionQuantity implements IReturn<IdResponse>, IPost, ICreateDb<OptionQuantity>
{
    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public value: number;

    public constructor(init?: Partial<CreateOptionQuantity>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateOptionQuantity'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/products", "POST")
// @DataContract
export class CreateProduct implements IReturn<IdResponse>, IPost, ICreateDb<Product>
{
    // @DataMember(Order=2)
    public categoryId: number;

    // @DataMember(Order=3)
    public name?: string;

    // @DataMember(Order=4)
    public cost: number;

    // @DataMember(Order=5)
    public imageUrl?: string;

    public constructor(init?: Partial<CreateProduct>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateProduct'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/validationrules", "POST")
// @DataContract
export class CreateValidationRule implements IReturn<IdResponse>, IPost, ICreateDb<ValidationRule>
{
    // @DataMember(Order=2)
    public type?: string;

    // @DataMember(Order=3)
    public field?: string;

    // @DataMember(Order=4)
    public createdBy?: string;

    // @DataMember(Order=5)
    public createdDate?: string;

    // @DataMember(Order=6)
    public modifiedBy?: string;

    // @DataMember(Order=7)
    public modifiedDate?: string;

    // @DataMember(Order=8)
    public suspendedBy?: string;

    // @DataMember(Order=9)
    public suspendedDate?: string;

    // @DataMember(Order=10)
    public notes?: string;

    // @DataMember(Order=11)
    public validator?: string;

    // @DataMember(Order=12)
    public condition?: string;

    // @DataMember(Order=13)
    public errorCode?: string;

    // @DataMember(Order=14)
    public message?: string;

    public constructor(init?: Partial<CreateValidationRule>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'CreateValidationRule'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/addresses/{Id}", "DELETE")
// @DataContract
export class DeleteAddress implements IReturn<IdResponse>, IDelete, IDeleteDb<Address>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteAddress>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAddress'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/agentruns/{Id}", "DELETE")
// @DataContract
export class DeleteAgentRun implements IReturn<IdResponse>, IDelete, IDeleteDb<AgentRun>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteAgentRun>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAgentRun'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/agentsteps/{Id}", "DELETE")
// @DataContract
export class DeleteAgentStep implements IReturn<IdResponse>, IDelete, IDeleteDb<AgentStep>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteAgentStep>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAgentStep'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatdocuments/{Id}", "DELETE")
// @DataContract
export class DeleteAichatDocument implements IReturn<IdResponse>, IDelete, IDeleteDb<AichatDocument>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteAichatDocument>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAichatDocument'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatfilestores/{Id}", "DELETE")
// @DataContract
export class DeleteAichatFilestore implements IReturn<IdResponse>, IDelete, IDeleteDb<AichatFilestore>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteAichatFilestore>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAichatFilestore'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatmedias/{Id}", "DELETE")
// @DataContract
export class DeleteAichatMedia implements IReturn<IdResponse>, IDelete, IDeleteDb<AichatMedia>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteAichatMedia>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAichatMedia'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetroleclaims/{Id}", "DELETE")
// @DataContract
export class DeleteAspNetRoleClaims implements IReturn<IdResponse>, IDelete, IDeleteDb<AspNetRoleClaims>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteAspNetRoleClaims>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAspNetRoleClaims'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetroles/{Id}", "DELETE")
// @DataContract
export class DeleteAspNetRoles implements IReturn<IdResponse>, IDelete, IDeleteDb<AspNetRoles>
{
    // @DataMember(Order=1)
    public id?: string;

    public constructor(init?: Partial<DeleteAspNetRoles>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAspNetRoles'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetuserclaims/{Id}", "DELETE")
// @ValidateRequest(Validator="IsAdmin")
// @DataContract
export class DeleteAspNetUserClaims implements IReturn<IdResponse>, IDelete, IDeleteDb<AspNetUserClaims>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteAspNetUserClaims>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAspNetUserClaims'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetusers/{Id}", "DELETE")
// @ValidateRequest(Validator="IsAdmin")
// @DataContract
export class DeleteAspNetUsers implements IReturn<IdResponse>, IDelete, IDeleteDb<AspNetUsers>
{
    // @DataMember(Order=1)
    public id?: string;

    public constructor(init?: Partial<DeleteAspNetUsers>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteAspNetUsers'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/categories/{Id}", "DELETE")
// @DataContract
export class DeleteCategory implements IReturn<IdResponse>, IDelete, IDeleteDb<Category>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteCategory>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteCategory'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/categoryoptions/{Id}", "DELETE")
// @DataContract
export class DeleteCategoryOption implements IReturn<IdResponse>, IDelete, IDeleteDb<CategoryOption>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteCategoryOption>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteCategoryOption'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistants/{Id}", "DELETE")
// @DataContract
export class DeleteChatAssistant implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatAssistant>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatAssistant>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatAssistant'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistantconversations/{Id}", "DELETE")
// @DataContract
export class DeleteChatAssistantConversation implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatAssistantConversation>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatAssistantConversation>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatAssistantConversation'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistantmessages/{Id}", "DELETE")
// @DataContract
export class DeleteChatAssistantMessage implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatAssistantMessage>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatAssistantMessage>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatAssistantMessage'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatdocuments/{Id}", "DELETE")
// @DataContract
export class DeleteChatDocument implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatDocument>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatDocument>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatDocument'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatfilestores/{Id}", "DELETE")
// @DataContract
export class DeleteChatFilestore implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatFilestore>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatFilestore>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatFilestore'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatmedias/{Id}", "DELETE")
// @DataContract
export class DeleteChatMedia implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatMedia>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatMedia>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatMedia'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatmessages/{Id}", "DELETE")
// @DataContract
export class DeleteChatMessage implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatMessage>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatMessage>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatMessage'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatrequests/{Id}", "DELETE")
// @DataContract
export class DeleteChatRequest implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatRequest>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatRequest>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatRequest'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatsources/{Id}", "DELETE")
// @DataContract
export class DeleteChatSource implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatSource>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatSource>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatSource'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatsourceruns/{Id}", "DELETE")
// @DataContract
export class DeleteChatSourceRun implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatSourceRun>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatSourceRun>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatSourceRun'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatthreads/{Id}", "DELETE")
// @DataContract
export class DeleteChatThread implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatThread>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatThread>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatThread'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chattoolapprovals/{Id}", "DELETE")
// @DataContract
export class DeleteChatToolApproval implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatToolApproval>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteChatToolApproval>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatToolApproval'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chattoolapprovalbatches/{Id}", "DELETE")
// @DataContract
export class DeleteChatToolApprovalBatch implements IReturn<IdResponse>, IDelete, IDeleteDb<ChatToolApprovalBatch>
{
    // @DataMember(Order=1)
    public id?: string;

    public constructor(init?: Partial<DeleteChatToolApprovalBatch>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteChatToolApprovalBatch'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/coffeeshoporders/{Id}", "DELETE")
// @DataContract
export class DeleteCoffeeShopOrder implements IReturn<IdResponse>, IDelete, IDeleteDb<CoffeeShopOrder>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteCoffeeShopOrder>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteCoffeeShopOrder'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/coffeeshoporderitems/{Id}", "DELETE")
// @DataContract
export class DeleteCoffeeShopOrderItem implements IReturn<IdResponse>, IDelete, IDeleteDb<CoffeeShopOrderItem>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteCoffeeShopOrderItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteCoffeeShopOrderItem'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/contextsnapshots/{Id}", "DELETE")
// @DataContract
export class DeleteContextSnapshot implements IReturn<IdResponse>, IDelete, IDeleteDb<ContextSnapshot>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteContextSnapshot>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteContextSnapshot'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/efmigrationshistories/{MigrationId}", "DELETE")
// @DataContract
export class DeleteEFMigrationsHistory implements IReturn<IdResponse>, IDelete, IDeleteDb<EFMigrationsHistory>
{
    // @DataMember(Order=1)
    public migrationId?: string;

    public constructor(init?: Partial<DeleteEFMigrationsHistory>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteEFMigrationsHistory'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/efmigrationslocks/{Id}", "DELETE")
// @DataContract
export class DeleteEFMigrationsLock implements IReturn<IdResponse>, IDelete, IDeleteDb<EFMigrationsLock>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteEFMigrationsLock>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteEFMigrationsLock'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/filesystemfiles/{Id}", "DELETE")
// @DataContract
export class DeleteFileSystemFile implements IReturn<IdResponse>, IDelete, IDeleteDb<FileSystemFile>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteFileSystemFile>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteFileSystemFile'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/filesystemitems/{Id}", "DELETE")
// @DataContract
export class DeleteFileSystemItem implements IReturn<IdResponse>, IDelete, IDeleteDb<FileSystemItem>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteFileSystemItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteFileSystemItem'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/migrations/{Id}", "DELETE")
// @DataContract
export class DeleteMigration implements IReturn<IdResponse>, IDelete, IDeleteDb<Migration>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteMigration>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteMigration'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/options/{Id}", "DELETE")
// @DataContract
export class DeleteOption implements IReturn<IdResponse>, IDelete, IDeleteDb<Option>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteOption>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteOption'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/optionquantities/{Id}", "DELETE")
// @DataContract
export class DeleteOptionQuantity implements IReturn<IdResponse>, IDelete, IDeleteDb<OptionQuantity>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteOptionQuantity>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteOptionQuantity'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/products/{Id}", "DELETE")
// @DataContract
export class DeleteProduct implements IReturn<IdResponse>, IDelete, IDeleteDb<Product>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteProduct>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteProduct'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/validationrules/{Id}", "DELETE")
// @DataContract
export class DeleteValidationRule implements IReturn<IdResponse>, IDelete, IDeleteDb<ValidationRule>
{
    // @DataMember(Order=1)
    public id: number;

    public constructor(init?: Partial<DeleteValidationRule>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'DeleteValidationRule'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/agentruns/{Id}", "PATCH")
// @DataContract
export class PatchAgentRun implements IReturn<IdResponse>, IPatch, IPatchDb<AgentRun>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public status?: string;

    // @DataMember(Order=5)
    public nextAction?: string;

    // @DataMember(Order=6)
    public model?: string;

    // @DataMember(Order=7)
    public stepCount: number;

    // @DataMember(Order=8)
    public sliceCount: number;

    // @DataMember(Order=9)
    public maxSteps: number;

    // @DataMember(Order=10)
    public contextTokens?: number;

    // @DataMember(Order=11)
    public contextLimit?: number;

    // @DataMember(Order=12)
    public leaseOwner?: string;

    // @DataMember(Order=13)
    public leaseExpiresAt?: string;

    // @DataMember(Order=14)
    public nextAttemptAt?: string;

    // @DataMember(Order=15)
    public error?: string;

    // @DataMember(Order=16)
    public createdAt?: string;

    // @DataMember(Order=17)
    public updatedAt?: string;

    // @DataMember(Order=18)
    public completedAt?: string;

    public constructor(init?: Partial<PatchAgentRun>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchAgentRun'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/agentsteps/{Id}", "PATCH")
// @DataContract
export class PatchAgentStep implements IReturn<IdResponse>, IPatch, IPatchDb<AgentStep>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public runId: number;

    // @DataMember(Order=3)
    public sequence: number;

    // @DataMember(Order=4)
    public type?: string;

    // @DataMember(Order=5)
    public status?: string;

    // @DataMember(Order=6)
    public input?: string;

    // @DataMember(Order=7)
    public output?: string;

    // @DataMember(Order=8)
    public idempotencyKey?: string;

    // @DataMember(Order=9)
    public attempt: number;

    // @DataMember(Order=10)
    public error?: string;

    // @DataMember(Order=11)
    public startedAt?: string;

    // @DataMember(Order=12)
    public completedAt?: string;

    // @DataMember(Order=13)
    public createdAt?: string;

    public constructor(init?: Partial<PatchAgentStep>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchAgentStep'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatdocuments/{Id}", "PATCH")
// @DataContract
export class PatchAichatDocument implements IReturn<IdResponse>, IPatch, IPatchDb<AichatDocument>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public filename?: string;

    // @DataMember(Order=7)
    public url?: string;

    // @DataMember(Order=8)
    public hash?: string;

    // @DataMember(Order=9)
    public size?: number;

    // @DataMember(Order=10)
    public displayName?: string;

    // @DataMember(Order=11)
    public name?: string;

    // @DataMember(Order=12)
    public customMetadata?: string;

    // @DataMember(Order=13)
    public createTime?: string;

    // @DataMember(Order=14)
    public updateTime?: string;

    // @DataMember(Order=15)
    public sizeBytes?: number;

    // @DataMember(Order=16)
    public mimeType?: string;

    // @DataMember(Order=17)
    public state?: string;

    // @DataMember(Order=18)
    public category?: string;

    // @DataMember(Order=19)
    public tags?: string;

    // @DataMember(Order=20)
    public startedAt?: string;

    // @DataMember(Order=21)
    public uploadedAt?: string;

    // @DataMember(Order=22)
    public metadata?: string;

    // @DataMember(Order=23)
    public error?: string;

    // @DataMember(Order=24)
    public ref?: string;

    public constructor(init?: Partial<PatchAichatDocument>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchAichatDocument'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatfilestores/{Id}", "PATCH")
// @DataContract
export class PatchAichatFilestore implements IReturn<IdResponse>, IPatch, IPatchDb<AichatFilestore>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public updatedAt?: string;

    // @DataMember(Order=5)
    public name?: string;

    // @DataMember(Order=6)
    public displayName?: string;

    // @DataMember(Order=7)
    public createTime?: string;

    // @DataMember(Order=8)
    public updateTime?: string;

    // @DataMember(Order=9)
    public activeDocumentsCount?: number;

    // @DataMember(Order=10)
    public pendingDocumentsCount?: number;

    // @DataMember(Order=11)
    public failedDocumentsCount?: number;

    // @DataMember(Order=12)
    public sizeBytes?: number;

    // @DataMember(Order=13)
    public metadata?: string;

    // @DataMember(Order=14)
    public error?: string;

    // @DataMember(Order=15)
    public ref?: string;

    public constructor(init?: Partial<PatchAichatFilestore>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchAichatFilestore'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatmedias/{Id}", "PATCH")
// @DataContract
export class PatchAichatMedia implements IReturn<IdResponse>, IPatch, IPatchDb<AichatMedia>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public name?: string;

    // @DataMember(Order=4)
    public type?: string;

    // @DataMember(Order=5)
    public prompt?: string;

    // @DataMember(Order=6)
    public model?: string;

    // @DataMember(Order=7)
    public created?: string;

    // @DataMember(Order=8)
    public cost?: number;

    // @DataMember(Order=9)
    public seed?: number;

    // @DataMember(Order=10)
    public url?: string;

    // @DataMember(Order=11)
    public hash?: string;

    // @DataMember(Order=12)
    public aspectRatio?: string;

    // @DataMember(Order=13)
    public width?: number;

    // @DataMember(Order=14)
    public height?: number;

    // @DataMember(Order=15)
    public size?: number;

    // @DataMember(Order=16)
    public duration?: number;

    // @DataMember(Order=17)
    public reactions?: string;

    // @DataMember(Order=18)
    public caption?: string;

    // @DataMember(Order=19)
    public description?: string;

    // @DataMember(Order=20)
    public phash?: string;

    // @DataMember(Order=21)
    public color?: string;

    // @DataMember(Order=22)
    public category?: string;

    // @DataMember(Order=23)
    public tags?: string;

    // @DataMember(Order=24)
    public rating?: string;

    // @DataMember(Order=25)
    public ratings?: string;

    // @DataMember(Order=26)
    public objects?: string;

    // @DataMember(Order=27)
    public variantId?: string;

    // @DataMember(Order=28)
    public variantName?: string;

    // @DataMember(Order=29)
    public publishedAt?: string;

    // @DataMember(Order=30)
    public publishedUrl?: string;

    // @DataMember(Order=31)
    public metadata?: string;

    public constructor(init?: Partial<PatchAichatMedia>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchAichatMedia'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetroleclaims/{Id}", "PATCH")
// @DataContract
export class PatchAspNetRoleClaims implements IReturn<IdResponse>, IPatch, IPatchDb<AspNetRoleClaims>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public roleId?: string;

    // @DataMember(Order=3)
    public claimType?: string;

    // @DataMember(Order=4)
    public claimValue?: string;

    public constructor(init?: Partial<PatchAspNetRoleClaims>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchAspNetRoleClaims'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetroles/{Id}", "PATCH")
// @DataContract
export class PatchAspNetRoles implements IReturn<IdResponse>, IPatch, IPatchDb<AspNetRoles>
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public normalizedName?: string;

    // @DataMember(Order=4)
    public concurrencyStamp?: string;

    public constructor(init?: Partial<PatchAspNetRoles>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchAspNetRoles'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetuserclaims/{Id}", "PATCH")
// @ValidateRequest(Validator="IsAdmin")
// @DataContract
export class PatchAspNetUserClaims implements IReturn<IdResponse>, IPatch, IPatchDb<AspNetUserClaims>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public userId?: string;

    // @DataMember(Order=3)
    public claimType?: string;

    // @DataMember(Order=4)
    public claimValue?: string;

    public constructor(init?: Partial<PatchAspNetUserClaims>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchAspNetUserClaims'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetusers/{Id}", "PATCH")
// @ValidateRequest(Validator="IsAdmin")
// @DataContract
export class PatchAspNetUsers implements IReturn<IdResponse>, IPatch, IPatchDb<AspNetUsers>
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public firstName?: string;

    // @DataMember(Order=3)
    public lastName?: string;

    // @DataMember(Order=4)
    public displayName?: string;

    // @DataMember(Order=5)
    public profileUrl?: string;

    // @DataMember(Order=6)
    public refreshToken?: string;

    // @DataMember(Order=7)
    public refreshTokenExpiry?: string;

    // @DataMember(Order=8)
    public userName?: string;

    // @DataMember(Order=9)
    public normalizedUserName?: string;

    // @DataMember(Order=10)
    public email?: string;

    // @DataMember(Order=11)
    public normalizedEmail?: string;

    // @DataMember(Order=12)
    public emailConfirmed: number;

    // @DataMember(Order=13)
    public passwordHash?: string;

    // @DataMember(Order=14)
    public securityStamp?: string;

    // @DataMember(Order=15)
    public concurrencyStamp?: string;

    // @DataMember(Order=16)
    public phoneNumber?: string;

    // @DataMember(Order=17)
    public phoneNumberConfirmed: number;

    // @DataMember(Order=18)
    public twoFactorEnabled: number;

    // @DataMember(Order=19)
    public lockoutEnd?: string;

    // @DataMember(Order=20)
    public lockoutEnabled: number;

    // @DataMember(Order=21)
    public accessFailedCount: number;

    public constructor(init?: Partial<PatchAspNetUsers>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchAspNetUsers'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/categories/{Id}", "PATCH")
// @DataContract
export class PatchCategory implements IReturn<IdResponse>, IPatch, IPatchDb<Category>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public description?: string;

    // @DataMember(Order=4)
    public temperatures?: string;

    // @DataMember(Order=5)
    public defaultTemperature?: string;

    // @DataMember(Order=6)
    public sizes?: string;

    // @DataMember(Order=7)
    public defaultSize?: string;

    // @DataMember(Order=8)
    public imageUrl?: string;

    public constructor(init?: Partial<PatchCategory>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchCategory'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/categoryoptions/{Id}", "PATCH")
// @DataContract
export class PatchCategoryOption implements IReturn<IdResponse>, IPatch, IPatchDb<CategoryOption>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public categoryId: number;

    // @DataMember(Order=3)
    public optionId: number;

    public constructor(init?: Partial<PatchCategoryOption>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchCategoryOption'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistants/{Id}", "PATCH")
// @DataContract
export class PatchChatAssistant implements IReturn<IdResponse>, IPatch, IPatchDb<ChatAssistant>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public name?: string;

    // @DataMember(Order=7)
    public publicId?: string;

    // @DataMember(Order=8)
    public enabled: number;

    // @DataMember(Order=9)
    public publishedAt?: string;

    // @DataMember(Order=10)
    public config?: string;

    public constructor(init?: Partial<PatchChatAssistant>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatAssistant'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistantconversations/{Id}", "PATCH")
// @DataContract
export class PatchChatAssistantConversation implements IReturn<IdResponse>, IPatch, IPatchDb<ChatAssistantConversation>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public assistantId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public sessionId?: string;

    // @DataMember(Order=7)
    public origin?: string;

    // @DataMember(Order=8)
    public pageUrl?: string;

    // @DataMember(Order=9)
    public userAgent?: string;

    // @DataMember(Order=10)
    public title?: string;

    // @DataMember(Order=11)
    public status?: string;

    // @DataMember(Order=12)
    public messageCount: number;

    // @DataMember(Order=13)
    public lastMessage?: string;

    public constructor(init?: Partial<PatchChatAssistantConversation>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatAssistantConversation'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistantmessages/{Id}", "PATCH")
// @DataContract
export class PatchChatAssistantMessage implements IReturn<IdResponse>, IPatch, IPatchDb<ChatAssistantMessage>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public conversationId: number;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public role?: string;

    // @DataMember(Order=5)
    public content?: string;

    // @DataMember(Order=6)
    public citations?: string;

    // @DataMember(Order=7)
    public error?: string;

    public constructor(init?: Partial<PatchChatAssistantMessage>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatAssistantMessage'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatdocuments/{Id}", "PATCH")
// @DataContract
export class PatchChatDocument implements IReturn<IdResponse>, IPatch, IPatchDb<ChatDocument>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public filename?: string;

    // @DataMember(Order=7)
    public url?: string;

    // @DataMember(Order=8)
    public hash?: string;

    // @DataMember(Order=9)
    public size?: number;

    // @DataMember(Order=10)
    public displayName?: string;

    // @DataMember(Order=11)
    public name?: string;

    // @DataMember(Order=12)
    public customMetadata?: string;

    // @DataMember(Order=13)
    public createTime?: string;

    // @DataMember(Order=14)
    public updateTime?: string;

    // @DataMember(Order=15)
    public sizeBytes?: number;

    // @DataMember(Order=16)
    public mimeType?: string;

    // @DataMember(Order=17)
    public state?: string;

    // @DataMember(Order=18)
    public category?: string;

    // @DataMember(Order=19)
    public tags?: string;

    // @DataMember(Order=20)
    public startedAt?: string;

    // @DataMember(Order=21)
    public uploadedAt?: string;

    // @DataMember(Order=22)
    public metadata?: string;

    // @DataMember(Order=23)
    public error?: string;

    // @DataMember(Order=24)
    public ref?: string;

    // @DataMember(Order=25)
    public sourceUrl?: string;

    // @DataMember(Order=26)
    public sourceId?: number;

    // @DataMember(Order=27)
    public sourceScopeId: number;

    // @DataMember(Order=28)
    public sourceKey?: string;

    // @DataMember(Order=29)
    public sourceEtag?: string;

    // @DataMember(Order=30)
    public contentHash?: string;

    // @DataMember(Order=31)
    public metadataHash?: string;

    // @DataMember(Order=32)
    public extractorVer?: string;

    // @DataMember(Order=33)
    public tombstonedAt?: string;

    // @DataMember(Order=34)
    public categoryPath?: string;

    // @DataMember(Order=35)
    public docType?: string;

    // @DataMember(Order=36)
    public status?: string;

    // @DataMember(Order=37)
    public locale?: string;

    // @DataMember(Order=38)
    public product?: string;

    // @DataMember(Order=39)
    public versions?: string;

    // @DataMember(Order=40)
    public sourceUpdatedAt?: number;

    public constructor(init?: Partial<PatchChatDocument>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatDocument'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatfilestores/{Id}", "PATCH")
// @DataContract
export class PatchChatFilestore implements IReturn<IdResponse>, IPatch, IPatchDb<ChatFilestore>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public updatedAt?: string;

    // @DataMember(Order=5)
    public name?: string;

    // @DataMember(Order=6)
    public displayName?: string;

    // @DataMember(Order=7)
    public createTime?: string;

    // @DataMember(Order=8)
    public updateTime?: string;

    // @DataMember(Order=9)
    public activeDocumentsCount?: number;

    // @DataMember(Order=10)
    public pendingDocumentsCount?: number;

    // @DataMember(Order=11)
    public failedDocumentsCount?: number;

    // @DataMember(Order=12)
    public sizeBytes?: number;

    // @DataMember(Order=13)
    public metadata?: string;

    // @DataMember(Order=14)
    public error?: string;

    // @DataMember(Order=15)
    public ref?: string;

    // @DataMember(Order=16)
    public visibility?: string;

    // @DataMember(Order=17)
    public facets?: string;

    public constructor(init?: Partial<PatchChatFilestore>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatFilestore'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatmedias/{Id}", "PATCH")
// @DataContract
export class PatchChatMedia implements IReturn<IdResponse>, IPatch, IPatchDb<ChatMedia>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public name?: string;

    // @DataMember(Order=4)
    public type?: string;

    // @DataMember(Order=5)
    public prompt?: string;

    // @DataMember(Order=6)
    public model?: string;

    // @DataMember(Order=7)
    public created?: string;

    // @DataMember(Order=8)
    public cost?: number;

    // @DataMember(Order=9)
    public seed?: number;

    // @DataMember(Order=10)
    public url?: string;

    // @DataMember(Order=11)
    public hash?: string;

    // @DataMember(Order=12)
    public aspectRatio?: string;

    // @DataMember(Order=13)
    public width?: number;

    // @DataMember(Order=14)
    public height?: number;

    // @DataMember(Order=15)
    public size?: number;

    // @DataMember(Order=16)
    public duration?: number;

    // @DataMember(Order=17)
    public reactions?: string;

    // @DataMember(Order=18)
    public caption?: string;

    // @DataMember(Order=19)
    public description?: string;

    // @DataMember(Order=20)
    public phash?: string;

    // @DataMember(Order=21)
    public color?: string;

    // @DataMember(Order=22)
    public category?: string;

    // @DataMember(Order=23)
    public tags?: string;

    // @DataMember(Order=24)
    public rating?: string;

    // @DataMember(Order=25)
    public ratings?: string;

    // @DataMember(Order=26)
    public objects?: string;

    // @DataMember(Order=27)
    public variantId?: string;

    // @DataMember(Order=28)
    public variantName?: string;

    // @DataMember(Order=29)
    public publishedAt?: string;

    // @DataMember(Order=30)
    public publishedUrl?: string;

    // @DataMember(Order=31)
    public metadata?: string;

    public constructor(init?: Partial<PatchChatMedia>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatMedia'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatmessages/{Id}", "PATCH")
// @DataContract
export class PatchChatMessage implements IReturn<IdResponse>, IPatch, IPatchDb<ChatMessage>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public sequence: number;

    // @DataMember(Order=4)
    public runId?: number;

    // @DataMember(Order=5)
    public stepId?: number;

    // @DataMember(Order=6)
    public role?: string;

    // @DataMember(Order=7)
    public message?: string;

    // @DataMember(Order=8)
    public timestamp?: number;

    // @DataMember(Order=9)
    public toolCallId?: string;

    // @DataMember(Order=10)
    public toolName?: string;

    // @DataMember(Order=11)
    public tokenCount?: number;

    // @DataMember(Order=12)
    public active: number;

    // @DataMember(Order=13)
    public createdAt?: string;

    public constructor(init?: Partial<PatchChatMessage>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatMessage'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatrequests/{Id}", "PATCH")
// @DataContract
export class PatchChatRequest implements IReturn<IdResponse>, IPatch, IPatchDb<ChatRequest>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public threadId?: number;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public title?: string;

    // @DataMember(Order=7)
    public model?: string;

    // @DataMember(Order=8)
    public duration?: number;

    // @DataMember(Order=9)
    public cost?: number;

    // @DataMember(Order=10)
    public inputPrice?: number;

    // @DataMember(Order=11)
    public inputTokens?: number;

    // @DataMember(Order=12)
    public inputCachedTokens?: number;

    // @DataMember(Order=13)
    public outputPrice?: number;

    // @DataMember(Order=14)
    public outputTokens?: number;

    // @DataMember(Order=15)
    public totalTokens?: number;

    // @DataMember(Order=16)
    public usage?: string;

    // @DataMember(Order=17)
    public provider?: string;

    // @DataMember(Order=18)
    public providerModel?: string;

    // @DataMember(Order=19)
    public providerRef?: string;

    // @DataMember(Order=20)
    public finishReason?: string;

    // @DataMember(Order=21)
    public startedAt?: string;

    // @DataMember(Order=22)
    public completedAt?: string;

    // @DataMember(Order=23)
    public error?: string;

    // @DataMember(Order=24)
    public stackTrace?: string;

    // @DataMember(Order=25)
    public ref?: string;

    public constructor(init?: Partial<PatchChatRequest>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatRequest'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatsources/{Id}", "PATCH")
// @DataContract
export class PatchChatSource implements IReturn<IdResponse>, IPatch, IPatchDb<ChatSource>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public name?: string;

    // @DataMember(Order=7)
    public type?: string;

    // @DataMember(Order=8)
    public enabled: number;

    // @DataMember(Order=9)
    public config?: string;

    // @DataMember(Order=10)
    public category?: string;

    // @DataMember(Order=11)
    public rules?: string;

    // @DataMember(Order=12)
    public include?: string;

    // @DataMember(Order=13)
    public exclude?: string;

    // @DataMember(Order=14)
    public extract?: string;

    // @DataMember(Order=15)
    public chunking?: string;

    // @DataMember(Order=16)
    public volatile?: string;

    // @DataMember(Order=17)
    public extractorVer?: string;

    // @DataMember(Order=18)
    public schedule?: string;

    // @DataMember(Order=19)
    public onDelete?: string;

    // @DataMember(Order=20)
    public cursor?: string;

    // @DataMember(Order=21)
    public lastRunId?: number;

    // @DataMember(Order=22)
    public lastRunAt?: string;

    // @DataMember(Order=23)
    public error?: string;

    public constructor(init?: Partial<PatchChatSource>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatSource'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatsourceruns/{Id}", "PATCH")
// @DataContract
export class PatchChatSourceRun implements IReturn<IdResponse>, IPatch, IPatchDb<ChatSourceRun>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public sourceId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public startedAt?: string;

    // @DataMember(Order=5)
    public completedAt?: string;

    // @DataMember(Order=6)
    public status?: string;

    // @DataMember(Order=7)
    public dryRun: number;

    // @DataMember(Order=8)
    public discovered: number;

    // @DataMember(Order=9)
    public added: number;

    // @DataMember(Order=10)
    public changed: number;

    // @DataMember(Order=11)
    public metadataOnly: number;

    // @DataMember(Order=12)
    public unchanged: number;

    // @DataMember(Order=13)
    public removed: number;

    // @DataMember(Order=14)
    public skipped: number;

    // @DataMember(Order=15)
    public failed: number;

    // @DataMember(Order=16)
    public bytes: number;

    // @DataMember(Order=17)
    public plan?: string;

    // @DataMember(Order=18)
    public log?: string;

    // @DataMember(Order=19)
    public error?: string;

    public constructor(init?: Partial<PatchChatSourceRun>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatSourceRun'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatthreads/{Id}", "PATCH")
// @DataContract
export class PatchChatThread implements IReturn<IdResponse>, IPatch, IPatchDb<ChatThread>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public updatedAt?: string;

    // @DataMember(Order=5)
    public title?: string;

    // @DataMember(Order=6)
    public systemPrompt?: string;

    // @DataMember(Order=7)
    public model?: string;

    // @DataMember(Order=8)
    public modelInfo?: string;

    // @DataMember(Order=9)
    public modalities?: string;

    // @DataMember(Order=10)
    public messages?: string;

    // @DataMember(Order=11)
    public streamingMessage?: string;

    // @DataMember(Order=12)
    public args?: string;

    // @DataMember(Order=13)
    public tools?: string;

    // @DataMember(Order=14)
    public toolHistory?: string;

    // @DataMember(Order=15)
    public cost?: number;

    // @DataMember(Order=16)
    public inputTokens?: number;

    // @DataMember(Order=17)
    public outputTokens?: number;

    // @DataMember(Order=18)
    public stats?: string;

    // @DataMember(Order=19)
    public provider?: string;

    // @DataMember(Order=20)
    public providerModel?: string;

    // @DataMember(Order=21)
    public startedAt?: string;

    // @DataMember(Order=22)
    public completedAt?: string;

    // @DataMember(Order=23)
    public metadata?: string;

    // @DataMember(Order=24)
    public status?: string;

    // @DataMember(Order=25)
    public error?: string;

    // @DataMember(Order=26)
    public ref?: string;

    // @DataMember(Order=27)
    public providerResponse?: string;

    // @DataMember(Order=28)
    public contextTokens?: number;

    // @DataMember(Order=29)
    public parentId?: number;

    // @DataMember(Order=30)
    public publishedAt?: string;

    // @DataMember(Order=31)
    public publishedUrl?: string;

    public constructor(init?: Partial<PatchChatThread>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatThread'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chattoolapprovals/{Id}", "PATCH")
// @DataContract
export class PatchChatToolApproval implements IReturn<IdResponse>, IPatch, IPatchDb<ChatToolApproval>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public batchId?: string;

    // @DataMember(Order=3)
    public threadId: number;

    // @DataMember(Order=4)
    public user?: string;

    // @DataMember(Order=5)
    public toolCallId?: string;

    // @DataMember(Order=6)
    public toolName?: string;

    // @DataMember(Order=7)
    public apiName?: string;

    // @DataMember(Order=8)
    public requestType?: string;

    // @DataMember(Order=9)
    public method?: string;

    // @DataMember(Order=10)
    public route?: string;

    // @DataMember(Order=11)
    public safety?: string;

    // @DataMember(Order=12)
    public status?: string;

    // @DataMember(Order=13)
    public sequence: number;

    // @DataMember(Order=14)
    public description?: string;

    // @DataMember(Order=15)
    public schema?: string;

    // @DataMember(Order=16)
    public proposedArgs?: string;

    // @DataMember(Order=17)
    public effectiveArgs?: string;

    // @DataMember(Order=18)
    public result?: string;

    // @DataMember(Order=19)
    public toolResult?: string;

    // @DataMember(Order=20)
    public error?: string;

    // @DataMember(Order=21)
    public reason?: string;

    // @DataMember(Order=22)
    public createdAt?: string;

    // @DataMember(Order=23)
    public updatedAt?: string;

    // @DataMember(Order=24)
    public resolvedAt?: string;

    public constructor(init?: Partial<PatchChatToolApproval>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatToolApproval'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chattoolapprovalbatches/{Id}", "PATCH")
// @DataContract
export class PatchChatToolApprovalBatch implements IReturn<IdResponse>, IPatch, IPatchDb<ChatToolApprovalBatch>
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public status?: string;

    // @DataMember(Order=5)
    public createdAt?: string;

    // @DataMember(Order=6)
    public updatedAt?: string;

    // @DataMember(Order=7)
    public completedAt?: string;

    public constructor(init?: Partial<PatchChatToolApprovalBatch>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchChatToolApprovalBatch'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/coffeeshoporders/{Id}", "PATCH")
// @DataContract
export class PatchCoffeeShopOrder implements IReturn<IdResponse>, IPatch, IPatchDb<CoffeeShopOrder>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public orderNumber?: string;

    // @DataMember(Order=3)
    public customerName?: string;

    // @DataMember(Order=4)
    public customerUserId?: string;

    // @DataMember(Order=5)
    public status?: string;

    // @DataMember(Order=6)
    public notes?: string;

    // @DataMember(Order=7)
    public subtotal: number;

    // @DataMember(Order=8)
    public createdDate?: string;

    public constructor(init?: Partial<PatchCoffeeShopOrder>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchCoffeeShopOrder'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/coffeeshoporderitems/{Id}", "PATCH")
// @DataContract
export class PatchCoffeeShopOrderItem implements IReturn<IdResponse>, IPatch, IPatchDb<CoffeeShopOrderItem>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public coffeeShopOrderId: number;

    // @DataMember(Order=3)
    public productId: number;

    // @DataMember(Order=4)
    public productName?: string;

    // @DataMember(Order=5)
    public quantity: number;

    // @DataMember(Order=6)
    public size?: string;

    // @DataMember(Order=7)
    public temperature?: string;

    // @DataMember(Order=8)
    public optionsJson?: string;

    // @DataMember(Order=9)
    public unitPrice: number;

    // @DataMember(Order=10)
    public lineTotal: number;

    public constructor(init?: Partial<PatchCoffeeShopOrderItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchCoffeeShopOrderItem'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/contextsnapshots/{Id}", "PATCH")
// @DataContract
export class PatchContextSnapshot implements IReturn<IdResponse>, IPatch, IPatchDb<ContextSnapshot>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public runId?: number;

    // @DataMember(Order=4)
    public version: number;

    // @DataMember(Order=5)
    public fromSequence: number;

    // @DataMember(Order=6)
    public toSequence: number;

    // @DataMember(Order=7)
    public summary?: string;

    // @DataMember(Order=8)
    public tokenCount?: number;

    // @DataMember(Order=9)
    public model?: string;

    // @DataMember(Order=10)
    public createdAt?: string;

    public constructor(init?: Partial<PatchContextSnapshot>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchContextSnapshot'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/efmigrationshistories/{MigrationId}", "PATCH")
// @DataContract
export class PatchEFMigrationsHistory implements IReturn<IdResponse>, IPatch, IPatchDb<EFMigrationsHistory>
{
    // @DataMember(Order=1)
    public migrationId?: string;

    // @DataMember(Order=2)
    public productVersion?: string;

    public constructor(init?: Partial<PatchEFMigrationsHistory>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchEFMigrationsHistory'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/efmigrationslocks/{Id}", "PATCH")
// @DataContract
export class PatchEFMigrationsLock implements IReturn<IdResponse>, IPatch, IPatchDb<EFMigrationsLock>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public timestamp?: string;

    public constructor(init?: Partial<PatchEFMigrationsLock>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchEFMigrationsLock'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/filesystemfiles/{Id}", "PATCH")
// @DataContract
export class PatchFileSystemFile implements IReturn<IdResponse>, IPatch, IPatchDb<FileSystemFile>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public fileName?: string;

    // @DataMember(Order=3)
    public filePath?: string;

    // @DataMember(Order=4)
    public contentType?: string;

    // @DataMember(Order=5)
    public contentLength: number;

    // @DataMember(Order=6)
    public fileSystemItemId: number;

    public constructor(init?: Partial<PatchFileSystemFile>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchFileSystemFile'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/filesystemitems/{Id}", "PATCH")
// @DataContract
export class PatchFileSystemItem implements IReturn<IdResponse>, IPatch, IPatchDb<FileSystemItem>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public fileAccessType?: string;

    // @DataMember(Order=3)
    public applicationUserId?: string;

    public constructor(init?: Partial<PatchFileSystemItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchFileSystemItem'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/migrations/{Id}", "PATCH")
// @DataContract
export class PatchMigration implements IReturn<IdResponse>, IPatch, IPatchDb<Migration>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public description?: string;

    // @DataMember(Order=4)
    public createdDate?: string;

    // @DataMember(Order=5)
    public completedDate?: string;

    // @DataMember(Order=6)
    public connectionString?: string;

    // @DataMember(Order=7)
    public namedConnection?: string;

    // @DataMember(Order=8)
    public log?: string;

    // @DataMember(Order=9)
    public errorCode?: string;

    // @DataMember(Order=10)
    public errorMessage?: string;

    // @DataMember(Order=11)
    public errorStackTrace?: string;

    // @DataMember(Order=12)
    public meta?: string;

    public constructor(init?: Partial<PatchMigration>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchMigration'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/options/{Id}", "PATCH")
// @DataContract
export class PatchOption implements IReturn<IdResponse>, IPatch, IPatchDb<Option>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public type?: string;

    // @DataMember(Order=3)
    public names?: string;

    // @DataMember(Order=4)
    public allowQuantity?: number;

    // @DataMember(Order=5)
    public quantityLabel?: string;

    public constructor(init?: Partial<PatchOption>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchOption'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/optionquantities/{Id}", "PATCH")
// @DataContract
export class PatchOptionQuantity implements IReturn<IdResponse>, IPatch, IPatchDb<OptionQuantity>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public value: number;

    public constructor(init?: Partial<PatchOptionQuantity>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchOptionQuantity'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/products/{Id}", "PATCH")
// @DataContract
export class PatchProduct implements IReturn<IdResponse>, IPatch, IPatchDb<Product>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public categoryId: number;

    // @DataMember(Order=3)
    public name?: string;

    // @DataMember(Order=4)
    public cost: number;

    // @DataMember(Order=5)
    public imageUrl?: string;

    public constructor(init?: Partial<PatchProduct>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchProduct'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/validationrules/{Id}", "PATCH")
// @DataContract
export class PatchValidationRule implements IReturn<IdResponse>, IPatch, IPatchDb<ValidationRule>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public type?: string;

    // @DataMember(Order=3)
    public field?: string;

    // @DataMember(Order=4)
    public createdBy?: string;

    // @DataMember(Order=5)
    public createdDate?: string;

    // @DataMember(Order=6)
    public modifiedBy?: string;

    // @DataMember(Order=7)
    public modifiedDate?: string;

    // @DataMember(Order=8)
    public suspendedBy?: string;

    // @DataMember(Order=9)
    public suspendedDate?: string;

    // @DataMember(Order=10)
    public notes?: string;

    // @DataMember(Order=11)
    public validator?: string;

    // @DataMember(Order=12)
    public condition?: string;

    // @DataMember(Order=13)
    public errorCode?: string;

    // @DataMember(Order=14)
    public message?: string;

    public constructor(init?: Partial<PatchValidationRule>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'PatchValidationRule'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/agentruns/{Id}", "PUT")
// @DataContract
export class UpdateAgentRun implements IReturn<IdResponse>, IPut, IUpdateDb<AgentRun>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public status?: string;

    // @DataMember(Order=5)
    public nextAction?: string;

    // @DataMember(Order=6)
    public model?: string;

    // @DataMember(Order=7)
    public stepCount: number;

    // @DataMember(Order=8)
    public sliceCount: number;

    // @DataMember(Order=9)
    public maxSteps: number;

    // @DataMember(Order=10)
    public contextTokens?: number;

    // @DataMember(Order=11)
    public contextLimit?: number;

    // @DataMember(Order=12)
    public leaseOwner?: string;

    // @DataMember(Order=13)
    public leaseExpiresAt?: string;

    // @DataMember(Order=14)
    public nextAttemptAt?: string;

    // @DataMember(Order=15)
    public error?: string;

    // @DataMember(Order=16)
    public createdAt?: string;

    // @DataMember(Order=17)
    public updatedAt?: string;

    // @DataMember(Order=18)
    public completedAt?: string;

    public constructor(init?: Partial<UpdateAgentRun>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAgentRun'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/agentsteps/{Id}", "PUT")
// @DataContract
export class UpdateAgentStep implements IReturn<IdResponse>, IPut, IUpdateDb<AgentStep>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public runId: number;

    // @DataMember(Order=3)
    public sequence: number;

    // @DataMember(Order=4)
    public type?: string;

    // @DataMember(Order=5)
    public status?: string;

    // @DataMember(Order=6)
    public input?: string;

    // @DataMember(Order=7)
    public output?: string;

    // @DataMember(Order=8)
    public idempotencyKey?: string;

    // @DataMember(Order=9)
    public attempt: number;

    // @DataMember(Order=10)
    public error?: string;

    // @DataMember(Order=11)
    public startedAt?: string;

    // @DataMember(Order=12)
    public completedAt?: string;

    // @DataMember(Order=13)
    public createdAt?: string;

    public constructor(init?: Partial<UpdateAgentStep>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAgentStep'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatdocuments/{Id}", "PUT")
// @DataContract
export class UpdateAichatDocument implements IReturn<IdResponse>, IPut, IUpdateDb<AichatDocument>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public filename?: string;

    // @DataMember(Order=7)
    public url?: string;

    // @DataMember(Order=8)
    public hash?: string;

    // @DataMember(Order=9)
    public size?: number;

    // @DataMember(Order=10)
    public displayName?: string;

    // @DataMember(Order=11)
    public name?: string;

    // @DataMember(Order=12)
    public customMetadata?: string;

    // @DataMember(Order=13)
    public createTime?: string;

    // @DataMember(Order=14)
    public updateTime?: string;

    // @DataMember(Order=15)
    public sizeBytes?: number;

    // @DataMember(Order=16)
    public mimeType?: string;

    // @DataMember(Order=17)
    public state?: string;

    // @DataMember(Order=18)
    public category?: string;

    // @DataMember(Order=19)
    public tags?: string;

    // @DataMember(Order=20)
    public startedAt?: string;

    // @DataMember(Order=21)
    public uploadedAt?: string;

    // @DataMember(Order=22)
    public metadata?: string;

    // @DataMember(Order=23)
    public error?: string;

    // @DataMember(Order=24)
    public ref?: string;

    public constructor(init?: Partial<UpdateAichatDocument>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAichatDocument'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatfilestores/{Id}", "PUT")
// @DataContract
export class UpdateAichatFilestore implements IReturn<IdResponse>, IPut, IUpdateDb<AichatFilestore>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public updatedAt?: string;

    // @DataMember(Order=5)
    public name?: string;

    // @DataMember(Order=6)
    public displayName?: string;

    // @DataMember(Order=7)
    public createTime?: string;

    // @DataMember(Order=8)
    public updateTime?: string;

    // @DataMember(Order=9)
    public activeDocumentsCount?: number;

    // @DataMember(Order=10)
    public pendingDocumentsCount?: number;

    // @DataMember(Order=11)
    public failedDocumentsCount?: number;

    // @DataMember(Order=12)
    public sizeBytes?: number;

    // @DataMember(Order=13)
    public metadata?: string;

    // @DataMember(Order=14)
    public error?: string;

    // @DataMember(Order=15)
    public ref?: string;

    public constructor(init?: Partial<UpdateAichatFilestore>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAichatFilestore'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aichatmedias/{Id}", "PUT")
// @DataContract
export class UpdateAichatMedia implements IReturn<IdResponse>, IPut, IUpdateDb<AichatMedia>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public name?: string;

    // @DataMember(Order=4)
    public type?: string;

    // @DataMember(Order=5)
    public prompt?: string;

    // @DataMember(Order=6)
    public model?: string;

    // @DataMember(Order=7)
    public created?: string;

    // @DataMember(Order=8)
    public cost?: number;

    // @DataMember(Order=9)
    public seed?: number;

    // @DataMember(Order=10)
    public url?: string;

    // @DataMember(Order=11)
    public hash?: string;

    // @DataMember(Order=12)
    public aspectRatio?: string;

    // @DataMember(Order=13)
    public width?: number;

    // @DataMember(Order=14)
    public height?: number;

    // @DataMember(Order=15)
    public size?: number;

    // @DataMember(Order=16)
    public duration?: number;

    // @DataMember(Order=17)
    public reactions?: string;

    // @DataMember(Order=18)
    public caption?: string;

    // @DataMember(Order=19)
    public description?: string;

    // @DataMember(Order=20)
    public phash?: string;

    // @DataMember(Order=21)
    public color?: string;

    // @DataMember(Order=22)
    public category?: string;

    // @DataMember(Order=23)
    public tags?: string;

    // @DataMember(Order=24)
    public rating?: string;

    // @DataMember(Order=25)
    public ratings?: string;

    // @DataMember(Order=26)
    public objects?: string;

    // @DataMember(Order=27)
    public variantId?: string;

    // @DataMember(Order=28)
    public variantName?: string;

    // @DataMember(Order=29)
    public publishedAt?: string;

    // @DataMember(Order=30)
    public publishedUrl?: string;

    // @DataMember(Order=31)
    public metadata?: string;

    public constructor(init?: Partial<UpdateAichatMedia>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAichatMedia'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetroleclaims/{Id}", "PUT")
// @DataContract
export class UpdateAspNetRoleClaims implements IReturn<IdResponse>, IPut, IUpdateDb<AspNetRoleClaims>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public roleId?: string;

    // @DataMember(Order=3)
    public claimType?: string;

    // @DataMember(Order=4)
    public claimValue?: string;

    public constructor(init?: Partial<UpdateAspNetRoleClaims>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAspNetRoleClaims'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetroles/{Id}", "PUT")
// @DataContract
export class UpdateAspNetRoles implements IReturn<IdResponse>, IPut, IUpdateDb<AspNetRoles>
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public normalizedName?: string;

    // @DataMember(Order=4)
    public concurrencyStamp?: string;

    public constructor(init?: Partial<UpdateAspNetRoles>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAspNetRoles'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetuserclaims/{Id}", "PUT")
// @ValidateRequest(Validator="IsAdmin")
// @DataContract
export class UpdateAspNetUserClaims implements IReturn<IdResponse>, IPut, IUpdateDb<AspNetUserClaims>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public userId?: string;

    // @DataMember(Order=3)
    public claimType?: string;

    // @DataMember(Order=4)
    public claimValue?: string;

    public constructor(init?: Partial<UpdateAspNetUserClaims>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAspNetUserClaims'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/aspnetusers/{Id}", "PUT")
// @ValidateRequest(Validator="IsAdmin")
// @DataContract
export class UpdateAspNetUsers implements IReturn<IdResponse>, IPut, IUpdateDb<AspNetUsers>
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public firstName?: string;

    // @DataMember(Order=3)
    public lastName?: string;

    // @DataMember(Order=4)
    public displayName?: string;

    // @DataMember(Order=5)
    public profileUrl?: string;

    // @DataMember(Order=6)
    public refreshToken?: string;

    // @DataMember(Order=7)
    public refreshTokenExpiry?: string;

    // @DataMember(Order=8)
    public userName?: string;

    // @DataMember(Order=9)
    public normalizedUserName?: string;

    // @DataMember(Order=10)
    public email?: string;

    // @DataMember(Order=11)
    public normalizedEmail?: string;

    // @DataMember(Order=12)
    public emailConfirmed: number;

    // @DataMember(Order=13)
    public passwordHash?: string;

    // @DataMember(Order=14)
    public securityStamp?: string;

    // @DataMember(Order=15)
    public concurrencyStamp?: string;

    // @DataMember(Order=16)
    public phoneNumber?: string;

    // @DataMember(Order=17)
    public phoneNumberConfirmed: number;

    // @DataMember(Order=18)
    public twoFactorEnabled: number;

    // @DataMember(Order=19)
    public lockoutEnd?: string;

    // @DataMember(Order=20)
    public lockoutEnabled: number;

    // @DataMember(Order=21)
    public accessFailedCount: number;

    public constructor(init?: Partial<UpdateAspNetUsers>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateAspNetUsers'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/categories/{Id}", "PUT")
// @DataContract
export class UpdateCategory implements IReturn<IdResponse>, IPut, IUpdateDb<Category>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public description?: string;

    // @DataMember(Order=4)
    public temperatures?: string;

    // @DataMember(Order=5)
    public defaultTemperature?: string;

    // @DataMember(Order=6)
    public sizes?: string;

    // @DataMember(Order=7)
    public defaultSize?: string;

    // @DataMember(Order=8)
    public imageUrl?: string;

    public constructor(init?: Partial<UpdateCategory>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateCategory'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/categoryoptions/{Id}", "PUT")
// @DataContract
export class UpdateCategoryOption implements IReturn<IdResponse>, IPut, IUpdateDb<CategoryOption>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public categoryId: number;

    // @DataMember(Order=3)
    public optionId: number;

    public constructor(init?: Partial<UpdateCategoryOption>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateCategoryOption'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistants/{Id}", "PUT")
// @DataContract
export class UpdateChatAssistant implements IReturn<IdResponse>, IPut, IUpdateDb<ChatAssistant>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public name?: string;

    // @DataMember(Order=7)
    public publicId?: string;

    // @DataMember(Order=8)
    public enabled: number;

    // @DataMember(Order=9)
    public publishedAt?: string;

    // @DataMember(Order=10)
    public config?: string;

    public constructor(init?: Partial<UpdateChatAssistant>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatAssistant'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistantconversations/{Id}", "PUT")
// @DataContract
export class UpdateChatAssistantConversation implements IReturn<IdResponse>, IPut, IUpdateDb<ChatAssistantConversation>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public assistantId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public sessionId?: string;

    // @DataMember(Order=7)
    public origin?: string;

    // @DataMember(Order=8)
    public pageUrl?: string;

    // @DataMember(Order=9)
    public userAgent?: string;

    // @DataMember(Order=10)
    public title?: string;

    // @DataMember(Order=11)
    public status?: string;

    // @DataMember(Order=12)
    public messageCount: number;

    // @DataMember(Order=13)
    public lastMessage?: string;

    public constructor(init?: Partial<UpdateChatAssistantConversation>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatAssistantConversation'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatassistantmessages/{Id}", "PUT")
// @DataContract
export class UpdateChatAssistantMessage implements IReturn<IdResponse>, IPut, IUpdateDb<ChatAssistantMessage>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public conversationId: number;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public role?: string;

    // @DataMember(Order=5)
    public content?: string;

    // @DataMember(Order=6)
    public citations?: string;

    // @DataMember(Order=7)
    public error?: string;

    public constructor(init?: Partial<UpdateChatAssistantMessage>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatAssistantMessage'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatdocuments/{Id}", "PUT")
// @DataContract
export class UpdateChatDocument implements IReturn<IdResponse>, IPut, IUpdateDb<ChatDocument>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public filename?: string;

    // @DataMember(Order=7)
    public url?: string;

    // @DataMember(Order=8)
    public hash?: string;

    // @DataMember(Order=9)
    public size?: number;

    // @DataMember(Order=10)
    public displayName?: string;

    // @DataMember(Order=11)
    public name?: string;

    // @DataMember(Order=12)
    public customMetadata?: string;

    // @DataMember(Order=13)
    public createTime?: string;

    // @DataMember(Order=14)
    public updateTime?: string;

    // @DataMember(Order=15)
    public sizeBytes?: number;

    // @DataMember(Order=16)
    public mimeType?: string;

    // @DataMember(Order=17)
    public state?: string;

    // @DataMember(Order=18)
    public category?: string;

    // @DataMember(Order=19)
    public tags?: string;

    // @DataMember(Order=20)
    public startedAt?: string;

    // @DataMember(Order=21)
    public uploadedAt?: string;

    // @DataMember(Order=22)
    public metadata?: string;

    // @DataMember(Order=23)
    public error?: string;

    // @DataMember(Order=24)
    public ref?: string;

    // @DataMember(Order=25)
    public sourceUrl?: string;

    // @DataMember(Order=26)
    public sourceId?: number;

    // @DataMember(Order=27)
    public sourceScopeId: number;

    // @DataMember(Order=28)
    public sourceKey?: string;

    // @DataMember(Order=29)
    public sourceEtag?: string;

    // @DataMember(Order=30)
    public contentHash?: string;

    // @DataMember(Order=31)
    public metadataHash?: string;

    // @DataMember(Order=32)
    public extractorVer?: string;

    // @DataMember(Order=33)
    public tombstonedAt?: string;

    // @DataMember(Order=34)
    public categoryPath?: string;

    // @DataMember(Order=35)
    public docType?: string;

    // @DataMember(Order=36)
    public status?: string;

    // @DataMember(Order=37)
    public locale?: string;

    // @DataMember(Order=38)
    public product?: string;

    // @DataMember(Order=39)
    public versions?: string;

    // @DataMember(Order=40)
    public sourceUpdatedAt?: number;

    public constructor(init?: Partial<UpdateChatDocument>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatDocument'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatfilestores/{Id}", "PUT")
// @DataContract
export class UpdateChatFilestore implements IReturn<IdResponse>, IPut, IUpdateDb<ChatFilestore>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public updatedAt?: string;

    // @DataMember(Order=5)
    public name?: string;

    // @DataMember(Order=6)
    public displayName?: string;

    // @DataMember(Order=7)
    public createTime?: string;

    // @DataMember(Order=8)
    public updateTime?: string;

    // @DataMember(Order=9)
    public activeDocumentsCount?: number;

    // @DataMember(Order=10)
    public pendingDocumentsCount?: number;

    // @DataMember(Order=11)
    public failedDocumentsCount?: number;

    // @DataMember(Order=12)
    public sizeBytes?: number;

    // @DataMember(Order=13)
    public metadata?: string;

    // @DataMember(Order=14)
    public error?: string;

    // @DataMember(Order=15)
    public ref?: string;

    // @DataMember(Order=16)
    public visibility?: string;

    // @DataMember(Order=17)
    public facets?: string;

    public constructor(init?: Partial<UpdateChatFilestore>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatFilestore'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatmedias/{Id}", "PUT")
// @DataContract
export class UpdateChatMedia implements IReturn<IdResponse>, IPut, IUpdateDb<ChatMedia>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public name?: string;

    // @DataMember(Order=4)
    public type?: string;

    // @DataMember(Order=5)
    public prompt?: string;

    // @DataMember(Order=6)
    public model?: string;

    // @DataMember(Order=7)
    public created?: string;

    // @DataMember(Order=8)
    public cost?: number;

    // @DataMember(Order=9)
    public seed?: number;

    // @DataMember(Order=10)
    public url?: string;

    // @DataMember(Order=11)
    public hash?: string;

    // @DataMember(Order=12)
    public aspectRatio?: string;

    // @DataMember(Order=13)
    public width?: number;

    // @DataMember(Order=14)
    public height?: number;

    // @DataMember(Order=15)
    public size?: number;

    // @DataMember(Order=16)
    public duration?: number;

    // @DataMember(Order=17)
    public reactions?: string;

    // @DataMember(Order=18)
    public caption?: string;

    // @DataMember(Order=19)
    public description?: string;

    // @DataMember(Order=20)
    public phash?: string;

    // @DataMember(Order=21)
    public color?: string;

    // @DataMember(Order=22)
    public category?: string;

    // @DataMember(Order=23)
    public tags?: string;

    // @DataMember(Order=24)
    public rating?: string;

    // @DataMember(Order=25)
    public ratings?: string;

    // @DataMember(Order=26)
    public objects?: string;

    // @DataMember(Order=27)
    public variantId?: string;

    // @DataMember(Order=28)
    public variantName?: string;

    // @DataMember(Order=29)
    public publishedAt?: string;

    // @DataMember(Order=30)
    public publishedUrl?: string;

    // @DataMember(Order=31)
    public metadata?: string;

    public constructor(init?: Partial<UpdateChatMedia>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatMedia'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatmessages/{Id}", "PUT")
// @DataContract
export class UpdateChatMessage implements IReturn<IdResponse>, IPut, IUpdateDb<ChatMessage>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public sequence: number;

    // @DataMember(Order=4)
    public runId?: number;

    // @DataMember(Order=5)
    public stepId?: number;

    // @DataMember(Order=6)
    public role?: string;

    // @DataMember(Order=7)
    public message?: string;

    // @DataMember(Order=8)
    public timestamp?: number;

    // @DataMember(Order=9)
    public toolCallId?: string;

    // @DataMember(Order=10)
    public toolName?: string;

    // @DataMember(Order=11)
    public tokenCount?: number;

    // @DataMember(Order=12)
    public active: number;

    // @DataMember(Order=13)
    public createdAt?: string;

    public constructor(init?: Partial<UpdateChatMessage>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatMessage'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatrequests/{Id}", "PUT")
// @DataContract
export class UpdateChatRequest implements IReturn<IdResponse>, IPut, IUpdateDb<ChatRequest>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public threadId?: number;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public title?: string;

    // @DataMember(Order=7)
    public model?: string;

    // @DataMember(Order=8)
    public duration?: number;

    // @DataMember(Order=9)
    public cost?: number;

    // @DataMember(Order=10)
    public inputPrice?: number;

    // @DataMember(Order=11)
    public inputTokens?: number;

    // @DataMember(Order=12)
    public inputCachedTokens?: number;

    // @DataMember(Order=13)
    public outputPrice?: number;

    // @DataMember(Order=14)
    public outputTokens?: number;

    // @DataMember(Order=15)
    public totalTokens?: number;

    // @DataMember(Order=16)
    public usage?: string;

    // @DataMember(Order=17)
    public provider?: string;

    // @DataMember(Order=18)
    public providerModel?: string;

    // @DataMember(Order=19)
    public providerRef?: string;

    // @DataMember(Order=20)
    public finishReason?: string;

    // @DataMember(Order=21)
    public startedAt?: string;

    // @DataMember(Order=22)
    public completedAt?: string;

    // @DataMember(Order=23)
    public error?: string;

    // @DataMember(Order=24)
    public stackTrace?: string;

    // @DataMember(Order=25)
    public ref?: string;

    public constructor(init?: Partial<UpdateChatRequest>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatRequest'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatsources/{Id}", "PUT")
// @DataContract
export class UpdateChatSource implements IReturn<IdResponse>, IPut, IUpdateDb<ChatSource>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public filestoreId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public createdAt?: string;

    // @DataMember(Order=5)
    public updatedAt?: string;

    // @DataMember(Order=6)
    public name?: string;

    // @DataMember(Order=7)
    public type?: string;

    // @DataMember(Order=8)
    public enabled: number;

    // @DataMember(Order=9)
    public config?: string;

    // @DataMember(Order=10)
    public category?: string;

    // @DataMember(Order=11)
    public rules?: string;

    // @DataMember(Order=12)
    public include?: string;

    // @DataMember(Order=13)
    public exclude?: string;

    // @DataMember(Order=14)
    public extract?: string;

    // @DataMember(Order=15)
    public chunking?: string;

    // @DataMember(Order=16)
    public volatile?: string;

    // @DataMember(Order=17)
    public extractorVer?: string;

    // @DataMember(Order=18)
    public schedule?: string;

    // @DataMember(Order=19)
    public onDelete?: string;

    // @DataMember(Order=20)
    public cursor?: string;

    // @DataMember(Order=21)
    public lastRunId?: number;

    // @DataMember(Order=22)
    public lastRunAt?: string;

    // @DataMember(Order=23)
    public error?: string;

    public constructor(init?: Partial<UpdateChatSource>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatSource'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatsourceruns/{Id}", "PUT")
// @DataContract
export class UpdateChatSourceRun implements IReturn<IdResponse>, IPut, IUpdateDb<ChatSourceRun>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public sourceId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public startedAt?: string;

    // @DataMember(Order=5)
    public completedAt?: string;

    // @DataMember(Order=6)
    public status?: string;

    // @DataMember(Order=7)
    public dryRun: number;

    // @DataMember(Order=8)
    public discovered: number;

    // @DataMember(Order=9)
    public added: number;

    // @DataMember(Order=10)
    public changed: number;

    // @DataMember(Order=11)
    public metadataOnly: number;

    // @DataMember(Order=12)
    public unchanged: number;

    // @DataMember(Order=13)
    public removed: number;

    // @DataMember(Order=14)
    public skipped: number;

    // @DataMember(Order=15)
    public failed: number;

    // @DataMember(Order=16)
    public bytes: number;

    // @DataMember(Order=17)
    public plan?: string;

    // @DataMember(Order=18)
    public log?: string;

    // @DataMember(Order=19)
    public error?: string;

    public constructor(init?: Partial<UpdateChatSourceRun>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatSourceRun'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chatthreads/{Id}", "PUT")
// @DataContract
export class UpdateChatThread implements IReturn<IdResponse>, IPut, IUpdateDb<ChatThread>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public user?: string;

    // @DataMember(Order=3)
    public createdAt?: string;

    // @DataMember(Order=4)
    public updatedAt?: string;

    // @DataMember(Order=5)
    public title?: string;

    // @DataMember(Order=6)
    public systemPrompt?: string;

    // @DataMember(Order=7)
    public model?: string;

    // @DataMember(Order=8)
    public modelInfo?: string;

    // @DataMember(Order=9)
    public modalities?: string;

    // @DataMember(Order=10)
    public messages?: string;

    // @DataMember(Order=11)
    public streamingMessage?: string;

    // @DataMember(Order=12)
    public args?: string;

    // @DataMember(Order=13)
    public tools?: string;

    // @DataMember(Order=14)
    public toolHistory?: string;

    // @DataMember(Order=15)
    public cost?: number;

    // @DataMember(Order=16)
    public inputTokens?: number;

    // @DataMember(Order=17)
    public outputTokens?: number;

    // @DataMember(Order=18)
    public stats?: string;

    // @DataMember(Order=19)
    public provider?: string;

    // @DataMember(Order=20)
    public providerModel?: string;

    // @DataMember(Order=21)
    public startedAt?: string;

    // @DataMember(Order=22)
    public completedAt?: string;

    // @DataMember(Order=23)
    public metadata?: string;

    // @DataMember(Order=24)
    public status?: string;

    // @DataMember(Order=25)
    public error?: string;

    // @DataMember(Order=26)
    public ref?: string;

    // @DataMember(Order=27)
    public providerResponse?: string;

    // @DataMember(Order=28)
    public contextTokens?: number;

    // @DataMember(Order=29)
    public parentId?: number;

    // @DataMember(Order=30)
    public publishedAt?: string;

    // @DataMember(Order=31)
    public publishedUrl?: string;

    public constructor(init?: Partial<UpdateChatThread>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatThread'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chattoolapprovals/{Id}", "PUT")
// @DataContract
export class UpdateChatToolApproval implements IReturn<IdResponse>, IPut, IUpdateDb<ChatToolApproval>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public batchId?: string;

    // @DataMember(Order=3)
    public threadId: number;

    // @DataMember(Order=4)
    public user?: string;

    // @DataMember(Order=5)
    public toolCallId?: string;

    // @DataMember(Order=6)
    public toolName?: string;

    // @DataMember(Order=7)
    public apiName?: string;

    // @DataMember(Order=8)
    public requestType?: string;

    // @DataMember(Order=9)
    public method?: string;

    // @DataMember(Order=10)
    public route?: string;

    // @DataMember(Order=11)
    public safety?: string;

    // @DataMember(Order=12)
    public status?: string;

    // @DataMember(Order=13)
    public sequence: number;

    // @DataMember(Order=14)
    public description?: string;

    // @DataMember(Order=15)
    public schema?: string;

    // @DataMember(Order=16)
    public proposedArgs?: string;

    // @DataMember(Order=17)
    public effectiveArgs?: string;

    // @DataMember(Order=18)
    public result?: string;

    // @DataMember(Order=19)
    public toolResult?: string;

    // @DataMember(Order=20)
    public error?: string;

    // @DataMember(Order=21)
    public reason?: string;

    // @DataMember(Order=22)
    public createdAt?: string;

    // @DataMember(Order=23)
    public updatedAt?: string;

    // @DataMember(Order=24)
    public resolvedAt?: string;

    public constructor(init?: Partial<UpdateChatToolApproval>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatToolApproval'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/chattoolapprovalbatches/{Id}", "PUT")
// @DataContract
export class UpdateChatToolApprovalBatch implements IReturn<IdResponse>, IPut, IUpdateDb<ChatToolApprovalBatch>
{
    // @DataMember(Order=1)
    public id?: string;

    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public user?: string;

    // @DataMember(Order=4)
    public status?: string;

    // @DataMember(Order=5)
    public createdAt?: string;

    // @DataMember(Order=6)
    public updatedAt?: string;

    // @DataMember(Order=7)
    public completedAt?: string;

    public constructor(init?: Partial<UpdateChatToolApprovalBatch>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateChatToolApprovalBatch'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/coffeeshoporders/{Id}", "PUT")
// @DataContract
export class UpdateCoffeeShopOrder implements IReturn<IdResponse>, IPut, IUpdateDb<CoffeeShopOrder>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public orderNumber?: string;

    // @DataMember(Order=3)
    public customerName?: string;

    // @DataMember(Order=4)
    public customerUserId?: string;

    // @DataMember(Order=5)
    public status?: string;

    // @DataMember(Order=6)
    public notes?: string;

    // @DataMember(Order=7)
    public subtotal: number;

    // @DataMember(Order=8)
    public createdDate?: string;

    public constructor(init?: Partial<UpdateCoffeeShopOrder>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateCoffeeShopOrder'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/coffeeshoporderitems/{Id}", "PUT")
// @DataContract
export class UpdateCoffeeShopOrderItem implements IReturn<IdResponse>, IPut, IUpdateDb<CoffeeShopOrderItem>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public coffeeShopOrderId: number;

    // @DataMember(Order=3)
    public productId: number;

    // @DataMember(Order=4)
    public productName?: string;

    // @DataMember(Order=5)
    public quantity: number;

    // @DataMember(Order=6)
    public size?: string;

    // @DataMember(Order=7)
    public temperature?: string;

    // @DataMember(Order=8)
    public optionsJson?: string;

    // @DataMember(Order=9)
    public unitPrice: number;

    // @DataMember(Order=10)
    public lineTotal: number;

    public constructor(init?: Partial<UpdateCoffeeShopOrderItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateCoffeeShopOrderItem'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/contextsnapshots/{Id}", "PUT")
// @DataContract
export class UpdateContextSnapshot implements IReturn<IdResponse>, IPut, IUpdateDb<ContextSnapshot>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public threadId: number;

    // @DataMember(Order=3)
    public runId?: number;

    // @DataMember(Order=4)
    public version: number;

    // @DataMember(Order=5)
    public fromSequence: number;

    // @DataMember(Order=6)
    public toSequence: number;

    // @DataMember(Order=7)
    public summary?: string;

    // @DataMember(Order=8)
    public tokenCount?: number;

    // @DataMember(Order=9)
    public model?: string;

    // @DataMember(Order=10)
    public createdAt?: string;

    public constructor(init?: Partial<UpdateContextSnapshot>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateContextSnapshot'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/efmigrationshistories/{MigrationId}", "PUT")
// @DataContract
export class UpdateEFMigrationsHistory implements IReturn<IdResponse>, IPut, IUpdateDb<EFMigrationsHistory>
{
    // @DataMember(Order=1)
    public migrationId?: string;

    // @DataMember(Order=2)
    public productVersion?: string;

    public constructor(init?: Partial<UpdateEFMigrationsHistory>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateEFMigrationsHistory'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/efmigrationslocks/{Id}", "PUT")
// @DataContract
export class UpdateEFMigrationsLock implements IReturn<IdResponse>, IPut, IUpdateDb<EFMigrationsLock>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public timestamp?: string;

    public constructor(init?: Partial<UpdateEFMigrationsLock>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateEFMigrationsLock'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/filesystemfiles/{Id}", "PUT")
// @DataContract
export class UpdateFileSystemFile implements IReturn<IdResponse>, IPut, IUpdateDb<FileSystemFile>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public fileName?: string;

    // @DataMember(Order=3)
    public filePath?: string;

    // @DataMember(Order=4)
    public contentType?: string;

    // @DataMember(Order=5)
    public contentLength: number;

    // @DataMember(Order=6)
    public fileSystemItemId: number;

    public constructor(init?: Partial<UpdateFileSystemFile>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateFileSystemFile'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/filesystemitems/{Id}", "PUT")
// @DataContract
export class UpdateFileSystemItem implements IReturn<IdResponse>, IPut, IUpdateDb<FileSystemItem>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public fileAccessType?: string;

    // @DataMember(Order=3)
    public applicationUserId?: string;

    public constructor(init?: Partial<UpdateFileSystemItem>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateFileSystemItem'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/migrations/{Id}", "PUT")
// @DataContract
export class UpdateMigration implements IReturn<IdResponse>, IPut, IUpdateDb<Migration>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public description?: string;

    // @DataMember(Order=4)
    public createdDate?: string;

    // @DataMember(Order=5)
    public completedDate?: string;

    // @DataMember(Order=6)
    public connectionString?: string;

    // @DataMember(Order=7)
    public namedConnection?: string;

    // @DataMember(Order=8)
    public log?: string;

    // @DataMember(Order=9)
    public errorCode?: string;

    // @DataMember(Order=10)
    public errorMessage?: string;

    // @DataMember(Order=11)
    public errorStackTrace?: string;

    // @DataMember(Order=12)
    public meta?: string;

    public constructor(init?: Partial<UpdateMigration>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateMigration'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/options/{Id}", "PUT")
// @DataContract
export class UpdateOption implements IReturn<IdResponse>, IPut, IUpdateDb<Option>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public type?: string;

    // @DataMember(Order=3)
    public names?: string;

    // @DataMember(Order=4)
    public allowQuantity?: number;

    // @DataMember(Order=5)
    public quantityLabel?: string;

    public constructor(init?: Partial<UpdateOption>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateOption'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/optionquantities/{Id}", "PUT")
// @DataContract
export class UpdateOptionQuantity implements IReturn<IdResponse>, IPut, IUpdateDb<OptionQuantity>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public name?: string;

    // @DataMember(Order=3)
    public value: number;

    public constructor(init?: Partial<UpdateOptionQuantity>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateOptionQuantity'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/products/{Id}", "PUT")
// @DataContract
export class UpdateProduct implements IReturn<IdResponse>, IPut, IUpdateDb<Product>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public categoryId: number;

    // @DataMember(Order=3)
    public name?: string;

    // @DataMember(Order=4)
    public cost: number;

    // @DataMember(Order=5)
    public imageUrl?: string;

    public constructor(init?: Partial<UpdateProduct>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateProduct'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

// @Route("/validationrules/{Id}", "PUT")
// @DataContract
export class UpdateValidationRule implements IReturn<IdResponse>, IPut, IUpdateDb<ValidationRule>
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public type?: string;

    // @DataMember(Order=3)
    public field?: string;

    // @DataMember(Order=4)
    public createdBy?: string;

    // @DataMember(Order=5)
    public createdDate?: string;

    // @DataMember(Order=6)
    public modifiedBy?: string;

    // @DataMember(Order=7)
    public modifiedDate?: string;

    // @DataMember(Order=8)
    public suspendedBy?: string;

    // @DataMember(Order=9)
    public suspendedDate?: string;

    // @DataMember(Order=10)
    public notes?: string;

    // @DataMember(Order=11)
    public validator?: string;

    // @DataMember(Order=12)
    public condition?: string;

    // @DataMember(Order=13)
    public errorCode?: string;

    // @DataMember(Order=14)
    public message?: string;

    public constructor(init?: Partial<UpdateValidationRule>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UpdateValidationRule'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new IdResponse(); }
}

