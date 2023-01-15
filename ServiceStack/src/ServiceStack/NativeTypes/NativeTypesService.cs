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
using ServiceStack.NativeTypes.Python;
using ServiceStack.NativeTypes.Swift;
using ServiceStack.NativeTypes.TypeScript;
using ServiceStack.NativeTypes.VbNet;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes;

[ExcludeMetadata]
[Route("/types")]
public class TypeLinks : NativeTypesBase, IGet, IReturn<Dictionary<string, string>> { }

[ExcludeMetadata]
[Route("/types/metadata")]
public class TypesMetadata : NativeTypesBase, IGet, IReturn<MetadataTypes> { }

[ExcludeMetadata]
[Route("/types/csharp")]
public class TypesCSharp : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/fsharp")]
public class TypesFSharp : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/vbnet")]
public class TypesVbNet : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/typescript")]
public class TypesTypeScript : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/typescript.d")]
public class TypesTypeScriptDefinition : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/dart")]
public class TypesDart : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/swift")]
public class TypesSwift : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/swift4")]
public class TypesSwift4 : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/java")]
public class TypesJava : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/kotlin")]
public class TypesKotlin : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/python")]
public class TypesPython : NativeTypesBase, IGet, IReturn<string> { }

[ExcludeMetadata]
[Route("/types/js")]
public class TypesCommonJs : NativeTypesBase, IGet, IReturn<string>
{
    public bool? Cache { get; set; }
    public string Vfx { get; set; }
}

[ExcludeMetadata]
[Route("/types/mjs")]
public class TypesMjs : NativeTypesBase, IGet, IReturn<string>
{
    public bool? Cache { get; set; }
    public string Vfx { get; set; }
}

public class NativeTypesBase
{
    public string BaseUrl { get; set; }
    public bool? MakePartial { get; set; }
    public bool? MakeVirtual { get; set; }
    public bool? MakeInternal { get; set; }
    public bool? AddReturnMarker { get; set; }
    public bool? AddDescriptionAsComments { get; set; }
    public bool? AddDocAnnotations { get; set; }
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
    public bool? AddNullableAnnotations { get; set; }
    public bool? MakePropertiesOptional { get; set; }
    public bool? ExportAsTypes { get; set; }
    public bool? ExportValueTypes { get; set; }
    public bool? ExcludeNamespace { get; set; }
    public string AddDefaultXmlNamespace { get; set; }
    public string GlobalNamespace { get; set; }
    public string BaseClass { get; set; }
    public string Package { get; set; }
    public string DataClass { get; set; }
    public string DataClassJson { get; set; }
    public List<string> AddNamespaces { get; set; }
    public List<string> DefaultNamespaces { get; set; }
    public List<string> DefaultImports { get; set; }
    public List<string> IncludeTypes { get; set; }
    public List<string> ExcludeTypes { get; set; }
    public List<string> ExportTags { get; set; }
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
    public static List<Action<IRequest,Dictionary<string, string>>> TypeLinksFilters { get; set; } = new();
        
    public object Any(TypeLinks request)
    {
        var links = new Dictionary<string,string> {
            {"Metadata", new TypesMetadata().ToAbsoluteUri(Request)},
            {"CSharp", new TypesCSharp().ToAbsoluteUri(Request)},
            {"FSharp", new TypesFSharp().ToAbsoluteUri(Request)},
            {"VbNet", new TypesVbNet().ToAbsoluteUri(Request)},
            {"TypeScript", new TypesTypeScript().ToAbsoluteUri(Request)},
            {"TypeScriptDefinition", new TypesTypeScriptDefinition().ToAbsoluteUri(Request)},
            {"CommonJs", new TypesCommonJs().ToAbsoluteUri(Request)},
            {"Mjs", new TypesMjs().ToAbsoluteUri(Request)},
            {"Dart", new TypesDart().ToAbsoluteUri(Request)},
            {"Java", new TypesJava().ToAbsoluteUri(Request)},
            {"Kotlin", new TypesKotlin().ToAbsoluteUri(Request)},
            {"Swift", new TypesSwift().ToAbsoluteUri(Request)},
            {"Python", new TypesPython().ToAbsoluteUri(Request)},
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
        var metadataTypes = ResolveMetadataTypes(typesConfig);
        return metadataTypes;
    }

    [AddHeader(ContentType = MimeTypes.PlainText)]
    public object Any(TypesCSharp request)
    {
        request.BaseUrl = GetBaseUrl(request.BaseUrl);

        var typesConfig = NativeTypesMetadata.GetConfig(request);

        if (request.AddServiceStackTypes == true)
            typesConfig.IgnoreTypesInNamespaces = new List<string>();
            
        var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
        var csharp = new CSharpGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
        return csharp;
    }

    [AddHeader(ContentType = MimeTypes.PlainText)]
    public object Any(TypesFSharp request)
    {
        request.BaseUrl = GetBaseUrl(request.BaseUrl);

        var typesConfig = NativeTypesMetadata.GetConfig(request);

        if (request.AddServiceStackTypes == true)
            typesConfig.IgnoreTypesInNamespaces = new List<string>();
            
        var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
        var fsharp = new FSharpGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
        return fsharp;
    }

    [AddHeader(ContentType = MimeTypes.PlainText)]
    public object Any(TypesVbNet request)
    {
        request.BaseUrl = GetBaseUrl(request.BaseUrl);

        var typesConfig = NativeTypesMetadata.GetConfig(request);

        if (request.AddServiceStackTypes == true)
            typesConfig.IgnoreTypesInNamespaces = new List<string>();
            
        var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);
        var vbnet = new VbNetGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
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
        var metadataTypes = ResolveMetadataTypes(typesConfig);

        var typeScript = new TypeScriptGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
        return typeScript;
    }

    [AddHeader(ContentType = MimeTypes.JavaScript)]
    public object Any(TypesCommonJs request)
    {
        string Generate()
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            typesConfig.MakePropertiesOptional = request.MakePropertiesOptional ?? false;
            typesConfig.ExportAsTypes = true;

            var metadataTypes = ResolveMetadataTypes(typesConfig);
            var typeScript = new CommonJsGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
            return typeScript;
        }

        if (request.Cache != false && !HostContext.DebugMode)
            return Request.ToOptimizedResultUsingCache(LocalCache, cacheKey:Request.AbsoluteUri, Generate);
            
        return Generate();
    }

    [AddHeader(ContentType = MimeTypes.JavaScript)]
    public object Any(TypesMjs request)
    {
        string Generate()
        {
            request.BaseUrl = GetBaseUrl(request.BaseUrl);

            var typesConfig = NativeTypesMetadata.GetConfig(request);
            typesConfig.MakePropertiesOptional = request.MakePropertiesOptional ?? false;
            typesConfig.ExportAsTypes = true;

            var metadataTypes = ResolveMetadataTypes(typesConfig);
            var typeScript = new MjsGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
            return typeScript;
        }

        if (request.Cache != false && !HostContext.DebugMode)
            return Request.ToOptimizedResultUsingCache(LocalCache, cacheKey:Request.AbsoluteUri, Generate);
            
        return Generate();
    }

    [AddHeader(ContentType = MimeTypes.PlainText)]
    public object Any(TypesPython request)
    {
        request.BaseUrl = GetBaseUrl(request.BaseUrl);

        var typesConfig = NativeTypesMetadata.GetConfig(request);
        typesConfig.MakePropertiesOptional = request.MakePropertiesOptional ?? false;
        typesConfig.ExportAsTypes = true;
            
        var metadataTypes = ResolveMetadataTypes(typesConfig);

        if (!PythonGenerator.GenerateServiceStackTypes)
        {
            var ignoreLibraryTypes = ReturnInterfaces.Map(x => x.Name);
            ignoreLibraryTypes.AddRange(BuiltinInterfaces.Select(x => x.Name));
            ignoreLibraryTypes.AddRange(BuiltInClientDtos.Select(x => x.Name));

            metadataTypes.Operations.RemoveAll(x => ignoreLibraryTypes.Contains(x.Request.Name));
            metadataTypes.Operations.Each(x => {
                if (x.Response != null && ignoreLibraryTypes.Contains(x.Response.Name))
                {
                    x.Response = null;
                }
            });
            metadataTypes.Types.RemoveAll(x => ignoreLibraryTypes.Contains(x.Name));
        }

        var gen = new PythonGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
        return gen;
    }

    [AddHeader(ContentType = MimeTypes.PlainText)]
    public object Any(TypesDart request)
    {
        request.BaseUrl = GetBaseUrl(request.BaseUrl);

        var typesConfig = NativeTypesMetadata.GetConfig(request);
        typesConfig.ExportAsTypes = true;
            
        var metadataTypes = ResolveMetadataTypes(typesConfig);

        if (!DartGenerator.GenerateServiceStackTypes)
        {
            var ignoreLibraryTypes = ReturnInterfaces.Map(x => x.Name);
            ignoreLibraryTypes.AddRange(BuiltinInterfaces.Select(x => x.Name));
            ignoreLibraryTypes.AddRange(BuiltInClientDtos.Select(x => x.Name));

            metadataTypes.Operations.RemoveAll(x => ignoreLibraryTypes.Contains(x.Request.Name));
            metadataTypes.Operations.Each(x => {
                if (x.Response != null && ignoreLibraryTypes.Contains(x.Response.Name))
                {
                    x.Response = null;
                }
            });
            metadataTypes.Types.RemoveAll(x => ignoreLibraryTypes.Contains(x.Name));
        }
            
        var dart = new DartGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
        return dart;
    }

    public static List<Type> ReturnInterfaces = new() {
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
        typeof(ICreateDb<>),
        typeof(IUpdateDb<>),
        typeof(IPatchDb<>),
        typeof(IDeleteDb<>),
        typeof(ISaveDb<>),
    }.ToList();

    public static List<Type> BuiltInClientDtos = new[] {
        typeof(ResponseStatus),
        typeof(ResponseError),
        typeof(EmptyResponse),
        typeof(IdResponse),
        typeof(StringResponse),
        typeof(StringsResponse),
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
        typeof(MetadataApp),
        typeof(GetFile),
        typeof(FileContent),
        typeof(StreamFiles), // gRPC Server Stream
        typeof(StreamServerEvents), // gRPC Server Stream
        typeof(AuditBase),
    }.ToList();

    public MetadataTypes ResolveMetadataTypes(MetadataTypesConfig typesConfig) =>
        ResolveMetadataTypes(typesConfig, NativeTypesMetadata, Request);
        
    public static MetadataTypes ResolveMetadataTypes(MetadataTypesConfig typesConfig, INativeTypesMetadata nativeTypesMetadata, IRequest req)
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

        var metadataTypes = nativeTypesMetadata.GetMetadataTypes(req, typesConfig);

        metadataTypes.Types.RemoveAll(x => x.Name == "Service");

        var returnInterfaces = new List<Type>(ReturnInterfaces);

        if (typesConfig.AddServiceStackTypes)
        {
            //IReturn markers are metadata properties that are not included as normal interfaces
            var generator = ((NativeTypesMetadata) nativeTypesMetadata).GetGenerator(typesConfig);

            var allTypes = metadataTypes.GetAllTypesOrdered();
            var allTypeNames = allTypes.Select(x => x.Name).ToSet();
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
            
        //Swift doesn't support generic protocols (requires Type modification)
        typesConfig.ExportTypes.Remove(typeof(ICreateDb<>));
        typesConfig.ExportTypes.Remove(typeof(IUpdateDb<>));
        typesConfig.ExportTypes.Remove(typeof(IPatchDb<>));
        typesConfig.ExportTypes.Remove(typeof(IDeleteDb<>));
        typesConfig.ExportTypes.Remove(typeof(ISaveDb<>));

        var metadataTypes = NativeTypesMetadata.GetMetadataTypes(Request, typesConfig);

        try
        {
            var swift = new SwiftGenerator(typesConfig).GetCode(metadataTypes, base.Request, NativeTypesMetadata);
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
        typesConfig.ExportTypes ??= new HashSet<Type>();
        typesConfig.ExportTypes.Add(typeof(KeyValuePair<,>));
        typesConfig.ExportTypes.Remove(typeof(IMeta));
    }
}