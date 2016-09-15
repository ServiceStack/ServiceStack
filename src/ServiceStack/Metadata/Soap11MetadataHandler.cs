#if !NETSTANDARD1_6

using System;
using ServiceStack.Serialization;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public class Soap11MetadataHandler : BaseSoapMetadataHandler
    {
        public override Format Format => Format.Soap11;

        protected override string CreateMessage(Type dtoType)
        {
            var requestObj = AutoMappingUtils.PopulateWith(Activator.CreateInstance(dtoType));
            var xml = DataContractSerializer.Instance.Parse(requestObj, true);
            var soapEnvelope =
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>

{xml}

    </soap:Body>
</soap:Envelope>";
            return soapEnvelope;
        }

        protected override void RenderOperation(System.Web.UI.HtmlTextWriter writer, IRequest httpReq, string operationName, string requestMessage, string responseMessage, string metadataHtml)
        {
            var operationControl = new Soap11OperationControl
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
