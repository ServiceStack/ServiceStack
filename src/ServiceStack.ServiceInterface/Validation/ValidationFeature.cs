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
			var filter = new ValidationFilter();
			appHost.RequestFilters.Add(filter.ValidateRequest);
		}

		public static void RegisterValidators(this Container container, params Assembly[] assemblies)
		{
			foreach (var assembly in assemblies)
			{
				var validators = 
                    from t in assembly.GetTypes()
					where
						!t.IsAbstract 
						&& t.IsGenericType 
						&& t.GetGenericTypeDefinition() == typeof(IValidator<>)
					select t;

				foreach (var validator in validators)
				{
					var baseType = validator.BaseType;
					while (!baseType.IsGenericType)
					{
						baseType = baseType.BaseType;
					}

					var dtoType = baseType.GetGenericArguments()[0];
					var validatorType = typeof(IValidator<>).MakeGenericType(dtoType);

					var registerFn = typeof(Container).GetMethods(BindingFlags.Public)
						.First(x => x.Name == "Register" 
							&& x.ReturnType == typeof(void) && x.GetParameters().Length == 1)
						.MakeGenericMethod(validatorType);
					
					registerFn.Invoke(container, new[] { validator });
				}
			}
		}
	}
}
