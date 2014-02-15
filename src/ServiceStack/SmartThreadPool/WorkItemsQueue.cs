using System;
using System.Collections.Generic;
using System.Threading;

namespace Amib.Threading.Internal
{
	#region WorkItemsQueue class

	/// <summary>
	/// WorkItemsQueue class.
	/// </summary>
	public class WorkItemsQueue : IDisposable
	{
		#region Member variables

		/// <summary>
		/// Waiters queue (implemented as stack).
		/// </summary>
		private readonly WaiterEntry _headWaiterEntry = new WaiterEntry();

		/// <summary>
		/// Waiters count
		/// </summary>
		private int _waitersCount = 0;

		/// <summary>
		/// Work items queue
		/// </summary>
		private readonly PriorityQueue _workItems = new PriorityQueue();

        /// <summary>
        /// Indicate that work items are allowed to be queued
        /// </summary>
        private bool _isWorkItemsQueueActive = true;


#if (WINDOWS_PHONE) 
        private static readonly Dictionary<int, WaiterEntry> _waiterEntries = new Dictionary<int, WaiterEntry>();
#elif (_WINDOWS_CE)
        private static LocalDataStoreSlot _waiterEntrySlot = Thread.AllocateDataSlot();
#else

        [ThreadStatic]
        private static WaiterEntry _waiterEntry;
#endif


        /// <summary>
        /// Each thread in the thread pool keeps its own waiter entry.
        /// </summary>
        private static WaiterEntry CurrentWaiterEntry
        {
#if (WINDOWS_PHONE) 
            get
            {
                lock (_waiterEntries)
                {
                    WaiterEntry waiterEntry;
                    if (_waiterEntries.TryGetValue(Thread.CurrentThread.ManagedThreadId, out waiterEntry))
                    {
                        return waiterEntry;
                    }
                }
                return null;
            }
            set
            {
                lock (_waiterEntries)
                {
                    _waiterEntries[Thread.CurrentThread.ManagedThreadId] = value;
                }
            }
#elif (_WINDOWS_CE)
            get
            {
                return Thread.GetData(_waiterEntrySlot) as WaiterEntry;
            }
            set
            {
                Thread.SetData(_waiterEntrySlot, value);
            }
#else
            get
            {
                return _waiterEntry;
            }
            set
            {
                _waiterEntry = value;
            }
#endif
        }

        /// <summary>
		/// A flag that indicates if the WorkItemsQueue has been disposed.
		/// </summary>
        private bool _isDisposed = false;

		#endregion

		#region Public properties

        /// <summary>
        /// Returns the current number of work items in the queue
        /// </summary>
		public int Count
		{
			get
			{
                return _workItems.Count;
			}
		}

		/// <summary>
		/// Returns the current number of waiters
		/// </summary>
		public int WaitersCount
		{
			get
			{
				return _waitersCount;
			}
		}


        #endregion

        #region Public methods

		/// <summary>
		/// Enqueue a work item to the queue.
		/// </summary>
		public bool EnqueueWorkItem(WorkItem workItem)
		{
			// A work item cannot be null, since null is used in the
			// WaitForWorkItem() method to indicate timeout or cancel
			if (null == workItem)
			{
				throw new ArgumentNullException("workItem" , "workItem cannot be null");
			}

			bool enqueue = true;

			// First check if there is a waiter waiting for work item. During 
			// the check, timed out waiters are ignored. If there is no 
			// waiter then the work item is queued.
			lock(this)
			{
                ValidateNotDisposed();

                if (!_isWorkItemsQueueActive)
                {
                    return false;
                }

				while(_waitersCount > 0)
				{
					// Dequeue a waiter.
					WaiterEntry waiterEntry = PopWaiter();

					// Signal the waiter. On success break the loop
					if (waiterEntry.Signal(workItem))
					{
						enqueue = false;
						break;
					}
				}

				if (enqueue)
				{
					// Enqueue the work item
					_workItems.Enqueue(workItem);
				}
			}
            return true;
		}


		/// <summary>
		/// Waits for a work item or exits on timeout or cancel
		/// </summary>
		/// <param name="millisecondsTimeout">Timeout in milliseconds</param>
		/// <param name="cancelEvent">Cancel wait handle</param>
		/// <returns>Returns true if the resource was granted</returns>
		public WorkItem DequeueWorkItem(
			int millisecondsTimeout, 
			WaitHandle cancelEvent)
		{
			// This method cause the caller to wait for a work item.
			// If there is at least one waiting work item then the 
			// method returns immidiately with it.
			// 
			// If there are no waiting work items then the caller 
			// is queued between other waiters for a work item to arrive.
			// 
			// If a work item didn't come within millisecondsTimeout or 
			// the user canceled the wait by signaling the cancelEvent 
			// then the method returns null to indicate that the caller 
			// didn't get a work item.

			WaiterEntry waiterEntry;
			WorkItem workItem = null;
		    lock (this)
		    {
                ValidateNotDisposed();

                // If there are waiting work items then take one and return.
                if (_workItems.Count > 0)
                {
                    workItem = _workItems.Dequeue() as WorkItem;
                    return workItem;
                }

                // No waiting work items ...

                // Get the waiter entry for the waiters queue
                waiterEntry = GetThreadWaiterEntry();

                // Put the waiter with the other waiters
                PushWaiter(waiterEntry);
		    }

		    // Prepare array of wait handle for the WaitHandle.WaitAny()
            WaitHandle [] waitHandles = new WaitHandle[] { 
																waiterEntry.WaitHandle, 
																cancelEvent };

			// Wait for an available resource, cancel event, or timeout.

			// During the wait we are supposes to exit the synchronization 
			// domain. (Placing true as the third argument of the WaitAny())
			// It just doesn't work, I don't know why, so I have two lock(this) 
			// statments instead of one.

            int index = STPEventWaitHandle.WaitAny(
				waitHandles,
				millisecondsTimeout, 
				true);

			lock(this)
			{
				// success is true if it got a work item.
				bool success = (0 == index);

				// The timeout variable is used only for readability.
				// (We treat cancel as timeout)
				bool timeout = !success;

				// On timeout update the waiterEntry that it is timed out
				if (timeout)
				{
					// The Timeout() fails if the waiter has already been signaled
					timeout = waiterEntry.Timeout();

					// On timeout remove the waiter from the queue.
					// Note that the complexity is O(1).
					if(timeout)
					{
						RemoveWaiter(waiterEntry, false);
					}

					// Again readability
					success = !timeout;
				}

				// On success return the work item
				if (success)
				{
					workItem = waiterEntry.WorkItem;

					if (null == workItem)
					{
						workItem = _workItems.Dequeue() as WorkItem;
					}
				}
			}
			// On failure return null.
			return workItem;
		}

        /// <summary>
        /// Cleanup the work items queue, hence no more work 
        /// items are allowed to be queue
        /// </summary>
        private void Cleanup()
        {
            lock(this)
            {
                // Deactivate only once
                if (!_isWorkItemsQueueActive)
                {
                    return;
                }

                // Don't queue more work items
                _isWorkItemsQueueActive = false;

                foreach(WorkItem workItem in _workItems)
                {
                    workItem.DisposeOfState();
                }

                // Clear the work items that are already queued
                _workItems.Clear();

                // Note: 
                // I don't iterate over the queue and dispose of work items's states, 
                // since if a work item has a state object that is still in use in the 
                // application then I must not dispose it.

                // Tell the waiters that they were timed out.
                // It won't signal them to exit, but to ignore their
                // next work item.
				while(_waitersCount > 0)
				{
					WaiterEntry waiterEntry = PopWaiter();
					waiterEntry.Timeout();
				}
            }
        }

        public object[] GetStates()
        {
            lock (this)
            {
                object[] states = new object[_workItems.Count];
                int i = 0;
                foreach (WorkItem workItem in _workItems)
                {
                    states[i] = workItem.GetWorkItemResult().State;
                    ++i;
                }
                return states;
            }
        }

		#endregion

		#region Private methods

		/// <summary>
		/// Returns the WaiterEntry of the current thread
		/// </summary>
		/// <returns></returns>
		/// In order to avoid creation and destuction of WaiterEntry
		/// objects each thread has its own WaiterEntry object.
		private static WaiterEntry GetThreadWaiterEntry()
		{
			if (null == CurrentWaiterEntry)
			{
				CurrentWaiterEntry = new WaiterEntry();
			}
			CurrentWaiterEntry.Reset();
			return CurrentWaiterEntry;
		}

		#region Waiters stack methods

		/// <summary>
		/// Push a new waiter into the waiter's stack
		/// </summary>
		/// <param name="newWaiterEntry">A waiter to put in the stack</param>
		public void PushWaiter(WaiterEntry newWaiterEntry)
		{
			// Remove the waiter if it is already in the stack and 
			// update waiter's count as needed
			RemoveWaiter(newWaiterEntry, false);

			// If the stack is empty then newWaiterEntry is the new head of the stack 
			if (null == _headWaiterEntry._nextWaiterEntry)
			{
				_headWaiterEntry._nextWaiterEntry = newWaiterEntry;
				newWaiterEntry._prevWaiterEntry = _headWaiterEntry;

			}
			// If the stack is not empty then put newWaiterEntry as the new head 
			// of the stack.
			else
			{
				// Save the old first waiter entry
				WaiterEntry oldFirstWaiterEntry = _headWaiterEntry._nextWaiterEntry;

                // Update the links
				_headWaiterEntry._nextWaiterEntry = newWaiterEntry;
				newWaiterEntry._nextWaiterEntry = oldFirstWaiterEntry;
				newWaiterEntry._prevWaiterEntry = _headWaiterEntry;
				oldFirstWaiterEntry._prevWaiterEntry = newWaiterEntry;
			}

			// Increment the number of waiters
			++_waitersCount;
		}

		/// <summary>
		/// Pop a waiter from the waiter's stack
		/// </summary>
		/// <returns>Returns the first waiter in the stack</returns>
		private WaiterEntry PopWaiter()
		{
			// Store the current stack head
			WaiterEntry oldFirstWaiterEntry = _headWaiterEntry._nextWaiterEntry;

			// Store the new stack head
			WaiterEntry newHeadWaiterEntry = oldFirstWaiterEntry._nextWaiterEntry;

			// Update the old stack head list links and decrement the number
			// waiters.
			RemoveWaiter(oldFirstWaiterEntry, true);

			// Update the new stack head
			_headWaiterEntry._nextWaiterEntry = newHeadWaiterEntry;
			if (null != newHeadWaiterEntry)
			{
				newHeadWaiterEntry._prevWaiterEntry = _headWaiterEntry;
			}

			// Return the old stack head
			return oldFirstWaiterEntry;
		}

		/// <summary>
		/// Remove a waiter from the stack
		/// </summary>
		/// <param name="waiterEntry">A waiter entry to remove</param>
		/// <param name="popDecrement">If true the waiter count is always decremented</param>
		private void RemoveWaiter(WaiterEntry waiterEntry, bool popDecrement)
		{
			// Store the prev entry in the list
			WaiterEntry prevWaiterEntry = waiterEntry._prevWaiterEntry;

			// Store the next entry in the list
			WaiterEntry nextWaiterEntry = waiterEntry._nextWaiterEntry;

			// A flag to indicate if we need to decrement the waiters count.
			// If we got here from PopWaiter then we must decrement.
			// If we got here from PushWaiter then we decrement only if
			// the waiter was already in the stack.
			bool decrementCounter = popDecrement;

			// Null the waiter's entry links
			waiterEntry._prevWaiterEntry = null;
			waiterEntry._nextWaiterEntry = null;

			// If the waiter entry had a prev link then update it.
			// It also means that the waiter is already in the list and we
			// need to decrement the waiters count.
			if (null != prevWaiterEntry)
			{
				prevWaiterEntry._nextWaiterEntry = nextWaiterEntry;
				decrementCounter = true;
			}

			// If the waiter entry had a next link then update it.
			// It also means that the waiter is already in the list and we
			// need to decrement the waiters count.
			if (null != nextWaiterEntry)
			{
				nextWaiterEntry._prevWaiterEntry = prevWaiterEntry;
				decrementCounter = true;
			}

			// Decrement the waiters count if needed
			if (decrementCounter)
			{
				--_waitersCount;
			}
		}

		#endregion

		#endregion

		#region WaiterEntry class 

		// A waiter entry in the _waiters queue.
		public sealed class WaiterEntry : IDisposable
		{
			#region Member variables

			/// <summary>
			/// Event to signal the waiter that it got the work item.
			/// </summary>
			//private AutoResetEvent _waitHandle = new AutoResetEvent(false);
            private AutoResetEvent _waitHandle = EventWaitHandleFactory.CreateAutoResetEvent();

			/// <summary>
			/// Flag to know if this waiter already quited from the queue 
			/// because of a timeout.
			/// </summary>
			private bool _isTimedout = false;

			/// <summary>
			/// Flag to know if the waiter was signaled and got a work item. 
			/// </summary>
			private bool _isSignaled = false;

            /// <summary>
            /// A work item that passed directly to the waiter withou going 
            /// through the queue
            /// </summary>
			private WorkItem _workItem = null;

            private bool _isDisposed = false;

			// Linked list members
			internal WaiterEntry _nextWaiterEntry = null;
			internal WaiterEntry _prevWaiterEntry = null;

			#endregion

			#region Construction

			public WaiterEntry()
			{
				Reset();
			}
						
			#endregion

			#region Public methods

			public WaitHandle WaitHandle
			{
				get { return _waitHandle; }
			}

			public WorkItem WorkItem
			{
				get
				{
					return _workItem;
				}
			}

			/// <summary>
			/// Signal the waiter that it got a work item.
			/// </summary>
			/// <returns>Return true on success</returns>
			/// The method fails if Timeout() preceded its call
			public bool Signal(WorkItem workItem)
			{
				lock(this)
				{
					if (!_isTimedout)
					{
						_workItem = workItem;
						_isSignaled = true;
						_waitHandle.Set();
						return true;
					}
				}
				return false;
			}

			/// <summary>
			/// Mark the wait entry that it has been timed out
			/// </summary>
			/// <returns>Return true on success</returns>
			/// The method fails if Signal() preceded its call
			public bool Timeout()
			{
				lock(this)
				{
					// Time out can happen only if the waiter wasn't marked as
					// signaled
					if (!_isSignaled)
					{
						// We don't remove the waiter from the queue, the DequeueWorkItem 
                        // method skips _waiters that were timed out.
						_isTimedout = true;
						return true;
					}
				}
				return false;
			}

			/// <summary>
			/// Reset the wait entry so it can be used again
			/// </summary>
			public void Reset()
			{
				_workItem = null;
				_isTimedout = false;
				_isSignaled = false;
				_waitHandle.Reset();
			}

			/// <summary>
			/// Free resources
			/// </summary>
			public void Close()
			{
				if (null != _waitHandle)
				{
					_waitHandle.Close();
					_waitHandle = null;
				}
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
                lock (this)
                {
                    if (!_isDisposed)
                    {
                        Close();
                    }
                    _isDisposed = true;
                }
            }

			#endregion
		}

		#endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Cleanup();
            }
            _isDisposed = true;
        }

        private void ValidateNotDisposed()
        {
            if(_isDisposed)
            {
                throw new ObjectDisposedException(GetType().ToString(), "The SmartThreadPool has been shutdown");
            }
        }

        #endregion
    }

	#endregion
}

