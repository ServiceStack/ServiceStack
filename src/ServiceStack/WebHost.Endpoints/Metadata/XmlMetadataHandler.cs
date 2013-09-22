using System;
using System.Web.UI;
using ServiceStack.Serialization;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.Utils;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
	public class XmlMetadataHandler : BaseMetadataHandler
	{
        public override Format Format { get { return Format.Xml; } }

		protected override string CreateMessage(Type dtoType)
		{
			var requestObj = ReflectionUtils.PopulateObject(dtoType.CreateInstance());
			return DataContractSerializer.Instance.Parse(requestObj, true);
		}

		protected override void RenderOperations(HtmlTextWriter writer, IHttpRequest httpReq, ServiceMetadata metadata)
		{
			var defaultPage = new OperationsControl {
				Title = EndpointHost.Config.ServiceName,
                OperationNames = metadata.GetOperationNamesForMetadata(httpReq, Format),
				MetadataOperationPageBodyHtml = EndpointHost.Config.MetadataOperationPageBodyHtml,
			};

			defaultPage.RenderControl(writer);
		}
	}
}