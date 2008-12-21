
using System.Collections.Generic;

namespace ServiceStack.Common.DesignPatterns.Command
{
    public interface ICommandList<T> : ICommand<List<T>>
    {
    }
}