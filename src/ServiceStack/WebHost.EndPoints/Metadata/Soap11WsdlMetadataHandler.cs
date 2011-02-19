using ServiceStack.WebHost.Endpoints.Support.Metadata;
using ServiceStack.WebHost.Endpoints.Support.Templates;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
	public class Soap11WsdlMetadataHandler : WsdlMetadataHandlerBase
	{
		protected override WsdlTemplateBase GetWsdlTemplate()
		{
			return new Soap11WsdlTemplate();
		}
	}
}