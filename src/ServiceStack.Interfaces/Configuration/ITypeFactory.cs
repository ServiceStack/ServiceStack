using System;

namespace ServiceStack.Configuration
{
    public interface ITypeFactory
    {
        object CreateInstance(IResolver resolver, Type type);
    }
}