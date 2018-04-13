using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.NativeTypes.Dart;
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
        public string Dart { get; set; }
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
    [Route("/types/dart")]
    public class TypesDart : NativeTypesBase { }

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
        public bool? ExportValueTypes { get; set; }
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
                Dart = new TypesDart().ToAbsoluteUri(),
                Java = new TypesJava().ToAbsoluteUri(),
                Kotlin = new TypesKotlin().ToAbsoluteUri(),
                Swift = new TypesSwift().ToAbsoluteUri(),
            };
            return response;
        }

        private string GetBaseUrl(string baseUrl) => baseUrl ?? HostContext.GetPlugin<NativeTypesFeature>().MetadataTypesConfig.BaseUrl ?? Request.GetBaseUrl();

        public MetadataTypes Any(TypesMetadata request)
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            return metadataTypes;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesCSharp request)
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            var csharp = new CSharpGenerator(typesConfig).GetCode(metadataTypes, base.Request);
            return csharp;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesFSharp request)
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            var fsharp = new FSharpGenerator(typesConfig).GetCode(metadataTypes, base.Request);
            return fsharp;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesVbNet request)
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
            var vbnet = new VbNetGenerator(typesConfig).GetCode(metadataTypes, base.Request);
            return vbnet;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesTypeScript request)
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            typesConfig.ExportAsTypes = true;

            return GenerateTypeScript(request, typesConfig);
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesTypeScriptDefinition request)
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            return GenerateTypeScript(request, NativeTypesMetadata.GetConfig(request));
        }

        public string GenerateTypeScript(NativeTypesBase request, MetadataTypesConfig typesConfig)
        {
            var metadataTypes = ConfigureScript(typesConfig);

            var typeScript = new TypeScriptGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
            return typeScript;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesDart request)
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            typesConfig.ExportAsTypes = true;
            
            var metadataTypes = ConfigureScript(typesConfig);

            if (!DartGenerator.GenerateServiceStackTypes)
            {
                var ignoreDartLibraryTypes = ReturnInterfaces.Map(x => x.Name);
                ignoreDartLibraryTypes.AddRange(BuiltinInterfaces.Select(x => x.Name));
                ignoreDartLibraryTypes.AddRange(BuiltInClientDtos.Select(x => x.Name));

                metadataTypes.Operations.RemoveAll(x => ignoreDartLibraryTypes.Contains(x.Request.Name));
                metadataTypes.Operations.Each(x => {
                    if (x.Response != null && ignoreDartLibraryTypes.Contains(x.Response.Name))
                    {
                        x.Response = null;
                    }
                });
                metadataTypes.Types.RemoveAll(x => ignoreDartLibraryTypes.Contains(x.Name));
            }
            
            var generator = ((NativeTypesMetadata) NativeTypesMetadata).GetMetadataTypesGenerator(typesConfig);
    
            var dart = new DartGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
            return dart;
        }

        public static List<Type> ReturnInterfaces = new List<Type> {
            typeof(IReturn<>),
            typeof(IReturnVoid),
        };

        public static List<Type> BuiltinInterfaces = new []{
            typeof(IGet),
            typeof(IPost),
            typeof(IPut),
            typeof(IDelete),
            typeof(IPatch),
            typeof(IOptions),
            typeof(IMeta),
            typeof(IHasSessionId),
            typeof(IHasVersion),
        }.ToList();

        public static List<Type> BuiltInClientDtos = new[] {
            typeof(ResponseStatus),
            typeof(ResponseError),
            typeof(QueryBase),
            typeof(QueryData<>),
            typeof(QueryDb<>),
            typeof(QueryDb<,>),
            typeof(QueryResponse<>),
            typeof(KeyValuePair<,>),
            typeof(Tuple<>),
            typeof(Tuple<,>),
            typeof(Tuple<,,>),
            typeof(Tuple<,,,>),
            typeof(Authenticate),
            typeof(AuthenticateResponse),
            typeof(Register),
            typeof(RegisterResponse),
            typeof(AssignRoles),
            typeof(AssignRolesResponse),
            typeof(UnAssignRoles),
            typeof(UnAssignRolesResponse),
            typeof(CancelRequest),
            typeof(CancelRequestResponse),
            typeof(UpdateEventSubscriber),
            typeof(UpdateEventSubscriberResponse),
            typeof(GetEventSubscribers),
            typeof(GetApiKeys),
            typeof(GetApiKeysResponse),
            typeof(RegenerateApiKeys),
            typeof(RegenerateApiKeysResponse),
            typeof(UserApiKey),
            typeof(ConvertSessionToToken),
            typeof(ConvertSessionToTokenResponse),
            typeof(GetAccessToken),
            typeof(GetAccessTokenResponse),
        }.ToList();

        private MetadataTypes ConfigureScript(MetadataTypesConfig typesConfig)
        {
            //Include SS types by removing ServiceStack namespaces
            if (typesConfig.AddServiceStackTypes)
                typesConfig.IgnoreTypesInNamespaces = new List<string>();

            typesConfig.ExportTypes.Add(typeof(KeyValuePair<,>));
            typesConfig.ExportTypes.Add(typeof(Tuple<>));
            typesConfig.ExportTypes.Add(typeof(Tuple<,>));
            typesConfig.ExportTypes.Add(typeof(Tuple<,,>));
            typesConfig.ExportTypes.Add(typeof(Tuple<,,,>));
            typesConfig.ExportTypes.Remove(typeof(IMeta));
            
            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);

            metadataTypes.Types.RemoveAll(x => x.Name == "Service");

            var returnInterfaces = new List<Type>(ReturnInterfaces);

            if (typesConfig.AddServiceStackTypes)
            {
                //IReturn markers are metadata properties that are not included as normal interfaces
                var generator = ((NativeTypesMetadata) NativeTypesMetadata).GetMetadataTypesGenerator(typesConfig);

                foreach (var op in metadataTypes.Operations)
                {
                    foreach (var typeName in op.Request.Implements.Safe())
                    {
                        var iface = BuiltinInterfaces.FirstOrDefault(x => x.Name == typeName.Name);
                        if (iface != null)
                        {
                            returnInterfaces.AddIfNotExists(iface);
                        }
                    }
                }

                metadataTypes.Types.InsertRange(0, returnInterfaces.Map(x => generator.ToType(x)));
            }

            return metadataTypes;
        }


        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesSwift request)
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);

            //Include SS types by removing ServiceStack namespaces
            if (typesConfig.AddServiceStackTypes)
                typesConfig.IgnoreTypesInNamespaces = new List<string>();

            ExportMissingSystemTypes(typesConfig);

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
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);

            //Include SS types by removing ServiceStack namespaces
            if (typesConfig.AddServiceStackTypes)
                typesConfig.IgnoreTypesInNamespaces = new List<string>();

            ExportMissingSystemTypes(typesConfig);

            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);

            metadataTypes.Types.RemoveAll(x => x.Name == "Service");

            var java = new JavaGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
            return java;
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesKotlin request)
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);

            //Include SS types by removing ServiceStack namespaces
            if (typesConfig.AddServiceStackTypes)
                typesConfig.IgnoreTypesInNamespaces = new List<string>();

            ExportMissingSystemTypes(typesConfig);

            var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);

            metadataTypes.Types.RemoveAll(x => x.Name == "Service");

            var java = new KotlinGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
            return java;
        }

        private static void ExportMissingSystemTypes(MetadataTypesConfig typesConfig)
        {
            if (typesConfig.ExportTypes == null)
                typesConfig.ExportTypes = new HashSet<Type>();

            typesConfig.ExportTypes.Add(typeof(KeyValuePair<,>));

            typesConfig.ExportTypes.Remove(typeof(IMeta));
        }
    }
}