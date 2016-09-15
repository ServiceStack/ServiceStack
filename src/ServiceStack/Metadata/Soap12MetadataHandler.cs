#if !NETSTANDARD1_6

using System;
using ServiceStack.Serialization;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public class Soap12MetadataHandler : BaseSoapMetadataHandler
    {
        public override Format Format => Format.Soap12;

        protected override string CreateMessage(Type dtoType)
        {
            var requestObj = AutoMappingUtils.PopulateWith(Activator.CreateInstance(dtoType));
            var xml = DataContractSerializer.Instance.Parse(requestObj, true);
            var soapEnvelope =
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
    <soap12:Body>

{xml}

    </soap12:Body>
</soap12:Envelope>";
            return soapEnvelope;
        }

        protected override void RenderOperation(System.Web.UI.HtmlTextWriter writer, IRequest httpReq, string operationName, string requestMessage, string responseMessage, string metadataHtml)
        {
            var operationControl = new Soap12OperationControl
            {
                HttpRequest = httpReq,
                MetadataConfig = HostContext.Config.ServiceEndpointsMetadataConfig,
                Title = HostContext.ServiceName,
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

#endif