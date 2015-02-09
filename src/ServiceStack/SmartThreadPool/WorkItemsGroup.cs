using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Amib.Threading.Internal
{

	#region WorkItemsGroup class 

	/// <summary>
	/// Summary description for WorkItemsGroup.
	/// </summary>
	public class WorkItemsGroup : WorkItemsGroupBase
	{
		#region Private members

		private readonly object _lock = new object();

		/// <summary>
		/// A reference to the SmartThreadPool instance that created this 
		/// WorkItemsGroup.
		/// </summary>
		private readonly SmartThreadPool _stp;

		/// <summary>
		/// The OnIdle event
		/// </summary>
		private event WorkItemsGroupIdleHandler _onIdle;

        /// <summary>
        /// A flag to indicate if the Work Items Group is now suspended.
        /// </summary>
        private bool _isSuspended;

		/// <summary>
		/// Defines how many work items of this WorkItemsGroup can run at once.
		/// </summary>
		private int _concurrency;

		/// <summary>
		/// Priority queue to hold work items before they are passed 
		/// to the SmartThreadPool.
		/// </summary>
		private readonly PriorityQueue _workItemsQueue;

		/// <summary>
		/// Indicate how many work items are waiting in the SmartThreadPool
		/// queue.
		/// This value is used to apply the concurrency.
		/// </summary>
		private int _workItemsInStpQueue;

		/// <summary>
		/// Indicate how many work items are currently running in the SmartThreadPool.
		/// This value is used with the Cancel, to calculate if we can send new 
		/// work items to the STP.
		/// </summary>
		private int _workItemsExecutingInStp = 0;

		/// <summary>
		/// WorkItemsGroup start information
		/// </summary>
		private readonly WIGStartInfo _workItemsGroupStartInfo;

		/// <summary>
		/// Signaled when all of the WorkItemsGroup's work item completed.
		/// </summary>
        //private readonly ManualResetEvent _isIdleWaitHandle = new ManualResetEvent(true);
        private readonly ManualResetEvent _isIdleWaitHandle = EventWaitHandleFactory.CreateManualResetEvent(true);

		/// <summary>
		/// A common object for all the work items that this work items group
		/// generate so we can mark them to cancel in O(1)
		/// </summary>
		private CanceledWorkItemsGroup _canceledWorkItemsGroup = new CanceledWorkItemsGroup();

		#endregion 

		#region Construction

	    public WorkItemsGroup(
			SmartThreadPool stp, 
			int concurrency, 
			WIGStartInfo wigStartInfo)
		{
			if (concurrency <= 0)
			{
				throw new ArgumentOutOfRangeException(
                    "concurrency",
#if !(_WINDOWS_CE) && !(_SILVERLIGHT) && !(WINDOWS_PHONE)
                    concurrency,
#endif
 "concurrency must be greater than zero");
			}
			_stp = stp;
			_concurrency = concurrency;
			_workItemsGroupStartInfo = new WIGStartInfo(wigStartInfo).AsReadOnly();
			_workItemsQueue = new PriorityQueue();
	        Name = "WorkItemsGroup";

			// The _workItemsInStpQueue gets the number of currently executing work items,
			// because once a work item is executing, it cannot be cancelled.
			_workItemsInStpQueue = _workItemsExecutingInStp;

            _isSuspended = _workItemsGroupStartInfo.StartSuspended;
		}

		#endregion 

        #region WorkItemsGroupBase Overrides

        public override int Concurrency
        {
            get { return _concurrency; }
            set
            {
                Debug.Assert(value > 0);

                int diff = value - _concurrency;
                _concurrency = value;
                if (diff > 0)
                {
                    EnqueueToSTPNextNWorkItem(diff);
                }
            }
        }

        public override int WaitingCallbacks
        {
            get { return _workItemsQueue.Count; }
        }

        public override object[] GetStates()
        {
            lock (_lock)
            {
                object[] states = new object[_workItemsQueue.Count];
                int i = 0;
                foreach (WorkItem workItem in _workItemsQueue)
                {
                    states[i] = workItem.GetWorkItemResult().State;
                    ++i;
                }
                return states;
            }
        }

	    /// <summary>
        /// WorkItemsGroup start information
        /// </summary>
        public override WIGStartInfo WIGStartInfo
        {
            get { return _workItemsGroupStartInfo; }
        }

	    /// <summary>
	    /// Start the Work Items Group if it was started suspended
	    /// </summary>
	    public override void Start()
	    {
	        // If the Work Items Group already started then quit
	        if (!_isSuspended)
	        {
	            return;
	        }
	        _isSuspended = false;
            
	        EnqueueToSTPNextNWorkItem(Math.Min(_workItemsQueue.Count, _concurrency));
	    }

	    public override void Cancel(bool abortExecution)
	    {
	        lock (_lock)
	        {
	            _canceledWorkItemsGroup.IsCanceled = true;
	            _workItemsQueue.Clear();
	            _workItemsInStpQueue = 0;
	            _canceledWorkItemsGroup = new CanceledWorkItemsGroup();
	        }

	        if (abortExecution)
	        {
	            _stp.CancelAbortWorkItemsGroup(this);
	        }
	    }

	    /// <summary>
        /// Wait for the thread pool to be idle
        /// </summary>
        public override bool WaitForIdle(int millisecondsTimeout)
        {
            SmartThreadPool.ValidateWorkItemsGroupWaitForIdle(this);
            return STPEventWaitHandle.WaitOne(_isIdleWaitHandle, millisecondsTimeout, false);
        }

	    public override event WorkItemsGroupIdleHandler OnIdle
		{
			add { _onIdle += value; }
			remove { _onIdle -= value; }
		}

	    #endregion 

		#region Private methods

	    private void RegisterToWorkItemCompletion(IWorkItemResult wir)
		{
			IInternalWorkItemResult iwir = (IInternalWorkItemResult)wir;
			iwir.OnWorkItemStarted += OnWorkItemStartedCallback;
			iwir.OnWorkItemCompleted += OnWorkItemCompletedCallback;
		}

	    public void OnSTPIsStarting()
		{
            if (_isSuspended)
            {
                return;
            }
			
            EnqueueToSTPNextNWorkItem(_concurrency);
		}

	    public void EnqueueToSTPNextNWorkItem(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                EnqueueToSTPNextWorkItem(null, false);
            }
        }

		private object FireOnIdle(object state)
		{
			FireOnIdleImpl(_onIdle);
			return null;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void FireOnIdleImpl(WorkItemsGroupIdleHandler onIdle)
		{
			if(null == onIdle)
			{
				return;
			}

			Delegate[] delegates = onIdle.GetInvocationList();
			foreach(WorkItemsGroupIdleHandler eh in delegates)
			{
				try
				{
					eh(this);
				}
                catch { }  // Suppress exceptions
			}
		}

		private void OnWorkItemStartedCallback(WorkItem workItem)
		{
			lock(_lock)
			{
				++_workItemsExecutingInStp;
			}
		}

		private void OnWorkItemCompletedCallback(WorkItem workItem)
		{
			EnqueueToSTPNextWorkItem(null, true);
		}

        internal override void Enqueue(WorkItem workItem)
        {
            EnqueueToSTPNextWorkItem(workItem);
        }

	    private void EnqueueToSTPNextWorkItem(WorkItem workItem)
		{
			EnqueueToSTPNextWorkItem(workItem, false);
		}

		private void EnqueueToSTPNextWorkItem(WorkItem workItem, bool decrementWorkItemsInStpQueue)
		{
			lock(_lock)
			{
				// Got here from OnWorkItemCompletedCallback()
				if (decrementWorkItemsInStpQueue)
				{
					--_workItemsInStpQueue;

					if(_workItemsInStpQueue < 0)
					{
						_workItemsInStpQueue = 0;
					}

					--_workItemsExecutingInStp;

					if(_workItemsExecutingInStp < 0)
					{
						_workItemsExecutingInStp = 0;
					}
				}

				// If the work item is not null then enqueue it
				if (null != workItem)
				{
					workItem.CanceledWorkItemsGroup = _canceledWorkItemsGroup;

					RegisterToWorkItemCompletion(workItem.GetWorkItemResult());
					_workItemsQueue.Enqueue(workItem);
					//_stp.IncrementWorkItemsCount();

					if ((1 == _workItemsQueue.Count) && 
						(0 == _workItemsInStpQueue))
					{
						_stp.RegisterWorkItemsGroup(this);
                        IsIdle = false;
                        _isIdleWaitHandle.Reset();
					}
				}

				// If the work items queue of the group is empty than quit
				if (0 == _workItemsQueue.Count)
				{
					if (0 == _workItemsInStpQueue)
					{
						_stp.UnregisterWorkItemsGroup(this);
                        IsIdle = true;
                        _isIdleWaitHandle.Set();
                        if (decrementWorkItemsInStpQueue && _onIdle != null && _onIdle.GetInvocationList().Length > 0)
                        {
                            _stp.QueueWorkItem(new WorkItemCallback(FireOnIdle));
                        }
					}
					return;
				}

                if (!_isSuspended)
				{
					if (_workItemsInStpQueue < _concurrency)
					{
						WorkItem nextWorkItem = _workItemsQueue.Dequeue() as WorkItem;
                        try
                        {
                            _stp.Enqueue(nextWorkItem);
                        }
                        catch (ObjectDisposedException e)
                        {
                            e.GetHashCode();
                            // The STP has been shutdown
                        }

						++_workItemsInStpQueue;
					}
				}
			}
		}

		#endregion
    }

	#endregion
}
