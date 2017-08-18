using System;

namespace ServiceStack.Templates
{
    public static class TemplateExtensions
    {
        public static object InStopFilter(this Exception ex, TemplateScopeContext scope, object options)
        {
            throw new StopFilterExecutionException(scope, options, ex);
        }

    }
}