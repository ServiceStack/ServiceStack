using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using ServiceStack.Common.Host.Support.Endpoints;
using ServiceStack.Common.Host.Support.Endpoints.Controls;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Common.Services.Utils;

namespace ServiceStack.Common.Host.Endpoints
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