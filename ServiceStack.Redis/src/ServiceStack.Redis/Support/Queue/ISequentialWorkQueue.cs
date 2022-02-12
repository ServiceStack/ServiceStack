using System;
using System.Collections.Generic;
using ServiceStack.Redis.Support.Queue.Implementation;

namespace ServiceStack.Redis.Support.Queue
{
	public interface ISequentialWorkQueue<T> : IDisposable where T : class
	{

        /// <summary>
        /// Enqueue item in priority queue corresponding to workItemId identifier
        /// </summary>
        /// <param name="workItemId"></param>
        /// <param name="workItem"></param>
		void Enqueue(string workItemId, T workItem);

        /// <summary>
        /// Prepare next work item id for dequeuing
        /// </summary>
        bool PrepareNextWorkItem();


        /// <summary>
        /// Dequeue up to maxBatchSize items from queue corresponding to workItemId identifier.
        /// Once this method is called, <see cref="Dequeue"/> will not
        /// return any items for workItemId until the dequeue lock returned is unlocked.
        /// </summary>
        /// <param name="maxBatchSize"></param>
        /// <returns></returns>
        ISequentialData<T> Dequeue(int maxBatchSize);


        /// <summary>
        /// Replace existing work item in workItemId queue
        /// </summary>
        /// <param name="workItemId"></param>
        /// <param name="index"></param>
        /// <param name="newWorkItem"></param>
	    void Update(string workItemId, int index, T newWorkItem);

	    bool HarvestZombies();
    }
}