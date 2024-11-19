using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;
using ProtoBuf.Meta;
using ServiceStack.DataAnnotations;
using ServiceStack.Grpc;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.NativeTypes;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public static class ServiceCollectionExtensions
{
    public static void AddServiceStackGrpc(this IServiceCollection services)
    {
        var marshallers = new List<MarshallerFactory> {
            GrpcMarshallerFactory.Instance,
            ProtoBufMarshallerFactory.Default,
        };
        var binder = BinderConfiguration.Create(marshallers);
        services.AddSingleton(binder);
        services.AddCodeFirstGrpc();
    }
}

public class GrpcMarshallerFactory : MarshallerFactory
{
    public static readonly GrpcMarshallerFactory Instance = new(GrpcConfig.TypeModel);

    public RuntimeTypeModel TypeModel { get; }
    private GrpcMarshallerFactory(RuntimeTypeModel typeModel) => TypeModel = typeModel;

    protected override bool CanSerialize(Type type)
    {
        return TypeModel.CanSerialize(type);
    }

    protected override byte[] Serialize<T>(T value)
    {
        try 
        { 
            return GrpcMarshaller<T>.Instance.Serializer(value);
        }
        catch (Exception e)
        {
            LogManager.GetLogger(typeof(GrpcMarshallerFactory)).Error($"Could not serialize '{typeof(T).Name}': " + e.Message, e);
            throw;
        }
    }

    protected override T Deserialize<T>(byte[] payload)
    {
        try 
        { 
            return GrpcMarshaller<T>.Instance.Deserializer(payload);
        }
        catch (Exception e)
        {
            LogManager.GetLogger(typeof(GrpcMarshallerFactory)).Error($"Could not deserialize '{typeof(T).Name}': " + e.Message, e);
            throw;
        }
    }
}

[Priority(10)]
public class GrpcFeature : IPlugin, IConfigureServices, IPreInitPlugin, IPostInitPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.Grpc;
    public string ServicesName { get; set; } = "GrpcServices";
    public Type GrpcServicesBaseType { get; set; } = typeof(GrpcServiceBase);
        
    public Type GrpcServicesType { get; private set; }
        
    public Action<TypeBuilder, MethodBuilder, Type> GenerateServiceFilter { get; set; }

    public Func<Type, string, bool> CreateDynamicService { get; set; } = GrpcConfig.HasDynamicAttribute;

    public Func<IResponse, Status?> ToGrpcStatus { get; set; }

    /// <summary>
    /// Only generate specified Verb entries for "ANY" routes
    /// </summary>
    public List<string> DefaultMethodsForAny { get; set; } =
    [
        HttpMethods.Get,
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Delete
    ];
        
    public List<string> AutoQueryMethodsForAny { get; set; } =
    [
        HttpMethods.Get
    ];

    public Func<Type, List<string>> GenerateMethodsForAny { get; }
        
    public List<string> DefaultGenerateMethodsForAny(Type requestType) =>
        typeof(IQuery).IsAssignableFrom(requestType)
            ? AutoQueryMethodsForAny
            : DefaultMethodsForAny;
        
    public HashSet<string>? IgnoreResponseHeaders { get; set; } =
    [
        HttpHeaders.Vary,
        HttpHeaders.XPoweredBy
    ];
        
    public List<Type> RegisterServices { get; set; } =
    [
        typeof(StreamFileService),
        typeof(SubscribeServerEventsService)
    ];

    public List<Type> IncludeRequestTypes { get; } =
    [
        typeof(GetFile),
        typeof(GetFileUpload),
        typeof(StoreFileUpload),
        typeof(ReplaceFileUpload),
        typeof(DeleteFileUpload),
        typeof(ConvertSessionToToken),
    ];
        
    internal Dictionary<Type, Type> RequestServiceTypeMap { get; } = new();

    public bool DisableResponseHeaders
    {
        get => IgnoreResponseHeaders == null;
        set => IgnoreResponseHeaders = null;
    }
        
    public bool DisableRequestParamsInHeaders { get; set; }
        
    public List<ProtoOptionDelegate> ProtoOptions { get; set; } =
    [
        ProtoOption.CSharpNamespace,
        ProtoOption.PhpNamespace
    ];

    private IApplicationBuilder? app;
    public IApplicationBuilder App
    {
        get => app ?? ServiceStackHost.Instance.GetApp()
            ?? throw new NotSupportedException("GrpcFeature.app is not configured");
        set => app = value;
    }

    public GrpcFeature(IApplicationBuilder? app = null)
    {
        this.app = app;
        GenerateMethodsForAny = DefaultGenerateMethodsForAny;
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<MetadataFeature>(
            feature => feature.AddPluginLink("types/proto", "gRPC .proto APIs"));
    }

    public void Configure(IServiceCollection services)
    {
        services.RegisterService(typeof(TypesProtoService));
        
        foreach (var serviceType in RegisterServices)
        {
            if (!typeof(IStreamService).IsAssignableFrom(serviceType))
            {
                services.RegisterService(serviceType);
            }
            else
            {
                services.AddSingleton(serviceType);
            }
        }
    }

    public void Register(IAppHost appHost)
    {
        var cors = appHost.GetPlugin<CorsFeature>();
        if (cors != null && IgnoreResponseHeaders != null)
        {
            IgnoreResponseHeaders.AddDistinctRange([
                HttpHeaders.AllowOrigin,
                HttpHeaders.AllowMethods,
                HttpHeaders.AllowHeaders,
                HttpHeaders.AllowCredentials,
                HttpHeaders.ExposeHeaders,
                HttpHeaders.AccessControlMaxAge,
            ]);
        }
            
        NativeTypesService.TypeLinksFilters.Add((req,links) => {
            links["Proto"] = new TypesProto().ToAbsoluteUri(req);
        });
    }

    public void AfterPluginsLoaded(IAppHost appHost)
    {
        var streamServices = RegisterServices.Where(x => typeof(IStreamService).IsAssignableFrom(x)).ToList();

        var ops = new List<Operation>();
        var allDtos = new HashSet<Type>();
        foreach (var op in appHost.Metadata.Operations)
        {
            if (!ShouldRegisterService(op)) 
                continue;

            ops.Add(op);

            ServiceMetadata.AddReferencedTypes(allDtos, op.RequestType);
            ServiceMetadata.AddReferencedTypes(allDtos, op.ResponseType);
        }

        GrpcServicesType = GenerateGrpcServices(ops, streamServices);
        var genericMi = GetType().GetMethod(nameof(MapGrpcService), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var mi = genericMi.MakeGenericMethod(GrpcServicesType);
        mi.Invoke(this, TypeConstants.EmptyObjectArray);

        foreach (var serviceType in streamServices)
        {
            var genericDef = serviceType.GetTypeWithGenericTypeDefinitionOf(typeof(IStreamService<,>));
            foreach (var argType in genericDef.GenericTypeArguments)
            {
                ServiceMetadata.AddReferencedTypes(allDtos, argType);
            }
            var requestType = genericDef.GenericTypeArguments[0];
            RequestServiceTypeMap[requestType] = serviceType;
        }
        RegisterDtoTypes(allDtos);
    }
        
    public Func<Operation, bool?> RegisterService { get; set; }

    private bool ShouldRegisterService(Operation op)
    {
        var ret = RegisterService?.Invoke(op);
        if (ret != null)
            return ret.Value;

        // don't register hidden services unless IncludeRequestTypes
        if (!IncludeRequestTypes.Contains(op.RequestType))
        {
            if (op.RequestType.FirstAttribute<RestrictAttribute>()?.VisibilityTo == RequestAttributes.None)
                return false;
            if (op.ServiceType.FirstAttribute<RestrictAttribute>()?.VisibilityTo == RequestAttributes.None)
                return false;
            if (op.RequestType.FirstAttribute<ExcludeAttribute>()?.Feature.HasFlag(Feature.Metadata) == true)
                return false;
            if (op.ServiceType.FirstAttribute<ExcludeAttribute>()?.Feature.HasFlag(Feature.Metadata) == true)
                return false;
        }

        // Only enable Services via Grpc with known Response Types 
        var responseType = op.ResponseType ?? typeof(EmptyResponse); //void responses can return empty ErrorResponse 
        if (responseType == typeof(object) || responseType == typeof(Task<object>))
            return false;
            
        // Only enable Services that are annotated with [DataContract] or [ProtoContract] attributes. ProtoBuf requires index per prop
        var isDataContract = op.RequestType.HasAttribute<DataContractAttribute>();
        var isProtoContract = op.RequestType.HasAttribute<ProtoContractAttribute>();
        if (!(isDataContract || isProtoContract))
            return false;

        if (isDataContract)
        {
            var log = LogManager.GetLogger(GetType());
            var missingMemberOrders = new List<string>();
            missingMemberOrders.Clear();
            foreach (var prop in op.RequestType.GetPublicProperties())
            {
                var dataMember = prop.FirstAttribute<DataMemberAttribute>();
                if (dataMember is { Order: 0 })
                    missingMemberOrders.Add(prop.Name);
            }
            if (missingMemberOrders.Count > 0)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"{op.RequestType.Name} properties: '{string.Join(", ", missingMemberOrders)}' are missing '[DataMember(Order=N)]' annotations required by GRPC.");
                return false;
            }

            missingMemberOrders.Clear();
            foreach (var prop in responseType.GetPublicProperties())
            {
                var dataMember = prop.FirstAttribute<DataMemberAttribute>();
                if (dataMember is { Order: 0 })
                    missingMemberOrders.Add(prop.Name);
            }
            if (missingMemberOrders.Count > 0)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"{responseType.Name} properties: '{string.Join(", ", missingMemberOrders)}' are missing '[DataMember(Order=N)]' annotations required by GRPC.");
                return false;
            }
        }
            
        return true;
    }

    private static void RegisterDtoTypes(IEnumerable<Type> allDtos)
    {
        // All DTO Types with inheritance need to be registered in GrpcMarshaller<T> / GrpcUtils.TypeModel
        foreach (var dto in allDtos)
        {
            GrpcConfig.Register(dto);
        }
    }

    internal void MapGrpcService<TService>() where TService : class
    {
        App.UseEndpoints(endpoints => endpoints.MapGrpcService<TService>());
    }

    public Type GenerateGrpcServices(IEnumerable<Operation> metadataOperations, IEnumerable<Type> streamServices)
    {
        var assemblyName = new AssemblyName { Name = "tmpAssemblyGrpc" };
        var typeBuilder =
            AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule("tmpModuleGrpc")
                .DefineType(ServicesName,
                    TypeAttributes.Public | TypeAttributes.Class,
                    GrpcServicesBaseType);

        var log = LogManager.GetLogger(GetType());

        var methods = new List<string>();
        foreach (var op in metadataOperations)
        {
            var responseType = op.ResponseType ?? typeof(EmptyResponse); //void responses can return empty ErrorResponse 
                
            methods.Clear();
            foreach (var action in op.Actions)
            {
                if (action == ActionContext.AnyAction)
                {
                    if (typeof(IVerb).IsAssignableFrom(op.RequestType))
                    {
                        if (typeof(IGet).IsAssignableFrom(op.RequestType))
                            methods.Add(HttpMethods.Get);
                        if (typeof(IPost).IsAssignableFrom(op.RequestType))
                            methods.Add(HttpMethods.Post);
                        if (typeof(IPut).IsAssignableFrom(op.RequestType))
                            methods.Add(HttpMethods.Put);
                        if (typeof(IDelete).IsAssignableFrom(op.RequestType))
                            methods.Add(HttpMethods.Delete);
                        if (typeof(IPatch).IsAssignableFrom(op.RequestType))
                            methods.Add(HttpMethods.Patch);
                        if (typeof(IOptions).IsAssignableFrom(op.RequestType))
                            methods.Add(HttpMethods.Options);
                    }
                    else
                    {
                        var crudMethod = AutoCrudOperation.ToHttpMethod(op.RequestType);
                        if (crudMethod != null)
                        {
                            methods.Add(crudMethod);
                        }
                        else
                        {
                            var anyMethods = GenerateMethodsForAny(op.RequestType);
                            if (!anyMethods.IsEmpty())
                            {
                                methods.AddRange(anyMethods);
                            }
                        }
                    }
                }
                else
                {
                    methods.Add(action);
                }
            }

            var genMethods = methods.Distinct();
            foreach (var action in genMethods)
            {
                var requestType = op.RequestType;
                var methodName = GrpcConfig.GetServiceName(action, requestType.Name);
                
                if (log.IsDebugEnabled)
                    log.DebugFormat("grpc {0}({1})", methodName, requestType.Name);
                
                var method = typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Virtual,
                        
                    CallingConventions.Standard,
                    returnType: typeof(Task<>).MakeGenericType(responseType),
                    parameterTypes: [requestType, typeof(CallContext)]);

                GenerateServiceFilter?.Invoke(typeBuilder, method, requestType);

                var il = method.GetILGenerator();

                var mi = GrpcServicesBaseType.GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                var genericMi = mi.MakeGenericMethod(responseType);
                    
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, action);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, genericMi);
                il.Emit(OpCodes.Ret);

                if (CreateDynamicService(requestType, action))
                {
                    var dynamicMethodName = GrpcConfig.GetServiceName(action + Keywords.Dynamic, requestType.Name);
                        
                    if (log.IsDebugEnabled)
                        log.DebugFormat("grpc dynamic {0}(DynamicRequest)", dynamicMethodName);
                
                    method = typeBuilder.DefineMethod(dynamicMethodName, MethodAttributes.Public | MethodAttributes.Virtual,
                        
                        CallingConventions.Standard,
                        returnType: typeof(Task<>).MakeGenericType(responseType),
                        parameterTypes: [typeof(DynamicRequest), typeof(CallContext)]);

                    GenerateServiceFilter?.Invoke(typeBuilder, method, requestType);

                    il = method.GetILGenerator();

                    mi = GrpcServicesBaseType.GetMethod("ExecuteDynamic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    genericMi = mi.MakeGenericMethod(requestType,responseType);
                    
                    il.Emit(OpCodes.Nop);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, action);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Callvirt, genericMi);
                    il.Emit(OpCodes.Ret);
                }
            }
        }

        foreach (var streamService in streamServices)
        {
            var genericDef = streamService.GetTypeWithGenericTypeDefinitionOf(typeof(IStreamService<,>));
            var requestType = genericDef.GenericTypeArguments[0];
            var responseType = genericDef.GenericTypeArguments[1];
            var methodName = GrpcConfig.GetServerStreamServiceName(requestType.Name);

            if (log.IsDebugEnabled)
                log.DebugFormat("grpc stream {0}({1})", methodName, requestType.Name);
                
            var method = typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Virtual,
                CallingConventions.Standard,
                returnType: typeof(IAsyncEnumerable<>).MakeGenericType(responseType),
                parameterTypes: [requestType, typeof(CallContext)]);

            GenerateServiceFilter?.Invoke(typeBuilder, method, requestType);

            var il = method.GetILGenerator();

            var mi = GrpcServicesBaseType.GetMethod("Stream", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
            var genericMi = mi.MakeGenericMethod(requestType, responseType);
                    
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, genericMi);
            il.Emit(OpCodes.Ret);
        }
            
        var servicesType = typeBuilder.CreateTypeInfo().AsType();
        return servicesType;
    }
}
    
[ExcludeMetadata]
[Route("/types/proto")]
public class TypesProto : NativeTypesBase { }

[DefaultRequest(typeof(TypesProto))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class TypesProtoService(INativeTypesMetadata metadata) : Service
{
    private string GetBaseUrl(string? baseUrl) => baseUrl ?? HostContext.GetPlugin<NativeTypesFeature>().MetadataTypesConfig.BaseUrl ?? Request.GetBaseUrl();

    [AddHeader(ContentType = MimeTypes.PlainText)]
    public object Get(TypesProto request)
    {
        request.BaseUrl = GetBaseUrl(request.BaseUrl);

        var typesConfig = metadata.GetConfig(request);
        var metadataTypes = metadata.GetMetadataTypes(Request, typesConfig);
        var proto = new GrpcProtoGenerator(typesConfig).GetCode(metadataTypes, base.Request);
        return proto;
    }
}

[Restrict(VisibilityTo = RequestAttributes.Grpc)]
public class StreamFileService : Service, IStreamService<StreamFiles,FileContent>
{
#pragma warning disable CS1998
    public async IAsyncEnumerable<FileContent> Stream(StreamFiles request, [EnumeratorCancellation] CancellationToken cancel = default)
#pragma warning restore CS1998
    {
        var i = 0;
        var paths = request.Paths ?? TypeConstants.EmptyStringList;
        while (!cancel.IsCancellationRequested)
        {
            var file = VirtualFileSources.GetFile(paths[i]);
            var bytes = file?.ReadAllBytes();
            var to = file != null
                ? new FileContent {
                    Name = file.Name,
                    Type = MimeTypes.GetMimeType(file.Extension),
                    Body = bytes,
                    Length = bytes.Length,
                }
                : new FileContent {
                    Name = paths[i],
                    ResponseStatus = new ResponseStatus {
                        ErrorCode = nameof(HttpStatusCode.NotFound),
                        Message = "File does not exist",
                    }
                };
                
            yield return to;

            if (++i >= paths.Count)
                yield break;
        }
    }
}

public class SubscribeServerEventsService : Service, IStreamService<StreamServerEvents, StreamServerEventsResponse>
{
    public static HashSet<string> IgnoreMetaProps { get; } = new(StringComparer.OrdinalIgnoreCase) {
        nameof(ServerEventMessage.EventId),
        nameof(ServerEventMessage.Data),
        nameof(ServerEventMessage.Channel),
        nameof(ServerEventMessage.Selector),
        nameof(ServerEventMessage.Json),
        nameof(ServerEventMessage.Op),
        nameof(ServerEventMessage.Target),
        nameof(ServerEventMessage.CssSelector),
        nameof(ServerEventCommand.UserId),
        nameof(ServerEventCommand.DisplayName),
        nameof(ServerEventCommand.ProfileUrl),
        nameof(ServerEventCommand.IsAuthenticated),
        nameof(ServerEventCommand.Channels),
        nameof(ServerEventCommand.CreatedAt),
        nameof(ServerEventConnect.Id),
        nameof(ServerEventConnect.UnRegisterUrl),
        nameof(ServerEventConnect.UpdateSubscriberUrl),
        nameof(ServerEventConnect.HeartbeatUrl),
        nameof(ServerEventConnect.HeartbeatIntervalMs),
        nameof(ServerEventConnect.IdleTimeoutMs),
    }; 
        
    public async IAsyncEnumerable<StreamServerEventsResponse> Stream(StreamServerEvents request, [EnumeratorCancellation] CancellationToken cancel=default)
    {
        if (request.Channels != null)
            Request.QueryString["channels"] = string.Join(",", request.Channels);

        var handler = new ServerEventsHandler();
        await handler.ProcessRequestAsync(Request, Request.Response, nameof(StreamServerEvents)).ConfigAwait();

        var res = (GrpcResponse) Request.Response;

        //ensure response is cancelled after stream is cancelled 
        using var deferResponse = new Defer(() => res.Close());

        while (!cancel.IsCancellationRequested)
        {
            var frame = await res.EventsChannel.Reader.ReadAsync(cancel);
            var idLine = frame.LeftPart('\n');
            var dataLine = frame.RightPart('\n');

            var e = ServerEventsClient.ToTypedMessage(new ServerEventMessage {
                EventId = idLine.RightPart(':').Trim().ToInt(),
                Data = dataLine.RightPart(':').Trim(),
            });

            Dictionary<string, string>? meta = null;
            if (e.Meta != null)
            {
                foreach (var entry in e.Meta)
                {
                    if (IgnoreMetaProps.Contains(entry.Key))
                        continue;
                    meta ??= new();
                    meta[entry.Key] = entry.Value;
                }
            }
                
            var to = new StreamServerEventsResponse {
                EventId = e.EventId,
                Data = e.Data,
                Channel = e.Channel,
                Selector = e.Selector,
                Json = e.Json,
                Op = e.Op,
                Target = e.Target,
                CssSelector = e.CssSelector,
                Meta = meta,
            };

            if (e is ServerEventCommand cmd)
            {
                to.UserId = cmd.UserId;
                to.DisplayName = cmd.DisplayName;
                to.ProfileUrl = cmd.ProfileUrl;
                to.IsAuthenticated = cmd.IsAuthenticated;
                to.Channels = cmd.Channels;
                to.CreatedAt = cmd.CreatedAt.ToUnixTimeMs();
            }

            if (e is ServerEventConnect conn)
            {
                to.Id = conn.Id;
                to.UnRegisterUrl = conn.UnRegisterUrl;
                to.UpdateSubscriberUrl = conn.UpdateSubscriberUrl;
                to.HeartbeatUrl = conn.HeartbeatUrl;
                to.HeartbeatIntervalMs = conn.HeartbeatIntervalMs;
                to.IdleTimeoutMs = conn.IdleTimeoutMs;
            }
                
            yield return to;
        }
    }
}