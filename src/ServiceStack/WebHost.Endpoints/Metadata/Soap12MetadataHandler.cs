using System;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
	public class Soap12MetadataHandler : BaseSoapMetadataHandler
	{
        public override Format Format { get { return Format.Soap12; } }

		protected override string CreateMessage(Type dtoType)
		{
			var requestObj = ReflectionUtils.PopulateObject(Activator.CreateInstance(dtoType));
			var xml = DataContractSerializer.Instance.Parse(requestObj, true);
			var soapEnvelope = string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
    <soap12:Body>

{0}

    </soap12:Body>
</soap12:Envelope>", xml);
			return soapEnvelope;
		}

        protected override void RenderOperation(System.Web.UI.HtmlTextWriter writer, IHttpRequest httpReq, string operationName, string requestMessage, string responseMessage, string metadataHtml)
        {
            var operationControl = new Soap12OperationControl
            {
                HttpRequest = httpReq,
                MetadataConfig = EndpointHost.Config.ServiceEndpointsMetadataConfig,
                Title = EndpointHost.Config.ServiceName,
                Format = this.Format,
                OperationName = operationName,
                HostName = httpReq.GetUrlHostName(),
                RequestMessage = requestMessage,
                ResponseMessage = responseMessage,
                MetadataHtml = metadataHtml,
            };
            if (!this.ContentType.IsNullOrEmpty())
            {
                operationControl.ContentType = this.ContentType;
            }
            if (!this.ContentFormat.IsNullOrEmpty())
            {
                operationControl.ContentFormat = this.ContentFormat;
            }

            operationControl.Render(writer);
        }
	}
}