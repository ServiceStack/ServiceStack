using System;

namespace ServiceStack.ServiceInterface
{
	public class CallContext : IDisposable
	{
		public CallContext(OperationContext operation, RequestContext request)
		{
			this.Operation = operation;
			this.Request = request;
		}

		public OperationContext Operation { get; set; }

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