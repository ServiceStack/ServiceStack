
namespace Amib.Threading.Internal
{
    /// <summary>
    /// An internal delegate to call when the WorkItem starts or completes
    /// </summary>
    internal delegate void WorkItemStateCallback(WorkItem workItem);

    internal interface IInternalWorkItemResult
    {
        event WorkItemStateCallback OnWorkItemStarted;
        event WorkItemStateCallback OnWorkItemCompleted;
    }

    internal interface IInternalWaitableResult
    {
        /// <summary>
        /// This method is intent for internal use.
        /// </summary>   
        IWorkItemResult GetWorkItemResult();
    }

    public interface IHasWorkItemPriority
    {
        WorkItemPriority WorkItemPriority { get; }
    }
}
