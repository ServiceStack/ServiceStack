using ServiceStack.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace ServiceStack.AI;

/// <summary>
/// Standard OpenAI-compatible provider: HTTP chat + SSE streaming + message normalization
/// (see ChatProviderHttp.cs / ChatProviderMessages.cs). Model metadata + config live in ChatProvider.
/// </summary>
public partial class OpenAiCompatibleProvider : ChatProvider
{
}

/// <summary>Groq (port of GroqProvider): OpenAI-compatible, strips modalities + message timestamps</summary>
public class GroqProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "@ai-sdk/groq";

    public override void Populate(JsonObject kwargs)
    {
        kwargs["api"] ??= "https://api.groq.com/openai/v1";
        base.Populate(kwargs);
    }

    public override async Task<JsonObject> ProcessChatAsync(JsonObject chat, string? providerId = null)
    {
        var ret = await base.ProcessChatAsync(chat, providerId).ConfigAwait();
        ret.Remove("modalities"); // groq doesn't support modalities
        if (ret.GetArray("messages") is { } messages)
        {
            foreach (var message in messages)
            {
                (message as JsonObject)?.Remove("timestamp"); // groq doesn't support timestamp
            }
        }
        return ret;
    }
}

/// <summary>Ollama: local model discovery via /api/tags, chats via its OpenAI-compatible /v1 endpoint</summary>
public class OllamaProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "ollama";
    public override string ChatUrl => $"{Api}/v1/chat/completions";

    public override string? Validate() => null;

    public override async Task LoadAsync(CancellationToken token = default)
    {
        if (Models.Count == 0)
        {
            await LoadModelsAsync(token).ConfigAwait();
        }
    }

    protected virtual async Task<Dictionary<string, string>> GetModelsAsync(CancellationToken token)
    {
        var ret = new Dictionary<string, string>();
        try
        {
            using var client = HttpClientFactory!.CreateClient();
            var url = $"{Api}/api/tags";
            Log.LogInformation("GET {Url}", url);
            var res = await client.GetStringAsync(url, token).ConfigAwait();
            var data = ChatJson.ParseObject(res);
            foreach (var model in data.GetArray("models") ?? [])
            {
                var modelId = (model as JsonObject).GetString("model");
                if (modelId == null)
                    continue;
                if (modelId.EndsWith(":latest"))
                    modelId = modelId[..^7];
                ret[modelId] = modelId;
            }
        }
        catch (Exception e)
        {
            Log.LogInformation("Error getting {Name} models: {Message}", Name, e.Message);
        }
        return ret;
    }

    public async Task LoadModelsAsync(CancellationToken token = default)
    {
        var modelMap = await GetModelsAsync(token).ConfigAwait();
        if (MapModels.Count > 0)
        {
            var mapModelValues = new HashSet<string>(MapModels.Values);
            var to = new Dictionary<string, string>();
            foreach (var entry in modelMap)
            {
                if (MapModels.ContainsKey(entry.Key))
                    to[entry.Key] = entry.Value;
                if (mapModelValues.Contains(entry.Value))
                    to[entry.Key] = entry.Value;
            }
            modelMap = to;
        }
        else
        {
            MapModels = modelMap;
        }

        Models = [];
        foreach (var entry in modelMap)
        {
            Models[entry.Key] = new JsonObject
            {
                ["id"] = entry.Key,
                ["name"] = entry.Value.Replace(":", " "),
                ["modalities"] = new JsonObject
                {
                    ["input"] = new JsonArray("text"),
                    ["output"] = new JsonArray("text"),
                },
                ["tool_call"] = true,
                ["cost"] = new JsonObject { ["input"] = 0, ["output"] = 0 },
            };
        }
    }
}

/// <summary>LM Studio: model discovery via OpenAI /models endpoint</summary>
public class LmStudioProvider : OllamaProvider
{
    public override string Sdk => "lmstudio";
    public override string ChatUrl => $"{Api}/chat/completions";

    protected override async Task<Dictionary<string, string>> GetModelsAsync(CancellationToken token)
    {
        var ret = new Dictionary<string, string>();
        try
        {
            using var client = HttpClientFactory!.CreateClient();
            var url = $"{Api}/models";
            Log.LogInformation("GET {Url}", url);
            var res = await client.GetStringAsync(url, token).ConfigAwait();
            var data = ChatJson.ParseObject(res);
            foreach (var model in data.GetArray("data") ?? [])
            {
                var id = (model as JsonObject).GetString("id");
                if (id != null)
                    ret[id] = id;
            }
        }
        catch (Exception e)
        {
            Log.LogInformation("Error getting {Name} models: {Message}", Name, e.Message);
        }
        return ret;
    }
}

public class OpenAiLocalProvider : LmStudioProvider
{
    public override string Sdk => "openai-local";
}
