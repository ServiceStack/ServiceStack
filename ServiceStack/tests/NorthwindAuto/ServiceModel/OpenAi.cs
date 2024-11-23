using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.Jobs;

namespace MyApp.ServiceModel;

public static class Tag
{
    public const string Tasks = nameof(Tasks);
    public const string User = nameof(User);
    public const string OpenAi = nameof(OpenAi);
    public const string Comfy = nameof(Comfy);
    public const string Info = nameof(Info);
    public const string Admin = nameof(Admin);
    public const string Jobs = nameof(Jobs);
}

[Tag(Tag.Admin)]
[ValidateAuthSecret]
public class GetWorkerStats : IGet, IReturn<GetWorkerStatsResponse> { }
public class GetWorkerStatsResponse
{
    public List<WorkerStats> Results { get; set; }
    public Dictionary<string, int> QueueCounts { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}


[Tag(ServiceModel.Tag.OpenAi)]
[ValidateApiKey]
[Route("/v1/chat/completions", "POST")]
[SystemJson(UseSystemJson.Response)]
public class OpenAiChatCompletion : OpenAiChat, IPost, IReturn<OpenAiChatResponse>
{
    [Description("Provide a unique identifier to track requests")]
    public string? RefId { get; set; }
    
    [Description("Specify which AI Provider to use")]
    public string? Provider { get; set; }
    
    [Description("Categorize like requests under a common group")]
    public string? Tag { get; set; }
}

/// <summary>
/// https://platform.openai.com/docs/api-reference/chat/create
/// </summary>
[Tag(Tag.OpenAi)]
[Description("Given a list of messages comprising a conversation, the model will return a response.")]
[DataContract]
public class OpenAiChat
{
    [Description("A list of messages comprising the conversation so far.")]
    [DataMember(Name = "messages")]
    public List<OpenAiMessage> Messages { get; set; }
    
    [Description("ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API")]
    [DataMember(Name = "model")]
    public string Model { get; set; }
    
    [Description("Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.")]
    [DataMember(Name = "frequency_penalty")]
    public double? FrequencyPenalty { get; set; }

    [Description("Modify the likelihood of specified tokens appearing in the completion.")]
    [DataMember(Name = "logit_bias")]
    public Dictionary<int,int>? LogitBias { get; set; }
    
    [Description("Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.")]
    [DataMember(Name = "logprobs")]
    public bool? LogProbs { get; set; }

    [Description("An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.")]
    [DataMember(Name = "top_logprobs")]
    public int? TopLogProbs { get; set; }

    [Description("The maximum number of tokens that can be generated in the chat completion.")]
    [DataMember(Name = "max_tokens")]
    public int? MaxTokens { get; set; }
    
    [Description("How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.")]
    [DataMember(Name = "n")]
    public int? N { get; set; }
    
    [Description("Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.")]
    [DataMember(Name = "presence_penalty")]
    public double? PresencePenalty { get; set; }
    
    [Description("An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.")]
    [DataMember(Name = "response_format")]
    public OpenAiResponseFormat? ResponseFormat { get; set; }
    
    [Description("This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.")]
    [DataMember(Name = "seed")]
    public int? Seed { get; set; }
    
    [Description("Up to 4 sequences where the API will stop generating further tokens.")]
    [DataMember(Name = "stop")]
    public List<string>? Stop { get; set; }
    
    [Description("If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.")]
    [DataMember(Name = "stream")]
    public bool? Stream { get; set; }
    
    [Description("What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.")]
    [DataMember(Name = "temperature")]
    public double? Temperature { get; set; }
    
    [Description("An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.")]
    [DataMember(Name = "top_p")]
    public double? TopP { get; set; }
    
    [Description("A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.")]
    [DataMember(Name = "tools")]
    public List<OpenAiTools>? Tools { get; set; }

    [Description("A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.")]
    [DataMember(Name = "user")]
    public string? User { get; set; }
}

[Description("A list of messages comprising the conversation so far.")]
[DataContract]
public class OpenAiMessage
{
    [Description("The contents of the message.")]
    [DataMember(Name = "content")]
    public string Content { get; set; }
    
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
public class OpenAiTools
{
    [Description("The type of the tool. Currently, only function is supported.")]
    [DataMember(Name = "type")]
    public OpenAiToolType Type { get; set; }
}

public enum OpenAiToolType
{
    [EnumMember(Value = "function")]
    Function,
}

[DataContract]
public class OpenAiToolFunction
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
public class OpenAiResponseFormat
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
public class OpenAiChatResponse
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
    public string SystemFingerprint { get; set; }
    
    [Description("The object type, which is always chat.completion.")]
    [DataMember(Name = "object")]
    public string Object { get; set; }
    
    [Description("Usage statistics for the completion request.")]
    [DataMember(Name = "usage")]
    public OpenAiUsage Usage { get; set; }
    
    [DataMember(Name = "responseStatus")]
    public ResponseStatus? ResponseStatus { get; set; }
}

[Description("Usage statistics for the completion request.")]
[DataContract]
public class OpenAiUsage
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
}

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
    
    [Description("The tool calls generated by the model, such as function calls.")]
    [DataMember(Name = "tool_calls")]
    public ToolCall[] ToolCalls { get; set; }

    [Description("The role of the author of this message.")]
    [DataMember(Name = "role")]
    public string Role { get; set; }
}

[Description("The tool calls generated by the model, such as function calls.")]
[DataContract]
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

[Description("The function that the model called.")]
[DataContract]
public class ToolFunction
{
    [Description("The name of the function to call.")]
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [Description("The arguments to call the function with, as generated by the model in JSON format. Note that the model does not always generate valid JSON, and may hallucinate parameters not defined by your function schema. Validate the arguments in your code before calling your function.")]
    [DataMember(Name = "arguments")]
    public string Arguments { get; set; }
}

[Description("Log probability information for the choice.")]
[DataContract]
public class Logprobs
{
    [Description("A list of message content tokens with log probability information.")]
    [DataMember(Name = "content")]
    public LogprobItem[] Content { get; set; }
}

[Description("A list of message content tokens with log probability information.")]
[DataContract]
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
    public LogprobItem[] TopLogprobs { get; set; }
}
