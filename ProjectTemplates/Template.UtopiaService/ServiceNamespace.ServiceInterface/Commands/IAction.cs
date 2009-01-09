using ServiceStack.DesignPatterns.Command;

namespace @ServiceNamespace@.ServiceInterface.Commands
{
	public interface IAction<ReturnType> : ICommand<ReturnType>
	{
		@DatabaseName@OperationContext Context { get; set; }
	}
}