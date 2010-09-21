using System;
using System.Threading;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.Logging;
using ServiceStack.Service;
using SilverlightStack;

namespace ServiceStack.Examples.Clients.Silverlight
{
	public class ContextBase<TAppContext>
	{
		protected ILog log;

		protected IAsyncServiceClient ServiceClient;
		protected TAppContext appContext;

		public ContextBase(IAsyncServiceClient serviceClient, TAppContext appContext)
		{
			this.ServiceClient = serviceClient;
			this.appContext = appContext;
			this.log = LogManager.GetLogger(GetType());
		}

		public event EventHandler<DataEventArgs> DataLoaded;

		protected void OnDataLoaded(DataEventArgs e)
		{
			var loaded = DataLoaded;
			if (loaded != null) loaded(this, e);
		}

		protected void Send<TResponse>(object request,
			Func<TResponse, ResponseStatus> checkResponseStatus,
			Action<TResponse> onSuccess)
		{
			Send(request, checkResponseStatus, onSuccess, null);
		}

		protected void Send<TResponse>(object request,
			Func<TResponse, ResponseStatus> checkResponseStatus,
			Action<TResponse> onSuccess,
			Action<SilverlightStackException> onError)
		{
			ServiceClient.Send<TResponse>(request, response =>
				{
					var responseStatus = checkResponseStatus(response);
					if (responseStatus.IsSuccess)
					{
						onSuccess(response);
					}
					else
					{
						if (onError == null) return;
						onError(new SilverlightStackException(responseStatus.ErrorCode));
					}
				});
		}

	}
}