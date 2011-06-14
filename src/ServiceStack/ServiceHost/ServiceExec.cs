using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack.ServiceHost
{
	public class ServiceExec
	{
		public const string Execute = "Execute";
		public const string ExecuteAsync = "ExecuteAsync";

		public const string RestGet = "Get";
		public const string RestPost = "Post";
		public const string RestPut = "Put";
		public const string RestDelete = "Delete";
		public const string RestPatch = "Patch";


		private static readonly Dictionary<Type, MethodInfo> ServiceExecCache = new Dictionary<Type, MethodInfo>();

		public static MethodInfo GetExecMethodInfo(Type serviceType, Type requestType)
		{
			MethodInfo mi;
			lock (ServiceExecCache)
			{
				if (!ServiceExecCache.TryGetValue(requestType /*serviceType */, out mi))
				{
					var genericType = typeof(ServiceExec<>).MakeGenericType(requestType);
					
					mi = genericType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);

					ServiceExecCache.Add(requestType /* serviceType */, mi);
				}
			}

			return mi;
		}

		public static MethodInfo GetRunTimeExecMethod(Type serviceType, Type requestType, EndpointAttributes attrs)
		{
			if ((attrs & EndpointAttributes.AsyncOneWay) == EndpointAttributes.AsyncOneWay)
			{
				var mi = serviceType.GetMethod(ExecuteAsync, new[] { requestType });
				if (mi != null) return mi;
			}
			else if ((attrs & EndpointAttributes.HttpGet) == EndpointAttributes.HttpGet)
			{
				var mi = serviceType.GetMethod(RestGet, new[] { requestType });
				if (mi != null) return mi;
			}
			else if ((attrs & EndpointAttributes.HttpPost) == EndpointAttributes.HttpPost)
			{
				var mi = serviceType.GetMethod(RestPost, new[] { requestType });
				if (mi != null) return mi;
			}
			else if ((attrs & EndpointAttributes.HttpPut) == EndpointAttributes.HttpPut)
			{
				var mi = serviceType.GetMethod(RestPut, new[] { requestType });
				if (mi != null) return mi;
			}
			else if ((attrs & EndpointAttributes.HttpDelete) == EndpointAttributes.HttpDelete)
			{
				var mi = serviceType.GetMethod(RestDelete, new[] { requestType });
				if (mi != null) return mi;
			}
			else if ((attrs & EndpointAttributes.HttpPatch) == EndpointAttributes.HttpPatch)
			{
				var mi = serviceType.GetMethod(RestPatch, new[] { requestType });
				if (mi != null) return mi;
			}
			return serviceType.GetMethod(Execute, new[] { requestType });
		}
	}

	public class ServiceExec<TReq>
	{
		public const string ExecuteMethodName = "Execute";

		public static object Execute(IService<TReq> service, TReq request, EndpointAttributes attrs)
		{
			if ((attrs & EndpointAttributes.AsyncOneWay) == EndpointAttributes.AsyncOneWay)
			{
				var asyncService = service as IAsyncService<TReq>;
				if (asyncService != null) return asyncService.ExecuteAsync(request);
			}
			else if ((attrs & EndpointAttributes.HttpGet) == EndpointAttributes.HttpGet)
			{
				var restService = service as IRestGetService<TReq>;
				if (restService != null) return restService.Get(request);
			}
			else if ((attrs & EndpointAttributes.HttpPost) == EndpointAttributes.HttpPost)
			{
				var restService = service as IRestPostService<TReq>;
				if (restService != null) return restService.Post(request);
			}
			else if ((attrs & EndpointAttributes.HttpPut) == EndpointAttributes.HttpPut)
			{
				var restService = service as IRestPutService<TReq>;
				if (restService != null) return restService.Put(request);
			}
			else if ((attrs & EndpointAttributes.HttpDelete) == EndpointAttributes.HttpDelete)
			{
				var restService = service as IRestDeleteService<TReq>;
				if (restService != null) return restService.Delete(request);
			}
			else if ((attrs & EndpointAttributes.HttpPatch) == EndpointAttributes.HttpPatch)
			{
				var restService = service as IRestPatchService<TReq>;
				if (restService != null) return restService.Patch(request);
			}
			return service.Execute(request);
		}
	}
}