using System;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support.Templates;

namespace ServiceStack.WebHost.Endpoints.Support.Metadata
{
	public abstract class WsdlMetadataHandlerBase : HttpHandlerBase
	{
		protected abstract WsdlTemplateBase GetWsdlTemplate();

        public void Execute(IHttpRequest httpReq, IHttpResponse httpRes)
        {
            httpRes.ContentType = "text/xml";

            var baseUri = httpReq.AbsoluteUri; // .GetParentBaseUrl();
            var optimizeForFlash = httpReq.QueryString["flash"] != null;
            var includeAllTypesInAssembly = httpReq.QueryString["includeAllTypes"] != null;
            var operations = includeAllTypesInAssembly ? EndpointHost.AllServiceOperations : EndpointHost.ServiceOperations;

            var wsdlTemplate = GetWsdlTemplate(operations, baseUri, optimizeForFlash, includeAllTypesInAssembly, httpReq.GetPathUrl());

            httpRes.Write(wsdlTemplate.ToString());
        }

		public override void Execute(HttpContext context)
		{
			EndpointHost.Config.AssertFeatures(Feature.Metadata);

			context.Response.ContentType = "text/xml";

			var baseUri = context.Request.GetParentBaseUrl();
			var optimizeForFlash = context.Request.QueryString["flash"] != null;
			var includeAllTypesInAssembly = context.Request.QueryString["includeAllTypes"] != null;
			var operations = includeAllTypesInAssembly ? EndpointHost.AllServiceOperations : EndpointHost.ServiceOperations;
			
			var wsdlTemplate = GetWsdlTemplate(operations, baseUri, optimizeForFlash, includeAllTypesInAssembly, context.Request.GetBaseUrl());

			context.Response.Write(wsdlTemplate.ToString());
		}

        

		public WsdlTemplateBase GetWsdlTemplate(ServiceOperations operations, string baseUri, bool optimizeForFlash, bool includeAllTypesInAssembly, string rawUrl)
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

			if (rawUrl.ToLower().StartsWith(baseUri))
			{
				wsdlTemplate.ReplyEndpointUri = rawUrl;
				wsdlTemplate.OneWayEndpointUri = rawUrl;
			}
			else
			{
				wsdlTemplate.ReplyEndpointUri = baseUri + "SyncReply.svc";
				wsdlTemplate.OneWayEndpointUri = baseUri + "AsyncOneWay.svc";
			}

			return wsdlTemplate;
		}
	}
}