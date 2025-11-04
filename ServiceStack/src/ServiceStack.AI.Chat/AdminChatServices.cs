using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;

namespace ServiceStack.AI;

[ExcludeMetadata, Tag(TagNames.Admin), ExplicitAutoQuery]
public class AdminQueryChatCompletionLogs : QueryDb<ChatCompletionLog>
{
    
    public DateTime? Month { get; set; }
}

public class AdminChatServices(IAutoQueryDb autoQuery) 
    : Service
{
    private ChatFeature AssertRequiredRole()
    {
        var feature = AssertPlugin<ChatFeature>();
        RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);
        return feature;
    }

    public object Any(AdminQueryChatCompletionLogs request)
    {
        var feature = AssertRequiredRole();
        var chatStore = feature.ChatStore
            ?? throw new Exception("ChatStore is not configured");
        var month = request.Month ?? DateTime.UtcNow;
        using var monthDb = chatStore.OpenMonthDb(month);
        var q = autoQuery.CreateQuery(request, base.Request, monthDb);
        return autoQuery.Execute(request, q, base.Request, monthDb);        
    }
}
