using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface.Tests.Support.Version100.Operations;

namespace ServiceStack.ServiceInterface.Tests.Support.Handlers.Version100
{
	[Port(typeof(GetCustomer))]
	public class GetCustomerHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			return context.Request.Get<GetCustomer>();
		}
	}
}