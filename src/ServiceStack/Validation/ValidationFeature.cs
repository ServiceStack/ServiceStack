using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Funq;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Validation
{
    public class ValidationFeature : IPlugin
    {
        public Func<IRequest, ValidationResult, object, object> ErrorResponseFilter { get; set; }

        public bool ScanAppHostAssemblies { get; set; } = true;
        public bool TreatInfoAndWarningsAsErrors { get; set; } = true;
        
        /// <summary>
        /// Activate the validation mechanism, so every request DTO with an existing validator
        /// will be validated.
        /// </summary>
        /// <param name="appHost">The app host</param>
        public void Register(IAppHost appHost)
        {
            if (TreatInfoAndWarningsAsErrors)
            {
                if (!appHost.GlobalRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsync))
                {
                    appHost.GlobalRequestFiltersAsync.Add(ValidationFilters.RequestFilterAsync);
                }

                if (!appHost.GlobalMessageRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsync))
                {
                    appHost.GlobalMessageRequestFiltersAsync.Add(ValidationFilters.RequestFilterAsync);
                }
            }
            else
            {
                if (!appHost.GlobalRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsyncIgnoreWarningsInfo))
                {
                    appHost.GlobalRequestFiltersAsync.Add(ValidationFilters.RequestFilterAsyncIgnoreWarningsInfo);
                }

                if (!appHost.GlobalMessageRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsyncIgnoreWarningsInfo))
                {
                    appHost.GlobalMessageRequestFiltersAsync.Add(ValidationFilters.RequestFilterAsyncIgnoreWarningsInfo);
                }
                
                if (!appHost.GlobalResponseFiltersAsync.Contains(ValidationFilters.ResponseFilterAsync))
                {
                    appHost.GlobalResponseFiltersAsync.Add(ValidationFilters.ResponseFilterAsync);
                }

                if (!appHost.GlobalMessageResponseFiltersAsync.Contains(ValidationFilters.ResponseFilterAsync))
                {
                    appHost.GlobalMessageResponseFiltersAsync.Add(ValidationFilters.ResponseFilterAsync);
                }
            }

            if (ScanAppHostAssemblies)
            {
                appHost.GetContainer().RegisterValidators(((ServiceStackHost)appHost).ServiceAssemblies.ToArray());
            }
        }
       
        /// <summary>
        /// Override to provide additional/less context about the Service Exception. 
        /// By default the request is serialized and appended to the ResponseStatus StackTrace.
        /// </summary>
        public virtual string GetRequestErrorBody(object request)
        {
            var requestString = "";
            try
            {
                requestString = TypeSerializer.SerializeToString(request);
            }
            catch /*(Exception ignoreSerializationException)*/
            {
                //Serializing request successfully is not critical and only provides added error info
            }

            return $"[{GetType().GetOperationName()}: {DateTime.UtcNow}]:\n[REQUEST: {requestString}]";
        }
    }

    public static class ValidationExtensions
    {
        /// <summary>
        /// Auto-scans the provided assemblies for a <see cref="IValidator"/>
        /// and registers it in the provided IoC container.
        /// </summary>
        /// <param name="container">The IoC container</param>
        /// <param name="assemblies">The assemblies to scan for a validator</param>
        public static void RegisterValidators(this Container container, params Assembly[] assemblies)
        {
            RegisterValidators(container, ReuseScope.None, assemblies);
        }

        public static void RegisterValidators(this Container container, ReuseScope scope, params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                foreach (var validator in assembly.GetTypes()
                    .Where(t => t.IsOrHasGenericInterfaceTypeOf(typeof(IValidator<>))))
                {
                    container.RegisterValidator(validator, scope);
                }
            }
        }

        public static void RegisterValidator(this Container container, Type validator, ReuseScope scope=ReuseScope.None)
        {
            var baseType = validator.BaseType;
            if (validator.IsInterface || baseType == null)
                return;

            while (baseType != null && !baseType.IsGenericType)
            {
                baseType = baseType.BaseType;
            }

            if (baseType == null)
                return;

            var dtoType = baseType.GetGenericArguments()[0];
            var validatorType = typeof(IValidator<>).GetCachedGenericType(dtoType);

            container.RegisterAutoWiredType(validator, validatorType, scope);
        }

        public static bool HasAsyncValidators(this IValidator validator, ValidationContext context, string ruleSet=null)
        {
            if (validator is IEnumerable<IValidationRule> rules)
            {
                foreach (var rule in rules)
                {
                    if (ruleSet != null && rule.RuleSets != null && !rule.RuleSets.Contains(ruleSet))
                        continue;

                    if (rule.Validators.Any(x => x is AsyncPredicateValidator || x is AsyncValidatorBase ||  x.ShouldValidateAsync(context)))
                        return true;
                }
            }
            return false;
        }
    }
}
