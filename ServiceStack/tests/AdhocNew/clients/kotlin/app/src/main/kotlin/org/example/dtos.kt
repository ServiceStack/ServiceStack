/* Options:
Date: 2025-10-10 00:02:58
Version: 8.81
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:5166

//Package:
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion:
//AddDescriptionAsComments: True
//IncludeTypes:
//ExcludeTypes:
//InitializeCollections: False
//TreatTypesAsStrings:
//DefaultImports: java.math.*,java.util.*,java.io.InputStream,net.servicestack.client.*,com.google.gson.annotations.*,com.google.gson.reflect.*
*/

package org.example

import java.math.*
import java.util.*
import java.io.InputStream
import net.servicestack.client.*
import com.google.gson.annotations.*
import com.google.gson.reflect.*


@Route(Path="/hello/{Name}")
open class Hello : IReturn<HelloResponse>, IGet
{
    open var name:String? = null
    companion object { private val responseType = HelloResponse::class.java }
    override fun getResponseType(): Any? = Hello.responseType
}

open class AdminData : IReturn<AdminDataResponse>, IGet
{
    companion object { private val responseType = AdminDataResponse::class.java }
    override fun getResponseType(): Any? = AdminData.responseType
}

/**
* Chat Completions API (OpenAI-Compatible)
*/
@Route(Path="/v1/chat/completions", Verbs="POST")
@Api(Description="Chat Completions API (OpenAI-Compatible)")
@DataContract
open class ChatCompletion : IReturn<ChatResponse>, IPost
{
    /**
    * The messages to generate chat completions for.
    */
    @DataMember(Name="messages")
    @SerializedName("messages")
    @ApiMember(Description="The messages to generate chat completions for.")
    open var messages:ArrayList<AiMessage> = ArrayList<AiMessage>()

    /**
    * ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API
    */
    @DataMember(Name="model")
    @SerializedName("model")
    @ApiMember(Description="ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API")
    open var model:String? = null

    /**
    * Parameters for audio output. Required when audio output is requested with modalities: [audio]
    */
    @DataMember(Name="audio")
    @SerializedName("audio")
    @ApiMember(Description="Parameters for audio output. Required when audio output is requested with modalities: [audio]")
    open var audio:AiChatAudio? = null

    /**
    * Modify the likelihood of specified tokens appearing in the completion.
    */
    @DataMember(Name="logit_bias")
    @SerializedName("logit_bias")
    @ApiMember(Description="Modify the likelihood of specified tokens appearing in the completion.")
    open var logitBias:HashMap<Int,Int>? = null

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    @DataMember(Name="metadata")
    @SerializedName("metadata")
    @ApiMember(Description="Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")
    open var metadata:HashMap<String,String>? = null

    /**
    * Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.
    */
    @DataMember(Name="reasoning_effort")
    @SerializedName("reasoning_effort")
    @ApiMember(Description="Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.")
    open var reasoningEffort:String? = null

    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.
    */
    @DataMember(Name="response_format")
    @SerializedName("response_format")
    @ApiMember(Description="An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.")
    open var responseFormat:AiResponseFormat? = null

    /**
    * Specifies the processing type used for serving the request.
    */
    @DataMember(Name="service_tier")
    @SerializedName("service_tier")
    @ApiMember(Description="Specifies the processing type used for serving the request.")
    open var serviceTier:String? = null

    /**
    * Up to 4 sequences where the API will stop generating further tokens.
    */
    @DataMember(Name="stop")
    @SerializedName("stop")
    @ApiMember(Description="Up to 4 sequences where the API will stop generating further tokens.")
    open var stop:ArrayList<String>? = null

    /**
    * Output types that you would like the model to generate. Most models are capable of generating text, which is the default:
    */
    @DataMember(Name="modalities")
    @SerializedName("modalities")
    @ApiMember(Description="Output types that you would like the model to generate. Most models are capable of generating text, which is the default:")
    open var modalities:ArrayList<String>? = null

    /**
    * Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.
    */
    @DataMember(Name="prompt_cache_key")
    @SerializedName("prompt_cache_key")
    @ApiMember(Description="Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.")
    open var promptCacheKey:String? = null

    /**
    * A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.
    */
    @DataMember(Name="safety_identifier")
    @SerializedName("safety_identifier")
    @ApiMember(Description="A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.")
    open var safetyIdentifier:String? = null

    /**
    * A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.
    */
    @DataMember(Name="tools")
    @SerializedName("tools")
    @ApiMember(Description="A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.")
    open var tools:ArrayList<Tool>? = null

    /**
    * Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.
    */
    @DataMember(Name="verbosity")
    @SerializedName("verbosity")
    @ApiMember(Description="Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.")
    open var verbosity:String? = null

    /**
    * What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
    */
    @DataMember(Name="temperature")
    @SerializedName("temperature")
    @ApiMember(Description="What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.")
    open var temperature:Double? = null

    /**
    * An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.
    */
    @DataMember(Name="max_completion_tokens")
    @SerializedName("max_completion_tokens")
    @ApiMember(Description="An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.")
    open var maxCompletionTokens:Int? = null

    /**
    * An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
    */
    @DataMember(Name="top_logprobs")
    @SerializedName("top_logprobs")
    @ApiMember(Description="An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.")
    open var topLogprobs:Int? = null

    /**
    * An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.
    */
    @DataMember(Name="top_p")
    @SerializedName("top_p")
    @ApiMember(Description="An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.")
    open var topP:Double? = null

    /**
    * Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
    */
    @DataMember(Name="frequency_penalty")
    @SerializedName("frequency_penalty")
    @ApiMember(Description="Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.")
    open var frequencyPenalty:Double? = null

    /**
    * Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
    */
    @DataMember(Name="presence_penalty")
    @SerializedName("presence_penalty")
    @ApiMember(Description="Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.")
    open var presencePenalty:Double? = null

    /**
    * This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.
    */
    @DataMember(Name="seed")
    @SerializedName("seed")
    @ApiMember(Description="This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.")
    open var seed:Int? = null

    /**
    * How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.
    */
    @DataMember(Name="n")
    @SerializedName("n")
    @ApiMember(Description="How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.")
    open var n:Int? = null

    /**
    * Whether or not to store the output of this chat completion request for use in our model distillation or evals products.
    */
    @DataMember(Name="store")
    @SerializedName("store")
    @ApiMember(Description="Whether or not to store the output of this chat completion request for use in our model distillation or evals products.")
    open var store:Boolean? = null

    /**
    * Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
    */
    @DataMember(Name="logprobs")
    @SerializedName("logprobs")
    @ApiMember(Description="Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.")
    open var logprobs:Boolean? = null

    /**
    * Whether to enable parallel function calling during tool use.
    */
    @DataMember(Name="parallel_tool_calls")
    @SerializedName("parallel_tool_calls")
    @ApiMember(Description="Whether to enable parallel function calling during tool use.")
    open var parallelToolCalls:Boolean? = null

    /**
    * Whether to enable thinking mode for some Qwen models and providers.
    */
    @DataMember(Name="enable_thinking")
    @SerializedName("enable_thinking")
    @ApiMember(Description="Whether to enable thinking mode for some Qwen models and providers.")
    open var enableThinking:Boolean? = null

    /**
    * If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.
    */
    @DataMember(Name="stream")
    @SerializedName("stream")
    @ApiMember(Description="If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.")
    open var stream:Boolean? = null
    companion object { private val responseType = ChatResponse::class.java }
    override fun getResponseType(): Any? = ChatCompletion.responseType
}

/**
* Sign In
*/
@Route(Path="/auth", Verbs="GET,POST")
// @Route(Path="/auth/{provider}", Verbs="POST")
@Api(Description="Sign In")
@DataContract
open class Authenticate : IReturn<AuthenticateResponse>, IPost
{
    /**
    * AuthProvider, e.g. credentials
    */
    @DataMember(Order=1)
    open var provider:String? = null

    @DataMember(Order=2)
    open var userName:String? = null

    @DataMember(Order=3)
    open var password:String? = null

    @DataMember(Order=4)
    open var rememberMe:Boolean? = null

    @DataMember(Order=5)
    open var accessToken:String? = null

    @DataMember(Order=6)
    open var accessTokenSecret:String? = null

    @DataMember(Order=7)
    open var returnUrl:String? = null

    @DataMember(Order=8)
    open var errorView:String? = null

    @DataMember(Order=9)
    open var meta:HashMap<String,String>? = null
    companion object { private val responseType = AuthenticateResponse::class.java }
    override fun getResponseType(): Any? = Authenticate.responseType
}

/**
* Find Bookings
*/
@Route(Path="/bookings", Verbs="GET")
// @Route(Path="/bookings/{Id}", Verbs="GET")
open class QueryBookings : QueryDb<Booking>(), IReturn<QueryResponse<Booking>>
{
    open var id:Int? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<Booking>>(){}.type }
    override fun getResponseType(): Any? = QueryBookings.responseType
}

/**
* Find Coupons
*/
@Route(Path="/coupons", Verbs="GET")
open class QueryCoupons : QueryDb<Coupon>(), IReturn<QueryResponse<Coupon>>
{
    open var id:String? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<Coupon>>(){}.type }
    override fun getResponseType(): Any? = QueryCoupons.responseType
}

@ValidateRequest(Validator="IsAdmin")
open class QueryUsers : QueryDb<User>(), IReturn<QueryResponse<User>>
{
    open var id:String? = null
    companion object { private val responseType = object : TypeToken<QueryResponse<User>>(){}.type }
    override fun getResponseType(): Any? = QueryUsers.responseType
}

/**
* Create a new Booking
*/
@Route(Path="/bookings", Verbs="POST")
@ValidateRequest(Validator="HasRole(`Employee`)")
open class CreateBooking : IReturn<IdResponse>, ICreateDb<Booking>
{
    /**
    * Name this Booking is for
    */
    @Validate(Validator="NotEmpty")
    open var name:String? = null

    open var roomType:RoomType? = null
    @Validate(Validator="GreaterThan(0)")
    open var roomNumber:Int? = null

    @Validate(Validator="GreaterThan(0)")
    open var cost:BigDecimal? = null

    @Required()
    open var bookingStartDate:Date? = null

    open var bookingEndDate:Date? = null
    open var notes:String? = null
    open var couponId:String? = null
    companion object { private val responseType = IdResponse::class.java }
    override fun getResponseType(): Any? = CreateBooking.responseType
}

/**
* Update an existing Booking
*/
@Route(Path="/booking/{Id}", Verbs="PATCH")
@ValidateRequest(Validator="HasRole(`Employee`)")
open class UpdateBooking : IReturn<IdResponse>, IPatchDb<Booking>
{
    open var id:Int? = null
    open var name:String? = null
    open var roomType:RoomType? = null
    @Validate(Validator="GreaterThan(0)")
    open var roomNumber:Int? = null

    @Validate(Validator="GreaterThan(0)")
    open var cost:BigDecimal? = null

    open var bookingStartDate:Date? = null
    open var bookingEndDate:Date? = null
    open var notes:String? = null
    open var couponId:String? = null
    open var cancelled:Boolean? = null
    companion object { private val responseType = IdResponse::class.java }
    override fun getResponseType(): Any? = UpdateBooking.responseType
}

/**
* Delete a Booking
*/
@Route(Path="/booking/{Id}", Verbs="DELETE")
@ValidateRequest(Validator="HasRole(`Manager`)")
open class DeleteBooking : IReturnVoid, IDeleteDb<Booking>
{
    open var id:Int? = null
}

@Route(Path="/coupons", Verbs="POST")
@ValidateRequest(Validator="HasRole(`Employee`)")
open class CreateCoupon : IReturn<IdResponse>, ICreateDb<Coupon>
{
    @Validate(Validator="NotEmpty")
    open var id:String? = null

    @Validate(Validator="NotEmpty")
    open var description:String? = null

    @Validate(Validator="GreaterThan(0)")
    open var discount:Int? = null

    @Validate(Validator="NotNull")
    open var expiryDate:Date? = null
    companion object { private val responseType = IdResponse::class.java }
    override fun getResponseType(): Any? = CreateCoupon.responseType
}

@Route(Path="/coupons/{Id}", Verbs="PATCH")
@ValidateRequest(Validator="HasRole(`Employee`)")
open class UpdateCoupon : IReturn<IdResponse>, IPatchDb<Coupon>
{
    open var id:String? = null
    @Validate(Validator="NotEmpty")
    open var description:String? = null

    @Validate(Validator="NotNull")
    // @Validate(Validator="GreaterThan(0)")
    open var discount:Int? = null

    @Validate(Validator="NotNull")
    open var expiryDate:Date? = null
    companion object { private val responseType = IdResponse::class.java }
    override fun getResponseType(): Any? = UpdateCoupon.responseType
}

/**
* Delete a Coupon
*/
@Route(Path="/coupons/{Id}", Verbs="DELETE")
@ValidateRequest(Validator="HasRole(`Manager`)")
open class DeleteCoupon : IReturnVoid, IDeleteDb<Coupon>
{
    open var id:String? = null
}

@DataContract
open class GetAnalyticsInfo : IReturn<GetAnalyticsInfoResponse>, IGet
{
    @DataMember(Order=1)
    open var month:Date? = null

    @DataMember(Order=2)
    @SerializedName("type") open var Type:String? = null

    @DataMember(Order=3)
    open var op:String? = null

    @DataMember(Order=4)
    open var apiKey:String? = null

    @DataMember(Order=5)
    open var userId:String? = null

    @DataMember(Order=6)
    open var ip:String? = null
    companion object { private val responseType = GetAnalyticsInfoResponse::class.java }
    override fun getResponseType(): Any? = GetAnalyticsInfo.responseType
}

@DataContract
open class GetAnalyticsReports : IReturn<GetAnalyticsReportsResponse>, IGet
{
    @DataMember(Order=1)
    open var month:Date? = null

    @DataMember(Order=2)
    open var filter:String? = null

    @DataMember(Order=3)
    open var value:String? = null

    @DataMember(Order=4)
    open var force:Boolean? = null
    companion object { private val responseType = GetAnalyticsReportsResponse::class.java }
    override fun getResponseType(): Any? = GetAnalyticsReports.responseType
}

open class HelloResponse
{
    open var result:String? = null
}

open class AdminDataResponse
{
    open var pageStats:ArrayList<PageStats> = ArrayList<PageStats>()
}

@DataContract
open class ChatResponse
{
    /**
    * A unique identifier for the chat completion.
    */
    @DataMember(Name="id")
    @SerializedName("id")
    @ApiMember(Description="A unique identifier for the chat completion.")
    open var id:String? = null

    /**
    * A list of chat completion choices. Can be more than one if n is greater than 1.
    */
    @DataMember(Name="choices")
    @SerializedName("choices")
    @ApiMember(Description="A list of chat completion choices. Can be more than one if n is greater than 1.")
    open var choices:ArrayList<Choice> = ArrayList<Choice>()

    /**
    * The Unix timestamp (in seconds) of when the chat completion was created.
    */
    @DataMember(Name="created")
    @SerializedName("created")
    @ApiMember(Description="The Unix timestamp (in seconds) of when the chat completion was created.")
    open var created:Long? = null

    /**
    * The model used for the chat completion.
    */
    @DataMember(Name="model")
    @SerializedName("model")
    @ApiMember(Description="The model used for the chat completion.")
    open var model:String? = null

    /**
    * This fingerprint represents the backend configuration that the model runs with.
    */
    @DataMember(Name="system_fingerprint")
    @SerializedName("system_fingerprint")
    @ApiMember(Description="This fingerprint represents the backend configuration that the model runs with.")
    open var systemFingerprint:String? = null

    /**
    * The object type, which is always chat.completion.
    */
    @DataMember(Name="object")
    @SerializedName("object")
    @ApiMember(Description="The object type, which is always chat.completion.")
    open var Object:String? = null

    /**
    * Specifies the processing type used for serving the request.
    */
    @DataMember(Name="service_tier")
    @SerializedName("service_tier")
    @ApiMember(Description="Specifies the processing type used for serving the request.")
    open var serviceTier:String? = null

    /**
    * Usage statistics for the completion request.
    */
    @DataMember(Name="usage")
    @SerializedName("usage")
    @ApiMember(Description="Usage statistics for the completion request.")
    open var usage:AiUsage? = null

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    @DataMember(Name="metadata")
    @SerializedName("metadata")
    @ApiMember(Description="Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")
    open var metadata:HashMap<String,String>? = null

    @DataMember(Name="responseStatus")
    @SerializedName("responseStatus")
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class AuthenticateResponse : IHasSessionId, IHasBearerToken
{
    @DataMember(Order=1)
    open var userId:String? = null

    @DataMember(Order=2)
    open var sessionId:String? = null

    @DataMember(Order=3)
    open var userName:String? = null

    @DataMember(Order=4)
    open var displayName:String? = null

    @DataMember(Order=5)
    open var referrerUrl:String? = null

    @DataMember(Order=6)
    open var bearerToken:String? = null

    @DataMember(Order=7)
    open var refreshToken:String? = null

    @DataMember(Order=8)
    open var refreshTokenExpiry:Date? = null

    @DataMember(Order=9)
    open var profileUrl:String? = null

    @DataMember(Order=10)
    open var roles:ArrayList<String>? = null

    @DataMember(Order=11)
    open var permissions:ArrayList<String>? = null

    @DataMember(Order=12)
    open var authProvider:String? = null

    @DataMember(Order=13)
    open var responseStatus:ResponseStatus? = null

    @DataMember(Order=14)
    open var meta:HashMap<String,String>? = null
}

@DataContract
open class QueryResponse<T>
{
    @DataMember(Order=1)
    open var offset:Int? = null

    @DataMember(Order=2)
    open var total:Int? = null

    @DataMember(Order=3)
    open var results:ArrayList<Booking> = ArrayList<Booking>()

    @DataMember(Order=4)
    open var meta:HashMap<String,String>? = null

    @DataMember(Order=5)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class IdResponse
{
    @DataMember(Order=1)
    open var id:String? = null

    @DataMember(Order=2)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class GetAnalyticsInfoResponse
{
    @DataMember(Order=1)
    open var months:ArrayList<String>? = null

    @DataMember(Order=2)
    open var result:AnalyticsLogInfo? = null

    @DataMember(Order=3)
    open var responseStatus:ResponseStatus? = null
}

@DataContract
open class GetAnalyticsReportsResponse
{
    @DataMember(Order=1)
    open var result:AnalyticsReports? = null

    @DataMember(Order=2)
    open var responseStatus:ResponseStatus? = null
}

/**
* A list of messages comprising the conversation so far.
*/
@Api(Description="A list of messages comprising the conversation so far.")
@DataContract
open class AiMessage
{
    /**
    * The contents of the message.
    */
    @DataMember(Name="content")
    @SerializedName("content")
    @ApiMember(Description="The contents of the message.")
    open var content:ArrayList<AiContent>? = null

    /**
    * The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.
    */
    @DataMember(Name="role")
    @SerializedName("role")
    @ApiMember(Description="The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.")
    open var role:String? = null

    /**
    * An optional name for the participant. Provides the model information to differentiate between participants of the same role.
    */
    @DataMember(Name="name")
    @SerializedName("name")
    @ApiMember(Description="An optional name for the participant. Provides the model information to differentiate between participants of the same role.")
    open var name:String? = null

    /**
    * The tool calls generated by the model, such as function calls.
    */
    @DataMember(Name="tool_calls")
    @SerializedName("tool_calls")
    @ApiMember(Description="The tool calls generated by the model, such as function calls.")
    open var toolCalls:ArrayList<ToolCall>? = null

    /**
    * Tool call that this message is responding to.
    */
    @DataMember(Name="tool_call_id")
    @SerializedName("tool_call_id")
    @ApiMember(Description="Tool call that this message is responding to.")
    open var toolCallId:String? = null
}

/**
* Parameters for audio output. Required when audio output is requested with modalities: [audio]
*/
@DataContract
open class AiChatAudio
{
    /**
    * Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.
    */
    @DataMember(Name="format")
    @SerializedName("format")
    @ApiMember(Description="Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.")
    open var format:String? = null

    /**
    * The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.
    */
    @DataMember(Name="voice")
    @SerializedName("voice")
    @ApiMember(Description="The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.")
    open var voice:String? = null
}

@DataContract
open class AiResponseFormat
{
    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.
    */
    @DataMember(Name="response_format")
    @SerializedName("response_format")
    @ApiMember(Description="An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.")
    open var Type:ResponseFormat? = null
}

@DataContract
open class Tool
{
    /**
    * The type of the tool. Currently, only function is supported.
    */
    @DataMember(Name="type")
    @SerializedName("type")
    @ApiMember(Description="The type of the tool. Currently, only function is supported.")
    open var Type:ToolType? = null
}

open class QueryDb<T> : QueryBase()
{
}

/**
* Booking Details
*/
open class Booking : AuditBase()
{
    open var id:Int? = null
    open var name:String? = null
    open var roomType:RoomType? = null
    open var roomNumber:Int? = null
    open var bookingStartDate:Date? = null
    open var bookingEndDate:Date? = null
    open var cost:BigDecimal? = null
    @References(Type=Coupon::class)
    open var couponId:String? = null

    open var discount:Coupon? = null
    open var notes:String? = null
    open var cancelled:Boolean? = null
    open var employee:User? = null
}

/**
* Discount Coupons
*/
open class Coupon
{
    open var id:String? = null
    open var description:String? = null
    open var discount:Int? = null
    open var expiryDate:Date? = null
}

open class User
{
    open var id:String? = null
    open var userName:String? = null
    open var firstName:String? = null
    open var lastName:String? = null
    open var displayName:String? = null
    open var profileUrl:String? = null
}

enum class RoomType
{
    Single,
    Double,
    Queen,
    Twin,
    Suite,
}

open class PageStats
{
    open var label:String? = null
    open var total:Int? = null
}

open class Choice
{
    /**
    * The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool
    */
    @DataMember(Name="finish_reason")
    @SerializedName("finish_reason")
    @ApiMember(Description="The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool")
    open var finishReason:String? = null

    /**
    * The index of the choice in the list of choices.
    */
    @DataMember(Name="index")
    @SerializedName("index")
    @ApiMember(Description="The index of the choice in the list of choices.")
    open var index:Int? = null

    /**
    * A chat completion message generated by the model.
    */
    @DataMember(Name="message")
    @SerializedName("message")
    @ApiMember(Description="A chat completion message generated by the model.")
    open var message:ChoiceMessage? = null
}

/**
* Usage statistics for the completion request.
*/
@Api(Description="Usage statistics for the completion request.")
@DataContract
open class AiUsage
{
    /**
    * Number of tokens in the generated completion.
    */
    @DataMember(Name="completion_tokens")
    @SerializedName("completion_tokens")
    @ApiMember(Description="Number of tokens in the generated completion.")
    open var completionTokens:Int? = null

    /**
    * Number of tokens in the prompt.
    */
    @DataMember(Name="prompt_tokens")
    @SerializedName("prompt_tokens")
    @ApiMember(Description="Number of tokens in the prompt.")
    open var promptTokens:Int? = null

    /**
    * Total number of tokens used in the request (prompt + completion).
    */
    @DataMember(Name="total_tokens")
    @SerializedName("total_tokens")
    @ApiMember(Description="Total number of tokens used in the request (prompt + completion).")
    open var totalTokens:Int? = null

    /**
    * Breakdown of tokens used in a completion.
    */
    @DataMember(Name="completion_tokens_details")
    @SerializedName("completion_tokens_details")
    @ApiMember(Description="Breakdown of tokens used in a completion.")
    open var completionTokensDetails:AiCompletionUsage? = null

    /**
    * Breakdown of tokens used in the prompt.
    */
    @DataMember(Name="prompt_tokens_details")
    @SerializedName("prompt_tokens_details")
    @ApiMember(Description="Breakdown of tokens used in the prompt.")
    open var promptTokensDetails:AiPromptUsage? = null
}

@DataContract
open class AnalyticsLogInfo
{
    @DataMember(Order=1)
    open var id:Long? = null

    @DataMember(Order=2)
    open var dateTime:Date? = null

    @DataMember(Order=3)
    open var browser:String? = null

    @DataMember(Order=4)
    open var device:String? = null

    @DataMember(Order=5)
    open var bot:String? = null

    @DataMember(Order=6)
    open var op:String? = null

    @DataMember(Order=7)
    open var userId:String? = null

    @DataMember(Order=8)
    open var userName:String? = null

    @DataMember(Order=9)
    open var apiKey:String? = null

    @DataMember(Order=10)
    open var ip:String? = null
}

@DataContract
open class AnalyticsReports
{
    @DataMember(Order=1)
    open var id:Long? = null

    @DataMember(Order=2)
    open var created:Date? = null

    @DataMember(Order=3)
    open var version:BigDecimal? = null

    @DataMember(Order=4)
    open var apis:HashMap<String,RequestSummary>? = null

    @DataMember(Order=5)
    open var users:HashMap<String,RequestSummary>? = null

    @DataMember(Order=6)
    open var tags:HashMap<String,RequestSummary>? = null

    @DataMember(Order=7)
    open var status:HashMap<String,RequestSummary>? = null

    @DataMember(Order=8)
    open var days:HashMap<String,RequestSummary>? = null

    @DataMember(Order=9)
    open var apiKeys:HashMap<String,RequestSummary>? = null

    @DataMember(Order=10)
    open var ips:HashMap<String,RequestSummary>? = null

    @DataMember(Order=11)
    open var browsers:HashMap<String,RequestSummary>? = null

    @DataMember(Order=12)
    open var devices:HashMap<String,RequestSummary>? = null

    @DataMember(Order=13)
    open var bots:HashMap<String,RequestSummary>? = null

    @DataMember(Order=14)
    open var durations:HashMap<String,Long>? = null
}

@DataContract
open class AiContent
{
    /**
    * The type of the content part.
    */
    @DataMember(Name="type")
    @SerializedName("type")
    @ApiMember(Description="The type of the content part.")
    open var Type:String? = null
}

/**
* The tool calls generated by the model, such as function calls.
*/
@Api(Description="The tool calls generated by the model, such as function calls.")
@DataContract
open class ToolCall
{
    /**
    * The ID of the tool call.
    */
    @DataMember(Name="id")
    @SerializedName("id")
    @ApiMember(Description="The ID of the tool call.")
    open var id:String? = null

    /**
    * The type of the tool. Currently, only `function` is supported.
    */
    @DataMember(Name="type")
    @SerializedName("type")
    @ApiMember(Description="The type of the tool. Currently, only `function` is supported.")
    open var Type:String? = null

    /**
    * The function that the model called.
    */
    @DataMember(Name="function")
    @SerializedName("function")
    @ApiMember(Description="The function that the model called.")
    open var function:String? = null
}

enum class ResponseFormat
{
    Text,
    JsonObject,
}

enum class ToolType
{
    Function,
}

@DataContract
open class QueryBase
{
    @DataMember(Order=1)
    open var skip:Int? = null

    @DataMember(Order=2)
    open var take:Int? = null

    @DataMember(Order=3)
    open var orderBy:String? = null

    @DataMember(Order=4)
    open var orderByDesc:String? = null

    @DataMember(Order=5)
    open var include:String? = null

    @DataMember(Order=6)
    open var fields:String? = null

    @DataMember(Order=7)
    open var meta:HashMap<String,String>? = null
}

@DataContract
open class AuditBase
{
    @DataMember(Order=1)
    open var createdDate:Date? = null

    @DataMember(Order=2)
    @Required()
    open var createdBy:String? = null

    @DataMember(Order=3)
    open var modifiedDate:Date? = null

    @DataMember(Order=4)
    @Required()
    open var modifiedBy:String? = null

    @DataMember(Order=5)
    open var deletedDate:Date? = null

    @DataMember(Order=6)
    open var deletedBy:String? = null
}

@DataContract
open class ChoiceMessage
{
    /**
    * The contents of the message.
    */
    @DataMember(Name="content")
    @SerializedName("content")
    @ApiMember(Description="The contents of the message.")
    open var content:String? = null

    /**
    * The refusal message generated by the model.
    */
    @DataMember(Name="refusal")
    @SerializedName("refusal")
    @ApiMember(Description="The refusal message generated by the model.")
    open var refusal:String? = null

    /**
    * The reasoning process used by the model.
    */
    @DataMember(Name="reasoning")
    @SerializedName("reasoning")
    @ApiMember(Description="The reasoning process used by the model.")
    open var reasoning:String? = null

    /**
    * The role of the author of this message.
    */
    @DataMember(Name="role")
    @SerializedName("role")
    @ApiMember(Description="The role of the author of this message.")
    open var role:String? = null

    /**
    * Annotations for the message, when applicable, as when using the web search tool.
    */
    @DataMember(Name="annotations")
    @SerializedName("annotations")
    @ApiMember(Description="Annotations for the message, when applicable, as when using the web search tool.")
    open var annotations:ArrayList<ChoiceAnnotation>? = null

    /**
    * If the audio output modality is requested, this object contains data about the audio response from the model.
    */
    @DataMember(Name="audio")
    @SerializedName("audio")
    @ApiMember(Description="If the audio output modality is requested, this object contains data about the audio response from the model.")
    open var audio:ChoiceAudio? = null

    /**
    * The tool calls generated by the model, such as function calls.
    */
    @DataMember(Name="tool_calls")
    @SerializedName("tool_calls")
    @ApiMember(Description="The tool calls generated by the model, such as function calls.")
    open var toolCalls:ArrayList<ToolCall>? = null
}

/**
* Usage statistics for the completion request.
*/
@Api(Description="Usage statistics for the completion request.")
@DataContract
open class AiCompletionUsage
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    @DataMember(Name="accepted_prediction_tokens")
    @SerializedName("accepted_prediction_tokens")
    @ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.\n\n")
    open var acceptedPredictionTokens:Int? = null

    /**
    * Audio input tokens generated by the model.
    */
    @DataMember(Name="audio_tokens")
    @SerializedName("audio_tokens")
    @ApiMember(Description="Audio input tokens generated by the model.")
    open var audioTokens:Int? = null

    /**
    * Tokens generated by the model for reasoning.
    */
    @DataMember(Name="reasoning_tokens")
    @SerializedName("reasoning_tokens")
    @ApiMember(Description="Tokens generated by the model for reasoning.")
    open var reasoningTokens:Int? = null

    /**
    * When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.
    */
    @DataMember(Name="rejected_prediction_tokens")
    @SerializedName("rejected_prediction_tokens")
    @ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.")
    open var rejectedPredictionTokens:Int? = null
}

/**
* Breakdown of tokens used in the prompt.
*/
@Api(Description="Breakdown of tokens used in the prompt.")
@DataContract
open class AiPromptUsage
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    @DataMember(Name="accepted_prediction_tokens")
    @SerializedName("accepted_prediction_tokens")
    @ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.\n\n")
    open var acceptedPredictionTokens:Int? = null

    /**
    * Audio input tokens present in the prompt.
    */
    @DataMember(Name="audio_tokens")
    @SerializedName("audio_tokens")
    @ApiMember(Description="Audio input tokens present in the prompt.")
    open var audioTokens:Int? = null

    /**
    * Cached tokens present in the prompt.
    */
    @DataMember(Name="cached_tokens")
    @SerializedName("cached_tokens")
    @ApiMember(Description="Cached tokens present in the prompt.")
    open var cachedTokens:Int? = null
}

@DataContract
open class RequestSummary
{
    @DataMember(Order=1)
    open var name:String? = null

    @DataMember(Order=2)
    open var totalRequests:Long? = null

    @DataMember(Order=3)
    open var totalRequestLength:Long? = null

    @DataMember(Order=4)
    open var minRequestLength:Long? = null

    @DataMember(Order=5)
    open var maxRequestLength:Long? = null

    @DataMember(Order=6)
    open var totalDuration:Double? = null

    @DataMember(Order=7)
    open var minDuration:Double? = null

    @DataMember(Order=8)
    open var maxDuration:Double? = null

    @DataMember(Order=9)
    open var status:HashMap<Int,Long>? = null

    @DataMember(Order=10)
    open var durations:HashMap<String,Long>? = null

    @DataMember(Order=11)
    open var apis:HashMap<String,Long>? = null

    @DataMember(Order=12)
    open var users:HashMap<String,Long>? = null

    @DataMember(Order=13)
    open var ips:HashMap<String,Long>? = null

    @DataMember(Order=14)
    open var apiKeys:HashMap<String,Long>? = null
}

/**
* Text content part
*/
@DataContract
open class AiTextContent : AiContent()
{
    /**
    * The text content.
    */
    @DataMember(Name="text")
    @SerializedName("text")
    @ApiMember(Description="The text content.")
    open var text:String? = null
}

/**
* Image content part
*/
@DataContract
open class AiImageContent : AiContent()
{
    /**
    * The image for this content.
    */
    @DataMember(Name="image_url")
    @SerializedName("image_url")
    @ApiMember(Description="The image for this content.")
    open var imageUrl:AiImageUrl? = null
}

/**
* Audio content part
*/
@DataContract
open class AiAudioContent : AiContent()
{
    /**
    * The audio input for this content.
    */
    @DataMember(Name="input_audio")
    @SerializedName("input_audio")
    @ApiMember(Description="The audio input for this content.")
    open var inputAudio:AiInputAudio? = null
}

/**
* File content part
*/
@DataContract
open class AiFileContent : AiContent()
{
    /**
    * The file input for this content.
    */
    @DataMember(Name="file")
    @SerializedName("file")
    @ApiMember(Description="The file input for this content.")
    open var file:AiFile? = null
}

/**
* Annotations for the message, when applicable, as when using the web search tool.
*/
@DataContract
open class ChoiceAnnotation
{
    /**
    * The type of the URL citation. Always url_citation.
    */
    @DataMember(Name="type")
    @SerializedName("type")
    @ApiMember(Description="The type of the URL citation. Always url_citation.")
    open var Type:String? = null

    /**
    * A URL citation when using web search.
    */
    @DataMember(Name="url_citation")
    @SerializedName("url_citation")
    @ApiMember(Description="A URL citation when using web search.")
    open var urlCitation:UrlCitation? = null
}

/**
* If the audio output modality is requested, this object contains data about the audio response from the model.
*/
@DataContract
open class ChoiceAudio
{
    /**
    * Base64 encoded audio bytes generated by the model, in the format specified in the request.
    */
    @DataMember(Name="data")
    @SerializedName("data")
    @ApiMember(Description="Base64 encoded audio bytes generated by the model, in the format specified in the request.")
    open var Data:String? = null

    /**
    * The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.
    */
    @DataMember(Name="expires_at")
    @SerializedName("expires_at")
    @ApiMember(Description="The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.")
    open var expiresAt:Int? = null

    /**
    * Unique identifier for this audio response.
    */
    @DataMember(Name="id")
    @SerializedName("id")
    @ApiMember(Description="Unique identifier for this audio response.")
    open var id:String? = null

    /**
    * Transcript of the audio generated by the model.
    */
    @DataMember(Name="transcript")
    @SerializedName("transcript")
    @ApiMember(Description="Transcript of the audio generated by the model.")
    open var transcript:String? = null
}

open class AiImageUrl
{
    /**
    * Either a URL of the image or the base64 encoded image data.
    */
    @DataMember(Name="url")
    @SerializedName("url")
    @ApiMember(Description="Either a URL of the image or the base64 encoded image data.")
    open var url:String? = null
}

/**
* Audio content part
*/
@DataContract
open class AiInputAudio
{
    /**
    * URL or Base64 encoded audio data.
    */
    @DataMember(Name="data")
    @SerializedName("data")
    @ApiMember(Description="URL or Base64 encoded audio data.")
    open var Data:String? = null

    /**
    * The format of the encoded audio data. Currently supports 'wav' and 'mp3'.
    */
    @DataMember(Name="format")
    @SerializedName("format")
    @ApiMember(Description="The format of the encoded audio data. Currently supports 'wav' and 'mp3'.")
    open var format:String? = null
}

/**
* File content part
*/
@DataContract
open class AiFile
{
    /**
    * The URL or base64 encoded file data, used when passing the file to the model as a string.
    */
    @DataMember(Name="file_data")
    @SerializedName("file_data")
    @ApiMember(Description="The URL or base64 encoded file data, used when passing the file to the model as a string.")
    open var fileData:String? = null

    /**
    * The name of the file, used when passing the file to the model as a string.
    */
    @DataMember(Name="filename")
    @SerializedName("filename")
    @ApiMember(Description="The name of the file, used when passing the file to the model as a string.")
    open var filename:String? = null

    /**
    * The ID of an uploaded file to use as input.
    */
    @DataMember(Name="file_id")
    @SerializedName("file_id")
    @ApiMember(Description="The ID of an uploaded file to use as input.")
    open var fileId:String? = null
}

/**
* Annotations for the message, when applicable, as when using the web search tool.
*/
@DataContract
open class UrlCitation
{
    /**
    * The index of the last character of the URL citation in the message.
    */
    @DataMember(Name="end_index")
    @SerializedName("end_index")
    @ApiMember(Description="The index of the last character of the URL citation in the message.")
    open var endIndex:Int? = null

    /**
    * The index of the first character of the URL citation in the message.
    */
    @DataMember(Name="start_index")
    @SerializedName("start_index")
    @ApiMember(Description="The index of the first character of the URL citation in the message.")
    open var startIndex:Int? = null

    /**
    * The title of the web resource.
    */
    @DataMember(Name="title")
    @SerializedName("title")
    @ApiMember(Description="The title of the web resource.")
    open var title:String? = null

    /**
    * The URL of the web resource.
    */
    @DataMember(Name="url")
    @SerializedName("url")
    @ApiMember(Description="The URL of the web resource.")
    open var url:String? = null
}
