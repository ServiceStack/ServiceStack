using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Xml;
using ServiceStack.Common.Host.Support.Endpoints;
using ServiceStack.Common.Host.Support.Endpoints.Controls;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Common.Services.Utils;

namespace ServiceStack.Common.Host.Endpoints
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