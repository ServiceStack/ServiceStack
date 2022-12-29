using System.Collections.Generic;
using ServiceStack;

namespace Check.ServiceInterface
{
    [Route("/Request1/", "GET")]
    public partial class GetRequest1 : IReturn<List<ReturnedDto>>, IGet { }

    [Route("/Request3", "GET")]
    public partial class GetRequest2 : IReturn<ReturnedDto>, IGet { }

    public partial class ReturnedDto
    {
        public virtual int Id { get; set; }
    }

    public class ReturnGenericListServices : Service
    {
        public object Any(GetRequest1 request) => request;
        public object Any(GetRequest2 request) => request;
    }
}