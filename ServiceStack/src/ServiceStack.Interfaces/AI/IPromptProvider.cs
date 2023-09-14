using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.AI;

/// <summary>
/// The App Provider to use to generate TypeChat Schema and Prompts 
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// Create a TypeChat TypeScript Schema from a TypeChatRequest
    /// </summary>
    Task<string> CreateSchemaAsync(CancellationToken token = default);

    /// <summary>
    /// Create a TypeChat TypeScript Prompt from a User request
    /// </summary>
    Task<string> CreatePromptAsync(string userMessage, CancellationToken token = default);
}