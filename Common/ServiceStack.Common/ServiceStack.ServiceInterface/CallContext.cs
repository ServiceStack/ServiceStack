using System;

namespace ServiceStack.ServiceInterface
{
	public class CallContext : IDisposable
	{
		public CallContext(AppContext app, RequestContext request)
		{
			this.App = app;
			this.Request = request;
		}

		public AppContext App { get; set; }

		public RequestContext Request { get; set; }

		public void Dispose()
		{
			if (this.Request != null)
			{
				this.Request.Dispose();
			}
		}
	}
}