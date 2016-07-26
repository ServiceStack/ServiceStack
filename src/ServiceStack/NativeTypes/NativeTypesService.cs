using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.NativeTypes.FSharp;
using ServiceStack.NativeTypes.Java;
using ServiceStack.NativeTypes.Kotlin;
using ServiceStack.NativeTypes.Swift;
using ServiceStack.NativeTypes.TypeScript;
using ServiceStack.NativeTypes.VbNet;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes
{
    [Exclude(Feature.Soap)]
    [Route("/types")]
    public class TypeLinks : NativeTypesBase, IReturn<TypeLinksResponse> { }

    public class TypeLinksResponse
    {
        public string Metadata { get; set; }
        public string Csharp { get; set; }
        public string Fsharp { get; set; }
        public string VbNet { get; set; }
        public string TypeScript { get; set; }
        public string TypeScriptDefinition { get; set; }
        public string Java { get; set; }
        public string Kotlin { get; set; }
        public string Swift { get; set; }
    }

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

    [Exclude(Feature.Soap)]
    [Route("/types/typescript.d")]
    public class TypesTypeScriptDefinition : NativeTypesBase { }

    [Exclude(Feature.Soap)]
    [Route("/types/swift")]
    public class TypesSwift : NativeTypesBase { }

    [Exclude(Feature.Soap)]
    [Route("/types/java")]
    public class TypesJava : NativeTypesBase { }

    [Exclude(Feature.Soap)]
    [Route("/types/kotlin")]
    public class TypesKotlin : NativeTypesBase { }

    public class NativeTypesBase
    {
        public string BaseUrl { get; set; }
        public bool? MakePartial { get; set; }
        public bool? MakeVirtual { get; set; }
        public bool? MakeInternal { get; set; }
        public bool? AddReturnMarker { get; set; }
        public bool? AddDescriptionAsComments { get; set; }
        public bool? AddDataContractAttributes { get; set; }
        public bool? MakeDataContractsExtensible { get; set; }
        public bool? AddIndexesToDataMembers { get; set; }
        public bool? AddGeneratedCodeAttributes { get; set; }
        public bool? InitializeCollections { get; set; }
        public int? AddImplicitVersion { get; set; }
        public bool? AddResponseStatus { get; set; }
        public bool? AddServiceStackTypes { get; set; }
        public bool? AddModelExtensions { get; set; }
        public bool? AddPropertyAccessors { get; set; }
        public bool? ExcludeGenericBaseTypes { get; set; }
        public bool? SettersReturnThis { get; set; }
        public bool? MakePropertiesOptional { get; set; }
        public bool? ExportAsTypes { get; set; }
        public bool? ExcludeNamespace { get; set; }
        public string AddDefaultXmlNamespace { get; set; }
        public string GlobalNamespace { get; set; }
        public string BaseClass { get; set; }
        public string Package { get; set; }
        public List<string> AddNamespaces { get; set; }
        public List<string> DefaultNamespaces { get; set; }
        public List<string> DefaultImports { get; set; }
        public List<string> IncludeTypes { get; set; }
        public List<string> ExcludeTypes { get; set; }
        public List<string> TreatTypesAsStrings { get; set; }
    }

    public interface INativeTypesMetadata
    {
        MetadataTypesConfig GetConfig(NativeTypesBase req);

        MetadataTypes GetMetadataTypes(IRequest req, MetadataTypesConfig config = null, Func<Operation, bool> predicate = null);
    }


    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class NativeTypesService : Service
    {
        public INativeTypesMetadata NativeTypesMetadata { get; set; }

        public object Any(TypeLinks request)
        {
            var response = new TypeLinksResponse
            {
                Metadata = new TypesMetadata().ToAbsoluteUri(),
                Csharp = new TypesCSharp().ToAbsoluteUri(),
                Fsharp = new TypesFSharp().ToAbsoluteUri(),
                VbNet = new TypesVbNet().ToAbsoluteUri(),
                TypeScript = new TypesTypeScript().ToAbsoluteUri(),
                TypeScriptDefinition = new TypesTypeScriptDefinition().ToAbsoluteUri(),
                Java = new TypesJava().ToAbsoluteUri(),
                Kotlin = new TypesKotlin().ToAbsoluteUri(),
                Swift = new TypesSwift().ToAbsoluteUri(),
            };
            return response;
        }

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
            var fsharp = new FSharpGenerator(typesConfig).GetCode(metadataTypes, base.Request);
            return fsharp;
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
            typesConfig.ExportAsTypes = true;

            return GenerateTypeScript(request, typesConfig);
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesTypeScriptDefinition request)
        {
            if (request.BaseUrl == null)
                request.BaseUrl = Request.GetBaseUrl();

            return GenerateTypeScript(request, NativeTypesMetadata.GetConfig(request));
        }

        public string GenerateTypeScript(NativeTypesBase request, MetadataTypesConfig typesConfig)
        {
            //Include SS types by removing ServiceStack namespaces
            if (typesConfig.AddServiceStackTypes)
                typesConfig.IgnoreTypesInNamespaces = new List<string>();

            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);

            metadataTypes.Types.RemoveAll(x => x.Name == "Service");

            if (typesConfig.AddServiceStackTypes)
            {
                //IReturn markers are metadata properties that are not included as normal interfaces
                var generator = ((NativeTypesMetadata)NativeTypesMetadata).GetMetadataTypesGenerator(typesConfig);
                metadataTypes.Types.Insert(0, generator.ToType(typeof(IReturn<>)));
                metadataTypes.Types.Insert(0, generator.ToType(typeof(IReturnVoid)));
            }

            var typeScript = new TypeScriptGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
            return typeScript;
        }


        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesSwift request)
        {
            if (request.BaseUrl == null)
                request.BaseUrl = Request.GetBaseUrl();

            var typesConfig = NativeTypesMetadata.GetConfig(request);

            //Include SS types by removing ServiceStack namespaces
            if (typesConfig.AddServiceStackTypes)
                typesConfig.IgnoreTypesInNamespaces = new List<string>();

            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);

            metadataTypes.Types.RemoveAll(x => x.Name == "Service");

            try
            {
                var swift = new SwiftGenerator(typesConfig).GetCode(metadataTypes, base.Request);
                return swift;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesJava request)
        {
            if (request.BaseUrl == null)
                request.BaseUrl = Request.GetBaseUrl();

            var typesConfig = NativeTypesMetadata.GetConfig(request);

            //Include SS types by removing ServiceStack namespaces
            if (typesConfig.AddServiceStackTypes)
                typesConfig.IgnoreTypesInNamespaces = new List<string>();

            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);

            metadataTypes.Types.RemoveAll(x => x.Name == "Service");

            var java = new JavaGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
            return java;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesKotlin request)
        {
            if (request.BaseUrl == null)
                request.BaseUrl = Request.GetBaseUrl();

            var typesConfig = NativeTypesMetadata.GetConfig(request);

            //Include SS types by removing ServiceStack namespaces
            if (typesConfig.AddServiceStackTypes)
                typesConfig.IgnoreTypesInNamespaces = new List<string>();

            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);

            metadataTypes.Types.RemoveAll(x => x.Name == "Service");

            var java = new KotlinGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
            return java;
        }
    }
}