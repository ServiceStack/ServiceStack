using System.Collections.Generic;
using ServiceStack;
using System.Linq;

namespace ServiceStack.OpenApi.Test.Services
{
    public partial class ReturnedDto
    {
        public virtual int Id { get; set; }
    }

    [Route("/return-list", "GET")]
    public partial class ReturnListRequest : IReturn<List<ReturnedDto>>, IGet { }

    [Route("/return-array", "GET")]
    public partial class ReturnArrayRequest : IReturn<ReturnedDto[]>, IGet { }

    [Route("/return-keyvaluepair", "GET")]
    public partial class ReturnKeyValuePairRequest : IReturn<KeyValuePair<string, string>>, IGet { }


    public partial class ReturnListRequest : IReturn<List<ReturnedDto>>, IGet { }


    public class ReturnGenericListServices : Service
    {
        public static readonly ReturnedDto[] returnedDtos = new ReturnedDto[] {
            new ReturnedDto() {Id = 1 },
            new ReturnedDto() {Id = 2 },
            new ReturnedDto() {Id = 3 },
        };

        public object Any(ReturnListRequest request) => returnedDtos.ToList();
        public object Any(ReturnArrayRequest request) => returnedDtos;

        public object Any(ReturnKeyValuePairRequest request) => new KeyValuePair<string, string>("key1", "value1");
    }
}