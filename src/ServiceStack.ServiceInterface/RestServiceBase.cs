using System;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
	public abstract class RestServiceBase<TRequest>
		: ServiceBase<TRequest>,
		IRestGetService<TRequest>,
		IRestPutService<TRequest>,
		IRestPostService<TRequest>,
		IRestDeleteService<TRequest>
	{
		protected override object Run(TRequest request)
		{
			throw new NotImplementedException("This base method should be overridden but not called");
		}

		public virtual object OnGet(TRequest request)
		{
			throw new NotImplementedException("This base method should be overridden but not called");
		}

		public object Get(TRequest request)
		{
			try
			{
				return OnGet(request);
			}
			catch (Exception ex)
			{
				return HandleException(request, ex);
			}
		}

		public virtual object OnPut(TRequest request)
		{
			throw new NotImplementedException("This base method should be overridden but not called");
		}

		public object Put(TRequest request)
		{
			try
			{
				return OnPut(request);
			}
			catch (Exception ex)
			{
				return HandleException(request, ex);
			}
		}

		public virtual object OnPost(TRequest request)
		{
			throw new NotImplementedException("This base method should be overridden but not called");
		}

		public object Post(TRequest request)
		{
			try
			{
				return OnPost(request);
			}
			catch (Exception ex)
			{
				return HandleException(request, ex);
			}
		}

		public virtual object OnDelete(TRequest request)
		{
			throw new NotImplementedException("This base method should be overridden but not called");
		}

		public object Delete(TRequest request)
		{
			try
			{
				return OnDelete(request);
			}
			catch (Exception ex)
			{
				return HandleException(request, ex);
			}
		}
	}
}