using System;

namespace ServiceStack;

public interface IContainer
{
    Func<object> CreateFactory(Type type);

    IContainer AddSingleton(Type serviceType, Func<object> factory);
        
    IContainer AddTransient(Type serviceType, Func<object> factory);

    object Resolve(Type type);

    bool Exists(Type type);
}