using System;
using System.Threading.Tasks;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Plugins.Tasks
{
	/// <summary>
	/// Process a Task<object> response
	/// </summary>
	public class AsyncTaskResult : AsyncResponseResult
	{
		private readonly Task<object> _task;

		public AsyncTaskResult(Task<object> task) : base(task)
		{
			_task = task;
		}

		public static Type AsyncResultType {
			get { return typeof (Task<object>); }
		}

		public override void Process(Action<object> next)
		{
			_task.ContinueWith(t => next(t.Result));
		}
	}
}