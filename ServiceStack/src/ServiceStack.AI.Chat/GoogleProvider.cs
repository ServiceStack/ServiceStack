using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

public class GoogleProvider(ILogger log, IHttpClientFactory factory) : OpenAiProviderBase(log, factory)
{
    public List<object>? SafetySettings { get; set; }
    public Dictionary<string, object>? ThinkingConfig { get; set; }

    public string GeminiChatSummaryJson(Dictionary<string, object> geminiChat)
    {
        var origJson = JSON.stringify(geminiChat);
        var obj = JSON.ParseObject(origJson);
        if (obj.TryGetValue("contents", out List<object> contents))
        {
            foreach (var content in contents.OfType<Dictionary<string,object>>())
            {
                if (content.TryGetValue("parts", out List<object> parts))
                {
                    foreach (var part in parts.OfType<Dictionary<string,object>>())
                    {
                        if (part.TryGetValue("inline_data", out Dictionary<string,object> inlineData)
                            && inlineData.TryGetValue("data", out string data))
                        {
                            inlineData["data"] = $"({data.Length})";
                        }
                    }
                }
            }
        }
        return ClientConfig.IndentJson(JSON.stringify(obj));
    }
    
    public override async Task<ChatResponse> ChatAsync(ChatCompletion request, CancellationToken token=default)
    {
        request.Model = Models.GetValueOrDefault(request.Model) ?? request.Model;
        
        using var client = Factory.CreateClient();
        await ProcessChatAsync(request, client);

        var generationConfig = new Dictionary<string, object>();
        var contents = new List<object>();
        string? systemPrompt = null;
        foreach (var message in request.Messages)
        {
            if (message.Role == "system")
            {
                systemPrompt = message.Content?.FirstOrDefault() is AiTextContent textContent
                    ? textContent.Text 
                    : null;
            }
            var parts = new List<object>();
            foreach (var item in message.Content.Safe())
            {
                if (item is AiImageContent imagePart)
                {
                    var url = imagePart.ImageUrl?.Url;
                    if (url == null)
                        continue;
                    if (!url.StartsWith("data:"))
                        throw new Exception($"Image was not downloaded: {url}");

                    var mimeType = url.RightPart(':').LeftPart(';') ?? "image/png";
                    var base64Data = url.RightPart(',');
                    parts.Add(new Dictionary<string,object> {
                        ["inline_data"] = new Dictionary<string, object> {
                            ["mime_type"] = mimeType,
                            ["data"] = base64Data,
                        }
                    });
                }
                else if (item is AiAudioContent audioPart)
                {
                    var inputAudio = audioPart.InputAudio;
                    if (inputAudio?.Data == null)
                        continue;
                    var data = inputAudio.Data;
                    var format = inputAudio.Format;
                    var mimeType = $"audio/{format}";
                    parts.Add(new Dictionary<string,object> {
                        ["inline_data"] = new Dictionary<string, object> {
                            ["mime_type"] = mimeType,
                            ["data"] = data,
                        }
                    });
                }
                else if (item is AiFileContent filePart)
                {
                    var file = filePart.File;
                    if (file?.FileData == null)
                        continue;
                    var data = file.FileData;
                    if (!data.StartsWith("data:"))
                        throw new Exception($"File was not downloaded: {data}");

                    var mimeType = data.RightPart(':').LeftPart(';') ?? "application/octet-stream";
                    var base64Data = data.RightPart(',');
                    parts.Add(new Dictionary<string,object> {
                        ["inline_data"] = new Dictionary<string, object> {
                            ["mime_type"] = mimeType,
                            ["data"] = base64Data,
                        }
                    });
                }
                else if (item is AiTextContent textPart)
                {
                    parts.Add(new Dictionary<string,object> {
                        ["text"] = textPart.Text,
                    });
                }
            }
            if (parts.Count > 0)
            {
                contents.Add(new Dictionary<string, object> {
                    ["role"] = message.Role is "user" ? "user" : "model",
                    ["parts"] = parts,
                });
            }
        }

        var geminiChat = new Dictionary<string, object>
        {
            ["contents"] = contents,
        };

        if (SafetySettings != null)
        {
            geminiChat["safety_settings"] = SafetySettings;
        }
        
        if (systemPrompt != null)
        {
            geminiChat["systemInstruction"] = new Dictionary<string, object>
            {
                ["parts"] = new List<object> {
                    new Dictionary<string, object> {
                        ["text"] = systemPrompt,
                    }
                }
            };
        }

        if (request.Stop != null)
        {
            generationConfig["stopSequences"] = request.Stop;
        }
        if (request.Temperature != null)
        {
            generationConfig["temperature"] = request.Temperature;
        }
        if (request.TopP != null)
        {
            generationConfig["topP"] = request.TopP;
        }
        if (request.TopLogprobs != null)
        {
            generationConfig["topK"] = request.TopLogprobs;
        }
        if (ThinkingConfig != null)
        {
            generationConfig["thinkingConfig"] = ThinkingConfig;
        }
        if (generationConfig.Count > 0)
        {
            geminiChat["generationConfig"] = generationConfig;
        }
        
        var startedAt = DateTime.UtcNow;
        var geminiChatUrl = ChatUrl!
            .Replace("$Model", request.Model)
            .Replace("$ApiKey", ApiKey!);

        if (Log.IsEnabled(LogLevel.Debug))
            Log.LogDebug("POST {ChatUrl}\n{Request}", geminiChatUrl, GeminiChatSummaryJson(geminiChat));

        var httpReq = new HttpRequestMessage(HttpMethod.Post, geminiChatUrl);
        var jsonRequest = JSON.stringify(geminiChat);
        httpReq.Content = new StringContent(jsonRequest);
        foreach (var entry in Headers)
        {
            if (HttpHeaders.Authorization.Equals(entry.Key, StringComparison.OrdinalIgnoreCase))
                continue;
            httpReq.WithHeader(entry.Key, entry.Value);
        }
        var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();
        var json = await httpRes.Content.ReadAsStringAsync(token).ConfigAwait();
        var obj = (Dictionary<string,object>) JSON.parse(json);
        
        if (obj.TryGetValue("error", out var oError))
        {
            var errorMsg = (oError as Dictionary<string, object>)?.GetValueOrDefault("message"); 
            log.LogError("Gemini Error: {Message}", errorMsg);
            throw new Exception($"Gemini Error: {errorMsg}");
        }

        httpRes.EnsureSuccessStatusCode();

        var unixTimeMs = startedAt.ToUnixTimeMs();
        
        var response = new ChatResponse
        {
            Id = $"chatcmpl-{unixTimeMs}",
            Object = "chat.completion",
            Created = unixTimeMs,
            Model = obj.GetValueOrDefault("modelVersion") as string ?? request.Model,
        };

        var choices = new List<Choice>();
        var i = 0;
        
        if (obj.TryGetValue("candidates", out List<object> candidates))
        {
            foreach (var candidate in candidates.OfType<Dictionary<string, object>>())
            {
                var role = "assistant";
                var content = "";
                var reasoning = "";

                if (candidate.TryGetValue("content", out Dictionary<string, object> contentDict))
                {
                    if (contentDict.TryGetValue("role", out string geminiRole))
                    {
                        role = geminiRole == "model" ? "assistant" : geminiRole;
                    }
                    if (contentDict.TryGetValue("parts", out List<object> parts))
                    {
                        var textParts = new List<string>();
                        var reasoningParts = new List<string>();
                        foreach (var part in parts.OfType<Dictionary<string, object>>())
                        {
                            if (part.TryGetValue("text", out string text))
                            {
                                if (part.TryGetValue("thought", out bool thought) && thought)
                                {
                                    reasoningParts.Add(text);
                                }
                                else
                                {
                                    textParts.Add(text);
                                }
                            }
                        }
                        content = string.Join(" ", textParts);
                        reasoning = string.Join(" ", reasoningParts);
                    }
                }

                var choice = new Choice
                {
                    Index = i,
                    FinishReason = candidate.GetValueOrDefault("finishReason") as string ?? "stop",
                    Message = new ChoiceMessage
                    {
                        Role = role,
                        Content = content,
                    }
                };
                if (!string.IsNullOrEmpty(reasoning))
                {
                    choice.Message.Reasoning = reasoning;
                }
                choices.Add(choice);
                i++;
            }
            response.Choices = choices;
            
            if (obj.TryGetValue("usageMetadata", out Dictionary<string, object> usageMetadata))
            {
                response.Usage = new AiUsage
                {
                    CompletionTokens = (int) usageMetadata.GetValueOrDefault("candidatesTokenCount", 0),
                    TotalTokens = (int) usageMetadata.GetValueOrDefault("totalTokenCount", 0),
                    PromptTokens = (int) usageMetadata.GetValueOrDefault("promptTokenCount", 0),
                };
            }
        }
        
        return response;
    }

    public static OpenAiProviderBase? Create(ILogger log, IHttpClientFactory factory, Dictionary<string, object?> definition)
    {
        var to = new GoogleProvider(log, factory);
        to.Populate(definition);
        
        if (definition.TryGetValue("safety_settings", out List<object> safetySettings))
        {
            to.SafetySettings = safetySettings;
        }
        if (definition.TryGetValue("thinking_config", out Dictionary<string, object> thinkingConfig))
        {
            to.ThinkingConfig = thinkingConfig;
        }

        if (string.IsNullOrEmpty(to.ApiKey))
            return null;
        
        to.BaseUrl = "https://generativelanguage.googleapis.com";
        to.ChatUrl = to.BaseUrl + "/v1beta/models/$Model:generateContent?key=$ApiKey";
        
        if (to.ApiKey == null)
            return null;
        if (to.Models.Count == 0)
            return null;

        return to;
    }
}