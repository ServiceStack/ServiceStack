using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ServiceStack.AI;

public class KernelTypeChat : ITypeChat
{
    public Kernel Kernel { get; }

    public KernelTypeChat(Kernel kernel)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        Kernel = kernel;
    }

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
        ArgumentNullException.ThrowIfNull(request);

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(request.Prompt ?? string.Empty);
        var chatCompletionService = !string.IsNullOrEmpty(ServiceId)
            ? Kernel.GetRequiredService<IChatCompletionService>(ServiceId)
            : Kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, new OpenAIPromptExecutionSettings {
            ModelId = ModelId,
        }, kernel:Kernel, cancellationToken: token);
        return new TypeChatResponse { Result = result?.Content ?? string.Empty };
    }
}
