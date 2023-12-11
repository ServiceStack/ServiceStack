using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.AI;
using ServiceStack.Text;

namespace ServiceStack;

public static class AiUtils
{
#if !NETFX
    public static async Task<IEnumerable<(string,int)>> GetPhraseWeightsAsync(this IPromptProvider provider, int? defaultWeight = 10, 
        CancellationToken token=default)
    {
        if (provider is IPhraseWeightsProvider weightsProvider)
            return await weightsProvider.GetPhraseWeightsAsync(token).ConfigAwait();
        if (provider is IPhrasesProvider phrasesProvider)
            return (await phrasesProvider.GetPhrasesAsync(token).ConfigAwait()).Map(x => (x, defaultWeight ?? 10));
        return Array.Empty<(string, int)>();
    }
#endif
}
