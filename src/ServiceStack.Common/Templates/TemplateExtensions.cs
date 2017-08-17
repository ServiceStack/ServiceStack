using System;

namespace ServiceStack.Templates
{
    public static class TemplateExtensions
    {
        public static object InStopFilter(this Exception ex, TemplateScopeContext scope, object options)
        {
            try
            {
                throw ex; //capture StackTrace in Original Exception
            }
            catch (Exception e)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
            return null;
        }

    }
}