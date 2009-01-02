using System;
using ServiceStack.LogicFacade;

namespace ServiceStack.ServiceInterface
{
	public class CallContext : ICallContext
	{
		public CallContext(OperationContext operation, RequestContext request)
		{
			this.Operation = operation;
			this.Request = request;
		}

		public IOperationContext Operation { get; set; }

		public IRequestContext Request { get; set; }

		public void Dispose()
		{
			if (this.Request != null)
			{
				this.Request.Dispose();
			}
		}
	}
}