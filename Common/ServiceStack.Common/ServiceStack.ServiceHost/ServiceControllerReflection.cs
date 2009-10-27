using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Keeping around just to compare how slow it is
	/// </summary>
	public class ServiceControllerReflection
	{
		private readonly Dictionary<Type, Delegate> handlerMap = new Dictionary<Type, Delegate>();

		public void Register<TServiceRequest>(Func<IService<TServiceRequest>> invoker)
		{
			var requestType = typeof(TServiceRequest);
			handlerMap.Add(requestType, invoker);
		}

		public object ExecuteReflection(object dto)
		{
			var requestType = dto.GetType();
			var oServiceFn = handlerMap[requestType];
			return ExecuteService(dto, requestType, oServiceFn);
		}

		private static object ExecuteService(object requestDto, Type requestType, Delegate factoryDelegate)
		{
			object service = factoryDelegate.Method.Invoke(factoryDelegate, new object[0]);

			var mi = service.GetType().GetMethod("Execute", new[] { requestType });
			var result = mi.Invoke(service, new[] { requestDto });
			return result;
		}
	}
}