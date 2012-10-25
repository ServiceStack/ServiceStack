using System.Collections.Generic;

namespace ServiceStack.ServiceHost
{
	public interface IHasOptions
	{
		IDictionary<string, string> Options { get; }
	}
}