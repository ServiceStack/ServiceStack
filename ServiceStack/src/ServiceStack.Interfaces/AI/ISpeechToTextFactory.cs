#nullable enable
using System;
using System.Collections.Generic;

namespace ServiceStack.AI;

/// <summary>
/// Represents a factory for creating instances of <see cref="ISpeechToText"/>.
/// </summary>
public interface ISpeechToTextFactory
{
    /// <summary>
    /// Gets an instance of <see cref="ISpeechToText"/> by name.
    /// </summary>
    /// <param name="name">The name of the Speech-to-Text provider instance to get.</param>
    /// <returns>An instance of <see cref="ISpeechToText"/> with the specified name.</returns>
    ISpeechToText Get(string name);
}

public class SpeechToTextFactory : ISpeechToTextFactory
{
    public Dictionary<string, ISpeechToText> Providers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Func<string, ISpeechToText>? Resolve { get; set; }
    
    public ISpeechToText Get(string name)
    {
        if (Providers.TryGetValue(name, out var provider))
            return provider;
        
        return Resolve?.Invoke(name)
               ?? throw new NotSupportedException($"No ISpeechToText provider was registered for '{name}'");
    }
}
