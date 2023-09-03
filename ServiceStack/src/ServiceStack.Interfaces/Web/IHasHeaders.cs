using System.Collections.Generic;

namespace ServiceStack.Web;

public interface IHasHeaders
{
    Dictionary<string, string> Headers { get; }
}