using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Image generators (ports of the *Generator/\*Image classes in llms/extensions/providers/*.py).
/// Each builds a provider-specific request from the last user prompt + resolved aspect ratio, POSTs it,
/// then caches the returned image (url or base64) and returns the shared OpenAI media response.
/// </summary>
public static class GeneratorUtils
{
    /// <summary>Info recorded alongside cached media (port of to_file_info)</summary>
    public static JsonObject ToFileInfo(JsonObject chat, JsonObject? info = null)
    {
        var ret = info?.Clone() ?? new JsonObject();
        if (chat.GetString("model") is { } model)
            ret["model"] ??= model;
        if (ChatMessages.LastUserPrompt(chat) is { } prompt)
            ret["prompt"] ??= prompt;
        foreach (var entry in chat.GetObject("image_config") ?? [])
        {
            ret[entry.Key] = entry.Value?.DeepClone();
        }
        return ret;
    }
}

/// <summary>Z.ai image generation — https://docs.z.ai/guides/image/glm-image</summary>
public class ZaiImageGenerator : GeneratorProvider
{
    public override string Sdk => "zai/image";

    // Z.ai publishes its own dimensions per ratio
    static readonly Dictionary<string, string> ZaiRatios = new()
    {
        ["1:1"] = "1280x1280", ["2:3"] = "1056x1568", ["3:2"] = "1568x1056",
        ["3:4"] = "1088x1472", ["4:3"] = "1472x1088", ["4:5"] = "1088x1472",
        ["5:4"] = "1472x1088", ["9:16"] = "960x1728", ["16:9"] = "1728x960",
        ["21:9"] = "1728x960",
    };

    public override void Populate(JsonObject kwargs)
    {
        kwargs["api"] ??= "https://api.z.ai/api/paas/v4";
        base.Populate(kwargs);
        AspectRatios = new(ZaiRatios);
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var parent = context.Provider;
        var model = parent?.ProviderModel(chat.GetString("model") ?? "") ?? chat.GetString("model");
        var url = $"{(parent as ChatProvider)?.Api ?? Api}/images/generations";
        var (size, width, height) = ResolveSize(chat);
        var prompt = ChatMessages.LastUserPrompt(chat) ?? "";

        var body = new JsonObject
        {
            ["model"] = model,
            ["prompt"] = prompt,
            ["size"] = size,
        };
        if (context.User is { } user)
            body["user"] = user;

        var startedAt = DateTimeOffset.UtcNow;
        var response = await PostJsonAsync(url, body, GetHeaders(parent), context).ConfigAwait();
        var duration = (long)(DateTimeOffset.UtcNow - startedAt).TotalSeconds;
        var usage = response.GetObject("usage");

        var images = new JsonArray();
        foreach (var itemNode in response.GetArray("data") ?? [])
        {
            var imageUrl = (itemNode as JsonObject).GetString("url");
            if (imageUrl == null)
                continue;
            var defaultName = $"{(model ?? "image").ToLowerInvariant()}-{response.GetString("id") ?? startedAt.ToUnixTimeSeconds().ToString()}.png";
            var (bytes, mimeType, fileName) = await DownloadAsync(imageUrl, defaultName, context.CancellationToken).ConfigAwait();

            var info = GeneratorUtils.ToFileInfo(chat, new JsonObject
            {
                ["width"] = width,
                ["height"] = height,
                ["duration"] = duration,
            });
            images.Add(ImagePart(CacheMedia(bytes, fileName, mimeType, context, info)));
        }
        return BuildMediaResponse("images", images, cost: usage.GetDouble("cost"));
    }
}

/// <summary>Chutes image generation (per-model endpoints under *.chutes.ai/generate)</summary>
public class ChutesImageGenerator : GeneratorProvider
{
    public override string Sdk => "chutes/image";

    double cfgScale = 7.5;
    int steps = 50;
    string negativePrompt = "blur, distortion, low quality";
    string genUrl = "https://image.chutes.ai/generate";

    // models that take a "resolution" or "size" instead of width/height
    static readonly Dictionary<string, Dictionary<string, string>> ModelResolutions = new()
    {
        ["chutes-hidream"] = new()
        {
            ["1:1"] = "1024x1024", ["9:16"] = "768x1360", ["16:9"] = "1360x768",
            ["3:4"] = "880x1168", ["4:3"] = "1168x880", ["2:3"] = "832x1248", ["3:2"] = "1248x832",
        },
    };
    static readonly HashSet<string> ModelSizes = ["chutes-hunyuan-image-3"];
    static readonly HashSet<string> ModelNegativePrompt =
    [
        "chroma", "qwen-image-edit-2509", "JuggernautXL-Ragnarok", "JuggernautXL", "Animij", "iLustMix",
    ];
    const string ImageClassicUrl = "https://vonkaiser-imageclassic.chutes.ai/generate";
    static readonly Dictionary<string, string> ModelGenUrls = new()
    {
        ["flux"] = ImageClassicUrl, ["dreamshaper"] = ImageClassicUrl, ["ilustmix"] = ImageClassicUrl,
        ["juggernaut"] = ImageClassicUrl,
        ["z-image-turbo"] = "https://vonkaiser-z-image-turbo.chutes.ai/generate",
        ["Qwen-Image-2512"] = "https://vonkaiser-qwen-image-2512.chutes.ai/generate",
        ["Qwen-Image-Edit-2511"] = "https://vonkaiser-qwen-image-edit-2511.chutes.ai/generate",
    };

    public override void Populate(JsonObject kwargs)
    {
        // GeneratorProvider defaults a missing "api" to "", so declare our endpoint before it does
        kwargs["api"] ??= genUrl;
        base.Populate(kwargs);
        cfgScale = kwargs.GetDouble("cfg_scale") ?? 7.5;
        steps = kwargs.GetInt("steps") ?? 50;
        negativePrompt = kwargs.GetString("negative_prompt") ?? negativePrompt;
        genUrl = Api;
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var parent = context.Provider;
        var model = parent?.ProviderModel(chat.GetString("model") ?? "") ?? chat.GetString("model") ?? "";
        var ratio = ChatMessages.ChatToAspectRatio(chat) ?? "1:1";
        var (_, width, height) = ResolveSize(chat);

        var scale = model == "z-image-turbo" ? Math.Min(cfgScale, 5) : cfgScale;
        var payload = new JsonObject
        {
            ["model"] = model,
            ["prompt"] = ChatMessages.LastUserPrompt(chat) ?? "",
            ["guidance_scale"] = scale,
            ["num_inference_steps"] = steps,
            ["width"] = width,
            ["height"] = height,
        };
        if (ModelNegativePrompt.Contains(model))
            payload["negative_prompt"] = negativePrompt;

        if (ModelResolutions.TryGetValue(model, out var resolutions))
        {
            payload.Remove("width");
            payload.Remove("height");
            payload["resolution"] = resolutions.GetValueOrDefault(ratio, resolutions["1:1"]);
        }
        else if (ModelSizes.Contains(model))
        {
            payload.Remove("width");
            payload.Remove("height");
            payload["size"] = ratio;
        }

        var url = ModelGenUrls.GetValueOrDefault(model);
        if (url == null)
        {
            url = $"https://{model}.chutes.ai/generate";
            payload.Remove("model");
        }

        // chutes returns the raw image bytes, not JSON
        using var client = CreateHttpClient();
        var httpReq = new HttpRequestMessage(HttpMethod.Post, url);
        var apiKey = parent?.ApiKey ?? ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
            httpReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        httpReq.Content = new StringContent(payload.ToJsonString(ChatJson.Options),
            System.Text.Encoding.UTF8, MimeTypes.Json);

        Log.LogInformation("POST {Url}", url);
        using var res = await client.SendAsync(httpReq, context.CancellationToken).ConfigAwait();
        if (!res.IsSuccessStatusCode)
        {
            var text = await res.Content.ReadAsStringAsync(context.CancellationToken).ConfigAwait();
            var detail = ChatJson.TryParseObject(text).GetString("detail");
            throw new Exception(detail ?? $"Failed to generate image {(int)res.StatusCode}");
        }

        var bytes = await res.Content.ReadAsByteArrayAsync(context.CancellationToken).ConfigAwait();
        var mimeType = res.Content.Headers.ContentType?.MediaType ?? "image/png";
        var ext = MimeTypes.GetExtension(mimeType).TrimStart('.');
        if (string.IsNullOrEmpty(ext)) ext = "png";

        var info = GeneratorUtils.ToFileInfo(chat, new JsonObject
        {
            ["aspect_ratio"] = ratio,
            ["width"] = width,
            ["height"] = height,
            ["cfg_scale"] = scale,
            ["steps"] = steps,
        });
        var cacheUrl = CacheMedia(bytes, $"{model}.{ext}", mimeType, context, info);
        return BuildMediaResponse("images", new JsonArray(ImagePart(cacheUrl)));
    }
}

/// <summary>NVIDIA GenAI image generation (returns base64 artifacts)</summary>
public class NvidiaImageGenerator : GeneratorProvider
{
    public override string Sdk => "nvidia/image";

    int defaultWidth = 1024, defaultHeight = 1024, steps = 20;
    double cfgScale = 3;
    string mode = "base";
    string genUrl = "https://ai.api.nvidia.com/v1/genai";

    public override void Populate(JsonObject kwargs)
    {
        // GeneratorProvider defaults a missing "api" to "", so declare our endpoint before it does
        kwargs["api"] ??= genUrl;
        base.Populate(kwargs);
        defaultWidth = kwargs.GetInt("width") ?? 1024;
        defaultHeight = kwargs.GetInt("height") ?? 1024;
        cfgScale = kwargs.GetDouble("cfg_scale") ?? 3;
        steps = kwargs.GetInt("steps") ?? 20;
        mode = kwargs.GetString("mode") ?? "base";
        genUrl = Api;
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var parent = context.Provider;
        var model = parent?.ProviderModel(chat.GetString("model") ?? "") ?? chat.GetString("model") ?? "";

        var body = new JsonObject { ["prompt"] = ChatMessages.LastUserPrompt(chat) ?? "" };
        if (chat.GetArray("modalities")?.Any(x => x?.GetValue<string>() == "image") == true)
        {
            var ratio = ChatMessages.ChatToAspectRatio(chat) ?? "1:1";
            if (ChatFeature.AspectRatios.GetValueOrDefault(ratio) is { } dimension)
            {
                var parts = dimension.Replace("×", "x").Split('x');
                body["width"] = int.Parse(parts[0]);
                body["height"] = int.Parse(parts[1]);
            }
            else
            {
                body["width"] = defaultWidth;
                body["height"] = defaultHeight;
            }
            body["mode"] = mode;
            body["cfg_scale"] = cfgScale;
            body["steps"] = steps;
        }

        var response = await PostJsonAsync($"{genUrl}/{model}", body, GetHeaders(parent), context).ConfigAwait();

        var artifacts = response.GetArray("artifacts")
            ?? throw new Exception("No artifacts in response");
        foreach (var artifactNode in artifacts)
        {
            if (artifactNode is not JsonObject artifact || artifact.GetString("base64") is not { } base64)
                continue;
            var seed = artifact.GetLong("seed");
            var shortModel = model.LastRightPart('/');
            var fileName = seed != null ? $"{shortModel}_{seed}.png" : $"{shortModel}.png";

            var info = GeneratorUtils.ToFileInfo(chat, new JsonObject { ["seed"] = seed });
            var cacheUrl = CacheMedia(Convert.FromBase64String(base64), fileName, "image/png", context, info);
            return BuildMediaResponse("images", new JsonArray(ImagePart(cacheUrl)));
        }
        throw new Exception("No artifacts in response");
    }
}

/// <summary>OpenAI images/generations (dall-e-*, gpt-image-*)</summary>
public class OpenAiImageGenerator : GeneratorProvider
{
    public override string Sdk => "openai/image";

    Dictionary<string, string> mapImageModels = new() { ["gpt-5.1-codex-mini"] = "gpt-image-1-mini" };

    public override void Populate(JsonObject kwargs)
    {
        kwargs["api"] ??= "https://api.openai.com/v1";
        base.Populate(kwargs);
        if (kwargs.GetObject("map_image_models") is { } map)
        {
            mapImageModels = map.Where(x => x.Value is JsonValue)
                .ToDictionary(x => x.Key, x => x.Value!.GetValue<string>());
        }
    }

    /// <summary>OpenAI only accepts a fixed set of sizes, varying by model</summary>
    static string AspectRatioToSize(string aspectRatio, string model)
    {
        var parts = aspectRatio.Split(':');
        var w = int.Parse(parts[0]);
        var h = int.Parse(parts[1]);
        if (model == "dall-e-2")
            return "1024x1024";
        if (model == "dall-e-3")
            return w > h ? "1792x1024" : h > w ? "1024x1792" : "1024x1024";
        return w > h ? "1536x1024" : h > w ? "1024x1536" : "1024x1024";
    }

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var parent = context.Provider;
        var model = chat.GetString("model") ?? "";
        if (mapImageModels.TryGetValue(model, out var mapped))
            model = mapped;

        var ratio = ChatMessages.ChatToAspectRatio(chat) ?? "1:1";
        var body = new JsonObject
        {
            ["model"] = model,
            ["prompt"] = ChatMessages.LastUserPrompt(chat) ?? "",
            ["size"] = AspectRatioToSize(ratio, model),
        };

        var response = await PostJsonAsync($"{Api}/images/generations", body, GetHeaders(parent), context).ConfigAwait();
        if (response.GetObject("error") is { } error)
            throw new Exception(error.GetString("message") ?? "Image generation failed");

        var data = response.GetArray("data")
            ?? throw new Exception("No 'data' field in response.");
        var images = new JsonArray();
        var i = 0;
        foreach (var itemNode in data)
        {
            if (itemNode is not JsonObject item)
                continue;
            byte[]? bytes = null;
            var mimeType = "image/png";
            if (item.GetString("b64_json") is { } b64)
            {
                bytes = Convert.FromBase64String(b64);
            }
            else if (item.GetString("url") is { } imageUrl)
            {
                (bytes, mimeType, _) = await DownloadAsync(imageUrl, "image.png", context.CancellationToken).ConfigAwait();
            }
            if (bytes == null)
                throw new Exception("No image data found");

            var ext = MimeTypes.GetExtension(mimeType).TrimStart('.');
            if (string.IsNullOrEmpty(ext)) ext = "png";
            var cacheUrl = CacheMedia(bytes, $"{model}-{i++}.{ext}", mimeType, context, GeneratorUtils.ToFileInfo(chat));
            images.Add(ImagePart(cacheUrl));
        }
        return BuildMediaResponse("images", images);
    }
}

/// <summary>Fireworks AI image generation (workflows text_to_image, returns raw image bytes)</summary>
public class FireworksImageGenerator : GeneratorProvider
{
    public override string Sdk => "fireworks/image";

    const string WorkflowsUrl = "https://api.fireworks.ai/inference/v1/workflows";

    static string ModelPath(string model) => model.Contains('/')
        ? $"accounts/{model.LeftPart('/')}/models/{model.RightPart('/')}"
        : model;

    public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        var parent = context.Provider;
        var model = parent?.ProviderModel(chat.GetString("model") ?? "") ?? chat.GetString("model") ?? "";
        var (_, width, height) = ResolveSize(chat);

        var body = new JsonObject
        {
            ["prompt"] = ChatMessages.LastUserPrompt(chat) ?? "",
            ["width"] = width,
            ["height"] = height,
        };

        using var client = CreateHttpClient();
        var url = $"{WorkflowsUrl}/{ModelPath(model)}/text_to_image";
        var httpReq = new HttpRequestMessage(HttpMethod.Post, url);
        var apiKey = parent?.ApiKey ?? ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
            httpReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        httpReq.Headers.TryAddWithoutValidation("Accept", "image/jpeg");
        httpReq.Content = new StringContent(body.ToJsonString(ChatJson.Options),
            System.Text.Encoding.UTF8, MimeTypes.Json);

        Log.LogInformation("POST {Url}", url);
        using var res = await client.SendAsync(httpReq, context.CancellationToken).ConfigAwait();
        if (!res.IsSuccessStatusCode)
        {
            var text = await res.Content.ReadAsStringAsync(context.CancellationToken).ConfigAwait();
            throw new Exception(OpenAiCompatibleProvider.HttpErrorToMessage(res, text));
        }

        var bytes = await res.Content.ReadAsByteArrayAsync(context.CancellationToken).ConfigAwait();
        var mimeType = res.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        var ext = MimeTypes.GetExtension(mimeType).TrimStart('.');
        if (string.IsNullOrEmpty(ext)) ext = "jpg";

        var cacheUrl = CacheMedia(bytes, $"{model.LastRightPart('/')}.{ext}", mimeType, context,
            GeneratorUtils.ToFileInfo(chat, new JsonObject { ["width"] = width, ["height"] = height }));
        return BuildMediaResponse("images", new JsonArray(ImagePart(cacheUrl)));
    }
}
