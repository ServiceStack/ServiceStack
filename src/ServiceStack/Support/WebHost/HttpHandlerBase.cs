using System;
using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Logging;

namespace ServiceStack.Support.WebHost
{
	public abstract class HttpHandlerBase : HttpAsyncTaskHandler, IHttpHandler
	{
		private readonly ILog log;

		protected HttpHandlerBase()
		{
			this.log = LogManager.GetLogger(this.GetType());
		}

		public override void ProcessRequest(HttpContextBase context)
		{
			var before = DateTime.UtcNow;
			Execute(context);
            var elapsed = DateTime.UtcNow - before;
			log.DebugFormat("'{0}' was completed in {1}ms", this.GetType().GetOperationName(), elapsed.TotalMilliseconds);
		}

		public abstract void Execute(HttpContextBase context);

		public override bool IsReusable
		{
			get { return false; }
		}
	}
}