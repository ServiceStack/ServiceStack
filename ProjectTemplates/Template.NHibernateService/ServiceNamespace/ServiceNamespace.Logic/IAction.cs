using ServiceStack.DesignPatterns.Command;
using ServiceStack.LogicFacade;
using @ServiceNamespace@.DataAccess;
using ServiceStack.ServiceInterface;

namespace @ServiceNamespace@.Logic
{
	public interface IAction<ReturnType> : ICommand<ReturnType>
	{
		@ServiceName@DataAccessProvider Provider { get; set; }
		IOperationContext AppContext { get; set; }
	}
}