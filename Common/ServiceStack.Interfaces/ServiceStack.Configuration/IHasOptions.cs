using System.Collections.Generic;

namespace ServiceStack.Configuration
{
	public interface IHasOptions
	{
		IDictionary<string, string> Options { get; }
	}
}