using System.Collections.Generic;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes
{
    [Route("/types/metadata")]
    public class TypesMetadata : NativeTypesBase { }

    [Route("/types/csharp")]
    public class TypesCSharp : NativeTypesBase { }

    public class NativeTypesBase
    {
        public string BaseUrl { get; set; }
        public bool? MakePartial { get; set; }
        public bool? MakeVirtual { get; set; }
        public bool? AddReturnMarker { get; set; }
        public bool? AddDescriptionAsComments { get; set; }
        public bool? AddDataContractAttributes { get; set; }
        public bool? MakeDataContractsExtensible { get; set; }
        public bool? AddIndexesToDataMembers { get; set; }
        public bool? InitializeCollections { get; set; }
        public int? AddImplicitVersion { get; set; }
        public bool? AddResponseStatus { get; set; }
        public string AddDefaultXmlNamespace { get; set; }
        public List<string> DefaultNamespaces { get; set; }
    }

    public interface INativeTypesMetadata
    {
        MetadataTypesConfig GetConfig(NativeTypesBase req);

        MetadataTypes GetMetadataTypes(IRequest req, MetadataTypesConfig config = null);
    }

    public class NativeTypesService : Service
    {
        public INativeTypesMetadata NativeTypesMetadata { get; set; }

        public MetadataTypes Any(TypesMetadata request)
        {
            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            return metadataTypes;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesCSharp request)
        {
            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            var csharp = new CSharpGenerator(typesConfig).GetCode(metadataTypes);
            return csharp;
        }
    }
}