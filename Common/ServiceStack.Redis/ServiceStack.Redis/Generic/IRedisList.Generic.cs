using System.Collections.Generic;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis.Generic
{
	/// <summary>
	/// Wrap the common redis list operations under a IList[string] interface.
	/// </summary>

	public interface IRedisList<T> 
		: IList<T>, IHasStringId
	{
		List<T> GetAll();
		List<T> GetRange(int startingFrom, int endingAt);
		List<T> GetRangeFromSortedList(int startingFrom, int endingAt);
		void RemoveAll();
		void Trim(int keepStartingFrom, int keepEndingAt);
		int RemoveValue(T value);
		int RemoveValue(T value, int noOfMatches);

		void Append(T value);
		void Prepend(T value);

		T Pop();
		T Dequeue();

		T PopAndPush(IRedisList<T> toList);
	}
}