
using System.Collections.Generic;

namespace ServiceStack.Common.DesignPatterns.Command
{
    public interface ICommandIEnumerable<T> : ICommand<IEnumerable<T>>
    {
    }
}