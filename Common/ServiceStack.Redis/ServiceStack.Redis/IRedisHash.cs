using System.Collections.Generic;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis
{
	public interface IRedisHash
		: IDictionary<string, string>, IHasStringId
	{
	}
}