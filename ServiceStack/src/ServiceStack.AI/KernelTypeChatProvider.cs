using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;

namespace ServiceStack.AI;

public class KernelTypeChat : ITypeChat
{
    public IKernel Kernel { get; }
    public KernelTypeChat(IKernel kernel) => Kernel = kernel;

    /// <summary>
    /// Service identifier.
    /// This identifies a service and is set when the AI service is registered.
    /// </summary>
    public string? ServiceId { get; set; } = null;

    /// <summary>
    /// Model identifier.
    /// This identifies the AI model these settings are configured for e.g., gpt-4, gpt-3.5-turbo
    /// </summary>
    public string? ModelId { get; set; } = null;

    public async Task<TypeChatResponse> TranslateMessageAsync(TypeChatRequest request, CancellationToken token = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(request.Prompt);
        var chatCompletionService = Kernel.GetService<IChatCompletion>();
        var result = await chatCompletionService.GenerateMessageAsync(chatHistory, new AIRequestSettings {
            ServiceId = ServiceId,
            ModelId = ModelId,
        }, cancellationToken: token);
        return new TypeChatResponse { Result = result };
    }
}
