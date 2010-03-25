using System.Collections.Generic;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis.Generic
{
	public interface IRedisSortedSet<T> : ICollection<T>, IHasStringId
	{
	}
}