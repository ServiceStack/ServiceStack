
namespace ServiceStack.Common.DesignPatterns.Command
{
    public interface ICommand<ReturnType>
    {
        ReturnType Execute();
    }
}