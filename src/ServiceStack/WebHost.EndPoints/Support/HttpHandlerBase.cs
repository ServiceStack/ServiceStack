using System;
using System.Web;
using ServiceStack.Logging;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public abstract class HttpHandlerBase : IHttpHandler
	{
		private readonly ILog log;

		protected HttpHandlerBase()
		{
			this.log = LogManager.GetLogger(this.GetType());
		}

		public void ProcessRequest(HttpContext context)
		{
			var before = DateTime.Now;
			Execute(context);
			var elapsed = DateTime.Now - before;
			log.DebugFormat("'{0}' was completed in {1}ms", this.GetType().Name, elapsed.TotalMilliseconds);
		}

		public abstract void Execute(HttpContext context);

		public bool IsReusable
		{
			get { return false; }
		}
	}
}