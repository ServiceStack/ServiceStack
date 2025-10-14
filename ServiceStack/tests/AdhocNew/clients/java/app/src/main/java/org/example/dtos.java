/* Options:
Date: 2025-10-14 12:13:33
Version: 8.81
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
//IncludeTypes: 
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

    @Route(Path="/hello/{Name}")
    public static class Hello implements IReturn<HelloResponse>, IGet
    {
        public String name = null;
        
        public String getName() { return name; }
        public Hello setName(String value) { this.name = value; return this; }
        private static Object responseType = HelloResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class AdminData implements IReturn<AdminDataResponse>, IGet
    {
        
        private static Object responseType = AdminDataResponse.class;
        public Object getResponseType() { return responseType; }
    }

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

    /**
    * Sign In
    */
    @Route(Path="/auth", Verbs="GET,POST")
    // @Route(Path="/auth/{provider}", Verbs="POST")
    @Api(Description="Sign In")
    @DataContract
    public static class Authenticate implements IReturn<AuthenticateResponse>, IPost
    {
        /**
        * AuthProvider, e.g. credentials
        */
        @DataMember(Order=1)
        public String provider = null;

        @DataMember(Order=2)
        public String userName = null;

        @DataMember(Order=3)
        public String password = null;

        @DataMember(Order=4)
        public Boolean rememberMe = null;

        @DataMember(Order=5)
        public String accessToken = null;

        @DataMember(Order=6)
        public String accessTokenSecret = null;

        @DataMember(Order=7)
        public String returnUrl = null;

        @DataMember(Order=8)
        public String errorView = null;

        @DataMember(Order=9)
        public HashMap<String,String> meta = null;
        
        public String getProvider() { return provider; }
        public Authenticate setProvider(String value) { this.provider = value; return this; }
        public String getUserName() { return userName; }
        public Authenticate setUserName(String value) { this.userName = value; return this; }
        public String getPassword() { return password; }
        public Authenticate setPassword(String value) { this.password = value; return this; }
        public Boolean isRememberMe() { return rememberMe; }
        public Authenticate setRememberMe(Boolean value) { this.rememberMe = value; return this; }
        public String getAccessToken() { return accessToken; }
        public Authenticate setAccessToken(String value) { this.accessToken = value; return this; }
        public String getAccessTokenSecret() { return accessTokenSecret; }
        public Authenticate setAccessTokenSecret(String value) { this.accessTokenSecret = value; return this; }
        public String getReturnUrl() { return returnUrl; }
        public Authenticate setReturnUrl(String value) { this.returnUrl = value; return this; }
        public String getErrorView() { return errorView; }
        public Authenticate setErrorView(String value) { this.errorView = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public Authenticate setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        private static Object responseType = AuthenticateResponse.class;
        public Object getResponseType() { return responseType; }
    }

    /**
    * Find Bookings
    */
    @Route(Path="/bookings", Verbs="GET")
    // @Route(Path="/bookings/{Id}", Verbs="GET")
    public static class QueryBookings extends QueryDb<Booking> implements IReturn<QueryResponse<Booking>>
    {
        public Integer id = null;
        
        public Integer getId() { return id; }
        public QueryBookings setId(Integer value) { this.id = value; return this; }
        private static Object responseType = new TypeToken<QueryResponse<Booking>>(){}.getType();
        public Object getResponseType() { return responseType; }
    }

    /**
    * Find Coupons
    */
    @Route(Path="/coupons", Verbs="GET")
    public static class QueryCoupons extends QueryDb<Coupon> implements IReturn<QueryResponse<Coupon>>
    {
        public String id = null;
        
        public String getId() { return id; }
        public QueryCoupons setId(String value) { this.id = value; return this; }
        private static Object responseType = new TypeToken<QueryResponse<Coupon>>(){}.getType();
        public Object getResponseType() { return responseType; }
    }

    @ValidateRequest(Validator="IsAdmin")
    public static class QueryUsers extends QueryDb<User> implements IReturn<QueryResponse<User>>
    {
        public String id = null;
        
        public String getId() { return id; }
        public QueryUsers setId(String value) { this.id = value; return this; }
        private static Object responseType = new TypeToken<QueryResponse<User>>(){}.getType();
        public Object getResponseType() { return responseType; }
    }

    /**
    * Create a new Booking
    */
    @Route(Path="/bookings", Verbs="POST")
    @ValidateRequest(Validator="HasRole(`Employee`)")
    public static class CreateBooking implements IReturn<IdResponse>, ICreateDb<Booking>
    {
        /**
        * Name this Booking is for
        */
        @Validate(Validator="NotEmpty")
        public String name = null;

        public RoomType roomType = null;
        @Validate(Validator="GreaterThan(0)")
        public Integer roomNumber = null;

        @Validate(Validator="GreaterThan(0)")
        public BigDecimal cost = null;

        @Required()
        public Date bookingStartDate = null;

        public Date bookingEndDate = null;
        public String notes = null;
        public String couponId = null;
        
        public String getName() { return name; }
        public CreateBooking setName(String value) { this.name = value; return this; }
        public RoomType getRoomType() { return roomType; }
        public CreateBooking setRoomType(RoomType value) { this.roomType = value; return this; }
        public Integer getRoomNumber() { return roomNumber; }
        public CreateBooking setRoomNumber(Integer value) { this.roomNumber = value; return this; }
        public BigDecimal getCost() { return cost; }
        public CreateBooking setCost(BigDecimal value) { this.cost = value; return this; }
        public Date getBookingStartDate() { return bookingStartDate; }
        public CreateBooking setBookingStartDate(Date value) { this.bookingStartDate = value; return this; }
        public Date getBookingEndDate() { return bookingEndDate; }
        public CreateBooking setBookingEndDate(Date value) { this.bookingEndDate = value; return this; }
        public String getNotes() { return notes; }
        public CreateBooking setNotes(String value) { this.notes = value; return this; }
        public String getCouponId() { return couponId; }
        public CreateBooking setCouponId(String value) { this.couponId = value; return this; }
        private static Object responseType = IdResponse.class;
        public Object getResponseType() { return responseType; }
    }

    /**
    * Update an existing Booking
    */
    @Route(Path="/booking/{Id}", Verbs="PATCH")
    @ValidateRequest(Validator="HasRole(`Employee`)")
    public static class UpdateBooking implements IReturn<IdResponse>, IPatchDb<Booking>
    {
        public Integer id = null;
        public String name = null;
        public RoomType roomType = null;
        @Validate(Validator="GreaterThan(0)")
        public Integer roomNumber = null;

        @Validate(Validator="GreaterThan(0)")
        public BigDecimal cost = null;

        public Date bookingStartDate = null;
        public Date bookingEndDate = null;
        public String notes = null;
        public String couponId = null;
        public Boolean cancelled = null;
        
        public Integer getId() { return id; }
        public UpdateBooking setId(Integer value) { this.id = value; return this; }
        public String getName() { return name; }
        public UpdateBooking setName(String value) { this.name = value; return this; }
        public RoomType getRoomType() { return roomType; }
        public UpdateBooking setRoomType(RoomType value) { this.roomType = value; return this; }
        public Integer getRoomNumber() { return roomNumber; }
        public UpdateBooking setRoomNumber(Integer value) { this.roomNumber = value; return this; }
        public BigDecimal getCost() { return cost; }
        public UpdateBooking setCost(BigDecimal value) { this.cost = value; return this; }
        public Date getBookingStartDate() { return bookingStartDate; }
        public UpdateBooking setBookingStartDate(Date value) { this.bookingStartDate = value; return this; }
        public Date getBookingEndDate() { return bookingEndDate; }
        public UpdateBooking setBookingEndDate(Date value) { this.bookingEndDate = value; return this; }
        public String getNotes() { return notes; }
        public UpdateBooking setNotes(String value) { this.notes = value; return this; }
        public String getCouponId() { return couponId; }
        public UpdateBooking setCouponId(String value) { this.couponId = value; return this; }
        public Boolean isCancelled() { return cancelled; }
        public UpdateBooking setCancelled(Boolean value) { this.cancelled = value; return this; }
        private static Object responseType = IdResponse.class;
        public Object getResponseType() { return responseType; }
    }

    /**
    * Delete a Booking
    */
    @Route(Path="/booking/{Id}", Verbs="DELETE")
    @ValidateRequest(Validator="HasRole(`Manager`)")
    public static class DeleteBooking implements IReturnVoid, IDeleteDb<Booking>
    {
        public Integer id = null;
        
        public Integer getId() { return id; }
        public DeleteBooking setId(Integer value) { this.id = value; return this; }
    }

    @Route(Path="/coupons", Verbs="POST")
    @ValidateRequest(Validator="HasRole(`Employee`)")
    public static class CreateCoupon implements IReturn<IdResponse>, ICreateDb<Coupon>
    {
        @Validate(Validator="NotEmpty")
        public String id = null;

        @Validate(Validator="NotEmpty")
        public String description = null;

        @Validate(Validator="GreaterThan(0)")
        public Integer discount = null;

        @Validate(Validator="NotNull")
        public Date expiryDate = null;
        
        public String getId() { return id; }
        public CreateCoupon setId(String value) { this.id = value; return this; }
        public String getDescription() { return description; }
        public CreateCoupon setDescription(String value) { this.description = value; return this; }
        public Integer getDiscount() { return discount; }
        public CreateCoupon setDiscount(Integer value) { this.discount = value; return this; }
        public Date getExpiryDate() { return expiryDate; }
        public CreateCoupon setExpiryDate(Date value) { this.expiryDate = value; return this; }
        private static Object responseType = IdResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @Route(Path="/coupons/{Id}", Verbs="PATCH")
    @ValidateRequest(Validator="HasRole(`Employee`)")
    public static class UpdateCoupon implements IReturn<IdResponse>, IPatchDb<Coupon>
    {
        public String id = null;
        @Validate(Validator="NotEmpty")
        public String description = null;

        @Validate(Validator="NotNull")
        // @Validate(Validator="GreaterThan(0)")
        public Integer discount = null;

        @Validate(Validator="NotNull")
        public Date expiryDate = null;
        
        public String getId() { return id; }
        public UpdateCoupon setId(String value) { this.id = value; return this; }
        public String getDescription() { return description; }
        public UpdateCoupon setDescription(String value) { this.description = value; return this; }
        public Integer getDiscount() { return discount; }
        public UpdateCoupon setDiscount(Integer value) { this.discount = value; return this; }
        public Date getExpiryDate() { return expiryDate; }
        public UpdateCoupon setExpiryDate(Date value) { this.expiryDate = value; return this; }
        private static Object responseType = IdResponse.class;
        public Object getResponseType() { return responseType; }
    }

    /**
    * Delete a Coupon
    */
    @Route(Path="/coupons/{Id}", Verbs="DELETE")
    @ValidateRequest(Validator="HasRole(`Manager`)")
    public static class DeleteCoupon implements IReturnVoid, IDeleteDb<Coupon>
    {
        public String id = null;
        
        public String getId() { return id; }
        public DeleteCoupon setId(String value) { this.id = value; return this; }
    }

    @DataContract
    public static class GetAnalyticsInfo implements IReturn<GetAnalyticsInfoResponse>, IGet
    {
        @DataMember(Order=1)
        public Date month = null;

        @DataMember(Order=2)
        public String type = null;

        @DataMember(Order=3)
        public String op = null;

        @DataMember(Order=4)
        public String apiKey = null;

        @DataMember(Order=5)
        public String userId = null;

        @DataMember(Order=6)
        public String ip = null;
        
        public Date getMonth() { return month; }
        public GetAnalyticsInfo setMonth(Date value) { this.month = value; return this; }
        public String getType() { return type; }
        public GetAnalyticsInfo setType(String value) { this.type = value; return this; }
        public String getOp() { return op; }
        public GetAnalyticsInfo setOp(String value) { this.op = value; return this; }
        public String getApiKey() { return apiKey; }
        public GetAnalyticsInfo setApiKey(String value) { this.apiKey = value; return this; }
        public String getUserId() { return userId; }
        public GetAnalyticsInfo setUserId(String value) { this.userId = value; return this; }
        public String getIp() { return ip; }
        public GetAnalyticsInfo setIp(String value) { this.ip = value; return this; }
        private static Object responseType = GetAnalyticsInfoResponse.class;
        public Object getResponseType() { return responseType; }
    }

    @DataContract
    public static class GetAnalyticsReports implements IReturn<GetAnalyticsReportsResponse>, IGet
    {
        @DataMember(Order=1)
        public Date month = null;

        @DataMember(Order=2)
        public String filter = null;

        @DataMember(Order=3)
        public String value = null;

        @DataMember(Order=4)
        public Boolean force = null;
        
        public Date getMonth() { return month; }
        public GetAnalyticsReports setMonth(Date value) { this.month = value; return this; }
        public String getFilter() { return filter; }
        public GetAnalyticsReports setFilter(String value) { this.filter = value; return this; }
        public String getValue() { return value; }
        public GetAnalyticsReports setValue(String value) { this.value = value; return this; }
        public Boolean isForce() { return force; }
        public GetAnalyticsReports setForce(Boolean value) { this.force = value; return this; }
        private static Object responseType = GetAnalyticsReportsResponse.class;
        public Object getResponseType() { return responseType; }
    }

    public static class HelloResponse
    {
        public String result = null;
        
        public String getResult() { return result; }
        public HelloResponse setResult(String value) { this.result = value; return this; }
    }

    public static class AdminDataResponse
    {
        public ArrayList<PageStats> pageStats = new ArrayList<PageStats>();
        
        public ArrayList<PageStats> getPageStats() { return pageStats; }
        public AdminDataResponse setPageStats(ArrayList<PageStats> value) { this.pageStats = value; return this; }
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
        public HashMap<String,String> getMetadata() { return metadata; }
        public ChatResponse setMetadata(HashMap<String,String> value) { this.metadata = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public ChatResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class AuthenticateResponse implements IHasSessionId, IHasBearerToken
    {
        @DataMember(Order=1)
        public String userId = null;

        @DataMember(Order=2)
        public String sessionId = null;

        @DataMember(Order=3)
        public String userName = null;

        @DataMember(Order=4)
        public String displayName = null;

        @DataMember(Order=5)
        public String referrerUrl = null;

        @DataMember(Order=6)
        public String bearerToken = null;

        @DataMember(Order=7)
        public String refreshToken = null;

        @DataMember(Order=8)
        public Date refreshTokenExpiry = null;

        @DataMember(Order=9)
        public String profileUrl = null;

        @DataMember(Order=10)
        public ArrayList<String> roles = null;

        @DataMember(Order=11)
        public ArrayList<String> permissions = null;

        @DataMember(Order=12)
        public String authProvider = null;

        @DataMember(Order=13)
        public ResponseStatus responseStatus = null;

        @DataMember(Order=14)
        public HashMap<String,String> meta = null;
        
        public String getUserId() { return userId; }
        public AuthenticateResponse setUserId(String value) { this.userId = value; return this; }
        public String getSessionId() { return sessionId; }
        public AuthenticateResponse setSessionId(String value) { this.sessionId = value; return this; }
        public String getUserName() { return userName; }
        public AuthenticateResponse setUserName(String value) { this.userName = value; return this; }
        public String getDisplayName() { return displayName; }
        public AuthenticateResponse setDisplayName(String value) { this.displayName = value; return this; }
        public String getReferrerUrl() { return referrerUrl; }
        public AuthenticateResponse setReferrerUrl(String value) { this.referrerUrl = value; return this; }
        public String getBearerToken() { return bearerToken; }
        public AuthenticateResponse setBearerToken(String value) { this.bearerToken = value; return this; }
        public String getRefreshToken() { return refreshToken; }
        public AuthenticateResponse setRefreshToken(String value) { this.refreshToken = value; return this; }
        public Date getRefreshTokenExpiry() { return refreshTokenExpiry; }
        public AuthenticateResponse setRefreshTokenExpiry(Date value) { this.refreshTokenExpiry = value; return this; }
        public String getProfileUrl() { return profileUrl; }
        public AuthenticateResponse setProfileUrl(String value) { this.profileUrl = value; return this; }
        public ArrayList<String> getRoles() { return roles; }
        public AuthenticateResponse setRoles(ArrayList<String> value) { this.roles = value; return this; }
        public ArrayList<String> getPermissions() { return permissions; }
        public AuthenticateResponse setPermissions(ArrayList<String> value) { this.permissions = value; return this; }
        public String getAuthProvider() { return authProvider; }
        public AuthenticateResponse setAuthProvider(String value) { this.authProvider = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public AuthenticateResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public AuthenticateResponse setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    @DataContract
    public static class QueryResponse<T>
    {
        @DataMember(Order=1)
        public Integer offset = null;

        @DataMember(Order=2)
        public Integer total = null;

        @DataMember(Order=3)
        public ArrayList<Booking> results = new ArrayList<Booking>();

        @DataMember(Order=4)
        public HashMap<String,String> meta = null;

        @DataMember(Order=5)
        public ResponseStatus responseStatus = null;
        
        public Integer getOffset() { return offset; }
        public QueryResponse<T> setOffset(Integer value) { this.offset = value; return this; }
        public Integer getTotal() { return total; }
        public QueryResponse<T> setTotal(Integer value) { this.total = value; return this; }
        public ArrayList<Booking> getResults() { return results; }
        public QueryResponse<T> setResults(ArrayList<Booking> value) { this.results = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public QueryResponse<T> setMeta(HashMap<String,String> value) { this.meta = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public QueryResponse<T> setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class IdResponse
    {
        @DataMember(Order=1)
        public String id = null;

        @DataMember(Order=2)
        public ResponseStatus responseStatus = null;
        
        public String getId() { return id; }
        public IdResponse setId(String value) { this.id = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public IdResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class GetAnalyticsInfoResponse
    {
        @DataMember(Order=1)
        public ArrayList<String> months = null;

        @DataMember(Order=2)
        public AnalyticsLogInfo result = null;

        @DataMember(Order=3)
        public ResponseStatus responseStatus = null;
        
        public ArrayList<String> getMonths() { return months; }
        public GetAnalyticsInfoResponse setMonths(ArrayList<String> value) { this.months = value; return this; }
        public AnalyticsLogInfo getResult() { return result; }
        public GetAnalyticsInfoResponse setResult(AnalyticsLogInfo value) { this.result = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public GetAnalyticsInfoResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
    }

    @DataContract
    public static class GetAnalyticsReportsResponse
    {
        @DataMember(Order=1)
        public AnalyticsReports result = null;

        @DataMember(Order=2)
        public ResponseStatus responseStatus = null;
        
        public AnalyticsReports getResult() { return result; }
        public GetAnalyticsReportsResponse setResult(AnalyticsReports value) { this.result = value; return this; }
        public ResponseStatus getResponseStatus() { return responseStatus; }
        public GetAnalyticsReportsResponse setResponseStatus(ResponseStatus value) { this.responseStatus = value; return this; }
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
        @DataMember(Name="response_format")
        @SerializedName("response_format")
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
        
        public ToolType getType() { return type; }
        public Tool setType(ToolType value) { this.type = value; return this; }
    }

    public static class QueryDb<T> extends QueryBase
    {
        
    }

    /**
    * Booking Details
    */
    public static class Booking extends AuditBase
    {
        public Integer id = null;
        public String name = null;
        public RoomType roomType = null;
        public Integer roomNumber = null;
        public Date bookingStartDate = null;
        public Date bookingEndDate = null;
        public BigDecimal cost = null;
        @References(Type=Coupon.class)
        public String couponId = null;

        public Coupon discount = null;
        public String notes = null;
        public Boolean cancelled = null;
        public User employee = null;
        
        public Integer getId() { return id; }
        public Booking setId(Integer value) { this.id = value; return this; }
        public String getName() { return name; }
        public Booking setName(String value) { this.name = value; return this; }
        public RoomType getRoomType() { return roomType; }
        public Booking setRoomType(RoomType value) { this.roomType = value; return this; }
        public Integer getRoomNumber() { return roomNumber; }
        public Booking setRoomNumber(Integer value) { this.roomNumber = value; return this; }
        public Date getBookingStartDate() { return bookingStartDate; }
        public Booking setBookingStartDate(Date value) { this.bookingStartDate = value; return this; }
        public Date getBookingEndDate() { return bookingEndDate; }
        public Booking setBookingEndDate(Date value) { this.bookingEndDate = value; return this; }
        public BigDecimal getCost() { return cost; }
        public Booking setCost(BigDecimal value) { this.cost = value; return this; }
        public String getCouponId() { return couponId; }
        public Booking setCouponId(String value) { this.couponId = value; return this; }
        public Coupon getDiscount() { return discount; }
        public Booking setDiscount(Coupon value) { this.discount = value; return this; }
        public String getNotes() { return notes; }
        public Booking setNotes(String value) { this.notes = value; return this; }
        public Boolean isCancelled() { return cancelled; }
        public Booking setCancelled(Boolean value) { this.cancelled = value; return this; }
        public User getEmployee() { return employee; }
        public Booking setEmployee(User value) { this.employee = value; return this; }
    }

    /**
    * Discount Coupons
    */
    public static class Coupon
    {
        public String id = null;
        public String description = null;
        public Integer discount = null;
        public Date expiryDate = null;
        
        public String getId() { return id; }
        public Coupon setId(String value) { this.id = value; return this; }
        public String getDescription() { return description; }
        public Coupon setDescription(String value) { this.description = value; return this; }
        public Integer getDiscount() { return discount; }
        public Coupon setDiscount(Integer value) { this.discount = value; return this; }
        public Date getExpiryDate() { return expiryDate; }
        public Coupon setExpiryDate(Date value) { this.expiryDate = value; return this; }
    }

    public static class User
    {
        public String id = null;
        public String userName = null;
        public String firstName = null;
        public String lastName = null;
        public String displayName = null;
        public String profileUrl = null;
        
        public String getId() { return id; }
        public User setId(String value) { this.id = value; return this; }
        public String getUserName() { return userName; }
        public User setUserName(String value) { this.userName = value; return this; }
        public String getFirstName() { return firstName; }
        public User setFirstName(String value) { this.firstName = value; return this; }
        public String getLastName() { return lastName; }
        public User setLastName(String value) { this.lastName = value; return this; }
        public String getDisplayName() { return displayName; }
        public User setDisplayName(String value) { this.displayName = value; return this; }
        public String getProfileUrl() { return profileUrl; }
        public User setProfileUrl(String value) { this.profileUrl = value; return this; }
    }

    public static enum RoomType
    {
        Single,
        Double,
        Queen,
        Twin,
        Suite;
    }

    public static class PageStats
    {
        public String label = null;
        public Integer total = null;
        
        public String getLabel() { return label; }
        public PageStats setLabel(String value) { this.label = value; return this; }
        public Integer getTotal() { return total; }
        public PageStats setTotal(Integer value) { this.total = value; return this; }
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
        
        public String getFinishReason() { return finishReason; }
        public Choice setFinishReason(String value) { this.finishReason = value; return this; }
        public Integer getIndex() { return index; }
        public Choice setIndex(Integer value) { this.index = value; return this; }
        public ChoiceMessage getMessage() { return message; }
        public Choice setMessage(ChoiceMessage value) { this.message = value; return this; }
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
        public Integer completionTokens = null;

        /**
        * Number of tokens in the prompt.
        */
        @DataMember(Name="prompt_tokens")
        @SerializedName("prompt_tokens")
        public Integer promptTokens = null;

        /**
        * Total number of tokens used in the request (prompt + completion).
        */
        @DataMember(Name="total_tokens")
        @SerializedName("total_tokens")
        public Integer totalTokens = null;

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
        
        public Integer getCompletionTokens() { return completionTokens; }
        public AiUsage setCompletionTokens(Integer value) { this.completionTokens = value; return this; }
        public Integer getPromptTokens() { return promptTokens; }
        public AiUsage setPromptTokens(Integer value) { this.promptTokens = value; return this; }
        public Integer getTotalTokens() { return totalTokens; }
        public AiUsage setTotalTokens(Integer value) { this.totalTokens = value; return this; }
        public AiCompletionUsage getCompletionTokensDetails() { return completionTokensDetails; }
        public AiUsage setCompletionTokensDetails(AiCompletionUsage value) { this.completionTokensDetails = value; return this; }
        public AiPromptUsage getPromptTokensDetails() { return promptTokensDetails; }
        public AiUsage setPromptTokensDetails(AiPromptUsage value) { this.promptTokensDetails = value; return this; }
    }

    @DataContract
    public static class AnalyticsLogInfo
    {
        @DataMember(Order=1)
        public Long id = null;

        @DataMember(Order=2)
        public Date dateTime = null;

        @DataMember(Order=3)
        public String browser = null;

        @DataMember(Order=4)
        public String device = null;

        @DataMember(Order=5)
        public String bot = null;

        @DataMember(Order=6)
        public String op = null;

        @DataMember(Order=7)
        public String userId = null;

        @DataMember(Order=8)
        public String userName = null;

        @DataMember(Order=9)
        public String apiKey = null;

        @DataMember(Order=10)
        public String ip = null;
        
        public Long getId() { return id; }
        public AnalyticsLogInfo setId(Long value) { this.id = value; return this; }
        public Date getDateTime() { return dateTime; }
        public AnalyticsLogInfo setDateTime(Date value) { this.dateTime = value; return this; }
        public String getBrowser() { return browser; }
        public AnalyticsLogInfo setBrowser(String value) { this.browser = value; return this; }
        public String getDevice() { return device; }
        public AnalyticsLogInfo setDevice(String value) { this.device = value; return this; }
        public String getBot() { return bot; }
        public AnalyticsLogInfo setBot(String value) { this.bot = value; return this; }
        public String getOp() { return op; }
        public AnalyticsLogInfo setOp(String value) { this.op = value; return this; }
        public String getUserId() { return userId; }
        public AnalyticsLogInfo setUserId(String value) { this.userId = value; return this; }
        public String getUserName() { return userName; }
        public AnalyticsLogInfo setUserName(String value) { this.userName = value; return this; }
        public String getApiKey() { return apiKey; }
        public AnalyticsLogInfo setApiKey(String value) { this.apiKey = value; return this; }
        public String getIp() { return ip; }
        public AnalyticsLogInfo setIp(String value) { this.ip = value; return this; }
    }

    @DataContract
    public static class AnalyticsReports
    {
        @DataMember(Order=1)
        public Long id = null;

        @DataMember(Order=2)
        public Date created = null;

        @DataMember(Order=3)
        public BigDecimal version = null;

        @DataMember(Order=4)
        public HashMap<String,RequestSummary> apis = null;

        @DataMember(Order=5)
        public HashMap<String,RequestSummary> users = null;

        @DataMember(Order=6)
        public HashMap<String,RequestSummary> tags = null;

        @DataMember(Order=7)
        public HashMap<String,RequestSummary> status = null;

        @DataMember(Order=8)
        public HashMap<String,RequestSummary> days = null;

        @DataMember(Order=9)
        public HashMap<String,RequestSummary> apiKeys = null;

        @DataMember(Order=10)
        public HashMap<String,RequestSummary> ips = null;

        @DataMember(Order=11)
        public HashMap<String,RequestSummary> browsers = null;

        @DataMember(Order=12)
        public HashMap<String,RequestSummary> devices = null;

        @DataMember(Order=13)
        public HashMap<String,RequestSummary> bots = null;

        @DataMember(Order=14)
        public HashMap<String,Long> durations = null;
        
        public Long getId() { return id; }
        public AnalyticsReports setId(Long value) { this.id = value; return this; }
        public Date getCreated() { return created; }
        public AnalyticsReports setCreated(Date value) { this.created = value; return this; }
        public BigDecimal getVersion() { return version; }
        public AnalyticsReports setVersion(BigDecimal value) { this.version = value; return this; }
        public HashMap<String,RequestSummary> getApis() { return apis; }
        public AnalyticsReports setApis(HashMap<String,RequestSummary> value) { this.apis = value; return this; }
        public HashMap<String,RequestSummary> getUsers() { return users; }
        public AnalyticsReports setUsers(HashMap<String,RequestSummary> value) { this.users = value; return this; }
        public HashMap<String,RequestSummary> getTags() { return tags; }
        public AnalyticsReports setTags(HashMap<String,RequestSummary> value) { this.tags = value; return this; }
        public HashMap<String,RequestSummary> getStatus() { return status; }
        public AnalyticsReports setStatus(HashMap<String,RequestSummary> value) { this.status = value; return this; }
        public HashMap<String,RequestSummary> getDays() { return days; }
        public AnalyticsReports setDays(HashMap<String,RequestSummary> value) { this.days = value; return this; }
        public HashMap<String,RequestSummary> getApiKeys() { return apiKeys; }
        public AnalyticsReports setApiKeys(HashMap<String,RequestSummary> value) { this.apiKeys = value; return this; }
        public HashMap<String,RequestSummary> getIps() { return ips; }
        public AnalyticsReports setIps(HashMap<String,RequestSummary> value) { this.ips = value; return this; }
        public HashMap<String,RequestSummary> getBrowsers() { return browsers; }
        public AnalyticsReports setBrowsers(HashMap<String,RequestSummary> value) { this.browsers = value; return this; }
        public HashMap<String,RequestSummary> getDevices() { return devices; }
        public AnalyticsReports setDevices(HashMap<String,RequestSummary> value) { this.devices = value; return this; }
        public HashMap<String,RequestSummary> getBots() { return bots; }
        public AnalyticsReports setBots(HashMap<String,RequestSummary> value) { this.bots = value; return this; }
        public HashMap<String,Long> getDurations() { return durations; }
        public AnalyticsReports setDurations(HashMap<String,Long> value) { this.durations = value; return this; }
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
        public String function = null;
        
        public String getId() { return id; }
        public ToolCall setId(String value) { this.id = value; return this; }
        public String getType() { return type; }
        public ToolCall setType(String value) { this.type = value; return this; }
        public String getFunction() { return function; }
        public ToolCall setFunction(String value) { this.function = value; return this; }
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
    public static class QueryBase
    {
        @DataMember(Order=1)
        public Integer skip = null;

        @DataMember(Order=2)
        public Integer take = null;

        @DataMember(Order=3)
        public String orderBy = null;

        @DataMember(Order=4)
        public String orderByDesc = null;

        @DataMember(Order=5)
        public String include = null;

        @DataMember(Order=6)
        public String fields = null;

        @DataMember(Order=7)
        public HashMap<String,String> meta = null;
        
        public Integer getSkip() { return skip; }
        public QueryBase setSkip(Integer value) { this.skip = value; return this; }
        public Integer getTake() { return take; }
        public QueryBase setTake(Integer value) { this.take = value; return this; }
        public String getOrderBy() { return orderBy; }
        public QueryBase setOrderBy(String value) { this.orderBy = value; return this; }
        public String getOrderByDesc() { return orderByDesc; }
        public QueryBase setOrderByDesc(String value) { this.orderByDesc = value; return this; }
        public String getInclude() { return include; }
        public QueryBase setInclude(String value) { this.include = value; return this; }
        public String getFields() { return fields; }
        public QueryBase setFields(String value) { this.fields = value; return this; }
        public HashMap<String,String> getMeta() { return meta; }
        public QueryBase setMeta(HashMap<String,String> value) { this.meta = value; return this; }
    }

    @DataContract
    public static class AuditBase
    {
        @DataMember(Order=1)
        public Date createdDate = null;

        @DataMember(Order=2)
        @Required()
        public String createdBy = null;

        @DataMember(Order=3)
        public Date modifiedDate = null;

        @DataMember(Order=4)
        @Required()
        public String modifiedBy = null;

        @DataMember(Order=5)
        public Date deletedDate = null;

        @DataMember(Order=6)
        public String deletedBy = null;
        
        public Date getCreatedDate() { return createdDate; }
        public AuditBase setCreatedDate(Date value) { this.createdDate = value; return this; }
        public String getCreatedBy() { return createdBy; }
        public AuditBase setCreatedBy(String value) { this.createdBy = value; return this; }
        public Date getModifiedDate() { return modifiedDate; }
        public AuditBase setModifiedDate(Date value) { this.modifiedDate = value; return this; }
        public String getModifiedBy() { return modifiedBy; }
        public AuditBase setModifiedBy(String value) { this.modifiedBy = value; return this; }
        public Date getDeletedDate() { return deletedDate; }
        public AuditBase setDeletedDate(Date value) { this.deletedDate = value; return this; }
        public String getDeletedBy() { return deletedBy; }
        public AuditBase setDeletedBy(String value) { this.deletedBy = value; return this; }
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
        * The role of the author of this message.
        */
        @DataMember(Name="role")
        @SerializedName("role")
        public String role = null;

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
        public String getRole() { return role; }
        public ChoiceMessage setRole(String value) { this.role = value; return this; }
        public ArrayList<ChoiceAnnotation> getAnnotations() { return annotations; }
        public ChoiceMessage setAnnotations(ArrayList<ChoiceAnnotation> value) { this.annotations = value; return this; }
        public ChoiceAudio getAudio() { return audio; }
        public ChoiceMessage setAudio(ChoiceAudio value) { this.audio = value; return this; }
        public ArrayList<ToolCall> getToolCalls() { return toolCalls; }
        public ChoiceMessage setToolCalls(ArrayList<ToolCall> value) { this.toolCalls = value; return this; }
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
        public Integer acceptedPredictionTokens = null;

        /**
        * Audio input tokens generated by the model.
        */
        @DataMember(Name="audio_tokens")
        @SerializedName("audio_tokens")
        public Integer audioTokens = null;

        /**
        * Tokens generated by the model for reasoning.
        */
        @DataMember(Name="reasoning_tokens")
        @SerializedName("reasoning_tokens")
        public Integer reasoningTokens = null;

        /**
        * When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.
        */
        @DataMember(Name="rejected_prediction_tokens")
        @SerializedName("rejected_prediction_tokens")
        public Integer rejectedPredictionTokens = null;
        
        public Integer getAcceptedPredictionTokens() { return acceptedPredictionTokens; }
        public AiCompletionUsage setAcceptedPredictionTokens(Integer value) { this.acceptedPredictionTokens = value; return this; }
        public Integer getAudioTokens() { return audioTokens; }
        public AiCompletionUsage setAudioTokens(Integer value) { this.audioTokens = value; return this; }
        public Integer getReasoningTokens() { return reasoningTokens; }
        public AiCompletionUsage setReasoningTokens(Integer value) { this.reasoningTokens = value; return this; }
        public Integer getRejectedPredictionTokens() { return rejectedPredictionTokens; }
        public AiCompletionUsage setRejectedPredictionTokens(Integer value) { this.rejectedPredictionTokens = value; return this; }
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
        public Integer acceptedPredictionTokens = null;

        /**
        * Audio input tokens present in the prompt.
        */
        @DataMember(Name="audio_tokens")
        @SerializedName("audio_tokens")
        public Integer audioTokens = null;

        /**
        * Cached tokens present in the prompt.
        */
        @DataMember(Name="cached_tokens")
        @SerializedName("cached_tokens")
        public Integer cachedTokens = null;
        
        public Integer getAcceptedPredictionTokens() { return acceptedPredictionTokens; }
        public AiPromptUsage setAcceptedPredictionTokens(Integer value) { this.acceptedPredictionTokens = value; return this; }
        public Integer getAudioTokens() { return audioTokens; }
        public AiPromptUsage setAudioTokens(Integer value) { this.audioTokens = value; return this; }
        public Integer getCachedTokens() { return cachedTokens; }
        public AiPromptUsage setCachedTokens(Integer value) { this.cachedTokens = value; return this; }
    }

    @DataContract
    public static class RequestSummary
    {
        @DataMember(Order=1)
        public String name = null;

        @DataMember(Order=2)
        public Long totalRequests = null;

        @DataMember(Order=3)
        public Long totalRequestLength = null;

        @DataMember(Order=4)
        public Long minRequestLength = null;

        @DataMember(Order=5)
        public Long maxRequestLength = null;

        @DataMember(Order=6)
        public Double totalDuration = null;

        @DataMember(Order=7)
        public Double minDuration = null;

        @DataMember(Order=8)
        public Double maxDuration = null;

        @DataMember(Order=9)
        public HashMap<Integer,Long> status = null;

        @DataMember(Order=10)
        public HashMap<String,Long> durations = null;

        @DataMember(Order=11)
        public HashMap<String,Long> apis = null;

        @DataMember(Order=12)
        public HashMap<String,Long> users = null;

        @DataMember(Order=13)
        public HashMap<String,Long> ips = null;

        @DataMember(Order=14)
        public HashMap<String,Long> apiKeys = null;
        
        public String getName() { return name; }
        public RequestSummary setName(String value) { this.name = value; return this; }
        public Long getTotalRequests() { return totalRequests; }
        public RequestSummary setTotalRequests(Long value) { this.totalRequests = value; return this; }
        public Long getTotalRequestLength() { return totalRequestLength; }
        public RequestSummary setTotalRequestLength(Long value) { this.totalRequestLength = value; return this; }
        public Long getMinRequestLength() { return minRequestLength; }
        public RequestSummary setMinRequestLength(Long value) { this.minRequestLength = value; return this; }
        public Long getMaxRequestLength() { return maxRequestLength; }
        public RequestSummary setMaxRequestLength(Long value) { this.maxRequestLength = value; return this; }
        public Double getTotalDuration() { return totalDuration; }
        public RequestSummary setTotalDuration(Double value) { this.totalDuration = value; return this; }
        public Double getMinDuration() { return minDuration; }
        public RequestSummary setMinDuration(Double value) { this.minDuration = value; return this; }
        public Double getMaxDuration() { return maxDuration; }
        public RequestSummary setMaxDuration(Double value) { this.maxDuration = value; return this; }
        public HashMap<Integer,Long> getStatus() { return status; }
        public RequestSummary setStatus(HashMap<Integer,Long> value) { this.status = value; return this; }
        public HashMap<String,Long> getDurations() { return durations; }
        public RequestSummary setDurations(HashMap<String,Long> value) { this.durations = value; return this; }
        public HashMap<String,Long> getApis() { return apis; }
        public RequestSummary setApis(HashMap<String,Long> value) { this.apis = value; return this; }
        public HashMap<String,Long> getUsers() { return users; }
        public RequestSummary setUsers(HashMap<String,Long> value) { this.users = value; return this; }
        public HashMap<String,Long> getIps() { return ips; }
        public RequestSummary setIps(HashMap<String,Long> value) { this.ips = value; return this; }
        public HashMap<String,Long> getApiKeys() { return apiKeys; }
        public RequestSummary setApiKeys(HashMap<String,Long> value) { this.apiKeys = value; return this; }
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
        public Integer expiresAt = null;

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
        public Integer getExpiresAt() { return expiresAt; }
        public ChoiceAudio setExpiresAt(Integer value) { this.expiresAt = value; return this; }
        public String getId() { return id; }
        public ChoiceAudio setId(String value) { this.id = value; return this; }
        public String getTranscript() { return transcript; }
        public ChoiceAudio setTranscript(String value) { this.transcript = value; return this; }
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

}
