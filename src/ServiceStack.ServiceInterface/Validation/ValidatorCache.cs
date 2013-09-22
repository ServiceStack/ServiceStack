using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ServiceStack.FluentValidation;
using ServiceStack.Server;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface.Validation
{
    public static class ValidatorCache
    {
        private static Dictionary<Type, ResolveValidatorDelegate> delegateCache 
        = new Dictionary<Type, ResolveValidatorDelegate>();
        
        private delegate IValidator ResolveValidatorDelegate(IHttpRequest httpReq);

        public static IValidator GetValidator(IHttpRequest httpReq, Type type)
        {
            ResolveValidatorDelegate parseFn;
            if (delegateCache.TryGetValue(type, out parseFn)) return parseFn.Invoke(httpReq);			

            var genericType = typeof(ValidatorCache<>).MakeGenericType(type);
            var mi = genericType.GetMethod("GetValidator", BindingFlags.Public | BindingFlags.Static);
            parseFn = (ResolveValidatorDelegate)Delegate.CreateDelegate(typeof(ResolveValidatorDelegate), mi);

            Dictionary<Type, ResolveValidatorDelegate> snapshot, newCache;
            do
            {
                snapshot = delegateCache;
                newCache = new Dictionary<Type, ResolveValidatorDelegate>(delegateCache);
                newCache[type] = parseFn;

            } while (!ReferenceEquals(
            Interlocked.CompareExchange(ref delegateCache, newCache, snapshot), snapshot));

            return parseFn.Invoke(httpReq);
        }		
    }

    public class ValidatorCache<T>
    {
        public static IValidator GetValidator(IHttpRequest httpReq)
        {
            return httpReq.TryResolve<IValidator<T>>();
        }
    }

}