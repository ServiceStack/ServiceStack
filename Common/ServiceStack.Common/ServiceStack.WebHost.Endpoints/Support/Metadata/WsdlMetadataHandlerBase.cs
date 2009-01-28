using System;
using System.Web;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support.Templates;

namespace ServiceStack.WebHost.Endpoints.Support.Metadata
{
	public abstract class WsdlMetadataHandlerBase : HttpHandlerBase
	{
		protected abstract WsdlTemplateBase GetWsdlTemplate();

		public override void Execute(HttpContext context)
		{
			context.Response.ContentType = "text/xml";
			var operations = EndpointHost.ServiceOperations;

			var baseUri = GetBaseUri(context.Request.Url);
			var optimizeForFlash = context.Request.QueryString["flash"] != null;
			var includeAllTypesInAssembly = context.Request.QueryString["includeAllTypes"] != null;

			var wsdlTemplate = GetWsdlTemplate(operations, baseUri, optimizeForFlash, includeAllTypesInAssembly);

			context.Response.Write(wsdlTemplate.ToString());
		}

		public WsdlTemplateBase GetWsdlTemplate(ServiceOperations operations, string baseUri, bool optimizeForFlash, bool includeAllTypesInAssembly)
		{
			var xsd = new XsdGenerator {
				OperationTypes = operations.AllOperations.Types,
				OptimizeForFlash = optimizeForFlash,
				IncludeAllTypesInAssembly = includeAllTypesInAssembly,
			}.ToString();

			var wsdlTemplate = GetWsdlTemplate();
			wsdlTemplate.Xsd = xsd;
			wsdlTemplate.ReplyOperationNames = operations.ReplyOperations.Names;
			wsdlTemplate.OneWayOperationNames = operations.OneWayOperations.Names;
			wsdlTemplate.ReplyEndpointUri = baseUri + "SyncReply.svc";
			wsdlTemplate.OneWayEndpointUri = baseUri + "AsyncOneWay.svc";
			return wsdlTemplate;
		}

		public static string GetBaseUri(Uri requestUrl)
		{
			var appPath = requestUrl.AbsolutePath;
			var endpointsPath = appPath.Substring(0, appPath.LastIndexOf('/') + 1);
			return requestUrl.GetLeftPart(UriPartial.Authority) + endpointsPath;
		}
	}
}