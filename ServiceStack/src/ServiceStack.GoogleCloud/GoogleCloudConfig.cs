using Google.Cloud.Speech.V2;

namespace ServiceStack.GoogleCloud;

public class GoogleCloudConfig
{
    public string? Project { get; set; } 
    public string? Location { get; set; }
    public string? Bucket { get; set; }
    public string? PhraseSetId { get; set; }
    public string? RecognizerId { get; set; }
    public string RecognizerModel { get; set; } = "latest_short";
    public string[] RecognizerLanguageCodes { get; set; } = { "en-US", "en-AU" };

    public GoogleCloudConfig ToSpeechToTextConfig(Action<GoogleCloudConfig>? configure = null)
    {
#if NET6_0_OR_GREATER        
        ArgumentNullException.ThrowIfNull(Project, nameof(Project));
        ArgumentNullException.ThrowIfNull(Location, nameof(Location));
        ArgumentNullException.ThrowIfNull(Bucket, nameof(Bucket));
#else
        if (Project == null) throw new ArgumentNullException(nameof(Project));
        if (Location == null) throw new ArgumentNullException(nameof(Location));
        if (Bucket == null) throw new ArgumentNullException(nameof(Bucket));
#endif

        var to = Clone();
        configure?.Invoke(to);
        return to;
    }
    
    public RecognitionConfig ToRecognitionConfig() => new()
    {
        AutoDecodingConfig = new AutoDetectDecodingConfig(),
        LanguageCodes = { RecognizerLanguageCodes },
        Model = RecognizerModel,
    };

    public static void AssertValidCredentials()
    {
        var googleCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (string.IsNullOrEmpty(googleCredentials))
            throw new InvalidOperationException("GOOGLE_APPLICATION_CREDENTIALS Environment Variable not set");
        if (!File.Exists(googleCredentials))
            throw new FileNotFoundException($"GOOGLE_APPLICATION_CREDENTIALS '{googleCredentials}' does not exist", googleCredentials);
    }

    public GoogleCloudConfig Clone() => new()
    {
        Project = Project,
        Location = Location,
        Bucket = Bucket,
        PhraseSetId = PhraseSetId,
        RecognizerId = RecognizerId,
        RecognizerModel = RecognizerModel,
        RecognizerLanguageCodes = RecognizerLanguageCodes,
    };
}
