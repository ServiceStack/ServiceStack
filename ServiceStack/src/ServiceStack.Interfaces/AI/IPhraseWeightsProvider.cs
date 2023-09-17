#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.AI;

public interface IPhraseWeightsProvider
{
    /// <summary>
    /// Get Phrases and their Weights to use
    /// </summary>
    Task<IEnumerable<(string,int)>> GetPhraseWeightsAsync(CancellationToken token = default);
}