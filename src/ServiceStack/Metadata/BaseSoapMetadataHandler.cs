using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.UI;
using System.Xml.Schema;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public abstract class BaseSoapMetadataHandler : BaseMetadataHandler
    {
        protected BaseSoapMetadataHandler()
        {
            OperationName = GetType().GetOperationName().Replace("Handler", "");
        }

        public string OperationName { get; set; }

        public override Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return TypeConstants.EmptyTask;

            if (!AssertAccess(httpReq, httpRes, httpReq.QueryString["op"])) 
                return TypeConstants.EmptyTask;


            if (httpReq.QueryString["xsd"] != null)
            {
#if !NETSTANDARD2_0
                var operationTypes = HostContext.Metadata.GetAllSoapOperationTypes();
                var xsdNo = Convert.ToInt32(httpReq.QueryString["xsd"]);
                var schemaSet = XsdUtils.GetXmlSchemaSet(operationTypes);
                var schemas = schemaSet.Schemas();
                var i = 0;
                if (xsdNo >= schemas.Count)
                    throw new ArgumentOutOfRangeException("xsd");

                httpRes.ContentType = "text/xml";
                foreach (XmlSchema schema in schemaSet.Schemas())
                {
                    if (xsdNo != i++) continue;
                    schema.Write(httpRes.OutputStream);
                    break;
                }
#endif
            }
            else
            {
                using (var sw = new StreamWriter(httpRes.OutputStream))
                {
                    var writer = new HtmlTextWriter(sw);
                    httpRes.ContentType = "text/html; charset=utf-8";
                    ProcessOperations(writer, httpReq, httpRes);
                }
            }

            httpRes.EndHttpHandlerRequest(skipHeaders:true);

            return TypeConstants.EmptyTask;
        }

    }
}