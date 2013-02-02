using System;

namespace ServiceStack.ServiceHost
{
    /// <summary>
    /// Base interface all webservices need to implement.
    /// For simplicity this is the only interface you need to implement
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Use the New API (ServiceStack.ServiceInterface.Service) for future services. See: https://github.com/ServiceStack/ServiceStack/wiki/New-Api")]
    public interface IService<T>
    {
        object Execute(T request);
    }
}
