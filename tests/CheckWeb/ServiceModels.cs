/* Options:
Version: 1
BaseUrl: http://localhost:55799

ServerVersion: 1
MakePartial: True
MakeVirtual: True
MakeDataContractsExtensible: False
AddReturnMarker: True
AddDescriptionAsComments: True
AddDataContractAttributes: False
AddDataAnnotationAttributes: False
AddIndexesToDataMembers: False
AddResponseStatus: False
AddImplicitVersion: 
InitializeCollections: True
AddDefaultXmlNamespace: http://schemas.servicestack.net/types
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using Check.ServiceModel.Types;
using Check.ServiceModel;
using Check.ServiceInterface;
using Check.ServiceModel.Operations;


namespace Check.ServiceInterface
{

    public partial class ACSProfile
        : IReturn<acsprofileResponse>
    {
        public virtual string profileId { get; set; }
        public virtual string shortName { get; set; }
        public virtual string longName { get; set; }
        public virtual string regionId { get; set; }
        public virtual string groupId { get; set; }
        public virtual string deviceID { get; set; }
        public virtual DateTime lastUpdated { get; set; }
        public virtual bool enabled { get; set; }
    }

    public partial class acsprofileResponse
    {
        public virtual string profileId { get; set; }
    }

    public partial class AnonType
    {
    }

    public partial class ChangeRequest
        : IReturn<ChangeRequest>
    {
        public virtual string Id { get; set; }
    }

    public partial class ChangeRequestResponse
    {
        public virtual string ContentType { get; set; }
        public virtual string Header { get; set; }
        public virtual string QueryString { get; set; }
        public virtual string Form { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
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
        public virtual string Sentence { get; set; }
    }

    ///<summary>
    ///Echoes a sentence
    ///</summary>
    public partial class Echoes
        : IReturn<Echo>
    {
        [ApiMember(ParameterType="form", Name="Sentence", Description="The sentence to echo.", DataType="string", IsRequired=true, AllowMultiple=false)]
        public virtual string Sentence { get; set; }
    }

    public partial class QueryRockstars
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
    }

    public partial class Rockstar
    {
        public virtual int Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual int? Age { get; set; }
    }

    public partial class ThrowHttpError
    {
        public virtual int Status { get; set; }
        public virtual string Message { get; set; }
    }
}

namespace Check.ServiceModel.Operations
{

    public partial class Hello
        : IReturn<Hello>
    {
        public virtual string Name { get; set; }
    }

    ///<summary>
    ///Description on HelloAll type
    ///</summary>
    [DataContract]
    public partial class HelloAll
        : IReturn<HelloAllResponse>
    {
        [DataMember]
        public virtual string Name { get; set; }
    }

    ///<summary>
    ///Description on HelloAllResponse type
    ///</summary>
    [DataContract]
    public partial class HelloAllResponse
    {
        [DataMember]
        public virtual string Result { get; set; }
    }

    public partial class HelloAllTypes
        : IReturn<HelloAllTypes>
    {
        public virtual string Name { get; set; }
        public virtual AllTypes AllTypes { get; set; }
    }

    public partial class HelloAllTypesResponse
    {
        public virtual string Result { get; set; }
        public virtual AllTypes AllTypes { get; set; }
    }

    public partial class HelloResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloString
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloVoid
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloWithAlternateReturnResponse
        : HelloWithReturnResponse
    {
        public virtual string AltResult { get; set; }
    }

    [DataContract]
    public partial class HelloWithDataContract
        : IReturn<HelloWithDataContract>
    {
        [DataMember(Name="name", Order=1, IsRequired=true, EmitDefaultValue=false)]
        public virtual string Name { get; set; }

        [DataMember(Name="id", Order=2, EmitDefaultValue=false)]
        public virtual int Id { get; set; }
    }

    [DataContract]
    public partial class HelloWithDataContractResponse
    {
        [DataMember(Name="result", Order=1, IsRequired=true, EmitDefaultValue=false)]
        public virtual string Result { get; set; }
    }

    ///<summary>
    ///Description on HelloWithDescription type
    ///</summary>
    public partial class HelloWithDescription
        : IReturn<HelloWithDescription>
    {
        public virtual string Name { get; set; }
    }

    ///<summary>
    ///Description on HelloWithDescriptionResponse type
    ///</summary>
    public partial class HelloWithDescriptionResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithInheritance
        : HelloBase, IReturn<HelloWithInheritance>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloWithInheritanceResponse
        : HelloResponseBase
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithReturn
        : IReturn<HelloWithAlternateReturnResponse>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloWithRoute
        : IReturn<HelloWithRoute>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloWithRouteResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithType
        : IReturn<HelloWithType>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloWithTypeResponse
    {
        public virtual HelloType Result { get; set; }
    }
}

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

        public virtual int Id { get; set; }
        public virtual int? NullableId { get; set; }
        public virtual byte Byte { get; set; }
        public virtual short Short { get; set; }
        public virtual int Int { get; set; }
        public virtual long Long { get; set; }
        public virtual ushort UShort { get; set; }
        public virtual uint UInt { get; set; }
        public virtual ulong ULong { get; set; }
        public virtual float Float { get; set; }
        public virtual double Double { get; set; }
        public virtual decimal Decimal { get; set; }
        public virtual string String { get; set; }
        public virtual DateTime DateTime { get; set; }
        public virtual TimeSpan TimeSpan { get; set; }
        public virtual DateTime? NullableDateTime { get; set; }
        public virtual TimeSpan? NullableTimeSpan { get; set; }
        public virtual List<string> StringList { get; set; }
        public virtual string[] StringArray { get; set; }
        public virtual Dictionary<string, string> StringMap { get; set; }
        public virtual Dictionary<int, string> IntStringMap { get; set; }
        public virtual SubType SubType { get; set; }
    }

    public partial class HelloBase
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloResponseBase
    {
        public virtual int RefId { get; set; }
    }

    public partial class HelloType
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithReturnResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class SubType
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }
}


