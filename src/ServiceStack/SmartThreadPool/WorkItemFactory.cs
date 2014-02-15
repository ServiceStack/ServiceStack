using System;

namespace Amib.Threading.Internal
{
	#region WorkItemFactory class 

	public class WorkItemFactory
	{
		/// <summary>
		/// Create a new work item
		/// </summary>
		/// <param name="workItemsGroup">The WorkItemsGroup of this workitem</param>
		/// <param name="wigStartInfo">Work item group start information</param>
		/// <param name="callback">A callback to execute</param>
		/// <returns>Returns a work item</returns>
		public static WorkItem CreateWorkItem(
			IWorkItemsGroup workItemsGroup,
			WIGStartInfo wigStartInfo,
			WorkItemCallback callback)
		{
			return CreateWorkItem(workItemsGroup, wigStartInfo, callback, null);
		}

		/// <summary>
		/// Create a new work item
		/// </summary>
        /// <param name="workItemsGroup">The WorkItemsGroup of this workitem</param>
        /// <param name="wigStartInfo">Work item group start information</param>
		/// <param name="callback">A callback to execute</param>
		/// <param name="workItemPriority">The priority of the work item</param>
		/// <returns>Returns a work item</returns>
		public static WorkItem CreateWorkItem(
			IWorkItemsGroup workItemsGroup,
			WIGStartInfo wigStartInfo,
			WorkItemCallback callback, 
			WorkItemPriority workItemPriority)
		{
			return CreateWorkItem(workItemsGroup, wigStartInfo, callback, null, workItemPriority);
		}

		/// <summary>
		/// Create a new work item
		/// </summary>
        /// <param name="workItemsGroup">The WorkItemsGroup of this workitem</param>
        /// <param name="wigStartInfo">Work item group start information</param>
		/// <param name="workItemInfo">Work item info</param>
		/// <param name="callback">A callback to execute</param>
		/// <returns>Returns a work item</returns>
		public static WorkItem CreateWorkItem(
			IWorkItemsGroup workItemsGroup,
			WIGStartInfo wigStartInfo,
			WorkItemInfo workItemInfo, 
			WorkItemCallback callback)
		{
			return CreateWorkItem(
				workItemsGroup,
				wigStartInfo,
				workItemInfo, 
				callback, 
				null);
		}

		/// <summary>
		/// Create a new work item
		/// </summary>
        /// <param name="workItemsGroup">The WorkItemsGroup of this workitem</param>
        /// <param name="wigStartInfo">Work item group start information</param>
		/// <param name="callback">A callback to execute</param>
		/// <param name="state">
		/// The context object of the work item. Used for passing arguments to the work item. 
		/// </param>
		/// <returns>Returns a work item</returns>
		public static WorkItem CreateWorkItem(
			IWorkItemsGroup workItemsGroup,
			WIGStartInfo wigStartInfo,
			WorkItemCallback callback, 
			object state)
		{
			ValidateCallback(callback);
            
			WorkItemInfo workItemInfo = new WorkItemInfo();
			workItemInfo.UseCallerCallContext = wigStartInfo.UseCallerCallContext;
			workItemInfo.UseCallerHttpContext = wigStartInfo.UseCallerHttpContext;
			workItemInfo.PostExecuteWorkItemCallback = wigStartInfo.PostExecuteWorkItemCallback;
			workItemInfo.CallToPostExecute = wigStartInfo.CallToPostExecute;
			workItemInfo.DisposeOfStateObjects = wigStartInfo.DisposeOfStateObjects;
            workItemInfo.WorkItemPriority = wigStartInfo.WorkItemPriority;

			WorkItem workItem = new WorkItem(
				workItemsGroup,
				workItemInfo,
				callback, 
				state);
			return workItem;
		}

		/// <summary>
		/// Create a new work item
		/// </summary>
		/// <param name="workItemsGroup">The work items group</param>
		/// <param name="wigStartInfo">Work item group start information</param>
		/// <param name="callback">A callback to execute</param>
		/// <param name="state">
		/// The context object of the work item. Used for passing arguments to the work item. 
		/// </param>
		/// <param name="workItemPriority">The work item priority</param>
		/// <returns>Returns a work item</returns>
		public static WorkItem CreateWorkItem(
			IWorkItemsGroup workItemsGroup,
			WIGStartInfo wigStartInfo,
			WorkItemCallback callback, 
			object state, 
			WorkItemPriority workItemPriority)
		{
			ValidateCallback(callback);

			WorkItemInfo workItemInfo = new WorkItemInfo();
			workItemInfo.UseCallerCallContext = wigStartInfo.UseCallerCallContext;
			workItemInfo.UseCallerHttpContext = wigStartInfo.UseCallerHttpContext;
			workItemInfo.PostExecuteWorkItemCallback = wigStartInfo.PostExecuteWorkItemCallback;
			workItemInfo.CallToPostExecute = wigStartInfo.CallToPostExecute;
			workItemInfo.DisposeOfStateObjects = wigStartInfo.DisposeOfStateObjects;
			workItemInfo.WorkItemPriority = workItemPriority;

			WorkItem workItem = new WorkItem(
				workItemsGroup,
				workItemInfo,
				callback, 
				state);

			return workItem;
		}

		/// <summary>
		/// Create a new work item
		/// </summary>
        /// <param name="workItemsGroup">The work items group</param>
		/// <param name="wigStartInfo">Work item group start information</param>
		/// <param name="workItemInfo">Work item information</param>
		/// <param name="callback">A callback to execute</param>
		/// <param name="state">
		/// The context object of the work item. Used for passing arguments to the work item. 
		/// </param>
		/// <returns>Returns a work item</returns>
        public static WorkItem CreateWorkItem(
            IWorkItemsGroup workItemsGroup,
            WIGStartInfo wigStartInfo,
            WorkItemInfo workItemInfo,
            WorkItemCallback callback,
            object state)
        {
            ValidateCallback(callback);
            ValidateCallback(workItemInfo.PostExecuteWorkItemCallback);

            WorkItem workItem = new WorkItem(
                workItemsGroup,
                new WorkItemInfo(workItemInfo),
                callback,
                state);

            return workItem;
        }

		/// <summary>
		/// Create a new work item
		/// </summary>
        /// <param name="workItemsGroup">The work items group</param>
		/// <param name="wigStartInfo">Work item group start information</param>
		/// <param name="callback">A callback to execute</param>
		/// <param name="state">
		/// The context object of the work item. Used for passing arguments to the work item. 
		/// </param>
		/// <param name="postExecuteWorkItemCallback">
		/// A delegate to call after the callback completion
		/// </param>
		/// <returns>Returns a work item</returns>
		public static WorkItem CreateWorkItem(
			IWorkItemsGroup workItemsGroup,
			WIGStartInfo wigStartInfo,
			WorkItemCallback callback, 
			object state,
			PostExecuteWorkItemCallback postExecuteWorkItemCallback)
		{
			ValidateCallback(callback);
			ValidateCallback(postExecuteWorkItemCallback);

			WorkItemInfo workItemInfo = new WorkItemInfo();
			workItemInfo.UseCallerCallContext = wigStartInfo.UseCallerCallContext;
			workItemInfo.UseCallerHttpContext = wigStartInfo.UseCallerHttpContext;
			workItemInfo.PostExecuteWorkItemCallback = postExecuteWorkItemCallback;
			workItemInfo.CallToPostExecute = wigStartInfo.CallToPostExecute;
			workItemInfo.DisposeOfStateObjects = wigStartInfo.DisposeOfStateObjects;
            workItemInfo.WorkItemPriority = wigStartInfo.WorkItemPriority;

			WorkItem workItem = new WorkItem(
				workItemsGroup,
				workItemInfo,
				callback, 
				state);

			return workItem;
		}

		/// <summary>
		/// Create a new work item
		/// </summary>
        /// <param name="workItemsGroup">The work items group</param>
		/// <param name="wigStartInfo">Work item group start information</param>
		/// <param name="callback">A callback to execute</param>
		/// <param name="state">
		/// The context object of the work item. Used for passing arguments to the work item. 
		/// </param>
		/// <param name="postExecuteWorkItemCallback">
		/// A delegate to call after the callback completion
		/// </param>
		/// <param name="workItemPriority">The work item priority</param>
		/// <returns>Returns a work item</returns>
		public static WorkItem CreateWorkItem(
			IWorkItemsGroup workItemsGroup,
			WIGStartInfo wigStartInfo,
			WorkItemCallback callback, 
			object state,
			PostExecuteWorkItemCallback postExecuteWorkItemCallback,
			WorkItemPriority workItemPriority)
		{
			ValidateCallback(callback);
			ValidateCallback(postExecuteWorkItemCallback);

			WorkItemInfo workItemInfo = new WorkItemInfo();
			workItemInfo.UseCallerCallContext = wigStartInfo.UseCallerCallContext;
			workItemInfo.UseCallerHttpContext = wigStartInfo.UseCallerHttpContext;
			workItemInfo.PostExecuteWorkItemCallback = postExecuteWorkItemCallback;
			workItemInfo.CallToPostExecute = wigStartInfo.CallToPostExecute;
			workItemInfo.DisposeOfStateObjects = wigStartInfo.DisposeOfStateObjects;
			workItemInfo.WorkItemPriority = workItemPriority;

			WorkItem workItem = new WorkItem(
				workItemsGroup,
				workItemInfo,
				callback, 
				state);

			return workItem;
		}

		/// <summary>
		/// Create a new work item
		/// </summary>
        /// <param name="workItemsGroup">The work items group</param>
		/// <param name="wigStartInfo">Work item group start information</param>
		/// <param name="callback">A callback to execute</param>
		/// <param name="state">
		/// The context object of the work item. Used for passing arguments to the work item. 
		/// </param>
		/// <param name="postExecuteWorkItemCallback">
		/// A delegate to call after the callback completion
		/// </param>
		/// <param name="callToPostExecute">Indicates on which cases to call to the post execute callback</param>
		/// <returns>Returns a work item</returns>
		public static WorkItem CreateWorkItem(
			IWorkItemsGroup workItemsGroup,
			WIGStartInfo wigStartInfo,
			WorkItemCallback callback, 
			object state,
			PostExecuteWorkItemCallback postExecuteWorkItemCallback,
			CallToPostExecute callToPostExecute)
		{
			ValidateCallback(callback);
			ValidateCallback(postExecuteWorkItemCallback);

			WorkItemInfo workItemInfo = new WorkItemInfo();
			workItemInfo.UseCallerCallContext = wigStartInfo.UseCallerCallContext;
			workItemInfo.UseCallerHttpContext = wigStartInfo.UseCallerHttpContext;
			workItemInfo.PostExecuteWorkItemCallback = postExecuteWorkItemCallback;
			workItemInfo.CallToPostExecute = callToPostExecute;
			workItemInfo.DisposeOfStateObjects = wigStartInfo.DisposeOfStateObjects;
            workItemInfo.WorkItemPriority = wigStartInfo.WorkItemPriority;

			WorkItem workItem = new WorkItem(
				workItemsGroup,
				workItemInfo,
				callback, 
				state);

			return workItem;
		}

		/// <summary>
		/// Create a new work item
		/// </summary>
        /// <param name="workItemsGroup">The work items group</param>
		/// <param name="wigStartInfo">Work item group start information</param>
		/// <param name="callback">A callback to execute</param>
		/// <param name="state">
		/// The context object of the work item. Used for passing arguments to the work item. 
		/// </param>
		/// <param name="postExecuteWorkItemCallback">
		/// A delegate to call after the callback completion
		/// </param>
		/// <param name="callToPostExecute">Indicates on which cases to call to the post execute callback</param>
		/// <param name="workItemPriority">The work item priority</param>
		/// <returns>Returns a work item</returns>
		public static WorkItem CreateWorkItem(
			IWorkItemsGroup workItemsGroup,
			WIGStartInfo wigStartInfo,
			WorkItemCallback callback, 
			object state,
			PostExecuteWorkItemCallback postExecuteWorkItemCallback,
			CallToPostExecute callToPostExecute,
			WorkItemPriority workItemPriority)
		{

			ValidateCallback(callback);
			ValidateCallback(postExecuteWorkItemCallback);

			WorkItemInfo workItemInfo = new WorkItemInfo();
			workItemInfo.UseCallerCallContext = wigStartInfo.UseCallerCallContext;
			workItemInfo.UseCallerHttpContext = wigStartInfo.UseCallerHttpContext;
			workItemInfo.PostExecuteWorkItemCallback = postExecuteWorkItemCallback;
			workItemInfo.CallToPostExecute = callToPostExecute;
			workItemInfo.WorkItemPriority = workItemPriority;
			workItemInfo.DisposeOfStateObjects = wigStartInfo.DisposeOfStateObjects;

			WorkItem workItem = new WorkItem(
				workItemsGroup,
				workItemInfo,
				callback, 
				state);
			
			return workItem;
		}

		private static void ValidateCallback(Delegate callback)
		{
            if (callback != null && callback.GetInvocationList().Length > 1)
			{
				throw new NotSupportedException("SmartThreadPool doesn't support delegates chains");
			}
		}
	}

	#endregion
}
