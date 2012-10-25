using System.Collections.Generic;

namespace ServiceStack.DesignPatterns.Command
{
    public interface ICommandList<T> : ICommand<List<T>>
    {
    }
}