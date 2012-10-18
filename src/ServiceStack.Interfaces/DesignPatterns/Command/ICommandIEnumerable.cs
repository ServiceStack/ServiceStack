using System.Collections.Generic;

namespace ServiceStack.DesignPatterns.Command
{
    public interface ICommandIEnumerable<T> : ICommand<IEnumerable<T>>
    {
    }
}