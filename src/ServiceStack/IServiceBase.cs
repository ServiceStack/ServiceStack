using ServiceStack.Configuration;
using ServiceStack.Web;

namespace ServiceStack
{
    public interface IServiceBase : IResolver
    {
        IResolver GetResolver();

        /// <summary>
        /// Resolve an alternate Web Service from ServiceStack's IOC container.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T ResolveService<T>();

        IRequestContext RequestContext { get; }
    }
}