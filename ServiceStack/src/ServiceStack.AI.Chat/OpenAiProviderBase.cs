using Microsoft.Extensions.Logging;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.AI;

public abstract class OpenAiProviderBase(ILogger log, IHttpClientFactory factory) : IChatClient
{
    public string BaseUrl { get; set; }
    public string? ChatUrl { get; set; }
    public string? ApiKey { get; set; }
    public Dictionary<string, string> Models { get; set; } = [];
    public Dictionary<string, string> Headers { get; set; } = [];

    public double? FrequencyPenalty { get; set; }
    public bool? LogProbs { get; set; }
    public int? MaxCompletionTokens { get; set; }
    public int? N { get; set; }
    public bool? ParallelToolCalls { get; set; }
    public double? PresencePenalty { get; set; }
    public string? PromptCacheKey { get; set; }
    public string? ReasoningEffort { get; set; }    
    public string? SafetyIdentifier { get; set; }
    public int? Seed { get; set; }
    public string? ServiceTier { get; set; }
    public List<string>? Stop { get; set; }
    public bool? Store { get; set; }
    public double? Temperature { get; set; }
    public int? TopLogprobs { get; set; }
    public double? TopP { get; set; }
    public string? Verbosity { get; set; }
    public bool? EnableThinking { get; set; }
    
    public ILogger Log { get; set; } = log;
    public IHttpClientFactory Factory { get; set; } = factory;

    public IVirtualFiles? VirtualFiles { get; set; }
    
    public async Task<Dictionary<string,object>> GetJsonObjectAsync(string url, CancellationToken token=default)
    {
        using var client = Factory.CreateClient();
        var httpReq = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var entry in Headers)
        {
            if (HttpHeaders.ContentType.Equals(entry.Key, StringComparison.OrdinalIgnoreCase))
                continue;
            httpReq.WithHeader(entry.Key, entry.Value);
        }
        Log.LogDebug("GET {Url}", url);
        var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();
        httpRes.EnsureSuccessStatusCode();
        var json = await httpRes.Content.ReadAsStringAsync(token).ConfigAwait();
        return (Dictionary<string, object>)JSON.parse(json);
    }

    public void Populate(Dictionary<string, object?> definition)
    {
        if (definition.TryGetValue("chat_url", out var oChatUrl)
            && oChatUrl is string chatUrl)
        {
            ChatUrl = chatUrl;
        }
        if (definition.TryGetValue("base_url", out var oBaseUrl)
            && oBaseUrl is string baseUrl)
        {
            BaseUrl = baseUrl;
            if (ChatUrl == null)
            {
                var lastSegment = baseUrl.LastRightPart('/');
                if (lastSegment.StartsWith('v') && int.TryParse(lastSegment[1..], out _))
                {
                    ChatUrl = baseUrl.CombineWith("chat/completions");
                }
                else
                {
                    ChatUrl = baseUrl.CombineWith("v1/chat/completions");
                }
            }
        }
        if (definition.TryGetValue("models", out var oModels)
            && oModels is Dictionary<string, object> models)
        {
            Models = models.ToObjectDictionary().ToStringDictionary();
        }
        
        if (definition.TryGetValue("headers", out var oHeaders) 
            && oHeaders is Dictionary<string, object> headers)
        {
            Headers = headers.ToObjectDictionary().ToStringDictionary();
        }
        Headers ??= new()
        {
            [HttpHeaders.ContentType] = "application/json"
        };

        if (definition.TryGetValue("api_key", out var oApiKey)
            && oApiKey is string apiKey && !string.IsNullOrEmpty(apiKey))
        {
            ApiKey = apiKey;
            Headers[HttpHeaders.Authorization] = $"Bearer {ApiKey}";
        }

        if (definition.TryGetValue("frequency_penalty", out var oFrequencyPenalty)
            && oFrequencyPenalty is double frequencyPenalty)
        {
            FrequencyPenalty = frequencyPenalty;
        }
        if (definition.TryGetValue("logprobs", out var oLogProbs)
            && oLogProbs is bool logProbs)
        {
            LogProbs = logProbs;
        }
        if (definition.TryGetValue("max_completion_tokens", out var oMaxCompletionTokens)
            && oMaxCompletionTokens is int maxCompletionTokens)
        {
            MaxCompletionTokens = maxCompletionTokens;
        }
        if (definition.TryGetValue("n", out var oN)
            && oN is int n)
        {
            N = n;
        }
        if (definition.TryGetValue("parallel_tool_calls", out var oParallelToolCalls)
            && oParallelToolCalls is bool parallelToolCalls)
        {
            ParallelToolCalls = parallelToolCalls;
        }
        if (definition.TryGetValue("presence_penalty", out var oPresencePenalty)
            && oPresencePenalty is double presencePenalty)
        {
            PresencePenalty = presencePenalty;
        }
        if (definition.TryGetValue("prompt_cache_key", out var oPromptCacheKey)
            && oPromptCacheKey is string promptCacheKey)
        {
            PromptCacheKey = promptCacheKey;
        }
        if (definition.TryGetValue("reasoning_effort", out var oReasoningEffort)
            && oReasoningEffort is string reasoningEffort)
        {
            ReasoningEffort = reasoningEffort;
        }
        if (definition.TryGetValue("safety_identifier", out var oSafetyIdentifier)
            && oSafetyIdentifier is string safetyIdentifier)
        {
            SafetyIdentifier = safetyIdentifier;
        }
        if (definition.TryGetValue("seed", out var oSeed)
            && oSeed is int seed)
        {
            Seed = seed;
        }
        if (definition.TryGetValue("service_tier", out var oServiceTier)
            && oServiceTier is string serviceTier)
        {
            ServiceTier = serviceTier;
        }
        if (definition.TryGetValue("stop", out var oStop))
        {
            Stop = oStop switch
            {
                List<object> stopList => stopList.Map(x => x.ToString()!),
                List<string> stops => stops,
                string stop => [stop],
                _ => null
            };
        }
        if (definition.TryGetValue("store", out var oStore)
            && oStore is bool store)
        {
            Store = store;
        }
        if (definition.TryGetValue("temperature", out var oTemperature)
            && oTemperature is double temperature)
        {
            Temperature = temperature;
        }
        if (definition.TryGetValue("top_logprobs", out var oTopLogprobs)
            && oTopLogprobs is int topLogprobs)
        {
            TopLogprobs = topLogprobs;
        }
        if (definition.TryGetValue("top_p", out var oTopP)
            && oTopP is double topP)
        {
            TopP = topP;
        }
        if (definition.TryGetValue("verbosity", out var oVerbosity)
            && oVerbosity is string verbosity)
        {
            Verbosity = verbosity;
        }
        if (definition.TryGetValue("enable_thinking", out var oEnableThinking)
            && oEnableThinking is bool enableThinking)
        {
            EnableThinking = enableThinking;
        }
    }
    
    public virtual Task LoadAsync(CancellationToken token=default) => Task.CompletedTask;

    public virtual bool IsUrl(string url) => url.StartsWith("http://") || url.StartsWith("https://");

    public virtual bool IsFilePath(string path) => path is { Length: < 1024 }
        && VirtualFiles?.GetFile(path) != null;

    public virtual bool IsBase64(string data)
    {
        try
        {
            _ = Convert.FromBase64String(data);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public virtual string GetMimeType(string urlOrPath)
    {
        if (urlOrPath.IndexOf('\\') >= 0)
            urlOrPath = urlOrPath.Replace('\\', '/');
        var filename = urlOrPath.Contains('.') 
            ? urlOrPath.LastRightPart('/')
            : null;
        if (filename == null)
            return MimeTypes.Binary;
        return MimeTypes.GetMimeType(filename);
    }
    
    public string ChatSummaryJson(ChatCompletion request)
    {
        var origJson = request.ToJson();
        var clone = origJson.FromJson<ChatCompletion>();
        foreach (var message in clone.Messages)
        {
            if (message.Content == null)
                continue;
            foreach (var part in message.Content)
            {
                if (part is AiImageContent imagePart && imagePart.ImageUrl?.Url.StartsWith("data:") == true)
                {
                    var url = imagePart.ImageUrl.Url;
                    var prefix = url.LeftPart(',');
                    imagePart.ImageUrl.Url = prefix + $",({url.Length - prefix.Length})";
                }
                else if (part is AiAudioContent audioPart && audioPart.InputAudio?.Data != null)
                {
                    var data = audioPart.InputAudio.Data;
                    audioPart.InputAudio.Data = $"({data.Length})";
                }
                else if (part is AiFileContent filePart && filePart.File?.FileData?.StartsWith("data:") == true)
                {
                    var data = filePart.File.FileData;
                    var prefix = data.LeftPart(',');
                    filePart.File.FileData = prefix + $",({data.Length - prefix.Length})";
                }
            }
        }
        var json = ClientConfig.ToSystemJson(clone);
        return json;
    }
    
    public virtual async Task ProcessChatAsync(ChatCompletion request, HttpClient httpClient)
    {
        request.Stream ??= false;
        if (request.Messages.Count == 0)
            throw new ArgumentNullException(nameof(request.Messages));
        
        if (FrequencyPenalty != null)
            request.FrequencyPenalty ??= FrequencyPenalty.Value;
        if (LogProbs != null)
            request.Logprobs ??= LogProbs.Value;
        if (MaxCompletionTokens != null)
            request.MaxCompletionTokens ??= MaxCompletionTokens.Value;
        if (N != null)
            request.N ??= N.Value;
        if (ParallelToolCalls != null)
            request.ParallelToolCalls ??= ParallelToolCalls.Value;
        if (PresencePenalty != null)
            request.PresencePenalty ??= PresencePenalty.Value;
        if (PromptCacheKey != null)
            request.PromptCacheKey ??= PromptCacheKey;
        if (ReasoningEffort != null)
            request.ReasoningEffort ??= ReasoningEffort;
        if (SafetyIdentifier != null)
            request.SafetyIdentifier ??= SafetyIdentifier;
        if (Seed != null)
            request.Seed ??= Seed.Value;
        if (ServiceTier != null)
            request.ServiceTier ??= ServiceTier;
        if (Stop != null)
            request.Stop ??= Stop;
        if (Store != null)
            request.Store ??= Store.Value;
        if (Temperature != null)
            request.Temperature ??= Temperature.Value;
        if (TopLogprobs != null)
            request.TopLogprobs ??= TopLogprobs.Value;
        if (TopP != null)
            request.TopP ??= TopP.Value;
        if (Verbosity != null)
            request.Verbosity ??= Verbosity;
        if (EnableThinking != null)
            request.EnableThinking ??= EnableThinking.Value;
        
        foreach (var message in request.Messages)
        {
            if (message.Content == null)
                continue;
            foreach (var part in message.Content)
            {
                if (part is AiImageContent imagePart)
                {
                    var url = imagePart.ImageUrl?.Url;
                    if (url == null)
                        continue;
                    if (IsUrl(url))
                    {
                        Log.LogDebug("Downloading image: {Url}", url);
                        var httpReq = new HttpRequestMessage(HttpMethod.Get, url);
                        var httpRes = await httpClient.SendAsync(httpReq).ConfigAwait();
                        httpRes.EnsureSuccessStatusCode();

                        var mimeType = httpRes.Content.Headers.ContentType?.MediaType
                            ?? GetMimeType(url);
                        
                        var bytes = await httpRes.Content.ReadAsByteArrayAsync().ConfigAwait();
                        var base64 = Convert.ToBase64String(bytes);
                        imagePart.ImageUrl!.Url = $"data:{mimeType};base64,{base64}";
                    }
                    else if (IsFilePath(url))
                    {
                        Log.LogDebug("Reading image: {Url}", url);
                        var file = VirtualFiles!.GetFile(url);
                        var mimeType = GetMimeType(file.Name);
                        var bytes = await file.ReadAllBytesAsync().ConfigAwait();
                        var base64 = Convert.ToBase64String(bytes);
                        imagePart.ImageUrl!.Url = $"data:{mimeType};base64,{base64}";
                    }
                    else if (!url.StartsWith("data:"))
                        throw new Exception($"Invalid image: {url}");
                }
                else if (part is AiAudioContent audioPart)
                {
                    var data = audioPart.InputAudio?.Data;
                    if (data == null)
                        continue;
                    if (IsUrl(data))
                    {
                        Log.LogDebug("Downloading audio: {Data}", data);
                        var httpReq = new HttpRequestMessage(HttpMethod.Get, data);
                        var httpRes = await httpClient.SendAsync(httpReq).ConfigAwait();
                        httpRes.EnsureSuccessStatusCode();
                        var mimeType = httpRes.Content.Headers.ContentType?.MediaType
                            ?? GetMimeType(data);
                        var bytes = await httpRes.Content.ReadAsByteArrayAsync().ConfigAwait();
                        var base64 = Convert.ToBase64String(bytes);
                        audioPart.InputAudio!.Data = base64;
                        // Typically only .mp3 or .wav is supported, sometimes URLs return audio/mpeg or .mp3
                        var format = mimeType.LastRightPart('/');
                        if (format is "mpeg")
                            format = "mp3";
                        audioPart.InputAudio.Format = format;
                    }
                    else if (IsFilePath(data))
                    {
                        Log.LogDebug("Reading audio: {Data}", data);
                        var file = VirtualFiles!.GetFile(data);
                        var mimeType = GetMimeType(file.Name);
                        var bytes = await file.ReadAllBytesAsync().ConfigAwait();
                        var base64 = Convert.ToBase64String(bytes);
                        audioPart.InputAudio!.Data = base64;
                        audioPart.InputAudio.Format = mimeType.LastRightPart('/');
                    }
                    else if (!IsBase64(data))
                        throw new Exception($"Invalid audio: {data}");
                }
                else if (part is AiFileContent filePart)
                {
                    var fileData = filePart.File?.FileData;
                    if (fileData == null)
                        continue;
                    if (IsUrl(fileData))
                    {
                        Log.LogDebug("Downloading file: {FileData}", fileData);
                        var httpReq = new HttpRequestMessage(HttpMethod.Get, fileData);
                        var httpRes = await httpClient.SendAsync(httpReq).ConfigAwait();
                        httpRes.EnsureSuccessStatusCode();
                        var bytes = await httpRes.Content.ReadAsByteArrayAsync().ConfigAwait();
                        var mimeType = httpRes.Content.Headers.ContentType?.MediaType
                            ?? GetMimeType(fileData);
                        var base64 = Convert.ToBase64String(bytes);
                        filePart.File!.Filename = fileData.LastRightPart('/');
                        filePart.File!.FileData = $"data:{mimeType};base64," + base64;
                    }
                    else if (IsFilePath(fileData))
                    {
                        Log.LogDebug("Reading file: {FileData}", fileData);
                        var file = VirtualFiles!.GetFile(fileData);
                        var bytes = await file.ReadAllBytesAsync().ConfigAwait();
                        var base64 = Convert.ToBase64String(bytes);
                        var mimeType = GetMimeType(file.Name);
                        filePart.File!.Filename = file.Name;
                        filePart.File!.FileData = $"data:{mimeType};base64," + base64;
                    }
                    else if (fileData.StartsWith("data:"))
                    {
                        filePart.File!.Filename ??= "file";
                    }
                    else throw new Exception($"Invalid file: {fileData}");
                }   
            }
        }
    }

    public virtual async Task<ChatResponse> ChatAsync(ChatCompletion request, CancellationToken token=default)
    {
        request.Model = Models.GetValueOrDefault(request.Model) ?? request.Model;
        
        using var client = Factory.CreateClient();
        await ProcessChatAsync(request, client);
        
        if (Log.IsEnabled(LogLevel.Debug))
            Log.LogDebug("POST {ChatUrl}\n{Request}", ChatUrl, ChatSummaryJson(request));

        var httpReq = new HttpRequestMessage(HttpMethod.Post, ChatUrl);
        var jsonRequest = request.ToJson();
        httpReq.Content = new StringContent(jsonRequest);
        foreach (var entry in Headers)
        {
            httpReq.WithHeader(entry.Key, entry.Value);
        }
        var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();
        var json = await httpRes.Content.ReadAsStringAsync(token).ConfigAwait();
        if (!httpRes.IsSuccessStatusCode)
        {
            Log.LogError("Chat Error: {Message}", json);
            var obj = (Dictionary<string,object>) JSON.parse(json);
            if (obj.TryGetValue("error", out var oError))
            {
                var errorMsg = (oError as Dictionary<string, object>)?.GetValueOrDefault("message"); 
                throw new Exception($"{GetType().Name} Error: {errorMsg}");
            }
        }
        httpRes.EnsureSuccessStatusCode();
        var dto = json.FromJson<ChatResponse>();
        dto.Object ??= "chat.completion";
        if (Log.IsEnabled(LogLevel.Debug))
            Log.LogDebug("Response:\n{Response}", ClientConfig.ToSystemJson(dto));

        return dto;
    }
}