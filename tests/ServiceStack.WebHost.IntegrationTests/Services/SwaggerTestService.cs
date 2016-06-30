using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Policy;
using ServiceStack.DataAnnotations;

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

        [DataMember]
        [ApiMember(IsRequired = false, AllowMultiple = true)]
        public DateTime[] MyDateBetween { get; set; }

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

    [Route("/swagger-complex", "POST")]
    public class SwaggerComplex : IReturn<SwaggerComplexResponse>
    {
        [ApiMember]
        [DataMember]
        [Description("IsRequired Description")]
        public bool IsRequired { get; set; }

        [ApiMember(IsRequired = true)]
        [DataMember]
        public string[] ArrayString { get; set; }

        [ApiMember]
        [DataMember]
        public int[] ArrayInt { get; set; }

        [ApiMember]
        [DataMember]
        public List<string> ListString { get; set; }

        [ApiMember]
        [DataMember]
        public List<int> ListInt { get; set; }

        [ApiMember]
        [DataMember]
        public Dictionary<string, string> DictionaryString { get; set; }
    }

    public class SwaggerComplexResponse
    {
        [ApiMember]
        [DataMember]
        public bool IsRequired { get; set; }

        [ApiMember(IsRequired = true)]
        [DataMember]
        public string[] ArrayString { get; set; }

        [ApiMember]
        [DataMember]
        public int[] ArrayInt { get; set; }

        [ApiMember]
        [DataMember]
        public List<string> ListString { get; set; }

        [ApiMember]
        [DataMember]
        public List<int> ListInt { get; set; }

        [ApiMember]
        [DataMember]
        public Dictionary<string, string> DictionaryString { get; set; }
    }

    [Route("/swaggerpost/{Required1}", Verbs = "GET")]
    [Route("/swaggerpost/{Required1}/{Optional1}", Verbs = "GET")]
    [Route("/swaggerpost", Verbs = "POST")]
    public class SwaggerPostTest : IReturn<HelloResponse>
    {
        [ApiMember(Verb = "POST")]
        [ApiMember(Route = "/swaggerpost/{Required1}", Verb = "GET", ParameterType = "path")]
        [ApiMember(Route = "/swaggerpost/{Required1}/{Optional1}", Verb = "GET", ParameterType = "path")]
        public string Required1 { get; set; }

        [ApiMember(Verb = "POST")]
        [ApiMember(Route = "/swaggerpost/{Required1}/{Optional1}", Verb = "GET", ParameterType = "path")]
        public string Optional1 { get; set; }
    }

    [Route("/swaggerpost2/{Required1}/{Required2}", Verbs = "GET")]
    [Route("/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verbs = "GET")]
    [Route("/swaggerpost2", Verbs = "POST")]
    public class SwaggerPostTest2 : IReturn<HelloResponse>
    {
        [ApiMember(Route = "/swaggerpost2/{Required1}/{Required2}", Verb = "GET", ParameterType = "path")]
        [ApiMember(Route = "/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb = "GET", ParameterType = "path")]
        public string Required1 { get; set; }

        [ApiMember(Route = "/swaggerpost2/{Required1}/{Required2}", Verb = "GET", ParameterType = "path")]
        [ApiMember(Route = "/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb = "GET", ParameterType = "path")]
        public string Required2 { get; set; }

        [ApiMember(Route = "/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb = "GET", ParameterType = "path")]
        public string Optional1 { get; set; }
    }

    [Api("Api GET All")]
    [Route("/swaggerexamples", "GET")]
    public class GetSwaggerExamples : IReturn<GetSwaggerExamples>
    {
        public string Get { get; set; }
    }

    [Api("Api POST")]
    [Route("/swaggerexamples", "POST")]
    public class PostSwaggerExamples : IReturn<PostSwaggerExamples>
    {
        public string Post { get; set; }
    }

    [Api("Api GET Id")]
    [Route("/swaggerexamples/{Id}", "GET")]
    public class GetSwaggerExample : IReturn<GetSwaggerExample>
    {
        public int Id { get; set; }
        public string Get { get; set; }
    }

    [Api("Api PUT Id")]
    [Route("/swaggerexamples/{Id}", "PUT")]
    public class PutSwaggerExample : IReturn<PutSwaggerExample>
    {
        public int Id { get; set; }
        public string Get { get; set; }
    }

    [Route("/lists", "GET")]
    public class GetLists : IReturn<GetLists>
    {
        public string Id { get; set; }
    }

    [Route("/lists", "POST")]
    [Exclude(Feature.Metadata)]
    public class CreateList : IReturn<CreateList>
    {
        public string Id { get; set; }
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

        public object Post(SwaggerComplex request)
        {
            return request.ConvertTo<SwaggerComplexResponse>();
        }

        public object Any(SwaggerPostTest request)
        {
            return new HelloResponse { Result = request.Required1 };
        }

        public object Any(SwaggerPostTest2 request)
        {
            return new HelloResponse { Result = request.Required1 };
        }

        public object Any(GetSwaggerExamples request)
        {
            return request;
        }

        public object Any(GetSwaggerExample request)
        {
            return request;
        }

        public object Any(PostSwaggerExamples request)
        {
            return request;
        }

        public object Any(PutSwaggerExample request)
        {
            return request;
        }

        public object Any(GetLists request)
        {
            return request;
        }

        public object Any(CreateList request)
        {
            return request;
        }
    }
}