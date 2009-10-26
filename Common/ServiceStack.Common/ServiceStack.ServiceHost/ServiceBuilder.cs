using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceHost
{
	public class ServiceController
	{
		private readonly Dictionary<Type, Delegate> handlerMap = 
			new Dictionary<Type, Delegate>();

		readonly Dictionary<Type, Func<object,object>> dynamicHandlerFactoryMap =
			new Dictionary<Type, Func<object, object>>();

		public void Register(Assembly assemblyWithServices)
		{
		}

		public void Register<TServiceRequest>(Func<IService<TServiceRequest>> invoker)
		{
			var requestType = typeof(TServiceRequest);
			handlerMap.Add(requestType, invoker);
		}

		public void Register(Type requestType, Type serviceType)
		{
			dynamicHandlerFactoryMap.Add(requestType, GetHandlerFn(requestType, serviceType));
		}

		public object Execute<TServiceRequest>(TServiceRequest dto)
		{
			var factory = (Func<IService<TServiceRequest>>)handlerMap[dto.GetType()];
			IService<TServiceRequest> service = factory();

			return service.Execute(dto);
		}

		public object ExecuteSlow(object dto)
		{
			var requestType = dto.GetType();
			var oServiceFn = handlerMap[requestType];
			return ExecuteService(dto, requestType, oServiceFn);
		}

		public object Execute(object dto)
		{
			var requestType = dto.GetType();
			return ExecuteService(dto, requestType);
		}

		private static object ExecuteService(object requestDto, Type requestType, Delegate factoryDelegate)
		{
			Func<object, object> func = dto => {
				object service = factoryDelegate.Method.Invoke(factoryDelegate, new object[0]);

				var mi = service.GetType().GetMethod("Execute", new[] { requestType });
				var result = mi.Invoke(service, new[] { dto });
				return result;
			};

			return func(requestDto);
		}

		private object ExecuteService(object requestDto, Type requestType)
		{
			Func<object, object> handlerFn = dynamicHandlerFactoryMap[requestType];
			return handlerFn(requestDto);
		}

		private static Func<object, object> GetHandlerFn(Type requestType, Type serviceType)
		{
			var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
			var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

			var mi = serviceType.GetMethod("Execute", new[] { requestType });

			var serviceObj = Expression.New(serviceType);
			Expression callExecute = Expression.Call(serviceObj, mi,
				new[] { requestDtoStrong });

			var executeFunc = (Func<object, object>)Expression.Lambda(callExecute, requestDtoParam).Compile();

			return executeFunc;
		}
	}

}