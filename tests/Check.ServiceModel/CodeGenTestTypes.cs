using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using Check.ServiceModel.Types;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Check.ServiceModel.Operations
{
    [Route("/hello/{Name}")]
    public class Hello
    {
        public string Name { get; set; }
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
    [Route("/allowed-attributes","GET")]
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
                   ParameterType = "path", DataType = "double", IsRequired = true)]
        public double Range { get; set; }

        [StringLength(20)]
        [References(typeof(Hello))]
        [Meta("Foo","Bar")]
        public string Name { get; set; }
    }

    [System.ComponentModel.Description("Description on HelloAllResponse type")]
    [DataContract]
    public class HelloAnnotatedResponse
    {
        [DataMember]
        public string Result { get; set; }
    }

    public class HelloAllTypes
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

    public class HelloString
    {
        public string Name { get; set; }
    }

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

    public class HelloWithListInheritance : List<InheritedItem> {}

    public class InheritedItem
    {
        public string Name { get; set; }
    }

    public abstract class HelloBase<T>
    {
        public List<T> Items { get; set; }
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
}

namespace Check.ServiceModel.Types
{
    public class AllTypes
    {
        public int Id { get; set; }
        public int? NullableId { get; set; }
        public byte Byte { get; set; }
        public short Short { get; set; }
        public int Int { get; set; }
        public long Long { get; set; }
        public UInt16 UShort { get; set; }
        public uint UInt { get; set; }
        public ulong ULong { get; set; }
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
        public string String { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public TimeSpan? NullableTimeSpan { get; set; }
        public List<string> StringList { get; set; }
        public string[] StringArray { get; set; }
        public Dictionary<string, string> StringMap { get; set; }
        public Dictionary<int, string> IntStringMap { get; set; }
        public SubType SubType { get; set; }
    }

    public class AllCollectionTypes
    {
        public int[] IntArray { get; set; }
        public List<int> IntList { get; set; }

        public string[] StringArray { get; set; }
        public List<string> StringList { get; set; }

        public Poco[] PocoArray { get; set; }
        public List<Poco> PocoList { get; set; }
    }

    public class Poco
    {
        public string Name { get; set; }
    }

    public class HelloBase
    {
        public int Id { get; set; }
    }

    public class HelloResponseBase
    {
        public int RefId { get; set; }
    }

    public class HelloType
    {
        public string Result { get; set; }
    }

    public class HelloWithReturnResponse
    {
        public string Result { get; set; }
    }

    public class SubType
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
