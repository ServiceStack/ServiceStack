using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Chat Client that can call OpenAI ChatCompletion API
/// </summary>
public interface IChatClient
{
    Task<ChatResponse> ChatAsync(ChatCompletion request, CancellationToken token = default);
}

/// <summary>
/// OpenAI Chat service that manages and interacts with multiple OpenAI Chat providers
/// </summary>
public interface IChatClients : IChatClient
{
    IChatClient? GetClient(string providerId);
}

/// <summary>
/// OpenAI Chat service that manages and interacts with multiple OpenAI Chat providers
/// </summary>
public class ChatClients(ILogger<ChatClients> log, ChatFeature feature) : IChatClients
{
    /// <summary>
    /// Get a specific OpenAI Chat Provider by Id
    /// </summary>
    public IChatClient? GetClient(string providerId) => 
        feature.Providers.GetValueOrDefault(providerId);

    /// <summary>
    /// Call ChatCompletion on all available providers and return the first successful response
    /// </summary>
    public async Task<ChatResponse> ChatAsync(ChatCompletion request, CancellationToken token = default)
    {
        var candidateProviders = feature.GetModelProviders(request);

        Exception? firstEx = null;
        var i = 0;
        var chatRequest = request;
        foreach (var entry in candidateProviders)
        {
            i++;
            try
            {
                var provider = entry.Value;
                chatRequest.Model = request.Model;
                var ret = await provider.ChatAsync(chatRequest, token).ConfigAwait();
                return ret;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error calling {Name} ({CandidateIndex}/{CandidatesTotal}): {Message}", 
                    i, candidateProviders.Count, entry.Key, ex.Message);
                firstEx ??= ex;
            }
        }

        firstEx ??= HttpError.NotFound($"Model {request.Model} not found");
        throw firstEx;
    }
}

public static class ChatFeatureExtensions
{
    public static IChatClient GetRequiredClient(this IChatClients clients, string providerId) => 
        clients.GetClient(providerId)
        ?? throw new Exception($"Chat Provider '{providerId}' is not available");
    public static T GetRequiredClient<T>(this IChatClients clients, string providerId) => 
        (T)(clients.GetClient(providerId)
            ?? throw new Exception($"Chat Provider '{providerId}' is not available"));
    public static OpenAiProvider GetOpenAiProvider(this IChatClients clients, string providerId) => 
        clients.GetRequiredClient<OpenAiProvider>(providerId);
    public static OllamaProvider GetOllamaProvider(this IChatClients clients, string providerId) => 
        clients.GetRequiredClient<OllamaProvider>(providerId);
    public static GoogleProvider GetGoogleProvider(this IChatClients clients, string providerId) => 
        clients.GetRequiredClient<GoogleProvider>(providerId);
}