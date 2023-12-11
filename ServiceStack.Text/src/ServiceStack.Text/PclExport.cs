//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack;

public abstract class PclExport
{
    public static class Platforms
    {
        public const string NetStandard = "NETStd";
        public const string Net6 = "NET6";
        public const string NetFX = "NETFX";
    }

    public static PclExport Instance =
#if NETFX
      new NetFxPclExport()
#elif NETSTANDARD2_0
      new NetStandardPclExport()
#elif NETCORE || NET6_0_OR_GREATER
      new Net6PclExport()
#endif
    ;

    public static ReflectionOptimizer Reflection => ReflectionOptimizer.Instance;

    static PclExport() {}

    public static bool ConfigureProvider(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type == null)
            return false;

        var mi = type.GetMethod("Configure");
        if (mi != null)
        {
            mi.Invoke(null, Array.Empty<object>());
        }

        return true;
    }

    public static void Configure(PclExport instance)
    {
        Instance = instance ?? Instance;

        if (Instance is { EmptyTask: null })
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            Instance.EmptyTask = tcs.Task;
        }
    }

    public Task EmptyTask;

    public char DirSep = '\\';

    public char AltDirSep = '/';

    public static readonly char[] DirSeps = { '\\', '/' };

    public string PlatformName = "Unknown";

    public RegexOptions RegexOptions = RegexOptions.None;

    public StringComparison InvariantComparison = StringComparison.Ordinal;

    public StringComparison InvariantComparisonIgnoreCase = StringComparison.OrdinalIgnoreCase;

    public StringComparer InvariantComparer = StringComparer.Ordinal;

    public StringComparer InvariantComparerIgnoreCase = StringComparer.OrdinalIgnoreCase;

    // HACK: The only way to detect anonymous types right now.
    public virtual bool IsAnonymousType(Type type)
    {
        return type.IsGenericType && type.Name.Contains("AnonymousType")
            && (type.Name.StartsWith("<>", StringComparison.Ordinal) || type.Name.StartsWith("VB$", StringComparison.Ordinal));
    }

    public virtual string ToInvariantUpper(char value)
    {
        return value.ToString().ToUpperInvariant();
    }

    public virtual string ReadAllText(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    public virtual bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public virtual bool DirectoryExists(string dirPath)
    {
        return Directory.Exists(dirPath);
    }

    public virtual void CreateDirectory(string dirPath)
    {
        Directory.CreateDirectory(dirPath);
    }

    public virtual void RegisterLicenseFromConfig()
    {            
    }

    public virtual string GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

    public virtual void WriteLine(string line) => Console.WriteLine(line);

    public virtual void WriteLine(string format, params object[] args) => Console.WriteLine(format, args);

    public virtual string[] GetFileNames(string dirPath, string searchPattern = null)
    {
        if (!Directory.Exists(dirPath))
            return TypeConstants.EmptyStringArray;

        return searchPattern != null
            ? Directory.GetFiles(dirPath, searchPattern)
            : Directory.GetFiles(dirPath);
    }

    public virtual string[] GetDirectoryNames(string dirPath, string searchPattern = null)
    {
        if (!Directory.Exists(dirPath))
            return TypeConstants.EmptyStringArray;

        return searchPattern != null
            ? Directory.GetDirectories(dirPath, searchPattern)
            : Directory.GetDirectories(dirPath);
    }
    
    public virtual void Config(HttpWebRequest req,
        bool? allowAutoRedirect = null,
        TimeSpan? timeout = null,
        TimeSpan? readWriteTimeout = null,
        string userAgent = null,
        bool? preAuthenticate = null)
    {
        try
        {
            if (allowAutoRedirect.HasValue) 
                req.AllowAutoRedirect = allowAutoRedirect.Value;

            if (userAgent != null)
                req.UserAgent = userAgent;

            if (readWriteTimeout.HasValue) 
                req.ReadWriteTimeout = (int) readWriteTimeout.Value.TotalMilliseconds;
            if (timeout.HasValue) 
                req.Timeout = (int) timeout.Value.TotalMilliseconds;

            if (preAuthenticate.HasValue)
                req.PreAuthenticate = preAuthenticate.Value;
        }
        catch (Exception ex)
        {
            Tracer.Instance.WriteError(ex);
        }
    }

    public virtual void AddCompression(WebRequest webRequest)
    {
        try
        {
            var httpReq = (HttpWebRequest)webRequest;
            httpReq.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate";
            httpReq.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }
        catch (Exception ex)
        {
            Tracer.Instance.WriteError(ex);
        }
    }

    public virtual Stream GetRequestStream(WebRequest webRequest)
    {
        var async = webRequest.GetRequestStreamAsync();
        async.Wait();
        return async.Result;
    }

    public virtual WebResponse GetResponse(WebRequest webRequest)
    {
        try
        {
            var async = webRequest.GetResponseAsync();
            async.Wait();
            return async.Result;
        }
        catch (Exception ex)
        {
            throw ex.UnwrapIfSingleException();
        }
    }

    public virtual Task<WebResponse> GetResponseAsync(WebRequest webRequest)
    {
        return webRequest.GetResponseAsync();
    }

    public virtual bool IsDebugBuild(Assembly assembly)
    {
        return assembly.AllAttributes()
            .Any(x => x.GetType().Name == "DebuggableAttribute");
    }

    public virtual string MapAbsolutePath(string relativePath, string appendPartialPathModifier)
    {
        return relativePath;
    }

    public virtual Assembly LoadAssembly(string assemblyPath)
    {
        return null;
    }

    public virtual void AddHeader(WebRequest webReq, string name, string value)
    {
        webReq.Headers[name] = value;
    }

    public virtual void SetUserAgent(HttpWebRequest httpReq, string value)
    {
        try
        {
            httpReq.UserAgent = value;
        }
        catch (Exception e) // API may have been removed by Xamarin's Linker
        {
            Tracer.Instance.WriteError(e);
        }
    }

    public virtual void SetContentLength(HttpWebRequest httpReq, long value)
    {
        try
        {
            httpReq.ContentLength = value;
        }
        catch (Exception e) // API may have been removed by Xamarin's Linker
        {
            Tracer.Instance.WriteError(e);
        }
    }

    public virtual void SetAllowAutoRedirect(HttpWebRequest httpReq, bool value)
    {
        try
        {
            httpReq.AllowAutoRedirect = value;
        }
        catch (Exception e) // API may have been removed by Xamarin's Linker
        {
            Tracer.Instance.WriteError(e);
        }
    }

    public virtual void SetKeepAlive(HttpWebRequest httpReq, bool value)
    {
        try
        {
            httpReq.KeepAlive = value;
        }
        catch (Exception e) // API may have been removed by Xamarin's Linker
        {
            Tracer.Instance.WriteError(e);
        }
    }

    public virtual Assembly[] GetAllAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies();
    }

    public virtual Type FindType(string typeName, string assemblyName)
    {
        return null;
    }

    public virtual string GetAssemblyCodeBase(Assembly assembly)
    {
        return assembly.FullName;
    }

    public virtual string GetAssemblyPath(Type source)
    {
        return null;
    }

    public virtual string GetAsciiString(byte[] bytes)
    {
        return GetAsciiString(bytes, 0, bytes.Length);
    }

    public virtual string GetAsciiString(byte[] bytes, int index, int count)
    {
        return Encoding.ASCII.GetString(bytes, index, count);
    }

    public virtual byte[] GetAsciiBytes(string str)
    {
        return Encoding.ASCII.GetBytes(str);
    }

    public virtual Encoding GetUTF8Encoding(bool emitBom=false)
    {
        return new UTF8Encoding(emitBom);
    }
    
    public virtual bool InSameAssembly(Type t1, Type t2)
    {
        return t1.AssemblyQualifiedName != null && t1.AssemblyQualifiedName.Equals(t2.AssemblyQualifiedName);
    }

    public virtual Type GetGenericCollectionType(Type type)
    {
        return type.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType
            && t.GetGenericTypeDefinition() == typeof(ICollection<>));
    }

    public virtual string ToXsdDateTimeString(DateTime dateTime)
    {
        return System.Xml.XmlConvert.ToString(dateTime.ToStableUniversalTime(), DateTimeSerializer.XsdDateTimeFormat);
    }

    public virtual string ToLocalXsdDateTimeString(DateTime dateTime)
    {
        return System.Xml.XmlConvert.ToString(dateTime, DateTimeSerializer.XsdDateTimeFormat);
    }

    public virtual DateTime ParseXsdDateTime(string dateTimeStr)
    {
        return System.Xml.XmlConvert.ToDateTimeOffset(dateTimeStr).DateTime;
    }

    public virtual DateTime ParseXsdDateTimeAsUtc(string dateTimeStr)
    {
        return DateTimeSerializer.ParseManual(dateTimeStr, DateTimeKind.Utc)
            ?? DateTime.ParseExact(dateTimeStr, DateTimeSerializer.XsdDateTimeFormat, CultureInfo.InvariantCulture);
    }

    public virtual DateTime ToStableUniversalTime(DateTime dateTime)
    {
        // Silverlight 3, 4 and 5 all work ok with DateTime.ToUniversalTime, but have no TimeZoneInfo.ConverTimeToUtc implementation.
        return dateTime.ToUniversalTime();
    }

    public virtual ParseStringDelegate GetDictionaryParseMethod<TSerializer>(Type type)
        where TSerializer : ITypeSerializer => null;

    public virtual ParseStringSpanDelegate GetDictionaryParseStringSpanMethod<TSerializer>(Type type)
        where TSerializer : ITypeSerializer => null;

    public virtual ParseStringDelegate GetSpecializedCollectionParseMethod<TSerializer>(Type type)
        where TSerializer : ITypeSerializer => null;

    public virtual ParseStringSpanDelegate GetSpecializedCollectionParseStringSpanMethod<TSerializer>(Type type)
        where TSerializer : ITypeSerializer => null;

    public virtual ParseStringDelegate GetJsReaderParseMethod<TSerializer>(Type type)
        where TSerializer : ITypeSerializer => null;

    public virtual ParseStringSpanDelegate GetJsReaderParseStringSpanMethod<TSerializer>(Type type)
        where TSerializer : ITypeSerializer => null;


    public virtual void InitHttpWebRequest(HttpWebRequest httpReq,
        long? contentLength = null, bool allowAutoRedirect = true, bool keepAlive = true)
    {
        httpReq.UserAgent = Env.ServerUserAgent;
        httpReq.AllowAutoRedirect = allowAutoRedirect;
        httpReq.KeepAlive = keepAlive;

        if (contentLength != null)
        {
            SetContentLength(httpReq, contentLength.Value);
        }
    }

    public virtual void CloseStream(Stream stream)
    {
        stream.Flush();
    }

    public virtual void ResetStream(Stream stream)
    {
        stream.Position = 0;
    }

    public virtual LicenseKey VerifyLicenseKeyText(string licenseKeyText)
    {
        return licenseKeyText.ToLicenseKey();
    }

    public virtual LicenseKey VerifyLicenseKeyTextFallback(string licenseKeyText)
    {
        return licenseKeyText.ToLicenseKeyFallback();
    }

    public virtual void BeginThreadAffinity() {}
    public virtual void EndThreadAffinity() {}

    public virtual DataContractAttribute GetWeakDataContract(Type type) => null;
    public virtual DataMemberAttribute GetWeakDataMember(PropertyInfo pi) => null;
    public virtual DataMemberAttribute GetWeakDataMember(FieldInfo pi) => null;

    public virtual void RegisterForAot() {}
    public virtual string GetStackTrace() => Environment.StackTrace;

    public virtual Task WriteAndFlushAsync(Stream stream, byte[] bytes)
    {
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
        return EmptyTask;
    }
}
