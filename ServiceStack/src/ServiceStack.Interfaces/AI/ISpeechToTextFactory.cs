using System;
using System.Collections.Generic;

namespace ServiceStack.AI;

public interface ISpeechToTextFactory
{
    ISpeechToText Get(string name);
}

public class SpeechToTextFactory : ISpeechToTextFactory
{
    public Dictionary<string, ISpeechToText> Providers { get; } = new(StringComparer.OrdinalIgnoreCase);
    
    public ISpeechToText Get(string name)
    {
        if (Providers.TryGetValue(name, out var provider))
            return provider;
        throw new NotSupportedException($"No ISpeechToText provider was registered for '{name}'");
    }
}
