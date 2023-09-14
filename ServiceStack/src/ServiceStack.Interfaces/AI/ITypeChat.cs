using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.AI;

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

public class TypeChatResponse
{
    /// <summary>
    /// JSON Response from a TypeChat Provider
    /// </summary>
    public string Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

public interface ITypeChat
{
    Task<TypeChatResponse> TranslateMessageAsync(TypeChatRequest request, CancellationToken token = default);
}

public enum TypeChatTranslator
{
    Json,
    Program,
}
