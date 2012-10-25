namespace ServiceStack.DesignPatterns.Command
{
    public interface ICommand<ReturnType>
    {
        ReturnType Execute();
    }
}