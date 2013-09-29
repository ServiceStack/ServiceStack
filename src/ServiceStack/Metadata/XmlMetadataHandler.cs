using System;
using System.Web.UI;
using ServiceStack.Host;
using ServiceStack.Serialization;
using ServiceStack.Utils;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Metadata
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
				Title = HostContext.ServiceName,
                OperationNames = metadata.GetOperationNamesForMetadata(httpReq, Format),
				MetadataOperationPageBodyHtml = HostContext.Config.MetadataOperationPageBodyHtml,
			};

			defaultPage.RenderControl(writer);
		}
	}
}