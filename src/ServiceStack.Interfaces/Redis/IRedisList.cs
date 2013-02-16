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

using System;
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
		void Append(string value);
		string RemoveStart();
		string BlockingRemoveStart(TimeSpan? timeOut);
		string RemoveEnd();

		void Enqueue(string value);
		string Dequeue();
		string BlockingDequeue(TimeSpan? timeOut);

		void Push(string value);
		string Pop();
		string BlockingPop(TimeSpan? timeOut);
		string PopAndPush(IRedisList toList);
	}
}