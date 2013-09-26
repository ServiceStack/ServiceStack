using ServiceStack.Support.Metadata;
using ServiceStack.Support.Templates;

namespace ServiceStack.Metadata
{
	public class Soap11WsdlMetadataHandler : WsdlMetadataHandlerBase
	{
		protected override WsdlTemplateBase GetWsdlTemplate()
		{
			return new Soap11WsdlTemplate();
		}
	}
}