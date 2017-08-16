using System;

namespace ServiceStack.Templates
{
    public static class TemplateExtensions
    {
        public static StopFilterExecutionException InStopFilter(this Exception ex, TemplateScopeContext scope, object options) =>
            new StopFilterExecutionException(scope, options, ex);

    }
}