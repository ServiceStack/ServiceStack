using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

public class QueueCheckUrl : IPost, IReturn<QueueCheckUrlResponse>
{
    [ValidateNotEmpty]
    public required string Url { get; set; }
    
    [ApiMember(Description = "Specify a user-defined UUID for the Job")]
    public string? RefId { get; set; }
    [ApiMember(Description = "Maintain a Reference to a parent Job")] 
    public long? ParentId { get; set; }
    [ApiMember(Description = "Named Worker Thread to execute Job on")]
    public string? Worker { get; set; }
    [ApiMember(Description = "Only run Job after date")]
    public DateTime? RunAfter { get; set; }
    [ApiMember(Description = "Command to Execute after successful completion of Job")]
    public string? Callback { get; set; }
    [ApiMember(Description = "Only execute job after successful completion of Parent Job")]
    public long? DependsOn { get; set; }
    [ApiMember(Description = "The ASP .NET Identity Auth User Id to populate the IRequest Context ClaimsPrincipal and User Session")]
    public string? UserId { get; set; }
    [ApiMember(Description = "How many times to attempt to retry Job on failure, default 2")]
    public virtual int? RetryLimit { get; set; }
    [ApiMember(Description = "Maintain a reference to a callback URL")]
    public string? ReplyTo { get; set; }
    [ApiMember(Description = "Associate Job with a tag group")]
    public string? Tag { get; set; }
    public virtual string? BatchId { get; set; }
    public string? CreatedBy { get; set; }
    public int? TimeoutSecs { get; set; }
}

public class QueueCheckUrlResponse
{
    public long Id { get; set; }
    public string RefId { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}
