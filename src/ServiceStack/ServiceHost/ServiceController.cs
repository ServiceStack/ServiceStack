using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceHost
{
	public class ServiceController
		: IServiceController
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceController));
		private const string ResponseDtoSuffix = "Response";

		public ServiceController(Func<IEnumerable<Type>> resolveServicesFn)
		{
			this.RequestServiceTypeMap = new Dictionary<Type, Type>();
			this.ResponseServiceTypeMap = new Dictionary<Type, Type>();
			this.AllOperationTypes = new List<Type>();
			this.OperationTypes = new List<Type>();
			this.RequestTypeFactoryMap = new Dictionary<Type, Func<IHttpRequest, object>>();
			this.ServiceTypes = new HashSet<Type>();
			this.EnableAccessRestrictions = true;
			this.routes = new ServiceRoutes();
			this.ResolveServicesFn = resolveServicesFn;
		}

		readonly Dictionary<Type, Func<IRequestContext, object, object>> requestExecMap
			= new Dictionary<Type, Func<IRequestContext, object, object>>();

		readonly Dictionary<Type, ServiceAttribute> requestServiceAttrs
			= new Dictionary<Type, ServiceAttribute>();

		private readonly ServiceRoutes routes;

		public bool EnableAccessRestrictions { get; set; }

		public Dictionary<Type, Type> ResponseServiceTypeMap { get; set; }

		public Dictionary<Type, Type> RequestServiceTypeMap { get; set; }

		public IList<Type> AllOperationTypes { get; protected set; }

		public IList<Type> OperationTypes { get; protected set; }

		public Dictionary<Type, Func<IHttpRequest, object>> RequestTypeFactoryMap { get; set; }

		public HashSet<Type> ServiceTypes { get; protected set; }

		public string DefaultOperationsNamespace { get; set; }

		public IServiceRoutes Routes { get { return routes; } }

		public Func<IEnumerable<Type>> ResolveServicesFn { get; set; }

		public void Register<TReq>(Func<IService<TReq>> invoker)
		{
			var requestType = typeof(TReq);
			Func<IRequestContext, object, object> handlerFn = (requestContext, dto) =>
			{
				var service = invoker();

				InjectRequestContext(service, requestContext);

				return ServiceExec<TReq>.Execute(
					service, (TReq)dto,
					requestContext != null ? requestContext.EndpointAttributes : EndpointAttributes.None);
			};

			requestExecMap.Add(requestType, handlerFn);
		}

		public void Register(ITypeFactory serviceFactoryFn)
		{
			foreach (var serviceType in ResolveServicesFn())
			{
				RegisterService(serviceFactoryFn, serviceType);
			}
		}

		public void RegisterService(ITypeFactory serviceFactoryFn, Type serviceType)
		{
			if (serviceType.IsAbstract || serviceType.ContainsGenericParameters) return;

			foreach (var service in serviceType.GetInterfaces())
			{
				if (!service.IsGenericType
				    || service.GetGenericTypeDefinition() != typeof (IService<>)
				) continue;

				var requestType = service.GetGenericArguments()[0];

				Register(requestType, serviceType, serviceFactoryFn);

				RegisterRestPaths(requestType);

				this.ServiceTypes.Add(serviceType);

				this.RequestServiceTypeMap[requestType] = serviceType;
				this.AllOperationTypes.Add(requestType);
				this.OperationTypes.Add(requestType);

				var responseTypeName = requestType.FullName + ResponseDtoSuffix;
				var responseType = AssemblyUtils.FindType(responseTypeName);
				if (responseType != null)
				{
					this.ResponseServiceTypeMap[responseType] = serviceType;
					this.AllOperationTypes.Add(responseType);
					this.OperationTypes.Add(responseType);
				}

				if (typeof(IRequiresRequestStream).IsAssignableFrom(requestType))
				{
					this.RequestTypeFactoryMap[requestType] = httpReq => {
						var rawReq = (IRequiresRequestStream) requestType.CreateInstance();
						rawReq.RequestStream = httpReq.InputStream;
						return rawReq;
					};
				}

				Log.DebugFormat("Registering {0} service '{1}' with request '{2}'",
					(responseType != null ? "SyncReply" : "OneWay"), 
					serviceType.Name, requestType.Name);
			}
		}

		public readonly Dictionary<string, List<RestPath>> RestPathMap = new Dictionary<string, List<RestPath>>();

		public void RegisterRestPaths(Type requestType)
		{
			var attrs = requestType.GetCustomAttributes(typeof(RouteAttribute), true);
			foreach (RouteAttribute attr in attrs)
			{
				var restPath = new RestPath(requestType, attr.Path, attr.Verbs);
				if (!restPath.IsValid)
					throw new NotSupportedException(string.Format(
						"RestPath '{0}' on Type '{1}' is not Valid", attr.Path, requestType.Name));

				RegisterRestPath(restPath);
			}
		}

		public void RegisterRestPath(RestPath restPath)
		{
			List<RestPath> pathsAtFirstMatch;
			if (!RestPathMap.TryGetValue(restPath.FirstMatchHashKey, out pathsAtFirstMatch))
			{
				pathsAtFirstMatch = new List<RestPath>();
				RestPathMap[restPath.FirstMatchHashKey] = pathsAtFirstMatch;
			}
			pathsAtFirstMatch.Add(restPath);
		}

		public void AfterInit()
		{
			foreach (var restPath in this.routes.RestPaths)
			{
				RegisterRestPath(restPath);
			}
		}

		public IRestPath GetRestPathForRequest(string httpMethod, string pathInfo)
		{
			var matchUsingPathParts = RestPath.GetPathPartsForMatching(pathInfo);

			List<RestPath> firstMatches;

			var yieldedHashMatches = RestPath.GetFirstMatchHashKeys(matchUsingPathParts);
			foreach (var potentialHashMatch in yieldedHashMatches)
			{
				if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

				foreach (var restPath in firstMatches)
				{
					if (restPath.IsMatch(httpMethod, matchUsingPathParts)) return restPath;
				}
			}

			var yieldedWildcardMatches = RestPath.GetFirstMatchWildCardHashKeys(matchUsingPathParts);
			foreach (var potentialHashMatch in yieldedWildcardMatches)
			{
				if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

				foreach (var restPath in firstMatches)
				{
					if (restPath.IsMatch(httpMethod, matchUsingPathParts)) return restPath;
				}
			}

			return null;
		}

		internal class TypeFactoryWrapper : ITypeFactory
		{
			private readonly Func<Type, object> typeCreator;

			public TypeFactoryWrapper(Func<Type, object> typeCreator)
			{
				this.typeCreator = typeCreator;
			}

			public object CreateInstance(Type type)
			{
				return typeCreator(type);
			}
		}

		public void Register(Type requestType, Type serviceType)
		{
			var handlerFactoryFn = Expression.Lambda<Func<Type, object>>
				(
					Expression.New(serviceType),
					Expression.Parameter(typeof(Type), "serviceType")
				).Compile();

			Register(requestType, serviceType, new TypeFactoryWrapper(handlerFactoryFn));
		}

		public void Register(Type requestType, Type serviceType, Func<Type, object> handlerFactoryFn)
		{
			Register(requestType, serviceType, new TypeFactoryWrapper(handlerFactoryFn));
		}

		public void Register(Type requestType, Type serviceType, ITypeFactory serviceFactoryFn)
		{
			var typeFactoryFn = CallServiceExecuteGeneric(requestType, serviceType);

			Func<IRequestContext, object, object> handlerFn = (requestContext, dto) =>
			{
				var service = serviceFactoryFn.CreateInstance(serviceType);
                try
                {
                    InjectRequestContext(service, requestContext);

                    var endpointAttrs = requestContext != null
                        ? requestContext.EndpointAttributes
                        : EndpointAttributes.None;

                    try
                    {
                        //Executes the service and returns the result
                        var response = typeFactoryFn(dto, service, endpointAttrs);
                        return response;
                    }
                    finally
                    {
                        if (EndpointHost.AppHost != null)
                        {
                            //Gets disposed by AppHost or ContainerAdapter if set
                            EndpointHost.AppHost.Release(service);
                        }
                        else
                        {
                            using (service as IDisposable) { }
                        }
                    } 
                }
                catch (TargetInvocationException tex)
                {
                    //Mono invokes using reflection
                    throw tex.InnerException ?? tex;
                }
            };

			try
			{
				requestExecMap.Add(requestType, handlerFn);
			}
			catch (ArgumentException)
			{
				throw new AmbiguousMatchException(
					string.Format("Could not register the service '{0}' as another service with the definition of type 'IService<{1}>' already exists.",
					serviceType.FullName, requestType.Name));
			}

			var serviceAttrs = requestType.GetCustomAttributes(typeof(ServiceAttribute), false);
			if (serviceAttrs.Length > 0)
			{
				requestServiceAttrs.Add(requestType, (ServiceAttribute)serviceAttrs[0]);
			}
		}

		private static void InjectRequestContext(object service, IRequestContext requestContext)
		{
			if (requestContext == null) return;

			var serviceRequiresContext = service as IRequiresRequestContext;
			if (serviceRequiresContext != null)
			{
				serviceRequiresContext.RequestContext = requestContext;
			}

			var servicesRequiresHttpRequest = service as IRequiresHttpRequest;
			if (servicesRequiresHttpRequest != null)
				servicesRequiresHttpRequest.HttpRequest = requestContext.Get<IHttpRequest>();
		}

		private static Func<object, object, EndpointAttributes, object> CallServiceExecuteGeneric(
			Type requestType, Type serviceType)
		{
			try
			{
				var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
				var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

				var serviceParam = Expression.Parameter(typeof(object), "serviceObj");
				var serviceStrong = Expression.Convert(serviceParam, serviceType);

				var attrsParam = Expression.Parameter(typeof(EndpointAttributes), "attrs");

				var mi = ServiceExec.GetExecMethodInfo(serviceType, requestType);

				Expression callExecute = Expression.Call(
					mi, new Expression[] { serviceStrong, requestDtoStrong, attrsParam });

				var executeFunc = Expression.Lambda<Func<object, object, EndpointAttributes, object>>
					(callExecute, requestDtoParam, serviceParam, attrsParam).Compile();

				return executeFunc;

			}
			catch (Exception)
			{
				//problems with MONO, using reflection for temp fix
				return delegate(object request, object service, EndpointAttributes attrs)
				{
					var mi = ServiceExec.GetExecMethodInfo(serviceType, requestType);
					return mi.Invoke(null, new[] { service, request, attrs });
				};
			}
		}

		public object Execute(object dto)
		{
			return Execute(dto, null);
		}

		public object Execute(object request, IRequestContext requestContext)
		{
			var requestType = request.GetType();

			if (EnableAccessRestrictions)
			{
				AssertServiceRestrictions(requestType,
					requestContext != null ? requestContext.EndpointAttributes : EndpointAttributes.None);
			}

			var handlerFn = GetService(requestType);
			return handlerFn(requestContext, request);
		}

		public Func<IRequestContext, object, object> GetService(Type requestType)
		{
			Func<IRequestContext, object, object> handlerFn;
			if (!requestExecMap.TryGetValue(requestType, out handlerFn))
			{
				throw new NotImplementedException(
						string.Format("Unable to resolve service '{0}'", requestType.Name));
			}

			return handlerFn;
		}

		public object ExecuteText(string requestXml, Type requestType, IRequestContext requestContext)
		{
			var request = DataContractDeserializer.Instance.Parse(requestXml, requestType);
			var response = Execute(request, requestContext);
			var responseXml = DataContractSerializer.Instance.Parse(response);
			return responseXml;
		}

		public void AssertServiceRestrictions(Type requestType, EndpointAttributes actualAttributes)
		{
			ServiceAttribute serviceAttr;
			var hasNoAccessRestrictions = !requestServiceAttrs.TryGetValue(requestType, out serviceAttr)
				|| serviceAttr.HasNoAccessRestrictions;

			if (hasNoAccessRestrictions)
			{
				return;
			}

			var failedScenarios = new StringBuilder();
			foreach (var requiredScenario in serviceAttr.RestrictAccessToScenarios)
			{
				var allServiceRestrictionsMet = (requiredScenario & actualAttributes) == actualAttributes;
				if (allServiceRestrictionsMet)
				{
					return;
				}

				var passed = requiredScenario & actualAttributes;
				var failed = requiredScenario & ~(passed);

				failedScenarios.AppendFormat("\n -[{0}]", failed);
			}

			var internalDebugMsg = (EndpointAttributes.InternalNetworkAccess & actualAttributes) != 0
				? "\n Unauthorized call was made from: " + actualAttributes
				: "";

			throw new UnauthorizedAccessException(
				string.Format("Could not execute service '{0}', The following restrictions were not met: '{1}'" + internalDebugMsg,
					requestType.Name, failedScenarios));
		}
	}

}