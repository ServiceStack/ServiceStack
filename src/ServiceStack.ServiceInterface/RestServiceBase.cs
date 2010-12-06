using System;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
	public abstract class RestServiceBase<TRequest>
		: ServiceBase<TRequest>,
		IRestGetService<TRequest>,
		IRestPutService<TRequest>,
		IRestPostService<TRequest>,
		IRestDeleteService<TRequest>,
		IRequiresRequestContext 
	{
		public IRequestContext RequestContext { get; set; }

		public virtual object Get(TRequest request)
		{
			throw new NotImplementedException();
		}

		public virtual object Put(TRequest request)
		{
			throw new NotImplementedException();
		}

		public virtual object Post(TRequest request)
		{
			throw new NotImplementedException();
		}

		public virtual object Delete(TRequest request)
		{
			throw new NotImplementedException();
		}
	}
}