using System;
using System.Web.UI;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Support.Endpoints.Controls;

namespace ServiceStack.WebHost.Endpoints.Endpoints
{
    public abstract class BaseJsonMetadataHandler : BaseMetadataHandler
    {
		public override string EndpointType { get { return "JSON"; } }
		
		protected override string CreateMessage(Type dtoType)
        {
            var requestObj = ReflectionUtils.PopulateObject(Activator.CreateInstance(dtoType));
            return JsonDataContractSerializer.Instance.Parse(requestObj);
        }

        protected override void RenderOperations(HtmlTextWriter writer, Operations allOperations)
        {
            var defaultPage = new OperationsControl
            {
                Title = this.ServiceName,
                OperationNames = allOperations.Names,
                UsageExamplesBaseUri = this.UsageExamplesBaseUri,
            };

            defaultPage.RenderControl(writer);
        }

    }
}