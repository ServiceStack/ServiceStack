using System;
using System.Threading;

namespace Amib.Threading
{
	#region Delegates

	/// <summary>
	/// A delegate that represents the method to run as the work item
	/// </summary>
	/// <param name="state">A state object for the method to run</param>
	public delegate object WorkItemCallback(object state);

	/// <summary>
	/// A delegate to call after the WorkItemCallback completed
	/// </summary>
	/// <param name="wir">The work item result object</param>
    public delegate void PostExecuteWorkItemCallback(IWorkItemResult wir);

    /// <summary>
    /// A delegate to call after the WorkItemCallback completed
    /// </summary>
    /// <param name="wir">The work item result object</param>
    public delegate void PostExecuteWorkItemCallback<TResult>(IWorkItemResult<TResult> wir);

	/// <summary>
	/// A delegate to call when a WorkItemsGroup becomes idle
	/// </summary>
	/// <param name="workItemsGroup">A reference to the WorkItemsGroup that became idle</param>
	public delegate void WorkItemsGroupIdleHandler(IWorkItemsGroup workItemsGroup);

    /// <summary>
    /// A delegate to call after a thread is created, but before 
    /// it's first use.
    /// </summary>
    public delegate void ThreadInitializationHandler();

    /// <summary>
    /// A delegate to call when a thread is about to exit, after 
    /// it is no longer belong to the pool.
    /// </summary>
    public delegate void ThreadTerminationHandler();

	#endregion

	#region WorkItem Priority

    /// <summary>
    /// Defines the availeable priorities of a work item.
    /// The higher the priority a work item has, the sooner
    /// it will be executed.
    /// </summary>
	public enum WorkItemPriority
	{
		Lowest,
		BelowNormal,
		Normal,
		AboveNormal,
		Highest,
	}

	#endregion

	#region IWorkItemsGroup interface 

	/// <summary>
	/// IWorkItemsGroup interface
    /// Created by SmartThreadPool.CreateWorkItemsGroup()
	/// </summary>
	public interface IWorkItemsGroup
	{
		/// <summary>
		/// Get/Set the name of the WorkItemsGroup
		/// </summary>
		string Name { get; set; }

        /// <summary>
        /// Get/Set the maximum number of workitem that execute cocurrency on the thread pool
        /// </summary>
        int Concurrency { get; set; }

        /// <summary>
        /// Get the number of work items waiting in the queue.
        /// </summary>
        int WaitingCallbacks { get; }

        /// <summary>
        /// Get an array with all the state objects of the currently running items.
        /// The array represents a snap shot and impact performance.
        /// </summary>
        object[] GetStates();

        /// <summary>
        /// Get the WorkItemsGroup start information
        /// </summary>
        WIGStartInfo WIGStartInfo { get; }

        /// <summary>
        /// Starts to execute work items
        /// </summary>
        void Start();

        /// <summary>
        /// Cancel all the work items.
        /// Same as Cancel(false)
        /// </summary>
        void Cancel();

        /// <summary>
        /// Cancel all work items using thread abortion
        /// </summary>
        /// <param name="abortExecution">True to stop work items by raising ThreadAbortException</param>
        void Cancel(bool abortExecution);

        /// <summary>
        /// Wait for all work item to complete.
        /// </summary>
		void WaitForIdle();

        /// <summary>
        /// Wait for all work item to complete, until timeout expired
        /// </summary>
        /// <param name="timeout">How long to wait for the work items to complete</param>
        /// <returns>Returns true if work items completed within the timeout, otherwise false.</returns>
		bool WaitForIdle(TimeSpan timeout);

        /// <summary>
        /// Wait for all work item to complete, until timeout expired
        /// </summary>
        /// <param name="millisecondsTimeout">How long to wait for the work items to complete in milliseconds</param>
        /// <returns>Returns true if work items completed within the timeout, otherwise false.</returns>
        bool WaitForIdle(int millisecondsTimeout);

        /// <summary>
        /// IsIdle is true when there are no work items running or queued.
        /// </summary>
        bool IsIdle { get; }

        /// <summary>
        /// This event is fired when all work items are completed.
        /// (When IsIdle changes to true)
        /// This event only work on WorkItemsGroup. On SmartThreadPool
        /// it throws the NotImplementedException.
        /// </summary>
        event WorkItemsGroupIdleHandler OnIdle;

        #region QueueWorkItem

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="callback">A callback to execute</param>
        /// <returns>Returns a work item result</returns>        
        IWorkItemResult QueueWorkItem(WorkItemCallback callback);

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="callback">A callback to execute</param>
        /// <param name="workItemPriority">The priority of the work item</param>
        /// <returns>Returns a work item result</returns>
        IWorkItemResult QueueWorkItem(WorkItemCallback callback, WorkItemPriority workItemPriority);

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="callback">A callback to execute</param>
        /// <param name="state">
        /// The context object of the work item. Used for passing arguments to the work item. 
        /// </param>
        /// <returns>Returns a work item result</returns>
        IWorkItemResult QueueWorkItem(WorkItemCallback callback, object state);

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="callback">A callback to execute</param>
        /// <param name="state">
        /// The context object of the work item. Used for passing arguments to the work item. 
        /// </param>
        /// <param name="workItemPriority">The work item priority</param>
        /// <returns>Returns a work item result</returns>
        IWorkItemResult QueueWorkItem(WorkItemCallback callback, object state, WorkItemPriority workItemPriority);

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="callback">A callback to execute</param>
        /// <param name="state">
        /// The context object of the work item. Used for passing arguments to the work item. 
        /// </param>
        /// <param name="postExecuteWorkItemCallback">
        /// A delegate to call after the callback completion
        /// </param>
        /// <returns>Returns a work item result</returns>
        IWorkItemResult QueueWorkItem(WorkItemCallback callback, object state, PostExecuteWorkItemCallback postExecuteWorkItemCallback);

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="callback">A callback to execute</param>
        /// <param name="state">
        /// The context object of the work item. Used for passing arguments to the work item. 
        /// </param>
        /// <param name="postExecuteWorkItemCallback">
        /// A delegate to call after the callback completion
        /// </param>
        /// <param name="workItemPriority">The work item priority</param>
        /// <returns>Returns a work item result</returns>
        IWorkItemResult QueueWorkItem(WorkItemCallback callback, object state, PostExecuteWorkItemCallback postExecuteWorkItemCallback, WorkItemPriority workItemPriority);

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="callback">A callback to execute</param>
        /// <param name="state">
        /// The context object of the work item. Used for passing arguments to the work item. 
        /// </param>
        /// <param name="postExecuteWorkItemCallback">
        /// A delegate to call after the callback completion
        /// </param>
        /// <param name="callToPostExecute">Indicates on which cases to call to the post execute callback</param>
        /// <returns>Returns a work item result</returns>
        IWorkItemResult QueueWorkItem(WorkItemCallback callback, object state, PostExecuteWorkItemCallback postExecuteWorkItemCallback, CallToPostExecute callToPostExecute);

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="callback">A callback to execute</param>
        /// <param name="state">
        /// The context object of the work item. Used for passing arguments to the work item. 
        /// </param>
        /// <param name="postExecuteWorkItemCallback">
        /// A delegate to call after the callback completion
        /// </param>
        /// <param name="callToPostExecute">Indicates on which cases to call to the post execute callback</param>
        /// <param name="workItemPriority">The work item priority</param>
        /// <returns>Returns a work item result</returns>
        IWorkItemResult QueueWorkItem(WorkItemCallback callback, object state, PostExecuteWorkItemCallback postExecuteWorkItemCallback, CallToPostExecute callToPostExecute, WorkItemPriority workItemPriority);

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="workItemInfo">Work item info</param>
        /// <param name="callback">A callback to execute</param>
        /// <returns>Returns a work item result</returns>
        IWorkItemResult QueueWorkItem(WorkItemInfo workItemInfo, WorkItemCallback callback);

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="workItemInfo">Work item information</param>
        /// <param name="callback">A callback to execute</param>
        /// <param name="state">
        /// The context object of the work item. Used for passing arguments to the work item. 
        /// </param>
        /// <returns>Returns a work item result</returns>
        IWorkItemResult QueueWorkItem(WorkItemInfo workItemInfo, WorkItemCallback callback, object state);

        #endregion

        #region QueueWorkItem(Action<...>)

        /// <summary>
        /// Queue a work item.
        /// </summary>
        /// <returns>Returns a IWorkItemResult object, but its GetResult() will always return null</returns>
        IWorkItemResult QueueWorkItem(Action action, WorkItemPriority priority = SmartThreadPool.DefaultWorkItemPriority);

        /// <summary>
        /// Queue a work item.
        /// </summary>
        /// <returns>Returns a IWorkItemResult object, but its GetResult() will always return null</returns>
        IWorkItemResult QueueWorkItem<T>(Action<T> action, T arg, WorkItemPriority priority = SmartThreadPool.DefaultWorkItemPriority);

        /// <summary>
        /// Queue a work item.
        /// </summary>
        /// <returns>Returns a IWorkItemResult object, but its GetResult() will always return null</returns>
        IWorkItemResult QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, WorkItemPriority priority = SmartThreadPool.DefaultWorkItemPriority);

        /// <summary>
        /// Queue a work item.
        /// </summary>
        /// <returns>Returns a IWorkItemResult object, but its GetResult() will always return null</returns>
        IWorkItemResult QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3, WorkItemPriority priority = SmartThreadPool.DefaultWorkItemPriority);

        /// <summary>
        /// Queue a work item.
        /// </summary>
        /// <returns>Returns a IWorkItemResult object, but its GetResult() will always return null</returns>
        IWorkItemResult QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WorkItemPriority priority = SmartThreadPool.DefaultWorkItemPriority);

        #endregion

        #region QueueWorkItem(Func<...>)

        /// <summary>
        /// Queue a work item.
        /// </summary>
        /// <returns>Returns a IWorkItemResult&lt;TResult&gt; object. 
        /// its GetResult() returns a TResult object</returns>
        IWorkItemResult<TResult> QueueWorkItem<TResult>(Func<TResult> func, WorkItemPriority priority = SmartThreadPool.DefaultWorkItemPriority);

        /// <summary>
        /// Queue a work item.
        /// </summary>
        /// <returns>Returns a IWorkItemResult&lt;TResult&gt; object. 
        /// its GetResult() returns a TResult object</returns>
        IWorkItemResult<TResult> QueueWorkItem<T, TResult>(Func<T, TResult> func, T arg, WorkItemPriority priority = SmartThreadPool.DefaultWorkItemPriority);

        /// <summary>
        /// Queue a work item.
        /// </summary>
        /// <returns>Returns a IWorkItemResult&lt;TResult&gt; object. 
        /// its GetResult() returns a TResult object</returns>
        IWorkItemResult<TResult> QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2, WorkItemPriority priority = SmartThreadPool.DefaultWorkItemPriority);

        /// <summary>
        /// Queue a work item.
        /// </summary>
        /// <returns>Returns a IWorkItemResult&lt;TResult&gt; object. 
        /// its GetResult() returns a TResult object</returns>
        IWorkItemResult<TResult> QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3, WorkItemPriority priority = SmartThreadPool.DefaultWorkItemPriority);

        /// <summary>
        /// Queue a work item.
        /// </summary>
        /// <returns>Returns a IWorkItemResult&lt;TResult&gt; object. 
        /// its GetResult() returns a TResult object</returns>
        IWorkItemResult<TResult> QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WorkItemPriority priority = SmartThreadPool.DefaultWorkItemPriority);

        #endregion
    }

	#endregion

	#region CallToPostExecute enumerator

	[Flags]
	public enum CallToPostExecute
	{
        /// <summary>
        /// Never call to the PostExecute call back
        /// </summary>
		Never                    = 0x00,

        /// <summary>
        /// Call to the PostExecute only when the work item is cancelled
        /// </summary>
		WhenWorkItemCanceled     = 0x01,

        /// <summary>
        /// Call to the PostExecute only when the work item is not cancelled
        /// </summary>
		WhenWorkItemNotCanceled  = 0x02,

        /// <summary>
        /// Always call to the PostExecute
        /// </summary>
		Always                   = WhenWorkItemCanceled | WhenWorkItemNotCanceled,
	}

	#endregion

	#region IWorkItemResult interface

    /// <summary>
    /// The common interface of IWorkItemResult and IWorkItemResult&lt;T&gt;
    /// </summary>
    public interface IWaitableResult
    {
        /// <summary>
        /// This method intent is for internal use.
        /// </summary>
        /// <returns></returns>
        IWorkItemResult GetWorkItemResult();

        /// <summary>
        /// This method intent is for internal use.
        /// </summary>
        /// <returns></returns>
        IWorkItemResult<TResult> GetWorkItemResultT<TResult>();
    }

    /// <summary>
    /// IWorkItemResult interface.
    /// Created when a WorkItemCallback work item is queued.
    /// </summary>
    public interface IWorkItemResult : IWorkItemResult<object>
    {
    }

	/// <summary>
    /// IWorkItemResult&lt;TResult&gt; interface.
    /// Created when a Func&lt;TResult&gt; work item is queued.
	/// </summary>
    public interface IWorkItemResult<TResult> : IWaitableResult
	{
		/// <summary>
		/// Get the result of the work item.
		/// If the work item didn't run yet then the caller waits.
		/// </summary>
		/// <returns>The result of the work item</returns>
        TResult GetResult();

		/// <summary>
		/// Get the result of the work item.
		/// If the work item didn't run yet then the caller waits until timeout.
		/// </summary>
		/// <returns>The result of the work item</returns>
		/// On timeout throws WorkItemTimeoutException
        TResult GetResult(
			int millisecondsTimeout,
			bool exitContext);

		/// <summary>
		/// Get the result of the work item.
		/// If the work item didn't run yet then the caller waits until timeout.
		/// </summary>
		/// <returns>The result of the work item</returns>
		/// On timeout throws WorkItemTimeoutException
        TResult GetResult(			
			TimeSpan timeout,
			bool exitContext);

		/// <summary>
		/// Get the result of the work item.
		/// If the work item didn't run yet then the caller waits until timeout or until the cancelWaitHandle is signaled.
		/// </summary>
		/// <param name="millisecondsTimeout">Timeout in milliseconds, or -1 for infinite</param>
		/// <param name="exitContext">
		/// true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it; otherwise, false. 
		/// </param>
		/// <param name="cancelWaitHandle">A cancel wait handle to interrupt the blocking if needed</param>
		/// <returns>The result of the work item</returns>
		/// On timeout throws WorkItemTimeoutException
		/// On cancel throws WorkItemCancelException
        TResult GetResult(			
			int millisecondsTimeout,
			bool exitContext,
			WaitHandle cancelWaitHandle);

		/// <summary>
		/// Get the result of the work item.
		/// If the work item didn't run yet then the caller waits until timeout or until the cancelWaitHandle is signaled.
		/// </summary>
		/// <returns>The result of the work item</returns>
		/// On timeout throws WorkItemTimeoutException
		/// On cancel throws WorkItemCancelException
        TResult GetResult(			
			TimeSpan timeout,
			bool exitContext,
			WaitHandle cancelWaitHandle);

		/// <summary>
		/// Get the result of the work item.
		/// If the work item didn't run yet then the caller waits.
		/// </summary>
		/// <param name="e">Filled with the exception if one was thrown</param>
		/// <returns>The result of the work item</returns>
        TResult GetResult(out Exception e);

		/// <summary>
		/// Get the result of the work item.
		/// If the work item didn't run yet then the caller waits until timeout.
		/// </summary>
		/// <param name="millisecondsTimeout"></param>
		/// <param name="exitContext"></param>
		/// <param name="e">Filled with the exception if one was thrown</param>
		/// <returns>The result of the work item</returns>
		/// On timeout throws WorkItemTimeoutException
        TResult GetResult(
			int millisecondsTimeout,
			bool exitContext,
			out Exception e);

		/// <summary>
		/// Get the result of the work item.
		/// If the work item didn't run yet then the caller waits until timeout.
		/// </summary>
		/// <param name="exitContext"></param>
		/// <param name="e">Filled with the exception if one was thrown</param>
		/// <param name="timeout"></param>
		/// <returns>The result of the work item</returns>
		/// On timeout throws WorkItemTimeoutException
        TResult GetResult(			
			TimeSpan timeout,
			bool exitContext,
			out Exception e);

		/// <summary>
		/// Get the result of the work item.
		/// If the work item didn't run yet then the caller waits until timeout or until the cancelWaitHandle is signaled.
		/// </summary>
		/// <param name="millisecondsTimeout">Timeout in milliseconds, or -1 for infinite</param>
		/// <param name="exitContext">
		/// true to exit the synchronization domain for the context before the wait (if in a synchronized context), and reacquire it; otherwise, false. 
		/// </param>
		/// <param name="cancelWaitHandle">A cancel wait handle to interrupt the blocking if needed</param>
		/// <param name="e">Filled with the exception if one was thrown</param>
		/// <returns>The result of the work item</returns>
		/// On timeout throws WorkItemTimeoutException
		/// On cancel throws WorkItemCancelException
        TResult GetResult(			
			int millisecondsTimeout,
			bool exitContext,
			WaitHandle cancelWaitHandle,
			out Exception e);

		/// <summary>
		/// Get the result of the work item.
		/// If the work item didn't run yet then the caller waits until timeout or until the cancelWaitHandle is signaled.
		/// </summary>
		/// <returns>The result of the work item</returns>
		/// <param name="cancelWaitHandle"></param>
		/// <param name="e">Filled with the exception if one was thrown</param>
		/// <param name="timeout"></param>
		/// <param name="exitContext"></param>
		/// On timeout throws WorkItemTimeoutException
		/// On cancel throws WorkItemCancelException
        TResult GetResult(			
			TimeSpan timeout,
			bool exitContext,
			WaitHandle cancelWaitHandle,
			out Exception e);

		/// <summary>
		/// Gets an indication whether the asynchronous operation has completed.
		/// </summary>
		bool IsCompleted { get; }

		/// <summary>
		/// Gets an indication whether the asynchronous operation has been canceled.
		/// </summary>
		bool IsCanceled { get; }

		/// <summary>
		/// Gets the user-defined object that contains context data 
        /// for the work item method.
		/// </summary>
		object State { get; }

		/// <summary>
        /// Same as Cancel(false).
		/// </summary>
        bool Cancel();

        /// <summary>
        /// Cancel the work item execution.
        /// If the work item is in the queue then it won't execute
        /// If the work item is completed, it will remain completed
        /// If the work item is in progress then the user can check the SmartThreadPool.IsWorkItemCanceled
        ///   property to check if the work item has been cancelled. If the abortExecution is set to true then
        ///   the Smart Thread Pool will send an AbortException to the running thread to stop the execution 
        ///   of the work item. When an in progress work item is canceled its GetResult will throw WorkItemCancelException.
        /// If the work item is already cancelled it will remain cancelled
        /// </summary>
        /// <param name="abortExecution">When true send an AbortException to the executing thread.</param>
        /// <returns>Returns true if the work item was not completed, otherwise false.</returns>
        bool Cancel(bool abortExecution);

		/// <summary>
		/// Get the work item's priority
		/// </summary>
		WorkItemPriority WorkItemPriority { get; }

		/// <summary>
		/// Return the result, same as GetResult()
		/// </summary>
        TResult Result { get; }

		/// <summary>
		/// Returns the exception if occured otherwise returns null.
		/// </summary>
		object Exception { get; }
	}

	#endregion

    #region .NET 3.5

    // All these delegate are built-in .NET 3.5
    // Comment/Remove them when compiling to .NET 3.5 to avoid ambiguity.

    public delegate void Action();
    public delegate void Action<T1, T2>(T1 arg1, T2 arg2);
    public delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
    public delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

    public delegate TResult Func<TResult>();
    public delegate TResult Func<T, TResult>(T arg1);
    public delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);
    public delegate TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
    public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

    #endregion
}
