namespace ServiceStack.Metadata
{
    public class Soap12WsdlMetadataHandler : WsdlMetadataHandlerBase
    {
        protected override WsdlTemplateBase GetWsdlTemplate()
        {
            return new Soap12WsdlTemplate();
        }
    }
}