using System;
using System.Linq;
using System.Reflection;

namespace ServiceStack.Text
{
    internal class TypeConfig
    {
        internal readonly Type Type;
        internal bool EnableAnonymousFieldSetters;
        internal PropertyInfo[] Properties;
        internal FieldInfo[] Fields;
        internal Func<object, string, object, object> OnDeserializing;
        internal bool IsUserType { get; set; }
        internal Func<TextCase> TextCaseResolver;
        internal TextCase? TextCase 
        {
            get
            {
                var result = TextCaseResolver?.Invoke();
                return result is null or Text.TextCase.Default ? null : result;
            }
        }

        internal TypeConfig(Type type)
        {
            Type = type;
            EnableAnonymousFieldSetters = false;
            Properties = TypeConstants.EmptyPropertyInfoArray;
            Fields = TypeConstants.EmptyFieldInfoArray;

            JsConfig.AddUniqueType(Type);
        }
    }
    
    public static class TypeConfig<T>
    {
        internal static TypeConfig config;

        static TypeConfig Config => config ??= Create();

        public static PropertyInfo[] Properties
        {
            get => Config.Properties;
            set => Config.Properties = value;
        }

        public static FieldInfo[] Fields
        {
            get => Config.Fields;
            set => Config.Fields = value;
        }

        public static bool EnableAnonymousFieldSetters
        {
            get => Config.EnableAnonymousFieldSetters;
            set => Config.EnableAnonymousFieldSetters = value;
        }

        public static bool IsUserType
        {
            get => Config.IsUserType;
            set => Config.IsUserType = value;
        }

        static TypeConfig()
        {
            Init();
        }

        internal static void Init()
        {
            if (config == null)
            {
                Create();
            }
        }

        public static Func<object, string, object, object> OnDeserializing
        {
            get => config.OnDeserializing;
            set => config.OnDeserializing = value;
        }

        static TypeConfig Create()
        {
            config = new TypeConfig(typeof(T)) {
                TextCaseResolver = () => JsConfig<T>.TextCase
            };

            var excludedProperties = JsConfig<T>.ExcludePropertyNames ?? TypeConstants.EmptyStringArray;

            var properties = excludedProperties.Length > 0
                ? config.Type.GetAllSerializableProperties().Where(x => !excludedProperties.Contains(x.Name))
                : config.Type.GetAllSerializableProperties();
            Properties = properties.Where(x => x.GetIndexParameters().Length == 0).ToArray();

            Fields = config.Type.GetSerializableFields().ToArray();
    
            if (!JsConfig<T>.HasDeserializingFn)
                OnDeserializing = ReflectionExtensions.GetOnDeserializing<T>();
            else
                config.OnDeserializing = (instance, memberName, value) => JsConfig<T>.OnDeserializingFn((T)instance, memberName, value);

            IsUserType = !typeof(T).IsValueType && typeof(T).Namespace != "System";

            return config;
        }

        public static void Reset()
        {
            config = null;
        }

        internal static TypeConfig GetState()
        {
            return Config;
        }
    }
}