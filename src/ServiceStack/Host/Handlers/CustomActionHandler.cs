using System;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class CustomActionHandler : HttpAsyncTaskHandler
    {
        public Action<IRequest, IResponse> Action { get; set; }

        public CustomActionHandler(Action<IRequest, IResponse> action)
        {
            Action = action ?? throw new NullReferenceException(nameof(action));
            this.RequestName = GetType().Name;
        }

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;

            Action(httpReq, httpRes);
            httpRes.EndHttpHandlerRequest(skipHeaders:true);
        }
    }

    public class CustomActionHandlerAsync : HttpAsyncTaskHandler
    {
        public Func<IRequest, IResponse, Task> Action { get; set; }

        public CustomActionHandlerAsync(Func<IRequest, IResponse, Task> action)
        {
            Action = action ?? throw new NullReferenceException(nameof(action));
            this.RequestName = GetType().Name;
        }

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;

            await Action(httpReq, httpRes);
            httpRes.EndHttpHandlerRequest(skipHeaders: true);
        }
    }
}