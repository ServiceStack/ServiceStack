using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Speech-to-text (port of llms-py's "voice" extension): POST /transcribe accepts multipart audio
/// and transcribes it using the first available option in LLMS_VOICE
/// (voxtype/transcribe local CLIs + ffmpeg, or Mistral's voxtral API).
/// Self-disables when none are available, like Python.
/// </summary>
public class VoiceExtension : IChatExtension
{
    public string Name => ChatExtension.Voice;

    ExtensionContext ctx = null!;
    string? mode;

    public void Install(ExtensionContext ctx)
    {
        this.ctx = ctx;
        var voiceOptions = (Environment.GetEnvironmentVariable("LLMS_VOICE")
            ?? "voxtype,transcribe,voxtral-mini-latest").Split(',');

        foreach (var opt in voiceOptions)
        {
            if (opt is "voxtype" or "transcribe")
            {
                if (Which(opt) == null)
                {
                    ctx.Log.LogDebug("Cannot use {Opt} - {Opt} not installed", opt, opt);
                    continue;
                }
                mode = opt;
                break;
            }
            if (opt.StartsWith("voxtral"))
            {
                var mistral = ctx.Config.GetObject("providers").GetObject("mistral");
                var apiKey = ctx.Feature.ResolveVariable("$MISTRAL_API_KEY");
                if (mistral == null || !mistral.GetBool("enabled") || string.IsNullOrEmpty(apiKey))
                {
                    ctx.Log.LogDebug("Cannot use {Opt} - Mistral not enabled", opt);
                    continue;
                }
                mode = opt;
                break;
            }
        }

        if (mode is "voxtype" or "transcribe" && Which("ffmpeg") == null)
        {
            ctx.Log.LogDebug("Cannot use {Mode} - ffmpeg not installed", mode);
            mode = null;
        }

        if (mode == null)
        {
            ctx.Disabled = true;
            return;
        }

        ctx.Log.LogInformation("Using {Mode} for voice", mode);
        ctx.AddPost("/transcribe", TranscribeAsync);
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

        if (mode!.StartsWith("voxtral"))
        {
            var result = await TranscribeMistralAsync(audioBytes, filename).ConfigAwait();
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

    /// <summary>Transcribe via Mistral's voxtral API, reusing the provider's transcription generator</summary>
    async Task<JsonObject> TranscribeMistralAsync(byte[] audioBytes, string filename)
    {
        var apiKey = ctx.Feature.ResolveVariable("$MISTRAL_API_KEY")
            ?? throw new Exception("MISTRAL_API_KEY not configured");
        var model = mode == "voxtral" ? "voxtral-mini-latest" : mode!;

        // prefer the live provider's generator so config/headers stay in one place
        var generator = ctx.Feature.Providers.Values.OfType<MistralProvider>().FirstOrDefault()?.Transcription
            ?? new MistralTranscriptionGenerator
            {
                Log = ctx.Log,
                HttpClientFactory = ctx.Feature.HttpClientFactory,
                Feature = ctx.Feature,
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
