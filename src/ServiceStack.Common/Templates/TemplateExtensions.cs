using System;

namespace ServiceStack.Templates
{
    public static class TemplateExtensions
    {
        public static object InStopFilter(this Exception ex, TemplateScopeContext scope, object options)
        {
            throw new StopFilterExecutionException(scope, options, ex);
        }

        public static string AsString(this object str) => str is IRawString r ? r.ToRawString() : str?.ToString();
    }
}