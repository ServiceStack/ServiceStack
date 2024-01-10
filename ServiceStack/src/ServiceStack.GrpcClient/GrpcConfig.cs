using System;
using System.Collections.Concurrent;
using System.Reflection;
using ProtoBuf;
using ProtoBuf.Meta;
using ServiceStack.Logging;

namespace ServiceStack
{
    public static class GrpcConfig
    {
        private static RuntimeTypeModel typeModel;
        public static RuntimeTypeModel TypeModel => typeModel ??= Init();
        public static CompatibilityLevel CompatibilityLevel => TypeModel.DefaultCompatibilityLevel;

        static GrpcConfig() => typeModel = Init();

        private static RuntimeTypeModel Init()
        {
            var model = RuntimeTypeModel.Create();
            model.AfterApplyDefaultBehaviour += OnAfterApplyDefaultBehaviour;
            model.DefaultCompatibilityLevel = CompatibilityLevel.Level300;
            return model;
        }

        private static void OnAfterApplyDefaultBehaviour(object sender, TypeAddedEventArgs args)
        {
            if (GrpcServiceClient.IsRequestDto(args.Type))
            {
                // query DTO; we'll flatten the query *into* this type, shifting everything
                // by some reserved number per level, starting at the most base level

                // walk backwards up the tree; at each level, offset everything by 100
                // and copy over from the default model
                var log = LogManager.GetLogger(typeof(MetaTypeConfig<>).MakeGenericType(args.Type));
                Type current = args.Type.BaseType;
                var mt = args.MetaType;
                while (current != null && current != typeof(object))
                {
                    try
                    {
                        mt.ApplyFieldOffset(100);
                    }
                    catch (Exception e)
                    {
                        log.Error($"Error in CreateMetaType() for '{current.Name}' when 'ApplyFieldOffset(100)': {e.Message}", e);
                        throw;
                    }
                    var source = RuntimeTypeModel.Default[current]?.GetFields();
                    foreach (var field in source)
                    {
                        try
                        {
                            AddField(mt, field);
                        }
                        catch (Exception e)
                        {
                            log.Error($"Error in CreateMetaType() for '{current.Name}' when adding field '{field.Name}': {e.Message}", e);
                            throw;
                        }
                    }

                    // keep going down the hierarchy
                    current = current.BaseType;
                }
            }
            static void AddField(MetaType mt, ValueMember field)
            {
                try
                {
                    var newField = mt.AddField(field.FieldNumber,
                        field.Member?.Name ?? field.Name,
                        field.ItemType,
                        field.DefaultType);
                    newField.DataFormat = field.DataFormat;
                    newField.IsMap = field.IsMap;
                    newField.IsPacked = field.IsPacked;
                    newField.IsRequired = field.IsRequired;
                    newField.IsStrict = field.IsStrict;
                    newField.DefaultValue = field.DefaultValue;
                    newField.MapKeyFormat = field.MapKeyFormat;
                    newField.MapValueFormat = field.MapValueFormat;
                    newField.Name = field.Name;
                    newField.OverwriteList = field.OverwriteList;
                    // newField.SupportNull = field.SupportNull;
                } catch(Exception ex)
                {
                    throw new InvalidOperationException($"Error adding field {field.Member?.Name}: {ex.Message}", ex);
                }
            }
        }

        public static Func<Type, bool> IgnoreTypeModel { get; set; } = DefaultIgnoreTypes;

        public static bool DefaultIgnoreTypes(Type type) => type.IsValueType || type == typeof(string);

        public static Func<string, string, string> ServiceNameResolver { get; set; } = DefaultServiceNameResolver;
        public static string DefaultServiceNameResolver(string verb, string requestName) => 
            requestName.StartsWithIgnoreCase(verb) ? "Call" + requestName : verb.ToPascalCase() + requestName;

        public static Func<string, string> ServerStreamServiceNameResolver { get; set; } = DefaultServerStreamServiceNameResolver;
        public static string DefaultServerStreamServiceNameResolver(string requestName) => 
            "Server" + requestName;
        
        public static MetaType Register<T>() => MetaTypeConfig<T>.GetMetaType();
        
        public static string GetServiceName(string verb, string requestName) => ServiceNameResolver(verb, requestName);

        public static string GetServerStreamServiceName(string requestName) => ServerStreamServiceNameResolver(requestName);

        private static readonly ConcurrentDictionary<Type, Func<MetaType>> FnCache = new();

        public static MetaType Register(Type type)
        {
            if (IgnoreTypeModel(type))
                return null;

            if (!FnCache.TryGetValue(type, out var fn))
            {
                var grpc = typeof(MetaTypeConfig<>).MakeGenericType(type);
                var mi = grpc.GetMethod("GetMetaType", BindingFlags.Static | BindingFlags.Public);
                FnCache[type] = fn = (Func<MetaType>) mi.CreateDelegate(typeof(Func<MetaType>));
            }

            return fn();
        }

        public static bool HasDynamicAttribute(Type requestType, string action) =>
            requestType.FirstAttribute<TagAttribute>()?.Name == GrpcClientConfig.Keywords.Dynamic;

        public static bool IsAutoQueryService(Type requestType, string action) =>
            Crud.AnyAutoQueryType(requestType);

        public static bool AutoQueryOrDynamicAttribute(Type requestType, string action) =>
            IsAutoQueryService(requestType, action) || HasDynamicAttribute(requestType, action);
    }
}