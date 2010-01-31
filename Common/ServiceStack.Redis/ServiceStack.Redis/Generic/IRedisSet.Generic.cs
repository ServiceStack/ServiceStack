using System.Collections.Generic;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis.Generic
{
	public interface IRedisSet<T> : ICollection<T>, IHasStringId
	{
	}
}