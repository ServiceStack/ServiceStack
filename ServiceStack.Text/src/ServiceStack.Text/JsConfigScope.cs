using System;
using System.Threading;
using System.Collections.Generic;
using ServiceStack.Text.Json;
using ServiceStack.Text.Jsv;
using ServiceStack.Text.Common;

namespace ServiceStack.Text;

public sealed class JsConfigScope : Config, IDisposable
{
    bool disposed;
    readonly JsConfigScope parent;

#if !NETFRAMEWORK
    private static AsyncLocal<JsConfigScope> head = new();
#else
        [ThreadStatic] private static JsConfigScope head;
#endif

    internal JsConfigScope()
    {
        PclExport.Instance.BeginThreadAffinity();

#if !NETFRAMEWORK
        parent = head.Value;
        head.Value = this;
#else
            parent = head;
            head = this;
#endif
    }

    internal static JsConfigScope Current => 
#if !NETFRAMEWORK
        head.Value;
#else
            head;
#endif

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
#if !NETFRAMEWORK
            head.Value = parent;
#else
                head = parent;
#endif

            PclExport.Instance.EndThreadAffinity();
        }
    }
}
    
public class Config
{
    private static Config instance;
    internal static Config Instance => instance ??= new Config(Defaults);
    internal static bool HasInit = false;

    public static Config AssertNotInit() => HasInit
        ? throw new NotSupportedException("JsConfig can't be mutated after JsConfig.Init(). Use BeginScope() or CreateScope() to use custom config after Init().")
        : Instance;

    private static string InitStackTrace = null;

    public static void Init() => Init(null);
    public static void Init(Config config)
    {
        if (HasInit && Env.StrictMode)
            throw new NotSupportedException($"JsConfig has already been initialized at: {InitStackTrace}");
            
        if (config != null)
            instance = config;

        HasInit = true;
        InitStackTrace = Environment.StackTrace;
    }

    /// <summary>
    /// Bypass Init checks. Only call on Startup.
    /// </summary>
    /// <param name="config"></param>
    public static void UnsafeInit(Config config)
    {
        if (config != null)
            instance = config;
    }

    public static void UnsafeInit(Action<Config> configure) => configure(instance);

    internal static void Reset()
    {
        HasInit = false;
        Instance.Populate(Defaults);
    }

    public Config()
    {
        Populate(Instance);
    }

    private Config(Config config)
    {
        if (config != null) //Defaults=null, instance=Defaults
            Populate(config);
    }

    public bool ConvertObjectTypesIntoStringDictionary { get; set; }
    public bool TryToParsePrimitiveTypeValues { get; set; }
    public bool TryToParseNumericType { get; set; }
    public bool TryParseIntoBestFit { get; set; }
    public ParseAsType ParsePrimitiveFloatingPointTypes { get; set; }
    public ParseAsType ParsePrimitiveIntegerTypes { get; set; }
    public bool ExcludeDefaultValues { get; set; }
    public bool IncludeNullValues { get; set; }
    public bool IncludeNullValuesInDictionaries { get; set; }
    public bool IncludeDefaultEnums { get; set; }
    public bool TreatEnumAsInteger { get; set; }
    public bool ExcludeTypeInfo { get; set; }
    public bool IncludeTypeInfo { get; set; }
    public bool Indent { get; set; }

    private string typeAttr;
    public string TypeAttr
    {
        get => typeAttr;
        set
        {
            typeAttrSpan = null;
            jsonTypeAttrInObject = null;
            jsvTypeAttrInObject = null;
            typeAttr = value;
        }
    }
    ReadOnlyMemory<char>? typeAttrSpan = null;
    public ReadOnlyMemory<char> TypeAttrMemory => typeAttrSpan ??= TypeAttr.AsMemory(); 
    public string DateTimeFormat { get; set; }
    private string jsonTypeAttrInObject;
    internal string JsonTypeAttrInObject => jsonTypeAttrInObject ??= JsonTypeSerializer.GetTypeAttrInObject(TypeAttr);
    private string jsvTypeAttrInObject;
    internal string JsvTypeAttrInObject => jsvTypeAttrInObject ??= JsvTypeSerializer.GetTypeAttrInObject(TypeAttr);
        
    public Func<Type, string> TypeWriter { get; set; }
    public Func<string, Type> TypeFinder { get; set; }
    public Func<string, object> ParsePrimitiveFn { get; set; }
    public bool SystemJsonCompatible { get; set; }
    public DateHandler DateHandler { get; set; }
    public TimeSpanHandler TimeSpanHandler { get; set; }
    public PropertyConvention PropertyConvention { get; set; }

    public TextCase TextCase { get; set; }
        
    [Obsolete("Use TextCase = TextCase.CamelCase")]
    public bool EmitCamelCaseNames
    {
        get => TextCase == TextCase.CamelCase;
        set => TextCase = value ? TextCase.CamelCase : TextCase;
    }

    [Obsolete("Use TextCase = TextCase.SnakeCase")]
    public bool EmitLowercaseUnderscoreNames
    {
        get => TextCase == TextCase.SnakeCase;
        set => TextCase = value ? TextCase.SnakeCase : TextCase.Default;
    }

    public bool ThrowOnError { get; set; }
    public bool SkipDateTimeConversion { get; set; }
    public bool AlwaysUseUtc { get; set; }
    public bool AssumeUtc { get; set; }
    public bool AppendUtcOffset { get; set; }
    public bool PreferInterfaces { get; set; }
    public bool IncludePublicFields { get; set; }
    public int MaxDepth { get; set; }
    public DeserializationErrorDelegate OnDeserializationError { get; set; }
    public EmptyCtorFactoryDelegate ModelFactory { get; set; }
    public string[] ExcludePropertyReferences { get; set; }
    public HashSet<Type> ExcludeTypes { get; set; }
    public HashSet<string> ExcludeTypeNames { get; set; }
    public bool EscapeUnicode { get; set; }
    public bool EscapeHtmlChars { get; set; }

    public static Config Defaults => new Config(null) {
        ConvertObjectTypesIntoStringDictionary = false,
        TryToParsePrimitiveTypeValues = false,
        TryToParseNumericType = false,
        TryParseIntoBestFit = false,
        ParsePrimitiveFloatingPointTypes = ParseAsType.Decimal,
        ParsePrimitiveIntegerTypes = ParseAsType.Byte | ParseAsType.SByte | ParseAsType.Int16 | ParseAsType.UInt16 |
                                     ParseAsType.Int32 | ParseAsType.UInt32 | ParseAsType.Int64 | ParseAsType.UInt64,
        ExcludeDefaultValues = false,
        ExcludePropertyReferences = null,
        IncludeNullValues = false,
        IncludeNullValuesInDictionaries = false,
        IncludeDefaultEnums = true,
        TreatEnumAsInteger = false,
        ExcludeTypeInfo = false,
        IncludeTypeInfo = false,
        Indent = false,
        TypeAttr = JsWriter.TypeAttr,
        DateTimeFormat = null,
        TypeWriter = AssemblyUtils.WriteType,
        TypeFinder = AssemblyUtils.FindType,
        ParsePrimitiveFn = null,
        DateHandler = Text.DateHandler.TimestampOffset,
        TimeSpanHandler = Text.TimeSpanHandler.DurationFormat,
        TextCase = TextCase.Default,
        PropertyConvention = Text.PropertyConvention.Strict,
        ThrowOnError = Env.StrictMode,
        SkipDateTimeConversion = false,
        AlwaysUseUtc = false,
        AssumeUtc = false,
        AppendUtcOffset = false,
        EscapeUnicode = false,
        EscapeHtmlChars = false,
        PreferInterfaces = false,
        IncludePublicFields = false,
        MaxDepth = 50,
        OnDeserializationError = null,
        ModelFactory = ReflectionExtensions.GetConstructorMethodToCache,
        ExcludeTypes = new HashSet<Type> {
            typeof(System.IO.Stream),
            typeof(System.Reflection.MethodBase),
        },
        ExcludeTypeNames = new HashSet<string> {}
    };

    public Config Populate(Config config)
    {
        ConvertObjectTypesIntoStringDictionary = config.ConvertObjectTypesIntoStringDictionary;
        TryToParsePrimitiveTypeValues = config.TryToParsePrimitiveTypeValues;
        TryToParseNumericType = config.TryToParseNumericType;
        TryParseIntoBestFit = config.TryParseIntoBestFit;
        ParsePrimitiveFloatingPointTypes = config.ParsePrimitiveFloatingPointTypes;
        ParsePrimitiveIntegerTypes = config.ParsePrimitiveIntegerTypes;
        ExcludeDefaultValues = config.ExcludeDefaultValues;
        ExcludePropertyReferences = config.ExcludePropertyReferences;
        IncludeNullValues = config.IncludeNullValues;
        IncludeNullValuesInDictionaries = config.IncludeNullValuesInDictionaries;
        IncludeDefaultEnums = config.IncludeDefaultEnums;
        TreatEnumAsInteger = config.TreatEnumAsInteger;
        ExcludeTypeInfo = config.ExcludeTypeInfo;
        IncludeTypeInfo = config.IncludeTypeInfo;
        Indent = config.Indent;
        TypeAttr = config.TypeAttr;
        DateTimeFormat = config.DateTimeFormat;
        TypeWriter = config.TypeWriter;
        TypeFinder = config.TypeFinder;
        ParsePrimitiveFn = config.ParsePrimitiveFn;
        DateHandler = config.DateHandler;
        TimeSpanHandler = config.TimeSpanHandler;
        TextCase = config.TextCase;
        PropertyConvention = config.PropertyConvention;
        ThrowOnError = config.ThrowOnError;
        SkipDateTimeConversion = config.SkipDateTimeConversion;
        AlwaysUseUtc = config.AlwaysUseUtc;
        AssumeUtc = config.AssumeUtc;
        AppendUtcOffset = config.AppendUtcOffset;
        EscapeUnicode = config.EscapeUnicode;
        EscapeHtmlChars = config.EscapeHtmlChars;
        PreferInterfaces = config.PreferInterfaces;
        IncludePublicFields = config.IncludePublicFields;
        MaxDepth = config.MaxDepth;
        OnDeserializationError = config.OnDeserializationError;
        ModelFactory = config.ModelFactory;
        ExcludeTypes = config.ExcludeTypes;
        ExcludeTypeNames = config.ExcludeTypeNames;
        return this;
    }
}