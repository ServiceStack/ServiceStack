using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Check.ServiceInterface.Operations;
using Check.ServiceInterface.Types;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class DtoGetTestService : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse { Result = request.Name };
        }

        public object Any(HelloAll request)
        {
            return new HelloAllResponse { Result = request.Name };
        }

        public object Any(HelloAllTypes request)
        {
            return new HelloAllTypesResponse { AllTypes = request.AllTypes, Result = request.Name };
        }

        public object Any(HelloString request)
        {
            return request.Name;
        }

        public void Any(HelloVoid request)
        {
        }

        public object Any(HelloWithDataContract request)
        {
            return new HelloWithDataContractResponse { Result = request.Name };
        }

        public object Any(HelloWithDescription request)
        {
            return new HelloWithDescriptionResponse { Result = request.Name };
        }

        public object Any(HelloWithInheritance request)
        {
            return new HelloWithInheritanceResponse { Result = request.Name };
        }

        public object Any(HelloWithReturn request)
        {
            return new HelloWithAlternateReturnResponse { Result = request.Name };
        }

        public object Any(HelloWithRoute request)
        {
            return new HelloWithRouteResponse { Result = request.Name };
        }

        public object Any(HelloWithType request)
        {
            return new HelloWithTypeResponse
            {
                Result = new HelloType { Result = request.Name }
            };
        }
    }
}

namespace Check.ServiceInterface.Operations
{
    public class Hello
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    [Description("Description on HelloAll type")]
    [DataContract]
    public class HelloAll
        : IReturn<HelloAllResponse>
    {
        [DataMember]
        public string Name { get; set; }
    }

    [Description("Description on HelloAllResponse type")]
    [DataContract]
    public class HelloAllResponse
    {
        [DataMember]
        public string Result { get; set; }
    }

    public class HelloAllTypes
    {
        public string Name { get; set; }
        public AllTypes AllTypes { get; set; }
    }

    public class HelloAllTypesResponse
    {
        public string Result { get; set; }
        public AllTypes AllTypes { get; set; }
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

    [Description("Description on HelloWithDescription type")]
    public class HelloWithDescription
    {
        public string Name { get; set; }
    }

    [Description("Description on HelloWithDescriptionResponse type")]
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

namespace Check.ServiceInterface.Types
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
