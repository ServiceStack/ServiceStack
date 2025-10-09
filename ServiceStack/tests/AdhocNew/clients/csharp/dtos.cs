/* Options:
Date: 2025-10-10 00:57:38
Version: 8.81
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:5166

//GlobalNamespace: 
//MakePartial: True
//MakeVirtual: True
//MakeInternal: False
//MakeDataContractsExtensible: False
//AddNullableAnnotations: False
//AddReturnMarker: True
//AddDescriptionAsComments: True
//AddDataContractAttributes: False
//AddIndexesToDataMembers: False
//AddGeneratedCodeAttributes: False
//AddResponseStatus: False
//AddImplicitVersion: 
//InitializeCollections: False
//ExportValueTypes: False
//IncludeTypes: 
//ExcludeTypes: 
//AddNamespaces: 
//AddDefaultXmlNamespace: http://schemas.servicestack.net/types
*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.AI;
using MyApp.ServiceModel;

namespace MyApp.ServiceModel
{
    public partial class AdminData
        : IReturn<AdminDataResponse>, IGet
    {
    }

    public partial class AdminDataResponse
    {
        public virtual List<PageStats> PageStats { get; set; } = [];
    }

    ///<summary>
    ///Booking Details
    ///</summary>
    public partial class Booking
        : AuditBase
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual RoomType RoomType { get; set; }
        public virtual int RoomNumber { get; set; }
        public virtual DateTime BookingStartDate { get; set; }
        public virtual DateTime? BookingEndDate { get; set; }
        public virtual decimal Cost { get; set; }
        [References(typeof(MyApp.ServiceModel.Coupon))]
        public virtual string CouponId { get; set; }

        public virtual Coupon Discount { get; set; }
        public virtual string Notes { get; set; }
        public virtual bool? Cancelled { get; set; }
        public virtual User Employee { get; set; }
    }

    ///<summary>
    ///Discount Coupons
    ///</summary>
    public partial class Coupon
    {
        public virtual string Id { get; set; }
        public virtual string Description { get; set; }
        public virtual int Discount { get; set; }
        public virtual DateTime ExpiryDate { get; set; }
    }

    ///<summary>
    ///Create a new Booking
    ///</summary>
    [Route("/bookings", "POST")]
    [ValidateRequest("HasRole(`Employee`)")]
    public partial class CreateBooking
        : IReturn<IdResponse>, ICreateDb<Booking>
    {
        ///<summary>
        ///Name this Booking is for
        ///</summary>
        [Validate("NotEmpty")]
        public virtual string Name { get; set; }

        public virtual RoomType RoomType { get; set; }
        [Validate("GreaterThan(0)")]
        public virtual int RoomNumber { get; set; }

        [Validate("GreaterThan(0)")]
        public virtual decimal Cost { get; set; }

        [Required]
        public virtual DateTime BookingStartDate { get; set; }

        public virtual DateTime? BookingEndDate { get; set; }
        public virtual string Notes { get; set; }
        public virtual string CouponId { get; set; }
    }

    [Route("/coupons", "POST")]
    [ValidateRequest("HasRole(`Employee`)")]
    public partial class CreateCoupon
        : IReturn<IdResponse>, ICreateDb<Coupon>
    {
        [Validate("NotEmpty")]
        public virtual string Id { get; set; }

        [Validate("NotEmpty")]
        public virtual string Description { get; set; }

        [Validate("GreaterThan(0)")]
        public virtual int Discount { get; set; }

        [Validate("NotNull")]
        public virtual DateTime ExpiryDate { get; set; }
    }

    ///<summary>
    ///Delete a Booking
    ///</summary>
    [Route("/booking/{Id}", "DELETE")]
    [ValidateRequest("HasRole(`Manager`)")]
    public partial class DeleteBooking
        : IReturnVoid, IDeleteDb<Booking>
    {
        public virtual int Id { get; set; }
    }

    ///<summary>
    ///Delete a Coupon
    ///</summary>
    [Route("/coupons/{Id}", "DELETE")]
    [ValidateRequest("HasRole(`Manager`)")]
    public partial class DeleteCoupon
        : IReturnVoid, IDeleteDb<Coupon>
    {
        public virtual string Id { get; set; }
    }

    [Route("/hello/{Name}")]
    public partial class Hello
        : IReturn<HelloResponse>, IGet
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class PageStats
    {
        public virtual string Label { get; set; }
        public virtual int Total { get; set; }
    }

    ///<summary>
    ///Find Bookings
    ///</summary>
    [Route("/bookings", "GET")]
    [Route("/bookings/{Id}", "GET")]
    public partial class QueryBookings
        : QueryDb<Booking>, IReturn<QueryResponse<Booking>>
    {
        public virtual int? Id { get; set; }
    }

    ///<summary>
    ///Find Coupons
    ///</summary>
    [Route("/coupons", "GET")]
    public partial class QueryCoupons
        : QueryDb<Coupon>, IReturn<QueryResponse<Coupon>>
    {
        public virtual string Id { get; set; }
    }

    [ValidateRequest("IsAdmin")]
    public partial class QueryUsers
        : QueryDb<User>, IReturn<QueryResponse<User>>
    {
        public virtual string Id { get; set; }
    }

    public enum RoomType
    {
        Single,
        Double,
        Queen,
        Twin,
        Suite,
    }

    ///<summary>
    ///Update an existing Booking
    ///</summary>
    [Route("/booking/{Id}", "PATCH")]
    [ValidateRequest("HasRole(`Employee`)")]
    public partial class UpdateBooking
        : IReturn<IdResponse>, IPatchDb<Booking>
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual RoomType? RoomType { get; set; }
        [Validate("GreaterThan(0)")]
        public virtual int? RoomNumber { get; set; }

        [Validate("GreaterThan(0)")]
        public virtual decimal? Cost { get; set; }

        public virtual DateTime? BookingStartDate { get; set; }
        public virtual DateTime? BookingEndDate { get; set; }
        public virtual string Notes { get; set; }
        public virtual string CouponId { get; set; }
        public virtual bool? Cancelled { get; set; }
    }

    [Route("/coupons/{Id}", "PATCH")]
    [ValidateRequest("HasRole(`Employee`)")]
    public partial class UpdateCoupon
        : IReturn<IdResponse>, IPatchDb<Coupon>
    {
        public virtual string Id { get; set; }
        [Validate("NotEmpty")]
        public virtual string Description { get; set; }

        [Validate("NotNull")]
        [Validate("GreaterThan(0)")]
        public virtual int? Discount { get; set; }

        [Validate("NotNull")]
        public virtual DateTime? ExpiryDate { get; set; }
    }

    public partial class User
    {
        public virtual string Id { get; set; }
        public virtual string UserName { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual string ProfileUrl { get; set; }
    }

}

namespace ServiceStack.AI
{
    ///<summary>
    ///Audio content part
    ///</summary>
    [DataContract]
    public partial class AiAudioContent
        : AiContent
    {
        ///<summary>
        ///The audio input for this content.
        ///</summary>
        [DataMember(Name="input_audio")]
        [ApiMember(Description="The audio input for this content.")]
        public virtual AiInputAudio InputAudio { get; set; }
    }

    ///<summary>
    ///Parameters for audio output. Required when audio output is requested with modalities: [audio]
    ///</summary>
    [DataContract]
    public partial class AiChatAudio
    {
        ///<summary>
        ///Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.
        ///</summary>
        [DataMember(Name="format")]
        [ApiMember(Description="Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.")]
        public virtual string Format { get; set; }

        ///<summary>
        ///The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.
        ///</summary>
        [DataMember(Name="voice")]
        [ApiMember(Description="The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.")]
        public virtual string Voice { get; set; }
    }

    ///<summary>
    ///Usage statistics for the completion request.
    ///</summary>
    [Api(Description="Usage statistics for the completion request.")]
    [DataContract]
    public partial class AiCompletionUsage
    {
        ///<summary>
        ///When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
        ///</summary>
        [DataMember(Name="accepted_prediction_tokens")]
        [ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.\n\n")]
        public virtual int AcceptedPredictionTokens { get; set; }

        ///<summary>
        ///Audio input tokens generated by the model.
        ///</summary>
        [DataMember(Name="audio_tokens")]
        [ApiMember(Description="Audio input tokens generated by the model.")]
        public virtual int AudioTokens { get; set; }

        ///<summary>
        ///Tokens generated by the model for reasoning.
        ///</summary>
        [DataMember(Name="reasoning_tokens")]
        [ApiMember(Description="Tokens generated by the model for reasoning.")]
        public virtual int ReasoningTokens { get; set; }

        ///<summary>
        ///When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.
        ///</summary>
        [DataMember(Name="rejected_prediction_tokens")]
        [ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.")]
        public virtual int RejectedPredictionTokens { get; set; }
    }

    [DataContract]
    public partial class AiContent
    {
        ///<summary>
        ///The type of the content part.
        ///</summary>
        [DataMember(Name="type")]
        [ApiMember(Description="The type of the content part.")]
        public virtual string Type { get; set; }
    }

    ///<summary>
    ///File content part
    ///</summary>
    [DataContract]
    public partial class AiFile
    {
        ///<summary>
        ///The URL or base64 encoded file data, used when passing the file to the model as a string.
        ///</summary>
        [DataMember(Name="file_data")]
        [ApiMember(Description="The URL or base64 encoded file data, used when passing the file to the model as a string.")]
        public virtual string FileData { get; set; }

        ///<summary>
        ///The name of the file, used when passing the file to the model as a string.
        ///</summary>
        [DataMember(Name="filename")]
        [ApiMember(Description="The name of the file, used when passing the file to the model as a string.")]
        public virtual string Filename { get; set; }

        ///<summary>
        ///The ID of an uploaded file to use as input.
        ///</summary>
        [DataMember(Name="file_id")]
        [ApiMember(Description="The ID of an uploaded file to use as input.")]
        public virtual string FileId { get; set; }
    }

    ///<summary>
    ///File content part
    ///</summary>
    [DataContract]
    public partial class AiFileContent
        : AiContent
    {
        ///<summary>
        ///The file input for this content.
        ///</summary>
        [DataMember(Name="file")]
        [ApiMember(Description="The file input for this content.")]
        public virtual AiFile File { get; set; }
    }

    ///<summary>
    ///Image content part
    ///</summary>
    [DataContract]
    public partial class AiImageContent
        : AiContent
    {
        ///<summary>
        ///The image for this content.
        ///</summary>
        [DataMember(Name="image_url")]
        [ApiMember(Description="The image for this content.")]
        public virtual AiImageUrl ImageUrl { get; set; }
    }

    public partial class AiImageUrl
    {
        ///<summary>
        ///Either a URL of the image or the base64 encoded image data.
        ///</summary>
        [DataMember(Name="url")]
        [ApiMember(Description="Either a URL of the image or the base64 encoded image data.")]
        public virtual string Url { get; set; }
    }

    ///<summary>
    ///Audio content part
    ///</summary>
    [DataContract]
    public partial class AiInputAudio
    {
        ///<summary>
        ///URL or Base64 encoded audio data.
        ///</summary>
        [DataMember(Name="data")]
        [ApiMember(Description="URL or Base64 encoded audio data.")]
        public virtual string Data { get; set; }

        ///<summary>
        ///The format of the encoded audio data. Currently supports 'wav' and 'mp3'.
        ///</summary>
        [DataMember(Name="format")]
        [ApiMember(Description="The format of the encoded audio data. Currently supports 'wav' and 'mp3'.")]
        public virtual string Format { get; set; }
    }

    ///<summary>
    ///A list of messages comprising the conversation so far.
    ///</summary>
    [Api(Description="A list of messages comprising the conversation so far.")]
    [DataContract]
    public partial class AiMessage
    {
        ///<summary>
        ///The contents of the message.
        ///</summary>
        [DataMember(Name="content")]
        [ApiMember(Description="The contents of the message.")]
        public virtual List<AiContent> Content { get; set; }

        ///<summary>
        ///The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.
        ///</summary>
        [DataMember(Name="role")]
        [ApiMember(Description="The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.")]
        public virtual string Role { get; set; }

        ///<summary>
        ///An optional name for the participant. Provides the model information to differentiate between participants of the same role.
        ///</summary>
        [DataMember(Name="name")]
        [ApiMember(Description="An optional name for the participant. Provides the model information to differentiate between participants of the same role.")]
        public virtual string Name { get; set; }

        ///<summary>
        ///The tool calls generated by the model, such as function calls.
        ///</summary>
        [DataMember(Name="tool_calls")]
        [ApiMember(Description="The tool calls generated by the model, such as function calls.")]
        public virtual List<ToolCall> ToolCalls { get; set; }

        ///<summary>
        ///Tool call that this message is responding to.
        ///</summary>
        [DataMember(Name="tool_call_id")]
        [ApiMember(Description="Tool call that this message is responding to.")]
        public virtual string ToolCallId { get; set; }
    }

    ///<summary>
    ///Breakdown of tokens used in the prompt.
    ///</summary>
    [Api(Description="Breakdown of tokens used in the prompt.")]
    [DataContract]
    public partial class AiPromptUsage
    {
        ///<summary>
        ///When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
        ///</summary>
        [DataMember(Name="accepted_prediction_tokens")]
        [ApiMember(Description="When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.\n\n")]
        public virtual int AcceptedPredictionTokens { get; set; }

        ///<summary>
        ///Audio input tokens present in the prompt.
        ///</summary>
        [DataMember(Name="audio_tokens")]
        [ApiMember(Description="Audio input tokens present in the prompt.")]
        public virtual int AudioTokens { get; set; }

        ///<summary>
        ///Cached tokens present in the prompt.
        ///</summary>
        [DataMember(Name="cached_tokens")]
        [ApiMember(Description="Cached tokens present in the prompt.")]
        public virtual int CachedTokens { get; set; }
    }

    [DataContract]
    public partial class AiResponseFormat
    {
        ///<summary>
        ///An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.
        ///</summary>
        [DataMember(Name="response_format")]
        [ApiMember(Description="An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.")]
        public virtual ResponseFormat Type { get; set; }
    }

    ///<summary>
    ///Text content part
    ///</summary>
    [DataContract]
    public partial class AiTextContent
        : AiContent
    {
        ///<summary>
        ///The text content.
        ///</summary>
        [DataMember(Name="text")]
        [ApiMember(Description="The text content.")]
        public virtual string Text { get; set; }
    }

    ///<summary>
    ///Usage statistics for the completion request.
    ///</summary>
    [Api(Description="Usage statistics for the completion request.")]
    [DataContract]
    public partial class AiUsage
    {
        ///<summary>
        ///Number of tokens in the generated completion.
        ///</summary>
        [DataMember(Name="completion_tokens")]
        [ApiMember(Description="Number of tokens in the generated completion.")]
        public virtual int CompletionTokens { get; set; }

        ///<summary>
        ///Number of tokens in the prompt.
        ///</summary>
        [DataMember(Name="prompt_tokens")]
        [ApiMember(Description="Number of tokens in the prompt.")]
        public virtual int PromptTokens { get; set; }

        ///<summary>
        ///Total number of tokens used in the request (prompt + completion).
        ///</summary>
        [DataMember(Name="total_tokens")]
        [ApiMember(Description="Total number of tokens used in the request (prompt + completion).")]
        public virtual int TotalTokens { get; set; }

        ///<summary>
        ///Breakdown of tokens used in a completion.
        ///</summary>
        [DataMember(Name="completion_tokens_details")]
        [ApiMember(Description="Breakdown of tokens used in a completion.")]
        public virtual AiCompletionUsage CompletionTokensDetails { get; set; }

        ///<summary>
        ///Breakdown of tokens used in the prompt.
        ///</summary>
        [DataMember(Name="prompt_tokens_details")]
        [ApiMember(Description="Breakdown of tokens used in the prompt.")]
        public virtual AiPromptUsage PromptTokensDetails { get; set; }
    }

    ///<summary>
    ///Chat Completions API (OpenAI-Compatible)
    ///</summary>
    [Route("/v1/chat/completions", "POST")]
    [Api(Description="Chat Completions API (OpenAI-Compatible)")]
    [DataContract]
    public partial class ChatCompletion
        : IReturn<ChatResponse>, IPost
    {
        ///<summary>
        ///The messages to generate chat completions for.
        ///</summary>
        [DataMember(Name="messages")]
        [ApiMember(Description="The messages to generate chat completions for.")]
        public virtual List<AiMessage> Messages { get; set; } = [];

        ///<summary>
        ///ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API
        ///</summary>
        [DataMember(Name="model")]
        [ApiMember(Description="ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API")]
        public virtual string Model { get; set; }

        ///<summary>
        ///Parameters for audio output. Required when audio output is requested with modalities: [audio]
        ///</summary>
        [DataMember(Name="audio")]
        [ApiMember(Description="Parameters for audio output. Required when audio output is requested with modalities: [audio]")]
        public virtual AiChatAudio Audio { get; set; }

        ///<summary>
        ///Modify the likelihood of specified tokens appearing in the completion.
        ///</summary>
        [DataMember(Name="logit_bias")]
        [ApiMember(Description="Modify the likelihood of specified tokens appearing in the completion.")]
        public virtual Dictionary<int, int> LogitBias { get; set; }

        ///<summary>
        ///Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
        ///</summary>
        [DataMember(Name="metadata")]
        [ApiMember(Description="Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")]
        public virtual Dictionary<string, string> Metadata { get; set; }

        ///<summary>
        ///Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.
        ///</summary>
        [DataMember(Name="reasoning_effort")]
        [ApiMember(Description="Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.")]
        public virtual string ReasoningEffort { get; set; }

        ///<summary>
        ///An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.
        ///</summary>
        [DataMember(Name="response_format")]
        [ApiMember(Description="An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.")]
        public virtual AiResponseFormat ResponseFormat { get; set; }

        ///<summary>
        ///Specifies the processing type used for serving the request.
        ///</summary>
        [DataMember(Name="service_tier")]
        [ApiMember(Description="Specifies the processing type used for serving the request.")]
        public virtual string ServiceTier { get; set; }

        ///<summary>
        ///Up to 4 sequences where the API will stop generating further tokens.
        ///</summary>
        [DataMember(Name="stop")]
        [ApiMember(Description="Up to 4 sequences where the API will stop generating further tokens.")]
        public virtual List<string> Stop { get; set; }

        ///<summary>
        ///Output types that you would like the model to generate. Most models are capable of generating text, which is the default:
        ///</summary>
        [DataMember(Name="modalities")]
        [ApiMember(Description="Output types that you would like the model to generate. Most models are capable of generating text, which is the default:")]
        public virtual List<string> Modalities { get; set; }

        ///<summary>
        ///Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.
        ///</summary>
        [DataMember(Name="prompt_cache_key")]
        [ApiMember(Description="Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.")]
        public virtual string PromptCacheKey { get; set; }

        ///<summary>
        ///A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.
        ///</summary>
        [DataMember(Name="safety_identifier")]
        [ApiMember(Description="A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.")]
        public virtual string SafetyIdentifier { get; set; }

        ///<summary>
        ///A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.
        ///</summary>
        [DataMember(Name="tools")]
        [ApiMember(Description="A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.")]
        public virtual List<Tool> Tools { get; set; }

        ///<summary>
        ///Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.
        ///</summary>
        [DataMember(Name="verbosity")]
        [ApiMember(Description="Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.")]
        public virtual string Verbosity { get; set; }

        ///<summary>
        ///What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
        ///</summary>
        [DataMember(Name="temperature")]
        [ApiMember(Description="What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.")]
        public virtual double? Temperature { get; set; }

        ///<summary>
        ///An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.
        ///</summary>
        [DataMember(Name="max_completion_tokens")]
        [ApiMember(Description="An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.")]
        public virtual int? MaxCompletionTokens { get; set; }

        ///<summary>
        ///An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
        ///</summary>
        [DataMember(Name="top_logprobs")]
        [ApiMember(Description="An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.")]
        public virtual int? TopLogprobs { get; set; }

        ///<summary>
        ///An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.
        ///</summary>
        [DataMember(Name="top_p")]
        [ApiMember(Description="An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.")]
        public virtual double? TopP { get; set; }

        ///<summary>
        ///Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
        ///</summary>
        [DataMember(Name="frequency_penalty")]
        [ApiMember(Description="Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.")]
        public virtual double? FrequencyPenalty { get; set; }

        ///<summary>
        ///Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
        ///</summary>
        [DataMember(Name="presence_penalty")]
        [ApiMember(Description="Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.")]
        public virtual double? PresencePenalty { get; set; }

        ///<summary>
        ///This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.
        ///</summary>
        [DataMember(Name="seed")]
        [ApiMember(Description="This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.")]
        public virtual int? Seed { get; set; }

        ///<summary>
        ///How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.
        ///</summary>
        [DataMember(Name="n")]
        [ApiMember(Description="How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.")]
        public virtual int? N { get; set; }

        ///<summary>
        ///Whether or not to store the output of this chat completion request for use in our model distillation or evals products.
        ///</summary>
        [DataMember(Name="store")]
        [ApiMember(Description="Whether or not to store the output of this chat completion request for use in our model distillation or evals products.")]
        public virtual bool? Store { get; set; }

        ///<summary>
        ///Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
        ///</summary>
        [DataMember(Name="logprobs")]
        [ApiMember(Description="Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.")]
        public virtual bool? Logprobs { get; set; }

        ///<summary>
        ///Whether to enable parallel function calling during tool use.
        ///</summary>
        [DataMember(Name="parallel_tool_calls")]
        [ApiMember(Description="Whether to enable parallel function calling during tool use.")]
        public virtual bool? ParallelToolCalls { get; set; }

        ///<summary>
        ///Whether to enable thinking mode for some Qwen models and providers.
        ///</summary>
        [DataMember(Name="enable_thinking")]
        [ApiMember(Description="Whether to enable thinking mode for some Qwen models and providers.")]
        public virtual bool? EnableThinking { get; set; }

        ///<summary>
        ///If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.
        ///</summary>
        [DataMember(Name="stream")]
        [ApiMember(Description="If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.")]
        public virtual bool? Stream { get; set; }
    }

    [DataContract]
    public partial class ChatResponse
    {
        ///<summary>
        ///A unique identifier for the chat completion.
        ///</summary>
        [DataMember(Name="id")]
        [ApiMember(Description="A unique identifier for the chat completion.")]
        public virtual string Id { get; set; }

        ///<summary>
        ///A list of chat completion choices. Can be more than one if n is greater than 1.
        ///</summary>
        [DataMember(Name="choices")]
        [ApiMember(Description="A list of chat completion choices. Can be more than one if n is greater than 1.")]
        public virtual List<Choice> Choices { get; set; } = [];

        ///<summary>
        ///The Unix timestamp (in seconds) of when the chat completion was created.
        ///</summary>
        [DataMember(Name="created")]
        [ApiMember(Description="The Unix timestamp (in seconds) of when the chat completion was created.")]
        public virtual long Created { get; set; }

        ///<summary>
        ///The model used for the chat completion.
        ///</summary>
        [DataMember(Name="model")]
        [ApiMember(Description="The model used for the chat completion.")]
        public virtual string Model { get; set; }

        ///<summary>
        ///This fingerprint represents the backend configuration that the model runs with.
        ///</summary>
        [DataMember(Name="system_fingerprint")]
        [ApiMember(Description="This fingerprint represents the backend configuration that the model runs with.")]
        public virtual string SystemFingerprint { get; set; }

        ///<summary>
        ///The object type, which is always chat.completion.
        ///</summary>
        [DataMember(Name="object")]
        [ApiMember(Description="The object type, which is always chat.completion.")]
        public virtual string Object { get; set; }

        ///<summary>
        ///Specifies the processing type used for serving the request.
        ///</summary>
        [DataMember(Name="service_tier")]
        [ApiMember(Description="Specifies the processing type used for serving the request.")]
        public virtual string ServiceTier { get; set; }

        ///<summary>
        ///Usage statistics for the completion request.
        ///</summary>
        [DataMember(Name="usage")]
        [ApiMember(Description="Usage statistics for the completion request.")]
        public virtual AiUsage Usage { get; set; }

        ///<summary>
        ///Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
        ///</summary>
        [DataMember(Name="metadata")]
        [ApiMember(Description="Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")]
        public virtual Dictionary<string, string> Metadata { get; set; }

        [DataMember(Name="responseStatus")]
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    public partial class Choice
    {
        ///<summary>
        ///The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool
        ///</summary>
        [DataMember(Name="finish_reason")]
        [ApiMember(Description="The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool")]
        public virtual string FinishReason { get; set; }

        ///<summary>
        ///The index of the choice in the list of choices.
        ///</summary>
        [DataMember(Name="index")]
        [ApiMember(Description="The index of the choice in the list of choices.")]
        public virtual int Index { get; set; }

        ///<summary>
        ///A chat completion message generated by the model.
        ///</summary>
        [DataMember(Name="message")]
        [ApiMember(Description="A chat completion message generated by the model.")]
        public virtual ChoiceMessage Message { get; set; }
    }

    ///<summary>
    ///Annotations for the message, when applicable, as when using the web search tool.
    ///</summary>
    [DataContract]
    public partial class ChoiceAnnotation
    {
        ///<summary>
        ///The type of the URL citation. Always url_citation.
        ///</summary>
        [DataMember(Name="type")]
        [ApiMember(Description="The type of the URL citation. Always url_citation.")]
        public virtual string Type { get; set; }

        ///<summary>
        ///A URL citation when using web search.
        ///</summary>
        [DataMember(Name="url_citation")]
        [ApiMember(Description="A URL citation when using web search.")]
        public virtual UrlCitation UrlCitation { get; set; }
    }

    ///<summary>
    ///If the audio output modality is requested, this object contains data about the audio response from the model.
    ///</summary>
    [DataContract]
    public partial class ChoiceAudio
    {
        ///<summary>
        ///Base64 encoded audio bytes generated by the model, in the format specified in the request.
        ///</summary>
        [DataMember(Name="data")]
        [ApiMember(Description="Base64 encoded audio bytes generated by the model, in the format specified in the request.")]
        public virtual string Data { get; set; }

        ///<summary>
        ///The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.
        ///</summary>
        [DataMember(Name="expires_at")]
        [ApiMember(Description="The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.")]
        public virtual int ExpiresAt { get; set; }

        ///<summary>
        ///Unique identifier for this audio response.
        ///</summary>
        [DataMember(Name="id")]
        [ApiMember(Description="Unique identifier for this audio response.")]
        public virtual string Id { get; set; }

        ///<summary>
        ///Transcript of the audio generated by the model.
        ///</summary>
        [DataMember(Name="transcript")]
        [ApiMember(Description="Transcript of the audio generated by the model.")]
        public virtual string Transcript { get; set; }
    }

    [DataContract]
    public partial class ChoiceMessage
    {
        ///<summary>
        ///The contents of the message.
        ///</summary>
        [DataMember(Name="content")]
        [ApiMember(Description="The contents of the message.")]
        public virtual string Content { get; set; }

        ///<summary>
        ///The refusal message generated by the model.
        ///</summary>
        [DataMember(Name="refusal")]
        [ApiMember(Description="The refusal message generated by the model.")]
        public virtual string Refusal { get; set; }

        ///<summary>
        ///The reasoning process used by the model.
        ///</summary>
        [DataMember(Name="reasoning")]
        [ApiMember(Description="The reasoning process used by the model.")]
        public virtual string Reasoning { get; set; }

        ///<summary>
        ///The role of the author of this message.
        ///</summary>
        [DataMember(Name="role")]
        [ApiMember(Description="The role of the author of this message.")]
        public virtual string Role { get; set; }

        ///<summary>
        ///Annotations for the message, when applicable, as when using the web search tool.
        ///</summary>
        [DataMember(Name="annotations")]
        [ApiMember(Description="Annotations for the message, when applicable, as when using the web search tool.")]
        public virtual List<ChoiceAnnotation> Annotations { get; set; }

        ///<summary>
        ///If the audio output modality is requested, this object contains data about the audio response from the model.
        ///</summary>
        [DataMember(Name="audio")]
        [ApiMember(Description="If the audio output modality is requested, this object contains data about the audio response from the model.")]
        public virtual ChoiceAudio Audio { get; set; }

        ///<summary>
        ///The tool calls generated by the model, such as function calls.
        ///</summary>
        [DataMember(Name="tool_calls")]
        [ApiMember(Description="The tool calls generated by the model, such as function calls.")]
        public virtual List<ToolCall> ToolCalls { get; set; }
    }

    public enum ResponseFormat
    {
        [EnumMember(Value="text")]
        Text,
        [EnumMember(Value="json_object")]
        JsonObject,
    }

    [DataContract]
    public partial class Tool
    {
        ///<summary>
        ///The type of the tool. Currently, only function is supported.
        ///</summary>
        [DataMember(Name="type")]
        [ApiMember(Description="The type of the tool. Currently, only function is supported.")]
        public virtual ToolType Type { get; set; }
    }

    ///<summary>
    ///The tool calls generated by the model, such as function calls.
    ///</summary>
    [Api(Description="The tool calls generated by the model, such as function calls.")]
    [DataContract]
    public partial class ToolCall
    {
        ///<summary>
        ///The ID of the tool call.
        ///</summary>
        [DataMember(Name="id")]
        [ApiMember(Description="The ID of the tool call.")]
        public virtual string Id { get; set; }

        ///<summary>
        ///The type of the tool. Currently, only `function` is supported.
        ///</summary>
        [DataMember(Name="type")]
        [ApiMember(Description="The type of the tool. Currently, only `function` is supported.")]
        public virtual string Type { get; set; }

        ///<summary>
        ///The function that the model called.
        ///</summary>
        [DataMember(Name="function")]
        [ApiMember(Description="The function that the model called.")]
        public virtual string Function { get; set; }
    }

    public enum ToolType
    {
        [EnumMember(Value="function")]
        Function,
    }

    ///<summary>
    ///Annotations for the message, when applicable, as when using the web search tool.
    ///</summary>
    [DataContract]
    public partial class UrlCitation
    {
        ///<summary>
        ///The index of the last character of the URL citation in the message.
        ///</summary>
        [DataMember(Name="end_index")]
        [ApiMember(Description="The index of the last character of the URL citation in the message.")]
        public virtual int EndIndex { get; set; }

        ///<summary>
        ///The index of the first character of the URL citation in the message.
        ///</summary>
        [DataMember(Name="start_index")]
        [ApiMember(Description="The index of the first character of the URL citation in the message.")]
        public virtual int StartIndex { get; set; }

        ///<summary>
        ///The title of the web resource.
        ///</summary>
        [DataMember(Name="title")]
        [ApiMember(Description="The title of the web resource.")]
        public virtual string Title { get; set; }

        ///<summary>
        ///The URL of the web resource.
        ///</summary>
        [DataMember(Name="url")]
        [ApiMember(Description="The URL of the web resource.")]
        public virtual string Url { get; set; }
    }

}

