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


    [Route("/timestamp", Verbs = "GET")]
    public class GetTimestamp : IReturn<TimestampData>
    {
    }

    public class TimestampData
    {
        public long Timestamp { get; set; }
    }

    public class TimestampService : Service
    {
        public object Get(GetTimestamp request)
        {
            return new TimestampData { Timestamp = 635980054734850470 };
        }
    }
}