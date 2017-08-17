using System;
using System.Collections.Generic;

namespace ServiceStack.Templates
{
    public static class TemplateConfig
    {
        public static HashSet<string> RemoveNewLineAfterFiltersNamed { get; set; } = new HashSet<string>
        {
            "assignTo",
            "do",
            "end",
            "throw",
            "endIfError",
            "ifError",
            "ifErrorFmt",
            "ifErrorSkipExecutingPageFilters",
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
    }
}