using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// The OpenAI-compatible /v1/chat/completions endpoint (typed ServiceStack service).
/// All other Chat UI endpoints are dispatched through ChatFeature's RouteRegistry.
/// Auth: Identity cookie OR Bearer API Key (when ApiKeysFeature is registered).
/// </summary>
public class ChatServices : Service
{
    public async Task<object> Post(ChatCompletion request)
    {
        var feature = AssertPlugin<ChatFeature>();

        var (isAuthenticated, _) = feature.ChatAuth.CheckAuth(Request!);
        if (!isAuthenticated)
            throw HttpError.Unauthorized("Authentication required");
        if (feature.ValidateRequest != null)
        {
            var error = await feature.ValidateRequest(Request!).ConfigAwait();
            if (error != null)
                return error;
        }

        // the request binder preserves the raw OpenAI JSON, which is richer than the typed DTO
        var chat = Request!.Items.TryGetValue(ChatFeature.ChatJsonKey, out var oChat) && oChat is JsonObject rawChat
            ? rawChat
            : ChatJson.ParseObject(request.ToJson());

        var context = ChatContext.FromChat(chat, feature.ChatAuth.GetUserName(Request!), request: Request);

        var response = await feature.ChatCompletionAsync(chat, context).ConfigAwait();
        // write the provider's JSON verbatim (re-serializing would lose provider-specific fields)
        return new HttpResult(response.ToJsonString(ChatJson.Options).ToUtf8Bytes(), MimeTypes.Json);
    }
}
