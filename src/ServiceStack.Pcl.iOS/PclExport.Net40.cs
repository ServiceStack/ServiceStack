#if !(XBOX || SL5 || NETFX_CORE || WP || PCL)
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;

#if !__IOS__
using System.Reflection.Emit;
using FastMember = ServiceStack.Text.FastMember;
#endif

namespace ServiceStack
{
    public class Net40PclExport : PclExport
    {
        public static Net40PclExport Provider = new Net40PclExport();

        public Net40PclExport()
        {
            this.SupportsEmit = SupportsExpression = true;
            this.DirSep = Path.DirectorySeparatorChar;
            this.AltDirSep = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
            this.RegexOptions = RegexOptions.Compiled;
            this.InvariantComparison = StringComparison.InvariantCulture;            
            this.InvariantComparisonIgnoreCase = StringComparison.InvariantCultureIgnoreCase;
            this.InvariantComparer = StringComparer.InvariantCulture;
            this.InvariantComparerIgnoreCase = StringComparer.InvariantCultureIgnoreCase;

            this.PlatformName = Environment.OSVersion.Platform.ToString();
        }

        public static PclExport Configure()
        {
            Configure(Provider);
            return Provider;
        }

        public override string ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public override string ToTitleCase(string value)
        {
            return TextInfo.ToTitleCase(value).Replace("_", String.Empty);
        }

        public override string ToInvariantUpper(char value)
        {
            return value.ToString(CultureInfo.InvariantCulture).ToUpper();
        }

        public override bool IsAnonymousType(Type type)
        {
            return type.HasAttribute<CompilerGeneratedAttribute>()
                   && type.IsGeneric() && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>", StringComparison.Ordinal) || type.Name.StartsWith("VB$", StringComparison.Ordinal))
                   && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public override bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public override bool DirectoryExists(string dirPath)
        {
            return Directory.Exists(dirPath);
        }

        public override void CreateDirectory(string dirPath)
        {
            Directory.CreateDirectory(dirPath);
        }

        public const string AppSettingsKey = "servicestack:license";
        public override void RegisterLicenseFromConfig()
        {
#if ANDROID
#elif __IOS__
#else
            //Automatically register license key stored in <appSettings/>
            var licenceKeyText = System.Configuration.ConfigurationManager.AppSettings[AppSettingsKey];
            if (!string.IsNullOrEmpty(licenceKeyText))
            {
                LicenseUtils.RegisterLicense(licenceKeyText);
            }
#endif
        }

        public override string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        public override void WriteLine(string line)
        {
            Console.WriteLine(line);
        }

        public override void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
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

        public override bool IsDebugBuild(Assembly assembly)
        {
            return assembly.AllAttributes()
                           .OfType<DebuggableAttribute>()
                           .Select(attr => attr.IsJITTrackingEnabled)
                           .FirstOrDefault();
        }

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

        public virtual void AddHeader(WebRequest webReq, string name, string value)
        {
            webReq.Headers.Add(name, value);
        }

        public override Assembly[] GetAllAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        public override Type FindType(string typeName, string assemblyName)
        {
            var binPath = AssemblyUtils.GetAssemblyBinPath(Assembly.GetExecutingAssembly());
            Assembly assembly = null;
            var assemblyDllPath = binPath + String.Format("{0}.{1}", assemblyName, "dll");
            if (File.Exists(assemblyDllPath))
            {
                assembly = AssemblyUtils.LoadAssembly(assemblyDllPath);
            }
            var assemblyExePath = binPath + String.Format("{0}.{1}", assemblyName, "exe");
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

        public virtual string GetAssemblyPath(Type source)
        {
            var assemblyUri = new Uri(source.Assembly.EscapedCodeBase);
            return assemblyUri.LocalPath;
        }

        public virtual string GetAsciiString(byte[] bytes, int index, int count)
        {
            return Encoding.ASCII.GetString(bytes, index, count);
        }

        public override byte[] GetAsciiBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public override bool InSameAssembly(Type t1, Type t2)
        {
            return t1.GetAssembly() == t2.GetAssembly();
        }

        public override Type GetGenericCollectionType(Type type)
        {
            return type.FindInterfaces((t, critera) =>
                t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(ICollection<>), null).FirstOrDefault();
        }

        public override PropertySetterDelegate GetPropertySetterFn(PropertyInfo propertyInfo)
        {
            var propertySetMethod = propertyInfo.SetMethod();
            if (propertySetMethod == null) return null;

            if (!SupportsExpression)
            {
                return (o, convertedValue) =>
                    propertySetMethod.Invoke(o, new[] { convertedValue });
            }

            var instance = Expression.Parameter(typeof(object), "i");
            var argument = Expression.Parameter(typeof(object), "a");

            var instanceParam = Expression.Convert(instance, propertyInfo.ReflectedType());
            var valueParam = Expression.Convert(argument, propertyInfo.PropertyType);

            var setterCall = Expression.Call(instanceParam, propertyInfo.SetMethod(), valueParam);

            return Expression.Lambda<PropertySetterDelegate>(setterCall, instance, argument).Compile();
        }

        public override PropertyGetterDelegate GetPropertyGetterFn(PropertyInfo propertyInfo)
        {
            if (!SupportsExpression)
                return base.GetPropertyGetterFn(propertyInfo);

            var getMethodInfo = propertyInfo.GetMethodInfo();
            if (getMethodInfo == null) return null;
            try
            {
                var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
                var instanceParam = Expression.Convert(oInstanceParam, propertyInfo.ReflectedType); //propertyInfo.DeclaringType doesn't work on Proxy types

                var exprCallPropertyGetFn = Expression.Call(instanceParam, getMethodInfo);
                var oExprCallPropertyGetFn = Expression.Convert(exprCallPropertyGetFn, typeof(object));

                var propertyGetFn = Expression.Lambda<PropertyGetterDelegate>
                    (
                        oExprCallPropertyGetFn,
                        oInstanceParam
                    ).Compile();

                return propertyGetFn;

            }
            catch (Exception ex)
            {
                Tracer.Instance.WriteError(ex);
                throw;
            }
        }

        public override string ToXsdDateTimeString(DateTime dateTime)
        {
            return XmlConvert.ToString(dateTime.ToStableUniversalTime(), XmlDateTimeSerializationMode.Utc);
        }

        public override string ToLocalXsdDateTimeString(DateTime dateTime)
        {
            return XmlConvert.ToString(dateTime, XmlDateTimeSerializationMode.Local);
        }

        public override DateTime ParseXsdDateTime(string dateTimeStr)
        {
            return XmlConvert.ToDateTime(dateTimeStr, XmlDateTimeSerializationMode.Utc);
        }

        public override DateTime ParseXsdDateTimeAsUtc(string dateTimeStr)
        {
            return XmlConvert.ToDateTime(dateTimeStr, XmlDateTimeSerializationMode.Utc).Prepare(parsedAsUtc: true);
        }

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

        public override ParseStringDelegate GetSpecializedCollectionParseMethod<TSerializer>(Type type)
        {
            if (type == typeof(StringCollection))
            {
                return SerializerUtils<TSerializer>.ParseStringCollection<TSerializer>;
            }
            return null;
        }

        public override ParseStringDelegate GetJsReaderParseMethod<TSerializer>(Type type)
        {
#if !__IOS__
            if (type.AssignableFrom(typeof(System.Dynamic.IDynamicMetaObjectProvider)) ||
                type.HasInterface(typeof(System.Dynamic.IDynamicMetaObjectProvider)))
            {
                return DeserializeDynamic<TSerializer>.Parse;
            }
#endif
			return null;
        }

        public override XmlSerializer NewXmlSerializer()
        {
            return new XmlSerializer();
        }

        public virtual void InitHttpWebRequest(HttpWebRequest httpReq,
            long? contentLength = null, bool allowAutoRedirect = true, bool keepAlive = true)
        {
            httpReq.UserAgent = Env.ServerUserAgent;
            httpReq.AllowAutoRedirect = allowAutoRedirect;
            httpReq.KeepAlive = keepAlive;

            if (contentLength != null)
            {
                httpReq.ContentLength = contentLength.Value;
            }
        }

        public override void CloseStream(Stream stream)
        {
            stream.Close();
        }

        public override LicenseKey VerifyLicenseKeyText(string licenseKeyText)
        {
            LicenseKey key;
            if (!licenseKeyText.VerifyLicenseKeyText(out key))
                throw new ArgumentException("licenseKeyText");

            return key;
        }

        public override void VerifyInAssembly(Type accessType, string assemblyName)
        {
            if (accessType.Assembly.ManifestModule.Name != assemblyName) //might get merged/mangled on alt platforms
                throw new LicenseException(LicenseUtils.ErrorMessages.UnauthorizedAccessRequest);
        }

        public override void BeginThreadAffinity()
        {
            Thread.BeginThreadAffinity();
        }

        public virtual void EndThreadAffinity()
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

#if !__IOS__
        public virtual SetPropertyDelegate GetSetPropertyMethod(PropertyInfo propertyInfo)
        {
            return CreateIlPropertySetter(propertyInfo);
        }

        public virtual SetPropertyDelegate GetSetFieldMethod(FieldInfo fieldInfo)
        {
            return CreateIlFieldSetter(fieldInfo);
        }

        public override SetPropertyDelegate GetSetMethod(PropertyInfo propertyInfo, FieldInfo fieldInfo)
        {
            return propertyInfo.CanWrite
                ? CreateIlPropertySetter(propertyInfo)
                : CreateIlFieldSetter(fieldInfo);
        }

        public override Type UseType(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                return DynamicProxy.GetInstanceFor(type).GetType();
            }
            return type;
        }

        public DataContractAttribute GetWeakDataContract(Type type)
        {
            return type.GetWeakDataContract();
        }

        public DataMemberAttribute GetWeakDataMember(PropertyInfo pi)
        {
            return pi.GetWeakDataMember();
        }

        public DataMemberAttribute GetWeakDataMember(FieldInfo pi)
        {
            return pi.GetWeakDataMember();
        }

        public static SetPropertyDelegate CreateIlPropertySetter(PropertyInfo propertyInfo)
        {
            var propSetMethod = propertyInfo.GetSetMethod(true);
            if (propSetMethod == null)
                return null;

            var setter = CreateDynamicSetMethod(propertyInfo);

            var generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            generator.Emit(OpCodes.Ldarg_1);

            generator.Emit(propertyInfo.PropertyType.IsClass
                               ? OpCodes.Castclass
                               : OpCodes.Unbox_Any,
                           propertyInfo.PropertyType);

            generator.EmitCall(OpCodes.Callvirt, propSetMethod, (Type[])null);
            generator.Emit(OpCodes.Ret);

            return (SetPropertyDelegate)setter.CreateDelegate(typeof(SetPropertyDelegate));
        }

        public static SetPropertyDelegate CreateIlFieldSetter(FieldInfo fieldInfo)
        {
            var setter = CreateDynamicSetMethod(fieldInfo);

            var generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, fieldInfo.DeclaringType);
            generator.Emit(OpCodes.Ldarg_1);

            generator.Emit(fieldInfo.FieldType.IsClass
                               ? OpCodes.Castclass
                               : OpCodes.Unbox_Any,
                           fieldInfo.FieldType);

            generator.Emit(OpCodes.Stfld, fieldInfo);
            generator.Emit(OpCodes.Ret);

            return (SetPropertyDelegate)setter.CreateDelegate(typeof(SetPropertyDelegate));
        }

        private static DynamicMethod CreateDynamicSetMethod(MemberInfo memberInfo)
        {
            var args = new[] { typeof(object), typeof(object) };
            var name = string.Format("_{0}{1}_", "Set", memberInfo.Name);
            var returnType = typeof(void);

            return !memberInfo.DeclaringType.IsInterface
                       ? new DynamicMethod(name, returnType, args, memberInfo.DeclaringType, true)
                       : new DynamicMethod(name, returnType, args, memberInfo.Module, true);
        }
#endif
    }

#if __IOS__
    public class IosPclExport : Net40PclExport
    {
        public static new IosPclExport Provider = new IosPclExport();

        public IosPclExport()
        {
            PlatformName = "IOS";
            SupportsEmit = SupportsExpression = false;
        }

        public override void VerifyInAssembly(Type accessType, string assemblyName)
        {
        }

        public new static void Configure()
        {
            Configure(Provider);
        }

        public override void ResetStream(Stream stream)
        {
            // MonoTouch throws NotSupportedException when setting System.Net.WebConnectionStream.Position
            // Not sure if the stream is used later though, so may have to copy to MemoryStream and
            // pass that around instead after this point?
        }
    }
#endif

#if ANDROID
    public class AndroidPclExport : Net40PclExport
    {
        public static new AndroidPclExport Provider = new AndroidPclExport();

        public AndroidPclExport()
        {
            PlatformName = "Android";
        }

        public override void VerifyInAssembly(Type accessType, string assemblyName)
        {
        }

        public new static void Configure()
        {
            Configure(Provider);
        }
    }
#endif

#if !__IOS__
    public static class DynamicProxy
    {
        public static T GetInstanceFor<T>()
        {
            return (T)GetInstanceFor(typeof(T));
        }

        static readonly ModuleBuilder ModuleBuilder;
        static readonly AssemblyBuilder DynamicAssembly;

        public static object GetInstanceFor(Type targetType)
        {
            lock (DynamicAssembly)
            {
                var constructedType = DynamicAssembly.GetType(ProxyName(targetType)) ?? GetConstructedType(targetType);
                var instance = Activator.CreateInstance(constructedType);
                return instance;
            }
        }

        static string ProxyName(Type targetType)
        {
            return targetType.Name + "Proxy";
        }

        static DynamicProxy()
        {
            var assemblyName = new AssemblyName("DynImpl");
            DynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder = DynamicAssembly.DefineDynamicModule("DynImplModule");
        }

        static Type GetConstructedType(Type targetType)
        {
            var typeBuilder = ModuleBuilder.DefineType(targetType.Name + "Proxy", TypeAttributes.Public);

            var ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new Type[] { });
            var ilGenerator = ctorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            IncludeType(targetType, typeBuilder);

            foreach (var face in targetType.GetInterfaces())
                IncludeType(face, typeBuilder);

            return typeBuilder.CreateType();
        }

        static void IncludeType(Type typeOfT, TypeBuilder typeBuilder)
        {
            var methodInfos = typeOfT.GetMethods();
            foreach (var methodInfo in methodInfos)
            {
                if (methodInfo.Name.StartsWith("set_", StringComparison.Ordinal)) continue; // we always add a set for a get.

                if (methodInfo.Name.StartsWith("get_", StringComparison.Ordinal))
                {
                    BindProperty(typeBuilder, methodInfo);
                }
                else
                {
                    BindMethod(typeBuilder, methodInfo);
                }
            }

            typeBuilder.AddInterfaceImplementation(typeOfT);
        }

        static void BindMethod(TypeBuilder typeBuilder, MethodInfo methodInfo)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                methodInfo.ReturnType,
                methodInfo.GetParameters().Select(p => p.GetType()).ToArray()
                );
            var methodILGen = methodBuilder.GetILGenerator();
            if (methodInfo.ReturnType == typeof(void))
            {
                methodILGen.Emit(OpCodes.Ret);
            }
            else
            {
                if (methodInfo.ReturnType.IsValueType || methodInfo.ReturnType.IsEnum)
                {
                    MethodInfo getMethod = typeof(Activator).GetMethod("CreateInstance",
                                                                       new[] { typeof(Type) });
                    LocalBuilder lb = methodILGen.DeclareLocal(methodInfo.ReturnType);
                    methodILGen.Emit(OpCodes.Ldtoken, lb.LocalType);
                    methodILGen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                    methodILGen.Emit(OpCodes.Callvirt, getMethod);
                    methodILGen.Emit(OpCodes.Unbox_Any, lb.LocalType);
                }
                else
                {
                    methodILGen.Emit(OpCodes.Ldnull);
                }
                methodILGen.Emit(OpCodes.Ret);
            }
            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        public static void BindProperty(TypeBuilder typeBuilder, MethodInfo methodInfo)
        {
            // Backing Field
            string propertyName = methodInfo.Name.Replace("get_", "");
            Type propertyType = methodInfo.ReturnType;
            FieldBuilder backingField = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            //Getter
            MethodBuilder backingGet = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public |
                MethodAttributes.SpecialName | MethodAttributes.Virtual |
                MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = backingGet.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, backingField);
            getIl.Emit(OpCodes.Ret);


            //Setter
            MethodBuilder backingSet = typeBuilder.DefineMethod("set_" + propertyName, MethodAttributes.Public |
                MethodAttributes.SpecialName | MethodAttributes.Virtual |
                MethodAttributes.HideBySig, null, new[] { propertyType });

            ILGenerator setIl = backingSet.GetILGenerator();

            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, backingField);
            setIl.Emit(OpCodes.Ret);

            // Property
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);
            propertyBuilder.SetGetMethod(backingGet);
            propertyBuilder.SetSetMethod(backingSet);
        }
    }
#endif

    internal class SerializerUtils<TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        private static int VerifyAndGetStartIndex(string value, Type createMapType)
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

        public static Hashtable ParseHashtable(string value)
        {
            var index = VerifyAndGetStartIndex(value, typeof(Hashtable));

            var result = new Hashtable();

            if (JsonTypeSerializer.IsEmptyMap(value, index)) return result;

            var valueLength = value.Length;
            while (index < valueLength)
            {
                var keyValue = Serializer.EatMapKey(value, ref index);
                Serializer.EatMapKeySeperator(value, ref index);
                var elementValue = Serializer.EatValue(value, ref index);
                if (keyValue == null) continue;

                var mapKey = keyValue;
                var mapValue = elementValue;

                result[mapKey] = mapValue;

                Serializer.EatItemSeperatorOrMapEndChar(value, ref index);
            }

            return result;
        }

        public static StringCollection ParseStringCollection<TS>(string value) where TS : ITypeSerializer
        {
            if ((value = DeserializeListWithElements<TS>.StripList(value)) == null) return null;
            return value == String.Empty
                   ? new StringCollection()
                   : ToStringCollection(DeserializeListWithElements<TSerializer>.ParseStringList(value));
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
        public static string ToFormUrlEncoded(this NameValueCollection queryParams)
        {
            var sb = new System.Text.StringBuilder();
            foreach (string key in queryParams)
            {
                var values = queryParams.GetValues(key);
                if (values == null) continue;

                foreach (var value in values)
                {
                    if (sb.Length > 0)
                        sb.Append('&');

                    sb.AppendFormat("{0}={1}", key.UrlEncode(), value.UrlEncode());
                }
            }

            return sb.ToString();
        }

        //HttpUtils
        public static WebResponse PostFileToUrl(this string url,
            FileInfo uploadFileInfo, string uploadFileMimeType,
            string accept = null,
            Action<HttpWebRequest> requestFilter = null)
        {
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            using (var fileStream = uploadFileInfo.OpenRead())
            {
                var fileName = uploadFileInfo.Name;

                webReq.UploadFile(fileStream, fileName, uploadFileMimeType, accept: accept, requestFilter: requestFilter, method: "POST");
            }

            if (HttpUtils.ResultsFilter != null)
                return null;

            return webReq.GetResponse();
        }

        public static WebResponse PutFileToUrl(this string url,
            FileInfo uploadFileInfo, string uploadFileMimeType,
            string accept = null,
            Action<HttpWebRequest> requestFilter = null)
        {
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            using (var fileStream = uploadFileInfo.OpenRead())
            {
                var fileName = uploadFileInfo.Name;

                webReq.UploadFile(fileStream, fileName, uploadFileMimeType, accept: accept, requestFilter: requestFilter, method: "PUT");
            }

            if (HttpUtils.ResultsFilter != null)
                return null;

            return webReq.GetResponse();
        }

        public static WebResponse UploadFile(this WebRequest webRequest,
            FileInfo uploadFileInfo, string uploadFileMimeType)
        {
            using (var fileStream = uploadFileInfo.OpenRead())
            {
                var fileName = uploadFileInfo.Name;

                webRequest.UploadFile(fileStream, fileName, uploadFileMimeType);
            }

            if (HttpUtils.ResultsFilter != null)
                return null;

            return webRequest.GetResponse();
        }

        //XmlSerializer
        public static void CompressToStream<TXmlDto>(TXmlDto from, Stream stream)
        {
#if __IOS__ || ANDROID
            throw new NotImplementedException("Compression is not supported on this platform");
#else
            using (var deflateStream = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Compress))
            using (var xw = new XmlTextWriter(deflateStream, Encoding.UTF8))
            {
                var serializer = new DataContractSerializer(from.GetType());
                serializer.WriteObject(xw, from);
                xw.Flush();
            }
#endif
        }

        public static byte[] Compress<TXmlDto>(TXmlDto from)
        {
            using (var ms = new MemoryStream())
            {
                CompressToStream(from, ms);

                return ms.ToArray();
            }
        }

        //License Utils
        public static bool VerifySignedHash(byte[] DataToVerify, byte[] SignedData, RSAParameters Key)
        {
            try
            {
                var RSAalg = new RSACryptoServiceProvider();
                RSAalg.ImportParameters(Key);
                return RSAalg.VerifySha1Data(DataToVerify, SignedData);

            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);

                return false;
            }
        }

        public static bool VerifyLicenseKeyText(this string licenseKeyText, out LicenseKey key)
        {
            var publicRsaProvider = new RSACryptoServiceProvider();
            publicRsaProvider.FromXmlString(LicenseUtils.LicensePublicKey);
            var publicKeyParams = publicRsaProvider.ExportParameters(false);

            key = licenseKeyText.ToLicenseKey();
            var originalData = key.GetHashKeyToSign().ToUtf8Bytes();
            var signedData = Convert.FromBase64String(key.Hash);

            return VerifySignedHash(originalData, signedData, publicKeyParams);
        }

        public static bool VerifySha1Data(this RSACryptoServiceProvider RSAalg, byte[] unsignedData, byte[] encryptedData)
        {
            return RSAalg.VerifyData(unsignedData, new SHA1CryptoServiceProvider(), encryptedData);
            //SL5 || WP
            //return RSAalg.VerifyData(unsignedData, encryptedData, new EMSAPKCS1v1_5_SHA1()); 
        }

#if !__IOS__
        //ReflectionExtensions
        const string DataContract = "DataContractAttribute";

        static readonly ConcurrentDictionary<Type, FastMember.TypeAccessor> typeAccessorMap
            = new ConcurrentDictionary<Type, FastMember.TypeAccessor>();

        public static DataContractAttribute GetWeakDataContract(this Type type)
        {
            var attr = type.AllAttributes().FirstOrDefault(x => x.GetType().Name == DataContract);
            if (attr != null)
            {
                var attrType = attr.GetType();

                FastMember.TypeAccessor accessor;
                if (!typeAccessorMap.TryGetValue(attrType, out accessor))
                    typeAccessorMap[attrType] = accessor = FastMember.TypeAccessor.Create(attr.GetType());

                return new DataContractAttribute
                {
                    Name = (string)accessor[attr, "Name"],
                    Namespace = (string)accessor[attr, "Namespace"],
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

                FastMember.TypeAccessor accessor;
                if (!typeAccessorMap.TryGetValue(attrType, out accessor))
                    typeAccessorMap[attrType] = accessor = FastMember.TypeAccessor.Create(attr.GetType());

                var newAttr = new DataMemberAttribute
                {
                    Name = (string)accessor[attr, "Name"],
                    EmitDefaultValue = (bool)accessor[attr, "EmitDefaultValue"],
                    IsRequired = (bool)accessor[attr, "IsRequired"],
                };

                var order = (int)accessor[attr, "Order"];
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

                FastMember.TypeAccessor accessor;
                if (!typeAccessorMap.TryGetValue(attrType, out accessor))
                    typeAccessorMap[attrType] = accessor = FastMember.TypeAccessor.Create(attr.GetType());

                var newAttr = new DataMemberAttribute
                {
                    Name = (string)accessor[attr, "Name"],
                    EmitDefaultValue = (bool)accessor[attr, "EmitDefaultValue"],
                    IsRequired = (bool)accessor[attr, "IsRequired"],
                };

                var order = (int)accessor[attr, "Order"];
                if (order >= 0)
                    newAttr.Order = order; //Throws Exception if set to -1

                return newAttr;
            }
            return null;
        }
#endif
    }
}

#if !__IOS__

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
        public virtual bool CreateNewSupported { get { return false; } }
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
            if (type == null) throw new ArgumentNullException("type");
            TypeAccessor obj = (TypeAccessor)typeLookyp[type];
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

                Label next = il.DefineLabel();
                il.Emit(propName);
                il.Emit(OpCodes.Ldstr, prop.Name);
                il.EmitCall(OpCodes.Call, strinqEquals, null);
                il.Emit(OpCodes.Brfalse_S, next);
                // match:
                il.Emit(target);
                Cast(il, type, loc);
                il.EmitCall(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, prop.GetGetMethod(), null);
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
                    il.EmitCall(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, prop.GetSetMethod(), null);
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
            public override bool CreateNewSupported { get { return ctor != null; } }
            public override object CreateNew()
            {
                return ctor != null ? ctor() : base.CreateNew();
            }
            public override object this[object target, string name]
            {
                get { return getter(target, name); }
                set { setter(target, name, value); }
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

            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
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

#endif
