using ServiceStack.IO;

namespace ServiceStack.AI;

public class WhisperApiSpeechToText : ISpeechToText, IRequireVirtualFiles
{
    public IVirtualFiles? VirtualFiles { get; set; }
    public Task InitAsync(InitSpeechToText config, CancellationToken token = default) => Task.CompletedTask;

    public async Task<TranscriptResult> TranscribeAsync(string recordingPath, CancellationToken token = default)
    {
        if (VirtualFiles == null)
            throw new ArgumentNullException(nameof(VirtualFiles));
        
        var file = VirtualFiles.GetFile(recordingPath);
        
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);
        using var body = new MultipartFormDataContent()
            .AddParam("model", "whisper-1")
            .AddParam("language", "en")
            .AddParam("response_format", "json")
            .AddFile("file", file);
        // body.Headers.Add("OpenAI-Organization", "");

        var response = await client.PostAsync(new Uri("https://api.openai.com/v1/audio/transcriptions"), body, token);
        var resBody = await response.Content.ReadAsStringAsync();
        
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