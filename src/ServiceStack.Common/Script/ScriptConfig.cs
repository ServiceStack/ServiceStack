using System;
using System.Collections.Generic;
using System.Globalization;

namespace ServiceStack.Script
{
    public static class ScriptConfig
    {
        public static HashSet<string> RemoveNewLineAfterFiltersNamed { get; set; } = new HashSet<string>
        {
            "assignTo",
            "assignToGlobal",
            "assignError",
            "addTo",
            "addToGlobal",
            "addToStart",
            "addToStartGlobal",
            "appendTo",
            "appendToGlobal",
            "prependTo",
            "prependToGlobal",
            "do",
            "end",
            "throw",
            "ifthrow",
            "throwIf",
            "throwIf",
            "ifThrowArgumentException",
            "ifThrowArgumentNullException",
            "throwArgumentNullExceptionIf",
            "throwArgumentException",
            "throwArgumentNullException",
            "throwNotSupportedException",
            "throwNotImplementedException",
            "throwUnauthorizedAccessException",
            "throwFileNotFoundException",
            "throwOptimisticConcurrencyException",
            "throwNotSupportedException",
            "ifError",
            "ifErrorFmt",
            "skipExecutingFiltersOnError",
            "continueExecutingFiltersOnError",
            "publishToGateway",
        };
        
        public static HashSet<string> OnlyEvaluateFiltersWhenSkippingPageFilterExecution { get; set; } = new HashSet<string>
        {
            "ifError",
            "lastError",
            "htmlError",
            "htmlErrorMessage",
            "htmlErrorDebug",
        };
        
        /// <summary>
        /// Rethrow fatal exceptions thrown on incorrect API usage    
        /// </summary>
        public static HashSet<Type> FatalExceptions { get; set; } = new HashSet<Type>
        {
            typeof(NotSupportedException),
            typeof(System.Reflection.TargetInvocationException),
            typeof(NotImplementedException),
        };
        
        public static HashSet<Type> CaptureAndEvaluateExceptionsToNull { get; set; } = new HashSet<Type>
        {
            typeof(NullReferenceException),
            typeof(ArgumentNullException),
        };
        
        public static HashSet<string> DontEvaluateBlocksNamed { get; set; } = new HashSet<string> {
            "raw"
        };

        public static int MaxQuota { get; set; } = 10000;
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
}