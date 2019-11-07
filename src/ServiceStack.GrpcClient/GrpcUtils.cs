using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoBuf.Meta;

namespace ServiceStack
{
    public static class GrpcUtils
    {
        public static Func<Type, bool> IgnoreTypeModel { get; set; } = DefaultIgnoreTypes;

        public static bool DefaultIgnoreTypes(Type type) => type.IsValueType || type == typeof(string);

        public static Func<string, string, string> ServiceNameResolver { get; set; } = DefaultServiceNameResolver;
        public static string DefaultServiceNameResolver(string verb, string requestName) => 
            requestName.StartsWithIgnoreCase(verb) ? "Call" + requestName : verb.ToPascalCase() + requestName;

        public static Func<string, string> ServerStreamServiceNameResolver { get; set; } = DefaultServerStreamServiceNameResolver;
        public static string DefaultServerStreamServiceNameResolver(string requestName) => 
            "Server" + requestName;
        
        public static MetaType Register<T>() => GrpcMarshaller<T>.GetMetaType();
        
        public static string GetServiceName(string verb, string requestName) => ServiceNameResolver(verb, requestName);

        public static string GetServerStreamServiceName(string requestName) => ServerStreamServiceNameResolver(requestName);

        private static readonly ConcurrentDictionary<Type, Func<MetaType>> FnCache =
            new ConcurrentDictionary<Type, Func<MetaType>>();

        public static MetaType Register(Type type)
        {
            if (IgnoreTypeModel(type))
                return null;

            if (!FnCache.TryGetValue(type, out var fn))
            {
                var grpc = typeof(GrpcMarshaller<>).MakeGenericType(type);
                var mi = grpc.GetMethod("GetMetaType", BindingFlags.Static | BindingFlags.Public);
                FnCache[type] = fn = (Func<MetaType>) mi.CreateDelegate(typeof(Func<MetaType>));
            }

            return fn();
        }

        public static RuntimeTypeModel TypeModel { get; } = ProtoBuf.Meta.TypeModel.Create();

        public static Task<TResponse> Execute<TRequest, TResponse>(this Channel channel, TRequest request,
            string servicesName, string methodName,
            CallOptions options = default, string host = null)
            where TRequest : class
            where TResponse : class
            => Execute<TRequest, TResponse>(new DefaultCallInvoker(channel), request, servicesName, methodName, options,
                host);

        public static async Task<TResponse> Execute<TRequest, TResponse>(this CallInvoker invoker, TRequest request,
            string servicesName, string methodName,
            CallOptions options = default, string host = null)
            where TRequest : class
            where TResponse : class
        {
            var method = new Method<TRequest, TResponse>(MethodType.Unary, servicesName, methodName,
                GrpcMarshaller<TRequest>.Instance, GrpcMarshaller<TResponse>.Instance);
            using (var auc = invoker.AsyncUnaryCall(method, host, options, request))
            {
                return await auc.ResponseAsync;
            }
        }
    }
}