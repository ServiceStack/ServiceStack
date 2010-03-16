using System;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{

	/// <summary>
	/// A Useful ServiceBase for all services with support for automatically serializing
	/// Exceptions into a common ResponseError DTO so errors can be handled generically by clients. 
	/// 
	/// If an 'IRedisClientsManager' is configured in your AppHost, service errors will
	/// also be maintained into a service specific and combined rolling error log.
	/// </summary>
	/// <typeparam name="TRequest"></typeparam>
	public abstract class ServiceBase<TRequest>
		: IService<TRequest>
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceBase<>));

		/// <summary>
		/// Naming convention for the request's Response DTO
		/// </summary>
		public const string ResponseDtoSuffix = "Response";

		/// <summary>
		/// Naming convention for the ResponseStatus property name on the response DTO
		/// </summary>
		public const string ResponseStatusPropertyName = "ResponseStatus";

		/// <summary>
		/// Service error logs are kept in 'urn:ServiceErrors:{ServiceName}'
		/// </summary>
		public const string UrnServiceErrorType = "ServiceErrors";

		/// <summary>
		/// Combined service error logs are maintained in 'urn:ServiceErrors:All'
		/// </summary>
		public const string CombinedServiceLogId = "All";


		/// <summary>
		/// Can be overriden to supply Custom 'ServiceName' error logs
		/// </summary>
		public virtual string ServiceName
		{
			get { return typeof(TRequest).Name; }
		}

		/// <summary>
		/// Maintains the current request DTO in this property
		/// </summary>
		protected TRequest CurrentRequestDto;

		/// <summary>
		/// Override to provide additional context about the Service Exception. 
		/// By default the request is serialized and appended to the ResponseStatus StackTrace.
		/// </summary>
		public virtual string GetRequestErrorBody()
		{
			var requestString = "";
			try
			{
				requestString = TypeSerializer.SerializeToString(CurrentRequestDto);
			}
			catch (Exception ignoreSerializationException)
			{
				//Serializing request successfully is not critical and only provides added error info
			}

			return string.Format("[{0}: {1}]:\n[REQUEST: {2}]", GetType().Name, DateTime.UtcNow, requestString);
		}

		/// <summary>
		/// Single method sub classes should implement to execute the request
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		protected abstract object Run(TRequest request);

		/// <summary>
		/// Execute the request with the protected abstract Run() method in a managed scope by
		/// provide default handling of Service Exceptions by serializing exceptions in the response
		/// DTO and maintaining all service errors in a managed service-specific and combined rolling error logs
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public object Execute(TRequest request)
		{
			try
			{
				//Run the request in a managed scope serializing all 
				return Run(request);
			}
			catch (Exception ex)
			{
				var responseStatus = ResponseStatusTranslator.Instance.Parse(ex);

				// View stack trace in tests and on the client
				responseStatus.StackTrace = GetRequestErrorBody() + ex;

				Log.Error("ServiceBase<TRequest>::Service Exception", ex);

				//If Redis is configured, maintain rolling service error logs in Redis (an in-memory datastore)
				var redisManager = AppHostBase.Instance.Container.TryResolve<IRedisClientsManager>();
				if (redisManager != null)
				{
					try
					{
						//Get a thread-safe redis client from the client manager pool
						using (var client = redisManager.GetClient())
						{
							//Get a client with a native interface for storing 'ResponseStatus' objects
							var redis = client.GetTypedClient<ResponseStatus>();

							//Store the errors in predictable Redis-named lists i.e. 
							//'urn:ServiceErrors:{ServiceName}' and 'urn:ServiceErrors:All' 
							var redisSeriviceErrorList = redis.Lists[UrnId.Create(UrnServiceErrorType, ServiceName)];
							var redisCombinedErrorList = redis.Lists[UrnId.Create(UrnServiceErrorType, CombinedServiceLogId)];

							//Append the error at the start of the service-specific and combined error logs.
							redisSeriviceErrorList.Prepend(responseStatus);
							redisCombinedErrorList.Prepend(responseStatus);

							//Clip old error logs from the managed logs
							const int rollingErrorCount = 1000;
							redisSeriviceErrorList.Trim(0, rollingErrorCount);
							redisCombinedErrorList.Trim(0, rollingErrorCount);
						}
					}
					catch (Exception suppressRedisException)
					{
						Log.Error("Could not append exception to redis service error logs", suppressRedisException);
					}
				}

				var responseDto = CreateResponseDto(request, responseStatus);

				if (responseDto == null)
				{
					throw;
				}

				return responseDto;
			}
		}

		/// <summary>
		/// Create an instance of the service response dto type and inject it with the supplied responseStatus
		/// </summary>
		/// <param name="request"></param>
		/// <param name="responseStatus"></param>
		/// <returns></returns>
		protected static object CreateResponseDto(TRequest request, ResponseStatus responseStatus)
		{
			// Predict the Response message type name
			var responseDtoName = request.GetType().FullName + ResponseDtoSuffix;

			// Get the type
			var responseDtoType = AssemblyUtils.FindType(responseDtoName);

			if (responseDtoType == null)
			{
				// We don't support creation of response messages without a predictable type name
				return null;
			}

			// Create an instance of the response message for this request
			var responseDto = ReflectionUtils.CreateInstance(responseDtoType);

			// For faster serialization of exceptions, services should implement IHasResponseStatus
			var hasResponseStatus = responseDto as IHasResponseStatus;
			if (hasResponseStatus != null)
			{
				hasResponseStatus.ResponseStatus = responseStatus;
			}
			else
			{
				// Get the ResponseStatus property
				var responseStatusProperty = responseDtoType.GetProperty(ResponseStatusPropertyName);

				if (responseStatusProperty == null)
				{
					// If it doesn't exist we can't support it
					return null;
				}

				// Set the ResponseStatus
				ReflectionUtils.SetProperty(responseDto, responseStatusProperty, responseStatus);
			}

			// Return an Error DTO with the exception populated
			return responseDto;
		}
	}

}
