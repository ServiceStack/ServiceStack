//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if NETCORE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;
using System.Globalization;
using System.Reflection;
using System.Net;

using System.Collections.Specialized;

namespace ServiceStack
{
    public class NetStandardPclExport : PclExport
    {
        public static NetStandardPclExport Provider = new();

        static string[] allDateTimeFormats = {
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ",
            "HH:mm:ss.FFFFFFF",
            "HH:mm:ss.FFFFFFFZ",
            "HH:mm:ss.FFFFFFFzzzzzz",
            "yyyy-MM-dd",
            "yyyy-MM-ddZ",
            "yyyy-MM-ddzzzzzz",
            "yyyy-MM",
            "yyyy-MMZ",
            "yyyy-MMzzzzzz",
            "yyyy",
            "yyyyZ",
            "yyyyzzzzzz",
            "--MM-dd",
            "--MM-ddZ",
            "--MM-ddzzzzzz",
            "---dd",
            "---ddZ",
            "---ddzzzzzz",
            "--MM--",
            "--MM--Z",
            "--MM--zzzzzz",
        };

        public NetStandardPclExport()
        {
            this.PlatformName = Platforms.NetStandard;
            this.DirSep = Path.DirectorySeparatorChar;
        }

        public const string AppSettingsKey = "servicestack:license";
        public const string EnvironmentKey = "SERVICESTACK_LICENSE";

        public override void RegisterLicenseFromConfig()
        {
            //Automatically register license key stored in <appSettings/> is done in .NET Core AppHost

            //or SERVICESTACK_LICENSE Environment variable
            var licenceKeyText = GetEnvironmentVariable(EnvironmentKey)?.Trim();
            if (!string.IsNullOrEmpty(licenceKeyText))
            {
                LicenseUtils.RegisterLicense(licenceKeyText);
            }
        }

        public override string MapAbsolutePath(string relativePath, string appendPartialPathModifier)
        {
            if (relativePath.StartsWith("~"))
            {
                var assemblyDirectoryPath = AppContext.BaseDirectory;

                // Escape the assembly bin directory to the hostname directory
                var hostDirectoryPath = appendPartialPathModifier != null
                    ? assemblyDirectoryPath + appendPartialPathModifier
                    : assemblyDirectoryPath;

                return Path.GetFullPath(relativePath.Replace("~", hostDirectoryPath));
            }
            return relativePath;
        }        

        public static PclExport Configure()
        {
            Configure(Provider);
            return Provider;
        }

        public override string GetAssemblyCodeBase(Assembly assembly)
        {
            var dll = typeof(PclExport).Assembly;
            var pi = dll.GetType().GetProperty("CodeBase");
            var codeBase = pi?.GetProperty(dll).ToString();
            return codeBase;
        }

        public override string GetAssemblyPath(Type source)
        {
            var codeBase = GetAssemblyCodeBase(source.GetTypeInfo().Assembly);
            if (codeBase == null)
                return null;

            var assemblyUri = new Uri(codeBase);
            return assemblyUri.LocalPath;
        }
        
        public override bool InSameAssembly(Type t1, Type t2)
        {
            return t1.Assembly == t2.Assembly;
        }

        public override Type GetGenericCollectionType(Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(t =>
                t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(ICollection<>));
        }

        public override DateTime ParseXsdDateTimeAsUtc(string dateTimeStr)
        {
            return DateTime.ParseExact(dateTimeStr, allDateTimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowLeadingWhite|DateTimeStyles.AllowTrailingWhite|DateTimeStyles.AdjustToUniversal)
                     .Prepare(parsedAsUtc: true);
        }

        //public override DateTime ToStableUniversalTime(DateTime dateTime)
        //{
        //    // .Net 2.0 - 3.5 has an issue with DateTime.ToUniversalTime, but works ok with TimeZoneInfo.ConvertTimeToUtc.
        //    // .Net 4.0+ does this under the hood anyway.
        //    return TimeZoneInfo.ConvertTimeToUtc(dateTime);
        //}

        public override ParseStringDelegate GetSpecializedCollectionParseMethod<TSerializer>(Type type)
        {
            if (type == typeof(StringCollection))
            {
                return v => ParseStringCollection<TSerializer>(v.AsSpan());
            }
            return null;
        }

        public override ParseStringSpanDelegate GetSpecializedCollectionParseStringSpanMethod<TSerializer>(Type type)
        {
            if (type == typeof(StringCollection))
            {
                return ParseStringCollection<TSerializer>;
            }
            return null;
        }

        private static StringCollection ParseStringCollection<TSerializer>(ReadOnlySpan<char> value) where TSerializer : ITypeSerializer
        {
            if ((value = DeserializeListWithElements<TSerializer>.StripList(value)).IsNullOrEmpty()) 
                return value.IsEmpty ? null : new StringCollection();

            var result = new StringCollection();

            if (value.Length > 0)
            {
                foreach (var item in DeserializeListWithElements<TSerializer>.ParseStringList(value))
                {
                    result.Add(item);
                }
            }

            return result;
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
                if (JsonReader<T>.Parse(default(ReadOnlySpan<char>)) != null) i++;
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
                DeserializeDictionary<TSerializer>.ParseDictionary<T, TElement>(default(ReadOnlySpan<char>), null, null, null);
                DeserializeDictionary<TSerializer>.ParseDictionary<TElement, T>(default(ReadOnlySpan<char>), null, null, null);

                ToStringDictionaryMethods<T, TElement, TSerializer>.WriteIDictionary(null, null, null, null);
                ToStringDictionaryMethods<TElement, T, TSerializer>.WriteIDictionary(null, null, null, null);

                // Include List deserialisations from the Register<> method above.  This solves issue where List<Guid> properties on responses deserialise to null.
                // No idea why this is happening because there is no visible exception raised.  Suspect IOS is swallowing an AOT exception somewhere.
                DeserializeArrayWithElements<TElement, TSerializer>.ParseGenericArray(default(ReadOnlySpan<char>), null);
                DeserializeListWithElements<TElement, TSerializer>.ParseGenericList(default(ReadOnlySpan<char>), null, null);

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
