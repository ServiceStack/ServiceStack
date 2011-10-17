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
		IRestPatchService<TRequest>
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
				OnBeforeExecute(request);
				return OnGet(request);
			}
			catch (Exception ex)
			{
				var result = HandleException(request, ex);

				if (result == null) throw;

				return result;
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
				OnBeforeExecute(request);
				return OnPut(request);
			}
			catch (Exception ex)
			{
				var result = HandleException(request, ex);

				if (result == null) throw;

				return result;
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
				OnBeforeExecute(request);
				return OnPost(request);
			}
			catch (Exception ex)
			{
				var result = HandleException(request, ex);

				if (result == null) throw;

				return result;
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
				OnBeforeExecute(request);
				return OnDelete(request);
			}
			catch (Exception ex)
			{
				var result = HandleException(request, ex);

				if (result == null) throw;

				return result;
			}
		}

		public virtual object OnPatch(TRequest request)
		{
			throw new NotImplementedException("This base method should be overridden but not called");
		}

		public object Patch(TRequest request)
		{
			try
			{
				OnBeforeExecute(request);
				return OnPatch(request);
			}
			catch (Exception ex)
			{
				var result = HandleException(request, ex);

				if (result == null) throw;

				return result;
			}
		}
	}
}