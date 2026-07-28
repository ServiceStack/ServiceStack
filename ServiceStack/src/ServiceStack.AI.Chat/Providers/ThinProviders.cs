using System.Text;
using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Text-chat ports of the first-party providers in llms/extensions/providers/*.py.
/// Their image/audio/transcription generators are added with the modality providers.
/// </summary>
static class ProviderMessageUtils
{
    /// <summary>Remove UI-only/unsupported fields from every message</summary>
    public static JsonObject StripMessageFields(this JsonObject chat, params string[] fields)
    {
        if (chat.GetArray("messages") is { } messages)
        {
            foreach (var message in messages)
            {
                if (message is not JsonObject msg)
                    continue;
                foreach (var field in fields)
                    msg.Remove(field);
            }
        }
        return chat;
    }
}

/// <summary>OpenAI (port of OpenAiProvider): chat completions don't accept "modalities"</summary>
public class OpenAiProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "@ai-sdk/openai";

    public override void Populate(JsonObject kwargs)
    {
        kwargs["api"] ??= "https://api.openai.com/v1";
        base.Populate(kwargs);
        Modalities["image"] = AttachGenerator(new OpenAiImageGenerator(), kwargs);
    }

    public override async Task<JsonObject> ProcessChatAsync(JsonObject chat, string? providerId = null)
    {
        var ret = await base.ProcessChatAsync(chat, providerId).ConfigAwait();
        ret.Remove("modalities");
        return ret;
    }
}

/// <summary>xAI (port of XaiProvider)</summary>
public class XaiProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "@ai-sdk/xai";

    public override void Populate(JsonObject kwargs)
    {
        kwargs["api"] ??= "https://api.x.ai/v1";
        base.Populate(kwargs);
    }
}

/// <summary>Codestral (port of CodestralProvider)</summary>
public class CodestralProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "codestral";
}

/// <summary>Cerebras: only accepts string content for text-only messages</summary>
public class CerebrasProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "@ai-sdk/cerebras";

    public override void Populate(JsonObject kwargs)
    {
        kwargs["api"] ??= "https://api.cerebras.ai/v1";
        base.Populate(kwargs);
    }

    public override async Task<JsonObject> ProcessChatAsync(JsonObject chat, string? providerId = null)
    {
        var ret = await base.ProcessChatAsync(chat, providerId).ConfigAwait();
        ret.Remove("modalities");

        if (ret.GetArray("messages") is { } messages)
        {
            foreach (var message in messages)
            {
                if (message is not JsonObject msg || msg["content"] is not JsonArray parts)
                    continue;
                var text = new StringBuilder();
                var isTextOnly = true;
                foreach (var part in parts)
                {
                    if ((part as JsonObject).GetString("type") != "text")
                    {
                        isTextOnly = false;
                        break;
                    }
                    text.Append((part as JsonObject).GetString("text") ?? "");
                }
                if (isTextOnly)
                    msg["content"] = text.ToString();
            }
        }
        return ret;
    }
}

/// <summary>Mistral: doesn't accept extra message fields</summary>
public class MistralProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "@ai-sdk/mistral";

    /// <summary>Used for audio-only models and by the voice extension</summary>
    public MistralTranscriptionGenerator Transcription { get; private set; } = new();

    public override void Populate(JsonObject kwargs)
    {
        kwargs["api"] ??= "https://api.mistral.ai/v1";
        base.Populate(kwargs);
        Transcription = (MistralTranscriptionGenerator)AttachGenerator(new MistralTranscriptionGenerator(), kwargs);
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        // models whose only input modality is audio are transcription-only
        var model = ProviderModel(chat.GetString("model") ?? "") ?? chat.GetString("model") ?? "";
        var inputModalities = ModelInfo(model).GetObject("modalities")?.GetArray("input");
        if (inputModalities is { Count: 1 } && inputModalities[0]?.GetValue<string>() == "audio")
        {
            return await Transcription.ChatAsync(chat, context).ConfigAwait();
        }
        return await base.ChatAsync(chat, context).ConfigAwait();
    }

    public override async Task<JsonObject> ProcessChatAsync(JsonObject chat, string? providerId = null)
    {
        var ret = await base.ProcessChatAsync(chat, providerId).ConfigAwait();
        return ret.StripMessageFields("timestamp");
    }
}

/// <summary>OpenRouter: strips modalities/enable_thinking and non-standard message fields</summary>
public class OpenRouterProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "@openrouter/ai-sdk-provider";

    public override void Populate(JsonObject kwargs)
    {
        base.Populate(kwargs);
        Modalities["image"] = AttachGenerator(new OpenRouterImageGenerator(), kwargs);
        Modalities["audio"] = AttachGenerator(new OpenRouterAudioGenerator(), kwargs);
        Modalities["speech"] = AttachGenerator(new OpenRouterTextToSpeech(), kwargs);
    }

    public override async Task<JsonObject> ProcessChatAsync(JsonObject chat, string? providerId = null)
    {
        var ret = await base.ProcessChatAsync(chat, providerId).ConfigAwait();
        ret.Remove("modalities");
        ret.Remove("enable_thinking");
        return ret.StripMessageFields("timestamp", "reasoning", "refusal");
    }
}

/// <summary>Fireworks AI: same message constraints as OpenRouter</summary>
public class FireworksProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "@fireworks/ai-sdk-provider";

    public override void Populate(JsonObject kwargs)
    {
        base.Populate(kwargs);
        Modalities["image"] = AttachGenerator(new FireworksImageGenerator(), kwargs);
    }

    public override async Task<JsonObject> ProcessChatAsync(JsonObject chat, string? providerId = null)
    {
        var ret = await base.ProcessChatAsync(chat, providerId).ConfigAwait();
        ret.Remove("modalities");
        ret.Remove("enable_thinking");
        return ret.StripMessageFields("timestamp", "reasoning", "refusal");
    }
}

/// <summary>llms.py server (llmspy.org)</summary>
public class LlmsPyProvider : OpenAiCompatibleProvider
{
    public override string Sdk => "llms-sdk-provider";

    public override async Task<JsonObject> ProcessChatAsync(JsonObject chat, string? providerId = null)
    {
        var ret = await base.ProcessChatAsync(chat, providerId).ConfigAwait();
        ret.Remove("modalities");
        return ret;
    }
}
