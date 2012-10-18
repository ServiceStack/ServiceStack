using System;
using System.Linq;
using System.Reflection;
using Funq;
using ServiceStack.Common.Extensions;
using ServiceStack.FluentValidation;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Validation;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Validation
{
    public class ValidationFeature : IPlugin
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ValidationFeature));

        public static bool Enabled { private set; get; }
        private IAppHost appHost;
        private HandleServiceExceptionDelegate existingHandler;

        /// <summary>
        /// Activate the validation mechanism, so every request DTO with an existing validator
        /// will be validated.
        /// </summary>
        /// <param name="appHost">The app host</param>
        public void Register(IAppHost appHost)
        {
            Enabled = true;
            var filter = new ValidationFilters();
            this.appHost = appHost;
            appHost.RequestFilters.Add(filter.RequestFilter);

            existingHandler = appHost.ServiceExceptionHandler;
            appHost.ServiceExceptionHandler = HandleException;
        }
        
        public object HandleException(object request, Exception ex)
        {
            var validationException = ex as ValidationException;
            if (validationException != null)
            {
                var errors = validationException.Errors.ConvertAll(x =>
                    new ValidationErrorField(x.ErrorCode, x.PropertyName, x.ErrorMessage));

                return DtoUtils.CreateErrorResponse(typeof(ValidationException).Name, validationException.Message, errors);
            }

            return existingHandler != null
                ? existingHandler(request, ex)
                : DtoUtils.HandleException(appHost, request, ex);
        }

        public static object HandleException(IResolver resolver, object request, Exception ex)
        {
            var validationException = ex as ValidationException;
            if (validationException != null)
            {
                var errors = validationException.Errors.ConvertAll(x =>
                    new ValidationErrorField(x.ErrorCode, x.PropertyName, x.ErrorMessage));

                return DtoUtils.CreateErrorResponse(typeof(ValidationException).Name, validationException.Message, errors);
            }

            return DtoUtils.HandleException(resolver, request, ex);
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

            return string.Format("[{0}: {1}]:\n[REQUEST: {2}]", GetType().Name, DateTime.UtcNow, requestString);
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
            foreach (var assembly in assemblies)
            {
                foreach (var validator in assembly.GetTypes()
                .Where(t => t.IsOrHasGenericInterfaceTypeOf(typeof(IValidator<>))))
                {
                    RegisterValidator(container, validator);
                }
            }
        }

        public static void RegisterValidator(this Container container, Type validator)
        {
            var baseType = validator.BaseType;
            while (!baseType.IsGenericType)
            {
                baseType = baseType.BaseType;
            }

            var dtoType = baseType.GetGenericArguments()[0];
            var validatorType = typeof(IValidator<>).MakeGenericType(dtoType);

            container.RegisterAutoWiredType(validator, validatorType);
        }
    }
}
