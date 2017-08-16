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
    }
}