using System.Collections.Generic;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis.Generic
{
	/// <summary>
	/// Wrap the common redis list operations under a IList[string] interface.
	/// </summary>

	public interface IRedisList<T> : IList<T>, IHasStringId
	{
	}
}