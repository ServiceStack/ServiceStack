using System;
using System.Threading.Tasks;
using ServiceStack.Web;
using ServiceStack.Text;

namespace ServiceStack.Metadata;

public abstract class BaseSoapMetadataHandler : BaseMetadataHandler
{
    protected BaseSoapMetadataHandler()
    {
        OperationName = GetType().GetOperationName().Replace("Handler", "");
    }

    public string OperationName { get; set; }

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
            return;

        if (!AssertAccess(httpReq, httpRes, httpReq.QueryString["op"])) 
            return;

        httpRes.ApplyGlobalResponseHeaders();
        if (httpReq.QueryString["xsd"] != null)
        {
#if !NETCORE
                var operationTypes = HostContext.Metadata.GetAllSoapOperationTypes();
                var xsdNo = Convert.ToInt32(httpReq.QueryString["xsd"]);
                var schemaSet = XsdUtils.GetXmlSchemaSet(operationTypes);
                var schemas = schemaSet.Schemas();
                var i = 0;
                if (xsdNo >= schemas.Count)
                    throw new ArgumentOutOfRangeException("xsd");

                httpRes.ContentType = "text/xml";
                foreach (System.Xml.Schema.XmlSchema schema in schemaSet.Schemas())
                {
                    if (xsdNo != i++) continue;
                    schema.Write(httpRes.AllowSyncIO().OutputStream);
                    break;
                }
#endif
        }
        else
        {
            httpRes.ContentType = "text/html; charset=utf-8";
            await ProcessOperationsAsync(httpRes.OutputStream, httpReq, httpRes).ConfigAwait();
        }

        await httpRes.EndHttpHandlerRequestAsync(skipHeaders:true).ConfigAwait();
    }

}