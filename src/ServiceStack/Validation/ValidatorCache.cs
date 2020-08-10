using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ServiceStack.FluentValidation;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Validation
{
    public static class ValidatorCache
    {
        private static Dictionary<Type, ResolveValidatorDelegate> delegateCache
        = new Dictionary<Type, ResolveValidatorDelegate>();

        private delegate IValidator ResolveValidatorDelegate(IRequest httpReq);

        public static IValidator GetValidator(IRequest httpReq, Type type)
        {
            if (delegateCache.TryGetValue(type, out var parseFn)) 
                return parseFn.Invoke(httpReq);

            var genericType = typeof(ValidatorCache<>).MakeGenericType(type);
            var mi = genericType.GetMethod("GetValidator", BindingFlags.Public | BindingFlags.Static);
            parseFn = (ResolveValidatorDelegate)mi.CreateDelegate(typeof(ResolveValidatorDelegate));

            Dictionary<Type, ResolveValidatorDelegate> snapshot, newCache;
            do
            {
                snapshot = delegateCache;
                newCache = new Dictionary<Type, ResolveValidatorDelegate>(delegateCache) { [type] = parseFn };
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref delegateCache, newCache, snapshot), snapshot));

            return parseFn.Invoke(httpReq);
        }
    }

    public class ValidatorCache<T>
    {
        public static IValidator GetValidator(IRequest httpReq)
        {
            try
            {
                var validator = httpReq.TryResolve<IValidator<T>>();
                if (validator is IRequiresRequest hasRequest)
                    hasRequest.Request = httpReq;
                return validator;
            }
            catch (Exception e)
            {
                var log = LogManager.GetLogger(typeof(ValidatorCache));
                log.Error($@"ValidatorCache<{typeof(T).Name}>.GetValidator() at {httpReq.PathInfo}", e);
                throw;
            }
        }
    }
}