using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class AutoQueryDataServiceSource
    {
        public static QueryDataSource<T> ServiceSource<T>(this QueryDataContext ctx, object requestDto, ICacheClient cache, TimeSpan? expiresIn=null, string cacheKey=null)
        {
            if (cacheKey == null)
                cacheKey = "aqd:" + requestDto.ToGetUrl();

            var cachedResults = cache.Get<List<T>>(cacheKey);
            if (cachedResults != null)
                return new MemoryDataSource<T>(ctx, cachedResults);

            var response = ServiceSource<T>(ctx, requestDto);
            return response.CacheMemorySource(cache, cacheKey, expiresIn);
        }

        internal static QueryDataSource<T> CacheMemorySource<T>(this MemoryDataSource<T> response, ICacheClient cache, string cacheKey, TimeSpan? expiresIn)
        {
            if (expiresIn != null)
                cache.Set(cacheKey, response.Data, expiresIn.Value);
            else
                cache.Set(cacheKey, response.Data);

            return response;
        }

        public static MemoryDataSource<T> ServiceSource<T>(this QueryDataContext ctx, object requestDto)
        {
            var response = HostContext.AppHost.GetServiceGateway(ctx.Request).Send<object>(requestDto);
            var results = GetResults<T>(response);
            if (results == null)
                throw new NotSupportedException(
                    $"IEnumerable<{typeof(T).Name}> could not be derived from Response {response.GetType().Name} from Request {requestDto.GetType().Name}");

            return new MemoryDataSource<T>(ctx, results);
        }

        public static IEnumerable<T> GetResults<T>(object response)
        {
            if (response is Task task)
                response = task.GetResult();

            if (response is IHttpResult httpResult)
                response = httpResult.Response;

            if (response is IEnumerable<T> result)
                return result;

            foreach (var pi in response.GetType().GetPublicProperties())
            {
                if (typeof(IEnumerable<T>).IsAssignableFrom(pi.PropertyType))
                {
                    return (IEnumerable<T>)pi.GetGetMethod().Invoke(response, TypeConstants.EmptyObjectArray);
                }
            }

            return null;
        }

        public static List<object> GetResults(object response)
        {
            if (response is Task task)
                response = task.GetResult();

            if (response is IHttpResult httpResult)
                response = httpResult.Response;

            if (response is IEnumerable result)
                return result.Map(x => x);

            foreach (var pi in response.GetType().GetPublicProperties())
            {
                if (typeof(IEnumerable).IsAssignableFrom(pi.PropertyType))
                {
                    return ((IEnumerable)pi.GetGetMethod().Invoke(response, TypeConstants.EmptyObjectArray)).Map(x => x);
                }
            }

            return null;
        }
    }
}