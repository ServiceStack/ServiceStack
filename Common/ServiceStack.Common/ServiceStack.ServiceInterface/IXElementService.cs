using ServiceStack.LogicFacade;

namespace ServiceStack.ServiceInterface
{
	public interface IXElementService 
	{
		object Execute(ICallContext context);
	}
}