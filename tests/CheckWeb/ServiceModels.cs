/* Options:
Version: 1
BaseUrl: 

ServerVersion: 1
MakePartial: True
MakeVirtual: False
MakeDataContractsExtensible: False
AddReturnMarker: True
AddDescriptionAsComments: True
AddDataContractAttributes: False
AddDataAnnotationAttributes: False
AddDefaultXmlNamespace: http://schemas.servicestack.net/types
AddIndexesToDataMembers: False
AddResponseStatus: False
AddImplicitVersion: 
InitializeCollections: True
DefaultNamespaces: System, System.Collections, System.Collections.Generic, System.Runtime.Serialization, ServiceStack, ServiceStack.DataAnnotations
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using Check.ServiceModel.Types;
using Check.ServiceInterface;
using Check.ServiceModel.Operations;
using Check.ServiceModel;

#region Operations


namespace Check.ServiceInterface
{

    public partial class ACSProfile
        : IReturn<acsprofileResponse>
    {
        public string profileId { get; set; }
        public string shortName { get; set; }
        public string longName { get; set; }
        public string regionId { get; set; }
        public string groupId { get; set; }
        public string deviceID { get; set; }
        public DateTime lastUpdated { get; set; }
        public bool enabled { get; set; }
    }

    public partial class acsprofileResponse
    {
        public string profileId { get; set; }
    }

    public partial class AnonType
    {
    }

    public partial class ChangeRequest
        : IReturn<ChangeRequestResponse>
    {
        public string Id { get; set; }
    }

    public partial class ChangeRequestResponse
    {
        public string ContentType { get; set; }
        public string Header { get; set; }
        public string QueryString { get; set; }
        public string Form { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
}

namespace Check.ServiceModel
{

    public partial class AsyncTest
        : IReturn<Echo>
    {
    }

    public partial class Echo
    {
        public string Sentence { get; set; }
    }

    ///<summary>
    ///Echoes a sentence
    ///</summary>
    public partial class Echoes
        : IReturn<Echo>
    {
        [ApiMember(Name="Sentence", DataType="string", Description="The sentence to echo.", IsRequired=true, ParameterType="form", AllowMultiple=false)]
        public string Sentence { get; set; }
    }

    public partial class QueryRockstars
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
    }

    public partial class ThrowHttpError
    {
        public int Status { get; set; }
        public string Message { get; set; }
    }
}

namespace Check.ServiceModel.Operations
{

    public partial class Hello
        : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public partial class HelloResponse
    {
        public string Result { get; set; }
    }

    ///<summary>
    ///Description on HelloAll type
    ///</summary>
    [DataContract]
    public partial class HelloAll
        : IReturn<HelloAllResponse>
    {
        [DataMember]
        public string Name { get; set; }
    }

    ///<summary>
    ///Description on HelloAllResponse type
    ///</summary>
    [DataContract]
    public partial class HelloAllResponse
    {
        [DataMember]
        public string Result { get; set; }
    }

    public partial class HelloAllTypes
        : IReturn<HelloAllTypesResponse>
    {
        public string Name { get; set; }
        public AllTypes AllTypes { get; set; }
    }

    public partial class HelloAllTypesResponse
    {
        public string Result { get; set; }
        public AllTypes AllTypes { get; set; }
    }

    public partial class HelloString
    {
        public string Name { get; set; }
    }

    public partial class HelloVoid
    {
        public string Name { get; set; }
    }

    [DataContract]
    public partial class HelloWithDataContract
        : IReturn<HelloWithDataContractResponse>
    {
        [DataMember(Name="name", Order=1, IsRequired=true, EmitDefaultValue=false)]
        public string Name { get; set; }

        [DataMember(Name="id", Order=2, EmitDefaultValue=false)]
        public int Id { get; set; }
    }

    [DataContract]
    public partial class HelloWithDataContractResponse
    {
        [DataMember(Name="result", Order=1, IsRequired=true, EmitDefaultValue=false)]
        public string Result { get; set; }
    }

    ///<summary>
    ///Description on HelloWithDescription type
    ///</summary>
    public partial class HelloWithDescription
        : IReturn<HelloWithDescriptionResponse>
    {
        public string Name { get; set; }
    }

    ///<summary>
    ///Description on HelloWithDescriptionResponse type
    ///</summary>
    public partial class HelloWithDescriptionResponse
    {
        public string Result { get; set; }
    }

    public partial class HelloWithInheritance
        : HelloBase, IReturn<HelloWithInheritanceResponse>
    {
        public string Name { get; set; }
    }

    public partial class HelloWithInheritanceResponse
        : HelloResponseBase
    {
        public string Result { get; set; }
    }

    public partial class HelloWithReturn
        : IReturn<HelloWithAlternateReturnResponse>
    {
        public string Name { get; set; }
    }

    public partial class HelloWithAlternateReturnResponse
        : HelloWithReturnResponse
    {
        public string AltResult { get; set; }
    }

    public partial class HelloWithRoute
        : IReturn<HelloWithRouteResponse>
    {
        public string Name { get; set; }
    }

    public partial class HelloWithRouteResponse
    {
        public string Result { get; set; }
    }

    public partial class HelloWithType
        : IReturn<HelloWithTypeResponse>
    {
        public string Name { get; set; }
    }

    public partial class HelloWithTypeResponse
    {
        public HelloType Result { get; set; }
    }
}

#endregion


#region Types


namespace Check.ServiceModel.Types
{

    public partial class AllTypes
    {
        public AllTypes()
        {
            StringList = new List<string>{};
            StringArray = new string[]{};
            StringMap = new Dictionary<string, string>{};
            IntStringMap = new Dictionary<int, string>{};
        }

        public int Id { get; set; }
        public int? NullableId { get; set; }
        public byte Byte { get; set; }
        public short Short { get; set; }
        public int Int { get; set; }
        public long Long { get; set; }
        public ushort UShort { get; set; }
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

    public partial class HelloBase
    {
        public int Id { get; set; }
    }

    public partial class HelloResponseBase
    {
        public int RefId { get; set; }
    }

    public partial class HelloType
    {
        public string Result { get; set; }
    }

    public partial class HelloWithReturnResponse
    {
        public string Result { get; set; }
    }

    public partial class SubType
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}

#endregion

