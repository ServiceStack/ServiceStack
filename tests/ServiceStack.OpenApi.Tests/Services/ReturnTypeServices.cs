using System.Collections.Generic;
using ServiceStack;
using System.Linq;

namespace ServiceStack.OpenApi.Test.Services
{
    public class ReturnedDto
    {
        public virtual int Id { get; set; }
    }

    [Route("/return-list", "GET")]
    public class ReturnListRequest : IReturn<List<ReturnedDto>>, IGet { }

    [Route("/return-array", "GET")]
    public class ReturnArrayRequest : IReturn<ReturnedDto[]>, IGet { }

    [Route("/return-keyvaluepair", "GET")]
    public class ReturnKeyValuePairRequest : IReturn<KeyValuePair<string, string>>, IGet { }

    [Route("/return-dictionarystring", "GET")]
    public class ReturnDictionaryStringRequest : IReturn<Dictionary<string,string>>, IGet { }

    [Route("/return-dictionarydto", "GET")]
    public class ReturnDictionaryDtoRequest : IReturn<Dictionary<string, ReturnedDto>>, IGet { }

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

        public object Any(ReturnDictionaryStringRequest request) => new Dictionary<string, string>(){
            {"key1", "value1"},
            {"key2", "value2"}
            };

        public object Any(ReturnDictionaryDtoRequest request) => new Dictionary<string, ReturnedDto>(){
                {"key1", new ReturnedDto {Id = 1 } },
                {"key2", new ReturnedDto {Id = 2 } }
            };
    }
}