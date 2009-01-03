using ServiceStack.DesignPatterns.Command;
using ServiceStack.LogicFacade;
using ServiceStack.SakilaNHibernate.DataAccess;
using ServiceStack.ServiceInterface;

namespace ServiceStack.SakilaNHibernate.Logic
{
	public interface IAction<ReturnType> : ICommand<ReturnType>
	{
		SakilaNHibernateServiceDataAccessProvider Provider { get; set; }
		IOperationContext AppContext { get; set; }
	}
}