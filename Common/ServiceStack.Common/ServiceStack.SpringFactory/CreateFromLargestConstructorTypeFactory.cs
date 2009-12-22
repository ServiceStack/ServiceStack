using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Common.Utils;

namespace ServiceStack.SpringFactory
{
	public class CreateFromLargestConstructorTypeFactory
	{
		private readonly FactoryProvider factoryProvider;
		private readonly object readWriteLock = new object();

		public CreateFromLargestConstructorTypeFactory(FactoryProvider factoryProvider)
		{
			this.factoryProvider = factoryProvider;
		}

		private class ConstructorDefinition
		{
			public ConstructorDefinition(object[] argValues, ObjectActivator objectActivator)
			{
				this.objectActivator = objectActivator;
				this.contructorArgValues = argValues;
			}

			private readonly ObjectActivator objectActivator;

			private readonly object[] contructorArgValues;

			public object Create()
			{
				return objectActivator(contructorArgValues);
			}
		}

		readonly Dictionary<Type, ConstructorDefinition> objectActivatorMap 
			= new Dictionary<Type, ConstructorDefinition>();

		public object Create(Type type)
		{
			ConstructorDefinition ctorDefinition;
			lock (readWriteLock)
			{
				if (!objectActivatorMap.TryGetValue(type, out ctorDefinition))
				{
					var largestConstructor = type.GetConstructors()
						.OrderByDescending(x => x.GetParameters().Length).First();

					var ctorValues = new List<object>();
					foreach (var parameterInfo in largestConstructor.GetParameters())
					{
						var ctorValue = factoryProvider.Resolve(parameterInfo.ParameterType)
						                ?? ReflectionUtils.GetDefaultValue(parameterInfo.ParameterType);

						ctorValues.Add(ctorValue);
					}

					ctorDefinition = new ConstructorDefinition(
						ctorValues.ToArray(), GetActivator(largestConstructor));

					objectActivatorMap[type] = ctorDefinition;
				}
			}

			return ctorDefinition.Create();
		}

		//courtesy of http://rogeralsing.com/2008/02/28/linq-expressions-creating-objects/
		public delegate object ObjectActivator(params object[] args);

		public static ObjectActivator GetActivator(ConstructorInfo ctor)
		{
			var type = ctor.DeclaringType;
			var paramsInfo = ctor.GetParameters();

			//create a single param of type object[]
			var param = Expression.Parameter(typeof(object[]), "args");

			var argsExp = new Expression[paramsInfo.Length];

			//pick each arg from the params array 
			//and create a typed expression of them
			for (var i = 0; i < paramsInfo.Length; i++)
			{
				var index = Expression.Constant(i);
				var paramType = paramsInfo[i].ParameterType;

				var paramAccessorExp = Expression.ArrayIndex(param, index);

				var paramCastExp = Expression.Convert(paramAccessorExp, paramType);

				argsExp[i] = paramCastExp;
			}

			//make a NewExpression that calls the
			//ctor with the args we just created
			var newExp = Expression.New(ctor, argsExp);

			//create a lambda with the New
			//Expression as body and our param object[] as arg
			var lambda = Expression.Lambda(typeof(ObjectActivator), newExp, param);

			//compile it
			var compiled = (ObjectActivator)lambda.Compile();
			return compiled;
		}
	}
}