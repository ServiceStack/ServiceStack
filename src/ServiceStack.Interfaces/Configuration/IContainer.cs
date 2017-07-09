using System;

namespace ServiceStack.Configuration
{
    public interface IContainer
    {
        Func<object> CreateFactory(Type type);

        IContainer AddSingleton(Type type, Func<object> factory);
        
        IContainer AddTransient(Type type, Func<object> factory);

        object Resolve(Type type);
    }
}