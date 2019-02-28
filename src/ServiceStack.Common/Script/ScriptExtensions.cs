using System;

namespace ServiceStack.Script
{
    public static class ScriptExtensions
    {
        public static object InStopFilter(this Exception ex, ScriptScopeContext scope, object options)
        {
            throw new StopFilterExecutionException(scope, options, ex);
        }

        public static string AsString(this object str) => str is IRawString r ? r.ToRawString() : str?.ToString();
    }
}