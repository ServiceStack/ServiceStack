using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Speech-to-text (port of llms-py's "voice" extension): POST /transcribe accepts multipart audio
/// and transcribes it using the first available option in LLMS_VOICE
/// (voxtype/transcribe local CLIs + ffmpeg, any OpenAI-compatible transcription API, or Mistral's
/// voxtral API). Self-disables when none are available, like Python.
/// </summary>
public class VoiceExtension() : ChatExtension("voice")
{
    string? mode;
    VoiceApiConfig? apiConfig;

    /// <summary>Providers the "api" mode knows about: id, API key variable, endpoint, default model</summary>
    static readonly (string Id, string EnvVar, string Url, string Model)[] ApiProviders =
    [
        ("groq", "GROQ_API_KEY", "https://api.groq.com/openai/v1/audio/transcriptions", "whisper-large-v3-turbo"),
        ("openai", "OPENAI_API_KEY", "https://api.openai.com/v1/audio/transcriptions", "whisper-1"),
        ("mistral", "MISTRAL_API_KEY", "https://api.mistral.ai/v1/audio/transcriptions", "voxtral-mini-latest"),
    ];

    /// <summary>
    /// The browser records webm, which the generic mime lookup calls video/webm — a type some
    /// transcription APIs reject. Map the formats these endpoints actually accept.
    /// </summary>
    static readonly Dictionary<string, string> AudioTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".webm"] = "audio/webm",
        [".wav"] = "audio/wav",
        [".mp3"] = "audio/mpeg",
        [".mpga"] = "audio/mpeg",
        [".m4a"] = "audio/mp4",
        [".mp4"] = "audio/mp4",
        [".ogg"] = "audio/ogg",
        [".oga"] = "audio/ogg",
        [".opus"] = "audio/opus",
        [".flac"] = "audio/flac",
    };

    static string AudioContentType(string? fileName) =>
        AudioTypes.TryGetValue(Path.GetExtension(fileName ?? ""), out var type)
            ? type
            : "application/octet-stream";

    /// <summary>
    /// Formats the transcription APIs reliably decode. The browser records webm/opus, which Groq
    /// and OpenAI accept but Mistral rejects with "Audio input could not be decoded", so anything
    /// outside this set is converted to WAV first when ffmpeg is available.
    /// </summary>
    static readonly HashSet<string> PortableFormats = new(StringComparer.OrdinalIgnoreCase)
        { ".wav", ".mp3", ".mpga", ".m4a", ".mp4", ".flac", ".ogg", ".oga" };

    /// <summary>
    /// Where ffmpeg usually lives. A process launched from a GUI, a service manager or a
    /// sanitised environment often doesn't inherit Homebrew's PATH, so PATH alone misses an
    /// ffmpeg the user definitely has installed.
    /// </summary>
    static readonly string[] FfmpegPaths =
        ["/opt/homebrew/bin/ffmpeg", "/usr/local/bin/ffmpeg", "/usr/bin/ffmpeg", "/snap/bin/ffmpeg"];

    static string? WhichFfmpeg()
    {
        var found = Which("ffmpeg");
        if (found != null)
            return found;
        foreach (var path in FfmpegPaths)
        {
            if (File.Exists(path))
                return path;
        }
        return null;
    }

    /// <summary>
    /// Convert to 16 kHz mono WAV when the recording is in a format some providers can't decode.
    /// Returns the original unchanged when ffmpeg isn't installed or the conversion fails.
    /// </summary>
    async Task<(byte[] Audio, string FileName, bool Converted, string Error)> ToPortableAudioAsync(
        byte[] audioBytes, string fileName)
    {
        var ext = Path.GetExtension(fileName ?? "");
        if (PortableFormats.Contains(ext))
            return (audioBytes, fileName!, false, "");
        var ffmpeg = WhichFfmpeg();
        if (ffmpeg == null)
        {
            const string reason = "ffmpeg not found on PATH";
            Log.LogInformation("{Reason}, sending {Ext} unconverted", reason, ext.Length > 0 ? ext : "audio");
            return (audioBytes, fileName!, false, reason);
        }

        var tempInput = Path.Combine(Path.GetTempPath(),
            "voice-" + Guid.NewGuid().ToString("n")[..8] + (ext.Length > 0 ? ext : ".webm"));
        var tempWav = tempInput + ".wav";
        try
        {
            await File.WriteAllBytesAsync(tempInput, audioBytes).ConfigAwait();
            var (_, stderr, exitCode) = await RunAsync(ffmpeg,
                ["-i", tempInput, "-ar", "16000", "-ac", "1", "-c:a", "pcm_s16le", tempWav, "-y"]).ConfigAwait();
            if (exitCode != 0 || !File.Exists(tempWav))
            {
                var reason = $"ffmpeg conversion failed ({Truncate(stderr.Trim(), 200)})";
                Log.LogInformation("{Reason}, sending original audio", reason);
                return (audioBytes, fileName!, false, reason);
            }
            var converted = await File.ReadAllBytesAsync(tempWav).ConfigAwait();
            var wavName = Path.GetFileNameWithoutExtension(fileName ?? "audio") + ".wav";
            Log.LogDebug("converted {Ext} ({InSize} bytes) to wav ({OutSize} bytes) with {Ffmpeg}",
                ext, audioBytes.Length, converted.Length, ffmpeg);
            return (converted, wavName, true, "");
        }
        catch (Exception e)
        {
            var reason = $"ffmpeg conversion failed ({e.Message})";
            Log.LogInformation("{Reason}, sending original audio", reason);
            return (audioBytes, fileName!, false, reason);
        }
        finally
        {
            try { File.Delete(tempInput); } catch (Exception) { /* best effort */ }
            try { File.Delete(tempWav); } catch (Exception) { /* best effort */ }
        }
    }

    public override void Install(ExtensionContext ctx)
    {
        var voiceOptions = (Environment.GetEnvironmentVariable("LLMS_VOICE")
            ?? "voxtype,transcribe,api,voxtral-mini-latest")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var opt in voiceOptions)
        {
            if (opt is "voxtype" or "transcribe")
            {
                if (Which(opt) == null)
                {
                    Log.LogDebug("Cannot use {Opt} - not installed", opt);
                    continue;
                }
                mode = opt;
                break;
            }
            if (opt == "api")
            {
                apiConfig = ResolveApiConfig(ctx);
                if (apiConfig == null)
                    continue;
                mode = opt;
                break;
            }
            if (opt.StartsWith("voxtral"))
            {
                var mistral = ctx.Config.GetObject("providers").GetObject("mistral");
                var apiKey = ctx.Feature.ResolveVariable("$MISTRAL_API_KEY");
                if (mistral == null || !mistral.GetBool("enabled") || string.IsNullOrEmpty(apiKey))
                {
                    Log.LogDebug("Cannot use {Opt} - Mistral not enabled", opt);
                    continue;
                }
                mode = opt;
                break;
            }
            Log.LogDebug("Cannot use {Opt} - unknown voice mode", opt);
        }

        if (mode is "voxtype" or "transcribe" && WhichFfmpeg() == null)
        {
            Log.LogDebug("Cannot use {Mode} - ffmpeg not installed", mode);
            mode = null;
        }

        if (mode == null)
        {
            ctx.Disabled = true;
            return;
        }

        if (mode == "api" && apiConfig != null)
        {
            Log.LogInformation("Using api for voice: {Provider} [{ProviderSource}] model={Model} [{ModelSource}]",
                apiConfig.Provider, apiConfig.ProviderSource, apiConfig.Model, apiConfig.ModelSource);
            Log.LogDebug("Voice endpoint: {Url} [{UrlSource}]", apiConfig.Url, apiConfig.UrlSource);
        }
        else
        {
            Log.LogInformation("Using {Mode} for voice", mode);
        }

        ctx.AddPost("/transcribe", TranscribeAsync);
    }

    /// <summary>
    /// Resolve the endpoint, model and key for the "api" mode.
    /// Precedence: LLMS_TRANSCRIBE_* environment > defaults.voice in llms.json >
    /// auto-detection from whichever provider API key is configured.
    /// Returns null when nothing is configured, which makes the mode unavailable.
    /// A configured provider whose API key is missing falls back to any other provider that
    /// has one, so the shipped default doesn't disable voice input for a different provider.
    /// </summary>
    VoiceApiConfig? ResolveApiConfig(ExtensionContext ctx)
    {
        var voice = ctx.GetConfigDefaults().GetObject("voice");

        // env wins, then llms.json (where a leading $ reads a variable, as api_key does elsewhere)
        (string Value, string Source) Setting(string envVar, string key)
        {
            var fromEnv = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return (fromEnv.Trim(), "env");
            var fromConfig = voice.GetString(key);
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                var resolved = ctx.Feature.ResolveVariable(fromConfig);
                if (!string.IsNullOrWhiteSpace(resolved))
                    return (resolved.Trim(), "llms.json");
            }
            return ("", "");
        }

        var (url, urlSource) = Setting("LLMS_TRANSCRIBE_URL", "url");
        var (model, modelSource) = Setting("LLMS_TRANSCRIBE_MODEL", "model");
        var (apiKey, keySource) = Setting("LLMS_TRANSCRIBE_KEY", "api_key");
        var (want, wantSource) = Setting("LLMS_TRANSCRIBE_PROVIDER", "provider");
        var (language, _) = Setting("LLMS_TRANSCRIBE_LANG", "language");
        var (prompt, _) = Setting("LLMS_TRANSCRIBE_PROMPT", "prompt");

        (string Id, string EnvVar, string Url, string Model)? FirstProviderWithKey()
        {
            foreach (var candidate in ApiProviders)
            {
                if (!string.IsNullOrEmpty(ctx.Feature.ResolveVariable("$" + candidate.EnvVar)))
                    return candidate;
            }
            return null;
        }

        (string Id, string EnvVar, string Url, string Model)? provider = null;
        var providerSource = "";

        if (want != "")
        {
            (string Id, string EnvVar, string Url, string Model)? named = null;
            foreach (var candidate in ApiProviders)
            {
                if (candidate.Id == want)
                {
                    named = candidate;
                    break;
                }
            }

            if (named == null)
            {
                Log.LogInformation("Unknown voice provider '{Provider}' (from {Source}), known: {Known}",
                    want, wantSource, string.Join(", ", ApiProviders.Select(x => x.Id)));
            }
            else if (apiKey != "" || !string.IsNullOrEmpty(ctx.Feature.ResolveVariable("$" + named.Value.EnvVar)))
            {
                provider = named;
                providerSource = wantSource;
            }
            else
            {
                Log.LogDebug("Voice provider '{Provider}' has no {EnvVar}, looking for another",
                    named.Value.Id, named.Value.EnvVar);
            }

            if (provider == null)
            {
                // Fall back rather than lose voice input, e.g. the shipped default names
                // mistral but this user only has a Groq key.
                provider = FirstProviderWithKey();
                if (provider != null)
                {
                    providerSource = "fallback";
                    // the configured model and url belonged to the provider we skipped
                    if (modelSource == "llms.json")
                    {
                        model = "";
                        modelSource = "";
                    }
                    if (urlSource == "llms.json")
                    {
                        url = "";
                        urlSource = "";
                    }
                }
            }
        }
        else if (url == "")
        {
            // only auto-detect when no explicit endpoint was given
            provider = FirstProviderWithKey();
            if (provider != null)
                providerSource = "auto";
        }

        string providerId;
        if (provider != null)
        {
            var p = provider.Value;
            providerId = p.Id;
            if (url == "")
            {
                url = p.Url;
                urlSource = "default";
            }
            if (model == "")
            {
                model = p.Model;
                modelSource = "default";
            }
            if (apiKey == "")
            {
                apiKey = ctx.Feature.ResolveVariable("$" + p.EnvVar) ?? "";
                keySource = "provider env";
            }
            if (string.IsNullOrEmpty(apiKey))
            {
                Log.LogDebug("Cannot use api - voice provider '{Provider}' selected ({Source}) but {EnvVar} is not set",
                    p.Id, providerSource, p.EnvVar);
                return null;
            }
        }
        else
        {
            // a local server (speaches, faster-whisper-server, ...) usually needs no key
            if (url == "")
            {
                Log.LogDebug("Cannot use api - no voice provider configured, see defaults.voice in llms.json");
                return null;
            }
            if (model == "")
            {
                Log.LogDebug("Cannot use api - a voice url is configured but no model");
                return null;
            }
            providerId = "custom";
            providerSource = "url";
        }

        return new VoiceApiConfig(providerId, url, model,
            apiKey == "" ? null : apiKey,
            language == "" ? null : language,
            prompt == "" ? null : prompt,
            providerSource, urlSource == "" ? "default" : urlSource,
            modelSource == "" ? "default" : modelSource,
            keySource == "" ? "provider env" : keySource);
    }

    async Task<object?> TranscribeAsync(ChatRequestContext req)
    {
        var file = req.Request.Files.FirstOrDefault(x => x.Name == "file")
            ?? req.Request.Files.FirstOrDefault()
            ?? throw new Exception("No audio file provided");

        using var ms = new MemoryStream();
        await file.InputStream.CopyToAsync(ms).ConfigAwait();
        var audioBytes = ms.ToArray();
        var filename = file.FileName ?? "audio.mp3";

        if (mode == "api")
        {
            var text = await TranscribeApiAsync(audioBytes, filename).ConfigAwait();
            return new JsonObject { ["text"] = text, ["mode"] = mode, ["model"] = apiConfig!.Model };
        }

        if (mode!.StartsWith("voxtral"))
        {
            // Mistral can't decode the browser's webm, so convert here too
            var (voxtralBytes, voxtralName, _, _) = await ToPortableAudioAsync(audioBytes, filename).ConfigAwait();
            var result = await TranscribeMistralAsync(voxtralBytes, voxtralName).ConfigAwait();
            result["mode"] = mode;
            return result;
        }

        // local CLI path: convert to 16kHz WAV then run voxtype/transcribe
        var tempInput = Path.Combine(Path.GetTempPath(), "voice-" + Guid.NewGuid().ToString("n")[..8] + Path.GetExtension(filename));
        var tempWav = tempInput + ".wav";
        try
        {
            await File.WriteAllBytesAsync(tempInput, audioBytes).ConfigAwait();
            await RunAsync("ffmpeg", ["-i", tempInput, "-ar", "16000", "-ac", "1", "-c:a", "pcm_s16le", tempWav, "-y"]).ConfigAwait();

            if (mode == "transcribe")
            {
                var (stdout, stderr, exitCode) = await RunAsync("transcribe", [tempWav]).ConfigAwait();
                if (exitCode != 0)
                    throw new Exception(stderr);
                return new JsonObject { ["text"] = stdout.Trim(), ["mode"] = mode };
            }

            var voxResult = await RunAsync("voxtype", ["transcribe", tempWav]).ConfigAwait();
            // take the last non-empty output line that isn't a log line
            var ansiEscape = new Regex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])");
            var transcription = voxResult.Stdout
                .Split('\n')
                .Select(line => ansiEscape.Replace(line, "").Trim())
                .LastOrDefault(line => line.Length > 0 && !line.StartsWith('[') && !line.Contains("INFO")) ?? "";

            return new JsonObject { ["text"] = transcription, ["mode"] = mode };
        }
        finally
        {
            try { File.Delete(tempInput); } catch (Exception) { /* best effort */ }
            try { File.Delete(tempWav); } catch (Exception) { /* best effort */ }
        }
    }

    /// <summary>Transcribe via any OpenAI-compatible /v1/audio/transcriptions endpoint</summary>
    async Task<string> TranscribeApiAsync(byte[] audioBytes, string fileName, CancellationToken token = default)
    {
        var cfg = apiConfig ?? throw new Exception("Voice api mode is not configured");

        bool converted;
        string convertError;
        (audioBytes, fileName, converted, convertError) =
            await ToPortableAudioAsync(audioBytes, fileName).ConfigAwait();

        using var client = Ctx.Feature.HttpClientFactory.CreateClient();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(cfg.Model), "model");
        form.Add(new StringContent("json"), "response_format");
        if (!string.IsNullOrEmpty(cfg.Language))
            form.Add(new StringContent(cfg.Language), "language");
        if (!string.IsNullOrEmpty(cfg.Prompt))
            form.Add(new StringContent(cfg.Prompt), "prompt");

        var fileContent = new ByteArrayContent(audioBytes);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(AudioContentType(fileName));
        form.Add(fileContent, "file", fileName);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, cfg.Url) { Content = form };
        if (!string.IsNullOrEmpty(cfg.ApiKey))
        {
            httpReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {cfg.ApiKey}");
            // Mistral's transcription endpoint also accepts x-api-key; send both, matching MistralTranscriptionGenerator
            if (cfg.Provider == "mistral")
                httpReq.Headers.TryAddWithoutValidation("x-api-key", cfg.ApiKey);
        }

        Log.LogDebug("POST {Url} model={Model} file={File} ({Size} bytes)",
            cfg.Url, cfg.Model, fileName, audioBytes.Length);

        using var res = await client.SendAsync(httpReq, token).ConfigAwait();
        var body = await res.Content.ReadAsStringAsync(token).ConfigAwait();
        if (!res.IsSuccessStatusCode)
        {
            var message = $"{cfg.Provider} returned {(int)res.StatusCode}: {Truncate(body, 500)}";
            if (!converted && body.Contains("decod", StringComparison.OrdinalIgnoreCase))
            {
                var ext = Path.GetExtension(fileName);
                message += $"\n{cfg.Provider} could not decode this recording "
                    + $"({(ext.Length > 0 ? ext : "unknown format")}) and it was not converted: "
                    + $"{(convertError.Length > 0 ? convertError : "already a portable format")}. "
                    + "Newer browsers convert to WAV before uploading; otherwise install ffmpeg, "
                    + "or use a provider that accepts this format (groq, openai).";
            }
            throw new Exception(message);
        }

        var result = ChatJson.TryParseObject(body)
            ?? throw new Exception($"{cfg.Provider} returned a non-JSON response: {Truncate(body, 300)}");

        var text = result.GetString("text");
        if (text == null)
        {
            // some servers only return segments
            text = string.Concat((result.GetArray("segments") ?? [])
                .OfType<JsonObject>()
                .Select(x => x.GetString("text") ?? ""));
        }
        return text.Trim();
    }

    /// <summary>Transcribe via Mistral's voxtral API, reusing the provider's transcription generator</summary>
    async Task<JsonObject> TranscribeMistralAsync(byte[] audioBytes, string filename)
    {
        var apiKey = Ctx.Feature.ResolveVariable("$MISTRAL_API_KEY")
            ?? throw new Exception("MISTRAL_API_KEY not configured");
        var model = mode == "voxtral" ? "voxtral-mini-latest" : mode!;

        // prefer the live provider's generator so config/headers stay in one place
        var generator = Ctx.Feature.Providers.Values.OfType<MistralProvider>().FirstOrDefault()?.Transcription
            ?? new MistralTranscriptionGenerator
            {
                Log = Ctx.Log,
                HttpClientFactory = Ctx.Feature.HttpClientFactory,
                Feature = Ctx.Feature,
            };
        return await generator.TranscribeAsync(audioBytes, filename, model, apiKey).ConfigAwait();
    }

    static async Task<(string Stdout, string Stderr, int ExitCode)> RunAsync(string fileName, string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await process.WaitForExitAsync(cts.Token).ConfigAwait();
        return (await stdoutTask.ConfigAwait(), await stderrTask.ConfigAwait(), process.ExitCode);
    }

    static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";

    static string? Which(string name)
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(dir, name);
            if (File.Exists(fullPath))
                return fullPath;
        }
        return null;
    }
}

/// <summary>Resolved configuration for the voice "api" mode, with where each value came from</summary>
public record VoiceApiConfig(
    string Provider,
    string Url,
    string Model,
    string? ApiKey,
    string? Language,
    string? Prompt,
    string ProviderSource,
    string UrlSource,
    string ModelSource,
    string KeySource);
