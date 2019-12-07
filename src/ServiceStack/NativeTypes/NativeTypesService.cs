﻿using System;
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
    public class TypeLinks : NativeTypesBase, IReturn<Dictionary<string, string>> { }

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

        MetadataTypesGenerator GetGenerator();
    }

    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class NativeTypesService : Service
    {
        public INativeTypesMetadata NativeTypesMetadata { get; set; }
        public static List<Action<IRequest,Dictionary<string, string>>> TypeLinksFilters { get; set; } = 
            new List<Action<IRequest,Dictionary<string, string>>>();
        
        public object Any(TypeLinks request)
        {
            var links = new Dictionary<string,string> {
                {"Metadata", new TypesMetadata().ToAbsoluteUri(Request)},
                {"CSharp", new TypesCSharp().ToAbsoluteUri(Request)},
                {"FSharp", new TypesFSharp().ToAbsoluteUri(Request)},
                {"VbNet", new TypesVbNet().ToAbsoluteUri(Request)},
                {"TypeScript", new TypesTypeScript().ToAbsoluteUri(Request)},
                {"TypeScriptDefinition", new TypesTypeScriptDefinition().ToAbsoluteUri(Request)},
                {"Dart", new TypesDart().ToAbsoluteUri(Request)},
                {"Java", new TypesJava().ToAbsoluteUri(Request)},
                {"Kotlin", new TypesKotlin().ToAbsoluteUri(Request)},
                {"Swift", new TypesSwift().ToAbsoluteUri(Request)},
            };
            foreach (var linksFilter in TypeLinksFilters)
            {
                linksFilter(Request,links);
            }
            return links;
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
            typesConfig.MakePropertiesOptional = request.MakePropertiesOptional ?? false;
            typesConfig.ExportAsTypes = true;

            return GenerateTypeScript(request, typesConfig);
        }

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(TypesTypeScriptDefinition request)
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            typesConfig.MakePropertiesOptional = request.MakePropertiesOptional ?? true;
            
            return GenerateTypeScript(request, typesConfig);
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
            
            var generator = ((NativeTypesMetadata) NativeTypesMetadata).GetGenerator(typesConfig);
    
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
            typeof(IHasBearerToken),
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
            typeof(NavItem),
            typeof(GetNavItems),
            typeof(GetNavItemsResponse),
            typeof(GetFile),
            typeof(FileContent),
            typeof(StreamFiles), // gRPC Server Stream
            typeof(StreamServerEvents), // gRPC Server Stream
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
                var generator = ((NativeTypesMetadata) NativeTypesMetadata).GetGenerator(typesConfig);

                var allTypes = metadataTypes.GetAllTypesOrdered();
                var allTypeNames = allTypes.Select(x => x.Name).ToHashSet();
                foreach (var type in allTypes)
                {
                    foreach (var typeName in type.Implements.Safe())
                    {
                        var iface = BuiltinInterfaces.FirstOrDefault(x => x.Name == typeName.Name);
                        if (iface != null && !allTypeNames.Contains(iface.Name))
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