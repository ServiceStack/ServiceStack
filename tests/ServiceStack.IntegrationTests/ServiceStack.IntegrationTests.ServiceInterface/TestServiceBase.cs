using ServiceStack.ServiceHost;

namespace ServiceStack.IntegrationTests.ServiceInterface
{
	public abstract class TestServiceBase<TRequest>
		: IService<TRequest>
	{
		protected abstract object Run(TRequest request);

		public object Execute(TRequest request)
		{
			return Run(request);
		}
	}
}