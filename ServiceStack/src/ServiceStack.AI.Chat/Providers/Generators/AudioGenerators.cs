using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// OpenRouter image generation (POST /images, returns base64 or urls)
/// </summary>
public class OpenRouterImageGenerator : GeneratorProvider
{
    public override string Sdk => "openrouter/image";

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var parent = context.Provider;
        var model = parent?.ProviderModel(chat.GetString("model") ?? "") ?? chat.GetString("model") ?? "";
        var api = (Api.IsNullOrEmpty() ? parent?.Api : Api) ?? "https://openrouter.ai/api/v1";

        var imageConfig = chat.GetObject("image_config") ?? new JsonObject();
        var body = new JsonObject
        {
            ["model"] = model,
            ["prompt"] = ChatMessages.LastUserPrompt(chat) ?? "",
        };
        if (ChatMessages.ChatToAspectRatio(chat) is { } ratio)
            body["aspect_ratio"] = ratio;
        foreach (var key in new[] { "resolution", "size", "quality", "num_images", "n", "seed", "response_format" })
        {
            if (imageConfig.TryGetPropertyValue(key, out var val) && val != null)
                body[key] = val.DeepClone();
        }

        var response = await PostJsonAsync($"{api.TrimEnd('/')}/images", body, GetHeaders(parent), context).ConfigAwait();

        var cost = response.GetObject("usage").GetDouble("cost") ?? response.GetDouble("cost");
        var images = new JsonArray();
        var i = 0;
        foreach (var itemNode in response.GetArray("data") ?? [])
        {
            if (itemNode is not JsonObject item)
                continue;
            var mediaType = item.GetString("media_type") ?? "image/png";
            byte[]? bytes = null;

            if (item.GetString("b64_json") is { } b64)
            {
                if (b64.StartsWith("data:"))
                {
                    // data:image/png;base64,....
                    mediaType = b64.LeftPart(',').LeftPart(';').RightPart(':');
                    bytes = Convert.FromBase64String(b64.RightPart(','));
                }
                else
                {
                    bytes = Convert.FromBase64String(b64);
                }
            }
            else if (item.GetString("url") is { } imageUrl)
            {
                (bytes, mediaType, _) = await DownloadAsync(imageUrl, "image.png", context.CancellationToken).ConfigAwait();
            }
            if (bytes == null)
                continue;

            var ext = MimeTypes.GetExtension(mediaType).TrimStart('.');
            if (string.IsNullOrEmpty(ext)) ext = "png";
            var cacheUrl = CacheMedia(bytes, $"{model.LastRightPart('/')}-{i++}.{ext}", mediaType, context,
                GeneratorUtils.ToFileInfo(chat));
            images.Add(ImagePart(cacheUrl));
        }
        return BuildMediaResponse("images", images, cost: cost);
    }
}

/// <summary>Shared audio-response construction for the speech/audio generators</summary>
public abstract class AudioGeneratorBase : GeneratorProvider
{
    public override string DefaultContent => "I've generated the audio for you.";

    /// <summary>Detect the container from magic bytes, so cached audio gets the right extension</summary>
    protected static string DetectAudioExtension(byte[] data, string fallback)
    {
        bool Starts(params byte[] prefix) =>
            data.Length >= prefix.Length && data.Take(prefix.Length).SequenceEqual(prefix);

        if (Starts(0x52, 0x49, 0x46, 0x46)) return "wav";                     // RIFF
        if (Starts(0x49, 0x44, 0x33) || Starts(0xFF, 0xFB)
            || Starts(0xFF, 0xF3) || Starts(0xFF, 0xF2)) return "mp3";
        if (data.Length > 8 && data[4] == 0x66 && data[5] == 0x74
            && data[6] == 0x79 && data[7] == 0x70) return "m4a";              // ftyp
        if (Starts(0x1A, 0x45, 0xDF, 0xA3)) return "webm";
        if (Starts(0x4F, 0x67, 0x67, 0x53)) return "ogg";                     // OggS
        if (Starts(0xFF, 0xF1) || Starts(0xFF, 0xF9)) return "aac";
        return fallback;
    }

    /// <summary>Wrap raw 16-bit PCM as a WAV so browsers can play it (24kHz mono, as Python does)</summary>
    protected static byte[] PcmToWav(byte[] pcm, int sampleRate = 24000, short channels = 1, short bitsPerSample = 16)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        w.Write("RIFF"u8.ToArray());
        w.Write(36 + pcm.Length);
        w.Write("WAVE"u8.ToArray());
        w.Write("fmt "u8.ToArray());
        w.Write(16);                                   // PCM chunk size
        w.Write((short)1);                             // PCM format
        w.Write(channels);
        w.Write(sampleRate);
        w.Write(byteRate);
        w.Write((short)(channels * bitsPerSample / 8)); // block align
        w.Write(bitsPerSample);
        w.Write("data"u8.ToArray());
        w.Write(pcm.Length);
        w.Write(pcm);
        w.Flush();
        return ms.ToArray();
    }

    /// <summary>Cache the audio and build the response, pricing it off the prompt length like Python</summary>
    protected JsonObject BuildAudioResponse(byte[] audioData, JsonObject chat, string ext,
        ChatContext context, DateTimeOffset startedAt, string? content = null)
    {
        var model = chat.GetString("model") ?? "";
        var prompt = ChatMessages.LastUserPrompt(chat) ?? "";

        var cost = 0d;
        string? pricingInfo = null;
        if (context.Provider?.ModelInfo(model)?.GetObject("cost") is { } pricing)
        {
            var inputPrice = pricing.GetDouble("input") ?? 0;
            cost = prompt.Length / 1_000_000.0 * inputPrice;
            if (pricing.GetDouble("output") is { } outputPrice)
                pricingInfo = $"{inputPrice}/{outputPrice}";
        }

        var mimeType = MimeTypes.GetMimeType($"f.{ext}");
        var cacheUrl = CacheMedia(audioData, $"{model.LastRightPart('/')}.{ext}", mimeType, context,
            GeneratorUtils.ToFileInfo(chat, new JsonObject { ["cost"] = cost }));

        var ret = BuildMediaResponse("audios", new JsonArray(AudioPart(cacheUrl)),
            usage: new JsonObject
            {
                ["prompt_tokens"] = prompt.Length,
                ["completion_tokens"] = 0,
                ["total_tokens"] = prompt.Length,
                ["cost"] = cost,
            },
            cost: cost,
            content: content);

        var metadata = new JsonObject
        {
            ["duration"] = (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds,
        };
        if (pricingInfo != null)
            metadata["pricing"] = pricingInfo;
        ret["metadata"] = metadata;

        context.ProviderResponse = ret;
        return ret;
    }
}

/// <summary>OpenRouter text-to-speech (POST /audio/speech, returns raw audio bytes)</summary>
public class OpenRouterTextToSpeech : AudioGeneratorBase
{
    public override string Sdk => "openrouter/text-to-speech";

    string responseFormat = "mp3";
    double? speed;

    public override void Populate(JsonObject kwargs)
    {
        base.Populate(kwargs);
        responseFormat = kwargs.GetString("response_format") ?? "mp3";
        speed = kwargs.GetDouble("speed");
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var parent = context.Provider;
        var model = parent?.ProviderModel(chat.GetString("model") ?? "") ?? chat.GetString("model") ?? "";
        var modelInfo = parent?.ModelInfo(model)
            ?? throw new Exception($"Could not find model_info for {model}");
        var modelDefaults = modelInfo.GetObject("defaults") ?? new JsonObject();
        var metadata = chat.GetObject("metadata") ?? new JsonObject();

        var api = (Api.IsNullOrEmpty() ? parent?.Api : Api) ?? "https://openrouter.ai/api/v1";
        var format = chat.GetString("response_format")
            ?? metadata.GetString("response_format")
            ?? modelDefaults.GetString("response_format")
            ?? responseFormat;

        var body = new JsonObject
        {
            ["model"] = model,
            ["input"] = ChatMessages.LastUserPrompt(chat) ?? "",
            ["voice"] = metadata.GetString("voice") ?? modelDefaults.GetString("voice"),
            ["response_format"] = format,
        };
        if (modelDefaults.GetObject("options") is { } options)
            body["provider"] = new JsonObject { ["options"] = options.Clone() };
        if ((chat.GetDouble("speed") ?? speed) is { } resolvedSpeed)
            body["speed"] = resolvedSpeed;

        var url = $"{api.TrimEnd('/')}/audio/speech";
        Log.LogInformation("POST {Url}", url);

        using var client = CreateHttpClient();
        var httpReq = new HttpRequestMessage(HttpMethod.Post, url);
        foreach (var entry in GetHeaders(parent))
        {
            if (!entry.Key.EqualsIgnoreCase("Content-Type"))
                httpReq.Headers.TryAddWithoutValidation(entry.Key, entry.Value);
        }
        httpReq.Content = new StringContent(body.ToJsonString(ChatJson.Options),
            System.Text.Encoding.UTF8, MimeTypes.Json);

        var startedAt = DateTimeOffset.UtcNow;
        using var res = await client.SendAsync(httpReq, context.CancellationToken).ConfigAwait();
        if (!res.IsSuccessStatusCode)
        {
            var text = await res.Content.ReadAsStringAsync(context.CancellationToken).ConfigAwait();
            throw new Exception(OpenAiCompatibleProvider.HttpErrorToMessage(res, text));
        }

        var audioData = await res.Content.ReadAsByteArrayAsync(context.CancellationToken).ConfigAwait();
        if (format == "pcm")
        {
            audioData = PcmToWav(audioData);
            format = "wav";
        }
        return BuildAudioResponse(audioData, chat, format, context, startedAt);
    }
}

/// <summary>
/// OpenRouter audio-output chat: asks a chat model for an audio modality and extracts the
/// returned audio (streamed or not).
/// </summary>
public class OpenRouterAudioGenerator : AudioGeneratorBase
{
    public override string Sdk => "openrouter/audio";

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var parent = context.Provider;
        var model = parent?.ProviderModel(chat.GetString("model") ?? "") ?? chat.GetString("model") ?? "";
        var isOpenAi = model.ToLowerInvariant().Contains("openai/");
        var api = (Api.IsNullOrEmpty() ? parent?.Api : Api) ?? "https://openrouter.ai/api/v1";

        var body = await ProcessGeneratorChatAsync(chat, parent).ConfigAwait();
        body["model"] = model;
        body["modalities"] = new JsonArray("text", "audio");
        body["audio"] = new JsonObject
        {
            ["voice"] = "alloy",
            ["format"] = isOpenAi ? "pcm16" : "mp3",
        };
        body["stream"] = false; // simpler + equivalent: OpenRouter returns message.audio.data
        body.Remove("metadata");

        var startedAt = DateTimeOffset.UtcNow;
        var response = await PostJsonAsync($"{api.TrimEnd('/')}/chat/completions", body,
            GetHeaders(parent), context).ConfigAwait();

        var message = (response.GetArray("choices") is { Count: > 0 } choices
            ? choices[0] as JsonObject
            : null).GetObject("message")
            ?? throw new Exception("No audio data found in response message.");
        var audioField = message.GetObject("audio")
            ?? throw new Exception("No audio data found in response message.");
        var base64Data = audioField.GetString("data")
            ?? throw new Exception("No audio data found in response message.");

        var audioData = Convert.FromBase64String(base64Data);
        var content = message.GetString("content") ?? audioField.GetString("transcript");

        var ext = DetectAudioExtension(audioData, isOpenAi ? "wav" : "mp3");
        if (isOpenAi && ext == "wav" && !(audioData.Length >= 4 && audioData[0] == 0x52))
        {
            audioData = PcmToWav(audioData);
        }
        return BuildAudioResponse(audioData, chat, ext, context, startedAt, content);
    }
}

/// <summary>Mistral audio transcription (voxtral) — https://docs.mistral.ai/api/endpoint/audio/transcriptions</summary>
public class MistralTranscriptionGenerator : GeneratorProvider
{
    public override string Sdk => "mistral/transcriptions";

    public const string ApiUrl = "https://api.mistral.ai/v1/audio/transcriptions";

    /// <summary>Transcribe raw audio bytes — also used directly by the voice extension</summary>
    public async Task<JsonObject> TranscribeAsync(byte[] fileBytes, string fileName,
        string? model = null, string? apiKey = null, CancellationToken token = default)
    {
        model ??= "voxtral-mini-latest";
        apiKey ??= ApiKey ?? throw new Exception("MISTRAL_API_KEY not configured");

        using var client = CreateHttpClient();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(model), "model");
        var fileContent = new ByteArrayContent(fileBytes);
        var mimeType = MimeTypes.GetMimeType(fileName);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            mimeType.StartsWith("audio/") ? mimeType : "audio/mpeg");
        form.Add(fileContent, "file", fileName);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        httpReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        httpReq.Headers.TryAddWithoutValidation("x-api-key", apiKey);
        httpReq.Content = form;

        Log.LogInformation("POST {Url} model={Model} file={File} ({Size} bytes)",
            ApiUrl, model, fileName, fileBytes.Length);

        using var res = await client.SendAsync(httpReq, token).ConfigAwait();
        var text = await res.Content.ReadAsStringAsync(token).ConfigAwait();
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Mistral API Error {(int)res.StatusCode}: {text}");
        return ChatJson.TryParseObject(text) ?? new JsonObject { ["text"] = text };
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var parent = context.Provider;
        var model = parent?.ProviderModel(chat.GetString("model") ?? "") ?? chat.GetString("model")
            ?? "voxtral-mini-latest";
        if (model == "voxtral-mini-transcription")
            model = "voxtral-mini-latest";

        var processed = await ProcessGeneratorChatAsync(chat, parent).ConfigAwait();

        // find the audio input in the messages (already resolved to base64 by ProcessChatAsync)
        byte[]? audioBytes = null;
        var fileName = "audio.mp3";
        foreach (var messageNode in processed.GetArray("messages") ?? [])
        {
            if ((messageNode as JsonObject)?["content"] is not JsonArray parts)
                continue;
            foreach (var partNode in parts)
            {
                if (partNode is not JsonObject part)
                    continue;
                if (part.GetString("type") == "input_audio"
                    && part.GetObject("input_audio")?.GetString("data") is { } data)
                {
                    var base64 = data.StartsWith("data:") ? data.RightPart(',') : data;
                    audioBytes = Convert.FromBase64String(base64);
                    if (part.GetObject("input_audio")?.GetString("format") is { } fmt)
                        fileName = $"audio.{fmt}";
                }
            }
        }
        if (audioBytes == null)
            throw new Exception("No audio input found to transcribe");

        var result = await TranscribeAsync(audioBytes, fileName, model,
            parent?.ApiKey ?? ApiKey, context.CancellationToken).ConfigAwait();
        context.ProviderResponse = result;

        var transcript = result.GetString("text") ?? "";
        return new JsonObject
        {
            ["choices"] = new JsonArray(new JsonObject
            {
                ["index"] = 0,
                ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = transcript },
                ["finish_reason"] = "stop",
            }),
            ["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["model"] = model,
            ["usage"] = result.GetObject("usage")?.Clone() ?? new JsonObject
            {
                ["prompt_tokens"] = 0, ["completion_tokens"] = 0, ["total_tokens"] = 0,
            },
        };
    }
}
