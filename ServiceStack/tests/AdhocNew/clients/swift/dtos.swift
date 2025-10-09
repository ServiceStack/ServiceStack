/* Options:
Date: 2025-10-10 00:26:17
SwiftVersion: 6.0
Version: 8.81
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:5166

//BaseClass: 
//AddModelExtensions: True
//AddServiceStackTypes: True
//MakePropertiesOptional: True
//IncludeTypes: 
//ExcludeTypes: 
//ExcludeGenericBaseTypes: False
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
//InitializeCollections: False
//TreatTypesAsStrings: 
//DefaultImports: Foundation,ServiceStack
*/

import Foundation
import ServiceStack

// @Route("/hello/{Name}")
public class Hello : IReturn, IGet, Codable
{
    public typealias Return = HelloResponse

    public var name:String?

    required public init(){}
}

public class AdminData : IReturn, IGet, Codable
{
    public typealias Return = AdminDataResponse

    required public init(){}
}

/**
* Chat Completions API (OpenAI-Compatible)
*/
// @Route("/v1/chat/completions", "POST")
// @Api(Description="Chat Completions API (OpenAI-Compatible)")
// @DataContract
public class ChatCompletion : IReturn, IPost, Codable
{
    public typealias Return = ChatResponse

    /**
    * The messages to generate chat completions for.
    */
    // @DataMember(Name="messages")
    // @ApiMember(Description="The messages to generate chat completions for.")
    public var messages:[AiMessage] = []

    /**
    * ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API
    */
    // @DataMember(Name="model")
    // @ApiMember(Description="ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API")
    public var model:String?

    /**
    * Parameters for audio output. Required when audio output is requested with modalities: [audio]
    */
    // @DataMember(Name="audio")
    // @ApiMember(Description="Parameters for audio output. Required when audio output is requested with modalities: [audio]")
    public var audio:AiChatAudio?

    /**
    * Modify the likelihood of specified tokens appearing in the completion.
    */
    // @DataMember(Name="logit_bias")
    // @ApiMember(Description="Modify the likelihood of specified tokens appearing in the completion.")
    public var logit_bias:[Int:Int]?

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    // @DataMember(Name="metadata")
    // @ApiMember(Description="Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")
    public var metadata:[String:String]?

    /**
    * Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.
    */
    // @DataMember(Name="reasoning_effort")
    // @ApiMember(Description="Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.")
    public var reasoning_effort:String?

    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.
    */
    // @DataMember(Name="response_format")
    // @ApiMember(Description="An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.")
    public var response_format:AiResponseFormat?

    /**
    * Specifies the processing type used for serving the request.
    */
    // @DataMember(Name="service_tier")
    // @ApiMember(Description="Specifies the processing type used for serving the request.")
    public var service_tier:String?

    /**
    * Up to 4 sequences where the API will stop generating further tokens.
    */
    // @DataMember(Name="stop")
    // @ApiMember(Description="Up to 4 sequences where the API will stop generating further tokens.")
    public var stop:[String]?

    /**
    * Output types that you would like the model to generate. Most models are capable of generating text, which is the default:
    */
    // @DataMember(Name="modalities")
    // @ApiMember(Description="Output types that you would like the model to generate. Most models are capable of generating text, which is the default:")
    public var modalities:[String]?

    /**
    * Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.
    */
    // @DataMember(Name="prompt_cache_key")
    // @ApiMember(Description="Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.")
    public var prompt_cache_key:String?

    /**
    * A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.
    */
    // @DataMember(Name="safety_identifier")
    // @ApiMember(Description="A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.")
    public var safety_identifier:String?

    /**
    * A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.
    */
    // @DataMember(Name="tools")
    // @ApiMember(Description="A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.")
    public var tools:[Tool]?

    /**
    * Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.
    */
    // @DataMember(Name="verbosity")
    // @ApiMember(Description="Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.")
    public var verbosity:String?

    /**
    * What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
    */
    // @DataMember(Name="temperature")
    // @ApiMember(Description="What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.")
    public var temperature:Double?

    /**
    * An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.
    */
    // @DataMember(Name="max_completion_tokens")
    // @ApiMember(Description="An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.")
    public var max_completion_tokens:Int?

    /**
    * An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
    */
    // @DataMember(Name="top_logprobs")
    // @ApiMember(Description="An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.")
    public var top_logprobs:Int?

    /**
    * An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.
    */
    // @DataMember(Name="top_p")
    // @ApiMember(Description="An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.")
    public var top_p:Double?

    /**
    * Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
    */
    // @DataMember(Name="frequency_penalty")
    // @ApiMember(Description="Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.")
    public var frequency_penalty:Double?

    /**
    * Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
    */
    // @DataMember(Name="presence_penalty")
    // @ApiMember(Description="Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.")
    public var presence_penalty:Double?

    /**
    * This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.
    */
    // @DataMember(Name="seed")
    // @ApiMember(Description="This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.")
    public var seed:Int?

    /**
    * How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.
    */
    // @DataMember(Name="n")
    // @ApiMember(Description="How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.")
    public var n:Int?

    /**
    * Whether or not to store the output of this chat completion request for use in our model distillation or evals products.
    */
    // @DataMember(Name="store")
    // @ApiMember(Description="Whether or not to store the output of this chat completion request for use in our model distillation or evals products.")
    public var store:Bool?

    /**
    * Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
    */
    // @DataMember(Name="logprobs")
    // @ApiMember(Description="Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.")
    public var logprobs:Bool?

    /**
    * Whether to enable parallel function calling during tool use.
    */
    // @DataMember(Name="parallel_tool_calls")
    // @ApiMember(Description="Whether to enable parallel function calling during tool use.")
    public var parallel_tool_calls:Bool?

    /**
    * Whether to enable thinking mode for some Qwen models and providers.
    */
    // @DataMember(Name="enable_thinking")
    // @ApiMember(Description="Whether to enable thinking mode for some Qwen models and providers.")
    public var enable_thinking:Bool?

    /**
    * If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.
    */
    // @DataMember(Name="stream")
    // @ApiMember(Description="If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.")
    public var stream:Bool?

    required public init(){}
}

/**
* Sign In
*/
// @Route("/auth", "GET,POST")
// @Route("/auth/{provider}", "POST")
// @Api(Description="Sign In")
// @DataContract
public class Authenticate : IReturn, IPost, Codable
{
    public typealias Return = AuthenticateResponse

    /**
    * AuthProvider, e.g. credentials
    */
    // @DataMember(Order=1)
    public var provider:String?

    // @DataMember(Order=2)
    public var userName:String?

    // @DataMember(Order=3)
    public var password:String?

    // @DataMember(Order=4)
    public var rememberMe:Bool?

    // @DataMember(Order=5)
    public var accessToken:String?

    // @DataMember(Order=6)
    public var accessTokenSecret:String?

    // @DataMember(Order=7)
    public var returnUrl:String?

    // @DataMember(Order=8)
    public var errorView:String?

    // @DataMember(Order=9)
    public var meta:[String:String]?

    required public init(){}
}

/**
* Find Bookings
*/
// @Route("/bookings", "GET")
// @Route("/bookings/{Id}", "GET")
public class QueryBookings : QueryDb<Booking>, IReturn
{
    public typealias Return = QueryResponse<Booking>

    public var id:Int?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case id
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(Int.self, forKey: .id)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if id != nil { try container.encode(id, forKey: .id) }
    }
}

/**
* Find Coupons
*/
// @Route("/coupons", "GET")
public class QueryCoupons : QueryDb<Coupon>, IReturn
{
    public typealias Return = QueryResponse<Coupon>

    public var id:String?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case id
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(String.self, forKey: .id)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if id != nil { try container.encode(id, forKey: .id) }
    }
}

// @ValidateRequest(Validator="IsAdmin")
public class QueryUsers : QueryDb<User>, IReturn
{
    public typealias Return = QueryResponse<User>

    public var id:String?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case id
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(String.self, forKey: .id)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if id != nil { try container.encode(id, forKey: .id) }
    }
}

/**
* Create a new Booking
*/
// @Route("/bookings", "POST")
// @ValidateRequest(Validator="HasRole(`Employee`)")
public class CreateBooking : IReturn, Codable
{
    public typealias Return = IdResponse

    /**
    * Name this Booking is for
    */
    // @Validate(Validator="NotEmpty")
    public var name:String?

    public var roomType:RoomType?
    // @Validate(Validator="GreaterThan(0)")
    public var roomNumber:Int?

    // @Validate(Validator="GreaterThan(0)")
    public var cost:Double?

    // @Required()
    public var bookingStartDate:Date?

    public var bookingEndDate:Date?
    public var notes:String?
    public var couponId:String?

    required public init(){}
}

/**
* Update an existing Booking
*/
// @Route("/booking/{Id}", "PATCH")
// @ValidateRequest(Validator="HasRole(`Employee`)")
public class UpdateBooking : IReturn, Codable
{
    public typealias Return = IdResponse

    public var id:Int?
    public var name:String?
    public var roomType:RoomType?
    // @Validate(Validator="GreaterThan(0)")
    public var roomNumber:Int?

    // @Validate(Validator="GreaterThan(0)")
    public var cost:Double?

    public var bookingStartDate:Date?
    public var bookingEndDate:Date?
    public var notes:String?
    public var couponId:String?
    public var cancelled:Bool?

    required public init(){}
}

/**
* Delete a Booking
*/
// @Route("/booking/{Id}", "DELETE")
// @ValidateRequest(Validator="HasRole(`Manager`)")
public class DeleteBooking : IReturnVoid, Codable
{
    public var id:Int?

    required public init(){}
}

// @Route("/coupons", "POST")
// @ValidateRequest(Validator="HasRole(`Employee`)")
public class CreateCoupon : IReturn, Codable
{
    public typealias Return = IdResponse

    // @Validate(Validator="NotEmpty")
    public var id:String?

    // @Validate(Validator="NotEmpty")
    public var Description:String?

    // @Validate(Validator="GreaterThan(0)")
    public var discount:Int?

    // @Validate(Validator="NotNull")
    public var expiryDate:Date?

    required public init(){}
}

// @Route("/coupons/{Id}", "PATCH")
// @ValidateRequest(Validator="HasRole(`Employee`)")
public class UpdateCoupon : IReturn, Codable
{
    public typealias Return = IdResponse

    public var id:String?
    // @Validate(Validator="NotEmpty")
    public var Description:String?

    // @Validate(Validator="NotNull")
    // @Validate(Validator="GreaterThan(0)")
    public var discount:Int?

    // @Validate(Validator="NotNull")
    public var expiryDate:Date?

    required public init(){}
}

/**
* Delete a Coupon
*/
// @Route("/coupons/{Id}", "DELETE")
// @ValidateRequest(Validator="HasRole(`Manager`)")
public class DeleteCoupon : IReturnVoid, Codable
{
    public var id:String?

    required public init(){}
}

// @DataContract
public class GetAnalyticsInfo : IReturn, IGet, Codable
{
    public typealias Return = GetAnalyticsInfoResponse

    // @DataMember(Order=1)
    public var month:Date?

    // @DataMember(Order=2)
    public var type:String?

    // @DataMember(Order=3)
    public var op:String?

    // @DataMember(Order=4)
    public var apiKey:String?

    // @DataMember(Order=5)
    public var userId:String?

    // @DataMember(Order=6)
    public var ip:String?

    required public init(){}
}

// @DataContract
public class GetAnalyticsReports : IReturn, IGet, Codable
{
    public typealias Return = GetAnalyticsReportsResponse

    // @DataMember(Order=1)
    public var month:Date?

    // @DataMember(Order=2)
    public var filter:String?

    // @DataMember(Order=3)
    public var value:String?

    // @DataMember(Order=4)
    public var force:Bool?

    required public init(){}
}

public class HelloResponse : Codable
{
    public var result:String?

    required public init(){}
}

public class AdminDataResponse : Codable
{
    public var pageStats:[PageStats] = []

    required public init(){}
}

// @DataContract
public class ChatResponse : Codable
{
    /**
    * A unique identifier for the chat completion.
    */
    // @DataMember(Name="id")
    // @ApiMember(Description="A unique identifier for the chat completion.")
    public var id:String?

    /**
    * A list of chat completion choices. Can be more than one if n is greater than 1.
    */
    // @DataMember(Name="choices")
    // @ApiMember(Description="A list of chat completion choices. Can be more than one if n is greater than 1.")
    public var choices:[Choice] = []

    /**
    * The Unix timestamp (in seconds) of when the chat completion was created.
    */
    // @DataMember(Name="created")
    // @ApiMember(Description="The Unix timestamp (in seconds) of when the chat completion was created.")
    public var created:Int?

    /**
    * The model used for the chat completion.
    */
    // @DataMember(Name="model")
    // @ApiMember(Description="The model used for the chat completion.")
    public var model:String?

    /**
    * This fingerprint represents the backend configuration that the model runs with.
    */
    // @DataMember(Name="system_fingerprint")
    // @ApiMember(Description="This fingerprint represents the backend configuration that the model runs with.")
    public var system_fingerprint:String?

    /**
    * The object type, which is always chat.completion.
    */
    // @DataMember(Name="object")
    // @ApiMember(Description="The object type, which is always chat.completion.")
    public var object:String?

    /**
    * Specifies the processing type used for serving the request.
    */
    // @DataMember(Name="service_tier")
    // @ApiMember(Description="Specifies the processing type used for serving the request.")
    public var service_tier:String?

    /**
    * Usage statistics for the completion request.
    */
    // @DataMember(Name="usage")
    // @ApiMember(Description="Usage statistics for the completion request.")
    public var usage:AiUsage?

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    // @DataMember(Name="metadata")
    // @ApiMember(Description="Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")
    public var metadata:[String:String]?

    // @DataMember(Name="responseStatus")
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class AuthenticateResponse : IHasSessionId, IHasBearerToken, Codable
{
    // @DataMember(Order=1)
    public var userId:String?

    // @DataMember(Order=2)
    public var sessionId:String?

    // @DataMember(Order=3)
    public var userName:String?

    // @DataMember(Order=4)
    public var displayName:String?

    // @DataMember(Order=5)
    public var referrerUrl:String?

    // @DataMember(Order=6)
    public var bearerToken:String?

    // @DataMember(Order=7)
    public var refreshToken:String?

    // @DataMember(Order=8)
    public var refreshTokenExpiry:Date?

    // @DataMember(Order=9)
    public var profileUrl:String?

    // @DataMember(Order=10)
    public var roles:[String]?

    // @DataMember(Order=11)
    public var permissions:[String]?

    // @DataMember(Order=12)
    public var authProvider:String?

    // @DataMember(Order=13)
    public var responseStatus:ResponseStatus?

    // @DataMember(Order=14)
    public var meta:[String:String]?

    required public init(){}
}

// @DataContract
public class IdResponse : Codable
{
    // @DataMember(Order=1)
    public var id:String?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class GetAnalyticsInfoResponse : Codable
{
    // @DataMember(Order=1)
    public var months:[String]?

    // @DataMember(Order=2)
    public var result:AnalyticsLogInfo?

    // @DataMember(Order=3)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

// @DataContract
public class GetAnalyticsReportsResponse : Codable
{
    // @DataMember(Order=1)
    public var result:AnalyticsReports?

    // @DataMember(Order=2)
    public var responseStatus:ResponseStatus?

    required public init(){}
}

/**
* A list of messages comprising the conversation so far.
*/
// @Api(Description="A list of messages comprising the conversation so far.")
// @DataContract
public class AiMessage : Codable
{
    /**
    * The contents of the message.
    */
    // @DataMember(Name="content")
    // @ApiMember(Description="The contents of the message.")
    public var content:[AiContent]?

    /**
    * The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.
    */
    // @DataMember(Name="role")
    // @ApiMember(Description="The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.")
    public var role:String?

    /**
    * An optional name for the participant. Provides the model information to differentiate between participants of the same role.
    */
    // @DataMember(Name="name")
    // @ApiMember(Description="An optional name for the participant. Provides the model information to differentiate between participants of the same role.")
    public var name:String?

    /**
    * The tool calls generated by the model, such as function calls.
    */
    // @DataMember(Name="tool_calls")
    // @ApiMember(Description="The tool calls generated by the model, such as function calls.")
    public var tool_calls:[ToolCall]?

    /**
    * Tool call that this message is responding to.
    */
    // @DataMember(Name="tool_call_id")
    // @ApiMember(Description="Tool call that this message is responding to.")
    public var tool_call_id:String?

    required public init(){}
}

/**
* Parameters for audio output. Required when audio output is requested with modalities: [audio]
*/
// @DataContract
public class AiChatAudio : Codable
{
    /**
    * Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.
    */
    // @DataMember(Name="format")
    // @ApiMember(Description="Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.")
    public var format:String?

    /**
    * The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.
    */
    // @DataMember(Name="voice")
    // @ApiMember(Description="The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.")
    public var voice:String?

    required public init(){}
}

// @DataContract
public class AiResponseFormat : Codable
{
    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.
    */
    // @DataMember(Name="response_format")
    // @ApiMember(Description="An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.")
    public var response_format:ResponseFormat?

    required public init(){}
}

// @DataContract
public class Tool : Codable
{
    /**
    * The type of the tool. Currently, only function is supported.
    */
    // @DataMember(Name="type")
    // @ApiMember(Description="The type of the tool. Currently, only function is supported.")
    public var type:ToolType?

    required public init(){}
}

/**
* Booking Details
*/
public class Booking : AuditBase
{
    public var id:Int?
    public var name:String?
    public var roomType:RoomType?
    public var roomNumber:Int?
    public var bookingStartDate:Date?
    public var bookingEndDate:Date?
    public var cost:Double?
    // @References(typeof(Coupon))
    public var couponId:String?

    public var discount:Coupon?
    public var notes:String?
    public var cancelled:Bool?
    public var employee:User?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case id
        case name
        case roomType
        case roomNumber
        case bookingStartDate
        case bookingEndDate
        case cost
        case couponId
        case discount
        case notes
        case cancelled
        case employee
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(Int.self, forKey: .id)
        name = try container.decodeIfPresent(String.self, forKey: .name)
        roomType = try container.decodeIfPresent(RoomType.self, forKey: .roomType)
        roomNumber = try container.decodeIfPresent(Int.self, forKey: .roomNumber)
        bookingStartDate = try container.decodeIfPresent(Date.self, forKey: .bookingStartDate)
        bookingEndDate = try container.decodeIfPresent(Date.self, forKey: .bookingEndDate)
        cost = try container.decodeIfPresent(Double.self, forKey: .cost)
        couponId = try container.decodeIfPresent(String.self, forKey: .couponId)
        discount = try container.decodeIfPresent(Coupon.self, forKey: .discount)
        notes = try container.decodeIfPresent(String.self, forKey: .notes)
        cancelled = try container.decodeIfPresent(Bool.self, forKey: .cancelled)
        employee = try container.decodeIfPresent(User.self, forKey: .employee)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if id != nil { try container.encode(id, forKey: .id) }
        if name != nil { try container.encode(name, forKey: .name) }
        if roomType != nil { try container.encode(roomType, forKey: .roomType) }
        if roomNumber != nil { try container.encode(roomNumber, forKey: .roomNumber) }
        if bookingStartDate != nil { try container.encode(bookingStartDate, forKey: .bookingStartDate) }
        if bookingEndDate != nil { try container.encode(bookingEndDate, forKey: .bookingEndDate) }
        if cost != nil { try container.encode(cost, forKey: .cost) }
        if couponId != nil { try container.encode(couponId, forKey: .couponId) }
        if discount != nil { try container.encode(discount, forKey: .discount) }
        if notes != nil { try container.encode(notes, forKey: .notes) }
        if cancelled != nil { try container.encode(cancelled, forKey: .cancelled) }
        if employee != nil { try container.encode(employee, forKey: .employee) }
    }
}

/**
* Discount Coupons
*/
public class Coupon : Codable
{
    public var id:String?
    public var Description:String?
    public var discount:Int?
    public var expiryDate:Date?

    required public init(){}
}

public class User : Codable
{
    public var id:String?
    public var userName:String?
    public var firstName:String?
    public var lastName:String?
    public var displayName:String?
    public var profileUrl:String?

    required public init(){}
}

public enum RoomType : String, Codable
{
    case Single
    case Double
    case Queen
    case Twin
    case Suite
}

public class PageStats : Codable
{
    public var label:String?
    public var total:Int?

    required public init(){}
}

public class Choice : Codable
{
    /**
    * The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool
    */
    // @DataMember(Name="finish_reason")
    // @ApiMember(Description="The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool")
    public var finish_reason:String?

    /**
    * The index of the choice in the list of choices.
    */
    // @DataMember(Name="index")
    // @ApiMember(Description="The index of the choice in the list of choices.")
    public var index:Int?

    /**
    * A chat completion message generated by the model.
    */
    // @DataMember(Name="message")
    // @ApiMember(Description="A chat completion message generated by the model.")
    public var message:ChoiceMessage?

    required public init(){}
}

/**
* Usage statistics for the completion request.
*/
// @Api(Description="Usage statistics for the completion request.")
// @DataContract
public class AiUsage : Codable
{
    /**
    * Number of tokens in the generated completion.
    */
    // @DataMember(Name="completion_tokens")
    // @ApiMember(Description="Number of tokens in the generated completion.")
    public var completion_tokens:Int?

    /**
    * Number of tokens in the prompt.
    */
    // @DataMember(Name="prompt_tokens")
    // @ApiMember(Description="Number of tokens in the prompt.")
    public var prompt_tokens:Int?

    /**
    * Total number of tokens used in the request (prompt + completion).
    */
    // @DataMember(Name="total_tokens")
    // @ApiMember(Description="Total number of tokens used in the request (prompt + completion).")
    public var total_tokens:Int?

    /**
    * Breakdown of tokens used in a completion.
    */
    // @DataMember(Name="completion_tokens_details")
    // @ApiMember(Description="Breakdown of tokens used in a completion.")
    public var completion_tokens_details:AiCompletionUsage?

    /**
    * Breakdown of tokens used in the prompt.
    */
    // @DataMember(Name="prompt_tokens_details")
    // @ApiMember(Description="Breakdown of tokens used in the prompt.")
    public var prompt_tokens_details:AiPromptUsage?

    required public init(){}
}

// @DataContract
public class AnalyticsLogInfo : Codable
{
    // @DataMember(Order=1)
    public var id:Int?

    // @DataMember(Order=2)
    public var dateTime:Date?

    // @DataMember(Order=3)
    public var browser:String?

    // @DataMember(Order=4)
    public var device:String?

    // @DataMember(Order=5)
    public var bot:String?

    // @DataMember(Order=6)
    public var op:String?

    // @DataMember(Order=7)
    public var userId:String?

    // @DataMember(Order=8)
    public var userName:String?

    // @DataMember(Order=9)
    public var apiKey:String?

    // @DataMember(Order=10)
    public var ip:String?

    required public init(){}
}

// @DataContract
public class AnalyticsReports : Codable
{
    // @DataMember(Order=1)
    public var id:Int?

    // @DataMember(Order=2)
    public var created:Date?

    // @DataMember(Order=3)
    public var version:Double?

    // @DataMember(Order=4)
    public var apis:[String:RequestSummary]?

    // @DataMember(Order=5)
    public var users:[String:RequestSummary]?

    // @DataMember(Order=6)
    public var tags:[String:RequestSummary]?

    // @DataMember(Order=7)
    public var status:[String:RequestSummary]?

    // @DataMember(Order=8)
    public var days:[String:RequestSummary]?

    // @DataMember(Order=9)
    public var apiKeys:[String:RequestSummary]?

    // @DataMember(Order=10)
    public var ips:[String:RequestSummary]?

    // @DataMember(Order=11)
    public var browsers:[String:RequestSummary]?

    // @DataMember(Order=12)
    public var devices:[String:RequestSummary]?

    // @DataMember(Order=13)
    public var bots:[String:RequestSummary]?

    // @DataMember(Order=14)
    public var durations:[String:Int]?

    required public init(){}
}

// @DataContract
public class AiContent : Codable
{
    /**
    * The type of the content part.
    */
    // @DataMember(Name="type")
    // @ApiMember(Description="The type of the content part.")
    public var type:String?

    required public init(){}
}

/**
* The tool calls generated by the model, such as function calls.
*/
// @Api(Description="The tool calls generated by the model, such as function calls.")
// @DataContract
public class ToolCall : Codable
{
    /**
    * The ID of the tool call.
    */
    // @DataMember(Name="id")
    // @ApiMember(Description="The ID of the tool call.")
    public var id:String?

    /**
    * The type of the tool. Currently, only `function` is supported.
    */
    // @DataMember(Name="type")
    // @ApiMember(Description="The type of the tool. Currently, only `function` is supported.")
    public var type:String?

    /**
    * The function that the model called.
    */
    // @DataMember(Name="function")
    // @ApiMember(Description="The function that the model called.")
    public var function:String?

    required public init(){}
}

public enum ResponseFormat : String, Codable
{
    case Text
    case JsonObject
}

public enum ToolType : String, Codable
{
    case Function
}

// @DataContract
public class ChoiceMessage : Codable
{
    /**
    * The contents of the message.
    */
    // @DataMember(Name="content")
    // @ApiMember(Description="The contents of the message.")
    public var content:String?

    /**
    * The refusal message generated by the model.
    */
    // @DataMember(Name="refusal")
    // @ApiMember(Description="The refusal message generated by the model.")
    public var refusal:String?

    /**
    * The reasoning process used by the model.
    */
    // @DataMember(Name="reasoning")
    // @ApiMember(Description="The reasoning process used by the model.")
    public var reasoning:String?

    /**
    * The role of the author of this message.
    */
    // @DataMember(Name="role")
    // @ApiMember(Description="The role of the author of this message.")
    public var role:String?

    /**
    * Annotations for the message, when applicable, as when using the web search tool.
    */
    // @DataMember(Name="annotations")
    // @ApiMember(Description="Annotations for the message, when applicable, as when using the web search tool.")
    public var annotations:[ChoiceAnnotation]?

    /**
    * If the audio output modality is requested, this object contains data about the audio response from the model.
    */
    // @DataMember(Name="audio")
    // @ApiMember(Description="If the audio output modality is requested, this object contains data about the audio response from the model.")
    public var audio:ChoiceAudio?

    /**
    * The tool calls generated by the model, such as function calls.
    */
    // @DataMember(Name="tool_calls")
    // @ApiMember(Description="The tool calls generated by the model, such as function calls.")
    public var tool_calls:[ToolCall]?

    required public init(){}
}

/**
* Usage statistics for the completion request.
*/
// @Api(Description="Usage statistics for the completion request.")
// @DataContract
public class AiCompletionUsage : Codable
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    // @DataMember(Name="accepted_prediction_tokens")
    // @ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.\n\n")
    public var accepted_prediction_tokens:Int?

    /**
    * Audio input tokens generated by the model.
    */
    // @DataMember(Name="audio_tokens")
    // @ApiMember(Description="Audio input tokens generated by the model.")
    public var audio_tokens:Int?

    /**
    * Tokens generated by the model for reasoning.
    */
    // @DataMember(Name="reasoning_tokens")
    // @ApiMember(Description="Tokens generated by the model for reasoning.")
    public var reasoning_tokens:Int?

    /**
    * When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.
    */
    // @DataMember(Name="rejected_prediction_tokens")
    // @ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.")
    public var rejected_prediction_tokens:Int?

    required public init(){}
}

/**
* Breakdown of tokens used in the prompt.
*/
// @Api(Description="Breakdown of tokens used in the prompt.")
// @DataContract
public class AiPromptUsage : Codable
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    // @DataMember(Name="accepted_prediction_tokens")
    // @ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.\n\n")
    public var accepted_prediction_tokens:Int?

    /**
    * Audio input tokens present in the prompt.
    */
    // @DataMember(Name="audio_tokens")
    // @ApiMember(Description="Audio input tokens present in the prompt.")
    public var audio_tokens:Int?

    /**
    * Cached tokens present in the prompt.
    */
    // @DataMember(Name="cached_tokens")
    // @ApiMember(Description="Cached tokens present in the prompt.")
    public var cached_tokens:Int?

    required public init(){}
}

// @DataContract
public class RequestSummary : Codable
{
    // @DataMember(Order=1)
    public var name:String?

    // @DataMember(Order=2)
    public var totalRequests:Int?

    // @DataMember(Order=3)
    public var totalRequestLength:Int?

    // @DataMember(Order=4)
    public var minRequestLength:Int?

    // @DataMember(Order=5)
    public var maxRequestLength:Int?

    // @DataMember(Order=6)
    public var totalDuration:Double?

    // @DataMember(Order=7)
    public var minDuration:Double?

    // @DataMember(Order=8)
    public var maxDuration:Double?

    // @DataMember(Order=9)
    public var status:[Int:Int]?

    // @DataMember(Order=10)
    public var durations:[String:Int]?

    // @DataMember(Order=11)
    public var apis:[String:Int]?

    // @DataMember(Order=12)
    public var users:[String:Int]?

    // @DataMember(Order=13)
    public var ips:[String:Int]?

    // @DataMember(Order=14)
    public var apiKeys:[String:Int]?

    required public init(){}
}

/**
* Text content part
*/
// @DataContract
public class AiTextContent : AiContent
{
    /**
    * The text content.
    */
    // @DataMember(Name="text")
    // @ApiMember(Description="The text content.")
    public var text:String?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case text
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        text = try container.decodeIfPresent(String.self, forKey: .text)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if text != nil { try container.encode(text, forKey: .text) }
    }
}

/**
* Image content part
*/
// @DataContract
public class AiImageContent : AiContent
{
    /**
    * The image for this content.
    */
    // @DataMember(Name="image_url")
    // @ApiMember(Description="The image for this content.")
    public var image_url:AiImageUrl?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case image_url
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        image_url = try container.decodeIfPresent(AiImageUrl.self, forKey: .image_url)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if image_url != nil { try container.encode(image_url, forKey: .image_url) }
    }
}

/**
* Audio content part
*/
// @DataContract
public class AiAudioContent : AiContent
{
    /**
    * The audio input for this content.
    */
    // @DataMember(Name="input_audio")
    // @ApiMember(Description="The audio input for this content.")
    public var input_audio:AiInputAudio?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case input_audio
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        input_audio = try container.decodeIfPresent(AiInputAudio.self, forKey: .input_audio)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if input_audio != nil { try container.encode(input_audio, forKey: .input_audio) }
    }
}

/**
* File content part
*/
// @DataContract
public class AiFileContent : AiContent
{
    /**
    * The file input for this content.
    */
    // @DataMember(Name="file")
    // @ApiMember(Description="The file input for this content.")
    public var file:AiFile?

    required public init(){ super.init() }

    private enum CodingKeys : String, CodingKey {
        case file
    }

    required public init(from decoder: Decoder) throws {
        try super.init(from: decoder)
        let container = try decoder.container(keyedBy: CodingKeys.self)
        file = try container.decodeIfPresent(AiFile.self, forKey: .file)
    }

    public override func encode(to encoder: Encoder) throws {
        try super.encode(to: encoder)
        var container = encoder.container(keyedBy: CodingKeys.self)
        if file != nil { try container.encode(file, forKey: .file) }
    }
}

/**
* Annotations for the message, when applicable, as when using the web search tool.
*/
// @DataContract
public class ChoiceAnnotation : Codable
{
    /**
    * The type of the URL citation. Always url_citation.
    */
    // @DataMember(Name="type")
    // @ApiMember(Description="The type of the URL citation. Always url_citation.")
    public var type:String?

    /**
    * A URL citation when using web search.
    */
    // @DataMember(Name="url_citation")
    // @ApiMember(Description="A URL citation when using web search.")
    public var url_citation:UrlCitation?

    required public init(){}
}

/**
* If the audio output modality is requested, this object contains data about the audio response from the model.
*/
// @DataContract
public class ChoiceAudio : Codable
{
    /**
    * Base64 encoded audio bytes generated by the model, in the format specified in the request.
    */
    // @DataMember(Name="data")
    // @ApiMember(Description="Base64 encoded audio bytes generated by the model, in the format specified in the request.")
    public var data:String?

    /**
    * The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.
    */
    // @DataMember(Name="expires_at")
    // @ApiMember(Description="The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.")
    public var expires_at:Int?

    /**
    * Unique identifier for this audio response.
    */
    // @DataMember(Name="id")
    // @ApiMember(Description="Unique identifier for this audio response.")
    public var id:String?

    /**
    * Transcript of the audio generated by the model.
    */
    // @DataMember(Name="transcript")
    // @ApiMember(Description="Transcript of the audio generated by the model.")
    public var transcript:String?

    required public init(){}
}

public class AiImageUrl : Codable
{
    /**
    * Either a URL of the image or the base64 encoded image data.
    */
    // @DataMember(Name="url")
    // @ApiMember(Description="Either a URL of the image or the base64 encoded image data.")
    public var url:String?

    required public init(){}
}

/**
* Audio content part
*/
// @DataContract
public class AiInputAudio : Codable
{
    /**
    * URL or Base64 encoded audio data.
    */
    // @DataMember(Name="data")
    // @ApiMember(Description="URL or Base64 encoded audio data.")
    public var data:String?

    /**
    * The format of the encoded audio data. Currently supports 'wav' and 'mp3'.
    */
    // @DataMember(Name="format")
    // @ApiMember(Description="The format of the encoded audio data. Currently supports 'wav' and 'mp3'.")
    public var format:String?

    required public init(){}
}

/**
* File content part
*/
// @DataContract
public class AiFile : Codable
{
    /**
    * The URL or base64 encoded file data, used when passing the file to the model as a string.
    */
    // @DataMember(Name="file_data")
    // @ApiMember(Description="The URL or base64 encoded file data, used when passing the file to the model as a string.")
    public var file_data:String?

    /**
    * The name of the file, used when passing the file to the model as a string.
    */
    // @DataMember(Name="filename")
    // @ApiMember(Description="The name of the file, used when passing the file to the model as a string.")
    public var filename:String?

    /**
    * The ID of an uploaded file to use as input.
    */
    // @DataMember(Name="file_id")
    // @ApiMember(Description="The ID of an uploaded file to use as input.")
    public var file_id:String?

    required public init(){}
}

/**
* Annotations for the message, when applicable, as when using the web search tool.
*/
// @DataContract
public class UrlCitation : Codable
{
    /**
    * The index of the last character of the URL citation in the message.
    */
    // @DataMember(Name="end_index")
    // @ApiMember(Description="The index of the last character of the URL citation in the message.")
    public var end_index:Int?

    /**
    * The index of the first character of the URL citation in the message.
    */
    // @DataMember(Name="start_index")
    // @ApiMember(Description="The index of the first character of the URL citation in the message.")
    public var start_index:Int?

    /**
    * The title of the web resource.
    */
    // @DataMember(Name="title")
    // @ApiMember(Description="The title of the web resource.")
    public var title:String?

    /**
    * The URL of the web resource.
    */
    // @DataMember(Name="url")
    // @ApiMember(Description="The URL of the web resource.")
    public var url:String?

    required public init(){}
}


