using System.Collections.Generic;

namespace ServiceStack.Server
{
	public interface IHasOptions
	{
		IDictionary<string, string> Options { get; }
	}
}