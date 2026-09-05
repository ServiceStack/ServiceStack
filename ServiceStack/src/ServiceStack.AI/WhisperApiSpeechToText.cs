using System.Net.Http;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.AI;

public class WhisperApiSpeechToText : ISpeechToText, IRequireVirtualFiles
{
    public IVirtualFiles? VirtualFiles { get; set; }
    
    public string BaseUri { get; set; } = "https://api.openai.com/v1";
    
    public string? ApiKey { get; set; }

    public HttpClient? HttpClient { get; set; }
    
    public Task InitAsync(InitSpeechToText config, CancellationToken token = default) => Task.CompletedTask;

    public async Task<TranscriptResult> TranscribeAsync(string recordingPath, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(recordingPath))
            throw new ArgumentNullException(nameof(recordingPath));

        if (VirtualFiles == null)
            throw new ArgumentNullException(nameof(VirtualFiles));
        
        var file = VirtualFiles.AssertFile(recordingPath);

        var apiKey = ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("OpenAI API Key was not found. Please set ApiKey or the OPENAI_API_KEY environment variable.");
        
        var client = HttpClient;
        var disposeClient = false;
        if (client == null)
        {
            client = new HttpClient();
            disposeClient = true;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUri.CombineWith("audio/transcriptions"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using var body = new MultipartFormDataContent()
                .AddParam("model", "whisper-1")
                .AddParam("language", "en")
                .AddParam("response_format", "json")
                .AddFile("file", file);

            request.Content = body;

            var response = await client.SendAsync(request, token).ConfigAwait();
#if NET6_0_OR_GREATER
            var resBody = await response.Content.ReadAsStringAsync(token).ConfigAwait();
#else
            var resBody = await response.Content.ReadAsStringAsync().ConfigAwait();
#endif
            
            string? text = null;
            if (response.IsSuccessStatusCode)
            {
                if (JSON.parse(resBody) is Dictionary<string, object> obj &&
                    obj.TryGetValue("text", out var oText))
                {
                    text = oText as string;
                }
            }
            if (text == null)
                throw new Exception($"Could not transcribe {recordingPath}: {resBody}");

            return new TranscriptResult
            {
                Transcript = text,
                ApiResponse = resBody,
            };
        }
        finally
        {
            if (disposeClient)
            {
                client.Dispose();
            }
        }
    }
}