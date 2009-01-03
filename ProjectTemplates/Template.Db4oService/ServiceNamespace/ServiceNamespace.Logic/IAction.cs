using ServiceStack.DesignPatterns.Command;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.DataAccess;

namespace @ServiceNamespace@.Logic
{
	public interface IAction<ReturnType> : ICommand<ReturnType>
	{
		IPersistenceProvider Provider { get; set; }
		IOperationContext AppContext { get; set; }
	}
}