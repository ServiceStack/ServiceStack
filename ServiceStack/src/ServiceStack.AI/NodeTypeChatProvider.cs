using System.Diagnostics;
using ServiceStack.Text;

namespace ServiceStack.AI;

public class NodeTypeChat : ITypeChat
{
    public Func<ProcessStartInfo, ProcessStartInfo>? ProcessFilter { get; set; }
        
    public async Task<TypeChatResponse> TranslateMessageAsync(TypeChatRequest request, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var schemaPath = request.SchemaPath ?? Path.GetTempFileName();
        var isTempSchema = request.SchemaPath == null;

        try
        {
#if NET6_0_OR_GREATER
            await File.WriteAllTextAsync(schemaPath, request.Schema ?? string.Empty, token);
#else
            File.WriteAllText(schemaPath, request.Schema ?? string.Empty);
#endif            
            var scriptPath = request.ScriptPath ?? "typechat.mjs";

            var shellRequest = (request.UserMessage ?? string.Empty).Replace('"', '\'');
            var processInfo = new ProcessStartInfo
            {
                WorkingDirectory = request.WorkingDirectory ?? Environment.CurrentDirectory,
                FileName = request.NodePath,
                Arguments = $"{scriptPath} {request.TypeChatTranslator} ./{schemaPath} \"{shellRequest}\"",
            };
            processInfo = ProcessFilter?.Invoke(processInfo) ?? processInfo;
            
            var sb = StringBuilderCache.Allocate();
            var sbError = StringBuilderCacheAlt.Allocate();
            string stdout;
            string stderr;
            try
            {
                await ProcessUtils.RunAsync(processInfo, request.NodeProcessTimeoutMs,
                    onOut: data => sb.AppendLine(data),
                    onError: data => sbError.AppendLine(data));
            }
            finally
            {
                stdout = StringBuilderCache.ReturnAndFree(sb);
                stderr = StringBuilderCacheAlt.ReturnAndFree(sbError);
            }

            if (stderr.Length > 0)
                throw new Exception($"Error running node {stderr}");

            var result = stdout;

            try
            {
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
            }
            catch
            {
                // Result was not JSON, fall through to returning result
            }
            
            return new TypeChatResponse { Result = result };
        }
        finally
        {
            if (isTempSchema && File.Exists(schemaPath))
            {
                try
                {
                    File.Delete(schemaPath);
                }
                catch
                {
                    // Ignore temp file cleanup failure
                }
            }
        }
    }
}
