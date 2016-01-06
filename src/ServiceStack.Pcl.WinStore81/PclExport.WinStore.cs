//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if NETFX_CORE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;

namespace ServiceStack
{
    public class WinStorePclExport : PclExport
    {
        public new static WinStorePclExport Provider = new WinStorePclExport();

        public WinStorePclExport()
        {
            this.PlatformName = Platforms.WindowsStore;
        }

        public static PclExport Configure()
        {
            Configure(Provider);
            return Provider;
        }

        public override string ReadAllText(string filePath)
        {
            var task = Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
            task.AsTask().Wait();

            var file = task.GetResults();

            var streamTask = file.OpenStreamForReadAsync();
            streamTask.Wait();

            var fileStream = streamTask.Result;

            return new StreamReader(fileStream).ReadToEnd();
        }

        public override bool FileExists(string filePath)
        {
            try
            {
                var task = Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                //no exception means file exists
                return true;
            }
            catch (Exception ex)
            {
                //find out through exception 
                return false;
            }
        }

        public override void WriteLine(string line)
        {
            System.Diagnostics.Debug.WriteLine(line);
        }

        public override void WriteLine(string format, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine(format, args);
        }

        public override Assembly[] GetAllAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        private sealed class AppDomain
        {
            public static AppDomain CurrentDomain { get; private set; }
            public static Assembly[] cacheObj = null;

            static AppDomain()
            {
                CurrentDomain = new AppDomain();
            }

            public Assembly[] GetAssemblies()
            {
                return cacheObj ?? GetAssemblyListAsync().Result.ToArray();
            }

            private async System.Threading.Tasks.Task<IEnumerable<Assembly>> GetAssemblyListAsync()
            {
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

                var assemblies = new List<Assembly>();
                foreach (Windows.Storage.StorageFile file in await folder.GetFilesAsync())
                {
                    if (file.FileType == ".dll" || file.FileType == ".exe")
                    {
                        try
                        {
                            var filename = file.Name.Substring(0, file.Name.Length - file.FileType.Length);
                            AssemblyName name = new AssemblyName() {Name = filename};
                            Assembly asm = Assembly.Load(name);
                            assemblies.Add(asm);
                        }
                        catch (Exception)
                        {
                            // Invalid WinRT assembly!
                        }
                    }
                }

                cacheObj = assemblies.ToArray();

                return cacheObj;
            }
        }

        public override string GetAssemblyCodeBase(Assembly assembly)
        {
            return assembly.GetName().FullName;
        }

        //public override DateTime ToStableUniversalTime(DateTime dateTime)
        //{
        //    // .Net 2.0 - 3.5 has an issue with DateTime.ToUniversalTime, but works ok with TimeZoneInfo.ConvertTimeToUtc.
        //    // .Net 4.0+ does this under the hood anyway.
        //    return TimeZoneInfo.ConvertTimeToUtc(dateTime);
        //}

        public override void VerifyInAssembly(Type accessType, ICollection<string> assemblyNames)
        {
        }

        public static void InitForAot()
        {
        }

        internal class Poco
        {
            public string Dummy { get; set; }
        }

        public override void RegisterForAot()
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

            //RegisterElement<Poco, JsonValue>();

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

        public static void RegisterTypeForAot<T>()
        {
            AotConfig.RegisterSerializers<T>();
        }

        public static void RegisterQueryStringWriter()
        {
            var i = 0;
            if (QueryStringWriter<Poco>.WriteFn() != null) i++;
        }

        public static int RegisterElement<T, TElement>()
        {
            var i = 0;
            i += AotConfig.RegisterSerializers<TElement>();
            AotConfig.RegisterElement<T, TElement, JsonTypeSerializer>();
            AotConfig.RegisterElement<T, TElement, Text.Jsv.JsvTypeSerializer>();
            return i;
        }

        internal class AotConfig
        {
            internal static JsReader<JsonTypeSerializer> jsonReader;
            internal static JsWriter<JsonTypeSerializer> jsonWriter;
            internal static JsReader<Text.Jsv.JsvTypeSerializer> jsvReader;
            internal static JsWriter<Text.Jsv.JsvTypeSerializer> jsvWriter;
            internal static JsonTypeSerializer jsonSerializer;
            internal static Text.Jsv.JsvTypeSerializer jsvSerializer;

            static AotConfig()
            {
                jsonSerializer = new JsonTypeSerializer();
                jsvSerializer = new Text.Jsv.JsvTypeSerializer();
                jsonReader = new JsReader<JsonTypeSerializer>();
                jsonWriter = new JsWriter<JsonTypeSerializer>();
                jsvReader = new JsReader<Text.Jsv.JsvTypeSerializer>();
                jsvWriter = new JsWriter<Text.Jsv.JsvTypeSerializer>();
            }

            internal static int RegisterSerializers<T>()
            {
                var i = 0;
                i += Register<T, JsonTypeSerializer>();
                if (jsonSerializer.GetParseFn<T>() != null) i++;
                if (jsonSerializer.GetWriteFn<T>() != null) i++;
                if (jsonReader.GetParseFn<T>() != null) i++;
                if (jsonWriter.GetWriteFn<T>() != null) i++;

                i += Register<T, Text.Jsv.JsvTypeSerializer>();
                if (jsvSerializer.GetParseFn<T>() != null) i++;
                if (jsvSerializer.GetWriteFn<T>() != null) i++;
                if (jsvReader.GetParseFn<T>() != null) i++;
                if (jsvWriter.GetWriteFn<T>() != null) i++;

                //RegisterCsvSerializer<T>();
                RegisterQueryStringWriter();
                return i;
            }

            internal static void RegisterCsvSerializer<T>()
            {
                CsvSerializer<T>.WriteFn();
                CsvSerializer<T>.WriteObject(null, null);
                CsvWriter<T>.Write(null, default(IEnumerable<T>));
                CsvWriter<T>.WriteRow(null, default(T));
            }

            public static ParseStringDelegate GetParseFn(Type type)
            {
                var parseFn = JsonTypeSerializer.Instance.GetParseFn(type);
                return parseFn;
            }

            internal static int Register<T, TSerializer>() where TSerializer : ITypeSerializer
            {
                var i = 0;

                if (JsonWriter<T>.WriteFn() != null) i++;
                if (JsonWriter.Instance.GetWriteFn<T>() != null) i++;
                if (JsonReader.Instance.GetParseFn<T>() != null) i++;
                if (JsonReader<T>.Parse(null) != null) i++;
                if (JsonReader<T>.GetParseFn() != null) i++;
                //if (JsWriter.GetTypeSerializer<JsonTypeSerializer>().GetWriteFn<T>() != null) i++;
                if (new List<T>() != null) i++;
                if (new T[0] != null) i++;

                JsConfig<T>.ExcludeTypeInfo = false;

                if (JsConfig<T>.OnDeserializedFn != null) i++;
                if (JsConfig<T>.HasDeserializeFn) i++;
                if (JsConfig<T>.SerializeFn != null) i++;
                if (JsConfig<T>.DeSerializeFn != null) i++;
                //JsConfig<T>.SerializeFn = arg => "";
                //JsConfig<T>.DeSerializeFn = arg => default(T);
                if (TypeConfig<T>.Properties != null) i++;

                WriteListsOfElements<T, TSerializer>.WriteList(null, null);
                WriteListsOfElements<T, TSerializer>.WriteIList(null, null);
                WriteListsOfElements<T, TSerializer>.WriteEnumerable(null, null);
                WriteListsOfElements<T, TSerializer>.WriteListValueType(null, null);
                WriteListsOfElements<T, TSerializer>.WriteIListValueType(null, null);
                WriteListsOfElements<T, TSerializer>.WriteGenericArrayValueType(null, null);
                WriteListsOfElements<T, TSerializer>.WriteArray(null, null);

                TranslateListWithElements<T>.LateBoundTranslateToGenericICollection(null, null);
                TranslateListWithConvertibleElements<T, T>.LateBoundTranslateToGenericICollection(null, null);

                QueryStringWriter<T>.WriteObject(null, null);
                return i;
            }

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
