using System.Diagnostics;
using ServiceStack.Text;

namespace ServiceStack.AI;

public class WhisperLocalSpeechToText : ISpeechToText
{
    private const string WhisperArgs = "--model small --fp16 False --output_format json --language English";
    public string? WhisperPath { get; set; }
    public string? WorkingDirectory { get; set; }
    public int TimeoutMs { get; set; } = 120 * 1000;
    public Func<ProcessStartInfo, ProcessStartInfo>? ProcessFilter { get; set; }

    public Task InitAsync(InitSpeechToText config, CancellationToken token = default) => Task.CompletedTask;

    public async Task<TranscriptResult> TranscribeAsync(string recordingPath, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(recordingPath))
            throw new ArgumentNullException(nameof(recordingPath));

        var relativePath = recordingPath.TrimStart('/');
        var fileName = recordingPath.LastRightPart('/');
        var whisperPath = WhisperPath ?? ProcessUtils.FindExePath("whisper");
        if (whisperPath == null)
            throw new NotSupportedException("whisper is not in $PATH");
                
        var processInfo = new ProcessStartInfo
        {
            WorkingDirectory = WorkingDirectory ?? Environment.CurrentDirectory.CombineWith(relativePath.LastLeftPart('/')),
            FileName = whisperPath,
            Arguments = $"{WhisperArgs} {fileName}",
        };
        processInfo = ProcessFilter?.Invoke(processInfo) ?? processInfo;

        var sb = StringBuilderCache.Allocate();
        var sbError = StringBuilderCacheAlt.Allocate();
        string stdout;
        string stderr;
        try
        {
            await ProcessUtils.RunAsync(processInfo, TimeoutMs,
                onOut: data => sb.AppendLine(data),
                onError: data => sbError.AppendLine(data)).ConfigAwait();
        }
        finally
        {
            stdout = StringBuilderCache.ReturnAndFree(sb);
            stderr = StringBuilderCacheAlt.ReturnAndFree(sbError);
        }
        
        string? text = null;
        string? json = null;

        var baseFileName = Path.GetFileNameWithoutExtension(fileName);
        var jsonFile = processInfo.WorkingDirectory.CombineWith(baseFileName + ".json");
        if (File.Exists(jsonFile))
        {
#if NET6_0_OR_GREATER
            json = await File.ReadAllTextAsync(jsonFile, token).ConfigAwait();
#else
            json = File.ReadAllText(jsonFile);
#endif            
            if (JSON.parse(json) is Dictionary<string, object> obj &&
                obj.TryGetValue("text", out var oText))
            {
                text = oText as string;
            }
        }

        if (text == null)
        {
            throw new Exception($"Failed to whisper transcribe {recordingPath}: {stderr}\n{stdout}");
        }

        var result = new TranscriptResult
        {
            Transcript = text,
            ApiResponse = json ?? string.Empty,
        };
        return result;
    }
}
