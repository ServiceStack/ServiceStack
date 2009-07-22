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

		protected IServiceClient ServiceClient;
		protected TAppContext appContext;

		public ContextBase(IServiceClient serviceClient, TAppContext appContext)
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

		protected TResponse Send<TResponse>(object request, Func<TResponse, ResponseStatus> chechResponseStatus)
		{
			var response = ServiceClient.Send<TResponse>(request);

			AssertSuccessResponse(chechResponseStatus(response));

			return response;
		}

		private static void AssertSuccessResponse(ResponseStatus responseStatus)
		{
			if (!responseStatus.IsSuccess)
			{
				throw new SilverlightStackException(responseStatus.ErrorCode);
			}
		}

		protected void InvokeAsync(Action action)
		{
			ThreadPool.QueueUserWorkItem(delegate {
				action();
			});

			//asyncAction.BeginInvoke is not supported in Silverlight

			//var asyncAction = (Action)delegate {
			//    try
			//    {
			//        action();
			//    }
			//    catch (Exception ex)
			//    {
			//        log.Error(string.Format("Error executing async command '{0}': {1}", action.Method.Name, ex.Message), ex);
			//    }
			//};

			//asyncAction.BeginInvoke(null, null);
		}

	}
}