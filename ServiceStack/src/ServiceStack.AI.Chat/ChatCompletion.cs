using System.ComponentModel;
using System.Runtime.Serialization;

namespace ServiceStack.AI;

/// <summary>
/// https://platform.openai.com/docs/api-reference/chat/create
/// </summary>
[Tag("AI")]
[DataContract]
[ValidateApiKey]
[Description("Chat Completions API (OpenAI-Compatible)")]
[Notes("The industry-standard, message-based interface for interfacing with Large Language Models.")]
[Route("/v1/chat/completions", "POST"),SystemJson(UseSystemJson.Never)]
public class ChatCompletion : IPost, IReturn<ChatResponse>
{
    [Description("The messages to generate chat completions for.")]
    [DataMember(Name = "messages")]
    [Input(Type = "ChatMessages", Label=""), FieldCss(Field = "col-span-12")]
    public List<AiMessage> Messages { get; set; } = [];
    
    [Description("ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API")]
    [DataMember(Name = "model")]
    [Input(Type = "combobox", EvalAllowableValues = "Chat.Models", Placeholder = "e.g. glm-4.6", Help = "ID of the model to use")]
    public string Model { get; set; }
    
    [Description("Parameters for audio output. Required when audio output is requested with modalities: [audio]")]
    [DataMember(Name = "audio")]
    public AiChatAudio? Audio { get; set; }
    
    [Description("Modify the likelihood of specified tokens appearing in the completion.")]
    [DataMember(Name = "logit_bias")]
    public Dictionary<int,int>? LogitBias { get; set; }
    
    [Description("Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")]
    [DataMember(Name = "metadata")]
    public Dictionary<string,string>? Metadata { get; set; }

    [Description("Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.")]
    [DataMember(Name = "reasoning_effort")]
    [Input(Type="combobox", EvalAllowableValues = "['low','medium','high','none','default']", Help = "Constrains effort on reasoning for reasoning models")]
    public string? ReasoningEffort { get; set; }
    
    [Description("An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.")]
    [DataMember(Name = "response_format")]
    public AiResponseFormat? ResponseFormat { get; set; }

    [Description("Specifies the processing type used for serving the request.")]
    [DataMember(Name = "service_tier")]
    [Input(Type = "combobox", EvalAllowableValues = "['auto','default']", Help = "Processing type for serving the request")]
    public string? ServiceTier { get; set; }
    
    [Description("A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.")]
    [DataMember(Name = "safety_identifier")]
    [Input(Type = "text", Placeholder = "e.g. user-id", Help = "Stable identifier to help detect policy violations")]
    public string? SafetyIdentifier { get; set; }
    
    [Description("Up to 4 sequences where the API will stop generating further tokens.")]
    [DataMember(Name = "stop")]
    [Input(Type = "tag", Max = "4", Help = "Up to 4 sequences for the API to stop generating tokens")]
    public List<string>? Stop { get; set; }
    
    [Description("Output types that you would like the model to generate. Most models are capable of generating text, which is the default:")]
    [DataMember(Name = "modalities")]
    [Input(Type = "tag", Max = "3", Help = "The output types you would like the model to generate")]
    public List<string>? Modalities { get; set; }
    
    [Description("Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.")]
    [DataMember(Name = "prompt_cache_key")]
    [Input(Type = "text", Placeholder = "e.g. my-cache-key", Help = "Used by OpenAI to cache responses for similar requests")]
    public string? PromptCacheKey { get; set; }
    
    [Description("A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.")]
    [DataMember(Name = "tools")]
    public List<Tool>? Tools { get; set; }
    
    [Description("Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.")]
    [DataMember(Name = "verbosity")]
    [Input(Type = "combobox", EvalAllowableValues = "['low','medium','high']", Placeholder = "e.g. low", Help = "Constrains verbosity of model's response")]
    public string? Verbosity { get; set; }
    
    [Description("What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.")]
    [DataMember(Name = "temperature")]
    [Input(Type = "number", Step = "0.1", Min = "0", Max = "2", Placeholder = "e.g. 0.7", Help = "Higher values more random, lower for more focus\n\n")]
    public double? Temperature { get; set; }

    [Description("An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.")]
    [DataMember(Name = "max_completion_tokens")]
    [Input(Type = "number", Value = "2048", Step = "1", Min = "1", Placeholder = "e.g. 2048", Help = "Max tokens for completion (inc. reasoning tokens)")]
    public int? MaxCompletionTokens { get; set; }

    [Description("An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.")]
    [DataMember(Name = "top_logprobs")]
    [Input(Type = "number", Step = "1", Min = "0", Max = "20", Placeholder = "e.g. 5", Help = "Number of most likely tokens to return with log probs")]
    public int? TopLogprobs { get; set; }
    
    [Description("An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.")]
    [DataMember(Name = "top_p")]
    [Input(Type = "number", Step = "0.1", Min = "0", Max = "1", Placeholder = "e.g. 0.5", Help = "Nucleus sampling - alternative to temperature")]
    public double? TopP { get; set; }

    [Description("Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.")]
    [DataMember(Name = "frequency_penalty")]
    [Input(Type = "number", Step = "0.1", Min = "0", Max = "2", Placeholder = "e.g. 0.5", Help = "Penalize tokens based on frequency in text\n\n")]
    public double? FrequencyPenalty { get; set; }
    
    [Description("Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.")]
    [DataMember(Name = "presence_penalty")]
    [Input(Type = "number", Step = "0.1", Min = "0", Max = "2", Placeholder = "e.g. 0.5", Help = "Penalize tokens based on presence in text\n\n")]
    public double? PresencePenalty { get; set; }
    
    [Description("This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.")]
    [DataMember(Name = "seed")]
    [Input(Type = "number", Placeholder = "e.g. 42", Help = "For deterministic sampling")]
    public int? Seed { get; set; }
    
    [Description("How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.")]
    [DataMember(Name = "n")]
    [Input(Type = "number", Placeholder = "e.g. 1", Help = "How many chat choices to generate for each input message")]
    public int? N { get; set; }
    
    [Description("Whether or not to store the output of this chat completion request for use in our model distillation or evals products.")]
    [Input(Type = "checkbox", Help = "Whether or not to store the output of this chat request")]
    [DataMember(Name = "store")]
    public bool? Store { get; set; }
    
    [Description("Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.")]
    [DataMember(Name = "logprobs")]
    [Input(Type = "checkbox", Help = "Whether to return log probabilities of the output tokens")]
    public bool? Logprobs { get; set; }
    
    [Description("Whether to enable parallel function calling during tool use.")]
    [DataMember(Name = "parallel_tool_calls")]
    [Input(Type = "checkbox", Help = "Enable parallel function calling during tool use")]
    public bool? ParallelToolCalls { get; set; }
    
    [Description("Whether to enable thinking mode for some Qwen models and providers.")]
    [DataMember(Name = "enable_thinking")]
    [Input(Type = "checkbox", Help = "Enable thinking mode for some Qwen providers")]
    public bool? EnableThinking { get; set; }
    
    [Description("If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.")]
    [DataMember(Name = "stream")]
    [Input(Type = "hidden", Help = "Enable streaming of partial message deltas")]
    public bool? Stream { get; set; }
}

[DataContract]
[Description("Parameters for audio output. Required when audio output is requested with modalities: [audio]")]
public class AiChatAudio
{
    [Description("Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.")]
    [DataMember(Name = "format")]
    public string Format { get; set; }

    [Description("The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.")]
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
    [System.Text.Json.Serialization.JsonIgnore]
    [Description("The type of the content part.")]
    [DataMember(Name = "type")]
    public string Type { get; set; }
}

[DataContract]
[Description("Text content part")]
public class AiTextContent : AiContent
{
    [Description("The text content.")]
    [DataMember(Name = "text")]
    public string Text { get; set; }
}

[DataContract]
[Description("Image content part")]
public class AiImageContent : AiContent
{
    [Description("The image for this content.")]
    [DataMember(Name = "image_url")]
    public AiImageUrl ImageUrl { get; set; }
}
[DataContract]
public class AiImageUrl
{
    [Description("Either a URL of the image or the base64 encoded image data.")]
    [DataMember(Name = "url")]
    public string Url { get; set; }
}

[DataContract]
[Description("Audio content part")]
public class AiAudioContent : AiContent
{
    [Description("The audio input for this content.")]
    [DataMember(Name = "input_audio")]
    public AiInputAudio InputAudio { get; set; }
}

[DataContract]
[Description("Audio content part")]
public class AiInputAudio
{
    [Description("URL or Base64 encoded audio data.")]
    [DataMember(Name = "data")]
    public string Data { get; set; }

    [Description("The format of the encoded audio data. Currently supports 'wav' and 'mp3'.")]
    [DataMember(Name = "format")]
    public string Format { get; set; }
}

[DataContract]
[Description("File content part")]
public class AiFileContent : AiContent
{
    [Description("The file input for this content.")]
    [DataMember(Name = "file")]
    public AiFile File { get; set; }
}
[DataContract]
[Description("File content part")]
public class AiFile
{
    [Description("The URL or base64 encoded file data, used when passing the file to the model as a string.")]
    [DataMember(Name = "file_data")]
    public string FileData { get; set; }

    [Description("The name of the file, used when passing the file to the model as a string.")]
    [DataMember(Name = "filename")]
    public string Filename { get; set; }

    [Description("The ID of an uploaded file to use as input.")]
    [DataMember(Name = "file_id")]
    public string? FileId { get; set; }
}

[DataContract]
[Description("A list of messages comprising the conversation so far.")]
public class AiMessage
{
    [Description("The contents of the message.")]
    [DataMember(Name = "content")]
    public List<AiContent>? Content { get; set; }
    
    [Description("The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.")]
    [DataMember(Name = "role")]
    public string Role { get; set; }
    
    [Description("An optional name for the participant. Provides the model information to differentiate between participants of the same role.")]
    [DataMember(Name = "name")]
    public string? Name { get; set; }
    
    [Description("The tool calls generated by the model, such as function calls.")]
    [DataMember(Name = "tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
    
    [Description("Tool call that this message is responding to.")]
    [DataMember(Name = "tool_call_id")]
    public string? ToolCallId { get; set; }
}

[DataContract]
public class Tool
{
    [Description("The type of the tool. Currently, only function is supported.")]
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
    [Description("The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64.")]
    [DataMember(Name = "name")]
    public string? Name { get; set; }

    [Description("A description of what the function does, used by the model to choose when and how to call the function.")]
    [DataMember(Name = "description")]
    public string? Description { get; set; }
    
    [Description("The parameters the functions accepts, described as a JSON Schema object. See the guide for examples, and the JSON Schema reference for documentation about the format.")]
    [DataMember(Name = "parameters")]
    public Dictionary<string,string>? Parameters { get; set; }
}

[DataContract]
public class AiResponseFormat
{
    public const string Text = "text";
    public const string JsonObject = "json_object";
    
    [Description("An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.")]
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

[DataContract]
public class ChatResponse
{
    [Description("A unique identifier for the chat completion.")]
    [DataMember(Name = "id")]
    public string Id { get; set; }
    
    [Description("A list of chat completion choices. Can be more than one if n is greater than 1.")]
    [DataMember(Name = "choices")]
    public List<Choice> Choices { get; set; }
    
    [Description("The Unix timestamp (in seconds) of when the chat completion was created.")]
    [DataMember(Name = "created")]
    public long Created { get; set; }
    
    [Description("The model used for the chat completion.")]
    [DataMember(Name = "model")]
    public string Model { get; set; }
    
    [Description("This fingerprint represents the backend configuration that the model runs with.")]
    [DataMember(Name = "system_fingerprint")]
    public string? SystemFingerprint { get; set; }
    
    [Description("The object type, which is always chat.completion.")]
    [DataMember(Name = "object")]
    public string Object { get; set; }
    
    [Description("Specifies the processing type used for serving the request.")]
    [DataMember(Name = "service_tier")]
    public string? ServiceTier { get; set; }
    
    [Description("Usage statistics for the completion request.")]
    [DataMember(Name = "usage")]
    public AiUsage Usage { get; set; }

    [Description("The provider used for the chat completion.")]
    [DataMember(Name = "provider")]
    public string? Provider { get; set; }
    
    [Description("Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")]
    [DataMember(Name = "metadata")]
    public Dictionary<string,string>? Metadata { get; set; }
    
    [DataMember(Name = "responseStatus")]
    public ResponseStatus? ResponseStatus { get; set; }
}

[DataContract]
[Description("Configuration options for reasoning models.")]
public class AiReasoning
{
    [Description("Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high.")]
    [DataMember(Name = "effort")]
    public string? Effort { get; set; }

    [Description("A summary of the reasoning performed by the model. This can be useful for debugging and understanding the model's reasoning process. One of auto, concise, or detailed.")]
    [DataMember(Name = "summary")]
    public string? Summary { get; set; }
}

[DataContract]
[Description("Usage statistics for the completion request.")]
public class AiUsage
{
    [Description("Number of tokens in the generated completion.")]
    [DataMember(Name = "completion_tokens")]
    public int CompletionTokens { get; set; }

    [Description("Number of tokens in the prompt.")]
    [DataMember(Name = "prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [Description("Total number of tokens used in the request (prompt + completion).")]
    [DataMember(Name = "total_tokens")]
    public int TotalTokens { get; set; }
    
    [Description("Breakdown of tokens used in a completion.")]
    [DataMember(Name = "completion_tokens_details")]
    public AiCompletionUsage? CompletionTokensDetails { get; set; }
    
    [Description("Breakdown of tokens used in the prompt.")]
    [DataMember(Name = "prompt_tokens_details")]
    public AiPromptUsage? PromptTokensDetails { get; set; }
}

[DataContract]
[Description("Usage statistics for the completion request.")]
public class AiCompletionUsage
{
    [Description("When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.\n\n")]
    [DataMember(Name = "accepted_prediction_tokens")]
    public int AcceptedPredictionTokens { get; set; }

    [Description("Audio input tokens generated by the model.")]
    [DataMember(Name = "audio_tokens")]
    public int AudioTokens { get; set; }

    [Description("Tokens generated by the model for reasoning.")]
    [DataMember(Name = "reasoning_tokens")]
    public int ReasoningTokens { get; set; }
    
    [Description("When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.")]
    [DataMember(Name = "rejected_prediction_tokens")]
    public int RejectedPredictionTokens { get; set; }
}

[DataContract]
[Description("Breakdown of tokens used in the prompt.")]
public class AiPromptUsage
{
    [Description("When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.\n\n")]
    [DataMember(Name = "accepted_prediction_tokens")]
    public int AcceptedPredictionTokens { get; set; }

    [Description("Audio input tokens present in the prompt.")]
    [DataMember(Name = "audio_tokens")]
    public int AudioTokens { get; set; }

    [Description("Cached tokens present in the prompt.")]
    [DataMember(Name = "cached_tokens")]
    public int CachedTokens { get; set; }
}

[DataContract]
public class Choice
{
    [Description("The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool")]
    [DataMember(Name = "finish_reason")]
    public string FinishReason { get; set; }

    [Description("The index of the choice in the list of choices.")]
    [DataMember(Name = "index")]
    public int Index { get; set; }
    
    [Description("A chat completion message generated by the model.")]
    [DataMember(Name = "message")]
    public ChoiceMessage Message { get; set; }
}

[DataContract]
public class ChoiceMessage
{
    [Description("The contents of the message.")]
    [DataMember(Name = "content")]
    public string Content { get; set; }

    [Description("The refusal message generated by the model.")]
    [DataMember(Name = "refusal")]
    public string? Refusal { get; set; }

    [Description("The reasoning process used by the model.")]
    [DataMember(Name = "reasoning")]
    public string? Reasoning { get; set; }
    
    [Description("The role of the author of this message.")]
    [DataMember(Name = "role")]
    public string Role { get; set; }

    [Description("Annotations for the message, when applicable, as when using the web search tool.")]
    [DataMember(Name = "annotations")]
    public List<ChoiceAnnotation>? Annotations { get; set; }

    [Description("If the audio output modality is requested, this object contains data about the audio response from the model.")]
    [DataMember(Name = "audio")]
    public ChoiceAudio? Audio { get; set; }
    
    [Description("The tool calls generated by the model, such as function calls.")]
    [DataMember(Name = "tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
}

[DataContract]
[Description("Annotations for the message, when applicable, as when using the web search tool.")]
public class ChoiceAnnotation
{
    [Description("The type of the URL citation. Always url_citation.")]
    [DataMember(Name = "type")]
    public string Type { get; set; }

    [Description("A URL citation when using web search.")]
    [DataMember(Name = "url_citation")]
    public UrlCitation UrlCitation { get; set; }
}

[DataContract]
[Description("Annotations for the message, when applicable, as when using the web search tool.")]
public class UrlCitation
{
    [Description("The index of the last character of the URL citation in the message.")]
    [DataMember(Name = "end_index")]
    public int EndIndex { get; set; }

    [Description("The index of the first character of the URL citation in the message.")]
    [DataMember(Name = "start_index")]
    public int StartIndex { get; set; }

    [Description("The title of the web resource.")]
    [DataMember(Name = "title")]
    public string Title { get; set; }

    [Description("The URL of the web resource.")]
    [DataMember(Name = "url")]
    public string Url { get; set; }
}

[DataContract]
[Description("If the audio output modality is requested, this object contains data about the audio response from the model.")]
public class ChoiceAudio
{
    [Description("Base64 encoded audio bytes generated by the model, in the format specified in the request.")]
    [DataMember(Name = "data")]
    public string Data { get; set; }

    [Description("The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.")]
    [DataMember(Name = "expires_at")]
    public int ExpiresAt { get; set; }
    
    [Description("Unique identifier for this audio response.")]
    [DataMember(Name = "id")]
    public string Id { get; set; }
    
    [Description("Transcript of the audio generated by the model.")]
    [DataMember(Name = "transcript")]
    public string Transcript { get; set; }
}

[DataContract]
[Description("The tool calls generated by the model, such as function calls.")]
public class ToolCall
{
    [Description("The ID of the tool call.")]
    [DataMember(Name = "id")]
    public string Id { get; set; }
    
    [Description("The type of the tool. Currently, only `function` is supported.")]
    [DataMember(Name = "type")]
    public string Type { get; set; }
    
    [Description("The function that the model called.")]
    [DataMember(Name = "function")]
    public string Function { get; set; }
}

[DataContract]
[Description("The function that the model called.")]
public class ToolFunction
{
    [Description("The name of the function to call.")]
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [Description("The arguments to call the function with, as generated by the model in JSON format. Note that the model does not always generate valid JSON, and may hallucinate parameters not defined by your function schema. Validate the arguments in your code before calling your function.")]
    [DataMember(Name = "arguments")]
    public string Arguments { get; set; }
}

[DataContract]
[Description("Log probability information for the choice.")]
public class Logprobs
{
    [Description("A list of message content tokens with log probability information.")]
    [DataMember(Name = "content")]
    public List<LogprobItem> Content { get; set; }
}

[DataContract]
[Description("A list of message content tokens with log probability information.")]
public class LogprobItem
{
    [Description("The token.")]
    [DataMember(Name = "token")]
    public string Token { get; set; }

    [Description("The log probability of this token, if it is within the top 20 most likely tokens. Otherwise, the value `-9999`.0 is used to signify that the token is very unlikely.")]
    [DataMember(Name = "logprob")]
    public double Logprob { get; set; }
    
    [Description("A list of integers representing the UTF-8 bytes representation of the token. Useful in instances where characters are represented by multiple tokens and their byte representations must be combined to generate the correct text representation. Can be `null` if there is no bytes representation for the token.")]
    [DataMember(Name = "bytes")]
    public byte[] Bytes { get; set; }
    
    [Description("List of the most likely tokens and their log probability, at this token position. In rare cases, there may be fewer than the number of requested `top_logprobs` returned.")]
    [DataMember(Name = "top_logprobs")]
    public List<LogprobItem> TopLogprobs { get; set; }
}
