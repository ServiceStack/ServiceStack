#if !NETSTANDARD1_6

namespace ServiceStack.Metadata
{
    public class Soap12WsdlMetadataHandler : WsdlMetadataHandlerBase
    {
        protected override WsdlTemplateBase GetWsdlTemplate() => new Soap12WsdlTemplate();
    }
}

#endif
