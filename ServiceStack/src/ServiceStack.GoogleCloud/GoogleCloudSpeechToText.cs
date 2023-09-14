using Google.Cloud.Speech.V2;
using ServiceStack.AI;

namespace ServiceStack.GoogleCloud;

public class GoogleCloudSpeechConfig
{
    public string Project { get; set; } 
    public string Location { get; set; }
    public string Bucket { get; set; }
    public string PhraseSetId { get; set; }
    public string RecognizerId { get; set; }
}

public class GoogleCloudSpeechToText : ISpeechToText
{
    GoogleCloudSpeechConfig Config { get; }
    SpeechClient SpeechClient { get; }

    public GoogleCloudSpeechToText(GoogleCloudSpeechConfig config, SpeechClient speechClient)
    {
        Config = config;
        SpeechClient = speechClient;
    }

    public async Task InitAsync(InitSpeechToText config, CancellationToken token = default)
    {
        if (config.PhraseWeights != null)
        {
            try
            {
                await SpeechClient.DeletePhraseSetAsync(new DeletePhraseSetRequest
                {
                    PhraseSetName = new PhraseSetName(Config.Project, Config.Location, Config.PhraseSetId)
                });
            }
            catch (Exception ignoreNonExistingPhraseSet) {}

            await SpeechClient.CreatePhraseSetAsync(new CreatePhraseSetRequest
            {
                Parent = $"projects/{Config.Project}/locations/{Config.Location}",
                PhraseSetId = Config.PhraseSetId,
                PhraseSet = new PhraseSet
                {
                    Phrases =
                    {
                        config.PhraseWeights.Map(x => new PhraseSet.Types.Phrase { Value = x.Key, Boost = x.Value })
                    }
                }
            });        
        }
        
        try
        {
            await SpeechClient.DeleteRecognizerAsync(new DeleteRecognizerRequest
            {
                RecognizerName = new RecognizerName(Config.Project, Config.Location, Config.RecognizerId)
            });
        }
        catch (Exception ignoreNonExistingRecognizer) {}

        await SpeechClient.CreateRecognizerAsync(new CreateRecognizerRequest
        {
            Parent = $"projects/{Config.Project}/locations/{Config.Location}",
            RecognizerId = Config.RecognizerId,
            Recognizer = new Recognizer
            {
                DefaultRecognitionConfig = new RecognitionConfig
                {
                    AutoDecodingConfig = new AutoDetectDecodingConfig(),
                    LanguageCodes = { "en-US", "en-AU" },
                    Model = "latest_short",
                    Adaptation = new SpeechAdaptation
                    {
                        PhraseSets =
                        {
                            new SpeechAdaptation.Types.AdaptationPhraseSet
                            {
                                PhraseSet = $"projects/{Config.Project}/locations/{Config.Location}/phraseSets/{Config.PhraseSetId}"
                            }
                        }
                    }
                },
            },
        });
    }

    public async Task<TranscriptResult> TranscribeAsync(string recordingPath, CancellationToken token = default)
    {
        var response = await SpeechClient.RecognizeAsync(new RecognizeRequest
        {
            Recognizer = $"projects/{Config.Project}/locations/{Config.Location}/recognizers/{Config.RecognizerId}",
            Uri = $"gs://{Config.Bucket}".CombineWith(recordingPath)
        });

        var alt = response.Results.Count > 0 && response.Results[0].Alternatives.Count > 0
            ? response.Results[0].Alternatives[0]
            : null;
        if (alt == null)
        {
            return new TranscriptResult
            {
                ResponseStatus = new() { ErrorCode = nameof(Exception), Message = $"{recordingPath} returned no results" },
                ApiResponse = response.ToJson(),
            };
        }
        
        var result = new TranscriptResult
        {
            Transcript = alt.Transcript,
            Confidence = alt.Confidence,
            ApiResponse = response.ToJson(),
        };
        return result;
    }
}
