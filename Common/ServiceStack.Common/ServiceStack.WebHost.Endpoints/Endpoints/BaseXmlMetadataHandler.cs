using System;
using System.Web.UI;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Support.Endpoints.Controls;

namespace ServiceStack.WebHost.Endpoints.Endpoints
{
	public abstract class BaseXmlMetadataHandler : BaseMetadataHandler
	{
		public override string EndpointType { get { return "XML"; } }

		protected override string CreateMessage(Type dtoType)
		{
			var requestObj = ReflectionUtils.PopulateObject(Activator.CreateInstance(dtoType));
			return DataContractSerializer.Instance.Parse(requestObj, true);
		}

		protected override void RenderOperations(HtmlTextWriter writer, Operations allOperations)
		{
			var defaultPage = new OperationsControl {
				Title = this.ServiceName,
				OperationNames = allOperations.Names,
				UsageExamplesBaseUri = this.UsageExamplesBaseUri,
			};

			defaultPage.RenderControl(writer);
		}
	}
}