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
    /// Associate Job with a tag group
    /// </summary>
    public virtual string? Tag { get; set; }

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

    public virtual int? DurationMs { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public virtual Dictionary<string, string>? Meta { get; set; }    
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
        
        return new ChatCompletionLog
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
        };
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