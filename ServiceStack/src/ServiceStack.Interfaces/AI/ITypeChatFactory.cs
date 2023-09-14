using System;
using System.Collections.Generic;

namespace ServiceStack.AI;

public interface ITypeChatFactory
{
    ITypeChat Get(string name);
}

public class TypeChatFactory : ITypeChatFactory
{
    public Dictionary<string, ITypeChat> Providers { get; } = new(StringComparer.OrdinalIgnoreCase);
    
    public ITypeChat Get(string name)
    {
        if (Providers.TryGetValue(name, out var provider))
            return provider;
        throw new NotSupportedException($"No ITypeChat provider was registered for '{name}'");
    }
}
