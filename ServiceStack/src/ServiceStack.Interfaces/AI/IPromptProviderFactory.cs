#nullable enable
using System;
using System.Collections.Generic;

namespace ServiceStack.AI;

public interface IPromptProviderFactory
{
    IPromptProvider Get(string name);
}

public class PromptProviderFactory : IPromptProviderFactory
{
    public Dictionary<string, IPromptProvider> Providers { get; } = new(StringComparer.OrdinalIgnoreCase);
    
    public IPromptProvider Get(string name)
    {
        if (Providers.TryGetValue(name, out var provider))
            return provider;
        throw new NotSupportedException($"No IPromptProvider was registered for '{name}'");
    }
}
