using System;
using System.Collections.Generic;


namespace ServiceStack.Redis.Support.Queue
{
	public interface ISimpleWorkQueue<T> : IDisposable where T : class
	{
        /// <summary>
        /// Enqueue item
        /// </summary>
        /// <param name="workItem"></param>
		void Enqueue(T workItem);

        /// <summary>
        /// Dequeue up to maxBatchSize items from queue
        /// </summary>
        /// <param name="maxBatchSize"></param>
        /// <returns></returns>
		IList<T> Dequeue(int maxBatchSize);
	}
}