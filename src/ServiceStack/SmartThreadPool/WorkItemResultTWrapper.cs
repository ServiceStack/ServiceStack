using System;
using System.Threading;

namespace Amib.Threading.Internal
{
    #region WorkItemResultTWrapper class

    internal class WorkItemResultTWrapper<TResult> : IWorkItemResult<TResult>, IInternalWaitableResult
    {
        private readonly IWorkItemResult _workItemResult;

        public WorkItemResultTWrapper(IWorkItemResult workItemResult)
        {
            _workItemResult = workItemResult;
        }

        #region IWorkItemResult<TResult> Members

        public TResult GetResult()
        {
            return (TResult)_workItemResult.GetResult();
        }

        public TResult GetResult(int millisecondsTimeout, bool exitContext)
        {
            return (TResult)_workItemResult.GetResult(millisecondsTimeout, exitContext);
        }

        public TResult GetResult(TimeSpan timeout, bool exitContext)
        {
            return (TResult)_workItemResult.GetResult(timeout, exitContext);
        }

        public TResult GetResult(int millisecondsTimeout, bool exitContext, WaitHandle cancelWaitHandle)
        {
            return (TResult)_workItemResult.GetResult(millisecondsTimeout, exitContext, cancelWaitHandle);
        }

        public TResult GetResult(TimeSpan timeout, bool exitContext, WaitHandle cancelWaitHandle)
        {
            return (TResult)_workItemResult.GetResult(timeout, exitContext, cancelWaitHandle);
        }

        public TResult GetResult(out Exception e)
        {
            return (TResult)_workItemResult.GetResult(out e);
        }

        public TResult GetResult(int millisecondsTimeout, bool exitContext, out Exception e)
        {
            return (TResult)_workItemResult.GetResult(millisecondsTimeout, exitContext, out e);
        }

        public TResult GetResult(TimeSpan timeout, bool exitContext, out Exception e)
        {
            return (TResult)_workItemResult.GetResult(timeout, exitContext, out e);
        }

        public TResult GetResult(int millisecondsTimeout, bool exitContext, WaitHandle cancelWaitHandle, out Exception e)
        {
            return (TResult)_workItemResult.GetResult(millisecondsTimeout, exitContext, cancelWaitHandle, out e);
        }

        public TResult GetResult(TimeSpan timeout, bool exitContext, WaitHandle cancelWaitHandle, out Exception e)
        {
            return (TResult)_workItemResult.GetResult(timeout, exitContext, cancelWaitHandle, out e);
        }

        public bool IsCompleted
        {
            get { return _workItemResult.IsCompleted; }
        }

        public bool IsCanceled
        {
            get { return _workItemResult.IsCanceled; }
        }

        public object State
        {
            get { return _workItemResult.State; }
        }

        public bool Cancel()
        {
            return _workItemResult.Cancel();
        }

        public bool Cancel(bool abortExecution)
        {
            return _workItemResult.Cancel(abortExecution);
        }

        public WorkItemPriority WorkItemPriority
        {
            get { return _workItemResult.WorkItemPriority; }
        }

        public TResult Result
        {
            get { return (TResult)_workItemResult.Result; }
        }

        public object Exception
        {
            get { return (TResult)_workItemResult.Exception; }
        }

        #region IInternalWorkItemResult Members

        public IWorkItemResult GetWorkItemResult()
        {
            return _workItemResult.GetWorkItemResult();
        }

        public IWorkItemResult<TRes> GetWorkItemResultT<TRes>()
        {
            return (IWorkItemResult<TRes>)this;
        }

        #endregion

        #endregion
    }

    #endregion

}
