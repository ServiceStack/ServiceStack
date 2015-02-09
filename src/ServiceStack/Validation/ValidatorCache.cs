using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
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
        public static IValidator GetValidator(IRequest httpReq)
        {
            return ((IResolver) httpReq).TryResolve<IValidator<T>>();
        }
    }

}