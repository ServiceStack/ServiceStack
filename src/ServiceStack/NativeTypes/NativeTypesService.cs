using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.NativeTypes.FSharp;
using ServiceStack.NativeTypes.TypeScript;
using ServiceStack.NativeTypes.VbNet;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes
{
    [Exclude(Feature.Soap)]
    [Route("/types/metadata")]
    public class TypesMetadata : NativeTypesBase { }

    [Exclude(Feature.Soap)]
    [Route("/types/csharp")]
    public class TypesCSharp : NativeTypesBase { }

    [Exclude(Feature.Soap)]
    [Route("/types/fsharp")]
    public class TypesFSharp : NativeTypesBase { }

    [Exclude(Feature.Soap)]
    [Route("/types/vbnet")]
    public class TypesVbNet : NativeTypesBase { }

    [Exclude(Feature.Soap)]
    [Route("/types/typescript")]
    public class TypesTypeScript : NativeTypesBase { }

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
        public bool? AddServiceStackTypes { get; set; }
        public string AddDefaultXmlNamespace { get; set; }
        public List<string> DefaultNamespaces { get; set; }
    }

    public interface INativeTypesMetadata
    {
        MetadataTypesConfig GetConfig(NativeTypesBase req);

        MetadataTypes GetMetadataTypes(IRequest req, MetadataTypesConfig config = null);
    }


    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class NativeTypesService : Service
    {
        public INativeTypesMetadata NativeTypesMetadata { get; set; }

        public MetadataTypes Any(TypesMetadata request)
        {
            if (request.BaseUrl == null)
                request.BaseUrl = Request.GetBaseUrl();

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            return metadataTypes;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesCSharp request)
        {
            if (request.BaseUrl == null)
                request.BaseUrl = Request.GetBaseUrl();

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            var csharp = new CSharpGenerator(typesConfig).GetCode(metadataTypes, base.Request);
            return csharp;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesFSharp request)
        {
            if (request.BaseUrl == null)
                request.BaseUrl = Request.GetBaseUrl();

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            var csharp = new FSharpGenerator(typesConfig).GetCode(metadataTypes, base.Request);
            return csharp;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesVbNet request)
        {
            if (request.BaseUrl == null)
                request.BaseUrl = Request.GetBaseUrl();

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            var vbnet = new VbNetGenerator(typesConfig).GetCode(metadataTypes, base.Request);
            return vbnet;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesTypeScript request)
        {
            if (request.BaseUrl == null)
                request.BaseUrl = Request.GetBaseUrl();

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            var csharp = new TypeScriptGenerator(typesConfig).GetCode(metadataTypes, base.Request);
            return csharp;
        }

    }
}