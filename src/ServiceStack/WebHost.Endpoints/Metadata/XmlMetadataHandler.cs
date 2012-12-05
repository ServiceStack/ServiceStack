using System;
using System.Web.UI;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
	public class XmlMetadataHandler : BaseMetadataHandler
	{
        public override Format Format { get { return Format.Xml; } }

		protected override string CreateMessage(Type dtoType)
		{
			var requestObj = ReflectionUtils.PopulateObject(Activator.CreateInstance(dtoType));
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