#nullable enable
using System;
using System.Collections.Generic;

namespace ServiceStack.AI;

/// <summary>
/// Represents a factory for creating instances of <see cref="ITypeChat"/>.
/// </summary>
public interface ITypeChatFactory
{
    /// <summary>
    /// Gets an instance of <see cref="ITypeChat"/> by name.
    /// </summary>
    /// <param name="name">The name of the TypeChat instance to get.</param>
    /// <returns>An instance of <see cref="ITypeChat"/> with the specified name.</returns>
    ITypeChat Get(string name);
}

public class TypeChatFactory : ITypeChatFactory
{
    public Dictionary<string, ITypeChat> Providers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Func<string, ITypeChat>? Resolve { get; set; }

    public ITypeChat Get(string name)
    {
        if (Providers.TryGetValue(name, out var provider))
            return provider;
        
        return Resolve?.Invoke(name)
               ?? throw new NotSupportedException($"No ITypeChat provider was registered for '{name}'");
    }
}
