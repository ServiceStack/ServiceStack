#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using ServiceStack.IO;

namespace ServiceStack.AI;

public class AwsSpeechToTextConfig
{
    public string? Bucket { get; set; }
    public string? VocabularyName { get; set; }
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
    public string? LanguageCode { get; set; } = "en-US";
    public string? OutputBucketName { get; set; }

    public Func<string,string>? TranscriptionJobNameFilter { get; set; }
    public Func<string,string>? OutputKeyFilter { get; set; }
    public Func<string,string>? FileUriFilter { get; set; }
    public Action<StartTranscriptionJobRequest>? StartTranscriptionJobFilter { get; set; }
}

public class AwsSpeechToText : ISpeechToText, IRequireVirtualFiles
{
    private AmazonTranscribeServiceClient Client { get; }
    private AwsSpeechToTextConfig Config { get; }
    public IVirtualFiles VirtualFiles { get; set; }

    public AwsSpeechToText(AmazonTranscribeServiceClient client, AwsSpeechToTextConfig config)
    {
        this.Client = client;
        this.Config = config;
    }

    public async Task InitAsync(InitSpeechToText config, CancellationToken token = default)
    {
        if (Config.VocabularyName == null)
            throw new Exception($"{nameof(Config.VocabularyName)} is not set");
        
        var phrases = config.PhraseWeights.Map(x => x.Key);
        var language = Config.LanguageCode != null ? new LanguageCode(Config.LanguageCode) : LanguageCode.EnAU;

        try
        {
            await Client.DeleteVocabularyAsync(new DeleteVocabularyRequest {
                VocabularyName = Config.VocabularyName,
            }, token);
        }
        catch (Exception ignoreNonExistingVocabulary) {}
        
        var response = await Client.CreateVocabularyAsync(new CreateVocabularyRequest {
            LanguageCode = language,
            VocabularyName = Config.VocabularyName,
            Phrases = phrases,
        }, token);

        var vocabularyState = response.VocabularyState;

        while (vocabularyState != VocabularyState.FAILED && vocabularyState != VocabularyState.READY)
        {
            var vocabulary = await Client.GetVocabularyAsync(new GetVocabularyRequest {
                VocabularyName = Config.VocabularyName
            }, token);
            if (vocabulary.VocabularyState == VocabularyState.FAILED)
                throw new Exception(vocabulary.FailureReason);
            
            vocabularyState = vocabulary.VocabularyState;
            await Task.Delay(Config.Delay, token);
        }
    }

    public async Task<TranscriptResult> TranscribeAsync(string recordingPath, CancellationToken token = default)
    {
        var request = new StartTranscriptionJobRequest 
        {
            IdentifyLanguage = Config.LanguageCode == null,
            LanguageCode = Config.LanguageCode != null ? new LanguageCode(Config.LanguageCode) : null,
            TranscriptionJobName = Config.TranscriptionJobNameFilter?.Invoke(recordingPath),
            Media = new() {
                MediaFileUri = Config.FileUriFilter?.Invoke(recordingPath)
            },
            OutputKey = Config.OutputKeyFilter?.Invoke(recordingPath),
            OutputBucketName = Config.OutputBucketName ?? Config.Bucket,
        };
        Config.StartTranscriptionJobFilter?.Invoke(request);
        if (request.TranscriptionJobName == null)
        {
            request.TranscriptionJobName = Guid.NewGuid().ToString("N");
            request.OutputKey = recordingPath.TrimStart('/').WithoutExtension() + ".json";
        }
        
        if (request.Media.MediaFileUri == null && Config.Bucket == null)
            throw new Exception("Bucket is not configured");
        request.Media.MediaFileUri ??= $"https://{Config.Bucket}.s3.amazonaws.com".CombineWith(recordingPath);
        request.MediaFormat = new(recordingPath.LastRightPart('.'));
        
        var jobResponse = await Client.StartTranscriptionJobAsync(request, token);
        var job = jobResponse.TranscriptionJob;
        
        while (job.TranscriptionJobStatus != TranscriptionJobStatus.COMPLETED
               && job.TranscriptionJobStatus != TranscriptionJobStatus.FAILED)
        {
            job = (await Client.GetTranscriptionJobAsync(new GetTranscriptionJobRequest {
                TranscriptionJobName = job.TranscriptionJobName
            }, token)).TranscriptionJob;
            await Task.Delay(Config.Delay, token);
        }

        if (job.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED)
        {
            var url = job.Transcript.TranscriptFileUri;
            MemoryStream? ms = null;
            if (VirtualFiles is S3VirtualFiles)
            {
                var path = url.RightPart(Config.Bucket);
                var file = VirtualFiles.GetFile(path);
                await using var fs = file.OpenRead();
                ms = await fs.CopyToNewMemoryStreamAsync();
            }

            if (ms == null)
            {
                await using var resultStream = await url.GetStreamFromUrlAsync(token:token);
                ms = await resultStream.CopyToNewMemoryStreamAsync();
            }
            ms.Position = 0;
            var obj = await JsonDocument.ParseAsync(ms, cancellationToken:token);

            var results = obj.RootElement
                .GetProperty("results");
            var result = new TranscriptResult
            {
                Transcript = results.GetProperty("transcripts")[0].GetProperty("transcript").ToString(),
            };
            var confidences = results.GetProperty("items").EnumerateArray()
                .Where(x => x.GetProperty("type").ToString() == "pronunciation")
                .Map(x => x.TryGetProperty("alternatives", out var alts)
                    ? (float.TryParse(alts.EnumerateArray().FirstOrDefault().GetProperty("confidence").ToString(), out var f) ? f : (float?)null)
                    : null)
                .Where(x => x != null)
                .ToList();

            result.Confidence = confidences.Count > 0
                ? (float) Math.Round(confidences.Sum().GetValueOrDefault() / confidences.Count, 3)
                : 0;
            
            ms.Position = 0;
            var json = await ms.ReadToEndAsync();
            result.ApiResponse = json;
            return result;
        }
        
        return new TranscriptResult
        {
            ResponseStatus = new()
            {
                ErrorCode = nameof(Exception),
                Message = job.FailureReason,
            }
        };
    }
}