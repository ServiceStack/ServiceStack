using System.Drawing;
using System.Net;
using System.Runtime.Serialization;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Api("SwaggerTest Service Description")]
    [ApiResponse(HttpStatusCode.BadRequest, "Your request was not understood")]
    [ApiResponse(HttpStatusCode.InternalServerError, "Oops, something broke")]
    [Route("/swagger", "GET", Summary = @"GET / Summary", Notes = "GET / Notes")]
    [Route("/swagger/{Name}", "GET", Summary = @"GET Summary", Notes = "GET /Name Notes")]
    [Route("/swagger/{Name}", "POST", Summary = @"POST Summary", Notes = "POST /Name Notes")]
    [DataContract]
    public class SwaggerTest
    {
        [ApiMember(Description = "Color Description",
                   ParameterType = "path", DataType = "string", IsRequired = true)]
        [ApiAllowableValues("Color", typeof(Color))] //Enum
        public string Color { get; set; }

        [ApiMember(Description = "Aliased Description",
                   ParameterType = "path", DataType = "string", IsRequired = true)]
        [DataMember(Name = "Aliased")]
        public string Name { get; set; }

        [ApiMember(Description = "Not Aliased Description",
                   ParameterType = "path", DataType = "string", IsRequired = true)]
        public string NotAliased { get; set; }
    }

    public class SwaggerTestService : Service
    {
         public object Get(SwaggerTest request)
         {
             return request;
         }
    }
}