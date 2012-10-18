using System.Collections.Generic;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis
{
	public interface IRedisHash
		: IDictionary<string, string>, IHasStringId
	{
		bool AddIfNotExists(KeyValuePair<string, string> item);
		void AddRange(IEnumerable<KeyValuePair<string, string>> items);
		int IncrementValue(string key, int incrementBy);
	}
}