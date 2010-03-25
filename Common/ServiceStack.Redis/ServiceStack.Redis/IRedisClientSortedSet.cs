using System.Collections.Generic;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis
{
	public interface IRedisClientSortedSet
		: ICollection<string>, IHasStringId
	{
		List<string> GetAll();
		List<string> GetRange(int startingRank, int endingRank);
		List<string> GetRangeByScore(double fromScore, double toScore);
		List<string> GetRangeByScore(double fromScore, double toScore, int? skip, int? take);
		void RemoveRange(int fromRank, int toRank);
		void RemoveRangeByScore(double fromScore, double toScore);
		void StoreFromIntersect(params IRedisClientSortedSet[] ofSets);
		void StoreFromUnion(params IRedisClientSortedSet[] ofSets);
		int GetItemIndex(string value);
		double GetItemScore(string value);
		void IncrementItemScore(string value, double incrementByScore);
		string PopItemWithHighestScore();
		string PopItemWithLowestScore();
	}
}