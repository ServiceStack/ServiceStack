/* Options:
Date: 2026-08-06 02:13:22
Version: 10.09
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:5000

Package: org.example
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
IncludeTypes: {AI}
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


/**
* Chat Completions API (OpenAI-Compatible)
*/
@Route(Path="/v1/chat/completions", Verbs="POST")
@DataContract
open class ChatCompletion : IReturn<ChatResponse>, IPost
{
    /**
    * The messages to generate chat completions for.
    */
    @DataMember(Name="messages")
    @SerializedName("messages")
    open var messages:ArrayList<AiMessage> = ArrayList<AiMessage>()

    /**
    * ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API
    */
    @DataMember(Name="model")
    @SerializedName("model")
    open var model:String? = null

    /**
    * Parameters for audio output. Required when audio output is requested with modalities: [audio]
    */
    @DataMember(Name="audio")
    @SerializedName("audio")
    open var audio:AiChatAudio? = null

    /**
    * Modify the likelihood of specified tokens appearing in the completion.
    */
    @DataMember(Name="logit_bias")
    @SerializedName("logit_bias")
    open var logitBias:HashMap<Int,Int>? = null

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    @DataMember(Name="metadata")
    @SerializedName("metadata")
    open var metadata:HashMap<String,String>? = null

    /**
    * Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.
    */
    @DataMember(Name="reasoning_effort")
    @SerializedName("reasoning_effort")
    open var reasoningEffort:String? = null

    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.
    */
    @DataMember(Name="response_format")
    @SerializedName("response_format")
    open var responseFormat:AiResponseFormat? = null

    /**
    * Specifies the processing type used for serving the request.
    */
    @DataMember(Name="service_tier")
    @SerializedName("service_tier")
    open var serviceTier:String? = null

    /**
    * A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.
    */
    @DataMember(Name="safety_identifier")
    @SerializedName("safety_identifier")
    open var safetyIdentifier:String? = null

    /**
    * Up to 4 sequences where the API will stop generating further tokens.
    */
    @DataMember(Name="stop")
    @SerializedName("stop")
    open var stop:ArrayList<String>? = null

    /**
    * Output types that you would like the model to generate. Most models are capable of generating text, which is the default:
    */
    @DataMember(Name="modalities")
    @SerializedName("modalities")
    open var modalities:ArrayList<String>? = null

    /**
    * Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.
    */
    @DataMember(Name="prompt_cache_key")
    @SerializedName("prompt_cache_key")
    open var promptCacheKey:String? = null

    /**
    * A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.
    */
    @DataMember(Name="tools")
    @SerializedName("tools")
    open var tools:ArrayList<Tool>? = null

    /**
    * Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.
    */
    @DataMember(Name="verbosity")
    @SerializedName("verbosity")
    open var verbosity:String? = null

    /**
    * What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
    */
    @DataMember(Name="temperature")
    @SerializedName("temperature")
    open var temperature:Double? = null

    /**
    * An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.
    */
    @DataMember(Name="max_completion_tokens")
    @SerializedName("max_completion_tokens")
    open var maxCompletionTokens:Int? = null

    /**
    * An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
    */
    @DataMember(Name="top_logprobs")
    @SerializedName("top_logprobs")
    open var topLogprobs:Int? = null

    /**
    * An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.
    */
    @DataMember(Name="top_p")
    @SerializedName("top_p")
    open var topP:Double? = null

    /**
    * Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
    */
    @DataMember(Name="frequency_penalty")
    @SerializedName("frequency_penalty")
    open var frequencyPenalty:Double? = null

    /**
    * Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
    */
    @DataMember(Name="presence_penalty")
    @SerializedName("presence_penalty")
    open var presencePenalty:Double? = null

    /**
    * This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.
    */
    @DataMember(Name="seed")
    @SerializedName("seed")
    open var seed:Int? = null

    /**
    * How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.
    */
    @DataMember(Name="n")
    @SerializedName("n")
    open var n:Int? = null

    /**
    * Whether or not to store the output of this chat completion request for use in our model distillation or evals products.
    */
    @DataMember(Name="store")
    @SerializedName("store")
    open var store:Boolean? = null

    /**
    * Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
    */
    @DataMember(Name="logprobs")
    @SerializedName("logprobs")
    open var logprobs:Boolean? = null

    /**
    * Whether to enable parallel function calling during tool use.
    */
    @DataMember(Name="parallel_tool_calls")
    @SerializedName("parallel_tool_calls")
    open var parallelToolCalls:Boolean? = null

    /**
    * Whether to enable thinking mode for some Qwen models and providers.
    */
    @DataMember(Name="enable_thinking")
    @SerializedName("enable_thinking")
    open var enableThinking:Boolean? = null

    /**
    * If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.
    */
    @DataMember(Name="stream")
    @SerializedName("stream")
    open var stream:Boolean? = null
    companion object { private val responseType = ChatResponse::class.java }
    override fun getResponseType(): Any? = ChatCompletion.responseType
}

@DataContract
open class ChatResponse
{
    /**
    * A unique identifier for the chat completion.
    */
    @DataMember(Name="id")
    @SerializedName("id")
    open var id:String? = null

    /**
    * A list of chat completion choices. Can be more than one if n is greater than 1.
    */
    @DataMember(Name="choices")
    @SerializedName("choices")
    open var choices:ArrayList<Choice> = ArrayList<Choice>()

    /**
    * The Unix timestamp (in seconds) of when the chat completion was created.
    */
    @DataMember(Name="created")
    @SerializedName("created")
    open var created:Long? = null

    /**
    * The model used for the chat completion.
    */
    @DataMember(Name="model")
    @SerializedName("model")
    open var model:String? = null

    /**
    * This fingerprint represents the backend configuration that the model runs with.
    */
    @DataMember(Name="system_fingerprint")
    @SerializedName("system_fingerprint")
    open var systemFingerprint:String? = null

    /**
    * The object type, which is always chat.completion.
    */
    @DataMember(Name="object")
    @SerializedName("object")
    open var Object:String? = null

    /**
    * Specifies the processing type used for serving the request.
    */
    @DataMember(Name="service_tier")
    @SerializedName("service_tier")
    open var serviceTier:String? = null

    /**
    * Usage statistics for the completion request.
    */
    @DataMember(Name="usage")
    @SerializedName("usage")
    open var usage:AiUsage? = null

    /**
    * The provider used for the chat completion.
    */
    @DataMember(Name="provider")
    @SerializedName("provider")
    open var provider:String? = null

    /**
    * Total cost of the completion in USD, accumulated across every request in the tool loop.
    */
    @DataMember(Name="cost")
    @SerializedName("cost")
    open var cost:Double? = null

    /**
    * The assistant and tool messages exchanged during the tool-execution loop, in order.
    */
    @DataMember(Name="tool_history")
    @SerializedName("tool_history")
    open var toolHistory:ArrayList<ChoiceMessage>? = null

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    @DataMember(Name="metadata")
    @SerializedName("metadata")
    open var metadata:HashMap<String,String>? = null

    @DataMember(Name="responseStatus")
    @SerializedName("responseStatus")
    open var responseStatus:ResponseStatus? = null
}

/**
* A list of messages comprising the conversation so far.
*/
@DataContract
open class AiMessage
{
    /**
    * The contents of the message.
    */
    @DataMember(Name="content")
    @SerializedName("content")
    open var content:ArrayList<AiContent>? = null

    /**
    * The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.
    */
    @DataMember(Name="role")
    @SerializedName("role")
    open var role:String? = null

    /**
    * An optional name for the participant. Provides the model information to differentiate between participants of the same role.
    */
    @DataMember(Name="name")
    @SerializedName("name")
    open var name:String? = null

    /**
    * The tool calls generated by the model, such as function calls.
    */
    @DataMember(Name="tool_calls")
    @SerializedName("tool_calls")
    open var toolCalls:ArrayList<ToolCall>? = null

    /**
    * Tool call that this message is responding to.
    */
    @DataMember(Name="tool_call_id")
    @SerializedName("tool_call_id")
    open var toolCallId:String? = null

    /**
    * The reasoning an assistant message was generated with, normalized per provider when replayed as history.
    */
    @DataMember(Name="reasoning")
    @SerializedName("reasoning")
    open var reasoning:String? = null

    /**
    * The reasoning an assistant message was generated with, as emitted by Gemini and most OpenAI-compatible providers.
    */
    @DataMember(Name="reasoning_content")
    @SerializedName("reasoning_content")
    open var reasoningContent:String? = null

    /**
    * Unix timestamp (in milliseconds) the message was generated.
    */
    @DataMember(Name="timestamp")
    @SerializedName("timestamp")
    open var timestamp:Long? = null

    /**
    * Images attached to the message. Folded into `content` parts before sending to a provider.
    */
    @DataMember(Name="images")
    @SerializedName("images")
    open var images:ArrayList<AiContent>? = null
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
    open var format:String? = null

    /**
    * The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.
    */
    @DataMember(Name="voice")
    @SerializedName("voice")
    open var voice:String? = null
}

@DataContract
open class AiResponseFormat
{
    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.
    */
    @DataMember(Name="type")
    @SerializedName("type")
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
    open var Type:ToolType? = null

    /**
    * The function definition the model may call.
    */
    @DataMember(Name="function")
    @SerializedName("function")
    open var function:AiToolFunction? = null
}

@DataContract
open class Choice
{
    /**
    * The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool
    */
    @DataMember(Name="finish_reason")
    @SerializedName("finish_reason")
    open var finishReason:String? = null

    /**
    * The index of the choice in the list of choices.
    */
    @DataMember(Name="index")
    @SerializedName("index")
    open var index:Int? = null

    /**
    * A chat completion message generated by the model.
    */
    @DataMember(Name="message")
    @SerializedName("message")
    open var message:ChoiceMessage? = null

    /**
    * Log probability information for the choice.
    */
    @DataMember(Name="logprobs")
    @SerializedName("logprobs")
    open var logprobs:Logprobs? = null
}

/**
* Usage statistics for the completion request.
*/
@DataContract
open class AiUsage
{
    /**
    * Number of tokens in the generated completion.
    */
    @DataMember(Name="completion_tokens")
    @SerializedName("completion_tokens")
    open var completionTokens:Long? = null

    /**
    * Number of tokens in the prompt.
    */
    @DataMember(Name="prompt_tokens")
    @SerializedName("prompt_tokens")
    open var promptTokens:Long? = null

    /**
    * Total number of tokens used in the request (prompt + completion).
    */
    @DataMember(Name="total_tokens")
    @SerializedName("total_tokens")
    open var totalTokens:Long? = null

    /**
    * Breakdown of tokens used in a completion.
    */
    @DataMember(Name="completion_tokens_details")
    @SerializedName("completion_tokens_details")
    open var completionTokensDetails:AiCompletionUsage? = null

    /**
    * Breakdown of tokens used in the prompt.
    */
    @DataMember(Name="prompt_tokens_details")
    @SerializedName("prompt_tokens_details")
    open var promptTokensDetails:AiPromptUsage? = null

    /**
    * Seconds spent servicing the completion, including every request in the tool loop.
    */
    @DataMember(Name="duration")
    @SerializedName("duration")
    open var duration:Long? = null
}

@DataContract
open class ChoiceMessage
{
    /**
    * The contents of the message.
    */
    @DataMember(Name="content")
    @SerializedName("content")
    open var content:String? = null

    /**
    * The refusal message generated by the model.
    */
    @DataMember(Name="refusal")
    @SerializedName("refusal")
    open var refusal:String? = null

    /**
    * The reasoning process used by the model.
    */
    @DataMember(Name="reasoning")
    @SerializedName("reasoning")
    open var reasoning:String? = null

    /**
    * The reasoning process used by the model, as emitted by Gemini and most OpenAI-compatible providers.
    */
    @DataMember(Name="reasoning_content")
    @SerializedName("reasoning_content")
    open var reasoningContent:String? = null

    /**
    * The reasoning process used by the model, as emitted by Anthropic.
    */
    @DataMember(Name="thinking")
    @SerializedName("thinking")
    open var thinking:String? = null

    /**
    * The role of the author of this message.
    */
    @DataMember(Name="role")
    @SerializedName("role")
    open var role:String? = null

    /**
    * Unix timestamp (in milliseconds) the message was generated.
    */
    @DataMember(Name="timestamp")
    @SerializedName("timestamp")
    open var timestamp:Long? = null

    /**
    * The tool call this message is responding to, set on `tool` role messages in tool_history.
    */
    @DataMember(Name="tool_call_id")
    @SerializedName("tool_call_id")
    open var toolCallId:String? = null

    /**
    * Images generated by the model or produced by a tool call.
    */
    @DataMember(Name="images")
    @SerializedName("images")
    open var images:ArrayList<AiContent>? = null

    /**
    * Audio generated by the model or produced by a tool call.
    */
    @DataMember(Name="audios")
    @SerializedName("audios")
    open var audios:ArrayList<AiContent>? = null

    /**
    * Files produced by a tool call.
    */
    @DataMember(Name="files")
    @SerializedName("files")
    open var files:ArrayList<AiContent>? = null

    /**
    * Annotations for the message, when applicable, as when using the web search tool.
    */
    @DataMember(Name="annotations")
    @SerializedName("annotations")
    open var annotations:ArrayList<ChoiceAnnotation>? = null

    /**
    * If the audio output modality is requested, this object contains data about the audio response from the model.
    */
    @DataMember(Name="audio")
    @SerializedName("audio")
    open var audio:ChoiceAudio? = null

    /**
    * The tool calls generated by the model, such as function calls.
    */
    @DataMember(Name="tool_calls")
    @SerializedName("tool_calls")
    open var toolCalls:ArrayList<ToolCall>? = null
}

@DataContract
open class AiContent
{
    /**
    * The type of the content part.
    */
    @DataMember(Name="type")
    @SerializedName("type")
    open var Type:String? = null
}

/**
* The tool calls generated by the model, such as function calls.
*/
@DataContract
open class ToolCall
{
    /**
    * The ID of the tool call.
    */
    @DataMember(Name="id")
    @SerializedName("id")
    open var id:String? = null

    /**
    * The type of the tool. Currently, only `function` is supported.
    */
    @DataMember(Name="type")
    @SerializedName("type")
    open var Type:String? = null

    /**
    * The function that the model called.
    */
    @DataMember(Name="function")
    @SerializedName("function")
    open var function:ToolFunction? = null
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
open class AiToolFunction
{
    /**
    * The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64.
    */
    @DataMember(Name="name")
    @SerializedName("name")
    open var name:String? = null

    /**
    * A description of what the function does, used by the model to choose when and how to call the function.
    */
    @DataMember(Name="description")
    @SerializedName("description")
    open var description:String? = null

    /**
    * The parameters the functions accepts, described as a JSON Schema object. See the guide for examples, and the JSON Schema reference for documentation about the format.
    */
    @DataMember(Name="parameters")
    @SerializedName("parameters")
    open var parameters:HashMap<String,Object>? = null
}

/**
* Log probability information for the choice.
*/
@DataContract
open class Logprobs
{
    /**
    * A list of message content tokens with log probability information.
    */
    @DataMember(Name="content")
    @SerializedName("content")
    open var content:ArrayList<LogprobItem> = ArrayList<LogprobItem>()
}

/**
* Usage statistics for the completion request.
*/
@DataContract
open class AiCompletionUsage
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    @DataMember(Name="accepted_prediction_tokens")
    @SerializedName("accepted_prediction_tokens")
    open var acceptedPredictionTokens:Long? = null

    /**
    * Audio input tokens generated by the model.
    */
    @DataMember(Name="audio_tokens")
    @SerializedName("audio_tokens")
    open var audioTokens:Long? = null

    /**
    * Tokens generated by the model for reasoning.
    */
    @DataMember(Name="reasoning_tokens")
    @SerializedName("reasoning_tokens")
    open var reasoningTokens:Long? = null

    /**
    * When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.
    */
    @DataMember(Name="rejected_prediction_tokens")
    @SerializedName("rejected_prediction_tokens")
    open var rejectedPredictionTokens:Long? = null
}

/**
* Breakdown of tokens used in the prompt.
*/
@DataContract
open class AiPromptUsage
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    @DataMember(Name="accepted_prediction_tokens")
    @SerializedName("accepted_prediction_tokens")
    open var acceptedPredictionTokens:Long? = null

    /**
    * Audio input tokens present in the prompt.
    */
    @DataMember(Name="audio_tokens")
    @SerializedName("audio_tokens")
    open var audioTokens:Long? = null

    /**
    * Cached tokens present in the prompt.
    */
    @DataMember(Name="cached_tokens")
    @SerializedName("cached_tokens")
    open var cachedTokens:Long? = null
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
    open var Type:String? = null

    /**
    * A URL citation when using web search.
    */
    @DataMember(Name="url_citation")
    @SerializedName("url_citation")
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
    open var Data:String? = null

    /**
    * The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.
    */
    @DataMember(Name="expires_at")
    @SerializedName("expires_at")
    open var expiresAt:Long? = null

    /**
    * Unique identifier for this audio response.
    */
    @DataMember(Name="id")
    @SerializedName("id")
    open var id:String? = null

    /**
    * Transcript of the audio generated by the model.
    */
    @DataMember(Name="transcript")
    @SerializedName("transcript")
    open var transcript:String? = null
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
    open var file:AiFile? = null
}

/**
* Generated audio content part, referenced by URL (emitted by tool calls and audio models)
*/
@DataContract
open class AiAudioUrlContent : AiContent()
{
    /**
    * The audio for this content.
    */
    @DataMember(Name="audio_url")
    @SerializedName("audio_url")
    open var audioUrl:AiAudioUrl? = null
}

/**
* The function that the model called.
*/
@DataContract
open class ToolFunction
{
    /**
    * The name of the function to call.
    */
    @DataMember(Name="name")
    @SerializedName("name")
    open var name:String? = null

    /**
    * The arguments to call the function with, as generated by the model in JSON format. Note that the model does not always generate valid JSON, and may hallucinate parameters not defined by your function schema. Validate the arguments in your code before calling your function.
    */
    @DataMember(Name="arguments")
    @SerializedName("arguments")
    open var arguments:String? = null
}

/**
* A list of message content tokens with log probability information.
*/
@DataContract
open class LogprobItem
{
    /**
    * The token.
    */
    @DataMember(Name="token")
    @SerializedName("token")
    open var token:String? = null

    /**
    * The log probability of this token, if it is within the top 20 most likely tokens. Otherwise, the value `-9999`.0 is used to signify that the token is very unlikely.
    */
    @DataMember(Name="logprob")
    @SerializedName("logprob")
    open var logprob:Double? = null

    /**
    * A list of integers representing the UTF-8 bytes representation of the token. Useful in instances where characters are represented by multiple tokens and their byte representations must be combined to generate the correct text representation. Can be `null` if there is no bytes representation for the token.
    */
    @DataMember(Name="bytes")
    @SerializedName("bytes")
    open var bytes:ByteArray = ByteArray(0)

    /**
    * List of the most likely tokens and their log probability, at this token position. In rare cases, there may be fewer than the number of requested `top_logprobs` returned.
    */
    @DataMember(Name="top_logprobs")
    @SerializedName("top_logprobs")
    open var topLogprobs:ArrayList<LogprobItem> = ArrayList<LogprobItem>()
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
    open var endIndex:Int? = null

    /**
    * The index of the first character of the URL citation in the message.
    */
    @DataMember(Name="start_index")
    @SerializedName("start_index")
    open var startIndex:Int? = null

    /**
    * The title of the web resource.
    */
    @DataMember(Name="title")
    @SerializedName("title")
    open var title:String? = null

    /**
    * The URL of the web resource.
    */
    @DataMember(Name="url")
    @SerializedName("url")
    open var url:String? = null
}

@DataContract
open class AiImageUrl
{
    /**
    * Either a URL of the image or the base64 encoded image data.
    */
    @DataMember(Name="url")
    @SerializedName("url")
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
    open var Data:String? = null

    /**
    * The format of the encoded audio data. Currently supports 'wav' and 'mp3'.
    */
    @DataMember(Name="format")
    @SerializedName("format")
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
    open var fileData:String? = null

    /**
    * The name of the file, used when passing the file to the model as a string.
    */
    @DataMember(Name="filename")
    @SerializedName("filename")
    open var filename:String? = null

    /**
    * The ID of an uploaded file to use as input.
    */
    @DataMember(Name="file_id")
    @SerializedName("file_id")
    open var fileId:String? = null
}

@DataContract
open class AiAudioUrl
{
    /**
    * Either a URL of the audio or the base64 encoded audio data.
    */
    @DataMember(Name="url")
    @SerializedName("url")
    open var url:String? = null
}
