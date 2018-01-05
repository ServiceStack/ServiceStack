﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.Serialization;
using Check.ServiceModel.Types;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Check.ServiceModel.Operations
{
    [System.ComponentModel.Description("Description for HelloACodeGenTest")]
    public class HelloACodeGenTest
    {
        [Description("Description for FirstField")]
        public int FirstField { get; set; }

        public List<string> SecondFields { get; set; }
    }

    [DataContract]
    public class HelloACodeGenTestResponse
    {
        [DataMember]
        [Description("Description for FirstResult")]
        public int FirstResult { get; set; }

        [DataMember]
        [ApiMember(Description = "Description for SecondResult")]
        public int SecondResult { get; set; }
    }

    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello
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

    public class OnlyInReturnListArg
    {
        public string Result { get; set; }
    }

    public class ArrayResult
    {
        public string Result { get; set; }
    }

    public class HelloList : IReturn<List<ListResult>>
    {
        public List<string> Names { get; set; }
    }

    public class HelloReturnList : IReturn<List<OnlyInReturnListArg>>
    {
        public List<string> Names { get; set; }
    }

    public class HelloArray : IReturn<ArrayResult[]>
    {
        public List<string> Names { get; set; }
    }

    public class HelloExisting : IReturn<HelloExistingResponse>
    {
        public List<string> Names { get; set; }
    }

    public class HelloExistingResponse
    {
        public HelloList HelloList { get; set; }
        public HelloArray HelloArray { get; set; }
        public ArrayResult[] ArrayResults { get; set; }
        public List<ListResult> ListResults { get; set; }
    }
    
    public class HelloWithEnum
    {
        public EnumType EnumProp { get; set; }
        public EnumWithValues EnumWithValues { get; set; }
        public EnumType? NullableEnumProp { get; set; }

        public EnumFlags EnumFlags { get; set; }
    }

    public enum EnumType
    {
        Value1,
        Value2
    }

    public enum EnumWithValues
    {
        Value1 = 1,
        Value2 = 2
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
    [Api(@"AllowedAttributes Description")]
    [ApiResponse(HttpStatusCode.BadRequest, "Your request was not understood")]
    [Description("Description on AllowedAttributes")]
    public class AllowedAttributes
    {
        [Required]
        [Range(1, 10)]
        [Default(5)]
        [DataMember]
        public int Id { get; set; }

        [Range(1.0, 10.0)]
        [DataMember(Name = "Aliased")]
        [ApiMember(Description = "Range Description",
                   ParameterType = "path", DataType = "double", IsRequired = true)]
        public double Range { get; set; }

        [StringLength(20)]
        [References(typeof(Hello))]
        [Meta("Foo", "Bar")]
        public string Name { get; set; }
    }

    [Api(@"Multi 
Line 
Class")]
    public class HelloMultiline
    {
        [ApiMember(Description = @"Multi 
Line 
Property")]
        public string Overflow { get; set; }
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

    public class HelloString : IReturn<string>
    {
        public string Name { get; set; }
    }

    public class HelloVoid : IReturnVoid
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
        public List<int> Counts { get; set; }
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
    public class AllTypes : IReturn<AllTypes>
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
        public DateTimeOffset DateTimeOffset { get; set; }
        public Guid Guid { get; set; }
        public Char Char { get; set; }
        public KeyValuePair<string,string> KeyValuePair { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public TimeSpan? NullableTimeSpan { get; set; }
        public List<string> StringList { get; set; }
        public string[] StringArray { get; set; }
        public Dictionary<string, string> StringMap { get; set; }
        public Dictionary<int, string> IntStringMap { get; set; }
        public SubType SubType { get; set; }
        public Point Point { get; set; }

        [DataMember(Name = "aliasedName")]
        public string OriginalName { get; set; }
    }

    public class AllCollectionTypes
    {
        public int[] IntArray { get; set; }
        public List<int> IntList { get; set; }

        public string[] StringArray { get; set; }
        public List<string> StringList { get; set; }

        public Poco[] PocoArray { get; set; }
        public List<Poco> PocoList { get; set; }

        public byte?[] NullableByteArray { get; set; }
        public List<byte?> NullableByteList { get; set; }

        public DateTime?[] NullableDateTimeArray { get; set; }
        public List<DateTime?> NullableDateTimeList { get; set; }

        public Dictionary<string, List<Poco>> PocoLookup { get; set; }
        public Dictionary<string, List<Dictionary<string,Poco>>> PocoLookupMap { get; set; } 
    }

    public class Poco
    {
        public string Name { get; set; }
    }

    public struct Point
    {
        public Point(double x=0, double y=0) : this()
        {
            X = x;
            Y = y;
        }

        public Point(string point) : this()
        {
            var parts = point.Split(',');
            X = double.Parse(parts[0]);
            Y = double.Parse(parts[1]);
        }

        public double X { get; set; }
        public double Y { get; set; }

        public override string ToString()
        {
            return X.ToString(CultureInfo.InvariantCulture) + "," + Y.ToString(CultureInfo.InvariantCulture);
        }
    }

    public class HelloStruct : IReturn<HelloStruct>
    {
        public Point Point { get; set; }
        public Point? NullablePoint { get; set; }
    }

    public abstract class HelloBase
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

    public class HelloSession : IReturn<HelloSessionResponse>
    {        
    }

    public class HelloSessionResponse
    {
        public AuthUserSession Result { get; set; }
    }

    public class HelloInterface : IGenericInterface<string>
    {
        public IPoco Poco { get; set; }
        public IEmptyInterface EmptyInterface { get; set; }
        public EmptyClass EmptyClass { get; set; }
        public string Value { get; set; }
        //public IGenericInterface<string> GenericInterface { get; set; }
    }

    public class HelloImplementsInterface : IReturn<HelloImplementsInterface>, ImplementsPoco
    {
        public string Name { get; set; }
    }

    public interface ImplementsPoco
    {
        string Name { get; set; }
    }

    public interface IPoco
    {
        string Name { get; set; }
    }

    public interface IEmptyInterface {}
    public class EmptyClass {}

    public interface IGenericInterface<T>
    {
        T Value { get; }
    }

    /// <summary>
    /// Duplicate Types
    /// </summary>
    public class TypeB
    {
        public string Foo { get; set; }
    }

    public class TypeA
    {
        public List<TypeB> Bar { get; set; }
    }

    public class Request1 : IReturn<Request1Response>
    {
        public TypeA Test { get; set; }
    }

    public class Request1Response
    {
        public TypeA Test { get; set; }
    }

    public class Request2 : IReturn<Request2Response>
    {
        public TypeA Test { get; set; }
    }

    public class Request2Response
    {
        public TypeA Test { get; set; }
    }

    public class TypesGroup
    {
        public class InnerType
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }

        public class InnerTypeItem
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

        public List<TypesGroup.InnerTypeItem> InnerList { get; set; }
    }

    public class QueryTemplate : IReturn<QueryResponseTemplate<Poco>> {}

    [DataContract]
    public class QueryResponseTemplate<T> : IHasResponseStatus, IMeta
    {
        [DataMember(Order = 1)]
        public virtual int Offset { get; set; }

        [DataMember(Order = 2)]
        public virtual int Total { get; set; }

        [DataMember(Order = 3)]
        public virtual List<T> Results { get; set; }

        [DataMember(Order = 4)]
        public virtual Dictionary<string, string> Meta { get; set; }

        [DataMember(Order = 5)]
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    public class HelloReserved
    {
        public string Class { get; set; }
        public string Type { get; set; }
        public string extension { get; set; }
    }

    public class HelloDictionary : IReturn<Dictionary<string, string>>
    {
        public string Key { get; set; }
        public string Value { get; set; }
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

    public class ExcludeTest1 : IReturn<ExcludeTestNested>
    {
    }

    public class ExcludeTest2 : IReturn<string>
    {
        public ExcludeTestNested ExcludeTestNested { get; set; }
    }

    public class ExcludeTestNested
    {
        public int Id { get; set; }
    }


    [Exclude(Feature.Metadata)]
    public class ExcludeMetadata : IReturn<ExcludeMetadata>
    {
        public int Id { get; set; }
    }

    [Restrict(LocalhostOnly = true)]
    public class RestrictLocalhost : IReturn<RestrictLocalhost>
    {
        public int Id { get; set; }
    }

    [Restrict(InternalOnly = true)]
    public class RestrictInternal : IReturn<RestrictInternal>
    {
        public int Id { get; set; }
    }

    [Restrict(ExternalOnly = true)]
    public class RestrictExternal : IReturn<RestrictExternal>
    {
        public int Id { get; set; }
    }

    public class IgnoreInMetadataConfig : IReturn<IgnoreInMetadataConfig>
    {
        public int Id { get; set; }
    }

    public class HelloTuple : IReturn<HelloTuple>
    {
        public Tuple<string,long> Tuple2 { get; set; }
        public Tuple<string,long,bool> Tuple3 { get; set; }

        public List<Tuple<string, long>> Tuples2 { get; set; }
        public List<Tuple<string, long, bool>> Tuples3 { get; set; }
    }

    public class HelloAuthenticated : IReturn<HelloAuthenticatedResponse>, IHasSessionId
    {
        public string SessionId { get; set; }
        public int Version { get; set; }
    }

    public class HelloAuthenticatedResponse
    {
        public int Version { get; set; }
        public string SessionId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsAuthenticated { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
}


