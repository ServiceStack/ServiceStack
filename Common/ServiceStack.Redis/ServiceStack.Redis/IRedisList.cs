using System.Collections.Generic;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis
{
	public interface IRedisList
		: IList<string>, IHasStringId
	{
		List<string> GetAll();
		List<string> GetRange(int startingFrom, int endingAt);
		List<string> GetRangeFromSortedList(int startingFrom, int endingAt);
		void RemoveAll();
		void Trim(int keepStartingFrom, int keepEndingAt);
		int RemoveValue(string value);
		int RemoveValue(string value, int noOfMatches);

		void Prepend(string value);
		string Dequeue();
		string Pop();

		string PopAndPush(IRedisList toList);
	}
}