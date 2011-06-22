using System;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Xml.Schema;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Support.Metadata;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
    public abstract class BaseSoapMetadataHandler : BaseMetadataHandler, IServiceStackHttpHandler
    {
		protected BaseSoapMetadataHandler()
		{
			OperationName = GetType().Name.Replace("Handler", "");
		}
		
		public string OperationName { get; set; }
    	
    	public override void Execute(HttpContext context)
    	{
			ProcessRequest(
				new HttpRequestWrapper(OperationName, context.Request),
				new HttpResponseWrapper(context.Response), 
				OperationName);
    	}

		public new void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
    	{
			EndpointHost.Config.AssertFeatures(Feature.Metadata);

			var operations = EndpointHost.ServiceOperations;

    		if (httpReq.QueryString["xsd"] != null)
    		{
				var xsdNo = Convert.ToInt32(httpReq.QueryString["xsd"]);
    			var schemaSet = XsdUtils.GetXmlSchemaSet(operations.AllOperations.Types);
    			var schemas = schemaSet.Schemas();
    			var i = 0;
    			if (xsdNo >= schemas.Count)
    			{
    				throw new ArgumentOutOfRangeException("xsd");
    			}
    			httpRes.ContentType = "text/xml";
    			foreach (XmlSchema schema in schemaSet.Schemas())
    			{
    				if (xsdNo != i++) continue;
    				schema.Write(httpRes.OutputStream);
    				break;
    			}
    			return;
    		}

			using (var sw = new StreamWriter(httpRes.OutputStream))
			{
				var writer = new HtmlTextWriter(sw);
				httpRes.ContentType = "text/html";
				ProcessOperations(writer, httpReq);
			}
    	}

    	protected override void RenderOperations(HtmlTextWriter writer, IHttpRequest httpReq, Operations allOperations)
    	{
			var defaultPage = new IndexOperationsControl {
				HttpRequest = httpReq,
				MetadataConfig = EndpointHost.Config.ServiceEndpointsMetadataConfig,
				Title = EndpointHost.Config.ServiceName,
				Xsds = XsdTypes.Xsds,
				XsdServiceTypesIndex = 1,
				OperationNames = allOperations.Names,
				MetadataPageBodyHtml = EndpointHost.Config.MetadataPageBodyHtml,
			};

			defaultPage.RenderControl(writer);
		}

    }
}