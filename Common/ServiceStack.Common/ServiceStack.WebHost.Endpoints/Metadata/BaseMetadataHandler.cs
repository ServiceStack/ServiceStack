using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
	public abstract class BaseMetadataHandler : HttpHandlerBase
    {
        const string RESPONSE_SUFFIX = "Response";

		public abstract EndpointType EndpointType { get; }
		protected HttpRequest Request { get; set; }

        public override void Execute(HttpContext context)
        {
            this.Request = context.Request;
            var writer = new HtmlTextWriter(context.Response.Output);
            context.Response.ContentType = "text/html";

			ProcessOperations(writer);
        }

		protected virtual void ProcessOperations(HtmlTextWriter writer)
		{
			var operations = new ServiceOperations(EndpointHost.ServiceModelAssembly, EndpointHost.Config.OperationsNamespace);
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
				MetadataConfig = EndpointHost.Config.ServiceEndpointsMetadataConfig,
				Title = EndpointHost.Config.ServiceName,
                EndpointType = this.EndpointType,
				OperationName = operationName,
				HostName = Request.Url.Host,
				RequestMessage = requestMessage,
				ResponseMessage = responseMessage,
			};
			operationControl.Render(writer);
		}

        protected abstract void RenderOperations(HtmlTextWriter writer, Operations allOperations);
    }
}