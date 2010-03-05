using System.Collections.Generic;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis
{
	public interface IRedisClientSet
		: ICollection<string>, IHasStringId
	{
		List<string> GetRangeFromSortedSet(int startingFrom, int endingAt);
		HashSet<string> GetAll();
		string Pop();
		void Move(string value, IRedisClientSet toSet);
		HashSet<string> Intersect(params IRedisClientSet[] withSets);
		void StoreIntersect(params IRedisClientSet[] withSets);
		HashSet<string> Union(params IRedisClientSet[] withSets);
		void StoreUnion(params IRedisClientSet[] withSets);
		HashSet<string> Diff(IRedisClientSet[] withSets);
		void StoreDiff(IRedisClientSet fromSet, params IRedisClientSet[] withSets);
		string GetRandomEntry();
	}
}