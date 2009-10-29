using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceHost
{
	public class ServiceController
	{
		readonly Dictionary<Type, Func<object, object>> requestExecMap = new Dictionary<Type, Func<object, object>>();

		public void Register(Assembly assemblyWithServices)
		{
		}

		public void Register<TServiceRequest>(Func<IService<TServiceRequest>> invoker)
		{
			var requestType = typeof(TServiceRequest);

			Func<object, object> handlerFn = dto => {
				var service = invoker();
				return service.Execute((TServiceRequest)dto);
			};

			requestExecMap.Add(requestType, handlerFn);
		}

		public void Register(Type requestType, Type serviceType)
		{
			requestExecMap.Add(requestType, GetDefaultConstructorHandlerFn(requestType, serviceType));
		}

		public void Register(Type requestType, Type serviceType, Func<Type, object> handlerFactoryFn)
		{
			Func<object, object> handlerFn = dto => handlerFactoryFn(serviceType);
			requestExecMap.Add(requestType, handlerFn);
		}

		public void Register(Type requestType, Type serviceType, ITypeFactory handlerFactoryFn)
		{
			var typeFactoryFn = CallServiceExecuteGeneric(requestType, serviceType);

			Func<object, object> handlerFn = dto => {
				var service = handlerFactoryFn.CreateInstance(serviceType);
				return typeFactoryFn(dto, service);
			};
			requestExecMap.Add(requestType, handlerFn);
		}

		public object Execute(object dto)
		{
			return ExecuteService(dto, dto.GetType());
		}

		private object ExecuteService(object requestDto, Type requestType)
		{
			var handlerFn = requestExecMap[requestType];
			return handlerFn(requestDto);
		}

		private static Func<object, object, object> CallServiceExecuteGeneric(Type requestType, Type serviceType)
		{
			var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
			var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

			var serviceParam = Expression.Parameter(typeof(object), "serviceObj");
			var serviceStrong = Expression.Convert(serviceParam, serviceType);

			var mi = serviceType.GetMethod("Execute", new[] { requestType });

			Expression callExecute = Expression.Call(serviceStrong, mi, new[] { requestDtoStrong });

			var executeFunc = Expression.Lambda<Func<object, object, object>>
				(callExecute, requestDtoParam, serviceParam).Compile();

			return executeFunc;
		}

		private static Func<object, object> GetDefaultConstructorHandlerFn(Type requestType, Type serviceType)
		{
			var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
			var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

			var mi = serviceType.GetMethod("Execute", new[] { requestType });

			var serviceObj = Expression.New(serviceType);
			Expression callExecute = Expression.Call(serviceObj, mi,
				new[] { requestDtoStrong });

			var executeFunc = Expression.Lambda<Func<object, object>>(callExecute, requestDtoParam).Compile();

			return executeFunc;
		}
	}

}