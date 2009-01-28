using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface.Tests.Support.Version100.Operations;

namespace ServiceStack.ServiceInterface.Tests.Support.Handlers.Version100
{
	[Port(typeof(StoreCustomer))]
	public class StoreCustomerHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			return context.Request.Get<StoreCustomer>();
		}
	}
}