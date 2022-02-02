using System;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class CustomResponseHandler : HttpAsyncTaskHandler
    {
        public Func<IRequest, IResponse, object> Action { get; set; }

        public CustomResponseHandler(Func<IRequest, IResponse, object> action, string operationName = null)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            RequestName = operationName ?? "CustomResponse";
        }

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (Action == null)
                throw new Exception("Action was not supplied to ActionHandler");

            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;

            if (httpReq.OperationName == null)
                httpReq.OperationName = RequestName;

            var response = Action(httpReq, httpRes);
            httpRes.WriteToResponse(httpReq, response);
        }
    }

    public class CustomResponseHandlerAsync : HttpAsyncTaskHandler
    {
        public Func<IRequest, IResponse, Task<object>> Action { get; set; }

        public CustomResponseHandlerAsync(Func<IRequest, IResponse, Task<object>> action, string operationName = null)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            RequestName = operationName ?? "CustomResponse";
        }

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (Action == null)
                throw new Exception("Action was not supplied to ActionHandler");

            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;

            if (httpReq.OperationName == null)
                httpReq.OperationName = RequestName;

            var response = await Action(httpReq, httpRes);
            await httpRes.WriteToResponse(httpReq, response);
        }
    }
}