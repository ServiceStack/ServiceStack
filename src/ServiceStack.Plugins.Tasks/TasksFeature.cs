using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.ServiceHost;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ServiceStack.Plugins.Tasks
{
	public delegate object HandleTaskException(object service, object request, Exception ex);
	public delegate object GetTaskResult(object service, object request, object response);

	public class TasksFeature : IPlugin
	{
		public void Register(IAppHost appHost)
		{
			AsyncResultFactory.RegisterAsyncResult(AsyncTaskResult.AsyncResultType, (task)=> new AsyncTaskResult(task as Task<object>));
		}
	}
}
