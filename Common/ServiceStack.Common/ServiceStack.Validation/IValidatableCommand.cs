using ServiceStack.DesignPatterns.Command;

namespace ServiceStack.Validation
{
	public interface IValidatableCommand<ReturnType> : IValidatable, ICommand<ReturnType>
	{
	}
}