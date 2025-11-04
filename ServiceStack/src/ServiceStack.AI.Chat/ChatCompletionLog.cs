using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.Web;

namespace ServiceStack.AI;

public class ChatCompletionLog : IMeta
{
    [AutoIncrement]
    public long Id { get; set; }
    
    /// <summary>
    /// Unique user-specified or system generated GUID for Job
    /// </summary>
    [Index(Unique = true)] public virtual string? RefId { get; set; }

    /// <summary>
    /// The ASP .NET Identity Auth User Id to populate the IRequest Context ClaimsPrincipal and User Session
    /// </summary>
    public virtual string? UserId { get; set; }

    /// <summary>
    /// The API Key, if one was used to access the Chat Service
    /// </summary>
    public virtual string? ApiKey { get; set; }

    public string Model { get; set; }
    public string? UserPrompt { get; set; }

    public string? Answer { get; set; }

    /// <summary>
    /// JSON Body of Request
    /// </summary>
    [StringLength(StringLengthAttribute.MaxText)]
    public virtual string RequestBody { get; set; }

    /// <summary>
    /// The Response DTO JSON Body
    /// </summary>
    [StringLength(StringLengthAttribute.MaxText)]
    public virtual string? ResponseBody { get; set; }

    public virtual string? ErrorCode { get; set; }

    public virtual ResponseStatus? Error { get; set; }

    [Index] public virtual DateTime CreatedDate { get; set; }

    /// <summary>
    /// Associate Request with a tag group
    /// </summary>
    public virtual string? Tag { get; set; }

    public virtual string? ThreadId { get; set; }

    public virtual int? DurationMs { get; set; }

    public virtual int? PromptTokens { get; set; }
    public virtual int? CompletionTokens { get; set; }
    
    public virtual decimal Cost { get; set; }

    public virtual string? ProviderRef { get; set; }

    public virtual string? ProviderModel { get; set; }

    public virtual string? FinishReason { get; set; }

    public virtual ModelUsage? Usage { get; set; }
    
    public virtual Dictionary<string, string>? Meta { get; set; }    
}

[DataContract]
public class ModelUsage
{
    [DataMember]
    public string? Cost { get; set; }
    [DataMember]
    public string? Input { get; set; }
    [DataMember]
    public string? Output { get; set; }
    [DataMember]
    public int? Duration { get; set; }
    public int? PromptTokens { get; set; }
    [DataMember(Name = "completion_tokens")]
    public int? CompletionTokens { get; set; }
    [DataMember]
    public int? InputCachedTokens { get; set; }
    [DataMember]
    public int? OutputCachedTokens { get; set; }
    [DataMember(Name = "audio_tokens")]
    public int? AudioTokens { get; set; }
    [DataMember(Name = "reasoning_tokens")]
    public int? ReasoningTokens { get; set; }
    [DataMember]
    public int? TotalTokens { get; set; }
}

public static class ChatCompletionLogUtils
{
    public static ChatCompletionLog ToChatCompletionLog(this IRequest req, ChatCompletion request, ChatResponse response, string? refId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);

        var userId = req.GetClaimsPrincipal()?.GetUserId();
        var apiKey = req.GetApiKey();
        userId ??= apiKey?.UserAuthId;
        userId ??= req.GetSession()?.UserAuthId;
        var duration = req.GetElapsed();
        
        var usage = response.Usage;
        var ret = new ChatCompletionLog
        {
            RefId = refId ?? req.GetTraceId() ?? Guid.NewGuid().ToString("N"),
            UserId = userId,
            ApiKey = apiKey?.Key,
            Model = request.Model,
            UserPrompt = request.GetUserPrompt(),
            Answer = response.GetAnswer(),
            RequestBody = request.ToJson(),
            ResponseBody = response.ToJson(),
            CreatedDate = DateTime.UtcNow,
            DurationMs = duration != TimeSpan.Zero ? (int)duration.TotalMilliseconds : null,
            PromptTokens = response.Usage?.PromptTokens,
            CompletionTokens = response.Usage?.CompletionTokens,
            ThreadId = request.Metadata?.TryGetValue("threadId", out var threadId) == true
                ? threadId
                : null,
        };
        ret.Usage = new()
        {
            Duration = ret.DurationMs,
            PromptTokens = usage.PromptTokens,
            CompletionTokens = usage.CompletionTokens,
            InputCachedTokens = usage.PromptTokensDetails?.CachedTokens,
            ReasoningTokens = usage.CompletionTokensDetails?.ReasoningTokens,
            AudioTokens = usage.CompletionTokensDetails?.AudioTokens,
            TotalTokens = usage.TotalTokens,
        };
        if (response.Metadata != null)
        {
            if (response.Metadata.TryGetValue("duration", out var durationStr)
                && int.TryParse(durationStr, out var durationMs))
            {
                ret.DurationMs = durationMs;
            }
            if (response.Metadata.TryGetValue("pricing", out var pricingStr))
            {
                ret.Usage.Input = pricingStr.LeftPart('/');
                ret.Usage.Output = pricingStr.RightPart('/');
                ret.Cost = (usage.PromptTokens * decimal.Parse(ret.Usage.Input) + 
                            usage.CompletionTokens * decimal.Parse(ret.Usage.Output));
            }
        }
        return ret;
    }

    public static ChatCompletionLog ToChatCompletionLog(this IRequest req, ChatCompletion request, Exception ex, string? refId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(ex);

        var userId = req.GetClaimsPrincipal()?.GetUserId();
        var apiKey = req.GetApiKey();
        userId ??= apiKey?.UserAuthId;
        userId ??= req.GetSession()?.UserAuthId;
        var duration = req.GetElapsed();
        
        var status = ex.ToResponseStatus();
        
        return new ChatCompletionLog
        {
            RefId = refId ?? req.GetTraceId() ?? Guid.NewGuid().ToString("N"),
            UserId = userId,
            ApiKey = apiKey?.Key,
            Model = request.Model,
            UserPrompt = request.GetUserPrompt(),
            RequestBody = request.ToJson(),
            ErrorCode = status.ErrorCode,
            Error = status,
            CreatedDate = DateTime.UtcNow,
            DurationMs = duration != TimeSpan.Zero ? (int)duration.TotalMilliseconds : null,
        };
    }
}