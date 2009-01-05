using System;
using ServiceStack.LogicFacade;

namespace ServiceStack.ServiceInterface
{
	public class OperationContext : IOperationContext
	{
		public OperationContext(IApplicationContext application, IRequestContext request)
		{
			this.Application = application;
			this.Request = request;
		}

		public IApplicationContext Application { get; set; }

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