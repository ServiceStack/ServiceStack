using System.Linq;
using ServiceStack.Metadata;
using ServiceStack.MiniProfiler;
using ServiceStack.Support.WebHost;
using ServiceStack.Web;

namespace ServiceStack
{
    public abstract partial class ServiceStackHost
    {
        /// <summary>
        /// Applies the raw request filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public bool ApplyPreRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes)
        {
            foreach (var requestFilter in PreRequestFilters)
            {
                requestFilter(httpReq, httpRes);
                if (httpRes.IsClosed) break;
            }

            return httpRes.IsClosed;
        }

        /// <summary>
        /// Applies the request filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public bool ApplyRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes, object requestDto)
        {
            httpReq.ThrowIfNull("httpReq");
            httpRes.ThrowIfNull("httpRes");

            using (Profiler.Current.Step("Executing Request Filters"))
            {
                //Exec all RequestFilter attributes with Priority < 0
                var attributes = FilterAttributeCache.GetRequestFilterAttributes(requestDto.GetType());
                var i = 0;
                for (; i < attributes.Length && attributes[i].Priority < 0; i++)
                {
                    var attribute = attributes[i];
                    ServiceManager.Container.AutoWire(attribute);
                    attribute.RequestFilter(httpReq, httpRes, requestDto);
                    Release(attribute);
                    if (httpRes.IsClosed) return httpRes.IsClosed;
                }

                //Exec global filters
                foreach (var requestFilter in GlobalRequestFilters)
                {
                    requestFilter(httpReq, httpRes, requestDto);
                    if (httpRes.IsClosed) return httpRes.IsClosed;
                }

                //Exec remaining RequestFilter attributes with Priority >= 0
                for (; i < attributes.Length; i++)
                {
                    var attribute = attributes[i];
                    ServiceManager.Container.AutoWire(attribute);
                    attribute.RequestFilter(httpReq, httpRes, requestDto);
                    Release(attribute);
                    if (httpRes.IsClosed) return httpRes.IsClosed;
                }

                return httpRes.IsClosed;
            }
        }

        /// <summary>
        /// Applies the response filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public bool ApplyResponseFilters(IHttpRequest httpReq, IHttpResponse httpRes, object response)
        {
            httpReq.ThrowIfNull("httpReq");
            httpRes.ThrowIfNull("httpRes");

            using (Profiler.Current.Step("Executing Response Filters"))
            {
                var responseDto = response.GetResponseDto();
                var attributes = responseDto != null
                    ? FilterAttributeCache.GetResponseFilterAttributes(responseDto.GetType())
                    : null;

                //Exec all ResponseFilter attributes with Priority < 0
                var i = 0;
                if (attributes != null)
                {
                    for (; i < attributes.Length && attributes[i].Priority < 0; i++)
                    {
                        var attribute = attributes[i];
                        ServiceManager.Container.AutoWire(attribute);
                        attribute.ResponseFilter(httpReq, httpRes, response);
                        Release(attribute);
                        if (httpRes.IsClosed) return httpRes.IsClosed;
                    }
                }

                //Exec global filters
                foreach (var responseFilter in GlobalResponseFilters)
                {
                    responseFilter(httpReq, httpRes, response);
                    if (httpRes.IsClosed) return httpRes.IsClosed;
                }

                //Exec remaining RequestFilter attributes with Priority >= 0
                if (attributes != null)
                {
                    for (; i < attributes.Length; i++)
                    {
                        var attribute = attributes[i];
                        ServiceManager.Container.AutoWire(attribute);
                        attribute.ResponseFilter(httpReq, httpRes, response);
                        Release(attribute);
                        if (httpRes.IsClosed) return httpRes.IsClosed;
                    }
                }

                return httpRes.IsClosed;
            }
        }

        public MetadataPagesConfig MetadataPagesConfig
        {
            get
            {
                return new MetadataPagesConfig(
                    Metadata,
                    Config.ServiceEndpointsMetadataConfig,
                    Config.IgnoreFormatsInMetadata,
                    ContentTypes.ContentTypeFormats.Keys.ToList());
            }
        }
    }
}