using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;

namespace @ServiceNamespace@.ServiceInterface.Version100
{
	public abstract class HandlerBase : IService
	{
		public object Execute(IOperationContext context)
		{
			return Execute((@DatabaseName@OperationContext) context);
		}

		public abstract object Execute(@DatabaseName@OperationContext context);
	}
}