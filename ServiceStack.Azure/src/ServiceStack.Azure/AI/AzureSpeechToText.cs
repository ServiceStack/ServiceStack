using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.AI;

public class AzureSpeechToText(SpeechConfig config) : ISpeechToText, IRequireVirtualFiles
{
    SpeechConfig Config { get; } = config;
    public IVirtualFiles? VirtualFiles { get; set; }

    public Task InitAsync(InitSpeechToText config, CancellationToken token = default) => Task.CompletedTask;

    public async Task<TranscriptResult> TranscribeAsync(string recordingPath, CancellationToken token = default)
    {
        if (VirtualFiles == null)
            throw new ArgumentNullException(nameof(VirtualFiles));
        
        var ext = recordingPath.LastRightPart('.').ToLower();
        var format = AudioStreamFormat.GetCompressedFormat(ext switch
        {
            "ogg" => AudioStreamContainerFormat.OGG_OPUS,
            "flac" => AudioStreamContainerFormat.FLAC,
            "mp3" => AudioStreamContainerFormat.MP3,
            _ => AudioStreamContainerFormat.ANY 
        });

        var file = VirtualFiles.AssertFile(recordingPath);
#if NET6_0_OR_GREATER
        await using var stream = file.OpenRead();
#else        
        using var stream = file.OpenRead();
#endif
        
        using var audioInput = AudioConfig.FromStreamInput(
            new PullAudioInputStream(new BinaryAudioStreamReader(new BinaryReader(stream)),
            format));

        using var recognizer = new SpeechRecognizer(Config, audioInput);
        var speechResult = await recognizer.RecognizeOnceAsync().ConfigAwait();

        switch (speechResult.Reason)
        {
            case ResultReason.NoMatch:
                return new TranscriptResult {
                    ResponseStatus = new() {
                        ErrorCode = $"{speechResult.Reason}", 
                        Message = "NOMATCH: Speech could not be recognized"
                    }
                };
            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(speechResult);
                return new TranscriptResult {
                    ResponseStatus = cancellation.Reason == CancellationReason.Error
                        ? new() { ErrorCode = $"{cancellation.ErrorCode}", Message = cancellation.ErrorDetails }
                        : new() { ErrorCode = nameof(Exception), Message = $"{cancellation.Reason}" }
                }; 
        }
        
        var json = speechResult.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
        var result = new TranscriptResult
        {
            Transcript = speechResult.Text,
            ApiResponse = json,
        };

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("NBest", out var oNBest) && oNBest.ValueKind == JsonValueKind.Array)
        {
            var best = oNBest.EnumerateArray().FirstOrDefault();
            if (best.ValueKind == JsonValueKind.Object)
            {
                if (best.TryGetProperty("Confidence", out var oConfidence) &&
                    oConfidence.ValueKind == JsonValueKind.Number)
                {
                    result.Confidence = oConfidence.GetSingle();
                }
            }
        }
        return result;
    }
}

/// <summary>
/// Adapter class to the native stream api.
/// </summary>
public sealed class BinaryAudioStreamReader(BinaryReader reader) : PullAudioInputStreamCallback
{
    public BinaryAudioStreamReader(Stream stream)
        : this(new BinaryReader(stream)) {}
    public override int Read(byte[] dataBuffer, uint size)
    {
        return reader.Read(dataBuffer, 0, (int)size);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
            reader.Dispose();

        disposed = true;
        base.Dispose(disposing);
    }
    private bool disposed;
}
