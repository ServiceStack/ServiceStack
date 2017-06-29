using System.Collections.Generic;
using ServiceStack;
using System.Linq;
using System.Net;

namespace ServiceStack.OpenApi.Test.Services
{
    public class ReturnedDto
    {
        public virtual int Id { get; set; }
    }

    [Route("/return-list", "GET")]
    public class ReturnListRequest : IReturn<List<ReturnedDto>>, IGet
    {
    }

    [Route("/return-array", "GET")]
    public class ReturnArrayRequest : IReturn<ReturnedDto[]>, IGet
    {
    }

    [Route("/return-keyvaluepair", "GET")]
    public class ReturnKeyValuePairRequest : IReturn<KeyValuePair<string, string>>, IGet
    {
    }

    [Route("/return-dictionarystring", "GET")]
    public class ReturnDictionaryStringRequest : IReturn<Dictionary<string, string>>, IGet
    {
    }

    [Route("/return-dictionarydto", "GET")]
    public class ReturnDictionaryDtoRequest : IReturn<Dictionary<string, ReturnedDto>>, IGet
    {
    }

    [Route("/return-ireturnvoid", "GET")]
    public class ReturnIReturnVoidDtoRequest : IReturn<IReturnVoid>, IGet
    {
    }


    [Route("/return-void", "GET")]
    public class ReturnVoidDtoRequest : IReturnVoid
    {
    }

    public class Return200Response
    {
        public string SuccessMessage { get; set; }
    }

    public class Return403Response
    {
        public string ForbiddenMessage { get; set; }
    }

    [Route("/return-annotated", "GET")]
    [ApiResponse(StatusCode = (int) HttpStatusCode.OK, Description = "All OK")]
    [ApiResponse(StatusCode = (int) HttpStatusCode.Forbidden, Description = "Forbidden Service",
        ResponseType = typeof(Return403Response))]
    [ApiResponse(Description = "Default Response", ResponseType = typeof(Return200Response), IsDefaultResponse = true)]
    public class ReturnAnnotatedDtoRequest : IReturn<Return200Response>, IGet
    {
        public int Code { get; set; }
    }

    [Route("/dhcp/servers/{ServerName}/scopes/{ScopeId}", "DELETE", Summary = "Deletes a DHCP scope.")]
    public class DeleteDhcpScope : IReturnVoid
    {
        [ApiMember(ParameterType = "path", IsRequired = true, Description = "The FQDN of the DHCP server")]
        public string ServerName { get; set; }

        [ApiMember(ParameterType = "path", Description = "The Scope Id of the DHCP scope")]
        public string ScopeId { get; set; }
    }

    public class ReturnGenericListServices : Service
    {
        public static readonly ReturnedDto[] returnedDtos = new ReturnedDto[]
        {
            new ReturnedDto() {Id = 1},
            new ReturnedDto() {Id = 2},
            new ReturnedDto() {Id = 3},
        };

        public object Any(ReturnListRequest request) => returnedDtos.ToList();
        public object Any(ReturnArrayRequest request) => returnedDtos;

        public object Any(ReturnKeyValuePairRequest request) => new KeyValuePair<string, string>("key1", "value1");

        public object Any(ReturnDictionaryStringRequest request) => new Dictionary<string, string>
        {
            {"key1", "value1"},
            {"key2", "value2"}
        };

        public object Any(ReturnDictionaryDtoRequest request) => new Dictionary<string, ReturnedDto>
        {
            {"key1", new ReturnedDto {Id = 1}},
            {"key2", new ReturnedDto {Id = 2}}
        };

        public void Any(ReturnVoidDtoRequest request)
        {
        }

        public void Any(ReturnIReturnVoidDtoRequest request)
        {
        }

        public void Any(DeleteDhcpScope request)
        {
        }

        public object Any(ReturnAnnotatedDtoRequest request)
        {
            switch (request.Code)
            {
                case 403:
                    return new Return403Response();
                default:
                    return new Return200Response();
            }
        }
    }
}