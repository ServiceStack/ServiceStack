using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using ServiceStack.WebHost.Endpoints.Support.Endpoints;
using ServiceStack.WebHost.Endpoints.Support.Endpoints.Controls;

namespace ServiceStack.WebHost.Endpoints.Endpoints
{
    public abstract class BaseMetadataHandler : IHttpHandler
    {
        const string RESPONSE_SUFFIX = "Response";

        public Type ServiceOperationType { get; set; }
        public string ServiceName { get; set; }
		public string UsageExamplesBaseUri { get; set; }
		public abstract string EndpointType { get; }
		protected HttpRequest Request { get; set; }

        public virtual void ProcessRequest(HttpContext context)
        {
            Request = context.Request;
            var writer = new HtmlTextWriter(context.Response.Output);
            context.Response.ContentType = "text/html";

			ProcessOperations(writer);
        }

		protected virtual void ProcessOperations(HtmlTextWriter writer)
		{
			var operations = new ServiceOperations(ServiceOperationType,
				OperationVerbs.ReplyOperationVerbs, OperationVerbs.OneWayOperationVerbs);
			var operationName = Request.QueryString["op"];
			if (operationName != null)
			{
				var allTypes = operations.AllOperations.Types;
				var operationType = allTypes.Single(x => x.Name == operationName);
				var requestMessage = CreateMessage(operationType);
				string responseMessage = null;
				if (allTypes.Any(x => x.Name == operationName + RESPONSE_SUFFIX))
				{
					var operationResponseType = allTypes.Single(x => x.Name == operationName + RESPONSE_SUFFIX);
					responseMessage = CreateMessage(operationResponseType);
				}
				RenderOperation(writer, operationName, requestMessage, responseMessage);
				return;
			}

			RenderOperations(writer, operations.AllOperations);
		}

        protected abstract string CreateMessage(Type dtoType);

		protected virtual void RenderOperation(HtmlTextWriter writer, string operationName, 
			string requestMessage, string responseMessage)
		{
			var operationControl = new OperationControl {
				Title = this.ServiceName,
                EndpointType = this.EndpointType,
				OperationName = operationName,
				HostName = Request.Url.Host,
				RequestMessage = requestMessage,
				ResponseMessage = responseMessage,
			};
			operationControl.RenderControl(writer);
		}

        protected abstract void RenderOperations(HtmlTextWriter writer, Operations allOperations);

        public bool IsReusable
        {
            get { return false; }
        }
    }
}