#region Release History

// Smart Thread Pool
// 7 Aug 2004 - Initial release
//
// 14 Sep 2004 - Bug fixes 
//
// 15 Oct 2004 - Added new features
//		- Work items return result.
//		- Support waiting synchronization for multiple work items.
//		- Work items can be cancelled.
//		- Passage of the caller thread’s context to the thread in the pool.
//		- Minimal usage of WIN32 handles.
//		- Minor bug fixes.
//
// 26 Dec 2004 - Changes:
//		- Removed static constructors.
//      - Added finalizers.
//		- Changed Exceptions so they are serializable.
//		- Fixed the bug in one of the SmartThreadPool constructors.
//		- Changed the SmartThreadPool.WaitAll() so it will support any number of waiters. 
//        The SmartThreadPool.WaitAny() is still limited by the .NET Framework.
//		- Added PostExecute with options on which cases to call it.
//      - Added option to dispose of the state objects.
//      - Added a WaitForIdle() method that waits until the work items queue is empty.
//      - Added an STPStartInfo class for the initialization of the thread pool.
//      - Changed exception handling so if a work item throws an exception it 
//        is rethrown at GetResult(), rather then firing an UnhandledException event.
//        Note that PostExecute exception are always ignored.
//
// 25 Mar 2005 - Changes:
//		- Fixed lost of work items bug
//
// 3 Jul 2005: Changes.
//      - Fixed bug where Enqueue() throws an exception because PopWaiter() returned null, hardly reconstructed. 
//
// 16 Aug 2005: Changes.
//		- Fixed bug where the InUseThreads becomes negative when canceling work items. 
//
// 31 Jan 2006 - Changes:
//		- Added work items priority
//		- Removed support of chained delegates in callbacks and post executes (nobody really use this)
//		- Added work items groups
//		- Added work items groups idle event
//		- Changed SmartThreadPool.WaitAll() behavior so when it gets empty array
//		  it returns true rather then throwing an exception.
//		- Added option to start the STP and the WIG as suspended
//		- Exception behavior changed, the real exception is returned by an 
//		  inner exception
//		- Added option to keep the Http context of the caller thread. (Thanks to Steven T.)
//		- Added performance counters
//		- Added priority to the threads in the pool
//
// 13 Feb 2006 - Changes:
//		- Added a call to the dispose of the Performance Counter so
//		  their won't be a Performance Counter leak.
//		- Added exception catch in case the Performance Counters cannot 
//		  be created.
//
// 17 May 2008 - Changes:
//      - Changed the dispose behavior and removed the Finalizers.
//      - Enabled the change of the MaxThreads and MinThreads at run time.
//      - Enabled the change of the Concurrency of a IWorkItemsGroup at run 
//        time If the IWorkItemsGroup is a SmartThreadPool then the Concurrency 
//        refers to the MaxThreads. 
//      - Improved the cancel behavior.
//      - Added events for thread creation and termination. 
//      - Fixed the HttpContext context capture.
//      - Changed internal collections so they use generic collections
//      - Added IsIdle flag to the SmartThreadPool and IWorkItemsGroup
//      - Added support for WinCE
//      - Added support for Action<T> and Func<T>
//
// 07 April 2009 - Changes:
//      - Added support for Silverlight and Mono
//      - Added Join, Choice, and Pipe to SmartThreadPool.
//      - Added local performance counters (for Mono, Silverlight, and WindowsCE)
//      - Changed duration measures from DateTime.Now to Stopwatch.
//      - Queues changed from System.Collections.Queue to System.Collections.Generic.LinkedList<T>.
//
// 21 December 2009 - Changes:
//      - Added work item timeout (passive)
//
// 20 August 2012 - Changes:
//      - Added set name to threads
//      - Fixed the WorkItemsQueue.Dequeue. 
//        Replaced while (!Monitor.TryEnter(this)); with lock(this) { ... }
//      - Fixed SmartThreadPool.Pipe
//      - Added IsBackground option to threads
//      - Added ApartmentState to threads
//      - Fixed thread creation when queuing many work items at the same time.
//
// 24 August 2012 - Changes:
//      - Enabled cancel abort after cancel. See: http://smartthreadpool.codeplex.com/discussions/345937 by alecswan
//      - Added option to set MaxStackSize of threads 

#endregion

using System;
using System.Security;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Amib.Threading.Internal;

namespace Amib.Threading
{
	#region SmartThreadPool class
	/// <summary>
	/// Smart thread pool class.
	/// </summary>
	public partial class SmartThreadPool : WorkItemsGroupBase, IDisposable
	{
		#region Public Default Constants

		/// <summary>
		/// Default minimum number of threads the thread pool contains. (0)
		/// </summary>
		public const int DefaultMinWorkerThreads = 0;

		/// <summary>
		/// Default maximum number of threads the thread pool contains. (25)
		/// </summary>
		public const int DefaultMaxWorkerThreads = 25;

		/// <summary>
		/// Default idle timeout in milliseconds. (One minute)
		/// </summary>
		public const int DefaultIdleTimeout = 60*1000; // One minute

		/// <summary>
		/// Indicate to copy the security context of the caller and then use it in the call. (false)
		/// </summary>
		public const bool DefaultUseCallerCallContext = false; 

		/// <summary>
		/// Indicate to copy the HTTP context of the caller and then use it in the call. (false)
		/// </summary>
		public const bool DefaultUseCallerHttpContext = false;

		/// <summary>
		/// Indicate to dispose of the state objects if they support the IDispose interface. (false)
		/// </summary>
		public const bool DefaultDisposeOfStateObjects = false; 

		/// <summary>
        /// The default option to run the post execute (CallToPostExecute.Always)
		/// </summary>
		public const CallToPostExecute DefaultCallToPostExecute = CallToPostExecute.Always;

		/// <summary>
		/// The default post execute method to run. (None)
		/// When null it means not to call it.
		/// </summary>
		public static readonly PostExecuteWorkItemCallback DefaultPostExecuteWorkItemCallback;

		/// <summary>
        /// The default work item priority (WorkItemPriority.Normal)
		/// </summary>
		public const WorkItemPriority DefaultWorkItemPriority = WorkItemPriority.Normal;

		/// <summary>
		/// The default is to work on work items as soon as they arrive
		/// and not to wait for the start. (false)
		/// </summary>
		public const bool DefaultStartSuspended = false;

		/// <summary>
        /// The default name to use for the performance counters instance. (null)
		/// </summary>
		public static readonly string DefaultPerformanceCounterInstanceName;

#if !(WINDOWS_PHONE)

		/// <summary>
        /// The default thread priority (ThreadPriority.Normal)
		/// </summary>
		public const ThreadPriority DefaultThreadPriority = ThreadPriority.Normal;
#endif
        /// <summary>
        /// The default thread pool name. (SmartThreadPool)
        /// </summary>
        public const string DefaultThreadPoolName = "SmartThreadPool";

        /// <summary>
        /// The default Max Stack Size. (SmartThreadPool)
        /// </summary>
        public static readonly int? DefaultMaxStackSize = null;

        /// <summary>
        /// The default fill state with params. (false)
        /// It is relevant only to QueueWorkItem of Action&lt;...&gt;/Func&lt;...&gt;
        /// </summary>
        public const bool DefaultFillStateWithArgs = false;

        /// <summary>
        /// The default thread backgroundness. (true)
        /// </summary>
        public const bool DefaultAreThreadsBackground = true;

#if !(_SILVERLIGHT) && !(WINDOWS_PHONE)
        /// <summary>
        /// The default apartment state of a thread in the thread pool. 
        /// The default is ApartmentState.Unknown which means the STP will not 
        /// set the apartment of the thread. It will use the .NET default.
        /// </summary>
        public const ApartmentState DefaultApartmentState = ApartmentState.Unknown;
#endif

		#endregion

        #region Member Variables

		/// <summary>
		/// Dictionary of all the threads in the thread pool.
		/// </summary>
        private readonly SynchronizedDictionary<Thread, ThreadEntry> _workerThreads = new SynchronizedDictionary<Thread, ThreadEntry>();

		/// <summary>
		/// Queue of work items.
		/// </summary>
		private readonly WorkItemsQueue _workItemsQueue = new WorkItemsQueue();

		/// <summary>
		/// Count the work items handled.
		/// Used by the performance counter.
		/// </summary>
		private int _workItemsProcessed;

		/// <summary>
		/// Number of threads that currently work (not idle).
		/// </summary>
		private int _inUseWorkerThreads;

        /// <summary>
        /// Stores a copy of the original STPStartInfo.
        /// It is used to change the MinThread and MaxThreads
        /// </summary>
        private STPStartInfo _stpStartInfo;

		/// <summary>
		/// Total number of work items that are stored in the work items queue 
		/// plus the work items that the threads in the pool are working on.
		/// </summary>
		private int _currentWorkItemsCount;

		/// <summary>
		/// Signaled when the thread pool is idle, i.e. no thread is busy
		/// and the work items queue is empty
		/// </summary>
		//private ManualResetEvent _isIdleWaitHandle = new ManualResetEvent(true);
		private ManualResetEvent _isIdleWaitHandle = EventWaitHandleFactory.CreateManualResetEvent(true);

		/// <summary>
		/// An event to signal all the threads to quit immediately.
		/// </summary>
		//private ManualResetEvent _shuttingDownEvent = new ManualResetEvent(false);
		private ManualResetEvent _shuttingDownEvent = EventWaitHandleFactory.CreateManualResetEvent(false);

        /// <summary>
        /// A flag to indicate if the Smart Thread Pool is now suspended.
        /// </summary>
        private bool _isSuspended;

		/// <summary>
		/// A flag to indicate the threads to quit.
		/// </summary>
		private bool _shutdown;

		/// <summary>
		/// Counts the threads created in the pool.
		/// It is used to name the threads.
		/// </summary>
		private int _threadCounter;

		/// <summary>
		/// Indicate that the SmartThreadPool has been disposed
		/// </summary>
		private bool _isDisposed;

		/// <summary>
		/// Holds all the WorkItemsGroup instaces that have at least one 
		/// work item int the SmartThreadPool
		/// This variable is used in case of Shutdown
		/// </summary>
        private readonly SynchronizedDictionary<IWorkItemsGroup, IWorkItemsGroup> _workItemsGroups = new SynchronizedDictionary<IWorkItemsGroup, IWorkItemsGroup>();

        /// <summary>
        /// A common object for all the work items int the STP
        /// so we can mark them to cancel in O(1)
        /// </summary>
        private CanceledWorkItemsGroup _canceledSmartThreadPool = new CanceledWorkItemsGroup();

        /// <summary>
        /// Windows STP performance counters
        /// </summary>
        private ISTPInstancePerformanceCounters _windowsPCs = NullSTPInstancePerformanceCounters.Instance;

        /// <summary>
        /// Local STP performance counters
        /// </summary>
        private ISTPInstancePerformanceCounters _localPCs = NullSTPInstancePerformanceCounters.Instance;


#if (WINDOWS_PHONE) 
        private static readonly Dictionary<int, ThreadEntry> _threadEntries = new Dictionary<int, ThreadEntry>();
#elif (_WINDOWS_CE)
        private static LocalDataStoreSlot _threadEntrySlot = Thread.AllocateDataSlot();
#else
        [ThreadStatic]
        private static ThreadEntry _threadEntry;

#endif

        /// <summary>
        /// An event to call after a thread is created, but before 
        /// it's first use.
        /// </summary>
        private event ThreadInitializationHandler _onThreadInitialization;

        /// <summary>
        /// An event to call when a thread is about to exit, after 
        /// it is no longer belong to the pool.
        /// </summary>
        private event ThreadTerminationHandler _onThreadTermination;

        #endregion

        #region Per thread properties

        /// <summary>
        /// A reference to the current work item a thread from the thread pool 
        /// is executing.
        /// </summary>
        internal static ThreadEntry CurrentThreadEntry
        {
#if (WINDOWS_PHONE)
            get
            {
                lock(_threadEntries)
                {
                    ThreadEntry threadEntry;
                    if (_threadEntries.TryGetValue(Thread.CurrentThread.ManagedThreadId, out threadEntry))
                    {
                        return threadEntry;
                    }
                }
                return null;
            }
            set
            {
                lock(_threadEntries)
                {
                    _threadEntries[Thread.CurrentThread.ManagedThreadId] = value;
                }
            }
#elif (_WINDOWS_CE)
            get
            {
                //Thread.CurrentThread.ManagedThreadId
                return Thread.GetData(_threadEntrySlot) as ThreadEntry;
            }
            set
            {
                Thread.SetData(_threadEntrySlot, value);
            }
#else
            get
            {
                return _threadEntry;
            }
            set
            {
                _threadEntry = value;
            }
#endif
        }
        #endregion

        #region Construction and Finalization

        /// <summary>
		/// Constructor
		/// </summary>
		public SmartThreadPool()
		{
            _stpStartInfo = new STPStartInfo();
            Initialize();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="idleTimeout">Idle timeout in milliseconds</param>
		public SmartThreadPool(int idleTimeout)
		{
            _stpStartInfo = new STPStartInfo
            {
                IdleTimeout = idleTimeout,
            };
			Initialize();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="idleTimeout">Idle timeout in milliseconds</param>
		/// <param name="maxWorkerThreads">Upper limit of threads in the pool</param>
		public SmartThreadPool(
			int idleTimeout,
			int maxWorkerThreads)
		{
            _stpStartInfo = new STPStartInfo
            {
                IdleTimeout = idleTimeout,
                MaxWorkerThreads = maxWorkerThreads,
            };
			Initialize();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="idleTimeout">Idle timeout in milliseconds</param>
		/// <param name="maxWorkerThreads">Upper limit of threads in the pool</param>
		/// <param name="minWorkerThreads">Lower limit of threads in the pool</param>
		public SmartThreadPool(
			int idleTimeout,
			int maxWorkerThreads,
			int minWorkerThreads)
		{
            _stpStartInfo = new STPStartInfo
            {
                IdleTimeout = idleTimeout,
                MaxWorkerThreads = maxWorkerThreads,
                MinWorkerThreads = minWorkerThreads,
            };
			Initialize();
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stpStartInfo">A SmartThreadPool configuration that overrides the default behavior</param>
		public SmartThreadPool(STPStartInfo stpStartInfo)
		{
			_stpStartInfo = new STPStartInfo(stpStartInfo);
			Initialize();
		}

		private void Initialize()
		{
            Name = _stpStartInfo.ThreadPoolName;
			ValidateSTPStartInfo();

            // _stpStartInfoRW stores a read/write copy of the STPStartInfo.
            // Actually only MaxWorkerThreads and MinWorkerThreads are overwritten

            _isSuspended = _stpStartInfo.StartSuspended;

#if (_WINDOWS_CE) || (_SILVERLIGHT) || (_MONO) || (WINDOWS_PHONE)
			if (null != _stpStartInfo.PerformanceCounterInstanceName)
			{
                throw new NotSupportedException("Performance counters are not implemented for Compact Framework/Silverlight/Mono, instead use StpStartInfo.EnableLocalPerformanceCounters");
            }
#else
            if (null != _stpStartInfo.PerformanceCounterInstanceName)
            {
                try
                {
                    _windowsPCs = new STPInstancePerformanceCounters(_stpStartInfo.PerformanceCounterInstanceName);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Unable to create Performance Counters: " + e);
                    _windowsPCs = NullSTPInstancePerformanceCounters.Instance;
                }
            }
#endif

            if (_stpStartInfo.EnableLocalPerformanceCounters)
            {
                _localPCs = new LocalSTPInstancePerformanceCounters();
            }

		    // If the STP is not started suspended then start the threads.
            if (!_isSuspended)
            {
                StartOptimalNumberOfThreads();
            }
		}

		private void StartOptimalNumberOfThreads()
		{
			int threadsCount = Math.Max(_workItemsQueue.Count, _stpStartInfo.MinWorkerThreads);
            threadsCount = Math.Min(threadsCount, _stpStartInfo.MaxWorkerThreads);
            threadsCount -= _workerThreads.Count;
            if (threadsCount > 0)
            {
                StartThreads(threadsCount);
            }
		}

		private void ValidateSTPStartInfo()
		{
            if (_stpStartInfo.MinWorkerThreads < 0)
			{
				throw new ArgumentOutOfRangeException(
					"MinWorkerThreads", "MinWorkerThreads cannot be negative");
			}

            if (_stpStartInfo.MaxWorkerThreads <= 0)
			{
				throw new ArgumentOutOfRangeException(
					"MaxWorkerThreads", "MaxWorkerThreads must be greater than zero");
			}

            if (_stpStartInfo.MinWorkerThreads > _stpStartInfo.MaxWorkerThreads)
			{
				throw new ArgumentOutOfRangeException(
					"MinWorkerThreads, maxWorkerThreads", 
					"MaxWorkerThreads must be greater or equal to MinWorkerThreads");
			}
		}

		private static void ValidateCallback(Delegate callback)
		{
			if(callback.GetInvocationList().Length > 1)
			{
				throw new NotSupportedException("SmartThreadPool doesn't support delegates chains");
			}
		}

		#endregion

		#region Thread Processing

		/// <summary>
		/// Waits on the queue for a work item, shutdown, or timeout.
		/// </summary>
		/// <returns>
		/// Returns the WaitingCallback or null in case of timeout or shutdown.
		/// </returns>
		private WorkItem Dequeue()
		{
			WorkItem workItem =
                _workItemsQueue.DequeueWorkItem(_stpStartInfo.IdleTimeout, _shuttingDownEvent);

			return workItem;
		}

		/// <summary>
		/// Put a new work item in the queue
		/// </summary>
		/// <param name="workItem">A work item to queue</param>
		internal override void Enqueue(WorkItem workItem)
		{
			// Make sure the workItem is not null
			Debug.Assert(null != workItem);

			IncrementWorkItemsCount();

            workItem.CanceledSmartThreadPool = _canceledSmartThreadPool;
			_workItemsQueue.EnqueueWorkItem(workItem);
			workItem.WorkItemIsQueued();

			// If all the threads are busy then try to create a new one
			if (_currentWorkItemsCount > _workerThreads.Count) 
			{
				StartThreads(1);
			}
		}

		private void IncrementWorkItemsCount()
		{
			_windowsPCs.SampleWorkItems(_workItemsQueue.Count, _workItemsProcessed);
            _localPCs.SampleWorkItems(_workItemsQueue.Count, _workItemsProcessed);

			int count = Interlocked.Increment(ref _currentWorkItemsCount);
			//Trace.WriteLine("WorkItemsCount = " + _currentWorkItemsCount.ToString());
			if (count == 1) 
			{
                IsIdle = false;
                _isIdleWaitHandle.Reset();
			}
		}

		private void DecrementWorkItemsCount()
		{
            int count = Interlocked.Decrement(ref _currentWorkItemsCount);
            //Trace.WriteLine("WorkItemsCount = " + _currentWorkItemsCount.ToString());
            if (count == 0)
            {
                IsIdle = true;
                _isIdleWaitHandle.Set();
            }

            Interlocked.Increment(ref _workItemsProcessed);

            if (!_shutdown)
            {
			    // The counter counts even if the work item was cancelled
			    _windowsPCs.SampleWorkItems(_workItemsQueue.Count, _workItemsProcessed);
                _localPCs.SampleWorkItems(_workItemsQueue.Count, _workItemsProcessed);
            }

		}

		internal void RegisterWorkItemsGroup(IWorkItemsGroup workItemsGroup)
		{
			_workItemsGroups[workItemsGroup] = workItemsGroup;
		}

		internal void UnregisterWorkItemsGroup(IWorkItemsGroup workItemsGroup)
		{
			if (_workItemsGroups.Contains(workItemsGroup))
			{
				_workItemsGroups.Remove(workItemsGroup);
			}
		}

		/// <summary>
		/// Inform that the current thread is about to quit or quiting.
		/// The same thread may call this method more than once.
		/// </summary>
		private void InformCompleted()
		{
			// There is no need to lock the two methods together 
			// since only the current thread removes itself
			// and the _workerThreads is a synchronized dictionary
			if (_workerThreads.Contains(Thread.CurrentThread))
			{
				_workerThreads.Remove(Thread.CurrentThread);
				_windowsPCs.SampleThreads(_workerThreads.Count, _inUseWorkerThreads);
                _localPCs.SampleThreads(_workerThreads.Count, _inUseWorkerThreads);
			}
		}

		/// <summary>
		/// Starts new threads
		/// </summary>
		/// <param name="threadsCount">The number of threads to start</param>
		private void StartThreads(int threadsCount)
		{
            if (_isSuspended)
			{
				return;
			}

			lock(_workerThreads.SyncRoot)
			{
				// Don't start threads on shut down
				if (_shutdown)
				{
					return;
				}

				for(int i = 0; i < threadsCount; ++i)
				{
					// Don't create more threads then the upper limit
                    if (_workerThreads.Count >= _stpStartInfo.MaxWorkerThreads)
					{
						return;
					}

                    // Create a new thread

#if (_SILVERLIGHT) || (WINDOWS_PHONE)
					Thread workerThread = new Thread(ProcessQueuedItems);
#else
                    Thread workerThread =
                        _stpStartInfo.MaxStackSize.HasValue
                        ? new Thread(ProcessQueuedItems, _stpStartInfo.MaxStackSize.Value)
                        : new Thread(ProcessQueuedItems);
#endif
					// Configure the new thread and start it
					workerThread.Name = "STP " + Name + " Thread #" + _threadCounter;
                    workerThread.IsBackground = _stpStartInfo.AreThreadsBackground;

#if !(_SILVERLIGHT) && !(_WINDOWS_CE) && !(WINDOWS_PHONE)
                    if (_stpStartInfo.ApartmentState != ApartmentState.Unknown)
                    {
                        workerThread.SetApartmentState(_stpStartInfo.ApartmentState);
                    }
#endif

#if !(_SILVERLIGHT) && !(WINDOWS_PHONE)
                    workerThread.Priority = _stpStartInfo.ThreadPriority;
#endif
                    workerThread.Start();
					++_threadCounter;

                    // Add it to the dictionary and update its creation time.
                    _workerThreads[workerThread] = new ThreadEntry(this);

					_windowsPCs.SampleThreads(_workerThreads.Count, _inUseWorkerThreads);
                    _localPCs.SampleThreads(_workerThreads.Count, _inUseWorkerThreads);
				}
			}
		}

		/// <summary>
		/// A worker thread method that processes work items from the work items queue.
		/// </summary>
		private void ProcessQueuedItems()
		{
            // Keep the entry of the dictionary as thread's variable to avoid the synchronization locks
            // of the dictionary.
            CurrentThreadEntry = _workerThreads[Thread.CurrentThread];

            FireOnThreadInitialization();

			try
			{
				bool bInUseWorkerThreadsWasIncremented = false;

				// Process until shutdown.
				while(!_shutdown)
				{
					// Update the last time this thread was seen alive.
					// It's good for debugging.
                    CurrentThreadEntry.IAmAlive();

                    // The following block handles the when the MaxWorkerThreads has been
                    // incremented by the user at run-time.
                    // Double lock for quit.
                    if (_workerThreads.Count > _stpStartInfo.MaxWorkerThreads)
                    {
                        lock (_workerThreads.SyncRoot)
                        {
                            if (_workerThreads.Count > _stpStartInfo.MaxWorkerThreads)
                            {
                                // Inform that the thread is quiting and then quit.
                                // This method must be called within this lock or else
                                // more threads will quit and the thread pool will go
                                // below the lower limit.
                                InformCompleted();
                                break;
                            }
                        }
                    }

					// Wait for a work item, shutdown, or timeout
					WorkItem workItem = Dequeue();

					// Update the last time this thread was seen alive.
					// It's good for debugging.
                    CurrentThreadEntry.IAmAlive();

					// On timeout or shut down.
					if (null == workItem)
					{
						// Double lock for quit.
                        if (_workerThreads.Count > _stpStartInfo.MinWorkerThreads)
						{
							lock(_workerThreads.SyncRoot)
							{
                                if (_workerThreads.Count > _stpStartInfo.MinWorkerThreads)
								{
									// Inform that the thread is quiting and then quit.
									// This method must be called within this lock or else
									// more threads will quit and the thread pool will go
									// below the lower limit.
									InformCompleted();
									break;
								}
							}
						}
					}

					// If we didn't quit then skip to the next iteration.
					if (null == workItem)
					{
						continue;
					}

					try 
					{
						// Initialize the value to false
						bInUseWorkerThreadsWasIncremented = false;

                        // Set the Current Work Item of the thread.
                        // Store the Current Work Item  before the workItem.StartingWorkItem() is called, 
                        // so WorkItem.Cancel can work when the work item is between InQueue and InProgress 
                        // states.
                        // If the work item has been cancelled BEFORE the workItem.StartingWorkItem() 
                        // (work item is in InQueue state) then workItem.StartingWorkItem() will return false.
                        // If the work item has been cancelled AFTER the workItem.StartingWorkItem() then
                        // (work item is in InProgress state) then the thread will be aborted
                        CurrentThreadEntry.CurrentWorkItem = workItem;

						// Change the state of the work item to 'in progress' if possible.
						// We do it here so if the work item has been canceled we won't 
						// increment the _inUseWorkerThreads.
						// The cancel mechanism doesn't delete items from the queue,  
						// it marks the work item as canceled, and when the work item
						// is dequeued, we just skip it.
						// If the post execute of work item is set to always or to
						// call when the work item is canceled then the StartingWorkItem()
						// will return true, so the post execute can run.
						if (!workItem.StartingWorkItem())
						{
							continue;
						}

						// Execute the callback.  Make sure to accurately
						// record how many callbacks are currently executing.
						int inUseWorkerThreads = Interlocked.Increment(ref _inUseWorkerThreads);
						_windowsPCs.SampleThreads(_workerThreads.Count, inUseWorkerThreads);
                        _localPCs.SampleThreads(_workerThreads.Count, inUseWorkerThreads);

						// Mark that the _inUseWorkerThreads incremented, so in the finally{}
						// statement we will decrement it correctly.
						bInUseWorkerThreadsWasIncremented = true;

                        workItem.FireWorkItemStarted();

						ExecuteWorkItem(workItem);
					}
					catch(Exception ex)
					{
                        ex.GetHashCode();
						// Do nothing
					}
					finally
					{
						workItem.DisposeOfState();

						// Set the CurrentWorkItem to null, since we 
						// no longer run user's code.
                        CurrentThreadEntry.CurrentWorkItem = null;

						// Decrement the _inUseWorkerThreads only if we had 
						// incremented it. Note the cancelled work items don't
						// increment _inUseWorkerThreads.
						if (bInUseWorkerThreadsWasIncremented)
						{
							int inUseWorkerThreads = Interlocked.Decrement(ref _inUseWorkerThreads);
							_windowsPCs.SampleThreads(_workerThreads.Count, inUseWorkerThreads);
                            _localPCs.SampleThreads(_workerThreads.Count, inUseWorkerThreads);
						}

						// Notify that the work item has been completed.
						// WorkItemsGroup may enqueue their next work item.
						workItem.FireWorkItemCompleted();

						// Decrement the number of work items here so the idle 
						// ManualResetEvent won't fluctuate.
						DecrementWorkItemsCount();
					}
				}
			} 
			catch(ThreadAbortException tae)
			{
                tae.GetHashCode();
                // Handle the abort exception gracfully.
#if !(_WINDOWS_CE) && !(_SILVERLIGHT) && !(WINDOWS_PHONE)
				Thread.ResetAbort();
#endif
            }
			catch(Exception e)
			{
				Debug.Assert(null != e);
			}
			finally
			{
				InformCompleted();
                FireOnThreadTermination();
			}
		}

		private void ExecuteWorkItem(WorkItem workItem)
		{
			_windowsPCs.SampleWorkItemsWaitTime(workItem.WaitingTime);
            _localPCs.SampleWorkItemsWaitTime(workItem.WaitingTime);
			try
			{
				workItem.Execute();
			}
			finally
			{
				_windowsPCs.SampleWorkItemsProcessTime(workItem.ProcessTime);
                _localPCs.SampleWorkItemsProcessTime(workItem.ProcessTime);
			}
		}


		#endregion

		#region Public Methods

		private void ValidateWaitForIdle()
		{
            if (null != CurrentThreadEntry && CurrentThreadEntry.AssociatedSmartThreadPool == this)
			{
				throw new NotSupportedException(
					"WaitForIdle cannot be called from a thread on its SmartThreadPool, it causes a deadlock");
			}
		}

		internal static void ValidateWorkItemsGroupWaitForIdle(IWorkItemsGroup workItemsGroup)
		{
            if (null == CurrentThreadEntry)
            {
                return;
            }

            WorkItem workItem = CurrentThreadEntry.CurrentWorkItem;
            ValidateWorkItemsGroupWaitForIdleImpl(workItemsGroup, workItem);
			if ((null != workItemsGroup) &&
                (null != workItem) &&
                CurrentThreadEntry.CurrentWorkItem.WasQueuedBy(workItemsGroup))
			{
				throw new NotSupportedException("WaitForIdle cannot be called from a thread on its SmartThreadPool, it causes a deadlock");
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void ValidateWorkItemsGroupWaitForIdleImpl(IWorkItemsGroup workItemsGroup, WorkItem workItem)
		{
			if ((null != workItemsGroup) && 
				(null != workItem) &&
				workItem.WasQueuedBy(workItemsGroup))
			{
				throw new NotSupportedException("WaitForIdle cannot be called from a thread on its SmartThreadPool, it causes a deadlock");
			}
		}

		/// <summary>
		/// Force the SmartThreadPool to shutdown
		/// </summary>
		public void Shutdown()
		{
			Shutdown(true, 0);
		}

        /// <summary>
        /// Force the SmartThreadPool to shutdown with timeout
        /// </summary>
        public void Shutdown(bool forceAbort, TimeSpan timeout)
		{
			Shutdown(forceAbort, (int)timeout.TotalMilliseconds);
		}

		/// <summary>
		/// Empties the queue of work items and abort the threads in the pool.
		/// </summary>
		public void Shutdown(bool forceAbort, int millisecondsTimeout)
		{
			ValidateNotDisposed();

			ISTPInstancePerformanceCounters pcs = _windowsPCs;

			if (NullSTPInstancePerformanceCounters.Instance != _windowsPCs)
			{
				// Set the _pcs to "null" to stop updating the performance
				// counters
				_windowsPCs = NullSTPInstancePerformanceCounters.Instance;

                pcs.Dispose();
			}

			Thread [] threads;
			lock(_workerThreads.SyncRoot)
			{
				// Shutdown the work items queue
				_workItemsQueue.Dispose();

				// Signal the threads to exit
				_shutdown = true;
				_shuttingDownEvent.Set();

				// Make a copy of the threads' references in the pool
				threads = new Thread [_workerThreads.Count];
				_workerThreads.Keys.CopyTo(threads, 0);
			}

			int millisecondsLeft = millisecondsTimeout;
            Stopwatch stopwatch = Stopwatch.StartNew();
            //DateTime start = DateTime.UtcNow;
			bool waitInfinitely = (Timeout.Infinite == millisecondsTimeout);
			bool timeout = false;

			// Each iteration we update the time left for the timeout.
			foreach(Thread thread in threads)
			{
				// Join don't work with negative numbers
				if (!waitInfinitely && (millisecondsLeft < 0))
				{
					timeout = true;
					break;
				}

				// Wait for the thread to terminate
				bool success = thread.Join(millisecondsLeft);
				if(!success)
				{
					timeout = true;
					break;
				}

				if(!waitInfinitely)
				{
					// Update the time left to wait
                    //TimeSpan ts = DateTime.UtcNow - start;
                    millisecondsLeft = millisecondsTimeout - (int)stopwatch.ElapsedMilliseconds;
				}
			}

			if (timeout && forceAbort)
			{
				// Abort the threads in the pool
				foreach(Thread thread in threads)
				{
                    
					if ((thread != null)
#if !(_WINDOWS_CE)
                        && thread.IsAlive
#endif                        
                        )
					{
						try 
						{
                            thread.Abort(); // Shutdown
						}
						catch(SecurityException e)
						{
                            e.GetHashCode();
						}
						catch(ThreadStateException ex)
						{
                            ex.GetHashCode();
							// In case the thread has been terminated 
							// after the check if it is alive.
						}
					}
				}
			}
		}

		/// <summary>
		/// Wait for all work items to complete
		/// </summary>
        /// <param name="waitableResults">Array of work item result objects</param>
		/// <returns>
		/// true when every work item in workItemResults has completed; otherwise false.
		/// </returns>
		public static bool WaitAll(
			IWaitableResult [] waitableResults)
		{
            return WaitAll(waitableResults, Timeout.Infinite, true);
		}

		/// <summary>
		/// Wait for all work items to complete
		/// </summary>
        /// <param name="waitableResults">Array of work item result objects</param>
		/// <param name="timeout">The number of milliseconds to wait, or a TimeSpan that represents -1 milliseconds to wait indefinitely. </param>
		/// <param name="exitContext">
		/// true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it; otherwise, false. 
		/// </param>
		/// <returns>
		/// true when every work item in workItemResults has completed; otherwise false.
		/// </returns>
		public static bool WaitAll(
			IWaitableResult [] waitableResults,
			TimeSpan timeout,
			bool exitContext)
		{
            return WaitAll(waitableResults, (int)timeout.TotalMilliseconds, exitContext);
		}

		/// <summary>
		/// Wait for all work items to complete
		/// </summary>
        /// <param name="waitableResults">Array of work item result objects</param>
		/// <param name="timeout">The number of milliseconds to wait, or a TimeSpan that represents -1 milliseconds to wait indefinitely. </param>
		/// <param name="exitContext">
		/// true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it; otherwise, false. 
		/// </param>
		/// <param name="cancelWaitHandle">A cancel wait handle to interrupt the wait if needed</param>
		/// <returns>
		/// true when every work item in workItemResults has completed; otherwise false.
		/// </returns>
		public static bool WaitAll(
            IWaitableResult[] waitableResults,  
			TimeSpan timeout,
			bool exitContext,
			WaitHandle cancelWaitHandle)
		{
            return WaitAll(waitableResults, (int)timeout.TotalMilliseconds, exitContext, cancelWaitHandle);
		}

		/// <summary>
		/// Wait for all work items to complete
		/// </summary>
        /// <param name="waitableResults">Array of work item result objects</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to wait, or Timeout.Infinite (-1) to wait indefinitely.</param>
		/// <param name="exitContext">
		/// true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it; otherwise, false. 
		/// </param>
		/// <returns>
		/// true when every work item in workItemResults has completed; otherwise false.
		/// </returns>
		public static bool WaitAll(
			IWaitableResult [] waitableResults,  
			int millisecondsTimeout,
			bool exitContext)
		{
            return WorkItem.WaitAll(waitableResults, millisecondsTimeout, exitContext, null);
		}

		/// <summary>
		/// Wait for all work items to complete
		/// </summary>
        /// <param name="waitableResults">Array of work item result objects</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to wait, or Timeout.Infinite (-1) to wait indefinitely.</param>
		/// <param name="exitContext">
		/// true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it; otherwise, false. 
		/// </param>
		/// <param name="cancelWaitHandle">A cancel wait handle to interrupt the wait if needed</param>
		/// <returns>
		/// true when every work item in workItemResults has completed; otherwise false.
		/// </returns>
		public static bool WaitAll(
            IWaitableResult[] waitableResults,  
			int millisecondsTimeout,
			bool exitContext,
			WaitHandle cancelWaitHandle)
		{
            return WorkItem.WaitAll(waitableResults, millisecondsTimeout, exitContext, cancelWaitHandle);
		}


		/// <summary>
		/// Waits for any of the work items in the specified array to complete, cancel, or timeout
		/// </summary>
        /// <param name="waitableResults">Array of work item result objects</param>
		/// <returns>
		/// The array index of the work item result that satisfied the wait, or WaitTimeout if any of the work items has been canceled.
		/// </returns>
		public static int WaitAny(
			IWaitableResult [] waitableResults)
		{
            return WaitAny(waitableResults, Timeout.Infinite, true);
		}

		/// <summary>
		/// Waits for any of the work items in the specified array to complete, cancel, or timeout
		/// </summary>
        /// <param name="waitableResults">Array of work item result objects</param>
		/// <param name="timeout">The number of milliseconds to wait, or a TimeSpan that represents -1 milliseconds to wait indefinitely. </param>
		/// <param name="exitContext">
		/// true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it; otherwise, false. 
		/// </param>
		/// <returns>
		/// The array index of the work item result that satisfied the wait, or WaitTimeout if no work item result satisfied the wait and a time interval equivalent to millisecondsTimeout has passed or the work item has been canceled.
		/// </returns>
		public static int WaitAny(
            IWaitableResult[] waitableResults,
			TimeSpan timeout,
			bool exitContext)
		{
            return WaitAny(waitableResults, (int)timeout.TotalMilliseconds, exitContext);
		}

		/// <summary>
		/// Waits for any of the work items in the specified array to complete, cancel, or timeout
		/// </summary>
        /// <param name="waitableResults">Array of work item result objects</param>
		/// <param name="timeout">The number of milliseconds to wait, or a TimeSpan that represents -1 milliseconds to wait indefinitely. </param>
		/// <param name="exitContext">
		/// true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it; otherwise, false. 
		/// </param>
		/// <param name="cancelWaitHandle">A cancel wait handle to interrupt the wait if needed</param>
		/// <returns>
		/// The array index of the work item result that satisfied the wait, or WaitTimeout if no work item result satisfied the wait and a time interval equivalent to millisecondsTimeout has passed or the work item has been canceled.
		/// </returns>
		public static int WaitAny(
			IWaitableResult [] waitableResults,
			TimeSpan timeout,
			bool exitContext,
			WaitHandle cancelWaitHandle)
		{
            return WaitAny(waitableResults, (int)timeout.TotalMilliseconds, exitContext, cancelWaitHandle);
		}

		/// <summary>
		/// Waits for any of the work items in the specified array to complete, cancel, or timeout
		/// </summary>
        /// <param name="waitableResults">Array of work item result objects</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to wait, or Timeout.Infinite (-1) to wait indefinitely.</param>
		/// <param name="exitContext">
		/// true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it; otherwise, false. 
		/// </param>
		/// <returns>
		/// The array index of the work item result that satisfied the wait, or WaitTimeout if no work item result satisfied the wait and a time interval equivalent to millisecondsTimeout has passed or the work item has been canceled.
		/// </returns>
		public static int WaitAny(
			IWaitableResult [] waitableResults,  
			int millisecondsTimeout,
			bool exitContext)
		{
            return WorkItem.WaitAny(waitableResults, millisecondsTimeout, exitContext, null);
		}

		/// <summary>
		/// Waits for any of the work items in the specified array to complete, cancel, or timeout
		/// </summary>
        /// <param name="waitableResults">Array of work item result objects</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to wait, or Timeout.Infinite (-1) to wait indefinitely.</param>
		/// <param name="exitContext">
		/// true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it; otherwise, false. 
		/// </param>
		/// <param name="cancelWaitHandle">A cancel wait handle to interrupt the wait if needed</param>
		/// <returns>
		/// The array index of the work item result that satisfied the wait, or WaitTimeout if no work item result satisfied the wait and a time interval equivalent to millisecondsTimeout has passed or the work item has been canceled.
		/// </returns>
		public static int WaitAny(
			IWaitableResult [] waitableResults,  
			int millisecondsTimeout,
			bool exitContext,
			WaitHandle cancelWaitHandle)
		{
            return WorkItem.WaitAny(waitableResults, millisecondsTimeout, exitContext, cancelWaitHandle);
		}

        /// <summary>
        /// Creates a new WorkItemsGroup.
        /// </summary>
        /// <param name="concurrency">The number of work items that can be run concurrently</param>
        /// <returns>A reference to the WorkItemsGroup</returns>
		public IWorkItemsGroup CreateWorkItemsGroup(int concurrency)
		{
            IWorkItemsGroup workItemsGroup = new WorkItemsGroup(this, concurrency, _stpStartInfo);
			return workItemsGroup;
		}

        /// <summary>
        /// Creates a new WorkItemsGroup.
        /// </summary>
        /// <param name="concurrency">The number of work items that can be run concurrently</param>
        /// <param name="wigStartInfo">A WorkItemsGroup configuration that overrides the default behavior</param>
        /// <returns>A reference to the WorkItemsGroup</returns>
		public IWorkItemsGroup CreateWorkItemsGroup(int concurrency, WIGStartInfo wigStartInfo)
		{
			IWorkItemsGroup workItemsGroup = new WorkItemsGroup(this, concurrency, wigStartInfo);
			return workItemsGroup;
		}

        #region Fire Thread's Events

        private void FireOnThreadInitialization()
        {
            if (null != _onThreadInitialization)
            {
                foreach (ThreadInitializationHandler tih in _onThreadInitialization.GetInvocationList())
                {
                    try
                    {
                        tih();
                    }
                    catch (Exception e)
                    {
                        e.GetHashCode();
                        Debug.Assert(false);
                        throw;
                    }
                }
            }
        }

        private void FireOnThreadTermination()
        {
            if (null != _onThreadTermination)
            {
                foreach (ThreadTerminationHandler tth in _onThreadTermination.GetInvocationList())
                {
                    try
                    {
                        tth();
                    }
                    catch (Exception e)
                    {
                        e.GetHashCode();
                        Debug.Assert(false);
                        throw;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// This event is fired when a thread is created.
        /// Use it to initialize a thread before the work items use it.
        /// </summary>
        public event ThreadInitializationHandler OnThreadInitialization
        {
            add { _onThreadInitialization += value; }
            remove { _onThreadInitialization -= value; }
        }

        /// <summary>
        /// This event is fired when a thread is terminating.
        /// Use it for cleanup.
        /// </summary>
        public event ThreadTerminationHandler OnThreadTermination
        {
            add { _onThreadTermination += value; }
            remove { _onThreadTermination -= value; }
        }


        internal void CancelAbortWorkItemsGroup(WorkItemsGroup wig)
        {
            foreach (ThreadEntry threadEntry in _workerThreads.Values)
            {
                WorkItem workItem = threadEntry.CurrentWorkItem;
                if (null != workItem &&
                    workItem.WasQueuedBy(wig) &&
                    !workItem.IsCanceled)
                {
                    threadEntry.CurrentWorkItem.GetWorkItemResult().Cancel(true);
                }
            }
        }

        

		#endregion

		#region Properties

		/// <summary>
		/// Get/Set the lower limit of threads in the pool.
		/// </summary>
		public int MinThreads 
		{ 
			get 
			{
				ValidateNotDisposed();
                return _stpStartInfo.MinWorkerThreads; 
			}
            set
            {
                Debug.Assert(value >= 0);
                Debug.Assert(value <= _stpStartInfo.MaxWorkerThreads);
                if (_stpStartInfo.MaxWorkerThreads < value)
                {
                    _stpStartInfo.MaxWorkerThreads = value;
                }
                _stpStartInfo.MinWorkerThreads = value;
                StartOptimalNumberOfThreads();
            }
		}

	    /// <summary>
		/// Get/Set the upper limit of threads in the pool.
		/// </summary>
		public int MaxThreads 
		{ 
			get 
			{
				ValidateNotDisposed();
                return _stpStartInfo.MaxWorkerThreads; 
			} 

			set 
			{
                Debug.Assert(value > 0);
                Debug.Assert(value >= _stpStartInfo.MinWorkerThreads);
                if (_stpStartInfo.MinWorkerThreads > value)
                {
                    _stpStartInfo.MinWorkerThreads = value;
                }
                _stpStartInfo.MaxWorkerThreads = value;
                StartOptimalNumberOfThreads();
            } 
		}
		/// <summary>
		/// Get the number of threads in the thread pool.
		/// Should be between the lower and the upper limits.
		/// </summary>
		public int ActiveThreads 
		{ 
			get 
			{
				ValidateNotDisposed();
				return _workerThreads.Count; 
			} 
		}

		/// <summary>
		/// Get the number of busy (not idle) threads in the thread pool.
		/// </summary>
		public int InUseThreads 
		{ 
			get 
			{ 
				ValidateNotDisposed();
				return _inUseWorkerThreads; 
			} 
		}

        /// <summary>
        /// Returns true if the current running work item has been cancelled.
        /// Must be used within the work item's callback method.
        /// The work item should sample this value in order to know if it
        /// needs to quit before its completion.
        /// </summary>
        public static bool IsWorkItemCanceled
        {
            get
            {
                return CurrentThreadEntry.CurrentWorkItem.IsCanceled;
            }
        } 
        
        /// <summary>
        /// Checks if the work item has been cancelled, and if yes then abort the thread.
        /// Can be used with Cancel and timeout
        /// </summary>
        public static void AbortOnWorkItemCancel()
        {
            if (IsWorkItemCanceled)
            {
                Thread.CurrentThread.Abort();
            }
        }

        /// <summary>
        /// Thread Pool start information (readonly)
        /// </summary>
        public STPStartInfo STPStartInfo
        {
            get 
            {
                return _stpStartInfo.AsReadOnly(); 
            }
        }

	    public bool IsShuttingdown
	    {
            get { return _shutdown;  }
	    }

        /// <summary>
        /// Return the local calculated performance counters
        /// Available only if STPStartInfo.EnableLocalPerformanceCounters is true.
        /// </summary>
        public ISTPPerformanceCountersReader PerformanceCountersReader
        {
            get { return (ISTPPerformanceCountersReader)_localPCs; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (!_shutdown)
                {
                    Shutdown();
                }

                if (null != _shuttingDownEvent)
                {
                    _shuttingDownEvent.Close();
                    _shuttingDownEvent = null;
                }
                _workerThreads.Clear();
                
                if (null != _isIdleWaitHandle)
                {
                    _isIdleWaitHandle.Close();
                    _isIdleWaitHandle = null;
                }

                _isDisposed = true;
            }
        }

        private void ValidateNotDisposed()
        {
            if(_isDisposed)
            {
                throw new ObjectDisposedException(GetType().ToString(), "The SmartThreadPool has been shutdown");
            }
        }
        #endregion

        #region WorkItemsGroupBase Overrides

        /// <summary>
        /// Get/Set the maximum number of work items that execute cocurrency on the thread pool
        /// </summary>
        public override int Concurrency
	    {
	        get { return MaxThreads; }
	        set { MaxThreads = value; }
	    }

	    /// <summary>
	    /// Get the number of work items in the queue.
	    /// </summary>
	    public override int WaitingCallbacks
	    {
	        get
	        {
	            ValidateNotDisposed();
	            return _workItemsQueue.Count;
	        }
	    }

        /// <summary>
        /// Get an array with all the state objects of the currently running items.
        /// The array represents a snap shot and impact performance.
        /// </summary>
        public override object[] GetStates()
        {
            object[] states = _workItemsQueue.GetStates();
            return states;
        }

        /// <summary>
        /// WorkItemsGroup start information (readonly)
        /// </summary>
        public override WIGStartInfo WIGStartInfo
        {
            get { return _stpStartInfo.AsReadOnly(); }
        }

	    /// <summary>
        /// Start the thread pool if it was started suspended.
        /// If it is already running, this method is ignored.
        /// </summary>
        public override void Start()
        {
            if (!_isSuspended)
            {
                return;
            }
            _isSuspended = false;

            ICollection workItemsGroups = _workItemsGroups.Values;
            foreach (WorkItemsGroup workItemsGroup in workItemsGroups)
            {
                workItemsGroup.OnSTPIsStarting();
            }

            StartOptimalNumberOfThreads();
        }

        /// <summary>
        /// Cancel all work items using thread abortion
        /// </summary>
        /// <param name="abortExecution">True to stop work items by raising ThreadAbortException</param>
        public override void Cancel(bool abortExecution)
        {
            _canceledSmartThreadPool.IsCanceled = true;
            _canceledSmartThreadPool = new CanceledWorkItemsGroup();

            ICollection workItemsGroups = _workItemsGroups.Values;
            foreach (WorkItemsGroup workItemsGroup in workItemsGroups)
            {
                workItemsGroup.Cancel(abortExecution);
            }

            if (abortExecution)
            {
                foreach (ThreadEntry threadEntry in _workerThreads.Values)
                {
                    WorkItem workItem = threadEntry.CurrentWorkItem;
                    if (null != workItem &&
                        threadEntry.AssociatedSmartThreadPool == this &&
                        !workItem.IsCanceled)
                    {
                        threadEntry.CurrentWorkItem.GetWorkItemResult().Cancel(true);
                    }
                }
            }
        }

	    /// <summary>
        /// Wait for the thread pool to be idle
        /// </summary>
        public override bool WaitForIdle(int millisecondsTimeout)
        {
            ValidateWaitForIdle();
            return STPEventWaitHandle.WaitOne(_isIdleWaitHandle, millisecondsTimeout, false);
        }

        /// <summary>
        /// This event is fired when all work items are completed.
        /// (When IsIdle changes to true)
        /// This event only work on WorkItemsGroup. On SmartThreadPool
        /// it throws the NotImplementedException.
        /// </summary>
        public override event WorkItemsGroupIdleHandler OnIdle
        {
            add
            {
                throw new NotImplementedException("This event is not implemented in the SmartThreadPool class. Please create a WorkItemsGroup in order to use this feature.");
                //_onIdle += value;
            }
            remove
            {
                throw new NotImplementedException("This event is not implemented in the SmartThreadPool class. Please create a WorkItemsGroup in order to use this feature.");
                //_onIdle -= value;
            }
        }

	    internal override void PreQueueWorkItem()
        {
            ValidateNotDisposed();   
        }

        #endregion

        #region Join, Choice, Pipe, etc.

        /// <summary>
        /// Executes all actions in parallel.
        /// Returns when they all finish.
        /// </summary>
        /// <param name="actions">Actions to execute</param>
        public void Join(IEnumerable<Action> actions)
        {
            WIGStartInfo wigStartInfo = new WIGStartInfo { StartSuspended = true };
            IWorkItemsGroup workItemsGroup = CreateWorkItemsGroup(int.MaxValue, wigStartInfo);
            foreach (Action action in actions)
            {
                workItemsGroup.QueueWorkItem(action);
            }
            workItemsGroup.Start();
            workItemsGroup.WaitForIdle();
        }

        /// <summary>
        /// Executes all actions in parallel.
        /// Returns when they all finish.
        /// </summary>
        /// <param name="actions">Actions to execute</param>
        public void Join(params Action[] actions)
        {
            Join((IEnumerable<Action>)actions);
        }

        private class ChoiceIndex
        {
            public int _index = -1;
        }

        /// <summary>
        /// Executes all actions in parallel
        /// Returns when the first one completes
        /// </summary>
        /// <param name="actions">Actions to execute</param>
        public int Choice(IEnumerable<Action> actions)
        {
            WIGStartInfo wigStartInfo = new WIGStartInfo { StartSuspended = true };
            IWorkItemsGroup workItemsGroup = CreateWorkItemsGroup(int.MaxValue, wigStartInfo);

            ManualResetEvent anActionCompleted = new ManualResetEvent(false);

            ChoiceIndex choiceIndex = new ChoiceIndex();
            
            int i = 0;
            foreach (Action action in actions)
            {
                Action act = action;
                int value = i;
                workItemsGroup.QueueWorkItem(() => { act(); Interlocked.CompareExchange(ref choiceIndex._index, value, -1); anActionCompleted.Set(); });
                ++i;
            }
	        workItemsGroup.Start();
            anActionCompleted.WaitOne();

            return choiceIndex._index;
        }

        /// <summary>
        /// Executes all actions in parallel
        /// Returns when the first one completes
        /// </summary>
        /// <param name="actions">Actions to execute</param>
        public int Choice(params Action[] actions)
	    {
            return Choice((IEnumerable<Action>)actions);
        }

        /// <summary>
        /// Executes actions in sequence asynchronously.
        /// Returns immediately.
        /// </summary>
        /// <param name="pipeState">A state context that passes </param>
        /// <param name="actions">Actions to execute in the order they should run</param>
        public void Pipe<T>(T pipeState, IEnumerable<Action<T>> actions)
        {
            WIGStartInfo wigStartInfo = new WIGStartInfo { StartSuspended = true };
            IWorkItemsGroup workItemsGroup = CreateWorkItemsGroup(1, wigStartInfo);
            foreach (Action<T> action in actions)
            {
                Action<T> act = action;
                workItemsGroup.QueueWorkItem(() => act(pipeState));
            }
            workItemsGroup.Start();
            workItemsGroup.WaitForIdle();
        }

        /// <summary>
        /// Executes actions in sequence asynchronously.
        /// Returns immediately.
        /// </summary>
        /// <param name="pipeState"></param>
        /// <param name="actions">Actions to execute in the order they should run</param>
        public void Pipe<T>(T pipeState, params Action<T>[] actions)
        {
            Pipe(pipeState, (IEnumerable<Action<T>>)actions);
        }
        #endregion
	}
	#endregion
}
