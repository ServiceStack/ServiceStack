using System.Collections.Generic;
using ServiceStack;

namespace Check.ServiceInterface
{
    public partial class ReturnedDto
    {
        public virtual int Id { get; set; }
    }

    [Route("/Request1", "GET")]
    public partial class GetRequest1 : IReturn<List<ReturnedDto>>, IGet { }

    [Route("/Request2", "GET")]
    public partial class GetRequest2 : IReturn<List<ReturnedDto>>, IGet { }

    public class ReturnGenericListServices : Service
    {
        public object Any(GetRequest1 request) => request;
        public object Any(GetRequest2 request) => request;
    }
}