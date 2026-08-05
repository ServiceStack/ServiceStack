/* Options:
Date: 2026-08-06 02:13:20
Version: 10.09
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:5000

Package: org.example
//GlobalNamespace: dtos
//AddPropertyAccessors: True
//SettersReturnThis: True
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddDescriptionAsComments: True
//AddImplicitVersion: 
IncludeTypes: {AI}
//ExcludeTypes: 
//TreatTypesAsStrings: 
//DefaultImports: java.math.*,java.util.*,java.io.InputStream,net.servicestack.client.*,com.google.gson.annotations.*,com.google.gson.reflect.*
*/

package org.example;

import java.math.*;
import java.util.*;
import java.io.InputStream;
import net.servicestack.client.*;
import com.google.gson.annotations.*;
import com.google.gson.reflect.*;

public class dtos
{

    /**
    * Chat Completions API (OpenAI-Compatible)
    */
    @Route(Path="/v1/chat/completions", Verbs="POST")
    @DataContract
    public static class ChatCompletion implements IReturn<ChatResponse>, IPost
    {
        /**
        * The messages to generate chat completions for.
        */
        @DataMember(Name="messages")
        @SerializedName("messages")
        public ArrayList<AiMessage> messages = new ArrayList<AiMessage>();

        /**
        * ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API
        */
        @DataMember(Name="model")
        @SerializedName("model")
        public String model = null;

        /**
        * Parameters for audio output. Required when audio output is requested with modalities: [audio]
        */
        @DataMember(Name="audio")
        @SerializedName("audio")
        public AiChatAudio audio = null;

        /**
        * Modify the likelihood of specified tokens appearing in the completion.
        */
        @DataMember(Name="logit_bias")
        @SerializedName("logit_bias")
        public HashMap<Integer,Integer> logitBias = null;

        /**
        * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
        */
        @DataMember(Name="metadata")
        @SerializedName("metadata")
        public HashMap<String,String> metadata = null;

        /**
        * Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.
        */
        @DataMember(Name="reasoning_effort")
        @SerializedName("reasoning_effort")
        public String reasoningEffort = null;

        /**
        * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.
        */
        @DataMember(Name="response_format")
        @SerializedName("response_format")
        public AiResponseFormat responseFormat = null;

        /**
        * Specifies the processing type used for serving the request.
        */
        @DataMember(Name="service_tier")
        @SerializedName("service_tier")
        public String serviceTier = null;

        /**
        * A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.
        */
        @DataMember(Name="safety_identifier")
        @SerializedName("safety_identifier")
        public String safetyIdentifier = null;

        /**
        * Up to 4 sequences where the API will stop generating further tokens.
        */
        @DataMember(Name="stop")
        @SerializedName("stop")
        public ArrayList<String> stop = null;

        /**
        * Output types that you would like the model to generate. Most models are capable of generating text, which is the default:
        */
        @DataMember(Name="modalities")
        @SerializedName("modalities")
        public ArrayList<String> modalities = null;

        /**
        * Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.
        */
        @DataMember(Name="prompt_cache_key")
        @SerializedName("prompt_cache_key")
        public String promptCacheKey = null;

        /**
        * A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.
        */
        @DataMember(Name="tools")
        @SerializedName("tools")
        public ArrayList<Tool> tools = null;

        /**
        * Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.
        */
        @DataMember(Name="verbosity")
        @SerializedName("verbosity")
        public String verbosity = null;

        /**
        * What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
        */
        @DataMember(Name="temperature")
        @SerializedName("temperature")
        public Double temperature = null;

        /**
        * An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.
        */
        @DataMember(Name="max_completion_tokens")
        @SerializedName("max_completion_tokens")
        public Integer maxCompletionTokens = null;

        /**
        * An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
        */
        @DataMember(Name="top_logprobs")
        @SerializedName("top_logprobs")
        public Integer topLogprobs = null;

        /**
        * An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.
        */
        @DataMember(Name="top_p")
        @SerializedName("top_p")
        public Double topP = null;

        /**
        * Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
        */
        @DataMember(Name="frequency_penalty")
        @SerializedName("frequency_penalty")
        public Double frequencyPenalty = null;

        /**
        * Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
        */
        @DataMember(Name="presence_penalty")
        @SerializedName("presence_penalty")
        public Double presencePenalty = null;

        /**
        * This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.
        */
        @DataMember(Name="seed")
        @SerializedName("seed")
        public Integer seed = null;

        /**
        * How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.
        */
        @DataMember(Name="n")
        @SerializedName("n")
        public Integer n = null;

        /**
        * Whether or not to store the output of this chat completion request for use in our model distillation or evals products.
        */
        @DataMember(Name="store")
        @SerializedName("store")
        public Boolean store = null;

        /**
        * Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
        */
        @DataMember(Name="logprobs")
        @SerializedName("logprobs")
        public Boolean logprobs = null;

        /**
        * Whether to enable parallel function calling during tool use.
        */
        @DataMember(Name="parallel_tool_calls")
        @SerializedName("parallel_tool_calls")
        public Boolean parallelToolCalls = null;

        /**
        * Whether to enable thinking mode for some Qwen models and providers.
        */
        @DataMember(Name="enable_thinking")
        @SerializedName("enable_thinking")
        public Boolean enableThinking = null;

        /**
        * If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.
        */
        @DataMember(Name="stream")
        @SerializedName("stream")
        public Boolean stream = null;
        
        public ArrayList<AiMessage> getMessages() { return messages; }
        public ChatCompletion setMessages(ArrayList<AiMessage> value) { this.messages = value; return this; }
        public String getModel() { return model; }
        public ChatCompletion setModel(String value) { this.model = value; return this; }
        public AiChatAudio getAudio() { return audio; }
        public ChatCompletion setAudio(AiChatAudio value) { this.audio = value; return this; }
        public HashMap<Integer,Integer> getLogitBias() { return logitBias; }
        public ChatCompletion setLogitBias(HashMap<Integer,Integer> value) { this.logitBias = value; return this; }
        public HashMap<String,String> getMetadata() { return metadata; }
        public ChatCompletion setMetadata(HashMap<String,String> value) { this.metadata = value; return this; }
        public String getReasoningEffort() { return reasoningEffort; }
        public ChatCompletion setReasoningEffort(String value) { this.reasoningEffort = value; return this; }
        public AiResponseFormat getResponseFormat() { return responseFormat; }
        public ChatCompletion setResponseFormat(AiResponseFormat value) { this.responseFormat = value; return this; }
        public String getServiceTier() { return serviceTier; }
        public ChatCompletion setServiceTier(String value) { this.serviceTier = value; return this; }
        public String getSafetyIdentifier() { return safetyIdentifier; }
        public ChatCompletion setSafetyIdentifier(String value) { this.safetyIdentifier = value; return this; }
        public ArrayList<String> getStop() { return stop; }
        public ChatCompletion setStop(ArrayList<String> value) { this.stop = value; return this; }
        public ArrayList<String> getModalities() { return modalities; }
        public ChatCompletion setModalities(ArrayList<String> value) { this.modalities = value; return this; }
        public String getPromptCacheKey() { return promptCacheKey; }
        public ChatCompletion setPromptCacheKey(String value) { this.promptCacheKey = value; return this; }
        public ArrayList<Tool> getTools() { return tools; }
        public ChatCompletion setTools(ArrayList<Tool> value) { this.tools = value; return this; }
        public String getVerbosity() { return verbosity; }
        public ChatCompletion setVerbosity(String value) { this.verbosity = value; return this; }
        public Double getTemperature() { return temperature; }
        public ChatCompletion setTemperature(Double value) { this.temperature = value; return this; }
        public Integer getMaxCompletionTokens() { return maxCompletionTokens; }
        public ChatCompletion setMaxCompletionTokens(Integer value) { this.maxCompletionTokens = value; return this; }
        public Integer getTopLogprobs() { return topLogprobs; }
        public ChatCompletion setTopLogprobs(Integer value) { this.topLogprobs = value; return this; }
        public Double getTopP() { return topP; }
        public ChatCompletion setTopP(Double value) { this.topP = value; return this; }
        public Double getFrequencyPenalty() { return frequencyPenalty; }
        public ChatCompletion setFrequencyPenalty(Double value) { this.frequencyPenalty = value; return this; }
        public Double getPresencePenalty() { return presencePenalty; }
        public ChatCompletion setPresencePenalty(Double value) { this.presencePenalty = value; return this; }
        public Integer getSeed() { return seed; }
        public ChatCompletion setSeed(Integer value) { this.seed = value; return this; }
        public Integer getN() { return n; }
        public ChatCompletion setN(Integer value) { this.n = value; return this; }
        public Boolean isStore() { return store; }
        public ChatCompletion setStore(Boolean value) { this.store = value; return this; }
        public Boolean isLogprobs() { return logprobs; }
        public ChatCompletion setLogprobs(Boolean value) { this.logprobs = value; return this; }
        public Boolean isParallelToolCalls() { return parallelToolCalls; }
        public ChatCompletion setParallelToolCalls(Boolean value) { this.parallelToolCalls = value; return this; }
        public Boolean isEnableThinking() { return enableThinking; }
        public ChatCompletion setEnableThinking(Boolean value) { this.enableThinking = value; return this; }
        public Boolean isStream() { return stream; }
        public ChatCompletion setStream(Boolean value) { this.stream = value; return this; }
        private static Object responseType = ChatResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class ChatResponse
    {
        /**
        * A unique identifier for the chat completion.
        */
        @DataMember(Name="id")
        @SerializedName("id")
        public String id = null;

        /**
        * A list of chat completion choices. Can be more than one if n is greater than 1.
        */
        @DataMember(Name="choices")
        @SerializedName("choices")
        public ArrayList<Choice> choices = new ArrayList<Choice>();

        /**
        * The Unix timestamp (in seconds) of when the chat completion was created.
        */
        @DataMember(Name="created")
        @SerializedName("created")
        public Long created = null;

        /**
        * The model used for the chat completion.
        */
        @DataMember(Name="model")
        @SerializedName("model")
        public String model = null;

        /**
        * This fingerprint represents the backend configuration that the model runs with.
        */
        @DataMember(Name="system_fingerprint")
        @SerializedName("system_fingerprint")
        public String systemFingerprint = null;

        /**
        * The object type, which is always chat.completion.
        */
        @DataMember(Name="object")
        @SerializedName("object")
        public String object = null;

        /**
        * Specifies the processing type used for serving the request.
        */
        @DataMember(Name="service_tier")
        @SerializedName("service_tier")
        public String serviceTier = null;

        /**
        * Usage statistics for the completion request.
        */
        @DataMember(Name="usage")
        @SerializedName("usage")
        public AiUsage usage = null;

        /**
        * The provider used for the chat completion.
        */
        @DataMember(Name="provider")
        @SerializedName("provider")
        public String provider = null;

        /**
        * Total cost of the completion in USD, accumulated across every request in the tool loop.
        */
        @DataMember(Name="cost")
        @SerializedName("cost")
        public Double cost = null;

        /**
        * The assistant and tool messages exchanged during the tool-execution loop, in order.
        */
        @DataMember(Name="tool_history")
        @SerializedName("tool_history")
        public ArrayList<ChoiceMessage> toolHistory = null;

        /**
        * Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
        */
        @DataMember(Name="metadata")
        @SerializedName("metadata")
        public HashMap<String,String> metadata = null;

        @DataMember(Name="responseStatus")
        @SerializedName("responseStatus")
        public ResponseStatus responseStatus = null;
        
        public String getId() { return id; }
        public ChatResponse setId(String value) { this.id = value; return this; }
        public ArrayList<Choice> getChoices() { return choices; }
        public ChatResponse setChoices(ArrayList<Choice> value) { this.choices = value; return this; }
        public Long getCreated() { return created; }
        public ChatResponse setCreated(Long value) { this.created = value; return this; }
        public String getModel() { return model; }
        public ChatResponse setModel(String value) { this.model = value; return this; }
        public String getSystemFingerprint() { return systemFingerprint; }
        public ChatResponse setSystemFingerprint(String value) { this.systemFingerprint = value; return this; }
        public String getObject() { return object; }
        public ChatResponse setObject(String value) { this.object = value; return this; }
        public String getServiceTier() { return serviceTier; }
        public ChatResponse setServiceTier(String value) { this.serviceTier = value; return this; }
        public AiUsage getUsage() { return usage; }
        public ChatResponse setUsage(AiUsage value) { this.usage = value; return this; }
        public String getProvider() { return provider; }
        public ChatResponse setProvider(String value) { this.provider = value; return this; }
        public Double getCost() { return cost; }
        public ChatResponse setCost(Double value) { this.cost = value; return this; }
        public ArrayList<ChoiceMessage> getToolHistory() { return toolHistory; }
        public ChatResponse setToolHistory(ArrayList<ChoiceMessage> value) { this.toolHistory = value; return this; }
        public HashMap<String,String> getMetadata() { return metadata; }
        public ChatResponse setMetadata(HashMap<String,String> value) { this.metadata = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public ChatResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    /**
    * A list of messages comprising the conversation so far.
    */
    @DataContract
    public static class AiMessage
    {
        /**
        * The contents of the message.
        */
        @DataMember(Name="content")
        @SerializedName("content")
        public ArrayList<AiContent> content = null;

        /**
        * The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.
        */
        @DataMember(Name="role")
        @SerializedName("role")
        public String role = null;

        /**
        * An optional name for the participant. Provides the model information to differentiate between participants of the same role.
        */
        @DataMember(Name="name")
        @SerializedName("name")
        public String name = null;

        /**
        * The tool calls generated by the model, such as function calls.
        */
        @DataMember(Name="tool_calls")
        @SerializedName("tool_calls")
        public ArrayList<ToolCall> toolCalls = null;

        /**
        * Tool call that this message is responding to.
        */
        @DataMember(Name="tool_call_id")
        @SerializedName("tool_call_id")
        public String toolCallId = null;

        /**
        * The reasoning an assistant message was generated with, normalized per provider when replayed as history.
        */
        @DataMember(Name="reasoning")
        @SerializedName("reasoning")
        public String reasoning = null;

        /**
        * The reasoning an assistant message was generated with, as emitted by Gemini and most OpenAI-compatible providers.
        */
        @DataMember(Name="reasoning_content")
        @SerializedName("reasoning_content")
        public String reasoningContent = null;

        /**
        * Unix timestamp (in milliseconds) the message was generated.
        */
        @DataMember(Name="timestamp")
        @SerializedName("timestamp")
        public Long timestamp = null;

        /**
        * Images attached to the message. Folded into `content` parts before sending to a provider.
        */
        @DataMember(Name="images")
        @SerializedName("images")
        public ArrayList<AiContent> images = null;
        
        public ArrayList<AiContent> getContent() { return content; }
        public AiMessage setContent(ArrayList<AiContent> value) { this.content = value; return this; }
        public String getRole() { return role; }
        public AiMessage setRole(String value) { this.role = value; return this; }
        public String getName() { return name; }
        public AiMessage setName(String value) { this.name = value; return this; }
        public ArrayList<ToolCall> getToolCalls() { return toolCalls; }
        public AiMessage setToolCalls(ArrayList<ToolCall> value) { this.toolCalls = value; return this; }
        public String getToolCallId() { return toolCallId; }
        public AiMessage setToolCallId(String value) { this.toolCallId = value; return this; }
        public String getReasoning() { return reasoning; }
        public AiMessage setReasoning(String value) { this.reasoning = value; return this; }
        public String getReasoningContent() { return reasoningContent; }
        public AiMessage setReasoningContent(String value) { this.reasoningContent = value; return this; }
        public Long getTimestamp() { return timestamp; }
        public AiMessage setTimestamp(Long value) { this.timestamp = value; return this; }
        public ArrayList<AiContent> getImages() { return images; }
        public AiMessage setImages(ArrayList<AiContent> value) { this.images = value; return this; }
    }

    /**
    * Parameters for audio output. Required when audio output is requested with modalities: [audio]
    */
    @DataContract
    public static class AiChatAudio
    {
        /**
        * Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.
        */
        @DataMember(Name="format")
        @SerializedName("format")
        public String format = null;

        /**
        * The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.
        */
        @DataMember(Name="voice")
        @SerializedName("voice")
        public String voice = null;
        
        public String getFormat() { return format; }
        public AiChatAudio setFormat(String value) { this.format = value; return this; }
        public String getVoice() { return voice; }
        public AiChatAudio setVoice(String value) { this.voice = value; return this; }
    }

    @DataContract
    public static class AiResponseFormat
    {
        /**
        * An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.
        */
        @DataMember(Name="type")
        @SerializedName("type")
        public ResponseFormat type = null;
        
        public ResponseFormat getType() { return type; }
        public AiResponseFormat setType(ResponseFormat value) { this.type = value; return this; }
    }

    @DataContract
    public static class Tool
    {
        /**
        * The type of the tool. Currently, only function is supported.
        */
        @DataMember(Name="type")
        @SerializedName("type")
        public ToolType type = null;

        /**
        * The function definition the model may call.
        */
        @DataMember(Name="function")
        @SerializedName("function")
        public AiToolFunction function = null;
        
        public ToolType getType() { return type; }
        public Tool setType(ToolType value) { this.type = value; return this; }
        public AiToolFunction getFunction() { return function; }
        public Tool setFunction(AiToolFunction value) { this.function = value; return this; }
    }

    @DataContract
    public static class Choice
    {
        /**
        * The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool
        */
        @DataMember(Name="finish_reason")
        @SerializedName("finish_reason")
        public String finishReason = null;

        /**
        * The index of the choice in the list of choices.
        */
        @DataMember(Name="index")
        @SerializedName("index")
        public Integer index = null;

        /**
        * A chat completion message generated by the model.
        */
        @DataMember(Name="message")
        @SerializedName("message")
        public ChoiceMessage message = null;

        /**
        * Log probability information for the choice.
        */
        @DataMember(Name="logprobs")
        @SerializedName("logprobs")
        public Logprobs logprobs = null;
        
        public String getFinishReason() { return finishReason; }
        public Choice setFinishReason(String value) { this.finishReason = value; return this; }
        public Integer getIndex() { return index; }
        public Choice setIndex(Integer value) { this.index = value; return this; }
        public ChoiceMessage getMessage() { return message; }
        public Choice setMessage(ChoiceMessage value) { this.message = value; return this; }
        public Logprobs getLogprobs() { return logprobs; }
        public Choice setLogprobs(Logprobs value) { this.logprobs = value; return this; }
    }

    /**
    * Usage statistics for the completion request.
    */
    @DataContract
    public static class AiUsage
    {
        /**
        * Number of tokens in the generated completion.
        */
        @DataMember(Name="completion_tokens")
        @SerializedName("completion_tokens")
        public Long completionTokens = null;

        /**
        * Number of tokens in the prompt.
        */
        @DataMember(Name="prompt_tokens")
        @SerializedName("prompt_tokens")
        public Long promptTokens = null;

        /**
        * Total number of tokens used in the request (prompt + completion).
        */
        @DataMember(Name="total_tokens")
        @SerializedName("total_tokens")
        public Long totalTokens = null;

        /**
        * Breakdown of tokens used in a completion.
        */
        @DataMember(Name="completion_tokens_details")
        @SerializedName("completion_tokens_details")
        public AiCompletionUsage completionTokensDetails = null;

        /**
        * Breakdown of tokens used in the prompt.
        */
        @DataMember(Name="prompt_tokens_details")
        @SerializedName("prompt_tokens_details")
        public AiPromptUsage promptTokensDetails = null;

        /**
        * Seconds spent servicing the completion, including every request in the tool loop.
        */
        @DataMember(Name="duration")
        @SerializedName("duration")
        public Long duration = null;
        
        public Long getCompletionTokens() { return completionTokens; }
        public AiUsage setCompletionTokens(Long value) { this.completionTokens = value; return this; }
        public Long getPromptTokens() { return promptTokens; }
        public AiUsage setPromptTokens(Long value) { this.promptTokens = value; return this; }
        public Long getTotalTokens() { return totalTokens; }
        public AiUsage setTotalTokens(Long value) { this.totalTokens = value; return this; }
        public AiCompletionUsage getCompletionTokensDetails() { return completionTokensDetails; }
        public AiUsage setCompletionTokensDetails(AiCompletionUsage value) { this.completionTokensDetails = value; return this; }
        public AiPromptUsage getPromptTokensDetails() { return promptTokensDetails; }
        public AiUsage setPromptTokensDetails(AiPromptUsage value) { this.promptTokensDetails = value; return this; }
        public Long getDuration() { return duration; }
        public AiUsage setDuration(Long value) { this.duration = value; return this; }
    }

    @DataContract
    public static class ChoiceMessage
    {
        /**
        * The contents of the message.
        */
        @DataMember(Name="content")
        @SerializedName("content")
        public String content = null;

        /**
        * The refusal message generated by the model.
        */
        @DataMember(Name="refusal")
        @SerializedName("refusal")
        public String refusal = null;

        /**
        * The reasoning process used by the model.
        */
        @DataMember(Name="reasoning")
        @SerializedName("reasoning")
        public String reasoning = null;

        /**
        * The reasoning process used by the model, as emitted by Gemini and most OpenAI-compatible providers.
        */
        @DataMember(Name="reasoning_content")
        @SerializedName("reasoning_content")
        public String reasoningContent = null;

        /**
        * The reasoning process used by the model, as emitted by Anthropic.
        */
        @DataMember(Name="thinking")
        @SerializedName("thinking")
        public String thinking = null;

        /**
        * The role of the author of this message.
        */
        @DataMember(Name="role")
        @SerializedName("role")
        public String role = null;

        /**
        * Unix timestamp (in milliseconds) the message was generated.
        */
        @DataMember(Name="timestamp")
        @SerializedName("timestamp")
        public Long timestamp = null;

        /**
        * The tool call this message is responding to, set on `tool` role messages in tool_history.
        */
        @DataMember(Name="tool_call_id")
        @SerializedName("tool_call_id")
        public String toolCallId = null;

        /**
        * Images generated by the model or produced by a tool call.
        */
        @DataMember(Name="images")
        @SerializedName("images")
        public ArrayList<AiContent> images = null;

        /**
        * Audio generated by the model or produced by a tool call.
        */
        @DataMember(Name="audios")
        @SerializedName("audios")
        public ArrayList<AiContent> audios = null;

        /**
        * Files produced by a tool call.
        */
        @DataMember(Name="files")
        @SerializedName("files")
        public ArrayList<AiContent> files = null;

        /**
        * Annotations for the message, when applicable, as when using the web search tool.
        */
        @DataMember(Name="annotations")
        @SerializedName("annotations")
        public ArrayList<ChoiceAnnotation> annotations = null;

        /**
        * If the audio output modality is requested, this object contains data about the audio response from the model.
        */
        @DataMember(Name="audio")
        @SerializedName("audio")
        public ChoiceAudio audio = null;

        /**
        * The tool calls generated by the model, such as function calls.
        */
        @DataMember(Name="tool_calls")
        @SerializedName("tool_calls")
        public ArrayList<ToolCall> toolCalls = null;
        
        public String getContent() { return content; }
        public ChoiceMessage setContent(String value) { this.content = value; return this; }
        public String getRefusal() { return refusal; }
        public ChoiceMessage setRefusal(String value) { this.refusal = value; return this; }
        public String getReasoning() { return reasoning; }
        public ChoiceMessage setReasoning(String value) { this.reasoning = value; return this; }
        public String getReasoningContent() { return reasoningContent; }
        public ChoiceMessage setReasoningContent(String value) { this.reasoningContent = value; return this; }
        public String getThinking() { return thinking; }
        public ChoiceMessage setThinking(String value) { this.thinking = value; return this; }
        public String getRole() { return role; }
        public ChoiceMessage setRole(String value) { this.role = value; return this; }
        public Long getTimestamp() { return timestamp; }
        public ChoiceMessage setTimestamp(Long value) { this.timestamp = value; return this; }
        public String getToolCallId() { return toolCallId; }
        public ChoiceMessage setToolCallId(String value) { this.toolCallId = value; return this; }
        public ArrayList<AiContent> getImages() { return images; }
        public ChoiceMessage setImages(ArrayList<AiContent> value) { this.images = value; return this; }
        public ArrayList<AiContent> getAudios() { return audios; }
        public ChoiceMessage setAudios(ArrayList<AiContent> value) { this.audios = value; return this; }
        public ArrayList<AiContent> getFiles() { return files; }
        public ChoiceMessage setFiles(ArrayList<AiContent> value) { this.files = value; return this; }
        public ArrayList<ChoiceAnnotation> getAnnotations() { return annotations; }
        public ChoiceMessage setAnnotations(ArrayList<ChoiceAnnotation> value) { this.annotations = value; return this; }
        public ChoiceAudio getAudio() { return audio; }
        public ChoiceMessage setAudio(ChoiceAudio value) { this.audio = value; return this; }
        public ArrayList<ToolCall> getToolCalls() { return toolCalls; }
        public ChoiceMessage setToolCalls(ArrayList<ToolCall> value) { this.toolCalls = value; return this; }
    }

    @DataContract
    public static class AiContent
    {
        /**
        * The type of the content part.
        */
        @DataMember(Name="type")
        @SerializedName("type")
        public String type = null;
        
        public String getType() { return type; }
        public AiContent setType(String value) { this.type = value; return this; }
    }

    /**
    * The tool calls generated by the model, such as function calls.
    */
    @DataContract
    public static class ToolCall
    {
        /**
        * The ID of the tool call.
        */
        @DataMember(Name="id")
        @SerializedName("id")
        public String id = null;

        /**
        * The type of the tool. Currently, only `function` is supported.
        */
        @DataMember(Name="type")
        @SerializedName("type")
        public String type = null;

        /**
        * The function that the model called.
        */
        @DataMember(Name="function")
        @SerializedName("function")
        public ToolFunction function = null;
        
        public String getId() { return id; }
        public ToolCall setId(String value) { this.id = value; return this; }
        public String getType() { return type; }
        public ToolCall setType(String value) { this.type = value; return this; }
        public ToolFunction getFunction() { return function; }
        public ToolCall setFunction(ToolFunction value) { this.function = value; return this; }
    }

    public static enum ResponseFormat
    {
        Text,
        JsonObject;
    }

    public static enum ToolType
    {
        Function;
    }

    @DataContract
    public static class AiToolFunction
    {
        /**
        * The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum length of 64.
        */
        @DataMember(Name="name")
        @SerializedName("name")
        public String name = null;

        /**
        * A description of what the function does, used by the model to choose when and how to call the function.
        */
        @DataMember(Name="description")
        @SerializedName("description")
        public String description = null;

        /**
        * The parameters the functions accepts, described as a JSON Schema object. See the guide for examples, and the JSON Schema reference for documentation about the format.
        */
        @DataMember(Name="parameters")
        @SerializedName("parameters")
        public HashMap<String,Object> parameters = null;
        
        public String getName() { return name; }
        public AiToolFunction setName(String value) { this.name = value; return this; }
        public String getDescription() { return description; }
        public AiToolFunction setDescription(String value) { this.description = value; return this; }
        public HashMap<String,Object> getParameters() { return parameters; }
        public AiToolFunction setParameters(HashMap<String,Object> value) { this.parameters = value; return this; }
    }

    /**
    * Log probability information for the choice.
    */
    @DataContract
    public static class Logprobs
    {
        /**
        * A list of message content tokens with log probability information.
        */
        @DataMember(Name="content")
        @SerializedName("content")
        public ArrayList<LogprobItem> content = new ArrayList<LogprobItem>();
        
        public ArrayList<LogprobItem> getContent() { return content; }
        public Logprobs setContent(ArrayList<LogprobItem> value) { this.content = value; return this; }
    }

    /**
    * Usage statistics for the completion request.
    */
    @DataContract
    public static class AiCompletionUsage
    {
        /**
        * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
        */
        @DataMember(Name="accepted_prediction_tokens")
        @SerializedName("accepted_prediction_tokens")
        public Long acceptedPredictionTokens = null;

        /**
        * Audio input tokens generated by the model.
        */
        @DataMember(Name="audio_tokens")
        @SerializedName("audio_tokens")
        public Long audioTokens = null;

        /**
        * Tokens generated by the model for reasoning.
        */
        @DataMember(Name="reasoning_tokens")
        @SerializedName("reasoning_tokens")
        public Long reasoningTokens = null;

        /**
        * When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.
        */
        @DataMember(Name="rejected_prediction_tokens")
        @SerializedName("rejected_prediction_tokens")
        public Long rejectedPredictionTokens = null;
        
        public Long getAcceptedPredictionTokens() { return acceptedPredictionTokens; }
        public AiCompletionUsage setAcceptedPredictionTokens(Long value) { this.acceptedPredictionTokens = value; return this; }
        public Long getAudioTokens() { return audioTokens; }
        public AiCompletionUsage setAudioTokens(Long value) { this.audioTokens = value; return this; }
        public Long getReasoningTokens() { return reasoningTokens; }
        public AiCompletionUsage setReasoningTokens(Long value) { this.reasoningTokens = value; return this; }
        public Long getRejectedPredictionTokens() { return rejectedPredictionTokens; }
        public AiCompletionUsage setRejectedPredictionTokens(Long value) { this.rejectedPredictionTokens = value; return this; }
    }

    /**
    * Breakdown of tokens used in the prompt.
    */
    @DataContract
    public static class AiPromptUsage
    {
        /**
        * When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
        */
        @DataMember(Name="accepted_prediction_tokens")
        @SerializedName("accepted_prediction_tokens")
        public Long acceptedPredictionTokens = null;

        /**
        * Audio input tokens present in the prompt.
        */
        @DataMember(Name="audio_tokens")
        @SerializedName("audio_tokens")
        public Long audioTokens = null;

        /**
        * Cached tokens present in the prompt.
        */
        @DataMember(Name="cached_tokens")
        @SerializedName("cached_tokens")
        public Long cachedTokens = null;
        
        public Long getAcceptedPredictionTokens() { return acceptedPredictionTokens; }
        public AiPromptUsage setAcceptedPredictionTokens(Long value) { this.acceptedPredictionTokens = value; return this; }
        public Long getAudioTokens() { return audioTokens; }
        public AiPromptUsage setAudioTokens(Long value) { this.audioTokens = value; return this; }
        public Long getCachedTokens() { return cachedTokens; }
        public AiPromptUsage setCachedTokens(Long value) { this.cachedTokens = value; return this; }
    }

    /**
    * Annotations for the message, when applicable, as when using the web search tool.
    */
    @DataContract
    public static class ChoiceAnnotation
    {
        /**
        * The type of the URL citation. Always url_citation.
        */
        @DataMember(Name="type")
        @SerializedName("type")
        public String type = null;

        /**
        * A URL citation when using web search.
        */
        @DataMember(Name="url_citation")
        @SerializedName("url_citation")
        public UrlCitation urlCitation = null;
        
        public String getType() { return type; }
        public ChoiceAnnotation setType(String value) { this.type = value; return this; }
        public UrlCitation getUrlCitation() { return urlCitation; }
        public ChoiceAnnotation setUrlCitation(UrlCitation value) { this.urlCitation = value; return this; }
    }

    /**
    * If the audio output modality is requested, this object contains data about the audio response from the model.
    */
    @DataContract
    public static class ChoiceAudio
    {
        /**
        * Base64 encoded audio bytes generated by the model, in the format specified in the request.
        */
        @DataMember(Name="data")
        @SerializedName("data")
        public String data = null;

        /**
        * The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.
        */
        @DataMember(Name="expires_at")
        @SerializedName("expires_at")
        public Long expiresAt = null;

        /**
        * Unique identifier for this audio response.
        */
        @DataMember(Name="id")
        @SerializedName("id")
        public String id = null;

        /**
        * Transcript of the audio generated by the model.
        */
        @DataMember(Name="transcript")
        @SerializedName("transcript")
        public String transcript = null;
        
        public String getData() { return data; }
        public ChoiceAudio setData(String value) { this.data = value; return this; }
        public Long getExpiresAt() { return expiresAt; }
        public ChoiceAudio setExpiresAt(Long value) { this.expiresAt = value; return this; }
        public String getId() { return id; }
        public ChoiceAudio setId(String value) { this.id = value; return this; }
        public String getTranscript() { return transcript; }
        public ChoiceAudio setTranscript(String value) { this.transcript = value; return this; }
    }

    /**
    * Text content part
    */
    @DataContract
    public static class AiTextContent extends AiContent
    {
        /**
        * The text content.
        */
        @DataMember(Name="text")
        @SerializedName("text")
        public String text = null;
        
        public String getText() { return text; }
        public AiTextContent setText(String value) { this.text = value; return this; }
    }

    /**
    * Image content part
    */
    @DataContract
    public static class AiImageContent extends AiContent
    {
        /**
        * The image for this content.
        */
        @DataMember(Name="image_url")
        @SerializedName("image_url")
        public AiImageUrl imageUrl = null;
        
        public AiImageUrl getImageUrl() { return imageUrl; }
        public AiImageContent setImageUrl(AiImageUrl value) { this.imageUrl = value; return this; }
    }

    /**
    * Audio content part
    */
    @DataContract
    public static class AiAudioContent extends AiContent
    {
        /**
        * The audio input for this content.
        */
        @DataMember(Name="input_audio")
        @SerializedName("input_audio")
        public AiInputAudio inputAudio = null;
        
        public AiInputAudio getInputAudio() { return inputAudio; }
        public AiAudioContent setInputAudio(AiInputAudio value) { this.inputAudio = value; return this; }
    }

    /**
    * File content part
    */
    @DataContract
    public static class AiFileContent extends AiContent
    {
        /**
        * The file input for this content.
        */
        @DataMember(Name="file")
        @SerializedName("file")
        public AiFile file = null;
        
        public AiFile getFile() { return file; }
        public AiFileContent setFile(AiFile value) { this.file = value; return this; }
    }

    /**
    * Generated audio content part, referenced by URL (emitted by tool calls and audio models)
    */
    @DataContract
    public static class AiAudioUrlContent extends AiContent
    {
        /**
        * The audio for this content.
        */
        @DataMember(Name="audio_url")
        @SerializedName("audio_url")
        public AiAudioUrl audioUrl = null;
        
        public AiAudioUrl getAudioUrl() { return audioUrl; }
        public AiAudioUrlContent setAudioUrl(AiAudioUrl value) { this.audioUrl = value; return this; }
    }

    /**
    * The function that the model called.
    */
    @DataContract
    public static class ToolFunction
    {
        /**
        * The name of the function to call.
        */
        @DataMember(Name="name")
        @SerializedName("name")
        public String name = null;

        /**
        * The arguments to call the function with, as generated by the model in JSON format. Note that the model does not always generate valid JSON, and may hallucinate parameters not defined by your function schema. Validate the arguments in your code before calling your function.
        */
        @DataMember(Name="arguments")
        @SerializedName("arguments")
        public String arguments = null;
        
        public String getName() { return name; }
        public ToolFunction setName(String value) { this.name = value; return this; }
        public String getArguments() { return arguments; }
        public ToolFunction setArguments(String value) { this.arguments = value; return this; }
    }

    /**
    * A list of message content tokens with log probability information.
    */
    @DataContract
    public static class LogprobItem
    {
        /**
        * The token.
        */
        @DataMember(Name="token")
        @SerializedName("token")
        public String token = null;

        /**
        * The log probability of this token, if it is within the top 20 most likely tokens. Otherwise, the value `-9999`.0 is used to signify that the token is very unlikely.
        */
        @DataMember(Name="logprob")
        @SerializedName("logprob")
        public Double logprob = null;

        /**
        * A list of integers representing the UTF-8 bytes representation of the token. Useful in instances where characters are represented by multiple tokens and their byte representations must be combined to generate the correct text representation. Can be `null` if there is no bytes representation for the token.
        */
        @DataMember(Name="bytes")
        @SerializedName("bytes")
        public byte[] bytes = new byte[]{};

        /**
        * List of the most likely tokens and their log probability, at this token position. In rare cases, there may be fewer than the number of requested `top_logprobs` returned.
        */
        @DataMember(Name="top_logprobs")
        @SerializedName("top_logprobs")
        public ArrayList<LogprobItem> topLogprobs = new ArrayList<LogprobItem>();
        
        public String getToken() { return token; }
        public LogprobItem setToken(String value) { this.token = value; return this; }
        public Double getLogprob() { return logprob; }
        public LogprobItem setLogprob(Double value) { this.logprob = value; return this; }
        public byte[] getBytes() { return bytes; }
        public LogprobItem setBytes(byte[] value) { this.bytes = value; return this; }
        public ArrayList<LogprobItem> getTopLogprobs() { return topLogprobs; }
        public LogprobItem setTopLogprobs(ArrayList<LogprobItem> value) { this.topLogprobs = value; return this; }
    }

    /**
    * Annotations for the message, when applicable, as when using the web search tool.
    */
    @DataContract
    public static class UrlCitation
    {
        /**
        * The index of the last character of the URL citation in the message.
        */
        @DataMember(Name="end_index")
        @SerializedName("end_index")
        public Integer endIndex = null;

        /**
        * The index of the first character of the URL citation in the message.
        */
        @DataMember(Name="start_index")
        @SerializedName("start_index")
        public Integer startIndex = null;

        /**
        * The title of the web resource.
        */
        @DataMember(Name="title")
        @SerializedName("title")
        public String title = null;

        /**
        * The URL of the web resource.
        */
        @DataMember(Name="url")
        @SerializedName("url")
        public String url = null;
        
        public Integer getEndIndex() { return endIndex; }
        public UrlCitation setEndIndex(Integer value) { this.endIndex = value; return this; }
        public Integer getStartIndex() { return startIndex; }
        public UrlCitation setStartIndex(Integer value) { this.startIndex = value; return this; }
        public String getTitle() { return title; }
        public UrlCitation setTitle(String value) { this.title = value; return this; }
        public String getUrl() { return url; }
        public UrlCitation setUrl(String value) { this.url = value; return this; }
    }

    @DataContract
    public static class AiImageUrl
    {
        /**
        * Either a URL of the image or the base64 encoded image data.
        */
        @DataMember(Name="url")
        @SerializedName("url")
        public String url = null;
        
        public String getUrl() { return url; }
        public AiImageUrl setUrl(String value) { this.url = value; return this; }
    }

    /**
    * Audio content part
    */
    @DataContract
    public static class AiInputAudio
    {
        /**
        * URL or Base64 encoded audio data.
        */
        @DataMember(Name="data")
        @SerializedName("data")
        public String data = null;

        /**
        * The format of the encoded audio data. Currently supports 'wav' and 'mp3'.
        */
        @DataMember(Name="format")
        @SerializedName("format")
        public String format = null;
        
        public String getData() { return data; }
        public AiInputAudio setData(String value) { this.data = value; return this; }
        public String getFormat() { return format; }
        public AiInputAudio setFormat(String value) { this.format = value; return this; }
    }

    /**
    * File content part
    */
    @DataContract
    public static class AiFile
    {
        /**
        * The URL or base64 encoded file data, used when passing the file to the model as a string.
        */
        @DataMember(Name="file_data")
        @SerializedName("file_data")
        public String fileData = null;

        /**
        * The name of the file, used when passing the file to the model as a string.
        */
        @DataMember(Name="filename")
        @SerializedName("filename")
        public String filename = null;

        /**
        * The ID of an uploaded file to use as input.
        */
        @DataMember(Name="file_id")
        @SerializedName("file_id")
        public String fileId = null;
        
        public String getFileData() { return fileData; }
        public AiFile setFileData(String value) { this.fileData = value; return this; }
        public String getFilename() { return filename; }
        public AiFile setFilename(String value) { this.filename = value; return this; }
        public String getFileId() { return fileId; }
        public AiFile setFileId(String value) { this.fileId = value; return this; }
    }

    @DataContract
    public static class AiAudioUrl
    {
        /**
        * Either a URL of the audio or the base64 encoded audio data.
        */
        @DataMember(Name="url")
        @SerializedName("url")
        public String url = null;
        
        public String getUrl() { return url; }
        public AiAudioUrl setUrl(String value) { this.url = value; return this; }
    }

}
