using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.AI;

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

public class InitSpeechToText
{
    public IEnumerable<KeyValuePair<string, int>>? PhraseWeights { get; set; }
}

public class TranscriptResult
{
    public string Transcript { get; set; }
    public float Confidence { get; set; }
    public string ApiResponse { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}
