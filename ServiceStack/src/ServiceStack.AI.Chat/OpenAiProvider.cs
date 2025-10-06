using Microsoft.Extensions.Logging;

namespace ServiceStack.AI;

public class OpenAiProvider(ILogger log, IHttpClientFactory factory) : OpenAiProviderBase(log, factory)
{
    public static OpenAiProviderBase? Create(ILogger log, IHttpClientFactory factory, Dictionary<string, object?> definition)
    {
        var to = new OpenAiProvider(log, factory);
        to.Populate(definition);
        if (string.IsNullOrEmpty(to.ApiKey))
            return null;
        if (to.Models.Count == 0)
            return null;

        return to;
    }
}