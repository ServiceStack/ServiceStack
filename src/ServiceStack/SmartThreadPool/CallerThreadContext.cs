
#if !(_WINDOWS_CE) && !(_SILVERLIGHT) && !(WINDOWS_PHONE)

using System;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Web;
using System.Runtime.Remoting.Messaging;


namespace Amib.Threading.Internal
{
#region CallerThreadContext class

	/// <summary>
	/// This class stores the caller call context in order to restore
	/// it when the work item is executed in the thread pool environment. 
	/// </summary>
	internal class CallerThreadContext 
	{
#region Prepare reflection information

		// Cached type information.
		private static readonly MethodInfo getLogicalCallContextMethodInfo =
			typeof(Thread).GetMethod("GetLogicalCallContext", BindingFlags.Instance | BindingFlags.NonPublic);

		private static readonly MethodInfo setLogicalCallContextMethodInfo =
			typeof(Thread).GetMethod("SetLogicalCallContext", BindingFlags.Instance | BindingFlags.NonPublic);

		private static string HttpContextSlotName = GetHttpContextSlotName();

		private static string GetHttpContextSlotName()
		{
			FieldInfo fi = typeof(HttpContext).GetField("CallContextSlotName", BindingFlags.Static | BindingFlags.NonPublic);

            if (fi != null)
            {
                return (string) fi.GetValue(null);
            }

		    return "HttpContext";
		}

        #endregion

#region Private fields

		private HttpContext _httpContext;
		private LogicalCallContext _callContext;

        #endregion

		/// <summary>
		/// Constructor
		/// </summary>
		private CallerThreadContext()
		{
		}

		public bool CapturedCallContext
		{
			get
			{
				return (null != _callContext);
			}
		}

		public bool CapturedHttpContext
		{
			get
			{
				return (null != _httpContext);
			}
		}

		/// <summary>
		/// Captures the current thread context
		/// </summary>
		/// <returns></returns>
		public static CallerThreadContext Capture(
			bool captureCallContext, 
			bool captureHttpContext)
		{
			Debug.Assert(captureCallContext || captureHttpContext);

			CallerThreadContext callerThreadContext = new CallerThreadContext();

			// TODO: In NET 2.0, redo using the new feature of ExecutionContext class - Capture()
			// Capture Call Context
			if(captureCallContext && (getLogicalCallContextMethodInfo != null))
			{
				callerThreadContext._callContext = (LogicalCallContext)getLogicalCallContextMethodInfo.Invoke(Thread.CurrentThread, null);
				if (callerThreadContext._callContext != null)
				{
					callerThreadContext._callContext = (LogicalCallContext)callerThreadContext._callContext.Clone();
				}
			}

			// Capture httpContext
			if (captureHttpContext && (null != HttpContext.Current))
			{
				callerThreadContext._httpContext = HttpContext.Current;
			}

			return callerThreadContext;
		}

		/// <summary>
		/// Applies the thread context stored earlier
		/// </summary>
		/// <param name="callerThreadContext"></param>
		public static void Apply(CallerThreadContext callerThreadContext)
		{
			if (null == callerThreadContext) 
			{
				throw new ArgumentNullException("callerThreadContext");			
			}

			// Todo: In NET 2.0, redo using the new feature of ExecutionContext class - Run()
			// Restore call context
			if ((callerThreadContext._callContext != null) && (setLogicalCallContextMethodInfo != null))
			{
				setLogicalCallContextMethodInfo.Invoke(Thread.CurrentThread, new object[] { callerThreadContext._callContext });
			}

			// Restore HttpContext 
			if (callerThreadContext._httpContext != null)
			{
                HttpContext.Current = callerThreadContext._httpContext;
				//CallContext.SetData(HttpContextSlotName, callerThreadContext._httpContext);
			}
		}
	}

    #endregion
}
#endif
