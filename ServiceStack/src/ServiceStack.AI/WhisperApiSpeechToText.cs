using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.AI;

public class WhisperApiSpeechToText : ISpeechToText, IRequireVirtualFiles
{
    public IVirtualFiles? VirtualFiles { get; set; }
    
    public string BaseUri { get; set; } = "https://api.openai.com/v1";
    
    public string? ApiKey { get; set; }
    
    public Task InitAsync(InitSpeechToText config, CancellationToken token = default) => Task.CompletedTask;

    public async Task<TranscriptResult> TranscribeAsync(string recordingPath, CancellationToken token = default)
    {
        if (VirtualFiles == null)
            throw new ArgumentNullException(nameof(VirtualFiles));
        
        var file = VirtualFiles.AssertFile(recordingPath);
        
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);
        using var body = new MultipartFormDataContent()
            .AddParam("model", "whisper-1")
            .AddParam("language", "en")
            .AddParam("response_format", "json")
            .AddFile("file", file);

        var response = await client.PostAsync(new Uri(BaseUri + "/audio/transcriptions"), body, token).ConfigAwait();
        var resBody = await response.Content.ReadAsStringAsync().ConfigAwait();
        
        string? text = null;
        if (response.IsSuccessStatusCode)
        {
            var obj = (Dictionary<string,object>) JSON.parse(resBody);
            text = obj.TryGetValue("text", out var oText)
                ? oText as string
                : null;
        }
        if (text == null)
            throw new Exception($"Could not transcribe {recordingPath}: {resBody}");

        return new TranscriptResult
        {
            Transcript = text,
            ApiResponse = resBody,
        };
    }
}