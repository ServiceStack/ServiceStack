using ServiceStack.DesignPatterns.Command;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.DataAccess;

namespace ServiceStack.SakilaDb4o.Logic
{
	public interface IAction<ReturnType> : ICommand<ReturnType>
	{
		IPersistenceProvider Provider { get; set; }
		IOperationContext AppContext { get; set; }
	}
}