using System;
using System.Collections.Generic;
using System.Globalization;
using ServiceStack.Text;

namespace ServiceStack.Script;

public static class ScriptConfig
{
    /// <summary>
    /// Rethrow fatal exceptions thrown on incorrect API usage    
    /// </summary>
    public static HashSet<Type> FatalExceptions { get; set; } = new HashSet<Type>
    {
        typeof(NotSupportedException),
        typeof(NotImplementedException),
        typeof(StackOverflowException),
    };
        
    public static HashSet<Type> CaptureAndEvaluateExceptionsToNull { get; set; } = new HashSet<Type>
    {
        typeof(NullReferenceException),
        typeof(ArgumentNullException),
    };
    public static CultureInfo DefaultCulture { get; set; } //Uses CurrentCulture by default
    public static string DefaultDateFormat { get; set; }  = "yyyy-MM-dd";
    public static string DefaultDateTimeFormat { get; set; } = "u";
    public static string DefaultTimeFormat { get; set; } = @"h\:mm\:ss";
    public static TimeSpan DefaultFileCacheExpiry { get; set; } =TimeSpan.FromMinutes(1);
    public static TimeSpan DefaultUrlCacheExpiry { get; set; } =TimeSpan.FromMinutes(1);
    public static string DefaultIndent { get; set; } = "\t";
    public static string DefaultNewLine { get; set; } = Environment.NewLine;
    public static string DefaultJsConfig { get; set; } = "excludetypeinfo";
    public static StringComparison DefaultStringComparison { get; set; } = StringComparison.Ordinal;
    public static string DefaultTableClassName { get; set; } = "table";
    public static string DefaultErrorClassName { get; set; } = "alert alert-danger";
    public static bool AllowUnixPipeSyntax { get; set; } = true;
    public static bool AllowAssignmentExpressions { get; set; } = true;
    public static ParseRealNumber ParseRealNumber = numLiteral => numLiteral.ParseDouble();

    public static CultureInfo CreateCulture()
    {
        var culture = DefaultCulture;
        if (culture == null)
        {
            culture = CultureInfo.CurrentCulture;
        }
        if (Equals(culture, CultureInfo.InvariantCulture))
        {
            culture = (CultureInfo) culture.Clone();
            culture.NumberFormat.CurrencySymbol = "$";
        }
        return culture;
    }
}

public delegate object ParseRealNumber(ReadOnlySpan<char> numLiteral);