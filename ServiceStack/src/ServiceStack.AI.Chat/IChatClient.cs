namespace ServiceStack.AI;

public interface IChatClients
{
    IChatClient? GetClient(string providerId);
}

public interface IChatClient
{
    Task<ChatResponse> ChatAsync(ChatCompletion request, CancellationToken token = default);
}

public class ChatClients(ChatFeature feature) : IChatClients
{
    public IChatClient? GetClient(string providerId) => 
        feature.Providers.GetValueOrDefault(providerId);
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