using System;
using System.Collections.Generic;

namespace ServiceStack.Redis.Support.Queue.Implementation
{
    public class SequentialData<T> : ISequentialData<T> where T : class
    {
        private string dequeueId;
        private IList<T> _dequeueItems;
        private readonly RedisSequentialWorkQueue<T>.DequeueManager _dequeueManager;
        private int processedCount;

        public SequentialData(string dequeueId, IList<T> _dequeueItems, RedisSequentialWorkQueue<T>.DequeueManager _dequeueManager)
        {
            this.dequeueId = dequeueId;
            this._dequeueItems = _dequeueItems;
            this._dequeueManager = _dequeueManager;
        }

        public IList<T> DequeueItems
        {
            get { return _dequeueItems; }
        }

        public string DequeueId
        {
            get { return dequeueId; }
        }

        /// <summary>
        /// pop remaining items that were returned by dequeue, and unlock queue
        /// </summary>
        /// <returns></returns>
        public void PopAndUnlock()
        {
            if (_dequeueItems == null || _dequeueItems.Count <= 0 || processedCount >= _dequeueItems.Count) return;
            _dequeueManager.PopAndUnlock(processedCount);
            processedCount = 0;
            _dequeueItems = null;
        }

        /// <summary>
        /// indicate that an item has been processed by the caller
        /// </summary>
        public void DoneProcessedWorkItem()
        {
            if (processedCount >= _dequeueItems.Count) return;
            _dequeueManager.DoneProcessedWorkItem();
            processedCount++;
        }

        /// <summary>
        /// Update first unprocessed work item
        /// </summary>
        /// <param name="newWorkItem"></param>
        public void UpdateNextUnprocessed(T newWorkItem)
        {
            _dequeueManager.UpdateNextUnprocessed(newWorkItem);
        }

    }
}