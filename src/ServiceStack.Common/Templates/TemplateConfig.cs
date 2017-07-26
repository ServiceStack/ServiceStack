using System.Collections.Generic;

namespace ServiceStack.Templates
{
    public static class TemplateConfig
    {
        public static HashSet<string> RemoveNewLineAfterFiltersNamed { get; set; } = new HashSet<string>
        {
            "assignTo",
            "do"
        };
    }
}