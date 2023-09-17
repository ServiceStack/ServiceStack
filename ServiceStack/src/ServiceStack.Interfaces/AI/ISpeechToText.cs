#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.AI;

/// <summary>
/// Abstraction for Speech-to-text Provider
/// </summary>
public interface ISpeechToText
{
    /// <summary>
    /// Once only task to run out-of-band before using the SpeechToText provider
    /// </summary>
    Task InitAsync(InitSpeechToText config, CancellationToken token = default);
    
    /// <summary>
    /// Transcribe the UserRequest and return a JSON API Result
    /// </summary>
    Task<TranscriptResult> TranscribeAsync(string request, CancellationToken token = default);
}

/// <summary>
/// Configuration for initializing a Speech-to-Text provider.
/// </summary>
public class InitSpeechToText
{
    /// <summary>
    /// Gets or sets the phrase weights for the initialization (optional).
    /// </summary>    
    public IEnumerable<KeyValuePair<string, int>>? PhraseWeights { get; set; }
}

/// <summary>
/// Represents the result of a transcription.
/// </summary>
public class TranscriptResult
{
    /// <summary>
    /// The transcribed text.
    /// </summary>
    public string Transcript { get; set; }
    
    /// <summary>
    /// The confidence level of the transcription.
    /// </summary>
    public float Confidence { get; set; }
    
    /// <summary>
    /// The JSON API Response of the Transcription 
    /// </summary>
    public string ApiResponse { get; set; }
    
    /// <summary>
    /// Error Information if transcription was unsuccessful
    /// </summary>
    public ResponseStatus? ResponseStatus { get; set; }
}
