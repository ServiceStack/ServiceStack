using System.Net;
using System.Runtime.Serialization;

namespace ServiceStack.OpenApi.Tests.Services
{

    public class HelloDto
    {
        [ApiMember(IsRequired = true)]
        public string Name { get; set; }
    }

    [Api(Description = "Create new hello", BodyParameter = GenerateBodyParameter.Always, IsRequired = true)]
    [Route("/annotated-hello", "POST", Summary = "Creates a new hello.")]
    public class CreateHelloReq : IReturn<Hello>
    {
        [ApiMember(IsRequired = true, ParameterType = "model")]
        public HelloDto Hello { get; set; }
    }

    [Api("Description of the response")]
    public class GatewayCredentialResponse
    {
        public string Result { get; set; }
    }

    [Route("/gatewaycredential/{MID}", "POST, OPTIONS")]
    [DataContract]
    public class GatewayCredentialRequest : IReturn<GatewayCredentialResponse>
    {
        [ApiMember(IsRequired = true, ExcludeInSchema = true, ParameterType = "path")]
        [DataMember]
        public string MID { get; set; }

        [ApiMember(IsRequired = true, ParameterType = "model")]
        [DataMember]
        public string UserName { get; set; }

        [ApiMember(IsRequired = true, ParameterType = "model")]
        [ApiAllowableValues("Type", Values = new string[] { "Merchant", "API" })]
        [DataMember]
        public string Type { get; set; }
    }



    [Api("Gets the movie")]
    [Route("/movie/{Id}")]
    public class GetMovie : IReturn<MovieResponse>
    {
        [ApiMember(IsRequired = true, Description = "Required ID of Movie.", DataType = "integer", ParameterType = "path")]
        public long Id { get; set; }

        [ApiMember(IsRequired = false, AllowMultiple = true, Description = "List of additional objects to include in the movie response.")]
        [ApiAllowableValues("Includes", Values = new string[] { "Genres", "Releases", "Contributors", "AlternateTitles", "Descriptions", "Companies", "Tags", "Images", "Videos" })]   // This breaks the swagger UI
        public string[] Includes { get; set; }
    }

    [Api("Movie response with includes")]
    public class MovieResponse
    {
        public string[] Includes { get; set; }
    }

    [Api("CRUD for ServiceProviders")]
    [Route("/ServiceProvider/{Id}", "Delete", Summary = "Delete a ServiceProvider by Id")]
    [ApiResponse(HttpStatusCode.InternalServerError, "Something went wrong. Please contact the support team")]
    public class DeleteServiceProviderRequestDto : IReturn<DeleteServiceProviderReponseDto>
    {
        [ApiMember(ParameterType = "path", IsRequired = true)]
        public int Id { get; set; }
        [ApiMember(DataType = "boolean")]
        public bool ForceDelete { get; set; }
    }

    public class DeleteServiceProviderReponseDto : IHasResponseStatus
    {
        public ResponseStatus ResponseStatus { get; set; }
    }


    public class AnnotatedService : Service
    {
        public object Any(GetMovie request) => new MovieResponse {Includes = request.Includes};

        public object Any(GatewayCredentialRequest request) => new GatewayCredentialResponse {Result = "hello"};

        public object Any(CreateHelloReq request) => new Hello { Name = request.Hello.Name };

        public object Any(DeleteServiceProviderRequestDto request) => new DeleteServiceProviderReponseDto { ResponseStatus = new ResponseStatus() {ErrorCode = "200"}};
    }
}