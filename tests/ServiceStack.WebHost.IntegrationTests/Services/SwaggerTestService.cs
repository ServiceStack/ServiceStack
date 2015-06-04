using System.Drawing;
using System.Net;
using System.Runtime.Serialization;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    public enum MyColor
    {
        Red,
        Green,
        Blue
    }

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
        [ApiAllowableValues("Name", typeof(MyColor))] //Enum
        [DataMember]
        public string Name { get; set; }

        [ApiMember]
        [ApiAllowableValues("Color", typeof(MyColor))] //Enum
        [DataMember]
        public MyColor Color { get; set; }

        [ApiMember(Description = "Aliased Description",
                   DataType = "string", IsRequired = true)]
        [DataMember(Name = "Aliased")]
        public string Original { get; set; }

        [ApiMember(Description = "Not Aliased Description",
                   DataType = "string", IsRequired = true)]
        [DataMember]
        public string NotAliased { get; set; }

        [ApiMember(Description = "Nested model 1", DataType = "SwaggerNestedModel")]
        [DataMember]
        public SwaggerNestedModel NestedModel1 { get; set; }

        [ApiMember(Description = "Nested model 2", DataType = "SwaggerNestedModel2")]
        [DataMember]
        public SwaggerNestedModel2 NestedModel2 { get; set; }
    }

    public class SwaggerNestedModel
    {
        [ApiMember(Description = "NestedProperty description")]
        public bool NestedProperty { get; set; }
    }

    public class SwaggerNestedModel2
    {
        [ApiMember(Description = "NestedProperty2 description")]
        public bool NestedProperty2 { get; set; }

        [ApiMember(Description = "MultipleValues description")]
        [ApiAllowableValues("MultipleValues", new[] { "val1", "val2" })]
        public string MultipleValues { get; set; }

        [ApiMember(Description = "TestRange description")]
        [ApiAllowableValues("TestRange", 1, 10)]
        public int TestRange { get; set; }
    }
    
    public enum MyEnum { A, B, C }

    [Route("/swaggertest2", "POST")]
    public class SwaggerTest2
    {
        [ApiMember]
        [ApiAllowableValues("MyEnumProperty", typeof(MyEnum))]
        public MyEnum MyEnumProperty { get; set; }

        [IgnoreDataMember]
        public string Ignored { get; set; }
    }

    public class SwaggerTestService : Service
    {
        public object Any(SwaggerTest request)
        {
            return request;
        }

        public object Post(SwaggerTest2 request)
        {
            return request;
        }
    }
}