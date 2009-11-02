using System;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
	public class BasicOperationContext<TApplicationContext, TRequestContext> 
		: IOperationContext
		where TApplicationContext : IApplicationContext
		where TRequestContext : IRequestContext
	{
		public BasicOperationContext(TApplicationContext application, TRequestContext request)
		{
			this.Application = application;
			this.Request = request;
		}

		public TApplicationContext Application { get; set; }

		public TRequestContext Request { get; set; }

		IApplicationContext IOperationContext.Application
		{
			get { return this.Application; }
		}

		IRequestContext IOperationContext.Request
		{
			get { return this.Request; }
		}

		public void Dispose()
		{
			if (!Equals(this.Request, default(TRequestContext)))
			{
				this.Request.Dispose();
			}
		}
	}
}