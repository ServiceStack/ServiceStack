using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Funq;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Validation
{
	public static class ValidationFeature
	{
		public static bool Enabled { private set; get; }

        /// <summary>
        /// Activate the validation mechanism, so every request DTO with an existing validator
        /// will be validated.
        /// </summary>
        /// <param name="appHost">The app host</param>
		public static void Init(IAppHost appHost)
		{
			Enabled = true;
			var filter = new ValidationFilters();
			appHost.RequestFilters.Add(filter.RequestFilter);
		}

        /// <summary>
        /// Auto-scans the provided assemblies for a <see cref="IValidator"/>
        /// and registers it in the provided IoC container.
        /// </summary>
        /// <param name="container">The IoC container</param>
        /// <param name="assemblies">The assemblies to scan for a validator</param>
		public static void RegisterValidators(this Container container, params Assembly[] assemblies)
		{
			var autoWire = new AutoWireContainer(container);
			foreach (var assembly in assemblies)
			{
				foreach (var validator in assembly.GetTypes()
					.Where(t => t.IsOrHasGenericInterfaceTypeOf(typeof(IValidator<>))))
				{
					RegisterValidator(autoWire, validator);
				}
			}
		}

		public static void RegisterValidator(this Container container, Type validator)
		{
			RegisterValidator(new AutoWireContainer(container), validator);
		}

		private static void RegisterValidator(AutoWireContainer autoWire, Type validator)
		{
			var baseType = validator.BaseType;
			while (!baseType.IsGenericType)
			{
				baseType = baseType.BaseType;
			}

			var dtoType = baseType.GetGenericArguments()[0];
			var validatorType = typeof (IValidator<>).MakeGenericType(dtoType);

			autoWire.RegisterType(validator, validatorType);
		}
	}
}
