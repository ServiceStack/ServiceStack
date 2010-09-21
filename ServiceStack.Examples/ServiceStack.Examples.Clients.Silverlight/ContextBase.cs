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
			Func<TResponse, ResponseStatus> chechResponseStatus,
			Action<TResponse> callback)
		{
			ServiceClient.Send<TResponse>(request, response =>
				{
					AssertSuccessResponse(chechResponseStatus(response));
					callback(response);
				});
		}

		private static void AssertSuccessResponse(ResponseStatus responseStatus)
		{
			if (!responseStatus.IsSuccess)
			{
				throw new SilverlightStackException(responseStatus.ErrorCode);
			}
		}
	}
}