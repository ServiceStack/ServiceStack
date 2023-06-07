#if NETFX
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;
using System.Reflection.Emit;

namespace ServiceStack
{
    public class NetFxPclExport : PclExport
    {
        public static NetFxPclExport Provider = new NetFxPclExport();

        public NetFxPclExport()
        {
            this.DirSep = Path.DirectorySeparatorChar;
            this.AltDirSep = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
            this.RegexOptions = RegexOptions.Compiled;
            this.InvariantComparison = StringComparison.InvariantCulture;
            this.InvariantComparisonIgnoreCase = StringComparison.InvariantCultureIgnoreCase;
            this.InvariantComparer = StringComparer.InvariantCulture;
            this.InvariantComparerIgnoreCase = StringComparer.InvariantCultureIgnoreCase;

            this.PlatformName = Platforms.NetFX;
            ReflectionOptimizer.Instance = EmitReflectionOptimizer.Provider;
        }

        public static PclExport Configure()
        {
            Configure(Provider);
            return Provider;
        }

        public override string ToInvariantUpper(char value)
        {
            return value.ToString(CultureInfo.InvariantCulture).ToUpper();
        }

        public override bool IsAnonymousType(Type type)
        {
            return type.HasAttribute<CompilerGeneratedAttribute>()
                   && type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>", StringComparison.Ordinal) || type.Name.StartsWith("VB$", StringComparison.Ordinal))
                   && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public const string AppSettingsKey = "servicestack:license";
        public const string EnvironmentKey = "SERVICESTACK_LICENSE";

        public override void RegisterLicenseFromConfig()
        {
            string licenceKeyText;
            try
            {
                //Automatically register license key stored in <appSettings/>
                licenceKeyText = System.Configuration.ConfigurationManager.AppSettings[AppSettingsKey];
                if (!string.IsNullOrEmpty(licenceKeyText))
                {
                    LicenseUtils.RegisterLicense(licenceKeyText);
                    return;
                }
            }
            catch (NotSupportedException) { return; } // Ignore Unity/IL2CPP Exception
            catch (Exception ex)
            {
                licenceKeyText = Environment.GetEnvironmentVariable(EnvironmentKey)?.Trim();
                if (string.IsNullOrEmpty(licenceKeyText))
                    throw;
                try
                {
                    LicenseUtils.RegisterLicense(licenceKeyText);
                }
                catch
                {
                    throw ex;
                }
            }

            //or SERVICESTACK_LICENSE Environment variable
            licenceKeyText = Environment.GetEnvironmentVariable(EnvironmentKey)?.Trim();
            if (!string.IsNullOrEmpty(licenceKeyText))
            {
                LicenseUtils.RegisterLicense(licenceKeyText);
            }
        }

        public override async Task WriteAndFlushAsync(Stream stream, byte[] bytes)
        {
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigAwait();
            await stream.FlushAsync().ConfigAwait();
        }

        public override void AddCompression(WebRequest webReq)
        {
            var httpReq = (HttpWebRequest)webReq;
            httpReq.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            httpReq.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        public override Stream GetRequestStream(WebRequest webRequest)
        {
            return webRequest.GetRequestStream();
        }

        public override WebResponse GetResponse(WebRequest webRequest)
        {
            return webRequest.GetResponse();
        }

#if !LITE
        public override bool IsDebugBuild(Assembly assembly)
        {
            return assembly.AllAttributes()
                           .OfType<System.Diagnostics.DebuggableAttribute>()
                           .Select(attr => attr.IsJITTrackingEnabled)
                           .FirstOrDefault();
        }
#endif

        public override string MapAbsolutePath(string relativePath, string appendPartialPathModifier)
        {
            if (relativePath.StartsWith("~"))
            {
                var assemblyDirectoryPath = Path.GetDirectoryName(new Uri(typeof(PathUtils).Assembly.EscapedCodeBase).LocalPath);

                // Escape the assembly bin directory to the hostname directory
                var hostDirectoryPath = appendPartialPathModifier != null
                                            ? assemblyDirectoryPath + appendPartialPathModifier
                                            : assemblyDirectoryPath;

                return Path.GetFullPath(relativePath.Replace("~", hostDirectoryPath));
            }
            return relativePath;
        }

        public override Assembly LoadAssembly(string assemblyPath)
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        public override void AddHeader(WebRequest webReq, string name, string value)
        {
            webReq.Headers.Add(name, value);
        }

        public override Type FindType(string typeName, string assemblyName)
        {
            var binPath = AssemblyUtils.GetAssemblyBinPath(Assembly.GetExecutingAssembly());
            Assembly assembly = null;
            var assemblyDllPath = binPath + $"{assemblyName}.dll";
            if (File.Exists(assemblyDllPath))
            {
                assembly = AssemblyUtils.LoadAssembly(assemblyDllPath);
            }
            var assemblyExePath = binPath + $"{assemblyName}.exe";
            if (File.Exists(assemblyExePath))
            {
                assembly = AssemblyUtils.LoadAssembly(assemblyExePath);
            }
            return assembly != null ? assembly.GetType(typeName) : null;
        }

        public override string GetAssemblyCodeBase(Assembly assembly)
        {
            return assembly.CodeBase;
        }

        public override string GetAssemblyPath(Type source)
        {
            var assemblyUri = new Uri(source.Assembly.EscapedCodeBase);
            return assemblyUri.LocalPath;
        }

        public override bool InSameAssembly(Type t1, Type t2)
        {
            return t1.Assembly == t2.Assembly;
        }

        public override Type GetGenericCollectionType(Type type)
        {
            return type.FindInterfaces((t, critera) =>
                t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(ICollection<>), null).FirstOrDefault();
        }

        public override string ToXsdDateTimeString(DateTime dateTime)
        {
#if !LITE
            return System.Xml.XmlConvert.ToString(dateTime.ToStableUniversalTime(), System.Xml.XmlDateTimeSerializationMode.Utc);
#else
            return dateTime.ToStableUniversalTime().ToString(DateTimeSerializer.XsdDateTimeFormat);
#endif
        }

        public override string ToLocalXsdDateTimeString(DateTime dateTime)
        {
#if !LITE
            return System.Xml.XmlConvert.ToString(dateTime, System.Xml.XmlDateTimeSerializationMode.Local);
#else
            return dateTime.ToString(DateTimeSerializer.XsdDateTimeFormat);
#endif
        }

        public override DateTime ParseXsdDateTime(string dateTimeStr)
        {
#if !LITE
            return System.Xml.XmlConvert.ToDateTime(dateTimeStr, System.Xml.XmlDateTimeSerializationMode.Utc);
#else
            return DateTime.ParseExact(dateTimeStr, DateTimeSerializer.XsdDateTimeFormat, CultureInfo.InvariantCulture);
#endif
        }

#if !LITE
        public override DateTime ParseXsdDateTimeAsUtc(string dateTimeStr)
        {
            return System.Xml.XmlConvert.ToDateTime(dateTimeStr, System.Xml.XmlDateTimeSerializationMode.Utc).Prepare(parsedAsUtc: true);
        }
#endif

        public override DateTime ToStableUniversalTime(DateTime dateTime)
        {
            // .Net 2.0 - 3.5 has an issue with DateTime.ToUniversalTime, but works ok with TimeZoneInfo.ConvertTimeToUtc.
            // .Net 4.0+ does this under the hood anyway.
            return TimeZoneInfo.ConvertTimeToUtc(dateTime);
        }

        public override ParseStringDelegate GetDictionaryParseMethod<TSerializer>(Type type)
        {
            if (type == typeof(Hashtable))
            {
                return SerializerUtils<TSerializer>.ParseHashtable;
            }
            return null;
        }

        public override ParseStringSpanDelegate GetDictionaryParseStringSpanMethod<TSerializer>(Type type)
        {
            if (type == typeof(Hashtable))
            {
                return SerializerUtils<TSerializer>.ParseHashtable;
            }
            return null;
        }

        public override ParseStringDelegate GetSpecializedCollectionParseMethod<TSerializer>(Type type)
        {
            if (type == typeof(StringCollection))
            {
                return SerializerUtils<TSerializer>.ParseStringCollection<TSerializer>;
            }
            return null;
        }

        public override ParseStringSpanDelegate GetSpecializedCollectionParseStringSpanMethod<TSerializer>(Type type)
        {
            if (type == typeof(StringCollection))
            {
                return SerializerUtils<TSerializer>.ParseStringCollection<TSerializer>;
            }
            return null;
        }


        public override ParseStringDelegate GetJsReaderParseMethod<TSerializer>(Type type)
        {
#if !LITE
            if (type.IsAssignableFrom(typeof(System.Dynamic.IDynamicMetaObjectProvider)) ||
                type.HasInterface(typeof(System.Dynamic.IDynamicMetaObjectProvider)))
            {
                return DeserializeDynamic<TSerializer>.Parse;
            }
#endif
            return null;
        }

        public override ParseStringSpanDelegate GetJsReaderParseStringSpanMethod<TSerializer>(Type type)
        {
#if !LITE
            if (type.IsAssignableFrom(typeof(System.Dynamic.IDynamicMetaObjectProvider)) ||
                type.HasInterface(typeof(System.Dynamic.IDynamicMetaObjectProvider)))
            {
                return DeserializeDynamic<TSerializer>.ParseStringSpan;
            }
#endif
            return null;
        }

        public override void CloseStream(Stream stream)
        {
            stream.Close();
        }

        public override LicenseKey VerifyLicenseKeyText(string licenseKeyText)
        {
            if (!licenseKeyText.VerifyLicenseKeyText(out LicenseKey key))
                throw new ArgumentException("licenseKeyText");

            return key;
        }

        public override void BeginThreadAffinity()
        {
            Thread.BeginThreadAffinity();
        }

        public override void EndThreadAffinity()
        {
            Thread.EndThreadAffinity();
        }

        public override void Config(HttpWebRequest req,
            bool? allowAutoRedirect = null,
            TimeSpan? timeout = null,
            TimeSpan? readWriteTimeout = null,
            string userAgent = null,
            bool? preAuthenticate = null)
        {
            req.MaximumResponseHeadersLength = int.MaxValue; //throws "The message length limit was exceeded" exception
            if (allowAutoRedirect.HasValue) req.AllowAutoRedirect = allowAutoRedirect.Value;
            if (readWriteTimeout.HasValue) req.ReadWriteTimeout = (int)readWriteTimeout.Value.TotalMilliseconds;
            if (timeout.HasValue) req.Timeout = (int)timeout.Value.TotalMilliseconds;
            if (userAgent != null) req.UserAgent = userAgent;
            if (preAuthenticate.HasValue) req.PreAuthenticate = preAuthenticate.Value;
        }

        public override DataContractAttribute GetWeakDataContract(Type type)
        {
            return type.GetWeakDataContract();
        }

        public override DataMemberAttribute GetWeakDataMember(PropertyInfo pi)
        {
            return pi.GetWeakDataMember();
        }

        public override DataMemberAttribute GetWeakDataMember(FieldInfo pi)
        {
            return pi.GetWeakDataMember();
        }
    }

    internal class SerializerUtils<TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        private static int VerifyAndGetStartIndex(ReadOnlySpan<char> value, Type createMapType)
        {
            var index = 0;
            if (!Serializer.EatMapStartChar(value, ref index))
            {
                //Don't throw ex because some KeyValueDataContractDeserializer don't have '{}'
                Tracer.Instance.WriteDebug("WARN: Map definitions should start with a '{0}', expecting serialized type '{1}', got string starting with: {2}",
                    JsWriter.MapStartChar, createMapType != null ? createMapType.Name : "Dictionary<,>", value.Substring(0, value.Length < 50 ? value.Length : 50));
            }
            return index;
        }

        public static Hashtable ParseHashtable(string value) => ParseHashtable(value.AsSpan());

        public static Hashtable ParseHashtable(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
                return null;

            var index = VerifyAndGetStartIndex(value, typeof(Hashtable));

            var result = new Hashtable();

            if (JsonTypeSerializer.IsEmptyMap(value, index)) return result;

            var valueLength = value.Length;
            while (index < valueLength)
            {
                var keyValue = Serializer.EatMapKey(value, ref index);
                Serializer.EatMapKeySeperator(value, ref index);
                var elementValue = Serializer.EatValue(value, ref index);
                if (keyValue.IsEmpty) continue;

                var mapKey = keyValue.ToString();
                var mapValue = elementValue.Value();

                result[mapKey] = mapValue;

                Serializer.EatItemSeperatorOrMapEndChar(value, ref index);
            }

            return result;
        }

        public static StringCollection ParseStringCollection<TS>(string value) where TS : ITypeSerializer => ParseStringCollection<TS>(value.AsSpan());


        public static StringCollection ParseStringCollection<TS>(ReadOnlySpan<char> value) where TS : ITypeSerializer
        {
            if ((value = DeserializeListWithElements<TS>.StripList(value)).IsNullOrEmpty()) 
                return value.IsEmpty ? null : new StringCollection();
    
            return ToStringCollection(DeserializeListWithElements<TSerializer>.ParseStringList(value));
        }

        public static StringCollection ToStringCollection(List<string> items)
        {
            var to = new StringCollection();
            foreach (var item in items)
            {
                to.Add(item);
            }
            return to;
        }
    }

    public static class PclExportExt
    {
        //XmlSerializer
        public static void CompressToStream<TXmlDto>(TXmlDto from, Stream stream)
        {
            using (var deflateStream = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Compress))
            using (var xw = new System.Xml.XmlTextWriter(deflateStream, Encoding.UTF8))
            {
                var serializer = new DataContractSerializer(from.GetType());
                serializer.WriteObject(xw, from);
                xw.Flush();
            }
        }

        public static byte[] Compress<TXmlDto>(TXmlDto from)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                CompressToStream(from, ms);

                return ms.ToArray();
            }
        }

        //ReflectionExtensions
        const string DataContract = "DataContractAttribute";

        public static DataContractAttribute GetWeakDataContract(this Type type)
        {
            var attr = type.AllAttributes().FirstOrDefault(x => x.GetType().Name == DataContract);
            if (attr != null)
            {
                var attrType = attr.GetType();

                var accessor = TypeProperties.Get(attr.GetType());

                return new DataContractAttribute
                {
                    Name = (string)accessor.GetPublicGetter("Name")(attr),
                    Namespace = (string)accessor.GetPublicGetter("Namespace")(attr),
                };
            }
            return null;
        }

        public static DataMemberAttribute GetWeakDataMember(this PropertyInfo pi)
        {
            var attr = pi.AllAttributes().FirstOrDefault(x => x.GetType().Name == ReflectionExtensions.DataMember);
            if (attr != null)
            {
                var attrType = attr.GetType();

                var accessor = TypeProperties.Get(attr.GetType());

                var newAttr = new DataMemberAttribute
                {
                    Name = (string)accessor.GetPublicGetter("Name")(attr),
                    EmitDefaultValue = (bool)accessor.GetPublicGetter("EmitDefaultValue")(attr),
                    IsRequired = (bool)accessor.GetPublicGetter("IsRequired")(attr),
                };

                var order = (int)accessor.GetPublicGetter("Order")(attr);
                if (order >= 0)
                    newAttr.Order = order; //Throws Exception if set to -1

                return newAttr;
            }
            return null;
        }

        public static DataMemberAttribute GetWeakDataMember(this FieldInfo pi)
        {
            var attr = pi.AllAttributes().FirstOrDefault(x => x.GetType().Name == ReflectionExtensions.DataMember);
            if (attr != null)
            {
                var attrType = attr.GetType();

                var accessor = TypeProperties.Get(attr.GetType());

                var newAttr = new DataMemberAttribute
                {
                    Name = (string)accessor.GetPublicGetter("Name")(attr),
                    EmitDefaultValue = (bool)accessor.GetPublicGetter("EmitDefaultValue")(attr),
                    IsRequired = (bool)accessor.GetPublicGetter("IsRequired")(attr),
                };

                var order = (int)accessor.GetPublicGetter("Order")(attr);
                if (order >= 0)
                    newAttr.Order = order; //Throws Exception if set to -1

                return newAttr;
            }
            return null;
        }
    }
}

//Not using it here, but @marcgravell's stuff is too good not to include
// http://code.google.com/p/fast-member/ Apache License 2.0
namespace ServiceStack.Text.FastMember
{
    /// <summary>
    /// Represents an individual object, allowing access to members by-name
    /// </summary>
    public abstract class ObjectAccessor
    {
        /// <summary>
        /// Get or Set the value of a named member for the underlying object
        /// </summary>
        public abstract object this[string name] { get; set; }
        /// <summary>
        /// The object represented by this instance
        /// </summary>
        public abstract object Target { get; }
        /// <summary>
        /// Use the target types definition of equality
        /// </summary>
        public override bool Equals(object obj)
        {
            return Target.Equals(obj);
        }
        /// <summary>
        /// Obtain the hash of the target object
        /// </summary>
        public override int GetHashCode()
        {
            return Target.GetHashCode();
        }
        /// <summary>
        /// Use the target's definition of a string representation
        /// </summary>
        public override string ToString()
        {
            return Target.ToString();
        }

        /// <summary>
        /// Wraps an individual object, allowing by-name access to that instance
        /// </summary>
        public static ObjectAccessor Create(object target)
        {
            if (target == null) throw new ArgumentNullException("target");
            //IDynamicMetaObjectProvider dlr = target as IDynamicMetaObjectProvider;
            //if (dlr != null) return new DynamicWrapper(dlr); // use the DLR
            return new TypeAccessorWrapper(target, TypeAccessor.Create(target.GetType()));
        }

        sealed class TypeAccessorWrapper : ObjectAccessor
        {
            private readonly object target;
            private readonly TypeAccessor accessor;
            public TypeAccessorWrapper(object target, TypeAccessor accessor)
            {
                this.target = target;
                this.accessor = accessor;
            }
            public override object this[string name]
            {
                get { return accessor[target, name.ToUpperInvariant()]; }
                set { accessor[target, name.ToUpperInvariant()] = value; }
            }
            public override object Target
            {
                get { return target; }
            }
        }

        //sealed class DynamicWrapper : ObjectAccessor
        //{
        //    private readonly IDynamicMetaObjectProvider target;
        //    public override object Target
        //    {
        //        get { return target; }
        //    }
        //    public DynamicWrapper(IDynamicMetaObjectProvider target)
        //    {
        //        this.target = target;
        //    }
        //    public override object this[string name]
        //    {
        //        get { return CallSiteCache.GetValue(name, target); }
        //        set { CallSiteCache.SetValue(name, target, value); }
        //    }
        //}
    }

    /// <summary>
    /// Provides by-name member-access to objects of a given type
    /// </summary>
    public abstract class TypeAccessor
    {
        // hash-table has better read-without-locking semantics than dictionary
        private static readonly Hashtable typeLookyp = new Hashtable();

        /// <summary>
        /// Does this type support new instances via a parameterless constructor?
        /// </summary>
        public virtual bool CreateNewSupported => false;

        /// <summary>
        /// Create a new instance of this type
        /// </summary>
        public virtual object CreateNew() { throw new NotSupportedException(); }

        /// <summary>
        /// Provides a type-specific accessor, allowing by-name access for all objects of that type
        /// </summary>
        /// <remarks>The accessor is cached internally; a pre-existing accessor may be returned</remarks>
        public static TypeAccessor Create(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var obj = (TypeAccessor)typeLookyp[type];
            if (obj != null) return obj;

            lock (typeLookyp)
            {
                // double-check
                obj = (TypeAccessor)typeLookyp[type];
                if (obj != null) return obj;

                obj = CreateNew(type);

                typeLookyp[type] = obj;
                return obj;
            }
        }

        //sealed class DynamicAccessor : TypeAccessor
        //{
        //    public static readonly DynamicAccessor Singleton = new DynamicAccessor();
        //    private DynamicAccessor(){}
        //    public override object this[object target, string name]
        //    {
        //        get { return CallSiteCache.GetValue(name, target); }
        //        set { CallSiteCache.SetValue(name, target, value); }
        //    }
        //}

        private static AssemblyBuilder assembly;
        private static ModuleBuilder module;
        private static int counter;

        private static void WriteGetter(ILGenerator il, Type type, PropertyInfo[] props, FieldInfo[] fields, bool isStatic)
        {
            LocalBuilder loc = type.IsValueType ? il.DeclareLocal(type) : null;
            OpCode propName = isStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2, target = isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1;
            foreach (PropertyInfo prop in props)
            {
                if (prop.GetIndexParameters().Length != 0 || !prop.CanRead) continue;
                var getFn = prop.GetGetMethod();
                if (getFn == null) continue; //Mono

                Label next = il.DefineLabel();
                il.Emit(propName);
                il.Emit(OpCodes.Ldstr, prop.Name);
                il.EmitCall(OpCodes.Call, strinqEquals, null);
                il.Emit(OpCodes.Brfalse_S, next);
                // match:
                il.Emit(target);
                Cast(il, type, loc);
                il.EmitCall(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, getFn, null);
                if (prop.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, prop.PropertyType);
                }
                il.Emit(OpCodes.Ret);
                // not match:
                il.MarkLabel(next);
            }
            foreach (FieldInfo field in fields)
            {
                Label next = il.DefineLabel();
                il.Emit(propName);
                il.Emit(OpCodes.Ldstr, field.Name);
                il.EmitCall(OpCodes.Call, strinqEquals, null);
                il.Emit(OpCodes.Brfalse_S, next);
                // match:
                il.Emit(target);
                Cast(il, type, loc);
                il.Emit(OpCodes.Ldfld, field);
                if (field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, field.FieldType);
                }
                il.Emit(OpCodes.Ret);
                // not match:
                il.MarkLabel(next);
            }
            il.Emit(OpCodes.Ldstr, "name");
            il.Emit(OpCodes.Newobj, typeof(ArgumentOutOfRangeException).GetConstructor(new Type[] { typeof(string) }));
            il.Emit(OpCodes.Throw);
        }
        private static void WriteSetter(ILGenerator il, Type type, PropertyInfo[] props, FieldInfo[] fields, bool isStatic)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Ldstr, "Write is not supported for structs");
                il.Emit(OpCodes.Newobj, typeof(NotSupportedException).GetConstructor(new Type[] { typeof(string) }));
                il.Emit(OpCodes.Throw);
            }
            else
            {
                OpCode propName = isStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2,
                       target = isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1,
                       value = isStatic ? OpCodes.Ldarg_2 : OpCodes.Ldarg_3;
                LocalBuilder loc = type.IsValueType ? il.DeclareLocal(type) : null;
                foreach (PropertyInfo prop in props)
                {
                    if (prop.GetIndexParameters().Length != 0 || !prop.CanWrite) continue;
                    var setFn = prop.GetSetMethod();
                    if (setFn == null) continue; //Mono

                    Label next = il.DefineLabel();
                    il.Emit(propName);
                    il.Emit(OpCodes.Ldstr, prop.Name);
                    il.EmitCall(OpCodes.Call, strinqEquals, null);
                    il.Emit(OpCodes.Brfalse_S, next);
                    // match:
                    il.Emit(target);
                    Cast(il, type, loc);
                    il.Emit(value);
                    Cast(il, prop.PropertyType, null);
                    il.EmitCall(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, setFn, null);
                    il.Emit(OpCodes.Ret);
                    // not match:
                    il.MarkLabel(next);
                }
                foreach (FieldInfo field in fields)
                {
                    Label next = il.DefineLabel();
                    il.Emit(propName);
                    il.Emit(OpCodes.Ldstr, field.Name);
                    il.EmitCall(OpCodes.Call, strinqEquals, null);
                    il.Emit(OpCodes.Brfalse_S, next);
                    // match:
                    il.Emit(target);
                    Cast(il, type, loc);
                    il.Emit(value);
                    Cast(il, field.FieldType, null);
                    il.Emit(OpCodes.Stfld, field);
                    il.Emit(OpCodes.Ret);
                    // not match:
                    il.MarkLabel(next);
                }
                il.Emit(OpCodes.Ldstr, "name");
                il.Emit(OpCodes.Newobj, typeof(ArgumentOutOfRangeException).GetConstructor(new Type[] { typeof(string) }));
                il.Emit(OpCodes.Throw);
            }
        }
        private static readonly MethodInfo strinqEquals = typeof(string).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) });

        sealed class DelegateAccessor : TypeAccessor
        {
            private readonly Func<object, string, object> getter;
            private readonly Action<object, string, object> setter;
            private readonly Func<object> ctor;
            public DelegateAccessor(Func<object, string, object> getter, Action<object, string, object> setter, Func<object> ctor)
            {
                this.getter = getter;
                this.setter = setter;
                this.ctor = ctor;
            }
            public override bool CreateNewSupported => ctor != null;

            public override object CreateNew()
            {
                return ctor != null ? ctor() : base.CreateNew();
            }
            public override object this[object target, string name]
            {
                get => getter(target, name);
                set => setter(target, name, value);
            }
        }

        private static bool IsFullyPublic(Type type)
        {
            while (type.IsNestedPublic) type = type.DeclaringType;
            return type.IsPublic;
        }

        static TypeAccessor CreateNew(Type type)
        {
            //if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type))
            //{
            //    return DynamicAccessor.Singleton;
            //}

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            ConstructorInfo ctor = null;
            if (type.IsClass && !type.IsAbstract)
            {
                ctor = type.GetConstructor(Type.EmptyTypes);
            }
            ILGenerator il;
            if (!IsFullyPublic(type))
            {
                DynamicMethod dynGetter = new DynamicMethod(type.FullName + "_get", typeof(object), new Type[] { typeof(object), typeof(string) }, type, true),
                              dynSetter = new DynamicMethod(type.FullName + "_set", null, new Type[] { typeof(object), typeof(string), typeof(object) }, type, true);
                WriteGetter(dynGetter.GetILGenerator(), type, props, fields, true);
                WriteSetter(dynSetter.GetILGenerator(), type, props, fields, true);
                DynamicMethod dynCtor = null;
                if (ctor != null)
                {
                    dynCtor = new DynamicMethod(type.FullName + "_ctor", typeof(object), Type.EmptyTypes, type, true);
                    il = dynCtor.GetILGenerator();
                    il.Emit(OpCodes.Newobj, ctor);
                    il.Emit(OpCodes.Ret);
                }
                return new DelegateAccessor(
                    (Func<object, string, object>)dynGetter.CreateDelegate(typeof(Func<object, string, object>)),
                    (Action<object, string, object>)dynSetter.CreateDelegate(typeof(Action<object, string, object>)),
                    dynCtor == null ? null : (Func<object>)dynCtor.CreateDelegate(typeof(Func<object>)));
            }

            // note this region is synchronized; only one is being created at a time so we don't need to stress about the builders
            if (assembly == null)
            {
                AssemblyName name = new AssemblyName("FastMember_dynamic");
                assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
                module = assembly.DefineDynamicModule(name.Name);
            }
            TypeBuilder tb = module.DefineType("FastMember_dynamic." + type.Name + "_" + Interlocked.Increment(ref counter),
                (typeof(TypeAccessor).Attributes | TypeAttributes.Sealed) & ~TypeAttributes.Abstract, typeof(TypeAccessor));

            tb.DefineDefaultConstructor(MethodAttributes.Public);
            PropertyInfo indexer = typeof(TypeAccessor).GetProperty("Item");
            MethodInfo baseGetter = indexer.GetGetMethod(), baseSetter = indexer.GetSetMethod();
            MethodBuilder body = tb.DefineMethod(baseGetter.Name, baseGetter.Attributes & ~MethodAttributes.Abstract, typeof(object), new Type[] { typeof(object), typeof(string) });
            il = body.GetILGenerator();
            WriteGetter(il, type, props, fields, false);
            tb.DefineMethodOverride(body, baseGetter);

            body = tb.DefineMethod(baseSetter.Name, baseSetter.Attributes & ~MethodAttributes.Abstract, null, new Type[] { typeof(object), typeof(string), typeof(object) });
            il = body.GetILGenerator();
            WriteSetter(il, type, props, fields, false);
            tb.DefineMethodOverride(body, baseSetter);

            if (ctor != null)
            {
                MethodInfo baseMethod = typeof(TypeAccessor).GetProperty("CreateNewSupported").GetGetMethod();
                body = tb.DefineMethod(baseMethod.Name, baseMethod.Attributes, typeof(bool), Type.EmptyTypes);
                il = body.GetILGenerator();
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
                tb.DefineMethodOverride(body, baseMethod);

                baseMethod = typeof(TypeAccessor).GetMethod("CreateNew");
                body = tb.DefineMethod(baseMethod.Name, baseMethod.Attributes, typeof(object), Type.EmptyTypes);
                il = body.GetILGenerator();
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Ret);
                tb.DefineMethodOverride(body, baseMethod);
            }

            return (TypeAccessor)Activator.CreateInstance(tb.CreateType());
        }

        private static void Cast(ILGenerator il, Type type, LocalBuilder addr)
        {
            if (type == typeof(object)) { }
            else if (type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
                if (addr != null)
                {
                    il.Emit(OpCodes.Stloc, addr);
                    il.Emit(OpCodes.Ldloca_S, addr);
                }
            }
            else
            {
                il.Emit(OpCodes.Castclass, type);
            }
        }

        /// <summary>
        /// Get or set the value of a named member on the target instance
        /// </summary>
        public abstract object this[object target, string name]
        {
            get;
            set;
        }
    }
}

#endif
