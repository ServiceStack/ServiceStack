using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// In-process client for the OpenAI-compatible Chat Completions API — the same pipeline that backs
/// POST /v1/chat/completions (provider selection, retry/failover, the tool-execution loop, usage
/// and cost accounting, extension filters) without the HTTP round-trip.
/// </summary>
public interface IChatClient
{
    Task<ChatResponse> ChatAsync(ChatCompletion request, CancellationToken token=default);
}

public class ChatClient(ChatFeature feature) : IChatClient
{
    /// <summary>
    /// Run a completion through the chat pipeline. Throws on failure — the same exceptions the
    /// service surfaces, e.g. HttpError.NotFound when no configured provider serves the model.
    /// </summary>
    public async Task<ChatResponse> ChatAsync(ChatCompletion request, CancellationToken token = default)
    {
        var chat = ToChatJson(request);

        // there's no IRequest to authenticate, so in-process callers opt into a user with
        // metadata["user"]; null attributes threads/usage to the "default" user, matching how the
        // feature behaves with RequireAuth = false
        var user = request.Metadata?.GetValueOrDefault("user");

        var context = ChatContext.FromChat(chat, user, token);
        var response = await feature.ChatCompletionAsync(chat, context).ConfigAwait();

        return FromChatJson(response);
    }

    /// <summary>
    /// Typed request -> the OpenAI JSON the pipeline operates on. ServiceStack.Text honours the
    /// DTOs' [DataMember] names and omits nulls, so this already is snake_case wire format, with
    /// polymorphic content parts (text/image_url/input_audio/file) written as their own shapes.
    /// </summary>
    public static JsonObject ToChatJson(ChatCompletion request) =>
        ChatJson.ParseObject(request.ToJson());

    /// <summary>
    /// Provider JSON -> typed response. The wire response is richer than the DTO models (providers
    /// add their own fields), so anything unmapped is dropped here; the HTTP service returns the
    /// JSON verbatim instead, which is why it doesn't go through this.
    /// </summary>
    public static ChatResponse FromChatJson(JsonObject response) =>
        response.ToJsonString(ChatJson.Options).FromJson<ChatResponse>();
}
