' Options:
'Date: 2025-10-10 01:20:18
'Version: 8.81
'Tip: To override a DTO option, remove "''" prefix before updating
'BaseUrl: http://localhost:5166
'
'''GlobalNamespace: 
'''MakePartial: True
'''MakeVirtual: True
'''MakeDataContractsExtensible: False
'''AddReturnMarker: True
'''AddDescriptionAsComments: True
'''AddDataContractAttributes: False
'''AddIndexesToDataMembers: False
'''AddGeneratedCodeAttributes: False
'''AddResponseStatus: False
'''AddImplicitVersion: 
'''InitializeCollections: False
'''ExportValueTypes: False
'''IncludeTypes: 
'''ExcludeTypes: 
'''AddNamespaces: 
'''AddDefaultXmlNamespace: http://schemas.servicestack.net/types

Imports System
Imports System.IO
Imports System.Collections
Imports System.Collections.Generic
Imports System.Runtime.Serialization
Imports ServiceStack
Imports ServiceStack.DataAnnotations
Imports ServiceStack.AI
Imports MyApp.ServiceModel

Namespace Global

    Namespace MyApp.ServiceModel

        Public Partial Class AdminData
            Implements IReturn(Of AdminDataResponse)
            Implements IGet
        End Class

        Public Partial Class AdminDataResponse
            Public Overridable Property PageStats As List(Of PageStats) = New List(Of PageStats)
        End Class

        '''<Summary>
        '''Booking Details
        '''</Summary>
        Public Partial Class Booking
            Inherits AuditBase
            Public Overridable Property Id As Integer
            Public Overridable Property Name As String
            Public Overridable Property RoomType As RoomType
            Public Overridable Property RoomNumber As Integer
            Public Overridable Property BookingStartDate As Date
            Public Overridable Property BookingEndDate As Date?
            Public Overridable Property Cost As Decimal
            <References(GetType(Coupon))>
            Public Overridable Property CouponId As String

            Public Overridable Property Discount As Coupon
            Public Overridable Property Notes As String
            Public Overridable Property Cancelled As Boolean?
            Public Overridable Property Employee As User
        End Class

        '''<Summary>
        '''Discount Coupons
        '''</Summary>
        Public Partial Class Coupon
            Public Overridable Property Id As String
            Public Overridable Property Description As String
            Public Overridable Property Discount As Integer
            Public Overridable Property ExpiryDate As Date
        End Class

        '''<Summary>
        '''Create a new Booking
        '''</Summary>
        <Route("/bookings", "POST")>
        <ValidateRequest(Validator:="HasRole(`Employee`)")>
        Public Partial Class CreateBooking
            Implements IReturn(Of IdResponse)
            Implements ICreateDb(Of Booking)
            '''<Summary>
            '''Name this Booking is for
            '''</Summary>
            <Validate(Validator:="NotEmpty")>
            Public Overridable Property Name As String

            Public Overridable Property RoomType As RoomType
            <Validate(Validator:="GreaterThan(0)")>
            Public Overridable Property RoomNumber As Integer

            <Validate(Validator:="GreaterThan(0)")>
            Public Overridable Property Cost As Decimal

            <Required>
            Public Overridable Property BookingStartDate As Date

            Public Overridable Property BookingEndDate As Date?
            Public Overridable Property Notes As String
            Public Overridable Property CouponId As String
        End Class

        <Route("/coupons", "POST")>
        <ValidateRequest(Validator:="HasRole(`Employee`)")>
        Public Partial Class CreateCoupon
            Implements IReturn(Of IdResponse)
            Implements ICreateDb(Of Coupon)
            <Validate(Validator:="NotEmpty")>
            Public Overridable Property Id As String

            <Validate(Validator:="NotEmpty")>
            Public Overridable Property Description As String

            <Validate(Validator:="GreaterThan(0)")>
            Public Overridable Property Discount As Integer

            <Validate(Validator:="NotNull")>
            Public Overridable Property ExpiryDate As Date
        End Class

        '''<Summary>
        '''Delete a Booking
        '''</Summary>
        <Route("/booking/{Id}", "DELETE")>
        <ValidateRequest(Validator:="HasRole(`Manager`)")>
        Public Partial Class DeleteBooking
            Implements IReturnVoid
            Implements IDeleteDb(Of Booking)
            Public Overridable Property Id As Integer
        End Class

        '''<Summary>
        '''Delete a Coupon
        '''</Summary>
        <Route("/coupons/{Id}", "DELETE")>
        <ValidateRequest(Validator:="HasRole(`Manager`)")>
        Public Partial Class DeleteCoupon
            Implements IReturnVoid
            Implements IDeleteDb(Of Coupon)
            Public Overridable Property Id As String
        End Class

        <Route("/hello/{Name}")>
        Public Partial Class Hello
            Implements IReturn(Of HelloResponse)
            Implements IGet
            Public Overridable Property Name As String
        End Class

        Public Partial Class HelloResponse
            Public Overridable Property Result As String
        End Class

        Public Partial Class PageStats
            Public Overridable Property Label As String
            Public Overridable Property Total As Integer
        End Class

        '''<Summary>
        '''Find Bookings
        '''</Summary>
        <Route("/bookings", "GET")>
        <Route("/bookings/{Id}", "GET")>
        Public Partial Class QueryBookings
            Inherits QueryDb(Of Booking)
            Implements IReturn(Of QueryResponse(Of Booking))
            Public Overridable Property Id As Integer?
        End Class

        '''<Summary>
        '''Find Coupons
        '''</Summary>
        <Route("/coupons", "GET")>
        Public Partial Class QueryCoupons
            Inherits QueryDb(Of Coupon)
            Implements IReturn(Of QueryResponse(Of Coupon))
            Public Overridable Property Id As String
        End Class

        <ValidateRequest(Validator:="IsAdmin")>
        Public Partial Class QueryUsers
            Inherits QueryDb(Of User)
            Implements IReturn(Of QueryResponse(Of User))
            Public Overridable Property Id As String
        End Class

        Public Enum RoomType
            [Single]
            [Double]
            Queen
            Twin
            Suite
        End Enum

        '''<Summary>
        '''Update an existing Booking
        '''</Summary>
        <Route("/booking/{Id}", "PATCH")>
        <ValidateRequest(Validator:="HasRole(`Employee`)")>
        Public Partial Class UpdateBooking
            Implements IReturn(Of IdResponse)
            Implements IPatchDb(Of Booking)
            Public Overridable Property Id As Integer
            Public Overridable Property Name As String
            Public Overridable Property RoomType As RoomType?
            <Validate(Validator:="GreaterThan(0)")>
            Public Overridable Property RoomNumber As Integer?

            <Validate(Validator:="GreaterThan(0)")>
            Public Overridable Property Cost As Decimal?

            Public Overridable Property BookingStartDate As Date?
            Public Overridable Property BookingEndDate As Date?
            Public Overridable Property Notes As String
            Public Overridable Property CouponId As String
            Public Overridable Property Cancelled As Boolean?
        End Class

        <Route("/coupons/{Id}", "PATCH")>
        <ValidateRequest(Validator:="HasRole(`Employee`)")>
        Public Partial Class UpdateCoupon
            Implements IReturn(Of IdResponse)
            Implements IPatchDb(Of Coupon)
            Public Overridable Property Id As String
            <Validate(Validator:="NotEmpty")>
            Public Overridable Property Description As String

            <Validate(Validator:="NotNull")>
            <Validate(Validator:="GreaterThan(0)")>
            Public Overridable Property Discount As Integer?

            <Validate(Validator:="NotNull")>
            Public Overridable Property ExpiryDate As Date?
        End Class

        Public Partial Class User
            Public Overridable Property Id As String
            Public Overridable Property UserName As String
            Public Overridable Property FirstName As String
            Public Overridable Property LastName As String
            Public Overridable Property DisplayName As String
            Public Overridable Property ProfileUrl As String
        End Class
    End Namespace

    Namespace ServiceStack.AI

        '''<Summary>
        '''Audio content part
        '''</Summary>
        <DataContract>
        Public Partial Class AiAudioContent
            Inherits AiContent
            '''<Summary>
            '''The audio input for this content.
            '''</Summary>
            <DataMember(Name:="input_audio")>
            <ApiMember(Description:="The audio input for this content.")>
            Public Overridable Property InputAudio As AiInputAudio
        End Class

        '''<Summary>
        '''Parameters for audio output. Required when audio output is requested with modalities: [audio]
        '''</Summary>
        <DataContract>
        Public Partial Class AiChatAudio
            '''<Summary>
            '''Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.
            '''</Summary>
            <DataMember(Name:="format")>
            <ApiMember(Description:="Specifies the output audio format. Must be one of wav, mp3, flac, opus, or pcm16.")>
            Public Overridable Property Format As String

            '''<Summary>
            '''The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.
            '''</Summary>
            <DataMember(Name:="voice")>
            <ApiMember(Description:="The voice the model uses to respond. Supported voices are alloy, ash, ballad, coral, echo, fable, nova, onyx, sage, and shimmer.")>
            Public Overridable Property Voice As String
        End Class

        '''<Summary>
        '''Usage statistics for the completion request.
        '''</Summary>
        <Api(Description:="Usage statistics for the completion request.")>
        <DataContract>
        Public Partial Class AiCompletionUsage
            '''<Summary>
            '''When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
            '''</Summary>
            <DataMember(Name:="accepted_prediction_tokens")>
            <ApiMember(Description:="When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.

")>
            Public Overridable Property AcceptedPredictionTokens As Integer

            '''<Summary>
            '''Audio input tokens generated by the model.
            '''</Summary>
            <DataMember(Name:="audio_tokens")>
            <ApiMember(Description:="Audio input tokens generated by the model.")>
            Public Overridable Property AudioTokens As Integer

            '''<Summary>
            '''Tokens generated by the model for reasoning.
            '''</Summary>
            <DataMember(Name:="reasoning_tokens")>
            <ApiMember(Description:="Tokens generated by the model for reasoning.")>
            Public Overridable Property ReasoningTokens As Integer

            '''<Summary>
            '''When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.
            '''</Summary>
            <DataMember(Name:="rejected_prediction_tokens")>
            <ApiMember(Description:="When using Predicted Outputs, the number of tokens in the prediction that did not appear in the completion.")>
            Public Overridable Property RejectedPredictionTokens As Integer
        End Class

        <DataContract>
        Public Partial Class AiContent
            '''<Summary>
            '''The type of the content part.
            '''</Summary>
            <DataMember(Name:="type")>
            <ApiMember(Description:="The type of the content part.")>
            Public Overridable Property Type As String
        End Class

        '''<Summary>
        '''File content part
        '''</Summary>
        <DataContract>
        Public Partial Class AiFile
            '''<Summary>
            '''The URL or base64 encoded file data, used when passing the file to the model as a string.
            '''</Summary>
            <DataMember(Name:="file_data")>
            <ApiMember(Description:="The URL or base64 encoded file data, used when passing the file to the model as a string.")>
            Public Overridable Property FileData As String

            '''<Summary>
            '''The name of the file, used when passing the file to the model as a string.
            '''</Summary>
            <DataMember(Name:="filename")>
            <ApiMember(Description:="The name of the file, used when passing the file to the model as a string.")>
            Public Overridable Property Filename As String

            '''<Summary>
            '''The ID of an uploaded file to use as input.
            '''</Summary>
            <DataMember(Name:="file_id")>
            <ApiMember(Description:="The ID of an uploaded file to use as input.")>
            Public Overridable Property FileId As String
        End Class

        '''<Summary>
        '''File content part
        '''</Summary>
        <DataContract>
        Public Partial Class AiFileContent
            Inherits AiContent
            '''<Summary>
            '''The file input for this content.
            '''</Summary>
            <DataMember(Name:="file")>
            <ApiMember(Description:="The file input for this content.")>
            Public Overridable Property File As AiFile
        End Class

        '''<Summary>
        '''Image content part
        '''</Summary>
        <DataContract>
        Public Partial Class AiImageContent
            Inherits AiContent
            '''<Summary>
            '''The image for this content.
            '''</Summary>
            <DataMember(Name:="image_url")>
            <ApiMember(Description:="The image for this content.")>
            Public Overridable Property ImageUrl As AiImageUrl
        End Class

        Public Partial Class AiImageUrl
            '''<Summary>
            '''Either a URL of the image or the base64 encoded image data.
            '''</Summary>
            <DataMember(Name:="url")>
            <ApiMember(Description:="Either a URL of the image or the base64 encoded image data.")>
            Public Overridable Property Url As String
        End Class

        '''<Summary>
        '''Audio content part
        '''</Summary>
        <DataContract>
        Public Partial Class AiInputAudio
            '''<Summary>
            '''URL or Base64 encoded audio data.
            '''</Summary>
            <DataMember(Name:="data")>
            <ApiMember(Description:="URL or Base64 encoded audio data.")>
            Public Overridable Property Data As String

            '''<Summary>
            '''The format of the encoded audio data. Currently supports 'wav' and 'mp3'.
            '''</Summary>
            <DataMember(Name:="format")>
            <ApiMember(Description:="The format of the encoded audio data. Currently supports 'wav' and 'mp3'.")>
            Public Overridable Property Format As String
        End Class

        '''<Summary>
        '''A list of messages comprising the conversation so far.
        '''</Summary>
        <Api(Description:="A list of messages comprising the conversation so far.")>
        <DataContract>
        Public Partial Class AiMessage
            '''<Summary>
            '''The contents of the message.
            '''</Summary>
            <DataMember(Name:="content")>
            <ApiMember(Description:="The contents of the message.")>
            Public Overridable Property Content As List(Of AiContent)

            '''<Summary>
            '''The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.
            '''</Summary>
            <DataMember(Name:="role")>
            <ApiMember(Description:="The role of the author of this message. Valid values are `system`, `user`, `assistant` and `tool`.")>
            Public Overridable Property Role As String

            '''<Summary>
            '''An optional name for the participant. Provides the model information to differentiate between participants of the same role.
            '''</Summary>
            <DataMember(Name:="name")>
            <ApiMember(Description:="An optional name for the participant. Provides the model information to differentiate between participants of the same role.")>
            Public Overridable Property Name As String

            '''<Summary>
            '''The tool calls generated by the model, such as function calls.
            '''</Summary>
            <DataMember(Name:="tool_calls")>
            <ApiMember(Description:="The tool calls generated by the model, such as function calls.")>
            Public Overridable Property ToolCalls As List(Of ToolCall)

            '''<Summary>
            '''Tool call that this message is responding to.
            '''</Summary>
            <DataMember(Name:="tool_call_id")>
            <ApiMember(Description:="Tool call that this message is responding to.")>
            Public Overridable Property ToolCallId As String
        End Class

        '''<Summary>
        '''Breakdown of tokens used in the prompt.
        '''</Summary>
        <Api(Description:="Breakdown of tokens used in the prompt.")>
        <DataContract>
        Public Partial Class AiPromptUsage
            '''<Summary>
            '''When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.
            '''</Summary>
            <DataMember(Name:="accepted_prediction_tokens")>
            <ApiMember(Description:="When using Predicted Outputs, the number of tokens in the prediction that appeared in the completion.

")>
            Public Overridable Property AcceptedPredictionTokens As Integer

            '''<Summary>
            '''Audio input tokens present in the prompt.
            '''</Summary>
            <DataMember(Name:="audio_tokens")>
            <ApiMember(Description:="Audio input tokens present in the prompt.")>
            Public Overridable Property AudioTokens As Integer

            '''<Summary>
            '''Cached tokens present in the prompt.
            '''</Summary>
            <DataMember(Name:="cached_tokens")>
            <ApiMember(Description:="Cached tokens present in the prompt.")>
            Public Overridable Property CachedTokens As Integer
        End Class

        <DataContract>
        Public Partial Class AiResponseFormat
            '''<Summary>
            '''An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.
            '''</Summary>
            <DataMember(Name:="response_format")>
            <ApiMember(Description:="An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than gpt-3.5-turbo-1106.")>
            Public Overridable Property Type As ResponseFormat
        End Class

        '''<Summary>
        '''Text content part
        '''</Summary>
        <DataContract>
        Public Partial Class AiTextContent
            Inherits AiContent
            '''<Summary>
            '''The text content.
            '''</Summary>
            <DataMember(Name:="text")>
            <ApiMember(Description:="The text content.")>
            Public Overridable Property Text As String
        End Class

        '''<Summary>
        '''Usage statistics for the completion request.
        '''</Summary>
        <Api(Description:="Usage statistics for the completion request.")>
        <DataContract>
        Public Partial Class AiUsage
            '''<Summary>
            '''Number of tokens in the generated completion.
            '''</Summary>
            <DataMember(Name:="completion_tokens")>
            <ApiMember(Description:="Number of tokens in the generated completion.")>
            Public Overridable Property CompletionTokens As Integer

            '''<Summary>
            '''Number of tokens in the prompt.
            '''</Summary>
            <DataMember(Name:="prompt_tokens")>
            <ApiMember(Description:="Number of tokens in the prompt.")>
            Public Overridable Property PromptTokens As Integer

            '''<Summary>
            '''Total number of tokens used in the request (prompt + completion).
            '''</Summary>
            <DataMember(Name:="total_tokens")>
            <ApiMember(Description:="Total number of tokens used in the request (prompt + completion).")>
            Public Overridable Property TotalTokens As Integer

            '''<Summary>
            '''Breakdown of tokens used in a completion.
            '''</Summary>
            <DataMember(Name:="completion_tokens_details")>
            <ApiMember(Description:="Breakdown of tokens used in a completion.")>
            Public Overridable Property CompletionTokensDetails As AiCompletionUsage

            '''<Summary>
            '''Breakdown of tokens used in the prompt.
            '''</Summary>
            <DataMember(Name:="prompt_tokens_details")>
            <ApiMember(Description:="Breakdown of tokens used in the prompt.")>
            Public Overridable Property PromptTokensDetails As AiPromptUsage
        End Class

        '''<Summary>
        '''Chat Completions API (OpenAI-Compatible)
        '''</Summary>
        <Route("/v1/chat/completions", "POST")>
        <Api(Description:="Chat Completions API (OpenAI-Compatible)")>
        <DataContract>
        Public Partial Class ChatCompletion
            Implements IReturn(Of ChatResponse)
            Implements IPost
            '''<Summary>
            '''The messages to generate chat completions for.
            '''</Summary>
            <DataMember(Name:="messages")>
            <ApiMember(Description:="The messages to generate chat completions for.")>
            Public Overridable Property Messages As List(Of AiMessage) = New List(Of AiMessage)

            '''<Summary>
            '''ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API
            '''</Summary>
            <DataMember(Name:="model")>
            <ApiMember(Description:="ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API")>
            Public Overridable Property Model As String

            '''<Summary>
            '''Parameters for audio output. Required when audio output is requested with modalities: [audio]
            '''</Summary>
            <DataMember(Name:="audio")>
            <ApiMember(Description:="Parameters for audio output. Required when audio output is requested with modalities: [audio]")>
            Public Overridable Property Audio As AiChatAudio

            '''<Summary>
            '''Modify the likelihood of specified tokens appearing in the completion.
            '''</Summary>
            <DataMember(Name:="logit_bias")>
            <ApiMember(Description:="Modify the likelihood of specified tokens appearing in the completion.")>
            Public Overridable Property LogitBias As Dictionary(Of Integer, Integer)

            '''<Summary>
            '''Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
            '''</Summary>
            <DataMember(Name:="metadata")>
            <ApiMember(Description:="Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")>
            Public Overridable Property Metadata As Dictionary(Of String, String)

            '''<Summary>
            '''Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.
            '''</Summary>
            <DataMember(Name:="reasoning_effort")>
            <ApiMember(Description:="Constrains effort on reasoning for reasoning models. Currently supported values are minimal, low, medium, and high (none, default). Reducing reasoning effort can result in faster responses and fewer tokens used on reasoning in a response.")>
            Public Overridable Property ReasoningEffort As String

            '''<Summary>
            '''An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.
            '''</Summary>
            <DataMember(Name:="response_format")>
            <ApiMember(Description:="An object specifying the format that the model must output. Compatible with GPT-4 Turbo and all GPT-3.5 Turbo models newer than `gpt-3.5-turbo-1106`. Setting Type to ResponseFormat.JsonObject enables JSON mode, which guarantees the message the model generates is valid JSON.")>
            Public Overridable Property ResponseFormat As AiResponseFormat

            '''<Summary>
            '''Specifies the processing type used for serving the request.
            '''</Summary>
            <DataMember(Name:="service_tier")>
            <ApiMember(Description:="Specifies the processing type used for serving the request.")>
            Public Overridable Property ServiceTier As String

            '''<Summary>
            '''Up to 4 sequences where the API will stop generating further tokens.
            '''</Summary>
            <DataMember(Name:="stop")>
            <ApiMember(Description:="Up to 4 sequences where the API will stop generating further tokens.")>
            Public Overridable Property [Stop] As List(Of String)

            '''<Summary>
            '''Output types that you would like the model to generate. Most models are capable of generating text, which is the default:
            '''</Summary>
            <DataMember(Name:="modalities")>
            <ApiMember(Description:="Output types that you would like the model to generate. Most models are capable of generating text, which is the default:")>
            Public Overridable Property Modalities As List(Of String)

            '''<Summary>
            '''Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.
            '''</Summary>
            <DataMember(Name:="prompt_cache_key")>
            <ApiMember(Description:="Used by OpenAI to cache responses for similar requests to optimize your cache hit rates.")>
            Public Overridable Property PromptCacheKey As String

            '''<Summary>
            '''A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.
            '''</Summary>
            <DataMember(Name:="safety_identifier")>
            <ApiMember(Description:="A stable identifier used to help detect users of your application that may be violating OpenAI's usage policies. The IDs should be a string that uniquely identifies each user.")>
            Public Overridable Property SafetyIdentifier As String

            '''<Summary>
            '''A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.
            '''</Summary>
            <DataMember(Name:="tools")>
            <ApiMember(Description:="A list of tools the model may call. Currently, only functions are supported as a tool. Use this to provide a list of functions the model may generate JSON inputs for. A max of 128 functions are supported.")>
            Public Overridable Property Tools As List(Of Tool)

            '''<Summary>
            '''Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.
            '''</Summary>
            <DataMember(Name:="verbosity")>
            <ApiMember(Description:="Constrains the verbosity of the model's response. Lower values will result in more concise responses, while higher values will result in more verbose responses. Currently supported values are low, medium, and high.")>
            Public Overridable Property Verbosity As String

            '''<Summary>
            '''What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
            '''</Summary>
            <DataMember(Name:="temperature")>
            <ApiMember(Description:="What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.")>
            Public Overridable Property Temperature As Double?

            '''<Summary>
            '''An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.
            '''</Summary>
            <DataMember(Name:="max_completion_tokens")>
            <ApiMember(Description:="An upper bound for the number of tokens that can be generated for a completion, including visible output tokens and reasoning tokens.")>
            Public Overridable Property MaxCompletionTokens As Integer?

            '''<Summary>
            '''An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
            '''</Summary>
            <DataMember(Name:="top_logprobs")>
            <ApiMember(Description:="An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.")>
            Public Overridable Property TopLogprobs As Integer?

            '''<Summary>
            '''An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.
            '''</Summary>
            <DataMember(Name:="top_p")>
            <ApiMember(Description:="An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.")>
            Public Overridable Property TopP As Double?

            '''<Summary>
            '''Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.
            '''</Summary>
            <DataMember(Name:="frequency_penalty")>
            <ApiMember(Description:="Number between `-2.0` and `2.0`. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.")>
            Public Overridable Property FrequencyPenalty As Double?

            '''<Summary>
            '''Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.
            '''</Summary>
            <DataMember(Name:="presence_penalty")>
            <ApiMember(Description:="Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.")>
            Public Overridable Property PresencePenalty As Double?

            '''<Summary>
            '''This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.
            '''</Summary>
            <DataMember(Name:="seed")>
            <ApiMember(Description:="This feature is in Beta. If specified, our system will make a best effort to sample deterministically, such that repeated requests with the same seed and parameters should return the same result. Determinism is not guaranteed, and you should refer to the system_fingerprint response parameter to monitor changes in the backend.")>
            Public Overridable Property Seed As Integer?

            '''<Summary>
            '''How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.
            '''</Summary>
            <DataMember(Name:="n")>
            <ApiMember(Description:="How many chat completion choices to generate for each input message. Note that you will be charged based on the number of generated tokens across all of the choices. Keep `n` as `1` to minimize costs.")>
            Public Overridable Property N As Integer?

            '''<Summary>
            '''Whether or not to store the output of this chat completion request for use in our model distillation or evals products.
            '''</Summary>
            <DataMember(Name:="store")>
            <ApiMember(Description:="Whether or not to store the output of this chat completion request for use in our model distillation or evals products.")>
            Public Overridable Property Store As Boolean?

            '''<Summary>
            '''Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
            '''</Summary>
            <DataMember(Name:="logprobs")>
            <ApiMember(Description:="Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.")>
            Public Overridable Property Logprobs As Boolean?

            '''<Summary>
            '''Whether to enable parallel function calling during tool use.
            '''</Summary>
            <DataMember(Name:="parallel_tool_calls")>
            <ApiMember(Description:="Whether to enable parallel function calling during tool use.")>
            Public Overridable Property ParallelToolCalls As Boolean?

            '''<Summary>
            '''Whether to enable thinking mode for some Qwen models and providers.
            '''</Summary>
            <DataMember(Name:="enable_thinking")>
            <ApiMember(Description:="Whether to enable thinking mode for some Qwen models and providers.")>
            Public Overridable Property EnableThinking As Boolean?

            '''<Summary>
            '''If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.
            '''</Summary>
            <DataMember(Name:="stream")>
            <ApiMember(Description:="If set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only server-sent events as they become available, with the stream terminated by a `data: [DONE]` message.")>
            Public Overridable Property Stream As Boolean?
        End Class

        <DataContract>
        Public Partial Class ChatResponse
            '''<Summary>
            '''A unique identifier for the chat completion.
            '''</Summary>
            <DataMember(Name:="id")>
            <ApiMember(Description:="A unique identifier for the chat completion.")>
            Public Overridable Property Id As String

            '''<Summary>
            '''A list of chat completion choices. Can be more than one if n is greater than 1.
            '''</Summary>
            <DataMember(Name:="choices")>
            <ApiMember(Description:="A list of chat completion choices. Can be more than one if n is greater than 1.")>
            Public Overridable Property Choices As List(Of Choice) = New List(Of Choice)

            '''<Summary>
            '''The Unix timestamp (in seconds) of when the chat completion was created.
            '''</Summary>
            <DataMember(Name:="created")>
            <ApiMember(Description:="The Unix timestamp (in seconds) of when the chat completion was created.")>
            Public Overridable Property Created As Long

            '''<Summary>
            '''The model used for the chat completion.
            '''</Summary>
            <DataMember(Name:="model")>
            <ApiMember(Description:="The model used for the chat completion.")>
            Public Overridable Property Model As String

            '''<Summary>
            '''This fingerprint represents the backend configuration that the model runs with.
            '''</Summary>
            <DataMember(Name:="system_fingerprint")>
            <ApiMember(Description:="This fingerprint represents the backend configuration that the model runs with.")>
            Public Overridable Property SystemFingerprint As String

            '''<Summary>
            '''The object type, which is always chat.completion.
            '''</Summary>
            <DataMember(Name:="object")>
            <ApiMember(Description:="The object type, which is always chat.completion.")>
            Public Overridable Property [Object] As String

            '''<Summary>
            '''Specifies the processing type used for serving the request.
            '''</Summary>
            <DataMember(Name:="service_tier")>
            <ApiMember(Description:="Specifies the processing type used for serving the request.")>
            Public Overridable Property ServiceTier As String

            '''<Summary>
            '''Usage statistics for the completion request.
            '''</Summary>
            <DataMember(Name:="usage")>
            <ApiMember(Description:="Usage statistics for the completion request.")>
            Public Overridable Property Usage As AiUsage

            '''<Summary>
            '''Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.
            '''</Summary>
            <DataMember(Name:="metadata")>
            <ApiMember(Description:="Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format.")>
            Public Overridable Property Metadata As Dictionary(Of String, String)

            <DataMember(Name:="responseStatus")>
            Public Overridable Property ResponseStatus As ResponseStatus
        End Class

        Public Partial Class Choice
            '''<Summary>
            '''The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool
            '''</Summary>
            <DataMember(Name:="finish_reason")>
            <ApiMember(Description:="The reason the model stopped generating tokens. This will be stop if the model hit a natural stop point or a provided stop sequence, length if the maximum number of tokens specified in the request was reached, content_filter if content was omitted due to a flag from our content filters, tool_calls if the model called a tool")>
            Public Overridable Property FinishReason As String

            '''<Summary>
            '''The index of the choice in the list of choices.
            '''</Summary>
            <DataMember(Name:="index")>
            <ApiMember(Description:="The index of the choice in the list of choices.")>
            Public Overridable Property Index As Integer

            '''<Summary>
            '''A chat completion message generated by the model.
            '''</Summary>
            <DataMember(Name:="message")>
            <ApiMember(Description:="A chat completion message generated by the model.")>
            Public Overridable Property Message As ChoiceMessage
        End Class

        '''<Summary>
        '''Annotations for the message, when applicable, as when using the web search tool.
        '''</Summary>
        <DataContract>
        Public Partial Class ChoiceAnnotation
            '''<Summary>
            '''The type of the URL citation. Always url_citation.
            '''</Summary>
            <DataMember(Name:="type")>
            <ApiMember(Description:="The type of the URL citation. Always url_citation.")>
            Public Overridable Property Type As String

            '''<Summary>
            '''A URL citation when using web search.
            '''</Summary>
            <DataMember(Name:="url_citation")>
            <ApiMember(Description:="A URL citation when using web search.")>
            Public Overridable Property UrlCitation As UrlCitation
        End Class

        '''<Summary>
        '''If the audio output modality is requested, this object contains data about the audio response from the model.
        '''</Summary>
        <DataContract>
        Public Partial Class ChoiceAudio
            '''<Summary>
            '''Base64 encoded audio bytes generated by the model, in the format specified in the request.
            '''</Summary>
            <DataMember(Name:="data")>
            <ApiMember(Description:="Base64 encoded audio bytes generated by the model, in the format specified in the request.")>
            Public Overridable Property Data As String

            '''<Summary>
            '''The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.
            '''</Summary>
            <DataMember(Name:="expires_at")>
            <ApiMember(Description:="The Unix timestamp (in seconds) for when this audio response will no longer be accessible on the server for use in multi-turn conversations.")>
            Public Overridable Property ExpiresAt As Integer

            '''<Summary>
            '''Unique identifier for this audio response.
            '''</Summary>
            <DataMember(Name:="id")>
            <ApiMember(Description:="Unique identifier for this audio response.")>
            Public Overridable Property Id As String

            '''<Summary>
            '''Transcript of the audio generated by the model.
            '''</Summary>
            <DataMember(Name:="transcript")>
            <ApiMember(Description:="Transcript of the audio generated by the model.")>
            Public Overridable Property Transcript As String
        End Class

        <DataContract>
        Public Partial Class ChoiceMessage
            '''<Summary>
            '''The contents of the message.
            '''</Summary>
            <DataMember(Name:="content")>
            <ApiMember(Description:="The contents of the message.")>
            Public Overridable Property Content As String

            '''<Summary>
            '''The refusal message generated by the model.
            '''</Summary>
            <DataMember(Name:="refusal")>
            <ApiMember(Description:="The refusal message generated by the model.")>
            Public Overridable Property Refusal As String

            '''<Summary>
            '''The reasoning process used by the model.
            '''</Summary>
            <DataMember(Name:="reasoning")>
            <ApiMember(Description:="The reasoning process used by the model.")>
            Public Overridable Property Reasoning As String

            '''<Summary>
            '''The role of the author of this message.
            '''</Summary>
            <DataMember(Name:="role")>
            <ApiMember(Description:="The role of the author of this message.")>
            Public Overridable Property Role As String

            '''<Summary>
            '''Annotations for the message, when applicable, as when using the web search tool.
            '''</Summary>
            <DataMember(Name:="annotations")>
            <ApiMember(Description:="Annotations for the message, when applicable, as when using the web search tool.")>
            Public Overridable Property Annotations As List(Of ChoiceAnnotation)

            '''<Summary>
            '''If the audio output modality is requested, this object contains data about the audio response from the model.
            '''</Summary>
            <DataMember(Name:="audio")>
            <ApiMember(Description:="If the audio output modality is requested, this object contains data about the audio response from the model.")>
            Public Overridable Property Audio As ChoiceAudio

            '''<Summary>
            '''The tool calls generated by the model, such as function calls.
            '''</Summary>
            <DataMember(Name:="tool_calls")>
            <ApiMember(Description:="The tool calls generated by the model, such as function calls.")>
            Public Overridable Property ToolCalls As List(Of ToolCall)
        End Class

        Public Enum ResponseFormat
            <EnumMember(Value:="text")>
            Text
            <EnumMember(Value:="json_object")>
            JsonObject
        End Enum

        <DataContract>
        Public Partial Class Tool
            '''<Summary>
            '''The type of the tool. Currently, only function is supported.
            '''</Summary>
            <DataMember(Name:="type")>
            <ApiMember(Description:="The type of the tool. Currently, only function is supported.")>
            Public Overridable Property Type As ToolType
        End Class

        '''<Summary>
        '''The tool calls generated by the model, such as function calls.
        '''</Summary>
        <Api(Description:="The tool calls generated by the model, such as function calls.")>
        <DataContract>
        Public Partial Class ToolCall
            '''<Summary>
            '''The ID of the tool call.
            '''</Summary>
            <DataMember(Name:="id")>
            <ApiMember(Description:="The ID of the tool call.")>
            Public Overridable Property Id As String

            '''<Summary>
            '''The type of the tool. Currently, only `function` is supported.
            '''</Summary>
            <DataMember(Name:="type")>
            <ApiMember(Description:="The type of the tool. Currently, only `function` is supported.")>
            Public Overridable Property Type As String

            '''<Summary>
            '''The function that the model called.
            '''</Summary>
            <DataMember(Name:="function")>
            <ApiMember(Description:="The function that the model called.")>
            Public Overridable Property [Function] As String
        End Class

        Public Enum ToolType
            <EnumMember(Value:="function")>
            [Function]
        End Enum

        '''<Summary>
        '''Annotations for the message, when applicable, as when using the web search tool.
        '''</Summary>
        <DataContract>
        Public Partial Class UrlCitation
            '''<Summary>
            '''The index of the last character of the URL citation in the message.
            '''</Summary>
            <DataMember(Name:="end_index")>
            <ApiMember(Description:="The index of the last character of the URL citation in the message.")>
            Public Overridable Property EndIndex As Integer

            '''<Summary>
            '''The index of the first character of the URL citation in the message.
            '''</Summary>
            <DataMember(Name:="start_index")>
            <ApiMember(Description:="The index of the first character of the URL citation in the message.")>
            Public Overridable Property StartIndex As Integer

            '''<Summary>
            '''The title of the web resource.
            '''</Summary>
            <DataMember(Name:="title")>
            <ApiMember(Description:="The title of the web resource.")>
            Public Overridable Property Title As String

            '''<Summary>
            '''The URL of the web resource.
            '''</Summary>
            <DataMember(Name:="url")>
            <ApiMember(Description:="The URL of the web resource.")>
            Public Overridable Property Url As String
        End Class
    End Namespace
End Namespace

