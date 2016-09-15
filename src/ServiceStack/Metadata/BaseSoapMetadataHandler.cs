using System;
using System.IO;
using System.Web;
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

#if !NETSTANDARD1_6
        public override void Execute(HttpContextBase context)
        {
            var httpReq = context.ToRequest(OperationName);
            ProcessRequestAsync(httpReq, httpReq.Response, OperationName);
        }
#endif

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;

            if (!AssertAccess(httpReq, httpRes, httpReq.QueryString["op"])) return;

            var operationTypes = HostContext.Metadata.GetAllSoapOperationTypes();

            if (httpReq.QueryString["xsd"] != null)
            {
#if !NETSTANDARD1_6
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
        }

    }
}