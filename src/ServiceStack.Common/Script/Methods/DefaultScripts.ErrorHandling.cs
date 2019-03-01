using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceStack.Script
{
    // ReSharper disable InconsistentNaming
    
    public partial class DefaultScripts
    {
        public object assignError(ScriptScopeContext scope, string errorBinding)
        {
            scope.PageResult.AssignExceptionsTo = errorBinding;
            return StopExecution.Value;
        }

        public object assignErrorAndContinueExecuting(ScriptScopeContext scope, string errorBinding)
        {
            assignError(scope, errorBinding);
            return continueExecutingFiltersOnError(scope);
        }

        public object continueExecutingFiltersOnError(ScriptScopeContext scope, object ignoreTarget) => continueExecutingFiltersOnError(scope);
        public object continueExecutingFiltersOnError(ScriptScopeContext scope)
        {
            scope.PageResult.SkipExecutingFiltersIfError = false;
            return StopExecution.Value;
        }

        public object skipExecutingFiltersOnError(ScriptScopeContext scope, object ignoreTarget) => skipExecutingFiltersOnError(scope);
        public object skipExecutingFiltersOnError(ScriptScopeContext scope)
        {
            scope.PageResult.SkipExecutingFiltersIfError = true;
            return StopExecution.Value;
        }

        public object endIfError(ScriptScopeContext scope) => scope.PageResult.LastFilterError != null ? (object)StopExecution.Value : IgnoreResult.Value;
        public object endIfError(ScriptScopeContext scope, object value) => scope.PageResult.LastFilterError != null ? StopExecution.Value : value;

        public object ifNoError(ScriptScopeContext scope) => scope.PageResult.LastFilterError != null ? (object)StopExecution.Value : IgnoreResult.Value;
        public object ifNoError(ScriptScopeContext scope, object value) => scope.PageResult.LastFilterError != null ? StopExecution.Value : value;

        public object ifError(ScriptScopeContext scope, object ignoreTarget) => ifError(scope);
        public object ifError(ScriptScopeContext scope) => (object) scope.PageResult.LastFilterError ?? StopExecution.Value;
        public object ifDebug(ScriptScopeContext scope, object ignoreTarget) => ifDebug(scope);
        public object ifDebug(ScriptScopeContext scope) => scope.Context.DebugMode ? (object)IgnoreResult.Value : StopExecution.Value;
        public object debug(ScriptScopeContext scope) => scope.Context.DebugMode;

        public bool hasError(ScriptScopeContext scope) => scope.PageResult.LastFilterError != null;
        
        public Exception lastError(ScriptScopeContext scope) => scope.PageResult.LastFilterError;
        public string lastErrorMessage(ScriptScopeContext scope) => scope.PageResult.LastFilterError?.Message;
        public string lastErrorStackTrace(ScriptScopeContext scope) => scope.PageResult.LastFilterStackTrace?.Length > 0
            ? scope.PageResult.LastFilterStackTrace.Map(x => "   at " + x).Join(Environment.NewLine)
            : null;

        public object ensureAllArgsNotNull(ScriptScopeContext scope, object args) => ensureAllArgsNotNull(scope, args, null);
        public object ensureAllArgsNotNull(ScriptScopeContext scope, object args, object options)
        {
            try
            {
                var filterArgs = options.AssertOptions(nameof(ensureAllArgsNotNull));
                var message = filterArgs.TryGetValue("message", out object oMessage) ? oMessage as string : null;
                
                if (args is IDictionary<string, object> argsMap)
                {
                    if (argsMap.Count == 0)
                        throw new NotSupportedException($"'{nameof(ensureAllArgsNotNull)}' expects a non empty Object Dictionary");
                    
                    var keys = argsMap.Keys.OrderBy(x => x);
                    foreach (var key in keys)
                    {
                        var value = argsMap[key];
                        if (!isNull(value)) 
                            continue;
                        
                        if (message != null)
                            throw new ArgumentException(string.Format(message, key));
                        
                        throw new ArgumentNullException(key);
                    }
                    return args;
                }
                throw new NotSupportedException($"'{nameof(ensureAllArgsNotNull)}' expects an Object Dictionary but received a '{args.GetType().Name}'");
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        public object ensureAnyArgsNotNull(ScriptScopeContext scope, object args) => ensureAnyArgsNotNull(scope, args, null);
        public object ensureAnyArgsNotNull(ScriptScopeContext scope, object args, object options)
        {
            try
            {
                var filterArgs = options.AssertOptions(nameof(ensureAnyArgsNotNull));
                var message = filterArgs.TryGetValue("message", out object oMessage) ? oMessage as string : null;
                
                if (args is IDictionary<string, object> argsMap)
                {
                    if (argsMap.Count == 0)
                        throw new NotSupportedException($"'{nameof(ensureAnyArgsNotNull)}' expects a non empty Object Dictionary");
                    
                    var keys = argsMap.Keys.OrderBy(x => x);
                    foreach (var key in keys)
                    {
                        var value = argsMap[key];
                        if (!isNull(value))
                            return args;
                    }

                    var firstKey = argsMap.Keys.OrderBy(x => x).First();
                    if (message != null)
                        throw new ArgumentException(string.Format(message, firstKey));
                        
                    throw new ArgumentNullException(firstKey);
                }
                throw new NotSupportedException($"'{nameof(ensureAnyArgsNotNull)}' expects an Object Dictionary but received a '{args.GetType().Name}'");
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        public object ensureAllArgsNotEmpty(ScriptScopeContext scope, object args) => ensureAllArgsNotEmpty(scope, args, null);
        public object ensureAllArgsNotEmpty(ScriptScopeContext scope, object args, object options)
        {
            try
            {
                var filterArgs = options.AssertOptions(nameof(ensureAllArgsNotEmpty));
                var message = filterArgs.TryGetValue("message", out object oMessage) ? oMessage as string : null;
                
                if (args is IDictionary<string, object> argsMap)
                {
                    if (argsMap.Count == 0)
                        throw new NotSupportedException($"'{nameof(ensureAllArgsNotEmpty)}' expects a non empty Object Dictionary");
                    
                    var keys = argsMap.Keys.OrderBy(x => x);
                    foreach (var key in keys)
                    {
                        var value = argsMap[key];
                        if (!isEmpty(value)) 
                            continue;
                        
                        if (message != null)
                            throw new ArgumentException(string.Format(message, key));
                        
                        throw new ArgumentNullException(key);
                    }
                    return args;
                }
                throw new NotSupportedException($"'{nameof(ensureAllArgsNotEmpty)}' expects an Object Dictionary but received a '{args.GetType().Name}'");
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        public object ensureAnyArgsNotEmpty(ScriptScopeContext scope, object args) => ensureAnyArgsNotEmpty(scope, args, null);
        public object ensureAnyArgsNotEmpty(ScriptScopeContext scope, object args, object options)
        {
            try
            {
                var filterArgs = options.AssertOptions(nameof(ensureAnyArgsNotEmpty));
                var message = filterArgs.TryGetValue("message", out object oMessage) ? oMessage as string : null;
                
                if (args is IDictionary<string, object> argsMap)
                {
                    if (argsMap.Count == 0)
                        throw new NotSupportedException($"'{nameof(ensureAnyArgsNotEmpty)}' expects a non empty Object Dictionary");

                    var keys = argsMap.Keys.OrderBy(x => x);
                    foreach (var key in keys)
                    {
                        var value = argsMap[key];
                        if (!isEmpty(value)) 
                            return args;
                    }

                    var firstKey = argsMap.Keys.OrderBy(x => x).First();
                    if (message != null)
                        throw new ArgumentException(string.Format(message, firstKey));
                        
                    throw new ArgumentNullException(firstKey);
                }
                throw new NotSupportedException($"'{nameof(ensureAnyArgsNotEmpty)}' expects an Object Dictionary but received a '{args.GetType().Name}'");
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }
        
        public object ifthrow(ScriptScopeContext scope, bool test, string message) => test 
            ? new Exception(message).InStopFilter(scope, null)
            : StopExecution.Value;
        public object ifthrow(ScriptScopeContext scope, bool test, string message, object options) => test 
            ? new Exception(message).InStopFilter(scope, options)
            : StopExecution.Value;

        public object throwIf(ScriptScopeContext scope, string message, bool test) => test 
            ? new Exception(message).InStopFilter(scope, null)
            : StopExecution.Value;
        public object throwIf(ScriptScopeContext scope, string message, bool test, object options) => test 
            ? new Exception(message).InStopFilter(scope, options)
            : StopExecution.Value;

        public object ifThrowArgumentException(ScriptScopeContext scope, bool test, string message) => test 
            ? new ArgumentException(message).InStopFilter(scope, null)
            : StopExecution.Value;

        public object ifThrowArgumentException(ScriptScopeContext scope, bool test, string message, object options)
        {
            if (!test) 
                return StopExecution.Value;
            
            if (options is string paramName)
                return new ArgumentException(message, paramName).InStopFilter(scope, null);

            return new ArgumentException(message).InStopFilter(scope, options);
        }

        public object ifThrowArgumentException(ScriptScopeContext scope, bool test, string message, string paramName, object options) => test 
            ? new ArgumentException(message, paramName).InStopFilter(scope, options)
            : StopExecution.Value;

        public object ifThrowArgumentNullException(ScriptScopeContext scope, bool test, string paramName) => test 
            ? new ArgumentNullException(paramName).InStopFilter(scope, null)
            : StopExecution.Value;
        public object ifThrowArgumentNullException(ScriptScopeContext scope, bool test, string paramName, object options) => test 
            ? new ArgumentNullException(paramName).InStopFilter(scope, options)
            : StopExecution.Value;
        
        public object throwArgumentNullExceptionIf(ScriptScopeContext scope, string paramName, bool test) => test 
            ? new ArgumentNullException(paramName).InStopFilter(scope, null)
            : StopExecution.Value;
        public object throwArgumentNullExceptionIf(ScriptScopeContext scope, string paramName, bool test, object options) => test 
            ? new ArgumentNullException(paramName).InStopFilter(scope, options)
            : StopExecution.Value;

        public object throwArgumentException(ScriptScopeContext scope, string message) => new ArgumentException(message).InStopFilter(scope, null);
        public object throwArgumentException(ScriptScopeContext scope, string message, string options) => ifThrowArgumentException(scope, true, message, options);
        public object throwArgumentNullException(ScriptScopeContext scope, string paramName) => new ArgumentNullException(paramName).InStopFilter(scope, null);
        public object throwArgumentNullException(ScriptScopeContext scope, string paramName, object options) => new ArgumentNullException(paramName).InStopFilter(scope, options);
        public object throwNotSupportedException(ScriptScopeContext scope, string message) => new NotSupportedException(message).InStopFilter(scope, null);
        public object throwNotSupportedException(ScriptScopeContext scope, string message, object options) => new NotSupportedException(message).InStopFilter(scope, options);
        public object throwNotImplementedException(ScriptScopeContext scope, string message) => new NotImplementedException(message).InStopFilter(scope, null);
        public object throwNotImplementedException(ScriptScopeContext scope, string message, object options) => new NotImplementedException(message).InStopFilter(scope, options);
        public object throwUnauthorizedAccessException(ScriptScopeContext scope, string message) => new UnauthorizedAccessException(message).InStopFilter(scope, null);
        public object throwUnauthorizedAccessException(ScriptScopeContext scope, string message, object options) => new UnauthorizedAccessException(message).InStopFilter(scope, options);
        public object throwFileNotFoundException(ScriptScopeContext scope, string message) => new FileNotFoundException(message).InStopFilter(scope, null);
        public object throwFileNotFoundException(ScriptScopeContext scope, string message, object options) => new FileNotFoundException(message).InStopFilter(scope, options);
        public object throwOptimisticConcurrencyException(ScriptScopeContext scope, string message) => new Data.OptimisticConcurrencyException(message).InStopFilter(scope, null);
        public object throwOptimisticConcurrencyException(ScriptScopeContext scope, string message, object options) => new Data.OptimisticConcurrencyException(message).InStopFilter(scope, options);

        public async Task<object> @throwAsync(ScriptScopeContext scope, string message)
        {
            await Task.Yield();
            return new Exception(message).InStopFilter(scope, null);
        }

        public async Task<object> @throwAsync(ScriptScopeContext scope, string message, object options)
        {
            await Task.Yield();
            return new Exception(message).InStopFilter(scope, options);
        }
    }
}