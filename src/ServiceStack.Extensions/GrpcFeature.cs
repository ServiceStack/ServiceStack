using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;
using ProtoBuf.Meta;
using ServiceStack.Host;
using ServiceStack.Logging;

namespace ServiceStack
{
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
        private static ILog log = LogManager.GetLogger(typeof(GrpcMarshallerFactory));
        public static readonly GrpcMarshallerFactory Instance = new GrpcMarshallerFactory(GrpcUtils.TypeModel);

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
                log.Error($"Could not serialize '{typeof(T).Name}': " + e.Message, e);
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
                log.Error($"Could not deserialize '{typeof(T).Name}': " + e.Message, e);
                throw;
            }
        }
    }

    [Priority(10)]
    public class GrpcFeature : IPlugin, IPostInitPlugin
    {
        public string ServicesName { get; set; } = "GrpcServices";
        public Type GrpcServicesBaseType { get; set; } = typeof(GrpcServiceBase);
        
        public Action<TypeBuilder, MethodBuilder, Type> GenerateServiceFilter { get; set; }

        /// <summary>
        /// Only generate specified Verb entries for "ANY" routes
        /// </summary>
        public List<string> GenerateMethodsForAny { get; set; } = new List<string> {
            HttpMethods.Get,
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Delete,
            HttpMethods.Patch,
        };
        
        public List<string> GenerateMethodsForAnyAutoQuery { get; set; } = new List<string> {
            HttpMethods.Get,
        };
        
        public HashSet<string> IgnoreResponseHeaders { get; set; } = new HashSet<string> {
            HttpHeaders.Vary,
            HttpHeaders.XPoweredBy,
        };
        
        public List<Type> RegisterServices { get; set; } = new List<Type> {
            typeof(GetFileService),
        };

        public bool DisableResponseHeaders
        {
            get => IgnoreResponseHeaders == null;
            set => IgnoreResponseHeaders = null;
        }

        private readonly IApplicationBuilder app;
        public GrpcFeature(IApplicationBuilder app)
        {
            this.app = app;
        }

        public void Register(IAppHost appHost)
        {
            var cors = appHost.GetPlugin<CorsFeature>();
            if (cors != null)
            {
                new[]{
                    HttpHeaders.AllowOrigin,
                    HttpHeaders.AllowMethods,
                    HttpHeaders.AllowHeaders,
                    HttpHeaders.AllowCredentials,
                    HttpHeaders.ExposeHeaders,
                    HttpHeaders.AccessControlMaxAge,
                }.Each(x => IgnoreResponseHeaders.Add(x));
            }

            foreach (var service in RegisterServices)
            {
                appHost.RegisterService(service);
            }
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            var servicesType = GenerateGrpcServices(appHost.Metadata.Operations);
            var genericMi = GetType().GetMethod(nameof(MapGrpcService), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var mi = genericMi.MakeGenericMethod(servicesType);
            mi.Invoke(this, TypeConstants.EmptyObjectArray);
            
            RegisterDtoTypes(appHost.Metadata.GetAllDtos());
        }

        private void RegisterDtoTypes(IEnumerable<Type> allDtos)
        {
            // All DTO Types with inheritance need to be registered in GrpcMarshaller<T> / GrpcUtils.TypeModel
            foreach (var dto in allDtos)
            {
                GrpcUtils.Register(dto);
            }
        }

        internal void MapGrpcService<TService>() where TService : class
        {
            app.UseEndpoints(endpoints => endpoints.MapGrpcService<TService>());
        }

        public Type GenerateGrpcServices(IEnumerable<Operation> metadataOperations)
        {
            var log = LogManager.GetLogger(GetType());
                
            var assemblyName = new AssemblyName { Name = "tmpAssemblyGrpc" };
            var typeBuilder =
                AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                    .DefineDynamicModule("tmpModuleGrpc")
                    .DefineType(ServicesName,
                        TypeAttributes.Public | TypeAttributes.Class,
                        GrpcServicesBaseType);

            var methods = new List<string>();
            var missingMemberOrders = new List<string>();
            foreach (var op in metadataOperations)
            {
                // Only enable Services via Grpc with known Response Types 
                var responseType = op.ResponseType ?? typeof(EmptyResponse); //void responses can return empty ErrorResponse 
                if (responseType == typeof(object) || responseType == typeof(Task<object>))
                    continue;
                
                // Only enable Services that are annotated with [DataContract] or [ProtoContract] attributes. ProtoBuf requires index per prop
                var isDataContract = op.RequestType.HasAttribute<DataContractAttribute>();
                var isProtoContract = op.RequestType.HasAttribute<ProtoContractAttribute>();
                if (!(isDataContract || isProtoContract))
                    continue;

                if (isDataContract)
                {
                    missingMemberOrders.Clear();
                    foreach (var prop in op.RequestType.GetPublicProperties())
                    {
                        var dataMember = prop.FirstAttribute<DataMemberAttribute>();
                        if (dataMember != null && dataMember.Order == default)
                            missingMemberOrders.Add(prop.Name);
                    }
                    if (missingMemberOrders.Count > 0)
                    {
                        if (log.IsDebugEnabled)
                            log.Debug($"{op.RequestType.Name} properties: '{string.Join(", ", missingMemberOrders)}' are missing '[DataMember(Order=N)]' annotations required by GRPC.");
                        continue;
                    }

                    missingMemberOrders.Clear();
                    foreach (var prop in responseType.GetPublicProperties())
                    {
                        var dataMember = prop.FirstAttribute<DataMemberAttribute>();
                        if (dataMember != null && dataMember.Order == default)
                            missingMemberOrders.Add(prop.Name);
                    }
                    if (missingMemberOrders.Count > 0)
                    {
                        if (log.IsDebugEnabled)
                            log.Debug($"{responseType.Name} properties: '{string.Join(", ", missingMemberOrders)}' are missing '[DataMember(Order=N)]' annotations required by GRPC.");
                        continue;
                    }
                }
                
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
                            if (typeof(IQuery).IsAssignableFrom(op.RequestType))
                                methods.AddRange(GenerateMethodsForAnyAutoQuery);
                            else
                                methods.AddRange(GenerateMethodsForAny);
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
                    var methodName = action.ToPascalCase() + requestType.Name;
                    
                    var method = typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Virtual,
                        CallingConventions.Standard,
                        returnType: typeof(Task<>).MakeGenericType(responseType),
                        parameterTypes: new[] { requestType, typeof(CallContext) });

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
                }
            }
            
            var servicesType = typeBuilder.CreateTypeInfo().AsType();
            return servicesType;
        }
    }

    [DefaultRequest(typeof(GetFile))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class GetFileService : Service
    {
        public object Any(GetFile request)
        {
            var file = VirtualFileSources.GetFile(request.Path);
            if (file == null)
                throw HttpError.NotFound("File does not exist");

            var bytes = file.GetBytesContentsAsBytes();
            var to = new GetFileResponse {
                Name = file.Name,
                Type = MimeTypes.GetMimeType(file.Extension),
                Body = bytes,
                Length = bytes.Length,
            };
            return to;
        }
    }

}