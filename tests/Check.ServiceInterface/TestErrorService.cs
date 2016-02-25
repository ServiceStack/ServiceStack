using ServiceStack;

namespace Check.ServiceInterface
{
    [Route("/test/errorview")]
    public class TestErrorView
    {
        public string Id { get; set; }
    }

    public class TestErrorService : Service
    {
        public object Any(TestErrorView request)
        {
            return request;
        }
    }
}