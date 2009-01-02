using ServiceStack.DesignPatterns.Command;
using ServiceStack.LogicFacade;
using ServiceStack.Sakila.DataAccess;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Sakila.Logic
{
	public interface IAction<ReturnType> : ICommand<ReturnType>
	{
		SakilaServiceDataAccessProvider Provider { get; set; }
		IOperationContext AppContext { get; set; }
	}
}