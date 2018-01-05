using ServiceStack;

namespace Check.ServiceModel
{
    public class CustomHttpError
    {
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; }
    }
    public class CustomHttpErrorResponse
    {
        public string Custom { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CustomFieldHttpError { }
    public class CustomFieldHttpErrorResponse
    {
        public string Custom { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/alwaysthrows")]
    public class AlwaysThrows : IReturn<AlwaysThrows> { }

    [Route("/alwaysthrowsfilterattribute")]
    public class AlwaysThrowsFilterAttribute : IReturn<AlwaysThrowsFilterAttribute> { }

    [Route("/alwaysthrowsglobalfilter")]
    public class AlwaysThrowsGlobalFilter : IReturn<AlwaysThrowsGlobalFilter> { }
}