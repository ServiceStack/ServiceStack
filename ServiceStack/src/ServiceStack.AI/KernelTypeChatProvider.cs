using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;

namespace ServiceStack.AI;

public class KernelTypeChat : ITypeChat
{
    public IKernel Kernel { get; }
    public KernelTypeChat(IKernel kernel) => Kernel = kernel;

    public async Task<TypeChatResponse> TranslateMessageAsync(TypeChatRequest request, CancellationToken token = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(request.Prompt);
        var chatCompletionService = Kernel.GetService<IChatCompletion>();
        var result = await chatCompletionService.GenerateMessageAsync(chatHistory, new ChatRequestSettings {
            Temperature = 0.0,
        }, cancellationToken: token);
        return new TypeChatResponse { Result = result };
    }
}
