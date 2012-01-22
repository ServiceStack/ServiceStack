using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
	public interface IServiceBase : IResolver
	{
		IAppHost AppHost { get; set; }

		/// <summary>
		/// Resolve an alternate Web Service from ServiceStack's IOC container.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		T ResolveService<T>();

		IRequestContext RequestContext { get; }
	}
}