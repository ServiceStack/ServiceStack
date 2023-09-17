#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.AI;

/// <summary>
/// Abstraction to implement a TypeChat TypeScript Schema LLM provider
/// </summary>
public interface ITypeChat
{
    /// <summary>
    /// Uses LLM provider to translates a TypeChat message instruction asynchronously.
    /// </summary>
    /// <param name="request">The TypeChat request to translate.</param>
    /// <param name="token">A cancellation token to cancel the operation (optional).</param>
    /// <returns>A task representing the asynchronous operation that returns a <see cref="TypeChatResponse"/>.</returns>
    Task<TypeChatResponse> TranslateMessageAsync(TypeChatRequest request, CancellationToken token = default);
}

/// <summary>
/// The kind of Response to expect, an action to execute or a JSON message
/// </summary>
public enum TypeChatTranslator
{
    Json,
    Program,
}

/// <summary>
/// Request to process a TypeChat Request
/// </summary>
public class TypeChatRequest
{
    public TypeChatRequest(string schema, string prompt, string userMessage)
    {
        Schema = schema;
        Prompt = prompt;
        UserMessage = userMessage;
    }

    /// <summary>
    /// TypeScript Schema
    /// </summary>
    public string Schema { get; set; }
    
    /// <summary>
    /// TypeChat Prompt
    /// </summary>
    public string Prompt { get; set; }
    
    /// <summary>
    /// Chat Request
    /// </summary>
    public string UserMessage { get; }
    
    /// <summary>
    /// Path to node exe (default node in $PATH)
    /// </summary>
    public string? NodePath { get; set; }

    /// <summary>
    /// Timeout to wait for node script to complete (default 120s)
    /// </summary>
    public int NodeProcessTimeoutMs { get; set; } = 120 * 1000;

    /// <summary>
    /// Path to node TypeChat script (default typechat.mjs)
    /// </summary>
    public string? ScriptPath { get; set; }
    
    /// <summary>
    /// TypeChat Behavior we want to use (Json | Program)
    /// </summary>
    public TypeChatTranslator TypeChatTranslator { get; set; }

    /// <summary>
    /// Path to write TypeScript Schema to (default Temp File)
    /// </summary>
    public string? SchemaPath { get; set; }
    
    /// <summary>
    /// Which directory to execute the ScriptPath (default CurrentDirectory) 
    /// </summary>
    public string? WorkingDirectory { get; set; }
}

/// <summary>
/// The result of an executed TypeChat request
/// </summary>
public class TypeChatResponse
{
    /// <summary>
    /// JSON Response from a TypeChat Provider
    /// </summary>
    public string Result { get; set; }

    /// <summary>
    /// Error Information if transcription was unsuccessful
    /// </summary>
    public ResponseStatus? ResponseStatus { get; set; }
}
