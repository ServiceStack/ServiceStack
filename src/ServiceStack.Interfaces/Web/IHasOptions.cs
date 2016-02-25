using System.Collections.Generic;

namespace ServiceStack.Web
{
    public interface IHasOptions
    {
        IDictionary<string, string> Options { get; }
    }
}