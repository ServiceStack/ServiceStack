#if __IOS__ || __ANDROID__ || WINDOWS_UWP || __MOBILE__

// When Linking "SDK and User Assemblies" in Xamarin you can copy this class to your project and call `JsAot.Run()` on Startup
// Alternative solution is to add 'ServiceStack.Text' to your "Skip linking assemblies" list which should contain:
// ServiceStack.Text;ServiceStack.Client;{Your}.ServiceModel

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using Xamarin.Forms.Internals;

namespace ServiceStack
{
    public static class JsAot
    {
        [Preserve]
        public static void Init() {}

        /// <summary>
        /// Provide hint to IOS AOT compiler to pre-compile generic classes for all your DTOs.
        /// Just needs to be called once in a static constructor.
        /// </summary>
        [Preserve]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Run()
        {
            try
            {
                RegisterForAot();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        [Preserve(AllMembers = true)]
        internal class Poco
        {
            public string Dummy { get; set; }
        }

        [Preserve]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void RegisterForAot()
        {
            RegisterTypeForAot<Poco>();

            RegisterElement<Poco, string>();

            RegisterElement<Poco, bool>();
            RegisterElement<Poco, char>();
            RegisterElement<Poco, byte>();
            RegisterElement<Poco, sbyte>();
            RegisterElement<Poco, short>();
            RegisterElement<Poco, ushort>();
            RegisterElement<Poco, int>();
            RegisterElement<Poco, uint>();

            RegisterElement<Poco, long>();
            RegisterElement<Poco, ulong>();
            RegisterElement<Poco, float>();
            RegisterElement<Poco, double>();
            RegisterElement<Poco, decimal>();

            RegisterElement<Poco, bool?>();
            RegisterElement<Poco, char?>();
            RegisterElement<Poco, byte?>();
            RegisterElement<Poco, sbyte?>();
            RegisterElement<Poco, short?>();
            RegisterElement<Poco, ushort?>();
            RegisterElement<Poco, int?>();
            RegisterElement<Poco, uint?>();
            RegisterElement<Poco, long?>();
            RegisterElement<Poco, ulong?>();
            RegisterElement<Poco, float?>();
            RegisterElement<Poco, double?>();
            RegisterElement<Poco, decimal?>();

            RegisterElement<Poco, JsonValue>();
            RegisterTypeForAot<DayOfWeek>(); // used by DateTime

            // register built in structs
            RegisterTypeForAot<Guid>();
            RegisterTypeForAot<TimeSpan>();
            RegisterTypeForAot<DateTime>();
            RegisterTypeForAot<DateTimeOffset>();

            RegisterTypeForAot<Guid?>();
            RegisterTypeForAot<TimeSpan?>();
            RegisterTypeForAot<DateTime?>();
            RegisterTypeForAot<DateTimeOffset?>();
        }

        [Preserve]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void RegisterTypeForAot<T>()
        {
            AotConfig.RegisterSerializers<T>();
        }

        [Preserve]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void RegisterElement<T, TElement>()
        {
            AotConfig.RegisterSerializers<TElement>();
            AotConfig.RegisterElement<T, TElement, Text.Json.JsonTypeSerializer>();
            AotConfig.RegisterElement<T, TElement, Text.Jsv.JsvTypeSerializer>();
        }

        [Preserve(AllMembers = true)]
        internal class AotConfig
        {
            internal static JsReader<Text.Json.JsonTypeSerializer> jsonReader;
            internal static JsWriter<Text.Json.JsonTypeSerializer> jsonWriter;
            internal static JsReader<Text.Jsv.JsvTypeSerializer> jsvReader;
            internal static JsWriter<Text.Jsv.JsvTypeSerializer> jsvWriter;
            internal static Text.Json.JsonTypeSerializer jsonSerializer;
            internal static Text.Jsv.JsvTypeSerializer jsvSerializer;

            [Preserve]
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            static AotConfig()
            {
                jsonSerializer = new Text.Json.JsonTypeSerializer();
                jsvSerializer = new Text.Jsv.JsvTypeSerializer();
                jsonReader = new JsReader<Text.Json.JsonTypeSerializer>();
                jsonWriter = new JsWriter<Text.Json.JsonTypeSerializer>();
                jsvReader = new JsReader<Text.Jsv.JsvTypeSerializer>();
                jsvWriter = new JsWriter<Text.Jsv.JsvTypeSerializer>();
            }

            [Preserve]
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal static void RegisterSerializers<T>()
            {
                Register<T, Text.Json.JsonTypeSerializer>();
                jsonSerializer.GetParseFn<T>();
                jsonSerializer.GetWriteFn<T>();
                jsonReader.GetParseFn<T>();
                jsonWriter.GetWriteFn<T>();

                Register<T, Text.Jsv.JsvTypeSerializer>();
                jsvSerializer.GetParseFn<T>();
                jsvSerializer.GetWriteFn<T>();
                jsvReader.GetParseFn<T>();
                jsvWriter.GetWriteFn<T>();

                CsvSerializer.InitAot<T>();
                QueryStringWriter<T>.WriteFn();
            }

            [Preserve]
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public static ParseStringDelegate GetParseFn(Type type)
            {
                var parseFn = Text.Json.JsonTypeSerializer.Instance.GetParseFn(type);
                return parseFn;
            }

            [Preserve]
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal static void Register<T, TSerializer>() where TSerializer : ITypeSerializer
            {
                Text.Json.JsonReader.InitAot<T>();
                Text.Json.JsonWriter.InitAot<T>();

                Text.Jsv.JsvReader.InitAot<T>();
                Text.Jsv.JsvWriter.InitAot<T>();

                var hold = new object[]
                {
                    new List<T>(),
                    new T[0],
                    new Dictionary<string, string>(),
                    new Dictionary<string, T>(),
                    new HashSet<T>(),
                };

                JsConfig<T>.ExcludeTypeInfo = false;

                var r1 = JsConfig<T>.OnDeserializedFn;
                var r2 = JsConfig<T>.HasDeserializeFn;
                var r3 = JsConfig<T>.SerializeFn;
                var r4 = JsConfig<T>.DeSerializeFn;
                var r5 = TypeConfig<T>.Properties;

                JsReader<TSerializer>.InitAot<T>();
                JsWriter<TSerializer>.InitAot<T>();
            }

            [Preserve]
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal static void RegisterElement<T, TElement, TSerializer>() where TSerializer : ITypeSerializer
            {
                DeserializeDictionary<TSerializer>.ParseDictionary<T, TElement>(null, null, null, null);
                DeserializeDictionary<TSerializer>.ParseDictionary<TElement, T>(null, null, null, null);

                ToStringDictionaryMethods<T, TElement, TSerializer>.WriteIDictionary(null, null, null, null);
                ToStringDictionaryMethods<TElement, T, TSerializer>.WriteIDictionary(null, null, null, null);

                // Include List deserialisations from the Register<> method above.  This solves issue where List<Guid> properties on responses deserialise to null.
                // No idea why this is happening because there is no visible exception raised.  Suspect IOS is swallowing an AOT exception somewhere.
                DeserializeArrayWithElements<TElement, TSerializer>.ParseGenericArray(null, null);
                DeserializeListWithElements<TElement, TSerializer>.ParseGenericList(null, null, null);

                // Cannot use the line below for some unknown reason - when trying to compile to run on device, mtouch bombs during native code compile.
                // Something about this line or its inner workings is offensive to mtouch. Luckily this was not needed for my List<Guide> issue.
                // DeserializeCollection<JsonTypeSerializer>.ParseCollection<TElement>(null, null, null);

                TranslateListWithElements<TElement>.LateBoundTranslateToGenericICollection(null, typeof(List<TElement>));
                TranslateListWithConvertibleElements<TElement, TElement>.LateBoundTranslateToGenericICollection(null, typeof(List<TElement>));
            }
        }
    }

}

#endif
