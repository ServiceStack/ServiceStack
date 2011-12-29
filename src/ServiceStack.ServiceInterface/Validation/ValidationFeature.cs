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
		public static void Init(IAppHost appHost)
		{
			var filter = new ValidationFilters();
			appHost.RequestFilters.Add(filter.RequestFilter);
		}

		public static void RegisterValidators(this Container container, params Assembly[] assemblies)
		{
			var autoWire = new ExpressionTypeFunqContainer(container);
			foreach (var assembly in assemblies)
			{
				foreach (var validator in assembly.GetTypes()
					.Where(t => t.IsOrHasGenericInterfaceTypeOf(typeof(IValidator<>))))
				{
					var baseType = validator.BaseType;
					while (!baseType.IsGenericType)
					{
						baseType = baseType.BaseType;
					}

					var dtoType = baseType.GetGenericArguments()[0];
					var validatorType = typeof(IValidator<>).MakeGenericType(dtoType);

					autoWire.RegisterType(validator, validatorType);
				}
			}
		}
	}
}
