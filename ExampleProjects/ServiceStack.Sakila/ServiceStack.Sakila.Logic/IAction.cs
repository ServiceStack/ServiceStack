using ServiceStack.DesignPatterns.Command;
using ServiceStack.Sakila.DataAccess;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Sakila.Logic
{
	public interface IAction<ReturnType> : ICommand<ReturnType>
	{
		SakilaServiceDataAccessProvider Provider { get; set; }
		AppContext AppContext { get; set; }
	}
}