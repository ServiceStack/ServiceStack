// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace ServiceStack
{
    public class RedisErrorLoggerFeature : IPlugin
    {
        public static ILog Log = LogManager.GetLogger(typeof(RedisErrorLoggerFeature));

        public IRedisClientsManager redisManager;

        public RedisErrorLoggerFeature(IRedisClientsManager redisManager)
        {
            this.redisManager = redisManager ?? throw new ArgumentNullException(nameof(redisManager));
        }

        public void Register(IAppHost appHost)
        {
            appHost.ServiceExceptionHandlers.Add(HandleServiceException);
            appHost.UncaughtExceptionHandlers.Add(HandleUncaughtException);
        }

        /// <summary>
        /// Service error logs are kept in 'urn:ServiceErrors:{ServiceName}'
        /// </summary>
        public const string UrnServiceErrorType = "ServiceErrors";

        /// <summary>
        /// Combined service error logs are maintained in 'urn:ServiceErrors:All'
        /// </summary>
        public const string CombinedServiceLogId = "All";

        public void HandleUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            LogErrorInRedis(operationName, ex);
        }

        public object HandleServiceException(IRequest httpReq, object request, Exception ex)
        {
            LogErrorInRedis(httpReq.OperationName, ex);

            return null;
        }

        private void LogErrorInRedis(string operationName, Exception ex)
        {
            try
            {
                //Get a thread-safe redis client from the client manager pool
                using (var client = redisManager.GetClient())
                {
                    //Get a client with a native interface for storing 'ResponseStatus' objects
                    var redis = client.As<ResponseStatus>();

                    //Store the errors in predictable Redis-named lists i.e. 
                    //'urn:ServiceErrors:{ServiceName}' and 'urn:ServiceErrors:All' 
                    var redisSeriviceErrorList = redis.Lists[UrnId.Create(UrnServiceErrorType, operationName)];
                    var redisCombinedErrorList = redis.Lists[UrnId.Create(UrnServiceErrorType, CombinedServiceLogId)];

                    //Append the error at the start of the service-specific and combined error logs.
                    var responseStatus = ex.ToResponseStatus();
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
}