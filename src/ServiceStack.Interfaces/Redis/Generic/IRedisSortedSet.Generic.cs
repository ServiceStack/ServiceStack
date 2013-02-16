//
// https://github.com/mythz/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack.
//
// Licensed under the same terms of Redis and ServiceStack: new BSD license.
//

using System.Collections.Generic;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis.Generic
{
	public interface IRedisSortedSet<T> : ICollection<T>, IHasStringId
	{
		T PopItemWithHighestScore();
		T PopItemWithLowestScore();
		double IncrementItem(T item, double incrementBy);
		int IndexOf(T item);
		int IndexOfDescending(T item);
		List<T> GetAll();
		List<T> GetAllDescending();
		List<T> GetRange(int fromRank, int toRank);
		List<T> GetRangeByLowestScore(double fromScore, double toScore);
		List<T> GetRangeByLowestScore(double fromScore, double toScore, int? skip, int? take);
		List<T> GetRangeByHighestScore(double fromScore, double toScore);
		List<T> GetRangeByHighestScore(double fromScore, double toScore, int? skip, int? take);
		int RemoveRange(int minRank, int maxRank);
		int RemoveRangeByScore(double fromScore, double toScore);
		double GetItemScore(T item);
		int PopulateWithIntersectOf(params IRedisSortedSet<T>[] setIds);
		int PopulateWithUnionOf(params IRedisSortedSet<T>[] setIds);
	}
}