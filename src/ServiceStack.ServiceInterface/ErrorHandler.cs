using System;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
    public interface IErrorHandler
    {
        object HandleException<TRequest>(IAppHost appHost, TRequest request, Exception ex);
    }

    public class ErrorHandler : IErrorHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ErrorHandler));

        public static IErrorHandler Instance = new ErrorHandler();

        /// <summary>
        /// Service error logs are kept in 'urn:ServiceErrors:{ServiceName}'
        /// </summary>
        public const string UrnServiceErrorType = "ServiceErrors";

        /// <summary>
        /// Combined service error logs are maintained in 'urn:ServiceErrors:All'
        /// </summary>
        public const string CombinedServiceLogId = "All";
        
        public object HandleException<TRequest>(IAppHost appHost, TRequest request, Exception ex)
        {
            if (ex.InnerException != null && !(ex is IHttpError))
                ex = ex.InnerException;

            var responseStatus = ResponseStatusTranslator.Instance.Parse(ex);

            if (EndpointHost.UserConfig.DebugMode)
            {
                // View stack trace in tests and on the client
                responseStatus.StackTrace = GetRequestErrorBody(request) + ex;
            }

            Log.Error("ServiceBase<TRequest>::Service Exception", ex);

            if (appHost != null)
            {
                //If Redis is configured, maintain rolling service error logs in Redis (an in-memory datastore)
                var redisManager = appHost.TryResolve<IRedisClientsManager>();
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
                            var redisSeriviceErrorList = redis.Lists[UrnId.Create(UrnServiceErrorType, typeof(TRequest).Name)];
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
            }

            var errorResponse = DtoUtils.CreateErrorResponse(request, ex, responseStatus);

            return errorResponse;
        }

        /// <summary>
        /// Override to provide additional/less context about the Service Exception. 
        /// By default the request is serialized and appended to the ResponseStatus StackTrace.
        /// </summary>
        public virtual string GetRequestErrorBody(object request)
        {
            var requestString = "";
            try
            {
                requestString = TypeSerializer.SerializeToString(request);
            }
            catch /*(Exception ignoreSerializationException)*/
            {
                //Serializing request successfully is not critical and only provides added error info
            }

            return string.Format("[{0}: {1}]:\n[REQUEST: {2}]", GetType().Name, DateTime.UtcNow, requestString);
        }
    }
}