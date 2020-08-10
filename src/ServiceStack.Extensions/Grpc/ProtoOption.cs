using ServiceStack.Web;

namespace ServiceStack.Grpc
{
    public delegate string ProtoOptionDelegate(IRequest request, MetadataTypesConfig config);
    
    public static class ProtoOption
    {
        public static string CSharpNamespace(IRequest req, MetadataTypesConfig config) =>
            $"option csharp_namespace = \"{config.GlobalNamespace}\";";

        public static string PhpNamespace(IRequest req, MetadataTypesConfig config) =>
            $"option php_namespace = \"{config.GlobalNamespace}\";";
    }
}