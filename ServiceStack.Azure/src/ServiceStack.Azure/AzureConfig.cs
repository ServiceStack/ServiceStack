#nullable enable

using System;
using Microsoft.CognitiveServices.Speech;

namespace ServiceStack.Azure;

public class AzureConfig
{
    public string? ConnectionString { get; set; }
    public string? ContainerName { get; set; }
    public string? SpeechKey { get; set; }
    public string? SpeechRegion { get; set; }
    public string? SpeechRecognitionLanguage { get; set; } = "en-US";

    public SpeechConfig ToSpeechConfig(Action<SpeechConfig>? configure=null)
    {
#if NET6_0_OR_GREATER        
        ArgumentNullException.ThrowIfNull(SpeechKey, nameof(SpeechKey));
        ArgumentNullException.ThrowIfNull(SpeechRegion, nameof(SpeechRegion));
        ArgumentNullException.ThrowIfNull(SpeechRecognitionLanguage, nameof(SpeechRecognitionLanguage));
#endif
        
        var to = SpeechConfig.FromSubscription(SpeechKey, SpeechRegion);
        to.SpeechRecognitionLanguage = SpeechRecognitionLanguage;
        
        configure?.Invoke(to);
        
        return to;
    }
}
