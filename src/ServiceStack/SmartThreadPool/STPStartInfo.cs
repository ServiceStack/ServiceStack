using System;
using System.Threading;

namespace Amib.Threading
{
	/// <summary>
	/// Summary description for STPStartInfo.
	/// </summary>
    public class STPStartInfo : WIGStartInfo
    {
        private int _idleTimeout = SmartThreadPool.DefaultIdleTimeout;
        private int _minWorkerThreads = SmartThreadPool.DefaultMinWorkerThreads;
        private int _maxWorkerThreads = SmartThreadPool.DefaultMaxWorkerThreads;
#if !(WINDOWS_PHONE)
        private ThreadPriority _threadPriority = SmartThreadPool.DefaultThreadPriority;
#endif
        private string _performanceCounterInstanceName = SmartThreadPool.DefaultPerformanceCounterInstanceName;
        private bool _areThreadsBackground = SmartThreadPool.DefaultAreThreadsBackground;
        private bool _enableLocalPerformanceCounters;
        private string _threadPoolName = SmartThreadPool.DefaultThreadPoolName;
        private int? _maxStackSize = SmartThreadPool.DefaultMaxStackSize;

        public STPStartInfo()
        {
            _performanceCounterInstanceName = SmartThreadPool.DefaultPerformanceCounterInstanceName;
#if !(WINDOWS_PHONE)
            _threadPriority = SmartThreadPool.DefaultThreadPriority;
#endif
            _maxWorkerThreads = SmartThreadPool.DefaultMaxWorkerThreads;
            _idleTimeout = SmartThreadPool.DefaultIdleTimeout;
            _minWorkerThreads = SmartThreadPool.DefaultMinWorkerThreads;
        }

	    public STPStartInfo(STPStartInfo stpStartInfo)
            : base(stpStartInfo)
        {
            _idleTimeout = stpStartInfo.IdleTimeout;
            _minWorkerThreads = stpStartInfo.MinWorkerThreads;
            _maxWorkerThreads = stpStartInfo.MaxWorkerThreads;
#if !(WINDOWS_PHONE)
            _threadPriority = stpStartInfo.ThreadPriority;
#endif
            _performanceCounterInstanceName = stpStartInfo.PerformanceCounterInstanceName;
            _enableLocalPerformanceCounters = stpStartInfo._enableLocalPerformanceCounters;
            _threadPoolName = stpStartInfo._threadPoolName;
            _areThreadsBackground = stpStartInfo.AreThreadsBackground;
#if !(_SILVERLIGHT) && !(WINDOWS_PHONE)
            _apartmentState = stpStartInfo._apartmentState;
#endif
        }
	  
	    /// <summary>
	    /// Get/Set the idle timeout in milliseconds.
	    /// If a thread is idle (starved) longer than IdleTimeout then it may quit.
	    /// </summary>
	    public virtual int IdleTimeout
	    {
	        get { return _idleTimeout; }
	        set 
            {
                ThrowIfReadOnly();
                _idleTimeout = value; 
            }
	    }


	    /// <summary>
	    /// Get/Set the lower limit of threads in the pool.
	    /// </summary>
	    public virtual int MinWorkerThreads
	    {
	        get { return _minWorkerThreads; }
	        set 
            {
                ThrowIfReadOnly();
                _minWorkerThreads = value; 
            }
	    }


	    /// <summary>
	    /// Get/Set the upper limit of threads in the pool.
	    /// </summary>
	    public virtual int MaxWorkerThreads
	    {
	        get { return _maxWorkerThreads; }
	        set 
            {
                ThrowIfReadOnly();
                _maxWorkerThreads = value; 
            }
	    }

#if !(WINDOWS_PHONE)
	    /// <summary>
	    /// Get/Set the scheduling priority of the threads in the pool.
	    /// The Os handles the scheduling.
	    /// </summary>
	    public virtual ThreadPriority ThreadPriority
	    {
	        get { return _threadPriority; }
	        set 
            {
                ThrowIfReadOnly();
                _threadPriority = value; 
            }
	    }
#endif
        /// <summary>
        /// Get/Set the thread pool name. Threads will get names depending on this.
        /// </summary>
        public virtual string ThreadPoolName {
            get { return _threadPoolName; }
            set
            {
                ThrowIfReadOnly ();
                _threadPoolName = value;
            }
        }

	    /// <summary>
	    /// Get/Set the performance counter instance name of this SmartThreadPool
	    /// The default is null which indicate not to use performance counters at all.
	    /// </summary>
	    public virtual string PerformanceCounterInstanceName
	    {
	        get { return _performanceCounterInstanceName; }
	        set 
            {
                ThrowIfReadOnly();
                _performanceCounterInstanceName = value; 
            }
	    }

        /// <summary>
        /// Enable/Disable the local performance counter.
        /// This enables the user to get some performance information about the SmartThreadPool 
        /// without using Windows performance counters. (Useful on WindowsCE, Silverlight, etc.)
        /// The default is false.
        /// </summary>
        public virtual bool EnableLocalPerformanceCounters
	    {
	        get { return _enableLocalPerformanceCounters; }
	        set 
			{ 
				ThrowIfReadOnly(); 
				_enableLocalPerformanceCounters = value; 
			}
	    }

        /// <summary>
        /// Get/Set backgroundness of thread in thread pool.
        /// </summary>
	    public virtual bool AreThreadsBackground
 	    {
 	        get { return _areThreadsBackground; }
 	        set
 	        {
 	            ThrowIfReadOnly ();
 	            _areThreadsBackground = value;
 	        }
 	    }

	    /// <summary>
        /// Get a readonly version of this STPStartInfo.
        /// </summary>
        /// <returns>Returns a readonly reference to this STPStartInfo</returns>
        public new STPStartInfo AsReadOnly()
        {
            return new STPStartInfo(this) { _readOnly = true };
        }

#if !(_SILVERLIGHT) && !(WINDOWS_PHONE)

        private ApartmentState _apartmentState = SmartThreadPool.DefaultApartmentState;

        /// <summary>
        /// Get/Set the apartment state of threads in the thread pool
        /// </summary>
        public ApartmentState ApartmentState
        {
            get { return _apartmentState; }
            set
            {
                ThrowIfReadOnly();
                _apartmentState = value;
            }
        } 

#if !(_SILVERLIGHT) && !(WINDOWS_PHONE)
        
        /// <summary>
        /// Get/Set the max stack size of threads in the thread pool
        /// </summary>
        public int? MaxStackSize
        {
            get { return _maxStackSize; }
            set
            {
                ThrowIfReadOnly();
                if (value.HasValue && value.Value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Value must be greater than 0.");
                }
                _maxStackSize = value;
            }
        }
#endif

#endif
    }
}
