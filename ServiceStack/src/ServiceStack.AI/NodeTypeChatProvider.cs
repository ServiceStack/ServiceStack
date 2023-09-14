using System.Diagnostics;
using ServiceStack.Text;

namespace ServiceStack.AI;

public class NodeTypeChat : ITypeChat
{
    public async Task<TypeChatResponse> TranslateMessageAsync(TypeChatRequest request, CancellationToken token = default)
    {
        var schemaPath = request.SchemaPath
            ?? Path.GetTempFileName();
        
        await File.WriteAllTextAsync(schemaPath, request.Schema, token);
        var scriptPath = request.ScriptPath ?? "typechat.mjs";

        var shellRequest = request.UserMessage.Replace('"', '\'');
        var processInfo = new ProcessStartInfo
        {
            WorkingDirectory = request.WorkingDirectory ?? Environment.CurrentDirectory,
            FileName = request.NodePath,
            Arguments = $"{scriptPath} {request.TypeChatTranslator} ./{schemaPath} \"{shellRequest}\"",
        };
        if (Env.IsWindows)
            processInfo = processInfo.ConvertToCmdExec();

        var sb = StringBuilderCache.Allocate();
        var sbError = StringBuilderCacheAlt.Allocate();
        await ProcessUtils.RunAsync(processInfo, request.NodeProcessTimeoutMs,
            onOut: data => sb.AppendLine(data),
            onError: data => sbError.AppendLine(data));

        if (sbError.Length > 0)
            throw new Exception($"Error running node {StringBuilderCacheAlt.ReturnAndFree(sbError)}");

        if (request.SchemaPath == null)
            File.Delete(schemaPath);
        
        var result = StringBuilderCache.ReturnAndFree(sb);

        if (JSON.parse(result) is Dictionary<string, object> obj && obj.TryGetValue("responseStatus", out var oResponseStatus) 
            && oResponseStatus is Dictionary<string,object> responseStatus)
        {
            return new TypeChatResponse
            {
                ResponseStatus = new()
                {
                    ErrorCode = (responseStatus.TryGetValue("errorCode", out var oErrorCode) ? oErrorCode as string : null) ?? string.Empty,
                    Message = (responseStatus.TryGetValue("message", out var oMessage) ? oMessage as string : null) ?? string.Empty,
                }  
            };
        }
        
        return new TypeChatResponse { Result = result };
    }
}
