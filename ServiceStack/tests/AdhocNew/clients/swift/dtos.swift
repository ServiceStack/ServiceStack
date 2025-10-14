/* Options:
Date: 2025-10-14 12:48:40
SwiftVersion: 6.0
Version: 8.81
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: https://localhost:5001

//BaseClass: 
//AddModelExtensions: True
//AddServiceStackTypes: True
//MakePropertiesOptional: True
IncludeTypes: ChatCompletion.*
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

/**
* Chat Completions API (OpenAI-Compatible)
*/
// @Route("/v1/chat/completions", "POST")
// @DataContract
public class ChatCompletion : IReturn, IPost, Codable
{
    public typealias Return = ChatResponse

    /**
    * The messages to generate chat completions for.
    */
    // @DataMember(Name="messages")
    public var messages:[AiMessage] = []

    /**
    * ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API
    */
    // @DataMember(Name="model")
    public var model:String?

    /**
    * Parameters for audio output. Required when audio output is requested with modalities: [audio]
    */
    // @DataMember(Name="audio")
    public var audio:AiChatAudio?

    /**
    * Modify the likelihood of specified tokens appearing in the completion.
    */
    // @DataMember(Name="logit_bias")
    public var logit_bias:[Int:Int]?

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    // @DataMember(Name="metadata")
    public var metadata:[String:String]?

    /**
    * Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.
    */
    // @DataMember(Name="reasoning_effort")
    public var reasoning_effort:String?

    /**
    * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.
    */
    // @DataMember(Name="response_format")
    public var response_format:AiResponseFormat?

    /**
    * Specifies the processing type used for serving the request.
    */
    // @DataMember(Name="service_tier")
    public var service_tier:String?

    /**
    * A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.
    */
    // @DataMember(Name="safety_identifier")
    public var safety_identifier:String?

    /**
    * Up to 4 sequences where the API will stop generating further tokens.
    */
    // @DataMember(Name="stop")
    public var stop:[String]?

    /**
    * Output types that you would like the model to generate. Most models are capable of generating text, which is the default:
    */
    // @DataMember(Name="modalities")
    public var modalities:[String]?

    /**
    * Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.
    */
    // @DataMember(Name="prompt_cache_key")
    public var prompt_cache_key:String?

    /**
    * A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.
    */
    // @DataMember(Name="tools")
    public var tools:[Tool]?

    /**
    * Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.
    */
    // @DataMember(Name="verbosity")
    public var verbosity:String?

    /**
    * What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
    */
    // @DataMember(Name="temperature")
    public var temperature:Double?

    /**
    * An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.
    */
    // @DataMember(Name="max_completion_tokens")
    public var max_completion_tokens:Int?

    /**
    * An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
    */
    // @DataMember(Name="top_logprobs")
    public var top_logprobs:Int?

    /**
    * An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.
    */
    // @DataMember(Name="top_p")
    public var top_p:Double?

    /**
    * Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
    */
    // @DataMember(Name="frequency_penalty")
    public var frequency_penalty:Double?

    /**
    * Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
    */
    // @DataMember(Name="presence_penalty")
    public var presence_penalty:Double?

    /**
    * This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.
    */
    // @DataMember(Name="seed")
    public var seed:Int?

    /**
    * How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.
    */
    // @DataMember(Name="n")
    public var n:Int?

    /**
    * Whether or not to store the output of this chat completion request for use in our model distillation or evals products.
    */
    // @DataMember(Name="store")
    public var store:Bool?

    /**
    * Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
    */
    // @DataMember(Name="logprobs")
    public var logprobs:Bool?

    /**
    * Whether to enable parallel function calling during tool use.
    */
    // @DataMember(Name="parallel_tool_calls")
    public var parallel_tool_calls:Bool?

    /**
    * Whether to enable thinking mode for some Qwen models and providers.
    */
    // @DataMember(Name="enable_thinking")
    public var enable_thinking:Bool?

    /**
    * If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.
    */
    // @DataMember(Name="stream")
    public var stream:Bool?

    required public init(){}
}

// @DataContract
public class ChatResponse : Codable
{
    /**
    * A unique identifier for the chat completion.
    */
    // @DataMember(Name="id")
    public var id:String?

    /**
    * A list of chat completion choices. Can be more than one if n is greater than 1.
    */
    // @DataMember(Name="choices")
    public var choices:[Choice] = []

    /**
    * The Unix timestamp (in seconds) of when the chat completion was created.
    */
    // @DataMember(Name="created")
    public var created:Int?

    /**
    * The model used for the chat completion.
    */
    // @DataMember(Name="model")
    public var model:String?

    /**
    * This fingerprint represents the backend configuration that the model runs with.
    */
    // @DataMember(Name="system_fingerprint")
    public var system_fingerprint:String?

    /**
    * The object type, which is always chat.completion.
    */
    // @DataMember(Name="object")
    public var object:String?

    /**
    * Specifies the processing type used for serving the request.
    */
    // @DataMember(Name="service_tier")
    public var service_tier:String?

    /**
    * Usage statistics for the completion request.
    */
    // @DataMember(Name="usage")
    public var usage:AiUsage?

    /**
    * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
    */
    // @DataMember(Name="metadata")
    public var metadata:[String:String]?

    // @DataMember(Name="responseStatus")
    public var responseStatus:ResponseStatus?

    required public init(){}
}

/**
* A list of messages comprising the conversation so far.
*/
// @DataContract
public class AiMessage : Codable
{
    /**
    * The contents of the message.
    */
    // @DataMember(Name="content")
    public var content:[AiContent]?

    /**
    * The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.
    */
    // @DataMember(Name="role")
    public var role:String?

    /**
    * An optional name for the participant. Provides the model information to differentiate between participants of the same role.
    */
    // @DataMember(Name="name")
    public var name:String?

    /**
    * The tool calls generated by the model, such as function calls.
    */
    // @DataMember(Name="tool_calls")
    public var tool_calls:[ToolCall]?

    /**
    * Tool call that this message is responding to.
    */
    // @DataMember(Name="tool_call_id")
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
    public var format:String?

    /**
    * The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.
    */
    // @DataMember(Name="voice")
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
    public var type:ToolType?

    required public init(){}
}

// @DataContract
public class Choice : Codable
{
    /**
    * The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool
    */
    // @DataMember(Name="finish_reason")
    public var finish_reason:String?

    /**
    * The index of the choice in the list of choices.
    */
    // @DataMember(Name="index")
    public var index:Int?

    /**
    * A chat completion message generated by the model.
    */
    // @DataMember(Name="message")
    public var message:ChoiceMessage?

    required public init(){}
}

/**
* Usage statistics for the completion request.
*/
// @DataContract
public class AiUsage : Codable
{
    /**
    * Number of tokens in the generated completion.
    */
    // @DataMember(Name="completion_tokens")
    public var completion_tokens:Int?

    /**
    * Number of tokens in the prompt.
    */
    // @DataMember(Name="prompt_tokens")
    public var prompt_tokens:Int?

    /**
    * Total number of tokens used in the request (prompt + completion).
    */
    // @DataMember(Name="total_tokens")
    public var total_tokens:Int?

    /**
    * Breakdown of tokens used in a completion.
    */
    // @DataMember(Name="completion_tokens_details")
    public var completion_tokens_details:AiCompletionUsage?

    /**
    * Breakdown of tokens used in the prompt.
    */
    // @DataMember(Name="prompt_tokens_details")
    public var prompt_tokens_details:AiPromptUsage?

    required public init(){}
}

// @DataContract
public class AiContent : Codable
{
    /**
    * The type of the content part.
    */
    // @DataMember(Name="type")
    public var type:String?

    required public init(){}
}

/**
* The tool calls generated by the model, such as function calls.
*/
// @DataContract
public class ToolCall : Codable
{
    /**
    * The ID of the tool call.
    */
    // @DataMember(Name="id")
    public var id:String?

    /**
    * The type of the tool. Currently, only `function` is supported.
    */
    // @DataMember(Name="type")
    public var type:String?

    /**
    * The function that the model called.
    */
    // @DataMember(Name="function")
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
    public var content:String?

    /**
    * The refusal message generated by the model.
    */
    // @DataMember(Name="refusal")
    public var refusal:String?

    /**
    * The reasoning process used by the model.
    */
    // @DataMember(Name="reasoning")
    public var reasoning:String?

    /**
    * The role of the author of this message.
    */
    // @DataMember(Name="role")
    public var role:String?

    /**
    * Annotations for the message, when applicable, as when using the web search tool.
    */
    // @DataMember(Name="annotations")
    public var annotations:[ChoiceAnnotation]?

    /**
    * If the audio output modality is requested, this object contains data about the audio response from the model.
    */
    // @DataMember(Name="audio")
    public var audio:ChoiceAudio?

    /**
    * The tool calls generated by the model, such as function calls.
    */
    // @DataMember(Name="tool_calls")
    public var tool_calls:[ToolCall]?

    required public init(){}
}

/**
* Usage statistics for the completion request.
*/
// @DataContract
public class AiCompletionUsage : Codable
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    // @DataMember(Name="accepted_prediction_tokens")
    public var accepted_prediction_tokens:Int?

    /**
    * Audio input tokens generated by the model.
    */
    // @DataMember(Name="audio_tokens")
    public var audio_tokens:Int?

    /**
    * Tokens generated by the model for reasoning.
    */
    // @DataMember(Name="reasoning_tokens")
    public var reasoning_tokens:Int?

    /**
    * When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.
    */
    // @DataMember(Name="rejected_prediction_tokens")
    public var rejected_prediction_tokens:Int?

    required public init(){}
}

/**
* Breakdown of tokens used in the prompt.
*/
// @DataContract
public class AiPromptUsage : Codable
{
    /**
    * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
    */
    // @DataMember(Name="accepted_prediction_tokens")
    public var accepted_prediction_tokens:Int?

    /**
    * Audio input tokens present in the prompt.
    */
    // @DataMember(Name="audio_tokens")
    public var audio_tokens:Int?

    /**
    * Cached tokens present in the prompt.
    */
    // @DataMember(Name="cached_tokens")
    public var cached_tokens:Int?

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
    public var type:String?

    /**
    * A URL citation when using web search.
    */
    // @DataMember(Name="url_citation")
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
    public var data:String?

    /**
    * The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.
    */
    // @DataMember(Name="expires_at")
    public var expires_at:Int?

    /**
    * Unique identifier for this audio response.
    */
    // @DataMember(Name="id")
    public var id:String?

    /**
    * Transcript of the audio generated by the model.
    */
    // @DataMember(Name="transcript")
    public var transcript:String?

    required public init(){}
}

// @DataContract
public class AiImageUrl : Codable
{
    /**
    * Either a URL of the image or the base64 encoded image data.
    */
    // @DataMember(Name="url")
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
    public var data:String?

    /**
    * The format of the encoded audio data. Currently supports 'wav' and 'mp3'.
    */
    // @DataMember(Name="format")
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
    public var file_data:String?

    /**
    * The name of the file, used when passing the file to the model as a string.
    */
    // @DataMember(Name="filename")
    public var filename:String?

    /**
    * The ID of an uploaded file to use as input.
    */
    // @DataMember(Name="file_id")
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
    public var end_index:Int?

    /**
    * The index of the first character of the URL citation in the message.
    */
    // @DataMember(Name="start_index")
    public var start_index:Int?

    /**
    * The title of the web resource.
    */
    // @DataMember(Name="title")
    public var title:String?

    /**
    * The URL of the web resource.
    */
    // @DataMember(Name="url")
    public var url:String?

    required public init(){}
}


