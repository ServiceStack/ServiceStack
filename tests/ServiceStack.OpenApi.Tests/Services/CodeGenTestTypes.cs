using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OpenApi.Tests.Services
{
    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        [Required]
        public string Name { get; set; }
        public string Title { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    public class HelloWithNestedClass : IReturn<HelloResponse>
    {
        public string Name { get; set; }
        public NestedClass NestedClassProp { get; set; }

        // This will generate a class definition "public partial class Hello.NestedClass"
        public class NestedClass
        {
            public string Value { get; set; }
        }
    }

    public class ListResult
    {
        public string Result { get; set; }
    }

    public class ArrayResult
    {
        public string Result { get; set; }
    }

    [Route("/hello-list")]
    public class HelloList : IReturn<List<ListResult>>
    {
        public List<string> Names { get; set; }
    }

    [Route("/hello-array")]
    public class HelloArray : IReturn<ArrayResult[]>
    {
        public List<string> Names { get; set; }
    }

    public class HelloWithEnum
    {
        public EnumType EnumProp { get; set; }
        public EnumType? NullableEnumProp { get; set; }

        public EnumFlags EnumFlags { get; set; }
    }

    public enum EnumType
    {
        Value1,
        Value2
    }

    [Flags]
    public enum EnumFlags
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 4,
    }

    [Restrict(InternalOnly = true)]
    [System.ComponentModel.Description("Description on HelloAll type")]
    [DataContract]
    public class HelloAnnotated
        : IReturn<HelloAnnotatedResponse>
    {
        [DataMember]
        public string Name { get; set; }
    }

    [Restrict(ExternalOnly = true)]
    public class HelloExternal
    {
        public string Name { get; set; }
    }

    [Restrict(InternalOnly = true)]
    [Alias("Alias")]
    public class RestrictedAttributes
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [Index]
        [ApiAllowableValues("DateKind", typeof(DateTimeKind))]
        public string Name { get; set; }

        public Hello Hello { get; set; }
    }

    [DataContract]
    [Route("/allowed-attributes", "GET")]
    [Api("AllowedAttributes Description")]
    [ApiResponse(HttpStatusCode.BadRequest, "Your request was not understood")]
    [Description("Description on AllowedAttributes")]
    public class AllowedAttributes
    {
        [Required]
        [Range(1, 10)]
        [Default(5)]
        public int Id { get; set; }

        [Range(1.0, 10.0)]
        [DataMember(Name = "Aliased")]
        [ApiMember(Description = "Range Description",
                   ParameterType = "query", DataType = "integer", IsRequired = true)]
        public double Range { get; set; }

        [StringLength(20)]
        [References(typeof(Hello))]
        [Meta("Foo", "Bar")]
        public string Name { get; set; }
    }

    [System.ComponentModel.Description("Description on HelloAllResponse type")]
    [DataContract]
    public class HelloAnnotatedResponse
    {
        [DataMember]
        public string Result { get; set; }
    }

    [Route("/all-types")]
    public class HelloAllTypes
    {
        public string Name { get; set; }
        public AllTypes AllTypes { get; set; }
        public AllCollectionTypes AllCollectionTypes { get; set; }
    }

    [Route("/all-types-result")]
    public class HelloAllTypesWithResult : IReturn<HelloAllTypesResponse>
    {
        public string Name { get; set; }
        public AllTypes AllTypes { get; set; }
        public AllCollectionTypes AllCollectionTypes { get; set; }
    }


    public class HelloAllTypesResponse
    {
        public string Result { get; set; }
        public AllTypes AllTypes { get; set; }
        public AllCollectionTypes AllCollectionTypes { get; set; }
    }

    [Route("/hello-string")]
    public class HelloString : IReturn<string>
    {
        public string Name { get; set; }
    }

    [Route("/hello-datetime")]
    public class HelloDateTime : IReturn<HelloDateTime>
    {
        public DateTime DateTime { get; set; }
    }

    [Route("/hello-void")]
    public class HelloVoid
    {
        public string Name { get; set; }
    }

    [DataContract]
    public class HelloWithDataContract
    {
        [DataMember(Name = "name", Order = 1, IsRequired = true, EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "id", Order = 2, EmitDefaultValue = false)]
        public int Id { get; set; }
    }

    [DataContract]
    public class HelloWithDataContractResponse
    {
        [DataMember(Name = "result", Order = 1, IsRequired = true, EmitDefaultValue = false)]
        public string Result { get; set; }
    }

    [System.ComponentModel.Description("Description on HelloWithDescription type")]
    public class HelloWithDescription
    {
        public string Name { get; set; }
    }

    [System.ComponentModel.Description("Description on HelloWithDescriptionResponse type")]
    public class HelloWithDescriptionResponse
    {
        public string Result { get; set; }
    }

    public class HelloWithInheritance
        : HelloBase
    {
        public string Name { get; set; }
    }

    public class HelloWithInheritanceResponse
        : HelloResponseBase
    {
        public string Result { get; set; }
    }

    public class HelloWithGenericInheritance : HelloBase<Poco>
    {
        public string Result { get; set; }
    }

    public class HelloWithGenericInheritance2 : HelloBase<Hello>
    {
        public string Result { get; set; }
    }

    public class HelloWithNestedInheritance : HelloBase<HelloWithNestedInheritance.Item>
    {
        public class Item
        {
            public string Value { get; set; }
        }
    }

    public class HelloWithListInheritance : List<InheritedItem> { }

    public class InheritedItem
    {
        public string Name { get; set; }
    }

    public abstract class HelloBase<T>
    {
        public List<T> Items { get; set; }
        public virtual List<int> Counts { get; set; }
    }

    public class HelloWithReturn
        : IReturn<HelloWithAlternateReturnResponse>
    {
        public string Name { get; set; }
    }

    public class HelloWithAlternateReturnResponse
        : HelloWithReturnResponse
    {
        public string AltResult { get; set; }
    }

    [Route("/helloroute")]
    public class HelloWithRoute
    {
        public string Name { get; set; }
    }

    public class HelloWithRouteResponse
    {
        public string Result { get; set; }
    }

    public class HelloWithType
    {
        public string Name { get; set; }
    }

    public class HelloWithTypeResponse
    {
        public HelloType Result { get; set; }
    }

    public class HelloInterface
    {
        public IPoco Poco { get; set; }
        public IEmptyInterface EmptyInterface { get; set; }
        public EmptyClass EmptyClass { get; set; }
    }

    public interface IPoco
    {
        string Name { get; set; }
    }

    public interface IEmptyInterface { }
    public class EmptyClass { }

    public class TypesGroup
    {
        public class InnerType
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }

        public enum InnerEnum
        {
            Foo,
            Bar,
            Baz
        }
    }

    public class HelloInnerTypes : IReturn<HelloInnerTypesResponse> { }

    public class HelloInnerTypesResponse
    {
        public TypesGroup.InnerType InnerType { get; set; }

        public TypesGroup.InnerEnum InnerEnum { get; set; }
    }

    public class HelloBuiltin
    {
        public DayOfWeek DayOfWeek { get; set; }
    }

    public class HelloVerbResponse
    {
        public string Result { get; set; }
    }

    public class HelloGet : IReturn<HelloVerbResponse>, IGet
    {
        public int Id { get; set; }
    }
    public class HelloPost : HelloBase, IReturn<HelloVerbResponse>, IPost
    {
    }
    public class HelloPut : IReturn<HelloVerbResponse>, IPut
    {
        public int Id { get; set; }
    }
    public class HelloDelete : IReturn<HelloVerbResponse>, IDelete
    {
        public int Id { get; set; }
    }
    public class HelloPatch : IReturn<HelloVerbResponse>, IPatch
    {
        public int Id { get; set; }
    }

    public class HelloReturnVoid : IReturnVoid
    {
        public int Id { get; set; }
    }

    public class EnumRequest : IReturn<EnumResponse>, IPut
    {
        public ScopeType Operator { get; set; }
    }

    public class EnumResponse
    {
        public ScopeType Operator { get; set; }
    }

    [DataContract]
    public enum ScopeType
    {
        [EnumMember]
        Global = 1,
        [EnumMember]
        Sale = 2,
    }

    [Route("/hellotypes")]
    public class HelloTypes : IReturn<HelloTypes>
    {
        public string String { get; set; }
        public bool Bool { get; set; }
        public int Int { get; set; }
    }

    [DataContract]
    [Route("/hellozip")]
    public class HelloZip : IReturn<HelloZipResponse>
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> Test { get; set; }
    }

    [DataContract]
    public class HelloZipResponse
    {
        [DataMember]
        public string Result { get; set; }
    }
}