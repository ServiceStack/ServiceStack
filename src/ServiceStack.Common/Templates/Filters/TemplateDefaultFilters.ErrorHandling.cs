using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceStack.Templates
{
    // ReSharper disable InconsistentNaming
    
    public partial class TemplateDefaultFilters
    {
        public object assignError(TemplateScopeContext scope, string errorBinding)
        {
            scope.PageResult.AssignExceptionsTo = errorBinding;
            return StopExecution.Value;
        }

        public object assignErrorAndContinueExecuting(TemplateScopeContext scope, string errorBinding)
        {
            assignError(scope, errorBinding);
            return continueExecutingFiltersOnError(scope);
        }

        public object continueExecutingFiltersOnError(TemplateScopeContext scope, object ignoreTarget) => continueExecutingFiltersOnError(scope);
        public object continueExecutingFiltersOnError(TemplateScopeContext scope)
        {
            scope.PageResult.SkipExecutingFiltersIfError = false;
            return StopExecution.Value;
        }

        public object skipExecutingFiltersOnError(TemplateScopeContext scope, object ignoreTarget) => skipExecutingFiltersOnError(scope);
        public object skipExecutingFiltersOnError(TemplateScopeContext scope)
        {
            scope.PageResult.SkipExecutingFiltersIfError = true;
            return StopExecution.Value;
        }

        [HandleUnknownValue] public object endIfError(TemplateScopeContext scope) => scope.PageResult.LastFilterError != null ? (object)StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object endIfError(TemplateScopeContext scope, object value) => scope.PageResult.LastFilterError != null ? StopExecution.Value : value;

        [HandleUnknownValue] public object ifNoError(TemplateScopeContext scope) => scope.PageResult.LastFilterError != null ? (object)StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object ifNoError(TemplateScopeContext scope, object value) => scope.PageResult.LastFilterError != null ? StopExecution.Value : value;

        [HandleUnknownValue] public object ifError(TemplateScopeContext scope, object ignoreTarget) => ifError(scope);
        [HandleUnknownValue] public object ifError(TemplateScopeContext scope) => (object) scope.PageResult.LastFilterError ?? StopExecution.Value;
        [HandleUnknownValue] public object ifDebug(TemplateScopeContext scope, object ignoreTarget) => ifDebug(scope);
        [HandleUnknownValue] public object ifDebug(TemplateScopeContext scope) => scope.Context.DebugMode ? (object)IgnoreResult.Value : StopExecution.Value;
        public object debug(TemplateScopeContext scope) => scope.Context.DebugMode;

        public bool hasError(TemplateScopeContext scope) => scope.PageResult.LastFilterError != null;
        
        [HandleUnknownValue] public Exception lastError(TemplateScopeContext scope) => scope.PageResult.LastFilterError;
        [HandleUnknownValue] public string lastErrorMessage(TemplateScopeContext scope) => scope.PageResult.LastFilterError?.Message;
        [HandleUnknownValue] public string lastErrorStackTrace(TemplateScopeContext scope) => scope.PageResult.LastFilterStackTrace?.Length > 0
            ? scope.PageResult.LastFilterStackTrace.Map(x => "   at " + x).Join("\n")
            : null;

        public object ensureAllArgsNotNull(TemplateScopeContext scope, object args) => ensureAllArgsNotNull(scope, args, null);
        public object ensureAllArgsNotNull(TemplateScopeContext scope, object args, object options)
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

        public object ensureAnyArgsNotNull(TemplateScopeContext scope, object args) => ensureAnyArgsNotNull(scope, args, null);
        public object ensureAnyArgsNotNull(TemplateScopeContext scope, object args, object options)
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

        public object ensureAllArgsNotEmpty(TemplateScopeContext scope, object args) => ensureAllArgsNotEmpty(scope, args, null);
        public object ensureAllArgsNotEmpty(TemplateScopeContext scope, object args, object options)
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

        public object ensureAnyArgsNotEmpty(TemplateScopeContext scope, object args) => ensureAnyArgsNotEmpty(scope, args, null);
        public object ensureAnyArgsNotEmpty(TemplateScopeContext scope, object args, object options)
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
        
        public object ifthrow(TemplateScopeContext scope, bool test, string message) => test 
            ? new Exception(message).InStopFilter(scope, null)
            : StopExecution.Value;
        public object ifthrow(TemplateScopeContext scope, bool test, string message, object options) => test 
            ? new Exception(message).InStopFilter(scope, options)
            : StopExecution.Value;

        public object throwIf(TemplateScopeContext scope, string message, bool test) => test 
            ? new Exception(message).InStopFilter(scope, null)
            : StopExecution.Value;
        public object throwIf(TemplateScopeContext scope, string message, bool test, object options) => test 
            ? new Exception(message).InStopFilter(scope, options)
            : StopExecution.Value;

        public object ifThrowArgumentException(TemplateScopeContext scope, bool test, string message) => test 
            ? new ArgumentException(message).InStopFilter(scope, null)
            : StopExecution.Value;

        public object ifThrowArgumentException(TemplateScopeContext scope, bool test, string message, object options)
        {
            if (!test) 
                return StopExecution.Value;
            
            if (options is string paramName)
                return new ArgumentException(message, paramName).InStopFilter(scope, null);

            return new ArgumentException(message).InStopFilter(scope, options);
        }

        public object ifThrowArgumentException(TemplateScopeContext scope, bool test, string message, string paramName, object options) => test 
            ? new ArgumentException(message, paramName).InStopFilter(scope, options)
            : StopExecution.Value;

        public object ifThrowArgumentNullException(TemplateScopeContext scope, bool test, string paramName) => test 
            ? new ArgumentNullException(paramName).InStopFilter(scope, null)
            : StopExecution.Value;
        public object ifThrowArgumentNullException(TemplateScopeContext scope, bool test, string paramName, object options) => test 
            ? new ArgumentNullException(paramName).InStopFilter(scope, options)
            : StopExecution.Value;
        
        public object throwArgumentNullExceptionIf(TemplateScopeContext scope, string paramName, bool test) => test 
            ? new ArgumentNullException(paramName).InStopFilter(scope, null)
            : StopExecution.Value;
        public object throwArgumentNullExceptionIf(TemplateScopeContext scope, string paramName, bool test, object options) => test 
            ? new ArgumentNullException(paramName).InStopFilter(scope, options)
            : StopExecution.Value;

        public object throwArgumentException(TemplateScopeContext scope, string message) => new ArgumentException(message).InStopFilter(scope, null);
        public object throwArgumentException(TemplateScopeContext scope, string message, string options) => ifThrowArgumentException(scope, true, message, options);
        public object throwArgumentNullException(TemplateScopeContext scope, string paramName) => new ArgumentNullException(paramName).InStopFilter(scope, null);
        public object throwArgumentNullException(TemplateScopeContext scope, string paramName, object options) => new ArgumentNullException(paramName).InStopFilter(scope, options);
        public object throwNotSupportedException(TemplateScopeContext scope, string message) => new NotSupportedException(message).InStopFilter(scope, null);
        public object throwNotSupportedException(TemplateScopeContext scope, string message, object options) => new NotSupportedException(message).InStopFilter(scope, options);
        public object throwNotImplementedException(TemplateScopeContext scope, string message) => new NotImplementedException(message).InStopFilter(scope, null);
        public object throwNotImplementedException(TemplateScopeContext scope, string message, object options) => new NotImplementedException(message).InStopFilter(scope, options);
        public object throwUnauthorizedAccessException(TemplateScopeContext scope, string message) => new UnauthorizedAccessException(message).InStopFilter(scope, null);
        public object throwUnauthorizedAccessException(TemplateScopeContext scope, string message, object options) => new UnauthorizedAccessException(message).InStopFilter(scope, options);
        public object throwFileNotFoundException(TemplateScopeContext scope, string message) => new FileNotFoundException(message).InStopFilter(scope, null);
        public object throwFileNotFoundException(TemplateScopeContext scope, string message, object options) => new FileNotFoundException(message).InStopFilter(scope, options);
        public object throwOptimisticConcurrencyException(TemplateScopeContext scope, string message) => new Data.OptimisticConcurrencyException(message).InStopFilter(scope, null);
        public object throwOptimisticConcurrencyException(TemplateScopeContext scope, string message, object options) => new Data.OptimisticConcurrencyException(message).InStopFilter(scope, options);

        public async Task<object> @throwAsync(TemplateScopeContext scope, string message)
        {
            await Task.Yield();
            return new Exception(message).InStopFilter(scope, null);
        }

        public async Task<object> @throwAsync(TemplateScopeContext scope, string message, object options)
        {
            await Task.Yield();
            return new Exception(message).InStopFilter(scope, options);
        }
    }
}