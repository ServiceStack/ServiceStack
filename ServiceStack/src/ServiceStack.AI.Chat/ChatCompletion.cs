using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace ServiceStack.AI;

public static class Tags
{
    public const string Agent = nameof(Agent);
    public const string AI = nameof(AI);
}

[ValidateApiKey]
[Tag(Tags.Agent)]
public class GetChatCompletion : IGet, IReturn<ChatCompletion>
{
    [ValidateNotEmpty, ValidateExactLength(32)]
    public string Device { get; set; }
    [ValidateNotEmpty]
    public List<string> Models { get; set; }
}

[ValidateApiKey]
[Tag(Tags.Agent)]
public class CompleteChatCompletion : ChatResponse, IPost, IReturn<EmptyResponse>
{
    [ValidateNotEmpty]
    public long RefId { get; set; }
}


/// <summary>
/// https://platform.openai.com/docs/api-reference/chat/create
/// </summary>
[Tag(Tags.AI)]
[ValidateApiKey]
[Route("/v1/chat/completions", "POST")]
[SystemJson(UseSystemJson.Never)]
public class CreateChatCompletion : ChatCompletion, IPost, IReturn<ChatResponse>
{
    [ApiMember(Description="Provide a unique identifier to track requests")]
    public string? RefId { get; set; }
    
    [ApiMember(Description="Categorize like requests under a common group")]
    public string? Tag { get; set; }
}

/// <summary>
/// https://platform.openai.com/docs/api-reference/chat/create
/// </summary>
[Tag(Tags.AI)]
[Api("Given a list of messages comprising a conversation, the model will return a response.")]
[DataContract]
public class ChatCompletion
{
    [ApiMember(Description = "A list of messages comprising the conversation so far.")]
    [DataMember(Name = "messages")]
    public List<AiMessage> Messages { get; set; } = [];
    
    [ApiMember(Description="ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API")]
    [DataMember(Name = "model")]
    public string Model { get; set; }
    
    [ApiMember(Description="Parameters for audio output. Required when audio output is requested with modalities: [audio]")]
    [DataMember(Name = "audio")]
    public AiChatAudio? Audio { get; set; }
    
    [ApiMember(Description="Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.")]
    [DataMember(Name = "frequency_penalty")]
    public double? FrequencyPenalty { get; set; }

    [ApiMember(Description="Modify the likelihood of specified tokens appearing in the completion.")]
    [DataMember(Name = "logit_bias")]
    public Dictionary<int,int>? LogitBias { get; set; }
    
    [ApiMember(Description="Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.")]
    [DataMember(Name = "logprobs")]
    public bool? Logprobs { get; set; }

    [ApiMember(Description="An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.")]
    [DataMember(Name = "max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }
    
    [ApiMember(Description = "Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")]
    [DataMember(Name = "metadata")]
    public Dictionary<string,string>? Metadata { get; set; }
    
    [ApiMember(Description = "Output types that you would like the model to generate. Most models are capable of generating text, which is the default:")]
    [DataMember(Name = "modalities")]
    public List<string>? Modalities { get; set; }
    
    [ApiMember(Description="How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.")]
    [DataMember(Name = "n")]
    public int? N { get; set; }
    
    [ApiMember(Description="Whether to enable parallel function calling during tool use.")]
    [DataMember(Name = "parallel_tool_calls")]
    public bool? ParallelToolCalls { get; set; }
    
    [ApiMember(Description="Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.")]
    [DataMember(Name = "presence_penalty")]
    public double? PresencePenalty { get; set; }

    [ApiMember(Description="Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.")]
    [DataMember(Name = "prompt_cache_key")]
    public string? PromptCacheKey { get; set; }
    
    [ApiMember(Description="Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.")]
    [DataMember(Name = "reasoning_effort")]
    public string? ReasoningEffort { get; set; }
    
    [ApiMember(Description="An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.")]
    [DataMember(Name = "response_format")]
    public AiResponseFormat? ResponseFormat { get; set; }

    [ApiMember(Description="A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.")]
    [DataMember(Name = "safety_identifier")]
    public string? SafetyIdentifier { get; set; }
    
    [ApiMember(Description="This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.")]
    [DataMember(Name = "seed")]
    public int? Seed { get; set; }
    
    [ApiMember(Description="Specifies the processing type used for serving the request.")]
    [DataMember(Name = "service_tier")]
    public string? ServiceTier { get; set; }
    
    [ApiMember(Description="Up to 4 sequences where the API will stop generating further tokens.")]
    [DataMember(Name = "stop")]
    public List<string>? Stop { get; set; }
    
    [ApiMember(Description="Whether or not to store the output of this chat completion request for use in our model distillation or evals products.")]
    [DataMember(Name = "store")]
    public bool? Store { get; set; }
    
    [ApiMember(Description="If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.")]
    [DataMember(Name = "stream")]
    public bool? Stream { get; set; }
    
    [ApiMember(Description="What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.")]
    [DataMember(Name = "temperature")]
    public double? Temperature { get; set; }
    
    [ApiMember(Description="A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.")]
    [DataMember(Name = "tools")]
    public List<Tool>? Tools { get; set; }

    [ApiMember(Description="An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.")]
    [DataMember(Name = "top_logprobs")]
    public int? TopLogprobs { get; set; }
    
    [ApiMember(Description="An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.")]
    [DataMember(Name = "top_p")]
    public double? TopP { get; set; }
    
    [ApiMember(Description="Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.")]
    [DataMember(Name = "verbosity")]
    public string? Verbosity { get; set; }
    
    [ApiMember(Description="Whether to enable thinking mode for some Qwen models and providers.")]
    [DataMember(Name = "enable_thinking")]
    public bool? EnableThinking { get; set; }
}

[Description("Parameters for audio output. Required when audio output is requested with modalities: [audio]")]
[DataContract]
public class AiChatAudio
{
    [ApiMember(Description="Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.")]
    [DataMember(Name = "format")]
    public string Format { get; set; }

    [ApiMember(Description="The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.")]
    [DataMember(Name = "voice")]
    public string Voice { get; set; }
}

[DataContract]
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AiTextContent),  typeDiscriminator: "text")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AiImageContent), typeDiscriminator: "image_url")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AiAudioContent), typeDiscriminator: "input_audio")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AiFileContent),  typeDiscriminator: "file")]
public abstract class AiContent
{
    [ApiMember(Description="The type of the content part.")]
    [DataMember(Name = "type")]
    public string Type { get; set; }
}

[DataContract]
[Description("Text content part")]
public class AiTextContent : AiContent
{
    [ApiMember(Description="The text content.")]
    [DataMember(Name = "text")]
    public string Text { get; set; }
}

[DataContract]
[Description("Image content part")]
public class AiImageContent : AiContent
{
    [ApiMember(Description="The image for this content.")]
    [DataMember(Name = "image_url")]
    public AiImageUrl ImageUrl { get; set; }
}
public class AiImageUrl
{
    [ApiMember(Description="Either a URL of the image or the base64 encoded image data.")]
    [DataMember(Name = "url")]
    public string Url { get; set; }
}

[DataContract]
[Description("Audio content part")]
public class AiAudioContent : AiContent
{
    [ApiMember(Description="The audio input for this content.")]
    [DataMember(Name = "input_audio")]
    public AiInputAudio InputAudio { get; set; }
}

[DataContract]
[Description("Audio content part")]
public class AiInputAudio
{
    [ApiMember(Description="URL or Base64 encoded audio data.")]
    [DataMember(Name = "data")]
    public string Data { get; set; }

    [ApiMember(Description="The format of the encoded audio data. Currently supports 'wav' and 'mp3'.")]
    [DataMember(Name = "format")]
    public string Format { get; set; }
}

[DataContract]
[Description("File content part")]
public class AiFileContent : AiContent
{
    [ApiMember(Description="The file input for this content.")]
    [DataMember(Name = "file")]
    public AiFile File { get; set; }
}
[DataContract]
[Description("File content part")]
public class AiFile
{
    [ApiMember(Description="The URL or base64 encoded file data, used when passing the file to the model as a string.")]
    [DataMember(Name = "file_data")]
    public string FileData { get; set; }

    [ApiMember(Description="The name of the file, used when passing the file to the model as a string.")]
    [DataMember(Name = "filename")]
    public string Filename { get; set; }

    [ApiMember(Description="The ID of an uploaded file to use as input.")]
    [DataMember(Name = "file_id")]
    public string? FileId { get; set; }
}


[Api("A list of messages comprising the conversation so far.")]
[DataContract]
public class AiMessage
{
    [ApiMember(Description="The contents of the message.")]
    [DataMember(Name = "content")]
    public List<AiContent>? Content { get; set; }
    
    // [ApiMember(Description="The images for the message.")]
    // [DataMember(Name = "images")]
    // public List<string>? Images { get; set; }
    
    [ApiMember(Description="The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.")]
    [DataMember(Name = "role")]
    public string Role { get; set; }
    
    [ApiMember(Description="An optional name for the participant. Provides the model information to differentiate between participants of the same role.")]
    [DataMember(Name = "name")]
    public string? Name { get; set; }
    
    [ApiMember(Description="The tool calls generated by the model, such as function calls.")]
    [DataMember(Name = "tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
    
    [ApiMember(Description="Tool call that this message is responding to.")]
    [DataMember(Name = "tool_call_id")]
    public string? ToolCallId { get; set; }
}

[DataContract]
public class Tool
{
    [ApiMember(Description="The type of the tool. Currently, only function is supported.")]
    [DataMember(Name = "type")]
    public ToolType Type { get; set; }
}

public enum ToolType
{
    [EnumMember(Value = "function")]
    Function,
}

[DataContract]
public class AiToolFunction
{
    [ApiMember(Description="The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64.")]
    [DataMember(Name = "name")]
    public string? Name { get; set; }

    [ApiMember(Description="A description of what the function does, used by the model to choose when and how to call the function.")]
    [DataMember(Name = "description")]
    public string? Description { get; set; }
    
    [ApiMember(Description="The parameters the functions accepts, described as a JSON Schema object. See the guide for examples, and the JSON Schema reference for documentation about the format.")]
    [DataMember(Name = "parameters")]
    public Dictionary<string,string>? Parameters { get; set; }
}

[DataContract]
public class AiResponseFormat
{
    public const string Text = "text";
    public const string JsonObject = "json_object";
    
    [ApiMember(Description="An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.")]
    [DataMember(Name = "response_format")]
    public ResponseFormat Type { get; set; }
}

public enum ResponseFormat
{
    [EnumMember(Value = "text")]
    Text,
    [EnumMember(Value = "json_object")]
    JsonObject
}

/*
{
    'model': 'mistral-small3.2:24b', 
    'created_at': '2025-06-21T19:22:25.687062838Z', 
    'message': {'role': 'assistant', 'content': 'La réponse est 4.'}, 
    'done_reason': 'stop', 
    'done': True, 
    'total_duration': 8119019132, 
    'load_duration': 3657250637, 
    'prompt_eval_count': 511, 
    'prompt_eval_duration': 4042996254, 
    'eval_count': 7, 
    'eval_duration': 400910229
}
*/

[DataContract]
public class ChatResponse
{
    [ApiMember(Description="A unique identifier for the chat completion.")]
    [DataMember(Name = "id")]
    public string Id { get; set; }
    
    [ApiMember(Description="A list of chat completion choices. Can be more than one if n is greater than 1.")]
    [DataMember(Name = "choices")]
    public List<Choice> Choices { get; set; }
    
    [ApiMember(Description="The Unix timestamp (in seconds) of when the chat completion was created.")]
    [DataMember(Name = "created")]
    public long Created { get; set; }
    
    [ApiMember(Description="The model used for the chat completion.")]
    [DataMember(Name = "model")]
    public string Model { get; set; }
    
    [ApiMember(Description="This fingerprint represents the backend configuration that the model runs with.")]
    [DataMember(Name = "system_fingerprint")]
    public string? SystemFingerprint { get; set; }
    
    [ApiMember(Description="The object type, which is always chat.completion.")]
    [DataMember(Name = "object")]
    public string Object { get; set; }
    
    [ApiMember(Description="Specifies the processing type used for serving the request.")]
    [DataMember(Name = "service_tier")]
    public string? ServiceTier { get; set; }
    
    [ApiMember(Description="Usage statistics for the completion request.")]
    [DataMember(Name = "usage")]
    public AiUsage Usage { get; set; }
    
    [ApiMember(Description = "Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")]
    [DataMember(Name = "metadata")]
    public Dictionary<string,string>? Metadata { get; set; }
    
    [DataMember(Name = "responseStatus")]
    public ResponseStatus? ResponseStatus { get; set; }
}

[Api(Description="Configuration options for reasoning models.")]
[DataContract]
public class AiReasoning
{
    [ApiMember(Description="Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high.")]
    [DataMember(Name = "effort")]
    public string? Effort { get; set; }

    [ApiMember(Description="A summary of the reasoning performed by the model. This can be useful for debugging and understanding the model's reasoning process. One of auto, concise, or detailed.")]
    [DataMember(Name = "summary")]
    public string? Summary { get; set; }
}

[Api(Description="Usage statistics for the completion request.")]
[DataContract]
public class AiUsage
{
    [ApiMember(Description="Number of tokens in the generated completion.")]
    [DataMember(Name = "completion_tokens")]
    public int CompletionTokens { get; set; }

    [ApiMember(Description="Number of tokens in the prompt.")]
    [DataMember(Name = "prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [ApiMember(Description="Total number of tokens used in the request (prompt + completion).")]
    [DataMember(Name = "total_tokens")]
    public int TotalTokens { get; set; }
    
    [ApiMember(Description="Breakdown of tokens used in a completion.")]
    [DataMember(Name = "completion_tokens_details")]
    public AiCompletionUsage? CompletionTokensDetails { get; set; }
    
    [ApiMember(Description="Breakdown of tokens used in the prompt.")]
    [DataMember(Name = "prompt_tokens_details")]
    public AiPromptUsage? PromptTokensDetails { get; set; }
}

[Api(Description="Usage statistics for the completion request.")]
[DataContract]
public class AiCompletionUsage
{
    [ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.\n\n")]
    [DataMember(Name = "accepted_prediction_tokens")]
    public int AcceptedPredictionTokens { get; set; }

    [ApiMember(Description="Audio input tokens generated by the model.")]
    [DataMember(Name = "audio_tokens")]
    public int AudioTokens { get; set; }

    [ApiMember(Description="Tokens generated by the model for reasoning.")]
    [DataMember(Name = "reasoning_tokens")]
    public int ReasoningTokens { get; set; }
    
    [ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.")]
    [DataMember(Name = "rejected_prediction_tokens")]
    public int RejectedPredictionTokens { get; set; }
}

[Api(Description="Breakdown of tokens used in the prompt.")]
[DataContract]
public class AiPromptUsage
{
    [ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.\n\n")]
    [DataMember(Name = "accepted_prediction_tokens")]
    public int AcceptedPredictionTokens { get; set; }

    [ApiMember(Description="Audio input tokens present in the prompt.")]
    [DataMember(Name = "audio_tokens")]
    public int AudioTokens { get; set; }

    [ApiMember(Description="Cached tokens present in the prompt.")]
    [DataMember(Name = "cached_tokens")]
    public int CachedTokens { get; set; }
}

public class Choice
{
    [ApiMember(Description="The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool")]
    [DataMember(Name = "finish_reason")]
    public string FinishReason { get; set; }

    [ApiMember(Description="The index of the choice in the list of choices.")]
    [DataMember(Name = "index")]
    public int Index { get; set; }
    
    [ApiMember(Description="A chat completion message generated by the model.")]
    [DataMember(Name = "message")]
    public ChoiceMessage Message { get; set; }
}

[DataContract]
public class ChoiceMessage
{
    [ApiMember(Description="The contents of the message.")]
    [DataMember(Name = "content")]
    public string Content { get; set; }

    [ApiMember(Description="The refusal message generated by the model.")]
    [DataMember(Name = "refusal")]
    public string? Refusal { get; set; }

    [ApiMember(Description="The reasoning process used by the model.")]
    [DataMember(Name = "reasoning")]
    public string? Reasoning { get; set; }
    
    [ApiMember(Description="The role of the author of this message.")]
    [DataMember(Name = "role")]
    public string Role { get; set; }

    [ApiMember(Description="Annotations for the message, when applicable, as when using the web search tool.")]
    [DataMember(Name = "annotations")]
    public List<ChoiceAnnotation>? Annotations { get; set; }

    [ApiMember(Description="If the audio output modality is requested, this object contains data about the audio response from the model.")]
    [DataMember(Name = "audio")]
    public ChoiceAudio? Audio { get; set; }
    
    [ApiMember(Description="The tool calls generated by the model, such as function calls.")]
    [DataMember(Name = "tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
}

[Description("Annotations for the message, when applicable, as when using the web search tool.")]
[DataContract]
public class ChoiceAnnotation
{
    [ApiMember(Description="The type of the URL citation. Always url_citation.")]
    [DataMember(Name = "type")]
    public string Type { get; set; }

    [ApiMember(Description="A URL citation when using web search.")]
    [DataMember(Name = "url_citation")]
    public UrlCitation UrlCitation { get; set; }
}

[Description("Annotations for the message, when applicable, as when using the web search tool.")]
[DataContract]
public class UrlCitation
{
    [ApiMember(Description="The index of the last character of the URL citation in the message.")]
    [DataMember(Name = "end_index")]
    public int EndIndex { get; set; }

    [ApiMember(Description="The index of the first character of the URL citation in the message.")]
    [DataMember(Name = "start_index")]
    public int StartIndex { get; set; }

    [ApiMember(Description="The title of the web resource.")]
    [DataMember(Name = "title")]
    public string Title { get; set; }

    [ApiMember(Description="The URL of the web resource.")]
    [DataMember(Name = "url")]
    public string Url { get; set; }
}

[Description("If the audio output modality is requested, this object contains data about the audio response from the model.")]
[DataContract]
public class ChoiceAudio
{
    [ApiMember(Description="Base64 encoded audio bytes generated by the model, in the format specified in the request.")]
    [DataMember(Name = "data")]
    public string Data { get; set; }

    [ApiMember(Description="The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.")]
    [DataMember(Name = "expires_at")]
    public int ExpiresAt { get; set; }
    
    [ApiMember(Description="Unique identifier for this audio response.")]
    [DataMember(Name = "id")]
    public string Id { get; set; }
    
    [ApiMember(Description="Transcript of the audio generated by the model.")]
    [DataMember(Name = "transcript")]
    public string Transcript { get; set; }
}

[Api("The tool calls generated by the model, such as function calls.")]
[DataContract]
public class ToolCall
{
    [ApiMember(Description="The ID of the tool call.")]
    [DataMember(Name = "id")]
    public string Id { get; set; }
    
    [ApiMember(Description="The type of the tool. Currently, only `function` is supported.")]
    [DataMember(Name = "type")]
    public string Type { get; set; }
    
    [ApiMember(Description="The function that the model called.")]
    [DataMember(Name = "function")]
    public string Function { get; set; }
}

[Api("The function that the model called.")]
[DataContract]
public class ToolFunction
{
    [ApiMember(Description="The name of the function to call.")]
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [ApiMember(Description="The arguments to call the function with, as generated by the model in JSON format. Note that the model does not always generate valid JSON, and may hallucinate parameters not defined by your function schema. Validate the arguments in your code before calling your function.")]
    [DataMember(Name = "arguments")]
    public string Arguments { get; set; }
}

[Api("Log probability information for the choice.")]
[DataContract]
public class Logprobs
{
    [ApiMember(Description="A list of message content tokens with log probability information.")]
    [DataMember(Name = "content")]
    public List<LogprobItem> Content { get; set; }
}

[Api("A list of message content tokens with log probability information.")]
[DataContract]
public class LogprobItem
{
    [ApiMember(Description="The token.")]
    [DataMember(Name = "token")]
    public string Token { get; set; }

    [ApiMember(Description="The log probability of this token, if it is within the top 20 most likely tokens. Otherwise, the value `-9999`.0 is used to signify that the token is very unlikely.")]
    [DataMember(Name = "logprob")]
    public double Logprob { get; set; }
    
    [ApiMember(Description="A list of integers representing the UTF-8 bytes representation of the token. Useful in instances where characters are represented by multiple tokens and their byte representations must be combined to generate the correct text representation. Can be `null` if there is no bytes representation for the token.")]
    [DataMember(Name = "bytes")]
    public byte[] Bytes { get; set; }
    
    [ApiMember(Description="List of the most likely tokens and their log probability, at this token position. In rare cases, there may be fewer than the number of requested `top_logprobs` returned.")]
    [DataMember(Name = "top_logprobs")]
    public List<LogprobItem> TopLogprobs { get; set; }
}
