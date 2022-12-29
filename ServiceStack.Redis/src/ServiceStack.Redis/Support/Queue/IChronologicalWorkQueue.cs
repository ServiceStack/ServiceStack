using System;
using System.Collections.Generic;

namespace ServiceStack.Redis.Support.Queue
{
	public interface IChronologicalWorkQueue<T> : IDisposable where T : class
	{
		void Enqueue(string workItemId, T workItem, double time);

		IList<KeyValuePair<string, T>> Dequeue(double minTime, double maxTime, int maxBatchSize);
	}
}